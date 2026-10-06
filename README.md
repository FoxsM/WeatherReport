# WeatherReport

Консольное приложение на C# (.NET 8): загружает список городов из файла, получает текущую погоду
через [wttr.in](https://wttr.in) (`https://wttr.in/{City}?format=j1`) и выводит погоду по каждому городу
и статистику по странам.

Внешних пакетов нет — для JSON используется встроенный `System.Text.Json`.

## Запуск

Требуется [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone <ссылка на репозиторий>
cd WeatherReport
dotnet run --project src/WeatherReport
```

Свой файл со списком городов:

```bash
dotnet run --project src/WeatherReport -- path/to/cities.txt
```

Формат файла — один город в строке (пустые строки и строки с `#` пропускаются).
По умолчанию используется `src/WeatherReport/cities.txt`.

## Пример вывода

```
Уникальных городов: 8

Погода по городам:
  Moscow, Russia +13 °C
  Khabarovsk, Russia +8 °C
  Saint-Petersburg, Russia +14 °C
  Vienna, Austria +24 °C
  Izhevsk, Russia +5 °C
  Perm, Russia +4 °C
  NhaTrang, Vietnam +26 °C
  Villach, Austria +16 °C

По странам:
  Austria — 2 cities, avg: +20 °C, min: +16 °C, max: +24 °C
  Russia — 5 cities, avg: +8.8 °C, min: +4 °C, max: +14 °C
  Vietnam — 1 city, avg: +26 °C, min: +26 °C, max: +26 °C
```

## Как это устроено

| Шаг задания | Где в коде |
|---|---|
| 1. Загрузка списка городов в память | `Services/CityListLoader.cs` — чтение файла, `Trim`, удаление дубликатов без учёта регистра с сохранением порядка |
| 2. Запрос к API для каждого **уникального** города | `Services/WttrClient.cs` — `HttpClient`, до 4 параллельных запросов, 3 попытки с паузой при сетевых ошибках / 429 / 5xx |
| 3. Модель `WeatherData` (город, температура °C, страна) | `Models/WeatherData.cs` |
| 4. Вывод по каждому городу | `ReportPrinter.CityLine` — `Tokyo, Japan +18 °C` |
| 5. Группировка по странам: количество, среднее, мин., макс. | `ReportPrinter.GroupByCountry` (LINQ `GroupBy` + `Count/Average/Min/Max`) |

Поля ответа wttr.in, которые используются:

- `current_condition[0].temp_C` — текущая температура;
- `nearest_area[0].country[0].value` — страна;
- `nearest_area[0].areaName[0].value` — какой пункт нашёл wttr.in. Если это другой город (в латинице),
  он показывается в выводе, например `(wttr.in: Permes)`, — так видно ошибки геокодинга.
  Названия кириллицей (wttr.in отдаёт российские города как «Москва») считаются переводом и не помечаются.

Ошибка по одному городу (нет сети, город не найден) не прерывает программу: город выводится с ошибкой,
статистика считается по остальным.
