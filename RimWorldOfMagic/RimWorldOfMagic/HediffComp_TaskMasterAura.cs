using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_TaskMasterAura : HediffComp
    {

        private bool initialized = false;

        private int nextApplyTick = 0;

        private int pwrVal = 0;
        private int verVal = 0;
        private float radius = 0f;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref nextApplyTick, "nextApplyTick", 0);
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

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            var emitter = ClassProgression.SpiritHostedCommander.AuraEmitter(Pawn);
            var might = Pawn?.GetCompAbilityUserMight();
            if (emitter == null || might == null || !might.IsMightUser)
            {
                Pawn?.health?.RemoveHediff(parent);
                return;
            }
            if (!initialized)
            {
                initialized = true;
                DetermineHediff();
            }
            if (Find.TickManager.TicksGame > nextApplyTick)
            {
                nextApplyTick = Find.TickManager.TicksGame + Rand.Range(1000, 1200) - (50 * verVal);
                var recipients = emitter.Map.mapPawns.AllPawnsSpawned.ToList();
                foreach (var recipient in recipients)
                    if (recipient != emitter && recipient.RaceProps.Humanlike && recipient.Faction != null &&
                        recipient.Faction == emitter.Faction && !TM_Calc.IsUndeadNotVamp(recipient) &&
                        emitter.Position.InHorDistOf(recipient.Position, radius))
                        ApplyHediff(recipient);
                might.MightUserXP += Rand.Range(2, 5);
            }
            if (Find.TickManager.TicksGame % 1200 == 0) DetermineHediff();
        }

        private void DetermineHediff()
        {           
            CompAbilityUserMight comp = this.Pawn.GetCompAbilityUserMight();
            if (parent.def == TorannMagicDefOf.TM_TaskMasterAuraHD && comp != null)
            {
                pwrVal = comp.MightData.MightPowerSkill_TaskMasterAura.FirstOrDefault((MightPowerSkill x) => x.label == "TM_TaskMasterAura_pwr").level;
                verVal = comp.MightData.MightPowerSkill_TaskMasterAura.FirstOrDefault((MightPowerSkill x) => x.label == "TM_TaskMasterAura_ver").level;
            }
            this.radius = 15f + (2f * verVal);
        }

        private void ApplyHediff(Pawn p)
        {
            if (p.health != null && p.health.hediffSet != null)
            {
                Hediff hd = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_TaskMasterHD);
                if (hd != null)
                {
                    if (hd.Severity < (.5f + pwrVal))
                    {
                        hd.Severity = .5f + pwrVal;
                    }
                }
                else
                {
                    HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_TaskMasterHD, .5f + pwrVal);
                }
                HediffComp_TaskMaster hdComp = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_TaskMasterHD, false).TryGetComp<HediffComp_TaskMaster>();
                if(hdComp != null)
                {
                    hdComp.duration += 2;
                }
            }            
        }
    }
}
