using SINoVision;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private int numHighFreqTicks = 0;
        private int numScanDelayTicks = 0;

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
                if (cachedSourceScreen != null) { cachedSourceScreen.Dispose(); }
                cachedSourceScreen = srcScreenshot.Clone(new Rectangle(0, 0, srcScreenshot.Width, srcScreenshot.Height), srcScreenshot.PixelFormat);

                if (screenReader.GetState() != ScreenReader.EState.WindowTooSmall &&
                    numScanDelayTicks <= 0)
                {
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
            UpdateTimerFreq();
            DetailLog();
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
                textBoxDetails.Visible = false;
                labelScreenshotFailed.Visible = false;
                buttonDetails.Text = "Show details";

                Size = MinimumSize;
            }
            else
            {
                pictureBoxAnalyzed.Visible = true;
                pictureBoxAnalyzed.Enabled = true;
                textBoxDetails.Visible = true;
                labelScreenshotFailed.Visible = false;
                buttonDetails.Text = "Hide details";

                Size = new Size(MinimumSize.Width, 734);
            }

            hasDetailCtrl = !hasDetailCtrl;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
#if !DEBUG
            buttonDetails_Click(null, null);
#endif // !DEBUG
        }

        private void UpdateTimerFreq()
        {
            bool hasValidScreen = gameLogic.screenScanner != null;
            if (hasValidScreen)
            {
                numHighFreqTicks = 10 * 20; // keep for 20s
            }
            else
            {
                numHighFreqTicks -= 1;
                if (numHighFreqTicks <= 0)
                {
                    numHighFreqTicks = 0;
                }
            }

            numScanDelayTicks -= 1;
            if (numScanDelayTicks < 0)
            {
                numScanDelayTicks = (numHighFreqTicks > 0) ? 0 : 20;
            }
        }

        private void DetailLog()
        {
            if (!textBoxDetails.Visible || Size.Width <= MinimumSize.Width)
            {
                return;
            }

            var lines = new List<string>();
            lines.Add(string.Format("Tick: high freq:{0}, delay:{1}{2}",
                numHighFreqTicks, numScanDelayTicks, numScanDelayTicks <= 0 ? " (scan now)" : ""));
            lines.Add(string.Format("Screenshot:{0} ({1})",
                cachedSourceScreen != null ? string.Format("{0}x{1}", cachedSourceScreen.Width, cachedSourceScreen.Height) : "n/a",
                screenReader.GetState()));
            lines.Add(string.Format("Logic:{0}, delay:{1}{2}",
                gameLogic.state, gameLogic.GetScanSkipCounter(), gameLogic.GetScanSkipCounter() <= 1 ? " (click)" : ""));

            // scanner status
            foreach (var scanner in scanners)
            {
                lines.Add(string.Format("  {0} = {1}", scanner.ScannerName, scanner.GetState()));
            }

            // cached data status           
            if (gameLogic.cachedDataCombat != null)
            {
                lines.Add("");
                lines.Add("Cached Combat:");
                string[] tokens = gameLogic.cachedDataCombat.ToString().Split('\n');
                lines.AddRange(tokens);
            }

            if (gameLogic.cachedDataColoCombat != null)
            {
                lines.Add("");
                lines.Add("Cached ColoCombat:");
                string[] tokens = gameLogic.cachedDataColoCombat.ToString().Split('\n');
                lines.AddRange(tokens);
            }

            if (gameLogic.cachedDataColoPurify != null)
            {
                lines.Add("");
                lines.Add("Cached ColoPurify:");
                string[] tokens = gameLogic.cachedDataColoPurify.ToString().Split('\n');
                lines.AddRange(tokens);
            }

            if (gameLogic.cachedDataMessageBox != null)
            {
                lines.Add("");
                lines.Add("Cached MessageBox:");
                string[] tokens = gameLogic.cachedDataMessageBox.ToString().Split('\n');
                lines.AddRange(tokens);
            }

            textBoxDetails.Lines = lines.ToArray();
        }
    }
}
