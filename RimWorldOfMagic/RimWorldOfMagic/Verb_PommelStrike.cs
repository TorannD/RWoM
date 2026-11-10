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
    public class Verb_PommelStrike : Verb_UseAbility
    {


        protected override bool TryCastShot()
        {
            if (currentTarget.Thing is Pawn target)
                Strike(CasterPawn, target, CasterPawn.GetCompAbilityUserMight());
            return true;
        }

        internal static void Strike(Pawn body, Pawn target, CompAbilityUserMight owner)
        {
            var damage = new DamageInfo(DamageDefOf.Stun, 10, 0, -1, body);
            int power = owner?.MightData.MightPowerSkill_FieldTraining
                .FirstOrDefault(skill => skill.label == "TM_FieldTraining_pwr")?.level ?? 0;
            float factor = power >= 2 ? 4f : 2f;
            if (Rand.Chance(target.health.hediffSet.PainTotal * factor))
            {
                var part = target.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.Spine).RandomElementWithFallback();
                var weapon = body.equipment?.Primary;
                float amount = part != null && weapon != null ? 3 * factor : 4;
                float penetration = part == null ? 2 : weapon == null ? 12 : 6 * factor;
                damage = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_DisablingBlow, amount, penetration, -1, body,
                    part, part != null ? weapon?.def : null, DamageInfo.SourceCategory.ThingOrUnknown, target);
                var start = body.DrawPos;
                start.z += .7f;
                var angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(start, target.DrawPos)).ToAngleFlat();
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Strike, start, body.Map, .5f, .1f, .05f, .1f, 0, 10f, angle, angle);
            }
            target.TakeDamage(damage);
        }
    }
}
