using System.Reflection;
using Verse;

namespace TorannMagic
{
    internal static class CoreKnowledgeProvider
    {
        // The host can run with no Core or with a Core predating the knowledge API.
        internal static void Bind(Assembly core)
        {
            BindSlot(core, "PawnSpellKnowledge", typeof(CoreKnowledgeProvider), nameof(Read));
            BindSlot(core, "AbilityCasting", typeof(CoreCastProvider), nameof(CoreCastProvider.Check));
            BindSlot(core, "PawnDisciplines", typeof(CoreDisciplineProvider), nameof(CoreDisciplineProvider.Read));
        }

        private static void BindSlot(Assembly core, string api, System.Type provider, string name)
        {
            var slot = core.GetType("TorannMagic.Core." + api)?.GetProperty("HostReader",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (slot == null) return;
            var method = provider.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
            var reader = System.Delegate.CreateDelegate(slot.PropertyType, method, false);
            if (reader != null) slot.SetValue(null, reader);
        }

        private static bool? Read(Pawn pawn, string id, out bool learned, out int tier, out int power,
            out int efficiency, out int versatility, out bool autocast)
        {
            learned = autocast = false;
            tier = power = efficiency = versatility = 0;
            var comps = pawn.AllComps;
            if (comps == null) return false;
            foreach (var comp in comps)
            {
                if (!(comp is AbilityKnowledgeComp knowledge)) continue;
                if (!knowledge.ReadyForQuery) return null;
                var entry = knowledge.Find(id);
                if (entry == null) return false;
                learned = entry.learned;
                tier = entry.tier;
                power = entry.pwr;
                efficiency = entry.eff;
                versatility = entry.ver;
                autocast = entry.autocast;
                return true;
            }
            return false;
        }
    }
}
