using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;

namespace TorannMagic
{
    class Verb_PsionicBlast : Verb_UseAbility  
    {

        bool validTarg;
        private int verVal;

        //public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        //{
        //    if (targ.Thing != null && targ.Thing == this.caster)
        //    {
        //        return this.verbProps.targetParams.canTargetSelf;
        //    }
        //    if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
        //    {
        //        if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
        //        {
        //            ShootLine shootLine;
        //            validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
        //        }
        //        else
        //        {
        //            validTarg = false;
        //        }
        //    }
        //    else
        //    {
        //        validTarg = false;
        //    }
        //    return validTarg;
        //}

        protected override bool TryCastShot()
        {
            bool result = false;
            Pawn pawn = this.CasterPawn;
            verVal = TM_Calc.GetSkillVersatilityLevel(pawn, this.Ability.Def as TMAbilityDef);
            //verVal = TM_Calc.GetMightSkillLevel(pawn, pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_PsionicBlast, "TM_PsionicBlast", "_ver", true);
            //this.verVal = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_PsionicBlast.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicBlast_ver").level;
            //if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    this.verVal = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver").level;
            //}
            Map map = this.CasterPawn.Map;
            IntVec3 targetVariation = this.currentTarget.Cell;
            targetVariation.x += Mathf.RoundToInt(Rand.Range(-.1f, .1f) * Vector3.Distance(pawn.DrawPos, this.currentTarget.CenterVector3) + Rand.Range(-1f, 1f)) ;
            targetVariation.z += Mathf.RoundToInt(Rand.Range(-.1f, .1f) * Vector3.Distance(pawn.DrawPos, this.currentTarget.CenterVector3) + Rand.Range(-1f, 1f));
            float angle = (Quaternion.AngleAxis(90, Vector3.up) * GetVector(pawn.Position, targetVariation)).ToAngleFlat();
            Vector3 drawPos = pawn.DrawPos + (GetVector(pawn.Position, targetVariation) * .5f);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiBlastStart, drawPos, pawn.Map, Rand.Range(.4f, .6f), Rand.Range(.0f, .05f), .1f, .2f, 0, 0, 0, angle); //throw psi blast start
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiBlastEnd, drawPos, pawn.Map, Rand.Range(.4f, .8f), Rand.Range(.0f, .1f), .2f, .3f, 0, Rand.Range(1f, 1.5f), angle, angle); //throw psi blast end 
            this.TryLaunchProjectile(this.verbProps.defaultProjectile, targetVariation);
            float psiEnergy = 0;
            if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_PsionicHD"), false))
            {
                psiEnergy = this.CasterPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_PsionicHD"), false).Severity;
                HealthUtility.AdjustSeverity(pawn, HediffDef.Named("TM_PsionicHD"), -(4 - this.verVal));
            }
            result = Rand.Chance(((verVal*4)+psiEnergy)/100);            
            return result;
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            Vector3 heading = (objectPos - center).ToVector3();
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }
    }
}
