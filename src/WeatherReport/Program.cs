using System.Text;
using WeatherReport;
using WeatherReport.Models;
using WeatherReport.Services;

Console.OutputEncoding = Encoding.UTF8;

// 1. Список городов: путь из аргумента или cities.txt рядом с программой
var path = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "cities.txt");

IReadOnlyList<string> cities;
try
{
    cities = CityListLoader.LoadUnique(path);
}
catch (Exception e)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}

if (cities.Count == 0)
{
    Console.Error.WriteLine("Список городов пуст.");
    return 1;
}
Console.WriteLine($"Уникальных городов: {cities.Count} (файл: {path})\n");

// 2. Запросы к wttr.in — параллельно, но не больше 4 одновременно, чтобы не упереться в rate limit
using var client = new WttrClient();
using var limiter = new SemaphoreSlim(4);

var tasks = cities.Select(async city =>
{
    await limiter.WaitAsync();
    try { return (city, data: (WeatherData?)await client.GetAsync(city), error: (string?)null); }
    catch (Exception e) { return (city, data: (WeatherData?)null, error: e.Message); }
    finally { limiter.Release(); }
});
var results = await Task.WhenAll(tasks); // порядок совпадает с порядком городов в файле

// 4. Погода по каждому городу
Console.WriteLine("Погода по городам:");
foreach (var (city, data, error) in results)
    Console.WriteLine(data != null ? "  " + ReportPrinter.CityLine(data) : $"  {city}: ошибка — {error}");

// 5. Группировка по странам
var ok = results.Where(r => r.data != null).Select(r => r.data!).ToList();
Console.WriteLine("\nПо странам:");
foreach (var stats in ReportPrinter.GroupByCountry(ok))
    Console.WriteLine("  " + ReportPrinter.CountryLine(stats));

var failed = results.Count(r => r.data == null);
if (failed > 0) Console.WriteLine($"\nНе удалось получить данные: {failed} из {results.Length}.");
return failed == results.Length ? 2 : 0;
