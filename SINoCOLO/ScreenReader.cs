using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SINoCOLO
{
    class ScreenReader
    {
        public enum EState
        {
            None,
            MissingGameProcess,
            MissingGameWindow,
            WindowTooSmall,
            Success,
        }

        private Process cachedProcess;
        private HandleRef cachedWindowHandle;
        private HandleRef cachedInputWindowHandle;
        private HandleRef cachedClientWindowHandle;
        private Rectangle cachedGameWindow;
        private Rectangle cachedGameClipWindow;
        private Bitmap cachedScreenshot;

        private EState currentState;
        private bool savedScreenshot;

        private Size finalSize = new Size(338, 600);

        public ScreenReader()
        {
            currentState = EState.None;
            savedScreenshot = false;
        }

        public EState GetState() { return currentState; }
        public HandleRef GetInputWindowHandle() { return cachedInputWindowHandle; }

        public void OnScreenshotSave() { savedScreenshot = true; }
        public bool CanSaveScreenshot() { return !savedScreenshot; }
        public Size GetExpectedSize() { return finalSize; }

        public void ConvertLocalToClient(int localX, int localY, out int clientX, out int clientY)
        {
            // scale up back to cropped size
            float scaleX = (float)cachedGameClipWindow.Width / finalSize.Width;
            float scaleY = (float)cachedGameClipWindow.Height / finalSize.Height;
            float tmpClientX = localX * scaleX;
            float tmpClientY = localY * scaleY;

            clientX = Math.Max(0, (int)tmpClientX);
            clientY = Math.Max(0, (int)tmpClientY);
        }

        public Bitmap DoWork()
        {
            //perfTimer.Restart();
            currentState = EState.Success;

            cachedWindowHandle = FindGameWindow();
            bool bHasWindow = cachedWindowHandle.Handle.ToInt64() != 0;
            if (bHasWindow)
            {
                if (cachedScreenshot != null) { cachedScreenshot.Dispose(); }
                cachedScreenshot = TakeScreenshot(cachedWindowHandle);
                savedScreenshot = false;
            }
#if DEBUG
            else
            {
                string testScreenPath = @"D:\Projects\Git\SINoCOLO\samples\";
                //testScreenPath += "real-combat.jpg";
                testScreenPath += "real-source3.jpg";

                try
                {
                    cachedScreenshot = Image.FromFile(testScreenPath) as Bitmap;
                }
                catch (Exception ex) { Console.WriteLine("Failed to load test screenshot: {0}", ex); }
            }
#endif // DEBUG

            //perfTimer.Stop();
            //Console.WriteLine("Screenshot load: {0}ms", perfTimer.ElapsedMilliseconds);

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

        #endregion 
        
        private HandleRef FindGameWindow()
        {
            HandleRef WindowHandle = new HandleRef();
            if (cachedProcess == null || !cachedProcess.MainWindowTitle.StartsWith("BlueStacks"))
            {
                Process[] processes = Process.GetProcessesByName("BlueStacks");
                cachedProcess = null;
                cachedInputWindowHandle = new HandleRef();
                foreach (Process p in processes)
                {
                    bool hasMatchingTitle = p.MainWindowTitle.StartsWith("BlueStacks");
                    if (hasMatchingTitle)
                    {
                        cachedProcess = p;
                        break;
                    }
                }
            }

            if (cachedProcess != null)
            {
                IntPtr childHandleInput = FindWindowEx(cachedProcess.MainWindowHandle, (IntPtr)0, null, "BlueStacks Android PluginAndroid");
                IntPtr childHandleClient = (childHandleInput != IntPtr.Zero) ? FindWindowEx(childHandleInput, (IntPtr)0, null, "_ctl.Window") : IntPtr.Zero;
                if (childHandleInput != IntPtr.Zero && childHandleClient != IntPtr.Zero)
                {
                    cachedInputWindowHandle = new HandleRef(this, childHandleInput);
                    cachedClientWindowHandle = new HandleRef(this, childHandleClient);
                    WindowHandle = new HandleRef(this, cachedProcess.MainWindowHandle);
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

            return WindowHandle;
        }

        private Rectangle GetGameWindowBoundsFromAPI(HandleRef windowHandle)
        {
            Rectangle result = new Rectangle(0, 0, 0, 0);

            bool bHasWindow = windowHandle.Handle.ToInt64() != 0;
            if (bHasWindow)
            {
                if (GetWindowRect(windowHandle, out RECT windowRectApi))
                {
                    result = new Rectangle(windowRectApi.Left, windowRectApi.Top, windowRectApi.Right - windowRectApi.Left, windowRectApi.Bottom - windowRectApi.Top);
                }
            }

            return result;
        }

        private Bitmap TakeScreenshot(HandleRef windowHandle)
        {
            Bitmap bitmap = null;

            Rectangle bounds = GetGameWindowBoundsFromAPI(windowHandle);
            if (bounds.Width > 0)
            {
                cachedGameWindow = bounds;
                Rectangle absClipWindow = GetGameWindowBoundsFromAPI(cachedClientWindowHandle);
                Point relClipWindowPos = new Point(
                    Math.Max(0, absClipWindow.X - cachedGameWindow.X), 
                    Math.Max(0, absClipWindow.Y - cachedGameWindow.Y));
                Size relClipWindowSize = new Size(
                    Math.Min(bounds.Width - relClipWindowPos.X, absClipWindow.Width),
                    Math.Min(bounds.Height - relClipWindowPos.Y, absClipWindow.Height));

                cachedGameClipWindow = new Rectangle(relClipWindowPos, relClipWindowSize);

                bitmap = new Bitmap(cachedGameWindow.Width, cachedGameWindow.Height, PixelFormat.Format32bppArgb);
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
                    PrintWindow(windowHandle.Handle, hdcBitmap, 0);
                    g.ReleaseHdc(hdcBitmap);
                }

                Bitmap croppedBitmap = bitmap.Clone(cachedGameClipWindow, bitmap.PixelFormat);
                bitmap.Dispose();
                bitmap = croppedBitmap;

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
