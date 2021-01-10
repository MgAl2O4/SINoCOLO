using SINoVision;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SINoCOLO
{
    class GameLogic
    {
        public delegate void MouseClickDelegate(int posX, int posY);
        public event MouseClickDelegate OnMouseClickRequested;
        private Random randGen = new Random();

        public ScannerBase screenScanner;
        public object screenData;

        private ScannerColoCombat.ScreenData cachedDataColoCombat;
        private ScannerColoPurify.ScreenData cachedDataColoPurify;
        private ScannerCombat.ScreenData cachedDataCombat;
        private ScannerMessageBox.ScreenData cachedDataMessageBox;

        public int slotIdx = -1;
        public int specialIdx = -1;
        private float interpActiveFill = 0.0f;
        private int scanSkipCounter = 0;
        private int purifySlot = 0;
        private int[] boostUpkeep = new int[] { 0, 0, 0, 0 };
        private int boostUpkeepTicks = 80; // 8s

        private Font overlayFont = new Font(FontFamily.GenericSansSerif, 7.0f);
        private Color colorPaletteRed = Color.FromArgb(0xff, 0xad, 0xad);
        private Color colorPaletteGreen = Color.FromArgb(0xca, 0xff, 0xbf);
        private Color colorPaletteBlue = Color.FromArgb(0x9b, 0xf6, 0xff);
        private Color colorPaletteYellow = Color.FromArgb(0xfd, 0xff, 0xb6);
        private Color colorPaletteActive = Color.FromArgb(0xff, 0xc6, 0xff);

        public enum EState
        {
            Unknown,
            ColoCombat,
            ColoPurify,
            Combat,
            MessageBox,
        }
        private EState state;

        private void OnStateChanged()
        {
            slotIdx = -1;
            specialIdx = -1;
            scanSkipCounter = 0;
            // don't clear purify slot
        }

        public void OnScanPrep()
        {
            screenScanner = null;
            screenData = null;

            interpActiveFill -= (float)Math.Truncate(interpActiveFill);
            interpActiveFill += 0.25f;

            for (int idx = 0; idx < boostUpkeep.Length; idx++)
            {
                if (boostUpkeep[idx] > 0)
                {
                    boostUpkeep[idx]--;
                }
            }
        }

        public void OnScan()
        {
            bool handled = false;
            handled = handled || OnScan_ColoCombat(screenData as ScannerColoCombat.ScreenData);
            handled = handled || OnScan_ColoPurify(screenData as ScannerColoPurify.ScreenData);
            handled = handled || OnScan_MessageBox(screenData as ScannerMessageBox.ScreenData);
            handled = handled || OnScan_Combat(screenData as ScannerCombat.ScreenData);

            if (!handled)
            {
                state = EState.Unknown;
                OnStateChanged();
            }
        }

        public void DrawScanHighlights(Graphics g)
        {
            bool handled = false;
            handled = handled || DrawScanHighlights_ColoCombat(g, screenData as ScannerColoCombat.ScreenData);
            handled = handled || DrawScanHighlights_ColoPurify(g, screenData as ScannerColoPurify.ScreenData);
            handled = handled || DrawScanHighlights_MessageBox(g, screenData as ScannerMessageBox.ScreenData);
            handled = handled || DrawScanHighlights_Combat(g, screenData as ScannerCombat.ScreenData);

            if (!handled)
            {
                Rectangle[] actionBoxes = screenScanner.GetActionBoxes();
                if (actionBoxes != null)
                {
                    for (int idx = 0; idx < actionBoxes.Length; idx++)
                    {
                        string actionDesc = "ACTION #" + idx;
                        DrawActionArea(g, actionBoxes[idx], actionDesc, colorPaletteBlue, slotIdx == idx);
                    }
                }

                for (int idx = 0; idx < 16; idx++)
                {
                    Rectangle specialBox = screenScanner.GetSpecialActionBox(idx);
                    if (specialBox.Width <= 0)
                    {
                        break;
                    }

                    string actionDesc = "SPECIAL #" + idx;
                    DrawActionArea(g, specialBox, actionDesc, colorPaletteBlue, specialIdx == idx);
                }
            }
        }

        private void RequestMouseClick(Rectangle actionBox, int newSlotIdx, int newSpecialIdx)
        {
            slotIdx = newSlotIdx;
            specialIdx = newSpecialIdx;

            int posX = actionBox.X + randGen.Next(0, actionBox.Width);
            int posY = actionBox.Y + randGen.Next(0, actionBox.Height);
            OnMouseClickRequested.Invoke(posX, posY);
        }

        private void DrawActionArea(Graphics g, Rectangle bounds, string desc, Color color, bool isActive)
        {
            Pen usePen = new Pen(color);
            Brush useBrush = new SolidBrush(color);
            float textBarHeight = 12.0f;

            if (isActive)
            {
                Brush activeBrush = new SolidBrush(Color.FromArgb((int)(128 * interpActiveFill), colorPaletteActive));
                g.FillRectangle(activeBrush, bounds);
            }

            bounds.Inflate(1, 1);
            g.DrawRectangle(usePen, bounds);
            g.FillRectangle(useBrush, bounds.X, bounds.Y - textBarHeight, bounds.Width + 1, textBarHeight);

            g.DrawString(desc, overlayFont, Brushes.Black, bounds.X + 1.0f, bounds.Y - textBarHeight);
        }

        public void AppendDetails(List<string> lines)
        {
            lines.Add(string.Format("Logic:{0}, delay:{1}{2}",
                state, scanSkipCounter, scanSkipCounter <= 1 ? " (click)" : ""));

            string boostDesc = "";
            for (int idx = 0; idx < boostUpkeep.Length; idx++)
            {
                var elemType = (ScannerCombatBase.EElementType)idx;
                if (elemType != ScannerCombatBase.EElementType.Unknown && boostUpkeep[idx] > 0)
                {
                    if (boostDesc.Length > 0) { boostDesc += ", "; }
                    boostDesc += elemType.ToString();
                    
                    if (boostUpkeep[idx] < boostUpkeepTicks)
                    {
                        boostDesc += string.Format(" (fading: {0})", boostUpkeep[idx]);
                    }
                }
            }

            if (cachedDataCombat != null || cachedDataColoCombat != null || boostDesc.Length > 0)
            {
                lines.Add("Boost: " + (boostDesc.Length > 0 ? boostDesc : "n/a"));
            }

            // cached data status
            string scannerNamePrefix = "Scanner";

            var cachedScreenData = new object[] { cachedDataCombat, cachedDataColoCombat, cachedDataColoPurify, cachedDataMessageBox };
            foreach (var item in cachedScreenData)
            {
                if (item != null)
                {
                    var scannerTypeName = item.GetType().DeclaringType.Name;
                    if (scannerTypeName.StartsWith(scannerNamePrefix)) { scannerTypeName = scannerTypeName.Remove(0, scannerNamePrefix.Length); }

                    lines.Add("");
                    lines.Add("Cached " + scannerTypeName + ":");
                    string[] tokens = item.ToString().Split('\n');
                    lines.AddRange(tokens);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private bool OnScan_ColoCombat(ScannerColoCombat.ScreenData screenData)
        {
            if (screenData == null) { return false; }
            if (state != EState.ColoCombat)
            {
                state = EState.ColoCombat;
                OnStateChanged();
            }

            cachedDataColoCombat = screenData;
            cachedDataCombat = null;
            cachedDataMessageBox = null;
            scanSkipCounter--;
            if (scanSkipCounter > 0)
            {
                return true;
            }

            // random delay: 0.5..0.8s between action presses (OnScan interval = 100ms)
            scanSkipCounter = randGen.Next(5, 8);
            Rectangle actionBox;

            // priority check: low SP = purify
            float SPPct = (screenData.SPIsValid && !screenData.SPIsObstructed) ? screenData.SPFillPct : 1.0f;
            float SPPctUnsafe = screenData.SPIsValid ? screenData.SPFillPct : 1.0f;
            if (SPPct < 0.3f)
            {
                specialIdx = (int)ScannerColoCombat.ESpecialBox.EnterPurify;
                actionBox = screenScanner.GetSpecialActionBox(specialIdx);
                RequestMouseClick(actionBox, -1, specialIdx);
                return true;
            }

            // priority check: demon soon = purify if needed
            if ((screenData.demonState == ScannerColoCombat.EDemonState.Preparing) && (SPPct < 0.7f))
            {
                specialIdx = (int)ScannerColoCombat.ESpecialBox.EnterPurify;
                actionBox = screenScanner.GetSpecialActionBox(specialIdx);
                RequestMouseClick(actionBox, -1, specialIdx);
                return true;
            }

            // if big button is showing up:
            // - click it on reload
            switch (screenData.specialAction)
            {
                case ScannerColoCombat.ESpecialAction.Reload:
                    specialIdx = (int)ScannerColoCombat.ESpecialBox.BigButton;
                    actionBox = screenScanner.GetSpecialActionBox(specialIdx);
                    RequestMouseClick(actionBox, -1, specialIdx);
                    return true;

                case ScannerColoCombat.ESpecialAction.Revive:
                    if (SPPctUnsafe < 0.99f)
                    {
                        specialIdx = (int)ScannerColoCombat.ESpecialBox.EnterPurify;
                        actionBox = screenScanner.GetSpecialActionBox(specialIdx);
                        RequestMouseClick(actionBox, -1, specialIdx);
                    }
                    return true;

                case ScannerColoCombat.ESpecialAction.AttackShip:
                    if (SPPctUnsafe < 0.7f)
                    {
                        specialIdx = (int)ScannerColoCombat.ESpecialBox.EnterPurify;
                        actionBox = screenScanner.GetSpecialActionBox(specialIdx);
                        RequestMouseClick(actionBox, -1, specialIdx);
                    }
                    else
                    {
                        specialIdx = (int)ScannerColoCombat.ESpecialBox.BigButton;
                        actionBox = screenScanner.GetSpecialActionBox(specialIdx);
                        RequestMouseClick(actionBox, -1, specialIdx);
                    }
                    return true;

                default:
                    break;
            }

            // otherwise, just click actions random valid actions
            {
                var prioritySlotsDemon = new List<int>();
                var prioritySlotsBoost = new List<int>();
                var validSlots = new List<int>();
                for (int idx = 0; idx < screenData.actions.Length; idx++)
                {
                    if (screenData.actions[idx].isValid)
                    {
                        validSlots.Add(idx);

                        if (screenData.actions[idx].element != ScannerCombatBase.EElementType.Unknown)
                        {
                            if (screenData.actions[idx].hasBoost)
                            {
                                boostUpkeep[(int)screenData.actions[idx].element] = boostUpkeepTicks;
                                prioritySlotsBoost.Add(idx);
                            }
                            else if (boostUpkeep[(int)screenData.actions[idx].element] > 0)
                            {
                                prioritySlotsBoost.Add(idx);
                            }
                        }
                    }

                    if (screenData.demonState == ScannerColoCombat.EDemonState.Active &&
                        screenData.demonType == screenData.actions[idx].weaponClass)
                    {
                        prioritySlotsDemon.Add(idx);
                    }
                }

                var slotList =
                    (prioritySlotsDemon.Count > 0) ? prioritySlotsDemon :
                    (prioritySlotsBoost.Count > 0) ? prioritySlotsBoost :
                    validSlots;

                if (slotList.Count > 0)
                {
                    Rectangle[] actionBoxes = screenScanner.GetActionBoxes();
                    slotIdx = slotList[randGen.Next(slotList.Count)];
                    RequestMouseClick(actionBoxes[slotIdx], slotIdx, -1);
                    return true;
                }
            }

            // no valid slots to click on, ignore and wait for next scan
            return true;
        }

        private bool DrawScanHighlights_ColoCombat(Graphics g, ScannerColoCombat.ScreenData screenData)
        {
            if (screenData == null) { return false; }

            Rectangle bigButtonBox = screenScanner.GetSpecialActionBox((int)ScannerColoCombat.ESpecialBox.BigButton);
            bool isActiveBigButton = specialIdx == (int)ScannerColoCombat.ESpecialBox.BigButton;
            switch (screenData.specialAction)
            {
                case ScannerColoCombat.ESpecialAction.Reload:
                    DrawActionArea(g, bigButtonBox, "RELOAD", colorPaletteYellow, isActiveBigButton);
                    break;

                case ScannerColoCombat.ESpecialAction.Revive:
                    DrawActionArea(g, bigButtonBox, "REVIVE", colorPaletteGreen, isActiveBigButton);
                    break;

                case ScannerColoCombat.ESpecialAction.AttackShip:
                    DrawActionArea(g, bigButtonBox, "SHIP", colorPaletteRed, isActiveBigButton);
                    break;

                default: break;
            }

            Rectangle purifyBox = screenScanner.GetSpecialActionBox((int)ScannerColoCombat.ESpecialBox.EnterPurify);
            bool isActivePurify = specialIdx == (int)ScannerColoCombat.ESpecialBox.EnterPurify;
            DrawActionArea(g, purifyBox, string.Format("SP: {0:P0}", screenData.SPFillPct), colorPaletteBlue, isActivePurify);

            // not really an action area, but show regardless:
            // - demon state
            switch (screenData.demonState)
            {
                case ScannerColoCombat.EDemonState.Active:
                    DrawActionArea(g, new Rectangle(144, 86, 51, 51), screenData.demonType.ToString(), colorPaletteYellow, false);
                    break;

                case ScannerColoCombat.EDemonState.Preparing:
                    DrawActionArea(g, new Rectangle(83, 73, 171, 21), "DEMON SOON", colorPaletteYellow, false);
                    break;

                default: break;
            }

            // - NM elemental boost
            const int idxFire = (int)ScannerCombatBase.EElementType.Fire;
            const int idxWater = (int)ScannerCombatBase.EElementType.Water;
            const int idxWind = (int)ScannerCombatBase.EElementType.Wind;

            var boostColor =
                (boostUpkeep[idxFire] > boostUpkeep[idxWater]) && (boostUpkeep[idxFire] > boostUpkeep[idxWind]) ? colorPaletteRed :
                (boostUpkeep[idxWater] > boostUpkeep[idxFire]) && (boostUpkeep[idxWater] > boostUpkeep[idxWind]) ? colorPaletteBlue :
                (boostUpkeep[idxWind] > boostUpkeep[idxFire]) && (boostUpkeep[idxWind] > boostUpkeep[idxWater]) ? colorPaletteGreen :
                Color.White;

            if (boostColor != Color.White)
            {
                DrawActionArea(g, new Rectangle(7, 371, 53, 53), "BOOST", boostColor, false);
            }

            // weapon icons
            Rectangle[] actionBoxes = screenScanner.GetActionBoxes();
            for (int idx = 0; idx < actionBoxes.Length; idx++)
            {
                ScannerCombatBase.ActionData actionData = screenData.actions[idx];
                if (actionData.isValid)
                {
                    Color actionColor =
                        (actionData.element == ScannerCombatBase.EElementType.Fire) ? colorPaletteRed :
                        (actionData.element == ScannerCombatBase.EElementType.Wind) ? colorPaletteGreen :
                        (actionData.element == ScannerCombatBase.EElementType.Water) ? colorPaletteBlue :
                        Color.White;

                    DrawActionArea(g, actionBoxes[idx], actionData.weaponClass.ToString(), actionColor, slotIdx == idx);
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private bool OnScan_ColoPurify(ScannerColoPurify.ScreenData screenData)
        {
            if (screenData == null) { return false; }
            if (state != EState.ColoPurify)
            {
                state = EState.ColoPurify;
                OnStateChanged();

                // when returning to purify state, check if there's burst ready with at least 1 big on screen
                // enforce longer delay so stuff can spawn back in and be detected
                if (cachedDataColoPurify != null &&
                    (cachedDataColoPurify.BurstState == ScannerColoPurify.EBurstState.Ready || cachedDataColoPurify.BurstState == ScannerColoPurify.EBurstState.ReadyAndCenter))
                {
                    int numPrevBig = 0;
                    foreach (var slot in cachedDataColoPurify.Slots)
                    {
                        numPrevBig += (slot == ScannerColoPurify.ESlotType.Big) ? 1 : 0;
                    }

                    if (numPrevBig >= 1)
                    {
                        scanSkipCounter = 30;
                    }
                }
            }

            cachedDataColoPurify = screenData;
            cachedDataMessageBox = null;

            // don't do anything when burst is already active
            if (screenData.BurstState == ScannerColoPurify.EBurstState.Active)
            {
                purifySlot = 0;
                return true;
            }

            scanSkipCounter--;
            if (scanSkipCounter > 0)
            {
                return true;
            }

            scanSkipCounter = randGen.Next(2, 5);

            const float burstBigPct = 90.0f / 400.0f;
            int numSmall = 0;
            int numBig = 0;
            int numLocked = 0;
            int numLockedBig = 0;
            foreach (var slot in screenData.Slots)
            {
                numSmall += (slot == ScannerColoPurify.ESlotType.Small) ? 1 : 0;
                numBig += (slot == ScannerColoPurify.ESlotType.Big) ? 1 : 0;
                numLocked += (slot == ScannerColoPurify.ESlotType.Locked) ? 1 : 0;
                numLockedBig += (slot == ScannerColoPurify.ESlotType.LockedBig) ? 1 : 0;
            }

            int numTotal = numSmall + numBig + numLocked + numLockedBig;

            // if about to transition to next scene (or in flight) and burst is ready, wait a bit longer
            // to make sure everything spawns in before decision to use burst
            if (numTotal == 0 && (screenData.BurstState != ScannerColoPurify.EBurstState.None))
            {
                scanSkipCounter = 15;   // 1.5s
                return true;
            }

            // if burst is ready, check if there's anything worth using it on
            if (screenData.BurstState == ScannerColoPurify.EBurstState.ReadyAndCenter && (numBig > 0))
            {
                float SPAfterBurst = (screenData.SPIsValid ? screenData.SPFillPct : 1.0f) + (numBig * burstBigPct);
                bool hasDemon = (cachedDataColoCombat != null) && (cachedDataColoCombat.demonState != ScannerColoCombat.EDemonState.None);
                bool shouldUseBurst = (SPAfterBurst < 0.99) || hasDemon;

                specialIdx = (int)(shouldUseBurst ? ScannerColoPurify.ESpecialBox.BurstReady : ScannerColoPurify.ESpecialBox.ReturnToBattle);
                Rectangle actionBox = screenScanner.GetSpecialActionBox(specialIdx);
                RequestMouseClick(actionBox, -1, specialIdx);
                return true;
            }

            // alternative: if burst is ready but not centered and there are mobs worth using it on - fire
            if (screenData.BurstState == ScannerColoPurify.EBurstState.Ready && (numBig > 0) && (numTotal >= 5))
            {
                float SPAfterBurst = (screenData.SPIsValid ? screenData.SPFillPct : 1.0f) + (numBig * burstBigPct);
                bool shouldUseBurst = (SPAfterBurst < 0.95);

                specialIdx = (int)(shouldUseBurst ? ScannerColoPurify.ESpecialBox.BurstReady : ScannerColoPurify.ESpecialBox.ReturnToBattle);
                Rectangle actionBox = screenScanner.GetSpecialActionBox(specialIdx);
                RequestMouseClick(actionBox, -1, specialIdx);
                return true;
            }

            // click on first available valid slot, going clockwise, starting from last used (skip over locked ones)
            for (int idx = 0; idx < 8; idx++)
            {
                if (screenData.Slots[purifySlot] == ScannerColoPurify.ESlotType.Small ||
                    screenData.Slots[purifySlot] == ScannerColoPurify.ESlotType.Big)
                {
                    Rectangle[] actionBoxes = screenScanner.GetActionBoxes();
                    slotIdx = purifySlot;
                    RequestMouseClick(actionBoxes[slotIdx], slotIdx, -1);
                    return true;
                }

                purifySlot = (purifySlot + 1) % 8;
            }

            // no valid slots detected? ignore for now
            // maybe click every 0.5s on random one?
            return true;
        }

        private bool DrawScanHighlights_ColoPurify(Graphics g, ScannerColoPurify.ScreenData screenData)
        {
            if (screenData == null) { return false; }

            if (screenData.BurstState == ScannerColoPurify.EBurstState.ReadyAndCenter)
            {
                Rectangle burstBox = screenScanner.GetSpecialActionBox((int)ScannerColoPurify.ESpecialBox.BurstCenter);
                DrawActionArea(g, burstBox, "BURST", colorPaletteGreen, specialIdx == (int)ScannerColoPurify.ESpecialBox.BurstCenter);
            }
            else if (screenData.BurstState == ScannerColoPurify.EBurstState.Ready)
            {
                Rectangle burstBox = screenScanner.GetSpecialActionBox((int)ScannerColoPurify.ESpecialBox.BurstReady);
                DrawActionArea(g, burstBox, "BURST", colorPaletteGreen, specialIdx == (int)ScannerColoPurify.ESpecialBox.BurstReady);
            }

            if (screenData.BurstState == ScannerColoPurify.EBurstState.Active)
            {
                // not really an action area, but show regardless
                Rectangle labelBox = new Rectangle(0, 60, 1000, 1000);
                DrawActionArea(g, labelBox, "BURST IN PROGRESS", colorPaletteRed, false);
            }
            else
            {
                Rectangle returnBox = screenScanner.GetSpecialActionBox((int)ScannerColoPurify.ESpecialBox.ReturnToBattle);
                string returnDesc = string.Format("SP: {0:P0}", screenData.SPFillPct);
                DrawActionArea(g, returnBox, returnDesc, colorPaletteYellow, specialIdx == (int)ScannerColoPurify.ESpecialBox.ReturnToBattle);

                Rectangle[] actionBoxes = screenScanner.GetActionBoxes();
                for (int idx = 0; idx < actionBoxes.Length; idx++)
                {
                    var slotType = screenData.Slots[idx];
                    if (slotType != ScannerColoPurify.ESlotType.None)
                    {
                        string desc = slotType.ToString();
                        Color actionColor = Color.White;

                        switch (slotType)
                        {
                            case ScannerColoPurify.ESlotType.Small:
                                actionColor = colorPaletteGreen;
                                break;
                            case ScannerColoPurify.ESlotType.Big:
                                actionColor = colorPaletteBlue;
                                break;
                            case ScannerColoPurify.ESlotType.Locked:
                                actionColor = colorPaletteYellow;
                                desc = "Lock";
                                break;
                            case ScannerColoPurify.ESlotType.LockedBig:
                                actionColor = colorPaletteYellow;
                                desc = "Lock";
                                break;
                            default: break;
                        }

                        DrawActionArea(g, actionBoxes[idx], desc, actionColor, slotIdx == idx);
                    }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private bool OnScan_MessageBox(ScannerMessageBox.ScreenData screenData)
        {
            if (screenData == null) { return false; }
            if (state != EState.MessageBox)
            {
                state = EState.MessageBox;
                OnStateChanged();
            }

            cachedDataMessageBox = screenData;

            scanSkipCounter--;
            if (scanSkipCounter > 0)
            {
                return true;
            }

            // random delay: 0.5..0.8s between action presses (OnScan interval = 100ms)
            scanSkipCounter = randGen.Next(5, 8);
            specialIdx = -1;

            switch (screenData.mode)
            {
                case ScannerMessageBox.EMessageType.CombatReport:
                    if (screenData.actions[(int)ScannerMessageBox.EButtonPos.CombatReportRetry].buttonType == ScannerMessageBox.EButtonType.Retry)
                    {
                        scanSkipCounter = randGen.Next(25, 30);
                        specialIdx = (int)ScannerMessageBox.EButtonPos.CombatReportRetry;
                    }
                    break;

                case ScannerMessageBox.EMessageType.Ok:
                    specialIdx = (int)ScannerMessageBox.EButtonPos.Center;
                    break;

                case ScannerMessageBox.EMessageType.OkCancel:
                    specialIdx = (int)ScannerMessageBox.EButtonPos.CenterTwoRight;
                    break;

                default: break;
            }

            if (specialIdx >= 0)
            {
                RequestMouseClick(screenScanner.GetSpecialActionBox(specialIdx), -1, specialIdx);
            }

            return true;
        }

        private bool DrawScanHighlights_MessageBox(Graphics g, ScannerMessageBox.ScreenData screenData)
        {
            if (screenData == null) { return false; }

            Rectangle boxA = Rectangle.Empty, boxB = Rectangle.Empty;
            string desc = null;

            switch (screenData.mode)
            {
                case ScannerMessageBox.EMessageType.Ok:
                    boxA = screenScanner.GetSpecialActionBox((int)ScannerMessageBox.EButtonPos.Center);
                    DrawActionArea(g, boxA, "Ok", colorPaletteGreen, specialIdx == (int)ScannerMessageBox.EButtonPos.Center);
                    break;

                case ScannerMessageBox.EMessageType.OkCancel:
                    boxA = screenScanner.GetSpecialActionBox((int)ScannerMessageBox.EButtonPos.CenterTwoLeft);
                    boxB = screenScanner.GetSpecialActionBox((int)ScannerMessageBox.EButtonPos.CenterTwoRight);
                    DrawActionArea(g, boxA, "Cancel", colorPaletteYellow, specialIdx == (int)ScannerMessageBox.EButtonPos.CenterTwoLeft);
                    DrawActionArea(g, boxB, "Ok", colorPaletteGreen, specialIdx == (int)ScannerMessageBox.EButtonPos.CenterTwoRight);
                    break;

                case ScannerMessageBox.EMessageType.CombatReport:
                    boxA = screenScanner.GetSpecialActionBox((int)ScannerMessageBox.EButtonPos.CombatReportRetry);
                    boxB = screenScanner.GetSpecialActionBox((int)ScannerMessageBox.EButtonPos.CombatReportOk);
                    desc = screenData.actions[(int)ScannerMessageBox.EButtonPos.CombatReportRetry].buttonType.ToString();
                    DrawActionArea(g, boxA, desc, colorPaletteGreen, specialIdx == (int)ScannerMessageBox.EButtonPos.CombatReportRetry);
                    DrawActionArea(g, boxB, "Ok", colorPaletteYellow, specialIdx == (int)ScannerMessageBox.EButtonPos.CombatReportOk);
                    break;

                case ScannerMessageBox.EMessageType.Close:
                    boxA = screenScanner.GetSpecialActionBox((int)ScannerMessageBox.EButtonPos.Center);
                    DrawActionArea(g, boxA, "Close", colorPaletteYellow, specialIdx == (int)ScannerMessageBox.EButtonPos.Center);
                    break;

                default: break;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private bool OnScan_Combat(ScannerCombat.ScreenData screenData)
        {
            if (screenData == null) { return false; }
            if (state != EState.Combat)
            {
                state = EState.Combat;
                OnStateChanged();
            }

            cachedDataCombat = screenData;
            cachedDataColoCombat = null;
            cachedDataColoPurify = null;
            cachedDataMessageBox = null;

            scanSkipCounter--;
            if (scanSkipCounter > 0)
            {
                return true;
            }

            // random delay: 0.5..0.8s between action presses (OnScan interval = 100ms)
            scanSkipCounter = randGen.Next(5, 8);

            // if big button is showing up:
            // - click it on reload
            if (screenData.reloadActive)
            {
                specialIdx = 0;
                Rectangle actionBox = screenScanner.GetSpecialActionBox(specialIdx);
                RequestMouseClick(actionBox, -1, specialIdx);
                return true;
            }

            // otherwise, just click actions random valid actions
            {
                var prioritySlotsBoost = new List<int>();
                var validSlots = new List<int>();
                for (int idx = 0; idx < screenData.actions.Length; idx++)
                {
                    if (screenData.actions[idx].isValid)
                    {
                        validSlots.Add(idx);

                        if (screenData.actions[idx].element != ScannerCombatBase.EElementType.Unknown)
                        {
                            if (screenData.actions[idx].hasBoost)
                            {
                                boostUpkeep[(int)screenData.actions[idx].element] = boostUpkeepTicks;
                                prioritySlotsBoost.Add(idx);
                            }
                            else if (boostUpkeep[(int)screenData.actions[idx].element] > 0)
                            {
                                prioritySlotsBoost.Add(idx);
                            }
                        }
                    }
                }

                var slotList =
                    (prioritySlotsBoost.Count > 0) ? prioritySlotsBoost :
                    validSlots;

                if (slotList.Count > 0)
                {
                    Rectangle[] actionBoxes = screenScanner.GetActionBoxes();
                    slotIdx = slotList[randGen.Next(slotList.Count)];
                    RequestMouseClick(actionBoxes[slotIdx], slotIdx, -1);
                    return true;
                }
            }

            // no valid slots to click on, ignore and wait for next scan
            return true;
        }

        private bool DrawScanHighlights_Combat(Graphics g, ScannerCombat.ScreenData screenData)
        {
            if (screenData == null) { return false; }

            if (screenData.reloadActive)
            {
                Rectangle bigButtonBox = screenScanner.GetSpecialActionBox(0);
                bool isActiveBigButton = specialIdx == 0;
                DrawActionArea(g, bigButtonBox, "RELOAD", colorPaletteYellow, isActiveBigButton);
            }

            // not really an action area, but show regardless:
            // - NM elemental boost
            const int idxFire = (int)ScannerCombatBase.EElementType.Fire;
            const int idxWater = (int)ScannerCombatBase.EElementType.Water;
            const int idxWind = (int)ScannerCombatBase.EElementType.Wind;

            var boostColor =
                (boostUpkeep[idxFire] > boostUpkeep[idxWater]) && (boostUpkeep[idxFire] > boostUpkeep[idxWind]) ? colorPaletteRed :
                (boostUpkeep[idxWater] > boostUpkeep[idxFire]) && (boostUpkeep[idxWater] > boostUpkeep[idxWind]) ? colorPaletteBlue :
                (boostUpkeep[idxWind] > boostUpkeep[idxFire]) && (boostUpkeep[idxWind] > boostUpkeep[idxWater]) ? colorPaletteGreen :
                Color.White;

            if (boostColor != Color.White)
            {
                DrawActionArea(g, new Rectangle(9, 402, 49, 49), "BOOST", boostColor, false);
            }

            // weapon icons
            Rectangle[] actionBoxes = screenScanner.GetActionBoxes();
            for (int idx = 0; idx < actionBoxes.Length; idx++)
            {
                ScannerCombatBase.ActionData actionData = screenData.actions[idx];
                if (actionData.isValid)
                {
                    Color actionColor =
                        (actionData.element == ScannerCombatBase.EElementType.Fire) ? colorPaletteRed :
                        (actionData.element == ScannerCombatBase.EElementType.Wind) ? colorPaletteGreen :
                        (actionData.element == ScannerCombatBase.EElementType.Water) ? colorPaletteBlue :
                        Color.White;

                    DrawActionArea(g, actionBoxes[idx], actionData.weaponClass.ToString(), actionColor, slotIdx == idx);
                }
            }

            return true;
        }
    }
}
