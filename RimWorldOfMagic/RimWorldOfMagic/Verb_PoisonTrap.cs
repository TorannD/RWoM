using AbilityUser;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;

namespace TorannMagic
{
    public class Verb_PoisonTrap : Verb_UseAbility
    {
        internal const string Order = "TM_SpiritCastPoisonTrap";
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            var body=CasterPawn;
            return body?.Spawned==true && !(body.jobs?.curDriver is JobDriver_RangerBreakthrough hunt && hunt.Active) &&
                !target.HasThing && JobDriver_PlacePoisonTrap.OpenCell(body.Map,target.Cell) &&
                (root-target.Cell).LengthHorizontal < verbProps.range && TryFindShootLineFromTo(root,target,out _);
        }

        internal static bool Valid(Pawn body,MightAbility power,LocalTargetInfo target)
        {
            if (power==null || body?.Spawned!=true) return false;
            var verb=new Verb_PoisonTrap {Ability=power,caster=body,verbProps=power.mightDef.MainVerb};
            return verb.CanHitTargetFrom(body.Position,target);
        }

        internal static Job MakeOrder(Pawn owner,LocalTargetInfo target) =>
            JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed(Order),target,owner);

        internal static void Choose(Pawn body,MightAbility power)
        {
            var map=body.Map;
            bool Available(LocalTargetInfo target) => body.Map==map &&
                SpiritHostedMartial.Find(body,power.Pawn,Order)==power && SpiritHostedMartial.Ready(body,power,Order) &&
                Valid(body,power,target);
            Find.Targeter.BeginTargeting(power.mightDef.MainVerb.targetParams,target=>{
                if (Available(target)) body.jobs.TryTakeOrderedJob(MakeOrder(power.Pawn,target),JobTag.Misc);
            },highlightAction:t=>GenDraw.DrawRadiusRing(body.Position,power.mightDef.MainVerb.range),
                targetValidator:Available,caster:body);
        }

        protected override bool TryCastShot()
        {
            burstShotsLeft=0;
            if (!CanHitTargetFrom(CasterPawn.Position,currentTarget)) return false;
            // External abilities that use this verb directly retain the already-paid vanilla-format placement job.
            if (!CasterPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(TorannMagicDefOf.PlacePoisonTrap,currentTarget),JobTag.Misc)) return false;
            Ability.PostAbilityAttempt();
            return true;
        }
    }
}
