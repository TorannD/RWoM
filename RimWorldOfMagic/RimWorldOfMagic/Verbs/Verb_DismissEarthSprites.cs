using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_DismissEarthSprites : Verb_UseAbility
    {

        protected override bool TryCastShot()
        {
            bool flag = false;
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();

            if (comp.IsMagicUser)
            {
                if (comp.earthSpriteType != 0)
                {
                    comp.earthSpriteType = 0;
                    comp.earthSpriteMap = null;
                    comp.earthSprites = IntVec3.Invalid;
                }
                else
                {
                    Messages.Message("TM_NoSpritesToDismiss".Translate(
                            this.CasterPawn.LabelShort
                        ), MessageTypeDefOf.RejectInput);
                }
            }

            this.PostCastShot(flag, out flag);
            return flag;
        }
    }
}
