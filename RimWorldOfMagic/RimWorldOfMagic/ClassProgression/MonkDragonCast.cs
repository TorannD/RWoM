using RimWorld;
using TorannMagic.Odyssey;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class MonkDragonCast
    {
        internal const string Order = "TM_SpiritDragonStrike";
        private static Effect_DragonStrike Verb(Pawn body, MightAbility power) =>
            new Effect_DragonStrike { caster = body, Ability = power, verbProps = power.mightDef.MainVerb };

        private static bool Valid(Pawn body, MightAbility power, LocalTargetInfo target) =>
            SpiritHostedMartial.Ready(body, power, Order) && target.IsValid && target.Cell.InBounds(body.Map) &&
            target.Cell != body.Position && target.Cell.Standable(body.Map) && !target.Cell.Fogged(body.Map) &&
            Verb(body, power).CanHitTargetFrom(body.Position, target);

        internal static bool CanLaunch(Pawn body, MightAbility power, LocalTargetInfo target) =>
            Valid(body, power, target) && ManualTravelSafety.CanComplete(body, body.Map, target.Cell, 180);

        internal static void Choose(Pawn body, Pawn soul, MightAbility power)
        {
            var map = body.Map;
            bool Available(LocalTargetInfo target) => body.Spawned && body.Map == map &&
                SpiritHostedMartial.Find(body, soul, Order) == power && Valid(body, power, target);
            Find.Targeter.BeginTargeting(new TargetingParameters { canTargetLocations = true, canTargetPawns = true,
                canTargetBuildings = false, canTargetItems = false }, target => {
                    if (!Available(target)) return;
                    var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(Order), soul, target);
                    job.playerForced = true;
                    ManualTravelSafety.Submit(body, job, map, target.Cell, true, () => Available(target),
                        () => body.jobs.TryTakeOrderedJob(job, JobTag.Misc), 180);
                }, highlightAction: target => GenDraw.DrawRadiusRing(body.Position, power.mightDef.MainVerb.range),
                targetValidator: Available, caster: body);
        }

        internal static bool Launch(Pawn body, MightAbility power, LocalTargetInfo target)
        {
            if (!CanLaunch(body, power, target)) return false;
            var damage = Verb(body, power).PrepareImpact();
            if (!damage.HasValue) return false;
            if (ModCheck.Validate.GiddyUp.Core_IsInitialized()) ModCheck.GiddyUp.ForceDismount(body);
            var flight = (FlyingObject_DragonStrike)GenSpawn.Spawn(ThingDef.Named("FlyingObject_DragonStrike"), body.Position, body.Map);
            flight.SetSkillOwner(power.Pawn);
            flight.Launch(body, target, body, damage);
            if (!flight.GetDirectlyHeldThings().Contains(body)) return false;
            ManualTravelSafety.Settle(power);
            return true;
        }
    }
}
