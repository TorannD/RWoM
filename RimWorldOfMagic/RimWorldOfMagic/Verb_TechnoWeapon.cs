using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;
using Verse.AI;


namespace TorannMagic
{
    public class Verb_TechnoWeapon : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Thing thing = this.currentTarget.Cell.GetFirstItem(caster.Map);
            TM_Action.DoAction_TechnoWeaponCopy(caster, thing);
            
            this.burstShotsLeft = 0;
            return true;
        }
    }
}
