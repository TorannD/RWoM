using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_DispelBranding : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            bool flag = false;
            CompAbilityUserMagic comp = this.CasterPawn.GetComp<CompAbilityUserMagic>();

            if (comp.IsMagicUser)
            {
                if (comp.Brandings.Count > 0)
                {
                    foreach(TMDefs.Branding br in comp.Brandings)
                    {
                        br.pawn.health.RemoveHediff(br.pawn.health.hediffSet.GetFirstHediffOfDef(br.hediffDef));
                    }
                    comp.brandings.Clear();
                }
            }

            this.PostCastShot(flag, out flag);
            return flag;
        }
    }
}
