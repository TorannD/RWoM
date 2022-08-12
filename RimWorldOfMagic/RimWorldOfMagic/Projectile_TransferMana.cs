using Verse;
using AbilityUser;
using System.Linq;


namespace TorannMagic
{
    public class Projectile_TransferMana : Projectile_AbilityBase
    {
        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            //base.Impact(hitThing);
            ThingDef def = this.def;

            Pawn hitPawn = hitThing as Pawn;
            Pawn caster = this.launcher as Pawn;
            CompAbilityUserMagic compHitPawn = hitPawn.GetCompAbilityUserMagic();            
            CompAbilityUserMagic compCaster = caster.GetCompAbilityUserMagic();

            if (hitPawn != null && compHitPawn != null)
            {
                if (compHitPawn.IsMagicUser && compHitPawn.MagicData != null && compHitPawn.Mana != null)
                {
                    MagicPowerSkill regen = hitPawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_global_regen.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_global_regen_pwr");
                    compHitPawn.Mana.CurLevel += (.2f + (.01f * regen.level)) * compCaster.arcaneDmg;
                    TM_MoteMaker.ThrowManaPuff(hitPawn.DrawPos, hitPawn.Map, 1f);
                    TM_MoteMaker.ThrowManaPuff(hitPawn.DrawPos, hitPawn.Map, 1f);
                    if(compCaster.MagicData != null && compCaster.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr").level >= 6)
                    {
                        float sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Manipulation, sev);
                        sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Movement, sev);
                        sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Breathing, sev);
                        sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Sight, sev);
                        TM_MoteMaker.ThrowManaPuff(hitPawn.Position.ToVector3(), hitPawn.Map, 1f);
                        TM_MoteMaker.ThrowManaPuff(hitPawn.Position.ToVector3(), hitPawn.Map, 1f);
                    }
                }
                else
                {
                    float sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Manipulation, sev);
                    sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Movement, sev);
                    sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Breathing, sev);
                    sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Sight, sev);
                    TM_MoteMaker.ThrowManaPuff(hitPawn.DrawPos, hitPawn.Map, 1f);
                    TM_MoteMaker.ThrowManaPuff(hitPawn.DrawPos, hitPawn.Map, 1f);
                }
            }
            Destroy();
        }
    }
}
