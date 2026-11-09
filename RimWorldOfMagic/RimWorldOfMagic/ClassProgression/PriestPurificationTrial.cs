using System.Linq;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class PriestPurificationTrial
    {
        public const string TrialId = "rwom.priest.purify-ally";
        public sealed class Snapshot
        {
            internal Pawn caster, target;
            internal PawnAbility ability;
            internal DisciplineProgress discipline;
            internal Hediff_Injury[] scars;
            internal bool evaluated;
        }

        private static DisciplineProgress Eligible(Pawn caster, Pawn target, PawnAbility ability)
        {
            var owner = SpiritRebirthCasting.SkillOwner(caster, ability);
            if (owner == null || !ClassIdentity.TryGetState(owner, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            if (caster == null || target == null || caster == target || caster.Dead || target.Dead
                || !caster.Spawned || !target.Spawned || caster.Map != target.Map
                || target.RaceProps?.Humanlike != true || target.Faction == null || caster.Faction == null
                || target.HostileTo(caster) || caster.HostileTo(target) || TM_Calc.IsUndead(target)
                || target.health?.hediffSet == null || ability?.Def != TorannMagicDefOf.TM_Purify
                || ClassIdentity.Profile("rwom.priest")?.domain != DisciplineDomain.Magic) return null;
            var path = state.Find("rwom.priest");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage != InitiationStage.Adept
                || path.CompletedTrials.Contains(TrialId)) return null;
            var comp = owner.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            if (comp == null || ability.AbilityUser != comp || !comp.AbilityData.Powers.Any(p => ReferenceEquals(p, ability))) return null;
            return path;
        }

        public static Snapshot Capture(Pawn caster, Pawn target, PawnAbility ability)
        {
            var path = Eligible(caster, target, ability);
            if (path == null) return null;
            // Capture individual existing scars, not total health or newly added conditions.
            var scars = target.health.hediffSet.hediffs.OfType<Hediff_Injury>()
                .Where(h => h.IsPermanent() && h.Severity > 0f && !float.IsInfinity(h.Severity)).ToArray();
            if (scars.Length == 0) return null;
            return new Snapshot { caster = caster, target = target, ability = ability, discipline = path, scars = scars };
        }

        public static bool Complete(Snapshot snapshot, bool succeeded)
        {
            if (snapshot == null || snapshot.evaluated) return false;
            snapshot.evaluated = true;
            if (!succeeded || Eligible(snapshot.caster, snapshot.target, snapshot.ability) != snapshot.discipline) return false;
            var current = snapshot.target.health.hediffSet.hediffs;
            if (!snapshot.scars.Any(s => s.Severity <= 0f || !current.Any(h => ReferenceEquals(h, s)))) return false;
            return snapshot.discipline.RecordCompletedTrial(TrialId);
        }
    }

    [HarmonyPatch(typeof(Verb_Purify), "TryCastShot")]
    public static class PriestPurificationTrialPatch
    {
        public static void Prefix(Verb_UseAbility __instance, out PriestPurificationTrial.Snapshot __state)
            => __state = PriestPurificationTrial.Capture(__instance.CasterPawn, __instance.CurrentTarget.Thing as Pawn, __instance.Ability);
        public static void Postfix(bool __result, PriestPurificationTrial.Snapshot __state)
            => PriestPurificationTrial.Complete(__state, __result);
    }
}
