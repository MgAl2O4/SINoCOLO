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
        private bool selectInstanceMode = false;
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
            scanners.Add(new ScannerTitleScreen());

            foreach (var scanner in scanners)
            {
                scanner.DebugLevel = ScannerBase.EDebugLevel.None;
            }

            gameLogic.OnMouseClickRequested += GameLogic_OnMouseClickRequested;
            gameLogic.OnSaveScreenshot += GameLogic_OnSaveScreenshot;

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

        private void GameLogic_OnSaveScreenshot()
        {
            if (cachedSourceScreen != null)
            {
                for (int idx = 1; idx < 1000000; idx++)
                {
                    string testPath = "image-err" + idx + ".jpg";
                    if (!System.IO.File.Exists(testPath))
                    {
                        cachedSourceScreen.Save(testPath);
                        break;
                    }
                }
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
                            break;
                        }
                    }

                    gameLogic.OnScan();
                    if (hasDetailCtrl)
                    {
                        using (Graphics graphics = Graphics.FromImage(srcScreenshot))
                        {
                            gameLogic.DrawScanHighlights(graphics);
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
            selectInstanceMode = false;
            string statusDesc = "Status: ";
            switch (NewState)
            {
                case ScreenReader.EState.MissingGameProcess:
                    if (screenReader.GetAvailableGames().Count > 1)
                    {
                        statusDesc += "Multiple BlueStacks instaces found - click here to select";
                        selectInstanceMode = true;
                    }
                    else
                    {
                        statusDesc += "Can't find BlueStacks process";
                    }
                    panelStatus.BackColor = Color.MistyRose;
                    break;

                case ScreenReader.EState.MissingGameWindow:
                    statusDesc += "Can't find BlueStacks window";
                    panelStatus.BackColor = Color.MistyRose;
                    break;

                case ScreenReader.EState.WindowTooSmall:
                    statusDesc += "BlueStacks window is too small";
                    panelStatus.BackColor = Color.MistyRose;
                    break;

                case ScreenReader.EState.Success:
                    statusDesc += "Game active: " +
                        (gameLogic.screenScanner == null ? "[Unknown state]" : gameLogic.screenScanner.ScannerName);

                    panelStatus.BackColor = checkBoxClicks.Checked ? Color.LightGreen : Color.LightYellow;
                    break;
            }

            labelStatus.Text = statusDesc;
        }

        private void topPanelClick(object sender, EventArgs e)
        {
            if (selectInstanceMode)
            {
                var showLocation = new Point(Location.X, Location.Y + Math.Min(Size.Height, 100));
                var selectForm = new InstanceSelectForm{ 
                    screenReader = screenReader,
                    StartPosition = FormStartPosition.Manual,
                    Location = showLocation
                };
                selectForm.ShowDialog();
            }
            else
            {
                checkBoxClicks.Checked = !checkBoxClicks.Checked;
            }
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
                MaximumSize = MinimumSize;
            }
            else
            {
                pictureBoxAnalyzed.Visible = true;
                pictureBoxAnalyzed.Enabled = true;
                textBoxDetails.Visible = true;
                labelScreenshotFailed.Visible = false;
                buttonDetails.Text = "Hide details";

                MaximumSize = new Size(0, 0);
                Size = new Size(MinimumSize.Width, 734);
            }

            hasDetailCtrl = !hasDetailCtrl;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            string[] itemDesc = new string[4];
            itemDesc[(int)GameLogic.EStoryMode.None] = "Ignore";
            itemDesc[(int)GameLogic.EStoryMode.AdvanceChapter] = "Complete Chapter";
            itemDesc[(int)GameLogic.EStoryMode.FarmStage] = "Farm Stage";
            itemDesc[(int)GameLogic.EStoryMode.FarmEvent] = "Farm Event";

            comboBoxStoryMode.Items.Clear();
            comboBoxStoryMode.Items.AddRange(itemDesc);
            comboBoxStoryMode.SelectedIndex = (int)GameLogic.EStoryMode.FarmStage;

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

            foreach (var scanner in scanners)
            {
                lines.Add(string.Format("  {0} = {1}", scanner.ScannerName, scanner.GetState()));
            }

            gameLogic.AppendDetails(lines);
            textBoxDetails.Lines = lines.ToArray();
        }

        private void comboBoxStoryMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            var mode = GameLogic.EStoryMode.None;
            if (comboBoxStoryMode.SelectedIndex > 0)
            {
                mode = (GameLogic.EStoryMode)comboBoxStoryMode.SelectedIndex;
            }

            gameLogic.SetStoryMode(mode);
        }
    }
}
