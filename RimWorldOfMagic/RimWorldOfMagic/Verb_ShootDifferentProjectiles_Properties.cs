using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using System;

namespace TorannMagic
{
    public class Verb_ShootDifferentProjectiles_Properties : VerbProperties_Ability
    {
        public List<ThingDef> projectiles;
        public bool shootInListOrder = true;
        public int addShotPerMagicLevels = 1000;
        public int addShotPerMightLevels = 1000;
        public Verb_ShootDifferentProjectiles_Properties() : base()
        {
            this.verbClass = verbClass ?? typeof(Verb_ShootDifferentProjectiles);
        }
    }

    public class Verb_ShootDifferentProjectiles : Verb_SB
    {
        private int ShotsSoFar;
        protected override bool TryCastShot()
        {
            Verb_ShootDifferentProjectiles_Properties Properties = this.verbProps as Verb_ShootDifferentProjectiles_Properties;
            if (this.ShotsSoFar == 0)
            {
                CompAbilityUserMagic compMagic = this.CasterPawn.GetCompAbilityUserMagic();
                if (compMagic != null && compMagic.MagicData != null)
                {
                    this.burstShotsLeft += (int)(this.CasterPawn.GetCompAbilityUserMagic().MagicUserLevel / Properties.addShotPerMagicLevels);
                }
                CompAbilityUserMight compMight = this.CasterPawn.GetCompAbilityUserMight();
                if (compMight != null && compMight.MightData != null)
                {
                    this.burstShotsLeft += (int)(this.CasterPawn.GetCompAbilityUserMight().MightUserLevel / Properties.addShotPerMightLevels);
                }
            }
            IntVec3 targetVariation = (IntVec3)this.currentTarget;

            if (this.verbProps.ForcedMissRadius > 0)
            {
                targetVariation = this.currentTarget.Cell;
                System.Random rnd = new System.Random();
                targetVariation.x += rnd.Next(-(int)Math.Sqrt(this.verbProps.ForcedMissRadius), (int)Math.Sqrt(this.verbProps.ForcedMissRadius));
                targetVariation.z += rnd.Next(-(int)Math.Sqrt(this.verbProps.ForcedMissRadius), (int)Math.Sqrt(this.verbProps.ForcedMissRadius));
            }
            ThingDef toLaunch;
            if (Properties.projectiles != null)
            {
                if (Properties.shootInListOrder)
                {
                    toLaunch = Properties.projectiles[ShotsSoFar % Properties.projectiles.Count];
                    //If the ability fires ten shots per burst but two are listed, it'd alternate between the two
                }
                else
                {
                    System.Random rand = new System.Random();
                    toLaunch = Properties.projectiles[rand.Next(Properties.projectiles.Count)];
                    //just roll a random projectile from the list
                }
            }
            else
            {
                toLaunch = this.verbProps.defaultProjectile;
            }
            this.TryLaunchProjectile(toLaunch, targetVariation);
            ShotsSoFar++;
            // return (ShotsSoFar <= verbProps.burstShotCount + (int)(this.CasterPawn.GetCompAbilityUserMagic().MagicUserLevel / 10));
            return true;
        }
    }
}