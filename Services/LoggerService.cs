using ProjectsDonetskWaterHope.Data;
using ProjectsDonetskWaterHope.Models;

namespace ProjectsDonetskWaterHope.Services
{
    public class LoggerService
    {
        private readonly IServiceProvider _serviceProvider;

        public LoggerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task LogAsync(string eventType, string message, int? userId = null, int? deviceId = null)
        {
            // Створюємо новий scope, щоб отримати DbContext (це важливо для фонових задач)
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var log = new SystemLog
            {
                EventType = eventType,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                RelatedUserId = userId,
                RelatedDeviceId = deviceId
            };

            db.SystemLogs.Add(log);
            await db.SaveChangesAsync();
        }
    }
}