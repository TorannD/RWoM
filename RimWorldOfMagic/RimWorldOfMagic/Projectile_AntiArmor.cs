using RimWorld;
using Verse;
using System;
using AbilityUser;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace TorannMagic
{
    public class Projectile_AntiArmor : Projectile_AbilityBase
    {

        float xProb;
        IntVec3 newPos;
        bool xflag = false;
        bool zflag = false;
        int value = 0;

        private int verVal;
        private int pwrVal;

        private void Initialize(IntVec3 target, Pawn pawn)
        {
            newPos = target;
            XProb(target, pawn);
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;

            Pawn pawn = this.launcher as Pawn;
            Pawn victim = hitThing as Pawn;
            try
            {
                CompAbilityUserMight comp = pawn.GetComp<CompAbilityUserMight>();
                //MightPowerSkill pwr = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_AntiArmor.FirstOrDefault((MightPowerSkill x) => x.label == "TM_AntiArmor_pwr");
                //verVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_AntiArmor, "TM_AntiArmor", "_ver", true);
                //pwrVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_AntiArmor, "TM_AntiArmor", "_pwr", true);
                //MightPowerSkill ver = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_AntiArmor.FirstOrDefault((MightPowerSkill x) => x.label == "TM_AntiArmor_ver");
                MightPowerSkill str = comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_strength_pwr");
                //ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                //pwrVal = pwr.level;
                //verVal = ver.level;
                //if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                //{
                //    MightPowerSkill mpwr = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                //    MightPowerSkill mver = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                //    pwrVal = mpwr.level;
                //    verVal = mver.level;
                //}
                //if (settingsRef.AIHardMode && !pawn.IsColonist)
                //{
                //    pwrVal = 3;
                //    verVal = 3;
                //}
                verVal = TM_Calc.GetSkillVersatilityLevel(pawn, TorannMagicDefOf.TM_AntiArmor);
                pwrVal = TM_Calc.GetSkillPowerLevel(pawn, TorannMagicDefOf.TM_AntiArmor);
                this.Initialize(base.Position, pawn);

                if (victim != null && !victim.Dead && Rand.Chance(this.launcher.GetStatValue(StatDefOf.ShootingAccuracyPawn, true)))
                {
                    int dmg = GetWeaponDmg(pawn);
                    bool flagFleshType = victim.RaceProps.FleshType != FleshTypeDefOf.Normal;
                    float num = TM_Calc.GetOverallArmor(victim, StatDefOf.ArmorRating_Sharp);
                    bool flagArmorAmount =  num > 1f;
                    if (flagArmorAmount || flagFleshType)
                    {
                        FleckMaker.ThrowMicroSparks(victim.Position.ToVector3(), map);
                        TM_Action.DamageEntities(victim, null, dmg, 1.5f, this.def.projectile.damageDef, pawn);
                        FleckMaker.Static(victim.Position, pawn.Map, FleckDefOf.ExplosionFlash, 4f);
                        TM_Action.DamageEntities(victim, null, GetWeaponDmgMech(pawn, dmg), 1.5f, this.def.projectile.damageDef, pawn);
                        FleckMaker.ThrowMicroSparks(victim.Position.ToVector3(), map);
                        for (int i = 0; i < 1 + verVal; i++)
                        {
                            GenExplosion.DoExplosion(newPos, map, Rand.Range((.1f) * (1 + verVal), (.3f) * (1 + verVal)), DamageDefOf.Bomb, this.launcher, (this.def.projectile.GetDamageAmount(1, null) / 4) * (1 + verVal), .4f, SoundDefOf.MetalHitImportant, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, true);
                            GenExplosion.DoExplosion(newPos, map, Rand.Range((.2f) * (1 + verVal), (.4f) * (1 + verVal)), DamageDefOf.Stun, this.launcher, (this.def.projectile.GetDamageAmount(1, null) / 2) * (1 + verVal), .4f, SoundDefOf.MetalHitImportant, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, true);
                            newPos = GetNewPos(newPos, pawn.Position.x <= victim.Position.x, pawn.Position.z <= victim.Position.z, false, 0, 0, xProb, 1 - xProb);
                            FleckMaker.ThrowMicroSparks(victim.Position.ToVector3(), base.Map);
                            FleckMaker.ThrowDustPuff(newPos, map, Rand.Range(1.2f, 2.4f));
                        }
                        DestroyArmor(victim, 4, 100);
                    }
                    else
                    {
                        TM_Action.DamageEntities(victim, null, dmg, 1.5f, this.def.projectile.damageDef, pawn);
                    }
                }
                else
                {
                    Log.Message("No valid target for anti armor shot or missed");
                }
            }
            catch(NullReferenceException ex)
            {
                //
            }
        }

        public static void DestroyArmor(Pawn p, int itemsHit, float averageDestroyHP)
        {
            if (p.apparel != null && p.apparel.WornApparel != null && p.apparel.WornApparel.Count > 0)
            {
                List<Apparel> apparel = p.apparel.WornApparel.InRandomOrder().ToList();
                for(int i = 0; i < apparel.Count; i++)
                {
                    Apparel item = apparel[i];
                    if(itemsHit > 0)
                    {
                        itemsHit--;
                        item.HitPoints = Mathf.RoundToInt(Mathf.Clamp(item.HitPoints - (Rand.Range(averageDestroyHP * .5f, averageDestroyHP * 1.5f)), 0, item.MaxHitPoints));
                    }
                }
            }
            
        }

        public static int GetWeaponDmg(Pawn pawn)
        {
            CompAbilityUserMight comp = pawn.GetComp<CompAbilityUserMight>();

            float dmg = comp.weaponDamage;
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (!pawn.IsColonist && settingsRef.AIHardMode)
            {
                dmg += 8;
            }

            dmg = Mathf.RoundToInt(dmg * TorannMagicDefOf.TM_AntiArmor.weaponDamageFactor);
            return (int)Mathf.Clamp(dmg, 0, 60);
        }

        public static int GetWeaponDmgMech(Pawn pawn, int dmg)
        {

            //MightPowerSkill pwr = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_AntiArmor.FirstOrDefault((MightPowerSkill x) => x.label == "TM_AntiArmor_pwr");
            //int pwrVal = TM_Calc.GetMightSkillLevel(pawn, pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_AntiArmor, "TM_AntiArmor", "_pwr", true);
            int pwrVal = TM_Calc.GetSkillPowerLevel(pawn, TorannMagicDefOf.TM_AntiArmor);
            int mechDmg = dmg + Mathf.RoundToInt(dmg * (1 + .5f * pwrVal));
            return mechDmg;
        }

        public void damageEntities(Pawn victim, BodyPartRecord hitPart, int amt, DamageDef type)
        {
            DamageInfo dinfo;
            amt = (int)((float)amt * Rand.Range(.5f, 1.5f));
            if ( hitPart != null)
            {
                dinfo = new DamageInfo(type, amt, 0, (float)-1, this.launcher as Pawn, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
            }
            else
            {
                dinfo = new DamageInfo(type, amt, 0, this.ExactRotation.eulerAngles.y, this.launcher as Pawn, null, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown);                
            }
            victim.TakeDamage(dinfo);
        }

        private void XProb(IntVec3 target, Pawn pawn)
        {
            float hyp = 0;
            float angleRad = 0;
            float angleDeg = 0;

            hyp = Mathf.Sqrt((Mathf.Pow(pawn.Position.x - target.x, 2)) + (Mathf.Pow(pawn.Position.z - target.z, 2)));
            angleRad = Mathf.Asin(Mathf.Abs(pawn.Position.x - target.x) / hyp);
            angleDeg = Mathf.Rad2Deg * angleRad;
            xProb = angleDeg / 90;
        }

        private IntVec3 GetNewPos(IntVec3 curPos, bool xdir, bool zdir, bool halfway, float zvar, float xvar, float xguide, float zguide)
        {
            float rand = (float)Rand.Range(0, 100);
            bool flagx = rand <= ((xguide + Mathf.Abs(xvar)) * 100) && !xflag;
            bool flagz = rand <= ((zguide + Mathf.Abs(zvar)) * 100) && !zflag;

            if (halfway)
            {
                xvar = (-1 * xvar);
                zvar = (-1 * zvar);
            }

            if (xdir && zdir)
            {
                //top right
                if (flagx)
                {
                    if (xguide + xvar >= 0) { curPos.x++; }
                    else { curPos.x--; }
                }
                if (flagz)
                {
                    if (zguide + zvar >= 0) { curPos.z++; }
                    else { curPos.z--; }
                }
            }
            if (xdir && !zdir)
            {
                //bottom right
                if (flagx)
                {
                    if (xguide + xvar >= 0) { curPos.x++; }
                    else { curPos.x--; }
                }
                if (flagz)
                {
                    if ((-1 * zguide) + zvar >= 0) { curPos.z++; }
                    else { curPos.z--; }
                }
            }
            if (!xdir && zdir)
            {
                //top left
                if (flagx)
                {
                    if ((-1 * xguide) + xvar >= 0) { curPos.x++; }
                    else { curPos.x--; }
                }
                if (flagz)
                {
                    if (zguide + zvar >= 0) { curPos.z++; }
                    else { curPos.z--; }
                }
            }
            if (!xdir && !zdir)
            {
                //bottom left
                if (flagx)
                {
                    if ((-1 * xguide) + xvar >= 0) { curPos.x++; }
                    else { curPos.x--; }
                }
                if (flagz)
                {
                    if ((-1 * zguide) + zvar >= 0) { curPos.z++; }
                    else { curPos.z--; }
                }
            }
            else
            {
                //no direction identified
            }
            return curPos;
            //return curPos;
        }
    }
}