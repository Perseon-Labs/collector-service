using System.Text.Json.Serialization;

[JsonSerializable(typeof(WeatherForecast[]))]
public partial class WeatherForecastJsonContext : JsonSerializerContext
{
}