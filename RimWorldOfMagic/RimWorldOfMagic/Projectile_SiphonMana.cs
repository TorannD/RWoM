using Verse;
using AbilityUser;
using System.Linq;
using RimWorld;


namespace TorannMagic
{
    public class Projectile_SiphonMana : Projectile_AbilityBase
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

            if (hitPawn != null && !hitPawn.Dead && !caster.Dead && !caster.Downed && caster != null)
            {
                if (hitPawn.Faction != caster.Faction)
                {
                    if (Rand.Chance(TM_Calc.GetSpellSuccessChance(caster, hitPawn, true)))
                    {
                        if (compHitPawn != null && compHitPawn.IsMagicUser)
                        {
                            MagicPowerSkill regen = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_global_regen.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_global_regen_pwr");
                            float manaDrained = compHitPawn.Mana.CurLevel;
                            if (manaDrained > (.5f * compCaster.arcaneDmg))
                            {
                                manaDrained = (.5f * compCaster.arcaneDmg);
                            }
                            compHitPawn.Mana.CurLevel -= manaDrained;
                            compCaster.Mana.CurLevel += (manaDrained * .6f) * (1 + regen.level * .05f);
                            //TM_MoteMaker.ThrowSiphonMote(hitPawn.Position.ToVector3Shifted(), hitPawn.Map, 1f);
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Siphon, hitPawn.DrawPos, hitPawn.Map, 1.2f, .05f, .15f, .65f, -300, .2f, Rand.Range(0, 360), Rand.Range(0, 360));
                            TM_MoteMaker.ThrowManaPuff(caster.DrawPos, caster.Map, 1f);
                            if (compCaster.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr").level >= 6)
                            {
                                float sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                                HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_AntiManipulation, sev);
                                sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                                HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_AntiMovement, sev);
                                sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                                HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_AntiBreathing, sev);
                                sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                                HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_AntiSight, sev);
                                TM_MoteMaker.ThrowSiphonMote(hitPawn.Position.ToVector3(), hitPawn.Map, 1f);
                                TM_MoteMaker.ThrowSiphonMote(hitPawn.Position.ToVector3(), hitPawn.Map, 1f);
                            }
                        }
                        else if(hitPawn.health != null && hitPawn.health.hediffSet != null && hitPawn.Map != null)
                        {
                            float sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                            HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_AntiManipulation, sev);
                            sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                            HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_AntiMovement, sev);
                            sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                            HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_AntiBreathing, sev);
                            sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                            HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_AntiSight, sev);
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Siphon, hitPawn.DrawPos, hitPawn.Map, 1.2f, .05f, .15f, .65f, -300, .2f, Rand.Range(0, 360), Rand.Range(0, 360));
                        }
                    }
                    else
                    {
                        MoteMaker.ThrowText(hitPawn.DrawPos, hitPawn.Map, "TM_ResistedSpell".Translate(), -1);
                    }
                }
                else
                {
                    if (compHitPawn != null && compHitPawn.IsMagicUser)
                    {
                        MagicPowerSkill regen = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_global_regen.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_global_regen_pwr");
                        float manaDrained = compHitPawn.Mana.CurLevel;
                        if (manaDrained > .5f)
                        {
                            manaDrained = .5f;
                        }
                        compHitPawn.Mana.CurLevel -= manaDrained / compCaster.arcaneDmg;
                        compCaster.Mana.CurLevel += (manaDrained * .6f) * (1 + regen.level * .05f) * compCaster.arcaneDmg;
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Siphon, hitPawn.DrawPos, hitPawn.Map, 1.2f, .05f, .15f, .65f, -300, .2f, Rand.Range(0, 360), Rand.Range(0, 360));
                        TM_MoteMaker.ThrowManaPuff(caster.Position.ToVector3(), caster.Map, 1f);
                        if (compCaster.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr").level >= 6)
                        {
                            float sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                            HealthUtility.AdjustSeverity(caster, TorannMagicDefOf.TM_Manipulation, sev);
                            sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                            HealthUtility.AdjustSeverity(caster, TorannMagicDefOf.TM_Movement, sev);
                            sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                            HealthUtility.AdjustSeverity(caster, TorannMagicDefOf.TM_Breathing, sev);
                            sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                            HealthUtility.AdjustSeverity(caster, TorannMagicDefOf.TM_Sight, sev);
                            TM_MoteMaker.ThrowManaPuff(caster.Position.ToVector3(), caster.Map, 1f);
                            TM_MoteMaker.ThrowManaPuff(caster.Position.ToVector3(), caster.Map, 1f);
                        }
                    }
                    else if(hitPawn.health != null && hitPawn.health.hediffSet != null)
                    {
                        float sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_AntiManipulation, sev);
                        sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_AntiMovement, sev);
                        sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_AntiBreathing, sev);
                        sev = Rand.Range(0, 10) * compCaster.arcaneDmg;
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_AntiSight, sev);
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Siphon, hitPawn.DrawPos, hitPawn.Map, 1.2f, .05f, .15f, .65f, -300, .2f, Rand.Range(0, 360), Rand.Range(0, 360));
                    }
                }
            }
            Destroy();
        }
    }
}