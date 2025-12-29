using System.Security.Claims;
using ProjectsDonetskWaterHope.Data;
using ProjectsDonetskWaterHope.DTOs;
using ProjectsDonetskWaterHope.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectsDonetskWaterHope.Endpoints
{
    public static class ConsumptionEndpoints
    {
        public static void MapConsumptionEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/consumption").RequireAuthorization();

            group.MapPost("/", async (CreateConsumptionDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                var device = await db.Devices
                    .Include(d => d.Tariff)
                    .FirstOrDefaultAsync(d => d.DeviceId == dto.DeviceId);

                if (device == null)
                    return Results.BadRequest(new { error = "Пристрій не знайдено." });

                bool isAdmin = context.User.IsInRole("Admin");
                bool isOwner = device.UserId == currentUserId;

                if (!isAdmin && !isOwner)
                {
                    return Results.Json(new { error = "Ви не маєте прав вносити показники для цього пристрою." }, statusCode: 403);
                }

                var lastRecord = await db.ConsumptionRecords
                     .Where(r => r.DeviceId == dto.DeviceId)
                     .OrderByDescending(r => r.CreatedAt)
                     .FirstOrDefaultAsync();

                int delta = 0;
                decimal cost = 0;

                if (lastRecord != null)
                {
                    int previousValue = lastRecord.Value;
                    delta = dto.CurrentValue - previousValue;

                    if (delta < 0)
                    {
                        return Results.BadRequest(new
                        {
                            error = $"Новий показник ({dto.CurrentValue}) не може бути меншим за попередній ({previousValue})."
                        });
                    }

                    cost = delta * device.Tariff.PricePerUnit;
                }
                else
                {
                    delta = 0;
                    cost = 0;
                }

                var record = new ConsumptionRecord
                {
                    DeviceId = dto.DeviceId,
                    Value = dto.CurrentValue,
                    Delta = delta,      
                    MustToPay = cost,  
                    CreatedAt = DateTime.UtcNow,
                    TariffId = device.TariffId
                };

                db.ConsumptionRecords.Add(record);
                await db.SaveChangesAsync();

                return Results.Created($"/api/consumption/{record.ConsumptionRecordId}", new
                {
                    message = "Показники внесено успішно",
                    delta = delta,
                    toPay = cost
                });
            }).WithTags("User");

            group.MapGet("/device/{deviceId}", async (int deviceId, HttpContext context, ApplicationDbContext db) =>
            {
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                var device = await db.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.DeviceId == deviceId);

                if (device == null)
                    return Results.NotFound(new { error = "Пристрій не знайдено." });

                bool isAdmin = context.User.IsInRole("Admin");
                bool isOwner = device.UserId == currentUserId;

                if (!isAdmin && !isOwner)
                {
                    return Results.Json(new { error = "Доступ заборонено. Ви можете переглядати історію лише своїх пристроїв." }, statusCode: 403);
                }

                var records = await db.ConsumptionRecords
                    .AsNoTracking()
                    .Where(r => r.DeviceId == deviceId)
                    .Include(r => r.Tariff)
                    .Include(r => r.Device)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ConsumptionRecordDto(
                        r.ConsumptionRecordId,
                        r.Value,
                        r.Delta,
                        r.MustToPay,
                        r.CreatedAt,
                        r.Tariff.Name,
                        r.Tariff.PricePerUnit,
                        r.Device.SerialNumber
                    ))
                    .ToListAsync();

                return Results.Ok(records);
            }).WithTags("Public");

            group.MapGet("/my", async (HttpContext context, ApplicationDbContext db) =>
            {
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                var records = await db.ConsumptionRecords
                    .AsNoTracking()
                    .Where(r => r.Device.UserId == currentUserId)
                    .Include(r => r.Tariff)
                    .Include(r => r.Device)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ConsumptionRecordDto(
                        r.ConsumptionRecordId,
                        r.Value,
                        r.Delta,
                        r.MustToPay,
                        r.CreatedAt,
                        r.Tariff.Name,
                        r.Tariff.PricePerUnit,
                        r.Device.SerialNumber
                    ))
                    .ToListAsync();

                return Results.Ok(records);
            }).WithTags("User");

            group.MapDelete("/{id}", async (int id, HttpContext context, ApplicationDbContext db) =>
            {
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Тільки адміністратор може видаляти фінансові записи." }, statusCode: 403);

                var record = await db.ConsumptionRecords.FindAsync(id);
                if (record == null) return Results.NotFound();

                db.ConsumptionRecords.Remove(record);
                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Запис про споживання видалено." });
            }).WithTags("Admin");
        }
    }
}