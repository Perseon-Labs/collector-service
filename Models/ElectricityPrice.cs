namespace CollectorService.Models
{
    public class ElectricityPrice
    {
        public double SEK_per_kWh { get; set; }
        public double EUR_per_kWh { get; set; }
        public double EXR { get; set; }
        public DateTimeOffset time_start { get; set; }
        public DateTimeOffset time_end { get; set; }
    }
}
