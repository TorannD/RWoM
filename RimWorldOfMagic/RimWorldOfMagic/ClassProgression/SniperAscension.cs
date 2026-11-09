using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace TorannMagic.ClassProgression
{
    internal enum SniperUltimate { None, ShadowSlayer, SingleOpening }

    internal static class SniperAscension
    {
        internal const int RequiredRescues = 6;
        internal const int RequiredDrills = 8;
        private const string Trial = "rwom.sniper.evacuation";
        private const string Opening = "TM_SniperSingleOpening";

        private static bool Training(DisciplineProgress path) =>
            path?.Stage == InitiationStage.Master && SniperSurvey.HasCombatShot(path) &&
            !path.CompletedTrials.Contains(Trial);

        internal static int Rescues(Pawn pawn) => SniperSurvey.Path(pawn)?.CoveredRescues?.Count ?? 0;
        internal static int Drills(Pawn pawn) => SniperSurvey.Path(pawn)?.EvacuationDrills ?? 0;
        internal static bool DrillAllowed(Pawn pawn)
        {
            var path = SniperSurvey.Path(pawn);
            return Training(path) && Find.TickManager.TicksGame >= path.NextDrillTick;
        }

        internal static bool Drill(Pawn pawn)
        {
            if (!DrillAllowed(pawn)) return false;
            var path = SniperSurvey.Path(pawn);
            path.NextDrillTick = Find.TickManager.TicksGame + GenDate.TicksPerDay;
            if (++path.EvacuationDrills == RequiredDrills) path.RecordCompletedTrial(Trial);
            return true;
        }

        internal static void Rescue(Pawn sniper, Pawn saved)
        {
            var path = SniperSurvey.Path(sniper);
            if (!Training(path) || saved == null || saved == sniper || saved.thingIDNumber <= 0) return;
            path.CoveredRescues ??= new List<int>();
            if (path.CoveredRescues.Contains(saved.thingIDNumber)) return;
            path.CoveredRescues.Add(saved.thingIDNumber);
            if (path.CoveredRescues.Count == RequiredRescues) path.RecordCompletedTrial(Trial);
        }

        internal static bool Ready(Pawn pawn)
        {
            var path = SniperSurvey.Path(pawn);
            return path?.Stage == InitiationStage.Master && path.CompletedTrials.Contains(Trial) &&
                pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightUserLevel >= 30;
        }
        internal static bool HasOpening(Pawn pawn) =>
            SniperSurvey.Path(pawn) is DisciplineProgress path && path.Stage == InitiationStage.Ascended &&
            path.SniperUltimate == SniperUltimate.SingleOpening;

        internal static bool Choose(Pawn pawn, bool opening)
        {
            if (LegacyMartialConversion.AlternativeConflicts(pawn,opening)) return false;
            if (!Ready(pawn)) return false;
            var comp = pawn.GetCompAbilityUserMight();
            var path = SniperSurvey.Path(pawn);
            var shadow = Shadow(comp);
            if (path.SniperUltimate != SniperUltimate.None || (opening
                ? DefDatabase<TMAbilityDef>.GetNamedSilentFail(Opening) == null : shadow?.abilityDef == null)) return false;
            if (!ClassIdentity.Component(pawn).State.TryAdvanceAfterTrial(path.ClassId, InitiationStage.Master, Trial)) return false;
            path.SniperUltimate = opening ? SniperUltimate.SingleOpening : SniperUltimate.ShadowSlayer;
            Restore(comp, path);
            return true;
        }

        private static MightPower Shadow(CompAbilityUserMight comp) =>
            TorannMagicDefOf.TM_ShadowSlayer == null ? null :
            comp?.ExistingMightData?.MightPowersS.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_ShadowSlayer));

        internal static void Restore(CompAbilityUserMight comp, DisciplineProgress path)
        {
            if (path?.Stage != InitiationStage.Ascended) return;
            if (path.SniperUltimate == SniperUltimate.SingleOpening)
                TM_ClassState.AddRuntimeAbility(comp, DefDatabase<TMAbilityDef>.GetNamedSilentFail(Opening));
            else if (path.SniperUltimate == SniperUltimate.ShadowSlayer && Shadow(comp) is MightPower owner)
            {
                owner.learned = true;
                TM_ClassState.AddRuntimeAbility(comp, owner.abilityDef);
            }
        }
    }
}
