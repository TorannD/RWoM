using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;
using TorannMagic.Utils;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_Provisioner : HediffComp
    {

        private bool initializing = true;
        private int nextTickAction = 0;

        public int verVal = 0;
        public int pwrVal = 0;
        public int duration = 1;

        private bool removeNow = false;

        public override void CompExposeData()
        {
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1, false);
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
            if (spawned)
            {

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
            if(Find.TickManager.TicksGame >= this.nextTickAction)
            {
                this.duration--;                
                this.nextTickAction = Find.TickManager.TicksGame + Rand.Range(600, 700);
                if (pwrVal >= 1 && Rand.Chance(.2f + (.04f * pwrVal)))
                {
                    TickActionGear();
                }
                if (pwrVal >= 2)
                {
                    TickActionHealth();
                }
                if (this.duration <= 0)
                {
                    this.removeNow = true;
                }
            }
        }

        public void TickActionGear()
        {
            List<Apparel> gear = this.Pawn.apparel.WornApparel;
            for(int i = 0; i < gear.Count; i++)
            {
                if(gear[i].HitPoints < gear[i].MaxHitPoints)
                {
                    gear[i].HitPoints++;
                }
            }
            Thing weapon = this.Pawn.equipment.Primary;
            if (weapon != null && (weapon.def.IsRangedWeapon || weapon.def.IsMeleeWeapon))
            {
                if(weapon.HitPoints < weapon.MaxHitPoints)
                {
                    weapon.HitPoints++;
                }
            }
        }

        public void TickActionHealth()
        {
            IEnumerable<Hediff_Injury> injuries = Pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(injury => injury.CanHealNaturally())
                .DistinctBy(injury => injury.Part)
                .Take(2);

            foreach (Hediff_Injury injury in injuries)
            {                
                injury.Heal(Rand.Range(.2f, .5f) + .1f * pwrVal);
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.removeNow;
            }
        }
    }
}
