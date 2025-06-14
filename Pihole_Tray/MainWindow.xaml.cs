﻿using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Dynamic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shell;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Interop;
using MenuItem = Wpf.Ui.Controls.MenuItem;
using Button = Wpf.Ui.Controls.Button;
using TextBlock = System.Windows.Controls.TextBlock;
using static Interop;
using Wpf.Ui;
using TextBox = Wpf.Ui.Controls.TextBox;
using PasswordBox = Wpf.Ui.Controls.PasswordBox;
using Microsoft.Win32;
using System.Security.Policy;

namespace Pihole_Tray
{

    public partial class MainWindow : FluentWindow
    {
        private string url = "https://api.github.com/repos/PinchToDebug/Pihole-Tray/releases/latest";
        private readonly string apiUrl = "http://pi.hole/admin/api.php";
        private readonly string regKeyName = "Pihole_Tray";
        private string API_KEY;
        private bool isPinned = false;
        private bool coldRun = true;
        private bool startOnLogin;
        private bool canResize = true;
        private bool allInstanceShown = true;

        private bool isAnimating = false;
        private bool editingInstance = false;
        private bool enterAnim = false;
        private bool leaveAnim = false;
        private bool isWin11;
        private bool isDarkTheme;
        private bool notifClickUpdateInfo = false;

        public static CancellationTokenSource? cancelToken;

        public static InstanceStorage storage;
        public static Instance selectedInstance;

        private SliderValues slider;
        public static FluentWindow settingsWindow;
        private readonly HttpClient httpClient;

        // V5
        private JObject topSources;
        private JArray queries_data;
        private JObject forward_destinations;
        private JObject querytypes;

        // V6
        private JArray blocked;
        private JArray topClients;
        private JArray upStreams;
        private JObject queryTypes;

        private RegistryHelper reg;
        Brush AddressBrush;

        private Slider DisableSlider;
        private TextBlock DisableEnableButton;
        private ContextMenu DisableEnableContextMenu;
        private ContextMenu InstanceContextMenu;
        private CalcHeight CalcHeight;
      


        public MainWindow()
        {
            this.Visibility = Visibility.Visible;
            this.WindowStyle = WindowStyle.None;

            InitializeComponent();

            versionHeader.Header += " " + Process.GetCurrentProcess().MainModule.FileVersionInfo.FileVersion.ToString();

            isWin11 = isWindows11();
            if (!isWin11)
            {
                BackgroundMenuItem.Items.RemoveAt(0);
            }
            reg = new RegistryHelper(regKeyName);
            AddressBrush = AddressTB.BorderBrush;
            NavHyperlinkButton.Visibility = Visibility.Collapsed;
            LoginV6.Visibility = Visibility.Collapsed;
            LoginV5.Visibility = Visibility.Collapsed;
            //   ApiSaveBTN.Visibility = Visibility.Collapsed;

            storage = new InstanceStorage();
            slider = new SliderValues();
            CalcHeight = new CalcHeight();
            var mouseEnterStoryboard = (Storyboard)BlockHistoryCard.Resources["MouseEnterStoryboard"];
            var mouseLeaveStoryboard = (Storyboard)BlockHistoryCard.Resources["MouseLeaveStoryboard"];

            if (mouseEnterStoryboard != null)
            {
                mouseEnterStoryboard.Completed += OnAnimationCompleted!;
            }
            if (mouseLeaveStoryboard != null)
            {
                mouseLeaveStoryboard.Completed += OnAnimationCompleted!;
            }


            Default_StackPanel.Visibility = Visibility.Visible;
            Info_StackPanel.Visibility = Visibility.Hidden;

            if (reg.KeyExistsRoot("SysinfoTS")) SysinfoTS.IsChecked = (bool)reg.ReadKeyValueRoot("SysinfoTS"); HideShowElementPairs(SysInfoLBL, SysinfoCard, SysinfoTS);
            if (reg.KeyExistsRoot("RecentBlocksTS")) RecentBlocksTS.IsChecked = (bool)reg.ReadKeyValueRoot("RecentBlocksTS"); HideShowElementPairs(RecentBlockLBL, BlockHistoryCard, RecentBlocksTS);
            if (reg.KeyExistsRoot("QueryTS")) QueryTS.IsChecked = (bool)reg.ReadKeyValueRoot("QueryTS"); HideShowElementPairs(QueryLBL, QueryCard, QueryTS); Debug.WriteLine(" ");
            if (reg.KeyExistsRoot("SourcesTS")) SourcesTS.IsChecked = (bool)reg.ReadKeyValueRoot("SourcesTS"); HideShowElementPairs(SourcesLBL, SourcesCard, SourcesTS);
            if (reg.KeyExistsRoot("ForwardDestinationsTS")) ForwardDestinationsTS.IsChecked = (bool)reg.ReadKeyValueRoot("ForwardDestinationsTS"); HideShowElementPairs(ForwardDestinationsLBL, ForwardDestinationsCard, ForwardDestinationsTS);
            if (reg.KeyExistsRoot("Background"))
            {
                switch ((string)reg.ReadKeyValueRoot("Background"))
                {
                    case "Mica":
                        MicaBG.IsChecked = true;
                        this.WindowBackdropType = WindowBackdropType.Mica;

                        break;
                    case "Acrylic":
                        AcrylicBG.IsChecked = true;
                        this.WindowBackdropType = WindowBackdropType.Acrylic;
                        break;
                    case "None":
                        NoneBG.IsChecked = true;
                        this.WindowBackdropType = WindowBackdropType.None;
                        break;
                }
            }
            else            
            {
                reg.WriteToRegistryRoot("Background", "Acrylic");
                AcrylicBG.IsChecked = true;
                this.WindowBackdropType = WindowBackdropType.Acrylic;
            }
            UpdateWPFUITheme(ShouldSystemUseDarkMode());


            if (reg.KeyExistsRoot("startOnLogin")) startOnLogin = (bool)reg.ReadKeyValueRoot("startOnLogin");

            Autorun_Button.IsChecked = startOnLogin;

            this.Height = Info_StackPanel.ActualHeight + Info_StackPanel.Margin.Bottom + Info_StackPanel.Margin.Top;
            this.Left = (int)SystemParameters.PrimaryScreenWidth - this.Width - 12;
            this.Top = (int)SystemParameters.WorkArea.Bottom - this.Height - 12;
            if (Default_StackPanel.Visibility == Visibility.Visible)
            {
                this.Height = 500;
                //  this.Top = (int)SystemParameters.WorkArea.Bottom - Default_StackPanel.Height;
                Debug.WriteLine($"{(int)SystemParameters.PrimaryScreenWidth} {this.Width}");
                this.Left = (int)SystemParameters.PrimaryScreenWidth - this.Width - 12;
                this.Top = (int)SystemParameters.WorkArea.Bottom - this.Height - 12;
                Debug.WriteLine("OKKK " + Default_StackPanel.Height.ToString());
            }
            Debug.WriteLine(this.Height);
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(2000)
            };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Pi-hole tray");


            storage.FillUp();

            Debug.WriteLine($"da count::: {storage.Instances.Count}");

            if (storage.Instances.Count > 0)
            {
                // defaultInstance = storage.DefaultInstance();
                Debug.WriteLine("Using default API_KEY");

                if (!(bool)storage.DefaultInstance().isV6)
                {
                    API_KEY = storage.DefaultInstance()!.API_KEY ?? "";

                }
                // ApiTB.Text = API_KEY;
                // this.Top = (int)SystemParameters.PrimaryScreenHeight;
                if (cancelToken != null)
                {
                    cancelToken.Cancel();
                    ClearElements();
                }
                cancelToken = new CancellationTokenSource();
                selectedInstance = storage.DefaultInstance();
                this.Top = (int)SystemParameters.PrimaryScreenHeight - 1;
                //UpdateInfo(selectedInstance, cancelToken.Token);
                // INFO: It shouldn't do it, it also gets canceled when gets activated on launch.
            }

        }

        private void OnAnimationCompleted(object sender, EventArgs e)
        {
            Debug.WriteLine("CAN RESIZE now");
            canResize = true;

        }
        protected override void OnExtendsContentIntoTitleBarChanged(bool oldValue, bool newValue)
        {
            SetCurrentValue(WindowStyleProperty, WindowStyle);
            // https://github.com/lepoco/wpfui/issues/576
            // this also fixes the thick shadow
            WindowChrome.SetWindowChrome(
                this,
                new WindowChrome
                {
                    CaptionHeight = 0,
                    CornerRadius = default,
                    GlassFrameThickness = new Thickness(-1),
                    ResizeBorderThickness = ResizeMode == ResizeMode.NoResize ? default : new Thickness(4),
                    UseAeroCaptionButtons = false,
                }
            );

            _ = UnsafeNativeMethods.RemoveWindowTitlebarContents(this);
        }
        private bool isWindows11()
        {
            return FileVersionInfo.GetVersionInfo("C:\\Windows\\System32\\kernel32.dll").FileBuildPart >= 22000;
        }
        private void OnAnimationCompleted()
        {
            canResize = true;
        }
        private void OnAnimationStarted(Storyboard storyboard)
        {
            canResize = false;
        }

        private void StartMouseEnterAnimation()
        {
            var storyboard = (Storyboard)BlockHistoryCard.Resources["MouseEnterStoryboard"];
            if (storyboard != null)
            {
                OnAnimationStarted(storyboard);
                storyboard.Begin();
            }
        }

        private void StartMouseLeaveAnimation()
        {
            var storyboard = (Storyboard)BlockHistoryCard.Resources["MouseLeaveStoryboard"];
            if (storyboard != null)
            {
                OnAnimationStarted(storyboard);
                storyboard.Begin();
            }
        }
        private async void ApiSaveBTN_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(NameTB.Text)) return;
            if (string.IsNullOrEmpty(AddressTB.Text)) return;

            bool setDefault = (bool)setDefaultTS.IsChecked;
            if (storage.Instances.Count == 0)
            {
                Debug.WriteLine("Couldn't find any instance");
                setDefault = true;
            }
            Debug.WriteLine($"tf: {storage.Instances.Count == 0}");
            Instance temp = new Instance
            {
                Name = NameTB.Text,
                Address = AddressTB.Text,
                Password = PasswordTB.Password,
                API_KEY = ApiTB.Text,
                SID = "noSID",
                Order = storage.Instances.Count + 1,
                isV6 = !ApiTB.Text.Contains("/admin/api.php"),
                IsDefault = (bool)setDefaultTS.IsChecked ? true : setDefault,
            };
            notifClickUpdateInfo = false;

            stopUpdatingInfo = false;
            apiSaveCalled = true;

            if (cancelToken != null)
            {
                cancelToken.Cancel();
                ClearElements();

            }
            cancelToken = new CancellationTokenSource();
            ClearElements();
            UpdateInfo(temp, cancelToken.Token);

        }

        private bool stopUpdatingInfo = false;

        bool apiSaveCalled = false;



        private async void UpdateWPFUITheme(bool darkMode)
        {
            ApplicationTheme theme = darkMode ? ApplicationTheme.Dark : ApplicationTheme.Light;
            Window? mainWindow = UiApplication.Current.MainWindow;
            UiApplication.Current.MainWindow = null;
            ApplicationThemeManager.Apply(theme, Wpf.Ui.Controls.WindowBackdropType.None, false);
            UiApplication.Current.MainWindow = mainWindow;

            Brush foregroundBrush = darkMode ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFBBC4F7")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF465AFF"));
            Brush greenBrush = darkMode ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6EF563")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF26B100"));
            Brush redBrush = darkMode ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFB4B4")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE63B3B"));
            Brush redBrush2 = darkMode ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF7E7E")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF5E5E"));

            Brush purpleBrush = darkMode ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFABA4FF")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5B50E2"));


            for (int i = 0; i < QueryTypesGrid.RowDefinitions.Count; i++)
            {
                var percentageBlock = QueryTypesGrid.Children.OfType<TextBlock>()
                    .FirstOrDefault(tb => Grid.GetRow(tb) == i && Grid.GetColumn(tb) == 1);

                if (percentageBlock != null)
                {
                    percentageBlock.Foreground = foregroundBrush;
                }
            }

            for (int i = 0; i < ForwardDestinationsGrid.RowDefinitions.Count; i++)
            {
                var percentageBlock = ForwardDestinationsGrid.Children.OfType<TextBlock>()
                    .FirstOrDefault(tb => Grid.GetRow(tb) == i && Grid.GetColumn(tb) == 1);

                if (percentageBlock != null)
                {
                    percentageBlock.Foreground = foregroundBrush;
                }
            }


            if (StatusTB.Text == "enabled") StatusTB.Foreground = greenBrush;
            else StatusTB.Foreground = redBrush;
            DomainsBlockedTB.Foreground = redBrush2;
            AdsBlockedTB.Foreground = redBrush2;
            DnsQueryTB.Foreground = purpleBrush;


            Color color = isDarkTheme ? Colors.White : Colors.Black;
            Exit_Button.Foreground = new SolidColorBrush(color);


            if (AcrylicBG.IsChecked == true)
            {
                if (darkMode)
                {
                    MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#02FFFFFF"));
                    this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B2101010"));
                }
                else
                {
                    this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B2FFFFFF"));
                    MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#02FFFFFF"));
                }
            }
            if (MicaBG.IsChecked == true)
            {
                if (darkMode)
                {
                    this.Background = new SolidColorBrush(Colors.Transparent);
                    MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#02FFFFFF"));
                }
                else
                {
                    this.Background = new SolidColorBrush(Colors.Transparent);
                    MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#02FFFFFF"));
                }
            }
            if (NoneBG.IsChecked == true)
            {
                if (darkMode)
                {
                    this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF292929"));
                }
                else
                {
                    MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#02FFFFFF"));

                    this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEDEDED"));
                }
            }
        }

    
        





        public async void UpdateInfo(Instance instance, CancellationToken token)
        {
            if (instance == null)
            {
                return;
            }
            try
            {

                instance.isV6 = !instance.Address!.Contains("/admin/api.php");
                OpenInBrowser_Button.Header = instance.Name;
                if (coldRun) this.Top = (int)SystemParameters.PrimaryScreenHeight;
                Debug.WriteLine("v6 status: " + instance.isV6);
                selectedInstance = instance;

                if (!string.IsNullOrEmpty(ApiTB.Text))
                {
                    Debug.WriteLine("using key from TB");
                    //  WriteToRegistry("startOnLogin", Autorun_Button.IsChecked);
                }

                var contentDialog = new ContentDialog(RootContentDialogPresenter);

                try
                {
                    dynamic _ = new ExpandoObject();
                    Debug.WriteLine($"Instances address: {instance.Address}");
                    bool pingFailed = false;
                    try
                    {
                        if (instance.isV6 == true)
                        {
                            try
                            {
                                await instance.Login(instance.Password, httpClient);
                            }
                            catch (Exception ex)
                            {
                                if (token != null)
                                {
                                    ContentGrid.Effect = new BlurEffect
                                    {
                                        Radius = 20,
                                        RenderingBias = RenderingBias.Quality,
                                        KernelType = KernelType.Gaussian
                                    };
                                    LostConnectionGrid.Visibility = Visibility.Visible;
                                    LostConnectionTB.Text = $"{ex.Message}";
                                }
                                return;
                            }
                            storage.WriteInstanceToKey(instance);

                        }
                        else
                        {
                            _ = JsonConvert.DeserializeObject<dynamic>(await httpClient.GetStringAsync(instance.Address + "?summary&auth=" + instance.API_KEY))!;

                            DnsQueryTB.Text = DnsQueryTB.Text = _.dns_queries_all_types;
                            storage.WriteInstanceToKey(instance);

                        }


                    }
                    catch (Exception ex)
                    {

                        Debug.WriteLine("ERROR 1: " + ex.Message);
                        using (Ping ping = new Ping())
                        {
                            try
                            {

                                PingReply reply = ping.Send(new Uri(instance.Address).AbsoluteUri, 2000);

                                if (reply.Status != IPStatus.Success)
                                {
                                    contentDialog.SetCurrentValue(ContentDialog.TitleProperty, $"Error");
                                    contentDialog.SetCurrentValue(ContentControl.ContentProperty, $"{reply.Status}");
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine("ERROR 2: " + e.Message);

                                pingFailed = true;
                                contentDialog.SetCurrentValue(ContentDialog.TitleProperty, $"Error");
                                contentDialog.SetCurrentValue(ContentControl.ContentProperty, ex.Message+"\n"+e.Message);
                                if (reg.KeyExists("API_KEY", instance))
                                {
                                    LoginBTN.Visibility = Visibility.Visible;
                                    ApiSaveBTN.Visibility = Visibility.Visible;

                                }
                            }
                        }
                        if (!pingFailed)
                        {
                            contentDialog.SetCurrentValue(ContentDialog.TitleProperty, "Error");
                            contentDialog.SetCurrentValue(ContentControl.ContentProperty, ex.Message);
                        }

                        Debug.WriteLine($"ERROR: {ex.Message}");
                        this.Top = (int)SystemParameters.WorkArea.Bottom - this.Height - 12;
                        await contentDialog.ShowAsync();

                        return;
                    }


                }
                catch (Exception ex)
                {

                    Debug.WriteLine($"Request error: {ex.Message} \n");
                    return;
                }

                if (!notifClickUpdateInfo)
                {
                    Default_StackPanel.Visibility = Visibility.Hidden;
                    Info_StackPanel.Visibility = Visibility.Visible;
                    notifClickUpdateInfo = false;
                }

                Debug.WriteLine("Connected successfully!");

                dynamic summary = new ExpandoObject();
                dynamic status = new ExpandoObject();
                dynamic cpuTemp = new ExpandoObject();
                dynamic cpuRamUsage = new ExpandoObject();


                //if (coldRun)
                //{
                //    storage.WriteInstanceToKey(instance);
                //    writeOnce = false;
                //}

                bool showEffect = true;
                //bool blurCard = true;
                dynamic response;

                while (true)
                {

                        UpdateWPFUITheme(ShouldSystemUseDarkMode());
                        isDarkTheme = ShouldSystemUseDarkMode();
                  
                   
                                  
                    
                    token.ThrowIfCancellationRequested();
                    // Debug.WriteLine($"api_i: {instance.API_KEY}");
                    if (storage.Instances.Count > 1)
                    {
                        CurrentNameTB.Text = $"{instance.Name}:";
                    }
                    else
                    {
                        CurrentNameTB.Text = "Status:";
                    }
                    //if (this.Visibility != Visibility.Visible && !coldRun)
                    //{
                    //   await Task.Delay(100);

                    //    continue;
                    //}
                    if (this.Visibility != Visibility.Visible)
                    {
                        Debug.WriteLine("GOT HIDDEN");
                        return;
                    }
                    var tasks = new List<Task>();
                    while (isAnimating)
                    {
                        await Task.Delay(10);

                        continue;
                    }
                    if (coldRun)
                    {
                        this.Visibility = Visibility.Hidden;
                    }

                    try
                    {
                        await Task.Delay(50, token);
                        if (instance.isV6 == true)
                        {

                            summary = JsonConvert.DeserializeObject<dynamic>(await httpClient.GetStringAsync($"{instance.Address}/stats/summary"))!;
                            await Task.Delay(50, token);

                            status = JsonConvert.DeserializeObject<dynamic>(await httpClient.GetStringAsync($"{instance.Address}/dns/blocking"))!;
                            await Task.Delay(50, token);

                            if ((bool)SysinfoTS.IsChecked!)
                            {
                                cpuTemp = JsonConvert.DeserializeObject<dynamic>(await httpClient.GetStringAsync($"{instance.Address}/info/sensors"));
                                await Task.Delay(50, token);

                                cpuRamUsage = JsonConvert.DeserializeObject<dynamic>(await httpClient.GetStringAsync($"{instance.Address}/info/system"));
                                await Task.Delay(50, token);
                            }
                                if ((bool)RecentBlocksTS.IsChecked!)
                            {
                                response = JsonConvert.DeserializeObject<dynamic>(await httpClient.GetStringAsync($"{instance.Address}/queries?length=100&upstream=blocklist"));
                                blocked = (JArray)response.queries;
                                await Task.Delay(50, token);

                            }
                            if ((bool)SourcesTS.IsChecked!)
                            {
                                response = JsonConvert.DeserializeObject<dynamic>(await httpClient.GetStringAsync($"{instance.Address}/stats/top_clients"));
                                topClients = (JArray)response.clients;
                                await Task.Delay(50, token);
                            }
                            if ((bool)ForwardDestinationsTS.IsChecked!)
                            {
                                //working on this currently
                                response = JsonConvert.DeserializeObject<dynamic>(await httpClient.GetStringAsync($"{instance.Address}/stats/upstreams"))!;
                                upStreams = (JArray)response.upstreams;
                                await Task.Delay(50, token);

                            }
                            if ((bool)QueryTS.IsChecked!)
                            {
                                response = JsonConvert.DeserializeObject<dynamic>(await httpClient.GetStringAsync($"{instance.Address}/stats/query_types"));
                                queryTypes = (JObject)response.types;
                            }

                        }

                        else
                        {

                            summary = JsonConvert.DeserializeObject<dynamic>(await httpClient.GetStringAsync(instance.Address + "?summary&auth=" + instance.API_KEY))!;
                            await Task.Delay(50, token);
                            if ((bool)RecentBlocksTS.IsChecked!)
                            {

                                response = JsonConvert.DeserializeObject<dynamic>(await httpClient.GetStringAsync(instance.Address + "?getAllQueries=250&auth=" + instance.API_KEY))!;
                                var queriesData = (JArray)response.data;
                                queries_data = new JArray(queriesData.Reverse());
                                await Task.Delay(50, token);
                            }
                            if ((bool)SourcesTS.IsChecked!)
                            {
                                response = JsonConvert.DeserializeObject<dynamic>(await httpClient.GetStringAsync(instance.Address + "?getQuerySources&auth=" + instance.API_KEY))!;
                                topSources = (JObject)response.top_sources;
                                await Task.Delay(50, token);

                            }
                            if ((bool)ForwardDestinationsTS.IsChecked!)
                            {
                                response = JsonConvert.DeserializeObject<dynamic>(await httpClient.GetStringAsync(instance.Address + "?getForwardDestinations&auth=" + instance.API_KEY))!;
                                forward_destinations = (JObject)response.forward_destinations;
                                await Task.Delay(50, token);

                            }
                            if ((bool)QueryTS.IsChecked!)
                            {

                                response = JsonConvert.DeserializeObject<dynamic>(await httpClient.GetStringAsync(instance.Address + "?getQueryTypes&auth=" + instance.API_KEY))!;
                                querytypes = (JObject)response.querytypes;
                            }

                            DnsQueryTB.Text = summary.dns_queries_all_types;
                        }

                        if (!editingInstance)
                        {
                            ContentGrid.Effect = null;
                        }
                       
                        showEffect = true;
                        LostConnectionGrid.Visibility = Visibility.Hidden;
                    }
                    catch (TaskCanceledException to)
                    {

                        //Debug.WriteLine("ERROR 3: " + to.Message);
                        //if (showEffect && token != null)
                        //{
                        //    ContentGrid.Effect = new BlurEffect
                        //    {
                        //        Radius = 20,
                        //        RenderingBias = RenderingBias.Quality,
                        //        KernelType = KernelType.Gaussian
                        //    };
                        //    showEffect = false;
                        //    LostConnectionGrid.Visibility = Visibility.Visible;
                        //    LostConnectionTB.Text = "lost connection, trying to reconnect";
                        //}

                        // Not needed
                        return;

                    }
                    catch (Exception e)
                    {
                        try
                        {
                            await instance.Login(instance.Password, httpClient);
                        }
                        catch (Exception ex)
                        {
                            if (showEffect && token != null)
                            {
                                ContentGrid.Effect = new BlurEffect
                                {
                                    Radius = 20,
                                    RenderingBias = RenderingBias.Quality,
                                    KernelType = KernelType.Gaussian
                                };
                                showEffect = false;
                                LostConnectionGrid.Visibility = Visibility.Visible;
                                LostConnectionTB.Text = $"{e.Message} \n{ex.Message}";
                            }
                            continue;
                        }
                        storage.WriteInstanceToKey(instance);
                        Debug.WriteLine("ERROR 3: " + e.Message);
                        if (showEffect && token != null)
                        {
                            ContentGrid.Effect = new BlurEffect
                            {
                                Radius = 20,
                                RenderingBias = RenderingBias.Quality,
                                KernelType = KernelType.Gaussian
                            };
                            showEffect = false;
                            LostConnectionGrid.Visibility = Visibility.Visible;
                            LostConnectionTB.Text = e.Message;
                        }
                        continue;

                    }


                    while (isAnimating)
                    {
                        await Task.Delay(15, token);
                        continue;
                    }

                    token.ThrowIfCancellationRequested();

                    Brush redBrush = isDarkTheme
                         ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFB4B4"))
                         : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE63B3B"));

                    Brush greenBrush = isDarkTheme
                         ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6EF563"))
                         : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF26B100"));

                    Brush blueBrush = isDarkTheme
                         ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFBBC4F7"))
                         : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6A7AFF"));
                   
                    Brush purpleBrush = isDarkTheme
                         ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDDE1FB"))
                         : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF7B8BF5"));


                    CpuTempTB.Foreground = blueBrush;
                    CpuUsageTB.Foreground = blueBrush;
                    RamUsageTB.Foreground = blueBrush;
                    ramUsedRun.Foreground = blueBrush;
                    ramTotalRun.Foreground = blueBrush;

                    if (instance.isV6 == true)
                    {
                        GravityLB.Visibility = Visibility.Collapsed;
                        GravityTB.Visibility = Visibility.Collapsed;
                        GravityRow.Height = new GridLength(0.0);
                        try // Temp solution
                        {
                            AdsBlockedTB.Text = string.Format("{0:N0}", summary.queries.blocked);
                            DnsQueryTB.Text = string.Format("{0:N0}", summary.queries.total);
                            DomainsBlockedTB.Text = string.Format("{0:N0}", summary.gravity.domains_being_blocked);
                        }
                        catch
                        {
                            Debug.WriteLine("String formatting failed");
                            AdsBlockedTB.Text = summary.queries.blocked;
                            DnsQueryTB.Text = summary.queries.total;
                            DomainsBlockedTB.Text = summary.gravity.domains_being_blocked;
                        }

                        StatusTB.Text = status.blocking;

                        if (StatusTB.Text == "enabled") StatusTB.Foreground = greenBrush;
                        else StatusTB.Foreground = redBrush;

                        if ((bool)SysinfoTS.IsChecked) await new SystemInfoLoader().LoadAsync(CpuTempTB,CpuUsageTB,ramUsedRun,ramTotalRun, cpuTemp, cpuRamUsage, (bool)instance.isV6, blueBrush);
                        if ((bool)RecentBlocksTS.IsChecked) await new RecentBlocksLoader().LoadAsync(BlockHistoryItemsControl, blocked, (bool)instance.isV6, redBrush);
                        if ((bool)SourcesTS.IsChecked) await new SourcesLoader().LoadAsync(SourcesItemsControl, topClients, (bool)instance.isV6, purpleBrush, blueBrush);
                        if ((bool)ForwardDestinationsTS.IsChecked) await new DnsRoutesLoader().LoadAsync(ForwardDestinationsGrid, upStreams, (bool)instance.isV6, isDarkTheme);
                        if ((bool)QueryTS.IsChecked) await new QueryTypesLoader().LoadAsync(QueryTypesGrid, queryTypes,(bool)instance.isV6, isDarkTheme);
                        token.ThrowIfCancellationRequested();
                    }
                    else
                    {
                        GravityLB.Visibility = Visibility.Visible;
                        GravityTB.Visibility = Visibility.Visible;
                        GravityRow.Height = new GridLength(22.0);
                        AdsBlockedTB.Text = summary.ads_blocked_today;
                        GravityTB.Text = $"{(summary.gravity_last_updated.relative.days > 0 ? summary.gravity_last_updated.relative.days + "d" : string.Empty)} " + $"{(summary.gravity_last_updated.relative.hours > 0 ? summary.gravity_last_updated.relative.hours + "h" : string.Empty)} " + $"{(summary.gravity_last_updated.relative.minutes > 0 ? summary.gravity_last_updated.relative.minutes + "m" : string.Empty)}".Replace("  ", " ");
                        DomainsBlockedTB.Text = summary.domains_being_blocked;

                        StatusTB.Text = summary.status;

                        if (StatusTB.Text == "enabled") StatusTB.Foreground = greenBrush;
                        else StatusTB.Foreground = redBrush;

                        if ((bool)RecentBlocksTS.IsChecked) await new RecentBlocksLoader().LoadAsync(BlockHistoryItemsControl, queries_data, (bool)instance.isV6, redBrush);
                        if ((bool)SourcesTS.IsChecked) await new SourcesLoader().LoadAsync(SourcesItemsControl, topSources, (bool)instance.isV6, purpleBrush, blueBrush);
                        if ((bool)ForwardDestinationsTS.IsChecked) await new DnsRoutesLoader().LoadAsync(ForwardDestinationsGrid, forward_destinations, (bool)instance.isV6, isDarkTheme);
                        if ((bool)QueryTS.IsChecked) await new QueryTypesLoader().LoadAsync(QueryTypesGrid, querytypes, (bool)instance.isV6, isDarkTheme);
                        token.ThrowIfCancellationRequested();
                    }


                    if (apiSaveCalled)
                    {
                        bool shouldWrite = true;
                        foreach (Instance i in storage.Instances)
                        {
                            if (i.Name == instance.Name)
                            {
                                shouldWrite = false;
                            }
                        }

                        if (shouldWrite) // if name isnt contained
                        {
                            //  selectedInstance = instance;
                            if ((bool)setDefaultTS.IsChecked)
                            {
                                foreach (var i in storage.Instances)
                                {
                                    if (i.IsDefault == true)
                                    {
                                        i.IsDefault = false;
                                        storage.WriteInstanceToKey(i);
                                    }
                                }
                            }
                            storage.WriteInstanceToKey(instance);
                            storage.Instances.Add(instance);
                        }
                        apiSaveCalled = false;
                    }



                    if (canResize /*&& !BlockHistoryCard.IsMouseOver*/)
                    {
                        Info_StackPanel.Height = CalcHeight.Calc(Info_StackPanel);

                        this.Height = CalcHeight.Calc(Info_StackPanel);
                        this.Left = (int)SystemParameters.PrimaryScreenWidth - this.Width - 12;

                        if (coldRun) this.Top = (int)SystemParameters.PrimaryScreenHeight;
                        else this.Top = (int)SystemParameters.WorkArea.Bottom - this.Height - 12;
                    }


                    if (coldRun)
                    {
                        coldRun = false;

                        // this.Visibility = Visibility.Hidden;
                        await Task.Delay(50, token);
                        continue;
                    }
                    await Task.Delay(1000, token);

                }

            }
            catch (OperationCanceledException e){ }
            catch (Exception e)
            {
                Debug.WriteLine($"Updateinfo crash: {e.Message}");
                if (instance.isV6 == true)
                {
                    try
                    {
                        await instance.Login(instance.Password, httpClient);
                    }
                    catch (Exception ex)
                    {
                        if ( token != null)
                        {
                            ContentGrid.Effect = new BlurEffect
                            {
                                Radius = 20,
                                RenderingBias = RenderingBias.Quality,
                                KernelType = KernelType.Gaussian
                            };

                            LostConnectionGrid.Visibility = Visibility.Visible;
                            LostConnectionTB.Text = $"{e.Message} \n{ex.Message}";
                        }
                        
                    }

                    storage.WriteInstanceToKey(instance);

                }
            }

        }



        private async void OnMouseEnterBlockHistoryCard(object sender, MouseEventArgs e)
        {
            await Task.Delay(300);
            if (BlockHistoryCard.IsMouseOver && BlockHistoryItemsControl.Items.Count > 8)
            {
                BlockHistorySV.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

                if (BlockHistoryCard != null)
                {
                    isAnimating = true;
                    enterAnim = true;
                    double targetHeight = this.ActualHeight - BlockHistoryCard.TransformToVisual(this).Transform(new Point(0, 0)).Y - 16;
                    var animation = new DoubleAnimation
                    {
                        To = targetHeight,
                        Duration = TimeSpan.FromSeconds(0.15)
                    };

                    animation.Completed += (s, e) =>
                    {
                        if (!leaveAnim)
                        {
                            isAnimating = false;
                        }
                        enterAnim = false;
                    };
                    BlockHistorySV.MinHeight = targetHeight-10;
                    BlockHistoryCard.BeginAnimation(FrameworkElement.HeightProperty, animation);
                }
            }
            else
            {
                isAnimating = false;
                enterAnim = false;
            }
        }
        private void OnMouseLeaveBlockHistoryCard(object sender, MouseEventArgs e)
        {
            if (BlockHistoryCard != null)
            {
                isAnimating = true;
                leaveAnim = true;
                var animation = new DoubleAnimation
                {
                    To = 161, // original height of blockHistoryCard
                    Duration = TimeSpan.FromSeconds(0.15)
                };

                animation.Completed += (s, e) =>
                {
                    if (!enterAnim)
                    {
                        isAnimating = false;
                        BlockHistorySV.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                    }

                    leaveAnim = false;
                };
                BlockHistoryCard.BeginAnimation(FrameworkElement.HeightProperty, animation);
                BlockHistorySV.ScrollToVerticalOffset(0);

            }
        }



        private static int CountProperties(dynamic obj)
        {
            return ((JObject)obj.forward_destinations).Count;
        }
        protected override void OnDeactivated(EventArgs e)
        {
            if (!isPinned)
            {
                this.Hide();
            }
        }

        private double GetTotalVisibleHeightWithPadding(Grid grid)
        {
            double totalHeight = 0;

            foreach (UIElement child in grid.Children)
            {
                if (child is FrameworkElement frameworkElement && frameworkElement.Visibility == Visibility.Visible)
                {

                    frameworkElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                    totalHeight += frameworkElement.DesiredSize.Height +
                                   frameworkElement.Margin.Top +
                                   frameworkElement.Margin.Bottom;
                }
            }
            return totalHeight;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (storage.Instances.Count() > 0)
            {
                if (cancelToken != null)
                {
                    cancelToken.Cancel();
                }
                if (selectedInstance != null)
                {
                    cancelToken = new CancellationTokenSource();

                    UpdateInfo(selectedInstance, cancelToken.Token);

                }
            }
            this.Show();
        }

        private void NotifyIcon_LeftClick(Wpf.Ui.Tray.Controls.NotifyIcon sender, RoutedEventArgs e)
        {
            base.OnActivated(e); this.Activate();
            if (!this.IsVisible || this.Top >= SystemParameters.PrimaryScreenHeight)
            {
                this.Show(); this.Activate();
                this.Top = (int)SystemParameters.WorkArea.Bottom - this.Height - 12;
            }

            if (storage.Instances.Count() > 0)
            {
                if (cancelToken != null)
                {
                    cancelToken.Cancel();
                }
                cancelToken = new CancellationTokenSource();
                notifClickUpdateInfo = true;
                UpdateInfo(selectedInstance, cancelToken.Token);

            }
        }



        private void fluentWindow_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int GWL_STYLE = -16;
            int WS_CAPTION = 0x00020000;
            int style = GetWindowLong(hwnd, GWL_STYLE);
            int newStyle = style & ~WS_CAPTION;

            SetWindowLong(hwnd, GWL_STYLE, style & ~WS_CAPTION); // Removes the opening and closing animation
        }



    




        private void ExitApp(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Exit_Button_MouseEnter(object sender, MouseEventArgs e)
        {
            Exit_Button.Foreground = new SolidColorBrush(Color.FromArgb(255, 254, 107, 107));

        }

        private void Exit_Button_MouseLeave(object sender, MouseEventArgs e)
        {
            Color color = isDarkTheme ? Colors.White : Colors.Black;
            Exit_Button.Foreground = new SolidColorBrush(color);
        }


        private void Autorun_Checked(object sender, RoutedEventArgs e)
        {
            if (Autorun_Button.IsChecked)
            {

                reg.AddToAutoRun(regKeyName, Process.GetCurrentProcess().MainModule!.FileName);
            }
            else
            {
                reg.RemoveFromAutoRun(regKeyName);
            }
           reg.WriteToRegistryRoot("startOnLogin", Autorun_Button.IsChecked);
        }

        private async void OpenInBrowser_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = selectedInstance.Address;

                Debug.WriteLine(selectedInstance.isV6);
                if (selectedInstance.isV6 == true)
                {
                    Debug.WriteLine("replav6");
                    url = url.Replace("/api", "");
                }
                else
                {
                    url = url.Replace("/admin/api.php", "");

                }
                ProcessStartInfo sInfo = new ProcessStartInfo($"{url}/admin") { UseShellExecute = true };
                _ = Process.Start(sInfo);
            }
            catch
            {
            }
        }


        private void PinApp(object sender, RoutedEventArgs e)
        {
            if (!isPinned)
            {
                Pin_Button.Icon = new SymbolIcon(SymbolRegular.PinOff20);
                Pin_Button.Header = "Unpin App";
                isPinned = true;
                this.Activate();
                this.Show();
                this.Topmost = true;

                if (cancelToken != null)
                {
                    cancelToken.Cancel();

                }
                cancelToken = new CancellationTokenSource();
                if (selectedInstance != null)
                {
                    UpdateInfo(selectedInstance, cancelToken.Token);

                }
            }
            else
            {
                Pin_Button.Icon = new SymbolIcon(SymbolRegular.Pin20);
                isPinned = false;
                this.Topmost = false;
                Pin_Button.Header = "Pin App";
                this.Hide();
            }
        }

        private void LoginBTN_Click(object sender, RoutedEventArgs e)
        {
            if (cancelToken != null)
            {
                cancelToken.Cancel();
                ClearElements();
            }
            cancelToken = new CancellationTokenSource();

            UpdateInfo(selectedInstance, cancelToken.Token);
        }



        private void HideShowElementPairs(Label label, Card card, ToggleSwitch ts)
        {

            if (ts.IsChecked == true)
            {
                label.Visibility = Visibility.Visible;
                card.Visibility = Visibility.Visible;
                reg.WriteToRegistryRoot(ts.Name, ts.IsChecked);
            }
            else
            {
                label.Visibility = Visibility.Collapsed;
                card.Visibility = Visibility.Collapsed;
                reg.WriteToRegistryRoot(ts.Name, ts.IsChecked);
            }
            Resize();
        }



        private void Resize()
        {
            if (canResize && !BlockHistoryCard.IsMouseOver)
            {
                Info_StackPanel.Height = CalcHeight.Calc(Info_StackPanel);

                this.Height = CalcHeight.Calc(Info_StackPanel);
                this.Left = (int)SystemParameters.PrimaryScreenWidth - this.Width - 12;

                if (coldRun) this.Top = (int)SystemParameters.PrimaryScreenHeight;
                else this.Top = (int)SystemParameters.WorkArea.Bottom - this.Height - 12;
            }
        }


        private void SysinfoTS_Click(object sender, RoutedEventArgs e)
        {
            HideShowElementPairs(SysInfoLBL, SysinfoCard, SysinfoTS);

        }
        private void RecentBlocksTS_Click(object sender, RoutedEventArgs e)
        {
            HideShowElementPairs(RecentBlockLBL, BlockHistoryCard, RecentBlocksTS);

        }
        private void SourcesTS_Click(object sender, RoutedEventArgs e)
        {
            HideShowElementPairs(SourcesLBL, SourcesCard, SourcesTS);

        }
        private void ForwardDestinationsTS_Click(object sender, RoutedEventArgs e)
        {
            HideShowElementPairs(ForwardDestinationsLBL, ForwardDestinationsCard, ForwardDestinationsTS);


        }
        private void QueryTS_Click(object sender, RoutedEventArgs e)
        {
            HideShowElementPairs(QueryLBL, QueryCard, QueryTS);

        }



        private void DisableSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Debug.WriteLine(DisableSlider.Value);

            if (slider.Values.TryGetValue((int)DisableSlider.Value, out var data))
            {
                DisableEnableButton.Text = data.Item1;
            }

        }

        private async void DisableButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("HEY");

            if (DisableSlider != null && slider.Values.TryGetValue((int)DisableSlider.Value, out var data) && StatusTB.Text == "enabled")
            {
                Debug.WriteLine($"Trying to disable");
                HttpResponseMessage response;
                if (selectedInstance.isV6 == true)
                {
                    response = await httpClient.PostAsJsonAsync($"{selectedInstance.Address}/dns/blocking", new { blocking = false, timer = data.Item2 });
                }
                else
                {
                    response = await httpClient.GetAsync(selectedInstance.Address + $"?disable={data.Item2}&auth=" + selectedInstance.API_KEY);
                }


                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Disabled for: {data.Item1}");
                }
                else
                {
                    Debug.WriteLine($"FAILED to disable");

                }
                DisableSlider.Value = 0;

            }



            else if (StatusTB.Text == "disabled")
            {
                Debug.WriteLine($"Trying to enable");
                HttpResponseMessage response;
                if (selectedInstance.isV6 == true)
                {
                    response = await httpClient.PostAsJsonAsync($"{selectedInstance.Address}/dns/blocking", new { blocking = true, timer = 0 });
                }
                else
                {
                    response = await httpClient.GetAsync(selectedInstance.Address + "?enable&auth=" + selectedInstance.API_KEY);
                }
                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Enabled");
                }
                else
                {
                    Debug.WriteLine($"FAILED to enable");
                }
            }

            DisableEnableContextMenu.IsOpen = false;
        }


        private async void Instance_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // InstanceContextMenu.ClearValue();
            InstanceContextMenu = new ContextMenu();
            if (storage.Instances.Count > 1)
            {
                //TODO: implement view all
                goto Skip;
                Debug.WriteLine("da count: " + storage.Instances.Count);

                MenuItem allView = new MenuItem
                {
                    FontSize = 13,
                    IsEnabled = false,
                    FontWeight = FontWeights.Normal,
                    Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children = {
                          new TextBlock
                          {
                              Text = "\u25CF",
                              FontSize = 16,
                              Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                              Margin = new Thickness(0, 0, 7, 2),
                              VerticalAlignment = VerticalAlignment.Center
                          },
                          new TextBlock
                          {
                              Text = "View all",
                              VerticalAlignment = VerticalAlignment.Center
                          }
                    }
                    }
                };

                InstanceContextMenu.Items.Add(allView);
                InstanceContextMenu.Items.Add(new Separator());
            }

        Skip:


            foreach (Instance instance in storage.Instances)
            {
                MenuItem menuItem = new MenuItem
                {
                    Tag = instance,
                    FontSize = 13,
                    FontWeight = FontWeights.Normal,
                    Header = new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition(),
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                        Children =
                        {
                            new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                Children =
                                {
                                    new TextBlock
                                    {
                                        ToolTip = "",
                                        Text = "\u25CF",
                                        FontSize = 16,
                                        Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                                        Margin = new Thickness(0, 0, 7, 2),
                                        VerticalAlignment = VerticalAlignment.Center
                                    },
                                    new TextBlock
                                    {
                                        Text = instance.Name,
                                        VerticalAlignment = VerticalAlignment.Center
                                    }
                                }
                            },
                            new Button
                            {
                                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings48 },
                                FontSize = 12,
                                Padding = new Thickness(3),
                                VerticalAlignment = VerticalAlignment.Center,
                                Visibility = Visibility.Hidden
                            }
                        }
                    }
                };
                Grid.SetColumn((menuItem.Header as Grid).Children[0], 0); 
                Grid.SetColumn((menuItem.Header as Grid).Children[1], 1);


                Button editButton = (Button)((Grid)menuItem.Header).Children[1];
                editButton.Click += EditInstanceButton_Click;

                Debug.WriteLine($"instance menu added:{instance.Name}");
                menuItem.MouseEnter += Instance_MenuItem_MouseEnter;
                menuItem.MouseLeave += Instance_MenuItem_MouseLeave;
                menuItem.Click += InstanceSelected_Click;
                InstanceContextMenu.Items.Add(menuItem);
            }


            MenuItem AddButton = new MenuItem
            {
                Header = new TextBlock
                {
                    Text = "\u002B",
                    FontSize = 18,
                    Margin = new Thickness(0, 0, 7, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                },

            };
            AddButton.Click += Addbutton_Click;

            if (storage.Instances.Count > 1)
            {
                InstanceContextMenu.Items.Add(new Separator());
            }
            InstanceContextMenu.Items.Add(AddButton);
            InstanceContextMenu.IsOpen = true;

            foreach (var item in InstanceContextMenu.Items)
            {
                if (item is MenuItem menuItem && menuItem.Header is Grid grid)
                {

                    if (grid.Children[0] is StackPanel sp)
                    {
                        var textBlock = sp.Children.OfType<TextBlock>().Skip(1).FirstOrDefault();
                        if (textBlock != null)
                        {
                            string menuItemText = textBlock.Text;

                            foreach (var instance in storage.Instances)
                            {
                                if (menuItemText == instance.Name)
                                {
                                    {
                                        _ = Task.Run(() =>
                                        {
                                            int status = instance.Status().Result;
                                            if (instance.isV6 == true) // TODO: change later when instances can be edited
                                            {
                                                storage.WriteInstanceToKey(instance);
                                            }

                                            Debug.WriteLine($"{instance.Name}, {status}");
                                            Dispatcher.Invoke(() =>
                                            {
                                                SolidColorBrush brush = new SolidColorBrush();
                                                switch (status)
                                                {
                                                    case 0: // Enabled
                                                        brush = new SolidColorBrush(Color.FromRgb(70, 244, 64)); // Green
                                                        break;

                                                    case 1: // Disabled
                                                        brush = new SolidColorBrush(Color.FromRgb(244, 64, 64)); // Red
                                                        break;

                                                    case 2: // Reachable but can't reach API
                                                        brush = new SolidColorBrush(Color.FromRgb(64, 116, 244)); // Blue
                                                        menuItem.Click -= InstanceSelected_Click;
                                                        if (instance.isV6 == true)
                                                        {
                                                            menuItem.ToolTip = "Address reachable, but API inaccessible.\nLikely the password is incorrect.";
                                                        }
                                                        else
                                                        {
                                                            menuItem.ToolTip = "Address reachable, but API inaccessible.";
                                                        }
                                                        menuItem.Cursor = Cursors.Help;
                                                        break;

                                                    case -1: // Unreachable
                                                        brush = new SolidColorBrush(Color.FromRgb(244, 207, 64)); // Orange-ish
                                                        menuItem.ToolTip = "Address unreachable.";
                                                        menuItem.Click -= InstanceSelected_Click;
                                                        menuItem.Cursor = Cursors.Help;

                                                        break;
                                                    default:
                                                        break;
                                                }

                                                (sp.Children[0] as TextBlock).Foreground = brush;

                                            });

                                        });
                                    }
                                }
                            }
                        }
                    }
                }

            }

        }
        private async void ClearElements()
        {
            BlockHistoryItemsControl.ItemsSource = null;
            SourcesItemsControl.ItemsSource = null;
            ForwardDestinationsGrid.Children.Clear();
            ForwardDestinationsGrid.RowDefinitions.Clear();
            QueryTypesGrid.Children.Clear();
            QueryTypesGrid.RowDefinitions.Clear();
        }

        private async void EditInstanceButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton == null)
                return;

            MenuItem parentMenuItem = null;

            DependencyObject current = clickedButton;
            while (current != null)
            {
                if (current is MenuItem parent)
                {
                    parentMenuItem = parent;
                    break;
                }
                current = VisualTreeHelper.GetParent(current);
            }

            if (parentMenuItem == null)
                return;

            var instance = parentMenuItem.Tag as Instance;

            if (instance == null)
                return;

            var stackPanel = new StackPanel
            {
                Width = 218,
                Margin = new Thickness(0, 0, 0, -40),
                HorizontalAlignment = HorizontalAlignment.Left,
            };


            stackPanel.Children.Add(new TextBlock
            {
                Text = $"Name:",
                Margin = new Thickness(1, 10, 1, 5)
            });
            stackPanel.Children.Add(new TextBox
            {
                Name = "newName",
                Text = instance.Name,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10)
            });

            stackPanel.Children.Add(new TextBlock
            {
                Text = $"Address:",
                Margin = new Thickness(1, 10, 1, 5)
            });
            stackPanel.Children.Add(new TextBox
            {
                Name = "newAddress",
                Text = instance.Address,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10)
            });


            if ((bool)instance.isV6)
            {
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Password:",
                    Margin = new Thickness(1, 10, 1, 5)
                });

                stackPanel.Children.Add(new PasswordBox
                {
                    Name = "newPassword",
                    Text = instance.Password,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }
            else
            {
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "API Key:",
                    Margin = new Thickness(1, 10, 1, 5)
                });

                stackPanel.Children.Add(new TextBox
                {
                    Name = "newApikey",
                    Text = instance.API_KEY,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }
            stackPanel.Children.Add(new ToggleSwitch
            {
                Name = "newIsDefault",
                IsChecked = instance.IsDefault,
                OnContent = "It is the default.",
                OffContent = "It is not the default.",
                Margin = new Thickness(1, 10, 1, 5)
            });

            var contentDialog = new ContentDialog(RootContentDialogPresenter)
            {
                Title = $"Editing {instance.Name}",
                Content = stackPanel,
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Delete",
                SecondaryButtonAppearance = ControlAppearance.Danger,
                CloseButtonText = "Cancel"
            };
            editingInstance = true;
            ContentGrid.Effect = new BlurEffect
            {
                Radius = 30,
                RenderingBias = RenderingBias.Quality,
                KernelType = KernelType.Gaussian
            };
            ContentDialogResult result = await contentDialog.ShowAsync();

            switch (result)
            {
                case ContentDialogResult.Primary:
                    foreach (var i in storage.Instances)
                    {
                        if (instance.Name == i.Name && instance.Address == i.Address)
                        {
                            string oldKeyName = i.Name;
                            i.Name = stackPanel.Children.OfType<TextBox>().FirstOrDefault(tb => tb.Name == "newName")!.Text;
                            i.Address = stackPanel.Children.OfType<TextBox>().FirstOrDefault(tb => tb.Name == "newAddress")!.Text;
                            i.IsDefault = (bool)stackPanel.Children.OfType<ToggleSwitch>().FirstOrDefault(ts => ts.Name == "newIsDefault")!.IsChecked!;

                            if ((bool)instance.isV6) i.Password = stackPanel.Children.OfType<PasswordBox>().FirstOrDefault(pb => pb.Name == "newPassword")!.Password;
                            else i.API_KEY = stackPanel.Children.OfType<TextBox>().FirstOrDefault(tb => tb.Name == "newApikey")!.Text;


                            if ((bool)i.IsDefault)
                            {
                                Debug.WriteLine("NEW DEFAULT SRT");
                                foreach (var ins in storage.Instances)
                                {
                                    if (ins.IsDefault == true && ins.Name != i.Name)
                                    {
                                        Debug.WriteLine($"Setting default to false in: {ins.Name}");
                                        ins.IsDefault = false;
                                        storage.WriteInstanceToKey(ins);
                                    }
                                }
                            }
                            storage.WroteOverInstanceToKey(i, oldKeyName);
                            break;
                        }

                    }
                    break;
                case ContentDialogResult.Secondary:
                    var confirmationDialog = new ContentDialog(RootContentDialogPresenter)
                    {
                        Title = "Confirm Deletion",
                        Content = "Are you sure you want to delete this instance?",
                        PrimaryButtonText = "Yes",
                        PrimaryButtonAppearance = ControlAppearance.Danger,
                        CloseButtonText = "No",
                    };
                    ContentDialogResult confirmationResult = await confirmationDialog.ShowAsync();

                    if (confirmationResult == ContentDialogResult.Primary)
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(@$"SOFTWARE\Pihole_Tray\Instances\{instance.Name}", throwOnMissingSubKey: false);
                        storage.Instances.RemoveAll(storedInstance => storedInstance.Name == instance.Name);
                    }
                    break;
            }
            ContentGrid.Effect = null;
            editingInstance = false;

        }

   

        private void Instance_MenuItem_MouseEnter(object sender, RoutedEventArgs e) {
            if (sender is MenuItem menuItem && menuItem.Header is Grid grid)
            {
                var button = grid.Children.OfType<Button>().FirstOrDefault();
                if (button != null)
                {
                    button.Visibility = Visibility.Visible; 
                }
            }
        }
        private void Instance_MenuItem_MouseLeave(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Header is Grid grid)
            {
                var button = grid.Children.OfType<Button>().FirstOrDefault();
                if (button != null)
                {
                    button.Visibility = Visibility.Hidden;
                }
            }
        }
        private void InstanceSelected_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is Instance instance)
            {
                cancelToken?.Cancel();
                ClearElements();

                cancelToken = new CancellationTokenSource();

                selectedInstance = instance;

                UpdateInfo(selectedInstance, cancelToken.Token);
            }
        }

        private void Addbutton_Click(object sender, RoutedEventArgs e)
        {
            LoginBTN.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Visible;
            Info_StackPanel.Visibility = Visibility.Hidden;
            Default_StackPanel.Visibility = Visibility.Visible;
            LoginV5.Visibility = Visibility.Collapsed;
            LoginV6.Visibility = Visibility.Collapsed;
            ApiSaveBTN.Visibility = Visibility.Collapsed;
            stopUpdatingInfo = true;
            if (storage.Instances.Count != 0)
            {
                setDefaultTS.Visibility = Visibility.Visible;
            }
            if (cancelToken != null)
            {
                cancelToken.Cancel();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) 
        {
            stopUpdatingInfo = false;
            if (cancelToken != null)
            {
                cancelToken.Cancel();
            }
            cancelToken = new CancellationTokenSource();
            // ClearElements();
            UpdateInfo(selectedInstance, cancelToken.Token);

            Info_StackPanel.Visibility = Visibility.Visible;
            Default_StackPanel.Visibility = Visibility.Hidden;
        }

        private void StatusTB_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DisableEnableContextMenu = new ContextMenu();
            if (StatusTB.Text == "disabled")
            {
                MenuItem DisableEnableButton = new MenuItem
                {
                    Header = "Enable",
                    Foreground = isDarkTheme ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6EF563")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF26B100")),
                    FontSize = 13,
                    FontWeight = FontWeights.Normal
                };
                DisableEnableContextMenu.Width = 20;
                DisableEnableContextMenu.Items.Add(DisableEnableButton);
                DisableEnableButton.Click += DisableButton_Click;
            }
            else
            {
                MenuItem disableBlockingItem = new MenuItem
                {
                    IsEnabled = false,
                    Header = "Disable blocking for:",
                    Foreground = isDarkTheme ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black),
                    FontSize = 12,
                    FontWeight = FontWeights.Normal
                };

                DisableSlider = new Slider
                {
                    TickPlacement = TickPlacement.Both,
                    TickFrequency = 1,
                    IsSnapToTickEnabled = true
                };
                DisableSlider.ValueChanged += DisableSlider_ValueChanged;

                DisableEnableButton = new TextBlock
                {
                    Text = "15 seconds",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF6E6E")),
                    TextAlignment = TextAlignment.Center,
                    FontSize = 13,
                    Margin = new Thickness(3)
                };
                DisableEnableButton.MouseLeftButtonUp += DisableButton_Click;

                DisableEnableContextMenu.Items.Add(disableBlockingItem);
                DisableEnableContextMenu.Items.Add(DisableSlider);
                DisableEnableContextMenu.Items.Add(new Separator());
                DisableEnableContextMenu.Items.Add(DisableEnableButton);
            }

            DisableEnableContextMenu.IsOpen = true;
        }

        private async void ChangeBackground_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch ts)
            {
                MicaBG.IsChecked = false;
                AcrylicBG.IsChecked = false;
                NoneBG.IsChecked = false;

                ts.IsChecked = true;

                if (ts.Name == "MicaBG")
                {

                    this.WindowBackdropType = WindowBackdropType.Mica;
                  //  MainGrid.Background = (Brush)new BrushConverter().ConvertFrom("#0CFFFFFF");
               //    MainGrid.Background = null;
                }
                else if (ts.Name == "AcrylicBG")
                {
                    this.WindowBackdropType = WindowBackdropType.Acrylic;
                   // MainGrid.Background = (Brush)new BrushConverter().ConvertFrom("#B2101010");
                    if (!isWin11)
                    {
                        var windowHelper = new WindowInteropHelper(this);
                        var accentPolicy = new AccentPolicy
                        {
                            AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                            GradientColor = unchecked((int)0x330C0C0C)
                        };

                        var accentStructSize = Marshal.SizeOf(accentPolicy);
                        var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                        Marshal.StructureToPtr(accentPolicy, accentPtr, false);

                        var data = new WindowCompositionAttributeData
                        {
                            Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                            SizeOfData = accentStructSize,
                            Data = accentPtr
                        };

                        SetWindowCompositionAttribute(windowHelper.Handle, ref data);
                        Marshal.FreeHGlobal(accentPtr);
                    }    
                }
                else
                {
                    this.WindowBackdropType = WindowBackdropType.None;
                  //  MainGrid.Background = (Brush)new BrushConverter().ConvertFrom("#B2101010");
                }
                base.OnActivated(e);

                if (!this.IsVisible || this.Top >= SystemParameters.PrimaryScreenHeight)
                {
                    this.Show(); this.Activate();
                    this.Top = (int)SystemParameters.WorkArea.Bottom - this.Height - 12;
                }
                if (cancelToken != null)
                {
                    cancelToken.Cancel();
                }

                cancelToken = new CancellationTokenSource();
                // ClearElements();
                UpdateInfo(selectedInstance, cancelToken.Token);

                reg.WriteToRegistryRoot("Background", ts.Name.Replace("BG", ""));
            }
        }

        private void fluentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (isWin11) // Makes sure it doesn't change on Windows 10 as it crashes the program
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                var attribute = GCHandle.Alloc((uint)4, GCHandleType.Pinned);
                var result = DwmSetWindowAttribute(hwnd, (uint)33, attribute.AddrOfPinnedObject(), sizeof(uint));
                attribute.Free();
                if (result != 0)
                {
                    Debug.WriteLine("Couldn't change DWM");
                }
            }
            else if (AcrylicBG.IsChecked == true) // Windows 10 Acrylic effect
            {

                var windowHelper = new WindowInteropHelper(this);
                var accentPolicy = new AccentPolicy
                {
                    AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                    GradientColor = unchecked((int)0x330C0C0C)
                };

                var accentStructSize = Marshal.SizeOf(accentPolicy);
                var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accentPolicy, accentPtr, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };

                SetWindowCompositionAttribute(windowHelper.Handle, ref data);
                Marshal.FreeHGlobal(accentPtr);
            }
         

        }

        private void SelectOtherInstnaceBTN_Click(object sender, RoutedEventArgs e)
        {
            OtherInstanceContextMenu.Items.Clear();
            SolidColorBrush greenBrush = isDarkTheme ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6EF563")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF26B100"));
            SolidColorBrush redBrush = isDarkTheme ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFB4B4")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE63B3B"));
            SolidColorBrush orangeBrush = isDarkTheme ? new SolidColorBrush(Color.FromRgb(244, 207, 64)) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6EF563"));

            foreach (Instance instance in storage.Instances)
            {
                MenuItem menuItem = new MenuItem
                {
                    FontSize = 13,
                    FontWeight = FontWeights.Normal,
                    Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children = {
                  new TextBlock
                  {
                      ToolTip = "",
                      Text = "\u25CF",
                      FontSize = 16,
                      Foreground =  new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                      Margin = new Thickness(0, 0, 7, 2),
                      VerticalAlignment = VerticalAlignment.Center
                  },
                  new TextBlock
                  {
                      Text = instance.Name,
                      VerticalAlignment = VerticalAlignment.Center
                  }
              }
                    }
                };
                Debug.WriteLine($"instance menu added:{instance.Name}");
                menuItem.Click += InstanceSelected_Click;
                OtherInstanceContextMenu.Items.Add(menuItem);
            }

            foreach (var item in OtherInstanceContextMenu.Items)
            {
                if (item is MenuItem menuItem && menuItem.Header is StackPanel sp)
                {

                    foreach (var instance in storage.Instances)
                    {

                        if ((sp.Children[1] as TextBlock).Text == instance.Name)
                        {
                            _ = Task.Run(() =>
                            {
                                int status = instance.Status().Result;
                                Dispatcher.Invoke(() =>
                                {
                                    SolidColorBrush brush = new SolidColorBrush();
                                    switch (status)
                                    {
                                        case 0: // Enabled
                                            brush = greenBrush; // Green
                                            break;

                                        case 1: // Disabled
                                            brush = redBrush; // Red
                                            break;

                                        case 2: // Reachable but can't reach API
                                            brush = new SolidColorBrush(Color.FromRgb(64, 116, 244)); // Blue
                                            menuItem.Click -= InstanceSelected_Click;
                                            menuItem.ToolTip = "Address reachable, but API inaccessible.";
                                            menuItem.Cursor = Cursors.Help;
                                            break;

                                        case -1: // Unreachable
                                            brush = orangeBrush; // Orange-ish
                                            menuItem.ToolTip = "Address unreachable.";
                                            menuItem.Click -= InstanceSelected_Click;
                                            menuItem.Cursor = Cursors.Help;

                                            break;
                                        default:
                                            break;
                                    }

                                    (sp.Children[0] as TextBlock).Foreground = brush;

                                });

                            });

                        }
                    }
                }
            }
            OtherInstanceContextMenu.IsOpen = true;
        }
        static async Task<string> CheckUrl(HttpClient client, string url)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                return response.IsSuccessStatusCode ? url : null;
            }
            catch
            {
                return null;
            }
        }
        private async void AddressTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            Brush temp = AddressTB.BorderBrush;
            if (AddressTB.Text.EndsWith("/api.php") && (AddressTB.Text.StartsWith("https://") | AddressTB.Text.StartsWith("http://")))
            {
                AddressTB.BorderBrush = AddressBrush;
                LoginV6.Visibility = Visibility.Collapsed;
                LoginV5.Visibility = Visibility.Visible;
                ApiSaveBTN.Content = "Save and use API key";
                ApiSaveBTN.Visibility = Visibility.Visible;
                AddressLB.Text = "V5 API address:";

                NavHyperlinkButton.NavigateUri = AddressTB.Text.Replace("/api.php", "/settings.php?tab=api");
                NavHyperlinkButton.Content = string.IsNullOrEmpty(NavHyperlinkButton.NavigateUri) ? "API url not available" : "Link to get the API key";
                NavHyperlinkButton.Visibility = Visibility.Visible;
            }

            else if (AddressTB.Text.EndsWith("/api") && !AddressTB.Text.EndsWith("admin/api") && (AddressTB.Text.StartsWith("https://") | AddressTB.Text.StartsWith("http://")))
            {
                AddressTB.BorderBrush = AddressBrush;
                LoginV5.Visibility = Visibility.Collapsed;
                LoginV6.Visibility = Visibility.Visible;
                ApiSaveBTN.Content = "Save and use password";
                NavHyperlinkButton.Visibility = Visibility.Collapsed;
                ApiSaveBTN.Visibility = Visibility.Visible;
                AddressLB.Text = "V6 API address:";

            }
            else if (!string.IsNullOrEmpty(AddressTB.Text))
            {

                AddressTB.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#B2FF5252");
                NavHyperlinkButton.Visibility = Visibility.Collapsed;
                ApiSaveBTN.Visibility = Visibility.Collapsed;
                AddressLB.Text = "API address:";
            }
            else
            {
                AddressTB.BorderBrush = AddressBrush;
            }
        }

        private async void Default_StackPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Default_StackPanel.Visibility == Visibility.Visible)
            {
                if (AddressTB.Text.EndsWith("/api.php") && (AddressTB.Text.StartsWith("https://") | AddressTB.Text.StartsWith("http://")))
                {
                    AddressTB.BorderBrush = AddressBrush;
                    LoginV6.Visibility = Visibility.Collapsed;
                    LoginV5.Visibility = Visibility.Visible;
                    ApiSaveBTN.Content = "Save and use API key";
                    ApiSaveBTN.Visibility = Visibility.Visible;
                    AddressLB.Text = "V5 API address:";
                    NavHyperlinkButton.Visibility = Visibility.Visible;
                }

                else if (AddressTB.Text.EndsWith("/api") && !AddressTB.Text.EndsWith("admin/api") && (AddressTB.Text.StartsWith("https://") | AddressTB.Text.StartsWith("http://")))
                {
                    AddressTB.BorderBrush = AddressBrush;
                    LoginV5.Visibility = Visibility.Collapsed;
                    LoginV6.Visibility = Visibility.Visible;
                    ApiSaveBTN.Content = "Save and use password";
                    NavHyperlinkButton.Visibility = Visibility.Collapsed;
                    ApiSaveBTN.Visibility = Visibility.Visible;
                    AddressLB.Text = "V6 API address:";
                }
                else
                {
                    AddressTB.BorderBrush = AddressBrush;
                }
            }
        }
        private async void Update()
        {
            await Updater.CheckUpdateAsync(url);
        }
        private void Update_Button_Click(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void visitGitHubM_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessStartInfo sInfo = new ProcessStartInfo($"https://github.com/PinchToDebug/Pihole-Tray") { UseShellExecute = true };
                _ = Process.Start(sInfo);
            }
            catch
            {
            }
        }

        private void InstanceSettings_Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(settingsWindow);
            if (settingsWindow == null)
            {
                settingsWindow = new SettingsWindow(storage);
                settingsWindow.Show();
            }
            else
            {
                settingsWindow.Close();
                settingsWindow = new SettingsWindow(storage);
                settingsWindow.Show();
            }
        }


    }
}