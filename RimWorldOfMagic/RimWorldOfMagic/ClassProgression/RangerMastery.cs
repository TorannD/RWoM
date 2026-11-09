using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class RangerMastery
    {
        internal const int RequiredTraps = 8;
        internal const int RequiredClearances = 12;
        private const string Trail = "TM_RangerSafeTrail";
        private const string Trial = "rwom.ranger.routes";
        private static bool Training(DisciplineProgress path) => path?.Stage == InitiationStage.Adept &&
            path.RangerClearances < RequiredClearances && (path.RangerTrapVictims?.Count ?? 0) < RequiredTraps;
        internal static int Traps(Pawn ranger) => RangerCareer.Path(ranger)?.RangerTrapVictims?.Count ?? 0;
        internal static int Clearances(Pawn ranger) => RangerCareer.Path(ranger)?.RangerClearances ?? 0;

        internal static void Trapped(Pawn ranger, Pawn victim)
        {
            var path = RangerCareer.Path(ranger);
            if (!Training(path) || victim?.Spawned != true || victim.thingIDNumber <= 0 || victim.Dead || victim.Downed ||
                victim.IsPrisoner || victim.RaceProps.Animal || !victim.HostileTo(ranger)) return;
            path.RangerTrapVictims ??= new List<int>();
            if (path.RangerTrapVictims.Contains(victim.thingIDNumber)) return;
            path.RangerTrapVictims.Add(victim.thingIDNumber);
            if (path.RangerTrapVictims.Count == RequiredTraps) path.RecordCompletedTrial(Trial);
        }

        internal static void Cleared(Pawn ranger, IntVec3 cell)
            => ClearedFrom(ranger,ranger,cell);

        internal static void ClearedFrom(Pawn body,Pawn owner,IntVec3 cell)
        {
            var path=RangerCareer.ActivePath(body,out var current);
            if (current!=owner || !Training(path) || body?.Spawned != true || !cell.InBounds(body.Map)) return;
            var sector = SniperSurvey.Sector(body.Map,cell);
            int now = Find.TickManager.TicksGame;
            path.RangerClearanceTimes ??= new Dictionary<string,int>();
            if (path.RangerClearanceTimes.TryGetValue(sector,out int previous) && (long)now-previous < GenDate.TicksPerDay) return;
            path.RangerClearanceTimes[sector] = now;
            if (++path.RangerClearances == RequiredClearances) path.RecordCompletedTrial(Trial);
        }

        internal static bool Ready(Pawn ranger)
        {
            var path=RangerCareer.Path(ranger);
            return path?.Stage == InitiationStage.Adept && path.CompletedTrials.Contains(Trial);
        }
        internal static bool HasTrail(Pawn ranger) =>
            RangerCareer.Path(ranger) is DisciplineProgress path && path.Stage >= InitiationStage.Master && path.CompletedTrials.Contains(Trial);
        internal static bool Claim(Pawn ranger)
        {
            if (!Ready(ranger) || DefDatabase<TMAbilityDef>.GetNamedSilentFail(Trail) == null) return false;
            var might = ranger.GetCompAbilityUserMight();
            if (might == null || !ClassIdentity.Component(ranger).State.TryAdvanceAfterTrial(RangerCareer.Id,InitiationStage.Adept,Trial)) return false;
            Restore(might);
            return true;
        }
        internal static void Restore(CompAbilityUserMight might)
        {
            if (HasTrail(might.Pawn)) TM_ClassState.AddRuntimeAbility(might,DefDatabase<TMAbilityDef>.GetNamedSilentFail(Trail));
        }
    }
}
