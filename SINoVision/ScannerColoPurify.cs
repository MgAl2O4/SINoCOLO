using System;
using System.Drawing;

namespace SINoVision
{
    public class ScannerColoPurify : ScannerBase
    {
        public enum ESlotType
        {
            None,
            Small,
            Big,
            Locked,
            LockedBig,
        }

        public enum EBurstState
        {
            None,
            Ready,
            ReadyAndCenter,
            Active,
        }

        public enum ESpecialBox
        {
            BurstCenter,
            BurstReady,
            ReturnToBattle,
        }

        public class ScreenData
        {
            public bool SPIsValid;
            public float SPFillPct;
            public float BurstMarkerPctX;
            public float BurstMarkerPctY;
            public EBurstState BurstState;
            public ESlotType[] Slots = new ESlotType[8];

            public override string ToString()
            {
                string desc = string.Format("SP bar> valid:{0}, fill:{1:P2}\n", SPIsValid, SPFillPct);
                desc += string.Format("Burst> {0}\n", BurstState);
                for (int idx = 0; idx < Slots.Length; idx++)
                {
                    desc += string.Format("Slot[{0}]: {1}\n", idx, Slots[idx]);
                }

                return desc;
            }
        }

        private Point[] posPurifyPlateI = new Point[] { new Point(100, 9), new Point(118, 8), new Point(218, 8), new Point(235, 9) };
        private Point[] posPurifyPlateO = new Point[] { new Point(131, 11), new Point(205, 11) };
        private Point[] posActionSlots = new Point[] {
            new Point(185, 106), new Point(259, 170), new Point(258, 265), new Point(221, 357),
            new Point(92, 367), new Point(16, 302), new Point(52, 210), new Point(52, 111),
        };
        private Point[] posBurstMarkerI = new Point[] { new Point(11, 2), new Point(13, 2), new Point(21, 9), new Point(4, 6), new Point(20, 6), new Point(8, 10), new Point(16, 10) };
        private Point[] posBurstMarkerO = new Point[] { new Point(8, 4), new Point(16, 4), new Point(12, 7) };
        private Point posBurstMarkerCenter = new Point(12, 13);
        private Point cachedBurstPos;

        private Rectangle rectPurifyPlate = new Rectangle(132, 8, 74, 1);
        private Rectangle rectSPBar = new Rectangle(34, 66, 296, 1);
        private Rectangle rectBurstActive = new Rectangle(1, 235, 5, 90);
        private Rectangle rectBurstCenter = new Rectangle(153, 302, 30, 2);
        private Rectangle rectBurstArea = new Rectangle(65, 90, 190, 285);
        private Rectangle rectBurstAction = new Rectangle(154, 244, 30, 30);
        private Rectangle rectReturnAction = new Rectangle(254, 11, 75, 19);
        private Rectangle[] rectActionSlots;

        private FastPixelMatch matchPlateShiny = new FastPixelMatchMono(80, 230);
        private FastPixelMatch matchPlateBack = new FastPixelMatchMono(0, 40);
        private FastPixelMatch matchSPFull = new FastPixelMatchHueMono(40, 55, 90, 255);
        private FastPixelMatch matchSPEmpty = new FastPixelMatchHueMono(0, 360, 0, 50);
        private FastPixelMatch matchBurstCenter = new FastPixelMatchHueMono(20, 40, 130, 195);
        private FastPixelMatch matchBurstMarker = new FastPixelMatchHSV(0, 120, 0, 100, 80, 100);

        private MLClassifierPurifyType classifierPurify = new MLClassifierPurifyType();
        private string[] scannerStates = new string[] { "Idle", "NoTextBox", "NoPurifyPlate", "Ok" };

        public ScannerColoPurify()
        {
            ScannerName = "[ColoPurify]";
            DebugLevel = EDebugLevel.Simple;

            classifierPurify.InitializeModel();

            rectActionSlots = new Rectangle[posActionSlots.Length];
            for (int idx = 0; idx < posActionSlots.Length; idx++)
            {
                posActionSlots[idx].Y -= 8;
                rectActionSlots[idx] = new Rectangle(posActionSlots[idx].X + (42/2) - (32/2) + 5, posActionSlots[idx].Y + (92/2) - (32/2) + 5, 32, 32);
            }

            for (int idx = 0; idx < posBurstMarkerI.Length; idx++)
            {
                posBurstMarkerI[idx].X -= posBurstMarkerCenter.X;
                posBurstMarkerI[idx].Y -= posBurstMarkerCenter.Y;
            }

            for (int idx = 0; idx < posBurstMarkerO.Length; idx++)
            {
                posBurstMarkerO[idx].X -= posBurstMarkerCenter.X;
                posBurstMarkerO[idx].Y -= posBurstMarkerCenter.Y;
            }
        }

        public override string GetState()
        {
            return scannerStates[scannerState];
        }

        public override object Process(FastBitmapHSV bitmap)
        {
            scannerState = 1;
            var hasTextBox = HasChatBoxArea(bitmap);
            if (hasTextBox)
            {
                scannerState = 2;
                var hasPurifyPlate = HasPurifyPlate(bitmap);
                if (hasPurifyPlate)
                {
                    scannerState = 3;
                    var outputOb = new ScreenData();
                    ScanSP(bitmap, outputOb);
                    ScanBurst(bitmap, outputOb);

                    for (int idx = 0; idx < posActionSlots.Length; idx++)
                    {
                        ScanActionSlot(bitmap, posActionSlots[idx], outputOb, idx);
                    }

                    return outputOb;
                }
            }

            return null;
        }

        public override Rectangle[] GetActionBoxes()
        {
            return rectActionSlots;
        }

        public override Rectangle GetSpecialActionBox(int actionType)
        {
            if (actionType == (int)ESpecialBox.BurstCenter)
            {
                return rectBurstAction;
            }
            else if (actionType == (int)ESpecialBox.BurstReady)
            {
                return new Rectangle(cachedBurstPos.X - 15, cachedBurstPos.Y + 40, 30, 30);
            }
            else if (actionType == (int)ESpecialBox.ReturnToBattle)
            {
                return rectReturnAction;
            }

            return Rectangle.Empty;
        }

        protected bool HasPurifyPlate(FastBitmapHSV bitmap)
        {
            var hasMatch = true;

            for (int idx = 0; idx < posPurifyPlateI.Length; idx++)
            {
                var testPx = bitmap.GetPixel(posPurifyPlateI[idx].X, posPurifyPlateI[idx].Y);
                var isMatch = matchPlateShiny.IsMatching(testPx);
                if (!isMatch)
                {
                    hasMatch = false;
                    break;
                }
            }

            for (int idx = 0; idx < posPurifyPlateO.Length; idx++)
            {
                var testPx = bitmap.GetPixel(posPurifyPlateO[idx].X, posPurifyPlateO[idx].Y);
                var isMatch = matchPlateBack.IsMatching(testPx);
                if (!isMatch)
                {
                    hasMatch = false;
                    break;
                }
            }

            var rectSample0 = bitmap.GetPixel(rectPurifyPlate.X, rectPurifyPlate.Y);

            int maxHDiff = 0;
            int maxMDiff = 0;
            for (int idx = 1; idx < rectPurifyPlate.Width; idx++)
            {
                var testPx = bitmap.GetPixel(rectPurifyPlate.X + idx, rectPurifyPlate.Y);

                int hDiff = Math.Abs(testPx.GetHue() - rectSample0.GetHue());
                int mDiff = Math.Abs(testPx.GetMonochrome() - rectSample0.GetMonochrome());
                if (maxHDiff < hDiff) { maxHDiff = hDiff; }
                if (maxMDiff < mDiff) { maxMDiff = mDiff; }
            }

            if (maxHDiff > 35 || maxMDiff > 25)
            {
                hasMatch = false;
            }

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} HasPurifyPlate: {1}", ScannerName, hasMatch);
            }
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                string debugDesc = "  in: ";
                for (int idx = 0; idx < posPurifyPlateI.Length; idx++)
                {
                    var testPx = bitmap.GetPixel(posPurifyPlateI[idx].X, posPurifyPlateI[idx].Y);
                    if (idx > 0) { debugDesc += ", "; }

                    debugDesc += "(" + testPx + ")";
                }

                Console.WriteLine(debugDesc);

                debugDesc = "  out: ";
                for (int idx = 0; idx < posPurifyPlateO.Length; idx++)
                {
                    var testPx = bitmap.GetPixel(posPurifyPlateO[idx].X, posPurifyPlateO[idx].Y);
                    if (idx > 0) { debugDesc += ", "; }

                    debugDesc += "(" + testPx + ")";
                }

                Console.WriteLine(debugDesc);
                Console.WriteLine("  filterIn({0}), filterOut({1})", matchPlateShiny, matchPlateBack);

                Console.WriteLine("  maxHDiff:{0}, maxMDiff:{1}", maxHDiff, maxMDiff);
            }

            return hasMatch;
        }

        protected void ScanSP(FastBitmapHSV bitmap, ScreenData screenData)
        {
            int numMatchFull = 0;
            int numMatchEmpty = 0;
            int numChanges = 0;
            bool wasFull = false;
            bool wasEmpty = false;

            int lastPosFull = 0;
            for (int idx = 0; idx < rectSPBar.Width; idx++)
            {
                var testPx = bitmap.GetPixel(rectSPBar.X + idx, rectSPBar.Y);

                var isFull = matchSPFull.IsMatching(testPx);
                var isEmpty = matchSPEmpty.IsMatching(testPx);

                if (isFull) { lastPosFull = idx; }

                numChanges += (idx > 0 && isFull != wasFull) ? 1 : 0;
                numChanges += (idx > 0 && isEmpty != wasEmpty) ? 1 : 0;
                numMatchFull += isFull ? 1 : 0;
                numMatchEmpty += isEmpty ? 1 : 0;

                wasFull = isFull;
                wasEmpty = isEmpty;
            }

            float matchedPct = (numMatchEmpty + numMatchFull) / (float)rectSPBar.Width;

            screenData.SPIsValid = (matchedPct > 0.75);
            screenData.SPFillPct = lastPosFull / (float)rectSPBar.Width;

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} HasSP: {1}, fillPct:{2}", ScannerName, screenData.SPIsValid, screenData.SPFillPct);
            }
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                for (int idx = 0; idx < rectSPBar.Width; idx++)
                {
                    var testPx = bitmap.GetPixel(rectSPBar.X + idx, rectSPBar.Y);
                    Console.WriteLine("  X:{0},Y:{1} = {2} => Full:{3}, Empty:{4}",
                        rectSPBar.X + idx, rectSPBar.Y,
                        testPx,
                        matchSPFull.IsMatching(testPx),
                        matchSPEmpty.IsMatching(testPx));
                }

                Console.WriteLine("  filterFull({0}), filterEmpty({1})", matchSPFull, matchSPEmpty);
                Console.WriteLine("  numMatchFull: {0}, numMatchEmpty: {1}, matchedPct: {2}, numChanges:{3}",
                    numMatchFull, numMatchEmpty, matchedPct, numChanges);
            }
        }

        protected void ScanActionSlot(FastBitmapHSV bitmap, Point slotPos, ScreenData screenData, int slotIdx)
        {
            float[] pixelInput = ExtractActionSlotData(bitmap, slotIdx);
            ESlotType SlotType = (ESlotType)classifierPurify.Calculate(pixelInput, out float BestPct);

            screenData.Slots[slotIdx] = SlotType;
            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} ScanActionSlot[{1}]: {2} ({3:P2})", ScannerName, slotIdx, screenData.Slots[slotIdx], BestPct);
            }

            if (DebugLevel >= EDebugLevel.Verbose)
            {
                byte frameColor =
                    (SlotType == ESlotType.None) ? (byte)0 :
                    (SlotType == ESlotType.Locked || SlotType == ESlotType.LockedBig) ? (byte)128 :
                    (byte)255;

                DrawRectangle(bitmap, slotPos.X, slotPos.Y, 48, 96, frameColor, 1);
                if (SlotType == ESlotType.Big)
                {
                    DrawRectangle(bitmap, slotPos.X, slotPos.Y, 48, 96, frameColor, 3);
                }

                int previewX = slotPos.X + ((slotIdx < 4) ? -20 : (48 + 5));
                int previewY = slotPos.Y;
                int readIdx = 0;
                for (int idxY = 0; idxY < 16; idxY++)
                {
                    for (int idxX = 0; idxX < 16; idxX++)
                    {
                        byte color = (byte)(pixelInput[readIdx] * 255);
                        readIdx++;

                        bitmap.SetPixel(previewX + idxX, previewY + idxY, new FastPixelHSV(color, color, color));
                    }
                }
            }
        }

        private float GetActionSlotPixelValue(FastPixelHSV pixel)
        {
            const int hueSteps = 16;
            const int monoSteps = 16;

            const float monoScale = 1.0f / monoSteps;
            const float hueScale = monoScale / hueSteps;

            int hueV = pixel.GetHue() / (360 / hueSteps);
            int monoV = pixel.GetMonochrome() / (256 / monoSteps);

            float pixelV = (hueV * hueScale) + (monoV * monoScale);
            return pixelV;
        }

        public float[] ExtractActionSlotData(FastBitmapHSV bitmap, int slotIdx)
        {
            // scan area: 48x64, downsample to 16x16
            float[] values = new float[16 * 16];
            for (int idx = 0; idx < values.Length; idx++)
            {
                values[idx] = 0.0f;
            }

            Point slotPos = posActionSlots[slotIdx];
            const float scaleV = 1.0f / 12;
            bool shouldMirror = slotIdx < 4;

            if (shouldMirror)
            {
                for (int idxY = 0; idxY < 64; idxY++)
                {
                    for (int idxX = 0; idxX < 48; idxX++)
                    {
                        values[(idxX / 3) + ((idxY / 4) * 16)] += GetActionSlotPixelValue(bitmap.GetPixel(slotPos.X + 47 - idxX, slotPos.Y + idxY)) * scaleV;
                    }
                }
            }
            else
            {
                for (int idxY = 0; idxY < 64; idxY++)
                {
                    for (int idxX = 0; idxX < 48; idxX++)
                    {
                        values[(idxX / 3) + ((idxY / 4) * 16)] += GetActionSlotPixelValue(bitmap.GetPixel(slotPos.X + idxX, slotPos.Y + idxY)) * scaleV;
                    }
                }
            }

            return values;
        }

        private void ScanBurst(FastBitmapHSV bitmap, ScreenData screenData)
        {
            float monoAcc = 0.0f;
            for (int idxY = 0; idxY < rectBurstActive.Height; idxY++)
            {
                for (int idxX = 0; idxX < rectBurstActive.Width; idxX++)
                {
                    FastPixelHSV testPx = bitmap.GetPixel(rectBurstActive.X + idxX, rectBurstActive.Y + idxY);
                    monoAcc += testPx.GetMonochrome();
                }
            }

            float monoAvg = monoAcc / (rectBurstActive.Width * rectBurstActive.Height);
            float centerFillPct = 0;

            if (monoAvg < 15)
            {
                screenData.BurstState = EBurstState.Active;
            }
            else
            {
                centerFillPct = ScreenshotUtilities.CountFillPct(bitmap, rectBurstCenter, matchBurstCenter);
                if (centerFillPct > 0.75f)
                {
                    screenData.BurstState = EBurstState.ReadyAndCenter;
                    screenData.BurstMarkerPctX = 0.5f;
                    screenData.BurstMarkerPctY = 0.5f;

                    if (DebugLevel >= EDebugLevel.Verbose)
                    {
                        Rectangle box = GetSpecialActionBox((int)ESpecialBox.BurstCenter);
                        DrawRectangle(bitmap, box.X, box.Y, box.Width, box.Height, 255);
                    }
                }
                else
                {
                    ScanBurstPosition(bitmap, screenData);

                    if (DebugLevel >= EDebugLevel.Verbose && screenData.BurstState == EBurstState.Ready)
                    {
                        Rectangle box = GetSpecialActionBox((int)ESpecialBox.BurstReady);
                        DrawRectangle(bitmap, box.X, box.Y, box.Width, box.Height, 255);
                    }
                }
            }

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} ScanBurst: {1}", ScannerName, screenData.BurstState);
            }
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                Console.WriteLine(">> monoAvg: {0}, centerFillPct: {1}", monoAvg, centerFillPct);
            }
        }

        private void ScanBurstPosition(FastBitmapHSV bitmap, ScreenData screenData)
        {
            bool hasMarker = false;
            for (int idxY = 0; idxY < rectBurstArea.Height; idxY++)
            {
                for (int idxX = 0; idxX < rectBurstArea.Width; idxX++)
                {
                    FastPixelHSV testPx = bitmap.GetPixel(rectBurstArea.X + idxX, rectBurstArea.Y + idxY);
                    bool isMatch = matchBurstMarker.IsMatching(testPx);
                    if (isMatch)
                    {
                        hasMarker = HasBurstMarker(bitmap, rectBurstArea.X + idxX, rectBurstArea.Y + idxY);
                        if (hasMarker)
                        {
                            screenData.BurstState = EBurstState.Ready;
                            screenData.BurstMarkerPctX = idxX * 1.0f / rectBurstArea.Width;
                            screenData.BurstMarkerPctY = idxY * 1.0f / rectBurstArea.Height;
                            cachedBurstPos = new Point(rectBurstArea.X + idxX, rectBurstArea.Y + idxY);

                            DrawRectangle(bitmap, rectBurstArea.X + idxX - 15, rectBurstArea.Y + idxY - 15, 30, 30, 255);
                            break;
                        }
                    }
                }

                if (hasMarker) { break; }
            }

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} ScanBurstPosition: {1}", ScannerName, screenData.BurstState);
            }
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                Console.WriteLine(">> marker.X:{0:P2}, marker.Y:{1:P2}", screenData.BurstMarkerPctX, screenData.BurstMarkerPctY);
            }
        }

        private bool HasBurstMarker(FastBitmapHSV bitmap, int testX, int testY)
        {
            //bool wantsLogs = testX == 153 && testY == 112;

            for (int idx = 0; idx < posBurstMarkerI.Length; idx++)
            {
                FastPixelHSV testPx = bitmap.GetPixel(posBurstMarkerI[idx].X + testX, posBurstMarkerI[idx].Y + testY);
                bool isMatch = matchBurstMarker.IsMatching(testPx);
                //if (wantsLogs) Console.WriteLine("HasBurstMarker({0}, {1}) - inner[{2}]({3},{4}) = {5}", testX, testY, idx, posBurstMarkerI[idx].X + testX, posBurstMarkerI[idx].Y + testY, testPx);
                if (!isMatch)
                {
                    return false;
                }
            }

            for (int idx = 0; idx < posBurstMarkerO.Length; idx++)
            {
                FastPixelHSV testPx = bitmap.GetPixel(posBurstMarkerO[idx].X + testX, posBurstMarkerO[idx].Y + testY);
                bool isMatch = matchBurstMarker.IsMatching(testPx);
                //if (wantsLogs) Console.WriteLine("HasBurstMarker({0}, {1}) - outer[{2}]({3},{4}) = {5}", testX, testY, idx, posBurstMarkerO[idx].X + testX, posBurstMarkerO[idx].Y + testY, testPx);
                if (isMatch)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
