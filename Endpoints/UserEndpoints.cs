using System.Security.Claims;
using ProjectsDonetskWaterHope.Data;
using ProjectsDonetskWaterHope.DTOs;
using ProjectsDonetskWaterHope.Models;
using ProjectsDonetskWaterHope.Services;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail; // Для простої перевірки Email
using ProjectsDonetskWaterHope.Validation;

namespace ProjectsDonetskWaterHope.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            // --- РЕЄСТРАЦІЯ ---
            app.MapPost("/api/auth/register", async (
     ApplicationDbContext db,
     RegisterUserDto dto,
     LoggerService logger) =>
            {
                if (string.IsNullOrWhiteSpace(dto.Email))
                    return Results.BadRequest(new { error = "Email обовʼязковий" });

                var normalizedEmail = dto.Email.ToLower().Trim();

                // 1. Валідація Email
                if (!IsValidEmail(normalizedEmail))
                    return Results.BadRequest(new { error = "Некоректний формат Email" });

                // 2. Перевірка унікальності Email
                if (await db.Users.AnyAsync(u => u.Email == normalizedEmail))
                    return Results.BadRequest(new { error = "Користувач з таким Email вже існує" });

                // 3. Валідація телефону
                var phoneAttr = new UkrainianPhoneAttribute();
                if (!phoneAttr.IsValid(dto.PhoneNumber))
                    return Results.BadRequest(new { error = "Некоректний формат українського номера телефону" });

                var normalizedPhone = UkrainianPhoneAttribute.NormalizePhone(dto.PhoneNumber);
                var newAccountNumber = await GenerateUniqueAccountNumber(db);

                var user = new User
                {
                    AccountNumber = newAccountNumber,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = normalizedEmail,
                    PhoneNumber = normalizedPhone,
                    PasswordHash = PasswordHasher.HashPassword(dto.Password),
                    Role = "User"
                };

                db.Users.Add(user);

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Results.Conflict(new { error = "Користувач з таким Email вже існує" });
                }

                await logger.LogAsync(
                    "UserRegistered",
                    $"Новий користувач: {user.FirstName} ({user.AccountNumber})",
                    user.UserId
                );

                return Results.Ok(new UserDto(
                    user.UserId,
                    user.AccountNumber,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.PhoneNumber,
                    user.Role
                ));
            });

            // --- ЛОГІН ---
            app.MapPost("/api/auth/login", async (ApplicationDbContext db, TokenService tokenService, LoginDto dto) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.AccountNumber == dto.AccountNumber);

                // Guard Clause: Невірні дані
                if (user == null || !PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash))
                    return Results.BadRequest(new { error = "Невірний особовий рахунок або пароль" });

                var tokenString = tokenService.GenerateToken(user);

                return Results.Ok(new AuthResponseDto(
                    Token: tokenString,
                    User: new UserDto(
                        user.UserId, user.AccountNumber, user.FirstName, user.LastName, user.Email, user.PhoneNumber, user.Role
                    )
                ));
            });

            // --- ОТРИМАННЯ ПРОФІЛЮ ---
            app.MapPatch("/api/users/{id}", async (
                int id,
                UpdateUserDto dto,
                HttpContext context,
                ApplicationDbContext db) =>
            {
                // Отримуємо ID поточного користувача з токена
                if (!int.TryParse(
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    out int currentUserId))
                    return Results.Unauthorized();

                bool isAdmin = context.User.IsInRole("Admin");
                bool isSelf = currentUserId == id;

                // --- ПЕРЕВІРКА ПРАВ ДОСТУПУ ---
                // Якщо користувач намагається змінити не себе і він не адмін
                if (!isSelf && !isAdmin)
                {
                    return Results.Json(new { error = "Вам заборонено змінювати дані інших користувачів" }, statusCode: 403);
                }

                var user = await db.Users.FindAsync(id);
                if (user == null)
                    return Results.NotFound(new { error = "Користувача не знайдено" });

                // --- ІМʼЯ / ПРІЗВИЩЕ ---
                if (!string.IsNullOrWhiteSpace(dto.FirstName))
                    user.FirstName = dto.FirstName;

                if (!string.IsNullOrWhiteSpace(dto.LastName))
                    user.LastName = dto.LastName;

                // --- ТЕЛЕФОН ---
                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    var phoneAttr = new UkrainianPhoneAttribute();
                    if (!phoneAttr.IsValid(dto.PhoneNumber))
                        return Results.BadRequest(new { error = "Некоректний формат телефону" });

                    user.PhoneNumber = UkrainianPhoneAttribute.NormalizePhone(dto.PhoneNumber);
                }

                // --- EMAIL ---
                if (!string.IsNullOrWhiteSpace(dto.Email))
                {
                    var normalizedEmail = dto.Email.ToLower().Trim();
                    if (!IsValidEmail(normalizedEmail))
                        return Results.BadRequest(new { error = "Некоректний формат Email" });

                    if (normalizedEmail != user.Email)
                    {
                        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail && u.UserId != id))
                            return Results.BadRequest(new { error = "Email вже зайнятий іншим акаунтом" });

                        user.Email = normalizedEmail;
                    }
                }

                // --- ROLE (ТІЛЬКИ ADMIN) ---
                if (!string.IsNullOrWhiteSpace(dto.Role))
                {
                    if (!isAdmin)
                    {
                        return Results.Json(new { error = "Користувачам заборонено змінювати свою роль" }, statusCode: 403);
                    }
                    user.Role = dto.Role;
                }

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Results.Conflict(new { error = "Помилка при оновленні даних у базі" });
                }

                return Results.Ok(new UserDto(
                    user.UserId,
                    user.AccountNumber,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.PhoneNumber,
                    user.Role
                ));
            })
            .RequireAuthorization();

            // --- ОТРИМАННЯ ВСІХ (Тільки Admin) ---
            app.MapGet("/api/users", async (HttpContext context, ApplicationDbContext db) =>
            {
                // Guard Clause: Тільки адмін
                if (!context.User.IsInRole("Admin"))
                {
                    return Results.Json(new { error = "Доступ заборонено. Потрібні права адміністратора." }, statusCode: 403);
                }

                var users = await db.Users
                    .Select(u => new UserDto(
                        u.UserId, u.AccountNumber, u.FirstName, u.LastName, u.Email, u.PhoneNumber, u.Role
                    ))
                    .ToListAsync();

                return Results.Ok(users);
            }).RequireAuthorization();

            // --- ВИДАЛЕННЯ (Тільки Admin + Безпека даних) ---
            app.MapDelete("/api/users/{id}", async (int id, HttpContext context, ApplicationDbContext db) =>
            {
                // 1. Guard Clause: Безпека
                if (!context.User.IsInRole("Admin"))
                {
                    return Results.Json(new { error = "Тільки адміністратор може видаляти користувачів." }, statusCode: 403);
                }

                // 2. Пошук
                var user = await db.Users.FindAsync(id);
                if (user == null)
                    return Results.NotFound(new { message = "Користувача не знайдено." });

                // 3. Видалення з обробкою помилок (Try-Catch)
                try
                {
                    db.Users.Remove(user);
                    await db.SaveChangesAsync();
                    return Results.Ok(new { message = $"Користувача {user.AccountNumber} видалено." });
                }
                catch (DbUpdateException)
                {
                    // Якщо база не дає видалити через зв'язки з Devices/Tickets
                    return Results.Conflict(new { error = "Неможливо видалити користувача: у нього є активні лічильники або звернення." });
                }
            }).RequireAuthorization();
        }
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch { return false; }
        }
        // --- PRIVATE HELPER METHOD (Чистий код: винесли складну логіку) ---
        private static async Task<string> GenerateUniqueAccountNumber(ApplicationDbContext db)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string newAccountNumber;
            bool exists;

            do
            {
                var randomPart = new string(Enumerable.Repeat(chars, 8)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                newAccountNumber = $"WH-{randomPart}";
                exists = await db.Users.AnyAsync(u => u.AccountNumber == newAccountNumber);
            }
            while (exists);

            return newAccountNumber;
        }
    }
}