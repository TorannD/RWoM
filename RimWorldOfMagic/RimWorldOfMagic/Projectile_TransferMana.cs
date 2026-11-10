using Verse;
using AbilityUser;
using System.Linq;


namespace TorannMagic
{
    public class Projectile_TransferMana : Projectile_AbilityBase
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            //base.Impact(hitThing);
            ThingDef def = this.def;

            // Misses, non-pawn hits and a despawned/dead caster must not throw here.
            Pawn hitPawn = hitThing as Pawn;
            Pawn caster = this.launcher as Pawn;
            CompAbilityUserMagic compHitPawn = hitPawn?.GetCompAbilityUserMagic();
            CompAbilityUserMagic compCaster = caster?.GetCompAbilityUserMagic();

            if (hitPawn != null && !hitPawn.Dead && compHitPawn != null)
            {
                if (compHitPawn.IsMagicUser && compHitPawn.MagicData != null && compHitPawn.Mana != null)
                {
                    float arcaneDmg = compCaster?.arcaneDmg ?? 1f;
                    MagicPowerSkill regen = compHitPawn.MagicData.MagicPowerSkill_global_regen.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_global_regen_pwr");
                    compHitPawn.Mana.CurLevel += (.2f + (.01f * (regen?.level ?? 0))) * arcaneDmg;
                    TM_MoteMaker.ThrowManaPuff(hitPawn.DrawPos, hitPawn.Map, 1f);
                    TM_MoteMaker.ThrowManaPuff(hitPawn.DrawPos, hitPawn.Map, 1f);
                    if(compCaster?.MagicData != null && (compCaster.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr")?.level ?? 0) >= 6)
                    {
                        float sev = Rand.Range(0, 10) * arcaneDmg;
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Manipulation, sev);
                        sev = Rand.Range(0, 10) * arcaneDmg;
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Movement, sev);
                        sev = Rand.Range(0, 10) * arcaneDmg;
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Breathing, sev);
                        sev = Rand.Range(0, 10) * arcaneDmg;
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Sight, sev);
                        TM_MoteMaker.ThrowManaPuff(hitPawn.Position.ToVector3(), hitPawn.Map, 1f);
                        TM_MoteMaker.ThrowManaPuff(hitPawn.Position.ToVector3(), hitPawn.Map, 1f);
                    }
                }
                else
                {
                    float arcaneDmg = compCaster?.arcaneDmg ?? 1f;
                    float sev = Rand.Range(0, 10) * arcaneDmg;
                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Manipulation, sev);
                    sev = Rand.Range(0, 10) * arcaneDmg;
                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Movement, sev);
                    sev = Rand.Range(0, 10) * arcaneDmg;
                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Breathing, sev);
                    sev = Rand.Range(0, 10) * arcaneDmg;
                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Sight, sev);
                    TM_MoteMaker.ThrowManaPuff(hitPawn.DrawPos, hitPawn.Map, 1f);
                    TM_MoteMaker.ThrowManaPuff(hitPawn.DrawPos, hitPawn.Map, 1f);
                }
            }
            Destroy();
        }
    }
}
