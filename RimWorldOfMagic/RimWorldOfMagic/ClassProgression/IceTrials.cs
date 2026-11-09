using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public sealed class IceTrialProgress : IExposable
    {
        public float IceboltDamage;
        public List<int> SnowballVictims = new List<int>();
        public void ExposeData()
        {
            Scribe_Values.Look(ref IceboltDamage, "iceboltDamage", 0f);
            Scribe_Collections.Look(ref SnowballVictims, "snowballVictims", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                IceboltDamage = float.IsNaN(IceboltDamage) || float.IsInfinity(IceboltDamage) ? 0 : Math.Max(0, Math.Min(20, IceboltDamage));
                SnowballVictims = (SnowballVictims ?? new List<int>()).Where(id => id > 0).Distinct().Take(2).ToList();
            }
        }
    }

    public static class IceTrials
    {
        public const string FoundationTrialId = "rwom.ice.foundation";
        public const string MasteryTrialId = "rwom.ice.mastery";
        public enum Spell { Icebolt, Snowball }
        public sealed class Tag : IExposable
        {
            public Spell spell;
            public int casterId;
            public void ExposeData()
            { Scribe_Values.Look(ref spell, "spell"); Scribe_Values.Look(ref casterId, "casterId", 0); }
        }
        public sealed class Snapshot
        {
            internal DisciplineProgress path;
            internal int victimId;
            internal Spell spell;
            internal bool consumed;
        }
        private static DisciplineProgress Owned(Pawn caster, InitiationStage stage, TMAbilityDef ability)
        {
            if (caster == null || caster.Dead || caster.Faction == null ||
                (!caster.Spawned && !((ability == TorannMagicDefOf.TM_Icebolt || ability == TorannMagicDefOf.TM_Snowball || ability == TorannMagicDefOf.TM_Rainmaker || ability == TorannMagicDefOf.TM_Soothe) &&
                    SpiritRebirthState.TryGet(caster,out var spirit) && spirit.HasValidHost(caster) && spirit.Host.Spawned))
                || caster.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true
                || !ClassIdentity.TryGetState(caster, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.ice");
            var trial = stage == InitiationStage.Foundation ? FoundationTrialId : MasteryTrialId;
            if (path?.Domain != DisciplineDomain.Magic || path.Stage != stage || path.CompletedTrials.Contains(trial)) return null;
            var magic = caster.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            return magic?.MagicData.MagicPowersHoF.Any(p => p.learned && p.TMabilityDefs.Contains(ability)) == true ? path : null;
        }
        public static Snapshot Capture(Pawn caster, Pawn victim, Spell spell)
        {
            var path = Owned(caster, spell == Spell.Icebolt ? InitiationStage.Foundation : InitiationStage.Adept,
                spell == Spell.Icebolt ? TorannMagicDefOf.TM_Icebolt : TorannMagicDefOf.TM_Snowball);
            if (path == null || victim == null || victim.Dead || victim.thingIDNumber <= 0 || !victim.Spawned
                || caster.MapHeld != victim.Map || victim.IsPrisoner || victim.IsSlave || !victim.HostileTo(caster)
                || (victim.Faction == null ? !victim.InAggroMentalState : victim.Faction == caster.Faction || !victim.Faction.HostileTo(caster.Faction))) return null;
            return new Snapshot { path = path, victimId = victim.thingIDNumber, spell = spell };
        }
        public static void Complete(Snapshot snapshot, float damage)
        {
            if (snapshot == null || snapshot.consumed) return;
            snapshot.consumed = true;
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0) return;
            var path = snapshot.path;
            if (snapshot.spell == Spell.Icebolt && path.Stage == InitiationStage.Foundation)
            {
                path.IceTrials.IceboltDamage = Math.Min(20, path.IceTrials.IceboltDamage + damage);
                if (path.IceTrials.IceboltDamage >= 20) path.RecordCompletedTrial(FoundationTrialId);
            }
            else if (snapshot.spell == Spell.Snowball && path.Stage == InitiationStage.Adept)
            {
                var victims = path.IceTrials.SnowballVictims;
                if (victims.Count < 2 && !victims.Contains(snapshot.victimId)) victims.Add(snapshot.victimId);
                if (victims.Count >= 2) path.RecordCompletedTrial(MasteryTrialId);
            }
        }
        // Called only around the native aura's recipient mutation; toggling the caster aura is not proof.
        public static void ApplyAura(Hediff source, Pawn recipient, HediffDef effect)
        {
            var caster = source?.pawn;
            var emitter = source?.TryGetComp<HediffComp_Aura>()?.ProjectedHost ?? caster;
            var path = source?.def == TorannMagicDefOf.TM_SoothingBreeze_AuraHD
                && (effect == TorannMagicDefOf.SoothingBreeze || effect == TorannMagicDefOf.SoothingBreeze_I
                    || effect == TorannMagicDefOf.SoothingBreeze_II || effect == TorannMagicDefOf.SoothingBreeze_III)
                && recipient != emitter && recipient != null && !recipient.Dead && recipient.Spawned
                && recipient.Map == emitter?.Map && recipient.Faction == emitter?.Faction && !recipient.IsPrisoner && !recipient.IsSlave
                && caster.health.hediffSet.hediffs.Contains(source)
                ? Owned(caster, InitiationStage.Foundation, TorannMagicDefOf.TM_Soothe) : null;
            if (path == null) { HealthUtility.AdjustSeverity(recipient, effect, 1f); return; }
            float before = recipient.health.hediffSet.GetFirstHediffOfDef(effect)?.Severity ?? 0;
            HealthUtility.AdjustSeverity(recipient, effect, 1f);
            float after = recipient.health.hediffSet.GetFirstHediffOfDef(effect)?.Severity ?? 0;
            if (path != null && before == 0 && after > before && !float.IsInfinity(after)) path.RecordCompletedTrial(FoundationTrialId);
        }
        public static void TransitionWeather(Pawn caster, Map map, WeatherDef weather)
        {
            var path = caster?.MapHeld == map ? Owned(caster, InitiationStage.Adept, TorannMagicDefOf.TM_Rainmaker) : null;
            var before = map.weatherManager.curWeather;
            map.weatherManager.TransitionTo(weather);
            if (path != null && before != weather && map.weatherManager.curWeather == weather) path.RecordCompletedTrial(MasteryTrialId);
        }

        private static readonly ConditionalWeakTable<Explosion, Tag> tags = new ConditionalWeakTable<Explosion, Tag>();
        [ThreadStatic] private static Tag creating;
        public sealed class CreationScope : IDisposable
        {
            private readonly Tag previous;
            internal CreationScope(Tag tag) { previous = creating; creating = tag; }
            public void Dispose() { creating = previous; }
        }
        public static CreationScope Creating(Pawn caster, Spell spell) => new CreationScope(new Tag { casterId = caster?.thingIDNumber ?? 0, spell = spell });
        public static void Stamp(Explosion explosion, Pawn caster, Spell spell)
        {
            if (caster == null || caster.thingIDNumber <= 0 || explosion == null) return;
            tags.Remove(explosion); tags.Add(explosion, new Tag { casterId = caster.thingIDNumber, spell = spell });
        }
        public static void StartExplosion(Explosion explosion)
        {
            var tag = creating;
            creating = null; // Only the direct creation can claim this scope; nested explosions cannot.
            if (tag != null && explosion.instigator is Pawn caster && caster.thingIDNumber == tag.casterId
                && explosion.damType == (tag.spell == Spell.Icebolt ? TMDamageDefOf.DamageDefOf.Iceshard : TMDamageDefOf.DamageDefOf.Snowball))
                Stamp(explosion, caster, tag.spell);
        }
        public static void ExposeExplosion(Explosion explosion)
        {
            tags.TryGetValue(explosion, out var tag);
            if (Scribe.mode == LoadSaveMode.Saving && tag == null) return;
            Scribe_Deep.Look(ref tag, "TM_iceTrialSource");
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                tags.Remove(explosion);
                if (tag != null && tag.casterId > 0 && (tag.spell == Spell.Icebolt || tag.spell == Spell.Snowball)) tags.Add(explosion, tag);
            }
        }
        public sealed class DamageScope { internal Explosion explosion; internal Thing victim; internal Tag tag; internal bool used; }
        public sealed class HitScope { internal DamageScope previous; internal Snapshot snapshot; }
        [ThreadStatic] private static DamageScope current;
        public static DamageScope EnterExplosion(Explosion explosion, Thing victim)
        {
            var previous = current;
            tags.TryGetValue(explosion, out var tag);
            current = tag == null ? null : new DamageScope { explosion = explosion, victim = victim, tag = tag };
            return previous;
        }
        public static void ExitExplosion(DamageScope previous) => current = previous;
        public static HitScope BeginHit(Thing victim, DamageInfo damage)
        {
            var scope = current;
            if (scope == null) return null;
            current = null;
            var hit = new HitScope { previous = scope };
            if (scope.used || !ReferenceEquals(scope.victim, victim) || damage.Instigator != scope.explosion.instigator
                || damage.Instigator?.thingIDNumber != scope.tag.casterId || damage.Def != scope.explosion.damType
                || damage.Def != (scope.tag.spell == Spell.Icebolt ? TMDamageDefOf.DamageDefOf.Iceshard : TMDamageDefOf.DamageDefOf.Snowball)) return hit;
            scope.used = true;
            hit.snapshot = Capture(damage.Instigator as Pawn, victim as Pawn, scope.tag.spell);
            return hit;
        }
        public static void CompleteHit(HitScope hit, DamageWorker.DamageResult result) => Complete(hit?.snapshot, result?.totalDamageDealt ?? 0);
        public static void EndHit(HitScope hit) { if (hit != null) current = hit.previous; }
    }
    [HarmonyPatch(typeof(Explosion), nameof(Explosion.StartExplosion))]
    public static class IceExplosionStartPatch { public static void Prefix(Explosion __instance) { IceTrials.StartExplosion(__instance); BloodMageTrials.StartExplosion(__instance); } }
    [HarmonyPatch(typeof(Explosion), nameof(Explosion.ExposeData))]
    public static class IceExplosionSavePatch { public static void Postfix(Explosion __instance) => IceTrials.ExposeExplosion(__instance); }
    [HarmonyPatch(typeof(DamageWorker), "ExplosionDamageThing")]
    public static class IceExplosionDamagePatch
    {
        // ConditionalWeakTable throws on a null key; a missing explosion is simply
        // never a tagged cast, so enter nothing instead of throwing from the prefix.
        public static void Prefix(Explosion explosion, Thing t, out IceTrials.DamageScope __state) => __state = explosion == null ? null : IceTrials.EnterExplosion(explosion, t);
        public static Exception Finalizer(Exception __exception, IceTrials.DamageScope __state) { IceTrials.ExitExplosion(__state); return __exception; }
    }
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class IceDamagePatch
    {
        public static void Prefix(Thing __instance, DamageInfo dinfo, out IceTrials.HitScope __state) => __state = IceTrials.BeginHit(__instance, dinfo);
        public static void Postfix(DamageWorker.DamageResult __result, IceTrials.HitScope __state) => IceTrials.CompleteHit(__state, __result);
        public static Exception Finalizer(Exception __exception, IceTrials.HitScope __state) { IceTrials.EndHit(__state); return __exception; }
    }
}
