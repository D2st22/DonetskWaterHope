using System.ComponentModel.DataAnnotations;

namespace ProjectsDonetskWaterHope.DTOs
{
    // 1. СТВОРЕННЯ (Зазвичай робить IoT пристрій, але поки що Адмін)
    public record CreateAlertDto(
        [Required] int DeviceId,
        [Required] string MessageText, 
        [Required] string Type        
    );

    // 2. ВІДПОВІДЬ (Для списків)
    public record AlertDto(
        int AlertId,
        string MessageText,
        string Type,
        DateTime CreatedAt,
        string DeviceSerialNumber, 
        string UserAccountNumber   
    );
}