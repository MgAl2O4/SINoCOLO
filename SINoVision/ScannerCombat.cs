﻿using System;
using System.Drawing;

namespace SINoVision
{
    public class ScannerCombat : ScannerCombatBase
    {
        public enum ESpecialAction
        {
            None,
            Reload,
        }

        public enum ESpecialBox
        {
            BigButton,
            CloseChatA,
            CloseChatB,
        }

        public class ScreenData : ScannerCombatBase.ScreenDataBase
        {
            public ESpecialAction specialAction = ESpecialAction.None;

            public override string ToString()
            {
                string desc = base.ToString();
                desc += string.Format("\nSpecialAction> {0}", specialAction);
                return desc;
            }
        }

        private Point posChestG = new Point(219, 8);
        private Point posChestS = new Point(260, 8);
        private Point posChestB = new Point(300, 8);
        private Rectangle rectChestArea = new Rectangle(2, 6, 6, 3);

        private FastPixelMatch matchSpecialReload = new FastPixelMatchHueMono(26, 60, 50, 255);
        private string[] scannerStates = new string[] { "Idle", "NoTextBox", "NoChests", "Ok" };

        public ScannerCombat()
        {
            ScannerName = "[Combat]";
            DebugLevel = EDebugLevel.Simple;
        }

        public override string GetState()
        {
            return scannerStates[scannerState];
        }

        public override object Process(FastBitmapHSV bitmap)
        {
            scannerState = 1;
            int chatLineMode = 0;

            var hasTextBox = HasChatBoxArea(bitmap);
            var hasOpenedChat = HasOpenedChatLine(bitmap, out chatLineMode);
            if (hasTextBox || hasOpenedChat)
            {
                scannerState = 2;
                var hasRewardChests = HasRewardChests(bitmap);
                if (hasRewardChests)
                {
                    scannerState = 3;
                    var outputOb = new ScreenData();
                    outputOb.chatMode = (EChatMode)chatLineMode;

                    ScanSP(bitmap, outputOb);
                    ScanSummonSelector(bitmap, outputOb);

                    for (int idx = 0; idx < outputOb.actions.Length; idx++)
                    {
                        outputOb.actions[idx] = new ActionData();
                        ScanActionSlot(bitmap, rectActionSlots[idx], outputOb.actions[idx], idx);
                    }

                    if (!outputOb.actions[0].isValid && !outputOb.actions[4].isValid)
                    {
                        ScanSpecialAction(bitmap, outputOb);
                    }

                    return outputOb;
                }
            }

            return null;
        }

        public override Rectangle GetSpecialActionBox(int actionType)
        {
            switch (actionType)
            {
                case (int)ESpecialBox.BigButton: return rectBigButton;
                case (int)ESpecialBox.CloseChatA: return rectChatLineConfirmA;
                case (int)ESpecialBox.CloseChatB: return rectChatLineConfirmB;
                default: break;
            }

            return Rectangle.Empty;
        }

        protected void GetAverageChestColor(FastBitmapHSV bitmap, Point chestPos, out int avgHue, out int avgSat)
        {
            float scale = 1.0f / (rectChestArea.Width * rectChestArea.Height);
            float accHue = 0;
            float accSat = 0;

            for (int idxX = 0; idxX < rectChestArea.Width; idxX++)
            {
                for (int idxY = 0; idxY < rectChestArea.Height; idxY++)
                {
                    FastPixelHSV testPx = bitmap.GetPixel(chestPos.X + rectChestArea.X + idxX, chestPos.Y + rectChestArea.Y + idxY);
                    accHue += testPx.GetHue();
                    accSat += testPx.GetSaturation();
                }
            }

            avgHue = (int)(accHue * scale);
            avgSat = (int)(accSat * scale);
        }

        protected bool HasRewardChests(FastBitmapHSV bitmap)
        {
            GetAverageChestColor(bitmap, posChestG, out int avgHueG, out int avgSatG);
            GetAverageChestColor(bitmap, posChestS, out int avgHueS, out int avgSatS);
            GetAverageChestColor(bitmap, posChestB, out int avgHueB, out int avgSatB);

            bool hasGold = (avgHueG < 45) && (avgHueG > 25) && (avgSatG < 60) && (avgSatG > 40);
            bool hasSilver = (avgHueS < 45) && (avgHueS > 25) && (avgSatS < 30) && (avgSatS > -1);
            bool hasBronze = (avgHueB < 30) && (avgHueB > 5) && (avgSatB < 60) && (avgSatB > 30);

            bool hasMatch = hasGold && hasSilver && hasBronze;

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} HasRewardChests: {1}", ScannerName, hasMatch);
            }
            if (DebugLevel >= EDebugLevel.Verbose)
            {
                Console.WriteLine("  gold: avgH:{0} avgS:{1} -> {2}", avgHueG, avgSatG, hasGold);
                Console.WriteLine("  silver: avgH:{0} avgS:{1} -> {2}", avgHueS, avgSatS, hasSilver);
                Console.WriteLine("  bronze: avgH:{0} avgS:{1} -> {2}", avgHueB, avgSatB, hasBronze);
            }

            return hasMatch;
        }

        private void ScanSpecialAction(FastBitmapHSV bitmap, ScreenData screenData)
        {
            FastPixelHSV[] samples = FindSpecialActionButton(bitmap);
            if (samples != null)
            {
                for (int idx = 0; idx < samples.Length; idx++)
                {
                    bool hasMatch = matchSpecialReload.IsMatching(samples[idx]);
                    if (hasMatch)
                    {
                        screenData.specialAction = ESpecialAction.Reload;
                        break;
                    }
                }
            }

            if (DebugLevel >= EDebugLevel.Simple)
            {
                Console.WriteLine("{0} ScanSpecialAction: {1}", ScannerName, screenData.specialAction);
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
            }
        }
    }
}
