using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;

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
            Pawn pawn = this.Pawn;
            int num = 2;

            using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    BodyPartRecord rec = enumerator.Current;
                    bool flag2 = num > 0;
                    int num2 = 1;
                    if (flag2)
                    {
                        IEnumerable<Hediff_Injury> arg_BB_0 = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                        Func<Hediff_Injury, bool> arg_BB_1;

                        arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                        foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                        {
                            bool flag3 = num2 > 0;
                            if (flag3)
                            {
                                bool flag5 = current.CanHealNaturally() && !current.IsPermanent();
                                if (flag5)
                                {
                                    current.Heal(Rand.Range(.2f, .5f) + (.1f * pwrVal));
                                    num--;
                                    num2--;
                                }
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
                return base.CompShouldRemove || this.removeNow;
            }
        }
    }
}
