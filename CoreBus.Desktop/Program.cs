using System;
using Avalonia;
using Avalonia.Media;
using ReactiveUI.Avalonia;
using CoreBus.Base;

namespace CoreBus.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new FontManagerOptions
            {
                // Дополнительные fallback-шрифты для языков, где у базового Inter может не хватать глифов.
                // На основной шрифт UI (латиница/кириллица) это не влияет.
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
            .LogToTrace()
            .UseReactiveUI(builder => { });
}
