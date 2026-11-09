using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_RegrowLimb : ClassProgression.Verb_SpiritTargetable
    {

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            bool validTarg = false;
            if (targ.Thing != null && targ.Thing == this.caster)
            {
                return this.verbProps.targetParams.canTargetSelf;
            }
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    ShootLine shootLine;
                    validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
                }
                else
                {
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot()
        {
            if (!ApplyHostedEffect())
                Messages.Message("InvalidSummon".Translate(), MessageTypeDefOf.RejectInput);
            return false;
        }

        protected override bool ApplyHostedEffect()
        {
            var map = CasterPawn.Map;
            if (map == null || !currentTarget.IsValid || !currentTarget.Cell.InBounds(map)) return false;
            var rect = CellRect.CenteredOn(currentTarget.Cell, 1);
            rect.ClipInsideMap(map);
            return rect.CenterCell.Standable(map) && Place(ThingDef.Named("SeedofRegrowth"), rect.CenterCell, map);
        }

        public static void SingleSpawnLoop(SpawnThings spawnables, IntVec3 position, Map map)
            => Place(spawnables.def, position, map);

        private static bool Place(ThingDef def, IntVec3 position, Map map)
        {
            return def != null && GenPlace.TryPlaceThing(ThingMaker.MakeThing(def, def.MadeFromStuff ? ThingDefOf.WoodLog : null),
                position, map, ThingPlaceMode.Near);
        }
    }
}
