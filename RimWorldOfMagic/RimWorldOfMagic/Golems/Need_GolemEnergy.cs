using RimWorld;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TorannMagic.Golems
{
    public class Need_GolemEnergy : Need  
    {
        public int lastTick = -999;

        private float needGain = 1f;
        private float needLoss = 1f;

        public float maxEnergyBoost = 0f;
        public float energyRegenBoost = 0f;

        public float NeedLoss => needLoss - energyRegenBoost;

        public override float MaxLevel
        {
            get
            {
                return 1000f + maxEnergyBoost; 
            }
        }

        public GolemEnergyCategory CurCategory
        {
            get
            {
                bool flag = this.CurLevel < 250f;
                GolemEnergyCategory result;
                if (flag)
                {
                    result = GolemEnergyCategory.Low;
                }
                else
                {
                    bool flag2 = this.CurLevel < 750f;
                    if (flag2)
                    {
                        result = GolemEnergyCategory.Medium;
                    }
                    else
                    {
                        result = GolemEnergyCategory.High;
                    }
                }
                return result;
            }
        }

        static Need_GolemEnergy()
        {
        }

        public Need_GolemEnergy(Pawn pawn) : base(pawn)
		{
            this.lastTick = -999;
            this.threshPercents = new List<float>();
            this.threshPercents.Add((250f / this.MaxLevel));
            this.threshPercents.Add((500f / this.MaxLevel));
            this.threshPercents.Add((750f / this.MaxLevel));
        }

        public override void SetInitialLevel()
        {
            CurLevel = 0f;
        }

        public override void NeedInterval()
        {
            if(!base.IsFrozen)
            {
                CurLevel -= NeedLoss;
            }
        }       

        public bool InCaravan(Pawn p)
        {
            if(p.Map == null)
            {
                if (p.ParentHolder.ToString().Contains("Caravan"))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public enum GolemEnergyCategory
    {
        Low,
        Medium,
        High
    }
}
