using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using TorannMagic.ClassProgression;
using TorannMagic.Odyssey;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Verb_FoldReality : Verb_SpiritTargetable
    {
        private static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("Other/gatewayring", true);
        private const int MaxLaunchDistance = 999;
        private Map origin;
        private bool selecting;
        private int generation;

        protected override bool TryCastShot() => ApplyHostedEffect();

        protected override bool ApplyHostedEffect()
        {
            generation++;
            origin = CasterPawn?.Map;
            selecting = true;
            if (!Ready()) { selecting = false; return false; }
            ManualTravelSafety.BeginAttempt(Ability);
            StartChoosingDestination();
            return true;
        }

        private bool Ready()
        {
            if (!selecting || CasterPawn?.Spawned != true || CasterPawn.Map != origin || CasterPawn.Downed ||
                CasterPawn.InMentalState || !Find.Maps.Contains(origin) || !(Ability is MagicAbility power)) return false;
            if (power.Pawn != CasterPawn)
                return ReferenceEquals(SpiritHostedBolt.Find(CasterPawn, power.Pawn, power.magicDef), power) && SpiritHostedBolt.Ready(CasterPawn, power);
            return !CasterPawn.Dead && power.CooldownTicksLeft <= 0 &&
                power.MagicUser.AbilityData.AllPowers.Contains(power) && power.MagicUser.Mana != null &&
                power.MagicUser.Mana.CurLevel >= power.MagicUser.ActualManaCost(power.magicDef);
        }

        private void StartChoosingDestination()
        {
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(origin.info.parent));
            Find.WorldSelector.ClearSelection();
            var tile = origin.Tile;
            int request = generation;
            Find.WorldTargeter.BeginTargeting(target => request == generation && Find.WorldTargeter.IsTargeting && ChoseWorldTarget(target), true, TargeterMouseAttachment, true,
                () => GenDraw.DrawWorldRadiusRing(tile, MaxLaunchDistance),
                target => target.IsValid && !InRange(target) ? "TM_GatewayBeyondMaximumRange".Translate().ToString() : null);
        }

        private bool InRange(GlobalTargetInfo target)
        {
            if (!target.IsValid || target.Tile.Layer != origin.Tile.Layer) return false;
            int distance = Find.WorldGrid.TraversalDistanceBetween(origin.Tile, target.Tile);
            return distance >= 0 && distance <= MaxLaunchDistance;
        }

        private bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            if (!Ready()) return false;
            if (!InRange(target))
            {
                Messages.Message("TM_GatewayBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            if (!(target.WorldObject is MapParent parent) || !parent.HasMap)
            {
                Messages.Message("TM_InvalidGatewayNotOccupied".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            var map = parent.Map;
            Current.Game.CurrentMap = map;
            var portal = new TargetingParameters {
                canTargetLocations = true, canTargetSelf = false, canTargetPawns = false,
                canTargetBuildings = false, canTargetItems = false,
                validator = info => Ready() && ValidCell(map, info.Cell)
            };
            int request = generation;
            bool picked = false;
            Find.Targeter.BeginTargeting(portal, cell => {
                if (picked || request != generation) return;
                picked = true;
                InitiateMassSummon(map, cell);
            }, null, () => {
                if (request != generation) return;
                if (!picked) selecting = false;
                if (Find.Maps.Contains(origin)) Current.Game.CurrentMap = origin;
            }, TargeterMouseAttachment);
            return true;
        }

        private static bool ValidCell(Map map, IntVec3 cell) => map != null && Find.Maps.Contains(map) &&
            cell.InBounds(map) && !cell.Fogged(map) && cell.Walkable(map);

        public void InitiateMassSummon(Map map, LocalTargetInfo target)
        {
            if (!Ready() || !target.IsValid || !ValidCell(map, target.Cell)) return;
            var body = CasterPawn;
            var destination = body.Position;
            var power = (MagicAbility)Ability;
            int radius = Mathf.RoundToInt(7 * power.MagicUser.arcaneDmg);
            // Materialize before moving: nearby placement may put an item back inside the source area.
            var cells = GenRadial.RadialCellsAround(target.Cell, radius, true).Where(c => c.InBounds(map)).ToArray();
            var things = cells.SelectMany(c => c.GetThingList(map)).Where(t => t != body && t.def.mote == null &&
                (t is Pawn || t.def.EverHaulable) && (map != origin || t.Position != destination)).Distinct().ToList();
            if (things.Count == 0) return;
            var warnings = new List<string>();
            var acceptedHazards = new Dictionary<Pawn,TravelEnvironmentResult>();
            foreach (var pawn in things.OfType<Pawn>())
            {
                var result = TravelEnvironmentSafety.Evaluate(pawn, origin, destination);
                if ((result.Hazards & TravelEnvironmentHazard.InvalidDestination) != 0) return;
                if (!result.IsSafe) warnings.Add(ManualTravelSafety.Warning(pawn, result));
                acceptedHazards[pawn] = result;
            }
            int request = generation;
            void Commit()
            {
                if (request != generation || !Ready() || body.Position != destination || !ValidCell(map,target.Cell)) return;
                selecting = false;
                bool moved = false;
                try
                {
                    foreach (var thing in things)
                    {
                        if (!thing.Spawned || thing.Map != map || !thing.Position.InHorDistOf(target.Cell,radius)) continue;
                        var from = thing.Position;
                        if (thing is Pawn pawn)
                        {
                            var risk = TravelEnvironmentSafety.Evaluate(pawn, origin, destination);
                            var accepted = acceptedHazards[pawn];
                            if ((risk.Hazards & ~accepted.Hazards) != 0 || (risk.ShellExpiring && !accepted.ShellExpiring)) continue;
                            if (origin == map)
                            {
                                if (Compatibility.TM_Teleport.Move(pawn,destination)) moved = true;
                                continue;
                            }
                        }
                        int originalCount = thing.stackCount;
                        FleckMaker.ThrowLightningGlow(thing.DrawPos,map,thing is Pawn ? 1f : .6f);
                        FleckMaker.ThrowHeatGlow(from,map,1f);
                        try
                        {
                            thing.DeSpawn();
                            if (thing is Pawn) GenSpawn.Spawn(thing,destination,origin);
                            else GenPlace.TryPlaceThing(thing,destination,origin,ThingPlaceMode.Near);
                        }
                        finally
                        {
                            if (thing.Destroyed || thing.stackCount < originalCount || (thing.Spawned && (thing.Map != map || thing.Position != from))) moved = true;
                            if (!thing.Destroyed && !thing.Spawned && thing.holdingOwner == null) GenSpawn.Spawn(thing,from,map);
                        }
                    }
                    foreach (var cell in cells) FleckMaker.ThrowSmoke(cell.ToVector3Shifted(),map,.6f);
                }
                finally
                {
                    if (moved) ManualTravelSafety.SettleHosted(power,body);
                }
            }
            if (warnings.Count == 0) Commit();
            else ManualTravelSafety.Confirm(string.Join("\n",warnings), Commit);
        }
    }
}
