using System.Security.Claims;
using ProjectsDonetskWaterHope.Data;
using ProjectsDonetskWaterHope.DTOs;
using ProjectsDonetskWaterHope.Models;
using Microsoft.EntityFrameworkCore;
using ProjectsDonetskWaterHope.Services;

namespace ProjectsDonetskWaterHope.Endpoints
{
    public static class SupportTicketEndpoints
    {
        public static void MapSupportTicketEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/tickets").RequireAuthorization();

            group.MapPost("/", async (CreateTicketDto dto, ApplicationDbContext db, HttpContext context, LoggerService logger) =>
            {
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                // Валідація девайса
                if (dto.DeviceId.HasValue)
                {
                    bool isMyDevice = await db.Devices.AnyAsync(d => d.DeviceId == dto.DeviceId && d.UserId == currentUserId);
                    if (!isMyDevice)
                        return Results.BadRequest(new { error = "Ви не можете створити звернення щодо чужого пристрою." });
                }

                var ticket = new SupportTicket
                {
                    Subject = dto.Subject,
                    MessageText = dto.MessageText,
                    Status = "Open",
                    CreatedAt = DateTime.UtcNow,
                    UserId = currentUserId,
                    DeviceId = dto.DeviceId
                };

                db.SupportTickets.Add(ticket);
                await db.SaveChangesAsync();

                // ЛОГУВАННЯ
                await logger.LogAsync(
                    "TicketCreated",
                    $"Нове звернення: {ticket.Subject}",
                    currentUserId,
                    ticket.DeviceId
                );

                return Results.Created($"/api/tickets/{ticket.SupportTicketId}", new { message = "Звернення створено", id = ticket.SupportTicketId });
            });

            // --- 2. ОТРИМАННЯ ВСІХ ТІКЕТІВ (Тільки Admin) ---
            group.MapGet("/all", async (HttpContext context, ApplicationDbContext db) =>
            {
                // Guard Clause: Тільки Адмін
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Доступ заборонено. Потрібні права адміністратора." }, statusCode: 403);

                var tickets = await db.SupportTickets
                    .AsNoTracking()
                    .Include(t => t.User)
                    .Include(t => t.Device)
                    .Select(t => new SupportTicketDto(
                        t.SupportTicketId, t.Subject, t.MessageText, t.Status, t.CreatedAt, t.Comment,
                        t.Device != null ? t.Device.SerialNumber : null,
                        t.User.AccountNumber
                    ))
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return Results.Ok(tickets);
            });

            // --- 3. ОТРИМАННЯ "МОЇХ" ТІКЕТІВ (Залогінений юзер) ---
            group.MapGet("/my", async (HttpContext context, ApplicationDbContext db) =>
            {
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                var tickets = await db.SupportTickets
                    .AsNoTracking()
                    .Where(t => t.UserId == currentUserId) // Фільтр: Тільки мої
                    .Include(t => t.User)
                    .Include(t => t.Device)
                    .Select(t => new SupportTicketDto(
                        t.SupportTicketId, t.Subject, t.MessageText, t.Status, t.CreatedAt, t.Comment,
                        t.Device != null ? t.Device.SerialNumber : null,
                        t.User.AccountNumber
                    ))
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return Results.Ok(tickets);
            });

            // --- 4. ОТРИМАННЯ КОНКРЕТНОГО ТІКЕТА ПО ID ---
            group.MapGet("/{id}", async (int id, HttpContext context, ApplicationDbContext db) =>
            {
                if (!int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId))
                    return Results.Unauthorized();

                // Спочатку шукаємо тікет (без проекції, щоб перевірити права)
                var ticket = await db.SupportTickets
                    .Include(t => t.User)
                    .Include(t => t.Device)
                    .AsNoTracking() // Важливо для швидкодії
                    .FirstOrDefaultAsync(t => t.SupportTicketId == id);

                if (ticket == null)
                    return Results.NotFound(new { message = "Звернення не знайдено." });

                // Перевірка прав: Адмін або Власник
                bool isAdmin = context.User.IsInRole("Admin");
                bool isOwner = ticket.UserId == currentUserId;

                if (!isAdmin && !isOwner)
                {
                    return Results.Json(new { error = "Це не ваше звернення." }, statusCode: 403);
                }

                // Формуємо DTO вручну, бо ми вже витягли дані з бази
                var dto = new SupportTicketDto(
                    ticket.SupportTicketId,
                    ticket.Subject,
                    ticket.MessageText,
                    ticket.Status,
                    ticket.CreatedAt,
                    ticket.Comment,
                    ticket.Device?.SerialNumber,
                    ticket.User.AccountNumber
                );

                return Results.Ok(dto);
            });

            // --- 5. РЕДАГУВАННЯ (Тільки Admin) ---
            group.MapPatch("/{id}", async (int id, UpdateTicketAdminDto dto, HttpContext context, ApplicationDbContext db) =>
            {
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Тільки адміністратор може редагувати звернення." }, statusCode: 403);

                var ticket = await db.SupportTickets.FindAsync(id);
                if (ticket == null) return Results.NotFound();

                ticket.Status = dto.Status;
                if (!string.IsNullOrWhiteSpace(dto.Comment)) ticket.Comment = dto.Comment;

                await db.SaveChangesAsync();
                return Results.Ok(new { message = "Звернення оновлено." });
            });

            // --- 6. ВИДАЛЕННЯ (Тільки Admin) ---
            group.MapDelete("/{id}", async (int id, HttpContext context, ApplicationDbContext db) =>
            {
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Тільки адміністратор може видаляти звернення." }, statusCode: 403);

                var ticket = await db.SupportTickets.FindAsync(id);
                if (ticket == null) return Results.NotFound();

                db.SupportTickets.Remove(ticket);
                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Звернення видалено." });
            });
        }
    }
}