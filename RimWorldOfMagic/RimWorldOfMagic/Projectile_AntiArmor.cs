using RimWorld;
using Verse;
using System;
using AbilityUser;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;


namespace TorannMagic
{
    public class Projectile_AntiArmor : Projectile_SniperRound
    {

        float xProb;
        IntVec3 newPos;
        bool xflag = false;
        bool zflag = false;
        int value = 0;

        private int verVal;

        private void Initialize(IntVec3 target, IntVec3 from)
        {
            newPos = target;
            XProb(target, from);
        }

        protected override void CaptureWeaponDamage(Pawn body) => shotDamage = WeaponDamage(skillOwner,PhysicalWeaponDamage(body));

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            Pawn pawn = launcher as Pawn;
            base.Impact(hitThing,blockedByShield);
            if (blockedByShield || (!shotBound && pawn==null) || !(hitThing is Pawn victim) || victim.Dead) return;
            verVal = shotBound ? versatility : TM_Calc.GetSkillVersatilityLevel(pawn,TorannMagicDefOf.TM_AntiArmor);
            var from = shotBound ? firingCell : pawn.Position;
            Initialize(Position,from);
            if (Rand.Chance(shotBound ? accuracy : pawn.GetStatValue(StatDefOf.ShootingAccuracyPawn)))
            {
                int dmg = shotBound ? shotDamage : GetWeaponDmg(pawn);
                bool flagFleshType = victim.RaceProps.FleshType != FleshTypeDefOf.Normal;
                float num = TM_Calc.GetOverallArmor(victim, StatDefOf.ArmorRating_Sharp);
                bool flagArmorAmount =  num > 1f;
                if (flagArmorAmount || flagFleshType)
                {
                    FleckMaker.ThrowMicroSparks(victim.Position.ToVector3(), map);
                    TM_Action.DamageEntities(victim, null, dmg, 1.5f, this.def.projectile.damageDef, EffectOwner);
                    FleckMaker.Static(victim.Position, map, FleckDefOf.ExplosionFlash, 4f);
                    TM_Action.DamageEntities(victim, null, MechDamage(dmg,shotBound ? powerRank : TM_Calc.GetSkillPowerLevel(pawn,TorannMagicDefOf.TM_AntiArmor)), 1.5f, this.def.projectile.damageDef, EffectOwner);
                    FleckMaker.ThrowMicroSparks(victim.Position.ToVector3(), map);
                    for (int i = 0; i < 1 + verVal; i++)
                    {
                        GenExplosion.DoExplosion(newPos, map, Rand.Range((.1f) * (1 + verVal), (.3f) * (1 + verVal)), DamageDefOf.Bomb, EffectOwner, (this.def.projectile.GetDamageAmount(1, null) / 4) * (1 + verVal), .4f, SoundDefOf.MetalHitImportant, def, this.equipmentDef, null, null, 0f, 1, null, null, 0, false, null, 0f, 1, 0f, true);
                        GenExplosion.DoExplosion(newPos, map, Rand.Range((.2f) * (1 + verVal), (.4f) * (1 + verVal)), DamageDefOf.Stun, EffectOwner, (this.def.projectile.GetDamageAmount(1, null) / 2) * (1 + verVal), .4f, SoundDefOf.MetalHitImportant, def, this.equipmentDef, null, null, 0f, 1, null, null, 0, false, null, 0f, 1, 0f, true);
                        newPos = GetNewPos(newPos, from.x <= victim.Position.x, from.z <= victim.Position.z, false, 0, 0, xProb, 1 - xProb);
                        FleckMaker.ThrowMicroSparks(victim.Position.ToVector3(), map);
                        if (!newPos.InBounds(map)) break;
                        FleckMaker.ThrowDustPuff(newPos, map, Rand.Range(1.2f, 2.4f));
                    }
                    DestroyArmor(victim, 4, 100);
                }
                else
                {
                    TM_Action.DamageEntities(victim, null, dmg, 1.5f, this.def.projectile.damageDef, EffectOwner);
                }
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
            return WeaponDamage(pawn,pawn.GetCompAbilityUserMight().weaponDamage);
        }

        internal static int WeaponDamage(Pawn pawn,float dmg)
        {
            
            if (!pawn.IsColonist && ModOptions.Settings.Instance.AIHardMode)
            {
                dmg += 8;
            }

            dmg = Mathf.RoundToInt(dmg * TorannMagicDefOf.TM_AntiArmor.weaponDamageFactor);
            return (int)Mathf.Clamp(dmg, 0, 60);
        }

        public static int GetWeaponDmgMech(Pawn pawn, int dmg)
        {

            return MechDamage(dmg,TM_Calc.GetSkillPowerLevel(pawn,TorannMagicDefOf.TM_AntiArmor));
        }

        private static int MechDamage(int dmg,int rank) => dmg + Mathf.RoundToInt(dmg * (1 + .5f * rank));

        public void damageEntities(Pawn victim, BodyPartRecord hitPart, int amt, DamageDef type)
        {
            DamageInfo dinfo;
            amt = (int)((float)amt * Rand.Range(.5f, 1.5f));
            if ( hitPart != null)
            {
                dinfo = new DamageInfo(type, amt, 0, (float)-1, EffectOwner, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
            }
            else
            {
                dinfo = new DamageInfo(type, amt, 0, this.ExactRotation.eulerAngles.y, EffectOwner, null, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown);
            }
            victim.TakeDamage(dinfo);
        }

        private void XProb(IntVec3 target, IntVec3 from)
        {
            float hyp = 0;
            float angleRad = 0;
            float angleDeg = 0;

            hyp = Mathf.Sqrt((Mathf.Pow(from.x - target.x, 2)) + (Mathf.Pow(from.z - target.z, 2)));
            angleRad = Mathf.Asin(Mathf.Abs(from.x - target.x) / hyp);
            angleDeg = Mathf.Rad2Deg * angleRad;
            xProb = hyp > 0f ? angleDeg / 90 : 0f;
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