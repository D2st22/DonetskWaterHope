using System.Security.Claims;
using ProjectsDonetskWaterHope.Data;
using ProjectsDonetskWaterHope.DTOs;
using ProjectsDonetskWaterHope.Models;
using Microsoft.EntityFrameworkCore;
using ProjectsDonetskWaterHope.Services;

namespace ProjectsDonetskWaterHope.Endpoints
{
    public static class DeviceEndpoints
    {
        public static void MapDeviceEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/devices").RequireAuthorization();

            // --- 1. ОТРИМАННЯ МОЇХ ПРИСТРОЇВ (User) ---
            group.MapGet("/my", async (HttpContext context, ApplicationDbContext db) =>
            {
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                var devices = await db.Devices
                    .AsNoTracking()
                    .Where(d => d.UserId == currentUserId)
                    .Include(d => d.Tariff)
                    .Include(d => d.User)
                    .Include(d => d.RegisteredByUser) // Нова назва
                    .Select(d => new DeviceDto(
                        d.DeviceId,
                        d.SerialNumber,
                        d.Name,
                        d.Type,
                        d.Status,
                        d.RegistrationAt,
                        // Уважно слідкуємо за порядком в DTO:
                        d.User.AccountNumber,                                // AccountNumber
                        d.RegisteredByUser != null ? d.RegisteredByUser.AccountNumber : null, // RegisteredByAdmin
                        d.Comment,                                           // Comment
                        d.Tariff.Name,                                       // TariffName
                        d.Tariff.PricePerUnit,                               // TariffPrice
                        d.UserId                                             // UserId
                    ))
                    .ToListAsync();

                return Results.Ok(devices);
            });

            // --- 2. ОТРИМАННЯ ВСІХ ПРИСТРОЇВ (Admin) ---
            group.MapGet("/", async (HttpContext context, ApplicationDbContext db) =>
            {
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Доступ заборонено." }, statusCode: 403);

                var devices = await db.Devices
                    .AsNoTracking()
                    .Include(d => d.Tariff)
                    .Include(d => d.User)
                    .Include(d => d.RegisteredByUser)
                    .Select(d => new DeviceDto(
                        d.DeviceId,
                        d.SerialNumber,
                        d.Name,
                        d.Type,
                        d.Status,
                        d.RegistrationAt,
                        d.User.AccountNumber,
                        d.RegisteredByUser != null ? d.RegisteredByUser.AccountNumber : "Невідомо",
                        d.Comment,
                        d.Tariff.Name,
                        d.Tariff.PricePerUnit,
                        d.UserId
                    ))
                    .ToListAsync();

                return Results.Ok(devices);
            });

            // --- 3. ОТРИМАННЯ ПО ID ---
            group.MapGet("/{id}", async (int id, HttpContext context, ApplicationDbContext db) =>
            {
                var device = await db.Devices
                    .Include(d => d.Tariff)
                    .Include(d => d.User)
                    .Include(d => d.RegisteredByUser)
                    .FirstOrDefaultAsync(d => d.DeviceId == id);

                if (device == null) return Results.NotFound(new { message = "Пристрій не знайдено" });

                var userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                bool isAdmin = context.User.IsInRole("Admin");
                bool isOwner = int.TryParse(userIdStr, out int uid) && device.UserId == uid;

                if (!isAdmin && !isOwner) return Results.Forbid();

                return Results.Ok(new DeviceDto(
                    device.DeviceId,
                    device.SerialNumber,
                    device.Name,
                    device.Type,
                    device.Status,
                    device.RegistrationAt,
                    device.User.AccountNumber,
                    device.RegisteredByUser?.AccountNumber,
                    device.Comment,
                    device.Tariff.Name,
                    device.Tariff.PricePerUnit,
                    device.UserId
                ));
            });

            // --- 4. ДОДАННЯ ПРИСТРОЮ (Admin) ---
            group.MapPost("/", async (CreateDeviceDto dto, ApplicationDbContext db, HttpContext context, LoggerService logger) =>
            {
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Тільки адміністратор може реєструвати пристрої." }, statusCode: 403);

                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int adminId))
                    return Results.Unauthorized();

                // Валідації
                if (!await db.Users.AnyAsync(u => u.UserId == dto.UserId))
                    return Results.BadRequest(new { error = "Вказаного користувача не існує." });

                if (!await db.Tariffs.AnyAsync(t => t.TariffId == dto.TariffId))
                    return Results.BadRequest(new { error = "Вказаного тарифу не існує." });

                if (await db.Devices.AnyAsync(d => d.SerialNumber == dto.SerialNumber))
                    return Results.BadRequest(new { error = "Пристрій з таким серійним номером вже зареєстровано." });

                var device = new Device
                {
                    SerialNumber = dto.SerialNumber,
                    Name = dto.Name,
                    Type = dto.Type,
                    Status = "Active",
                    RegistrationAt = DateTime.UtcNow,
                    RegisteredByUserId = adminId, // Використовуємо нове ім'я властивості
                    TariffId = dto.TariffId,
                    UserId = dto.UserId,
                    Comment = dto.Comment
                };

                db.Devices.Add(device);
                await db.SaveChangesAsync();
                // ЛОГУВАННЯ
                await logger.LogAsync("DeviceAdded", $"Додано пристрій {device.SerialNumber}", device.UserId, device.DeviceId);

                // Підвантажуємо для відповіді
                await db.Entry(device).Reference(d => d.Tariff).LoadAsync();
                await db.Entry(device).Reference(d => d.User).LoadAsync();
                await db.Entry(device).Reference(d => d.RegisteredByUser).LoadAsync();

                return Results.Created($"/api/devices/{device.DeviceId}", new DeviceDto(
                    device.DeviceId,
                    device.SerialNumber,
                    device.Name,
                    device.Type,
                    device.Status,
                    device.RegistrationAt,
                    device.User.AccountNumber,
                    device.RegisteredByUser?.AccountNumber,
                    device.Comment,
                    device.Tariff.Name,
                    device.Tariff.PricePerUnit,
                    device.UserId
                ));
            });

            // --- 5. ОНОВЛЕННЯ (Admin) ---
            group.MapPatch("/{id}", async (int id, UpdateDeviceAdminDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Тільки адміністратор може змінювати параметри пристрою." }, statusCode: 403);

                var device = await db.Devices
                    .Include(d => d.User)
                    .Include(d => d.RegisteredByUser) // Нова назва
                    .FirstOrDefaultAsync(d => d.DeviceId == id);

                if (device == null) return Results.NotFound(new { message = "Пристрій не знайдено" });

                if (!string.IsNullOrWhiteSpace(dto.Name)) device.Name = dto.Name;
                if (!string.IsNullOrWhiteSpace(dto.Status)) device.Status = dto.Status;
                if (!string.IsNullOrWhiteSpace(dto.Comment)) device.Comment = dto.Comment;

                if (dto.TariffId.HasValue)
                {
                    if (!await db.Tariffs.AnyAsync(t => t.TariffId == dto.TariffId))
                        return Results.BadRequest(new { error = "Нового тарифу не існує." });
                    device.TariffId = dto.TariffId.Value;
                }

                await db.SaveChangesAsync();
                await db.Entry(device).Reference(d => d.Tariff).LoadAsync();

                return Results.Ok(new DeviceDto(
                    device.DeviceId,
                    device.SerialNumber,
                    device.Name,
                    device.Type,
                    device.Status,
                    device.RegistrationAt,
                    device.User.AccountNumber,
                    device.RegisteredByUser?.AccountNumber,
                    device.Comment,
                    device.Tariff.Name,
                    device.Tariff.PricePerUnit,
                    device.UserId
                ));
            });
        }
    }
}