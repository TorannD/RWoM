using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public sealed class LightningTrialProgress : IExposable
    {
        public float BoltDamage;
        public List<int> CloudVictims = new List<int>();
        public void ExposeData()
        {
            Scribe_Values.Look(ref BoltDamage, "boltDamage", 0f);
            Scribe_Collections.Look(ref CloudVictims, "cloudVictims", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                BoltDamage = float.IsNaN(BoltDamage) || float.IsInfinity(BoltDamage) ? 0 : Math.Max(0, Math.Min(20, BoltDamage));
                CloudVictims = (CloudVictims ?? new List<int>()).Where(id => id > 0).Distinct().Take(2).ToList();
            }
        }
    }
    public static class LightningTrials
    {
        public const string FoundationTrialId = "rwom.lightning.foundation", MasteryTrialId = "rwom.lightning.mastery";
        public enum Spell { Bolt, Cloud, Amp }
        public sealed class Tag : IExposable
        {
            public Spell spell;
            public int casterId;
            public void ExposeData() { Scribe_Values.Look(ref spell, "spell"); Scribe_Values.Look(ref casterId, "casterId", 0); }
        }
        public sealed class Snapshot
        {
            internal DisciplineProgress path;
            internal int victimId;
            internal Spell spell;
            internal bool consumed;
            internal float before;
            internal Pawn recipient;
        }
        static TMAbilityDef Ability(Spell spell) => spell == Spell.Bolt ? TorannMagicDefOf.TM_LightningBolt : spell == Spell.Cloud ? TorannMagicDefOf.TM_LightningCloud : TorannMagicDefOf.TM_AMP;
        static bool Matches(AbilityUser.AbilityDef ability, Spell spell) => ability != null && (ability == Ability(spell) || spell == Spell.Amp && (ability == TorannMagicDefOf.TM_AMP_I || ability == TorannMagicDefOf.TM_AMP_II || ability == TorannMagicDefOf.TM_AMP_III));
        static string ProjectileName(Spell spell) => spell == Spell.Bolt ? "Laser_LightningBolt" : spell == Spell.Cloud ? "Projectile_LightningCloud" : "Buff_AMP";
        static DisciplineProgress Owned(Pawn caster, Spell spell)
        {
            if (caster == null || caster.Dead || PhysicalMap(caster) == null || caster.Faction == null
                || caster.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true
                || !ClassIdentity.TryGetState(caster, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.lightning");
            var stage = spell == Spell.Cloud ? InitiationStage.Adept : InitiationStage.Foundation;
            if (path?.Domain != DisciplineDomain.Magic || path.Stage != stage || path.CompletedTrials.Contains(spell == Spell.Cloud ? MasteryTrialId : FoundationTrialId)) return null;
            var magic = caster.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            return magic?.MagicData.MagicPowersSB.Any(p => p.learned && p.TMabilityDefs.Contains(Ability(spell))) == true ? path : null;
        }
        public static Snapshot Capture(Pawn caster, Pawn victim, Spell spell)
        {
            var path = Owned(caster, spell);
            if (path == null || victim == null || victim.Dead || victim.thingIDNumber <= 0 || !victim.Spawned
                || PhysicalMap(caster) != victim.Map || victim.IsPrisoner || victim.IsSlave) return null;
            if (spell == Spell.Amp)
            {
                if (victim == caster || victim.Faction != caster.Faction || victim.health?.hediffSet == null) return null;
            }
            else if (!victim.HostileTo(caster) || (victim.Faction == null ? !victim.InAggroMentalState : victim.Faction == caster.Faction || !victim.Faction.HostileTo(caster.Faction))) return null;
            return new Snapshot { path = path, victimId = victim.thingIDNumber, spell = spell, recipient = victim, before = spell == Spell.Amp ? AmpSeverity(victim) : 0 };
        }
        public static void Complete(Snapshot snapshot, float damage)
        {
            if (snapshot == null || snapshot.consumed) return;
            snapshot.consumed = true;
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0) return;
            var path = snapshot.path;
            if (snapshot.spell == Spell.Bolt && path.Stage == InitiationStage.Foundation)
            {
                path.LightningTrials.BoltDamage = Math.Min(20, path.LightningTrials.BoltDamage + damage);
                if (path.LightningTrials.BoltDamage >= 20) path.RecordCompletedTrial(FoundationTrialId);
            }
            else if (snapshot.spell == Spell.Cloud && path.Stage == InitiationStage.Adept)
            {
                var victims = path.LightningTrials.CloudVictims;
                if (victims.Count < 2 && !victims.Contains(snapshot.victimId)) victims.Add(snapshot.victimId);
                if (victims.Count >= 2) path.RecordCompletedTrial(MasteryTrialId);
            }
            else if (snapshot.spell == Spell.Amp && path.Stage == InitiationStage.Foundation) path.RecordCompletedTrial(FoundationTrialId);
        }
        static float AmpSeverity(Pawn pawn) => pawn.health.hediffSet.hediffs.Where(h => h.def.defName == "AMP_I" || h.def.defName == "AMP_II" || h.def.defName == "AMP_III").Sum(h => h.Severity);
        private static readonly ConditionalWeakTable<Projectile_AbilityBase, Tag> projectileTags = new ConditionalWeakTable<Projectile_AbilityBase, Tag>();
        [ThreadStatic] private static Tag launching;
        public static Tag BeginLaunch(Verb_UseAbility verb, ThingDef projectileDef)
        {
            var previous = launching; launching = null;
            Spell spell;
            switch (projectileDef?.defName)
            {
                case "Laser_LightningBolt": spell = Spell.Bolt; break;
                case "Projectile_LightningCloud": spell = Spell.Cloud; break;
                case "Buff_AMP": spell = Spell.Amp; break;
                default: return previous;
            }
            if (!Matches(verb.Ability?.Def, spell) || Owned(verb.CasterPawn, spell) == null) return previous;
            var magic = verb.CasterPawn.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            if (verb.Ability.AbilityUser == magic && magic.AbilityData.Powers.Any(p => ReferenceEquals(p, verb.Ability)))
                launching = new Tag { casterId = verb.CasterPawn.thingIDNumber, spell = spell };
            return previous;
        }
        public static void EndLaunch(Tag previous) => launching = previous;
        private static Map PhysicalMap(Pawn caster) => caster?.Spawned == true ? caster.Map :
            SpiritRebirthState.TryGet(caster, out var spirit) && spirit.HasValidHost(caster) ? spirit.Host.Map : null;
        private static Pawn SourcePawn(Projectile_AbilityBase projectile) =>
            projectile is Projectile_SpiritBolt shot ? shot.SourcePawn : projectile.Caster;

        internal static void StampHosted(Projectile_AbilityBase projectile, MagicAbility ability, Spell spell)
        {
            var owner = ability.Pawn;
            if (projectile.def.defName != ProjectileName(spell) || !Matches(ability.Def, spell) || Owned(owner, spell) == null) return;
            projectileTags.Remove(projectile);
            projectileTags.Add(projectile, new Tag { casterId = owner.thingIDNumber, spell = spell });
        }
        public static void Launched(Projectile_AbilityBase projectile, Thing caster, AbilityUser.AbilityDef ability)
        {
            var tag = launching; launching = null;
            projectileTags.Remove(projectile);
            if (tag != null && caster?.thingIDNumber == tag.casterId && projectile.def?.defName == ProjectileName(tag.spell) && Matches(ability, tag.spell))
                projectileTags.Add(projectile, tag);
        }
        static Tag Source(Projectile_AbilityBase projectile, Spell spell)
        {
            projectileTags.TryGetValue(projectile, out var tag);
            return tag != null && tag.spell == spell && tag.casterId == SourcePawn(projectile)?.thingIDNumber
                && projectile.def?.defName == ProjectileName(spell) && Matches(projectile.localAbilityDef, spell) ? tag : null;
        }
        public static void ExposeProjectile(Projectile_AbilityBase projectile)
        {
            projectileTags.TryGetValue(projectile, out var tag);
            if (Scribe.mode == LoadSaveMode.Saving && tag == null) return;
            Scribe_Deep.Look(ref tag, "TM_lightningTrialSource");
            if (Scribe.mode != LoadSaveMode.Saving)
            { projectileTags.Remove(projectile); if (tag != null && tag.casterId > 0) projectileTags.Add(projectile, tag); }
        }
        public static DamageWorker.DamageResult Damage(Projectile_AbilityBase projectile, Thing victim, DamageInfo damage, Spell spell)
        {
            var snapshot = Source(projectile, spell) != null ? Capture(SourcePawn(projectile), victim as Pawn, spell) : null;
            var result = victim.TakeDamage(damage);
            Complete(snapshot, result?.totalDamageDealt ?? 0);
            return result;
        }
        public static Snapshot CaptureAmp(Projectile_AbilityBase projectile, Pawn caster, Pawn victim)
            => Source(projectile, Spell.Amp) != null && projectile.Caster == caster ? Capture(caster, victim, Spell.Amp) : null;
        public static void CompleteAmp(Snapshot snapshot)
        { if (snapshot != null) Complete(snapshot, AmpSeverity(snapshot.recipient) - snapshot.before); }
        public static void StampBoltExplosion(Explosion explosion, Projectile_AbilityBase projectile)
        { if (Source(projectile, Spell.Bolt) != null) Stamp(explosion, projectile.Caster, Spell.Bolt); }
        private static readonly ConditionalWeakTable<Explosion, Tag> tags = new ConditionalWeakTable<Explosion, Tag>();
        public static void Stamp(Explosion explosion, Pawn caster, Spell spell)
        {
            if (caster == null || caster.thingIDNumber <= 0 || explosion == null) return;
            tags.Remove(explosion); tags.Add(explosion, new Tag { casterId = caster.thingIDNumber, spell = spell });
        }
        public static void ExposeExplosion(Explosion explosion)
        {
            tags.TryGetValue(explosion, out var tag);
            if (Scribe.mode == LoadSaveMode.Saving && tag == null) return;
            Scribe_Deep.Look(ref tag, "TM_lightningTrialSource");
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                tags.Remove(explosion);
                if (tag != null && tag.casterId > 0 && (tag.spell == Spell.Bolt || tag.spell == Spell.Cloud)) tags.Add(explosion, tag);
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
                || damage.Def != (scope.tag.spell == Spell.Bolt ? TMDamageDefOf.DamageDefOf.TM_Lightning : TMDamageDefOf.DamageDefOf.TM_LightningCloud)) return hit;
            scope.used = true;
            hit.snapshot = Capture(damage.Instigator as Pawn, victim as Pawn, scope.tag.spell);
            return hit;
        }
        public static void CompleteHit(HitScope hit, DamageWorker.DamageResult result) => Complete(hit?.snapshot, result?.totalDamageDealt ?? 0);
        public static void EndHit(HitScope hit) { if (hit != null) current = hit.previous; }
    }
    [HarmonyPatch(typeof(Explosion), nameof(Explosion.ExposeData))]
    public static class LightningExplosionSavePatch { public static void Postfix(Explosion __instance) { LightningTrials.ExposeExplosion(__instance); BloodMageTrials.ExposeExplosion(__instance); } }
    [HarmonyPatch(typeof(DamageWorker), "ExplosionDamageThing")]
    public static class LightningExplosionDamagePatch
    {
        public struct Scope { public LightningTrials.DamageScope lightning; public BloodMageTrials.DamageScope blood; }
        // ConditionalWeakTable throws on a null key; a missing explosion is simply
        // never a tagged cast, so enter nothing instead of throwing from the prefix.
        public static void Prefix(Explosion explosion, Thing t, out Scope __state)
            => __state = explosion == null ? default
                : new Scope { lightning = LightningTrials.EnterExplosion(explosion, t), blood = BloodMageTrials.EnterExplosion(explosion, t) };
        public static Exception Finalizer(Exception __exception, Scope __state) { LightningTrials.ExitExplosion(__state.lightning); BloodMageTrials.ExitExplosion(__state.blood); return __exception; }
    }
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class LightningDamagePatch
    {
        public struct Scope { public LightningTrials.HitScope lightning; public BloodMageTrials.HitScope blood; public BrightmageTrials.HitScope bright; }
        public static void Prefix(Thing __instance, DamageInfo dinfo, out Scope __state)
            => __state = new Scope { lightning = LightningTrials.BeginHit(__instance, dinfo), blood = BloodMageTrials.BeginHit(__instance, dinfo), bright = BrightmageTrials.BeginHit(__instance, dinfo) };
        public static void Postfix(DamageWorker.DamageResult __result, Scope __state) { LightningTrials.CompleteHit(__state.lightning, __result); BloodMageTrials.CompleteHit(__state.blood, __result); BrightmageTrials.CompleteHit(__state.bright, __result); }
        public static Exception Finalizer(Exception __exception, Scope __state) { LightningTrials.EndHit(__state.lightning); BloodMageTrials.EndHit(__state.blood); BrightmageTrials.EndHit(__state.bright); return __exception; }
    }
}

namespace TorannMagic.ClassProgression
{
    [HarmonyPatch(typeof(Verb_UseAbility), nameof(Verb_UseAbility.TryLaunchProjectileCheck))]
    public static class LightningLaunchScopePatch
    {
        public struct Scope { public LightningTrials.Tag lightning; public ArcanistTrials.Tag arcanist; public SummonerTrials.Tag summoner; public DemonTrials.Tag demon; public GeomancerTrials.Tag geomancer; public BloodMageTrials.Tag blood; }
        public static void Prefix(Verb_UseAbility __instance, ThingDef projectileDef, out Scope __state)
            => __state = new Scope { lightning = LightningTrials.BeginLaunch(__instance, projectileDef), arcanist = ArcanistTrials.BeginLaunch(__instance, projectileDef), summoner = SummonerTrials.BeginLaunch(__instance, projectileDef), demon = DemonTrials.BeginLaunch(__instance, projectileDef), geomancer = GeomancerTrials.BeginLaunch(__instance, projectileDef), blood = BloodMageTrials.BeginLaunch(__instance, projectileDef) };
        public static Exception Finalizer(Exception __exception, Scope __state)
        { LightningTrials.EndLaunch(__state.lightning); ArcanistTrials.EndLaunch(__state.arcanist); SummonerTrials.EndLaunch(__state.summoner); DemonTrials.EndLaunch(__state.demon); GeomancerTrials.EndLaunch(__state.geomancer); BloodMageTrials.EndLaunch(__state.blood); return __exception; }
    }
    [HarmonyPatch(typeof(Projectile_AbilityBase), nameof(Projectile_AbilityBase.Launch), new Type[] { typeof(Thing), typeof(AbilityUser.AbilityDef), typeof(UnityEngine.Vector3), typeof(LocalTargetInfo), typeof(ProjectileHitFlags), typeof(bool), typeof(Thing), typeof(List<ApplyHediffs>), typeof(List<ApplyMentalStates>), typeof(List<SpawnThings>) })]
    public static class LightningLaunchPatch
    {
        public static void Postfix(Projectile_AbilityBase __instance, Thing launcher, AbilityUser.AbilityDef abilityDef)
        { LightningTrials.Launched(__instance, launcher, abilityDef); ArcanistTrials.Launched(__instance, launcher, abilityDef); SummonerTrials.Launched(__instance, launcher, abilityDef); DemonTrials.Launched(__instance, launcher, abilityDef); GeomancerTrials.Launched(__instance, launcher, abilityDef); BloodMageTrials.Launched(__instance, launcher, abilityDef); }
    }
    [HarmonyPatch(typeof(Projectile_AbilityBase), nameof(Projectile_AbilityBase.ExposeData))]
    public static class LightningProjectileSavePatch { public static void Postfix(Projectile_AbilityBase __instance) { LightningTrials.ExposeProjectile(__instance); ArcanistTrials.ExposeProjectile(__instance); SummonerTrials.ExposeProjectile(__instance); DemonTrials.ExposeProjectile(__instance); GeomancerTrials.ExposeProjectile(__instance); BloodMageTrials.ExposeProjectile(__instance); } }
    [HarmonyPatch(typeof(Projectile_AbilityBase), nameof(Projectile_AbilityBase.ApplyHediffsAndMentalStates))]
    public static class LightningAmpPatch
    {
        public static void Prefix(Projectile_AbilityBase __instance, Pawn caster, Pawn victim, out LightningTrials.Snapshot __state) => __state = LightningTrials.CaptureAmp(__instance, caster, victim);
        public static void Postfix(LightningTrials.Snapshot __state) => LightningTrials.CompleteAmp(__state);
    }
}


