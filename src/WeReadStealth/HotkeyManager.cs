using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WeReadStealth
{
    internal enum AppHotkey
    {
        ToggleVisibility = 1,
        HideImmediately = 2,
        ToggleTopMost = 3,
        CycleOpacity = 4,
        ToggleTheme = 5
    }

    internal sealed class HotkeyManager : IDisposable
    {
        private readonly IntPtr _windowHandle;
        private readonly HashSet<int> _registeredIds = new HashSet<int>();

        internal HotkeyManager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
        }

        internal IReadOnlyList<AppHotkey> RegisterDefaults()
        {
            List<AppHotkey> conflicts = new List<AppHotkey>();
            Register(AppHotkey.ToggleVisibility, Keys.W, conflicts);
            Register(AppHotkey.HideImmediately, Keys.B, conflicts);
            Register(AppHotkey.ToggleTopMost, Keys.T, conflicts);
            Register(AppHotkey.CycleOpacity, Keys.O, conflicts);
            Register(AppHotkey.ToggleTheme, Keys.M, conflicts);
            return conflicts;
        }

        internal bool TryGetHotkey(Message message, out AppHotkey hotkey)
        {
            if (message.Msg == NativeMethods.WmHotkey && _registeredIds.Contains(message.WParam.ToInt32()))
            {
                hotkey = (AppHotkey)message.WParam.ToInt32();
                return true;
            }

            hotkey = default(AppHotkey);
            return false;
        }

        public void Dispose()
        {
            foreach (int id in _registeredIds)
            {
                NativeMethods.UnregisterHotKey(_windowHandle, id);
            }

            _registeredIds.Clear();
        }

        private void Register(AppHotkey hotkey, Keys key, ICollection<AppHotkey> conflicts)
        {
            int id = (int)hotkey;
            uint modifiers = NativeMethods.ModControl | NativeMethods.ModShift | NativeMethods.ModNoRepeat;

            if (NativeMethods.RegisterHotKey(_windowHandle, id, modifiers, (uint)key))
            {
                _registeredIds.Add(id);
            }
            else
            {
                conflicts.Add(hotkey);
            }
        }
    }
}
