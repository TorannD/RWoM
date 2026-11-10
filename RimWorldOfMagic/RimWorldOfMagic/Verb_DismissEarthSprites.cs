using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_DismissEarthSprites : ClassProgression.Verb_SpiritTargetable
    {
        protected override bool ApplyHostedEffect()
        {
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn, Ability);
            var comp = owner?.GetCompAbilityUserMagic();
            if (comp == null || comp.earthSpriteType == 0) return false;
            comp.ClearEarthSprites();
            return true;
        }

        protected override bool TryCastShot()
        {
            if (!ApplyHostedEffect())
                Messages.Message("TM_NoSpritesToDismiss".Translate(CasterPawn.LabelShort), MessageTypeDefOf.RejectInput);
            PostCastShot(false, out bool fired);
            return fired;
        }
    }
}
