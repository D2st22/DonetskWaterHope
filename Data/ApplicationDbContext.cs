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

            modelBuilder.Entity<Device>()
                .HasOne(d => d.User)
                .WithMany(u => u.Devices) 
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade); 

            modelBuilder.Entity<Device>()
                .HasOne(d => d.Tariff)
                .WithMany() 
                .HasForeignKey(d => d.TariffId)
                .OnDelete(DeleteBehavior.Restrict); 
            modelBuilder.Entity<Device>()
        .HasOne(d => d.RegisteredByUser)     
        .WithMany()
        .HasForeignKey(d => d.RegisteredByUserId) 
        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConsumptionRecord>()
                .HasOne(cr => cr.Tariff)
                .WithMany()
                .HasForeignKey(cr => cr.TariffId)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<ConsumptionRecord>()
                .HasOne(cr => cr.Device)
                .WithMany(d => d.ConsumptionRecords) 
                .HasForeignKey(cr => cr.DeviceId)
                .OnDelete(DeleteBehavior.Cascade); 

            modelBuilder.Entity<SupportTicket>()
                .HasOne(st => st.User)
                .WithMany(u => u.SupportTickets) 
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SupportTicket>()
                .HasOne(st => st.Device)
                .WithMany() 
                .HasForeignKey(st => st.DeviceId)
                .IsRequired(false) 
                .OnDelete(DeleteBehavior.SetNull); 

            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Device)
                .WithMany(d => d.Alerts) 
                .HasForeignKey(a => a.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Device>()
                .HasIndex(d => d.SerialNumber)
                .IsUnique();

            modelBuilder.Entity<Tariff>()
                .Property(t => t.PricePerUnit)
                .HasColumnType("decimal(18,2)"); 

            modelBuilder.Entity<ConsumptionRecord>()
                .Property(cr => cr.MustToPay)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<User>()
    .HasIndex(u => u.Email)
    .IsUnique();
        }
    }
}