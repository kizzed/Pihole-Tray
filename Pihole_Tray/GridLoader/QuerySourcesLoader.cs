﻿using System.Windows.Controls;
using System.Windows;
using Newtonsoft.Json.Linq;

public class QuerySourcesType
{
    public string Device { get; set; }
    public string IPAddress { get; set; }
    public string RequestCount { get; set; }
}

public class QuerySourcesLoader
{
    public async Task LoadAsync(ItemsControl itemsControl, JObject obj)
    {
        var items = new List<QuerySourcesType>();


        foreach (var item in obj)
        {
            var _ = item.Key.ToString().Split('|');


            items.Add(new QuerySourcesType
            {
                Device = _[0],
                IPAddress = _[1],
                RequestCount = item.Value.ToString()
            });

        }
        Application.Current.Dispatcher.Invoke(() =>
        {
            itemsControl.ItemsSource = items;
        });
    }

}

