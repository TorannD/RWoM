using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using TorannMagic.Utils;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_NanoStimulant : HediffComp
    {

        private bool initialized = false;
        private int age = 0;
        private int duration = 1500;
        private int hediffPwr = 0;

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
            
            if (spawned)
            {
                this.hediffPwr = Mathf.RoundToInt(this.parent.Severity);
                this.duration = 1500 + (60 * this.hediffPwr);
            }
        }

        public override bool CompShouldRemove => base.CompShouldRemove || this.age > this.duration;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (base.Pawn != null & base.parent != null)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }
            }
            this.age++;
            
            if (age == duration)
            {
                HealthUtility.AdjustSeverity(base.Pawn, HediffDef.Named("TM_NanoStimulantWithdrawalHD"), 1 - (.03f * this.hediffPwr));
                Pawn pawn = base.Pawn as Pawn;

                if (pawn == null) return;
                TM_MoteMaker.ThrowRegenMote(pawn.DrawPos, pawn.Map, 1f);

                
                int injuriesToHeal;
                int injuriesPerBodyPart;
                if (ModOptions.Settings.Instance.AIHardMode && !pawn.IsColonist)
                {
                    injuriesToHeal = 5;
                    injuriesPerBodyPart = 5;
                }
                else
                {
                    injuriesToHeal = 2 + Mathf.RoundToInt(hediffPwr * .2f);
                    injuriesPerBodyPart = 3 + Mathf.RoundToInt(hediffPwr * .35f);
                }

                IEnumerable<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs
                    .OfType<Hediff_Injury>()
                    .Where(injury => injury.CanHealNaturally())
                    .DistinctBy(injury => injury.Part, injuriesPerBodyPart)
                    .Take(injuriesToHeal);

                float healAmount = pawn.IsColonist ? 2f + .35f * hediffPwr : 10f;
                float tendChance = .4f + .02f * hediffPwr;
                foreach (Hediff_Injury injury in injuries)
                {
                    injury.Heal(healAmount);
                    if (Rand.Chance(tendChance))
                        injury.Tended(Rand.Range(.4f + .02f * hediffPwr, .5f + .03f * hediffPwr), 1f);
                }                
            }            
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", 0, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1500, false);
            Scribe_Values.Look<int>(ref this.hediffPwr, "hediffPwr", 0, false);
        }

    }
}
