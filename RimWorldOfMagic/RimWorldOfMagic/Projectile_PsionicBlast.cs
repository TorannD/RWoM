using System.Linq;
using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;
using System.Collections.Generic;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class Projectile_PsionicBlast : Projectile_AbilityBase
    {
        ColorInt colorInt = new ColorInt(0, 128, 255);
        private bool initialized = false;
        private int pwrVal = 0;
        private float arcaneDmg = 1f;
        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();

            Pawn pawn = this.launcher as Pawn;
            Pawn victim = hitThing as Pawn;
            if(!pawn.Spawned)
            {
                //pwrVal = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicStorm.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicStorm_pwr").level;
                //pwrVal = TM_Calc.GetMightSkillLevel(pawn, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicStorm, "TM_PsionicStorm", "_pwr", true);
                pwrVal = TM_Calc.GetSkillPowerLevel(pawn, TorannMagicDefOf.TM_PsionicStorm, false);
                arcaneDmg = pawn.GetCompAbilityUserMight().mightPwr;
            }
            else
            {
                //MightPowerSkill pwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicBlast.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicBlast_pwr");
                //pwrVal = TM_Calc.GetMightSkillLevel(pawn, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicBlast, "TM_PsionicBlast", "_pwr", true);
                //pwrVal = pwr.level;
                pwrVal = TM_Calc.GetSkillPowerLevel(pawn, TorannMagicDefOf.TM_PsionicBlast, false);
                arcaneDmg = pawn.GetCompAbilityUserMight().mightPwr;
            }
           
            //if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mpwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    pwrVal = mpwr.level;
            //}

            TM_MoteMaker.MakePowerBeamMotePsionic(base.Position, map, this.def.projectile.explosionRadius * 6f, 2f, .7f, .1f, .6f);
            float angle = (Quaternion.AngleAxis(90, Vector3.up) * GetVector(pawn.Position, base.Position)).ToAngleFlat();
            GenExplosion.DoExplosion(base.Position, map, this.def.projectile.explosionRadius, TMDamageDefOf.DamageDefOf.TM_PsionicInjury, this.launcher, Mathf.RoundToInt(this.def.projectile.GetDamageAmount(1, null) * pawn.GetStatValue(StatDefOf.PsychicSensitivity, false) * (1 + (0.15f * pwrVal)) * this.arcaneDmg), 0, this.def.projectile.soundExplode, def, this.equipmentDef, this.intendedTarget.Thing, null, 0f, 1, false, null, 0f, 1, 0.0f, false);
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            Vector3 heading = (objectPos - center).ToVector3();
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }
    }    
}


