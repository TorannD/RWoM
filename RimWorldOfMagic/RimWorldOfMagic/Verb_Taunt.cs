using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;
using Verse.Sound;
using Verse.AI;


namespace TorannMagic
{
    public class Verb_Taunt : Verb_UseAbility
    {
        float radius = 15f;
        float tauntChance = .6f;
        int targetsMax = 5;

        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            bool flag = pawn != null && !pawn.Dead;
            if (flag)
            {

                CompAbilityUserMight comp = caster.GetComp<CompAbilityUserMight>();
                //int verVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_Custom, "TM_Taunt", "_ver", true);
                //int pwrVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_Custom, "TM_Taunt", "_pwr", true);
                int verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
                int pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
                radius += (2f * verVal);
                tauntChance += (pwrVal * .05f);
                targetsMax += pwrVal;

                SoundInfo info = SoundInfo.InMap(new TargetInfo(caster.Position, caster.Map, false), MaintenanceType.None);
                if(this.CasterPawn.gender == Gender.Female)
                {
                    info.pitchFactor = Rand.Range(1.1f, 1.3f); 
                }
                else
                {
                    info.pitchFactor = Rand.Range(.7f, .9f);
                }
                TorannMagicDefOf.TM_Roar.PlayOneShot(info);
                Effecter RageWave = TorannMagicDefOf.TM_RageWaveED.Spawn();
                RageWave.Trigger(new TargetInfo(caster.Position, caster.Map, false), new TargetInfo(caster.Position, caster.Map, false));
                RageWave.Cleanup();
                TM_Action.SearchAndTaunt(caster, this.radius, targetsMax, tauntChance);                
            }

            return true;
        }        
    }
}