using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using RimWorld;
using RimWorld.Planet;
using System.Text;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Verb_LightSkipGlobal : Verb_UseAbility  
    {
        int pwrVal = 0;
        float arcaneDmg = 1;
        Pawn pawn;
        IntVec3 launcherPosition = default(IntVec3);
        List<Thing> pods = new List<Thing>();
        List<Pawn> pawnList = new List<Pawn>();
        List<IntVec3> unroofedCells = new List<IntVec3>();
        List<CompTransporter> podTList = new List<CompTransporter>();
        IntVec3 safePos = default(IntVec3);
        private int gi = 0;
        private int MaxLaunchDistance = 300;

        IntVec3 destCell = default(IntVec3);
        int destTile = 0;
        bool draftFlag = false;

        private static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("Other/PortalBldg", true);
        bool validTarg;
        CompLaunchable cl = null;
        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt), Light Skip requires unroofed destination

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = !targ.Cell.Roofed(base.CasterPawn.Map);
                }
                else
                {
                    //out of range
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot()
        {
            bool result = false;
            Pawn pawn = this.CasterPawn;
            Map map = this.CasterPawn.Map;
            if (map != null && !pawn.Position.Roofed(map))
            {

                this.pawn = this.CasterPawn;
                launcherPosition = this.CasterPawn.Position;
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                //pwrVal = TM_Calc.GetMagicSkillLevel(this.pawn, comp.MagicData.MagicPowerSkill_LightSkip, "TM_LightSkip", "_pwr", false);
                pwrVal = TM_Calc.GetSkillPowerLevel(pawn, TorannMagicDefOf.TM_LightSkip, false);
                this.arcaneDmg = comp.arcaneDmg;
                this.draftFlag = this.pawn.drafter != null ? this.pawn.Drafted : false;
                this.gi = 0;
                podTList.Clear();
                pawnList.Clear();
                pods.Clear();

                List<Pawn> tmpList = TM_Calc.FindAllPawnsAround(this.CasterPawn.Map, launcherPosition, 5f, this.pawn.Faction, true);
                for(int i = 0; i < tmpList.Count; i++)
                {
                    if(!tmpList[i].Position.Roofed(map) )
                    {                        
                        if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                        {
                            if (ModCheck.GiddyUp.IsMount(tmpList[i]))
                            {
                                continue;
                            }
                        }
                        if (tmpList[i].carryTracker != null && tmpList[i].carryTracker.CarriedThing != null)
                        {
                            tmpList[i].carryTracker.TryDropCarriedThing(tmpList[i].Position, ThingPlaceMode.Near, out Thing _);
                        }
                        pawnList.Add(tmpList[i]);
                    }
                }
                StartChoosingDestination();         
            }
            else
            {
                Messages.Message("TM_CannotCastUnderRoof".Translate(pawn.LabelShort, Ability.Def.label), MessageTypeDefOf.NegativeEvent);
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Light Skip: " + StringsToTranslate.AU_CastFailure, -1f);
            }
            return result;
        }

        public void CreatePodGroup()
        {
            if (pawnList != null && pawnList.Count > 0)
            {
                for (int i = 0; i < pawnList.Count; i++)
                {
                    Pawn p = pawnList[i];
                    Pawn mount = null;
                    if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                    {
                        mount = ModCheck.GiddyUp.GetMount(p);
                        ModCheck.GiddyUp.ForceDismount(p);
                    }
                    Thing pod = ThingMaker.MakeThing(TorannMagicDefOf.TM_LightPod, null);
                    CompLaunchable podL = pod.TryGetComp<CompLaunchable>();
                    CompTransporter podT = podL.Transporter;
                    GenSpawn.Spawn(pod, p.Position, p.Map, WipeMode.Vanish);
                    podT.groupID = 12;
                    p.DeSpawn();
                    if(mount != null)
                    {
                        mount.DeSpawn();
                        podT.innerContainer.TryAddOrTransfer(mount);
                    }
                    podT.innerContainer.TryAddOrTransfer(p);
                    podTList.Add(podT);
                    pods.Add(pod);
                }
            }
        }

        public void LaunchLightPod(int destinationTile, IntVec3 destinationCell, TransportPodsArrivalAction arrivalAction)
        {
            Map map = this.CasterPawn.Map;
            CreatePodGroup();
            podTList[0].TryRemoveLord(map);
            int groupID = podTList[0].groupID;
            for (int i = 0; i < podTList.Count; i++)
            {
                ThingOwner directlyHeldThings = podTList[i].GetDirectlyHeldThings();
                ActiveDropPod activeDropPod = (ActiveDropPod)ThingMaker.MakeThing(ThingDefOf.ActiveDropPod);
                activeDropPod.Contents = new ActiveDropPodInfo();
                activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer(directlyHeldThings, canMergeWithExistingStacks: true, destroyLeftover: true);
                WorldTransport.TM_DropPodLeaving obj = (WorldTransport.TM_DropPodLeaving)SkyfallerMaker.MakeSkyfaller(TorannMagicDefOf.TM_LightPodLeaving, activeDropPod);
                obj.groupID = groupID;
                obj.destinationTile = destinationTile;
                obj.arrivalAction = arrivalAction;
                obj.arrivalCell = destinationCell;
                obj.draftFlag = this.draftFlag;
                podTList[i].CleanUpLoadingVars(map);
                podTList[i].parent.Destroy();
                GenSpawn.Spawn(obj, podTList[i].parent.Position, map);
            }
            CameraJumper.TryHideWorld();
            if (!map.mapPawns.AnyColonistSpawned && !map.IsPlayerHome)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("TM_AbandoningMap".Translate(map.Parent.LabelCap));
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(stringBuilder.ToString(), delegate
                {
                    Settlement sm = map.Parent as Settlement;
                    WorldTransport.TM_DelayedDestroyMap ddm = new WorldTransport.TM_DelayedDestroyMap();
                    ddm.parent = sm;
                    ddm.delayTicks = 120;
                    sm.AllComps.Add(ddm);
                }));
            }
        }

        public IntVec3 GetRelativePositionOffset(IntVec3 targetCell, IntVec3 relativePosition, IntVec3 offsetPosition)
        {
            IntVec3 cell = targetCell + (offsetPosition - relativePosition);
            if (cell.Roofed(this.CasterPawn.Map))
            {
                cell = unroofedCells.RandomElement();
            }
            return cell;
        }

        public List<IntVec3> GetUnroofedCellsAround(IntVec3 center, float r)
        {
            List<IntVec3> cellList = new List<IntVec3>();
            cellList.Clear();
            IEnumerable<IntVec3> allCells = GenRadial.RadialCellsAround(center, r, true);
            foreach (IntVec3 item in allCells)
            {
                if (!item.Roofed(this.CasterPawn.Map))
                {
                    cellList.Add(item);
                }
            }
            return cellList;
        }

        private void StartChoosingDestination()
        {
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(CasterPawn.Map.Parent));
            Find.WorldSelector.ClearSelection();
            int tile = CasterPawn.Map.Tile;
            Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChooseWorldTarget), true, Verb_LightSkipGlobal.TargeterMouseAttachment, true, delegate
            {
                GenDraw.DrawWorldRadiusRing(tile, 300);
            }, delegate (GlobalTargetInfo target)
            {
                if (!target.IsValid)
                {
                    return null;
                }
                int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
                if (num > MaxLaunchDistance)
                {
                    GUI.color = ColorLibrary.RedReadable;
                    return "TM_UnableToTravel".Translate();
                }
                IEnumerable<FloatMenuOption> transportPodsFloatMenuOptionsAt = GetTransportPodsFloatMenuOptionsAt(target.Tile, target);
                if (!transportPodsFloatMenuOptionsAt.Any())
                {
                    return "";
                }
                if (transportPodsFloatMenuOptionsAt.Count() == 1)
                {
                    if (transportPodsFloatMenuOptionsAt.First().Disabled)
                    {
                        GUI.color = ColorLibrary.RedReadable;
                    }
                    return transportPodsFloatMenuOptionsAt.First().Label;
                }
                MapParent mapParent = target.WorldObject as MapParent;
                if (mapParent != null)
                {
                    return "ClickToSeeAvailableOrders_WorldObject".Translate(mapParent.LabelCap);
                }
                return "ClickToSeeAvailableOrders_Empty".Translate();
            });
        }

        private bool ChooseWorldTarget(GlobalTargetInfo target)
        {
            if (!target.IsValid)
            {
                Messages.Message("TM_UnableToTravel".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            int num = Find.WorldGrid.TraversalDistanceBetween(this.CasterPawn.Map.Tile, target.Tile);
            if (num > MaxLaunchDistance)
            {
                Messages.Message("TM_UnableToTravel".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            IEnumerable<FloatMenuOption> transportPodsFloatMenuOptionsAt = GetTransportPodsFloatMenuOptionsAt(target.Tile, target);
            if (!transportPodsFloatMenuOptionsAt.Any())
            {
                if (Find.World.Impassable(target.Tile))
                {
                    Messages.Message("TM_UnableToTravel".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                    return false;
                }
                LaunchLightPod(target.Tile, default(IntVec3), null);
                return true;
            }

            if (transportPodsFloatMenuOptionsAt.Count() == 1)
            {
                if (!transportPodsFloatMenuOptionsAt.First().Disabled)
                {
                    transportPodsFloatMenuOptionsAt.First().action();
                }
                return false;
            }
            Find.WindowStack.Add(new FloatMenu(transportPodsFloatMenuOptionsAt.ToList()));
            return false;
        }

        public IEnumerable<FloatMenuOption> GetTransportPodsFloatMenuOptionsAt(int tile, GlobalTargetInfo target)
        {
            bool anything = false;
            MapParent mapParent = target.WorldObject as MapParent;
            if (mapParent != null && mapParent.HasMap)
            {
                yield return new FloatMenuOption("TM_SelectTargetOnMap".Translate(mapParent.LabelCap), delegate
                {                  
                    Map map = mapParent.Map;
                    Current.Game.CurrentMap = map;
                    CameraJumper.TryHideWorld();
                    Find.Targeter.BeginTargeting(TargetingParameters.ForDropPodsDestination(), delegate (LocalTargetInfo x)
                    {
                        destCell = x.Cell;
                        destTile = map.Tile;
                        LaunchLightPod(destTile, destCell, null);

                    }, null);
                });
            }

            if (!Find.World.Impassable(tile))
            {
                yield return new FloatMenuOption("FormCaravanHere".Translate(), delegate
                {
                    LaunchLightPod(target.Tile, default(IntVec3), new TransportPodsArrivalAction_FormCaravan());
                });
            }
        }
    }
}