using TorannMagic.ClassProgression;
using Verse;

namespace TorannMagic
{
    public class Verb_SoL_CreateLight : Verb_SpiritTargetable
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            var map = CasterPawn?.Map;
            return map != null && target.IsValid && (!target.HasThing || target.Thing.Map == map)
                && target.Cell.InBounds(map) && !target.Cell.Fogged(map) && target.Cell.Walkable(map)
                && (root - target.Cell).LengthHorizontal < verbProps.range;
        }

        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            var owner = Ability?.Pawn ?? body;
            var spirit = owner?.GetCompAbilityUserMagic()?.SoL;
            if (body == null || body.Downed || spirit?.Spawned != true || spirit.OwnerPawn != owner
                || spirit.Map != body.Map || !CanHitTargetFrom(body.Position, currentTarget)) return false;
            if (!spirit.IsGlowing) spirit.glowCenter = currentTarget.Cell;
            spirit.shouldGlow = true;
            return true;
        }

        protected override bool TryCastShot()
        {
            ApplyHostedEffect();
            burstShotsLeft = 0;
            return false;
        }
    }
}
