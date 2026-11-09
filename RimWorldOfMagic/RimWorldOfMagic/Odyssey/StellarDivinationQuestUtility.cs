using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
namespace TorannMagic.Odyssey
{
    public static class StellarDivinationQuestUtility
    {
        private static Slate SlateFor(Map map)
        {
            var slate = new Slate();
            slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(map));
            slate.Set("discoveryMethod", "TM_StellarDiscovery".Translate().ToString());
            return slate;
        }
        public static List<QuestScriptDef> EligibleQuests(Map map, Slate slate)
        {
            if (!ModsConfig.OdysseyActive || map == null || Find.World == null || QuestGen.Working) return new List<QuestScriptDef>();
            float points = slate.Get<float>("points");
            var result = new List<QuestScriptDef>();
            foreach (var quest in QuestUtility.GetGiverQuests(QuestGiverTag.OrbitalScanner))
            {
                if (quest == null) continue;
                try
                {
                    if (quest.CanRun(slate, map) && NaturalRandomQuestChooser.GetNaturalRandomSelectionWeight(quest, points, Find.World.StoryState) > 0f) result.Add(quest);
                }
                catch (Exception ex)
                {
                    Log.WarningOnce("[RWoM] Stellar quest unavailable: " + quest.defName + ": " + ex.Message,
                        GenText.StableStringHash("TM_stellar_" + quest.defName));
                }
            }
            return result;
        }
        public static bool HasEligibleQuest(Map map) => map != null && EligibleQuests(map, SlateFor(map)).Count != 0;
        public static Quest Generate(Map map)
        {
            var slate = SlateFor(map);
            var eligible = EligibleQuests(map, slate);
            if (eligible.Count == 0) return null;
            float points = slate.Get<float>("points");
            var root = eligible.RandomElementByWeight(q => NaturalRandomQuestChooser.GetNaturalRandomSelectionWeight(q, points, Find.World.StoryState));
            // Generate separately from registration, so null generation never reaches QuestManager.Add.
            return QuestGen.Generate(root, slate);
        }
        public static void SendLetter(Quest quest)
        {
            if (!quest.hidden && quest.root.sendAvailableLetter)
                QuestUtility.SendLetterQuestAvailable(quest, "TM_StellarDiscovery".Translate());
        }
    }
}
