namespace ProjectsDonetskWaterHope.Models
{
    public class ConsumptionRecord
    {
        public int ConsumptionRecordId { get; set; }
        public int Value { get; set; } 
        public int Delta { get; set; } 
        public decimal MustToPay { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int TariffId { get; set; }
        public Tariff Tariff { get; set; } = null!;

        public int DeviceId { get; set; }
        public Device Device { get; set; } = null!;
    }
}