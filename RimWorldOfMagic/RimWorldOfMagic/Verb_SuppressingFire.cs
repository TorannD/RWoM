using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using RimWorld;


namespace TorannMagic
{
    public class Verb_SuppressingFire : Verb_UseAbility  
    {
        private bool initialized = false;
        private int effVal;
        private int shotcount = 0;

        protected override bool TryCastShot()
        {
            Pawn pawn = this.CasterPawn;
            if (this.currentTarget != null && pawn.equipment != null && pawn.equipment.Primary != null)
            {
                IntVec3 targetVariation = this.currentTarget.Cell;
                targetVariation.x += Mathf.RoundToInt(Rand.Range(-.15f, .15f) * Vector3.Distance(pawn.DrawPos, this.currentTarget.CenterVector3) + Rand.Range(-1f, 1f));
                targetVariation.z += Mathf.RoundToInt(Rand.Range(-.15f, .15f) * Vector3.Distance(pawn.DrawPos, this.currentTarget.CenterVector3) + Rand.Range(-1f, 1f));

                if (!initialized)
                {
                    initialized = true;
                    shotcount = GetShotCount(pawn);
                }

                Vector3 drawPos = pawn.DrawPos + (TM_Calc.GetVector(pawn.Position, targetVariation) * .5f);
                FleckMaker.ThrowSmoke(drawPos, pawn.Map, Rand.Range(.4f, .7f));
                TM_CopyAndLaunchProjectile.CopyAndLaunchThing(pawn.equipment.Primary.def.Verbs.FirstOrDefault().defaultProjectile, pawn, targetVariation, targetVariation, ProjectileHitFlags.All, pawn.equipment.Primary);
                shotcount--;
                if (shotcount <= 0)
                {
                    initialized = false;
                }
                return (shotcount > 0);
            }
            else
            {
                Log.Warning("Target or weapon was null when using suppressing fire.");
            }
            return false;
        }

        public static int GetShotCount(Pawn pawn)
        {
            int shots = 0;
            float weaponDamage = pawn.equipment.Primary.def.Verbs.FirstOrDefault().defaultProjectile.projectile.GetDamageAmount(pawn.equipment.Primary, null);
            float burstShots = pawn.equipment.Primary.def.Verbs.FirstOrDefault().burstShotCount;
            int effVal = TM_Calc.GetSkillEfficiencyLevel(pawn, TorannMagicDefOf.TM_RifleSpec);
            shots = Mathf.RoundToInt(((50f / weaponDamage) + (2 * burstShots)) * (1.2f + .15f * effVal) * pawn.GetCompAbilityUserMight().mightPwr);
            return shots;
        }
    }
}
