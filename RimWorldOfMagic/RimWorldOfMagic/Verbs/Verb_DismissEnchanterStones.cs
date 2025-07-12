using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_DismissEnchanterStones : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if(comp.IsMagicUser)
            {
                if(comp.enchanterStones.Count > 0)
                {
                    Thing stone = comp.enchanterStones[0];
                    stone.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    Messages.Message("Found no enchanter stones to destroy.", MessageTypeDefOf.RejectInput);
                }
            }
            return true;
        }
    }
}
