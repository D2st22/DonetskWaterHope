using System.ComponentModel.DataAnnotations;

namespace ProjectsDonetskWaterHope.DTOs
{
    // Вхідні дані для створення (Тільки Адмін)
    public record CreateDeviceDto(
        [Required] string SerialNumber,
        [Required] string Name,      // Наприклад: "Кухня Холодна"
        [Required] string Type,      // Наприклад: "WaterMeter"
        [Required] int TariffId,     // Який тариф прив'язати
        [Required] int UserId,       // Кому належить
        string? Comment
    );

    // Вхідні дані для оновлення (Тільки Адмін)
    public record UpdateDeviceAdminDto(
        string? Name,
        string? Status,       // Наприклад: "Active", "Blocked", "Maintenance"
        string? Comment,
        int? TariffId
    );

    // Вихідні дані (для фронтенду)
    public record DeviceDto(
        int DeviceId,
        string SerialNumber,
        string Name,
        string Type,
        string Status,
        DateTime RegistrationAt,
        string AccountNumber,       // Власник
    string? RegisteredByAdmin,
        string? Comment,
        string TariffName,      // Повертаємо назву тарифу, а не просто ID
        decimal TariffPrice,    // І ціну, щоб юзер бачив
        int UserId              // ID власника
    );
}