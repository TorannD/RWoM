using System;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    internal class JobDriver_PortalDestination : JobDriver
    {
        private static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("Other/PortalBldg", true);

        private const TargetIndex building = TargetIndex.A;
        Building_TMPortal portalBldg; // = new Building_TMPortal();
        CompAbilityUserMagic comp;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(building);
            Toil reserveTargetA = Toils_Reserve.Reserve(building);
            yield return reserveTargetA;
            yield return Toils_Goto.GotoThing(building, PathEndMode.ClosestTouch);

            Toil selectDestination = new Toil();
            
            selectDestination.initAction = () =>
            {
                
                portalBldg = TargetA.Thing as Building_TMPortal;
                if (portalBldg != null)
                {

                    
                }
            };
            selectDestination.AddFinishAction(() =>
            {
                if (portalBldg != null)
                {                    
                    StartChoosingDestination();
                }
            });

            yield return selectDestination;


        }

        private void StartChoosingDestination()
        {
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(TargetA.ToGlobalTargetInfo(this.Map)));
            Find.WorldSelector.ClearSelection();
            int tile = this.pawn.Map.Tile;
            Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChooseWorldTarget), true, JobDriver_PortalDestination.TargeterMouseAttachment, true, delegate
            {
                GenDraw.DrawWorldRadiusRing(tile, (int)(this.portalBldg.MaxLaunchDistance));  //center, max launch distance
            }, delegate (GlobalTargetInfo target)
            {
                if (!target.IsValid)
                {
                    return null;
                }
                int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
                if (num <= portalBldg.MaxLaunchDistance) // <= max launch distance
                {
                    return null;
                }
                if (num > portalBldg.MaxLaunchDistance) //this.MaxLaunchDistanceEverPossible
                {
                    return "TM_PortalDestinationInvalid".Translate();
                }
                return "TM_PortalDestinationInvalid".Translate();
            });
        }

        private bool ChooseWorldTarget(GlobalTargetInfo target)
        {
            if (!target.IsValid)
            {
                Messages.Message("TM_PortalDestinationInvalid".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            int num = Find.WorldGrid.TraversalDistanceBetween(portalBldg.Map.Tile, target.Tile);
            if (num > portalBldg.MaxLaunchDistance)
            {
                Messages.Message("TM_PortalDestinationInvalid".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            MapParent mapParent = target.WorldObject as MapParent;
            if (mapParent != null && mapParent.HasMap)
            {
                Map myMap = portalBldg.Map;
                Map map = mapParent.Map;
                Current.Game.CurrentMap = map;
                comp = pawn.GetCompAbilityUserMagic();
                TargetingParameters portalTarget = new TargetingParameters();
                portalTarget.canTargetLocations = true;
                portalTarget.canTargetSelf = false;
                portalTarget.canTargetPawns = false;
                portalTarget.canTargetFires = false;
                portalTarget.canTargetBuildings = false;
                portalTarget.canTargetItems = false;
                portalTarget.validator = ((TargetInfo x) => x.IsValid && !x.Cell.Fogged(map) && x.Cell.InBoundsWithNullCheck(map) && x.Cell.Walkable(map));  //TargetingParameters.ForDropPodsDestination()
                Find.Targeter.BeginTargeting(portalTarget, delegate (LocalTargetInfo x)
                {                    
                    portalBldg.PortalDestinationPosition = x.Cell;
                    portalBldg.PortalDestinationMap = map;
                    comp.Mana.CurLevel = comp.Mana.CurLevel - .7f;
                    int xpnum = Rand.Range(200, 230);
                    comp.MagicUserXP += xpnum;
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.MapHeld, "XP +" + xpnum, -1f);
                    portalBldg.IsPaired = true;

                }, null, delegate
                {
                    if (Find.Maps.Contains(myMap))
                    {
                        Current.Game.CurrentMap = myMap;
                    }
                }, JobDriver_PortalDestination.TargeterMouseAttachment);
                return true;
            }
            bool flag;
            if (mapParent != null)
            {
                Settlement settlement = mapParent as Settlement;
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                if (settlement != null && settlement.Faction == pawn.Faction)
                {
                    Messages.Message("TM_PortalDestinationInvalid".Translate(), MessageTypeDefOf.RejectInput);
                }
                //if (mapParent.HasMap)
                //{
                //    Messages.Message("TM_PortalDestinationInvalid".Translate(), MessageTypeDefOf.RejectInput);
                //    return false;
                //}
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
                Messages.Message("TM_PortalDestinationInvalid".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            //this.TryLaunch(target, PawnsArriveMode.Undecided, false);
            Messages.Message("TM_NoValidPortalDestination".Translate(), MessageTypeDefOf.RejectInput);
            return false;
        }
    }
}
