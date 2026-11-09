using RimWorld;
using TorannMagic.ClassProgression;
using Verse;

namespace TorannMagic
{
    class Verb_Dominate : Verb_SpiritTargetable
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            var map = CasterPawn?.Map;
            return map != null && target.Pawn is Pawn victim && victim.Spawned && !victim.Dead &&
                victim.Map == map && !victim.Position.Fogged(map) && victim.Position.Walkable(map) &&
                (root - victim.Position).LengthHorizontal < verbProps.range;
        }

        protected override bool TryCastShot()
        {
            burstShotsLeft = 0;
            var body = CasterPawn;
            var owner = SpiritRebirthCasting.SkillOwner(body, Ability);
            if (owner == null || !CanHitTargetFrom(body.Position,currentTarget)) return false;
            var victim = currentTarget.Pawn;
            bool physical = ClassIdentity.DemonEffect(owner,false,owner.gender == Gender.Female || owner.story?.traits?.HasTrait(TorannMagicDefOf.Succubus) == true);
            bool mental = ClassIdentity.DemonEffect(owner,true,owner.gender == Gender.Male || owner.story?.traits?.HasTrait(TorannMagicDefOf.Warlock) == true);
            if (!physical && !mental) return false;
            if (!Rand.Chance(TM_Calc.GetSpellSuccessChance(owner,victim,true)))
            {
                MoteMaker.ThrowText(victim.DrawPos,victim.Map,"TM_ResistedSpell".Translate(),-1);
                return true;
            }

            var spell = (TMAbilityDef)Ability.Def;
            int power = TM_Calc.GetSkillPowerLevel(owner,spell);
            int versatility = TM_Calc.GetSkillVersatilityLevel(owner,spell,true);
            int efficiency = TM_Calc.GetSkillEfficiencyLevel(owner,spell);
            if (physical)
            {
                Apply(victim,Effect(false,power),versatility,efficiency);
                for (int i = 0; i < 4; i++)
                    TM_MoteMaker.ThrowShadowMote(victim.Position.ToVector3(),body.Map,Rand.Range(.6f,1f));
            }
            if (mental) Apply(victim,Effect(true,power),versatility,efficiency);
            if (victim.IsColonist && !body.IsColonist) TM_Action.SpellAffectedPlayerWarning(victim);
            return true;
        }

        private static HediffDef Effect(bool mental,int power) => power switch
        {
            3 => mental ? TorannMagicDefOf.TM_WDDominateHD_III : TorannMagicDefOf.TM_SDDominateHD_III,
            2 => mental ? TorannMagicDefOf.TM_WDDominateHD_II : TorannMagicDefOf.TM_SDDominateHD_II,
            1 => mental ? TorannMagicDefOf.TM_WDDominateHD_I : TorannMagicDefOf.TM_SDDominateHD_I,
            _ => mental ? TorannMagicDefOf.TM_WDDominateHD : TorannMagicDefOf.TM_SDDominateHD
        };

        private static void Apply(Pawn victim,HediffDef def,int versatility,int efficiency)
        {
            HealthUtility.AdjustSeverity(victim,def,Rand.Range(1.5f + versatility,3f + versatility));
            var effect = victim.health.hediffSet.GetFirstHediffOfDef(def)?.TryGetComp<HediffComp_Dominate>();
            if (effect != null)
            {
                effect.EffVal = efficiency;
                effect.VerVal = versatility;
            }
        }
    }
}
