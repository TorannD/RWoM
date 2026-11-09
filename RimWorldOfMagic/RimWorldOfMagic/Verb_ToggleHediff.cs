using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_ToggleHediff : ClassProgression.Verb_SpiritTargetable
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(caster, Ability);
            if (owner == null) return false;

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
                        if (owner != caster && hd.TryGetComp<HediffComp_SuppressiveAura>()?.SourceId != owner.thingIDNumber) return false;
                        caster.health.RemoveHediff(hd);
                    }
                    else
                    {
                        var empathTrial = ClassProgression.EmpathTrials.CaptureAura(this.Ability, caster);
                        HealthUtility.AdjustSeverity(caster, hdDef, hdDef.initialSeverity);
                        if (owner != caster)
                        {
                            var emitter = caster.health.hediffSet.GetFirstHediffOfDef(hdDef)?.TryGetComp<HediffComp_SuppressiveAura>();
                            if (emitter != null) emitter.SourceId = owner.thingIDNumber;
                        }
                        ClassProgression.EmpathTrials.CompleteAura(empathTrial);
                        if (caster.Map != null)
                        {
                            FleckMaker.ThrowLightningGlow(caster.DrawPos, caster.Map, 1f);
                            FleckMaker.ThrowDustPuff(caster.Position, caster.Map, 1f);
                        }
                    }

                    CompAbilityUserMagic magicComp = owner.GetCompAbilityUserMagic();
                    if(magicComp != null && magicComp.MagicData != null)
                    {
                        MagicPower mp = magicComp.MagicData.ReturnMatchingMagicPower(ability);
                        if(mp != null)
                        {
                            mp.autocast = caster.health.hediffSet.HasHediff(hdDef);
                        }
                    }
                    CompAbilityUserMight mightComp = owner.GetCompAbilityUserMight();
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
