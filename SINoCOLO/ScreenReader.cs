using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SINoCOLO
{
    public class ScreenReader
    {
        public enum EState
        {
            None,
            MissingGameProcess,
            MissingGameWindow,
            WindowTooSmall,
            Success,
        }

        public class GameInfo
        {
            public Process process;
            public string windowTitle;
            public HandleRef windowMain;
            public HandleRef windowInput;
            public HandleRef windowClient;

            public Rectangle rectMain;
            public Rectangle rectClient;

            public bool IsValid() { return (process != null) && !process.HasExited && windowMain.Handle != IntPtr.Zero; }
        }

        private List<GameInfo> availableGameInfo = new List<GameInfo>();
        private GameInfo cachedGameInfo;
        private Bitmap cachedScreenshot;
        private EState currentState;
        private Size finalSize = new Size(338, 600);
        private IntPtr selectedWindow;

        public ScreenReader()
        {
            currentState = EState.None;
        }

        public EState GetState() { return currentState; }
        public Size GetExpectedSize() { return finalSize; }
        public List<GameInfo> GetAvailableGames() { return availableGameInfo; }

        public void SetSelectedWindow(IntPtr windowHandle) { selectedWindow = windowHandle; }

        public HandleRef GetInputWindowHandle() 
        {
            return (cachedGameInfo != null) ? cachedGameInfo.windowInput : new HandleRef();
        }

        public void ConvertLocalToClient(int localX, int localY, out int clientX, out int clientY)
        {
            // scale up back to cropped size
            float scaleX = (float)cachedGameInfo.rectClient.Width / finalSize.Width;
            float scaleY = (float)cachedGameInfo.rectClient.Height / finalSize.Height;
            float tmpClientX = localX * scaleX;
            float tmpClientY = localY * scaleY;

            clientX = Math.Max(0, (int)tmpClientX);
            clientY = Math.Max(0, (int)tmpClientY);
        }

        public Bitmap DoWork()
        {
            currentState = EState.Success;

            if (cachedGameInfo == null || !cachedGameInfo.IsValid())
            {
                UpdateAvailableGameInfo();
            }

            if (cachedGameInfo != null)
            {
                if (cachedGameInfo.windowMain.Handle != IntPtr.Zero)
                {
                    if (cachedScreenshot != null) { cachedScreenshot.Dispose(); }
                    cachedScreenshot = TakeScreenshot();
                }
                else
                {
                    currentState = EState.MissingGameWindow;
                }
            }
            else
            {
                currentState = EState.MissingGameProcess;
            }

            return cachedScreenshot;
        }

        #region API

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string DeviceName, int ModeNum, ref DEVMODE lpDevMode);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner

            public override string ToString()
            {
                return string.Format("[L:{0},T:{1},R:{2},B:{3}]", Left, Top, Right, Bottom);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWINFO
        {
            public uint cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;

            public WINDOWINFO(Boolean? filler) : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
            {
                cbSize = (UInt32)(Marshal.SizeOf(typeof(WINDOWINFO)));
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);
        #endregion

        private void UpdateAvailableGameInfo()
        {
            const uint WS_VISIBLE = 0x10000000;
            const uint WS_EX_APPWINDOW = 0x00040000;

            var processes = Process.GetProcessesByName("BlueStacks");
            //Console.WriteLine("Scanning processes, check:{0}", processes.Length);

            foreach (var proc in processes)
            {
                //Console.WriteLine(">> proc:{0:x}, mainWnd:{1:x} ({2})", proc.Id, proc.MainWindowHandle, proc.MainWindowTitle);
                if (proc.MainWindowHandle.ToInt64() != 0)
                {
                    var windowHandles = new List<IntPtr>();
                    foreach (ProcessThread thread in proc.Threads)
                    {
                        EnumThreadWindows(thread.Id, (hWnd, lParam) =>
                        {
                            WINDOWINFO wndInfo = new WINDOWINFO();
                            GetWindowInfo(hWnd, ref wndInfo);

                            var isVisible = (wndInfo.dwStyle & WS_VISIBLE) != 0;
                            var isAppWnd = (wndInfo.dwExStyle & WS_EX_APPWINDOW) != 0;
                            if (isVisible && isAppWnd)
                            {
                                windowHandles.Add(hWnd);
                            }

                            return true;
                        }, IntPtr.Zero);
                    }

                    // remove all handles not on the list
                    for (int idx = availableGameInfo.Count - 1; idx >= 0; idx--)
                    {
                        if (!availableGameInfo[idx].IsValid())
                        {
                            availableGameInfo.RemoveAt(idx);
                        }
                        else if (availableGameInfo[idx].process == proc)
                        {
                            var isKnown = windowHandles.Contains(availableGameInfo[idx].windowMain.Handle);
                            if (!isKnown)
                            {
                                availableGameInfo.RemoveAt(idx);
                            }
                        }
                    }

                    // process all known
                    foreach (IntPtr testHandle in windowHandles)
                    {
                        StringBuilder sb = new StringBuilder(256);
                        GetWindowText(testHandle, sb, 256);

                        //Console.WriteLine("  >> testing window:{0:x} ({1})", testHandle, sb);

                        IntPtr childHandleInput = FindWindowEx(testHandle, (IntPtr)0, null, null);
                        //Console.WriteLine("    >> childInput:{0:x}", childHandleInput.ToInt64());
                        if (childHandleInput != IntPtr.Zero)
                        {
                            IntPtr childHandleClient = FindWindowEx(childHandleInput, (IntPtr)0, null, "_ctl.Window");
                            //Console.WriteLine("    >> childClient:{0:x}", childHandleClient.ToInt64());
                            if (childHandleClient != IntPtr.Zero)
                            {
                                bool added = false;
                                foreach (var info in availableGameInfo)
                                {
                                    if (info.windowMain.Handle == testHandle)
                                    {
                                        if (info.windowClient.Handle != childHandleClient) { info.windowClient = new HandleRef(this, childHandleClient); }
                                        if (info.windowInput.Handle != childHandleInput) { info.windowInput = new HandleRef(this, childHandleInput); }

                                        //Console.WriteLine("    >> already added");
                                        added = true;
                                        break;
                                    }
                                }

                                if (!added)
                                {
                                    var newInfo = new GameInfo
                                    {
                                        process = proc,
                                        windowTitle = sb.ToString(),
                                        windowMain = new HandleRef(this, testHandle),
                                        windowClient = new HandleRef(this, childHandleClient),
                                        windowInput = new HandleRef(this, childHandleInput)
                                    };

                                    //Console.WriteLine("    >> new entry!");
                                    availableGameInfo.Add(newInfo);
                                }
                            }
                        }
                    }
                }
            }
        
            foreach (var info in availableGameInfo)
            {
                info.rectMain = GetGameWindowBoundsFromAPI(info.windowMain);
                if (selectedWindow != IntPtr.Zero && info.windowMain.Handle == selectedWindow)
                {
                    cachedGameInfo = info;
                }
            }

            if (availableGameInfo.Count == 1)
            {
                cachedGameInfo = availableGameInfo[0];
            }
        }

        private Rectangle GetGameWindowBoundsFromAPI(HandleRef windowHandle)
        {
            Rectangle result = new Rectangle(0, 0, 0, 0);

            bool bHasWindow = windowHandle.Handle != IntPtr.Zero;
            if (bHasWindow)
            {
                if (GetWindowRect(windowHandle, out RECT windowRectApi))
                {
                    result = new Rectangle(windowRectApi.Left, windowRectApi.Top, windowRectApi.Right - windowRectApi.Left, windowRectApi.Bottom - windowRectApi.Top);
                }
            }

            return result;
        }

        private Bitmap TakeScreenshot()
        {
            Bitmap bitmap = null;

            Rectangle bounds = GetGameWindowBoundsFromAPI(cachedGameInfo.windowMain);
            if (bounds.Width > 0)
            {
                cachedGameInfo.rectMain = bounds;
                Rectangle absClipWindow = GetGameWindowBoundsFromAPI(cachedGameInfo.windowClient);
                Point relClipWindowPos = new Point(
                    Math.Max(0, absClipWindow.X - cachedGameInfo.rectMain.X), 
                    Math.Max(0, absClipWindow.Y - cachedGameInfo.rectMain.Y));
                Size relClipWindowSize = new Size(
                    Math.Min(bounds.Width - relClipWindowPos.X, absClipWindow.Width),
                    Math.Min(bounds.Height - relClipWindowPos.Y, absClipWindow.Height));

                cachedGameInfo.rectClient = new Rectangle(relClipWindowPos, relClipWindowSize);

                bitmap = new Bitmap(cachedGameInfo.rectMain.Width, cachedGameInfo.rectMain.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    IntPtr hdcBitmap;
                    try
                    {
                        hdcBitmap = g.GetHdc();
                    }
                    catch
                    {
                        return null;
                    }

                    // capture window contents only
                    PrintWindow(cachedGameInfo.windowMain.Handle, hdcBitmap, 0);
                    g.ReleaseHdc(hdcBitmap);
                }

                if (cachedGameInfo.rectClient.Width > 0 && cachedGameInfo.rectClient.Height > 0)
                {
                    Bitmap croppedBitmap = bitmap.Clone(cachedGameInfo.rectClient, bitmap.PixelFormat);
                    bitmap.Dispose();
                    bitmap = croppedBitmap;
                }

                if ((bitmap.Width >= finalSize.Width) && (bitmap.Height >= finalSize.Height))
                {
                    if (bitmap.Size != finalSize)
                    {
                        Bitmap scaledBitmap = new Bitmap(finalSize.Width, finalSize.Height);
                        using (Graphics g = Graphics.FromImage(scaledBitmap))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(bitmap, 0, 0, finalSize.Width, finalSize.Height);
                        }

                        bitmap.Dispose();
                        bitmap = scaledBitmap;
                    }
                }
                else
                {
                    currentState = EState.WindowTooSmall;
                }
            }
            else
            {
                currentState = EState.MissingGameWindow;
            }

            return bitmap;
        }
    }
}
