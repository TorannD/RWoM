using AbilityUser;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;

namespace TorannMagic
{
    public class Verb_PsionicBarrier : Verb_UseAbility
    {
        internal const string SelfOrder="TM_SpiritPsionicBarrier", FieldOrder="TM_SpiritProjectedBarrier";
        internal static bool Handles(string name) => name==SelfOrder || name==FieldOrder;
        internal static bool Valid(Pawn body,MightAbility power,LocalTargetInfo target)
        {
            if (body?.Spawned!=true || power==null || !target.IsValid || !target.Cell.InBounds(body.Map)) return false;
            if (power.Def==TorannMagicDefOf.TM_PsionicBarrier) return target.Cell==body.Position;
            return power.Def==TorannMagicDefOf.TM_PsionicBarrier_Projected && !target.Cell.Fogged(body.Map) &&
                target.Cell.InHorDistOf(body.Position,power.mightDef.MainVerb.range) &&
                new Verb_PsionicBarrier {caster=body,Ability=power,verbProps=power.mightDef.MainVerb}.CanHitTargetFrom(body.Position,target);
        }

        internal static void Choose(Pawn body,Pawn source,MightAbility power,string name)
        {
            var jobDef=DefDatabase<JobDef>.GetNamed(name);
            bool Available(LocalTargetInfo t) => body.CurJobDef!=jobDef && SpiritHostedMartial.Find(body,source,name)==power &&
                SpiritHostedMartial.Ready(body,power,name) && Valid(body,power,t);
            void Submit(LocalTargetInfo t)
            {
                if (Available(t)) body.jobs.TryTakeOrderedJob(JobMaker.MakeJob(jobDef,source,t.Cell),JobTag.Misc);
            }
            if (name==SelfOrder) Submit(body.Position);
            else Find.Targeter.BeginTargeting(power.mightDef.MainVerb.targetParams,Submit,
                highlightAction:t=>GenDraw.DrawRadiusRing(body.Position,power.mightDef.MainVerb.range),
                targetValidator:Available,caster:body);
        }

        internal static bool Begin(Pawn body,MightAbility power,LocalTargetInfo target)
        {
            if (!Valid(body,power,target)) return false;
            var job=JobMaker.MakeJob(TorannMagicDefOf.JobDriver_PsionicBarrier,target.Cell);
            if (power.Pawn!=body)
            {
                job.targetB=power.Pawn;
                job.count=power.Def==TorannMagicDefOf.TM_PsionicBarrier_Projected ? 2 : 1;
            }
            if (!body.jobs.TryTakeOrderedJob(job,JobTag.Misc)) return false;
            Odyssey.ManualTravelSafety.Settle(power);
            return true;
        }

        protected override bool TryCastShot()
        {
            if (Ability is MightAbility power) Begin(CasterPawn,power,verbProps.targetParams.canTargetLocations?currentTarget:CasterPawn.Position);
            burstShotsLeft=0;
            // Payment was settled when the channel was accepted.
            return false;
        }
    }
}
