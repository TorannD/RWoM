using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_Regeneration : HediffComp
    {

        private bool initialized = false;
        private int age = 0;
        private int regenRate = 300;
        private int lastRegen = 0;
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
                FleckMaker.ThrowLightningGlow(base.Pawn.DrawPos, base.Pawn.Map, 1f);
                if (this.Def == TorannMagicDefOf.TM_Regeneration_III)
                {
                    hediffPwr = 3;
                }
                else if (this.Def == TorannMagicDefOf.TM_Regeneration_II)
                {
                    hediffPwr = 2;
                }
                else if (this.Def == TorannMagicDefOf.TM_Regeneration_I)
                {
                    hediffPwr = 1;
                }
                else
                {
                    hediffPwr = 0;
                }
            }
        }

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
            if(!this.Pawn.DestroyedOrNull() && !this.Pawn.Dead)
            {
                if (age > lastRegen + regenRate)
                {
                    HealthUtility.AdjustSeverity(base.Pawn, this.Def, -0.3f);
                    this.lastRegen = this.age;
                    Pawn pawn = this.Pawn;

                    TM_MoteMaker.ThrowRegenMote(pawn.DrawPos, pawn.Map, 1f);

                    if (!TM_Calc.IsUndead(pawn))
                    {
                        
                        int injuriesToHeal = ModOptions.Settings.Instance.AIHardMode && !pawn.IsColonist ? 2 : 1;
                        IEnumerable<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs
                            .OfType<Hediff_Injury>()
                            .Where(injury => injury.CanHealNaturally())
                            .Take(injuriesToHeal);

                        float amountToHeal = pawn.IsColonist ? 4f + .5f * hediffPwr : 10f + 1.5f * hediffPwr;
                        foreach (Hediff_Injury injury in injuries)
                        {
                            injury.Heal(amountToHeal);
                        }                        
                    }
                    else
                    {
                        TM_Action.DamageUndead(pawn, (2f + 1f * hediffPwr), null);
                    }
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", 0, false);
            Scribe_Values.Look<int>(ref this.regenRate, "regenRate", 300, false);
            Scribe_Values.Look<int>(ref this.lastRegen, "lastRegen", 0, false);
            Scribe_Values.Look<int>(ref this.hediffPwr, "hediffPwr", 0, false);
        }

    }
}
