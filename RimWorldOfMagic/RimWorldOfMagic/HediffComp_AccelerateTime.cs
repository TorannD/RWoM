using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;


namespace TorannMagic
{
    public class HediffComp_AccelerateTime : HediffComp
    {
        private bool initialized;

        public bool isBad = false;
        public int durationTicks = 6000;
        private int tickEffect = 300;
        float maxAge = 100f;

        private int currentAge = 1;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.isBad, "isBad", false, false);
            Scribe_Values.Look<int>(ref this.durationTicks, "durationTicks", 6000, false);
            Scribe_Values.Look<int>(ref this.currentAge, "currentAge", 1, false);
            Scribe_Values.Look<int>(ref this.tickEffect, "tickEffect", 300, false);
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
            if (spawned && base.Pawn.Map != null)
            {
                FleckMaker.ThrowLightningGlow(base.Pawn.TrueCenter(), base.Pawn.Map, 3f);
            }
            this.currentAge = base.Pawn.ageTracker.AgeBiologicalYears;
            this.tickEffect = Math.Max(1, this.durationTicks / 25);
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
            }           

            if (Find.TickManager.TicksGame % 60 == 0)
            {
                if (this.Pawn.RaceProps != null && this.Pawn.RaceProps.lifeExpectancy != 0)
                {
                    maxAge = this.Pawn.RaceProps.lifeExpectancy;
                }
                var tracker = Pawn.ageTracker;
                tracker.AgeBiologicalTicks = NextAge(tracker.AgeBiologicalTicks,tracker.AgeBiologicalYears,parent.Severity,isBad);
                if(this.Pawn.ageTracker.AgeBiologicalYears > this.currentAge)
                {
                    this.currentAge = this.Pawn.ageTracker.AgeBiologicalYears;                    
                    if (Rand.Chance(this.currentAge / this.maxAge))
                    {
                        BirthdayBiological(this.Pawn, this.currentAge);
                    }
                    if (this.isBad)
                    {
                        RaceAgainstTime(this.Pawn, this.currentAge);
                    }
                }

                AccelerateHediff(this.Pawn, 60);
                this.durationTicks -= 60;

                if(Pawn.Spawned && Find.TickManager.TicksGame % Math.Max(1,tickEffect) == 0)
                {
                    AccelerateEffects(this.Pawn, 1);
                }
            }
        }

        internal static long NextAge(long ticks,int years,float severity,bool harmful)
        {
            double next = years >= (harmful ? 100 : 200)
                ? (double)ticks + (years / 100) * 3600000L
                : harmful ? ticks * (1.02d + .002d * severity) : ticks * 1.00001d + 2500;
            return next >= long.MaxValue ? long.MaxValue : (long)Math.Round(next);
        }

        private void BirthdayBiological(Pawn pawn, float age)
        {
            foreach (HediffGiver_Birthday item in AgeInjuryUtility.RandomHediffsToGainOnBirthday(pawn, Mathf.RoundToInt(age)))
            {
                if ((age > 150 && Rand.Chance(.01f * age)))
                {
                    bool hasAgelessGene = false;
                    if (pawn.genes != null)
                    {
                        
                        if (pawn.genes != null)
                        {
                            foreach (Gene g in Pawn.genes.GenesListForReading)
                            {
                                if (g.def.defName == "Ageless")
                                {
                                    hasAgelessGene = true;
                                }
                            }
                        }
                    }
                    if(!hasAgelessGene)
                    {
                        item.TryApply(pawn);
                    }
                }
            }
        }

        private void RaceAgainstTime(Pawn pawn, float age)
        {               
            if (Rand.Chance(age/maxAge))
            {
                bool hasAgelessGene = false;
                if (pawn.genes != null)
                {
                    foreach(Gene g in Pawn.genes.GenesListForReading)
                    {
                        if(g.def.defName == "Ageless")
                        {
                            hasAgelessGene = true;
                        }
                    }
                }
                if(!hasAgelessGene)
                { 
                    AgeInjuryUtility.GenerateRandomOldAgeInjuries(pawn, false);
                }
            }
            if (Rand.Chance((age/maxAge)*.5f))
            {
                if (!pawn.health.hediffSet.HasHediff(HediffDef.Named("HeartArteryBlockage")))
                {
                    HealthUtility.AdjustSeverity(pawn, HediffDef.Named("HeartArteryBlockage"), Rand.Range(.095f, .195f));
                }
                else
                {
                    HealthUtility.AdjustSeverity(pawn, HediffDef.Named("HeartArteryBlockage"), Rand.Range(.0095f * this.parent.Severity, .0195f * this.parent.Severity));
                }
            }
        }

        private void AccelerateHediff(Pawn pawn, int ticks)
        {
            float totalBleedRate = 0;
            foreach (var rec in pawn.health.hediffSet.hediffs.ToArray())
            {
                HediffComp_Immunizable immuneComp = rec.TryGetComp<HediffComp_Immunizable>();
                if(immuneComp != null)
                {
                    if (immuneComp.Def.CompProps<HediffCompProperties_Immunizable>() != null)
                    {
                        float immuneSevDay = immuneComp.Def.CompProps<HediffCompProperties_Immunizable>().severityPerDayNotImmune;
                        if (immuneSevDay != 0 && !rec.FullyImmune())
                        {
                            rec.Severity += ((immuneSevDay * ticks * this.parent.Severity)/(24*2500));
                        }
                    }
                }
                HediffComp_SeverityPerDay sevDayComp = rec.TryGetComp<HediffComp_SeverityPerDay>();
                if (sevDayComp != null)
                {
                    if (sevDayComp.Def.CompProps<HediffCompProperties_SeverityPerDay>() != null)
                    {
                        float sevDay = sevDayComp.Def.CompProps<HediffCompProperties_SeverityPerDay>().severityPerDay;
                        if (sevDay != 0)
                        {
                            rec.Severity += ((sevDay * ticks * this.parent.Severity)/(24*2500));
                        }
                    }
                }
                HediffComp_Disappears tickComp = rec.TryGetComp<HediffComp_Disappears>();
                if (tickComp != null && tickComp.ticksToDisappear != 0)
                    tickComp.ticksToDisappear -= Mathf.RoundToInt(ticks * parent.Severity);
                if (rec.Bleeding) totalBleedRate += rec.BleedRate;
            }
            if (totalBleedRate != 0)
                HealthUtility.AdjustSeverity(pawn,HediffDefOf.BloodLoss,totalBleedRate * ticks * parent.Severity / 60000f);

            var pregnancy = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Pregnant")) as Hediff_Pregnant;
            if (pregnancy != null && pawn.RaceProps.gestationPeriodDays > 0)
                pregnancy.Severity += parent.Severity / (pawn.RaceProps.gestationPeriodDays * 2500f);
            var eggs = pawn.TryGetComp<CompEggLayer>();
            if (eggs != null && Active(eggs) && eggs.Props.eggLayIntervalDays > 0)
            {
                var progress = Traverse.Create(eggs).Field("eggProgress");
                progress.SetValue(progress.GetValue<float>() + parent.Severity / (eggs.Props.eggLayIntervalDays * 2500f));
            }
            var milk = pawn.TryGetComp<CompMilkable>();
            if (milk != null && Active(milk) && milk.Props.milkIntervalDays > 0)
                Traverse.Create(milk).Field("fullness").SetValue(milk.Fullness + parent.Severity / (milk.Props.milkIntervalDays * 2500f));
        }

        public bool Active(CompMilkable comp)
        {
            if(!Active())
            {
                return false;
            }
            if(Pawn.gender != Gender.Female && comp.Props.milkFemaleOnly)
            {
                return false;
            }
            if(!Pawn.ageTracker.CurLifeStage.milkable)
            {
                return false;
            }
            return true;            
        }

        public bool Active(CompEggLayer comp)
        {
            if (!Active())
            {
                return false;
            }
            if (Pawn.gender != Gender.Female && comp.Props.eggLayFemaleOnly)
            {
                return false;
            }
            if (!Pawn.ageTracker.CurLifeStage.milkable)
            {
                return false;
            }
            return true;
        }

        public bool Active()
        {
            if (this.Pawn.Faction == null)
            {
                return false;
            }
            if (this.Pawn.Suspended)
            {
                return false;
            }
            return true;
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.durationTicks <= 0;
            }
        }

        public void AccelerateEffects(Pawn pawn, int intensity)
        {
            Effecter AccelEffect = TorannMagicDefOf.TM_TimeAccelerationEffecter.Spawn();
            AccelEffect.Trigger(new TargetInfo(pawn), new TargetInfo(pawn));
            AccelEffect.Cleanup();
        }
    }
}
