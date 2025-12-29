using System.ComponentModel.DataAnnotations;

namespace ProjectsDonetskWaterHope.DTOs
{
    public record CreateDeviceDto(
        [Required] string SerialNumber,
        [Required] string Name,    
        [Required] string Type,      
        [Required] int TariffId,    
        [Required] int UserId,       
        string? Comment
    );

    public record UpdateDeviceAdminDto(
        string? SerialNumber, 
        string? Name,
        string? Type,        
        string? Status,
        string? Comment,
        int? TariffId
    );

    public record DeviceDto(
        int DeviceId,
        string SerialNumber,
        string Name,
        string Type,
        string Status,
        DateTime RegistrationAt,
        string AccountNumber,      
    string? RegisteredByAdmin,
        string? Comment,
        string TariffName,      
        decimal TariffPrice,    
        int UserId              
    );
}