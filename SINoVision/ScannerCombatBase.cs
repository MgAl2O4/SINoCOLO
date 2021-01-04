using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace SINoVision
{
    public abstract class ScannerCombatBase : ScannerBase
    {
        public enum EWeaponType
        {
            Unknown,
            Instrument,
            Tome,
            Staff,
            Orb,
        }

        public enum EElementType
        {
            Unknown,
            Fire,
            Water,
            Wind,
        }

        public class ActionData
        {
            public bool isValid = false;
            public EWeaponType weaponClass = EWeaponType.Unknown;
            public EElementType element = EElementType.Unknown;

            public override string ToString()
            {
                return isValid ? string.Format("{0} ({1})", weaponClass, element) : "n/a";
            }
        }

        public class ScreenDataBase
        {
            public bool SPIsValid = false;
            public bool SPIsObstructed = false;
            public float SPFillPct = 0;

            public ActionData[] actions = new ActionData[5];

            public override string ToString()
            {
                string desc = "SP> " + (!SPIsValid ? "n/a" :
                    string.Format("{0:P0}{1}", SPFillPct, SPIsObstructed ? ", obstructed" : ""));

                for (int idx = 0; idx < actions.Length; idx++)
                {
                    desc += "\nAction[" + idx + "]> " + actions[idx];
                }

                return desc;
            }
        }

        protected Rectangle rectSPBar = new Rectangle(86, 484, 164, 1);
        protected Rectangle[] rectActionSlots = { new Rectangle(17, 501, 52, 52), new Rectangle(80, 501, 52, 52), new Rectangle(143, 501, 52, 52), new Rectangle(206, 501, 52, 52), new Rectangle(269, 501, 52, 52) };
        protected Rectangle rectActionIcon = new Rectangle(39, 4, 10, 10);
        protected Rectangle rectActionAvail = new Rectangle(3, 44, 4, 4);
        protected Rectangle[] rectActionElements = new Rectangle[] { new Rectangle(3, 3, 28, 2), new Rectangle(35, 47, 14, 2), new Rectangle(47, 36, 2, 10) };
        protected Rectangle rectBigButton = new Rectangle(103, 506, 131, 44);
        protected Point[] posBigButton = { new Point(106, 506), new Point(170, 506), new Point(230, 506), new Point(106, 551), new Point(170, 551), new Point(230, 551) };

        private FastPixelMatch matchSPFull = new FastPixelMatchHueMono(40, 55, 90, 255);
        private FastPixelMatch matchSPEmpty = new FastPixelMatchHueMono(0, 360, 0, 50);
        private FastPixelMatch matchActionAvail = new FastPixelMatchMono(180, 255);

        protected MLClassifierWeaponType classifierWeapon = new MLClassifierWeaponType();

        public ScannerCombatBase()
        {
            ScannerName = "[CombatBase]";
            DebugLevel = EDebugLevel.Simple;

            classifierWeapon.InitializeModel();
        }

        public override Rectangle[] GetActionBoxes()
        {
            return rectActionSlots;
        }

        protected void ScanSP(FastBitmapHSV bitmap, ScreenDataBase screenData)
        {
            int numMatchFull = 0;
            int numMatchEmpty = 0;
            int numChanges = 0;
            bool wasFull = false;
            bool wasEmpty = false;

            int lastPosFull = 0;
            for (int idx = 0; idx <= rectSPBar.Width; idx++)
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
            screenData.SPIsObstructed = (matchedPct < 0.95);

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} HasSP: {1}, isObstructed:{2}, fillPct:{3}", ScannerName, screenData.SPIsValid, screenData.SPIsObstructed, screenData.SPFillPct);
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

        protected EElementType ScanElementType(FastBitmapHSV bitmap, Rectangle bounds)
        {
            EElementType element = EElementType.Unknown;
            int countElemR = 0;
            int countElemG = 0;
            int countElemB = 0;
            int countTotal = 0;

            const int hueDrift = 30;
            const int hueB = 180;
            const int hueG = 130;
            const int hueR = 15;

            foreach (var sampleBounds in rectActionElements)
            {
                for (int idxY = 0; idxY < sampleBounds.Height; idxY++)
                {
                    for (int idxX = 0; idxX < sampleBounds.Width; idxX++)
                    {
                        FastPixelHSV testPx = bitmap.GetPixel(bounds.X + sampleBounds.X + idxX, bounds.Y + sampleBounds.Y + idxY);
                        countTotal++;

                        int testMono = testPx.GetMonochrome();
                        if (testMono < 210)
                        {
                            int testHue = testPx.GetHue();
                            countElemR += ((testHue > (hueR + 360 - hueDrift)) || (testHue < (hueR + hueDrift))) ? 1 : 0;
                            countElemG += ((testHue > (hueG - hueDrift)) && (testHue < (hueG + hueDrift))) ? 1 : 0;
                            countElemB += ((testHue > (hueB - hueDrift)) && (testHue < (hueB + hueDrift))) ? 1 : 0;
                        }
                    }
                }
            }

            int minThr = countTotal * 30 / 100;
            if ((countElemR > minThr) && (countElemR > countElemG) && (countElemR > countElemB))
            {
                element = EElementType.Fire;
            }
            else if ((countElemG > minThr) && (countElemG > countElemR) && (countElemG > countElemB))
            {
                element = EElementType.Wind;
            }
            else if ((countElemB > minThr) && (countElemB > countElemR) && (countElemB > countElemG))
            {
                element = EElementType.Water;
            }

            if (DebugLevel >= EDebugLevel.Verbose)
            {
                Console.WriteLine(">> elem counters: R:{0}, G:{1}, B:{2} => {3}", countElemR, countElemG, countElemB, element);
            }
            return element;
        }

        protected void ScanActionSlot(FastBitmapHSV bitmap, Rectangle bounds, ActionData actionData, int slotIdx)
        {
            for (int idxY = 0; idxY < rectActionAvail.Height; idxY++)
            {
                for (int idxX = 0; idxX < rectActionAvail.Width; idxX++)
                {
                    FastPixelHSV testPx = bitmap.GetPixel(bounds.X + rectActionAvail.X + idxX, bounds.Y + rectActionAvail.Y + idxY);
                    bool match = matchActionAvail.IsMatching(testPx);
                    if (match)
                    {
                        actionData.isValid = true;
                        break;
                    }
                }
            }

            if (actionData.isValid)
            {
                float[] pixelInput = ExtractActionSlotWeaponData(bitmap, slotIdx);
                actionData.weaponClass = (EWeaponType)classifierWeapon.Calculate(pixelInput, out float dummyPct);
                actionData.element = ScanElementType(bitmap, bounds);
            }

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} Action[{1}]: valid:{2}, class:{3}, elem: {4}", ScannerName, slotIdx,
                    actionData.isValid,
                    actionData.weaponClass,
                    actionData.element);
            }
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                var minMono = 255;
                var maxMono = 0;
                for (int idxY = 0; idxY < rectActionAvail.Height; idxY++)
                {
                    for (int idxX = 0; idxX < rectActionAvail.Width; idxX++)
                    {
                        FastPixelHSV testPx = bitmap.GetPixel(bounds.X + rectActionAvail.X + idxX, bounds.Y + rectActionAvail.Y + idxY);
                        minMono = Math.Min(minMono, testPx.GetMonochrome());
                        maxMono = Math.Max(maxMono, testPx.GetMonochrome());
                    }
                }

                Console.WriteLine(">> avail M:{0}..{1} (x:{2},y:{3},w:{4},h:{5})",
                    minMono, maxMono,
                    bounds.X + rectActionAvail.X, bounds.Y + rectActionAvail.Y,
                    rectActionAvail.Width, rectActionAvail.Height);
            }
        }

        public EElementType ScanElementType(FastBitmapHSV bitmap, int slotIdx)
        {
            var element = ScanElementType(bitmap, rectActionSlots[slotIdx]);
            return element;
        }

        public float[] ExtractActionSlotWeaponData(FastBitmapHSV bitmap, int slotIdx)
        {
            // scan area: 10x10 (rectActionIcon)
            float[] values = new float[10 * 10];
            for (int idx = 0; idx < values.Length; idx++)
            {
                values[idx] = 0.0f;
            }

            const int monoSteps = 16;
            const float monoScale = 1.0f / monoSteps;

            Point slotPos = rectActionSlots[slotIdx].Location;
            for (int idxY = 0; idxY < 10; idxY++)
            {
                for (int idxX = 0; idxX < 10; idxX++)
                {
                    FastPixelHSV pixel = bitmap.GetPixel(slotPos.X + rectActionIcon.X + idxX, slotPos.Y + rectActionIcon.Y + idxY);
                    int monoV = pixel.GetMonochrome() / (256 / monoSteps);

                    values[idxX + (idxY * 10)] = monoV * monoScale;
                }
            }

            return values;
        }

        protected FastPixelHSV[] FindSpecialActionButton(FastBitmapHSV bitmap)
        {
            FastPixelHSV[] samples = new FastPixelHSV[posBigButton.Length];
            samples[0] = bitmap.GetPixel(posBigButton[0].X, posBigButton[0].Y);

            int maxHDiff = 0;
            for (int idx = 1; idx < samples.Length; idx++)
            {
                samples[idx] = bitmap.GetPixel(posBigButton[idx].X, posBigButton[idx].Y);

                int hDiff = Math.Abs(samples[idx].GetHue() - samples[0].GetHue());
                if (maxHDiff < hDiff) { maxHDiff = hDiff; }
            }

            var hasSpecialAction = (maxHDiff < 20);
            return hasSpecialAction ? samples : null;
        }
    }
}
