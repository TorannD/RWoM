using RimWorld;
using Verse;
using System.Collections.Generic;
using System;
using System.Linq;
using TorannMagic.Utils;

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
            IEnumerable<Hediff_Injury> injuries = Pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(injury => injury.CanHealNaturally())
                .DistinctBy(injury => injury.Part)
                .Take(2);

            foreach (Hediff_Injury injury in injuries)
            {
                injury.Heal(Rand.Range(.2f, 1f));
            }                
        }

        public void TickActionPerm()
        {
            Hediff_Injury injuryToHeal = Pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .FirstOrDefault(injury => injury.IsPermanent());
            injuryToHeal?.Heal(Rand.Range(.1f, .3f));
        }
    }
}
