using RimWorld;
using Verse;
using System.Collections.Generic;
using System;
using System.Linq;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_InnerHealing : HediffComp
    {

        private bool initializing = true;
        CompAbilityUserMight comp = null;

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
                FleckMaker.ThrowLightningGlow(base.Pawn.TrueCenter(), base.Pawn.Map, 3f);
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
            if(Find.TickManager.TicksGame % 900 == 0)
            {
                TickAction();
            }
            if(comp != null)
            {
                if (Find.TickManager.TicksGame % 1200 == 0)
                {
                    if(comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_ver").level >= 4)
                    {
                        TickAction();
                    }
                }
                if(Find.TickManager.TicksGame % 2000 ==0)
                {
                    if (comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_ver").level >= 11)
                    {
                        TickActionPerm();
                    }
                }
            }
            else
            {
                comp = this.Pawn.GetCompAbilityUserMight();
            }
        }

        public void TickAction()
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
                                    current.Heal(Rand.Range(.2f, 1f));
                                    num--;
                                    num2--;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void TickActionPerm()
        {
            Pawn pawn = this.Pawn;
            int num = 1;
            using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    BodyPartRecord rec = enumerator.Current;
                    bool flag2 = num > 0;
                    if (flag2)
                    {
                        int num2 = 1;
                        IEnumerable<Hediff_Injury> arg_BB_0 = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                        Func<Hediff_Injury, bool> arg_BB_1;

                        arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                        foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                        {
                            bool flag4 = num2 > 0;
                            if (flag4)
                            {
                                bool flag5 = !current.CanHealNaturally() && current.IsPermanent();
                                if (flag5)
                                {
                                    current.Heal(Rand.Range(.1f, .3f));
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
