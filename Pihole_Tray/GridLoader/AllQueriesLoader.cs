﻿using System.Windows.Controls;
using System.Windows;
using Newtonsoft.Json.Linq;

public class AllQueriesType
{
    public string Time { get; set; }
    public string DomainName { get; set; }
}

public class AllQueriesLoader
{  
    public async Task LoadAsync(ItemsControl itemsControl, JArray arr)
    {
        await Task.Run(() =>
        {
            var items = new List<AllQueriesType>();

            foreach (var item in arr)
            {
                if (item[4].ToString() != "1" && item[4].ToString() != "4") continue; // Skipping if not blocked
                var time = DateTimeOffset.FromUnixTimeSeconds((long)item[0]).ToLocalTime().ToString("HH:mm:ss");
                var domainName = item[2].ToString();

                items.Add(new AllQueriesType
                {
                    Time = time,
                    DomainName = domainName
                });

            }
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                itemsControl.ItemsSource = items;
            });

        });
    }
}
