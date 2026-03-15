using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LIveSubs.utils
{
    /// <summary>
    /// Manages global keyboard shortcuts using Win32 RegisterHotKey API.
    /// Hotkeys work even when the application is not focused.
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(nint hWnd, int id);

        private const int WM_HOTKEY = 0x0312;

        // Modifier keys
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        // Virtual key codes
        public const uint VK_T = 0x54;  // T key
        public const uint VK_O = 0x4F;  // O key
        public const uint VK_C = 0x43;  // C key
        public const uint VK_P = 0x50;  // P key

        // Hotkey IDs
        public const int HOTKEY_TOGGLE_PAUSE = 9001;
        public const int HOTKEY_TOGGLE_OVERLAY = 9002;
        public const int HOTKEY_COPY_TRANSLATION = 9003;

        private nint _windowHandle;
        private HwndSource? _source;
        private readonly Dictionary<int, Action> _hotkeyActions = new();
        private bool _disposed = false;

        public event Action<string>? HotkeyTriggered;

        public void Initialize(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _windowHandle = helper.Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source?.AddHook(HwndHook);
        }

        public bool RegisterGlobalHotkey(int id, uint modifiers, uint key, Action action)
        {
            if (_windowHandle == nint.Zero)
                return false;

            bool result = RegisterHotKey(_windowHandle, id, modifiers | MOD_NOREPEAT, key);
            if (result)
                _hotkeyActions[id] = action;
            return result;
        }

        public void UnregisterGlobalHotkey(int id)
        {
            if (_windowHandle != nint.Zero)
            {
                UnregisterHotKey(_windowHandle, id);
                _hotkeyActions.Remove(id);
            }
        }

        private nint HwndHook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_hotkeyActions.TryGetValue(id, out var action))
                {
                    action.Invoke();
                    handled = true;
                }
            }
            return nint.Zero;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var id in _hotkeyActions.Keys.ToList())
                    UnregisterGlobalHotkey(id);
                _source?.RemoveHook(HwndHook);
                _disposed = true;
            }
        }
    }
}
