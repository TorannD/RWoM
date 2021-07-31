using RimWorld;
using Verse;
using System.Collections.Generic;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_SpellMending : HediffComp
    {

        private bool initializing = true;
        private int ticksTillNextMend = 0;
        private int mendTick = 0;

        public int mendTickTimer = 80;

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
            if(mendTick > ticksTillNextMend)
            {
                TickAction();
            }
            mendTick++;
        }

        public void TickAction()
        {
            List<Apparel> gear = this.Pawn.apparel.WornApparel;
            int tmpDmgItems = 0;

            for (int i = 0; i < gear.Count; i++)
            {
                if (gear[i].HitPoints < gear[i].MaxHitPoints)
                {
                    gear[i].HitPoints++;
                    for (int j = 0; j < Rand.Range(1, 3); j++)
                    {
                        TM_MoteMaker.ThrowTwinkle(this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.4f, .7f), Rand.Range(100, 500), Rand.Range(.4f, 1f), Rand.Range(.05f, .2f), .05f, Rand.Range(.4f, .85f));
                    }
                    tmpDmgItems++;
                }
                if (gear[i].WornByCorpse && Rand.Chance(.1f))
                {
                    gear[i].Notify_PawnResurrected();
                    Traverse.Create(root: gear[i]).Field(name: "wornByCorpseInt").SetValue(false);
                }
            }
            Thing weapon = this.Pawn.equipment.Primary;
            if (weapon != null && (weapon.def.IsRangedWeapon || weapon.def.IsMeleeWeapon))
            {
                if (weapon.HitPoints < weapon.MaxHitPoints)
                {
                    weapon.HitPoints++;
                    for (int j = 0; j < Rand.Range(1, 3); j++)
                    {
                        TM_MoteMaker.ThrowTwinkle(this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.4f, .7f), Rand.Range(100, 500), Rand.Range(.4f, 1f), Rand.Range(.05f, .2f), .05f, Rand.Range(.4f, .85f));
                    }                    
                    tmpDmgItems++;
                }
            }
            mendTick = 0;
            ticksTillNextMend = mendTickTimer * tmpDmgItems;
        }

    }
}
