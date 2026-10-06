using System.Globalization;
using System.Net;
using System.Text.Json;
using WeatherReport.Models;

namespace WeatherReport.Services;

/// <summary>Клиент https://wttr.in/{City}?format=j1</summary>
public sealed class WttrClient : IDisposable
{
    private readonly HttpClient _http;
    private const int MaxAttempts = 3;

    public WttrClient()
    {
        _http = new HttpClient { BaseAddress = new Uri("https://wttr.in/"), Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("WeatherReport/1.0");
        _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en");
    }

    public async Task<WeatherData> GetAsync(string city, CancellationToken ct = default)
    {
        var url = $"{Uri.EscapeDataString(city)}?format=j1";
        Exception? last = null;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var resp = await _http.GetAsync(url, ct);
                if (resp.StatusCode is HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError)
                    throw new HttpRequestException($"HTTP {(int)resp.StatusCode}");
                resp.EnsureSuccessStatusCode();

                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                return Parse(city, doc.RootElement);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                last = e;
                if (attempt < MaxAttempts) await Task.Delay(TimeSpan.FromSeconds(2 * attempt), ct);
            }
        }
        throw new InvalidOperationException($"wttr.in не ответил для «{city}»: {last?.Message}", last);
    }

    /// <summary>
    /// Разбор ответа j1:
    /// current_condition[0].temp_C — текущая температура (строка);
    /// nearest_area[0].country[0].value — страна; nearest_area[0].areaName[0].value — найденный пункт.
    /// </summary>
    public static WeatherData Parse(string city, JsonElement root)
    {
        var current = First(root, "current_condition")
                      ?? throw new FormatException("В ответе нет current_condition — город не найден?");
        var tempStr = current.GetProperty("temp_C").GetString();
        if (!int.TryParse(tempStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var temp))
            throw new FormatException($"Некорректная температура: «{tempStr}»");

        var area = First(root, "nearest_area");
        var country = area is { } a ? Value(a, "country") : null;
        var areaName = area is { } b ? Value(b, "areaName") : null;

        return new WeatherData(city, temp, string.IsNullOrWhiteSpace(country) ? "Unknown" : country!, areaName ?? "");
    }

    private static JsonElement? First(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0
            ? arr[0] : null;

    // Поля вида "country": [ { "value": "Russia" } ]
    private static string? Value(JsonElement obj, string name) =>
        First(obj, name) is { } e && e.TryGetProperty("value", out var v) ? v.GetString()?.Trim() : null;

    public void Dispose() => _http.Dispose();
}
