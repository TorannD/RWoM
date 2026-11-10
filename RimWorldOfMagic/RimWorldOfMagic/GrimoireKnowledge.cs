using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TorannMagic
{
    // Ф4 step 4: grimoire adapter over the knowledge store. Sleeping knowledge —
    // learned entries whose def no longer resolves — is listed for the grey
    // section; convention spellIds prettify into readable names. Card layout
    // itself keeps reading legacy owners while the migration runs.
    public static class GrimoireKnowledge
    {
        public static string DisplayName(string spellId)
        {
            if (string.IsNullOrEmpty(spellId)) return "???";
            if (!spellId.StartsWith("rwom.")) return spellId;
            string name = spellId.Substring("rwom.".Length);
            if (name.StartsWith("tm_")) name = name.Substring(3);
            string[] words = name.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length == 0) continue;
                words[i] = IsRomanNumeral(words[i])
                    ? words[i].ToUpperInvariant()
                    : char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            }
            return string.Join(" ", words);
        }

        private static bool IsRomanNumeral(string word)
        {
            foreach (char c in word)
                if (c != 'i' && c != 'v' && c != 'x') return false;
            return true;
        }

        // Learned entries whose def fails to resolve (removed or renamed spell).
        // Never throws; the list is recomputed per frame (knowledge lists are small).
        // The pawn's persisted spell knowledge, when loaded and reconciled.
        // Read-only gateway for restore paths: entries are only written by
        // legitimate learning, so a learned entry is authoritative ownership.
        public static AbilityKnowledgeComp ActiveKnowledge(Pawn pawn)
            => AbilityKnowledgeComp.ActiveFor(pawn);

        public static List<SpellKnowledge> SleepingSpells(Pawn pawn)
            => SleepingSpells(AbilityKnowledgeComp.ActiveFor(pawn));

        public static List<SpellKnowledge> SleepingSpells(AbilityKnowledgeComp knowledge)
        {
            var sleeping = new List<SpellKnowledge>();
            if (knowledge == null) return sleeping;
            foreach (SpellKnowledge spell in knowledge.Spells)
            {
                if (spell == null || !spell.learned || string.IsNullOrEmpty(spell.spellId)) continue;
                if (!CoreBridge.TryResolve(spell.spellId, out _)) sleeping.Add(spell);
            }
            return sleeping;
        }

        // Ф4 step 4 stage 2: grimoire writes (learn / level-up) mirror into the
        // knowledge store so it stays a complete mirror after the one-shot
        // import — the same dual-write contract as the autocast toggles.
        public static void ChangeTier(Pawn pawn, Def previous, Def current, int tier, bool learned, bool autocast)
        {
            var knowledge = pawn?.TryGetComp<AbilityKnowledgeComp>();
            if (knowledge == null || current == null) return;
            var oldId = SpellIds.Of(previous);
            var id = SpellIds.Of(current);
            var old = knowledge.Find(oldId);
            var next = knowledge.GetOrCreate(id, tier);
            next.tier = tier;
            next.learned = learned;
            next.autocast = autocast;
            if (old != null && old != next)
            {
                next.pwr = old.pwr;
                next.eff = old.eff;
                next.ver = old.ver;
                knowledge.Remove(oldId);
            }
        }

        public static void SyncEntry(Pawn pawn, Verse.Def def, int tier, bool learned)
        {
            AbilityKnowledgeComp knowledge = pawn?.TryGetComp<AbilityKnowledgeComp>();
            if (knowledge == null || def == null) return;
            SpellKnowledge entry = knowledge.GetOrCreate(SpellIds.Of(def), tier);
            entry.tier = tier;
            entry.learned = learned;
        }

        // Skill-rank mirror; the role comes from the legacy label suffix
        // (TM_X_pwr/eff/ver). Global stat skills have no suffix and are skipped.
        public static void SyncRank(Pawn pawn, Verse.Def def, string skillLabel, int rank)
        {
            if (def == TorannMagicDefOf.TM_MindOverBody && skillLabel == "TM_MindOverBody_pwr")
                ClassProgression.MonkMartialPath.RefreshTraining(pawn);
            int role = RoleOf(skillLabel);
            if (def == null || role < 0) return;
            AbilityKnowledgeComp knowledge = pawn?.TryGetComp<AbilityKnowledgeComp>();
            if (knowledge == null) return;
            knowledge.SetSkillRank(SpellIds.Of(def), role, rank);
        }

        public static int RoleOf(string skillLabel)
        {
            if (skillLabel == null) return -1;
            if (skillLabel.EndsWith("_pwr")) return 0;
            if (skillLabel.EndsWith("_eff")) return 1;
            if (skillLabel.EndsWith("_ver")) return 2;
            return -1;
        }
    }
}
