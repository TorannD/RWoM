using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_BrandingSiphon : HediffComp_BrandingBase
    {
        public override void DoSigilAction(bool surging = false, bool draining = false)
        {
            if(parent.Severity >= .1f)
            {
                CompAbilityUserMagic comp = this.Pawn.GetCompAbilityUserMagic();
                CompAbilityUserMagic branderComp = BranderPawn.GetCompAbilityUserMagic();
                if(comp != null && comp.Mana != null && branderComp != null && branderComp.Mana != null)
                {
                    float pwrAmt = .005f * this.parent.Severity;
                    float transferredMana = comp.Mana.CurLevel > pwrAmt ? pwrAmt : comp.Mana.CurLevel;
                    comp.Mana.CurLevel -= transferredMana;
                    branderComp.Mana.CurLevel += transferredMana;
                }
            }
        }
    }
}
