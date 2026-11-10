using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class ChronomancerTrials
    {
        public const string FoundationTrialId = "rwom.chronomancer.foundation", MasteryTrialId = "rwom.chronomancer.mastery";
        static CompAbilityUserMagic Magic(Pawn pawn) => pawn?.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
        static MagicPower Owner(Pawn pawn, bool reverse) => Magic(pawn)?.MagicData.MagicPowersC.FirstOrDefault(p => p.learned && p.TMabilityDefs.Contains(reverse ? TorannMagicDefOf.TM_ReverseTime : TorannMagicDefOf.TM_Prediction));
        static DisciplineProgress Owned(Pawn pawn, bool reverse)
        {
            if (pawn == null || pawn.Dead || ChronomancerPrediction.CastingBody(pawn) == null || pawn.Faction == null
                || pawn.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true
                || !ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.chronomancer");
            return path?.Domain == DisciplineDomain.Magic && path.Stage == (reverse ? InitiationStage.Adept : InitiationStage.Foundation)
                && !path.CompletedTrials.Contains(reverse ? MasteryTrialId : FoundationTrialId) && Owner(pawn, reverse) != null ? path : null;
        }
        static bool RuntimeOwned(PawnAbility ability, Pawn caster, bool reverse) => ability != null && ability.Pawn == caster
            && ability.Def == (reverse ? TorannMagicDefOf.TM_ReverseTime : TorannMagicDefOf.TM_Prediction)
            && ability.AbilityUser == Magic(caster) && Magic(caster)?.AbilityData.Powers.Any(p => ReferenceEquals(p, ability)) == true;
        public sealed class PredictionActivation
        {
            internal Pawn caster, body;
            internal PawnAbility ability;
            internal MagicPower owner;
            internal DisciplineProgress path;
            internal bool used;
        }
        public static PredictionActivation CapturePrediction(PawnAbility ability, Pawn target)
        {
            var caster = ability?.Pawn;
            var path = Owned(caster, false);
            return path != null && ChronomancerPrediction.CastingBody(caster) == target && RuntimeOwned(ability, caster, false)
                && target?.health?.hediffSet != null && !target.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PredictionHD)
                ? new PredictionActivation { caster = caster, body = target, ability = ability, owner = Owner(caster, false), path = path } : null;
        }
        public static bool CompletePrediction(PredictionActivation activation)
        {
            if (activation == null || activation.used) return false;
            activation.used = true;
            if (Owned(activation.caster, false) != activation.path || Owner(activation.caster, false) != activation.owner
                || !RuntimeOwned(activation.ability, activation.caster, false)) return false;
            if (ChronomancerPrediction.CastingBody(activation.caster) != activation.body || ChronomancerPrediction.Predictor(activation.body) != activation.caster) return false;
            var hd = activation.body.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_PredictionHD);
            var comp = hd?.TryGetComp<HediffComp_Prediction>();
            if (hd == null || !Finite(hd.Severity) || hd.Severity <= 0 || comp == null || comp.removeNow) return false;
            comp.ownPredictionTrialSource = true;
            return true;
        }
        // Called only at the successful native insertion seam, never during queue inspection or restoration.
        public static bool RecordPrediction(CompAbilityUserMagic comp, QueuedIncident entry)
        {
            var caster = comp?.Pawn;
            var path = Owned(caster, false);
            var body = ChronomancerPrediction.CastingBody(caster);
            if (path == null || body == null || comp != Magic(caster) || entry?.FiringIncident?.parms == null
                || entry.FiringIncident.parms.target != body.Map || comp.predictionIncidentDef != entry.FiringIncident.def
                || comp.predictionTick != entry.FireTick || comp.predictionHash != entry.FiringIncident.parms.GetHashCode()
                || ChronomancerPrediction.ResolveOwned(comp, Find.Storyteller?.incidentQueue) != entry
                || !comp.AbilityData.Powers.Any(a => RuntimeOwned(a, caster, false))) return false;
            var hd = body.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_PredictionHD);
            var source = hd?.TryGetComp<HediffComp_Prediction>();
            return hd != null && hd.pawn == body && source?.Owner == caster && Finite(hd.Severity) && hd.Severity > 0
                && source != null && source.ownPredictionTrialSource && !source.removeNow && path.RecordCompletedTrial(FoundationTrialId);
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool Item(Pawn body, Thing target) => body != null && target != null && !target.Destroyed && target.Spawned && target.Map == body.Map
            && target.def?.category == ThingCategory.Item && !(target is Corpse) && !(target is Pawn)
            && (target.Faction == null || !target.Faction.HostileTo(body.Faction));
        public sealed class ReverseSnapshot
        {
            internal Pawn caster, body;
            internal PawnAbility ability;
            internal Thing target;
            internal MagicPower owner;
            internal DisciplineProgress path;
            internal CompRottable rottable;
            internal float rot;
            internal int hp;
            internal bool food, used;
        }
        public static ReverseSnapshot CaptureReverse(PawnAbility ability, Thing target, bool food)
        {
            var caster = ability?.Pawn;
            var path = Owned(caster, true);
            var body = ChronomancerPrediction.CastingBody(caster);
            if (path == null || !RuntimeOwned(ability, caster, true) || !Item(body, target)) return null;
            var rot = food ? target.TryGetComp<CompRottable>() : null;
            if (food ? rot == null || !Finite(rot.RotProgress) || rot.RotProgress <= 0 : !target.def.useHitPoints || target.HitPoints <= 0) return null;
            return new ReverseSnapshot { caster = caster, body = body, ability = ability, target = target, owner = Owner(caster, true), path = path,
                food = food, rottable = rot, rot = rot?.RotProgress ?? 0, hp = target.HitPoints };
        }
        public static bool CompleteReverse(ReverseSnapshot snapshot)
        {
            if (snapshot == null || snapshot.used) return false;
            snapshot.used = true;
            if (Owned(snapshot.caster, true) != snapshot.path || Owner(snapshot.caster, true) != snapshot.owner
                || !RuntimeOwned(snapshot.ability, snapshot.caster, true) || ChronomancerPrediction.CastingBody(snapshot.caster) != snapshot.body || !Item(snapshot.body, snapshot.target)) return false;
            bool improved = snapshot.food
                ? snapshot.target.TryGetComp<CompRottable>() == snapshot.rottable && Finite(snapshot.rottable.RotProgress)
                    && snapshot.rottable.RotProgress >= 0 && snapshot.rottable.RotProgress < snapshot.rot
                : snapshot.target.HitPoints > snapshot.hp && snapshot.target.HitPoints <= snapshot.target.MaxHitPoints;
            return improved && snapshot.path.RecordCompletedTrial(MasteryTrialId);
        }
    }
}
