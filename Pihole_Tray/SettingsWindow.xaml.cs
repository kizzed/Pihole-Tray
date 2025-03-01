using ABI.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui;
using Wpf.Ui.Controls;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace Pihole_Tray
{
    /// <summary>
    /// Interaction logic for InstanceSettingsWindow.xaml
    /// </summary>

    public partial class SettingsWindow : FluentWindow
    {
        public InstanceStorage Storage;
        public static string ActiveWindow ="sa";

        public string GetPageName()
        {
            return ActiveWindow;
        }

        
        public SettingsWindow(InstanceStorage storage)
        {
            InitializeComponent();
            Storage = storage;

            foreach (var instance in storage.Instances)
            {
                var navItem = new NavigationViewItem
                {
                    Width = 200,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Content = instance.Name,
                    TargetPageType = typeof(SettingsPage),
                    Icon = new SymbolIcon
                    {
                        Symbol = SymbolRegular.Shield20
                    }
                };

                navItem.Click += (sender, e) =>
                {
                    ActiveWindow = navItem.Content.ToString();
                    Debug.WriteLine("AW: " + ActiveWindow);
                };

                instance.PropertyChanged += (sender, e) =>
                {
                    MainWindow.storage.WroteOverInstanceToKey(instance, navItem.Content.ToString());
                    if (e.PropertyName == nameof(Instance.Name)) navItem.Content = instance.Name;
                  
                };


                Nav.MenuItems.Add(navItem);
            }
        }

    }






}
