using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectsDonetskWaterHope.Models
{
    public class SystemLog
    {
        [Key]
        public int LogId { get; set; }

        // Додано модифікатор 'required'. 
        // Тепер не можна створити лог без вказання типу та повідомлення.
        public required string EventType { get; set; } = null!;
        public required string Message { get; set; }  = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? RelatedUserId { get; set; }
        public int? RelatedDeviceId { get; set; }
    }
}