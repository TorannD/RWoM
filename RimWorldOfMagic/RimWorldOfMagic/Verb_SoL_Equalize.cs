using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;

namespace TorannMagic
{
    public class Verb_SoL_Equalize : Verb_UseAbility  
    {
        
        protected override bool TryCastShot()
        {
            Pawn pawn = this.CasterPawn;
            Map map = this.CasterPawn.Map;
            if (pawn != null && !pawn.Downed)
            {
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                Hediff hd = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightCapacitanceHD);
                if(comp != null && comp.SoL != null && hd != null)
                {
                    HediffComp_LightCapacitance hdlc = hd.TryGetComp<HediffComp_LightCapacitance>();
                    if(hdlc != null)
                    {
                        float val = (comp.SoL.LightEnergy +hdlc.LightEnergy)/2f;
                        comp.SoL.LightEnergy = val;
                        hdlc.LightEnergy = val;
                        FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1.2f);
                        FleckMaker.ThrowLightningGlow(comp.SoL.DrawPos, comp.SoL.Map, 1.2f);
                    }
                }
            }
             
            this.burstShotsLeft = 0;
            return false;
        }
    }
}
