﻿//
// Based on code from Stephen Toub's MSDN blog at
// http://blogs.msdn.com/b/toub/archive/2006/05/03/589423.aspx
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SharedLibraryNG
{

    public class KeyboardHook
    {

        internal static class NativeMethods
        {
            // ReSharper disable InconsistentNaming
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool PostMessage(IntPtr hWnd, Int32 Msg, IntPtr wParam, HookKeyMsgData lParam);
            // ReSharper restore InconsistentNaming
        }

        [Flags]
        public enum ModifierKeys
        {
            None = 0x0000,
            Shift = 0x0001,
            LeftShift = 0x002,
            RightShift = 0x004,
            Control = 0x0008,
            LeftControl = 0x010,
            RightControl = 0x20,
            Alt = 0x0040,
            LeftAlt = 0x0080,
            RightAlt = 0x0100,
            Win = 0x0200,
            LeftWin = 0x0400,
            RightWin = 0x0800,
        }

        protected class KeyNotificationEntry
			: IEquatable<KeyNotificationEntry>
        {
            private IntPtr WindowHandle;
            public Int32 KeyCode;
            public ModifierKeys ModifierKeys;
            public Boolean Block;

            public KeyNotificationEntry(IntPtr h, Int32 k, ModifierKeys m, Boolean b)
            {
                WindowHandle = h;
                KeyCode = k;
                ModifierKeys = m;
                Block = b;
            }
            public bool Equals(KeyNotificationEntry obj)
            {
                return (WindowHandle == obj.WindowHandle &&
                        KeyCode == obj.KeyCode &&
                        ModifierKeys == obj.ModifierKeys &&
                        Block == obj.Block);
            }

            public IntPtr getWinHdl()
            {
                //logger.Log.InfoFormat("WindowHandle: {0}", WindowHandle.ToString());
                return WindowHandle;
            }

            public void setWinHdl(IntPtr hdl)
            {
                WindowHandle = hdl;
            }
        }

        public const string HookKeyMsgName = "HOOKKEYMSG-{56BE0940-34DA-11E1-B308-C6714824019B}";
	    private static Int32 _hookKeyMsg;
        public static Int32 HookKeyMsg
        {
            get
            {
	            if (_hookKeyMsg == 0)
	            {
					_hookKeyMsg = Win32.NativeMethods.RegisterWindowMessage(HookKeyMsgName).ToInt32();
                    if (_hookKeyMsg == 0)
                    {
                        logger.Log.WarnFormat("_hookKeyMsg == 0");
                        throw new System.InvalidOperationException("_hookKeyMsg == 0");
                    }
				}
	            return _hookKeyMsg;
            }
        }

        // this is a custom structure that will be passed to
        // the requested hWnd via a WM_APP_HOOKKEYMSG message
        [StructLayout(LayoutKind.Sequential)]
        public class HookKeyMsgData
        {
            public Int32 KeyCode;
            public ModifierKeys ModifierKeys;
            public Boolean WasBlocked;
        }

        private static int _referenceCount;
        private static IntPtr _hook;
        private static readonly Win32.LowLevelKeyboardProcDelegate LowLevelKeyboardProcStaticDelegate = LowLevelKeyboardProc;
        private static readonly List<KeyNotificationEntry> NotificationEntries = new List<KeyNotificationEntry>();
        
        public KeyboardHook()
        {
            _referenceCount++;
            SetHook();
        }

        ~KeyboardHook()
        {
            _referenceCount--;
            if (_referenceCount < 1) UnsetHook();
        }

        private static void SetHook()
        {
            if (_hook != IntPtr.Zero) return;

            var curProcess = Process.GetCurrentProcess();
            var curModule = curProcess.MainModule;

            var hook = Win32.NativeMethods.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, LowLevelKeyboardProcStaticDelegate, Win32.NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            if (hook == IntPtr.Zero)
            {
                var e = Marshal.GetLastWin32Error();
                logger.Log.WarnFormat("hook == IntPtr.Zero: {0}", e.ToString());
                throw new Win32Exception(e);
            }

            _hook = hook;
        }

        private static void UnsetHook()
        {
            if (_hook == IntPtr.Zero) return;

            Win32.NativeMethods.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }

        private static IntPtr LowLevelKeyboardProc(Int32 nCode, IntPtr wParam, Win32.KBDLLHOOKSTRUCT lParam)
        {
            var wParamInt = wParam.ToInt32();
            var result = 0;

            if (nCode == Win32.HC_ACTION)
            {
                switch (wParamInt)
                {
                    case Win32.WM_KEYDOWN:
                    case Win32.WM_SYSKEYDOWN:
                    case Win32.WM_KEYUP:
                    case Win32.WM_SYSKEYUP:
                        result = OnKey(wParamInt, lParam);
                        break;
                }
            }

            if (result != 0) return new IntPtr(result);

            return Win32.NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        private static int OnKey(Int32 msg, Win32.KBDLLHOOKSTRUCT key)
        {
            var result = 0;

            foreach (var notificationEntry in NotificationEntries)
            // If error code is null, have to ignore the exception
            // For some unknown reason, sometimes GetFocusWindows throws an exception
            // Mainly when the station is unlocked, or after an admin password is asked
                try
                {
                    IntPtr hdl = notificationEntry.getWinHdl();
                    IntPtr focusWin = GetFocusWindow();

                    /* Sometimes these values return 0 for unknown reasons...
                     * Seems to occur more frequently with Win+l to lock the system.
                     * Let's try to avoid throwing exceptions in this scenario.
                     */
                    if(focusWin.Equals(IntPtr.Zero) || hdl.Equals(IntPtr.Zero))
                    {
                        logger.Log.WarnFormat("Handles returned 0. Can't process keys: code: {0}  mod: {1}", notificationEntry.KeyCode, notificationEntry.ModifierKeys);
                        continue;
                    }

                    if (focusWin == hdl && notificationEntry.KeyCode == key.vkCode)
                    {
                        var modifierKeys = GetModifierKeyState();
                        if (!ModifierKeysMatch(notificationEntry.ModifierKeys, modifierKeys)) continue;

                        var wParam = new IntPtr(msg);
                        var lParam = new HookKeyMsgData
                        {
                            KeyCode = key.vkCode,
                            ModifierKeys = modifierKeys,
                            WasBlocked = notificationEntry.Block,
                        };

                        /*
                        logger.Log.DebugFormat("About to post message - hdl: {0}  HookKeyMsg: {1}  wParam: {2}  lParam: {3}", 
                                                hdl.ToString(), HookKeyMsg.ToString(), wParam.ToString(), lParam.ToString()
                                              );
                         */
                        if (!NativeMethods.PostMessage(hdl, HookKeyMsg, wParam, lParam))
                        {
                            var e = Marshal.GetLastWin32Error();
                            logger.Log.WarnFormat("Post Message Failed (1): {0}", e.ToString());
                            throw new Win32Exception(e);
                        }

                        if (notificationEntry.Block) result = 1;
                    }
                }
                catch (Win32Exception e)
                {
                    if (e.NativeErrorCode != 0)
                    {
                        logger.Log.WarnFormat("Post Message Failed (2): {0}", e.ToString());
                        throw;
                    }
                }

            return result;
        }

        private static IntPtr GetFocusWindow()
        {
            var guiThreadInfo = new Win32.GUITHREADINFO();
            if (!Win32.NativeMethods.GetGUIThreadInfo(0, guiThreadInfo))
            {
                var except = Marshal.GetLastWin32Error();
                logger.Log.WarnFormat("GetFocus failed: {0}", except.ToString());


                if (except != 0)
                    throw new Win32Exception(except);
                else
                    return IntPtr.Zero;
            }
			return Win32.NativeMethods.GetAncestor(guiThreadInfo.getFocus(), Win32.GA_ROOT);
        }

        protected static Dictionary<Int32, ModifierKeys> ModifierKeyTable = new Dictionary<Int32, ModifierKeys>
        {
            { Win32.VK_SHIFT, ModifierKeys.Shift },
            { Win32.VK_LSHIFT, ModifierKeys.LeftShift },
            { Win32.VK_RSHIFT, ModifierKeys.RightShift },
            { Win32.VK_CONTROL, ModifierKeys.Control },
            { Win32.VK_LCONTROL, ModifierKeys.LeftControl },
            { Win32.VK_RCONTROL, ModifierKeys.RightControl },
            { Win32.VK_MENU, ModifierKeys.Alt },
            { Win32.VK_LMENU, ModifierKeys.LeftAlt },
            { Win32.VK_RMENU, ModifierKeys.RightAlt },
            { Win32.VK_LWIN, ModifierKeys.LeftWin },
            { Win32.VK_RWIN, ModifierKeys.RightWin },
        };

        public static ModifierKeys GetModifierKeyState()
        {
            var modifierKeyState = ModifierKeys.None;

            foreach (KeyValuePair<Int32, ModifierKeys> pair in ModifierKeyTable)
            {
                if ((Win32.NativeMethods.GetAsyncKeyState(pair.Key) & Win32.KEYSTATE_PRESSED) != 0) modifierKeyState |= pair.Value;
            }

	        if ((modifierKeyState & ModifierKeys.LeftWin) != 0) modifierKeyState |= ModifierKeys.Win;
			if ((modifierKeyState & ModifierKeys.RightWin) != 0) modifierKeyState |= ModifierKeys.Win;

            return modifierKeyState;
        }

		public static Boolean ModifierKeysMatch(ModifierKeys requestedKeys, ModifierKeys pressedKeys)
		{
			if ((requestedKeys & ModifierKeys.Shift) != 0) pressedKeys &= ~(ModifierKeys.LeftShift | ModifierKeys.RightShift);
			if ((requestedKeys & ModifierKeys.Control) != 0) pressedKeys &= ~(ModifierKeys.LeftControl | ModifierKeys.RightControl);
			if ((requestedKeys & ModifierKeys.Alt) != 0) pressedKeys &= ~(ModifierKeys.LeftAlt | ModifierKeys.RightAlt);
			if ((requestedKeys & ModifierKeys.Win) != 0) pressedKeys &= ~(ModifierKeys.LeftWin | ModifierKeys.RightWin);
			return requestedKeys == pressedKeys;
		}

        public static void RequestKeyNotification(IntPtr windowHandle, Int32 keyCode, Boolean block)
        {
            RequestKeyNotification(windowHandle, keyCode, ModifierKeys.None, block);
        }

        public static void RequestKeyNotification(IntPtr windowHandle, Int32 keyCode, ModifierKeys modifierKeys = ModifierKeys.None, Boolean block = false)
        {
            var newNotificationEntry = new KeyNotificationEntry(windowHandle, keyCode, modifierKeys, block);

            foreach (var notificationEntry in NotificationEntries)
                if (notificationEntry == newNotificationEntry) return;

            NotificationEntries.Add(newNotificationEntry);
        }

		public static void CancelKeyNotification(IntPtr windowHandle, Int32 keyCode, Boolean block)
		{
			CancelKeyNotification(windowHandle, keyCode, ModifierKeys.None, block);
		}

		public static void CancelKeyNotification(IntPtr windowHandle, Int32 keyCode, ModifierKeys modifierKeys = ModifierKeys.None, Boolean block = false)
		{
			var notificationEntry = new KeyNotificationEntry(windowHandle, keyCode, modifierKeys, block);

            NotificationEntries.Remove(notificationEntry);
		}
    }
}
