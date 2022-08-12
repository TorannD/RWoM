using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_CommanderAura : HediffComp
    {

        private bool initialized = false;

        private int nextApplyTick = 0;
        public int nextSpeechTick = 0;

        public int pwrVal = 0;
        public int verVal = 0;
        private float radius = 0f;

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
            CompAbilityUserMight comp = this.Pawn.GetCompAbilityUserMight();
            if (spawned && comp != null && comp.IsMightUser)
            {
                DetermineHediff();
            }
            else
            {
                this.Pawn.health.RemoveHediff(this.parent);
            }
        }        

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }

                if(base.Pawn.Map != null)
                {
                    if (Find.TickManager.TicksGame > this.nextApplyTick)
                    {
                        this.nextApplyTick = Find.TickManager.TicksGame + Rand.Range(1000, 1200);
                        List<Pawn> mapPawns = this.Pawn.Map.mapPawns.AllPawnsSpawned;
                        for (int i = 0; i < mapPawns.Count; i++)
                        {
                            if (mapPawns[i].RaceProps.Humanlike && mapPawns[i].Faction != null && mapPawns[i].Faction == base.Pawn.Faction && mapPawns[i] != this.Pawn)
                            {
                                if (!TM_Calc.IsUndeadNotVamp(mapPawns[i]))
                                {
                                    if (base.Pawn.Position.InHorDistOf(mapPawns[i].Position, radius))
                                    {
                                        ApplyHediff(mapPawns[i]);
                                    }
                                }
                            }
                        }
                        CompAbilityUserMight comp = this.Pawn.GetCompAbilityUserMight();
                        comp.MightUserXP += Rand.Range(2, 5);                        
                    }        
                }
                else //map null
                {
                    if (Find.TickManager.TicksGame >= this.nextApplyTick)
                    {
                        this.nextApplyTick = Find.TickManager.TicksGame + Rand.Range(1000, 1200);
                        if (this.Pawn.ParentHolder.ToString().Contains("Caravan"))
                        {
                            foreach (Pawn current in base.Pawn.holdingOwner)
                            {
                                if (current != null)
                                {
                                    if (current.RaceProps.Humanlike && current.Faction != null && current.Faction == this.Pawn.Faction && current != this.Pawn)
                                    {
                                        ApplyHediff(current);
                                    }
                                }
                            }
                        }
                    }
                }

                if(Find.TickManager.TicksGame % 1200 == 0)
                {
                    DetermineHediff();
                }
            }
        }     

        private void DetermineHediff()
        {           
            CompAbilityUserMight comp = this.Pawn.GetCompAbilityUserMight();
            if (parent.def == TorannMagicDefOf.TM_CommanderAuraHD && comp != null)
            {
                pwrVal = comp.MightData.MightPowerSkill_CommanderAura.FirstOrDefault((MightPowerSkill x) => x.label == "TM_CommanderAura_pwr").level;
                verVal = comp.MightData.MightPowerSkill_CommanderAura.FirstOrDefault((MightPowerSkill x) => x.label == "TM_CommanderAura_ver").level;
            }
            this.radius = 15f + (2f * verVal);
        }

        private void ApplyHediff(Pawn p)
        {
            if (p.health != null && p.health.hediffSet != null)
            {
                if (this.pwrVal >= 3)
                {
                    HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_CommanderHD_III, .5f + (.05f * verVal));
                }
                else if(this.pwrVal >= 2)
                {
                    HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_CommanderHD_II, .5f + (.05f * verVal));
                }
                else if(this.pwrVal >= 1)
                {
                    HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_CommanderHD_I, .5f + (.05f * verVal));
                }
                else
                {
                    HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_CommanderHD, .5f + (.05f * verVal));
                }
            }            
        }

        public void DoMotivationalSpeech(Pawn p)
        {

        }
    }
}
