namespace WeatherReport.Models;

/// <summary>Погода в одном городе.</summary>
/// <param name="City">Город (как указан во входном файле).</param>
/// <param name="TemperatureC">Текущая температура, °C.</param>
/// <param name="Country">Страна (из ответа wttr.in).</param>
/// <param name="ResolvedArea">Населённый пункт, который wttr.in сопоставил запросу — для контроля геокодинга.</param>
public sealed record WeatherData(string City, int TemperatureC, string Country, string ResolvedArea);
