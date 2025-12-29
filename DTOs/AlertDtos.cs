using System.ComponentModel.DataAnnotations;

namespace ProjectsDonetskWaterHope.DTOs
{
    public record CreateAlertDto(
        [Required] int DeviceId,
        [Required] string MessageText, 
        [Required] string Type        
    );

    public record AlertDto(
        int AlertId,
        string MessageText,
        string Type,
        DateTime CreatedAt,
        string DeviceSerialNumber, 
        string UserAccountNumber   
    );
}