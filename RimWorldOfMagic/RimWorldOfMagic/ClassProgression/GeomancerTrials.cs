using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AbilityUser;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class GeomancerTrials
    {
        public const string FoundationTrialId = "rwom.geomancer.foundation", MasteryTrialId = "rwom.geomancer.mastery";
        static bool Matches(AbilityUser.AbilityDef def, bool encase) => def != null && (encase
            ? def == TorannMagicDefOf.TM_Encase || def == TorannMagicDefOf.TM_Encase_I || def == TorannMagicDefOf.TM_Encase_II || def == TorannMagicDefOf.TM_Encase_III
            : def == TorannMagicDefOf.TM_Stoneskin);
        static Pawn WardBody(Pawn caster)
        {
            if (!SpiritRebirthState.TryGet(caster,out var state)) return caster?.Spawned == true ? caster : null;
            return !state.DepletionPending && state.HasValidHost(caster) && state.Host.Spawned ? state.Host : null;
        }
        static DisciplineProgress Owned(Pawn caster, AbilityUser.AbilityDef def, bool encase)
        {
            if (!Matches(def, encase) || caster == null || caster.Dead || WardBody(caster) == null || caster.Faction == null
                || caster.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true
                || !ClassIdentity.TryGetState(caster, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.geomancer");
            var stage = encase ? InitiationStage.Adept : InitiationStage.Foundation;
            var magic = caster.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            return path?.Domain == DisciplineDomain.Magic && path.Stage == stage
                && !path.CompletedTrials.Contains(encase ? MasteryTrialId : FoundationTrialId)
                && magic?.MagicData.MagicPowersG.Any(p => p.learned && p.TMabilityDefs.Contains(def as TMAbilityDef)) == true ? path : null;
        }
        static DisciplineProgress Owned(PawnAbility ability, bool encase)
        {
            var magic = ability?.Pawn?.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            return magic != null && ability.AbilityUser == magic && magic.AbilityData.Powers.Any(p => ReferenceEquals(p, ability))
                ? Owned(ability.Pawn, ability.Def, encase) : null;
        }
        static bool Ally(Pawn caster, Pawn target) => target != null && target != caster && !target.Dead && target.Spawned
            && target.Map == caster.Map && target.RaceProps?.Humanlike == true && target.Faction == caster.Faction
            && !target.IsPrisoner && !target.IsSlave && !TM_Calc.IsUndead(target) && target.health?.hediffSet != null;
        static Hediff Ward(Pawn target) => target.health.hediffSet.hediffs.FirstOrDefault(h => h.def.defName == "TM_StoneskinHD");
        public sealed class Snapshot { internal PawnAbility ability; internal Pawn target, body; internal DisciplineProgress path; internal bool used; }
        public static Snapshot CaptureStoneskin(PawnAbility ability, Pawn target)
        {
            var path = Owned(ability, false);
            var body = WardBody(ability?.Pawn);
            return path != null && body != null && Ally(body, target) && Ward(target) == null
                ? new Snapshot { ability = ability, target = target, body = body, path = path } : null;
        }
        public static bool CompleteStoneskin(Snapshot snapshot)
        {
            if (snapshot == null || snapshot.used) return false;
            snapshot.used = true;
            var caster = snapshot.ability.Pawn;
            var ward = WardBody(caster) == snapshot.body && Ally(snapshot.body, snapshot.target) ? Ward(snapshot.target) : null;
            return Owned(snapshot.ability, false) == snapshot.path && ward != null && ward.Severity > 0 && !float.IsInfinity(ward.Severity)
                && caster.AllComps.OfType<CompAbilityUserMagic>().First().stoneskinPawns.Contains(snapshot.target)
                && snapshot.path.RecordCompletedTrial(FoundationTrialId);
        }
        public sealed class Tag : IExposable
        {
            public int casterId;
            public string abilityName;
            public List<IntVec3> cells = new List<IntVec3>();
            public void ExposeData()
            {
                Scribe_Values.Look(ref casterId, "casterId", 0);
                Scribe_Values.Look(ref abilityName, "abilityName");
                Scribe_Collections.Look(ref cells, "cells", LookMode.Value);
                if (Scribe.mode == LoadSaveMode.PostLoadInit) cells = (cells ?? new List<IntVec3>()).Where(c => c.IsValid).Distinct().Take(3).ToList();
            }
        }
        static readonly ConditionalWeakTable<Projectile_AbilityBase, Tag> projectiles = new ConditionalWeakTable<Projectile_AbilityBase, Tag>();
        [ThreadStatic] static Tag launching;
        static bool ProjectileMatches(ThingDef projectile, AbilityUser.AbilityDef ability) => Matches(ability, true)
            && projectile?.defName == "Projectile_Encase" + ability.defName.Substring("TM_Encase".Length);
        public static Tag BeginLaunch(Verb_UseAbility verb, ThingDef def)
        {
            var previous = launching; launching = null;
            if (ProjectileMatches(def, verb?.Ability?.Def) && Owned(verb.Ability, true) != null)
                launching = new Tag { casterId = verb.Ability.Pawn.thingIDNumber, abilityName = verb.Ability.Def.defName };
            return previous;
        }
        public static void EndLaunch(Tag previous) => launching = previous;
        public static void Launched(Projectile_AbilityBase projectile, Thing caster, AbilityUser.AbilityDef ability)
        {
            var tag = launching; launching = null; projectiles.Remove(projectile);
            if (tag != null && tag.casterId == caster?.thingIDNumber && tag.abilityName == ability?.defName && ProjectileMatches(projectile.def, ability)) projectiles.Add(projectile, tag);
        }
        internal static void StampHosted(Projectile_Encase field, MagicAbility power)
        {
            if (field.SpellOwner != power.Pawn || WardBody(power.Pawn) != field.Caster ||
                !ProjectileMatches(field.def, power.Def) || Owned(power,true) == null) return;
            projectiles.Remove(field);
            projectiles.Add(field,new Tag { casterId = power.Pawn.thingIDNumber, abilityName = power.Def.defName });
        }
        public static void ExposeProjectile(Projectile_AbilityBase projectile)
        {
            projectiles.TryGetValue(projectile, out var tag);
            if (Scribe.mode == LoadSaveMode.Saving && tag == null) return;
            Scribe_Deep.Look(ref tag, "TM_geomancerTrialSource");
            if (Scribe.mode != LoadSaveMode.Saving) { projectiles.Remove(projectile); if (tag?.casterId > 0) projectiles.Add(projectile, tag); }
        }
        public static void WallSpawned(Projectile_AbilityBase projectile, Thing wall, IntVec3 cell, Map map)
        {
            var owner = projectile is Projectile_Encase field ? field.SpellOwner : projectile.Caster;
            if (!projectiles.TryGetValue(projectile, out var tag) || tag.casterId != owner?.thingIDNumber || WardBody(owner) != projectile.Caster
                || tag.abilityName != projectile.localAbilityDef?.defName || !ProjectileMatches(projectile.def, projectile.localAbilityDef)
                || wall == null || !wall.Spawned || wall.Map != map || wall.Position != cell || map != projectile.Caster.Map
                || wall.def.category != ThingCategory.Building || (wall.def.defName != "Sandstone" && wall.def.defName != "Slate" && wall.def.defName != "Limestone" && wall.def.defName != "Granite")) return;
            var path = Owned(owner, projectile.localAbilityDef, true);
            if (path == null || tag.cells.Contains(cell) || tag.cells.Count >= 3) return;
            tag.cells.Add(cell);
            if (tag.cells.Count >= 3) path.RecordCompletedTrial(MasteryTrialId);
        }
    }
}
