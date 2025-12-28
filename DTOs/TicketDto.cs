using System.ComponentModel.DataAnnotations;

namespace ProjectsDonetskWaterHope.DTOs
{
    // 1. СТВОРЕННЯ (Користувач пише про проблему)
    public record CreateTicketDto(
        [Required] string Subject,
        [Required] string MessageText,
        int? DeviceId // Опціонально: якщо проблема з конкретним лічильником
    );

    // 2. РЕДАГУВАННЯ АДМІНОМ (Вирішення проблеми)
    public record UpdateTicketAdminDto(
        [Required] string Status, // "In Progress", "Closed", "Rejected"
        string? Comment           // Відповідь адміна: "Майстер виїхав"
    );

    // 3. ВІДПОВІДЬ (Що бачить фронтенд)
    public record SupportTicketDto(
        int TicketId,
        string Subject,
        string MessageText,
        string Status,
        DateTime CreatedAt,
        string? AdminComment,     // Коментар адміна
        string? DeviceSerialNumber, // Серійний номер замість ID (зручніше)
        string UserAccountNumber    // Хто створив
    );
}