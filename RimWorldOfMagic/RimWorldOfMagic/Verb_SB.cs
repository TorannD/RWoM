using Verse;
using AbilityUser;

namespace TorannMagic
{
    public class Verb_SB : ClassProgression.Verb_SpiritTargetable
    {
        bool validTarg;
        //Used for non-unique abilities that can be used with shieldbelt
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (CasterPawn?.Spawned != true || CasterPawn.Map.Disposed || !targ.IsValid) return false;
            if (Ability?.Def == TorannMagicDefOf.TM_AccelerateTime && targ.Thing?.def.category == ThingCategory.Item && !Projectile_AccelerateTime.CanAgeItem(targ.Thing)) return false;
            if (Ability?.Def == TorannMagicDefOf.TM_TechnoTurret && !Projectile_TechnoTurret.CanDeploy(CasterPawn?.Map,targ.Cell)) return false;
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
    }
}
