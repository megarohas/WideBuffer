using System;
using System.Windows;
using System.Windows.Interop;
using System.IO;
using WindowsInput;
using WindowsInput.Native;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Generic;

namespace WideBuffer
{
    public partial class MainWindow : Window
    {
        public Keys ActivationKey;
        public Keys ControlKey;

        private bool ControlKeyState;

        private IntPtr              hWndNextViewer;
        private HwndSource          hWndSource;
        private GlobalKeyboardHook  KeyboardListener;

        public List<string> Buffer = new List<string>();
        public int          BufferMaxSize;
        public int          BufferIterator;

        public MainWindow()
        {
            InitializeComponent();

            this.WindowState    = WindowState.Minimized;
            ControlKeyState     = false;


            ActivationKey       = Keys.D;
            ControlKey          = Keys.LControlKey;
            BufferMaxSize       = 5;


            Popup.VerticalOffset    = (SystemParameters.FullPrimaryScreenHeight - Popup.Height) / 2;
            Popup.HorizontalOffset  = (SystemParameters.FullPrimaryScreenWidth - Popup.Width) / 2;

            KeyboardListener = new GlobalKeyboardHook();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.ShowInTaskbar = false;
            this.InitCBViewer();

            KeyboardListener.HookedKeys.Add(ControlKey);
            KeyboardListener.HookedKeys.Add(ActivationKey);
            KeyboardListener.KeyDown += new KeyEventHandler(KeyDownHandler);
            KeyboardListener.KeyUp += new KeyEventHandler(KeyUpHandler);
        }

        void KeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == ControlKey)
                ControlKeyState = true;


            if (ControlKeyState && e.KeyCode == ActivationKey)
            {
                if (!Popup.IsOpen)
                {
                    Popup.IsOpen = true;
                    BufferIterator = 0;
                }
                else if (BufferIterator + 1 >= Buffer.Count)
                    BufferIterator = 0;
                else 
                    BufferIterator++;
                

                Selector.Margin = new Thickness(0, 40*BufferIterator, 0, 0);
                e.Handled = true;
            }
        }

        void KeyUpHandler(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey)
            {
                if (Popup.IsOpen)
                {
                    Popup.IsOpen = false;

                    System.Windows.Clipboard.SetText(Buffer[BufferIterator]);
                    InputSimulator input = new InputSimulator();
                    input.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
                }
                ControlKeyState = false;
            }
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            this.CloseCBViewer();
        }

        private void InitCBViewer()
        {
            WindowInteropHelper wih = new WindowInteropHelper(this);
            hWndSource = HwndSource.FromHwnd(wih.Handle);

            hWndSource.AddHook(this.WinProc);  
            hWndNextViewer = Win32.SetClipboardViewer(hWndSource.Handle); 
        }

        private void CloseCBViewer()
        {
            Win32.ChangeClipboardChain(hWndSource.Handle, hWndNextViewer);

            hWndNextViewer = IntPtr.Zero;
            hWndSource.RemoveHook(this.WinProc);
        }

        private void RebuildMenu()
        {
            MenuItems.Children.Clear();
            foreach (string item in Buffer)
            {
                System.Windows.Controls.Label MenuItem = new System.Windows.Controls.Label();
                MenuItem.Height = 40;
                MenuItem.VerticalAlignment = VerticalAlignment.Center;
                MenuItem.FontSize = 15;
                MenuItem.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                MenuItem.Foreground = System.Windows.Media.Brushes.White;
                if (item.Length > 40)
                    MenuItem.Content = item.Substring(0, 40) + "...";
                else
                    MenuItem.Content = item;
                MenuItems.Children.Add(MenuItem);
            }
        }

        private void AddItem()
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                string NewValue = System.Windows.Clipboard.GetText();
                if (!Buffer.Contains(NewValue))
                {
                    Buffer.Insert(0, NewValue);
                    if (Buffer.Count > BufferMaxSize)
                        Buffer.RemoveAt(Buffer.Count - 1);
                    RebuildMenu();
                }
            }
        }

        private IntPtr WinProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case Win32.WM_CHANGECBCHAIN:
                    if (wParam == hWndNextViewer)
                        hWndNextViewer = lParam;
                    else if (hWndNextViewer != IntPtr.Zero)
                        Win32.SendMessage(hWndNextViewer, msg, wParam, lParam);
                    break;

                case Win32.WM_DRAWCLIPBOARD:
                    this.AddItem();
                    Win32.SendMessage(hWndNextViewer, msg, wParam, lParam);
                    break;
            }

            return IntPtr.Zero;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Popup.IsOpen = true;
        }
    }
}