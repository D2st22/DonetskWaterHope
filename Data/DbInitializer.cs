
using ProjectsDonetskWaterHope.Models;
using ProjectsDonetskWaterHope.Services; 
using System.Linq; 

namespace ProjectsDonetskWaterHope.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext db)
        {
          
            bool adminExists = db.Users.Any(u => u.Role == "Admin");

            if (adminExists)
            {
                return; 
            }

            var admin = new User
            {
                AccountNumber = "WH-ADMIN01",
                FirstName = "Super",
                LastName = "Admin",
                Email = "admin@waterhope.com",
                PhoneNumber = "0000000000",
                Role = "Admin", 
                PasswordHash = PasswordHasher.HashPassword("admin123")
            };

            db.Users.Add(admin);
            db.SaveChanges();

            Console.WriteLine("--- [SEEDER] Адміністратора WH-ADMIN01 створено автоматично ---");
        }
    }
}