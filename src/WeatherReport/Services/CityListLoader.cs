namespace WeatherReport.Services;

public static class CityListLoader
{
    /// <summary>
    /// Загружает список городов в память: по одному на строку, пустые строки и строки с # пропускаются.
    /// Дубликаты убираются без учёта регистра и пробелов по краям; порядок первого появления сохраняется.
    /// </summary>
    public static IReadOnlyList<string> LoadUnique(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Файл со списком городов не найден: {path}");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var raw in File.ReadAllLines(path))
        {
            var city = raw.Trim().TrimStart('﻿');
            if (city.Length == 0 || city.StartsWith('#')) continue;
            if (seen.Add(city)) result.Add(city);
        }
        return result;
    }
}
