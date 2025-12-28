using System.ComponentModel.DataAnnotations;

namespace ProjectsDonetskWaterHope.DTOs
{
    // 1. ВХІДНІ ДАНІ (Ми просимо мінімум)
    public record CreateConsumptionDto(
        [Required] int DeviceId,
        [Required] int CurrentValue 
    );

    // 2. ВИХІДНІ ДАНІ (Повна картина)
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