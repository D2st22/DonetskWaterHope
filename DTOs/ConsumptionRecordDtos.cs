using System.ComponentModel.DataAnnotations;

namespace ProjectsDonetskWaterHope.DTOs
{

    public record CreateConsumptionDto(
        [Required] int DeviceId,
        [Required] int CurrentValue 
    );

    public record ConsumptionRecordDto(
        int RecordId,
        int Value,             
        int Delta,            
        decimal MustToPay,     
        DateTime CreatedAt,
        string TariffName,    
        decimal PricePerUnit,   
        string DeviceSerialNumber
    );
}