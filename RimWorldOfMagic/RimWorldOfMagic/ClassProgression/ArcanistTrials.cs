using System;
using System.Linq;
using System.Runtime.CompilerServices;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public sealed class ArcanistTrialProgress : IExposable
    {
        public float MissileDamage;
        public void ExposeData()
        {
            Scribe_Values.Look(ref MissileDamage, "missileDamage", 0f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                MissileDamage = float.IsNaN(MissileDamage) || float.IsInfinity(MissileDamage) ? 0 : Math.Max(0, Math.Min(20, MissileDamage));
        }
    }
    public static class ArcanistTrials
    {
        public const string FoundationTrialId = "rwom.arcanist.foundation", MasteryTrialId = "rwom.arcanist.mastery";
        public enum Spell { Missile, Blink, Shadow, Summon }
        static TMAbilityDef BaseAbility(Spell spell) => spell == Spell.Missile ? TorannMagicDefOf.TM_MagicMissile : spell == Spell.Blink ? TorannMagicDefOf.TM_Blink : spell == Spell.Shadow ? TorannMagicDefOf.TM_Shadow : TorannMagicDefOf.TM_Summon;
        static bool Matches(AbilityUser.AbilityDef def, Spell spell)
        {
            string root = BaseAbility(spell)?.defName;
            return def != null && root != null && (def.defName == root || def.defName == root + "_I" || def.defName == root + "_II" || def.defName == root + "_III");
        }
        static DisciplineProgress Owned(Pawn pawn, Spell spell)
        {
            if (pawn == null || pawn.Dead || CastingBody(pawn)?.Spawned != true || pawn.Faction == null || pawn.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true
                || !ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.arcanist");
            var stage = spell == Spell.Missile || spell == Spell.Blink ? InitiationStage.Foundation : InitiationStage.Adept;
            if (path?.Domain != DisciplineDomain.Magic || path.Stage != stage || path.CompletedTrials.Contains(stage == InitiationStage.Foundation ? FoundationTrialId : MasteryTrialId)) return null;
            var magic = pawn.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            return magic?.MagicData.MagicPowersA.Any(p => p.learned && p.TMabilityDefs.Contains(BaseAbility(spell))) == true ? path : null;
        }
        static DisciplineProgress Owned(PawnAbility ability, Spell spell)
        {
            var pawn = ability?.Pawn;
            var magic = pawn?.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            return Matches(ability?.Def, spell) && magic != null && ability.AbilityUser == magic && magic.AbilityData.Powers.Any(p => ReferenceEquals(p, ability)) ? Owned(pawn, spell) : null;
        }
        public sealed class Movement
        {
            internal DisciplineProgress path;
            internal Pawn caster;
            internal Map map;
            internal IntVec3 origin, destination;
            internal bool used;
        }
        private static Pawn CastingBody(Pawn caster)
        {
            if (caster == null || caster.Spawned) return caster;
            return SpiritRebirthState.TryGet(caster, out var state) && state.HasValidHost(caster) ? state.Host : null;
        }
        public static Movement CaptureMove(PawnAbility ability, Thing target, Spell spell)
        {
            var path = Owned(ability, spell);
            var body = CastingBody(ability?.Pawn);
            if (path == null || body == null || target == null || !target.Spawned || target.Map != body.Map
                || (spell != Spell.Blink && spell != Spell.Summon) || (spell == Spell.Blink ? target != body : target.def.category != ThingCategory.Item)) return null;
            return new Movement { path = path, caster = body, map = target.Map, origin = target.Position, destination = body.Position };
        }
        public static void CompleteBlink(Movement move)
        {
            if (move == null || move.used) return;
            move.used = true;
            if (move.path.Stage == InitiationStage.Foundation && move.caster.Spawned && move.caster.Map == move.map && (move.caster.Position - move.origin).LengthHorizontal >= 8)
                move.path.RecordCompletedTrial(FoundationTrialId);
        }
        public static void CompleteSummon(Movement move, Thing placed, bool success)
        {
            if (move == null || move.used) return;
            move.used = true;
            if (success && placed != null && placed.Spawned && placed.Map == move.map && placed.def.category == ThingCategory.Item && move.path.Stage == InitiationStage.Adept
                && (move.origin - move.destination).LengthHorizontal >= 8
                && (placed.Position - move.destination).LengthHorizontal < (move.origin - move.destination).LengthHorizontal)
                move.path.RecordCompletedTrial(MasteryTrialId);
        }
        public sealed class Tag : IExposable
        {
            public int casterId;
            public void ExposeData() => Scribe_Values.Look(ref casterId, "casterId", 0);
        }
        static readonly ConditionalWeakTable<Hediff, Tag> auras = new ConditionalWeakTable<Hediff, Tag>();
        public static void MarkAura(PawnAbility ability)
        {
            if (!Matches(ability?.Def, Spell.Shadow)) return;
            var aura = ability?.Pawn?.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_Shadow_AuraHD);
            if (aura == null) return;
            auras.Remove(aura);
            if (Owned(ability, Spell.Shadow) != null) auras.Add(aura, new Tag { casterId = ability.Pawn.thingIDNumber });
        }
        public static void ExposeAura(Hediff aura)
        {
            auras.TryGetValue(aura, out var tag);
            if (Scribe.mode == LoadSaveMode.Saving && tag == null) return;
            Scribe_Deep.Look(ref tag, "TM_arcanistTrialSource");
            if (Scribe.mode != LoadSaveMode.Saving) { auras.Remove(aura); if (tag?.casterId > 0) auras.Add(aura, tag); }
        }
        public static void ApplyAura(Hediff source, Pawn recipient, HediffDef effect)
        {
            auras.TryGetValue(source, out var tag);
            var caster = source.pawn;
            var emitter = source.TryGetComp<HediffComp_Aura>()?.ProjectedHost ?? caster;
            var path = source.def == TorannMagicDefOf.TM_Shadow_AuraHD && tag?.casterId == caster?.thingIDNumber
                && (effect == TorannMagicDefOf.Shadow || effect == TorannMagicDefOf.Shadow_I || effect == TorannMagicDefOf.Shadow_II || effect == TorannMagicDefOf.Shadow_III)
                && recipient != null && recipient != emitter && !recipient.Dead && recipient.Spawned && recipient.Map == emitter.Map && recipient.Faction == emitter.Faction
                && !recipient.IsPrisoner && !recipient.IsSlave && !recipient.RaceProps.Animal && caster.health.hediffSet.hediffs.Contains(source) ? Owned(caster, Spell.Shadow) : null;
            float before = path != null ? recipient.health.hediffSet.GetFirstHediffOfDef(effect)?.Severity ?? 0 : 0;
            IceTrials.ApplyAura(source, recipient, effect);
            float after = path != null ? recipient.health.hediffSet.GetFirstHediffOfDef(effect)?.Severity ?? 0 : 0;
            if (path != null && before == 0 && after > 0 && !float.IsInfinity(after)) path.RecordCompletedTrial(MasteryTrialId);
        }
        static readonly ConditionalWeakTable<Projectile_AbilityBase, Tag> projectiles = new ConditionalWeakTable<Projectile_AbilityBase, Tag>();
        [ThreadStatic] static Tag launching;
        public static Tag BeginLaunch(Verb_UseAbility verb, ThingDef def)
        {
            var previous = launching; launching = null;
            if (def?.defName == "Projectile_MagicMissile" && Owned(verb.Ability, Spell.Missile) != null)
                launching = new Tag { casterId = verb.CasterPawn.thingIDNumber };
            return previous;
        }
        public static void EndLaunch(Tag previous) => launching = previous;
        internal static void StampHostedMissile(Projectile_AbilityBase missile, MagicAbility ability)
        {
            projectiles.Remove(missile);
            if (Owned(ability, Spell.Missile) != null) projectiles.Add(missile, new Tag { casterId = ability.Pawn.thingIDNumber });
        }
        public static void Launched(Projectile_AbilityBase projectile, Thing caster, AbilityUser.AbilityDef ability)
        {
            var tag = launching; launching = null; projectiles.Remove(projectile);
            if (tag?.casterId == caster?.thingIDNumber && tag != null && projectile.def?.defName == "Projectile_MagicMissile" && Matches(ability, Spell.Missile)) projectiles.Add(projectile, tag);
        }
        public static void ExposeProjectile(Projectile_AbilityBase projectile)
        {
            projectiles.TryGetValue(projectile, out var tag);
            if (Scribe.mode == LoadSaveMode.Saving && tag == null) return;
            Scribe_Deep.Look(ref tag, "TM_arcanistTrialSource");
            if (Scribe.mode != LoadSaveMode.Saving) { projectiles.Remove(projectile); if (tag?.casterId > 0) projectiles.Add(projectile, tag); }
        }
        public sealed class Impact { internal Pawn caster; internal Thing victim; }
        public sealed class Hit { internal Impact previous; internal DisciplineProgress path; internal bool used; }
        [ThreadStatic] static Impact impact;
        public static Impact EnterImpact(Projectile_AbilityBase projectile, Thing victim)
        {
            var previous = impact; impact = null;
            projectiles.TryGetValue(projectile, out var tag);
            if (tag != null && tag.casterId == projectile.Caster?.thingIDNumber && projectile.def?.defName == "Projectile_MagicMissile" && Matches(projectile.localAbilityDef, Spell.Missile))
                impact = new Impact { caster = projectile.Caster, victim = victim };
            return previous;
        }
        public static void ExitImpact(Impact previous) => impact = previous;
        public static Hit BeginHit(Thing victim, DamageInfo damage)
        {
            var source = impact;
            if (source == null) return null;
            impact = null;
            var hit = new Hit { previous = source };
            var pawn = victim as Pawn; var caster = source.caster;
            if (source.victim == victim && damage.Instigator == caster && damage.Def?.defName == "TM_MagicMissile" && pawn != null && !pawn.Dead && pawn.Spawned && pawn.Map == CastingBody(caster)?.Map
                && !pawn.IsPrisoner && !pawn.IsSlave && pawn.HostileTo(caster) && (pawn.Faction == null ? pawn.InAggroMentalState : pawn.Faction != caster.Faction && pawn.Faction.HostileTo(caster.Faction)))
                hit.path = Owned(caster, Spell.Missile);
            return hit;
        }
        public static void CompleteHit(Hit hit, float damage)
        {
            if (hit == null || hit.used) return;
            hit.used = true;
            if (hit.path == null || hit.path.Stage != InitiationStage.Foundation || float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0) return;
            hit.path.ArcanistTrials.MissileDamage = Math.Min(20, hit.path.ArcanistTrials.MissileDamage + damage);
            if (hit.path.ArcanistTrials.MissileDamage >= 20) hit.path.RecordCompletedTrial(FoundationTrialId);
        }
        public static void EndHit(Hit hit) { if (hit != null) impact = hit.previous; }
    }
    [HarmonyPatch(typeof(Projectile_AbilityBase), "Impact")]
    public static class ArcanistImpactPatch
    {
        public static void Prefix(Projectile_AbilityBase __instance, Thing hitThing, out ArcanistTrials.Impact __state) => __state = ArcanistTrials.EnterImpact(__instance, hitThing);
        public static Exception Finalizer(Exception __exception, ArcanistTrials.Impact __state) { ArcanistTrials.ExitImpact(__state); return __exception; }
    }
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class ArcanistDamagePatch
    {
        public static void Prefix(Thing __instance, DamageInfo dinfo, out ArcanistTrials.Hit __state) => __state = ArcanistTrials.BeginHit(__instance, dinfo);
        public static void Postfix(DamageWorker.DamageResult __result, ArcanistTrials.Hit __state) => ArcanistTrials.CompleteHit(__state, __result?.totalDamageDealt ?? 0);
        public static Exception Finalizer(Exception __exception, ArcanistTrials.Hit __state) { ArcanistTrials.EndHit(__state); return __exception; }
    }
}
