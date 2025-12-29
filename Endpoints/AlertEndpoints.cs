using System.Security.Claims;
using ProjectsDonetskWaterHope.Data;
using ProjectsDonetskWaterHope.DTOs;
using ProjectsDonetskWaterHope.Models;
using Microsoft.EntityFrameworkCore;
using ProjectsDonetskWaterHope.Services;

namespace ProjectsDonetskWaterHope.Endpoints
{
    public static class AlertEndpoints
    {
        public static void MapAlertEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/alerts").RequireAuthorization();

            // --- 1. СТВОРЕННЯ СПОВІЩЕННЯ (Тільки Admin) ---
            group.MapPost("/", async (CreateAlertDto dto, ApplicationDbContext db, HttpContext context, LoggerService logger) =>
            {
                // 1. Отримуємо ID того, хто стукає (User або Admin)
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                // 2. Шукаємо пристрій
                var device = await db.Devices.FirstOrDefaultAsync(d => d.DeviceId == dto.DeviceId);
                if (device == null)
                    return Results.BadRequest(new { error = "Пристрій з вказаним ID не знайдено." });

                // 3. ВАЖЛИВО: Перевірка прав (Адмін АБО Власник)
                // Це дозволяє IoT-пристрою (Власнику) відправляти дані без помилки 403
                bool isAdmin = context.User.IsInRole("Admin");
                bool isOwner = device.UserId == currentUserId;

                if (!isAdmin && !isOwner)
                {
                    return Results.Json(new { error = "Ви не можете надсилати сповіщення від чужого пристрою." }, statusCode: 403);
                }

                // 4. Створення сповіщення
                var alert = new Alert
                {
                    DeviceId = dto.DeviceId,
                    MessageText = dto.MessageText,
                    Type = dto.Type,
                    CreatedAt = DateTime.UtcNow
                };

                db.Alerts.Add(alert);
                await db.SaveChangesAsync();

                // 5. ЛОГУВАННЯ ПОДІЇ (Тільки якщо це критична помилка)
                if (dto.Type == "Critical")
                {
                    // Записуємо в системний лог, щоб адмін бачив це в історії
                    await logger.LogAsync(
                        "LeakDetected",
                        $"УВАГА! Витік води на пристрої {dto.DeviceId}: {dto.MessageText}",
                        device.UserId, // Прив'язуємо до власника
                        dto.DeviceId   // Прив'язуємо до пристрою
                    );
                }

                return Results.Created($"/api/alerts/{alert.AlertId}", new { message = "Сповіщення створено." });
            });

            // --- 2. ОТРИМАННЯ ВСІХ (Тільки Admin) ---
            group.MapGet("/all", async (HttpContext context, ApplicationDbContext db) =>
            {
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Доступ заборонено." }, statusCode: 403);

                var alerts = await db.Alerts
                    .AsNoTracking()
                    .Include(a => a.Device).ThenInclude(d => d.User) // Підтягуємо власника
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new AlertDto(
                        a.AlertId, a.MessageText, a.Type, a.CreatedAt,
                        a.Device.SerialNumber,
                        a.Device.User.AccountNumber
                    ))
                    .ToListAsync();

                return Results.Ok(alerts);
            });

            // --- 3. ОТРИМАННЯ "МОЇХ" (Для User) ---
            // Показує сповіщення по ВСІХ пристроях, що належать юзеру
            group.MapGet("/my", async (HttpContext context, ApplicationDbContext db) =>
            {
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                var alerts = await db.Alerts
                    .AsNoTracking()
                    // Фільтруємо: беремо алерти, де Device належить поточному User
                    .Where(a => a.Device.UserId == currentUserId)
                    .Include(a => a.Device).ThenInclude(d => d.User)
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new AlertDto(
                        a.AlertId, a.MessageText, a.Type, a.CreatedAt,
                        a.Device.SerialNumber,
                        a.Device.User.AccountNumber
                    ))
                    .ToListAsync();

                return Results.Ok(alerts);
            });

            // --- 4. ОТРИМАННЯ ПО КОНКРЕТНОМУ ПРИСТРОЮ (Smart Access) ---
            group.MapGet("/device/{deviceId}", async (int deviceId, HttpContext context, ApplicationDbContext db) =>
            {
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                // 1. Перевіряємо пристрій і чий він
                var device = await db.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.DeviceId == deviceId);

                if (device == null)
                    return Results.NotFound(new { message = "Пристрій не знайдено." });

                // 2. Перевірка прав (Адмін або Власник)
                bool isAdmin = context.User.IsInRole("Admin");
                bool isOwner = device.UserId == currentUserId;

                if (!isAdmin && !isOwner)
                    return Results.Json(new { error = "Ви не маєте доступу до сповіщень цього пристрою." }, statusCode: 403);

                // 3. Вибірка сповіщень
                var alerts = await db.Alerts
                    .AsNoTracking()
                    .Where(a => a.DeviceId == deviceId)
                    .Include(a => a.Device).ThenInclude(d => d.User)
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new AlertDto(
                        a.AlertId, a.MessageText, a.Type, a.CreatedAt,
                        a.Device.SerialNumber,
                        a.Device.User.AccountNumber
                    ))
                    .ToListAsync();

                return Results.Ok(alerts);
            });

            // --- 5. ВИДАЛЕННЯ (Тільки Admin) ---
            // Користувачі не можуть видаляти історію аварій, тільки адмін може чистити базу
            group.MapDelete("/{id}", async (int id, HttpContext context, ApplicationDbContext db) =>
            {
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Тільки адміністратор може видаляти сповіщення." }, statusCode: 403);

                var alert = await db.Alerts.FindAsync(id);
                if (alert == null) return Results.NotFound();

                db.Alerts.Remove(alert);
                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Сповіщення видалено." });
            });
        }
    }
}