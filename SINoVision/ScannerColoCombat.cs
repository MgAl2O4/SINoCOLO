using System;
using System.Drawing;

namespace SINoVision
{
    public class ScannerColoCombat : ScannerCombatBase
    {
        public enum ESpecialAction
        {
            None,
            Reload,
            Revive,
            AttackShip,
        }

        public enum EDemonState
        {
            None,
            Preparing,
            Active,
        }

        public enum ESpecialBox
        {
            BigButton,
            EnterPurify,
        }

        public class ScreenData : ScannerCombatBase.ScreenDataBase
        {
            public ESpecialAction specialAction = ESpecialAction.None;
            public EDemonState demonState = EDemonState.None;
            public EWeaponType demonType = EWeaponType.Unknown;

            public override string ToString()
            {
                string desc = base.ToString();
                desc += string.Format("\nSpecialAction> {0}", specialAction);
                desc += string.Format("\nDemon> {0} {1}", demonState, (demonState == EDemonState.Active) ? demonType.ToString() : "");
                return desc;
            }
        }

        private Point[] posLifeforceMeter = { new Point(127, 83), new Point(168, 71), new Point(206, 64), new Point(254, 64), new Point(294, 71), new Point(331, 83) };
        private Point[] posDemonPrepI = { new Point(341, 163), new Point(336, 163), new Point(331, 163), new Point(325, 163), new Point(312, 163), new Point(304, 163) };
        private Point[] posDemonPrepO = { new Point(339, 163), new Point(334, 163), new Point(329, 163), new Point(322, 161), new Point(312, 161), new Point(306, 161) };
        private Point[] posDemonActiveI = { new Point(3, 6), new Point(10, 6), new Point(16, 6), new Point(21, 10), new Point(29, 7) };
        private Point[] posDemonActiveO = { new Point(6, 4), new Point(14, 6), new Point(16, 2), new Point(20, 6), new Point(31, 6) };

        private Rectangle rectPurify = new Rectangle(365, 666, 83, 40);
        private Rectangle rectDemonType = new Rectangle(199, 118, 16, 16);
        private Rectangle rectDemonL = new Rectangle(60, 148, 33, 13);
        private Rectangle rectDemonR = new Rectangle(410, 148, 33, 13);

        private FastPixelMatch matchLifeforceR = new FastPixelMatchHueMono(12, 27, 90, 255);
        private FastPixelMatch matchLifeforceG = new FastPixelMatchHueMono(82, 97, 90, 255);
        private FastPixelMatch matchSpecialReload = new FastPixelMatchHueMono(26, 60, 50, 255);
        private FastPixelMatch matchSpecialRevive = new FastPixelMatchHueMono(80, 120, 50, 255);
        private FastPixelMatch matchSpecialShip = new FastPixelMatchHueMono(0, 25, 50, 255);
        private FastPixelMatch matchDemonPrepI = new FastPixelMatchHSV(0, 25, 0, 50, 80, 100);
        private FastPixelMatch matchDemonPrepO = new FastPixelMatchHSV(10, 20, 70, 85, 20, 35);
        private FastPixelMatch matchDemonLI = new FastPixelMatchHSV(50, 100, 20, 30, 75, 95);
        private FastPixelMatch matchDemonLO = new FastPixelMatchHSV(110, 140, 55, 75, 25, 35);
        private FastPixelMatch matchDemonRI = new FastPixelMatchHSV(0, 50, 30, 50, 70, 90);
        private FastPixelMatch matchDemonRO = new FastPixelMatchHSV(0, 20, 85, 100, 30, 40);

        public ScannerColoCombat()
        {
            ScannerName = "[ColoCombat]";
            DebugLevel = EDebugLevel.Simple;
        }

        public override object Process(FastBitmapHSV bitmap)
        {
            var hasTextBox = HasChatBoxArea(bitmap);
            if (hasTextBox)
            {
                var hasLifeforceMeter = HasLifeforceMeter(bitmap);
                if (hasLifeforceMeter)
                {
                    var outputOb = new ScreenData();
                    ScanSP(bitmap, outputOb);
                    //ScanDemonSummon(bitmap, outputOb);

                    for (int idx = 0; idx < outputOb.actions.Length; idx++)
                    {
                        outputOb.actions[idx] = new ActionData();
                        ScanActionSlot(bitmap, rectActionSlots[idx], outputOb.actions[idx], idx);
                    }

                    if (!outputOb.actions[0].isValid && !outputOb.actions[4].isValid)
                    {
                        ScanSpecialAction(bitmap, outputOb);
                    }

                    ScanDemonSummon(bitmap, outputOb);
                    return outputOb;
                }
            }

            return null;
        }

        public override Rectangle GetSpecialActionBox(int actionType)
        {
            if (actionType == (int)ESpecialBox.BigButton)
            {
                return rectBigButton;
            }
            else if (actionType == (int)ESpecialBox.EnterPurify)
            {
                return rectPurify;
            }

            return Rectangle.Empty;
        }

        protected bool HasLifeforceMeter(FastBitmapHSV bitmap)
        {
            int numMatching = 0;
            int numChanges = 0;
            bool wasMatchingR = false;
            bool wasMatchingG = false;
            for (int idx = 0; idx < posLifeforceMeter.Length; idx++)
            {
                var testPx = bitmap.GetPixel(posLifeforceMeter[idx].X, posLifeforceMeter[idx].Y);
                var isMatchingR = matchLifeforceR.IsMatching(testPx);
                var isMatchingG = isMatchingR ? false : matchLifeforceG.IsMatching(testPx);

                numChanges += (idx > 0 && isMatchingR != wasMatchingR) ? 1 : 0;
                numChanges += (idx > 0 && isMatchingG != wasMatchingG) ? 1 : 0;
                numMatching += (isMatchingR || isMatchingG) ? 1 : 0;

                wasMatchingR = isMatchingR;
                wasMatchingG = isMatchingG;
            }

            bool hasMatch = (numMatching >= 5) && (numChanges < 4);

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} HasLifeforceMeter: {1}", ScannerName, hasMatch);
            }
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                string debugDesc = "  ";
                for (int idx = 0; idx < posLifeforceMeter.Length; idx++)
                {
                    var testPx = bitmap.GetPixel(posLifeforceMeter[idx].X, posLifeforceMeter[idx].Y);
                    if (idx > 0) { debugDesc += ", "; }

                    debugDesc += "(" + testPx + ")";
                }

                Console.WriteLine(debugDesc);
                Console.WriteLine("  filterGreen({0}), filterRed({1})", matchLifeforceG, matchLifeforceR);
                Console.WriteLine("  numMatching: {0}, numChanges: {1}", numMatching, numChanges);
            }

            return hasMatch;
        }

        private void ScanSpecialAction(FastBitmapHSV bitmap, ScreenData screenData)
        {
            FastPixelHSV[] samples = FindSpecialActionButton(bitmap);
            int numReload = 0;
            int numRevive = 0;
            int numShip = 0;

            if (samples != null)
            {
                for (int idx = 0; idx < samples.Length; idx++)
                {
                    numReload += matchSpecialReload.IsMatching(samples[idx]) ? 1 : 0;
                    numRevive += matchSpecialRevive.IsMatching(samples[idx]) ? 1 : 0;
                    numShip += matchSpecialShip.IsMatching(samples[idx]) ? 1 : 0;
                }

                screenData.specialAction =
                    (numReload > numRevive && numReload > numShip) ? ESpecialAction.Reload :
                    (numRevive > numReload && numRevive > numShip) ? ESpecialAction.Revive :
                    (numShip > numRevive && numShip > numReload) ? ESpecialAction.AttackShip :
                    ESpecialAction.None;
            }

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} Special: {1}", ScannerName, screenData.specialAction);
            }
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                if (samples != null)
                {
                    for (int idx = 0; idx < samples.Length; idx++)
                    {
                        Console.WriteLine(">> bigButton[{0}]: {1}", idx, samples[idx]);
                    }
                }

                Console.WriteLine(">> numReload:{0} ({1}), numRevive:{2} ({3}), numShip:{4} ({5})",
                    numReload, matchSpecialReload,
                    numRevive, matchSpecialRevive,
                    numShip, matchSpecialShip);
            }
        }

        protected bool HasMatchingSamples(FastBitmapHSV bitmap, Point[] points, int offsetX, int offsetY, FastPixelMatch match, string debugName)
        {
            if (DebugLevel == EDebugLevel.Verbose)
            {
                string desc = "";
                for (int idx = 0; idx < points.Length; idx++)
                {
                    if (idx > 0) { desc += ", "; }
                    desc += "(" + bitmap.GetPixel(points[idx].X + offsetX, points[idx].Y + offsetY) + ")";
                }

                Console.WriteLine("HasMatchingSamples: {2}> filter({0}) vs {1}", match, desc, debugName);
            }

            for (int idx = 0; idx < points.Length; idx++)
            {
                FastPixelHSV testPx = bitmap.GetPixel(points[idx].X + offsetX, points[idx].Y + offsetY);
                bool isMatch = match.IsMatching(testPx);
                if (!isMatch)
                {
                    return false;
                }
            }

            return true;
        }

        private void ScanDemonSummon(FastBitmapHSV bitmap, ScreenData screenData)
        {
            var hasCircle =
                HasMatchingSamples(bitmap, posDemonActiveI, rectDemonL.X, rectDemonL.Y, matchDemonLI, "activeLI") &&
                HasMatchingSamples(bitmap, posDemonActiveO, rectDemonL.X, rectDemonL.Y, matchDemonLO, "activeLO") &&
                HasMatchingSamples(bitmap, posDemonActiveI, rectDemonR.X, rectDemonR.Y, matchDemonRI, "activeRI") &&
                HasMatchingSamples(bitmap, posDemonActiveO, rectDemonR.X, rectDemonR.Y, matchDemonRO, "activeRO");

            if (hasCircle)
            {
                screenData.demonState = EDemonState.Active;

                float[] values = ExtractDemonTypeData(bitmap);
                screenData.demonType = (EWeaponType)classifierWeapon.Calculate(values, out float DummyPct);
            }
            else
            {
                var hasPrep =
                    HasMatchingSamples(bitmap, posDemonPrepI, 0, 0, matchDemonPrepI, "prepI") &&
                    HasMatchingSamples(bitmap, posDemonPrepO, 0, 0, matchDemonPrepO, "prepO");

                if (hasPrep)
                {
                    screenData.demonState = EDemonState.Preparing;
                }
            }

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} Demon: {1} {2}", ScannerName, screenData.demonState, 
                    hasCircle ? screenData.demonType.ToString() : "");
            }
        }

        public float[] ExtractDemonTypeData(FastBitmapHSV bitmap)
        {
            // scan area: 16x16 (rectActionIcon)
            float[] values = new float[16 * 16];
            for (int idx = 0; idx < values.Length; idx++)
            {
                values[idx] = 0.0f;
            }

            const int monoSteps = 16;
            const float monoScale = 1.0f / monoSteps;

            for (int idxY = 0; idxY < 16; idxY++)
            {
                for (int idxX = 0; idxX < 16; idxX++)
                {
                    FastPixelHSV pixel = bitmap.GetPixel(rectDemonType.X + idxX, rectDemonType.Y + idxY);
                    int monoV = pixel.GetMonochrome() / (256 / monoSteps);

                    values[idxX + (idxY * 16)] = monoV * monoScale;
                }
            }

            return values;
        }
    }
}
