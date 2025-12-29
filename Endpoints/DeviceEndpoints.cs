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

            group.MapGet("/my", async (HttpContext context, ApplicationDbContext db) =>
            {
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                var devices = await db.Devices
                    .AsNoTracking()
                    .Where(d => d.UserId == currentUserId)
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
                        d.RegisteredByUser != null ? d.RegisteredByUser.AccountNumber : null, 
                        d.Comment,                                         
                        d.Tariff.Name,                                      
                        d.Tariff.PricePerUnit,                              
                        d.UserId                                             
                    ))
                    .ToListAsync();

                return Results.Ok(devices);
            }).WithTags("User");

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
            }).WithTags("Admin");

group.MapGet("/{id}", async (int id, HttpContext context, ApplicationDbContext db) =>
{
    var device = await db.Devices
        .AsNoTracking() 
        .Include(d => d.Tariff)
        .Include(d => d.User)
        .Include(d => d.RegisteredByUser)
        .FirstOrDefaultAsync(d => d.DeviceId == id);

    if (device == null)
        return Results.NotFound(new { error = "Пристрій не знайдено" });

    var userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    bool isAdmin = context.User.IsInRole("Admin");
    bool isOwner = int.TryParse(userIdStr, out int uid) && device.UserId == uid;

    if (!isAdmin && !isOwner)
    {
        return Results.Json(new { error = "Доступ заборонено. Ви можете переглядати лише власні пристрої." }, statusCode: 403);
    }

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
}).WithTags("Public");

            group.MapPost("/", async (CreateDeviceDto dto, ApplicationDbContext db, HttpContext context, LoggerService logger) =>
            {
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Тільки адміністратор може реєструвати пристрої." }, statusCode: 403);

                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int adminId))
                    return Results.Unauthorized();

                var targetUser = await db.Users.FindAsync(dto.UserId);
                if (targetUser == null)
                    return Results.BadRequest(new { error = "Вказаного користувача не існує." });

                if (targetUser.Role == "Admin")
                {
                    return Results.BadRequest(new { error = "Заборонено реєструвати пристрої на облікові записи адміністраторів." });
                }

                if (!await db.Tariffs.AnyAsync(t => t.TariffId == dto.TariffId))
                    return Results.BadRequest(new { error = "Вказаного тарифу не існує." });
                var allowedTypes = new[] { "ColdWater", "HotWater" };
                if (string.IsNullOrWhiteSpace(dto.Type) || !allowedTypes.Contains(dto.Type))
                {
                    return Results.BadRequest(new
                    {
                        error = $"Некоректний тип пристрою. Дозволені типи: {string.Join(", ", allowedTypes)}"
                    });
                }
                if (await db.Devices.AnyAsync(d => d.SerialNumber == dto.SerialNumber))
                    return Results.BadRequest(new { error = "Пристрій з таким серійним номером вже зареєстровано." });

                var device = new Device
                {
                    SerialNumber = dto.SerialNumber,
                    Name = dto.Name,
                    Type = dto.Type,
                    Status = "Active",
                    RegistrationAt = DateTime.UtcNow,
                    RegisteredByUserId = adminId, 
                    TariffId = dto.TariffId,
                    UserId = dto.UserId,
                    Comment = dto.Comment
                };

                db.Devices.Add(device);
                await db.SaveChangesAsync();
                await logger.LogAsync("DeviceAdded", $"Додано пристрій {device.SerialNumber}", device.UserId, device.DeviceId);

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
            }).WithTags("Admin");

            group.MapPatch("/{id}", async (int id, UpdateDeviceAdminDto dto, ApplicationDbContext db, HttpContext context, LoggerService logger) =>
            {
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Тільки адміністратор може змінювати параметри пристрою." }, statusCode: 403);

                var device = await db.Devices
                    .Include(d => d.User)
                    .Include(d => d.RegisteredByUser)
                    .Include(d => d.Tariff)
                    .FirstOrDefaultAsync(d => d.DeviceId == id);

                if (device == null) return Results.NotFound(new { error = "Пристрій не знайдено" });

                if (!string.IsNullOrWhiteSpace(dto.SerialNumber) && dto.SerialNumber != device.SerialNumber)
                {
                    if (await db.Users.AnyAsync(u => u.AccountNumber == dto.SerialNumber)) 
                        return Results.BadRequest(new { error = "Пристрій з таким серійним номером вже зареєстровано в системі." });

                    device.SerialNumber = dto.SerialNumber;
                }

                if (!string.IsNullOrWhiteSpace(dto.Type))
                {
                    var allowedTypes = new[] { "ColdWater", "HotWater" };
                    if (!allowedTypes.Contains(dto.Type))
                        return Results.BadRequest(new { error = $"Недопустимий тип. Дозволені: {string.Join(", ", allowedTypes)}" });

                    device.Type = dto.Type;
                }

                if (!string.IsNullOrWhiteSpace(dto.Name)) device.Name = dto.Name;
                if (!string.IsNullOrWhiteSpace(dto.Comment)) device.Comment = dto.Comment;

                if (!string.IsNullOrWhiteSpace(dto.Status))
                {
                    var validStatuses = new[] { "Active", "Inactive", "Maintenance", "Blocked" };
                    if (!validStatuses.Contains(dto.Status))
                        return Results.BadRequest(new { error = "Некоректний статус пристрою." });
                    device.Status = dto.Status;
                }

                if (dto.TariffId.HasValue && dto.TariffId != device.TariffId)
                {
                    if (!await db.Tariffs.AnyAsync(t => t.TariffId == dto.TariffId))
                        return Results.BadRequest(new { error = "Вказаного тарифу не існує." });
                    device.TariffId = dto.TariffId.Value;
                }
                try
                {
                    await db.SaveChangesAsync();
                    await db.Entry(device).Reference(d => d.Tariff).LoadAsync();

                    await logger.LogAsync("DeviceUpdated", $"Адмін змінив параметри пристрою ID:{id}. Новий тип: {device.Type}", null, device.DeviceId);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Помилка БД при оновленні: " + ex.Message);
                }

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
            }).WithTags("Admin");
        }
    }
}