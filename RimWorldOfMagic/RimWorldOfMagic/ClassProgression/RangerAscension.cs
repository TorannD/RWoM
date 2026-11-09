using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal enum RangerUltimate { None, ArrowStorm, Breakthrough }
    internal static class RangerAscension
    {
        internal const int RequiredRetreats = 8;
        internal const int RequiredCares = 10;
        private const string Trial = "rwom.ranger.guardian";
        private const string Hunt = "TM_RangerBreakthrough";

        private static bool Training(DisciplineProgress path) => path?.Stage == InitiationStage.Master &&
            path.RangerRetreats < RequiredRetreats && path.RangerCares < RequiredCares;
        internal static int Retreats(Pawn ranger) => RangerCareer.Path(ranger)?.RangerRetreats ?? 0;
        internal static int Cares(Pawn ranger) => RangerCareer.Path(ranger)?.RangerCares ?? 0;

        internal static void Retreat(Pawn ranger, Pawn ally)
            => RetreatFrom(ranger,ranger,ally);

        internal static void RetreatFrom(Pawn body,Pawn owner,Pawn ally)
        {
            var path=RangerCareer.ActivePath(body,out var current);
            if (current!=owner || !Training(path) || body?.Spawned != true || ally == null || ally == body || ally.thingIDNumber <= 0 ||
                !ally.Spawned || ally.Map != body.Map || ally.Faction == null || ally.Faction != body.Faction) return;
            int now = Find.TickManager.TicksGame;
            path.RangerRetreatTimes ??= new Dictionary<int,int>();
            if (path.RangerRetreatTimes.TryGetValue(ally.thingIDNumber, out int last) && (long)now - last < GenDate.TicksPerDay) return;
            path.RangerRetreatTimes[ally.thingIDNumber] = now;
            if (++path.RangerRetreats == RequiredRetreats) path.RecordCompletedTrial(Trial);
        }

        internal static void Care(Pawn ranger, Pawn animal)
            => CareFrom(ranger,ranger,animal);

        internal static void CareFrom(Pawn body,Pawn owner,Pawn animal)
        {
            var path = RangerCareer.ActivePath(body,out var current);
            if (current!=owner || !Training(path) || body?.Spawned != true || animal?.Spawned != true || animal.Dead ||
                animal.Map != body.Map || animal.Faction == null || animal.Faction != body.Faction || !animal.RaceProps.Animal) return;
            int now = Find.TickManager.TicksGame;
            if (now < path.RangerNextCareShift) return;
            path.RangerNextCareShift = now + GenDate.TicksPerDay;
            if (++path.RangerCares == RequiredCares) path.RecordCompletedTrial(Trial);
        }

        internal static bool Ready(Pawn ranger)
        {
            var path = RangerCareer.Path(ranger);
            return path?.Stage == InitiationStage.Master && path.CompletedTrials.Contains(Trial) &&
                ranger.GetCompAbilityUserMight()?.ExistingMightData?.MightUserLevel >= 30;
        }
        internal static bool HasHunt(Pawn ranger) =>
            RangerCareer.Path(ranger) is DisciplineProgress path && path.Stage == InitiationStage.Ascended &&
            path.RangerUltimate == RangerUltimate.Breakthrough;

        private static MightPower Storm(CompAbilityUserMight might) => TorannMagicDefOf.TM_ArrowStorm == null ? null :
            might?.ExistingMightData?.MightPowersR.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_ArrowStorm));

        internal static bool Choose(Pawn ranger, bool hunt)
        {
            if (LegacyMartialConversion.AlternativeConflicts(ranger,hunt)) return false;
            if (!Ready(ranger)) return false;
            var might = ranger.GetCompAbilityUserMight();
            var path = RangerCareer.Path(ranger);
            if (path.RangerUltimate != RangerUltimate.None || (hunt
                ? DefDatabase<TMAbilityDef>.GetNamedSilentFail(Hunt) == null : Storm(might)?.abilityDef == null)) return false;
            if (!ClassIdentity.Component(ranger).State.TryAdvanceAfterTrial(path.ClassId, InitiationStage.Master, Trial)) return false;
            path.RangerUltimate = hunt ? RangerUltimate.Breakthrough : RangerUltimate.ArrowStorm;
            Restore(might, path);
            return true;
        }

        internal static void Restore(CompAbilityUserMight might, DisciplineProgress path)
        {
            if (path.Stage != InitiationStage.Ascended) return;
            if (path.RangerUltimate == RangerUltimate.Breakthrough)
                TM_ClassState.AddRuntimeAbility(might, DefDatabase<TMAbilityDef>.GetNamedSilentFail(Hunt));
            else if (path.RangerUltimate == RangerUltimate.ArrowStorm && Storm(might) is MightPower owner)
            {
                owner.learned = true;
                TM_ClassState.AddRuntimeAbility(might, owner.abilityDef);
            }
        }
    }
}
