using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public sealed class FireclawCastWitness : IExposable
    {
        public int castId;
        public int firstVictimId;
        public void ExposeData()
        {
            Scribe_Values.Look(ref castId, "castId", 0);
            Scribe_Values.Look(ref firstVictimId, "firstVictimId", 0);
        }
    }

    public static class FireclawTrial
    {
        public const string TrialId = "rwom.fire.fireclaw-pair";
        public const int WitnessLimit = 32;
        public sealed class CastTag : IExposable
        {
            public int castId;
            public void ExposeData() => Scribe_Values.Look(ref castId, "castId", 0);
        }
        public sealed class Snapshot
        {
            internal DisciplineProgress path;
            internal int castId, victimId;
            internal bool consumed;
        }
        public sealed class DamageScope
        {
            internal Explosion explosion;
            internal Thing victim;
            internal CastTag tag;
            internal bool used;
        }
        public sealed class HitScope
        {
            internal DamageScope previous;
            internal Snapshot snapshot;
        }
        private static readonly ConditionalWeakTable<Explosion, CastTag> tags = new ConditionalWeakTable<Explosion, CastTag>();
        [ThreadStatic] private static DamageScope current;

        // Every tendril explosion from this projectile shares its native persistent thing ID.
        public static void Stamp(Explosion explosion, int projectileId)
        {
            if (explosion == null || projectileId <= 0) return;
            tags.Remove(explosion);
            tags.Add(explosion, new CastTag { castId = projectileId });
        }

        public static void ExposeExplosion(Explosion explosion)
        {
            tags.TryGetValue(explosion, out var tag);
            if (Scribe.mode == LoadSaveMode.Saving && tag == null) return;
            Scribe_Deep.Look(ref tag, "TM_fireclawCast");
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                tags.Remove(explosion);
                if (tag != null && tag.castId > 0) tags.Add(explosion, tag);
            }
        }

        // An ordinary nested explosion deliberately masks the outer tagged explosion.
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
            // A hit can recursively cause further damage. Only this direct engine call
            // belongs to the worker's target; restore the scope in its finalizer.
            current = null;
            var hit = new HitScope { previous = scope };
            if (scope?.tag == null || scope.used || !ReferenceEquals(scope.victim, victim)
                || damage.Instigator != scope.explosion.instigator || damage.Def != scope.explosion.damType) return hit;
            scope.used = true;
            hit.snapshot = Capture(damage.Instigator as Pawn, victim as Pawn, scope.tag.castId);
            return hit;
        }
        public static void CompleteHit(HitScope hit, DamageWorker.DamageResult result)
            => Complete(hit?.snapshot, result?.totalDamageDealt ?? 0f);
        public static void EndHit(HitScope hit) => current = hit?.previous;

        public static Snapshot Capture(Pawn caster, Pawn victim, int castId)
        {
            if (caster == null) return null;
            var location = caster.Spawned ? caster.Map : SpiritRebirthState.TryGet(caster, out var spirit) &&
                spirit.HasValidHost(caster) ? spirit.Host.Map : null;
            if (castId <= 0 || !ClassIdentity.TryGetState(caster, out var state) || (state.Vocation & Vocation.Mage) == 0
                || caster.Dead || victim == null || victim.Dead || victim.thingIDNumber <= 0
                || !victim.Spawned || location != victim.Map
                || caster.Faction == null || victim.IsPrisoner || victim.IsSlave || !victim.HostileTo(caster)
                || (victim.Faction == null ? !victim.InAggroMentalState
                    : victim.Faction == caster.Faction || !victim.Faction.HostileTo(caster.Faction))) return null;
            var path = state.Find("rwom.fire");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage != InitiationStage.Adept || path.CompletedTrials.Contains(TrialId)) return null;
            var magic = caster.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            if (magic?.MagicData.MagicPowersIF.Any(p => p.learned && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Fireclaw)) != true) return null;
            return new Snapshot { path = path, castId = castId, victimId = victim.thingIDNumber };
        }
        public static void Complete(Snapshot snapshot, float damage)
        {
            if (snapshot == null || snapshot.consumed) return;
            snapshot.consumed = true;
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0) return;
            snapshot.path.RecordFireclawVictim(snapshot.castId, snapshot.victimId);
        }
    }

    [HarmonyPatch(typeof(Explosion), nameof(Explosion.ExposeData))]
    public static class FireclawExplosionSavePatch
    {
        public static void Postfix(Explosion __instance) => FireclawTrial.ExposeExplosion(__instance);
    }

    [HarmonyPatch(typeof(DamageWorker), "ExplosionDamageThing")]
    public static class FireclawExplosionDamagePatch
    {
        public static void Prefix(Explosion explosion, Thing t, out FireclawTrial.DamageScope __state)
            // ConditionalWeakTable throws on a null key; a missing explosion is simply
            // never a tagged cast, so enter nothing instead of throwing from the prefix.
            => __state = explosion == null ? null : FireclawTrial.EnterExplosion(explosion, t);
        public static Exception Finalizer(Exception __exception, FireclawTrial.DamageScope __state)
        { FireclawTrial.ExitExplosion(__state); return __exception; }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class FireclawDamagePatch
    {
        public static void Prefix(Thing __instance, DamageInfo dinfo, out FireclawTrial.HitScope __state)
            => __state = FireclawTrial.BeginHit(__instance, dinfo);
        public static void Postfix(DamageWorker.DamageResult __result, FireclawTrial.HitScope __state)
            => FireclawTrial.CompleteHit(__state, __result);
        public static Exception Finalizer(Exception __exception, FireclawTrial.HitScope __state)
        { FireclawTrial.EndHit(__state); return __exception; }
    }
}
