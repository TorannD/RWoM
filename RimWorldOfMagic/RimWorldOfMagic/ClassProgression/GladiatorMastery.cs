using System.Linq;
using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public enum GladiatorAttack { None, Cleave, Sweep }

    internal static class GladiatorMastery
    {
        internal const string Trial = "rwom.gladiator.holdLine";
        internal static void Defended(Pawn pawn, Thing attacker)
        {
            var path = JobDriver_GladiatorDefense.Path(pawn);
            if (path?.Stage == InitiationStage.Adept && attacker?.HostileTo(pawn) == true)
                path.RecordCompletedTrial(Trial);
        }

        internal static bool Choose(Pawn pawn, GladiatorAttack attack)
        {
            var path = JobDriver_GladiatorDefense.Path(pawn);
            if (path?.Stage != InitiationStage.Adept || path.Attack != GladiatorAttack.None ||
                (attack != GladiatorAttack.Cleave && attack != GladiatorAttack.Sweep)) return false;
            var comp = pawn.GetCompAbilityUserMight();
            var power = comp?.ExistingMightData?.MightPowersG.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Cleave));
            if (power == null || DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_GladiatorSweep") == null ||
                !ClassIdentity.Component(pawn).State.TryAdvanceAfterTrial(path.ClassId, InitiationStage.Adept, Trial)) return false;
            path.Attack = attack;
            if (attack == GladiatorAttack.Cleave)
            {
                power.learned = true;
            }
            else
            {
                // The Sweep owner must be learned like the Cleave one, or the
                // grimoire (which renders owners, not runtime abilities) never
                // shows the chosen attack.
                var sweep = comp?.ExistingMightData?.MightPowersG?.FirstOrDefault(
                    p => p?.TMabilityDefs?.Any(d => d.defName == "TM_GladiatorSweep") == true);
                if (sweep != null) sweep.learned = true;
                JobDriver_GladiatorInitiation.Restore(comp);
            }
            return true;
        }

        internal static bool Automatic(Pawn pawn, CompAbilityUserMight comp)
        {
            var path = JobDriver_GladiatorDefense.Path(pawn);
            if (path != null) return path.Attack == GladiatorAttack.Cleave;
            return pawn.story?.traits?.HasTrait(TorannMagicDefOf.Gladiator) == true || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_Cleave, null, comp);
        }

        internal static CompAbilityUserMight AutomaticSource(Pawn attacker, CompAbilityUserMight local)
        {
            if (local != null && Automatic(attacker,local)) return local;
            if (!SpiritRebirthState.TryGetPossessingSpirit(attacker,out var soul) ||
                !SpiritRebirthState.TryGet(soul,out var state) || state.DepletionPending || soul.story?.traits == null) return null;
            var original = soul.GetCompAbilityUserMight();
            return original != null && Automatic(soul,original) ? original : null;
        }
    }

    public sealed class Verb_GladiatorSweep : Verb_UseAbility
    {
        private static bool Victim(Pawn caster, Pawn target)
            => target != null && target.Spawned && !target.Dead && !target.Downed && !target.IsPrisoner &&
                target.Map == caster.Map && target.HostileTo(caster) && caster.Position.AdjacentTo8WayOrInside(target.Position)
                && caster.CanReachImmediate(target, PathEndMode.Touch);

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
            => CasterPawn?.Spawned == true && root == CasterPawn.Position && CasterPawn.equipment?.Primary?.def.IsMeleeWeapon == true &&
                JobDriver_GladiatorDefense.Path(Ability?.Pawn ?? CasterPawn)?.Attack == GladiatorAttack.Sweep && Victim(CasterPawn,targ.Pawn);

        internal static Verb_GladiatorSweep ForHost(Pawn body, MightAbility power)
            => new Verb_GladiatorSweep { Ability = power, caster = body, verbProps = power.mightDef.MainVerb };

        protected override bool TryCastShot() => Strike(currentTarget);

        internal bool Strike(LocalTargetInfo target)
        {
            var caster = CasterPawn;
            var primary = target.Pawn;
            if (caster?.Spawned != true || caster.Downed || !CanHitTargetFrom(caster.Position,target)) return false;
            var weapon = caster.equipment.Primary;
            var strike = weapon.GetComp<CompEquippable>()?.AllVerbs.Where(v => v.verbProps.IsMeleeAttack && v.tool != null)
                .OrderByDescending(v => v.tool.power).FirstOrDefault();
            if (strike == null) return false;
            var other = GenAdj.AdjacentCells.Select(offset => caster.Position + offset)
                .Where(cell => cell.InBounds(caster.Map) && cell.AdjacentTo8WayOrInside(primary.Position))
                .Select(cell => cell.GetFirstPawn(caster.Map)).FirstOrDefault(p => p != primary && Victim(caster,p));
            float amount = Mathf.Max(1f, strike.verbProps.AdjustedMeleeDamageAmount(strike, caster) * .65f);
            var hit = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Cleave, amount, 0f, -1f, caster, null, weapon.def);
            primary.TakeDamage(hit);
            if (caster.Spawned && !caster.Dead && !caster.Downed && Victim(caster,other)) other.TakeDamage(hit);
            return true;
        }
    }
}
