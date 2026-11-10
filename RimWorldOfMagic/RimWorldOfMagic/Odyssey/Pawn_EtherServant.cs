using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic.Odyssey
{
    public enum EtherServantEndReason { Active, Expired, Killed, Cancelled }

    public class Pawn_EtherServant : Pawn
    {
        private bool workSkillsInitialized;
        private long expiresAtTick = -1;
        private int summonedAtTick = -1;
        public int SummonedAtTick => summonedAtTick;
        private EtherServantEndReason endReason;
        private bool lootCreated, ending;
        public long ExpiresAtTick { get => expiresAtTick; set => expiresAtTick = value; }
        public EtherServantEndReason EndReason => endReason;
        public void StartLifetime(int now)
        {
            if (expiresAtTick >= 0) return;
            summonedAtTick = now;
            expiresAtTick = (long)now + CompEtherGate.DurationTicks;
            Current.Game?.GetComponent<GameComponent_EtherServants>()?.Register(this);
        }
        public void ExposeLifetimeData()
        {
            Scribe_Values.Look(ref summonedAtTick, "TM_etherSummonedAtTick", -1);
            Scribe_Values.Look(ref expiresAtTick, "TM_etherExpiresAtTick", -1L);
            Scribe_Values.Look(ref endReason, "TM_etherEndReason", EtherServantEndReason.Active);
            Scribe_Values.Look(ref lootCreated, "TM_etherLootCreated", false);
        }
        public void ExposeWorkData() => Scribe_Values.Look(ref workSkillsInitialized, "TM_etherWorkSkillsInitialized", false);
        public override void ExposeData() { base.ExposeData(); ExposeLifetimeData(); ExposeWorkData(); }
        public void TickLifetime(int now)
        {
            if (Destroyed) return;
            if (endReason != EtherServantEndReason.Active) TryEnd(endReason);
            else if (expiresAtTick >= 0 && now >= expiresAtTick) TryEnd(EtherServantEndReason.Expired);
        }
        protected override void Tick()
        {
            TickLifetime(Find.TickManager.TicksGame);
            if (!Destroyed && endReason == EtherServantEndReason.Active) base.Tick();
        }
        protected override void TickInterval(int delta)
        {
            TickLifetime(Find.TickManager.TicksGame);
            if (!Destroyed && endReason == EtherServantEndReason.Active) base.TickInterval(delta);
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            EnsureWorkSkills();
            Current.Game?.GetComponent<GameComponent_EtherServants>()?.Register(this);
            TickLifetime(Find.TickManager.TicksGame);
        }
        public void EnsureWorkSkills()
        {
            // Native skill learning/decay reads story.traits even for a nonhuman worker.
            if (story == null) story = new Pawn_StoryTracker(this);
            if (skills == null) skills = new Pawn_SkillTracker(this);
            if (!workSkillsInitialized)
            {
                skills.GetSkill(SkillDefOf.Mining).Level = 10;
                skills.GetSkill(SkillDefOf.Construction).Level = 5;
            }
            if (workSettings == null) workSettings = new Pawn_WorkSettings(this);
            if (!workSkillsInitialized || !workSettings.Initialized)
            {
                workSettings.EnableAndInitializeIfNotAlreadyInitialized();
                workSettings.DisableAll();
                string[] work = { "Construction", "Mining", "Hauling" };
                for (int i = 0; i < work.Length; i++)
                {
                    var type = DefDatabase<WorkTypeDef>.GetNamedSilentFail(work[i]);
                    if (type != null && !WorkTypeIsDisabled(type)) workSettings.SetPriority(type, i + 1);
                }
            }
            workSkillsInitialized = true;
        }
        public override Vector3 DrawPos => base.DrawPos + new Vector3(0f, 0f, .08f * Mathf.Sin((Find.TickManager.TicksGame + thingIDNumber * 17) * .045f)
            + (summonedAtTick >= 0 ? .25f * Mathf.Clamp01(1f - (Find.TickManager.TicksGame - summonedAtTick) / 180f) : 0f));
        public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null) => TryEnd(EtherServantEndReason.Killed);
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish) => TryEnd(mode == DestroyMode.KillFinalize ? EtherServantEndReason.Killed : EtherServantEndReason.Cancelled);

        public void CancelUnspawned()
        {
            endReason = EtherServantEndReason.Cancelled;
            if (!Spawned && ParentHolder == null && carryTracker?.CarriedThing == null && (inventory?.innerContainer.Count ?? 0) == 0
                && ClassProgression.SpiritSummonedHost.CanDismiss(this))
                base.Destroy(DestroyMode.Vanish);
        }

        private bool HasCargo => carryTracker?.CarriedThing != null || (inventory?.innerContainer.Count ?? 0) != 0;
        public bool TryTransferCargoTo(ThingOwner receiver)
        {
            if (receiver == null || receiver == carryTracker?.innerContainer || receiver == inventory?.innerContainer) return false;
            carryTracker?.innerContainer.TryTransferAllToContainer(receiver, false);
            inventory?.innerContainer.TryTransferAllToContainer(receiver, false);
            return !HasCargo;
        }
        private bool TryEndOffMap()
        {
            if (Current.Game == null) return false;
            ending = true;
            try
            {
                var caravan = ParentHolder as Caravan;
                if (caravan != null)
                {
                    var receivers = caravan.PawnsListForReading.Where(p => p != this && !p.Dead && !p.Destroyed && !(p is Pawn_EtherServant)
                        && p.inventory != null).ToList();
                    if (endReason == EtherServantEndReason.Killed && !lootCreated)
                    {
                        if (receivers.Count == 0 || inventory == null) return false;
                        var loot = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawMagicyte"));
                        loot.stackCount = 5;
                        if (!inventory.innerContainer.TryAdd(loot, false)) { loot.Destroy(); return false; }
                        lootCreated = true;
                    }
                    foreach (var receiver in receivers) if (TryTransferCargoTo(receiver.inventory.innerContainer)) break;
                    if (HasCargo) return false;
                    caravan.RemovePawn(this);
                    base.Destroy(DestroyMode.Vanish);
                    if (caravan.PawnsListForReading.Count == 0 && !caravan.Destroyed) caravan.Destroy();
                    return Destroyed;
                }
                // A shuttle, container or isolated world pawn keeps its cargo until there is
                // a safe recipient. Empty detached expired/cancelled servants can be removed.
                if (ParentHolder == null && !HasCargo && endReason != EtherServantEndReason.Killed && Find.WorldPawns != null)
                {
                    base.Destroy(DestroyMode.Vanish);
                    return Destroyed;
                }
                return false;
            }
            finally { ending = false; }
        }
        public bool TryEnd(EtherServantEndReason reason)
        {
            if (Destroyed) return true;
            if (reason == EtherServantEndReason.Active || ending) return false;
            if (endReason == EtherServantEndReason.Active) endReason = reason;
            if (!ClassProgression.SpiritSummonedHost.CanDismiss(this)) return false;
            // A transport or world owner remains durable. Defer disposal until a map can
            // receive every stack; never destroy an off-map cargo owner.
            if (!Spawned || Map == null) return TryEndOffMap();
            ending = true;
            try
            {
                if (endReason == EtherServantEndReason.Killed && !lootCreated)
                {
                    if (inventory == null) return false;
                    var loot = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawMagicyte"));
                    loot.stackCount = 5;
                    if (!inventory.innerContainer.TryAdd(loot, false)) { loot.Destroy(); return false; }
                    lootCreated = true;
                }
                if (carryTracker?.CarriedThing != null)
                    carryTracker.innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
                inventory?.innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
                if (carryTracker?.CarriedThing != null || (inventory?.innerContainer.Count ?? 0) != 0) return false;
                base.Destroy(DestroyMode.Vanish);
                return Destroyed;
            }
            finally { ending = false; }
        }
        public override string GetInspectString()
        {
            var remaining = Math.Max(0L, expiresAtTick - Find.TickManager.TicksGame);
            return base.GetInspectString() + "\n" + "TM_EtherServantRemaining".Translate(((int)Math.Min(int.MaxValue, remaining)).ToStringTicksToPeriod());
        }
    }

    public class JobGiver_EtherHaul : ThinkNode_JobGiver
    {
        public static IEnumerable<WorkGiver_Scanner> WorkGivers()
        {
            // Exact core workers: construction preparation/finishing, designated mining,
            // then hauling. No repair, deconstruction or combat work is introduced.
            foreach (var name in new[] { "ConstructFinishFrames", "ConstructDeliverResourcesToFrames", "ConstructDeliverResourcesToBlueprints", "Mine", "HaulGeneral" })
                if (DefDatabase<WorkGiverDef>.GetNamedSilentFail(name)?.Worker is WorkGiver_Scanner scanner) yield return scanner;
        }
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!(pawn is Pawn_EtherServant servant) || servant.EndReason != EtherServantEndReason.Active
                || !pawn.Spawned || pawn.Faction != Faction.OfPlayer) return null;
            if (servant.SummonedAtTick >= 0 && (long)Find.TickManager.TicksGame - servant.SummonedAtTick < 180)
                { var wait = JobMaker.MakeJob(JobDefOf.Wait); wait.expiryInterval = 30; return wait; }
            servant.EnsureWorkSkills();
            foreach (var giver in WorkGivers())
            {
                if (pawn.WorkTypeIsDisabled(giver.def.workType) || !pawn.workSettings.WorkIsActive(giver.def.workType)
                    || giver.MissingRequiredCapacity(pawn) != null || giver.ShouldSkip(pawn)) continue;
                var candidates = giver.PotentialWorkThingsGlobal(pawn) ?? pawn.Map.listerThings.ThingsMatching(giver.PotentialWorkThingRequest);
                foreach (var thing in candidates.OrderBy(t => t.Position.DistanceToSquared(pawn.Position)))
                {
                    if (thing.IsForbidden(pawn) || !pawn.CanReserveAndReach(thing, giver.PathEndMode, giver.MaxPathDanger(pawn))) continue;
                    if (giver is WorkGiver_HaulGeneral && !HaulAIUtility.PawnCanAutomaticallyHaul(pawn, thing, false)) continue;
                    if (!giver.HasJobOnThing(pawn, thing)) continue;
                    var job = giver.JobOnThing(pawn, thing);
                    if (job == null) continue;
                    job.workGiverDef = giver.def;
                    return job;
                }
            }
            return null;
        }
    }
}









