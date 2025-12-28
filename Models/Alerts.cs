namespace ProjectsDonetskWaterHope.Models
{
    public class Alert
    {
        public int AlertId { get; set; }
        public string MessageText { get; set; } = null!;
        public string Type { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        public int DeviceId { get; set; }
        public Device Device { get; set; } = null!;
    }
}