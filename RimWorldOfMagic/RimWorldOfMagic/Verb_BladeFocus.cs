using RimWorld;
using Verse;
using AbilityUser;
using System.Collections.Generic;
using System.Linq;

namespace TorannMagic
{
    public class Verb_BladeFocus : Verb_UseAbility
    {

        //bool validTarg;
        //public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        //{
        //    if (targ.Thing != null && targ.Thing == this.caster)
        //    {
        //        return this.verbProps.targetParams.canTargetSelf;
        //    }
        //    if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
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
            Map map = base.CasterPawn.Map;
            Pawn pawn = base.CasterPawn;
            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
            MightPowerSkill pwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_BladeFocus.FirstOrDefault((MightPowerSkill x) => x.label == "TM_BladeFocus_pwr");

            List<Trait> traits = this.CasterPawn.story.traits.allTraits;
            for (int i = 0; i < traits.Count; i++)
            {
                if (traits[i].def.defName == "Bladedancer")
                {
                    
                    if ( traits[i].Degree < pwr.level)
                    {
                        traits.Remove(traits[i]);
                        this.CasterPawn.story.traits.GainTrait(new Trait(TraitDef.Named("Bladedancer"), pwr.level, false));
                        FleckMaker.ThrowHeatGlow(base.CasterPawn.Position, map, 2);
                    }
                }
            }
            
            this.burstShotsLeft = 0;
            return false;
        }
    }
}
