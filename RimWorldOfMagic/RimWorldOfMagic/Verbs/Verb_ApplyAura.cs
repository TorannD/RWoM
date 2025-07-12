using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_ApplyAura : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            bool auraApplied = true;
            bool removedAura = RemoveAura();
            if(!removedAura)
            {
                auraApplied = ApplyAura();
            }
            if (auraApplied)
            {
                ToggleAbilityAutocast();
            }
            return true;
        }

        private bool RemoveAura()
        {
            bool auraRemoved = false;
            Hediff hediff = null;
            for (int h = 0; h < this.CasterPawn.health.hediffSet.hediffs.Count; h++)
            {
                if (this.CasterPawn.health.hediffSet.hediffs[h].def == TorannMagicDefOf.TM_Shadow_AuraHD || this.CasterPawn.health.hediffSet.hediffs[h].def == TorannMagicDefOf.TM_RayOfHope_AuraHD ||
                    this.CasterPawn.health.hediffSet.hediffs[h].def == TorannMagicDefOf.TM_SoothingBreeze_AuraHD || this.CasterPawn.health.hediffSet.hediffs[h].def == TorannMagicDefOf.TM_InnerFire_AuraHD)
                {
                    hediff = this.CasterPawn.health.hediffSet.hediffs[h];
                    this.CasterPawn.health.RemoveHediff(hediff);
                    auraRemoved = true;
                    break;
                }
            }
            return auraRemoved;
        }

        private bool ApplyAura()
        {
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
            TMAbilityDef ability = (TMAbilityDef)this.Ability.Def;
            if (comp.maxMP >= ability.upkeepEnergyCost)
            {
                if (ability == TorannMagicDefOf.TM_Shadow)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_Shadow_AuraHD, .5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Shadow_I)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_Shadow_AuraHD, 1.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Shadow_II)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_Shadow_AuraHD, 2.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Shadow_III)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_Shadow_AuraHD, 3.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_RayOfHope_AuraHD, .5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope_I)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_RayOfHope_AuraHD, 1.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope_II)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_RayOfHope_AuraHD, 2.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope_III)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_RayOfHope_AuraHD, 3.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_SoothingBreeze_AuraHD, .5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe_I)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_SoothingBreeze_AuraHD, 1.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe_II)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_SoothingBreeze_AuraHD, 2.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe_III)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_SoothingBreeze_AuraHD, 3.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_InnerFire_AuraHD, .5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope_I)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_InnerFire_AuraHD, 1.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope_II)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_InnerFire_AuraHD, 2.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope_III)
                {
                    HealthUtility.AdjustSeverity(this.CasterPawn, TorannMagicDefOf.TM_InnerFire_AuraHD, 3.5f);
                }
            }
            else
            {
                Messages.Message("TM_NotEnoughManaToSustain".Translate(
                                            this.CasterPawn.LabelShort,
                                            ability.label
                                        ), MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }

        private void ToggleAbilityAutocast()
        {
            MagicPower magicPower = null;
            if (this.Ability.Def == TorannMagicDefOf.TM_Shadow)
            {
               magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shadow);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_Shadow_I)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shadow_I);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_Shadow_II)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shadow_II);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_Shadow_III)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shadow_III);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RayofHope);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope_I)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RayofHope_I);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope_II)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RayofHope_II);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope_III)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RayofHope_III);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Soothe);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe_I)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Soothe_I);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe_II)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Soothe_II);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe_III)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Soothe_III);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_P_RayofHope);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope_I)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_P_RayofHope_I);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope_II)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_P_RayofHope_II);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope_III)
            {
                magicPower = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_P_RayofHope_III);
            }

            if (magicPower != null)
            {
                magicPower.autocast = !magicPower.autocast;
            }
        }
    }
}
