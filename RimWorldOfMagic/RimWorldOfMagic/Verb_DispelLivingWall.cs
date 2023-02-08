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
                if (comp.livingWall.Value != null && comp.livingWall.Value.Spawned)
                {
                    comp.livingWall.Value.Destroy(DestroyMode.Vanish);
                    comp.livingWall.Set(null);
                }
            }

            this.PostCastShot(flag, out flag);
            return flag;
        }
    }
}
