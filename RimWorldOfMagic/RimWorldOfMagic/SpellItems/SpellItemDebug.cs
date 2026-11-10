using System.Collections.Generic;
using LudeonTK;
using Verse;

namespace TorannMagic.SpellItems
{
    public static class SpellItemDebug
    {
        private static readonly string[] CastScrollDefs =
        {
            "TM_CastScroll_Fireball",
            "TM_CastScroll_Heal",
            "TM_CastScroll_Blink",
            "TM_CastScroll_Lightning",
            "TM_CastScroll_Regeneration",
            "TM_CastScroll_CureDisease",
            "TM_CastScroll_Purify",
            "TM_CastScroll_Soothe",
            "TM_CastScroll_Portal",
            "TM_CastScroll_Resurrection"
        };

        private static readonly string[] ChargedStaffDefs =
        {
            "TM_SpellStaff_Fireball",
            "TM_SpellStaff_Heal",
            "TM_SpellStaff_Blink",
            "TM_SpellStaff_Lightning",
            "TM_SpellStaff_Regeneration",
            "TM_SpellStaff_CureDisease",
            "TM_SpellStaff_Purify",
            "TM_SpellStaff_Soothe",
            "TM_SpellStaff_Portal"
        };

        [DebugAction("RWoM", "Spawn cast scroll...", actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnCastScroll()
        {
            OpenSpawnMenu(CastScrollDefs);
        }

        [DebugAction("RWoM", "Spawn charged staff...", actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnChargedStaff()
        {
            OpenSpawnMenu(ChargedStaffDefs);
        }

        private static void OpenSpawnMenu(IEnumerable<string> defNames)
        {
            var options = new List<DebugMenuOption>();
            foreach (string defName in defNames)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def == null)
                {
                    Compatibility.TM_Log.Warning("SpellItems", "Missing spell-item debug spawn def: " + defName);
                    continue;
                }

                ThingDef selected = def;
                options.Add(new DebugMenuOption(selected.LabelCap.ToString(), DebugMenuOptionMode.Action,
                    delegate
                    {
                        DebugTools.curTool = new DebugTool("Spawn " + selected.LabelCap, delegate
                        {
                            Map map = Find.CurrentMap;
                            IntVec3 cell = UI.MouseCell();
                            if (map != null && cell.InBounds(map))
                            {
                                DebugThingPlaceHelper.DebugSpawn(selected, cell, 1, false, null);
                            }
                        });
                    }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }
    }
}
