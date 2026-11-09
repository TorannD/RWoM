using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_ApplyAura : ClassProgression.Verb_SpiritTargetable
    {
        protected override bool TryCastShot()
        {
            Toggle(CasterPawn, null);
            return true;
        }

        protected override bool ApplyHostedEffect()
        {
            var soul = Ability?.Pawn;
            return ClassProgression.SpiritHostedAura.CanToggle(CasterPawn, soul, Ability?.Def as TMAbilityDef) && Toggle(soul, CasterPawn);
        }

        private bool Toggle(Pawn owner, Pawn host)
        {
            bool auraApplied = true;
            bool removedAura = RemoveAura(owner);
            if(!removedAura)
            {
                auraApplied = ApplyAura(owner);
                if (auraApplied)
                {
                    if (host != null)
                        owner.health.hediffSet.GetFirstHediffOfDef(((TMAbilityDef)Ability.Def).abilityHediff).TryGetComp<HediffComp_Aura>().BindHost(host, (TMAbilityDef)Ability.Def);
                    ClassProgression.ArcanistTrials.MarkAura(Ability);
                    ClassProgression.PaladinTrials.MarkAura(Ability);
                }
            }
            if (auraApplied)
            {
                ToggleAbilityAutocast(owner);
            }
            return auraApplied;
        }

        private bool RemoveAura(Pawn owner)
        {
            bool auraRemoved = false;
            Hediff hediff = null;
            for (int h = 0; h < owner.health.hediffSet.hediffs.Count; h++)
            {
                if (owner.health.hediffSet.hediffs[h].def == TorannMagicDefOf.TM_Shadow_AuraHD || owner.health.hediffSet.hediffs[h].def == TorannMagicDefOf.TM_RayOfHope_AuraHD ||
                    owner.health.hediffSet.hediffs[h].def == TorannMagicDefOf.TM_SoothingBreeze_AuraHD || owner.health.hediffSet.hediffs[h].def == TorannMagicDefOf.TM_InnerFire_AuraHD)
                {
                    hediff = owner.health.hediffSet.hediffs[h];
                    owner.health.RemoveHediff(hediff);
                    auraRemoved = true;
                    break;
                }
            }
            return auraRemoved;
        }

        private bool ApplyAura(Pawn owner)
        {
            CompAbilityUserMagic comp = owner.GetCompAbilityUserMagic();
            TMAbilityDef ability = (TMAbilityDef)this.Ability.Def;
            if (comp.maxMP >= ability.upkeepEnergyCost)
            {
                if (ability == TorannMagicDefOf.TM_Shadow)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_Shadow_AuraHD, .5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Shadow_I)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_Shadow_AuraHD, 1.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Shadow_II)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_Shadow_AuraHD, 2.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Shadow_III)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_Shadow_AuraHD, 3.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_RayOfHope_AuraHD, .5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope_I)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_RayOfHope_AuraHD, 1.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope_II)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_RayOfHope_AuraHD, 2.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope_III)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_RayOfHope_AuraHD, 3.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_SoothingBreeze_AuraHD, .5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe_I)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_SoothingBreeze_AuraHD, 1.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe_II)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_SoothingBreeze_AuraHD, 2.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe_III)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_SoothingBreeze_AuraHD, 3.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_InnerFire_AuraHD, .5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope_I)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_InnerFire_AuraHD, 1.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope_II)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_InnerFire_AuraHD, 2.5f);
                }
                else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope_III)
                {
                    HealthUtility.AdjustSeverity(owner, TorannMagicDefOf.TM_InnerFire_AuraHD, 3.5f);
                }
            }
            else
            {
                Messages.Message("TM_NotEnoughManaToSustain".Translate(
                                            owner.LabelShort,
                                            ability.label
                                        ), MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }

        private void ToggleAbilityAutocast(Pawn owner)
        {
            MagicPower magicPower = null;
            if (this.Ability.Def == TorannMagicDefOf.TM_Shadow)
            {
               magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shadow);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_Shadow_I)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shadow_I);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_Shadow_II)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shadow_II);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_Shadow_III)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shadow_III);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RayofHope);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope_I)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RayofHope_I);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope_II)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RayofHope_II);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_RayofHope_III)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RayofHope_III);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Soothe);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe_I)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Soothe_I);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe_II)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Soothe_II);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_Soothe_III)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Soothe_III);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_P_RayofHope);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope_I)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_P_RayofHope_I);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope_II)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_P_RayofHope_II);
            }
            else if (this.Ability.Def == TorannMagicDefOf.TM_P_RayofHope_III)
            {
                magicPower = owner.GetCompAbilityUserMagic().MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_P_RayofHope_III);
            }

            if (magicPower != null)
            {
                magicPower.autocast = !magicPower.autocast;
            }
        }
    }
}
