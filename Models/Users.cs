namespace ProjectsDonetskWaterHope.Models;

public class User
{
    public int UserId { get; set; }
    public string AccountNumber { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
    public virtual ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
}