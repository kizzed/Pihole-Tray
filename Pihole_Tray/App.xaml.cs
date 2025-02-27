using Microsoft.Toolkit.Uwp.Notifications;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Pihole_Tray
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ToastNotificationManagerCompat.OnActivated += ToastActivatedHandler;
        }
        private void ToastActivatedHandler(ToastNotificationActivatedEventArgsCompat toastArgs)
        {
            var args = ToastArguments.Parse(toastArgs.Argument);
            Current.Dispatcher.Invoke(() =>
            {
                if (args.Contains("action") && args["action"] == "install_update")
                {
                    Updater.InstallUpdate();
                }

            });
        }
    }

}
