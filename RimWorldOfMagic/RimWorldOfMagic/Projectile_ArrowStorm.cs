using System.Linq;
using System;
using RimWorld;
using Verse;
using AbilityUser;
using UnityEngine;

namespace TorannMagic
{
	public class Projectile_ArrowStorm : Projectile_AbilityBase
	{

        private bool initialized = false;
        private static int pwrVal;
        private static int verVal;
        Pawn pawn;

        public void Initialize(Map map)
        {
            pawn = this.launcher as Pawn;
            initialized = true;
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            try
            {
                Pawn victim = hitThing as Pawn;
                if (!initialized)
                {
                    Initialize(map);
                }

                int dmg = GetWeaponDmg(this.launcher as Pawn);
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();

                if (victim != null && Rand.Chance(GetWeaponAccuracy(pawn)))
                {
                    damageEntities(victim, null, dmg, DamageDefOf.Arrow);
                    TM_MoteMaker.ThrowBloodSquirt(victim.DrawPos, victim.Map, 1f);
                }
            }
            catch(NullReferenceException ex)
            {
                //
            }
        }

        public static float GetWeaponAccuracy(Pawn pawn)
        {
            float weaponAccuracy = pawn.equipment.Primary.GetStatValue(StatDefOf.AccuracyMedium, true);
            verVal = TM_Calc.GetSkillVersatilityLevel(pawn, TorannMagicDefOf.TM_ArrowStorm);
            //verVal = TM_Calc.GetMightSkillLevel(pawn, pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_ArrowStorm, "TM_ArrowStorm", "_ver", true);
            //MightPowerSkill ver = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_ArrowStorm.FirstOrDefault((MightPowerSkill x) => x.label == "TM_ArrowStorm_ver");
            //verVal = ver.level;
            //if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mver = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    verVal = mver.level;
            //}
            weaponAccuracy = Mathf.Min(1f, (.8f * weaponAccuracy) + (.05f * verVal));
            return weaponAccuracy;
        }

        public static int GetWeaponDmg(Pawn pawn)
        {
            CompAbilityUserMight comp = pawn.GetComp<CompAbilityUserMight>();
            //pwrVal = TM_Calc.GetMightSkillLevel(pawn, pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_ArrowStorm, "TM_ArrowStorm", "_pwr", true);   
            pwrVal = TM_Calc.GetSkillPowerLevel(pawn, TorannMagicDefOf.TM_ArrowStorm);

            float dmg = comp.weaponDamage;
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (!pawn.IsColonist && settingsRef.AIHardMode)
            {
                dmg += 8;
            }

            dmg = Mathf.RoundToInt(dmg * (1f + (.1f * pwrVal)) * TorannMagicDefOf.TM_ArrowStorm.weaponDamageFactor); 

            return (int)Mathf.Clamp(dmg, 0, 50);
        }

        public void damageEntities(Pawn victim, BodyPartRecord hitPart, int amt, DamageDef type)
        {
            DamageInfo dinfo;
            amt = (int)((float)amt * Rand.Range(.7f, 1.3f));
            dinfo = new DamageInfo(type, amt, 0, (float)-1, pawn, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
            dinfo.SetAllowDamagePropagation(false);
            victim.TakeDamage(dinfo);
        }
    }
}
