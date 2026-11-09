using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using Verse.Sound;
using RimWorld;
using System.Collections.Generic;

namespace TorannMagic
{
    public class Verb_LightSkip : Verb_UseAbility  
    {

        internal static bool ValidLanding(Pawn pawn, Map map, IntVec3 cell)
            => pawn?.Spawned == true && !pawn.Dead && !pawn.Downed && pawn.Map == map && !map.Disposed &&
                pawn.Position != cell && cell.InBounds(map) && !cell.Fogged(map) && cell.Walkable(map) &&
                !pawn.Position.Roofed(map) && !cell.Roofed(map);

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
            => targ.IsValid && ValidLanding(CasterPawn, CasterPawn?.Map, targ.Cell)
                && (root - targ.Cell).LengthHorizontal < verbProps.range;

        internal static CompTransporter PreparePod(Pawn passenger)
        {
            if (passenger?.Spawned != true) return null;
            var map = passenger.Map;
            var pod = ThingMaker.MakeThing(TorannMagicDefOf.TM_LightPod);
            var transporter = pod.TryGetComp<CompTransporter>();
            if (transporter == null || !GenPlace.TryPlaceThing(pod, passenger.Position, map, ThingPlaceMode.Near)) return null;
            transporter.groupID = Find.UniqueIDsManager.GetNextTransporterGroupID();
            Pawn mount = null;
            if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
            {
                mount = ModCheck.GiddyUp.GetMount(passenger);
                ModCheck.GiddyUp.ForceDismount(passenger);
            }
            if (!Board(passenger, transporter))
            {
                pod.Destroy();
                return null;
            }
            if (mount?.Spawned == true && mount.Map == map) Board(mount, transporter);
            return transporter;
        }

        private static bool Board(Pawn passenger, CompTransporter pod)
        {
            var map = passenger.Map;
            var start = passenger.Position;
            passenger.DeSpawn();
            if (!pod.innerContainer.TryAddOrTransfer(passenger))
            {
                GenSpawn.Spawn(passenger, start, map);
                return false;
            }
            passenger.teleporting = true;
            return true;
        }

        internal static bool DispatchPod(CompTransporter transporter, RimWorld.Planet.PlanetTile destination, IntVec3 cell, bool drafted, RimWorld.Planet.TransportersArrivalAction arrival = null)
        {
            var pod = transporter.parent;
            if (!pod.Spawned || transporter.innerContainer.Count == 0) return false;
            var map = pod.Map;
            var start = pod.Position;
            var active = (ActiveTransporter)ThingMaker.MakeThing(ThingDefOf.ActiveDropPod);
            active.Contents = new ActiveTransporterInfo();
            var leaving = (WorldTransport.TM_DropPodLeaving)SkyfallerMaker.MakeSkyfaller(TorannMagicDefOf.TM_LightPodLeaving, active);
            leaving.groupID = transporter.groupID;
            leaving.destinationTile = destination;
            leaving.arrivalCell = cell;
            leaving.arrivalAction = arrival;
            leaving.draftFlag = drafted;
            var passengers = active.Contents.innerContainer;
            try
            {
                passengers.TryAddRangeOrTransfer(transporter.innerContainer, canMergeWithExistingStacks: false, destroyLeftover: false);
                if (transporter.innerContainer.Count > 0) return false;
                GenSpawn.Spawn(leaving, start, map);
            }
            finally
            {
                if (!leaving.Spawned)
                    transporter.innerContainer.TryAddRangeOrTransfer(passengers, canMergeWithExistingStacks: false, destroyLeftover: false);
            }
            transporter.CleanUpLoadingVars(map);
            pod.Destroy();
            return true;
        }

        protected override bool TryCastShot()
        {
            bool result = false;
            Pawn pawn = this.CasterPawn;
            Map map = this.CasterPawn.Map;
            if (map != null && !pawn.Position.Roofed(map))
            {
                base.TryCastShot();
            }
            else
            {
                Messages.Message("TM_CannotCastUnderRoof".Translate(pawn.LabelShort, Ability.Def.label), MessageTypeDefOf.NegativeEvent);
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Light Skip: " + "AU_CastFailure".Translate(), -1f);
            }
            return result;
        }
    }
}

