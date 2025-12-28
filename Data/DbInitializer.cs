
using ProjectsDonetskWaterHope.Models;
using ProjectsDonetskWaterHope.Services; // Для PasswordHasher
using System.Linq; // Потрібно для LINQ запитів

namespace ProjectsDonetskWaterHope.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext db)
        {
          
            // --- 2. ПЕРЕВІРКА АДМІНА ---
            // ЗМІНА ТУТ: Ми перевіряємо не "чи пуста база", а "чи є хоч один Адмін"
            bool adminExists = db.Users.Any(u => u.Role == "Admin");

            if (adminExists)
            {
                return; // Якщо адмін вже є - нічого не робимо
            }

            // Якщо дійшли сюди - значить адмінів немає (навіть якщо є юзери)
            var admin = new User
            {
                AccountNumber = "WH-ADMIN01",
                FirstName = "Super",
                LastName = "Admin",
                Email = "admin@waterhope.com",
                PhoneNumber = "0000000000",
                Role = "Admin", // Важливо!
                PasswordHash = PasswordHasher.HashPassword("admin123")
            };

            db.Users.Add(admin);
            db.SaveChanges();

            Console.WriteLine("--- [SEEDER] Адміністратора WH-ADMIN01 створено автоматично ---");
        }
    }
}