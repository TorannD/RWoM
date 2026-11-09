using System.Linq;
using System.Runtime.CompilerServices;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class PaladinTrials
    {
        public const string FoundationTrialId = "rwom.paladin.foundation", MasteryTrialId = "rwom.paladin.mastery";
        static bool Matches(AbilityUser.AbilityDef ability, TMAbilityDef root)
            => ability != null && root != null && (ability == root || ability.defName == root.defName + "_I" || ability.defName == root.defName + "_II" || ability.defName == root.defName + "_III");
        static DisciplineProgress Owned(Pawn caster, TMAbilityDef spell, Pawn emitter = null)
        {
            if (caster == null || caster.Dead || !(emitter ?? caster).Spawned || caster.Faction == null
                || caster.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true
                || !ClassIdentity.TryGetState(caster, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.paladin");
            var stage = spell == TorannMagicDefOf.TM_Heal ? InitiationStage.Foundation : InitiationStage.Adept;
            if (path?.Domain != DisciplineDomain.Magic || path.Stage != stage
                || path.CompletedTrials.Contains(stage == InitiationStage.Foundation ? FoundationTrialId : MasteryTrialId)) return null;
            var magic = caster.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            return magic?.MagicData.MagicPowersP.Any(p => p.learned && p.TMabilityDefs.Contains(spell)) == true ? path : null;
        }
        static DisciplineProgress Owned(PawnAbility ability, TMAbilityDef spell, Pawn emitter = null)
        {
            var magic = ability?.Pawn?.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            return Matches(ability?.Def, spell) && magic != null && ability.AbilityUser == magic
                && magic.AbilityData.Powers.Any(p => ReferenceEquals(p, ability)) ? Owned(ability.Pawn, spell, emitter) : null;
        }
        static bool Ally(Pawn caster, Pawn target)
            => caster != null && target != null && target != caster && !target.Dead && target.Spawned && target.Map == caster.Map
                && target.RaceProps?.Humanlike == true && target.Faction != null && target.Faction == caster.Faction
                && !target.IsPrisoner && !target.IsSlave && !TM_Calc.IsUndead(target) && target.health?.hediffSet != null;
        public sealed class Snapshot
        {
            internal Pawn caster, target;
            internal PawnAbility ability;
            internal DisciplineProgress path;
            internal float severity;
            internal bool used;
        }
        static float InjurySeverity(Pawn target) => target.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(h => h.Severity);
        public static Snapshot Capture(Pawn caster, Pawn target, PawnAbility ability)
        {
            var path = ability?.Pawn == caster && Ally(caster, target) ? Owned(ability, TorannMagicDefOf.TM_Heal) : null;
            if (path == null) return null;
            float severity = InjurySeverity(target);
            return severity >= 8 && !float.IsInfinity(severity) ? new Snapshot { caster = caster, target = target, ability = ability, path = path, severity = severity } : null;
        }
        public static bool Complete(Snapshot snapshot, bool success)
        {
            if (snapshot == null || snapshot.used) return false;
            snapshot.used = true;
            if (!success || !Ally(snapshot.caster, snapshot.target) || Owned(snapshot.ability, TorannMagicDefOf.TM_Heal) != snapshot.path) return false;
            float after = InjurySeverity(snapshot.target);
            return !float.IsNaN(after) && !float.IsInfinity(after) && snapshot.severity - after >= 8
                && snapshot.path.RecordCompletedTrial(FoundationTrialId);
        }
        public sealed class Tag : IExposable
        {
            public int casterId;
            public void ExposeData() => Scribe_Values.Look(ref casterId, "casterId", 0);
        }
        static readonly ConditionalWeakTable<Hediff, Tag> auras = new ConditionalWeakTable<Hediff, Tag>();
        public static void MarkAura(PawnAbility ability)
        {
            if (!Matches(ability?.Def, TorannMagicDefOf.TM_P_RayofHope)) return;
            var aura = ability.Pawn?.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_RayOfHope_AuraHD);
            if (aura == null) return;
            auras.Remove(aura);
            if (Owned(ability, TorannMagicDefOf.TM_P_RayofHope, aura.TryGetComp<HediffComp_Aura>()?.ProjectedHost) != null) auras.Add(aura, new Tag { casterId = ability.Pawn.thingIDNumber });
        }
        public static void ExposeAura(Hediff aura)
        {
            auras.TryGetValue(aura, out var tag);
            if (Scribe.mode == LoadSaveMode.Saving && tag == null) return;
            Scribe_Deep.Look(ref tag, "TM_paladinTrialSource");
            if (Scribe.mode != LoadSaveMode.Saving) { auras.Remove(aura); if (tag?.casterId > 0) auras.Add(aura, tag); }
        }
        public static void ApplyAura(Hediff source, Pawn recipient, HediffDef effect)
        {
            auras.TryGetValue(source, out var tag);
            var caster = source.pawn;
            var emitter = source.TryGetComp<HediffComp_Aura>()?.ProjectedHost ?? caster;
            var path = source.def == TorannMagicDefOf.TM_RayOfHope_AuraHD && tag != null && tag.casterId == caster?.thingIDNumber
                && (effect == TorannMagicDefOf.RayofHope || effect == TorannMagicDefOf.RayofHope_I || effect == TorannMagicDefOf.RayofHope_II || effect == TorannMagicDefOf.RayofHope_III)
                && Ally(emitter, recipient) && caster.health.hediffSet.hediffs.Contains(source) ? Owned(caster, TorannMagicDefOf.TM_P_RayofHope, emitter) : null;
            float before = path != null ? recipient.health.hediffSet.GetFirstHediffOfDef(effect)?.Severity ?? 0 : 0;
            // Keep the existing Arcanist/Ice observers and the single native mutation.
            ArcanistTrials.ApplyAura(source, recipient, effect);
            float after = path != null ? recipient.health.hediffSet.GetFirstHediffOfDef(effect)?.Severity ?? 0 : 0;
            if (path != null && before == 0 && after > 0 && !float.IsInfinity(after)) path.RecordCompletedTrial(MasteryTrialId);
        }
    }
    [HarmonyPatch(typeof(Verb_Heal), "TryCastShot")]
    public static class PaladinHealingTrialPatch
    {
        public static void Prefix(Verb_UseAbility __instance, out PaladinTrials.Snapshot __state)
            => __state = PaladinTrials.Capture(__instance.CasterPawn, __instance.CurrentTarget.Thing as Pawn, __instance.Ability);
        public static void Postfix(bool __result, PaladinTrials.Snapshot __state) => PaladinTrials.Complete(__state, __result);
    }
}
