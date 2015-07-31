using System;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Application = System.Windows.Application;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace FloatingClock
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        private System.Windows.Forms.NotifyIcon notifyIcon = null;

        private DispatcherTimer refreshDispatcher;

        public MainWindow()
        {
            InitializeComponent();
            Refresh("", new EventArgs());
            ShowClock();
            InitializeDispatcher();
            WaitToFullMinuteAndRefresh();
            new HotKey(Key.C, KeyModifier.Alt, key => ShowClock());
            TrayIcon();
        }

        private void TrayIcon()
        {
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Click += new EventHandler(notifyIcon_Click);
            notifyIcon.Icon = new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/clock.ico")).Stream);
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(5, "Hello " + Environment.UserName, "Press Alt+C to show Clock\nRight Click on Tray to Close", ToolTipIcon.Info);
        }
        void notifyIcon_Click(object sender, EventArgs e)
        {
            if ((e as MouseEventArgs).Button == MouseButtons.Right)
                Close();
        }
        //private void InitializeBattery()
        //{

        //   if (SystemInformation.PowerStatus.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery)
        //{
        //    string path = "/Assets/";
        //    if (SystemInformation.PowerStatus.BatteryChargeStatus == BatteryChargeStatus.Charging)
        //    {
        //        path += "Charging/";
        //        path += (SystemInformation.PowerStatus.BatteryLifePercent * 100) / 10 + ".png";

        //    }
        //    else
        //    {
        //        path += "Normal/";
        //        path += (SystemInformation.PowerStatus.BatteryLifePercent * 100) / 10 + ".png";

        //    }
        //    BatteryImage.Source = new BitmapImage(new Uri("pack://application:,,,/FloatingClock;component" + path, UriKind.Absolute));

        //    Battery.Text = (SystemInformation.PowerStatus.BatteryLifePercent * 100).ToString();
        //}


        //}

        private void ShowClock()
        {
            SetPosition();
            Refresh("", new EventArgs());
            InitializeAnimationIn();

            WaitToFullMinuteAndRefresh();
        }

        private void InitializeAnimationIn()
        {
            Application.Current.MainWindow.Activate();
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += OpacityFadeIn;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 5);
            Application.Current.MainWindow.Visibility = Visibility.Visible;

            dispatcherTimer.Start();
        }

        private void SetPosition()
        {
            //var mousePosition = Control.MousePosition;
            var activeScreen = Screen.FromPoint(Control.MousePosition);
            //var screenHeight = activeScreen.Bounds.Height + activeScreen.Bounds.Y;
            Application.Current.MainWindow.Top = (activeScreen.Bounds.Height + activeScreen.Bounds.Y) - 140 - 48;
            Application.Current.MainWindow.Left = activeScreen.Bounds.X + 50;
        }

        private void InitializeClock()
        {
            var timeNow = DateTime.Now;
            Hours.Text = timeNow.ToString("HH");
            Minutes.Text = timeNow.ToString("mm");
            DayOfTheWeek.Text = timeNow.ToString("dddd");
            DayOfTheMonth.Text = timeNow.ToString("dd") + " " + timeNow.ToString("MMMM");
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += OpacityFadeOut;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 15);

            dispatcherTimer.Start();
            refreshDispatcher.Stop();
        }

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

        private static void OpacityFadeIn(object sender, EventArgs e)
        {
            if (Application.Current.MainWindow.Opacity < 0.95)
                Application.Current.MainWindow.Opacity += 0.05;
            else
                ((DispatcherTimer)sender).Stop();
        }

        private async void WaitToFullMinuteAndRefresh()
        {
            await Task.Delay((60 - DateTime.Now.Second) * 1000);
            Refresh("", new EventArgs());
            refreshDispatcher.Start();
        }

        private void InitializeDispatcher()
        {
            refreshDispatcher = new DispatcherTimer();
            refreshDispatcher.Tick += Refresh;
            refreshDispatcher.Interval = new TimeSpan(0, 1, 0);
        }

        private void Refresh(object sender, EventArgs e)
        {
            InitializeClock();
            //   InitializeBattery();
        }
    }
}