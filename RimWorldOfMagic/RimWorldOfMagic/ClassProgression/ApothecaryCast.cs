using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class ApothecaryCast
    {
        internal const string Balm="TM_SpiritCastBalm", Elixir="TM_SpiritCastElixir";
        internal const string Clarity="TM_SpiritCastHerbalClarity", Bliss="TM_SpiritCastHerbalBliss",
            Poison="TM_SpiritCastPoisonFlask", Fire="TM_SpiritCastFireSuppressionFlask";
        internal static bool Handles(string order) => order==Balm || order==Elixir || order==Clarity || order==Bliss || order==Poison || order==Fire;
        internal static TMAbilityDef Definition(string order) => order==Balm ? TorannMagicDefOf.TM_SoothingBalm :
            order==Elixir ? TorannMagicDefOf.TM_Elixir : order==Clarity ? TorannMagicDefOf.TM_HerbalClarity :
            order==Bliss ? TorannMagicDefOf.TM_HerbalBliss : order==Poison ? TorannMagicDefOf.TM_PoisonFlask :
            order==Fire ? TorannMagicDefOf.TM_FireSuppressionFlask : null;
        internal static bool Matches(string order,TMAbilityDef def) => Handles(order) && def!=null &&
            (def==Definition(order) || order==Fire &&
                (def==DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_FireSuppressionFlask_I") ||
                 def==DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_FireSuppressionFlask_II") ||
                 def==DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_FireSuppressionFlask_III")));
        private sealed class ProjectileVerb : Verb_SB
        {
            // The hosted job pays after launch, so do not run native prepayment refunds.
            protected override bool TryCastShot() => TryLaunchProjectileCheck(verbProps.defaultProjectile,currentTarget);
        }
        private static Verb_SpiritTargetable Verb(Pawn body,MightAbility power) =>
            power.Def==TorannMagicDefOf.TM_SoothingBalm ?
                new Verb_SoothingBalm{caster=body,Ability=power,verbProps=power.mightDef.MainVerb} :
            power.Def==TorannMagicDefOf.TM_Elixir ? new Verb_Elixir{caster=body,Ability=power,verbProps=power.mightDef.MainVerb} :
                new ProjectileVerb{caster=body,Ability=power,verbProps=power.mightDef.MainVerb};
        internal static bool Funded(Pawn body,MightAbility power,string order)
        {
            if(power==null || !Matches(order,power.mightDef) || ApothecaryMartialPath.Owner(body)!=power.Pawn ||
                power.mightDef.requiredHediff!=TorannMagicDefOf.TM_ApothecaryHerbsHD) return false;
            if(power.mightDef.MainVerb.isViolent && body.WorkTagIsDisabled(WorkTags.Violent)) return false;
            var stock=ApothecaryMartialPath.Stock(power.Pawn);
            return stock!=null && stock.Severity>=power.MightUser.ActualHediffCost(power.mightDef);
        }
        internal static bool Valid(Pawn body,MightAbility power,LocalTargetInfo target)
        {
            if(body?.Spawned!=true || power==null || !target.IsValid ||
                target.HasThing && (!target.Thing.Spawned || target.Thing.Map!=body.Map)) return false;
            bool flask=power.Def==TorannMagicDefOf.TM_PoisonFlask || Matches(Fire,power.mightDef);
            if(!flask && (target.Pawn==null || target.Pawn.Dead)) return false;
            return Verb(body,power).CanHitTargetFrom(body.Position,target);
        }
        internal static void Choose(Pawn body,Pawn soul,MightAbility power,string order)
        {
            var job=DefDatabase<JobDef>.GetNamed(order);
            bool Allowed(LocalTargetInfo target) => SpiritHostedMartial.Ready(body,SpiritHostedMartial.Find(body,soul,order),order) && Valid(body,power,target);
            Find.Targeter.BeginTargeting(power.mightDef.MainVerb.targetParams,target=>{
                if(body.CurJobDef!=job && Allowed(target)) body.jobs.TryTakeOrderedJob(JobMaker.MakeJob(job,soul,target),JobTag.Misc);
            },highlightAction:target=>GenDraw.DrawRadiusRing(body.Position,power.mightDef.MainVerb.range),targetValidator:Allowed,caster:body);
        }
        internal static bool Execute(Pawn body,Pawn soul,string order,LocalTargetInfo target)
        {
            var power=SpiritHostedMartial.Find(body,soul,order);
            if(!SpiritHostedMartial.Ready(body,power,order) || !Valid(body,power,target) ||
                !Verb(body,power).CastFromHost(target)) return false;
            power.PostAbilityAttempt();
            return true;
        }
    }
}
