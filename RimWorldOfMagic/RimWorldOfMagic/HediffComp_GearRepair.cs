using RimWorld;
using Verse;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_GearRepair : HediffComp
    {

        private bool initializing = true;

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
            if(Find.TickManager.TicksGame % 1200 == 0)
            {
                TickAction();
            }
        }

        public void TickAction()
        {
            List<Apparel> gear = this.Pawn.apparel.WornApparel;
            for(int i = 0; i < gear.Count; i++)
            {
                if(Rand.Chance(.2f) && gear[i].HitPoints < gear[i].MaxHitPoints)
                {
                    gear[i].HitPoints++;
                }
                CompAbilityUserMight comp = this.Pawn.GetCompAbilityUserMight();
                if (comp != null && comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_ver").level >= 5)
                {
                    if (gear[i].HitPoints >= gear[i].MaxHitPoints && gear[i].WornByCorpse)
                    {
                        gear[i].Notify_PawnResurrected();
                        Traverse.Create(root: gear[i]).Field(name: "wornByCorpseInt").SetValue(false);
                    }
                }
            }
            Thing weapon = this.Pawn.equipment.Primary;
            if (weapon != null && (weapon.def.IsRangedWeapon || weapon.def.IsMeleeWeapon))
            {
                if(Rand.Chance(.2f) && weapon.HitPoints < weapon.MaxHitPoints)
                {
                    weapon.HitPoints++;
                }
            }
        }

    }
}
