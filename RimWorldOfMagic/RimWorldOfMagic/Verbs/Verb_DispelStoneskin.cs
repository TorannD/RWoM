using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_DispelStoneskin : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            bool flag = false;
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();

            if (comp.IsMagicUser)
            {
                if (comp.stoneskinPawns.Count > 0)
                {
                    for(int i =0; i < comp.stoneskinPawns.Count; i++)
                    {
                        Pawn dispellingPawn = comp.stoneskinPawns[i];
                        Hediff stoneskin = dispellingPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_StoneskinHD"), false);
                        dispellingPawn.health.RemoveHediff(stoneskin);
                    }
                    
                }
            }

            this.PostCastShot(flag, out flag);
            return flag;
        }
    }
}
