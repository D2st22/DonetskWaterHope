using ProjectsDonetskWaterHope.Data;
using Microsoft.EntityFrameworkCore;

namespace ProjectsDonetskWaterHope.Endpoints
{
    public static class AdminEndpoints
    {
        public static void MapAdminEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/admin/logs").RequireAuthorization();

            group.MapGet("/", async (ApplicationDbContext db, HttpContext context) =>
            {
                // Тільки Адмін має доступ
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Доступ заборонено" }, statusCode: 403);

                var logs = await db.SystemLogs
                    .AsNoTracking()
                    .OrderByDescending(l => l.CreatedAt) // Спочатку нові
                    .Take(100) // Беремо останні 100 подій
                    .ToListAsync();

                return Results.Ok(logs);
            });
        }
    }
}