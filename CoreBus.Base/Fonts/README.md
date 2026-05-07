# Fonts

Дополнительные шрифты, которые используются как fallback для языков, не покрываемых основным UI-шрифтом Inter.

## Почему китайский и хинди

Основной шрифт UI — Inter (подключается в `CoreBus.Desktop/Program.cs` через `WithInterFont()`). Он покрывает латиницу и кириллицу, но не содержит китайских иероглифов и деванагари.

Без явных fallback-шрифтов эти символы рисовались бы системным шрифтом, который может отсутствовать в конкретной ОС. Чтобы локализации `zh` и `hi` точно были и выглядели везде одинаково, в проект добавлены:

- `NotoSansSC-Regular.otf` — упрощённый китайский.
- `NotoSansDevanagari-Regular.ttf` — хинди.

## Как это работает

Шрифты подключаются как ресурсы в `CoreBus.Base.csproj`:

```xml
<AvaloniaResource Include="Fonts\NotoSansSC-Regular.otf" />
<AvaloniaResource Include="Fonts\NotoSansDevanagari-Regular.ttf" />
```

И регистрируются как fallbacks в `CoreBus.Desktop/Program.cs`:

```csharp
.With(new FontManagerOptions
{
    FontFallbacks =
    [
        new FontFallback
        {
            FontFamily = new FontFamily("avares://CoreBus.Base/Fonts/NotoSansDevanagari-Regular.ttf#Noto Sans Devanagari Regular"),
        },
        new FontFallback
        {
            FontFamily = new FontFamily("avares://CoreBus.Base/Fonts/NotoSansSC-Regular.otf#Noto Sans SC Regular"),
        },
    ],
})
```

Цепочка отрисовки каждого глифа: Inter → системный шрифт (`$Default`) → `FontFallbacks` по порядку.

## Как добавить новый шрифт

1. Положить файл в эту папку.
2. Добавить `<AvaloniaResource Include="Fonts\<file>" />` в `CoreBus.Base.csproj`.
3. Добавить `FontFallback` в `CoreBus.Desktop/Program.cs`. Имя после `#` должно совпадать с font family внутри файла.
4. Проверить локализацию вручную на разных ОС — текст должен отображаться без прямоугольников.

