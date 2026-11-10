using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class NecromancerDeathMarkTrial
    {
        public const string TrialId = "rwom.necromancer.marked-enemy";
        internal static DisciplineProgress Eligible(Pawn caster)
        {
            if (caster == null || caster.Dead || !ClassIdentity.TryGetState(caster, out var state)) return null;
            var path = state.Find("rwom.necromancer");
            return path?.Domain == DisciplineDomain.Magic && path.Stage == InitiationStage.Adept
                && !path.CompletedTrials.Contains(TrialId) ? path : null;
        }
        internal static bool Enemy(Pawn caster, Pawn victim)
            => caster != null && victim != null && caster != victim && caster.Faction != null
                && victim.Faction != null && !victim.IsPrisoner && !victim.IsSlave
                && victim.Faction != caster.Faction && victim.Faction.HostileTo(caster.Faction)
                && victim.HostileTo(caster) && caster.HostileTo(victim);

        // Called only after an actual successful application, never for resisted casts.
        public static void Applied(Pawn caster, Pawn victim, PawnAbility ability, float appliedSeverity)
        {
            var owner = SpiritRebirthCasting.SkillOwner(caster, ability);
            if (Eligible(owner) == null || !Enemy(owner, victim) || victim.Dead
                || !caster.Spawned || !victim.Spawned || caster.Map != victim.Map || Current.Game == null
                || float.IsNaN(appliedSeverity) || float.IsInfinity(appliedSeverity) || appliedSeverity <= 0) return;
            var magic = owner.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            if (magic == null || ability?.AbilityUser != magic || !magic.AbilityData.Powers.Any(p => ReferenceEquals(p, ability))
                || !magic.MagicData.MagicPowersN.Any(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_DeathMark) && p.TMabilityDefs.Contains(ability.Def))) return;
            var curse = victim.health?.hediffSet?.hediffs.FirstOrDefault(h => h.def.defName == "TM_DeathMarkCurse");
            var tracker = curse?.TryGetComp<HediffComp_DeathMarkTrial>();
            if (tracker == null) return;
            tracker.Record(owner);
        }
    }

    public sealed class HediffCompProperties_DeathMarkTrial : HediffCompProperties
    {
        public HediffCompProperties_DeathMarkTrial() { compClass = typeof(HediffComp_DeathMarkTrial); }
    }
    public sealed class HediffComp_DeathMarkTrial : HediffComp
    {
        private List<Pawn> sources = new List<Pawn>();
        private List<Pawn> hostileAtDeath;
        public void Record(Pawn caster)
        {
            sources.RemoveAll(p => p == null || p.Destroyed);
            if (!sources.Contains(caster)) sources.Add(caster);
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Collections.Look(ref sources, "TM_trialSources", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && sources == null) sources = new List<Pawn>();
        }
        public override void Notify_PawnKilled()
        {
            base.Notify_PawnKilled();
            // RimWorld marks the victim destroyed before Notify_PawnDied, when HostileTo always returns false.
            hostileAtDeath = sources.Where(source => NecromancerDeathMarkTrial.Enemy(source, Pawn)).ToList();
        }
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (Pawn?.Dead != true) return;
            if (hostileAtDeath != null && Current.Game != null && parent.Severity > 0
                && !parent.ShouldRemove && Pawn.health.hediffSet.hediffs.Contains(parent))
                foreach (var source in hostileAtDeath)
                    NecromancerDeathMarkTrial.Eligible(source)?.RecordCompletedTrial(NecromancerDeathMarkTrial.TrialId);
            hostileAtDeath = null;
            sources.Clear();
        }
    }
}
