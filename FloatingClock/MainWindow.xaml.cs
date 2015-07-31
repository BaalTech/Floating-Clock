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
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private NotifyIcon notifyIcon;
        private DispatcherTimer refreshDispatcher;

        /// <summary>
        /// Initialize Application and Main Window
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Refresh();
            ShowClock();
            InitializeRefreshDispatcher();
            WaitToFullMinuteAndRefresh();
            new HotKey(Key.C, KeyModifier.Alt, key => ShowClock());
            TrayIcon();
        }

        /// <summary>
        /// Prepare Clock to Show 
        /// </summary>
        private void ShowClock()
        {
            SetPositionOnCurrentDisplay();
            Refresh();
            InitializeAnimationIn();
            WaitToFullMinuteAndRefresh();
        }

        /// <summary>
        /// Load Current Data to Controls
        /// </summary>
        private void LoadCurrentClockData()
        {
            var timeNow = DateTime.Now;
            Hours.Text = timeNow.ToString("HH");
            Minutes.Text = timeNow.ToString("mm");
            DayOfTheWeek.Text = timeNow.ToString("dddd");
            DayOfTheMonth.Text = timeNow.ToString("dd") + " " + timeNow.ToString("MMMM");
        }

        /// <summary>
        /// Initialize Refresh Dispatcher
        /// </summary>
        private void InitializeRefreshDispatcher()
        {
            refreshDispatcher = new DispatcherTimer();
            refreshDispatcher.Tick += Refresh;
            refreshDispatcher.Interval = new TimeSpan(0, 1, 0);
        }

        /// <summary>
        /// Wait to full minute refresh data and start refresh Dispatcher
        /// </summary>
        private async void WaitToFullMinuteAndRefresh()
        {
            await Task.Delay((60 - DateTime.Now.Second) * 1000);
            Refresh();
            refreshDispatcher.Start();
        }

        /// <summary>
        /// DispatcherTimer Refresh Event
        /// </summary>
        /// <param name="sender">Dispatcher</param>
        /// <param name="e">Dispatcher Arg</param>
        private void Refresh(object sender = null, EventArgs e = null)
        {
            LoadCurrentClockData();
        }
        /// <summary>
        /// Set position on current Display 
        /// </summary>
        private void SetPositionOnCurrentDisplay()
        {
            var activeScreen = Screen.FromPoint(Control.MousePosition);
            Application.Current.MainWindow.Top = (activeScreen.Bounds.Height + activeScreen.Bounds.Y) - 140 - 48;
            Application.Current.MainWindow.Left = activeScreen.Bounds.X + 50;
        }

        /// <summary>
        /// Initialize Tray Icon and BaloonTip
        /// </summary>
        private void TrayIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Click += notifyIcon_Click;
            var streamResourceInfo = Application.GetResourceStream(new Uri("pack://application:,,,/clock.ico"));
            if (streamResourceInfo != null)
                notifyIcon.Icon = new Icon(streamResourceInfo.Stream);
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(5, "Hello " + Environment.UserName,
                "Press Alt+C to show Clock\nRight Click on Tray to Close", ToolTipIcon.Info);
        }
        /// <summary>
        /// Closing app after Right Click
        /// </summary>
        /// <param name="sender">NotifyIcon Click Event</param>
        /// <param name="e">MouseEventArg (Left Right Mouse button)</param>
        private void notifyIcon_Click(object sender, EventArgs e)
        {
            var mouseEventArgs = e as MouseEventArgs;
            if (mouseEventArgs != null && mouseEventArgs.Button == MouseButtons.Right)
                Close();
        }

        /// <summary>
        /// Start Animation FadeIN
        /// </summary>
        private void InitializeAnimationIn()
        {
            Application.Current.MainWindow.Activate();
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += OpacityFadeIn;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 5);
            Application.Current.MainWindow.Visibility = Visibility.Visible;

            dispatcherTimer.Start();
        }

        /// <summary>
        /// Animation Fade In Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OpacityFadeIn(object sender, EventArgs e)
        {
            if (Application.Current.MainWindow.Opacity < 0.95)
                Application.Current.MainWindow.Opacity += 0.05;
            else
                ((DispatcherTimer) sender).Stop();
        }

        /// <summary>
        /// Start Animation FadeOut Event
        /// </summary>
        private void Window_Deactivated(object sender, EventArgs e)
        {
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += OpacityFadeOut;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 15);

            dispatcherTimer.Start();
            refreshDispatcher.Stop();
        }

        /// <summary>
        /// Animation Fade Out Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OpacityFadeOut(object sender, EventArgs e)
        {
            if (Application.Current.MainWindow.Opacity > 0)
                Application.Current.MainWindow.Opacity -= 0.1;
            else
            {
                ((DispatcherTimer) sender).Stop();
                Application.Current.MainWindow.Visibility = Visibility.Collapsed;
            }
        }
    }
}