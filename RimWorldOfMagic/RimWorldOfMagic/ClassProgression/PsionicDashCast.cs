using RimWorld;
using TorannMagic.Odyssey;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class PsionicDashCast
    {
        internal const string JobName = "TM_SpiritPsionicDash";
        internal const string StormOrder="TM_SpiritPsionicStorm";
        internal static bool Handles(string order) => order==JobName || order==StormOrder;
        internal static string OrderFor(MightAbility power) => power?.Def==TorannMagicDefOf.TM_PsionicStorm ? StormOrder : JobName;
        internal static int Horizon(Pawn body, IntVec3 cell, bool storm=false) => storm ? 900 : 180;
        internal static float PsiCost(Pawn owner,TMAbilityDef def)
        {
            int efficiency=owner?.GetCompAbilityUserMight()?.ExistingMightData?.GetSkill_Efficiency(def)?.level ?? 0;
            return Mathf.Max(0,def==TorannMagicDefOf.TM_PsionicStorm ? 65-5*efficiency : 8-efficiency);
        }
        internal static bool HasEnergy(MightAbility power) =>
            power.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_PsionicHD) is not Hediff energy ||
            energy.Severity>=PsiCost(power.Pawn,power.mightDef);

        private static bool Valid(Pawn body, MightAbility power, IntVec3 cell)
        {
            if (!SpiritHostedMartial.Ready(body,power,OrderFor(power)) || !cell.InBounds(body.Map) || cell == body.Position ||
                !cell.Standable(body.Map) || cell.Fogged(body.Map)) return false;
            AbilityUser.Verb_UseAbility verb=OrderFor(power)==StormOrder ? new Effect_PsionicStorm() : new Effect_PsionicDash();
            verb.caster=body;verb.Ability=power;verb.verbProps=power.mightDef.MainVerb;
            return verb.CanHitTargetFrom(body.Position,cell);
        }

        internal static bool CanLaunch(Pawn body, MightAbility power, IntVec3 cell) =>
            Valid(body,power,cell) && ManualTravelSafety.CanComplete(body,body.Map,cell,Horizon(body,cell,OrderFor(power)==StormOrder));

        internal static void ChooseDestination(Pawn body, Pawn soul, MightAbility power)
        {
            var map=body.Map;
            bool Available(IntVec3 cell) => body.Spawned && body.Map==map &&
                SpiritHostedMartial.Find(body,soul,OrderFor(power))==power && Valid(body,power,cell);
            Find.Targeter.BeginTargeting(new TargetingParameters { canTargetLocations=true, canTargetPawns=false,
                canTargetBuildings=false,canTargetItems=false }, target => {
                    var cell=target.Cell;
                    if(!Available(cell))return;
                    var order=JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(OrderFor(power)),soul,cell);
                    order.playerForced=true;
                    ManualTravelSafety.Submit(body,order,map,cell,true,()=>Available(cell),
                        ()=>body.jobs.TryTakeOrderedJob(order,JobTag.Misc),Horizon(body,cell,OrderFor(power)==StormOrder));
                }, highlightAction: target=>GenDraw.DrawRadiusRing(body.Position,power.mightDef.MainVerb.range),
                targetValidator: target=>Available(target.Cell), caster:body);
        }

        internal static bool Launch(Pawn body,MightAbility power,IntVec3 cell)
        {
            if (!CanLaunch(body,power,cell)) return false;
            if (ModCheck.Validate.GiddyUp.Core_IsInitialized()) ModCheck.GiddyUp.ForceDismount(body);
            IThingHolder flight;
            if(OrderFor(power)==StormOrder)
            {
                var storm=(FlyingObject_PsionicStorm)GenSpawn.Spawn(ThingDef.Named("FlyingObject_PsionicStorm"),body.Position,body.Map);
                storm.SetSkillOwner(power.Pawn);storm.Launch(body,cell,body,(Verb)null);flight=storm;
            }
            else
            {
                var dash=(FlyingObject_PsionicDash)GenSpawn.Spawn(ThingDef.Named("FlyingObject_PsionicDash"),body.Position,body.Map);
                dash.SetSkillOwner(power.Pawn);dash.Launch(body,cell,body);flight=dash;
            }
            if(!flight.GetDirectlyHeldThings().Contains(body))return false;
            var energy=power.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_PsionicHD);
            if(energy!=null)energy.Severity-=PsiCost(power.Pawn,power.mightDef);
            ManualTravelSafety.Settle(power);
            return flight.GetDirectlyHeldThings().Contains(body);
        }
    }
}
