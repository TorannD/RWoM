using AbilityUser;
using TorannMagic.AutoCast;
using TorannMagic.Compatibility;
using Verse;

namespace TorannMagic
{
    internal static class CoreCastProvider
    {
        internal static string Check(Verb verb, LocalTargetInfo target, bool automatic)
        {
            if (!(verb is Verb_UseAbility cast) || !(cast.Ability?.Def is TMAbilityDef def)) return "UnsupportedAbility";
            if (!(verb.caster is Pawn pawn) || !pawn.Spawned || pawn.Map == null) return "OffMap";
            if (pawn.Dead || pawn.Downed || !AbilityCastSafety.AllowsCaster(verb)) return "CasterRestricted";
            if (verb.state != VerbState.Idle || TM_ExternalCastOwnership.IsCasting(pawn)
                || pawn.stances?.curStance is Stance_Warmup || TM_AutocastCombat.IsAbilityJob(pawn.CurJob)) return "Busy";
            if (def.MainVerb == null || verb.verbProps == null) return "UnsupportedAbility";
            if (def.MainVerb.isViolent && pawn.WorkTagIsDisabled(WorkTags.Violent)) return "ViolenceDisabled";
            if (!target.IsValid || !target.Cell.InBounds(pawn.Map)
                || (target.HasThing && (!target.Thing.Spawned || target.Thing.Map != pawn.Map))
                || !AbilityCastSafety.AllowsSelfTarget(pawn, def, target)) return "InvalidTarget";
            if (automatic)
            {
                if (!AbilityCastSafety.CanAutocast(verb, target)) return "AutomaticCastDenied";
            }
            else if (!verb.CanHitTarget(target) || (verb.verbProps.requireLineOfSight
                && !verb.TryFindShootLineFromTo(pawn.Position, target, out _))) return "InvalidTarget";
            return cast.Ability.CanCastPowerCheck(automatic ? AbilityContext.AI : AbilityContext.Player, out _)
                ? "Allowed" : "AbilityNotReady";
        }
    }
}
