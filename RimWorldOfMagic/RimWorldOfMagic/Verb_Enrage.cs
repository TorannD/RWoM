using RimWorld;
using Verse;

namespace TorannMagic
{
    public class Verb_Enrage : ClassProgression.Verb_SpiritTargetable
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            var pawn = targ.Pawn;
            return pawn?.Spawned == true && !pawn.Dead && pawn.Map == CasterPawn?.Map &&
                pawn.RaceProps.Humanlike && !TM_Calc.IsUndead(pawn) && !pawn.Position.Fogged(pawn.Map) &&
                (root - pawn.Position).LengthHorizontal < verbProps.range;
        }

        protected override bool TryCastShot()
        {
            burstShotsLeft = 0;
            var body = CasterPawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body,Ability);
            var magic = owner?.GetCompAbilityUserMagic();
            if (body?.Spawned != true || magic == null || !CanHitTargetFrom(body.Position,currentTarget)) return false;
            var target = currentTarget.Pawn;
            if (target.HostileTo(body))
            {
                if (!Rand.Chance(TM_Calc.GetSpellSuccessChance(owner,target,true)))
                {
                    MoteMaker.ThrowText(target.DrawPos,target.Map,"TM_ResistedSpell".Translate(),-1);
                    return false;
                }
                target.mindState?.mentalStateHandler?.TryStartMentalState(MentalStateDefOf.Berserk,null,true,false,false,body);
            }
            int power = TM_Calc.GetSkillPowerLevel(owner,TorannMagicDefOf.TM_Enrage);
            int versatility = TM_Calc.GetSkillVersatilityLevel(owner,TorannMagicDefOf.TM_Enrage);
            HealthUtility.AdjustSeverity(target,TorannMagicDefOf.TM_EnrageHD,(.25f + .05f * power) * magic.arcaneDmg);
            var effect = target.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnrageHD)?.TryGetComp<HediffComp_Enrage>();
            if (effect == null) return false;
            effect.reductionFactor = 1f - .1f * versatility;
            effect.consumeJoy = versatility >= 3;
            return true;
        }
    }
}
