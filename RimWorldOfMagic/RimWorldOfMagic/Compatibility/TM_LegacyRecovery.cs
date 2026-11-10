using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using TorannMagic.Utils;
using Verse;
using AbilityUser;
using System.Globalization;

namespace TorannMagic.Compatibility
{
    public static class TM_LegacyRecovery
    {
        // Explicit debug action only. Automatic post-load repair stays quiet.
        public static void RepairWithReport(Pawn pawn)
        {
            string who = pawn == null ? "<none>" : pawn.ThingID + " (" + (pawn.Name?.ToStringShort ?? "unnamed") + ")";
            string prefix = "Repair Missing Class Data; pawn=" + who + "; ";
            TM_Log.Message("SaveRecovery", prefix + "status=started; " + SafeIdentity(pawn));
            try
            {
                string reason = pawn == null ? "no pawn" : pawn.story?.traits == null ? "no trait tracker"
                    : pawn.Dead ? "dead pawn" : pawn.IsShambler || pawn.IsGhoul ? "unsupported ghoul/shambler" : null;
                bool mage = reason == null && TM_ClassState.HasCurrentClass(pawn, true);
                bool fighter = reason == null && TM_ClassState.HasCurrentClass(pawn, false);
                if (reason == null && !mage && !fighter) reason = "no class trait";
                if (reason != null)
                {
                    TM_Log.Message("SaveRecovery", prefix + "status=skipped; reason=" + reason);
                    return;
                }
                string before = SafeSnapshot(pawn);
                Repair(pawn);
                string after = SafeSnapshot(pawn);
                var magic = pawn.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
                var might = pawn.AllComps.OfType<CompAbilityUserMight>().FirstOrDefault();
                bool incomplete = (mage && (RawData(magic, "magicData") == null || pawn.needs?.AllNeeds?.OfType<Need_Mana>().FirstOrDefault() == null))
                    || (fighter && (RawData(might, "mightData") == null || pawn.needs?.AllNeeds?.OfType<Need_Stamina>().FirstOrDefault() == null));
                string result = prefix + "status=" + (incomplete ? "incomplete" : "completed")
                    + "; summaryChanged=" + (before != after) + "; before: " + before + "; after: " + after;
                if (incomplete) TM_Log.Warning("SaveRecovery", result + "; class data or resource need is still missing");
                else TM_Log.Message("SaveRecovery", result);
            }
            catch (Exception e) { TM_Log.Error("SaveRecovery", prefix + "status=failed; repair may be partial", e); }
        }

        private static object RawData(object comp, string field) => comp == null ? null : AccessTools.Field(comp.GetType(), field)?.GetValue(comp);

        private static string SafeIdentity(Pawn pawn)
        {
            try
            {
                if (pawn?.story?.traits == null) return "identity=unavailable";
                var traits = pawn.story.traits.allTraits;
                var magic = pawn.AllComps?.OfType<CompAbilityUserMagic>().FirstOrDefault();
                var might = pawn.AllComps?.OfType<CompAbilityUserMight>().FirstOrDefault();
                var declared = (TM_ClassUtility.CustomClasses ?? new System.Collections.Generic.List<TMDefs.TM_CustomClass>())
                    .Where(c => c?.classTrait != null && traits.Any(t => t.def?.defName == c.classTrait.defName));
                // Never read lazy comp getters here: diagnostics must show the
                // existing cache and data without initializing or repairing them.
                return "identity: traits=[" + string.Join(",", traits.Select(t => (t.def?.defName ?? "<missing>") + ":" + t.Degree + ":suppressed=" + t.Suppressed))
                    + "]; declared=[" + string.Join(",", declared.Select(c => c.classTrait.defName + ":" + c.traitDegree))
                    + "]; resolvedMagic=" + TM_ClassState.HasCurrentClass(pawn, true)
                    + "; resolvedMight=" + TM_ClassState.HasCurrentClass(pawn, false)
                    + "; resolvedCustomMagic=[" + string.Join(",", TM_ClassState.CurrentCustomClasses(pawn, true).Select(c => c.classTrait.defName + ":" + c.traitDegree))
                    + "]; resolvedCustomMight=[" + string.Join(",", TM_ClassState.CurrentCustomClasses(pawn, false).Select(c => c.classTrait.defName + ":" + c.traitDegree)) + "]"
                    + "; cachedMagic=" + (RawData(magic, "isMagicUser") ?? "missing")
                    + "; magicCacheInitialized=" + (RawData(magic, "initializedIsMagicUser") ?? "missing")
                    + "; cachedMight=" + (RawData(might, "isMightUser") ?? "missing")
                    + "; magicTick=" + (magic == null ? "missing" : magic.TickConditionsMet.ToString())
                    + "; mightTick=" + (might == null ? "missing" : might.TickConditionsMet.ToString());
            }
            catch (Exception e) { return "identity unavailable (" + e.GetType().Name + ")"; }
        }

        private static string SafeSnapshot(Pawn pawn)
        {
            try { return RecoverySnapshot(pawn); }
            catch (Exception e) { return "snapshot unavailable (" + e.GetType().Name + ")"; }
        }

        private static string RecoverySnapshot(Pawn pawn)
        {
            var magic = pawn.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            var might = pawn.AllComps.OfType<CompAbilityUserMight>().FirstOrDefault();
            return "magic=" + DataSummary(magic, RawData(magic, "magicData"))
                + "; might=" + DataSummary(might, RawData(might, "mightData"))
                + "; mana=" + NeedSummary(pawn.needs?.AllNeeds?.OfType<Need_Mana>().FirstOrDefault())
                + "; stamina=" + NeedSummary(pawn.needs?.AllNeeds?.OfType<Need_Stamina>().FirstOrDefault());
        }

        private static string NeedSummary(Need need) => need == null ? "missing" : need.CurLevel.ToString("R", CultureInfo.InvariantCulture);

        private static string DataSummary(CompAbilityUser comp, object data)
        {
            if (comp == null) return "missing";
            if (data == null) return "data missing";
            // Read backing fields, not lazy data/list getters: diagnostics must
            // not create the very records whose absence they are reporting.
            var records = data.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(f => f.GetValue(data)).OfType<IList>().SelectMany(list => list.Cast<object>()).Distinct().ToList();
            var runtime = RawData(comp, "abilityData") as AbilityData;
            var m = data as MagicData; var t = data as MightData;
            return "[level=" + (m != null ? m.MagicUserLevel : t.MightUserLevel)
                + ", XP=" + (m != null ? m.MagicUserXP : t.MightUserXP)
                + ", points=" + (m != null ? m.MagicAbilityPoints : t.MightAbilityPoints)
                + ", abilities=" + (runtime?.Powers == null ? "missing" : runtime.Powers.Count.ToString(CultureInfo.InvariantCulture))
                + ", learned=" + (records.OfType<MagicPower>().Count(p => p.learned) + records.OfType<MightPower>().Count(p => p.learned))
                + ", skillRecords=" + (records.OfType<MagicPowerSkill>().Count() + records.OfType<MightPowerSkill>().Count()) + "]";
        }

        // Only declared builtin lists, never aggregates or XML/custom registries.
        // Defaults supply absent records, not replacement values for saved records.
        public static int FillMissingRecords(object data)
        {
            if (!(data is MagicData) && !(data is MightData)) return 0;
            object defaults = Activator.CreateInstance(data.GetType());
            int added = 0;
            foreach (var property in data.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                string name = property.Name;
                if (name.Contains("Custom") || !(name.StartsWith("MagicPowers") || name.StartsWith("MightPowers")
                    || name.StartsWith("MagicPowerSkill_") || name.StartsWith("MightPowerSkill_"))) continue;
                if (!(property.GetValue(data, null) is IList saved) || !(property.GetValue(defaults, null) is IList baseline)) continue;
                foreach (object entry in baseline)
                {
                    string key = Key(entry);
                    if (key == null || saved.Cast<object>().Any(s => Key(s) == key)) continue;
                    saved.Add(entry);
                    added++;
                }
            }
            return added;
        }

        private static string Key(object entry)
        {
            if (entry is MagicPower p) return p.TMabilityDefs?.FirstOrDefault()?.defName;
            if (entry is MightPower m) return m.TMabilityDefs?.FirstOrDefault()?.defName;
            if (entry is MagicPowerSkill s) return s.label;
            if (entry is MightPowerSkill t) return t.label;
            return null;
        }

        public static void Repair(Pawn pawn)
        {
            if (pawn?.story?.traits == null || pawn.Dead || pawn.IsShambler || pawn.IsGhoul) return;
            bool mage = TM_ClassState.HasCurrentClass(pawn, true);
            bool fighter = TM_ClassState.HasCurrentClass(pawn, false);
            if (!mage && !fighter) return;
            TM_PawnTracker.EnsureClassComps(pawn);
            TM_PawnTracker.ResolveComps(pawn);
            var magic = pawn.GetCompAbilityUserMagic();
            var might = pawn.GetCompAbilityUserMight();
            if (mage && magic != null)
            {
                magic.RecoverMissingProgression();
                TM_ClassState.RestoreCurrentMagicRuntime(magic);
                RestoreNeed(pawn, TorannMagicDefOf.TM_MagicUserHD, TorannMagicDefOf.TM_Mana);
            }
            if (fighter && might != null)
            {
                might.RecoverMissingProgression();
                TM_ClassState.RestoreCurrentMightRuntime(might);
                RestoreNeed(pawn, TorannMagicDefOf.TM_MightUserHD, TorannMagicDefOf.TM_Stamina);
            }
        }

        public static void RestoreNeed(Pawn pawn, HediffDef enabler, NeedDef need)
        {
            if (pawn?.health?.hediffSet == null || pawn.needs == null || enabler == null || need == null) return;
            // Preserve the existing need object and its exact level, including zero.
            if (!pawn.health.hediffSet.HasHediff(enabler)) HealthUtility.AdjustSeverity(pawn, enabler, 1f);
            if (pawn.needs.TryGetNeed(need) != null) return;
            var should = AccessTools.Method(typeof(Pawn_NeedsTracker), "ShouldHaveNeed");
            if ((bool)should.Invoke(pawn.needs, new object[] { need }))
                AccessTools.Method(typeof(Pawn_NeedsTracker), "AddNeed").Invoke(pawn.needs, new object[] { need });
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExposeData))]
    internal static class LegacyPawnRecoveryPatch
    {
        private static void Postfix(Pawn __instance)
        {
            if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
            // Needs, health and cross references must all finish loading first.
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                try
                {
                    ClassProgression.LegacyClassConversion.TryImport(__instance);
                    TM_LegacyRecovery.Repair(__instance);
                }
                catch (Exception e) { TM_Log.Warning("SaveRecovery", "Pawn recovery incomplete: " + e); }
            });
        }
    }
}
