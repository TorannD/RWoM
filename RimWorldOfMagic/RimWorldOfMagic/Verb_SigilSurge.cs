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
            CompAbilityUserMagic comp = this.CasterPawn.GetComp<CompAbilityUserMagic>();

            if (comp.IsMagicUser)
            {

            }

            this.PostCastShot(flag, out flag);
            return flag;
        }
    }
}
