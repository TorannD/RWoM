using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace TorannMagic
{
    public class TM_CopyAndLaunchProjectile : Projectile
    {
        public static void CopyAndLaunchThing(ThingDef projectileToCopy, Thing launcher, LocalTargetInfo target, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, Thing equipment = null)
        {
            Projectile newProjectile = (Projectile)GenSpawn.Spawn(projectileToCopy, launcher.Position, launcher.Map, WipeMode.Vanish);
            if (newProjectile != null && newProjectile is Projectile)
            {
                newProjectile.Launch(launcher, target, intendedTarget, hitFlags, false, equipment);
            }            
        }

        public static void CopyAndLaunchThingFromPosition(ThingDef projectileToCopy, Thing launcher, IntVec3 fromPosition, Map map, LocalTargetInfo target, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, Thing equipment = null)
        {
            Projectile newProjectile = (Projectile)GenSpawn.Spawn(projectileToCopy, fromPosition, map, WipeMode.Vanish);
            if (newProjectile != null && newProjectile is Projectile)
            {
                newProjectile.Launch(launcher, target, intendedTarget, hitFlags, false, equipment);
            }            
        }

        public static void CopyAndLaunchProjectile(Projectile projectileToCopy, Thing launcher, LocalTargetInfo target, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, Thing equipment = null)
        {
            if (projectileToCopy != null && projectileToCopy is Projectile)
            {
                Projectile newProjectile = (Projectile)GenSpawn.Spawn(projectileToCopy, launcher.Position, launcher.Map, WipeMode.Vanish);
                newProjectile.Launch(launcher, target, intendedTarget, hitFlags, false, equipment);
            }
        }

        public static void CopyAndLaunchProjectileFromPosition(Projectile projectileToCopy, Thing launcher, IntVec3 fromPosition, LocalTargetInfo target, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, Thing equipment = null)
        {
            if (projectileToCopy != null && projectileToCopy is Projectile)
            {
                Projectile newProjectile = (Projectile)GenSpawn.Spawn(projectileToCopy, fromPosition, launcher.Map, WipeMode.Vanish);
                newProjectile.Launch(launcher, target, intendedTarget, hitFlags, false, equipment);
            }
        }
    }
}
