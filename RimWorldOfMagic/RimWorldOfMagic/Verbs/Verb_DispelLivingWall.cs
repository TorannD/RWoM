using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_DispelLivingWall : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            bool flag = false;
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();

            if (comp.IsMagicUser)
            {
                if (comp.livingWall != null && comp.livingWall.Spawned)
                {
                    comp.livingWall.Destroy(DestroyMode.Vanish);
                    comp.livingWall = null;
                }
            }

            this.PostCastShot(flag, out flag);
            return flag;
        }
    }
}
