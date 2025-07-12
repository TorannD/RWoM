using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using AbilityUser;

namespace TorannMagic
{
    public class Verb_HurtOrSpawnInCone_Properties : VerbProperties_Ability
    {
        public int angle;
        public Verb_HurtOrSpawnInCone_Properties() : base()
        {
            this.verbClass = verbClass ?? typeof(Verb_HurtOrSpawnInCone);
        }
    }

    public class Verb_HurtOrSpawnInCone : Verb_SB
    {
        protected override bool TryCastShot()
        {
            Verb_HurtOrSpawnInCone_Properties Properties = this.verbProps as Verb_HurtOrSpawnInCone_Properties;
            bool dealDamage = false;
            if (this.verbProps.defaultProjectile.projectile.GetDamageAmount(1, null) > 0)
            {
                dealDamage = true;
            }
            //List<IntVec3> AoECells = GenRadial.RadialCellsAround(this.CasterPawn.Position, verbProps.range, false).ToList();
            float distance = Vector3.Distance(this.CasterPawn.Position.ToVector3(), this.currentTarget.CenterVector3);
            List<IntVec3> AoECells = GenRadial.RadialCellsAround(this.CasterPawn.Position, distance, false).ToList();
            if (verbProps.minRange > 0)
            {
                AoECells = AoECells.Except(GenRadial.RadialCellsAround(this.CasterPawn.Position, verbProps.minRange, false).ToList()).ToList();
            }
            float angle = GetAngle(this.CasterPawn.Position, this.currentTarget.Cell);
            float width = Properties.angle;
            List<IntVec3> ConeCells = new List<IntVec3>();
            foreach (IntVec3 c in AoECells)
            {
                if (Math.Abs(GetAngle(this.CasterPawn.Position, c) - angle) < width)
                {
                    if (Rand.Range(0, 1) < this.verbProps.defaultProjectile.projectile.postExplosionSpawnChance)
                    {
                        ConeCells.Add(c);
                    }
                }
            }
            if (dealDamage)
            {
                foreach (IntVec3 c in ConeCells)
                {
                    TM_CopyAndLaunchProjectile.CopyAndLaunchThing(this.verbProps.defaultProjectile, this.CasterPawn, c, c, ProjectileHitFlags.IntendedTarget);
                }
            }
            else
            {
                foreach (IntVec3 c in ConeCells)
                {
                    GenSpawn.Spawn(this.verbProps.defaultProjectile.projectile.postExplosionSpawnThingDef, c, this.CasterPawn.Map);
                }
            }
            return true;
        }

        public float GetAngle(IntVec3 cell1, IntVec3 cell2)
        {
            float x1 = (float)cell1.x;
            float x2 = (float)cell2.x;
            float z1 = (float)cell1.z;
            float z2 = (float)cell2.z;
            float result = (Convert.ToSingle(Math.Atan2(x1 - x2, z1 - z2)) * 180 / Convert.ToSingle(Math.PI)) % 360;
            return result;
        }
    }
}