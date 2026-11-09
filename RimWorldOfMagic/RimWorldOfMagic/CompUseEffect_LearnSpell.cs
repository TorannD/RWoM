using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace TorannMagic
{
    public class CompUseEffect_LearnSpell : CompUseEffect
    {
        private static readonly HashSet<MagicPower> sharedLearning = new HashSet<MagicPower>();

        // Called only after the original noncustom mage and required-trait checks.
        // Native DoEffect is the completed reading job's payment boundary.
        private bool TryCompleteEnrolledSharedScroll(Pawn user, CompAbilityUserMagic comp)
        {
            if (comp.customClass != null || !ClassProgression.ClassIdentity.TryGetState(user, out _)) return false;
            var owner = ClassProgression.ClassSpellAccess.SharedScrollOwner(comp.MagicData, parent.def);
            if (owner == null) return false;
            if (!ClassProgression.ClassSpellAccess.CanLearnFromScroll(user, owner, parent.def)) return false;
            if (!sharedLearning.Add(owner)) return true;
            try
            {
                if (owner.learned) return true;
                bool flagOwned = parent.def.defName == "SpellOf_Rain" ? comp.spell_Rain
                    : parent.def.defName == "SpellOf_Blink" ? comp.spell_Blink
                    : parent.def.defName == "SpellOf_Teleport" ? comp.spell_Teleport
                    : parent.def.defName == "SpellOf_Heal" ? comp.spell_Heal : comp.spell_SummonMinion;
                if (!flagOwned)
                {
                    if (parent.Destroyed || parent.stackCount < 1 || !parent.def.destroyable) return true;
                    var originalHolder = parent.holdingOwner;
                    var payment = parent.SplitOff(1);
                    if (payment == null) return true;
                    try { payment.Destroy(DestroyMode.Vanish); }
                    catch
                    {
                        // A callback may throw after destruction already committed payment.
                        if (!payment.Destroyed)
                        {
                            RestoreUnpaidScroll(user, payment, originalHolder);
                            throw;
                        }
                    }
                    if (!payment.Destroyed)
                    {
                        RestoreUnpaidScroll(user, payment, originalHolder);
                        return true;
                    }
                }
                // Persistent ownership is committed after payment, before runtime callbacks.
                // Legacy flag-only grants are repaired without charging a second time.
                owner.learned = true;
                switch (parent.def.defName)
                {
                    case "SpellOf_Rain": comp.spell_Rain = true; break;
                    case "SpellOf_Blink": comp.spell_Blink = true; break;
                    case "SpellOf_Teleport": comp.spell_Teleport = true; break;
                    case "SpellOf_Heal": comp.spell_Heal = true; break;
                    case "SpellOf_SummonMinion": comp.spell_SummonMinion = true; break;
                }
                ClassProgression.ClassSpellAccess.RestoreLearned(comp);
                return true;
            }
            finally { sharedLearning.Remove(owner); }
        }

        private void RestoreUnpaidScroll(Pawn user, Thing payment, ThingOwner originalHolder)
        {
            if (payment.Destroyed || payment.Spawned || payment.holdingOwner != null) return;
            // Keep the actual split unit, without invoking the failing Destroy callback
            // again through stack absorption. A real holder supplies save ownership.
            if (originalHolder != null)
            {
                try { originalHolder.TryAdd(payment, false); }
                catch { if (payment.holdingOwner != null || payment.Spawned) return; }
                if (payment.holdingOwner != null || payment.Spawned) return;
            }
            if (user.inventory == null) user.inventory = new Pawn_InventoryTracker(user);
            user.inventory.innerContainer.TryAdd(payment, false);
            if (payment.holdingOwner == null)
                throw new System.InvalidOperationException("Could not retain the unpaid shared scroll in its reader's inventory.");
        }

        private bool TryCompleteEnrolledBrightmageScroll(Pawn user, CompAbilityUserMagic comp)
        {
            if (parent.def?.defName != "SpellOf_SpiritOfLight" || !ClassProgression.ClassIdentity.TryGetState(user, out _)) return false;
            var owner = comp?.MagicData?.MagicPowersBrightmage.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SpiritOfLight));
            if (owner == null || owner.learned) return true;
            if (!ClassProgression.ClassIdentity.IsBrightmage(user)
                || !ClassProgression.ClassSpellAccess.CanLearnFromScroll(user, owner, parent.def)) return true;
            var required = parent.def.GetModExtension<DefModExtension_LearnAbilityRequiredTraits>()?.traits;
            if (required != null && required.Count > 0 && !required.Any(t => user.story?.traits?.HasTrait(t) == true
                || (t == TorannMagicDefOf.TM_Brightmage && ClassProgression.ClassIdentity.IsBrightmage(user)))) return true;
            if (!sharedLearning.Add(owner)) return true;
            try
            {
                if (parent.Destroyed || parent.stackCount < 1 || !parent.def.destroyable) return true;
                var holder = parent.holdingOwner;
                var payment = parent.SplitOff(1);
                if (payment == null) return true;
                try { payment.Destroy(DestroyMode.Vanish); }
                catch
                {
                    if (!payment.Destroyed) { RestoreUnpaidScroll(user, payment, holder); throw; }
                }
                if (!payment.Destroyed) { RestoreUnpaidScroll(user, payment, holder); return true; }
                owner.learned = true;
                ClassProgression.ClassSpellAccess.RestoreLearned(comp);
                return true;
            }
            finally { sharedLearning.Remove(owner); }
        }

        private bool TryCompleteEnrolledShamanScroll(Pawn user, CompAbilityUserMagic comp)
        {
            if (parent.def?.defName != "SpellOf_GuardianSpirit" || !ClassProgression.ClassIdentity.TryGetState(user, out _)) return false;
            var owner = comp?.MagicData?.MagicPowersShaman.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_GuardianSpirit));
            if (owner == null || owner.learned) return true;
            if (!ClassProgression.ClassIdentity.IsShaman(user)
                || !ClassProgression.ClassSpellAccess.CanLearnFromScroll(user, owner, parent.def)) return true;
            var required = parent.def.GetModExtension<DefModExtension_LearnAbilityRequiredTraits>()?.traits;
            if (required != null && required.Count > 0 && !required.Any(t => user.story?.traits?.HasTrait(t) == true
                || (t == TorannMagicDefOf.TM_Shaman && ClassProgression.ClassIdentity.IsShaman(user)))) return true;
            if (!sharedLearning.Add(owner)) return true;
            try
            {
                if (parent.Destroyed || parent.stackCount < 1 || !parent.def.destroyable) return true;
                var holder = parent.holdingOwner;
                var payment = parent.SplitOff(1);
                if (payment == null) return true;
                try { payment.Destroy(DestroyMode.Vanish); }
                catch
                {
                    if (!payment.Destroyed) { RestoreUnpaidScroll(user, payment, holder); throw; }
                }
                if (!payment.Destroyed) { RestoreUnpaidScroll(user, payment, holder); return true; }
                owner.learned = true;
                ClassProgression.ClassSpellAccess.RestoreLearned(comp);
                return true;
            }
            finally { sharedLearning.Remove(owner); }
        }

        private bool TryCompleteEnrolledEmpathScroll(Pawn user, CompAbilityUserMagic comp)
        {
            if (parent.def?.defName != "SpellOf_FadeEmotions" || !ClassProgression.ClassIdentity.TryGetState(user, out _)) return false;
            var owner = ClassProgression.EmpathOwners.FadeOwner(comp?.MagicData);
            if (owner == null || owner.learned) return true;
            if (!ClassProgression.ClassIdentity.IsEmpath(user)
                || !ClassProgression.ClassSpellAccess.CanLearnFromScroll(user, owner, parent.def)) return true;
            var required = parent.def.GetModExtension<DefModExtension_LearnAbilityRequiredTraits>()?.traits;
            if (required != null && required.Count > 0 && !required.Any(t => user.story?.traits?.HasTrait(t) == true
                || (t == TorannMagicDefOf.TM_Empath && ClassProgression.ClassIdentity.IsEmpath(user)))) return true;
            if (!sharedLearning.Add(owner)) return true;
            try
            {
                if (parent.Destroyed || parent.stackCount < 1 || !parent.def.destroyable) return true;
                var holder = parent.holdingOwner;
                var payment = parent.SplitOff(1);
                if (payment == null) return true;
                try { payment.Destroy(DestroyMode.Vanish); }
                catch
                {
                    if (!payment.Destroyed) { RestoreUnpaidScroll(user, payment, holder); throw; }
                }
                if (!payment.Destroyed) { RestoreUnpaidScroll(user, payment, holder); return true; }
                owner.learned = true;
                ClassProgression.ClassSpellAccess.RestoreLearned(comp);
                return true;
            }
            finally { sharedLearning.Remove(owner); }
        }

        private bool TryCompleteEnrolledGolemancerScroll(Pawn user, CompAbilityUserMagic comp)
        {
            bool brand = ClassProgression.GolemancerBrands.IsBrandScroll(parent.def);
            if ((!brand && parent.def?.defName != "SpellOf_LivingWall") || !ClassProgression.ClassIdentity.TryGetState(user, out _)) return false;
            var owner = brand ? ClassProgression.GolemancerBrands.ScrollOwner(comp?.MagicData, parent.def)
                : comp?.MagicData?.MagicPowersGolemancer.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LivingWall));
            if (owner == null || owner.learned) return true;
            if (!ClassProgression.ClassIdentity.IsGolemancer(user)
                || !ClassProgression.ClassSpellAccess.CanLearnFromScroll(user, owner, parent.def)) return true;
            var required = parent.def.GetModExtension<DefModExtension_LearnAbilityRequiredTraits>()?.traits;
            if (required != null && required.Count > 0 && !required.Any(t => user.story?.traits?.HasTrait(t) == true
                || (t == TorannMagicDefOf.TM_Golemancer && ClassProgression.ClassIdentity.IsGolemancer(user)))) return true;
            if (!sharedLearning.Add(owner)) return true;
            try
            {
                if (parent.Destroyed || parent.stackCount < 1 || !parent.def.destroyable) return true;
                var holder = parent.holdingOwner;
                var payment = parent.SplitOff(1);
                if (payment == null) return true;
                try { payment.Destroy(DestroyMode.Vanish); }
                catch
                {
                    if (!payment.Destroyed) { RestoreUnpaidScroll(user, payment, holder); throw; }
                }
                if (!payment.Destroyed) { RestoreUnpaidScroll(user, payment, holder); return true; }
                owner.learned = true;
                ClassProgression.ClassSpellAccess.RestoreLearned(comp);
                return true;
            }
            finally { sharedLearning.Remove(owner); }
        }

        public override void DoEffect(Pawn user)
        {
            CompAbilityUserMagic comp = user.GetCompAbilityUserMagic();
            MagicPower magicPower;
            if (ClassProgression.ClassIdentity.TryGetState(user, out _))
            {
                TM_ClassState.RestoreSharedMagicProgression(comp);
                var target = comp?.MagicData?.AllMagicPowersWithSkills.FirstOrDefault(p =>
                    p.TMabilityDefs.OfType<TMAbilityDef>().Any(a => a.learnItem == parent.def)
                    || (parent.def.defName == "SpellOf_Resurrection" && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Resurrection)));
                if (parent.def.defName == "SpellOf_FadeEmotions") target = ClassProgression.EmpathOwners.FadeOwner(comp?.MagicData);
                if (parent.def.defName == "SpellOf_GuardianSpirit")
                    target = comp?.MagicData?.MagicPowersShaman.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_GuardianSpirit));
                if (ClassProgression.GolemancerBrands.IsBrandScroll(parent.def))
                    target = ClassProgression.GolemancerBrands.ScrollOwner(comp?.MagicData, parent.def);
                if (parent.def.defName == "SpellOf_LivingWall")
                    target = comp?.MagicData?.MagicPowersGolemancer.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LivingWall));
                if (parent.def.defName == "SpellOf_SpiritOfLight")
                    target = comp?.MagicData?.MagicPowersBrightmage.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SpiritOfLight));
                if (parent.def.defName == "SpellOf_Recall")
                    target = comp?.MagicData?.MagicPowersC.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Recall));
                if (parent.def.defName == "SpellOf_Shapeshift")
                    target = comp?.MagicData?.MagicPowersE.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Shapeshift));
                if (parent.def.defName == "SpellOf_BloodMoon")
                    target = comp?.MagicData?.MagicPowersBM.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_BloodMoon));
                if (target != null && parent.def.defName != "SpellOf_FadeEmotions" && !ClassProgression.GolemancerBrands.IsBrandScroll(parent.def) && parent.def.defName != "SpellOf_SpiritOfLight" && parent.def.defName != "SpellOf_GuardianSpirit" && parent.def.defName != "SpellOf_LivingWall") target = TM_ClassState.PreferredMagicOwner(comp, target.TMabilityDefs[0]) ?? target;
                if (target != null && !ClassProgression.ClassSpellAccess.CanLearnFromScroll(user, target, parent.def))
                {
                    Messages.Message("TM_ProgressionStageLocked".Translate(), user, MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }
            if (TryCompleteEnrolledBrightmageScroll(user, comp)) return;
            if (TryCompleteEnrolledGolemancerScroll(user, comp)) return;
            if (TryCompleteEnrolledShamanScroll(user, comp)) return;
            if (TryCompleteEnrolledEmpathScroll(user, comp)) return;
            if (parent.def != null && (TM_Calc.IsMagicUser(user) || TM_Calc.IsWanderer(user)))
            {                
                List<TraitDef> restrictedTraits = null;
                if (this.parent.def.HasModExtension<DefModExtension_LearnAbilityRequiredTraits>())
                {                    
                    restrictedTraits = new List<TraitDef>();
                    restrictedTraits.Clear();
                    restrictedTraits = this.parent.def.GetModExtension<DefModExtension_LearnAbilityRequiredTraits>().traits;
                }
                bool hasRequiredTrait = true;
                if(comp.customClass != null && !comp.customClass.canLearnCantrips)
                {                    
                    hasRequiredTrait = false;
                }
                if (restrictedTraits != null && restrictedTraits.Count > 0)
                {                   
                    hasRequiredTrait = false;
                    foreach (TraitDef td in restrictedTraits)
                    {
                        if (comp.Pawn.story.traits.HasTrait(td))
                        {
                            hasRequiredTrait = true;
                            break;
                        }
                    }
                }                
                if (comp.customClass != null)
                {
                    bool itemUsed = false;                   
                    for (int i = 0; i < comp.MagicData.AllMagicPowers.Count; i++)
                    {
                        TMAbilityDef ad = (TMAbilityDef)comp.MagicData.AllMagicPowers[i].abilityDef;                        
                        if (ad.learnItem == parent.def)
                        {                            
                            if (!TM_Data.RestrictedAbilities.Contains(parent.def) && !comp.MagicData.AllMagicPowers[i].learned && hasRequiredTrait)
                            {                                
                                itemUsed = true;
                                comp.MagicData.AllMagicPowers[i].learned = true;
                                if(ad.shouldInitialize)
                                {
                                    comp.RemovePawnAbility(ad);
                                    comp.AddPawnAbility(ad);
                                }
                                comp.InitializeSpell();
                                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                                break;
                            }
                            else if ((TM_Data.RestrictedAbilities.Contains(parent.def) || hasRequiredTrait) && !comp.MagicData.AllMagicPowers[i].learned)
                            {                                
                                if (comp.customClass.learnableSpells.Contains(parent.def))
                                {
                                    itemUsed = true;
                                    comp.MagicData.AllMagicPowers[i].learned = true;
                                    if(ad.shouldInitialize)
                                    {
                                        comp.RemovePawnAbility(ad);
                                        comp.AddPawnAbility(ad);
                                    }
                                    comp.InitializeSpell();
                                    this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                                    break;
                                }
                                else
                                {
                                    Messages.Message("CannotLearnSpell".Translate(), MessageTypeDefOf.RejectInput);
                                    break;
                                }
                            }                            
                            else
                            {                                
                                if (!hasRequiredTrait)
                                {
                                    Messages.Message("CannotLearnSpell".Translate(), MessageTypeDefOf.RejectInput);
                                    return;
                                }
                                else
                                {
                                    Messages.Message("TM_AlreadyLearnedAbility".Translate(user.LabelShort, ad.label), MessageTypeDefOf.RejectInput);
                                    return;
                                }
                            }
                        }
                    }
                    if (!itemUsed)
                    {

                        Messages.Message("CannotLearnSpell".Translate(), MessageTypeDefOf.RejectInput);                        
                        return;
                    }
                }
                else
                {
                    TMAbilityDef customSkill = null;                    
                    for (int i = 0; i < comp.MagicData.MagicPowersCustomAll.Count; i++)
                    {
                        TMAbilityDef tempSkill = (TMAbilityDef)comp.MagicData.MagicPowersCustomAll[i].abilityDef;
                        if (tempSkill.learnItem != null && tempSkill.learnItem == parent.def)
                        {
                            if (!comp.MagicData.MagicPowersCustomAll[i].learned)
                            {
                                customSkill = tempSkill;
                                break;
                            }
                        }
                    }
                    if (hasRequiredTrait)
                    {
                        if (TryCompleteEnrolledSharedScroll(user, comp)) return;
                        if (parent.def.defName == "SpellOf_Rain" && comp.spell_Rain == false)
                        {
                            comp.spell_Rain = true;
                            magicPower = comp.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Rainmaker);
                            comp.AddPawnAbility(TorannMagicDefOf.TM_Rainmaker);
                            magicPower.learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Blink" && comp.spell_Blink == false)
                        {
                            comp.spell_Blink = true;
                            magicPower = comp.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Blink);
                            comp.AddPawnAbility(TorannMagicDefOf.TM_Blink);
                            if (!TorannMagic.ClassProgression.ClassIdentity.IsChaosMage(user))
                            {
                                magicPower.learned = true;
                            }
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Teleport" && comp.spell_Teleport == false)
                        {
                            comp.spell_Teleport = true;
                            magicPower = comp.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Teleport);
                            comp.AddPawnAbility(TorannMagicDefOf.TM_Teleport);
                            if (!TorannMagic.ClassProgression.ClassIdentity.IsChaosMage(user))
                            {
                                magicPower.learned = true;
                            }
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Heal" && comp.spell_Heal == false)
                        {
                            comp.spell_Heal = true;
                            magicPower = comp.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Heal);
                            comp.AddPawnAbility(TorannMagicDefOf.TM_Heal);
                            if (!TorannMagic.ClassProgression.ClassIdentity.IsChaosMage(user))
                            {
                                magicPower.learned = true;
                            }
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Heater" && comp.spell_Heater == false)
                        {
                            comp.spell_Heater = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Heater).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Cooler" && comp.spell_Cooler == false)
                        {
                            comp.spell_Cooler = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Cooler).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_PowerNode" && comp.spell_PowerNode == false)
                        {
                            comp.spell_PowerNode = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_PowerNode).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Sunlight" && comp.spell_Sunlight == false)
                        {
                            comp.spell_Sunlight = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_PowerNode).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_DryGround" && comp.spell_DryGround == false && ClassProgression.ClassIdentity.IsFireMage(user))
                        {
                            comp.spell_DryGround = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_DryGround).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Firestorm" && comp.spell_Firestorm == false && ClassProgression.ClassIdentity.IsFireMage(user))
                        {
                            comp.spell_Firestorm = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Firestorm).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_WetGround" && comp.spell_WetGround == false && ClassProgression.ClassIdentity.IsIceMage(user))
                        {
                            comp.spell_WetGround = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_WetGround).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Blizzard" && comp.spell_Blizzard == false && ClassProgression.ClassIdentity.IsIceMage(user))
                        {
                            comp.spell_Blizzard = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Blizzard).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_ChargeBattery" && comp.spell_ChargeBattery == false && ClassProgression.ClassIdentity.IsLightningMage(user))
                        {
                            comp.spell_ChargeBattery = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_ChargeBattery).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_SmokeCloud" && comp.spell_SmokeCloud == false)
                        {
                            comp.spell_SmokeCloud = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_SmokeCloud).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Extinguish" && comp.spell_Extinguish == false)
                        {
                            comp.spell_Extinguish = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_EMP" && comp.spell_EMP == false)
                        {
                            comp.spell_EMP = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_EMP).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_SummonMinion" && comp.spell_SummonMinion == false)
                        {
                            comp.spell_SummonMinion = true;
                            magicPower = comp.MagicData.MagicPowersS.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonMinion);
                            comp.AddPawnAbility(TorannMagicDefOf.TM_SummonMinion);
                            if (!TorannMagic.ClassProgression.ClassIdentity.IsChaosMage(user))
                            {
                                magicPower.learned = true;
                            }
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_TransferMana" && comp.spell_TransferMana == false)
                        {
                            comp.spell_TransferMana = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_TransferMana).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_SiphonMana" && comp.spell_SiphonMana == false)
                        {
                            comp.spell_SiphonMana = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_SiphonMana).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_RegrowLimb" && comp.spell_RegrowLimb == false && ClassProgression.ClassIdentity.IsDruid(user))
                        {
                            comp.spell_RegrowLimb = true;
                            comp.InitializeSpell();
                            magicPower = comp.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RegrowLimb);
                            magicPower.learned = true;
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_EyeOfTheStorm" && comp.spell_EyeOfTheStorm == false && ClassProgression.ClassIdentity.IsLightningMage(user))
                        {
                            comp.spell_EyeOfTheStorm = true;
                            magicPower = comp.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EyeOfTheStorm);
                            magicPower.learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_ManaShield" && comp.spell_ManaShield == false)
                        {
                            comp.spell_ManaShield = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_ManaShield).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_FoldReality" && comp.spell_FoldReality == false && ClassProgression.ClassIdentity.IsArcanist(user))
                        {
                            comp.spell_FoldReality = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_FoldReality).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Resurrection" && comp.spell_Resurrection == false && ClassProgression.ClassIdentity.IsPriest(user))
                        {
                            comp.spell_Resurrection = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Resurrection).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_BattleHymn" && comp.spell_BattleHymn == false && ClassProgression.ClassIdentity.IsBard(user))
                        {
                            comp.spell_BattleHymn = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_BattleHymn).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_HolyWrath" && comp.spell_HolyWrath == false && ClassProgression.ClassIdentity.IsPaladin(user))
                        {
                            comp.spell_HolyWrath = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_HolyWrath).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_LichForm" && comp.spell_LichForm == false && user.story.traits.HasTrait(TorannMagicDefOf.Necromancer))
                        {
                            comp.spell_LichForm = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_LichForm).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_SummonPoppi" && comp.spell_SummonPoppi == false && ClassProgression.ClassIdentity.IsSummoner(user))
                        {
                            comp.spell_SummonPoppi = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_SummonPoppi).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Scorn" && comp.spell_Scorn == false && ClassProgression.ClassIdentity.IsSuccubus(user))
                        {
                            comp.spell_Scorn = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Scorn).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_PsychicShock" && comp.spell_PsychicShock == false && ClassProgression.ClassIdentity.IsWarlock(user))
                        {
                            comp.spell_PsychicShock = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_PsychicShock).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Meteor" && comp.spell_Meteor == false && ClassProgression.ClassIdentity.IsGeomancer(user))
                        {
                            comp.spell_Meteor = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Meteor).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_OrbitalStrike" && comp.spell_OrbitalStrike == false && (user.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || ClassProgression.ClassIdentity.IsTechnomancer(user)))
                        {
                            comp.spell_OrbitalStrike = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_OrbitalStrike).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_CauterizeWound" && comp.spell_CauterizeWound == false && ClassProgression.ClassIdentity.IsFireMage(user))
                        {
                            comp.spell_CauterizeWound = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_CauterizeWound).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_FertileLands" && comp.spell_FertileLands == false && ClassProgression.ClassIdentity.IsDruid(user))
                        {
                            comp.spell_FertileLands = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_FertileLands).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_SpellMending" && comp.spell_SpellMending == false)
                        {
                            comp.spell_SpellMending = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_SpellMending).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_TechnoShield" && comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TechnoShield).learned == false && (user.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || ClassProgression.ClassIdentity.IsTechnomancer(user)))
                        {
                            comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TechnoShield).learned = true;
                            comp.AddPawnAbility(TorannMagicDefOf.TM_TechnoShield);
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Sabotage" && comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Sabotage).learned == false && (user.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || ClassProgression.ClassIdentity.IsTechnomancer(user)))
                        {
                            comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Sabotage).learned = true;
                            comp.AddPawnAbility(TorannMagicDefOf.TM_Sabotage);
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Overdrive" && comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Overdrive).learned == false && (user.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || ClassProgression.ClassIdentity.IsTechnomancer(user)))
                        {
                            comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Overdrive).learned = true;
                            comp.AddPawnAbility(TorannMagicDefOf.TM_Overdrive);
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_BloodMoon" && comp.spell_BloodMoon == false && ClassProgression.ClassIdentity.IsBloodMage(user))
                        {
                            comp.spell_BloodMoon = true;
                            comp.MagicData.MagicPowersBM.First(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_BloodMoon)).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_Shapeshift && comp.spell_Shapeshift == false && ClassProgression.ClassIdentity.IsEnchanter(user))
                        {
                            comp.spell_Shapeshift = true;
                            comp.MagicData.MagicPowersE.First(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Shapeshift)).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_Blur && comp.spell_Blur == false)
                        {
                            comp.spell_Blur = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Blur).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_BlankMind && comp.spell_BlankMind == false && ClassProgression.ClassIdentity.IsEnchanter(user))
                        {
                            comp.spell_BlankMind = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_BlankMind).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_DirtDevil && comp.spell_DirtDevil == false)
                        {
                            comp.spell_DirtDevil = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_DirtDevil).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_MechaniteReprogramming && comp.spell_MechaniteReprogramming == false && (user.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || ClassProgression.ClassIdentity.IsTechnomancer(user)))
                        {
                            comp.spell_MechaniteReprogramming = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_MechaniteReprogramming).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_ArcaneBolt && comp.spell_ArcaneBolt == false && comp.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                        {
                            comp.spell_ArcaneBolt = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_ArcaneBolt).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_LightningTrap && comp.spell_LightningTrap == false && comp.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                        {
                            comp.spell_LightningTrap = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_LightningTrap).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_Invisibility && comp.spell_Invisibility == false)
                        {
                            comp.spell_Invisibility = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Invisibility).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_BriarPatch && comp.spell_BriarPatch == false && ClassProgression.ClassIdentity.IsDruid(user))
                        {
                            comp.spell_BriarPatch = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_BriarPatch).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_Recall && comp.spell_Recall == false && ClassProgression.ClassIdentity.IsChronomancer(user))
                        {
                            comp.spell_Recall = true;
                            comp.MagicData.MagicPowersC.First(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Recall)).learned = true;
                            comp.MagicData.MagicPowersStandalone.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TimeMark).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_HeatShield && comp.spell_HeatShield == false && ClassProgression.ClassIdentity.IsFireMage(user))
                        {
                            comp.spell_HeatShield = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_HeatShield).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_MageLight && comp.spell_MageLight == false)
                        {
                            comp.spell_MageLight = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_MageLight).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_Ignite && comp.spell_Ignite == false)
                        {
                            comp.spell_Ignite = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Ignite).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_SnapFreeze && comp.spell_SnapFreeze == false)
                        {
                            comp.spell_SnapFreeze = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_SnapFreeze).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (customSkill != null)
                        {                            
                            comp.MagicData.ReturnMatchingMagicPower(customSkill).learned = true;
                            comp.AddPawnAbility(customSkill);
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else
                        {
                            Messages.Message("CannotLearnSpell".Translate(), MessageTypeDefOf.RejectInput);
                        }
                    }
                    else
                    {
                        Messages.Message("CannotLearnSpell".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
            }
            else
            {
                Messages.Message("NotMageToLearnSpell".Translate(), MessageTypeDefOf.RejectInput);
            }
        }
    }
}
