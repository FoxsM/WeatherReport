using System.Globalization;
using WeatherReport.Models;

namespace WeatherReport;

/// <summary>Статистика по стране — всё вычисляется кодом из списка WeatherData.</summary>
public sealed record CountryStats(string Country, int CityCount, double AvgC, int MinC, int MaxC);

public static class ReportPrinter
{
    /// <summary>+18 °C, -3 °C, 0 °C; среднее — с одним знаком после запятой, если оно не целое.</summary>
    public static string FormatTemp(double t)
    {
        var rounded = Math.Round(t, 1, MidpointRounding.AwayFromZero);
        if (rounded == 0) rounded = 0; // убираем «-0»
        var s = rounded.ToString("0.#", CultureInfo.InvariantCulture);
        return (rounded > 0 ? "+" : "") + s + " °C";
    }

    public static IReadOnlyList<CountryStats> GroupByCountry(IEnumerable<WeatherData> data) =>
        data.GroupBy(d => d.Country, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CountryStats(
                g.First().Country,
                g.Count(),
                g.Average(d => d.TemperatureC),
                g.Min(d => d.TemperatureC),
                g.Max(d => d.TemperatureC)))
            .OrderBy(s => s.Country, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static string CityLine(WeatherData d)
    {
        var line = $"{d.City}, {d.Country} {FormatTemp(d.TemperatureC)}";
        // Если wttr.in сопоставил запросу другой пункт — показываем, чтобы ошибку геокодинга было видно.
        var norm = (string s) => new string(s.Where(char.IsLetter).ToArray());
        if (d.ResolvedArea.Length > 0 && !norm(d.ResolvedArea).Equals(norm(d.City), StringComparison.OrdinalIgnoreCase))
            line += $"  (wttr.in: {d.ResolvedArea})";
        return line;
    }

    public static string CountryLine(CountryStats s) =>
        $"{s.Country} — {s.CityCount} {(s.CityCount == 1 ? "city" : "cities")}, " +
        $"avg: {FormatTemp(s.AvgC)}, min: {FormatTemp(s.MinC)}, max: {FormatTemp(s.MaxC)}";
}
