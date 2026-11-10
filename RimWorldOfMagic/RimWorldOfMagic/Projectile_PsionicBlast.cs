using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;

namespace TorannMagic
{
    class Projectile_PsionicBlast : Projectile_AbilityBase
    {
        private float damageFactor = -1f;

        internal void Prepare(Pawn owner, Pawn body, bool storm)
        {
            if (damageFactor >= 0f) return;
            var might = owner?.GetCompAbilityUserMight();
            int rank = might == null ? 0 : TM_Calc.GetSkillPowerLevel(owner,
                storm ? TorannMagicDefOf.TM_PsionicStorm : TorannMagicDefOf.TM_PsionicBlast, false);
            damageFactor = (body?.GetStatValue(StatDefOf.PsychicSensitivity, false) ?? 1f) *
                (1f + .15f * rank) * (might?.mightPwr ?? 1f) *
                ClassProgression.PsionicMartialPath.BlastFactor(owner);
        }

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget,
            LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false,
            Thing equipment = null, ThingDef targetCoverDef = null)
        {
            Prepare(launcher as Pawn, launcher as Pawn, false);
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags,
                preventFriendlyFire, equipment, targetCoverDef);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref damageFactor, "psionicBlastFactor", -1f);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            // Older in-flight projectiles did not store the originating spell or its ranks.
            if (damageFactor < 0f)
                Prepare(launcher as Pawn, launcher as Pawn, launcher is Pawn oldPawn && !oldPawn.Spawned);
            base.Impact(hitThing, blockedByShield);
            if (map == null) return;
            TM_MoteMaker.MakePowerBeamMotePsionic(Position, map, def.projectile.explosionRadius * 6f, 2f, .7f, .1f, .6f);
            GenExplosion.DoExplosion(Position, map, def.projectile.explosionRadius,
                TMDamageDefOf.DamageDefOf.TM_PsionicInjury, launcher,
                Mathf.RoundToInt(def.projectile.GetDamageAmount(1, null) * damageFactor),
                0, def.projectile.soundExplode, def, equipmentDef, intendedTarget.Thing,
                null, 0f, 1, null, null, 0, false, null, 0f, 1, 0f, false);
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos) =>
            (objectPos-center).ToVector3().normalized;
    }
}
