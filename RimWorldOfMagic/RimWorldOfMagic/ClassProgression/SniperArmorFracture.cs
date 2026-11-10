using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.ClassProgression
{
    public sealed class Verb_SniperArmorFracture : Verb_SniperBallisticShot
    {
        protected override bool OwnsTechnique(DisciplineProgress path) => SniperSurvey.HasCombatShot(path);
        protected override string RoundName => "TM_SniperFractureRound";
    }

    public sealed class SniperFractureBullet : SniperWeaponBullet
    {
        // A personal shield can absorb TakeDamage after a bullet has already reached its pawn.
        // Only the primary damage of this projectile may establish a breach.
        [System.ThreadStatic] internal static SniperFractureBullet resolvingImpact;
        private bool impactResolved;
        private bool primaryResolved;
        private bool reachedArmor;
        private int rank;

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget,
            ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            rank = SniperTraining.Rank(TechniqueOwner(launcher), breach: true);
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref rank, "sniperFractureRank");
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (impactResolved) return;
            impactResolved = true;
            var victim = hitThing as Pawn;
            var shooter = launcher as Pawn;
            if (blockedByShield || rank <= 0 || shooter == null || victim == null || victim != intendedTarget.Thing
                || victim.Dead || victim.Downed || victim.IsPrisoner || !victim.HostileTo(shooter))
            {
                base.Impact(hitThing, blockedByShield);
                return;
            }

            var previous = resolvingImpact;
            resolvingImpact = this;
            try { base.Impact(hitThing, blockedByShield); }
            finally { resolvingImpact = previous; }

            if (!reachedArmor) return;
            var owner = TechniqueOwner(shooter);
            SniperMastery.Hit(owner);
            if (!victim.Dead)
                SniperArmorBreach.Apply(owner, victim, rank);
        }

        internal void RecordPrimaryDamage(Pawn victim, DamageInfo damage, bool absorbed)
        {
            if (primaryResolved || victim != intendedTarget.Thing || damage.Instigator != launcher ||
                damage.Def != DamageDef || damage.Weapon != equipmentDef || damage.IntendedTarget != victim) return;
            primaryResolved = true;
            reachedArmor = !absorbed;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
    internal static class SniperFractureShieldPatch
    {
        [HarmonyPriority(Priority.Last)]
        static void Postfix(Pawn __instance, DamageInfo dinfo, bool absorbed)
            => SniperFractureBullet.resolvingImpact?.RecordPrimaryDamage(__instance, dinfo, absorbed);
    }
}
