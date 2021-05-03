using SINoVision;
using System;
using System.Collections.Generic;

namespace SINoCOLO
{
    class TrackerActionBoost
    {
        private const int upkeepDuration = 50;    // 5s

        private int[] boostedElement;
        private int[] boostedClass;
        private int[,] upkeepMap;

        private int numElements = 1;
        private int numClasses = 1;
        private bool isActive = false;

        public TrackerActionBoost()
        {
            numElements = Enum.GetNames(typeof(ScannerCombatBase.EElementType)).Length;
            numClasses = Enum.GetNames(typeof(ScannerCombatBase.EWeaponType)).Length;

            boostedElement = new int[numElements];
            boostedClass = new int[numClasses];
            upkeepMap = new int[numElements, numClasses];
        }

        public void Tick()
        {
            // ignore idxE, idxC = 0 (enum: none)
            for (int idxE = 1; idxE < numElements; idxE++)
            {
                for (int idxC = 1; idxC < numClasses; idxC++)
                {
                    upkeepMap[idxE, idxC] = (upkeepMap[idxE, idxC] > 0) ? (upkeepMap[idxE, idxC] - 1) : 0;
                }
            }

            UpdateBoostState();
        }

        public void UpdateActions(ScannerCombatBase.ActionData[] actions)
        {
            foreach (var action in actions)
            {
                if (action.hasBoost)
                {
                    upkeepMap[(int)action.element, (int)action.weaponClass] = upkeepDuration;
                }
            }

            UpdateBoostState();
        }

        public bool IsBoosted(ScannerCombatBase.ActionData action)
        {
            return action.hasBoost ||
                boostedElement[(int)action.element] > 0 ||
                boostedClass[(int)action.weaponClass] > 0;
        }

        public void AppendDetails(List<string> lines)
        {
            if (isActive)
            {
                string desc = "";

                for (int idxE = 0; idxE < boostedElement.Length; idxE++)
                {
                    if (boostedElement[idxE] > 0)
                    {
                        if (desc.Length > 0) { desc += ", "; }
                        desc += string.Format("{0} ({1})", (ScannerCombatBase.EElementType)idxE, boostedElement[idxE]);
                    }
                }

                for (int idxC = 0; idxC < boostedClass.Length; idxC++)
                {
                    if (boostedClass[idxC] > 0)
                    {
                        if (desc.Length > 0) { desc += ", "; }
                        desc += string.Format("{0} ({1})", (ScannerCombatBase.EWeaponType)idxC, boostedClass[idxC]);
                    }
                }

                lines.Add("Boost: " + desc);
            }
        }

        public bool GetBoostDesc(out ScannerColoCombat.EElementType bestElem)
        {
            bestElem = ScannerCombatBase.EElementType.Unknown;

            int maxV = 0;
            for (int idxE = 0; idxE < boostedElement.Length; idxE++)
            {
                if (maxV < boostedElement[idxE])
                {
                    maxV = boostedElement[idxE];
                    bestElem = (ScannerCombatBase.EElementType)idxE;
                }
            }

            return isActive;
        }

        private void UpdateBoostState()
        {
            int[] maxElem = new int[numElements];
            int numBoostedElements = 0;

            int[] maxClass = new int[numClasses];
            int numBoostedClasses = 0;

            // ignore idxE, idxC = 0 (unknown enum values)
            for (int idxE = 1; idxE < numElements; idxE++)
            {
                for (int idxC = 1; idxC < numClasses; idxC++)
                {
                    if (upkeepMap[idxE, idxC] > 0)
                    {
                        if (maxElem[idxE] == 0)
                        {
                            maxElem[idxE] = Math.Max(maxElem[idxE], upkeepMap[idxE, idxC]);
                            numBoostedElements++;
                        }

                        if (maxClass[idxC] == 0)
                        {
                            maxClass[idxC] = Math.Max(maxClass[idxC], upkeepMap[idxE, idxC]);
                            numBoostedClasses++;
                        }
                    }
                }
            }

            Array.Clear(boostedElement, 0, boostedElement.Length);
            Array.Clear(boostedClass, 0, boostedClass.Length);

            isActive = numBoostedClasses > 0 || numBoostedElements > 0;
            if (isActive)
            {
                int numTotal = numBoostedClasses + numBoostedElements;
                if (numTotal > 1)
                {
                    if (numBoostedElements == 1)
                    {
                        // multiple classes + single element => element boost

                        for (int idxE = 0; idxE < maxElem.Length; idxE++)
                        {
                            boostedElement[idxE] = maxElem[idxE];
                        }
                    }
                    else
                    {
                        // multiple elements + single class => class boost
                        // multiple elements and classes? => class boost

                        for (int idxC = 0; idxC < maxClass.Length; idxC++)
                        {
                            boostedClass[idxC] = maxClass[idxC];
                        }
                    }
                }
                else
                {
                    // single entry => element boost
                    for (int idxE = 0; idxE < maxElem.Length; idxE++)
                    {
                        boostedElement[idxE] = maxElem[idxE];
                    }
                }
            }
        }
    }
}
