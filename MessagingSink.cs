﻿using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Graphics.Gdi;
using System.ComponentModel;

namespace Peep
{
    internal class MessagingSink : IDisposable
    {
        private bool disposedValue;

        private WNDPROC? messageHandler;
        public string WindowId { get; private set; } = null!;
        public HWND MessagingSinkHwnd { get; private set; }

        public MessagingSink()
        {
            CreateMessageWindow();
        }

        private void CreateMessageWindow()
        {
            WindowId = "PeepTaskbarIcon_" + Guid.NewGuid();
            messageHandler = OnWindowMessageReceived;

            WNDCLASSW wc = new();
            unsafe
            {
                fixed (char* windowIdLocal = WindowId)
                {
                    {
                        wc.style = 0;
                        wc.lpfnWndProc = messageHandler;
                        wc.cbClsExtra = 0;
                        wc.hInstance = new HINSTANCE(IntPtr.Zero);
                        wc.hIcon = new HICON(IntPtr.Zero);
                        wc.hCursor = new HCURSOR(IntPtr.Zero);
                        wc.hbrBackground = new HBRUSH(IntPtr.Zero);
                        wc.lpszMenuName = new PCWSTR();
                        wc.lpszClassName = windowIdLocal;
                    }
                    ;
                }
            }

            ushort classAtom = PInvoke.RegisterClass(wc);

            HWND messageSinkHwnd;
            unsafe
            {
                fixed (char* emptyString = "")
                fixed (char* windowIdLocal = WindowId)
                {
                    messageSinkHwnd = PInvoke.CreateWindowEx(
                        WINDOW_EX_STYLE.WS_EX_RIGHTSCROLLBAR,
                        new PCWSTR(windowIdLocal),
                        new PCWSTR(emptyString),
                        WINDOW_STYLE.WS_OVERLAPPED,
                        0,
                        0,
                        1,
                        1,
                        new HWND(IntPtr.Zero),
                        new HMENU(IntPtr.Zero),
                        new HINSTANCE(IntPtr.Zero),
                        IntPtr.Zero.ToPointer()
                    );
                }
            }

            if (messageSinkHwnd.Value == IntPtr.Zero)
            {
                throw new Win32Exception("The Message Sink window's HWND was invalid.");
            }

            MessagingSinkHwnd = messageSinkHwnd;
        }

        private LRESULT OnWindowMessageReceived(HWND msg, uint msgId, WPARAM wParam, LPARAM lParam)
        {
            ProcessWindowMessage(msgId, wParam, lParam);
            return PInvoke.DefWindowProc(msg, msgId, wParam, lParam);
        }

        private void ProcessWindowMessage(uint msg, WPARAM wParam, LPARAM lParam)
        {
            // 0x0312 = WM_HOTKEY
            if (msg == 0x0312)
            {
                ((App)App.Current).HotkeyTriggered();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
            {
                return;
            }

            disposedValue = true;

            PInvoke.DestroyWindow(MessagingSinkHwnd);
            messageHandler = null;
        }

        ~MessagingSink()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
