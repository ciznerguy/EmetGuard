// נוצר בגרסה 1 מהתבנית של שכבת השירות, כדוגמה בלבד.
// המחלקה לא משמשת את הפרויקט.
namespace EmetGuard
{
    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string? Summary { get; set; }
    }
}
