using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_SoulBond : HediffComp
    {
        private bool initializing = true;
        private bool soulPawnRemove = false;
        Pawn bondedPawn = null;

        public override string CompLabelInBracketsExtra => bondedPawn != null ? bondedPawn.LabelShort + "[+]" + base.CompLabelInBracketsExtra : base.CompLabelInBracketsExtra;

        public string labelCap
        {
            get
            {
                if(bondedPawn != null)
                {
                    return base.Def.LabelCap + "(" + bondedPawn.LabelShort + ")";
                }
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                if (bondedPawn != null)
                {
                    return base.Def.label + "(" + bondedPawn.LabelShort + ")";
                }
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned)
            {
                FleckMaker.ThrowHeatGlow(base.Pawn.DrawPos.ToIntVec3(), base.Pawn.Map, 2f);
            }            
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);            
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (initializing)
                {
                    initializing = false;
                    this.Initialize();
                }
            }
            bool flag4 = Find.TickManager.TicksGame % 600 == 0;
            if (flag4)
            {
                if(Find.TickManager.TicksGame % 3600 == 0)
                {
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(TorannMagicDefOf.TM_UsedMagic, this.Pawn.Named(HistoryEventArgsNames.Doer), this.Pawn.Named(HistoryEventArgsNames.Subject), this.Pawn.Named(HistoryEventArgsNames.AffectedFaction), this.Pawn.Named(HistoryEventArgsNames.Victim)), true);
                }
                Pawn pawn = base.Pawn;
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                Pawn soulPawn = comp.soulBondPawn;
                bondedPawn = soulPawn;
                if(soulPawn != null)
                {
                    if (soulPawn.Dead || soulPawn.Destroyed)
                    {
                        this.soulPawnRemove = true;
                    }
                    else
                    {
                        bool soulPawnHasHediff = false;
                        using (IEnumerator<Hediff> enumerator = soulPawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                Hediff rec = enumerator.Current;
                                if (rec.def.defName == "TM_SoulBondPhysicalHD")
                                {
                                    soulPawnHasHediff = true;
                                    if (rec.Severity != .5f + comp.MagicData.MagicPowerSkill_SoulBond.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SoulBond_ver").level)
                                    {
                                        HealthUtility.AdjustSeverity(soulPawn, HediffDef.Named("TM_SoulBondPhysicalHD"), -4f);
                                        HealthUtility.AdjustSeverity(soulPawn, HediffDef.Named("TM_SoulBondPhysicalHD"), 0.5f + comp.MagicData.MagicPowerSkill_SoulBond.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SoulBond_ver").level);
                                    }
                                }
                                else if(rec.def.defName == "TM_SoulBondMentalHD")
                                {
                                    soulPawnHasHediff = true;
                                    if (rec.Severity != .5f + comp.MagicData.MagicPowerSkill_SoulBond.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SoulBond_ver").level)
                                    {
                                        HealthUtility.AdjustSeverity(soulPawn, HediffDef.Named("TM_SoulBondMentalHD"), -4f);
                                        HealthUtility.AdjustSeverity(soulPawn, HediffDef.Named("TM_SoulBondMentalHD"), 0.5f + comp.MagicData.MagicPowerSkill_SoulBond.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SoulBond_ver").level);
                                    }
                                }
                            }
                        }
                        if(!soulPawnHasHediff)
                        {
                            if(this.Pawn.gender == Gender.Female)
                            {
                                HealthUtility.AdjustSeverity(soulPawn, HediffDef.Named("TM_SoulBondPhysicalHD"), -4f);
                                HealthUtility.AdjustSeverity(soulPawn, HediffDef.Named("TM_SoulBondPhysicalHD"), 0.5f + comp.MagicData.MagicPowerSkill_SoulBond.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SoulBond_ver").level);
                            }
                            else if(this.Pawn.gender == Gender.Male)
                            {
                                HealthUtility.AdjustSeverity(soulPawn, HediffDef.Named("TM_SoulBondMentalHD"), -4f);
                                HealthUtility.AdjustSeverity(soulPawn, HediffDef.Named("TM_SoulBondMentalHD"), 0.5f + comp.MagicData.MagicPowerSkill_SoulBond.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SoulBond_ver").level);
                            }
                        }
                        if(this.parent.Severity != .5f + comp.MagicData.MagicPowerSkill_SoulBond.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SoulBond_pwr").level)
                        {
                            if (this.parent.def.defName == "TM_SDSoulBondPhysicalHD")
                            {
                                HealthUtility.AdjustSeverity(pawn, HediffDef.Named("TM_SDSoulBondPhysicalHD"), -4f);
                                HealthUtility.AdjustSeverity(pawn, HediffDef.Named("TM_SDSoulBondPhysicalHD"), 0.5f + comp.MagicData.MagicPowerSkill_SoulBond.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SoulBond_pwr").level);
                            }
                            else if (this.parent.def.defName == "TM_WDSoulBondMentalHD")
                            {
                                HealthUtility.AdjustSeverity(pawn, HediffDef.Named("TM_WDSoulBondMentalHD"), -4f);
                                HealthUtility.AdjustSeverity(pawn, HediffDef.Named("TM_WDSoulBondMentalHD"), 0.5f + comp.MagicData.MagicPowerSkill_SoulBond.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SoulBond_pwr").level);
                            }                            
                        }
                    }
                }
                else
                {
                    this.soulPawnRemove = true;
                }
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.soulPawnRemove;
            }
        }
    }
}
