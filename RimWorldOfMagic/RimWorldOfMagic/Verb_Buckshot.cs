using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using RimWorld;

namespace TorannMagic
{
    public class Verb_Buckshot : Verb_UseAbility  
    {
        private int effVal;
        private int shotcount = 0;

        protected override bool TryCastShot()
        {
            Pawn pawn = this.CasterPawn;
            shotcount = GetShotCount(pawn);

            Vector3 drawPos = pawn.DrawPos + (TM_Calc.GetVector(pawn.Position, this.currentTarget.Cell) * .5f);
            FleckMaker.ThrowSmoke(drawPos, pawn.Map, Rand.Range(.6f, .8f));
            for (int i = 0; i < shotcount; i++)
            {
                IntVec3 targetVariation = this.currentTarget.Cell;
                targetVariation.x += Mathf.RoundToInt(Rand.Range(-.1f, .1f) * Vector3.Distance(pawn.DrawPos, this.currentTarget.CenterVector3));
                targetVariation.z += Mathf.RoundToInt(Rand.Range(-.1f, .1f) * Vector3.Distance(pawn.DrawPos, this.currentTarget.CenterVector3));
                TM_CopyAndLaunchProjectile.CopyAndLaunchThing(this.verbProps.defaultProjectile, pawn, targetVariation, targetVariation, ProjectileHitFlags.All, pawn.equipment.Primary);
            }
            return this.burstShotsLeft >= 0;
        }

        public static int GetShotCount(Pawn pawn)
        {
            int shots = 0;
            return shots = Mathf.RoundToInt((5 + TM_Calc.GetSkillEfficiencyLevel(pawn, TorannMagicDefOf.TM_ShotgunSpec, false)) * pawn.GetComp<CompAbilityUserMight>().mightPwr);
        }
    }
}
