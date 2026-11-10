using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public class Projectile_ArrowStorm : Projectile_SniperRound
    {
        // Preserve the public entry point used by legacy callers.
        public void Initialize(Map map)
        {
            if (!shotBound && launcher is Pawn body) Capture(body,body);
        }

        protected override void CaptureWeaponDamage(Pawn body) => Capture(body,skillOwner);

        private void Capture(Pawn body,Pawn source)
        {
            shotBound=true;
            skillOwner=source;
            var weapon=body?.equipment?.Primary;
            firedWeapon=weapon?.def;
            if (weapon==null || source==null) return;
            int rank=TM_Calc.GetSkillPowerLevel(source,TorannMagicDefOf.TM_ArrowStorm);
            shotDamage=Damage(body,source,rank);
            accuracy=Accuracy(weapon,TM_Calc.GetSkillVersatilityLevel(source,TorannMagicDefOf.TM_ArrowStorm));
        }

        protected override void Impact(Thing hitThing,bool blockedByShield=false)
        {
            // A legacy in-flight arrow did not record its weapon. Recover what is available
            // before vanilla impact removes the projectile; new shots are already bound.
            Initialize(Map);
            base.Impact(hitThing,blockedByShield);
            if (blockedByShield || hitThing is not Pawn victim || !victim.Spawned || victim.Dead ||
                shotDamage<=0 || !Rand.Chance(accuracy)) return;
            damageEntities(victim,null,shotDamage,TorannMagicDefOf.Arrow);
            if (victim.Spawned) TM_MoteMaker.ThrowBloodSquirt(victim.DrawPos,victim.Map,1f);
        }

        private static float Accuracy(ThingWithComps weapon,int rank) =>
            Mathf.Clamp01(.8f*weapon.GetStatValue(StatDefOf.AccuracyMedium,true)+.05f*rank);

        public static float GetWeaponAccuracy(Pawn pawn) => pawn?.equipment?.Primary is ThingWithComps weapon ?
            Accuracy(weapon,TM_Calc.GetSkillVersatilityLevel(pawn,TorannMagicDefOf.TM_ArrowStorm)) : 0f;

        public static int GetWeaponDmg(Pawn pawn) => pawn?.equipment?.Primary!=null ?
            Damage(pawn,pawn,TM_Calc.GetSkillPowerLevel(pawn,TorannMagicDefOf.TM_ArrowStorm)) : 0;

        private static int Damage(Pawn body,Pawn owner,int rank)
        {
            var comp=owner.GetCompAbilityUserMight();
            var weapon=body.equipment.Primary;
            float damage=body==owner && comp?.weaponDamage>1f ? comp.weaponDamage :
                TM_Calc.RangedWeaponDamage(weapon,weapon.def.Verbs?.FirstOrDefault(),comp?.mightPwr??1f);
            if (!body.IsColonist && ModOptions.Settings.Instance.AIHardMode) damage+=8f;
            return Mathf.Clamp(Mathf.RoundToInt(damage*(1f+.1f*rank)*TorannMagicDefOf.TM_ArrowStorm.weaponDamageFactor),0,50);
        }

        public void damageEntities(Pawn victim,BodyPartRecord hitPart,int amt,DamageDef type)
        {
            amt=(int)(amt*Rand.Range(.7f,1.3f));
            var info=new DamageInfo(type,amt,0,-1,launcher as Pawn,hitPart,null,DamageInfo.SourceCategory.ThingOrUnknown);
            info.SetAllowDamagePropagation(false);
            victim.TakeDamage(info);
        }
    }
}
