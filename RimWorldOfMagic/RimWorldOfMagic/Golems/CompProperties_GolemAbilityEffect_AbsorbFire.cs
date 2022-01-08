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
    public class CompProperties_GolemAbilityEffect_AbsorbFire : CompProperties_GolemAbilityEffect
    {
        public float healthPerFire = 5f;
        public float range = 2f;

        public void AbsorbNearbyFire(Pawn caster, float range, float amount)
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
                    if (allThings[j].def == ThingDefOf.Fire)
                    {
                        allFilth.Add(allThings[j]);
                    }
                }
            }
            for (int i = 0; i < allFilth.Count; i++)
            {
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_RedSwirl, allFilth[i].DrawPos, caster.Map, Rand.Range(.3f, .35f), .4f, .1f, .25f, Rand.Range(-400, -200), 0f, 0, Rand.Range(0, 360));
                CompGolem cg = caster.TryGetComp<CompGolem>();
                if (cg != null)
                {
                    TM_Action.DoAction_HealPawn(caster, caster, 1, amount);
                }
                allFilth[i].Destroy(DestroyMode.Vanish);
            }
        }

        public override void Apply(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability, float effectLevel = 1f, float effectBonus = 1f)
        {
            base.Apply(target, caster, ability);
            AbsorbNearbyFire(caster, ability.autocasting.maxRange * LevelModifier, healthPerFire * LevelModifier * effectBonus);
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
