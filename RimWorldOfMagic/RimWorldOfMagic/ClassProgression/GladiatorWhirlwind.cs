using RimWorld;
using TorannMagic.Odyssey;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class GladiatorWhirlwind
    {
        internal const string JobName = "TM_SpiritCastWhirlwind";
        internal static int Horizon(Pawn body, IntVec3 cell) => Mathf.CeilToInt(body.Position.DistanceTo(cell) / .4f) + 1;

        private static bool Valid(Pawn body, MightAbility power, IntVec3 cell)
        {
            if (!SpiritHostedMartial.Ready(body,power,JobName) || !cell.InBounds(body.Map) || cell == body.Position ||
                !cell.Standable(body.Map) || cell.Fogged(body.Map)) return false;
            var verb=new Effect_Whirlwind { caster=body, Ability=power, verbProps=power.mightDef.MainVerb };
            return verb.CanHitTargetFrom(body.Position,cell);
        }

        internal static bool CanLaunch(Pawn body, MightAbility power, IntVec3 cell) =>
            Valid(body,power,cell) && ManualTravelSafety.CanComplete(body,body.Map,cell,Horizon(body,cell));

        internal static void ChooseDestination(Pawn body, Pawn soul, MightAbility power)
        {
            var map=body.Map;
            bool Available(IntVec3 cell) => body.Spawned && body.Map==map &&
                SpiritHostedMartial.Find(body,soul,JobName)==power && Valid(body,power,cell);
            Find.Targeter.BeginTargeting(new TargetingParameters { canTargetLocations=true, canTargetPawns=false,
                canTargetBuildings=false,canTargetItems=false }, target => {
                    var cell=target.Cell;
                    if(!Available(cell))return;
                    var order=JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(JobName),soul,cell);
                    order.playerForced=true;
                    ManualTravelSafety.Submit(body,order,map,cell,true,()=>Available(cell),
                        ()=>body.jobs.TryTakeOrderedJob(order,JobTag.Misc),Horizon(body,cell));
                }, highlightAction: target=>GenDraw.DrawRadiusRing(body.Position,power.mightDef.MainVerb.range),
                targetValidator: target=>Available(target.Cell), caster:body);
        }

        internal static bool Launch(Pawn body,MightAbility power,IntVec3 cell)
        {
            if (!CanLaunch(body,power,cell)) return false;
            if (ModCheck.Validate.GiddyUp.Core_IsInitialized()) ModCheck.GiddyUp.ForceDismount(body);
            var flight=(FlyingObject_Whirlwind)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Whirlwind"),body.Position,body.Map);
            flight.LaunchAbility(body,power,cell);
            return flight.GetDirectlyHeldThings().Contains(body);
        }
    }
}
