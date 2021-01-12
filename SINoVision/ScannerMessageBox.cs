using System;
using System.Drawing;

namespace SINoVision
{
    public class ScannerMessageBox : ScannerBase
    {
        public enum EMessageType
        {
            Unknown,
            Ok,
            OkCancel,
            Close,
            CombatReport,
            CombatStart,
        }

        public enum EButtonPos
        {
            Unknown,
            Center,
            CenterTwoLeft,
            CenterTwoRight,
            CombatReportRetry,
            CombatReportOk,
            CombatStart,
            CombatDetails,
        }

        public enum EButtonType   
        { 
            Unknown,
            Cancel,
            Close,
            Retry,
            Next,
            Ok,
            Start,
            Details,
        }

        public enum EButtonColor
        {
            Unknown,
            Red,
            White,
            Spec,
        }

        public class ActionData
        {
            public EButtonType buttonType;
            public EButtonColor buttonColor;
            public bool isDisabled;
        }

        public class ScreenData
        {
            public EMessageType mode;
            public ActionData[] actions = new ActionData[8];

            public override string ToString()
            {
                string desc = "Type:" + mode.ToString();
                for (int idx = 1; idx < actions.Length; idx++)
                {
                    desc += string.Format("\n[{0}] {1}:{2}{3} ({4})",
                        idx,
                        actions[idx].buttonType,
                        actions[idx].buttonColor,
                        actions[idx].isDisabled ? ":Disabled" : "",
                        (EButtonPos)idx);
                }

                return desc;
            }
        }

        private FastPixelMatch matchAvgRed = new FastPixelMatchHSV(10, 20, 50, 70, 20, 50);
        private FastPixelMatch matchAvgWhite = new FastPixelMatchHSV(25, 40, 5, 40, 20, 90);
        private FastPixelMatch matchAvgSpec = new FastPixelMatchHSV(20, 40, 20, 40, 40, 80);

        private Rectangle rectOkButton = new Rectangle(118, 547, 95, 27);
        private Rectangle rectCombatRetryButton = new Rectangle(11, 549, 95, 27);
        private Rectangle rectCombatOkButton = new Rectangle(123, 549, 95, 27);
        private Rectangle rectCombatStartButton = new Rectangle(123, 496, 95, 27);
        private Rectangle rectCombatDetailsButton = new Rectangle(234, 408, 95, 27);
        private Rectangle rectTwoButtonsLeft = new Rectangle(65, 547, 95, 27);
        private Rectangle rectTwoButtonsRight = new Rectangle(177, 547, 95, 27);
        private Rectangle[] rectButtonPos;

        private Rectangle rectButtonText = new Rectangle(29, 6, 32, 16);

        private MLClassifierButtons classifierButtons = new MLClassifierButtons();
        private string[] scannerStates = new string[] { "Idle", "NoButton", "Ok" };

        public ScannerMessageBox()
        {
            ScannerName = "[MessageBox]";
            DebugLevel = EDebugLevel.None;

            classifierButtons.InitializeModel();
            
            rectButtonPos = new Rectangle[8];
            rectButtonPos[(int)EButtonPos.Unknown] = Rectangle.Empty;
            rectButtonPos[(int)EButtonPos.Center] = rectOkButton;
            rectButtonPos[(int)EButtonPos.CenterTwoLeft] = rectTwoButtonsLeft;
            rectButtonPos[(int)EButtonPos.CenterTwoRight] = rectTwoButtonsRight;
            rectButtonPos[(int)EButtonPos.CombatReportRetry] = rectCombatRetryButton;
            rectButtonPos[(int)EButtonPos.CombatReportOk] = rectCombatOkButton;
            rectButtonPos[(int)EButtonPos.CombatStart] = rectCombatStartButton;
            rectButtonPos[(int)EButtonPos.CombatDetails] = rectCombatDetailsButton;
        }

        public override string GetState()
        {
            return scannerStates[scannerState];
        }

        public override object Process(FastBitmapHSV bitmap)
        {
            scannerState = 1;
            var outputOb = new ScreenData();
            var hasMsgBox = HasOkButtonArea(bitmap, outputOb);
            if (hasMsgBox)
            {
                scannerState = 2;
                return outputOb;
            }

            return null;
        }

        public override Rectangle GetSpecialActionBox(int actionType)
        {
            switch (actionType)
            {
                case (int)EButtonPos.Center: return rectOkButton;
                case (int)EButtonPos.CenterTwoLeft: return rectTwoButtonsLeft;
                case (int)EButtonPos.CenterTwoRight: return rectTwoButtonsRight;
                case (int)EButtonPos.CombatReportOk: return rectCombatOkButton;
                case (int)EButtonPos.CombatReportRetry: return rectCombatRetryButton;
            }

            return Rectangle.Empty;
        }

        protected FastPixelHSV GetAverageColor(FastBitmapHSV bitmap, Rectangle bounds)
        {
            float hueAcc = 0.0f;
            float satAcc = 0.0f;
            float valAcc = 0.0f;
            float scale = 1.0f / bounds.Width;

            for (int idx = 0; idx < bounds.Width; idx++)
            {
                FastPixelHSV testPx = bitmap.GetPixel(bounds.X + idx, bounds.Y);
                hueAcc += testPx.GetHue();
                satAcc += testPx.GetSaturation();
                valAcc += testPx.GetValue();
            }

            FastPixelHSV avgPx = new FastPixelHSV();
            avgPx.SetHSV((int)(hueAcc * scale), (int)(satAcc * scale), (int)(valAcc * scale));
            return avgPx;
        }

        protected bool HasOkButtonArea(FastBitmapHSV bitmap, ScreenData screenData)
        {
            FastPixelHSV[] avgPx = new FastPixelHSV[rectButtonPos.Length];
            for (int idx = 1; idx < avgPx.Length; idx++)
            {
                avgPx[idx] = GetAverageColor(bitmap, rectButtonPos[idx]);

                var scanOb = new ActionData();
                scanOb.buttonColor =
                    matchAvgRed.IsMatching(avgPx[idx]) ? EButtonColor.Red :
                    matchAvgWhite.IsMatching(avgPx[idx]) ? EButtonColor.White :
                    matchAvgSpec.IsMatching(avgPx[idx]) ? EButtonColor.Spec :
                    EButtonColor.Unknown;

                if (scanOb.buttonColor != EButtonColor.Unknown)
                {
                    float[] values = ExtractButtonData(bitmap, idx);
                    scanOb.buttonType = (EButtonType)classifierButtons.Calculate(values, out float DummyPct);

                    switch (scanOb.buttonColor)
                    {
                        case EButtonColor.Red: scanOb.isDisabled = avgPx[idx].GetValue() < 40; break;
                        case EButtonColor.White: scanOb.isDisabled = avgPx[idx].GetValue() < 70; break;
                        default: break;
                    }
                }

                screenData.actions[idx] = scanOb;
            }

            if (screenData.actions[(int)EButtonPos.CombatReportOk].buttonColor == EButtonColor.Red &&
                screenData.actions[(int)EButtonPos.CombatReportOk].buttonType == EButtonType.Ok &&
                screenData.actions[(int)EButtonPos.CombatReportRetry].buttonColor == EButtonColor.White &&
                (screenData.actions[(int)EButtonPos.CombatReportRetry].buttonType == EButtonType.Retry || screenData.actions[(int)EButtonPos.CombatReportRetry].buttonType == EButtonType.Next))
            {
                screenData.mode = EMessageType.CombatReport;
            }
            else if (screenData.actions[(int)EButtonPos.CombatStart].buttonColor == EButtonColor.Red &&
                screenData.actions[(int)EButtonPos.CombatStart].buttonType == EButtonType.Start &&
                screenData.actions[(int)EButtonPos.CombatDetails].buttonColor == EButtonColor.Spec &&
                (screenData.actions[(int)EButtonPos.CombatDetails].buttonType == EButtonType.Details))
            {
                screenData.mode = EMessageType.CombatStart;
            }
            else if (screenData.actions[(int)EButtonPos.Center].buttonColor == EButtonColor.Red && 
                screenData.actions[(int)EButtonPos.Center].buttonType == EButtonType.Ok)
            {
                screenData.mode = EMessageType.Ok;
            }
            else if (screenData.actions[(int)EButtonPos.CenterTwoLeft].buttonColor == EButtonColor.White && 
                screenData.actions[(int)EButtonPos.CenterTwoLeft].buttonType == EButtonType.Cancel &&
                screenData.actions[(int)EButtonPos.CenterTwoRight].buttonColor == EButtonColor.Red && 
                screenData.actions[(int)EButtonPos.CenterTwoRight].buttonType == EButtonType.Ok)
            {
                screenData.mode = EMessageType.OkCancel;
            }
            else if (screenData.actions[(int)EButtonPos.Center].buttonColor == EButtonColor.White &&
                screenData.actions[(int)EButtonPos.Center].buttonType == EButtonType.Close)
            {
                screenData.mode = EMessageType.Close;
            }

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} Mode: {1}", ScannerName, screenData.mode);
            }
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                Console.WriteLine("  filterRed:({0}), filterWhite:({1}), filterSpec({2})", matchAvgRed, matchAvgWhite, matchAvgSpec);
                for (int idx = 1; idx < avgPx.Length; idx++)
                {
                    Console.WriteLine("  [{0}]:({1}), color:{2}, class:{3}",
                        (EButtonPos)idx, avgPx[idx], 
                        screenData.actions[idx].buttonColor,
                        screenData.actions[idx].buttonType);
                }
            }

            return screenData.mode != EMessageType.Unknown;
        }

        public float[] ExtractButtonData(FastBitmapHSV bitmap, int slotIdx)
        {
            // scan area: 16x8 (rectButtonText scaled down)
            float[] values = new float[16 * 8];
            for (int idx = 0; idx < values.Length; idx++)
            {
                values[idx] = 0.0f;
            }

            const int monoSteps = 16;
            const float monoScale = 1.0f / monoSteps;

            Point slotPos = rectButtonPos[slotIdx].Location;
            slotPos.X += rectButtonText.Location.X;
            slotPos.Y += rectButtonText.Location.Y;

            for (int idxY = 0; idxY < 16; idxY++)
            {
                for (int idxX = 0; idxX < 32; idxX++)
                {
                    FastPixelHSV pixel = bitmap.GetPixel(slotPos.X + idxX, slotPos.Y + idxY);
                    int monoV = pixel.GetMonochrome() / (256 / monoSteps);

                    values[(idxX / 2) + ((idxY / 2) * 16)] += monoV * monoScale * 0.25f;
                }
            }

            return values;
        }
    }
}
