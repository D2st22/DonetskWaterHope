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

            group.MapPost("/", async (CreateAlertDto dto, ApplicationDbContext db, HttpContext context, LoggerService logger) =>
            {

                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                var allowedTypes = new[] { "Info", "Warning", "Critical", "Leakage", "BatteryLow" };
                if (!allowedTypes.Contains(dto.Type))
                {
                    return Results.BadRequest(new
                    {
                        error = $"Недопустимий тип сповіщення. Дозволені типи: {string.Join(", ", allowedTypes)}"
                    });
                }

                var device = await db.Devices.FirstOrDefaultAsync(d => d.DeviceId == dto.DeviceId);
                if (device == null)
                    return Results.BadRequest(new { error = "Пристрій з вказаним ID не знайдено." });

                bool isAdmin = context.User.IsInRole("Admin");
                bool isOwner = device.UserId == currentUserId;

                if (!isAdmin && !isOwner)
                {
                    return Results.Json(new { error = "Ви не можете надсилати сповіщення від чужого пристрою." }, statusCode: 403);
                }

                var alert = new Alert
                {
                    DeviceId = dto.DeviceId,
                    MessageText = dto.MessageText,
                    Type = dto.Type,
                    CreatedAt = DateTime.UtcNow
                };

                db.Alerts.Add(alert);
                await db.SaveChangesAsync();

                if (dto.Type == "Critical" || dto.Type == "Leakage")
                {
                    await logger.LogAsync(
                        "LeakDetected",
                        $"УВАГА! Критична подія на пристрої {dto.DeviceId}: {dto.MessageText}",
                        device.UserId,
                        dto.DeviceId
                    );
                }

                return Results.Created($"/api/alerts/{alert.AlertId}", new { message = "Сповіщення створено успішно." });
            }).WithTags("System");

            group.MapGet("/all", async (HttpContext context, ApplicationDbContext db) =>
            {
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Доступ заборонено." }, statusCode: 403);

                var alerts = await db.Alerts
                    .AsNoTracking()
                    .Include(a => a.Device).ThenInclude(d => d.User) 
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new AlertDto(
                        a.AlertId, a.MessageText, a.Type, a.CreatedAt,
                        a.Device.SerialNumber,
                        a.Device.User.AccountNumber
                    ))
                    .ToListAsync();

                return Results.Ok(alerts);
            }).WithTags("Admin");

            group.MapGet("/my", async (HttpContext context, ApplicationDbContext db) =>
            {
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                var alerts = await db.Alerts
                    .AsNoTracking()
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
            }).WithTags("User");

            group.MapGet("/device/{deviceId}", async (int deviceId, HttpContext context, ApplicationDbContext db) =>
            {

                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                var device = await db.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.DeviceId == deviceId);

                if (device == null)
                    return Results.NotFound(new { message = "Пристрій не знайдено." });

                bool isAdmin = context.User.IsInRole("Admin");
                bool isOwner = device.UserId == currentUserId;

                if (!isAdmin && !isOwner)
                    return Results.Json(new { error = "Ви не маєте доступу до сповіщень цього пристрою." }, statusCode: 403);

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
            }).WithTags("Public");

            group.MapDelete("/{id}", async (int id, HttpContext context, ApplicationDbContext db) =>
            {
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Тільки адміністратор може видаляти сповіщення." }, statusCode: 403);

                var alert = await db.Alerts.FindAsync(id);
                if (alert == null) return Results.NotFound();

                db.Alerts.Remove(alert);
                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Сповіщення видалено." });
            }).WithTags("Admin");
        }
    }
}