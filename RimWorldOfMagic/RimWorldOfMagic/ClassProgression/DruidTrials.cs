using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class DruidTrials
    {
        public const string FoundationTrialId = "rwom.druid.foundation", MasteryTrialId = "rwom.druid.mastery";
        static Pawn CastingBody(Pawn owner)
        {
            if (owner == null || owner.Spawned) return owner;
            return SpiritRebirthState.TryGet(owner, out var state) && state.HasValidHost(owner) ? state.Host : null;
        }
        static DisciplineProgress Owned(PawnAbility ability, TMAbilityDef spell, InitiationStage stage)
        {
            var caster = ability?.Pawn;
            if (caster == null || caster.Dead || CastingBody(caster)?.Spawned != true || caster.Faction == null
                || caster.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true
                || !ClassIdentity.TryGetState(caster, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.druid");
            var magic = caster.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            if (path?.Domain != DisciplineDomain.Magic || path.Stage != stage
                || path.CompletedTrials.Contains(stage == InitiationStage.Foundation ? FoundationTrialId : MasteryTrialId)
                || magic == null || ability.AbilityUser != magic || !magic.AbilityData.Powers.Any(p => ReferenceEquals(p, ability))) return null;
            return magic.MagicData.MagicPowersD.Any(p => p.learned && p.TMabilityDefs.Contains(spell) && p.TMabilityDefs.Contains(ability.Def)) ? path : null;
        }
        static bool Animal(Pawn caster, Pawn target) => target != null && !target.Dead && target.Spawned && target.Map == CastingBody(caster)?.Map
            && target.RaceProps?.Animal == true && target.Faction != caster.Faction;
        static bool Manhunter(Pawn target)
        {
            var state = target?.mindState?.mentalStateHandler?.CurStateDef;
            return state != null && (state == MentalStateDefOf.Manhunter || state == MentalStateDefOf.ManhunterPermanent);
        }
        static bool Ally(Pawn caster, Pawn target) => target != null && target != CastingBody(caster) && !target.Dead && target.Spawned && target.Map == CastingBody(caster)?.Map
            && target.RaceProps?.Humanlike == true && target.Faction == caster.Faction && !target.IsPrisoner && !target.IsSlave
            && !TM_Calc.IsUndead(target) && target.health?.hediffSet != null;
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static float Regeneration(Pawn target) => target.health.hediffSet.hediffs.Where(h => h.def == TorannMagicDefOf.TM_Regeneration
            || h.def == TorannMagicDefOf.TM_Regeneration_I || h.def == TorannMagicDefOf.TM_Regeneration_II || h.def == TorannMagicDefOf.TM_Regeneration_III).Sum(h => h.Severity);
        public sealed class Snapshot
        {
            internal PawnAbility ability;
            internal Pawn target;
            internal DisciplineProgress path;
            internal bool regeneration, used;
            internal float severity;
        }
        public static Snapshot CaptureAnimal(PawnAbility ability, Pawn target)
        {
            var path = Owned(ability, TorannMagicDefOf.TM_SootheAnimal, InitiationStage.Foundation);
            return path != null && Animal(ability.Pawn, target) && Manhunter(target)
                ? new Snapshot { ability = ability, target = target, path = path } : null;
        }
        public static Snapshot CaptureRegeneration(PawnAbility ability, Pawn target)
        {
            var path = Owned(ability, TorannMagicDefOf.TM_Regenerate, InitiationStage.Adept);
            if (path == null || !Ally(ability.Pawn, target)) return null;
            float injury = target.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h => h.Severity);
            float severity = Regeneration(target);
            return Finite(injury) && injury >= 8 && Finite(severity)
                ? new Snapshot { ability = ability, target = target, path = path, regeneration = true, severity = severity } : null;
        }
        public static bool Complete(Snapshot snapshot)
        {
            if (snapshot == null || snapshot.used) return false;
            snapshot.used = true;
            var ability = snapshot.ability;
            if (snapshot.regeneration)
            {
                float after = Ally(ability.Pawn, snapshot.target) ? Regeneration(snapshot.target) : float.NaN;
                return Owned(ability, TorannMagicDefOf.TM_Regenerate, InitiationStage.Adept) == snapshot.path && Finite(after) && after > snapshot.severity
                    && snapshot.path.RecordCompletedTrial(MasteryTrialId);
            }
            return Owned(ability, TorannMagicDefOf.TM_SootheAnimal, InitiationStage.Foundation) == snapshot.path
                && Animal(ability.Pawn, snapshot.target) && !Manhunter(snapshot.target) && snapshot.path.RecordCompletedTrial(FoundationTrialId);
        }
    }
}

