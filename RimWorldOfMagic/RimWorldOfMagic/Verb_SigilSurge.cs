using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_SigilSurge : ClassProgression.Verb_SpiritTargetable
    {
        protected override bool TryCastShot()
        {
            ApplyHostedEffect();
            bool result = false;
            PostCastShot(result,out result);
            return result;
        }

        protected override bool ApplyHostedEffect()
        {
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn, Ability);
            var comp = owner?.GetCompAbilityUserMagic();
            if (currentTarget.Pawn != CasterPawn || comp?.IsMagicUser != true) return false;
            comp.sigilSurging = !comp.sigilSurging;
            for (int i = 0; i < 16; i++)
            {
                float direction = Rand.Range(0, 360);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi_Yellow, caster.DrawPos, caster.Map, Rand.Range(.1f, .5f), 0.2f, .02f, .1f, 0, Rand.Range(5, 8), direction, direction);
            }
            FleckMaker.ThrowLightningGlow(caster.DrawPos, caster.Map, 1.2f);
            return true;
        }
    }
}
