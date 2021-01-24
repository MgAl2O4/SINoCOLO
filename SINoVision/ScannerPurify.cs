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

        private Point[] posActionSlots = {
            new Point(184, 203), new Point(242, 276), new Point(255, 360), new Point(224, 455),
            new Point(146, 445), new Point(64, 395), new Point(47, 298), new Point(97, 219),
        };

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
        }

        public override string GetState()
        {
            return scannerStates[scannerState];
        }

        public override object Process(FastBitmapHSV bitmap)
        {
            scannerState = 1;
            var hasTimer = HasTimerMarkers(bitmap);
            if (hasTimer)
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
    }
}
