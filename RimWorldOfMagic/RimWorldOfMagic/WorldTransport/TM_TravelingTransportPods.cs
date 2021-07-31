using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using HarmonyLib;

namespace TorannMagic.WorldTransport
{
    public class TM_TravelingTransportPods : TravelingTransportPods
    {
        public IntVec3 destinationCell = default(IntVec3);
        public bool draftFlag = false;

        private List<ActiveDropPodInfo> pods = new List<ActiveDropPodInfo>();
        private bool arrived;
        private float traveledPct;
        public float TravelSpeed = 0.00025f;
        private bool initialized = false;

        private float TraveledPctStepPerTick
        {
            get
            {
                Vector3 start = Find.WorldGrid.GetTileCenter(Traverse.Create(root: this).Field(name: "initialTile").GetValue<int>());
                Vector3 end = Find.WorldGrid.GetTileCenter(Traverse.Create(root: this).Field(name: "destinationTile").GetValue<int>());
                if (start == end)
                {
                    return 1f;
                }
                float num = GenMath.SphericalDistance(start.normalized, end.normalized);
                if (num == 0f)
                {
                    return 1f;
                }
                return TravelSpeed / num;
            }
        }

        public override void Tick()
        {
            for (int i = 0; i < this.AllComps.Count; i++)
            {
                AllComps[i].CompTick();
            }            

            traveledPct += TraveledPctStepPerTick;
            if (traveledPct >= 1f)
            {
                traveledPct = 1f;
                Arrived();
            }
        }

        private void Arrived()
        {
            if (!arrived)
            {
                arrived = true;
                if (arrivalAction == null || !(bool)arrivalAction.StillValid(pods.Cast<IThingHolder>(), destinationTile))
                {
                    arrivalAction = null;
                    List<Map> maps = Find.Maps;
                    for (int i = 0; i < maps.Count; i++)
                    {
                        if (maps[i].Tile == destinationTile)
                        {
                            if(destinationCell != default(IntVec3))
                            {
                                arrivalAction = new WorldTransport.TM_TransportPodsArrivalAction_LandAtExactCell(maps[i].Parent, destinationCell, draftFlag);
                                break;
                            }
                            arrivalAction = new TransportPodsArrivalAction_LandInSpecificCell(maps[i].Parent, DropCellFinder.RandomDropSpot(maps[i]));
                            break;
                        }
                    }
                    if (arrivalAction == null)
                    {
                        if (TransportPodsArrivalAction_FormCaravan.CanFormCaravanAt(pods.Cast<IThingHolder>(), destinationTile))
                        {
                            arrivalAction = new TransportPodsArrivalAction_FormCaravan();
                        }
                        else
                        {
                            List<Caravan> caravans = Find.WorldObjects.Caravans;
                            for (int j = 0; j < caravans.Count; j++)
                            {
                                if (caravans[j].Tile == destinationTile && (bool)TransportPodsArrivalAction_GiveToCaravan.CanGiveTo(pods.Cast<IThingHolder>(), caravans[j]))
                                {
                                    arrivalAction = new TransportPodsArrivalAction_GiveToCaravan(caravans[j]);
                                    break;
                                }
                            }
                        }
                    }
                }
                if (arrivalAction != null && arrivalAction.ShouldUseLongEvent(pods, destinationTile))
                {
                    LongEventHandler.QueueLongEvent(delegate
                    {
                        DoArrivalAction();
                    }, "GeneratingMapForNewEncounter", false, null);
                }
                else
                {
                    DoArrivalAction();
                }
            }
        }

        private void DoArrivalAction()
        {
            for (int i = 0; i < pods.Count; i++)
            {
                pods[i].savePawnsWithReferenceMode = false;
                pods[i].parent = null;
            }
            if (arrivalAction != null)
            {
                try
                {
                    arrivalAction.Arrived(pods, destinationTile);                    
                }
                catch (Exception arg)
                {
                    Log.Error("Exception in transport pods arrival action: " + arg);
                }
                arrivalAction = null;
            }
            else
            {
                for (int j = 0; j < pods.Count; j++)
                {
                    for (int k = 0; k < pods[j].innerContainer.Count; k++)
                    {
                        Pawn pawn = pods[j].innerContainer[k] as Pawn;
                        if (pawn != null && (pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer))
                        {
                            PawnBanishUtility.Banish(pawn, destinationTile);
                        }
                    }
                }
                for (int l = 0; l < pods.Count; l++)
                {
                    pods[l].innerContainer.ClearAndDestroyContentsOrPassToWorld();
                }
                Messages.Message("MessageTransportPodsArrivedAndLost".Translate(), new GlobalTargetInfo(destinationTile), MessageTypeDefOf.NegativeEvent);
            }
            pods.Clear();
            Destroy();
        }

        public new void AddPod(ActiveDropPodInfo contents, bool justLeftTheMap)
        {
            contents.parent = this;
            pods.Add(contents);
            ThingOwner innerContainer = contents.innerContainer;
            for (int i = 0; i < innerContainer.Count; i++)
            {
                Pawn pawn = innerContainer[i] as Pawn;
                if (pawn != null && !pawn.IsWorldPawn())
                {
                    if (!base.Spawned)
                    {
                        Log.Warning("Passing pawn " + pawn + " to world, but the TravelingTransportPod is not spawned. This means that WorldPawns can discard this pawn which can cause bugs.");
                    }
                    if (justLeftTheMap)
                    {
                        pawn.ExitMap(false, Rot4.Invalid);
                    }
                    else
                    {
                        Find.WorldPawns.PassToWorld(pawn);
                    }
                }
            }
            contents.savePawnsWithReferenceMode = true;
        }

    }
}
