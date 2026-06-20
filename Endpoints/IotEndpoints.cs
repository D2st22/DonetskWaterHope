using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProjectsDonetskWaterHope.Data;
using ProjectsDonetskWaterHope.DTOs;
using ProjectsDonetskWaterHope.Models;
using ProjectsDonetskWaterHope.Services;

namespace ProjectsDonetskWaterHope.Endpoints
{
    public static class IotEndpoints
    {
        private static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(20);

        public static void MapIotEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/iot").RequireAuthorization();

            group.MapPost("/status", async (
                UpdateIotStatusDto dto,
                HttpContext context,
                ApplicationDbContext db,
                LoggerService logger) =>
            {
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                var device = await db.Devices.FirstOrDefaultAsync(d => d.DeviceId == dto.DeviceId);
                if (device == null)
                    return Results.BadRequest(new { error = "Device not found." });

                var isAdmin = context.User.IsInRole("Admin");
                var isOwner = device.UserId == currentUserId;
                if (!isAdmin && !isOwner)
                    return Results.Json(new { error = "You cannot update IoT status for another user's device." }, statusCode: 403);

                var status = await db.IotDeviceStatuses.FirstOrDefaultAsync(s => s.DeviceId == dto.DeviceId);
                if (status == null)
                {
                    status = new IotDeviceStatus { DeviceId = dto.DeviceId };
                    db.IotDeviceStatuses.Add(status);
                }

                status.RawSensorValue = dto.RawSensorValue;
                status.FlowRate = dto.FlowRate;
                status.TotalCounter = dto.TotalCounter;
                status.LeakageDetected = dto.LeakageDetected;
                status.WifiRssi = dto.WifiRssi;
                status.FirmwareVersion = string.IsNullOrWhiteSpace(dto.FirmwareVersion) ? "unknown" : dto.FirmwareVersion.Trim();
                status.LastSeenAt = DateTime.UtcNow;

                if (device.Status != "Active")
                    device.Status = "Active";

                await db.SaveChangesAsync();

                if (dto.LeakageDetected)
                {
                    await logger.LogAsync(
                        "IotLeakageSignal",
                        $"IoT device {dto.DeviceId} reported high flow: {dto.FlowRate}.",
                        device.UserId,
                        device.DeviceId
                    );
                }

                return Results.Ok(new { message = "IoT status updated.", lastSeenAt = status.LastSeenAt });
            }).WithTags("IoT");

            group.MapGet("/my", async (HttpContext context, ApplicationDbContext db) =>
            {
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                var statuses = await db.IotDeviceStatuses
                    .AsNoTracking()
                    .Include(s => s.Device).ThenInclude(d => d.User)
                    .Where(s => s.Device.UserId == currentUserId)
                    .OrderByDescending(s => s.LastSeenAt)
                    .ToListAsync();

                return Results.Ok(statuses.Select(ToDto));
            }).WithTags("User");

            group.MapGet("/all", async (HttpContext context, ApplicationDbContext db) =>
            {
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Access denied." }, statusCode: 403);

                var statuses = await db.IotDeviceStatuses
                    .AsNoTracking()
                    .Include(s => s.Device).ThenInclude(d => d.User)
                    .OrderByDescending(s => s.LastSeenAt)
                    .ToListAsync();

                return Results.Ok(statuses.Select(ToDto));
            }).WithTags("Admin");

            group.MapGet("/device/{deviceId}", async (int deviceId, HttpContext context, ApplicationDbContext db) =>
            {
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                var status = await db.IotDeviceStatuses
                    .AsNoTracking()
                    .Include(s => s.Device).ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(s => s.DeviceId == deviceId);

                if (status == null)
                    return Results.NotFound(new { error = "IoT status not found." });

                var isAdmin = context.User.IsInRole("Admin");
                var isOwner = status.Device.UserId == currentUserId;
                if (!isAdmin && !isOwner)
                    return Results.Json(new { error = "Access denied." }, statusCode: 403);

                return Results.Ok(ToDto(status));
            }).WithTags("Public");
        }

        private static IotDeviceStatusDto ToDto(IotDeviceStatus status)
        {
            var now = DateTime.UtcNow;
            return new IotDeviceStatusDto(
                status.DeviceId,
                status.Device.Name,
                status.Device.SerialNumber,
                status.Device.Status,
                status.Device.User.AccountNumber,
                status.RawSensorValue,
                status.FlowRate,
                status.TotalCounter,
                status.LeakageDetected,
                status.WifiRssi,
                status.FirmwareVersion,
                status.LastSeenAt,
                now - status.LastSeenAt <= OnlineWindow
            );
        }
    }
}
