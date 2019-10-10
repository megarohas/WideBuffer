﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Interop;
using System.IO;
using WindowsInput;
using WindowsInput.Native;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
namespace WideBuffer
{
    class GlobalKeyboardHook
    {
        #region Constant, Structure and Delegate Definitions
        public delegate int keyboardHookProc(int code, int wParam, ref keyboardHookStruct lParam);
        private keyboardHookProc _keyboardHookProc;

        public struct keyboardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;
        const int WM_SYSKEYDOWN = 0x104;
        const int WM_SYSKEYUP = 0x105;
        #endregion

        #region Instance Variables
        public List<Keys> HookedKeys = new List<Keys>();
        IntPtr hhook = IntPtr.Zero;
        #endregion

        #region Events
        public event KeyEventHandler KeyDown;
        public event KeyEventHandler KeyUp;
        #endregion

        #region Constructors and Destructors
        public GlobalKeyboardHook()
        {
            hook();
        }

        ~GlobalKeyboardHook()
        {
            unhook();
        }
        #endregion

        #region Public Methods
        public void hook()
        {
            IntPtr hInstance = LoadLibrary("User32");
            _keyboardHookProc = new keyboardHookProc(hookProc);
            hhook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardHookProc, hInstance, 0);
        }

        public void unhook()
        {
            UnhookWindowsHookEx(hhook);
        }

        public int hookProc(int code, int wParam, ref keyboardHookStruct lParam)
        {
            if (code >= 0)
            {
                Keys key = (Keys)lParam.vkCode;
                if (HookedKeys.Contains(key))
                {
                    KeyEventArgs kea = new KeyEventArgs(key);
                    if ((wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) && (KeyDown != null))
                    {
                        KeyDown(this, kea);
                    }
                    else if ((wParam == WM_KEYUP || wParam == WM_SYSKEYUP) && (KeyUp != null))
                    {
                        KeyUp(this, kea);
                    }
                    if (kea.Handled)
                        return 1;
                }
            }
            return CallNextHookEx(hhook, code, wParam, ref lParam);
        }
        #endregion

        #region DLL imports
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, keyboardHookProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref keyboardHookStruct lParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);
        #endregion
    }
}
