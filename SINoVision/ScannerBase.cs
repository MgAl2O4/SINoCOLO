using System;
using System.Collections.Generic;
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

        private FastPixelMatchMono matchChatBoxInner = new FastPixelMatchMono(210, 250);
        private FastPixelMatchMono matchChatBoxOuter = new FastPixelMatchMono(20, 50);

        private Point[] posChatBoxOuter = new Point[] { new Point(136, 565), new Point(136, 597), new Point(215, 565), new Point(215, 597) };
        private Point[] posChatBoxInner = new Point[] { new Point(150, 572), new Point(150, 588), new Point(200, 572), new Point(200, 588) };

        public List<Rectangle> debugShapes = new List<Rectangle>();

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

        protected void DrawRectangle(FastBitmapHSV bitmap, int posX, int posY, int width, int height, byte color, int border = 1)
        {
            FastPixelHSV pixelOb = new FastPixelHSV(color, color, color);

            for (int idxX = 0; idxX < width; idxX++)
            {
                bitmap.Pixels[(posX + idxX) + ((posY - border) * bitmap.Width)] = pixelOb;
                bitmap.Pixels[(posX + idxX) + ((posY + height + border) * bitmap.Width)] = pixelOb;
            }

            for (int idxY = 0; idxY < height; idxY++)
            {
                bitmap.Pixels[(posX - border) + ((posY + idxY) * bitmap.Width)] = pixelOb;
                bitmap.Pixels[(posX + width + border) + ((posY + idxY) * bitmap.Width)] = pixelOb;
            }
        }
    }
}
