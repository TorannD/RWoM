using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_ToggleHediff : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            bool flag = caster != null && !caster.Dead;
            if (flag)
            {
                TMAbilityDef ability = (TMAbilityDef)this.Ability.Def;
                if (ability != null && ability.abilityHediff != null)
                {
                    HediffDef hdDef = ability.abilityHediff;
                    if (caster.health.hediffSet.HasHediff(hdDef))
                    {
                        Hediff hd = caster.health.hediffSet.GetFirstHediffOfDef(hdDef);
                        caster.health.RemoveHediff(hd);
                    }
                    else
                    {
                        HealthUtility.AdjustSeverity(caster, hdDef, hdDef.initialSeverity);
                        if (caster.Map != null)
                        {
                            FleckMaker.ThrowLightningGlow(caster.DrawPos, caster.Map, 1f);
                            FleckMaker.ThrowDustPuff(caster.Position, caster.Map, 1f);
                        }
                    }

                    CompAbilityUserMagic magicComp = caster.GetCompAbilityUserMagic();
                    if(magicComp != null && magicComp.MagicData != null)
                    {
                        MagicPower mp = magicComp.MagicData.ReturnMatchingMagicPower(ability);
                        if(mp != null)
                        {
                            mp.autocast = caster.health.hediffSet.HasHediff(hdDef);
                        }
                    }
                    CompAbilityUserMight mightComp = caster.GetCompAbilityUserMight();
                    if (mightComp != null && mightComp.MightData != null)
                    {
                        MightPower mp = mightComp.MightData.ReturnMatchingMightPower(ability);
                        if (mp != null)
                        {
                            mp.autocast = caster.health.hediffSet.HasHediff(hdDef);
                        }
                    }

                }
                else
                {
                    Log.Warning("Unrecognized ability or no hediff assigned for this ability.");
                }
                
            }
            return true;
        }
    }
}
