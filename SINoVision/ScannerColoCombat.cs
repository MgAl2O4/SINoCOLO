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

        private Point[] posLifeforceMeter = { new Point(93, 27), new Point(118, 19), new Point(153, 13), new Point(182, 13), new Point(218, 19), new Point(243, 27) };
        private Point[] posDemonPrepI = { new Point(250, 87), new Point(247, 87), new Point(243, 87), new Point(235, 86), new Point(228, 86), new Point(223, 86) };
        private Point[] posDemonPrepO = { new Point(249, 88), new Point(246, 88), new Point(241, 87), new Point(237, 86), new Point(226, 86), new Point(220, 86) };

        private Point[] posStatFriend = { new Point(90, 175), new Point(93, 286), new Point(95, 398), new Point(22, 229), new Point(21, 334) };
        private Point[] posStatEnemy = { new Point(198, 175), new Point(196, 287), new Point(194, 398), new Point(266, 229), new Point(268, 344) };

        private Rectangle rectPurify = new Rectangle(266, 459, 67, 28);
        private Rectangle rectDemonType = new Rectangle(147, 55, 10, 10);
        private Rectangle rectDemonL = new Rectangle(18, 76, 50, 10);
        private Rectangle rectDemonR = new Rectangle(277, 76, 50, 10);

        private FastPixelMatch matchLifeforceR = new FastPixelMatchHueMono(12, 27, 90, 255);
        private FastPixelMatch matchLifeforceG = new FastPixelMatchHueMono(82, 97, 90, 255);
        private FastPixelMatch matchSpecialReload = new FastPixelMatchHueMono(26, 60, 50, 255);
        private FastPixelMatch matchSpecialRevive = new FastPixelMatchHueMono(80, 120, 50, 255);
        private FastPixelMatch matchSpecialShip = new FastPixelMatchHueMono(0, 25, 50, 255);
        private FastPixelMatch matchDemonPrepI = new FastPixelMatchHSV(0, 25, 0, 100, 80, 100);
        private FastPixelMatch matchDemonPrepO = new FastPixelMatchHSV(0, 20, 40, 100, 20, 40);

        private MLClassifierDemon classifierDemon = new MLClassifierDemon();
        private string[] scannerStates = new string[] { "Idle", "NoTextBox", "NoLifeForce", "Ok" };

        public ScannerColoCombat()
        {
            ScannerName = "[ColoCombat]";
            DebugLevel = EDebugLevel.Simple;

            classifierDemon.InitializeModel();
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
                var hasLifeforceMeter = HasLifeforceMeter(bitmap);
                if (hasLifeforceMeter)
                {
                    scannerState = 3;
                    var outputOb = new ScreenData();
                    ScanSP(bitmap, outputOb);

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
                    var testPx = bitmap.GetPixel(points[idx].X + offsetX, points[idx].Y + offsetY);
                    var matching = match.IsMatching(testPx);

                    desc += "(" + testPx + "):" + matching;
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
            var hasCircle = false;
            float pctDemonL = 0, pctDemonR = 0;

            float[] values = ExtractDemonCounterData(bitmap, 0);
            int IsDemonL = classifierDemon.Calculate(values, out pctDemonL);
            int IsDemonR = 0;
            if (IsDemonL > 0)
            {
                values = ExtractDemonCounterData(bitmap, 1);
                IsDemonR = classifierDemon.Calculate(values, out pctDemonR);
                if (IsDemonR > 0)
                {
                    hasCircle = true;
                }
            }

            if (hasCircle)
            {
                screenData.demonState = EDemonState.Active;

                values = ExtractDemonTypeData(bitmap);
                screenData.demonType = (EWeaponType)classifierWeapon.Calculate(values, out float dummyPctDT);
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
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                Console.WriteLine(">> IsDemonL: {0} ({1:P2}), IsDemonR:{2} ({3:P2})", IsDemonL, pctDemonL, IsDemonR, pctDemonR);
            }
        }

        public float[] ExtractDemonTypeData(FastBitmapHSV bitmap)
        {
            // scan area: 10x10 (rectActionIcon)
            float[] values = new float[10 * 10];
            for (int idx = 0; idx < values.Length; idx++)
            {
                values[idx] = 0.0f;
            }

            const int monoSteps = 16;
            const float monoScale = 1.0f / monoSteps;

            for (int idxY = 0; idxY < 10; idxY++)
            {
                for (int idxX = 0; idxX < 10; idxX++)
                {
                    FastPixelHSV pixel = bitmap.GetPixel(rectDemonType.X + idxX, rectDemonType.Y + idxY);
                    int monoV = pixel.GetMonochrome() / (256 / monoSteps);

                    values[idxX + (idxY * 10)] = monoV * monoScale;
                }
            }

            return values;
        }

        private float GetDemonPixelValue(FastPixelHSV pixel)
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

        public float[] ExtractDemonCounterData(FastBitmapHSV bitmap, int side)
        {
            // scan area: 50x10
            float[] values = new float[50 * 10];
            for (int idx = 0; idx < values.Length; idx++)
            {
                values[idx] = 0.0f;
            }

            Rectangle bounds = (side == 0) ? rectDemonL : rectDemonR;
            for (int idxY = 0; idxY < 10; idxY++)
            {
                for (int idxX = 0; idxX < 50; idxX++)
                {
                    FastPixelHSV pixel = bitmap.GetPixel(bounds.X + idxX, bounds.Y + idxY);
                    values[idxX + (idxY * 50)] = GetDemonPixelValue(pixel);
                }
            }

            return values;
        }

        public float[] ExtractFriendStatData(FastBitmapHSV bitmap, int playerIdx, int statIdx, out EStatMode statMode)
        {
            return ExtractStatData(bitmap, posStatFriend, playerIdx, statIdx, out statMode);
        }

        public float[] ExtractEnemyStatData(FastBitmapHSV bitmap, int playerIdx, int statIdx, out EStatMode statMode)
        {
            return ExtractStatData(bitmap, posStatEnemy, playerIdx, statIdx, out statMode);
        }
    }
}
