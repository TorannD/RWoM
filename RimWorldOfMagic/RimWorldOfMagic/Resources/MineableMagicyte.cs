using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.Resources
{
    public class MineableMagicyte : Mineable
    {
        private int magicyteYield = 15;
        private ThingDef bonusGem;
        private bool rewardConsumed;
        private bool initialized;

        public override void PostMake()
        {
            base.PostMake();
            if (initialized) return;
            magicyteYield = Rand.RangeInclusive(15, 100);
            if (Rand.Value < 0.01f)
            {
                string[] families = { "maxMP", "mpRegenRate", "mpCost", "coolDown", "xpGain", "arcaneRes", "arcaneDmg", "wonder" };
                string[] grades = { "_minor", "", "_major" };
                int choice = Rand.Range(0, families.Length * grades.Length);
                bonusGem = DefDatabase<ThingDef>.GetNamed("TM_EStone_" + families[choice / 3] + grades[choice % 3]);
            }
            initialized = true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref magicyteYield, "magicyteYield", 15);
            Scribe_Defs.Look(ref bonusGem, "magicyteBonusGem");
            Scribe_Values.Look(ref rewardConsumed, "magicyteRewardConsumed");
            Scribe_Values.Look(ref initialized, "magicyteInitialized");
            // Old/malformed saves receive a fixed minimum, never a new random roll.
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                magicyteYield = Math.Max(15, Math.Min(100, magicyteYield));
                initialized = true;
            }
        }

        internal void ResolveYield(Map map, bool mined, Pawn miner)
        {
            if (rewardConsumed) return;
            if (!mined) { rewardConsumed = true; return; }
            if (map == null) return;
            var rewards = map.GetComponent<MapComponent_MagicyteRewards>();
            bool forbidden = miner == null || miner.Faction != Faction.OfPlayer;
            rewards.Enqueue(def.building.mineableThing, magicyteYield, Position, forbidden);
            if (bonusGem != null) rewards.Enqueue(bonusGem, 1, Position, forbidden);
            rewardConsumed = true;
            rewards.TryDeliver();
        }
    }

    // DestroyMined is nonvirtual and invokes this private native method after
    // removing the rock. The combat Destroy path passes moteOnWaste=false.
    [HarmonyPatch(typeof(Mineable), "TrySpawnYield", new Type[] { typeof(Map), typeof(bool), typeof(Pawn) })]
    public static class MagicyteMiningYieldPatch
    {
        public static bool Prefix(Mineable __instance, Map map, bool moteOnWaste, Pawn pawn)
        {
            if (!(__instance is MineableMagicyte ore)) return true;
            ore.ResolveYield(map, moteOnWaste, pawn);
            return false;
        }
    }

    // A mined rock is already destroyed when native yield runs. Persist unpaid
    // quantities on the map so a blocked drop cell cannot erase its reward.
    public class MapComponent_MagicyteRewards : MapComponent
    {
        private List<MagicyteReward> pending = new List<MagicyteReward>();
        private bool delivering;
        public MapComponent_MagicyteRewards(Map map) : base(map) { }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pending, "magicytePendingRewards", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && pending == null) pending = new List<MagicyteReward>();
        }
        internal void Enqueue(ThingDef resource, int count, IntVec3 cell, bool forbidden)
        {
            pending.Add(new MagicyteReward { resource = resource, remaining = count, cell = cell, forbidden = forbidden });
        }
        public override void MapComponentTick()
        {
            if (pending.Count > 0 && Find.TickManager.TicksGame % 60 == 0) TryDeliver();
        }
        public void TryDeliver()
        {
            if (delivering) return;
            delivering = true;
            try
            {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var reward = pending[i];
                if (reward.resource == null) continue;
                while (reward.remaining > 0)
                {
                    int count = Math.Min(reward.remaining, Math.Max(1, reward.resource.stackLimit));
                    Thing stack = ThingMaker.MakeThing(reward.resource);
                    stack.stackCount = count;
                    bool placed;
                    int unpaid;
                    try
                    {
                        placed = GenPlace.TryPlaceThing(stack, reward.cell, map, ThingPlaceMode.Near,
                            (thing, added) => { if (reward.forbidden && thing.def.EverHaulable && !thing.def.designateHaulable) thing.SetForbidden(true, false); });
                    }
                    finally
                    {
                        // A placement callback can throw after merging or spawning.
                        // Account for that transfer even when placement did not return.
                        unpaid = stack.Destroyed || stack.Spawned ? 0 : stack.stackCount;
                        reward.remaining -= count - unpaid;
                        if (!stack.Destroyed && !stack.Spawned) stack.Destroy(DestroyMode.Vanish);
                    }
                    if (!placed || unpaid > 0) break;
                }
                if (reward.remaining == 0) pending.RemoveAt(i);
            }
            }
            finally { delivering = false; }
        }
    }

    public class MagicyteReward : IExposable
    {
        public ThingDef resource;
        public int remaining;
        public IntVec3 cell;
        public bool forbidden;
        public void ExposeData()
        {
            Scribe_Defs.Look(ref resource, "resource");
            Scribe_Values.Look(ref remaining, "remaining");
            Scribe_Values.Look(ref cell, "cell");
            Scribe_Values.Look(ref forbidden, "forbidden");
        }
    }
}
