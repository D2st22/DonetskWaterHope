using ProjectsDonetskWaterHope.Data;
using ProjectsDonetskWaterHope.DTOs;
using ProjectsDonetskWaterHope.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectsDonetskWaterHope.Endpoints
{
    public static class TariffEndpoints
    {
        public static void MapTariffEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/tariffs").RequireAuthorization();

            group.MapGet("/", async (ApplicationDbContext db) =>
            {
                var tariffs = await db.Tariffs
                    .AsNoTracking()
                    .Select(t => new TariffDto(t.TariffId, t.Name, t.PricePerUnit))
                    .ToListAsync();

                return Results.Ok(tariffs);
            }).WithTags("Public");

            group.MapPost("/", async (CreateTariffDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Тільки адміністратор може створювати тарифи." }, statusCode: 403);

                if (await db.Tariffs.AnyAsync(t => t.Name == dto.Name))
                    return Results.BadRequest(new { error = $"Тариф з назвою '{dto.Name}' вже існує." });

                var tariff = new Tariff
                {
                    Name = dto.Name,
                    PricePerUnit = dto.PricePerUnit
                };

                db.Tariffs.Add(tariff);
                await db.SaveChangesAsync();

                return Results.Created($"/api/tariffs/{tariff.TariffId}", 
                    new TariffDto(tariff.TariffId, tariff.Name, tariff.PricePerUnit));
            }).WithTags("Admin");

            group.MapDelete("/{id}", async (int id, ApplicationDbContext db, HttpContext context) =>
            {

                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Тільки адміністратор може видаляти тарифи." }, statusCode: 403);

                var tariff = await db.Tariffs.FindAsync(id);
                if (tariff == null)
                    return Results.NotFound(new { message = "Тариф не знайдено." });

                try
                {
                    db.Tariffs.Remove(tariff);
                    await db.SaveChangesAsync();
                    return Results.Ok(new { message = $"Тариф '{tariff.Name}' успішно видалено." });
                }
                catch (DbUpdateException)
                {
                    return Results.Conflict(new { error = "Неможливо видалити тариф: він використовується на існуючих лічильниках." });
                }
            }).WithTags("Admin");
        }
    }
}