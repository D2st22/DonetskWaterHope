namespace ProjectsDonetskWaterHope.Models
{
    public class Device
    {
        public int DeviceId { get; set; }
        public string SerialNumber { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime RegistrationAt { get; set; } = DateTime.UtcNow;
        public string? Comment { get; set; }

        public int? RegisteredByUserId { get; set; }
        public User? RegisteredByUser { get; set; }

        public int TariffId { get; set; }
        public Tariff Tariff { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public virtual ICollection<ConsumptionRecord> ConsumptionRecords { get; set; } = new List<ConsumptionRecord>();
        public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}