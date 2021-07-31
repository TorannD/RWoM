using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_DismissHeater : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            CompAbilityUserMagic comp = pawn.GetComp<CompAbilityUserMagic>();
            if(comp.IsMagicUser)
            {
                if(comp.summonedHeaters.Count > 0)
                {
                    Thing heater = comp.summonedHeaters[0];
                    heater.Destroy();
                }
                else
                {

                }
            }
            return true;
        }
    }
}
