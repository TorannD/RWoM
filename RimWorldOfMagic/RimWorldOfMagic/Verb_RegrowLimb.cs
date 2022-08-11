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
    public class Verb_RegrowLimb : Verb_UseAbility
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

            CellRect cellRect = CellRect.CenteredOn(this.currentTarget.Cell, 1);
            cellRect.ClipInsideMap(this.CasterPawn.Map);
            IntVec3 centerCell = cellRect.CenterCell;
            Map map = this.CasterPawn.Map;

            Pawn caster = base.CasterPawn;

            if ((centerCell.IsValid && centerCell.Standable(map)))
            {
                AbilityUser.SpawnThings tempThing = new SpawnThings();
                tempThing.def = ThingDef.Named("SeedofRegrowth");
                Verb_RegrowLimb.SingleSpawnLoop(tempThing, centerCell, map);
            }
            else
            {
                Messages.Message("InvalidSummon".Translate(), MessageTypeDefOf.RejectInput);
            }
            return false;
        }

        public static void SingleSpawnLoop(SpawnThings spawnables, IntVec3 position, Map map)
        {
            bool flag = spawnables.def != null;
            if (flag)
            {
                ThingDef def = spawnables.def;
                ThingDef stuff = null;
                bool madeFromStuff = def.MadeFromStuff;
                if (madeFromStuff)
                {
                    stuff = ThingDefOf.WoodLog;
                }
                Thing thing = ThingMaker.MakeThing(def, stuff);
                GenPlace.TryPlaceThing(thing, position, map, ThingPlaceMode.Near);
                //GenSpawn.Spawn(thing, position, map, Rot4.North, WipeMode.Vanish, false);                
            }
        }
    }
}
