using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AbilityUser;
using TorannMagic.TMDefs;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal sealed class SpiritRuntimeClock : IExposable
    {
        private int lastTick = -1;
        public SpiritRuntimeClock() { }
        internal bool Observe(int now)
        {
            if (now < 0 || now <= lastTick || Find.TickManager.Paused) return false;
            lastTick = now;
            return true;
        }
        private Dictionary<string, int> remaining;
        private Dictionary<string, int> pending;
        private List<string> retainedMartial;
        private List<string> pendingMartial;

        private static TMAbilityDef LearnedMartial(MightData data, string name)
        {
            bool sprint = name == "TM_Sprint" || name == "TM_Sprint_I" || name == "TM_Sprint_II" || name == "TM_Sprint_III";
            TMAbilityDef def = name == "TM_FirstAid" ? TorannMagicDefOf.TM_FirstAid :
                name == "TM_PommelStrike" ? TorannMagicDefOf.TM_PommelStrike :
                name == "TM_GearRepair" ? TorannMagicDefOf.TM_GearRepair :
                name == "TM_InnerHealing" ? TorannMagicDefOf.TM_InnerHealing :
                name == "TM_StrongBack" ? TorannMagicDefOf.TM_StrongBack :
                name == "TM_HeavyBlow" ? TorannMagicDefOf.TM_HeavyBlow :
                name == "TM_ThickSkin" ? TorannMagicDefOf.TM_ThickSkin :
                name == "TM_FightersFocus" ? TorannMagicDefOf.TM_FightersFocus :
                sprint ? DefDatabase<TMAbilityDef>.GetNamedSilentFail(name) : null;
            if (def == null) return null;
            var rows = name == "TM_FirstAid" ? data?.mightPowerSS : sprint ? data?.mightPowerG : data?.mightPowerStandalone;
            return rows?.Count(p => p?.learned == true && p.TMabilityDefs?.Count > 0 &&
                p.TMabilityDefs[Math.Max(0,Math.Min(p.level,p.TMabilityDefs.Count - 1))] == def) == 1 ? def : null;
        }

        internal static bool TracksCooldowns(Pawn pawn)
        {
            // Saves retain progression data even while the beta gameplay facade is disabled.
            var progression = pawn?.AllComps?.OfType<CompClassProgression>().FirstOrDefault();
            return progression?.SpiritRebirth != null || progression?.State?.Version == ClassProgressionState.CurrentVersion &&
                progression.State.Vocation != Vocation.None;
        }

        internal void CaptureCooldowns(CompAbilityUser owner)
        {
            remaining = pending == null ? null : new Dictionary<string, int>(pending);
            retainedMartial = pendingMartial;
            if (owner is CompAbilityUserMight might && SpiritRebirthEnergy.Marked(might.Pawn))
            {
                var owned = owner.AbilityData.Powers.OfType<MightAbility>()
                    .Where(power => ReferenceEquals(power.AbilityUser, owner) && power.Def != null &&
                        LearnedMartial(might.ExistingMightData, power.Def.defName) == power.Def)
                    .Select(power => power.Def.defName).Distinct().ToList();
                retainedMartial = owned.Count > 0 ? owned : null;
            }
            foreach (var power in owner.AbilityData.Powers)
            {
                if (power?.Def == null || !ReferenceEquals(power.AbilityUser, owner) || power.CooldownTicksLeft <= 0) continue;
                remaining ??= new Dictionary<string, int>();
                remaining.TryGetValue(power.Def.defName, out int previous);
                remaining[power.Def.defName] = Math.Max(previous, power.CooldownTicksLeft);
            }
        }

        internal void RestoreCooldowns(CompAbilityUser owner)
        {
            // Restore only abilities actually present at save time and still backed by an exact learned native row.
            if (pendingMartial != null && owner is CompAbilityUserMight might && might.ExistingMightData != null && SpiritRebirthEnergy.Marked(might.Pawn))
            {
                foreach (var name in pendingMartial)
                {
                    var def = LearnedMartial(might.ExistingMightData, name);
                    if (def != null && !owner.AbilityData.Powers.Any(p => p?.Def == def))
                        TM_ClassState.AddRuntimeAbility(owner, def);
                }
                pendingMartial = null;
            }
            if (pending == null) return;
            foreach (var power in owner.AbilityData.Powers)
            {
                if (power?.Def == null || !ReferenceEquals(power.AbilityUser, owner) ||
                    !pending.TryGetValue(power.Def.defName, out int ticks)) continue;
                if (ticks > power.CooldownTicksLeft) power.CooldownTicksLeft = ticks;
                pending.Remove(power.Def.defName);
            }
            if (pending.Count == 0) pending = null;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref lastTick, "lastTick", -1);
            Scribe_Collections.Look(ref remaining, "ownedCooldowns", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref retainedMartial, "retainedMartial", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                pending = remaining;
                remaining = null;
                pendingMartial = retainedMartial;
                retainedMartial = null;
            }
        }
    }

    internal static class SpiritRebirthRuntime
    {
        // Cache type metadata only. Saved owners and lists are always read fresh.
        private static readonly FieldInfo[] skillFields = typeof(MagicData).GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(List<MagicPowerSkill>)).ToArray();
        private static readonly FieldInfo[] powerFields = typeof(MagicData).GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(List<MagicPower>)).ToArray();
        internal static int SavedRank(MagicData data, string label)
        {
            if (data == null) return 0;
            foreach (var field in skillFields)
                if (field.GetValue(data) is List<MagicPowerSkill> skills)
                    foreach (var skill in skills)
                        if (skill != null && TM_CustomSkillLabels.Canonical(skill.label) == label) return skill.level;
            return 0;
        }
        internal static int SavedRank(MagicData data, TMAbilityDef ability, string suffix)
        {
            if (ability == null) return 0;
            string label = ability.defName.TrimEnd('_', 'I', 'V', 'X') + suffix;
            foreach (var field in skillFields)
                if (field.GetValue(data) is List<MagicPowerSkill> skills)
                    foreach (var skill in skills)
                        if (skill != null && TM_CustomSkillLabels.Canonical(skill.label) == label) return skill.level;
            foreach (var definition in DefDatabase<TM_CustomPowerDef>.AllDefsListForReading)
            {
                var power = definition.customPower;
                if (power?.abilityDefs?.Contains(ability) != true || power.skills == null) continue;
                foreach (var skill in power.skills)
                    if (skill?.label?.EndsWith(suffix) == true) return SavedRank(data, TM_CustomSkillLabels.Canonical(skill.label));
            }
            return 0;
        }
        internal static TMAbilityDef SavedHediffAbility(MagicData data, Hediff hediff)
        {
            foreach (var field in powerFields)
                if (field.GetValue(data) is List<MagicPower> powers)
                    foreach (var power in powers)
                    {
                        var defs = power?.TMabilityDefs;
                        if (defs == null || defs.Count == 0) continue;
                        var ability = defs[Math.Max(0, Math.Min(power.level, defs.Count - 1))] as TMAbilityDef;
                        if (ability?.abilityHediff == hediff?.def) return ability;
                    }
            return null;
        }
        internal static bool TickMagic(CompAbilityUserMagic comp, SpiritRuntimeClock clock)
        {
            var pawn = comp.Pawn;
            if (!SpiritRebirthEnergy.Marked(pawn)) return false;
            if (comp.sigilSurging || comp.sigilDraining)
            {
                var body = GolemancerLifecycle.MasterBody(pawn);
                if (body == null || body.Downed)
                {
                    comp.sigilSurging = false;
                    comp.sigilDraining = false;
                }
                else if (comp.sigilSurging && comp.BrandPawns.Count > 0 && comp.Mana?.CurLevel <= .01f)
                    comp.sigilSurging = false;
            }
            int now = Find.TickManager.TicksGame;
            if (!SpiritRebirthBody.RetainsNeeds(pawn) || !clock.Observe(now)) return true;
            if (!pawn.Spawned && now % 30 == 0 && ClassIdentity.TryGetState(pawn, out var progression) &&
                progression.Find("rwom.summoner")?.Stage == InitiationStage.Foundation)
                SummonerTrials.Observe(pawn, now);
            if (comp.overdriveBuilding != null && now % Math.Max(1, comp.overdriveFrequency) == 0)
                comp.ResolveTechnomancerOverdrive();
            if (comp.earthSpriteType != 0 && SpiritRebirthState.TryGet(pawn, out var spriteState) && !spriteState.DepletionPending)
                comp.TickEarthSprites(spriteState.HasValidHost(pawn) ? spriteState.Host : pawn);
            SpiritHostedTechnoBit.Tick(comp);
            if (now % 60 == 0)
            {
                if (comp.HasHexRecords || comp.AbilityData.Powers.Any(p => ShamanLifecycle.Control(p?.Def)))
                    ShamanLifecycle.Restore(comp);
                BrightmageLifecycle.TickHostedLight(pawn,now);
                comp.SyncStoneskinControl();
                comp.SyncEarthSpriteControl();
                comp.SyncSentinelControl();
                comp.SyncGuardianControl();
                comp.SyncEnchanterStoneControl();
                comp.SyncEnchantWeaponControl();
                if (comp.recallSet && comp.recallExpiration <= now)
                {
                    comp.recallSet = comp.recallSpell = false;
                    comp.recallBodyId = 0;
                    comp.RemovePawnAbility(TorannMagicDefOf.TM_Recall);
                }
                if (comp.predictionIncidentDef != null && now > comp.predictionTick + 30) ChronomancerPrediction.Expire(comp,Find.Storyteller?.incidentQueue);
            }
            if (now % 100 == 0) BardTrials.TickHostedInspiration(pawn);
            if (now % 30 == 0) BardTrials.TickHostedHymn(pawn);
            SpiritHostedAura.TickAuras(pawn);
            SpiritRebirthHealthForm.TryRefreshPending(pawn);
            TickAbilities(comp, true);
            if (!SpiritRebirthBody.RetainsNeeds(pawn)) return true;
            for (int i = comp.chainedAbilitiesList.Count - 1; i >= 0; i--)
            {
                var chain = comp.chainedAbilitiesList[i];
                if (chain?.abilityDef == null) { comp.chainedAbilitiesList.RemoveAt(i); continue; }
                chain.expirationTicks--;
                if (!chain.expires || chain.expirationTicks > 0) continue;
                comp.RemovePawnAbility(chain.abilityDef);
                comp.chainedAbilitiesList.RemoveAt(i);
            }
            if (now % 60 == 0 && comp.ExistingMagicData != null) comp.ResolveOwnedMagicEconomy();
            // The native need tracker can also observe this tick. Its saved
            // interval receipt makes the two legitimate paths charge only once.
            if (pawn.IsHashIntervalTick(150)) comp.Mana?.NeedInterval();
            return true;
        }

        internal static bool TickMight(CompAbilityUserMight comp, SpiritRuntimeClock clock)
        {
            if (!SpiritRebirthEnergy.Marked(comp.Pawn)) return false;
            if (!SpiritRebirthBody.RetainsNeeds(comp.Pawn) || !clock.Observe(Find.TickManager.TicksGame)) return true;
            HediffComp_Psionic.TickHosted(comp.Pawn);
            HediffComp_Hate.TickHosted(comp.Pawn);
            HediffComp_Chi.TickHosted(comp.Pawn);
            TickAbilities(comp, false);
            if (comp.Pawn.IsHashIntervalTick(150)) comp.Stamina?.NeedInterval();
            for (int i = comp.chainedAbilitiesList.Count - 1; i >= 0; i--)
            {
                var chain = comp.chainedAbilitiesList[i];
                if (chain?.abilityDef == null) { comp.chainedAbilitiesList.RemoveAt(i); continue; }
                chain.expirationTicks--;
                if (!chain.expires || chain.expirationTicks > 0) continue;
                comp.RemovePawnAbility(chain.abilityDef);
                comp.chainedAbilitiesList.RemoveAt(i);
            }
            return true;
        }

        private static void TickAbilities(CompAbilityUser comp, bool magic)
        {
            bool active = magic && SpiritRebirthFormPolicy.IsSpawnedEthereal(comp.Pawn) && SpiritRebirthEnergy.CanCast(comp.Pawn);
            var family = active ? SpiritRebirthCasting.NativeFamily() : null;
            // One provenance walk per dispatch, not one graph traversal per power.
            var seen = new HashSet<PawnAbility>();
            foreach (var ability in comp.AbilityData.AllPowers.ToArray())
            {
                if (ability == null || !seen.Add(ability) || !ReferenceEquals(ability.AbilityUser, comp)) continue;
                if (active && family.Contains(ability.Def) && SpiritRebirthFormPolicy.IsSpawnedEthereal(comp.Pawn)
                    && SpiritRebirthEnergy.CanCast(comp.Pawn)) ability.Tick();
                else if (ability.CooldownTicksLeft > -1) ability.CooldownTicksLeft--;
            }
        }

        internal static void TickHeld(Thing_SoulStone stone)
        {
            var pawn = stone?.ContainedSpirit;
            if (!SpiritRebirthState.TryGet(pawn, out var state) || !state.HasValidStone(pawn)
                || !ReferenceEquals(state.Stone, stone)) return;
            SpiritRebirthHealthForm.TryRefreshPending(pawn);
            pawn.GetCompAbilityUserMagic()?.CompTick();
            pawn.GetCompAbilityUserMight()?.CompTick();
        }
    }
}
