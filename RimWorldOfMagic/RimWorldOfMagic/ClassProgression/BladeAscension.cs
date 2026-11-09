using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal enum BladeUltimate { None, PhaseStrike, SevenSteps }
    internal static class BladeAscension
    {
        internal const int RequiredRoutes = 6;
        internal const int RequiredDeliveries = 10;
        private const string Trial = "rwom.bladedancer.routes";
        private const string FormName = "TM_BladeSevenSteps";
        private static bool Training(DisciplineProgress path) => path?.Stage == InitiationStage.Master &&
            path.BladeRoutes < RequiredRoutes && path.BladeDeliveries < RequiredDeliveries;
        internal static int Routes(Pawn pawn) => BladeCareer.Path(pawn)?.BladeRoutes ?? 0;
        internal static int Deliveries(Pawn pawn) => BladeCareer.Path(pawn)?.BladeDeliveries ?? 0;
        internal static bool SupplyAllowed(Pawn pawn)
        {
            var path = BladeCareer.Path(pawn);
            return Training(path) && Find.TickManager.TicksGame >= path.BladeNextSupplyAt;
        }

        internal static bool Supply(Pawn pawn)
        {
            if (!SupplyAllowed(pawn)) return false;
            var path = BladeCareer.Path(pawn);
            path.BladeNextSupplyAt = Find.TickManager.TicksGame + GenDate.TicksPerDay;
            if (++path.BladeDeliveries == RequiredDeliveries) path.RecordCompletedTrial(Trial);
            return true;
        }

        internal static void StartRoute(Pawn pawn, Pawn struck, IntVec3 origin)
            => StartRouteFrom(pawn,pawn,struck,origin);

        internal static void StartRouteFrom(Pawn owner,Pawn pawn,Pawn struck,IntVec3 origin)
        {
            var path = BladeCareer.Path(owner);
            if (!Training(path) || pawn?.Spawned != true || struck == null || struck.thingIDNumber <= 0 || pawn.Position == origin) return;
            path.BladeRouteOpponent = struck.thingIDNumber;
            path.BladeRouteMap = pawn.Map.uniqueID;
            path.BladeRouteBody=pawn.thingIDNumber;
            path.BladeRouteOrigin = origin;
            path.BladeRouteUntil = Find.TickManager.TicksGame + 600;
        }

        internal static void FinishRoute(Pawn pawn, Pawn struck)
        {
            var owner=BladeCareer.TrialOwner(pawn);
            var path = BladeCareer.Path(owner);
            if (!Training(path) || path.BladeRouteOpponent <= 0 || struck == null || struck.thingIDNumber == path.BladeRouteOpponent ||
                pawn?.Spawned != true || pawn.thingIDNumber!=(path.BladeRouteBody>0?path.BladeRouteBody:owner.thingIDNumber) ||
                pawn.Map.uniqueID != path.BladeRouteMap || pawn.Position == path.BladeRouteOrigin ||
                Find.TickManager.TicksGame >= path.BladeRouteUntil) return;
            path.BladeRouteOpponent = 0;
            if (++path.BladeRoutes == RequiredRoutes) path.RecordCompletedTrial(Trial);
        }

        internal static bool Ready(Pawn pawn)
        {
            var path = BladeCareer.Path(pawn);
            return path?.Stage == InitiationStage.Master && path.CompletedTrials.Contains(Trial) &&
                pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightUserLevel >= 30;
        }

        internal static bool HasForm(Pawn pawn) =>
            BladeCareer.Path(pawn) is DisciplineProgress path && path.Stage == InitiationStage.Ascended &&
            path.BladeUltimate == BladeUltimate.SevenSteps;

        private static MightPower Phase(CompAbilityUserMight might) => TorannMagicDefOf.TM_PhaseStrike == null ? null :
            might?.ExistingMightData?.MightPowersB.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_PhaseStrike));

        internal static bool Choose(Pawn pawn, bool seven)
        {
            if (LegacyMartialConversion.AlternativeConflicts(pawn,seven)) return false;
            if (!Ready(pawn)) return false;
            var might = pawn.GetCompAbilityUserMight();
            var path = BladeCareer.Path(pawn);
            if (path.BladeUltimate != BladeUltimate.None || (seven
                ? DefDatabase<TMAbilityDef>.GetNamedSilentFail(FormName) == null : Phase(might)?.abilityDef == null)) return false;
            if (!ClassIdentity.Component(pawn).State.TryAdvanceAfterTrial(path.ClassId, InitiationStage.Master, Trial)) return false;
            path.BladeUltimate = seven ? BladeUltimate.SevenSteps : BladeUltimate.PhaseStrike;
            Restore(might, path);
            return true;
        }

        internal static void Restore(CompAbilityUserMight might, DisciplineProgress path)
        {
            if (path.Stage != InitiationStage.Ascended) return;
            if (path.BladeUltimate == BladeUltimate.SevenSteps)
                TM_ClassState.AddRuntimeAbility(might, DefDatabase<TMAbilityDef>.GetNamedSilentFail(FormName));
            else if (path.BladeUltimate == BladeUltimate.PhaseStrike && Phase(might) is MightPower owner)
            {
                owner.learned = true;
                TM_ClassState.AddRuntimeAbility(might, owner.abilityDef);
            }
        }
    }
}
