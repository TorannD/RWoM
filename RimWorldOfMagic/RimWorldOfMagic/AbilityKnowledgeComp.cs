using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic
{
    // Unified knowledge store for all spells a pawn knows. Versioned schema with
    // stepwise migrations. Replaces 26+125 legacy lists and 53 bool flags.
    // Consumers access through TryGet/TrySet — no direct def or label derefs.
    public class AbilityKnowledgeComp : ThingComp, IExposable
    {
        // v2: v1 stores drifted from legacy (toggles moved after the one-shot
        // import, before dual-write existed, and ranks were never imported).
        // A v1 load arms a one-shot reconcile on the next tick.
        public const int CurrentSchemaVersion = 2;
        private int schemaVersion = CurrentSchemaVersion;
        private bool legacyImported;
        private bool pendingReconcile;
        private bool checkShadowStepOnLoad;
        private List<SpellKnowledge> spells = new List<SpellKnowledge>();

        internal int NextDiagnosticTick { get; set; }
        public IReadOnlyList<SpellKnowledge> Spells => spells;

        public static AbilityKnowledgeComp ActiveFor(Pawn pawn)
            => CoreBridge.Available ? pawn?.TryGetComp<AbilityKnowledgeComp>() : null;
        internal bool ReadyForQuery => legacyImported && !pendingReconcile;
        public int SchemaVersion => schemaVersion;

        public SpellKnowledge Find(string spellId)
            => spells.FirstOrDefault(s => s != null && s.spellId == spellId);

        public SpellKnowledge GetOrCreate(string spellId, int tier)
        {
            var existing = Find(spellId);
            if (existing == null)
            {
                existing = new SpellKnowledge { spellId = spellId, tier = tier };
                spells.Add(existing);
            }
            return existing;
        }

        public bool Remove(string spellId)
            => spells.RemoveAll(s => s != null && s.spellId == spellId) > 0;

        // Skill rank by role: 0 = Power, 1 = Efficiency, 2 = Versatility.
        // Missing entries return 0 — never NRE.
        public int GetSkillRank(string spellId, int role)
        {
            var s = Find(spellId);
            if (s == null) return 0;
            return role switch { 0 => s.pwr, 1 => s.eff, 2 => s.ver, _ => 0 };
        }

        public void SetSkillRank(string spellId, int role, int value)
        {
            var s = GetOrCreate(spellId, 0);
            if (role == 0) s.pwr = value;
            else if (role == 1) s.eff = value;
            else if (role == 2) s.ver = value;
        }

        // One-shot import from legacy MagicData/MightData lists. Runs on the first
        // CompTick after spawn — by that time all comps are loaded and initialized.
        public void ImportLegacy()
        {
            if (legacyImported) return;
            ImportNow();
            legacyImported = true;
        }

        private void ImportNow(bool shadowStepsOnly = false)
        {
            var pawn = parent as Pawn;
            if (pawn == null) return;

            var imported = new HashSet<string>();
            var magicComp = pawn.TryGetComp<CompAbilityUserMagic>();
            if (magicComp?.ExistingMagicData != null)
            {
                foreach (var power in magicComp.MagicData.AllMagicPowers)
                {
                    if (power?.abilityDef is not TMAbilityDef ability) continue;
                    var spellId = SpellIds.Of(power.abilityDef);
                    if (shadowStepsOnly && spellId != SpellIds.NativeShadowStepId && spellId != SpellIds.KureShadowStepId) continue;
                    if (!power.learned)
                    {
                        if (!imported.Contains(spellId)) Remove(spellId);
                        continue;
                    }
                    if (!imported.Add(spellId)) continue;
                    var entry = GetOrCreate(spellId, power.level);
                    entry.tier = power.level;
                    entry.learned = power.learned;
                    entry.autocast = power.autocast;
                    entry.pwr = magicComp.MagicData.GetSkill_Power(ability)?.level ?? entry.pwr;
                    entry.eff = magicComp.MagicData.GetSkill_Efficiency(ability)?.level ?? entry.eff;
                    entry.ver = magicComp.MagicData.GetSkill_Versatility(ability)?.level ?? entry.ver;
                }
            }

            var mightComp = pawn.TryGetComp<CompAbilityUserMight>();
            if (mightComp?.ExistingMightData != null)
            {
                foreach (var power in mightComp.MightData.AllMightPowers)
                {
                    if (power?.abilityDef is not TMAbilityDef ability) continue;
                    var spellId = SpellIds.Of(power.abilityDef);
                    if (shadowStepsOnly && spellId != SpellIds.NativeShadowStepId && spellId != SpellIds.KureShadowStepId) continue;
                    if (!power.learned)
                    {
                        if (!imported.Contains(spellId)) Remove(spellId);
                        continue;
                    }
                    if (!imported.Add(spellId)) continue;
                    var entry = GetOrCreate(spellId, power.level);
                    entry.tier = power.level;
                    entry.learned = power.learned;
                    entry.autocast = power.autocast;
                    entry.pwr = mightComp.MightData.GetSkill_Power(ability)?.level ?? entry.pwr;
                    entry.eff = mightComp.MightData.GetSkill_Efficiency(ability)?.level ?? entry.eff;
                    entry.ver = mightComp.MightData.GetSkill_Versatility(ability)?.level ?? entry.ver;
                }
            }
        }

        public override void PostExposeData()
        {
            if (!Scribe.EnterNode("TM_abilityKnowledge")) return;
            try { ExposeData(); }
            finally { Scribe.ExitNode(); }
        }

        public void ExposeData()
        {
            // Previous unversioned knowledge nodes used schema 2. Always write the version
            // so a later schema change cannot silently adopt old data as current.
            Scribe_Values.Look(ref schemaVersion, "schemaVersion", 2, forceSave: true);
            Scribe_Values.Look(ref legacyImported, "legacyImported", false);
            Scribe_Collections.Look(ref spells, "spells", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                spells ??= new List<SpellKnowledge>();
                spells.RemoveAll(s => s == null || string.IsNullOrEmpty(s.spellId));
                // Comps are not fully loaded during PostLoadInit — defer the
                // re-import to the first tick.
                if (schemaVersion < CurrentSchemaVersion) pendingReconcile = true;
                schemaVersion = CurrentSchemaVersion;
                checkShadowStepOnLoad = Find(SpellIds.NativeShadowStepId) != null && Find(SpellIds.KureShadowStepId) == null;
            }
        }

        public override void CompTick()
        {
            // One-shot legacy import: runs on the first CompTick after spawn,
            // when all other comps have loaded their data.
            ImportLegacy();
            if (checkShadowStepOnLoad)
            {
                checkShadowStepOnLoad = false;
                if (parent is Pawn owner && owner.GetCompAbilityUserMight()?.ExistingMightData is MightData might &&
                    might.AllMightPowers.Any(p => p?.learned == true && p.TMabilityDefs?.Any(a => a?.defName == "TM_Shadowstep") == true))
                {
                    // Only a real, learned Kure owner can reclaim the old ambiguous key.
                    // A soul with both abilities keeps both entries; others are untouched.
                    if (owner.GetCompAbilityUserMagic()?.ExistingMagicData?.AllMagicPowers.Any(p =>
                        p?.learned == true && p.TMabilityDefs?.Any(a => a?.defName == "TM_ShadowStep") == true) != true)
                        Remove(SpellIds.NativeShadowStepId);
                    ImportNow(shadowStepsOnly: true);
                }
            }
            // Schema migration: re-read drifted flags and ranks from legacy once.
            if (pendingReconcile)
            {
                pendingReconcile = false;
                ImportNow();
            }
            // Ф5 spike: keep the vanilla tracker in sync with the knowledge
            // store (bridge pilot family) on the autocast cadence.
            if (CoreBridge.Available && parent is Pawn pawn && pawn.IsHashIntervalTick(180))
                VanillaBridgeSync.Sync(pawn);
        }
    }
}
