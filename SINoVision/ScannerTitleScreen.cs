using System;
using System.Drawing;

namespace SINoVision
{
    public class ScannerTitleScreen : ScannerBase
    {
        public class ScreenData
        {
            public override string ToString()
            {
                return "Title screen";
            }
        }

        private FastPixelMatch matchLogoRed = new FastPixelMatchHueMono(0, 20, 20, 80);
        private FastPixelMatch matchLogoBlue = new FastPixelMatchHueMono(200, 220, 50, 100);
        private FastPixelMatch matchLogoWhite = new FastPixelMatchMono(253, 255);
        private FastPixelMatch matchLogoBlack = new FastPixelMatchMono(0, 3);

        private Rectangle rectStartArea = new Rectangle(25, 265, 280, 130);

        private Point[] posLogoWhite = new Point[] {
            new Point(266, 551), new Point(266, 556), new Point(266, 562),
            new Point(290, 551), new Point(290, 556), new Point(290, 562),
            new Point(265, 576), new Point(278, 576), new Point(291, 576),
            new Point(265, 576), new Point(278, 576)
        };
        private Point[] posLogoBlue = new Point[] {
            new Point(278, 554), new Point(278, 558), new Point(278, 561), new Point(278, 565)
        };
        private Point[] posLogoBlack = new Point[] {
            new Point(303, 550), new Point(312, 550), new Point(321, 550), new Point(329, 550),
            new Point(303, 554), new Point(312, 554), new Point(321, 554), new Point(329, 554),
            new Point(303, 571), new Point(329, 571),
            new Point(303, 575), new Point(312, 575), new Point(321, 575), new Point(329, 575)
        };
        private Point[] posLogoRed = new Point[] {
            new Point(309, 569), new Point(313, 569), new Point(318, 569), new Point(322, 569)
        };

        private string[] scannerStates = new string[] { "Idle", "NoLogo", "Ok" };

        public ScannerTitleScreen()
        {
            ScannerName = "[TitleScreen]";
            DebugLevel = EDebugLevel.None;
        }

        public override string GetState()
        {
            return scannerStates[scannerState];
        }

        public override object Process(FastBitmapHSV bitmap)
        {
            scannerState = 1;
            var hasMsgBox = HasLogoMarkers(bitmap);
            if (hasMsgBox)
            {
                scannerState = 2;
                var outputOb = new ScreenData();
                return outputOb;
            }

            return null;
        }

        public override Rectangle GetSpecialActionBox(int actionType)
        {
            return rectStartArea;
        }

        protected bool HasLogoMarkers(FastBitmapHSV bitmap)
        {
            foreach (var pos in posLogoWhite)
            {
                var isMatching = matchLogoWhite.IsMatching(bitmap.GetPixel(pos.X, pos.Y));
                if (!isMatching)
                {
                    if (DebugLevel >= EDebugLevel.Verbose)
                    {
                        Console.WriteLine("{0} HasLogoMarkers: failed WHITE => ({1},{2}) = ({3}) vs ({4})",
                            ScannerName, pos.X, pos.Y, bitmap.GetPixel(pos.X, pos.Y), matchLogoWhite);
                    }

                    return false;
                }
            }

            foreach (var pos in posLogoBlack)
            {
                var isMatching = matchLogoBlack.IsMatching(bitmap.GetPixel(pos.X, pos.Y));
                if (!isMatching)
                {
                    if (DebugLevel >= EDebugLevel.Verbose)
                    {
                        Console.WriteLine("{0} HasLogoMarkers: failed BLACK => ({1},{2}) = ({3}) vs ({4})",
                            ScannerName, pos.X, pos.Y, bitmap.GetPixel(pos.X, pos.Y), matchLogoBlack);
                    }

                    return false;
                }
            }

            foreach (var pos in posLogoBlue)
            {
                var isMatching = matchLogoBlue.IsMatching(bitmap.GetPixel(pos.X, pos.Y));
                if (!isMatching)
                {
                    if (DebugLevel >= EDebugLevel.Verbose)
                    {
                        Console.WriteLine("{0} HasLogoMarkers: failed BLUE => ({1},{2}) = ({3}) vs ({4})",
                            ScannerName, pos.X, pos.Y, bitmap.GetPixel(pos.X, pos.Y), matchLogoBlue);
                    }

                    return false;
                }
            }

            foreach (var pos in posLogoRed)
            {
                var isMatching = matchLogoRed.IsMatching(bitmap.GetPixel(pos.X, pos.Y));
                if (!isMatching)
                {
                    if (DebugLevel >= EDebugLevel.Verbose)
                    {
                        Console.WriteLine("{0} HasLogoMarkers: failed RED => ({1},{2}) = ({3}) vs ({4})",
                            ScannerName, pos.X, pos.Y, bitmap.GetPixel(pos.X, pos.Y), matchLogoRed);
                    }

                    return false;
                }
            }

            return true;
        }
    }
}
