using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace NBackTask;

public partial class App : Application
{
    public App() : base()
    {
        Startup += Application_Startup;
    }

    // Set the US-culture across the application to avoid decimal point parsing/logging issues
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        EventManager.RegisterClassHandler(typeof(TextBox),
            UIElement.GotFocusEvent,
            new RoutedEventHandler(TextBox_GotFocus));
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        (sender as TextBox)?.SelectAll();
    }
}
