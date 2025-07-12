using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_CommanderAuras : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            bool removedAura = RemoveAura();
            if(!removedAura)
            {
                ApplyAura();
            }
            ToggleAbilityAutocast();
            return true;
        }

        private bool RemoveAura()
        {
            bool auraRemoved = false;
            Hediff hediff = null;
            for (int h = 0; h < this.CasterPawn.health.hediffSet.hediffs.Count; h++)
            {
                if (this.Ability.Def == TorannMagicDefOf.TM_ProvisionerAura && this.CasterPawn.health.hediffSet.hediffs[h].def == TorannMagicDefOf.TM_ProvisionerAuraHD)
                {
                    hediff = this.CasterPawn.health.hediffSet.hediffs[h];
                    this.CasterPawn.health.RemoveHediff(hediff);
                    auraRemoved = true;
                    break;
                }
                if (this.Ability.Def == TorannMagicDefOf.TM_TaskMasterAura && this.CasterPawn.health.hediffSet.hediffs[h].def == TorannMagicDefOf.TM_TaskMasterAuraHD)
                {
                    hediff = this.CasterPawn.health.hediffSet.hediffs[h];
                    this.CasterPawn.health.RemoveHediff(hediff);
                    auraRemoved = true;
                    break;
                }
                if (this.Ability.Def == TorannMagicDefOf.TM_CommanderAura && this.CasterPawn.health.hediffSet.hediffs[h].def == TorannMagicDefOf.TM_CommanderAuraHD)
                {
                    hediff = this.CasterPawn.health.hediffSet.hediffs[h];
                    this.CasterPawn.health.RemoveHediff(hediff);
                    auraRemoved = true;
                    break;
                }
            }
            return auraRemoved;
        }

        private void ApplyAura()
        {
            if (this.Ability.Def == TorannMagicDefOf.TM_ProvisionerAura)
            {
                HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_ProvisionerAuraHD, .5f);
            }
            else if(this.Ability.Def == TorannMagicDefOf.TM_TaskMasterAura)
            {
                HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_TaskMasterAuraHD, .5f);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_CommanderAura)
            {
                HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_CommanderAuraHD, .5f);
            }            
        }

        private void ToggleAbilityAutocast()
        {
            MightPower mightPower = null;
            if (this.Ability.Def == TorannMagicDefOf.TM_ProvisionerAura)
            {
               mightPower = this.CasterPawn.GetCompAbilityUserMight().MightData.MightPowersC.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_ProvisionerAura);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_TaskMasterAura)
            {
                mightPower = this.CasterPawn.GetCompAbilityUserMight().MightData.MightPowersC.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_TaskMasterAura);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_CommanderAura)
            {
                mightPower = this.CasterPawn.GetCompAbilityUserMight().MightData.MightPowersC.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_CommanderAura);
            }            

            if (mightPower != null)
            {
                mightPower.autocast = !mightPower.autocast;
            }
        }
    }
}
