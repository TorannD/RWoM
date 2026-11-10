using Verse;

namespace TorannMagic.Core
{
    public enum KnowledgeStatus
    {
        ProviderUnavailable, InvalidRequest, PendingImport, NoRecord, Available, MissingDefinition
    }

    public readonly record struct SpellKnowledgeSnapshot(
        string SpellId, bool Learned, int Tier, int Power, int Efficiency, int Versatility, bool Autocast);

    public static class PawnSpellKnowledge
    {
        // A nullable result distinguishes a pending import from an absent record.
        internal delegate bool? Reader(Pawn pawn, string id, out bool learned, out int tier,
            out int power, out int efficiency, out int versatility, out bool autocast);
        internal static Reader HostReader { private get; set; }

        public static KnowledgeStatus Read(Pawn pawn, string spellId, out SpellKnowledgeSnapshot snapshot)
        {
            snapshot = default;
            if (pawn == null || string.IsNullOrWhiteSpace(spellId)) return KnowledgeStatus.InvalidRequest;
            if (HostReader == null) return KnowledgeStatus.ProviderUnavailable;

            bool? found = HostReader(pawn, spellId, out bool learned, out int tier, out int power,
                out int efficiency, out int versatility, out bool autocast);
            if (!found.HasValue) return KnowledgeStatus.PendingImport;
            if (!found.Value) return KnowledgeStatus.NoRecord;

            snapshot = new(spellId, learned, tier, power, efficiency, versatility, autocast);
            return AbilityCore.TryResolve(spellId, out _) ? KnowledgeStatus.Available : KnowledgeStatus.MissingDefinition;
        }
    }
}
