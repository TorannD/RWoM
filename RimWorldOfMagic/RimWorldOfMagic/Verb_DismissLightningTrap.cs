using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_DismissLightningTrap : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if(comp.IsMagicUser)
            {
                if(comp.lightningTraps.Count > 0)
                {
                    Thing lightningTrap = comp.lightningTraps[0];
                    lightningTrap.Destroy();
                }
                else
                {

                }
            }
            return true;
        }
    }
}
