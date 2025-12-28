using System.Security.Claims;
using ProjectsDonetskWaterHope.Data;
using ProjectsDonetskWaterHope.DTOs;
using ProjectsDonetskWaterHope.Models;
using ProjectsDonetskWaterHope.Services;
using Microsoft.EntityFrameworkCore;

namespace ProjectsDonetskWaterHope.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            // --- РЕЄСТРАЦІЯ ---
            app.MapPost("/api/auth/register", async (ApplicationDbContext db, RegisterUserDto dto, IServiceProvider services, LoggerService logger) =>
            {
                // Guard Clause 1: Email зайнятий
                if (await db.Users.AnyAsync(u => u.Email == dto.Email))
                    return Results.BadRequest(new { error = "Користувач з таким Email вже існує" });

                // Логіку генерації винесено в окремий метод (див. нижче)
                string newAccountNumber = await GenerateUniqueAccountNumber(db);

                var user = new User
                {
                    AccountNumber = newAccountNumber,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    PasswordHash = PasswordHasher.HashPassword(dto.Password),
                    Role = "User"
                };

                db.Users.Add(user);
                await db.SaveChangesAsync();

                // Тут могла бути відправка листа...
                await logger.LogAsync("UserRegistered", $"Новий користувач: {user.FirstName} {user.LastName} ({user.AccountNumber})", user.UserId);

               
                return Results.Ok(new UserDto(
                    user.UserId, user.AccountNumber, user.FirstName, user.LastName, user.Email, user.PhoneNumber, user.Role
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
            app.MapGet("/api/users/{id}", async (int id, ApplicationDbContext db) =>
            {
                var userDto = await db.Users
                    .Where(u => u.UserId == id)
                    .Select(u => new UserDto(
                        u.UserId, u.AccountNumber, u.FirstName, u.LastName, u.Email, u.PhoneNumber, u.Role
                    ))
                    .FirstOrDefaultAsync();

                return userDto is not null
                    ? Results.Ok(userDto)
                    : Results.NotFound(new { message = "Користувача не знайдено" });
            }).RequireAuthorization();

            // --- ЗМІНА ДАНИХ (PATCH) ---
            app.MapPatch("/api/users/{id}", async (int id, UpdateUserDto dto, HttpContext context, ApplicationDbContext db) =>
            {
                // Отримуємо ID поточного юзера
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                bool isAdmin = context.User.IsInRole("Admin"); // Використовуємо IsInRole
                bool isSelf = currentUserId == id;

                // Guard Clause: Перевірка прав
                if (!isSelf && !isAdmin)
                    return Results.Json(new { error = "Недостатньо прав для редагування цього профілю" }, statusCode: 403);

                var user = await db.Users.FindAsync(id);
                if (user == null) return Results.NotFound();

                // Оновлення полів (тільки якщо прийшли нові дані)
                if (!string.IsNullOrWhiteSpace(dto.FirstName)) user.FirstName = dto.FirstName;
                if (!string.IsNullOrWhiteSpace(dto.LastName)) user.LastName = dto.LastName;
                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber;

                // Адмінські зміни
                if (isAdmin)
                {
                    if (!string.IsNullOrWhiteSpace(dto.Role)) user.Role = dto.Role;

                    if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
                    {
                        if (await db.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != id))
                            return Results.BadRequest(new { error = "Email вже зайнятий іншим користувачем" });
                        user.Email = dto.Email;
                    }
                }

                await db.SaveChangesAsync();

                return Results.Ok(new UserDto(
                    user.UserId, user.AccountNumber, user.FirstName, user.LastName, user.Email, user.PhoneNumber, user.Role
                ));
            }).RequireAuthorization();

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