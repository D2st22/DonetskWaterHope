namespace ProjectsDonetskWaterHope.Models
{
    public class SupportTicket
    {
        public int SupportTicketId { get; set; }
        public string Subject { get; set; } = null!;
        public string MessageText { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Comment { get; set; }

        public int? DeviceId { get; set; }
        public Device? Device { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}