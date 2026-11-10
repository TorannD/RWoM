using System.Linq;
using Verse;
using AbilityUser;
using UnityEngine;


namespace TorannMagic
{
    class Projectile_Firebolt : ClassProgression.Projectile_SpiritBolt
    {

        protected override string SkillStem => "TM_Firebolt";
        protected override System.Collections.Generic.List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_Firebolt;
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            

            Pawn pawn = this.launcher as Pawn;
            Pawn victim = hitThing as Pawn;
            if (!projected && pawn != null)
            {
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Firebolt.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Firebolt_pwr");
                pwrVal = pwr.level;
                arcaneDmg = comp.arcaneDmg;
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    MightPowerSkill mpwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                    pwrVal = mpwr.level;
                }
            }
            
            GenExplosion.DoExplosion(base.Position, map, 0.4f, TMDamageDefOf.DamageDefOf.Firebolt, SpellInstigator, Mathf.RoundToInt(this.def.projectile.GetDamageAmount(1, null) * arcaneDmg), 0, this.def.projectile.soundExplode, def, this.equipmentDef, this.intendedTarget.Thing, null, 0f, 1, null, null, 0, false, null, 0f, 1, 0.6f, false);
            CellRect cellRect = CellRect.CenteredOn(base.Position, 3);
            cellRect.ClipInsideMap(map);

            victim = base.Position.GetFirstPawn(map);
            if (victim != null)
            {                
                int dmg = Mathf.RoundToInt(((this.def.projectile.GetDamageAmount(1,null) / 3) * pwrVal)* arcaneDmg);  //projectile = 16
                if (!projected && ModOptions.Settings.Instance.AIHardMode && this.launcher is Pawn && !pawn.IsColonist)
                {
                    dmg += 10;
                }
                damageEntities(victim, dmg, TMDamageDefOf.DamageDefOf.Firebolt);
            }
        }

        public void damageEntities(Pawn e, int amt, DamageDef type)
        {
            DamageInfo dinfo = new DamageInfo(type, Mathf.RoundToInt(Rand.Range((float)(.5f * amt), (float)(1.25*amt))), 0, (float)-1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            bool flag = e != null;
            if (flag)
            {
                var trial = type == TMDamageDefOf.DamageDefOf.Firebolt
                    ? ClassProgression.FireboltDamageTrial.Capture(SpellInstigator as Pawn, e) : null;
                var damage = e.TakeDamage(dinfo);
                ClassProgression.FireboltDamageTrial.Complete(trial, damage?.totalDamageDealt ?? 0f);
            }
        }
    }    
}


