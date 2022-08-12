using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace TorannMagic
{
    public class CompUseEffect_LearnSpell : CompUseEffect
    {
        public override void DoEffect(Pawn user)
        {
            CompAbilityUserMagic comp = user.GetCompAbilityUserMagic();
            MagicPower magicPower;
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
                            if (!user.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
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
                            if (!user.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
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
                            if (!user.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
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
                        else if (parent.def.defName == "SpellOf_DryGround" && comp.spell_DryGround == false && user.story.traits.HasTrait(TorannMagicDefOf.InnerFire))
                        {
                            comp.spell_DryGround = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_DryGround).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Firestorm" && comp.spell_Firestorm == false && user.story.traits.HasTrait(TorannMagicDefOf.InnerFire))
                        {
                            comp.spell_Firestorm = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Firestorm).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_WetGround" && comp.spell_WetGround == false && user.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost))
                        {
                            comp.spell_WetGround = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_WetGround).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Blizzard" && comp.spell_Blizzard == false && user.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost))
                        {
                            comp.spell_Blizzard = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Blizzard).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_ChargeBattery" && comp.spell_ChargeBattery == false && user.story.traits.HasTrait(TorannMagicDefOf.StormBorn))
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
                            if (!user.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
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
                        else if (parent.def.defName == "SpellOf_RegrowLimb" && comp.spell_RegrowLimb == false && user.story.traits.HasTrait(TorannMagicDefOf.Druid))
                        {
                            comp.spell_RegrowLimb = true;
                            comp.InitializeSpell();
                            magicPower = comp.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RegrowLimb);
                            magicPower.learned = true;
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_EyeOfTheStorm" && comp.spell_EyeOfTheStorm == false && user.story.traits.HasTrait(TorannMagicDefOf.StormBorn))
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
                        else if (parent.def.defName == "SpellOf_FoldReality" && comp.spell_FoldReality == false && user.story.traits.HasTrait(TorannMagicDefOf.Arcanist))
                        {
                            comp.spell_FoldReality = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Firestorm).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Resurrection" && comp.spell_Resurrection == false && user.story.traits.HasTrait(TorannMagicDefOf.Priest))
                        {
                            comp.spell_Resurrection = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Resurrection).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_BattleHymn" && comp.spell_BattleHymn == false && user.story.traits.HasTrait(TorannMagicDefOf.TM_Bard))
                        {
                            comp.spell_BattleHymn = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_BattleHymn).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_HolyWrath" && comp.spell_HolyWrath == false && user.story.traits.HasTrait(TorannMagicDefOf.Paladin))
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
                        else if (parent.def.defName == "SpellOf_SummonPoppi" && comp.spell_SummonPoppi == false && user.story.traits.HasTrait(TorannMagicDefOf.Summoner))
                        {
                            comp.spell_SummonPoppi = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_SummonPoppi).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Scorn" && comp.spell_Scorn == false && user.story.traits.HasTrait(TorannMagicDefOf.Succubus))
                        {
                            comp.spell_Scorn = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Scorn).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_PsychicShock" && comp.spell_PsychicShock == false && user.story.traits.HasTrait(TorannMagicDefOf.Warlock))
                        {
                            comp.spell_PsychicShock = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_PsychicShock).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Meteor" && comp.spell_Meteor == false && user.story.traits.HasTrait(TorannMagicDefOf.Geomancer))
                        {
                            comp.spell_Meteor = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Meteor).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_OrbitalStrike" && comp.spell_OrbitalStrike == false && user.story.traits.HasTrait(TorannMagicDefOf.Technomancer))
                        {
                            comp.spell_OrbitalStrike = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_OrbitalStrike).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_CauterizeWound" && comp.spell_CauterizeWound == false && user.story.traits.HasTrait(TorannMagicDefOf.InnerFire))
                        {
                            comp.spell_CauterizeWound = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_CauterizeWound).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_FertileLands" && comp.spell_FertileLands == false && user.story.traits.HasTrait(TorannMagicDefOf.Druid))
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
                        else if (parent.def.defName == "SpellOf_TechnoShield" && comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TechnoShield).learned == false && user.story.traits.HasTrait(TorannMagicDefOf.Technomancer))
                        {
                            comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TechnoShield).learned = true;
                            comp.AddPawnAbility(TorannMagicDefOf.TM_TechnoShield);
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Sabotage" && comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Sabotage).learned == false && user.story.traits.HasTrait(TorannMagicDefOf.Technomancer))
                        {
                            comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Sabotage).learned = true;
                            comp.AddPawnAbility(TorannMagicDefOf.TM_Sabotage);
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_Overdrive" && comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Overdrive).learned == false && user.story.traits.HasTrait(TorannMagicDefOf.Technomancer))
                        {
                            comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Overdrive).learned = true;
                            comp.AddPawnAbility(TorannMagicDefOf.TM_Overdrive);
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def.defName == "SpellOf_BloodMoon" && comp.spell_BloodMoon == false && user.story.traits.HasTrait(TorannMagicDefOf.BloodMage))
                        {
                            comp.spell_BloodMoon = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_BloodMoon).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_Shapeshift && comp.spell_Shapeshift == false && user.story.traits.HasTrait(TorannMagicDefOf.Enchanter))
                        {
                            comp.spell_Shapeshift = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Shapeshift).learned = true;
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
                        else if (parent.def == TorannMagicDefOf.SpellOf_BlankMind && comp.spell_BlankMind == false && user.story.traits.HasTrait(TorannMagicDefOf.Enchanter))
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
                        else if (parent.def == TorannMagicDefOf.SpellOf_MechaniteReprogramming && comp.spell_MechaniteReprogramming == false && user.story.traits.HasTrait(TorannMagicDefOf.Technomancer))
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
                        else if (parent.def == TorannMagicDefOf.SpellOf_BriarPatch && comp.spell_BriarPatch == false && user.story.traits.HasTrait(TorannMagicDefOf.Druid))
                        {
                            comp.spell_BriarPatch = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_BriarPatch).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_Recall && comp.spell_Recall == false && user.story.traits.HasTrait(TorannMagicDefOf.Chronomancer))
                        {
                            comp.spell_Recall = true;
                            comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Recall).learned = true;
                            comp.MagicData.MagicPowersStandalone.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TimeMark).learned = true;
                            comp.InitializeSpell();
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (parent.def == TorannMagicDefOf.SpellOf_HeatShield && comp.spell_HeatShield == false && user.story.traits.HasTrait(TorannMagicDefOf.InnerFire))
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
