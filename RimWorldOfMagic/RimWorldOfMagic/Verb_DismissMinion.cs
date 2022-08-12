using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_DismissMinion : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if(comp.IsMagicUser)
            {
                if(comp.summonedMinions.Count > 0)
                {
                    TMPawnSummoned minion = (TMPawnSummoned)comp.summonedMinions[0];
                    minion.TicksLeft = 1;
                }
                else
                {
                    Messages.Message("TM_NoMinionToDismiss".Translate(
                            this.CasterPawn.LabelShort
                        ), MessageTypeDefOf.RejectInput);
                }
            }
            return true;
        }
    }
}
