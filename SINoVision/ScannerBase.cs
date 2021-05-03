using System;
using System.Drawing;

namespace SINoVision
{
    public class ScannerBase
    {
        public enum EDebugLevel
        {
            None,
            Simple,
            Verbose,
        }
        public EDebugLevel DebugLevel = EDebugLevel.None;
        public string ScannerName = "[Unknown]";
        protected int scannerState = 0;

        protected Rectangle rectChatLineConfirmA = new Rectangle(304, 578, 18, 12);
        protected Rectangle rectChatLineConfirmB = new Rectangle(273, 560, 34, 21);

        private FastPixelMatch matchChatBoxInner = new FastPixelMatchMono(210, 250);
        private FastPixelMatch matchChatBoxOuter = new FastPixelMatchMono(20, 50);
        private FastPixelMatch matchChatLineBack = new FastPixelMatchMono(250, 255);
        private FastPixelMatch matchChatLine = new FastPixelMatchHSV(150, 200, 0, 100, 0, 100);

        private Point[] posChatBoxOuter = new Point[] { new Point(136, 565), new Point(136, 597), new Point(215, 565), new Point(215, 597) };
        private Point[] posChatBoxInner = new Point[] { new Point(150, 572), new Point(150, 588), new Point(200, 572), new Point(200, 588) };
        private int posChatBackY = 599;
        private int posNoChatBackY = 494;
        private int[] posChatLineModeY = new int[] { -1, 590, 583 };
        private Rectangle rectChatLine = new Rectangle(20, 0, 200, 1);

        public virtual void PrepareForScan()
        {
            scannerState = 0;
        }

        public virtual string GetState()
        {
            return "--";
        }

        public virtual object Process(FastBitmapHSV bitmap)
        {
            return null;
        }

        public virtual Rectangle[] GetActionBoxes()
        {
            return null;
        }

        public virtual Rectangle GetSpecialActionBox(int actionType)
        {
            return Rectangle.Empty;
        }

        protected bool HasChatBoxArea(FastBitmapHSV bitmap)
        {
            var hasMatch =
                matchChatBoxInner.IsMatching(bitmap.GetPixel(posChatBoxInner[0].X, posChatBoxInner[0].Y)) &&
                matchChatBoxInner.IsMatching(bitmap.GetPixel(posChatBoxInner[1].X, posChatBoxInner[1].Y)) &&
                matchChatBoxInner.IsMatching(bitmap.GetPixel(posChatBoxInner[2].X, posChatBoxInner[2].Y)) &&
                matchChatBoxInner.IsMatching(bitmap.GetPixel(posChatBoxInner[3].X, posChatBoxInner[3].Y)) &&
                matchChatBoxOuter.IsMatching(bitmap.GetPixel(posChatBoxOuter[0].X, posChatBoxOuter[0].Y)) &&
                matchChatBoxOuter.IsMatching(bitmap.GetPixel(posChatBoxOuter[1].X, posChatBoxOuter[1].Y)) &&
                matchChatBoxOuter.IsMatching(bitmap.GetPixel(posChatBoxOuter[2].X, posChatBoxOuter[2].Y)) &&
                matchChatBoxOuter.IsMatching(bitmap.GetPixel(posChatBoxOuter[3].X, posChatBoxOuter[3].Y));

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} HasChatBoxArea: {1}", ScannerName, hasMatch);
            }
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                Console.WriteLine("  outer samples: {0}, {1}, {2}, {3} => filter({4})",
                    bitmap.GetPixel(posChatBoxOuter[0].X, posChatBoxOuter[0].Y).GetMonochrome(),
                    bitmap.GetPixel(posChatBoxOuter[1].X, posChatBoxOuter[1].Y).GetMonochrome(),
                    bitmap.GetPixel(posChatBoxOuter[2].X, posChatBoxOuter[2].Y).GetMonochrome(),
                    bitmap.GetPixel(posChatBoxOuter[3].X, posChatBoxOuter[3].Y).GetMonochrome(),
                    matchChatBoxOuter);

                Console.WriteLine("  inner samples: {0}, {1}, {2}, {3} => filter({4})",
                    bitmap.GetPixel(posChatBoxInner[0].X, posChatBoxInner[0].Y).GetMonochrome(),
                    bitmap.GetPixel(posChatBoxInner[1].X, posChatBoxInner[1].Y).GetMonochrome(),
                    bitmap.GetPixel(posChatBoxInner[2].X, posChatBoxInner[2].Y).GetMonochrome(),
                    bitmap.GetPixel(posChatBoxInner[3].X, posChatBoxInner[3].Y).GetMonochrome(),
                    matchChatBoxInner);
            }

            return hasMatch;
        }

        protected bool HasOpenedChatLine(FastBitmapHSV bitmap, out int mode)
        {
            mode = 0;
            for (int posX = 0; posX < bitmap.Width; posX++)
            {
                var testPx = bitmap.GetPixel(posX, posChatBackY);
                bool isMatching = matchChatLineBack.IsMatching(testPx);
                if (!isMatching)
                {
                    if (DebugLevel >= EDebugLevel.Verbose)
                    {
                        Console.WriteLine("{0} HasOpenedChatLine: failed match! ({1},{2})=({3}) vs filter({4})",
                            ScannerName, posX, posChatBackY, testPx, matchChatLineBack);
                    }
                    return false;
                }

                testPx = bitmap.GetPixel(posX, posNoChatBackY);
                isMatching = matchChatLineBack.IsMatching(testPx);
                if (isMatching)
                {
                    if (DebugLevel >= EDebugLevel.Verbose)
                    {
                        Console.WriteLine("{0} HasOpenedChatLine: failed no match! ({1},{2})=({3}) vs filter({4})",
                            ScannerName, posX, posNoChatBackY, testPx, matchChatLineBack);
                    }
                    return false;
                }
            }

            for (int testMode = 1; testMode < posChatLineModeY.Length; testMode++)
            {
                var avgPx = ScreenshotUtilities.GetAverageColor(bitmap, new Rectangle(rectChatLine.X, posChatLineModeY[testMode], rectChatLine.Width, rectChatLine.Height));
                bool isMatching = matchChatLine.IsMatching(avgPx) && !matchChatLineBack.IsMatching(avgPx);
                if (!isMatching)
                {
                    if (DebugLevel >= EDebugLevel.Verbose)
                    {
                        Console.WriteLine("{0} HasOpenedChatLine: failed mode:{1}... ({2}) vs filter({3})",
                            ScannerName, testMode, avgPx, matchChatLine);
                    }
                }
                else
                {
                    mode = testMode;
                    return true;
                }
            }

            return false;
        }

        protected void DrawRectangle(FastBitmapHSV bitmap, int posX, int posY, int width, int height, byte color, int border = 1)
        {
            FastPixelHSV pixelOb = new FastPixelHSV(color, color, color);

            for (int idxX = 0; idxX < width; idxX++)
            {
                bitmap.SetPixel(posX + idxX, posY - border, pixelOb);
                bitmap.SetPixel(posX + idxX, posY + height + border, pixelOb);
            }

            for (int idxY = 0; idxY < height; idxY++)
            {
                bitmap.SetPixel(posX - border, posY + idxY, pixelOb);
                bitmap.SetPixel(posX + width + border, posY + idxY, pixelOb);
            }
        }
    }
}
