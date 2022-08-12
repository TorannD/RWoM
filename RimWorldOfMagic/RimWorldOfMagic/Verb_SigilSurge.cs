using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_SigilSurge : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            bool flag = false;
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();

            if (comp.IsMagicUser)
            {
                comp.sigilSurging = !comp.sigilSurging;
                for (int i = 0; i < 16; i++)
                {
                    float direction = Rand.Range(0, 360);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi_Yellow, caster.DrawPos, caster.Map, Rand.Range(.1f, .5f), 0.2f, .02f, .1f, 0, Rand.Range(5, 8), direction, direction);
                }
                FleckMaker.ThrowLightningGlow(caster.DrawPos, caster.Map, 1.2f);
            }

            this.PostCastShot(flag, out flag);
            return flag;
        }
    }
}
