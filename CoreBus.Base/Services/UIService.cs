using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using CoreBus.Base.Views;
using Services.Interfaces;
using System.Reflection;

namespace CoreBus.Base.Services;

public class UIService : IUIService
{
    public async Task RunInUIThread(Action runnedAction)
    {
        await Dispatcher.UIThread.InvokeAsync(runnedAction);
    }

    public async Task CopyToClipboard(string data)
    {
        var clipboard = TopLevel.GetTopLevel(MainWindow.Instance)?.Clipboard;

        if (clipboard != null)
        {
            await clipboard.SetTextAsync(data);
        }
    }

    public void Set_Dark_Theme()
    {
        if (Application.Current != null)
        {
            Application.Current.Resources.MergedDictionaries.Clear();

            Application.Current.Resources.MergedDictionaries.Add(new ResourceInclude(
                new Uri("avares://AppDesign/Themes/Dark.axaml"))
            {
                Source = new Uri("avares://AppDesign/Themes/Dark.axaml")
            });

            Application.Current.RequestedThemeVariant =
                new Avalonia.Styling.ThemeVariant("Dark", Application.Current.ActualThemeVariant);
        }
    }

    public void Set_Light_Theme()
    {
        if (Application.Current != null)
        {
            Application.Current.Resources.MergedDictionaries.Clear();

            Application.Current.Resources.MergedDictionaries.Add(new ResourceInclude(
                new Uri("avares://AppDesign/Themes/Light.axaml"))
            {
                Source = new Uri("avares://AppDesign/Themes/Light.axaml")
            });

            Application.Current.RequestedThemeVariant =
                new Avalonia.Styling.ThemeVariant("Light", Application.Current.ActualThemeVariant);
        }
    }

    public Version? GetAppVersion()
    {
        char[] appVersion_Chars = new char[20];

        if (Assembly.GetExecutingAssembly().GetName().Version?.TryFormat(appVersion_Chars, 3, out int numberOfChars) == true)
        {
            return new Version(new string(appVersion_Chars, 0, numberOfChars));
        }

        return null;
    }

    public string? GetAvaloniaVersionString()
    {
        char[] GUIVersion_Chars = new char[20];

        if (typeof(AvaloniaObject).Assembly.GetName().Version?.TryFormat(GUIVersion_Chars, 3, out int numberOfChars) == true)
        {
            return new string(GUIVersion_Chars, 0, numberOfChars);
        }

        return null;
    }

    public Version GetRuntimeVersion()
    {
        return Environment.Version;
    }
}
