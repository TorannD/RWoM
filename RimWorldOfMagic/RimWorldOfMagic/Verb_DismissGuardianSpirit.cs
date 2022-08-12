using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_DismissGuardianSpirit : Verb_UseAbility
    {        
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if(comp.IsMagicUser)
            {
                if(comp.bondedSpirit != null)
                {
                    comp.bondedSpirit.SetFaction(Find.FactionManager.FirstFactionOfDef(TorannMagicDefOf.TM_SkeletalFaction), null);
                    comp.bondedSpirit.Kill(null);                    
                }
                else
                {
                    Messages.Message("TM_NoGuardianSpiritToDismiss".Translate(
                            this.CasterPawn.LabelShort
                        ), MessageTypeDefOf.RejectInput);
                }
            }
            return true;
        }
    }
}
