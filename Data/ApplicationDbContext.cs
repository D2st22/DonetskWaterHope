using Microsoft.EntityFrameworkCore;
using ProjectsDonetskWaterHope.Models;

namespace ProjectsDonetskWaterHope.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Реєстрація таблиць
        public DbSet<User> Users { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<ConsumptionRecord> ConsumptionRecords { get; set; }
        public DbSet<Tariff> Tariffs { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- 1. Налаштування Користувача та Пристроїв ---
            modelBuilder.Entity<Device>()
                .HasOne(d => d.User)
                .WithMany(u => u.Devices) // Використовуємо ICollection з моделі User
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Якщо видалити юзера -> видаляються всі його пристрої

            // --- 2. Налаштування Тарифів (Важливо для фінансів) ---
            // Тариф для пристрою (поточний)
            modelBuilder.Entity<Device>()
                .HasOne(d => d.Tariff)
                .WithMany() // У Тарифа може не бути списку "поточних пристроїв", це не обов'язково
                .HasForeignKey(d => d.TariffId)
                .OnDelete(DeleteBehavior.Restrict); // Не можна видалити тариф, якщо він встановлений на пристрої
            modelBuilder.Entity<Device>()
        .HasOne(d => d.RegisteredByUser)       // Використовуємо нову назву властивості
        .WithMany()
        .HasForeignKey(d => d.RegisteredByUserId) // Використовуємо нову назву FK
        .OnDelete(DeleteBehavior.Restrict);


            // Тариф в історії споживання (архівний)
            modelBuilder.Entity<ConsumptionRecord>()
                .HasOne(cr => cr.Tariff)
                .WithMany()
                .HasForeignKey(cr => cr.TariffId)
                .OnDelete(DeleteBehavior.Restrict); // КРИТИЧНО: Не можна видалити тариф, якщо по ньому є записи в історії

            // --- 3. Споживання та Пристрої ---
            modelBuilder.Entity<ConsumptionRecord>()
                .HasOne(cr => cr.Device)
                .WithMany(d => d.ConsumptionRecords) // Зв'язок з ICollection в Device
                .HasForeignKey(cr => cr.DeviceId)
                .OnDelete(DeleteBehavior.Cascade); // Видалення пристрою очищає його історію

            // --- 4. Технічна підтримка (SupportTickets) ---
            modelBuilder.Entity<SupportTicket>()
                .HasOne(st => st.User)
                .WithMany(u => u.SupportTickets) // Зв'язок з ICollection в User
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SupportTicket>()
                .HasOne(st => st.Device)
                .WithMany() // У Device немає списку тікетів (або є, якщо ви додали ICollection<SupportTicket>)
                .HasForeignKey(st => st.DeviceId)
                .IsRequired(false) // Тікет може бути без пристрою (загальне питання)
                .OnDelete(DeleteBehavior.SetNull); // Якщо пристрій видалять, тікет залишиться, але поле DeviceId стане null

            // --- 5. Сповіщення (Alerts) ---
            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Device)
                .WithMany(d => d.Alerts) // Зв'язок з ICollection в Device
                .HasForeignKey(a => a.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- 6. Унікальні ключі та обмеження ---
            // Серійний номер пристрою має бути унікальним у всій базі
            modelBuilder.Entity<Device>()
                .HasIndex(d => d.SerialNumber)
                .IsUnique();

            // Налаштування точності для грошей (PostgreSQL numeric)
            modelBuilder.Entity<Tariff>()
                .Property(t => t.PricePerUnit)
                .HasColumnType("decimal(18,2)"); // 18 цифр всього, 2 після коми

            modelBuilder.Entity<ConsumptionRecord>()
                .Property(cr => cr.MustToPay)
                .HasColumnType("decimal(18,2)");
        }
    }
}