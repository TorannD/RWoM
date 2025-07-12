using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using AbilityUser;
using System.Linq;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Verb_PistolWhip : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = this.CasterPawn;            
            if (caster != null && this.currentTarget != null && this.currentTarget.Thing != null && currentTarget.Thing is Pawn)
            { 
                Pawn pawn = this.currentTarget.Thing as Pawn;
                CompAbilityUserMight comp = caster.GetCompAbilityUserMight();
                MightPowerSkill ver = comp.MightData.MightPowerSkill_PistolSpec.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PistolSpec_ver");
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Stun, Mathf.RoundToInt(comp.weaponDamage * TorannMagicDefOf.TM_PistolWhip.weaponDamageFactor + ver.level), 4, (float)-1, this.CasterPawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                BodyPartRecord hitPart = pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, BodyPartHeight.Undefined, BodyPartDepth.Outside, null);
                dinfo.SetHitPart(hitPart);
                DamageInfo dinfo2 = new DamageInfo(DamageDefOf.Blunt, Mathf.RoundToInt((Rand.Range(6f, 8f) + ver.level) * comp.mightPwr), 2, -1.0f, caster, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);

                Vector3 strikeStartVec = caster.DrawPos;
                strikeStartVec.z += .5f;
                Vector3 angle = TM_Calc.GetVector(strikeStartVec, pawn.DrawPos);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Strike, strikeStartVec, this.CasterPawn.Map, .5f, .1f, .05f, .1f, 0, 10f, (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(), (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat());

                pawn.TakeDamage(dinfo2);
                pawn.TakeDamage(dinfo);

                if (Rand.Chance(.35f + (.05f * ver.level)) && pawn.equipment != null && pawn.equipment.Primary != null)
                {
                    ThingWithComps droppedEq = null;
                    pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out droppedEq, caster.Position, true);
                }
                
            }

            return true;

        }
    }
}
