using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace FloatingClock
{
    public class HotKey : IDisposable
    {
        private const int WmHotKey = 0x0312;
        private static Dictionary<int, HotKey> DictHotKeyToCalBackProc;
        private bool _disposed;
        // ******************************************************************
        public HotKey(Key k, KeyModifier keyModifiers, Action<HotKey> action, bool register = true)
        {
            Key = k;
            KeyModifiers = keyModifiers;
            Action = action;
            if (register)
            {
                Register();
            }
        }

        private Key Key { get; }
        private KeyModifier KeyModifiers { get; }
        private Action<HotKey> Action { get; }
        private int Id { get; set; }
        // ******************************************************************
        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vlc);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // ******************************************************************
        private bool Register()
        {
            var virtualKeyCode = KeyInterop.VirtualKeyFromKey(Key);
            Id = virtualKeyCode + ((int) KeyModifiers*0x10000);
            var result = RegisterHotKey(IntPtr.Zero, Id, (uint) KeyModifiers, (uint) virtualKeyCode);

            if (DictHotKeyToCalBackProc == null)
            {
                DictHotKeyToCalBackProc = new Dictionary<int, HotKey>();
                ComponentDispatcher.ThreadFilterMessage += ComponentDispatcherThreadFilterMessage;
            }

            DictHotKeyToCalBackProc.Add(Id, this);

            // Debug.Print(result + ", " + Id + ", " + virtualKeyCode);
            return result;
        }

        // ******************************************************************
        private void Unregister()
        {
            HotKey hotKey;
            if (DictHotKeyToCalBackProc.TryGetValue(Id, out hotKey))
            {
                UnregisterHotKey(IntPtr.Zero, Id);
            }
        }

        // ******************************************************************
        private static void ComponentDispatcherThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            if (handled) return;
            if (msg.message != WmHotKey) return;
            HotKey hotKey;

            if (!DictHotKeyToCalBackProc.TryGetValue((int) msg.wParam, out hotKey)) return;
            hotKey.Action?.Invoke(hotKey);
            handled = true;
        }

        // ******************************************************************
        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be _disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be _disposed.
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (_disposed) return;
            // If disposing equals true, dispose all managed
            // and unmanaged resources.
            if (disposing)
            {
                // Dispose managed resources.
                Unregister();
            }

            // Note disposing has been done.
            _disposed = true;
        }
    }

    // ******************************************************************
    [Flags]
    public enum KeyModifier
    {
        None = 0x0000,
        Alt = 0x0001,
        Ctrl = 0x0002,
        NoRepeat = 0x4000,
        Shift = 0x0004,
        Win = 0x0008
    }

    // ******************************************************************
}