using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace Pihole_Tray
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        private string oldKeyName;
        public SettingsPage()
        {
            Load();
        }
        async Task Load()
        {
            await Task.Delay(20);
            InitializeComponent();
            SettingsWindow settingsWindow = Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
            Debug.WriteLine("SP: " + settingsWindow.GetPageName());

            foreach (var item in MainWindow.storage.Instances)
            {
                if (item.Name == settingsWindow.GetPageName())
                {
                    AddressTextBox.Text = item.Address;
                    ApiKeyTextBox.Text = item.API_KEY;
                    IsDefaultTextBox.Content = (bool)item.IsDefault ? "True" : "False";
                    IsV6TextBox.Content = (bool)item.isV6 ? "Version 6" : "Version 5";
                    NameTextBox.Text = item.Name;
                    OrderTextBox.Text = item.Order.ToString();
                    PasswordTextBox.Text = item.Password;
                    SidTextBox.Text = item.SID;
              
                    AddressTextBox.TextChanged += (sender, e) => AddressTextBox_TextChanged(sender, e, item);
                    ApiKeyTextBox.TextChanged += (sender, e) => ApiKeyTextBox_TextChanged(sender, e, item);
                    NameTextBox.TextChanged += (sender, e) => NameTextBox_TextChanged(sender, e, item);
                    OrderTextBox.TextChanged += (sender, e) => OrderTextBox_TextChanged(sender, e, item);
                    PasswordTextBox.TextChanged += (sender, e) => PasswordTextBox_TextChanged(sender, e, item);
                    SidTextBox.TextChanged += (sender, e) => SidTextBox_TextChanged(sender, e, item);
                    IsV6False.Click += (sender, e) => Version5_Click(sender, e, item);
                    IsV6True.Click += (sender, e) => Version6_Click(sender, e, item);

                    item.PropertyChanged += (sender, e) =>
                    {
                        if (e.PropertyName == nameof(Instance.SID)) SidTextBox.Text = item.SID;
                    };
                }
            }
        }

        private void AddressTextBox_TextChanged(object sender, TextChangedEventArgs e, Instance item)
        {

            item.Address = AddressTextBox.Text;
            Debug.WriteLine(item.Address);
            MainWindow.storage.WroteOverInstanceToKey(item, oldKeyName);
            Debug.WriteLine(MainWindow.selectedInstance.Name);

        }

        private void ApiKeyTextBox_TextChanged(object sender, TextChangedEventArgs e, Instance item)
        {
            item.API_KEY = ApiKeyTextBox.Text;
            Debug.WriteLine(item.API_KEY);
            MainWindow.storage.WroteOverInstanceToKey(item, oldKeyName);
        }

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e, Instance item)
        {
            oldKeyName = item.Name;
            item.Name = NameTextBox.Text;
            Debug.WriteLine(item.Name);
        }

        private void OrderTextBox_TextChanged(object sender, TextChangedEventArgs e, Instance item)
        {
            if (int.TryParse(OrderTextBox.Text, out int order))
            {
                item.Order = order;
                Debug.WriteLine(item.Order);
                MainWindow.storage.WroteOverInstanceToKey(item, oldKeyName);

            }

        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e, Instance item)
        {
            item.Password = PasswordTextBox.Text;
            Debug.WriteLine(item.Password);
            MainWindow.storage.WroteOverInstanceToKey(item, oldKeyName);
        }

        private void SidTextBox_TextChanged(object sender, TextChangedEventArgs e, Instance item)
        {
            item.SID = SidTextBox.Text;
            Debug.WriteLine(item.SID);
            MainWindow.storage.WroteOverInstanceToKey(item, oldKeyName);
        }

        private void Version5_Click(object sender, RoutedEventArgs e, Instance item)
        {
            item.isV6 = false;
            IsV6TextBox.Content = "Version 5";
            MainWindow.storage.WroteOverInstanceToKey(item, oldKeyName);
        }

        private void Version6_Click(object sender, RoutedEventArgs e, Instance item)
        {
            item.isV6 = true;
            IsV6TextBox.Content = "Version 6";
            MainWindow.storage.WroteOverInstanceToKey(item, oldKeyName);
        }

    }
}




