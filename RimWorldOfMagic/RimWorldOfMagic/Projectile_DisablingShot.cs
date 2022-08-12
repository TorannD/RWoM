using RimWorld;
using Verse;
using System;
using AbilityUser;
using System.Linq;
using System.Collections.Generic;
using Verse.AI;
using Verse.AI.Group;


namespace TorannMagic
{
    public class Projectile_DisablingShot : Projectile_AbilityBase
    {

        BodyPartRecord vitalPart = null;
        private int verVal;

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;

            Pawn pawn = this.launcher as Pawn;
            Pawn victim = hitThing as Pawn;
            try
            {
                CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
                //MightPowerSkill ver = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_DisablingShot.FirstOrDefault((MightPowerSkill x) => x.label == "TM_DisablingShot_ver");
                //verVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_DisablingShot, "TM_DisablingShot", "_ver", true);
                verVal = TM_Calc.GetSkillVersatilityLevel(pawn, TorannMagicDefOf.TM_DisablingShot);
                MightPowerSkill str = comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_strength_pwr");
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                //verVal = ver.level;
                //if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                //{
                //    MightPowerSkill mver = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                //    verVal = mver.level;
                //}
                //if (settingsRef.AICasting && !pawn.IsColonist)
                //{
                //    verVal = 3;
                //}
                if (victim != null && !victim.Dead && Rand.Chance(this.launcher.GetStatValue(StatDefOf.ShootingAccuracyPawn, true)))
                {
                    int dmg = (this.def.projectile.GetDamageAmount(1, null));
                    if (victim.RaceProps.IsFlesh)
                    {
                        System.Random rnd = new System.Random();
                        if (verVal > 0 && victim.needs.food != null)
                        {
                            int randomTranqSev = GenMath.RoundRandom(rnd.Next((int)(verVal * .5f * str.level), (int)((verVal + .5f * str.level) * 3)));
                            LegShot(victim, randomTranqSev, TMDamageDefOf.DamageDefOf.TM_Tranquilizer);
                        }
                        else
                        {
                            LegShot(victim, dmg, TMDamageDefOf.DamageDefOf.TM_DisablingShot);
                        }
                    }
                    else
                    {
                        damageEntities(victim, null, dmg, this.def.projectile.damageDef);
                    }
                }
                else
                {
                    Log.Message("No valid target for Disabling Shot");
                }
            }
            catch(NullReferenceException ex)
            {

            }
        }

        public void LegShot(Pawn victim, int dmg, DamageDef dmgType)
        {            
            if (!victim.Dead )
            {
                IEnumerable<BodyPartRecord> partSearch = victim.def.race.body.AllParts;
                if( Rand.Chance(.5f)) { vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore)); }
                else { vitalPart = partSearch.LastOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore)); }                
                
                if (vitalPart != null)
                {
                    this.damageEntities(victim, vitalPart, dmg, dmgType);
                }
                else
                {
                    vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.MovingLimbSegment));
                    if (vitalPart != null)
                    {
                        this.damageEntities(victim, vitalPart, dmg, dmgType);
                    }
                    else
                    {
                        this.damageEntities(victim, vitalPart, dmg, null);
                    }
                }
            }
        }

        public void damageEntities(Pawn victim, BodyPartRecord hitPart, int amt, DamageDef type)
        {
            DamageInfo dinfo;
            if (victim != null && hitPart != null)
            {
                dinfo = new DamageInfo(type, amt, 0, (float)-1, this.launcher as Pawn, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);             
            }
            else
            {
                dinfo = new DamageInfo(type, amt, 0, (float)-1, this.launcher as Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            }
            victim.TakeDamage(dinfo);

            if (!victim.IsColonist && !victim.IsPrisoner && !victim.Faction.HostileTo(this.launcher.Faction) && victim.Faction != null && victim.Faction != this.launcher.Faction)
            {
                Faction faction = victim.Faction;
                faction.TryAffectGoodwillWith(this.launcher.Faction, -40, true, false, TorannMagicDefOf.TM_OffensiveMagic, null);
            }           

        }
    }
}
