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
            Pawn pawn = this.currentTarget.Thing as Pawn;

            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if(comp.IsMagicUser)
            {
                if(comp.bondedSpirit.Value != null)
                {
                    comp.bondedSpirit.Value.SetFaction(Find.FactionManager.FirstFactionOfDef(TorannMagicDefOf.TM_SkeletalFaction));
                    comp.bondedSpirit.Value.Kill(null);
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
