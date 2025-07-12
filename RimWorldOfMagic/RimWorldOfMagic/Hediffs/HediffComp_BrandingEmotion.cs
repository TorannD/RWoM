using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_BrandingEmotion : HediffComp_BrandingBase
    {
        public override void DoSigilAction(bool surging = false, bool draining = false)
        {
            if(parent.Severity >= .1f)
            {
                float sev = surging ? 2 * this.parent.Severity : this.parent.Severity;

                if(base.Pawn.InMentalState && Rand.Chance(sev * .05f))
                {
                    this.Pawn.MentalState.RecoverFromState();
                }

                if(this.Pawn.needs != null && this.Pawn.needs.mood != null)
                {
                    this.Pawn.needs.mood.CurLevel += sev * .025f;
                }
            }
        }
    }
}