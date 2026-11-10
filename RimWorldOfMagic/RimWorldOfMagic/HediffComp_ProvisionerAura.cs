using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_ProvisionerAura : HediffComp
    {

        private bool initialized = false;

        private int nextApplyTick = 0;

        private int pwrVal = 0;
        private int verVal = 0;
        private float radius = 0f;

        List<Pawn> speechPawns = new List<Pawn>();

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
                nextApplyTick = Find.TickManager.TicksGame + Rand.Range(1000, 1200);
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
            if (parent.def == TorannMagicDefOf.TM_ProvisionerAuraHD && comp != null)
            {
                pwrVal = comp.MightData.MightPowerSkill_ProvisionerAura.FirstOrDefault((MightPowerSkill x) => x.label == "TM_ProvisionerAura_pwr").level;
                verVal = comp.MightData.MightPowerSkill_ProvisionerAura.FirstOrDefault((MightPowerSkill x) => x.label == "TM_ProvisionerAura_ver").level;
            }
            this.radius = 15f + (2f * verVal);
        }

        private void ApplyHediff(Pawn p)
        {
            if (p.health != null && p.health.hediffSet != null)
            {
                Hediff hd = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ProvisionerHD);
                if (hd != null)
                {
                    if (hd.Severity < (.5f + pwrVal))
                    {
                        hd.Severity = .5f + pwrVal;
                    }
                }
                else
                {
                    HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_ProvisionerHD, .5f + pwrVal);
                    hd = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ProvisionerHD);
                }
                if (hd != null)
                {
                    HediffComp_Provisioner hdComp = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ProvisionerHD, false).TryGetComp<HediffComp_Provisioner>();
                    if (hdComp != null)
                    {
                        hdComp.duration += 2;
                        hdComp.pwrVal = pwrVal;
                        hdComp.verVal = verVal;
                    }
                }
                if(verVal >= 3)
                {
                    float sev = 1.15f;
                    bool flag = true;
                    if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnergyRegenHD, false))
                    {
                        Hediff hdRemove = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnergyRegenHD, false);
                        if (hdRemove != null && hdRemove.Severity > sev)
                        {
                            //do not overwrite a better regeneration buff
                            flag = false;
                        }
                        else
                        {
                            HediffComp_SetDuration hd2 = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnergyRegenHD, false).TryGetComp<HediffComp_SetDuration>();
                            if (hd2 != null)
                            {
                                hd2.duration += 10 + pwrVal;
                            }
                        }
                    }
                    if(flag)
                    {
                        HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_EnergyRegenHD, sev);
                        HediffComp_SetDuration hd2 = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnergyRegenHD, false).TryGetComp<HediffComp_SetDuration>();
                        if (hd2 != null)
                        {
                            hd2.duration = 20 + pwrVal*2;
                        }

                    }
                }
            }            
        }
    }
}
