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
        private int numScanProcessDelayTicks = 0;
        private int numScanSnapshotDelayTicks = 0;
        private int numTicksToResetStoryMode = -1;
        private string cachedTitle;

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
            scanners.Add(new ScannerTitleScreen());
            scanners.Add(new ScannerCombat());
            scanners.Add(new ScannerPurify());

            foreach (var scanner in scanners)
            {
                scanner.DebugLevel = ScannerBase.EDebugLevel.None;
            }

            gameLogic.OnMouseClickRequested += GameLogic_OnMouseClickRequested;
            gameLogic.OnSaveScreenshot += GameLogic_OnSaveScreenshot;
            gameLogic.OnEventCounterUpdated += GameLogic_OnEventCounterUpdated;
            gameLogic.OnStateChangeNotify += GameLogic_OnStateChangeNotify;

            // show version number
            var assembly = typeof(Program).Assembly.GetName();
            cachedTitle = string.Format("{0} v{1}", assembly.Name, assembly.Version.Major);
            Text = cachedTitle;
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
            try
            {
                if (cachedSourceScreen != null)
                {
                    for (int idx = 1; idx < 1000000; idx++)
                    {
                        string testPath = "screenshot-" + idx + ".jpg";
                        if (!System.IO.File.Exists(testPath))
                        {
                            cachedSourceScreen.Save(testPath);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception on saving:" + ex);
            }
        }

        private void GameLogic_OnEventCounterUpdated()
        {
            if (gameLogic.eventCounter != numericEventRepeat.Value)
            {
                numericEventRepeat.Value = gameLogic.eventCounter;
            }
        }

        private void GameLogic_OnStateChangeNotify(GameLogic.EState newState)
        {
            // always force story mode: ignore when activating colo combat
            // shouldn't matter but meh, doesn't hurt to be on safe side
            numTicksToResetStoryMode = -1;
            if (newState == GameLogic.EState.ColoCombat)
            {
                numTicksToResetStoryMode = 20;
            }

            UpdateVisibileControls();
        }

        private void Scan()
        {
            gameLogic.OnScanPrep();
            if (numScanSnapshotDelayTicks > 0)
            {
                return;
            }

            var srcScreenshot = screenReader.DoWork();
            if (srcScreenshot != null)
            {
                if (cachedSourceScreen != null) { cachedSourceScreen.Dispose(); }
                cachedSourceScreen = srcScreenshot.Clone(new Rectangle(0, 0, srcScreenshot.Width, srcScreenshot.Height), srcScreenshot.PixelFormat);

                if (screenReader.GetState() != ScreenReader.EState.WindowTooSmall &&
                    (numScanProcessDelayTicks <= 0))
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

            if (numTicksToResetStoryMode > 0)
            {
                numTicksToResetStoryMode--;
                if (numTicksToResetStoryMode == 0)
                {
                    comboBoxStoryMode.SelectedIndex = (int)GameLogic.EStoryMode.None;
                    numTicksToResetStoryMode = -1;
                }
            }
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
                        statusDesc += "Multiple BlueStacks instances found - click here to select";
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

            string newTitle = cachedTitle;
            var cachedGame = screenReader.GetCachedGame();
            if (cachedGame != null)
            {
                newTitle += " [" + cachedGame.windowTitle + "]";
            }

            if (Text != newTitle)
            {
                Text = newTitle;
            }
        }

        private void topPanelClick(object sender, EventArgs e)
        {
            if (selectInstanceMode)
            {
                var showLocation = new Point(Location.X + 20, Location.Y + Math.Min(Size.Height, 20));
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
                gameLogic.SetCanClick(checkBoxClicks.Checked);
            }
        }

        private void checkBoxClicks_CheckedChanged(object sender, EventArgs e)
        {
            gameLogic.SetCanClick(checkBoxClicks.Checked);
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
            {
                string[] itemDesc = new string[4];
                itemDesc[(int)GameLogic.EStoryMode.None] = "Ignore";
                itemDesc[(int)GameLogic.EStoryMode.AdvanceChapter] = "Complete Chapter";
                itemDesc[(int)GameLogic.EStoryMode.FarmStage] = "Farm Stage";
                itemDesc[(int)GameLogic.EStoryMode.FarmEvent] = "Farm Event";

                comboBoxStoryMode.Items.Clear();
                comboBoxStoryMode.Items.AddRange(itemDesc);
                comboBoxStoryMode.SelectedIndex = (int)GameLogic.EStoryMode.FarmStage;
            }
            {
                string[] itemDesc = new string[5];
                itemDesc[(int)GameLogic.ETargetingMode.None] = "Manual";
                itemDesc[(int)GameLogic.ETargetingMode.Deselect] = "Deselect";
                itemDesc[(int)GameLogic.ETargetingMode.CycleAll] = "Cycle All";
                itemDesc[(int)GameLogic.ETargetingMode.CycleTop3] = "Cycle Front";
                itemDesc[(int)GameLogic.ETargetingMode.LockStrongest] = "Lock Strongest";

                comboBoxColoTarget.Items.Clear();
                comboBoxColoTarget.Items.AddRange(itemDesc);
                comboBoxColoTarget.SelectedIndex = (int)GameLogic.ETargetingMode.None;
            }
            numericEventRepeat_ValueChanged(null, null);
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

            numScanProcessDelayTicks -= 1;
            if (numScanProcessDelayTicks < 0)
            {
                // high freq: process in every timer tick, 10x per second
                // otherwise: run recognition once every 1s
                numScanProcessDelayTicks = (numHighFreqTicks > 0) ? 0 : 10;
            }

            numScanSnapshotDelayTicks -= 1;
            if (numScanSnapshotDelayTicks < 0)
            {
                // can click: take screenshot in every timer tick, 10x per second
                // otherwise: grab window once every 0.5s
                numScanSnapshotDelayTicks = checkBoxClicks.Checked ? 0 : 5;
            }
        }

        private void DetailLog()
        {
            if (!textBoxDetails.Visible || Size.Width <= MinimumSize.Width)
            {
                return;
            }

            var lines = new List<string>();
            lines.Add(string.Format("Tick: HFreq:{0}, snap:{1}, process:{2} {3}",
                numHighFreqTicks, numScanSnapshotDelayTicks, numScanProcessDelayTicks,
                (numScanProcessDelayTicks <= 0 && numScanSnapshotDelayTicks <= 0) ? " (scan now)" : ""));
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
            UpdateVisibileControls();
        }

        private void comboBoxColoTarget_SelectedIndexChanged(object sender, EventArgs e)
        {
            var mode = GameLogic.ETargetingMode.None;
            if (comboBoxColoTarget.SelectedIndex > 0)
            {
                mode = (GameLogic.ETargetingMode)comboBoxColoTarget.SelectedIndex;
            }

            gameLogic.SetTargetingMode(mode);
        }

        private void UpdateVisibileControls()
        {
            bool wantsCombatSelectors =
                (gameLogic.GetState() == GameLogic.EState.ColoCombat) ||
                (gameLogic.GetState() == GameLogic.EState.ColoPurify);

            bool wantsEventCounter = gameLogic.GetStoryMode() == GameLogic.EStoryMode.FarmEvent;

            comboBoxStoryMode.Visible = !wantsCombatSelectors;
            comboBoxColoTarget.Visible = wantsCombatSelectors;
            labelCombatMode.Visible = wantsCombatSelectors;
            labelStoryMode.Visible = !wantsCombatSelectors && !wantsEventCounter;
            numericEventRepeat.Visible = !wantsCombatSelectors && wantsEventCounter;
        }

        private void numericEventRepeat_ValueChanged(object sender, EventArgs e)
        {
            gameLogic.eventCounter = (int)numericEventRepeat.Value;
        }
    }
}
