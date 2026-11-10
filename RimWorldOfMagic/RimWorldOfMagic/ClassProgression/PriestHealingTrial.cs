using System.Linq;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class PriestHealingTrial
    {
        public const string TrialId = "rwom.priest.heal-ally";

        public sealed class Snapshot
        {
            internal Pawn caster;
            internal Pawn target;
            internal PawnAbility ability;
            internal DisciplineProgress discipline;
            internal float severity;
            internal bool evaluated;
        }

        private static DisciplineProgress Eligible(Pawn caster, Pawn target, PawnAbility ability)
        {
            var owner = SpiritRebirthCasting.SkillOwner(caster, ability);
            if (owner == null || !ClassIdentity.TryGetState(owner, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            if (caster == null || target == null || target == caster || caster.Dead || target.Dead
                || !caster.Spawned || !target.Spawned || caster.Map != target.Map
                || target.RaceProps?.Humanlike != true || target.Faction == null || caster.Faction == null
                || target.HostileTo(caster) || caster.HostileTo(target) || TM_Calc.IsUndead(target)
                || target.health?.hediffSet == null || ability?.Def != TorannMagicDefOf.TM_AdvancedHeal) return null;
            var path = state.Find("rwom.priest");
            if (ClassIdentity.Profile("rwom.priest")?.domain != DisciplineDomain.Magic) return null;
            if (path?.Domain != DisciplineDomain.Magic || path.Stage != InitiationStage.Foundation
                || path.CompletedTrials.Contains(TrialId)) return null;
            var comp = owner.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            if (comp == null || ability.AbilityUser != comp || !comp.AbilityData.Powers.Any(p => ReferenceEquals(p, ability))) return null;
            return path;
        }

        private static float InjurySeverity(Pawn target)
            => target.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h => h.Severity);

        public static Snapshot Capture(Pawn caster, Pawn target, PawnAbility ability)
        {
            var path = Eligible(caster, target, ability);
            if (path == null) return null;
            float severity = InjurySeverity(target);
            if (severity < 20f || float.IsNaN(severity) || float.IsInfinity(severity)) return null;
            return new Snapshot { caster = caster, target = target, ability = ability, discipline = path, severity = severity };
        }

        public static bool Complete(Snapshot snapshot, bool succeeded)
        {
            if (snapshot == null || snapshot.evaluated) return false;
            snapshot.evaluated = true;
            if (!succeeded || Eligible(snapshot.caster, snapshot.target, snapshot.ability) != snapshot.discipline) return false;
            float after = InjurySeverity(snapshot.target);
            if (float.IsNaN(after) || float.IsInfinity(after) || snapshot.severity - after < 10f) return false;
            return snapshot.discipline.RecordCompletedTrial(TrialId);
        }
    }

    [HarmonyPatch(typeof(Verb_AdvancedHeal), "TryCastShot")]
    public static class PriestHealingTrialPatch
    {
        public static void Prefix(Verb_UseAbility __instance, out PriestHealingTrial.Snapshot __state)
            => __state = PriestHealingTrial.Capture(__instance.CasterPawn, __instance.CurrentTarget.Thing as Pawn, __instance.Ability);

        public static void Postfix(bool __result, PriestHealingTrial.Snapshot __state)
            => PriestHealingTrial.Complete(__state, __result);
    }
}
