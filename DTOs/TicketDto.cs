using System.ComponentModel.DataAnnotations;

namespace ProjectsDonetskWaterHope.DTOs
{
    public record CreateTicketDto(
        [Required] string Subject,
        [Required] string MessageText,
        int? DeviceId 
    );

    public record UpdateTicketAdminDto(
        [Required] string Status, 
        string? Comment         
    );

    public record SupportTicketDto(
        int TicketId,
        string Subject,
        string MessageText,
        string Status,
        DateTime CreatedAt,
        string? AdminComment,     
        string? DeviceSerialNumber, 
        string UserAccountNumber    
    );
}