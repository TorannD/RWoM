using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using TorannMagic.ClassProgression;
using TorannMagic.Odyssey;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Verb_LightSkipGlobal : Verb_SpiritTargetable
    {
        private const int MaxLaunchDistance = 300;
        private static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("Other/PortalBldg", true);
        private static readonly AccessTools.FieldRef<WorldTargeter, Func<GlobalTargetInfo, bool>> CurrentWorldAction =
            AccessTools.FieldRefAccess<WorldTargeter, Func<GlobalTargetInfo, bool>>("action");
        private static readonly AccessTools.FieldRef<Targeter, Action<LocalTargetInfo>> CurrentCellAction =
            AccessTools.FieldRefAccess<Targeter, Action<LocalTargetInfo>>("action");
        private Func<GlobalTargetInfo,bool> worldAction;
        private Map origin;
        private IntVec3 start;
        private int generation;
        private bool selecting;
        private List<Pawn> boarding;
        private readonly List<CompTransporter> pods = new List<CompTransporter>();

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target) =>
            CasterPawn?.Spawned == true && target.Pawn == CasterPawn && !CasterPawn.Position.Roofed(CasterPawn.Map);

        protected override bool TryCastShot() => ApplyHostedEffect();

        protected override bool ApplyHostedEffect()
        {
            generation++;
            origin = CasterPawn?.Map;
            start = CasterPawn?.Position ?? IntVec3.Invalid;
            selecting = true;
            if (!Ready()) { selecting = false; return false; }
            ManualTravelSafety.BeginAttempt(Ability);
            StartChoosingDestination();
            return true;
        }

        private bool Ready()
        {
            var body = CasterPawn;
            if (!selecting || body?.Spawned != true || body.Dead || body.Downed || body.InMentalState ||
                body.Map != origin || body.Position != start || start.Roofed(origin) || !(Ability is MagicAbility power)) return false;
            if (power.Pawn != body) return ReferenceEquals(SpiritHostedBolt.Find(body,power.Pawn,power.magicDef),power)
                && SpiritHostedBolt.Ready(body,power);
            return power.MagicUser.AbilityData.AllPowers.Contains(power) && power.CooldownTicksLeft <= 0
                && power.MagicUser.Mana != null && power.MagicUser.Mana.CurLevel >= power.MagicUser.ActualManaCost(power.magicDef);
        }

        private bool InRange(PlanetTile tile)
        {
            if (!tile.Valid || tile.Layer != origin.Tile.Layer) return false;
            int distance = Find.WorldGrid.TraversalDistanceBetween(origin.Tile,tile);
            return distance >= 0 && distance <= MaxLaunchDistance;
        }

        private bool OwnsWorldSelection(int request) => request == generation && Ready() &&
            ReferenceEquals(CurrentWorldAction(Find.WorldTargeter),worldAction);

        private void StartChoosingDestination()
        {
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(origin.Parent));
            Find.WorldSelector.ClearSelection();
            int request = generation;
            worldAction = target => OwnsWorldSelection(request) && ChooseWorldTarget(target);
            Find.WorldTargeter.BeginTargeting(worldAction,true,TargeterMouseAttachment,true,
                () => GenDraw.DrawWorldRadiusRing(origin.Tile,MaxLaunchDistance),
                target => target.IsValid && !InRange(target.Tile) ? "TM_UnableToTravel".Translate() : (TaggedString)null);
        }

        private bool ChooseWorldTarget(GlobalTargetInfo target)
        {
            if (!target.IsValid || !InRange(target.Tile))
            {
                Messages.Message("TM_UnableToTravel".Translate(),MessageTypeDefOf.RejectInput,false);
                return false;
            }
            var options = GetTransportPodsFloatMenuOptionsAt(target.Tile,target).ToList();
            if (options.Count == 1) options[0].action();
            else if (options.Count > 1) Find.WindowStack.Add(new FloatMenu(options));
            else Messages.Message("TM_UnableToTravel".Translate(),MessageTypeDefOf.RejectInput,false);
            return false;
        }

        public IEnumerable<FloatMenuOption> GetTransportPodsFloatMenuOptionsAt(int tile, GlobalTargetInfo target)
        {
            int request = generation;
            if (target.WorldObject is MapParent parent && parent.HasMap)
                yield return new FloatMenuOption("TM_SelectTargetOnMap".Translate(parent.LabelCap), () =>
                {
                    if (!OwnsWorldSelection(request) || !parent.HasMap || parent.Tile != target.Tile) return;
                    Find.WorldTargeter.StopTargeting();
                    Map map = parent.Map;
                    Current.Game.CurrentMap = map;
                    bool picked = false;
                    Action<LocalTargetInfo> choose = null;
                    bool Valid(LocalTargetInfo t) => !picked && generation == request && Ready() &&
                        ReferenceEquals(CurrentCellAction(Find.Targeter),choose) && Current.Game.CurrentMap == map &&
                        parent.Map == map && t.IsValid && ValidCell(map,t.Cell) && (map != origin || t.Cell != start);
                    choose = t =>
                    {
                        if (!Valid(t)) return;
                        picked = true;
                        RequestDeparture(target.Tile,map,t.Cell,null);
                    };
                    Find.Targeter.BeginTargeting(TargetingParameters.ForDropPodsDestination(), choose, highlightAction: null, targetValidator: Valid, actionWhenFinished: () =>
                    {
                        if (!picked && request == generation) selecting = false;
                        if (Find.Maps.Contains(origin)) Current.Game.CurrentMap = origin;
                    });
                });
            if (!Find.World.Impassable(target.Tile))
                yield return new FloatMenuOption("FormCaravanHere".Translate(), () =>
                {
                    if (!OwnsWorldSelection(request)) return;
                    Find.WorldTargeter.StopTargeting();
                    RequestDeparture(target.Tile,null,IntVec3.Invalid,new TransportersArrivalAction_FormCaravan());
                });
        }

        private static bool ValidCell(Map map, IntVec3 c) => map != null && Find.Maps.Contains(map) &&
            c.InBounds(map) && c.Walkable(map) && !c.Fogged(map) && !c.Roofed(map);

        private bool Eligible(Pawn pawn) => pawn?.Spawned == true && !pawn.Dead && pawn.Map == origin &&
            pawn.Faction == CasterPawn.Faction && pawn.Position.InHorDistOf(start,5f) && !pawn.Position.Roofed(origin) &&
            !(ModCheck.Validate.GiddyUp.Core_IsInitialized() && ModCheck.GiddyUp.IsMount(pawn));

        public void LaunchLightPod(int destinationTile, IntVec3 destinationCell, TransportersArrivalAction action)
        {
            if (!Ready()) return;
            PlanetTile tile = new PlanetTile(destinationTile,origin.Tile.Layer);
            var map = Find.Maps.Find(m=>m.Tile==tile);
            RequestDeparture(tile,map,destinationCell,action);
        }

        private void RequestDeparture(PlanetTile tile, Map map, IntVec3 cell, TransportersArrivalAction action)
        {
            if (!Ready() || !InRange(tile) || (action == null ? !ValidCell(map,cell) || map == origin && cell == start : Find.World.Impassable(tile))) return;
            int request = ++generation;
            var group = TM_Calc.FindAllPawnsAround(origin,start,5f,CasterPawn.Faction,true).Where(Eligible).Distinct().ToList();
            if (!group.Contains(CasterPawn)) return;
            var risks = new Dictionary<Pawn,TravelEnvironmentResult>();
            var warnings = new List<string>();
            if (action == null)
                foreach (var p in group)
                {
                    var risk = TravelEnvironmentSafety.Evaluate(p,map,cell);
                    if ((risk.Hazards & TravelEnvironmentHazard.InvalidDestination) != 0) return;
                    risks[p] = risk;
                    if (!risk.IsSafe) warnings.Add(ManualTravelSafety.Warning(p,risk));
                }
            void Commit()
            {
                if (request != generation || !Ready() || !InRange(tile) ||
                    (action == null ? !ValidCell(map,cell) || map.Tile != tile : Find.World.Impassable(tile))) return;
                boarding = group.Where(Eligible).ToList();
                if (!boarding.Contains(CasterPawn)) return;
                if (action == null && boarding.Any(p => {
                    var risk = TravelEnvironmentSafety.Evaluate(p,map,cell);
                    return (risk.Hazards & ~risks[p].Hazards) != 0 || risk.ShellExpiring && !risks[p].ShellExpiring;
                })) return;
                selecting = false;
                var body = CasterPawn;
                bool drafted = body.Drafted;
                bool sent = false;
                try
                {
                    CreatePodGroup();
                    foreach (var pod in pods)
                    {
                        if (Verb_LightSkip.DispatchPod(pod,tile,cell,drafted,action)) sent = true;
                        else if (!sent) break;
                    }
                }
                finally
                {
                    boarding = null;
                    foreach (var pod in pods.Where(p=>p.parent.Spawned))
                    {
                        foreach (var passenger in pod.innerContainer.OfType<Pawn>()) passenger.teleporting = false;
                        pod.innerContainer.TryDropAll(pod.parent.Position,origin,ThingPlaceMode.Near);
                        if (pod.innerContainer.Count == 0) pod.parent.Destroy();
                    }
                    pods.Clear();
                    if (sent)
                    {
                        // The off-map cooldown still ticks, but its completed selection verb is not a saved object.
                        if (body.stances?.curStance is Stance_Cooldown cooldown && ReferenceEquals(cooldown.verb, this))
                            cooldown.verb = null;
                        ManualTravelSafety.SettleHosted((MagicAbility)Ability,body);
                    }
                }
                if (sent) CheckAbandoned(origin);
            }
            if (warnings.Count == 0) Commit();
            else ManualTravelSafety.Confirm(string.Join("\n",warnings),Commit);
        }

        public void CreatePodGroup()
        {
            if (boarding == null) return;
            int id = Find.UniqueIDsManager.GetNextTransporterGroupID();
            var leader = CasterPawn;
            foreach (var pawn in boarding.OrderBy(p => p != leader))
            {
                if (pawn.carryTracker?.CarriedThing != null)
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position,ThingPlaceMode.Near,out _);
                var pod = Verb_LightSkip.PreparePod(pawn);
                if (pod == null)
                {
                    if (pawn == leader) break;
                    continue;
                }
                pod.groupID = id;
                pods.Add(pod);
            }
        }

        public IntVec3 GetRelativePositionOffset(IntVec3 targetCell, IntVec3 relativePosition, IntVec3 offsetPosition)
        {
            var cell = targetCell + (offsetPosition-relativePosition);
            if (ValidCell(origin,cell)) return cell;
            return GetUnroofedCellsAround(targetCell,5f).TryRandomElement(out var near) ? near : IntVec3.Invalid;
        }

        public List<IntVec3> GetUnroofedCellsAround(IntVec3 center,float radius) =>
            GenRadial.RadialCellsAround(center,radius,true).Where(c=>ValidCell(origin,c)).ToList();

        private static void CheckAbandoned(Map map)
        {
            if (!map.mapPawns.AnyColonistSpawned && !map.IsPlayerHome)
            {
                // Only settlement sites can be abandoned — on encounter/quest maps
                // map.Parent is not a Settlement and the cleanup comp would NRE.
                Settlement sm = map.Parent as Settlement;
                if (sm != null)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append("TM_AbandoningMap".Translate(map.Parent.LabelCap));
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(stringBuilder.ToString(), delegate
                    {
                        WorldTransport.TM_DelayedDestroyMap ddm = new WorldTransport.TM_DelayedDestroyMap();
                        ddm.parent = sm;
                        ddm.delayTicks = 120;
                        sm.AllComps.Add(ddm);
                    }));
                }
            }
        }

    }
}
