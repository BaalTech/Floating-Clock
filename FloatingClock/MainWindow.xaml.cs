using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Application = System.Windows.Application;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace FloatingClock
{
    using System.Windows.Media;
    using Microsoft.Win32;

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow Current;
        public static bool WindowIsVisible;
        public static bool HotCornerEnabled;
        public static bool SecondsEnabled;
        public static bool HideIfFocusLost = true;
        public static bool DisableGlass = false;

        private NotifyIcon notifyIcon;
        private DispatcherTimer refreshDispatcher;
        /// <summary>
        ///     Initialize Application and Main Window
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Current = this;

            RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\BaalTech\FloatingClock");
            HotCornerEnabled = Convert.ToBoolean(registryKey.GetValue(nameof(HotCornerEnabled), Convert.ToInt32(HotCornerEnabled)));
            SecondsEnabled = Convert.ToBoolean(registryKey.GetValue(nameof(SecondsEnabled), Convert.ToInt32(SecondsEnabled)));
            HideIfFocusLost = Convert.ToBoolean(registryKey.GetValue(nameof(HideIfFocusLost), Convert.ToInt32(HideIfFocusLost)));
            DisableGlass = Convert.ToBoolean(registryKey.GetValue(nameof(DisableGlass), Convert.ToInt32(DisableGlass)));
            registryKey.Close();

            Refresh();

            if(SystemParameters.IsGlassEnabled && !DisableGlass)
                ClockWindow.Background = SystemParameters.WindowGlassBrush;

            ShowClock();
            InitializeRefreshDispatcher();

            EnableSeconds(SecondsEnabled);

            new HotKey(Key.C, KeyModifier.Alt, key => ShowClock());
            EnableHotCorner(HotCornerEnabled);

            TrayIcon();
        }

        private void EnableHotCorner(bool enable)
        {
            if (enable)
                MouseHook._hookID = MouseHook.SetHook(MouseHook._proc);
            else MouseHook.UnhookWindowsHookEx(MouseHook._hookID);
            HotCornerEnabled = enable;
        }

        private void EnableSeconds(bool enable)
        {
            SecondsEnabled = enable;
            OptionalSeconds.Visibility = enable ? Visibility.Visible : Visibility.Hidden;
            Refresh();
            InitializeRefreshDispatcher();

            if (enable)
            {
                refreshDispatcher.Start();
            }
            else
            {
                WaitToFullMinuteAndRefresh();

            }
        }

        /// <summary>
        ///     Prepare Clock to Show 
        /// </summary>
        public void ShowClock()
        {
            if (!WindowIsVisible)
            {
                SetPositionOnCurrentDisplay();
                Refresh();
                InitializeAnimationIn();
                WaitToFullMinuteAndRefresh();
            }
            else
            {
                HideWindow();
            }
        }

        /// <summary>
        ///     Load Current Data to Controls
        /// </summary>
        private void LoadCurrentClockData()
        {
            var timeNow = DateTime.Now;
            Hours.Text = timeNow.ToString("HH");
            Minutes.Text = timeNow.ToString("mm");
            Seconds.Text = timeNow.ToString("ss");
            DayOfTheWeek.Text = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase( timeNow.ToString("dddd"));
            DayOfTheMonth.Text = timeNow.ToString("dd") + " " + System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(timeNow.ToString("MMMM"));
        }

        /// <summary>
        ///     Initialize Refresh Dispatcher
        /// </summary>
        private void InitializeRefreshDispatcher()
        {
            refreshDispatcher = new DispatcherTimer();
            refreshDispatcher.Tick += Refresh;
            if(SecondsEnabled)
                refreshDispatcher.Interval = new TimeSpan(0,0,1);
            else
                refreshDispatcher.Interval = new TimeSpan(0, 1, 0);
        }

        /// <summary>
        ///     Wait to full minute refresh data and start refresh Dispatcher
        /// </summary>
        private async void WaitToFullMinuteAndRefresh()
        {
            await Task.Delay((60 - DateTime.Now.Second) * 1000);
            Refresh();
            refreshDispatcher.Start();
        }

        /// <summary>
        ///     DispatcherTimer Refresh Event
        /// </summary>
        /// <param name="sender">Dispatcher</param>
        /// <param name="e">Dispatcher Arg</param>
        private void Refresh(object sender = null, EventArgs e = null)
        {
            LoadCurrentClockData();
        }

        /// <summary>
        ///     Set position on current Display 
        /// </summary>
        private void SetPositionOnCurrentDisplay()
        {
            var activeScreen = Screen.FromPoint(Control.MousePosition);
            Application.Current.MainWindow.Top = (activeScreen.Bounds.Height + activeScreen.Bounds.Y) - 140 - 48;
            Application.Current.MainWindow.Left = activeScreen.Bounds.X + 50;
        }

        /// <summary>
        ///     Initialize Tray Icon and BaloonTip
        /// </summary>
        private void TrayIcon()
        {
            notifyIcon = new NotifyIcon();
            //     notifyIcon.Click += NotifyIcon_Click;
            ContextMenu m_menu;

            m_menu = new ContextMenu();
            var activeHotCornerItem = new MenuItem("Activate HotCorner", ChangeHotCornerActiveState);
            activeHotCornerItem.Checked = HotCornerEnabled;
            var activeSecondsItem = new MenuItem("Enable Seconds", ChangeSecondsState);
            activeSecondsItem.Checked = SecondsEnabled;
            var hideIfFocusLostItem = new MenuItem("Enable Hiding if focus lost", ChangeHideIfFocusLostState);
            hideIfFocusLostItem.Checked = MainWindow.HideIfFocusLost;
            var disableGlassItem = new MenuItem("DisableGlass", ChangeDisableGlassState);
            disableGlassItem.Checked = MainWindow.HideIfFocusLost;
            var optionsItem = new MenuItem("Options", OpenOptionWindow);
            optionsItem.Enabled = false;
            var exitItem = new MenuItem("Exit", CloseWindow);
            m_menu.MenuItems.Add(0, (activeHotCornerItem));
            m_menu.MenuItems.Add(1, (activeSecondsItem));
            m_menu.MenuItems.Add(2, (hideIfFocusLostItem));
            m_menu.MenuItems.Add(3, (optionsItem));
            m_menu.MenuItems.Add(4, (exitItem));
            notifyIcon.ContextMenu = m_menu;

            var streamResourceInfo = Application.GetResourceStream(new Uri("pack://application:,,,/clock.ico"));
            if (streamResourceInfo != null)
                notifyIcon.Icon = new Icon(streamResourceInfo.Stream);
            
            notifyIcon.Visible = true;
            
            notifyIcon.ShowBalloonTip(5, "Hello " + Environment.UserName,
                "Press Alt+C to show Clock\nRight Click on Tray to Close", ToolTipIcon.Info);
        }

        private void ChangeDisableGlassState(object sender, EventArgs e)
        {
            DisableGlass = !DisableGlass;
            
            if (SystemParameters.IsGlassEnabled && !DisableGlass)
                ClockWindow.Background = SystemParameters.WindowGlassBrush;
            else
                ClockWindow.Background =new SolidColorBrush(Color.FromArgb(255,17,17,17)); 
            

            (sender as MenuItem).Checked = DisableGlass;
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\BaalTech\FloatingClock", true);
            registryKey.SetValue(nameof(DisableGlass), Convert.ToInt32(DisableGlass));
            registryKey.Close();
        }

        private void ChangeSecondsState(object sender, EventArgs e)
        {
            EnableSeconds(!SecondsEnabled);
            (sender as MenuItem).Checked = SecondsEnabled;
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\BaalTech\FloatingClock", true);
            registryKey.SetValue(nameof(SecondsEnabled), Convert.ToInt32(SecondsEnabled));
            registryKey.Close();
        }


        private void ChangeHideIfFocusLostState(object sender, EventArgs e)
        {
            HideIfFocusLost = !HideIfFocusLost;
            (sender as MenuItem).Checked = HideIfFocusLost;
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\BaalTech\FloatingClock", true);
            registryKey.SetValue(nameof(HideIfFocusLost), Convert.ToInt32(HideIfFocusLost));
            registryKey.Close();
        }

        private void OpenOptionWindow(object sender, EventArgs e)
        {

        }

        private void ChangeHotCornerActiveState(object sender, EventArgs e)
        {
            EnableHotCorner(!HotCornerEnabled);
            (sender as MenuItem).Checked =HotCornerEnabled;
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\BaalTech\FloatingClock",true);
            registryKey.SetValue(nameof(HotCornerEnabled), Convert.ToInt32(HotCornerEnabled));
            registryKey.Close();

        }

        private void CloseWindow(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        ///     Closing app after Right Click
        /// </summary>
        /// <param name="sender">NotifyIcon Click Event</param>
        /// <param name="e">MouseEventArg (Left Right Mouse button)</param>
        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            var mouseEventArgs = e as MouseEventArgs;
            if (mouseEventArgs != null && mouseEventArgs.Button == MouseButtons.Right)
                Close();
        }

        /// <summary>
        ///     Start Animation FadeIN
        /// </summary>
        private void InitializeAnimationIn()
        {
            Application.Current.MainWindow.Activate();
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += OpacityFadeIn;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 5);
            Application.Current.MainWindow.Visibility = Visibility.Visible;

            WindowIsVisible = true;
            dispatcherTimer.Start();

        }

        /// <summary>
        ///     Animation Fade In Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OpacityFadeIn(object sender, EventArgs e)
        {
            if (Application.Current.MainWindow.Opacity < 0.95)
                Application.Current.MainWindow.Opacity += 0.05;
            else
                ((DispatcherTimer)sender).Stop();
        }

        /// <summary>
        ///     Call HideWindow if Window Deactivated
        /// </summary>
        private void Window_Deactivated(object sender, EventArgs e)
        {
            if(HideIfFocusLost)
                HideWindow();
        }
        /// <summary>
        /// Start Fade out Animation and stop time Dispatchers
        /// </summary>
        private void HideWindow()
        {
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += OpacityFadeOut;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 15);

            dispatcherTimer.Start();
            WindowIsVisible = false;
            refreshDispatcher.Stop();

        }

        /// <summary>
        ///     Animation Fade Out Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OpacityFadeOut(object sender, EventArgs e)
        {
            if (Application.Current.MainWindow.Opacity > 0)
                Application.Current.MainWindow.Opacity -= 0.1;
            else
            {
                ((DispatcherTimer)sender).Stop();
                Application.Current.MainWindow.Visibility = Visibility.Collapsed;
            }
        }

    }
}