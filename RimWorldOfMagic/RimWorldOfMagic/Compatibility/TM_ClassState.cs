using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic.TMDefs;
using TorannMagic.Compatibility;
using Verse;
using AbilityDef = AbilityUser.AbilityDef;

namespace TorannMagic
{
    // Trait edits are not pawn generation or a respec. Keep the saved owners and
    // investments, and reconcile only the runtime grants belonging to changed classes.
    public static class TM_ClassState
    {
        private sealed class ClassEntry
        {
            public string Key;
            public bool Advanced;
            public List<AbilityDef> Abilities;
        }

        private sealed class Snapshot
        {
            public List<ClassEntry> Magic = new List<ClassEntry>();
            public List<ClassEntry> Might = new List<ClassEntry>();
            public bool MagicReady;
            public bool MightReady;
        }

        private sealed class State
        {
            public Snapshot Before;
            public int Depth;
            public bool Reconciling;
        }

        private static readonly ConditionalWeakTable<Pawn, State> States = new ConditionalWeakTable<Pawn, State>();
        private static readonly FieldInfo[] MagicFlags = UnlockFlags(typeof(CompAbilityUserMagic), "spell_");
        private static readonly FieldInfo[] MightFlags = UnlockFlags(typeof(CompAbilityUserMight), "skill_");

        private static FieldInfo[] UnlockFlags(Type type, string prefix) => type.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(bool) && f.Name.StartsWith(prefix, StringComparison.Ordinal)).ToArray();

        private static bool HasTrait(Pawn pawn, string name) => pawn?.story?.traits?.allTraits.Any(t => t.def?.defName == name) == true;
        private static bool HasClass(Pawn pawn, TM_CustomClass cls) => cls?.classTrait != null &&
            pawn?.story?.traits?.allTraits.Any(t => t.def?.defName == cls.classTrait.defName && t.Degree == cls.traitDegree) == true;

        public static List<TM_CustomClass> CurrentCustomClasses(Pawn pawn, bool magic) =>
            (TM_ClassUtility.CustomClasses ?? new List<TM_CustomClass>())
                .Where(c => c != null && (magic ? c.isMage : c.isFighter) && HasClass(pawn, c)).ToList();

        public static TM_CustomClass CurrentBaseClass(Pawn pawn, bool magic) =>
            CurrentCustomClasses(pawn, magic).FirstOrDefault(c => !c.isAdvancedClass);

        // Capture must run before GainTrait/RemoveTrait mutates the TraitSet. Nested
        // trait edits collapse into one transition; Postfix calls this after ResolveComps.
        public static void CaptureBeforeTraitsChanged(Pawn pawn)
        {
            if (pawn?.story?.traits == null || Scribe.mode != LoadSaveMode.Inactive) return;
            State state = States.GetValue(pawn, key => new State());
            if (state.Reconciling) return;
            if (state.Depth++ == 0) state.Before = Capture(pawn);
        }

        public static void OnTraitsChanged(Pawn pawn)
        {
            if (pawn?.story?.traits == null || Scribe.mode != LoadSaveMode.Inactive) return;
            State state = States.GetValue(pawn, key => new State());
            if (state.Reconciling) return;
            if (state.Depth > 0 && --state.Depth > 0) return;
            Snapshot before = state.Before;
            if (before == null) return; // Never infer a destructive transition on load.
            state.Before = null;
            state.Reconciling = true;
            try
            {
                var magic = pawn.GetCompAbilityUserMagic();
                var might = pawn.GetCompAbilityUserMight();
                magic?.SetIsMagicUser();
                might?.SetIsMightUser();
                Snapshot after = Capture(pawn);
                if (before.MagicReady && magic != null && Keys(before.Magic) != Keys(after.Magic))
                    ReconcileMagic(magic, before.Magic, after.Magic);
                if (before.MightReady && might != null && Keys(before.Might) != Keys(after.Might))
                    ReconcileMight(might, before.Might, after.Might);
            }
            finally { state.Reconciling = false; }
        }

        private static string Keys(List<ClassEntry> entries) => string.Join("|", entries.Select(e => e.Key).OrderBy(k => k, StringComparer.Ordinal));

        private static Snapshot Capture(Pawn pawn)
        {
            var magic = pawn.GetCompAbilityUserMagic();
            var might = pawn.GetCompAbilityUserMight();
            var result = new Snapshot
            {
                MagicReady = magic != null && (magic.Initialized || magic.magicPowersInitialized),
                MightReady = might != null && (might.Initialized || might.mightPowersInitialized)
            };
            if (result.MagicReady && magic.MagicData != null) result.Magic = MagicClasses(pawn, magic.MagicData);
            if (result.MightReady && might.MightData != null) result.Might = MightClasses(pawn, might.MightData);
            return result;
        }

        private static List<ClassEntry> MagicClasses(Pawn pawn, MagicData data)
        {
            var result = CustomEntries(pawn, true);
            foreach (var entry in TM_ClassGroups.MagicEntriesField)
                if (HasTrait(pawn, entry.trait) && !result.Any(c => c.Key.StartsWith(entry.trait + ":", StringComparison.Ordinal)))
                    result.Add(new ClassEntry { Key = entry.trait, Abilities = (TM_ClassGroups.ResolveMagicList(data, entry.field) ?? new List<MagicPower>()).SelectMany(p => p.TMabilityDefs).ToList() });
            return result;
        }

        private static List<ClassEntry> MightClasses(Pawn pawn, MightData data)
        {
            var result = CustomEntries(pawn, false);
            foreach (var entry in TM_ClassGroups.MightEntriesField)
                if (HasTrait(pawn, entry.trait) && !result.Any(c => c.Key.StartsWith(entry.trait + ":", StringComparison.Ordinal)))
                    result.Add(new ClassEntry { Key = entry.trait, Abilities = (TM_ClassGroups.ResolveMightList(data, entry.list) ?? new List<MightPower>()).SelectMany(p => p.TMabilityDefs).ToList() });
            return result;
        }

        private static List<ClassEntry> CustomEntries(Pawn pawn, bool magic) => CurrentCustomClasses(pawn, magic)
            .Select(c => new ClassEntry { Key = c.classTrait.defName + ":" + c.traitDegree, Advanced = c.isAdvancedClass,
                Abilities = (magic ? c.classMageAbilities : c.classFighterAbilities).Cast<AbilityDef>().ToList() }).ToList();

        private static bool Matches(IEnumerable<AbilityDef> family, IEnumerable<AbilityDef> abilities) => family != null && abilities != null && family.Intersect(abilities).Any();

        public static MagicPower PreferredMagicOwner(CompAbilityUserMagic comp, AbilityDef ability)
        {
            if (comp?.MagicData == null || ability == null) return null;
            var data = comp.MagicData;
            var family = data.AllMagicPowers.FirstOrDefault(p => p.TMabilityDefs.Contains(ability));
            if (family == null) return null;
            bool customClassUses = CurrentCustomClasses(comp.Pawn, true).Any(c => Matches(family.TMabilityDefs, c.classMageAbilities));
            if (customClassUses)
            {
                var custom = data.MagicPowersCustomAll.FirstOrDefault(p => Matches(p.TMabilityDefs, family.TMabilityDefs));
                if (custom != null) return custom;
                if (CurrentBaseClass(comp.Pawn, true)?.classMageAbilities.Any(a => family.TMabilityDefs.Contains(a)) == true)
                    return family; // Custom base classes historically use the aggregate owner (SD).
            }
            if (IsSharedFamily(family) && ClassProgression.ClassIdentity.TryGetDemonBranch(comp.Pawn, out bool selectedWarlock))
                return (selectedWarlock ? data.MagicPowersWD : data.MagicPowersSD).FirstOrDefault(p => Matches(p.TMabilityDefs, family.TMabilityDefs));
            if (IsSharedFamily(family))
            {
                if (HasTrait(comp.Pawn, "Warlock")) return data.MagicPowersWD.FirstOrDefault(p => Matches(p.TMabilityDefs, family.TMabilityDefs)) ?? family;
                if (HasTrait(comp.Pawn, "Succubus")) return data.MagicPowersSD.FirstOrDefault(p => Matches(p.TMabilityDefs, family.TMabilityDefs)) ?? family;
            }
            return family;
        }

        private static bool IsSharedFamily(MagicPower power) => power?.TMabilityDefs?.Any(a =>
            a != null && (a.defName == "TM_SoulBond" || a.defName == "TM_ShadowBolt" || a.defName == "TM_Dominate")) == true;

        public sealed class SharedMagicSnapshot
        {
            internal readonly List<MagicPower> Owners = new List<MagicPower>();
            internal readonly List<int> Ranks = new List<int>();
            internal readonly List<bool> Learned = new List<bool>();
            public void Apply()
            {
                for (int i = 0; i < Owners.Count; i++)
                {
                    Owners[i].level = Ranks[i];
                    Owners[i].learned = Learned[i];
                }
            }
        }

        public static SharedMagicSnapshot PrepareDemonOwners(CompAbilityUserMagic comp, ClassProgression.ClassDisciplineDef profile)
        {
            if (comp?.MagicData == null || profile?.domain != ClassProgression.DisciplineDomain.Magic
                || (profile.profileId != "rwom.warlock" && profile.profileId != "rwom.succubus")
                || profile.magicPowerList != (profile.profileId == "rwom.warlock" ? "magicPowerWD" : "magicPowerSD")) return null;
            return PrepareDemonOwners(comp, profile.profileId == "rwom.warlock");
        }

        // Save recovery knows the explicit branch ID even if its XML definition
        // was removed. This cannot authorize a purchase or grant unlearned power.
        private static SharedMagicSnapshot PrepareDemonOwners(CompAbilityUserMagic comp, bool warlock)
        {
            if (comp?.MagicData == null) return null;
            var data = comp.MagicData;
            var destination = warlock ? data.MagicPowersWD : data.MagicPowersSD;
            var snapshot = new SharedMagicSnapshot();
            foreach (string name in new[] { "TM_SoulBond", "TM_ShadowBolt", "TM_Dominate" })
            {
                var owners = destination.Where(p => p?.TMabilityDefs?.Any(a => a?.defName == name) == true).ToList();
                if (owners.Count != 1 || owners[0].TMabilityDefs.Count == 0) return null;
                var owner = owners[0];
                var wd = data.MagicPowersWD.Where(p => p?.TMabilityDefs?.Any(a => a?.defName == name) == true).ToList();
                var sd = data.MagicPowersSD.Where(p => p?.TMabilityDefs?.Any(a => a?.defName == name) == true).ToList();
                if (wd.Count != 1 || sd.Count != 1 || ReferenceEquals(wd[0], sd[0])
                    || wd[0].TMabilityDefs.Count != sd[0].TMabilityDefs.Count
                    || !wd[0].TMabilityDefs.SequenceEqual(sd[0].TMabilityDefs)) return null;
                var sources = wd.Concat(sd).ToList();
                snapshot.Owners.Add(owner);
                snapshot.Ranks.Add(Math.Min(Math.Max(0, sources.Max(p => p.level)), owner.TMabilityDefs.Count - 1));
                snapshot.Learned.Add(sources.Any(p => p.learned));
            }
            return snapshot;
        }

        // Only the historically duplicated WD/SD trio is repaired. Copy a bounded
        // maximum rank and learned union, never sum/refund/delete saved owners.
        public static void RestoreSharedMagicProgression(CompAbilityUserMagic comp)
        {
            if (comp?.MagicData == null) return;
            if (ClassProgression.ClassIdentity.TryGetDemonBranch(comp.Pawn, out bool warlock))
            {
                PrepareDemonOwners(comp, warlock)?.Apply();
                return;
            }
            var data = comp.MagicData;
            var authorized = MagicClasses(comp.Pawn, data).SelectMany(c => c.Abilities).ToList();
            foreach (var source in data.MagicPowersWD.Where(IsSharedFamily))
            {
                if (!Matches(source.TMabilityDefs, authorized)) continue;
                var owner = PreferredMagicOwner(comp, source.TMabilityDefs[0]);
                if (owner == null) continue;
                var saved = data.AllMagicPowers.Where(p => Matches(p.TMabilityDefs, source.TMabilityDefs)).ToList();
                int rank = saved.Max(p => p.level);
                int previousRank = owner.level;
                bool previousLearned = owner.learned;
                owner.level = Math.Max(owner.level, Math.Min(Math.Max(0, rank), owner.TMabilityDefs.Count - 1));
                owner.learned |= saved.Any(p => p.learned);
                if (owner.level != previousRank || owner.learned != previousLearned)
                    TM_Log.Trace("Progression", "Recovered shared magic owner " + owner.TMabilityDefs[0].defName + " for " + comp.Pawn?.ThingID);
            }
        }

        private static HashSet<AbilityDef> Expand(IEnumerable<AbilityDef> abilities, IEnumerable<List<AbilityDef>> families)
        {
            var result = new HashSet<AbilityDef>(abilities.Where(a => a != null));
            foreach (var family in families)
                if (Matches(family, result)) result.UnionWith(family.Where(a => a != null));
            var queue = new Queue<AbilityDef>(result);
            while (queue.Count > 0)
            {
                var ability = queue.Dequeue() as TMAbilityDef;
                if (ability?.childAbilities == null) continue;
                foreach (var child in ability.childAbilities)
                    if (child != null && result.Add(child)) queue.Enqueue(child);
            }
            // These legacy children predate childAbilities declarations.
            AddLegacyChildren(result, "TM_LightSkip", "TM_LightSkipMass", "TM_LightSkipGlobal");
            AddLegacyChildren(result, "TM_Hex", "TM_Hex_CriticalFail", "TM_Hex_MentalAssault", "TM_Hex_Pain");
            AddLegacyChildren(result, "TM_SoulBond", "TM_ShadowStep", "TM_ShadowCall");
            AddLegacyChildren(result, "TM_Recall", "TM_TimeMark");
            AddLegacyChildren(result, "TM_TechnoWeapon", "TM_NanoStimulant");
            AddLegacyChildren(result, "TM_EnchantedBody", "TM_EnchantedAura");
            AddLegacyChildren(result, "TM_Shapeshift", "TM_ShapeshiftDW");
            return result;
        }

        private static void AddLegacyChildren(HashSet<AbilityDef> abilities, string parent, params string[] children)
        {
            if (!abilities.Any(a => a.defName == parent)) return;
            foreach (string name in children)
            {
                var child = DefDatabase<TMAbilityDef>.GetNamedSilentFail(name);
                if (child != null) abilities.Add(child);
            }
        }

        private static void RemoveRuntime(CompAbilityUser comp, HashSet<AbilityDef> remove)
        {
            comp.AbilityData.Powers.RemoveAll(a => a != null && remove.Contains(a.Def));
            comp.AbilityData.AllPowers = null;
            comp.UpdateAbilities();
        }

        public static void AddRuntimeAbility(CompAbilityUser comp, AbilityDef ability)
        {
            if (ability == null) return;
            var powers = comp.AbilityData.Powers;
            bool kept = false;
            for (int i = 0; i < powers.Count; i++)
            {
                if (powers[i] == null || powers[i].Def != ability) continue;
                if (kept) powers.RemoveAt(i--);
                else kept = true;
            }
            if (!kept) comp.AddPawnAbility(ability);
            comp.AbilityData.AllPowers = null;
        }

        private static void Grant(CompAbilityUser comp, AbilityDef current, IEnumerable<AbilityDef> family)
        {
            if ((current as TMAbilityDef)?.shouldInitialize == true) AddRuntimeAbility(comp, current);
            foreach (var ability in family.OfType<TMAbilityDef>())
                if (ability.childAbilities != null)
                    foreach (var child in ability.childAbilities)
                        if (child.shouldInitialize) AddRuntimeAbility(comp, child);
        }

        private static void ClearRemovedFlags(object comp, FieldInfo[] flags, HashSet<AbilityDef> removed)
        {
            foreach (var field in flags)
            {
                string name = "TM_" + field.Name.Substring(field.Name.IndexOf('_') + 1);
                if (removed.Any(a => a.defName == name || (field.Name == "spell_Rain" && a.defName == "TM_Rainmaker"))) field.SetValue(comp, false);
            }
        }

        private static void ReconcileMagic(CompAbilityUserMagic comp, List<ClassEntry> before, List<ClassEntry> after)
        {
            var data = comp.MagicData;
            if (data == null) return;
            RestoreSharedMagicProgression(comp);
            var families = data.AllMagicPowers.Select(p => p.TMabilityDefs).ToList();
            var keep = Expand(after.SelectMany(c => c.Abilities), families);
            keep.UnionWith(Expand(SharedMagicPowers(comp).Where(p => p.learned).SelectMany(p => p.TMabilityDefs), families));
            var removed = Expand(before.SelectMany(c => c.Abilities), families);
            removed.ExceptWith(keep);
            RemoveRuntime(comp, removed);
            ClearRemovedFlags(comp, MagicFlags, removed);
            if (removed.Any(a => a.defName == "TM_Recall" || a.defName == "TM_TimeMark"))
            {
                comp.recallSpell = false;
                comp.recallSet = false;
            }
            foreach (var entry in after)
            {
                bool starts = !entry.Advanced && !before.Any(c => c.Key == entry.Key);
                foreach (var ability in entry.Abilities)
                {
                    var owner = PreferredMagicOwner(comp, ability);
                    if (owner == null) continue;
                    if (starts && !owner.requiresScroll) owner.learned = true;
                    if (owner.learned) Grant(comp, owner.abilityDef, owner.TMabilityDefs);
                }
            }
        }

        private static void ReconcileMight(CompAbilityUserMight comp, List<ClassEntry> before, List<ClassEntry> after)
        {
            var data = comp.MightData;
            if (data == null) return;
            var families = data.AllMightPowers.Select(p => p.TMabilityDefs).ToList();
            var keep = Expand(after.SelectMany(c => c.Abilities), families);
            keep.UnionWith(Expand(data.MightPowersStandalone.Concat(data.MightPowersCustomStandalone).Where(p => p.learned).SelectMany(p => p.TMabilityDefs), families));
            var removed = Expand(before.SelectMany(c => c.Abilities), families);
            removed.ExceptWith(keep);
            RemoveRuntime(comp, removed);
            ClearRemovedFlags(comp, MightFlags, removed);
            foreach (var entry in after)
            {
                bool starts = !entry.Advanced && !before.Any(c => c.Key == entry.Key);
                foreach (var ability in entry.Abilities)
                {
                    var owner = data.AllMightPowers.FirstOrDefault(p => p.TMabilityDefs.Contains(ability));
                    if (owner == null) continue;
                    bool nativeSuperSoldierLocked = entry.Key == "TM_SuperSoldier" &&
                        owner.TMabilityDefs.All(a => a.defName != "TM_CQC" && a.defName != "TM_FirstAid" && a.defName != "TM_60mmMortar");
                    if (starts && !nativeSuperSoldierLocked && (ability as TMAbilityDef)?.learnItem == null) owner.learned = true;
                    if (owner.learned) Grant(comp, owner.abilityDef, owner.TMabilityDefs);
                }
            }
        }

        public static bool HasCurrentClass(Pawn pawn, bool magic)
        {
            if (ClassProgression.ClassIdentity.TryGetRole(pawn, magic ? ClassProgression.Vocation.Mage : ClassProgression.Vocation.Warrior, out bool enabled)) return enabled;
            return CurrentCustomClasses(pawn, magic).Count > 0 ||
                (magic ? TM_ClassGroups.MagicEntriesField.Any(e => HasTrait(pawn, e.trait))
                       : TM_ClassGroups.MightEntriesField.Any(e => HasTrait(pawn, e.trait)));
        }

        public static void RestoreCurrentMagicRuntime(CompAbilityUserMagic comp, bool newData = false)
        {
            if (ClassProgression.ClassSpellAccess.RestoreLearned(comp)) return;
            var classes = MagicClasses(comp.Pawn, comp.MagicData);
            RestoreEmptyClassicMagicStarter(comp, classes);
            if (newData)
                foreach (var saved in comp.AbilityData.Powers.ToList())
                {
                    if (saved?.Def == null) continue;
                    var owner = PreferredMagicOwner(comp, saved.Def);
                    if (HasTrait(comp.Pawn, "ChaosMage"))
                        owner = comp.MagicData.AllMagicPowersForChaosMage.FirstOrDefault(p => p.TMabilityDefs.Contains(saved.Def)) ?? owner;
                    if (owner == null || (!IsMagicPowerAuthorized(comp, owner) && !HasTrait(comp.Pawn, "ChaosMage"))) continue;
                    owner.learned = true;
                    owner.level = Math.Max(owner.level, owner.TMabilityDefs.IndexOf(saved.Def));
                }
            ReconcileMagic(comp, newData ? new List<ClassEntry>() : classes, classes);
            foreach (var power in SharedMagicPowers(comp)
                .Concat(TM_ChaosProgression.ActivePowers(comp)).Where(p => p.learned))
                Grant(comp, power.abilityDef, power.TMabilityDefs);
            comp.RestoreSavedCooldowns();
        }

        public static void RestoreCurrentMightRuntime(CompAbilityUserMight comp, bool newData = false)
        {
            var classes = MightClasses(comp.Pawn, comp.MightData);
            RestoreEmptyClassicMightStarter(comp, classes);
            if (newData)
                foreach (var saved in comp.AbilityData.Powers.ToList())
                {
                    if (saved?.Def == null) continue;
                    var owner = comp.MightData.AllMightPowers.FirstOrDefault(p => p.TMabilityDefs.Contains(saved.Def));
                    if (owner == null || (!classes.Any(c => Matches(c.Abilities, owner.TMabilityDefs))
                        && !comp.MightData.MightPowersStandalone.Contains(owner) && !comp.MightData.MightPowersCustomStandalone.Contains(owner))) continue;
                    owner.learned = true;
                    owner.level = Math.Max(owner.level, owner.TMabilityDefs.IndexOf(saved.Def));
                }
            if (!ClassProgression.LegacyMartialConversion.RestoreImportedTools(comp))
            {
                ClassProgression.JobDriver_GladiatorInitiation.Restore(comp);
                ClassProgression.SniperSurvey.Restore(comp);
                ClassProgression.BladeCareer.Restore(comp);
                ClassProgression.RangerCareer.Restore(comp);
                if (ClassProgression.ClassIdentity.TryGetState(comp.Pawn, out var martialState))
                    ClassProgression.AdditionalMartialRegistry.Current(martialState, out var discipline)?.Restore(comp, discipline);
            }
            ReconcileMight(comp, newData ? new List<ClassEntry>() : classes, classes);
            foreach (var power in ClassProgression.WandererLessons.MartialPicks(comp, ClassProgression.WandererLessons.Path(comp.Pawn)))
                Grant(comp, power.abilityDef, power.TMabilityDefs);
            foreach (var power in comp.MightData.MightPowersStandalone.Concat(comp.MightData.MightPowersCustomStandalone).Where(p => p.learned))
                Grant(comp, power.abilityDef, power.TMabilityDefs);
            comp.RestoreSavedCooldowns();
        }

        // An existing data block is not proof that a classic class was ever
        // initialized. Repair only completely empty classes, without a reset.
        // Even with beta disabled, saved staged-progression pawns are excluded.
        private static bool CanRestoreClassicStarter(CompAbilityUser comp) =>
            comp.Pawn.GetComp<ClassProgression.CompClassProgression>()?.State == null &&
            !comp.AbilityData.Powers.Any(p => p?.Def != null);

        private static void RestoreEmptyClassicMagicStarter(CompAbilityUserMagic comp, List<ClassEntry> classes)
        {
            if (!CanRestoreClassicStarter(comp)) return;
            var powers = classes.SelectMany(c => c.Abilities).Select(a => PreferredMagicOwner(comp, a))
                .Where(p => p != null).Distinct().ToList();
            if (powers.Any(p => p.learned) || comp.MagicData.MagicPowersStandalone.Any(p => p.learned) ||
                comp.MagicData.MagicPowersCustomStandalone.Any(p => p.learned)) return;
            var starter = classes.Where(c => !c.Advanced).SelectMany(c => c.Abilities)
                .Select(a => PreferredMagicOwner(comp, a)).FirstOrDefault(p => p != null && !p.requiresScroll);
            if (starter != null) starter.learned = true;
        }

        private static void RestoreEmptyClassicMightStarter(CompAbilityUserMight comp, List<ClassEntry> classes)
        {
            if (!CanRestoreClassicStarter(comp)) return;
            var powers = classes.SelectMany(c => c.Abilities)
                .Select(a => comp.MightData.AllMightPowers.FirstOrDefault(p => p.TMabilityDefs.Contains(a)))
                .Where(p => p != null).Distinct().ToList();
            if (powers.Any(p => p.learned) || comp.MightData.MightPowersStandalone.Any(p => p.learned) ||
                comp.MightData.MightPowersCustomStandalone.Any(p => p.learned)) return;
            var starter = classes.Where(c => !c.Advanced).SelectMany(c => c.Abilities)
                .Where(a => (a as TMAbilityDef)?.learnItem == null)
                .Select(a => comp.MightData.AllMightPowers.FirstOrDefault(p => p.TMabilityDefs.Contains(a)))
                .FirstOrDefault(p => p != null);
            if (starter != null) starter.learned = true;
        }

        public static void GrantCurrentMagicStarts(CompAbilityUserMagic comp)
        {
            var classes = MagicClasses(comp.Pawn, comp.MagicData);
            foreach (var entry in classes) entry.Advanced = false;
            ReconcileMagic(comp, new List<ClassEntry>(), classes);
        }

        public static void GrantCurrentMightStarts(CompAbilityUserMight comp)
        {
            var classes = MightClasses(comp.Pawn, comp.MightData);
            foreach (var entry in classes) entry.Advanced = false;
            ReconcileMight(comp, new List<ClassEntry>(), classes);
        }

        public static bool HasSavedMagicProgression(MagicData data) => data != null &&
            (data.initialized || data.MagicUserLevel != 0 || data.MagicUserXP != 0 || data.MagicAbilityPoints != 0 ||
             data.AllMagicPowers.Any(p => p.level != 0 || p.learned) || data.AllMagicPowerSkills.Any(s => s.level != 0));

        public static bool IsMagicPowerAuthorized(CompAbilityUserMagic comp, MagicPower power)
        {
            var data = comp.MagicData;
            return data.MagicPowersStandalone.Contains(power) || data.MagicPowersCustomStandalone.Contains(power) || IsIndependentCustomCantrip(comp, power) ||
                MagicClasses(comp.Pawn, data).Any(c => Matches(c.Abilities, power.TMabilityDefs)) ||
                KnowledgeLearned(comp.Pawn, power.TMabilityDefs);
        }

        // A scroll legitimately learned while another class owned the power
        // (a Brightmage reading Heal) keeps its knowledge entry across saves,
        // and the entry is only written by legitimate learning, so it is the
        // source of truth for keeping the ability on the bar. Unlearned
        // off-class powers stay unauthorized.
        private static bool KnowledgeLearned(Pawn pawn, List<AbilityUser.AbilityDef> defs)
        {
            var knowledge = GrimoireKnowledge.ActiveKnowledge(pawn);
            if (knowledge == null || !knowledge.ReadyForQuery || defs == null) return false;
            foreach (var def in defs.OfType<TMAbilityDef>())
            {
                var entry = knowledge.Find(SpellIds.Of(def));
                if (entry != null && entry.learned) return true;
            }
            return false;
        }

        // Custom powers with upgrade records live outside CustomStandalone even
        // when they are shared scroll spells (for example Kure Magic Training).
        // Do not recover a former class's spell merely because its learned bit remains.
        private static bool IsIndependentCustomCantrip(CompAbilityUserMagic comp, MagicPower power)
        {
            var data = comp.MagicData;
            if (!data.MagicPowersCustom.Contains(power) ||
                !power.TMabilityDefs.OfType<TMAbilityDef>().Any(a => a.learnItem != null)) return false;
            if (TM_ClassUtility.CustomClasses.Any(c => c.classMageAbilities != null && Matches(c.classMageAbilities, power.TMabilityDefs))) return false;
            return !TM_ClassGroups.MagicEntriesField.Any(e =>
                (TM_ClassGroups.ResolveMagicList(data, e.field) ?? new List<MagicPower>())
                    .Any(p => Matches(p.TMabilityDefs, power.TMabilityDefs)));
        }

        private static IEnumerable<MagicPower> SharedMagicPowers(CompAbilityUserMagic comp)
            => comp.MagicData.MagicPowersStandalone.Concat(comp.MagicData.MagicPowersCustomStandalone)
                .Concat(comp.MagicData.MagicPowersCustom.Where(p => IsIndependentCustomCantrip(comp, p))).Distinct();

        public static void ResetMagicRuntime(CompAbilityUserMagic comp)
        {
            comp.AbilityData.Powers.Clear();
            comp.AbilityData.AllPowers = null;
            foreach (var flag in MagicFlags) flag.SetValue(comp, false);
            comp.recallSpell = false;
            comp.recallSet = false;
            comp.SetIsMagicUser();
        }

        public static void ResetMightRuntime(CompAbilityUserMight comp)
        {
            comp.AbilityData.Powers.Clear();
            comp.AbilityData.AllPowers = null;
            foreach (var flag in MightFlags) flag.SetValue(comp, false);
            comp.SetIsMightUser();
        }
    }
}
