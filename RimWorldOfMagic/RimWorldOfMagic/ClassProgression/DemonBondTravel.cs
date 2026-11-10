using System;
using System.Collections.Generic;
using AbilityUser;
using RimWorld;
using RimWorld.Planet;
using TorannMagic.Odyssey;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class DemonBondTravel
    {
        internal static bool Applies(PawnAbility power) =>
            power?.Def?.defName == "TM_ShadowStep" || power?.Def?.defName == "TM_ShadowCall";

        private static bool Route(Pawn body, MagicAbility power, out Pawn partner, out Pawn traveller, out GlobalTargetInfo destination, GlobalTargetInfo? bound = null)
        {
            partner = DemonSoulBond.TravelPartner(power.Pawn);
            traveller = power.Def == TorannMagicDefOf.TM_ShadowStep ? body : partner;
            var anchor = traveller == body ? partner : body;
            destination = GlobalTargetInfo.Invalid;
            if (body?.Spawned != true || body.Dead || partner == null || partner == body ||
                SpiritRebirthCasting.SkillOwner(body, power) != power.Pawn) return false;
            if (!anchor.Spawned)
            {
                var caravan = anchor.GetCaravan();
                if (caravan == null || caravan.Destroyed || !caravan.pawns.Contains(anchor)) return false;
                destination = new GlobalTargetInfo(caravan);
                return true;
            }
            if (traveller.Map == anchor.Map && traveller.Position.AdjacentTo8WayOrInside(anchor.Position)) return false;
            bool Open(IntVec3 cell) => cell.InBounds(anchor.Map) && cell.Standable(anchor.Map) &&
                cell.GetFirstPawn(anchor.Map) == null && !cell.ContainsStaticFire(anchor.Map);
            if (bound.HasValue)
            {
                var endpoint = bound.Value;
                if (endpoint.Map != anchor.Map || !endpoint.Cell.AdjacentTo8WayOrInside(anchor.Position) || !Open(endpoint.Cell)) return false;
                destination = endpoint;
                return true;
            }
            foreach (var offset in GenAdj.AdjacentCells)
            {
                var cell = anchor.Position + offset;
                if (!Open(cell)) continue;
                if (!destination.IsValid) destination = new GlobalTargetInfo(cell,anchor.Map);
                if (TravelEnvironmentSafety.IsSafe(traveller,anchor.Map,cell))
                {
                    destination = new GlobalTargetInfo(cell,anchor.Map);
                    break;
                }
            }
            return destination.IsValid;
        }

        internal static bool Submit(Pawn body, MagicAbility power, Job job, bool player, Func<bool> validate, Action submit)
        {
            if (job == null || !Route(body, power, out var partner, out var traveller, out var destination)) return false;
            job.targetC = power.MagicUser.soulBondPawn;
            job.targetQueueB = new List<LocalTargetInfo> { partner };
            job.globalTarget = destination;
            bool Valid() => validate() && Matches(body, power, job, out _, out _);
            if (destination.WorldObject is Caravan)
            {
                if (!Valid()) return false;
                submit();
                return true;
            }
            return ManualTravelSafety.Submit(traveller, job, destination.Map, destination.Cell, player, Valid, submit);
        }

        private static bool Matches(Pawn body, MagicAbility power, Job job, out Pawn traveller, out GlobalTargetInfo destination)
        {
            bool found = Route(body, power, out var partner, out traveller, out destination, job?.globalTarget);
            return found && job?.targetC.Pawn == power.MagicUser.soulBondPawn &&
                job.targetQueueB?.Count == 1 && job.targetQueueB[0].Pawn == partner && job.globalTarget == destination;
        }

        internal static Job Order(MagicAbility power, AbilityContext context, LocalTargetInfo target)
        {
            var body = power.Pawn;
            bool Valid() => target.Thing == body && body.Spawned && power.CanCastPowerCheck(context,out _);
            if (!Valid()) return null;
            var job = power.GetJob(context,target);
            bool player = context == AbilityContext.Player;
            return Submit(body,power,job,player,Valid,() => { if (player) body.jobs.TryTakeOrderedJob(job); }) ? job : null;
        }

        internal static bool Cast(Pawn body, MagicAbility power)
        {
            var job = body.CurJob;
            if (!Matches(body,power,job,out var traveller,out var destination)) return false;
            if (body == power.Pawn ? !power.CanCastPowerCheck(job.count == 0 ? AbilityContext.AI : AbilityContext.Player,out _) : !SpiritHostedBolt.Ready(body,power)) return false;
            if (!(destination.WorldObject is Caravan))
            {
                var hazards = TravelEnvironmentSafety.Evaluate(traveller,destination.Map,destination.Cell,0);
                if (!hazards.IsSafe && ((hazards.Hazards & TravelEnvironmentHazard.InvalidDestination) != 0 ||
                    !ManualTravelSafety.HasConsent(traveller,job,destination.Map,destination.Cell))) return false;
            }
            if (!Move(traveller,destination)) return false;
            if (power.Pawn == body) ManualTravelSafety.Settle(power);
            else ManualTravelSafety.SettleHosted(power,body);
            return true;
        }

        private static bool Move(Pawn pawn, GlobalTargetInfo destination)
        {
            var source = pawn.Map;
            var cell = pawn.Position;
            var rotation = pawn.Rotation;
            var caravan = pawn.GetCaravan();
            var arriving = destination.WorldObject as Caravan;
            bool drafted = pawn.Drafted;
            var cargo = pawn.carryTracker?.CarriedThing;
            if (source == null && (caravan == null || !caravan.pawns.Contains(pawn))) return false;
            void RestoreCargo()
            {
                if (cargo != null && !pawn.carryTracker.innerContainer.Contains(cargo) && !pawn.carryTracker.innerContainer.TryAdd(cargo,false))
                    throw new InvalidOperationException("Could not restore carried item.");
            }
            try
            {
                if (ModCheck.Validate.GiddyUp.Core_IsInitialized()) ModCheck.GiddyUp.ForceDismount(pawn);
                if (cargo != null) pawn.carryTracker.innerContainer.Remove(cargo);
                if (source != null) pawn.DeSpawn();
                else caravan.pawns.Remove(pawn);
                if (arriving != null)
                {
                    arriving.AddPawn(pawn,true);
                    if (!arriving.pawns.Contains(pawn)) throw new InvalidOperationException("Caravan did not accept bonded traveller.");
                    if (!Find.WorldPawns.Contains(pawn)) Find.WorldPawns.PassToWorld(pawn);
                }
                else
                {
                    GenSpawn.Spawn(pawn,destination.Cell,destination.Map,rotation);
                    if (pawn.Map != destination.Map || pawn.Position != destination.Cell) throw new InvalidOperationException("Traveller did not arrive.");
                    if (drafted && pawn.drafter != null) pawn.drafter.Drafted = true;
                }
                RestoreCargo();
            }
            catch (Exception error)
            {
                Compatibility.TM_Log.Error("ShadowTravel","Transfer failed: " + error);
                try
                {
                    if (arriving?.pawns.Contains(pawn) == true) arriving.pawns.Remove(pawn);
                    if (source != null && (pawn.Map != source || pawn.Position != cell))
                    {
                        if (pawn.Spawned) pawn.DeSpawn();
                        GenSpawn.Spawn(pawn,cell,source,rotation);
                    }
                    else if (source == null)
                    {
                        if (pawn.Spawned) pawn.DeSpawn();
                        if (!caravan.pawns.Contains(pawn)) caravan.AddPawn(pawn,true);
                        if (!Find.WorldPawns.Contains(pawn)) Find.WorldPawns.PassToWorld(pawn);
                    }
                    if (pawn.Spawned && drafted && pawn.drafter != null) pawn.drafter.Drafted = true;
                    RestoreCargo();
                }
                catch (Exception rollback)
                {
                    Compatibility.TM_Log.Error("ShadowTravel","Could not restore origin: " + rollback);
                    if (!pawn.Spawned && pawn.ParentHolder == null && !Find.WorldPawns.Contains(pawn))
                        Find.WorldPawns.PassToWorld(pawn,PawnDiscardDecideMode.KeepForever);
                }
                return false;
            }
            if (caravan != null && !caravan.Destroyed && caravan.PawnsListForReading.Count == 0)
            {
                try { caravan.Destroy(); }
                catch (Exception cleanup) { Compatibility.TM_Log.Error("ShadowTravel","Unable to remove empty caravan after transfer: " + cleanup); }
            }
            return true;
        }
    }
}
