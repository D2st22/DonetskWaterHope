namespace ProjectsDonetskWaterHope.Models
{
    public class Tariff
    {
        public int TariffId { get; set; }
        public string Name { get; set; } = null!;
        public decimal PricePerUnit { get; set; }

    }
}