using System;
using System.Drawing;

namespace SINoVision
{
    public class ScannerPurify : ScannerBase
    {
        public class ScreenData
        {
            public bool isActive = false;
            public bool hasBurstInCenter = false;

            public override string ToString()
            {
                return string.Format("Active: {0}, burstInCenter: {1}", isActive, hasBurstInCenter);
            }
        }

        private FastPixelMatch matchLabelI = new FastPixelMatchMono(170, 255);
        private FastPixelMatch matchLabelO = new FastPixelMatchMono(0, 140);
        private FastPixelMatch matchTimerI = new FastPixelMatchMono(170, 245);
        private FastPixelMatch matchTimerO = new FastPixelMatchHSV(-5, 15, 50, 100, 10, 60);
        private FastPixelMatch matchPause = new FastPixelMatchMono(200, 255);
        private FastPixelMatch matchBurstCenter = new FastPixelMatchHueMono(20, 40, 130, 195);

        private Rectangle rectBurstCenter = new Rectangle(153, 343, 30, 2);
        private Rectangle rectBurstAction = new Rectangle(156, 288, 30, 30);
        private Rectangle[] rectActions;

        private Point[] posLabelI = { new Point(23, 40), new Point(23, 47) };
        private Point[] posLabelO = { new Point(22, 40), new Point(22, 47) };
        private Point[] posTimeI = { new Point(154, 19), new Point(154, 22), new Point(161, 19), new Point(161, 22) };
        private Point[] posTimeO = { new Point(156, 18), new Point(156, 23), new Point(163, 18), new Point(163, 23) };
        private Point[] posPauseI = { new Point(303, 24), new Point(308, 24) };
        private Point[] posHeaderPattern = { new Point(217, 17), new Point(149, 18), new Point(7, 17) };

        private Point[] posActionSlots = {
            new Point(184, 203), new Point(242, 276), new Point(255, 360), new Point(224, 455),
            new Point(146, 445), new Point(64, 395), new Point(47, 298), new Point(97, 219),
        };

        private MLClassifierPurifyHeader classifierHeader = new MLClassifierPurifyHeader();
        private string[] scannerStates = new string[] { "Idle", "NoTimer", "Ok" };

        public ScannerPurify()
        {
            ScannerName = "[Purify]";
            DebugLevel = EDebugLevel.None;

            rectActions = new Rectangle[posActionSlots.Length];
            for (int idx = 0; idx < rectActions.Length; idx++)
            {
                rectActions[idx] = new Rectangle(posActionSlots[idx].X, posActionSlots[idx].Y, 32, 32);
            }

            classifierHeader.InitializeModel();
        }

        public override string GetState()
        {
            return scannerStates[scannerState];
        }

        public override object Process(FastBitmapHSV bitmap)
        {
            scannerState = 1;
            bool isValid = HasTimerMarkers(bitmap);
            if (!isValid)
            {
                isValid = HasPurifyHeader(bitmap);
            }

            if (isValid)
            {
                scannerState = 2;
                var outputOb = new ScreenData();

                ScanPauseButton(bitmap, outputOb);
                ScanBurst(bitmap, outputOb);

                return outputOb;
            }

            return null;
        }

        public override Rectangle GetSpecialActionBox(int actionType)
        {
            return rectBurstAction;
        }

        public override Rectangle[] GetActionBoxes()
        {
            return rectActions;
        }

        protected bool HasTimerMarkers(FastBitmapHSV bitmap)
        {
            foreach (var pos in posLabelI)
            {
                var isMatching = matchLabelI.IsMatching(bitmap.GetPixel(pos.X, pos.Y));
                if (!isMatching)
                {
                    if (DebugLevel >= EDebugLevel.Verbose)
                    {
                        Console.WriteLine("{0} HasTimerMarkers: failed LabelI => ({1},{2}) = ({3}) vs ({4})",
                            ScannerName, pos.X, pos.Y, bitmap.GetPixel(pos.X, pos.Y), matchLabelI);
                    }

                    return false;
                }
            }

            foreach (var pos in posLabelO)
            {
                var isMatching = matchLabelO.IsMatching(bitmap.GetPixel(pos.X, pos.Y));
                if (!isMatching)
                {
                    if (DebugLevel >= EDebugLevel.Verbose)
                    {
                        Console.WriteLine("{0} HasTimerMarkers: failed LabelO => ({1},{2}) = ({3}) vs ({4})",
                            ScannerName, pos.X, pos.Y, bitmap.GetPixel(pos.X, pos.Y), matchLabelO);
                    }

                    return false;
                }
            }

            foreach (var pos in posTimeI)
            {
                var isMatching = matchTimerI.IsMatching(bitmap.GetPixel(pos.X, pos.Y));
                if (!isMatching)
                {
                    if (DebugLevel >= EDebugLevel.Verbose)
                    {
                        Console.WriteLine("{0} HasTimerMarkers: failed TimerI => ({1},{2}) = ({3}) vs ({4})",
                            ScannerName, pos.X, pos.Y, bitmap.GetPixel(pos.X, pos.Y), matchTimerI);
                    }

                    return false;
                }
            }

            foreach (var pos in posTimeO)
            {
                var isMatching = matchTimerO.IsMatching(bitmap.GetPixel(pos.X, pos.Y));
                if (!isMatching)
                {
                    if (DebugLevel >= EDebugLevel.Verbose)
                    {
                        Console.WriteLine("{0} HasTimerMarkers: failed TimerO => ({1},{2}) = ({3}) vs ({4})",
                            ScannerName, pos.X, pos.Y, bitmap.GetPixel(pos.X, pos.Y), matchTimerO);
                    }

                    return false;
                }
            }

            return true;
        }

        protected void ScanPauseButton(FastBitmapHSV bitmap, ScreenData screenData)
        {
            var testPx1 = bitmap.GetPixel(posPauseI[0].X, posPauseI[0].Y);
            var testPx2 = bitmap.GetPixel(posPauseI[1].X, posPauseI[1].Y);

            screenData.isActive = matchPause.IsMatching(testPx1) && matchPause.IsMatching(testPx2);

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} ScanPauseButton: active:{1}", ScannerName, screenData.isActive);
            }
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                Console.WriteLine(">> px1:({0}), px2:({1}) vs ({2})", testPx1, testPx2, matchPause);
            }
        }

        protected void ScanBurst(FastBitmapHSV bitmap, ScreenData screenData)
        {
            var centerFillPct = ScreenshotUtilities.CountFillPct(bitmap, rectBurstCenter, matchBurstCenter);
            screenData.hasBurstInCenter = centerFillPct > 0.75f;

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} ScanBurst: {1}", ScannerName, screenData.hasBurstInCenter);
            }
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                Console.WriteLine(">> centerFillPct: {0}", centerFillPct);
            }
        }

        public float[] ExtractHeaderPatternData(FastBitmapHSV bitmap, int patternIdx)
        {
            // scan area: 20x8
            float[] values = new float[20 * 8];
            for (int idx = 0; idx < values.Length; idx++)
            {
                values[idx] = 0.0f;
            }

            const int monoSteps = 16;
            const float monoScale = 1.0f / monoSteps;

            for (int idxY = 0; idxY < 8; idxY++)
            {
                for (int idxX = 0; idxX < 20; idxX++)
                {
                    FastPixelHSV pixel = bitmap.GetPixel(posHeaderPattern[patternIdx].X + idxX, posHeaderPattern[patternIdx].Y + idxY);
                    int monoV = pixel.GetMonochrome() / (256 / monoSteps);

                    values[idxX + (idxY * 20)] = monoV * monoScale;
                }
            }

            return values;
        }

        protected bool HasPurifyHeader(FastBitmapHSV bitmap)
        {
            // failsafe, classifier based

            int thrMatching = posHeaderPattern.Length - 1;
            int numMatching = 0;

            for (int idx = 0; idx < posHeaderPattern.Length; idx++)
            {
                float[] values = ExtractHeaderPatternData(bitmap, idx);
                int headerClass = classifierHeader.Calculate(values, out float dummyPct);
                bool matching = headerClass == (idx + 1);
                numMatching += matching ? 1 : 0;
            }

            if (DebugLevel >= EDebugLevel.Verbose)
            {
                Console.WriteLine("{0} HasPurifyHeader: numMatching:{1}, threshold:{2} => {3}",
                    ScannerName, numMatching, thrMatching, numMatching >= thrMatching);
            }

            return numMatching >= thrMatching;
        }
    }
}
