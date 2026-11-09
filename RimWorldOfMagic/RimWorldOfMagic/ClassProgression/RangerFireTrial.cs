using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    [HarmonyPatch(typeof(DamageWorker_Extinguish), nameof(DamageWorker_Extinguish.Apply))]
    internal static class RangerFireTrial
    {
        private static void Prefix(DamageInfo dinfo, Thing victim, out (IntVec3 cell,Pawn owner) __state)
        {
            __state = default;
            if (victim is not Fire fire || !fire.Spawned || dinfo.Instigator is not Pawn ranger ||
                ranger.jobs?.curDriver is not JobDriver_BeatFire || ranger.CurJob.targetA.Thing != fire ||
                fire.instigator?.Faction?.IsPlayer == true || fire.parent is Pawn || ranger.Dead || ranger.Downed ||
                ranger.Faction?.IsPlayer != true || ranger.Map != fire.Map ||
                RangerCareer.ActivePath(ranger,out var owner)?.Stage != InitiationStage.Adept) return;
            __state = (fire.Position,owner);
        }

        private static void Postfix(DamageInfo dinfo, Thing victim, (IntVec3 cell,Pawn owner) __state)
        {
            if (__state.owner==null || !victim.Destroyed || dinfo.Instigator is not Pawn ranger || !ranger.Spawned) return;
            if (StoredGoods(__state.cell,ranger)) RangerMastery.ClearedFrom(ranger,__state.owner,__state.cell);
        }

        private static bool StoredGoods(IntVec3 origin, Pawn ranger)
        {
            var map = ranger.Map;
            foreach (var offset in GenAdj.AdjacentCellsAndInside)
            {
                var cell = origin + offset;
                if (!cell.InBounds(map)) continue;
                var group = cell.GetSlotGroup(map);
                if (group == null || !(group.parent is Zone_Stockpile || group.parent is Building store && store.Faction == ranger.Faction))
                    continue;
                var things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    var item = things[i];
                    if (item.def.category == ThingCategory.Item && item.stackCount > 0 && !item.IsForbidden(ranger.Faction) &&
                        group.Settings.AllowedToAccept(item) && item.GetStatValue(StatDefOf.Flammability) > 0f)
                        return true;
                }
            }
            return false;
        }
    }
}
