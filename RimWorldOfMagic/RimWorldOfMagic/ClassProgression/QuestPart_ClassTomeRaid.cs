using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace TorannMagic.ClassProgression
{
    // The native quest owns a fixed generated group. No incident or replacement
    // generation is allowed here, including after failed/partial arrival.
    public sealed class QuestPart_ClassTomeRaid : QuestPartActivable, IThingHolder
    {
        public enum RaidPhase { Offered, Preparing, Active, RewardPending, Delivered, Failed, Cancelled }
        public const int PreparationTicks = 2500;
        public const int CombatTicks = 120000;
        private Pawn initiator;
        private string profileId;
        private ThingDef rewardDef;
        private MapParent mapParent;
        private List<Pawn> attackers = new List<Pawn>();
        private RaidPhase phase;
        private int arrivalTick = -1, deadlineTick = -1;
        private bool arrivalCommitted, combatResolved, rewardTransferred;
        internal bool ElementalRift;
        private Thing riftMarker;
        private ThingOwner<Thing> reward;

        public QuestPart_ClassTomeRaid() { reward = new ThingOwner<Thing>(this); }
        public Pawn Initiator => initiator;
        public string ProfileId => profileId;
        public MapParent TargetMapParent => mapParent;
        public RaidPhase Phase => phase;
        public bool IsOutstanding => BlocksMap || RewardPending;
        public bool BlocksMap => phase == RaidPhase.Offered || phase == RaidPhase.Preparing || phase == RaidPhase.Active;
        public bool RewardPending => phase == RaidPhase.RewardPending && !rewardTransferred;
        public IThingHolder ParentHolder => null;
        public ThingOwner GetDirectlyHeldThings() => reward;
        public void GetChildHolders(List<IThingHolder> children) => ThingOwnerUtility.AppendThingHoldersFromThings(children, reward);
        public override bool RequiresAccepter => true;
        public override string DescriptionPart => RewardPending ? "TM_ClassTomePendingDesc".Translate().ToString() : null;

        public void Configure(Pawn pawn, string profile, ThingDef tome, MapParent target, List<Pawn> group, string acceptSignal)
        {
            if (phase != RaidPhase.Offered || attackers.Count != 0) throw new InvalidOperationException("Tome raid already configured.");
            if (pawn == null || tome == null || target == null || group == null || group.Count == 0 ||
                group.Any(p => p == null || p.Spawned || p.Dead) || group.Distinct().Count() != group.Count)
                throw new ArgumentException("A tome raid requires a distinct, living, unspawned group and a fixed destination.");
            initiator = pawn; profileId = profile; rewardDef = tome; mapParent = target;
            attackers = new List<Pawn>(group); inSignalEnable = acceptSignal;
        }

        protected override void Enable(SignalArgs args)
        {
            if (phase != RaidPhase.Offered) return;
            phase = RaidPhase.Preparing;
            arrivalTick = Find.TickManager.TicksGame + PreparationTicks;
            base.Enable(args);
            if (ElementalRift && !OpenRift()) Fail();
        }

        public override void QuestPartTick()
        {
            int now = Find.TickManager.TicksGame;
            if (RewardPending) { if (now % 250 == 0) EnsureReward(); return; }
            if (phase == RaidPhase.Preparing && now >= arrivalTick) Arrive(now);
            if (phase != RaidPhase.Active || now % 250 != 0) return;
            // Map loss is checked before inspecting retreat/death, never inferred as victory.
            Map map = mapParent?.Map;
            if (map == null || now >= deadlineTick) { Fail(); return; }
            if (!arrivalCommitted || attackers.Count == 0 || attackers.Any(p => p == null)) { Fail(); return; }
            if (attackers.All(p => ThreatResolved(p, map))) EarnReward();
        }

        private static bool ThreatResolved(Pawn pawn, Map map)
        {
            return pawn.Dead || pawn.Downed || pawn.IsPrisonerOfColony || pawn.MapHeld != map || !pawn.HostileTo(Faction.OfPlayer);
        }

        private bool OpenRift()
        {
            Map map=mapParent?.Map;
            var def=DefDatabase<ThingDef>.GetNamedSilentFail(TomeElementalRift.Marker);
            if(map==null || def==null || !map.IsPlayerHome || attackers.Count==0) return false;
            var parms=new IncidentParms {target=map,faction=attackers[0].Faction};
            if (!PawnsArrivalModeDefOf.EdgeWalkIn.Worker.TryResolveRaidSpawnCenter(parms)) return false;
            var cells=TomeElementalRift.Cells(map,parms.spawnCenter,1);
            if (cells.Count==0) return false;
            try
            {
                riftMarker=ThingMaker.MakeThing(def);
                GenSpawn.Spawn(riftMarker,cells[0],map);
                return riftMarker.Spawned;
            }
            catch (Exception e) { Compatibility.TM_Log.Warning("Mastery rift placement interrupted: "+e); return false; }
        }

        private void CloseRift()
        {
            if (riftMarker != null && !riftMarker.Destroyed) riftMarker.Destroy(DestroyMode.Vanish);
            riftMarker=null;
        }

        private void Arrive(int now)
        {
            if (arrivalCommitted) { Fail(); return; }
            Map map = mapParent?.Map;
            if (map == null || !map.IsPlayerHome || attackers.Count == 0 ||
                attackers.Any(p => p == null || p.Dead || p.Spawned || p.Faction == null || !p.HostileTo(Faction.OfPlayer)))
            { Fail(); return; }
            var parms = new IncidentParms { target = map, faction = attackers[0].Faction, spawnCenter = IntVec3.Invalid,
                raidArrivalMode = ElementalRift ? null : PawnsArrivalModeDefOf.EdgeWalkIn };
            var worker = ElementalRift ? null : PawnsArrivalModeDefOf.EdgeWalkIn.Worker;
            List<IntVec3> cells = null;
            if (ElementalRift)
            {
                if (riftMarker?.Spawned != true || riftMarker.Map != map) { Fail(); return; }
                cells = TomeElementalRift.Cells(map,riftMarker.Position,attackers.Count);
                if(cells.Count != attackers.Count) { Fail(); return; }
            }
            else if (!worker.CanUseOnMap(map) || !worker.TryResolveRaidSpawnCenter(parms)) { Fail(); return; }
            // Commit before calling native arrival: callback exceptions must never spawn another group.
            arrivalCommitted = true;
            bool arrived = false;
            try
            {
                if (ElementalRift)
                {
                    for(int i=0;i<attackers.Count;i++)
                    {
                        if (!TomeElementalRift.Empty(map,cells[i])) break;
                        GenSpawn.Spawn(attackers[i],cells[i],map);
                    }
                    CloseRift();
                }
                else worker.Arrive(attackers, parms);
                arrived = attackers.All(p => p.Spawned && p.Map == map);
            }
            catch (Exception e) { Log.Error("[RWoM] Tome raid arrival failed: " + e); }
            // Even a partial arrival leaves actual hostile survivors with normal assault AI.
            try
            {
                var spawned = attackers.Where(p => p != null && p.Spawned && p.Map == map && p.GetLord() == null).ToList();
                if (spawned.Count != 0)
                    LordMaker.MakeNewLord(parms.faction, new LordJob_AssaultColony(parms.faction, canTimeoutOrFlee: true), map, spawned);
            }
            catch (Exception e) { arrived = false; Log.Error("[RWoM] Tome raid lord creation failed: " + e); }
            if (!arrived) { Fail(); return; }
            deadlineTick = now + CombatTicks;
            phase = RaidPhase.Active;
        }

        private void EarnReward()
        {
            if (combatResolved || phase != RaidPhase.Active) return;
            // Colony victory is durable even if item construction/notification fails.
            combatResolved = true;
            phase = RaidPhase.RewardPending;
            EnsureReward();
        }

        private void EnsureReward()
        {
            if (!RewardPending || reward.Count != 0) return;
            Thing book = null;
            try
            {
                book = ThingMaker.MakeThing(rewardDef);
                book.stackCount = 1;
                if (!reward.TryAdd(book, false)) throw new InvalidOperationException("Unable to hold earned mastery tome.");
            }
            catch (Exception e)
            {
                // NotifyAdded can throw after acquiring ownership. Keep that same item.
                if (book != null && !book.Destroyed && !book.Spawned && book.holdingOwner == null) book.Destroy();
                Log.Error("[RWoM] Mastery tome creation interrupted; earned claim retained: " + e);
            }
        }
        public bool Claim(Map destination)
        {
            if (!RewardPending || destination == null || !destination.IsPlayerHome) return false;
            EnsureReward();
            if (reward.Count != 1) return false;
            Thing book = reward[0];
            if (book.Destroyed) return false;
            IntVec3 cell = IntVec3.Invalid;
            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(DropCellFinder.TradeDropSpot(destination), 12f, true))
            {
                if (candidate.InBounds(destination) && candidate.Standable(destination) && !candidate.Fogged(destination) &&
                    candidate.GetThingList(destination).Count == 0 && destination.reachability.CanReachColony(candidate))
                { cell = candidate; break; }
            }
            if (!cell.IsValid) return false;
            if (!TransferReward(destination, cell)) return false;
            Messages.Message("TM_ClassTomeDelivered".Translate(), book, MessageTypeDefOf.PositiveEvent);
            return true;
        }

        private bool TransferReward(Map destination, IntVec3 cell)
        {
            if (!RewardPending || reward.Count != 1) return false;
            Thing book = reward[0];
            try
            {
                reward.Remove(book);
                GenSpawn.Spawn(book, cell, destination);
            }
            catch (Exception e) { Log.Error("[RWoM] Mastery tome placement interrupted: " + e); }
            // Actual ownership is authoritative, including a throw after native spawn.
            if (book.Spawned || (book.holdingOwner != null && book.holdingOwner != reward))
            {
                rewardTransferred = true;
                phase = RaidPhase.Delivered;
                DelayNextRequest();
                quest?.End(QuestEndOutcome.Success);
                return true;
            }
            if (!book.Destroyed && book.holdingOwner == null) reward.TryAdd(book, false);
            return false;
        }

        public override IEnumerable<Gizmo> ExtraGizmos(ISelectable target)
        {
            if (!RewardPending || !(target is Pawn pawn) || !pawn.IsColonistPlayerControlled) yield break;
            yield return new Command_Action
            {
                defaultLabel = "TM_ClassTomeClaim".Translate(),
                defaultDesc = "TM_ClassTomeClaimDesc".Translate(),
                action = () =>
                {
                    var options = Find.Maps.Where(m => m.IsPlayerHome).Select(m =>
                        new FloatMenuOption(m.Parent.LabelCap, () => Claim(m))).ToList();
                    if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
                }
            };
        }

        private void DelayNextRequest() => initiator?.TryGetComp<CompClassProgression>()?.DelayNextTomeQuest(Find.TickManager.TicksGame);
        private void Fail()
        {
            if (!BlocksMap) return;
            phase = RaidPhase.Failed;
            CloseRift();
            DelayNextRequest();
            quest?.End(QuestEndOutcome.Fail);
        }

        public override bool QuestPartReserves(Pawn pawn) => IsOutstanding && attackers.Contains(pawn);
        public override void Cleanup()
        {
            // Never erase spawned raiders, captured pawns, or an already earned book.
            if (BlocksMap) { phase = RaidPhase.Cancelled; DelayNextRequest(); }
            // WorldPawns owns generated unspawned bodies. Releasing the reservation lets
            // native world-pawn cleanup discard them without deleting a retreated survivor.
            CloseRift();
            if (!RewardPending) attackers.Clear();
            base.Cleanup();
        }

        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get { if (riftMarker?.Spawned == true) yield return riftMarker; else if (mapParent != null) yield return mapParent; }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref initiator, "initiator");
            Scribe_Values.Look(ref profileId, "profileId");
            Scribe_Defs.Look(ref rewardDef, "rewardDef");
            Scribe_References.Look(ref mapParent, "mapParent");
            Scribe_Collections.Look(ref attackers, "attackers", LookMode.Reference);
            Scribe_Values.Look(ref ElementalRift, "elementalRift");
            Scribe_References.Look(ref riftMarker, "masteryRiftMarker");
            Scribe_Values.Look(ref phase, "raidPhase", RaidPhase.Offered);
            Scribe_Values.Look(ref arrivalTick, "arrivalTick", -1);
            Scribe_Values.Look(ref deadlineTick, "deadlineTick", -1);
            Scribe_Values.Look(ref arrivalCommitted, "arrivalCommitted");
            Scribe_Values.Look(ref combatResolved, "combatResolved");
            Scribe_Values.Look(ref rewardTransferred, "rewardTransferred");
            Scribe_Deep.Look(ref reward, "reward", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (attackers == null) attackers = new List<Pawn>();
                if (reward == null) reward = new ThingOwner<Thing>(this);
            }
        }
    }
}






