using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld.Planet;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_SuppressiveAura : HediffComp
    {

        private bool initialized = false;
        private bool removeNow = false;

        private int eventFrequency = 500;
        private int burdenFrequency = 30000;

        private int pwrVal = 0;  //chance to allow good emotions increases from 1-3, less chance to acquire a negative thoughts periodically
        private int verVal = 0;  //increases joy and comfort at higher levels
        private int effVal = 0;  //radius, level 3 applies to caravans
        private float arcaneDmg = 1f;
        private float radius = 35f;

        public override void CompExposeData()
        {
            base.CompExposeData();
        }

        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            CompAbilityUserMagic comp = this.Pawn.GetCompAbilityUserMagic();
            if (spawned && comp != null && comp.IsMagicUser)
            {
                pwrVal = comp.MagicData.GetSkill_Power(TorannMagicDefOf.TM_SuppressiveAura).level;
                verVal = comp.MagicData.GetSkill_Versatility(TorannMagicDefOf.TM_SuppressiveAura).level;
                effVal = comp.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_SuppressiveAura).level;
                this.arcaneDmg = comp.arcaneDmg;
                radius = (35f + (5 * effVal)) * arcaneDmg;
                comp.MagicUserXP += Rand.RangeInclusive(1, 2);
            }
            else
            {
                this.removeNow = true;
            }
        }        

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null && base.Pawn.Map != null && !Pawn.InMentalState;
            if (flag)
            {
                if(Find.TickManager.TicksGame % this.burdenFrequency == 0)
                {
                    this.Pawn.needs.mood.thoughts.memories.TryGainMemory(TorannMagicDefOf.TM_EmotionalWeightTD);
                }
                if (Find.TickManager.TicksGame % this.eventFrequency == 0)
                {
                    this.Initialize();                    

                    if (base.Pawn.Map != null)
                    {
                        List<Pawn> pList = TM_Calc.FindAllPawnsAround(Pawn.Map, Pawn.Position, radius, Pawn.Faction, true);
                        if (pList != null && pList.Count > 0)
                        {
                            foreach (Pawn p in pList)
                            {
                                if (p != Pawn && !TM_Calc.IsEmpath(p) && Rand.Chance(TM_Calc.GetSpellSuccessChance(Pawn, p, true)))
                                {
                                    HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_EmotionSuppressionHD, .5f + (.1f * pwrVal));
                                    if(p.needs != null)
                                    {
                                        if (p.needs.joy != null)
                                        {
                                            p.needs.joy.CurLevel += (.01f * verVal);
                                        }
                                        if(p.needs.comfort != null)
                                        {
                                            p.needs.comfort.CurLevel += (.01f * verVal);
                                        }
                                    }
                                    
                                }
                            }
                        }
                    }
                    else if(effVal >= 3)
                    {
                        if (base.Pawn.ParentHolder != null && base.Pawn.ParentHolder is Caravan)
                        {
                            Caravan car = base.Pawn.ParentHolder as Caravan;
                            foreach (Pawn p in car.pawns)
                            {
                                HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_EmotionSuppressionHD, .5f + (.1f * pwrVal));
                            }
                        }
                    }
                } 
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return this.removeNow || base.CompShouldRemove;
            }
        }        
    }
}
