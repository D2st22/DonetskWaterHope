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
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Доступ заборонено" }, statusCode: 403);

                var logs = await db.SystemLogs
                    .AsNoTracking()
                    .OrderByDescending(l => l.CreatedAt) 
                    .Take(100) 
                    .ToListAsync();

                return Results.Ok(logs);
            }).WithTags("Admin");
        }
    }
}