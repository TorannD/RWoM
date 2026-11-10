using AbilityUser;
using RimWorld;
using System.Linq;
using TorannMagic.Compatibility;
using Verse;

namespace TorannMagic
{
    class Verb_Transpose : Verb_UseAbility
    {
        internal const string Order = "TM_SpiritCastTranspose";
        internal static bool Family(TMAbilityDef def) => def != null && (def==TorannMagicDefOf.TM_Transpose ||
            def==TorannMagicDefOf.TM_Transpose_I || def==TorannMagicDefOf.TM_Transpose_II || def==TorannMagicDefOf.TM_Transpose_III);
        internal static Verb_Transpose For(Pawn body,MightAbility power) =>
            new Verb_Transpose {caster=body,Ability=power,verbProps=power.mightDef.MainVerb};

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            var body=CasterPawn;
            var other=target.Pawn;
            return body?.Spawned==true && other?.Spawned==true && body.Map==other.Map &&
                TM_Teleport.CanMove(body,other.Position) && TM_Teleport.CanMove(other,body.Position) &&
                !other.Position.Fogged(body.Map) && (root-other.Position).LengthHorizontal<verbProps.range &&
                TryFindShootLineFromTo(root,target,out _);
        }

        protected override bool TryCastShot()
        {
            burstShotsLeft=0;
            return Exchange(Ability?.Pawn ?? CasterPawn,currentTarget.Pawn);
        }

        internal bool Exchange(Pawn owner,Pawn target)
        {
            var body=CasterPawn;
            if (target==null || !CanHitTargetFrom(body.Position,target) || !TM_Teleport.Swap(body,target)) return false;
            if (!body.IsColonist)
            {
                if (target.IsColonist) TM_Action.SpellAffectedPlayerWarning(target);
                return true;
            }
            int rank=owner.GetCompAbilityUserMight()?.MightData.MightPowerSkill_Transpose.FirstOrDefault(s=>s.label=="TM_Transpose_ver")?.level ?? 0;
            var sickness=HediffDef.Named("TM_DisorientedVomit");
            if (rank<1) HealthUtility.AdjustSeverity(body,sickness,1f);
            Verb_Reversal.Strengthen(body,owner,2f+rank);
            if (target.needs?.food==null) return true;
            if (!target.HostileTo(body))
            {
                if (rank<2) HealthUtility.AdjustSeverity(target,sickness,1f);
            }
            else if (Rand.Chance(TM_Calc.GetSpellSuccessChance(owner,target,true)))
                HealthUtility.AdjustSeverity(target,sickness,1f);
            else MoteMaker.ThrowText(target.DrawPos,target.Map,"TM_ResistedSpell".Translate(),-1);
            return true;
        }
    }
}
