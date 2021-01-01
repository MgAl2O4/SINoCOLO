using SINoVision;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SINoCOLO
{
    public partial class MainForm : Form
    {
        private List<ScannerBase> scanners = new List<ScannerBase>();
        private ScreenReader screenReader = new ScreenReader();
        private GameLogic gameLogic = new GameLogic();
        private Bitmap cachedSourceScreen = null;
        private bool hasDetailCtrl = true;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_MOUSEMOVE = 0x200;
        private const int MK_LBUTTON = 0x0001;

        public MainForm()
        {
            InitializeComponent();

            scanners.Add(new ScannerColoCombat());
            scanners.Add(new ScannerColoPurify());
            scanners.Add(new ScannerMessageBox());
            scanners.Add(new ScannerCombat());

            foreach (var scanner in scanners)
            {
                scanner.DebugLevel = ScannerBase.EDebugLevel.None;
            }

            gameLogic.OnMouseClickRequested += GameLogic_OnMouseClickRequested;

            // show version number
            Text += " v" + typeof(Program).Assembly.GetName().Version.Major;            
        }

        private long MakeMouseMsgLParam(int posX, int posY)
        {
            screenReader.ConvertLocalToClient(posX, posY, out int clickX, out int clickY);
            return (long)((clickX & 0xffff) | ((clickY & 0xffff) << 16));
        }

        private void GameLogic_OnMouseClickRequested(int posX, int posY)
        {
            HandleRef windowHandle = screenReader.GetInputWindowHandle();
            if (windowHandle.Handle.ToInt64() != 0 && checkBoxClicks.Checked)
            {
                long lParam = MakeMouseMsgLParam(posX, posY);
                SendMessage(windowHandle.Handle, WM_LBUTTONDOWN, MK_LBUTTON, (IntPtr)lParam);
                SendMessage(windowHandle.Handle, WM_LBUTTONUP, 0, (IntPtr)lParam);
            }
        }

        private void Scan()
        {
            gameLogic.OnScanPrep();

            var srcScreenshot = screenReader.DoWork();
            if (srcScreenshot != null)
            {
                //if (screenReader.GetState() != ScreenReader.EState.WindowTooSmall)
                {
                    if (cachedSourceScreen != null) { cachedSourceScreen.Dispose(); }
                    cachedSourceScreen = srcScreenshot.Clone(new Rectangle(0, 0, srcScreenshot.Width, srcScreenshot.Height), srcScreenshot.PixelFormat);

                    var forcedSize = screenReader.GetExpectedSize();
                    var fastBitmap = ScreenshotUtilities.ConvertToFastBitmap(srcScreenshot, forcedSize.Width, forcedSize.Height);
                    foreach (var scanner in scanners)
                    {
                        var resultOb = scanner.Process(fastBitmap);
                        if (resultOb != null)
                        {
                            gameLogic.screenScanner = scanner;
                            gameLogic.screenData = resultOb;
                            gameLogic.OnScan();

                            if (hasDetailCtrl)
                            {
                                using (Graphics graphics = Graphics.FromImage(srcScreenshot))
                                {
                                    gameLogic.DrawScanHighlights(graphics);
                                }
                            }
                            break;
                        }
                    }
                }

                pictureBoxAnalyzed.Image = srcScreenshot;
                labelScreenshotFailed.Visible = false;
            }
            else
            {
                labelScreenshotFailed.Visible = hasDetailCtrl;
            }

            SetScreenState(screenReader.GetState());
        }

        private void timerScan_Tick(object sender, EventArgs e)
        {
            Scan();
        }

        private void SetScreenState(ScreenReader.EState NewState)
        {
            switch (NewState)
            {
                case ScreenReader.EState.MissingGameProcess:
                    labelStatus.Text = "Can't find BlueStacks process";
                    panelStatus.BackColor = Color.MistyRose;
                    break;

                case ScreenReader.EState.MissingGameWindow:
                    labelStatus.Text = "Can't find BlueStacks window";
                    panelStatus.BackColor = Color.MistyRose;
                    break;

                case ScreenReader.EState.WindowTooSmall:
                    labelStatus.Text = "BlueStacks window is too small";
                    panelStatus.BackColor = Color.MistyRose;
                    break;

                case ScreenReader.EState.Success:
                    labelStatus.Text = "Game active: " +
                        (gameLogic.screenScanner == null ? "[Unknown state]" : gameLogic.screenScanner.ScannerName);

                    panelStatus.BackColor = checkBoxClicks.Checked ? Color.LightGreen : Color.LightYellow;
                    break;
            }
        }

        private void topPanelClick(object sender, EventArgs e)
        {
            checkBoxClicks.Checked = !checkBoxClicks.Checked;
        }

        private void pictureBoxAnalyzed_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                HandleRef windowHandle = screenReader.GetInputWindowHandle();
                if (windowHandle.Handle.ToInt64() != 0)
                {
                    long lParam = MakeMouseMsgLParam(e.X, e.Y);
                    SendMessage(windowHandle.Handle, WM_MOUSEMOVE, MK_LBUTTON, (IntPtr)lParam);
                }
            }
        }

        private void pictureBoxAnalyzed_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                HandleRef windowHandle = screenReader.GetInputWindowHandle();
                if (windowHandle.Handle.ToInt64() != 0)
                {
                    long lParam = MakeMouseMsgLParam(e.X, e.Y);
                    SendMessage(windowHandle.Handle, WM_LBUTTONUP, 0, (IntPtr)lParam);
                }
            }
        }

        private void pictureBoxAnalyzed_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                HandleRef windowHandle = screenReader.GetInputWindowHandle();
                if (windowHandle.Handle.ToInt64() != 0)
                {
                    long lParam = MakeMouseMsgLParam(e.X, e.Y);
                    SendMessage(windowHandle.Handle, WM_LBUTTONDOWN, MK_LBUTTON, (IntPtr)lParam);
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (cachedSourceScreen != null)
                {
                    for (int idx = 1; idx < 1000000; idx++)
                    {
                        string testPath = "real-source" + idx + ".jpg";
                        if (!System.IO.File.Exists(testPath))
                        {
                            cachedSourceScreen.Save(testPath);
                            break;
                        }
                    }
                }
            }
        }

        private void buttonDetails_Click(object sender, EventArgs e)
        {
            if (hasDetailCtrl)
            {
                pictureBoxAnalyzed.Visible = false;
                pictureBoxAnalyzed.Enabled = false;
                labelScreenshotFailed.Visible = false;
                buttonDetails.Text = "Show details";

                Size = new Size(380, 130);
            }
            else
            {
                pictureBoxAnalyzed.Visible = true;
                pictureBoxAnalyzed.Enabled = true;
                labelScreenshotFailed.Visible = false;
                buttonDetails.Text = "Hide details";

                Size = new Size(380, 737);
            }

            hasDetailCtrl = !hasDetailCtrl;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
#if !DEBUG
            buttonDetails_Click(null, null);
#endif // !DEBUG
        }
    }
}
