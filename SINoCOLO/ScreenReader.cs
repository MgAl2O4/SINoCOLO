using System;
using System.Diagnostics;
using System.Drawing;
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
            SizeMismatch,
            Success,
        }

        private Process cachedProcess;
        private Screen cachedScreen;
        private HandleRef cachedWindowHandle;
        private HandleRef cachedInputWindowHandle;
        private float cachedScreenScaling;
        private Rectangle cachedGameWindow;
        private Bitmap cachedScreenshot;

        private EState currentState;
        private Stopwatch perfTimer;
        private bool savedScreenshot;

        private Size expectedWindowSize = new Size(462, 864);
        private bool forceUsingHDC = true;

        public ScreenReader()
        {
            perfTimer = new Stopwatch();
            currentState = EState.None;
            savedScreenshot = false;
        }

        public EState GetState() { return currentState; }
        public HandleRef GetInputWindowHandle() { return cachedInputWindowHandle; }
        public float GetScreenScaling() { return cachedScreenScaling; }
        public Size GetExpectedSize() { return expectedWindowSize; }

        public void OnScreenshotSave() { savedScreenshot = true; }
        public bool CanSaveScreenshot() { return !savedScreenshot; }

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
                WindowHandle = new HandleRef(this, cachedProcess.MainWindowHandle);
                
                IntPtr childHandle = FindWindowEx(cachedProcess.MainWindowHandle, (IntPtr)0, null, "BlueStacks Android PluginAndroid");
                if (childHandle != IntPtr.Zero)
                {
                    cachedInputWindowHandle = new HandleRef(this, childHandle);
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

        public float GetCustomScreenScalingFor(Screen screen)
        {
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            EnumDisplaySettings(screen.DeviceName, -1, ref dm);

            if (dm.dmPelsWidth == screen.Bounds.Width)
            {
                return 1.0f;
            }
            
            return (float)screen.Bounds.Width / (float)dm.dmPelsWidth;
        }

        private Rectangle GetAdjustedGameWindowBounds(HandleRef windowHandle)
        {
            Rectangle result = GetGameWindowBoundsFromAPI(windowHandle);

            Screen activeScreen = Screen.FromHandle(windowHandle.Handle);
            if (activeScreen != cachedScreen)
            {
                cachedScreen = activeScreen;
                cachedScreenScaling = GetCustomScreenScalingFor(activeScreen);
            }

            if (cachedScreenScaling != 1.0f)
            {
                result.X = (int)(result.X / cachedScreenScaling);
                result.Y = (int)(result.Y / cachedScreenScaling);
                result.Width = (int)(result.Width / cachedScreenScaling);
                result.Height = (int)(result.Height / cachedScreenScaling);
            }

            // force size to match learning sources: 462 x 864
            int diffWidth = Math.Abs(result.Width - expectedWindowSize.Width);
            int diffHeight = Math.Abs(result.Height - expectedWindowSize.Height);
            if ((diffWidth > 1) || (diffHeight > 1))
            {
                int wndWidth = (int)(expectedWindowSize.Width * cachedScreenScaling);
                int wndHeight = (int)(expectedWindowSize.Height * cachedScreenScaling);

                const uint SWP_NOMOVE = 0x0002;
                const uint SWP_NOOWNERZORDER = 0x0200;
                SetWindowPos(windowHandle.Handle, (IntPtr)0, result.X, result.Y, wndWidth, wndHeight, SWP_NOMOVE | SWP_NOOWNERZORDER);
            }

            return result;
        }

        private Bitmap TakeScreenshot(HandleRef windowHandle)
        {
            Bitmap bitmap = null;

            Rectangle bounds = GetAdjustedGameWindowBounds(windowHandle);
            if (bounds.Width > 0)
            {
                cachedGameWindow = bounds;

                bitmap = new Bitmap(cachedGameWindow.Width, cachedGameWindow.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    bool bIsNewerThanWindows7 = (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                        (Environment.OSVersion.Version.Major > 6) || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor > 1));

                    if (bIsNewerThanWindows7 && !forceUsingHDC)
                    {
                        // can't use PrintWindow API above win7, returns black screen
                        // copy entire screen - will capture all windows on top of game too
                        g.CopyFromScreen(cachedGameWindow.Location, Point.Empty, cachedGameWindow.Size);
                    }
                    else
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
                }

                int sizeDiff = Math.Abs(cachedGameWindow.Width - expectedWindowSize.Width) + Math.Abs(cachedGameWindow.Height - expectedWindowSize.Height);
                if (sizeDiff > 2)
                {
                    currentState = EState.SizeMismatch;
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
