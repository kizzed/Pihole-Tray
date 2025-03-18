using System.Windows.Controls;
using System.Windows;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Windows.Media;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;
using Newtonsoft.Json;

public class SysinfoType
{
    public string CpuTemp { get; set; }
    public int CpuUsage { get; set; }
    public int RamUsage { get; set; }
    public int RamTotal { get; set; }
    public Brush Brush { get; set; }

}





public class SystemInfoLoader
{
    public async Task LoadAsync(
        TextBlock CpuTempTB,
        TextBlock CpuUsageTB,
        System.Windows.Documents.Run ramUsedRun,
        System.Windows.Documents.Run ramTotalRun,
        dynamic cpuTemp, 
        dynamic cpuRamUsage, 
        bool isV6,
        Brush blueBrush)
    {
        var items = new List<SysinfoType>();
        await Task.Run(() =>
        {
            try
            {
                foreach (var item in cpuTemp.sensors.list)
                {
                    if (item["name"]?.ToString() == "cpu0_thermal")
                    {
                        var tempsArray = item["temps"] as JArray;

                        if (tempsArray != null && tempsArray.Count > 0)
                        {

                            var cpuTempValue = tempsArray[0]["value"];

                            if (cpuTempValue != null)
                            {
                                float cpuTemps = cpuTempValue.Value<float>();
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    CpuTempTB.Text = cpuTemps.ToString("F1") + " °C";
                                });
                            }
                            else
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    CpuTempTB.Text = "N/A";
                                });
                            }
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                CpuTempTB.Text = "N/A";
                            });
                        }
                        break;
                    }
                }

                var totalMemory = cpuRamUsage.system.memory.ram.total / 1000;
                var usedMemory = cpuRamUsage.system.memory.ram.used / 1000;
                var cpuLoadRawFirst = (cpuRamUsage.system.cpu.load.percent[0]).ToString("F1")+" %";

                Application.Current.Dispatcher.Invoke(() =>
                {
                    CpuUsageTB.Text = cpuLoadRawFirst;
                    ramUsedRun.Text = usedMemory;
                    ramTotalRun.Text = totalMemory;
                });
                

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);

            }
        });
    }
}

