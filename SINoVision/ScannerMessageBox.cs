using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace SINoVision
{
    public class ScannerMessageBox : ScannerBase
    {
        public enum ESpecialBox
        {
            Unknown,
            MessageBoxOk,
            CombatReportOk,
            CombatReportRetry,
        }

        public class ScreenData
        {
            public ESpecialBox mode;
            public bool hasRetry;

            public override string ToString()
            {
                string desc = mode.ToString();
                if (mode == ESpecialBox.CombatReportOk && hasRetry)
                {
                    desc += " (with retry)";
                }

                return desc;
            }
        }

        private FastPixelMatch matchAvgOk = new FastPixelMatchHSV(10, 20, 50, 70, 40, 50);
        private FastPixelMatch matchAvgRetry = new FastPixelMatchHSV(25, 35, 20, 30, 80, 100);

        private Rectangle rectOkButton = new Rectangle(118, 547, 95, 27);
        private Rectangle rectStageTryAgainButton = new Rectangle(11, 549, 95, 27);
        private Rectangle rectStageOkButton = new Rectangle(123, 549, 95, 27);

        private MLClassifierButtons classifierButtons = new MLClassifierButtons();
        private string[] scannerStates = new string[] { "Idle", "NoButton", "Ok" };

        public ScannerMessageBox()
        {
            ScannerName = "[MessageBox]";
            DebugLevel = EDebugLevel.None;

            classifierButtons.InitializeModel();
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
                case (int)ESpecialBox.MessageBoxOk: return rectOkButton;
                case (int)ESpecialBox.CombatReportOk: return rectStageOkButton;
                case (int)ESpecialBox.CombatReportRetry: return rectStageTryAgainButton;
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
            FastPixelHSV avgPxA = GetAverageColor(bitmap, rectOkButton);
            var matchOkA = matchAvgOk.IsMatching(avgPxA);

            FastPixelHSV avgPxB = GetAverageColor(bitmap, rectStageOkButton);
            var matchOkB = matchAvgOk.IsMatching(avgPxB);

            FastPixelHSV avgPxC = new FastPixelHSV();
            bool matchRetry = false;
            if (matchOkB || DebugLevel >= EDebugLevel.Verbose)
            {
                avgPxC = GetAverageColor(bitmap, rectStageTryAgainButton);
                matchRetry = matchAvgRetry.IsMatching(avgPxC);
            }

            if (matchRetry && matchOkB)
            {
                int typeIdx = (int)ESpecialBox.CombatReportRetry;
                var buttonData = ExtractButtonData(bitmap, typeIdx);
                int classIdx = classifierButtons.Calculate(buttonData, out float DummyPct);
                screenData.hasRetry = classIdx == typeIdx;

                typeIdx = (int)ESpecialBox.CombatReportOk;
                buttonData = ExtractButtonData(bitmap, typeIdx);
                classIdx = classifierButtons.Calculate(buttonData, out DummyPct);
                if (classIdx == typeIdx)
                {
                    screenData.mode = (ESpecialBox)typeIdx;
                }
            }

            if (screenData.mode == ESpecialBox.Unknown && matchOkA)
            {
                int typeIdx = (int)ESpecialBox.MessageBoxOk;
                var buttonData = ExtractButtonData(bitmap, typeIdx);
                int classIdx = classifierButtons.Calculate(buttonData, out float DummyPct);
                if (classIdx == typeIdx)
                {
                    screenData.mode = (ESpecialBox)typeIdx;
                }
            }

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} HasOkButtonArea: {1}", ScannerName, screenData.mode);
            }
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                Console.WriteLine("  avgMsgBox:({0}) vs filter({1}) => {2}", avgPxA, matchAvgOk, matchOkA);
                Console.WriteLine("  avgFightReport:({0}) vs filter({1}) => {2}", avgPxB, matchAvgOk, matchOkB);
                Console.WriteLine("  avgTryAgain:({0}) vs filter({1}) => {2}", avgPxC, matchAvgRetry, matchRetry);
            }

            return matchOkA || matchOkB;
        }

        public float[] ExtractButtonData(FastBitmapHSV bitmap, int slotIdx)
        {
            // scan area: 16x5 (95x27 crop: 80x25 and scale down by 5)
            float[] values = new float[16 * 5];
            for (int idx = 0; idx < values.Length; idx++)
            {
                values[idx] = 0.0f;
            }

            const int monoSteps = 16;
            const float monoScale = 1.0f / monoSteps;
            const float accScale = 1.0f / 25.0f;

            Point slotPos =
                (slotIdx == (int)ESpecialBox.CombatReportOk) ? rectStageOkButton.Location :
                (slotIdx == (int)ESpecialBox.CombatReportRetry) ? rectStageTryAgainButton.Location :
                rectOkButton.Location;

            for (int idxY = 0; idxY < 25; idxY++)
            {
                for (int idxX = 0; idxX < 80; idxX++)
                {
                    FastPixelHSV pixel = bitmap.GetPixel(slotPos.X + idxX, slotPos.Y + idxY);
                    int monoV = pixel.GetMonochrome() / (256 / monoSteps);

                    values[(idxX / 5) + ((idxY / 5) * 16)] = monoV * monoScale * accScale;
                }
            }

            return values;
        }
    }
}
