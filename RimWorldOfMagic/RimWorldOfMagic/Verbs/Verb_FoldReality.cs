using RimWorld.Planet;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Verse;
using AbilityUser;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Verb_FoldReality : Verb_UseAbility
    {
        private static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("Other/gatewayring", true);

        int MaxLaunchDistance = 999;
        CompAbilityUserMagic comp;

        protected override bool TryCastShot()
        {
            Map map = base.CasterPawn.Map;
            comp = this.CasterPawn.GetCompAbilityUserMagic();
            StartChoosingDestination();
            return false;
        }

        private void StartChoosingDestination()
        {
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(base.CasterPawn.Map.info.parent));
            Find.WorldSelector.ClearSelection();
            int tile = base.CasterPawn.Map.Tile;
            Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChoseWorldTarget), true, Verb_FoldReality.TargeterMouseAttachment, true, delegate
            {
                GenDraw.DrawWorldRadiusRing(tile, this.MaxLaunchDistance);  //center, max launch distance
            }, delegate (GlobalTargetInfo target)
            {
                if (!target.IsValid)
                {
                    return null;
                }
                int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
                if (num <= this.MaxLaunchDistance) // <= max launch distance
                {
                    return null;
                }
                if (num > this.MaxLaunchDistance) //this.MaxLaunchDistanceEverPossible
                {
                    return "TM_GatewayBeyondMaximumRange".Translate();
                }
                return "TM_InvalidGatewaySelection".Translate();
            });
        }

        private bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            if (!target.IsValid)
            {
                Messages.Message("TM_InvalidGatewaySelection".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            int num = Find.WorldGrid.TraversalDistanceBetween(base.CasterPawn.Map.Tile, target.Tile);
            if (num > this.MaxLaunchDistance)
            {
                Messages.Message("TM_GatewayBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            MapParent mapParent = target.WorldObject as MapParent;
            if (mapParent != null && mapParent.HasMap)
            {
                Map myMap = base.CasterPawn.Map;
                Map map = mapParent.Map;
                Current.Game.CurrentMap = map;
                TargetingParameters portalTarget = new TargetingParameters();
                portalTarget.canTargetLocations = true;
                portalTarget.canTargetSelf = false;
                portalTarget.canTargetPawns = false;
                portalTarget.canTargetFires = false;
                portalTarget.canTargetBuildings = false;
                portalTarget.canTargetItems = false;
                portalTarget.validator = ((TargetInfo x) => x.IsValid && !x.Cell.Fogged(map) && x.Cell.InBoundsWithNullCheck(map) && x.Cell.Walkable(map));
                Find.Targeter.BeginTargeting(portalTarget, delegate (LocalTargetInfo x)
                {

                    InitiateMassSummon(map, x);
                }, null, delegate
                {
                    if (Find.Maps.Contains(myMap))
                    {
                        Current.Game.CurrentMap = myMap;
                    }
                }, Verb_FoldReality.TargeterMouseAttachment);
                return true;
            }
            bool flag;
            if (mapParent != null)
            {
                Settlement settlement = mapParent as Settlement;
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                if (settlement != null && settlement.Visitable)
                {
                    Messages.Message("TM_InvalidGatewayNotOccupied".Translate(), MessageTypeDefOf.RejectInput);
                    return false;
                }
                //if (mapParent.TransportPodsCanLandAndGenerateMap)
                //{
                //    Messages.Message("TM_InvalidGatewayNotOccupied".Translate(), MessageTypeDefOf.RejectInput);
                //    return false;
                //}
                if (list.Any<FloatMenuOption>())
                {
                    Find.WorldTargeter.closeWorldTabWhenFinished = false;
                    Find.WindowStack.Add(new FloatMenu(list));
                    return true;
                }
                flag = true;
            }
            else
            {
                flag = true;
            }
            if (!flag)
            {
                return false;
            }
            if (Find.World.Impassable(target.Tile))
            {
                Messages.Message("TM_InvalidGatewaySelection".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            comp.Mana.CurLevel += comp.ActualManaCost(TorannMagicDefOf.TM_FoldReality);
            Messages.Message("TM_InvalidGatewayNotOccupied".Translate(), MessageTypeDefOf.RejectInput);
            return false;
        }

        public void InitiateMassSummon(Map map, LocalTargetInfo target)
        {
            IntVec3 curCell;
            IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(target.Cell, Mathf.RoundToInt(7*comp.arcaneDmg), true);
            int transportedItemCount = 0;
            for (int i = 0; i < targets.Count(); i++)
            {
                curCell = targets.ToArray<IntVec3>()[i];                
                if (curCell.InBoundsWithNullCheck(map) && curCell.IsValid)
                {
                    List<Thing> thingList = curCell.GetThingList(map);
                    for(int j = 0; j < thingList.Count(); j++)
                    {
                        if(thingList[j] != null && thingList[j].def.mote == null)
                        {
                            Thing targetThing = thingList[j];
                            if (targetThing != this.CasterPawn)
                            {
                                if (targetThing is Pawn)
                                {
                                    FleckMaker.ThrowLightningGlow(targetThing.DrawPos, targetThing.Map, 1f);
                                    FleckMaker.ThrowHeatGlow(targetThing.Position, targetThing.Map, 1f);
                                    targetThing.DeSpawn();
                                    GenSpawn.Spawn(targetThing, this.CasterPawn.Position, this.CasterPawn.Map);
                                    transportedItemCount++;
                                    j--;
                                }
                                else if (targetThing != null && targetThing.def.EverHaulable)
                                {
                                    FleckMaker.ThrowLightningGlow(targetThing.DrawPos, targetThing.Map, .6f);
                                    FleckMaker.ThrowHeatGlow(targetThing.Position, targetThing.Map, 1f);
                                    targetThing.DeSpawn();
                                    GenPlace.TryPlaceThing(targetThing, this.CasterPawn.Position, this.CasterPawn.Map, ThingPlaceMode.Near, null);
                                    transportedItemCount++;
                                    j--;
                                }
                            }
                        }                        
                    }
                    FleckMaker.ThrowSmoke(curCell.ToVector3Shifted(), map, .6f);                   
                }
            }
        }
    }
}
