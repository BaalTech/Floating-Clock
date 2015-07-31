using System.Windows;

namespace FloatingClock
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Unhook Mouse On Application Exit
        /// </summary>
        private void App_OnExit(object sender, ExitEventArgs e)
        {
            MouseHook.UnhookWindowsHookEx(MouseHook._hookID);

        }
    }
}