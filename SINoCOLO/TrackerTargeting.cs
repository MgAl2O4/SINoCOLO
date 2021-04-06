using System;
using System.Collections.Generic;
using System.Drawing;

namespace SINoCOLO
{
    class TrackerTargeting
    {
        public Rectangle rectNoTarget;
        public Rectangle[] rectTargets;
        public Random randGen;

        private int[] rotationPatternAll = { 0, 1, 2, 3, 4 };
        private int[] rotationPattern3 = { 0, 1, 2 };
        private int rotationIdx = -1;
        private int delay = 0;
        private GameLogic.ETargetingMode mode;
        private Rectangle pendingActionBox;

        public void Update()
        {
            if (mode == GameLogic.ETargetingMode.None)
            {
                return;
            }

            if (delay > 0)
            {
                delay--;
                return;
            }

            // default delay: 6..10s
            delay = randGen.Next(60, 100);

            switch (mode)
            {
                case GameLogic.ETargetingMode.Deselect:
                    delay = randGen.Next(100, 150); // 10..15s delay for removing accidental locks
                    pendingActionBox = rectNoTarget;
                    break;

                case GameLogic.ETargetingMode.LockStrongest:
                    pendingActionBox = rectTargets[0];
                    break;

                case GameLogic.ETargetingMode.CycleAll:
                    rotationIdx = (rotationIdx + 1) % rotationPatternAll.Length;
                    pendingActionBox = rectTargets[rotationPatternAll[rotationIdx]];
                    break;

                case GameLogic.ETargetingMode.CycleTop3:
                    rotationIdx = (rotationIdx + 1) % rotationPattern3.Length;
                    pendingActionBox = rectTargets[rotationPattern3[rotationIdx]];
                    break;

                default: break;
            }
        }

        public void Reset()
        {
            rotationIdx = -1;
            delay = 0;
            pendingActionBox = Rectangle.Empty;
        }

        public void SetMode(GameLogic.ETargetingMode mode)
        {
            this.mode = mode;
            Reset();
        }

        public bool GetAndConsumeAction(out Rectangle actionBox)
        {
            if (pendingActionBox.Width > 0)
            {
                actionBox = pendingActionBox;
                pendingActionBox = Rectangle.Empty;
                return true;
            }

            actionBox = Rectangle.Empty;
            return false;
        }

        public void AppendDetails(List<string> lines, string trackerName)
        {
            string desc = string.Format("Target {0}> {1} ", trackerName, mode);
            if (mode != GameLogic.ETargetingMode.None)
            {
                desc += ", wait:" + delay;
            }
            if (mode == GameLogic.ETargetingMode.CycleAll || mode == GameLogic.ETargetingMode.CycleTop3)
            {
                desc += string.Format(", cycle {0}/{1}",
                    rotationIdx,
                    mode == GameLogic.ETargetingMode.CycleAll ? rotationPatternAll.Length : rotationPattern3.Length);
            }

            lines.Add(desc);
        }
    }
}
