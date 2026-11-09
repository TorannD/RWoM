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
