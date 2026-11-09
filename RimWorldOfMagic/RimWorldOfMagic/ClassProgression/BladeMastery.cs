using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class BladeMastery
    {
        internal const int RequiredReplies = 8;
        internal const int RequiredRescues = 12;
        private const string Trial = "rwom.bladedancer.mastery";

        internal static bool Training(DisciplineProgress path) => path?.Stage == InitiationStage.Adept &&
            path.BladeReplies < RequiredReplies && path.BladeRescues < RequiredRescues;
        internal static int Replies(Pawn pawn) => BladeCareer.Path(pawn)?.BladeReplies ?? 0;
        internal static int Rescues(Pawn pawn) => BladeCareer.Path(pawn)?.BladeRescues ?? 0;
        internal static void Riposte(Pawn pawn)
        {
            var path = BladeCareer.Path(pawn);
            if (Training(path) && ++path.BladeReplies == RequiredReplies) path.RecordCompletedTrial(Trial);
        }

        // The native Rescue job has already delivered this injured ally to its chosen bed.
        internal static void Delivered(Pawn rescuer, Pawn ally)
        {
            var path = BladeCareer.Path(BladeCareer.TrialOwner(rescuer));
            if (!Training(path) || ally.thingIDNumber <= 0) return;
            int now = Find.TickManager.TicksGame;
            path.BladeRescueTimes ??= new Dictionary<int,int>();
            if (path.BladeRescueTimes.TryGetValue(ally.thingIDNumber, out int last) && (long)now - last < GenDate.TicksPerDay) return;
            path.BladeRescueTimes[ally.thingIDNumber] = now;
            if (++path.BladeRescues == RequiredRescues) path.RecordCompletedTrial(Trial);
        }

        internal static bool Ready(Pawn pawn)
        {
            var path = BladeCareer.Path(pawn);
            return path?.Stage == InitiationStage.Adept && path.CompletedTrials.Contains(Trial);
        }

        private static MightPower Owner(CompAbilityUserMight might) => TorannMagicDefOf.TM_SeismicSlash == null ? null :
            might?.ExistingMightData?.MightPowersB.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SeismicSlash));

        internal static bool Claim(Pawn pawn)
        {
            if (!Ready(pawn)) return false;
            var might = pawn.GetCompAbilityUserMight();
            if (Owner(might)?.abilityDef == null ||
                !ClassIdentity.Component(pawn).State.TryAdvanceAfterTrial(BladeCareer.Id, InitiationStage.Adept, Trial)) return false;
            Restore(might, BladeCareer.Path(pawn));
            return true;
        }
        internal static void Restore(CompAbilityUserMight might, DisciplineProgress path)
        {
            if (path.Stage < InitiationStage.Master || Owner(might) is not MightPower owner) return;
            owner.learned = true;
            TM_ClassState.AddRuntimeAbility(might, owner.abilityDef);
        }
    }
}
