using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
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
                
                TM_MoteMaker.ThrowRegenMote(pawn.DrawPos, pawn.Map, 1f);
                bool flag = pawn != null;
                if (flag)
                {
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    int num = 2 + Mathf.RoundToInt(this.hediffPwr * .2f);
                    if(settingsRef.AIHardMode && !pawn.IsColonist)
                    {
                        num = 5;
                    }
  
                    using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            BodyPartRecord rec = enumerator.Current;
                            bool flag2 = num > 0;

                            if (flag2)
                            {
                                
                                int num2 = 3 + Mathf.RoundToInt(this.hediffPwr * .35f); // + ver.level;
                                if(settingsRef.AIHardMode && !pawn.IsColonist)
                                {
                                    num2 = 5;
                                }
                                IEnumerable<Hediff_Injury> arg_BB_0 = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                                Func<Hediff_Injury, bool> arg_BB_1;

                                arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                                foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                                {
                                    bool flag4 = num2 > 0;
                                    if (flag4)
                                    {
                                        bool flag5 = current.CanHealNaturally() && !current.IsPermanent();
                                        if (flag5)
                                        {
                                            
                                            if(!pawn.IsColonist)
                                            {
                                                current.Heal(10f);
                                            }
                                            else
                                            {
                                                current.Heal(2f + (.35f * hediffPwr));
                                            }
                                            if(Rand.Chance(.4f + (.02f * hediffPwr)))
                                            {
                                                current.Tended(Rand.Range(.4f + (.02f * this.hediffPwr), .5f + (.03f * this.hediffPwr)), 1f);
                                            }
                                            num--;
                                            num2--;
                                        }
                                    }
                                }
                            }
                        }
                    }
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
