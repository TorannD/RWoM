using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TorannMagic
{
    class Verb_Recall : Verb_UseAbility
    {
        private int pwrVal = 0;
        CompAbilityUserMagic comp;
        Map map;

        protected override bool TryCastShot()
        {
            bool result = false;
            map = this.CasterPawn.Map;
            comp = this.CasterPawn.GetCompAbilityUserMagic();

            if (this.CasterPawn != null && !this.CasterPawn.Downed && comp != null && comp.recallSet)
            {
                TM_Action.DoRecall(this.CasterPawn, comp, false);
            }
            else
            {
                Log.Warning("failed to TryCastShot");
            }

            this.burstShotsLeft = 0;
            return result;
        }

       
    }
}
