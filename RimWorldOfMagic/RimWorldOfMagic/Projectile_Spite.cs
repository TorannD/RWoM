using System.Linq;
using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;

namespace TorannMagic
{
    public class Projectile_Spite : Projectile_AbilityBase
    {

        private int pwrVal = 0;
        private int verVal = 0;
        private float arcaneDmg = 0;
        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();

            Pawn pawn = this.launcher as Pawn;
            Pawn victim = hitThing as Pawn;
            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
            MightPowerSkill pwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Spite.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Spite_pwr");
            MightPowerSkill ver = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Spite.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Spite_ver");
            pwrVal = pwr.level;
            verVal = ver.level;
            if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                MightPowerSkill mpwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                MightPowerSkill mver = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                pwrVal = mpwr.level;
                verVal = mver.level;
            }
            this.arcaneDmg = comp.mightPwr;
            if (settingsRef.AIHardMode && !pawn.IsColonist)
            {
                pwrVal = 3;
                verVal = 3;
            }

            if(victim == null && this.intendedTarget != null && this.intendedTarget.Thing != null && this.intendedTarget.Thing is Pawn)
            {
                victim = this.intendedTarget.Thing as Pawn;
            }
            
            if (victim != null)
            {
                TM_Action.DamageEntities(victim, null, this.def.projectile.GetDamageAmount(1, null) * (1+ .1f * pwrVal) * this.arcaneDmg, this.def.projectile.damageDef, this.launcher);
                if (Rand.Chance(.15f * verVal))
                {
                    int dmg = Mathf.RoundToInt(((this.def.projectile.GetDamageAmount(1, null) / 3)) * this.arcaneDmg);  //projectile = 15
                    TM_Action.DamageEntities(victim, null, dmg, DamageDefOf.Stun, this.launcher);
                }                
            }
        }

        public override void Tick()
        {

            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_SpiritFlame, this.DrawPos, this.Map, Rand.Range(.3f, .4f), .05f, 0.05f, .1f, Rand.Range(-300, 300), Rand.Range(.2f, .6f), Rand.Range(0, 360), Rand.Range(0, 360));
            base.Tick();
        }

    }    
}


