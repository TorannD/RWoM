using RimWorld;
using System;
using System.Linq;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;

namespace TorannMagic
{
    public class Verb_MagicWardrobe : Verb_SB 
    {
        
        protected override bool TryCastShot()
        {
            bool result = false;
            Pawn pawn = this.CasterPawn;
            Map map = this.CasterPawn.Map;
            
            if (pawn != null && !pawn.Downed)
            {
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                if(comp != null && comp.MagicWardrobe != null && pawn.apparel != null)
                {                    
                    List<ThingWithComps> tmpHolder = new List<ThingWithComps>();
                    tmpHolder.Clear();
                    tmpHolder.AddRange(pawn.apparel.WornApparel);
                    for(int i = 0; i < tmpHolder.Count; i++)
                    {                       
                        pawn.apparel.Remove(tmpHolder[i] as Apparel);
                    }                    
                    List<ThingWithComps> tmpApparel = new List<ThingWithComps>();
                    tmpApparel.Clear();
                    tmpApparel.AddRange(comp.MagicWardrobe);
                    for(int i = 0; i < tmpApparel.Count; i++)
                    {                        
                        comp.MagicWardrobe.Remove(tmpApparel[i]);                       
                    }
                    comp.MagicWardrobe.Clear();
                    for(int i =0; i < tmpApparel.Count; i++)
                    {                       
                        pawn.apparel.Wear(tmpApparel[i] as Apparel);
                        pawn.apparel.Notify_ApparelChanged();
                        pawn.apparel.Lock(tmpApparel[i] as Apparel);
                    }
                    for (int i = 0; i < tmpHolder.Count; i++)
                    {                       
                        comp.MagicWardrobe.TryAddOrTransfer(tmpHolder[i]);
                    }
                    TM_Action.TransmutateEffects(pawn.Position, pawn);
                }
            }

            this.burstShotsLeft = 0;
            return result;
        }
    }
}
