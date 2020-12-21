using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace SINoVision
{
    public class ScannerMessageBox : ScannerBase
    {
        public class ScreenData
        {
            public override string ToString()
            {
                return "Yet Another Ok Button";
            }
        }

        private FastPixelMatch matchOkButtonInner = new FastPixelMatchHueMono(0, 20, 40, 120);
        private FastPixelMatch matchOkButtonOuter = new FastPixelMatchMono(20, 50);

        private Point[] posOkButtonOuter = new Point[] { new Point(151, 777), new Point(298, 777), new Point(151, 834), new Point(298, 834) };
        private Point[] posOkButtonInner = new Point[] { new Point(162, 788), new Point(286, 788), new Point(162, 822), new Point(286, 822) };
        private Rectangle rectOkButton = new Rectangle(164, 788, 122, 36);

        public ScannerMessageBox()
        {
            ScannerName = "[MessageBox]";
            DebugLevel = EDebugLevel.None;
        }

        public override object Process(FastBitmapHSV bitmap)
        {
            var hasMsgBox = HasOkButtonArea(bitmap);
            if (hasMsgBox)
            {
                return new ScreenData();
            }

            return null;
        }

        public override Rectangle[] GetActionBoxes()
        {
            return new Rectangle[] { rectOkButton };
        }

        protected bool HasOkButtonArea(FastBitmapHSV bitmap)
        {
            var hasMatch =
                matchOkButtonInner.IsMatching(bitmap.GetPixel(posOkButtonInner[0].X, posOkButtonInner[0].Y)) &&
                matchOkButtonInner.IsMatching(bitmap.GetPixel(posOkButtonInner[1].X, posOkButtonInner[1].Y)) &&
                matchOkButtonInner.IsMatching(bitmap.GetPixel(posOkButtonInner[2].X, posOkButtonInner[2].Y)) &&
                matchOkButtonInner.IsMatching(bitmap.GetPixel(posOkButtonInner[3].X, posOkButtonInner[3].Y)) &&
                matchOkButtonOuter.IsMatching(bitmap.GetPixel(posOkButtonOuter[0].X, posOkButtonOuter[0].Y)) &&
                matchOkButtonOuter.IsMatching(bitmap.GetPixel(posOkButtonOuter[1].X, posOkButtonOuter[1].Y)) &&
                matchOkButtonOuter.IsMatching(bitmap.GetPixel(posOkButtonOuter[2].X, posOkButtonOuter[2].Y)) &&
                matchOkButtonOuter.IsMatching(bitmap.GetPixel(posOkButtonOuter[3].X, posOkButtonOuter[3].Y));

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} HasOkButtonArea: {1}", ScannerName, hasMatch);
            }
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                Console.WriteLine("  outer samples: ({0}), ({1}), ({2}), ({3}) => filter({4})",
                    bitmap.GetPixel(posOkButtonOuter[0].X, posOkButtonOuter[0].Y),
                    bitmap.GetPixel(posOkButtonOuter[1].X, posOkButtonOuter[1].Y),
                    bitmap.GetPixel(posOkButtonOuter[2].X, posOkButtonOuter[2].Y),
                    bitmap.GetPixel(posOkButtonOuter[3].X, posOkButtonOuter[3].Y),
                    matchOkButtonOuter);

                Console.WriteLine("  inner samples: ({0}), ({1}), ({2}), ({3}) => filter({4})",
                    bitmap.GetPixel(posOkButtonInner[0].X, posOkButtonInner[0].Y),
                    bitmap.GetPixel(posOkButtonInner[1].X, posOkButtonInner[1].Y),
                    bitmap.GetPixel(posOkButtonInner[2].X, posOkButtonInner[2].Y),
                    bitmap.GetPixel(posOkButtonInner[3].X, posOkButtonInner[3].Y),
                    matchOkButtonInner);
            }

            return hasMatch;
        }
    }
}
