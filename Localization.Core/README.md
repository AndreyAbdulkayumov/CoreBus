# Локализация CoreBus

Файлы переводов хранятся в папке `Localization.Core/Localization`:

- `en.json`
- `ru.json`
- дополнительные языки в формате `<code>.json`

## Где используется

- Реализация загрузки: `Localization.Core/LocalizationService.cs`
- XAML markup extension: `Localization.Avalonia/LocExtension.cs`
- Точка копирования в runtime-output: `CoreBus.Desktop/CoreBus.Desktop.csproj`

Сервис читает переводы из `Localization/*.json` рядом с исполняемым файлом
(`AppContext.BaseDirectory`).

## Как добавить новый язык

1. Скопировать `Localization/ru.json` в файл с нужным кодом, например `Localization/de.json`.
2. Заполнить `_meta` и переводы.
3. Убедиться, что набор ключей совпадает с `ru.json`.
4. Пересобрать приложение.

При сборке `CoreBus.Desktop` файлы из `Localization.Core/Localization` копируются
в выходной каталог как `Localization/*.json`.
