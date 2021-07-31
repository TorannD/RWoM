using RimWorld;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public class Need_Travel : Need  
    {
        public int lastTravelTick = -999;

        private float needGain = 0.001f;
        private float needLoss = 0.00012f;

        public TravelCategory CurCategory
        {
            get
            {
                bool flag = this.CurLevel < 0.3f;
                TravelCategory result;
                if (flag)
                {
                    result = TravelCategory.Wanderlust;
                }
                else
                {
                    bool flag2 = this.CurLevel < 0.7f;
                    if (flag2)
                    {
                        result = TravelCategory.Complacent;
                    }
                    else
                    {
                        result = TravelCategory.Adventuring;
                    }
                }
                return result;
            }
        }

        static Need_Travel()
        {
        }

        public Need_Travel(Pawn pawn) : base(pawn)
		{
            this.lastTravelTick = -999;
            this.threshPercents = new List<float>();
            this.threshPercents.Add((0.3f / this.MaxLevel));
            this.threshPercents.Add((0.7f / this.MaxLevel));       
        }

        public override void SetInitialLevel()
        {
            CurLevel = Rand.Range(0.9f, 1f);
        }

        public override void NeedInterval()
        {
            if(!base.IsFrozen)
            {
                if(InCaravan(this.pawn))
                {
                    CurLevel += this.needGain;
                }
                else
                {
                    CurLevel -= this.needLoss;
                }
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
}
