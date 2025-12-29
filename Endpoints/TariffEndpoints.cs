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

            // --- ОТРИМАННЯ ВСІХ ТАРИФІВ (GET) ---
            // Доступно всім авторизованим (і адмінам, і юзерам, щоб знати ціни)
            group.MapGet("/", async (ApplicationDbContext db) =>
            {
                var tariffs = await db.Tariffs
                    .AsNoTracking() // Оптимізація для читання (швидше)
                    .Select(t => new TariffDto(t.TariffId, t.Name, t.PricePerUnit))
                    .ToListAsync();

                return Results.Ok(tariffs);
            });

            // --- ДОДАВАННЯ ТАРИФУ (POST) ---
            // Тільки Адмін
            group.MapPost("/", async (CreateTariffDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                // 1. Guard Clause: Права
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Тільки адміністратор може створювати тарифи." }, statusCode: 403);

                // 2. Валідація: Чи є вже такий тариф?
                if (await db.Tariffs.AnyAsync(t => t.Name == dto.Name))
                    return Results.BadRequest(new { error = $"Тариф з назвою '{dto.Name}' вже існує." });

                // 3. Створення
                var tariff = new Tariff
                {
                    Name = dto.Name,
                    PricePerUnit = dto.PricePerUnit
                };

                db.Tariffs.Add(tariff);
                await db.SaveChangesAsync();

                // 4. Return Created (201)
                return Results.Created($"/api/tariffs/{tariff.TariffId}", 
                    new TariffDto(tariff.TariffId, tariff.Name, tariff.PricePerUnit));
            });

            // --- ВИДАЛЕННЯ ТАРИФУ (DELETE) ---
            // Тільки Адмін
            group.MapDelete("/{id}", async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                // 1. Guard Clause: Права
                if (!context.User.IsInRole("Admin"))
                    return Results.Json(new { error = "Тільки адміністратор може видаляти тарифи." }, statusCode: 403);

                // 2. Пошук
                var tariff = await db.Tariffs.FindAsync(id);
                if (tariff == null)
                    return Results.NotFound(new { message = "Тариф не знайдено." });

                // 3. Видалення з обробкою зв'язків
                try
                {
                    db.Tariffs.Remove(tariff);
                    await db.SaveChangesAsync();
                    return Results.Ok(new { message = $"Тариф '{tariff.Name}' успішно видалено." });
                }
                catch (DbUpdateException)
                {
                    // Якщо до тарифу прив'язані лічильники (Devices), база не дасть видалити
                    return Results.Conflict(new { error = "Неможливо видалити тариф: він використовується на існуючих лічильниках." });
                }
            });
        }
    }
}