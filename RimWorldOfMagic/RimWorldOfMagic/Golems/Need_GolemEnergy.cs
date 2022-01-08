using RimWorld;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace TorannMagic.Golems
{
    public class Need_GolemEnergy : Need  
    {
        public int lastTick = -999;

        private float needGain = 1f;
        private float needLoss = 1f;

        public float maxEnergy = 1000f;
        public float energyEfficiency = .5f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.maxEnergy, "maxEnergy", 1000);
            Scribe_Values.Look<float>(ref this.energyEfficiency, "energyEfficiency", .5f);
        }

        public float NeedLoss => needLoss - energyEfficiency;

        public override float MaxLevel
        {
            get
            {
                return maxEnergy; 
            }
        }

        public void AddEnergy(float amount)
        {
            CurLevel += (amount * energyEfficiency);
        }

        public void SubtractEnergy(float amount)
        {
            CurLevel -= (ActualNeedCost(amount));
        }

        public float ActualNeedCost(float amount)
        {
            return amount / energyEfficiency;
        }

        public float CurEnergyPercent => Mathf.Clamp01(CurLevel / MaxLevel);

        public GolemEnergyCategory CurCategory
        {
            get
            {
                bool flag = this.CurLevel < 100f;
                GolemEnergyCategory result;
                if (flag)
                {
                    result = GolemEnergyCategory.Critical;
                }
                else
                {
                    bool flag2 = this.CurLevel < 250f;
                    if (flag2)
                    {
                        result = GolemEnergyCategory.Low;
                    }
                    else if(this.CurLevel < 750f)
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
            this.threshPercents.Add((100f / this.MaxLevel));
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
                if (InCaravan(pawn))
                {
                    Caravan car = pawn.ParentHolder as Caravan;
                    if(car.NightResting)
                    {
                        CurLevel += ActualNeedCost(NeedLoss);
                    }
                    else
                    {
                        CurLevel -= ActualNeedCost(NeedLoss);
                    }
                }
                else
                {
                    CurLevel -= ActualNeedCost(NeedLoss);
                }
            }
        }

        public bool InCaravan(Pawn p)
        {
            if (p.Map == null)
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
        Critical,
        Low,
        Medium,
        High
    }
}
