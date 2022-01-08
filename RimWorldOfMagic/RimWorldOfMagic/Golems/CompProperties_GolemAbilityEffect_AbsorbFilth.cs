using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

using Verse.Sound;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class CompProperties_GolemAbilityEffect_AbsorbFilth : CompProperties_GolemAbilityEffect
    {
        public float energyPerFilth;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.energyPerFilth, "energyPerFilth");
        }

        public void AbsorbNearbyFilth(Pawn caster, float range, float amount)
        {
            List<Thing> allThings = new List<Thing>();
            List<Thing> allFilth = new List<Thing>();
            allThings.Clear();
            allFilth.Clear();
            List<IntVec3> cellsAround = GenRadial.RadialCellsAround(caster.Position, range, true).ToList();
            for (int i = 0; i < cellsAround.Count; i++)
            {
                allThings = cellsAround[i].GetThingList(caster.Map);
                for (int j = 0; j < allThings.Count; j++)
                {
                    if (allThings[j].def.category == ThingCategory.Filth || allThings[j].def.IsFilth)
                    {
                        allFilth.Add(allThings[j]);
                    }
                }
            }
            for (int i = 0; i < allFilth.Count; i++)
            {
                TM_MoteMaker.ThrowGenericFleck(FleckDefOf.MicroSparks, caster.DrawPos, caster.Map, Rand.Range(.2f, .3f), .6f, .2f, .4f, Rand.Range(-400, -100), .3f, Rand.Range(0, 360), Rand.Range(0, 360));
                CompGolem cg = caster.TryGetComp<CompGolem>();
                if (cg != null)
                {
                    cg.Energy.AddEnergy(amount);
                }
                allFilth[i].Destroy(DestroyMode.Vanish);
            }
        }

        public override void Apply(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability, float effectLevel = 1f, float effectBonus = 1f)
        {
            base.Apply(target, caster, ability);
            AbsorbNearbyFilth(caster, ability.autocasting.maxRange * LevelModifier, energyPerFilth * LevelModifier * effectBonus);
        }

        public override bool CanApplyOn(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability)
        {
            if (caster != null && caster.Map != null)
            {
                return base.CanApplyOn(target, caster, ability);
            }
            return false;
        }
    }
}
