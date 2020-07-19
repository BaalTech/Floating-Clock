using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FloatingClock
{
    public static class MouseHook
    {


        /// <summary>
        ///  The _proc type defines a pointer to this callback function.
        /// </summary>
        public static readonly LowLevelMouseProc _proc = HookCallback;

        /// <summary>
        /// Pointer Hook ID
        /// </summary>
        public static IntPtr _hookID = IntPtr.Zero;

        /// <summary>
        /// Bool with information about state of corner
        /// </summary>
        private static bool cornerIsActive;

        /// <summary>
        /// If Corner is Active Wait for 2 sec and Disable it
        /// </summary>
        private static async void DisableCorner()
        {
            if (!cornerIsActive) return;
            await Task.Delay(2 * 1000);
            cornerIsActive = false;
        }

        /// <summary>
        /// Get Current Process and Module and Installs an application-defined hook procedure into a hook chain. You would install a hook procedure to monitor the system for certain types of events. These events are associated either with a specific thread or with all threads in the same desktop as the calling thread
        /// </summary>
        /// <param name="proc">HookCallback</param>
        /// <returns></returns>
        public static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);



        /// <summary>
        /// Low Level Mouse Proc - callback function used with the SetWindowsHookEx function. The system calls this function every time a new mouse input event is about to be posted into a thread input queue. 
        /// </summary>
        /// <param name="nCode">A code the hook procedure uses to determine how to process the message. If nCode is less than zero, the hook procedure must pass the message to the CallNextHookEx function without further processing and should return the value returned by CallNextHookEx. This parameter can be one of the following values. </param>
        /// <param name="wParam">The identifier of the mouse message. This parameter can be one of the following messages: WM_LBUTTONDOWN, WM_LBUTTONUP, WM_MOUSEMOVE, WM_MOUSEWHEEL, WM_MOUSEHWHEEL, WM_RBUTTONDOWN, or WM_RBUTTONUP. </param>
        /// <param name="lParam">A pointer to an MSLLHOOKSTRUCT structure. </param>
        /// <returns></returns>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (MainWindow.WindowIsVisible || nCode < 0) return CallNextHookEx(_hookID, nCode, wParam, lParam);
            var hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
            var activeScreen = Screen.FromPoint(Control.MousePosition);
            if (hookStruct.pt.x >= activeScreen.Bounds.X + activeScreen.Bounds.Width - 25)
            {
                if (
                    (hookStruct.pt.y <= activeScreen.Bounds.Y + 25)
                    ||
                    (hookStruct.pt.y >= activeScreen.Bounds.Y + activeScreen.Bounds.Height - 25)
                    )
                {
                    cornerIsActive = true;
                }
                else
                {
                    DisableCorner();
                }
                if (!cornerIsActive || (hookStruct.pt.y < activeScreen.Bounds.Y + (activeScreen.Bounds.Height / 5)) ||
                    (hookStruct.pt.y >
                     activeScreen.Bounds.Y + activeScreen.Bounds.Height - (activeScreen.Bounds.Height / 5)))
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                MainWindow.Current.ShowClock();
                cornerIsActive = false;
            }
            else
            {
                DisableCorner();
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }


        /// <summary>
        /// The WH_MOUSE_LL (id 14) hook enables you to monitor mouse input events about to be posted in a thread input queue. 
        /// </summary>
        private const int WH_MOUSE_LL = 14;

        /// <summary>
        /// Type of MouseEvent
        /// </summary>
        public enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }


        /// <summary>
        /// The POINT structure defines the x- and y- coordinates of a point.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }


        /// <summary>
        /// Contains information about a low-level mouse input event. 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            private uint mouseData;
            private uint flags;
            private uint time;
            private IntPtr dwExtraInfo;
        }


        /// <summary>
        /// Installs an application-defined hook procedure into a hook chain. You would install a hook procedure to monitor the system for certain types of events. These events are associated either with a specific thread or with all threads in the same desktop as the calling thread. 
        /// </summary>
        /// <param name="idHook">The type of hook procedure to be installed. This parameter can be one of the following values. </param>
        /// <param name="lpfn">A pointer to the hook procedure. If the dwThreadId parameter is zero or specifies the identifier of a thread created by a different process, the lpfn parameter must point to a hook procedure in a DLL. Otherwise, lpfn can point to a hook procedure in the code associated with the current process. </param>
        /// <param name="hMod">A handle to the DLL containing the hook procedure pointed to by the lpfn parameter. The hMod parameter must be set to NULL if the dwThreadId parameter specifies a thread created by the current process and if the hook procedure is within the code associated with the current process. </param>
        /// <param name="dwThreadId">The identifier of the thread with which the hook procedure is to be associated. For desktop apps, if this parameter is zero, the hook procedure is associated with all existing threads running in the same desktop as the calling thread. For Windows Store apps, see the Remarks section.</param>
        /// <returns>If the function succeeds, the return value is the handle to the hook procedure. If the function fails, the return value is NULL.To get extended error information, call GetLastError.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);



        /// <summary>
        /// Removes a hook procedure installed in a hook chain by the SetWindowsHookEx function. 
        /// </summary>
        /// <param name="hhk">A handle to the hook to be removed. This parameter is a hook handle obtained by a previous call to SetWindowsHookEx. </param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);



        /// <summary>
        /// Passes the hook information to the next hook procedure in the current hook chain. A hook procedure can call this function either before or after processing the hook information. 
        /// </summary>
        /// <param name="hhk"> This parameter is ignored. </param>
        /// <param name="nCode">The hook code passed to the current hook procedure. The next hook procedure uses this code to determine how to process the hook information.</param>
        /// <param name="wParam">The wParam value passed to the current hook procedure. The meaning of this parameter depends on the type of hook associated with the current hook chain.</param>
        /// <param name="lParam">The lParam value passed to the current hook procedure. The meaning of this parameter depends on the type of hook associated with the current hook chain.</param>
        /// <returns>This value is returned by the next hook procedure in the chain. The current hook procedure must also return this value. The meaning of the return value depends on the hook type. For more information, see the descriptions of the individual hook procedures.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);


        /// <summary>
        /// Retrieves a module handle for the specified module. The module must have been loaded by the calling process.
        /// </summary>
        /// <param name="lpModuleName">The name of the loaded module (either a .dll or .exe file). If the file name extension is omitted, the default library extension .dll is appended. The file name string can include a trailing point character (.) to indicate that the module name has no extension. The string does not have to specify a path. When specifying a path, be sure to use backslashes (\), not forward slashes (/). The name is compared (case independently) to the names of modules currently mapped into the address space of the calling process. </param>
        /// <returns>If the function succeeds, the return value is a handle to the specified module. If the function fails, the return value is NULL.To get extended error information, call GetLastError.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }


}
