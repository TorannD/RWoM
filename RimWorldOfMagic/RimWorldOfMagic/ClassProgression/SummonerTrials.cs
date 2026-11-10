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
    public sealed class SummonerTrialProgress : IExposable
    {
        public TMPawnSummoned Minion;
        public Thing Pylon;
        public int ControlledTicks;
        public int LastTick = -1;
        public bool LastActive;
        public void Observe(bool active, int now)
        {
            if (now <= LastTick) { if (!active) LastActive = false; return; }
            long delta = (long)now - LastTick;
            if (active && LastActive && LastTick >= 0 && delta <= 60)
                ControlledTicks = Math.Min(600, ControlledTicks + (int)delta);
            LastTick = now; LastActive = active;
        }
        public void ExposeData()
        {
            Scribe_References.Look(ref Minion, "minion");
            Scribe_References.Look(ref Pylon, "pylon");
            Scribe_Values.Look(ref ControlledTicks, "controlledTicks", 0);
            Scribe_Values.Look(ref LastTick, "lastTick", -1);
            Scribe_Values.Look(ref LastActive, "lastActive", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) { ControlledTicks = Math.Max(0, Math.Min(600, ControlledTicks)); LastActive = false; }
        }
    }
    public static class SummonerTrials
    {
        public const string FoundationTrialId = "rwom.summoner.foundation", MasteryTrialId = "rwom.summoner.mastery";
        public enum Spell { Minion, Pylon, Explosive }
        public sealed class Tag : IExposable
        {
            public int casterId, bodyId;
            public Spell spell;
            public Pawn caster, victim;
            public void ExposeData()
            {
                Scribe_Values.Look(ref casterId, "casterId", 0); Scribe_Values.Look(ref bodyId, "bodyId", 0); Scribe_Values.Look(ref spell, "spell");
                Scribe_References.Look(ref caster, "caster"); Scribe_References.Look(ref victim, "victim");
            }
        }
        static TMAbilityDef Ability(Spell spell) => spell == Spell.Minion ? TorannMagicDefOf.TM_SummonMinion : spell == Spell.Pylon ? TorannMagicDefOf.TM_SummonPylon : TorannMagicDefOf.TM_SummonExplosive;
        static string ProjectileName(Spell spell) => spell == Spell.Minion ? "Projectile_SummonMinion" : spell == Spell.Pylon ? "Projectile_SummonPylon" : "Projectile_SummonExplosive";
        static bool TrySpell(ThingDef projectile, out Spell spell)
        {
            switch (projectile?.defName)
            {
                case "Projectile_SummonMinion": spell = Spell.Minion; return true;
                case "Projectile_SummonPylon": spell = Spell.Pylon; return true;
                case "Projectile_SummonExplosive": spell = Spell.Explosive; return true;
                default: spell = default; return false;
            }
        }
        static bool Matches(AbilityUser.AbilityDef ability, Spell spell)
        {
            string root = Ability(spell)?.defName;
            return root != null && ability != null && (ability.defName == root || ability.defName == root + "_I" || ability.defName == root + "_II" || ability.defName == root + "_III");
        }
        static Pawn PhysicalBody(Pawn caster)
        {
            if (SpiritRebirthState.TryGet(caster, out var state))
                return state.HasValidHost(caster) && state.Host.Spawned ? state.Host : null;
            return caster?.Spawned == true ? caster : null;
        }
        static DisciplineProgress Owned(Pawn caster, Spell spell)
        {
            if (caster == null || caster.Dead || PhysicalBody(caster) == null || caster.Faction == null || caster.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true
                || !ClassIdentity.TryGetState(caster, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.summoner");
            var stage = spell == Spell.Explosive ? InitiationStage.Adept : InitiationStage.Foundation;
            if (path?.Domain != DisciplineDomain.Magic || path.Stage != stage || path.CompletedTrials.Contains(stage == InitiationStage.Foundation ? FoundationTrialId : MasteryTrialId)) return null;
            var magic = caster.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            return magic?.MagicData.MagicPowersS.Any(p => p.learned && p.TMabilityDefs.Contains(Ability(spell))) == true ? path : null;
        }
        static readonly ConditionalWeakTable<Projectile_AbilityBase, Tag> projectiles = new ConditionalWeakTable<Projectile_AbilityBase, Tag>();
        static readonly ConditionalWeakTable<Building_ExplosiveProximityTrap, Tag> mines = new ConditionalWeakTable<Building_ExplosiveProximityTrap, Tag>();
        [ThreadStatic] static Tag launching;
        public static Tag BeginLaunch(Verb_UseAbility verb, ThingDef def)
        {
            var previous = launching; launching = null;
            if (!TrySpell(def, out var spell)) return previous;
            if (!Matches(verb.Ability?.Def, spell) || Owned(verb.CasterPawn, spell) == null) return previous;
            var magic = verb.CasterPawn.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            if (verb.Ability.AbilityUser == magic && magic.AbilityData.Powers.Any(a => ReferenceEquals(a, verb.Ability)))
                launching = new Tag { casterId = verb.CasterPawn.thingIDNumber, spell = spell };
            return previous;
        }
        public static void EndLaunch(Tag previous) => launching = previous;
        public static void Launched(Projectile_AbilityBase projectile, Thing caster, AbilityUser.AbilityDef ability)
        {
            var tag = launching; launching = null; projectiles.Remove(projectile);
            var owner = projectile.TryGetComp<CompHostedSummon>()?.Owner;
            if (owner != null)
            {
                if (caster == null || PhysicalBody(owner) != caster) return;
                if (TrySpell(projectile.def, out var spell) && Matches(ability, spell) && Owned(owner, spell) != null)
                    projectiles.Add(projectile, new Tag { caster = owner, casterId = owner.thingIDNumber, bodyId = caster.thingIDNumber, spell = spell });
                return;
            }
            if (tag != null && tag.casterId == caster?.thingIDNumber && projectile.def?.defName == ProjectileName(tag.spell) && Matches(ability, tag.spell)) projectiles.Add(projectile, tag);
        }
        public static void ExposeProjectile(Projectile_AbilityBase projectile)
        {
            projectiles.TryGetValue(projectile, out var tag);
            if (Scribe.mode == LoadSaveMode.Saving && tag == null) return;
            Scribe_Deep.Look(ref tag, "TM_summonerTrialSource");
            if (Scribe.mode != LoadSaveMode.Saving) { projectiles.Remove(projectile); if (tag?.casterId > 0) projectiles.Add(projectile, tag); }
        }
        public static void Spawned(Projectile_AbilityBase projectile, Thing thing, Spell spell)
        {
            if (projectile == null || thing == null || !thing.Spawned) return;
            projectiles.TryGetValue(projectile, out var tag);
            var physical = projectile.Caster;
            var caster = tag?.bodyId > 0 ? tag.caster : physical;
            if (tag == null || tag.spell != spell || tag.casterId != caster?.thingIDNumber || projectile.def?.defName != ProjectileName(spell)
                || !Matches(projectile.localAbilityDef, spell) || physical == null || PhysicalBody(caster) != physical || (tag.bodyId != 0 && tag.bodyId != physical.thingIDNumber)
                || thing.Map != physical.Map || thing.Faction != caster.Faction) return;
            var path = Owned(caster, spell); if (path == null) return;
            if (tag.bodyId != 0 && spell != Spell.Minion && thing.TryGetComp<CompSummoned>()?.Spawner != caster) return;
            if (spell == Spell.Minion && thing is TMPawnSummoned minion && minion.Spawner == caster && minion.validSummoning && minion.Temporary && !minion.Dead)
            { if (path.SummonerTrials.Minion != minion) path.SummonerTrials.ControlledTicks = 0; path.SummonerTrials.Minion = minion; path.SummonerTrials.LastActive = false; }
            else if (spell == Spell.Pylon && IsVariant(thing.def?.defName, "DefensePylon") && thing.TryGetComp<CompSummoned>()?.Temporary == true)
            { if (path.SummonerTrials.Pylon != thing) path.SummonerTrials.ControlledTicks = 0; path.SummonerTrials.Pylon = thing; path.SummonerTrials.LastActive = false; }
            else if (spell == Spell.Explosive && thing is Building_ExplosiveProximityTrap mine && IsVariant(thing.def?.defName, "TM_ManaMine") && thing.TryGetComp<CompSummoned>()?.Temporary == true)
            { mines.Remove(mine); mines.Add(mine, new Tag { caster = caster, casterId = caster.thingIDNumber, bodyId = tag.bodyId, spell = spell }); }
        }
        static bool IsVariant(string name, string root) => name == root || name == root + "_I" || name == root + "_II" || name == root + "_III";
        public static void Observe(Pawn caster, int now)
        {
            var path = Owned(caster, Spell.Minion);
            if (path == null)
            {
                if (caster != null && ClassIdentity.TryGetState(caster, out var state))
                {
                    var pending = state.Find("rwom.summoner");
                    if (pending != null) pending.SummonerTrials.LastActive = false;
                }
                return;
            }
            var body = PhysicalBody(caster);
            var progress = path.SummonerTrials;
            var minion = progress.Minion; var pylon = progress.Pylon;
            bool active = Owned(caster, Spell.Pylon) != null && minion != null && !minion.Dead && !minion.Downed && minion.Spawned && minion.Map == body.Map
                && minion.Spawner == caster && minion.Faction == caster.Faction && minion.validSummoning && minion.Temporary
                && pylon != null && pylon.Spawned && pylon.Map == body.Map && pylon.Faction == caster.Faction && pylon.TryGetComp<CompSummoned>()?.Temporary == true
                && (body == caster || pylon.TryGetComp<CompSummoned>().Spawner == caster);
            progress.Observe(active, now);
            if (progress.ControlledTicks >= 600) path.RecordCompletedTrial(FoundationTrialId);
        }
        static bool Hostile(Tag tag, Building_ExplosiveProximityTrap mine, Pawn victim)
        {
            var caster = tag.caster;
            var body = PhysicalBody(caster);
            return body != null && (tag.bodyId == 0 ? body == caster : body.thingIDNumber == tag.bodyId && mine.TryGetComp<CompSummoned>()?.Spawner == caster)
                && victim != null && !victim.Dead && victim.Spawned && victim.Map == body.Map && mine.Map == body.Map && !victim.IsPrisoner && !victim.IsSlave
                && victim.HostileTo(body) && (victim.Faction == null ? victim.InAggroMentalState : victim.Faction != caster.Faction && victim.Faction.HostileTo(caster.Faction));
        }
        public static void Triggered(Building_ExplosiveProximityTrap mine, Pawn victim)
        {
            if (mines.TryGetValue(mine, out var tag) && mine.Spawned && mine.Faction == tag.caster?.Faction && Hostile(tag, mine, victim) && Owned(tag.caster, Spell.Explosive) != null)
                tag.victim = victim;
        }
        public static void WickStarted(Building_ExplosiveProximityTrap mine, Pawn victim)
        {
            if (!mines.TryGetValue(mine, out var tag) || tag.victim != victim || victim == null || mine.TryGetComp<CompExplosive>()?.wickStarted != true || mine.Armed || !mine.Spawned || mine.Faction != tag.caster?.Faction || !Hostile(tag, mine, victim)) return;
            Owned(tag.caster, Spell.Explosive)?.RecordCompletedTrial(MasteryTrialId);
            tag.victim = null;
        }
        public static void ExposeMine(Building_ExplosiveProximityTrap mine)
        {
            mines.TryGetValue(mine, out var tag);
            if (Scribe.mode == LoadSaveMode.Saving && tag == null) return;
            Scribe_Deep.Look(ref tag, "TM_summonerTrialOwner");
            if (Scribe.mode != LoadSaveMode.Saving) { mines.Remove(mine); if (tag?.casterId > 0) mines.Add(mine, tag); }
        }
    }
}
