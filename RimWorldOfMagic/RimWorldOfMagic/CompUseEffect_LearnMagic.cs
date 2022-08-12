using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;


namespace TorannMagic
{
	public class CompUseEffect_LearnMagic : CompUseEffect
	{

		public override void DoEffect(Pawn user)
		{
            if (parent.def != null)
            {
                bool customClass = false;
                bool advancedClass = false;
                string failMessage = "";
                CompAbilityUserMagic comp = user.GetCompAbilityUserMagic();
                for (int i = 0; i < TM_ClassUtility.CustomClasses.Count; i++)
                {
                    TMDefs.TM_CustomClass cc = TM_ClassUtility.CustomClasses[i];
                    if (cc.isMage && cc.isAdvancedClass && comp != null)
                    {
                        if (parent.def == cc.tornScript || parent.def == cc.fullScript)
                        {
                            if(TM_Calc.HasAdvancedMageRequirements(user, cc, out failMessage))
                            {
                                advancedClass = true;
                                if (parent.def == cc.fullScript)
                                {
                                    HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                                }
                                ApplyAdvancedTrait(user, cc);
                                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                            }
                            break;
                        }
                        
                    }
                    else if (cc.isMage && user.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted) || (cc.isMage && cc.isFighter && (user.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted) || user.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy))))
                    {
                        if (parent.def == cc.tornScript || parent.def == TM_ClassUtility.CustomClasses[i].fullScript)
                        {
                            customClass = true;
                            if (parent.def == TM_ClassUtility.CustomClasses[i].fullScript)
                            {
                                HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                            }

                            ApplyTrait(user, TM_ClassUtility.CustomClasses[i].classTrait, TM_ClassUtility.CustomClasses[i].traitDegree);

                            //Unique actions hook
                            ApplyTraitAdjustments(user, TM_ClassUtility.CustomClasses[i].classTrait);
                            //
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                            if (comp != null)
                            {
                                comp.customIndex = i;
                                comp.customClass = TM_ClassUtility.CustomClasses[i];
                            }
                            else
                            {
                                Log.Message("failed to initialize custom magic class comp");
                            }
                            CompAbilityUserMight mComp = user.GetCompAbilityUserMight();
                            if (mComp != null && TM_ClassUtility.CustomClasses[i].isFighter)
                            {
                                mComp.customIndex = i;
                                mComp.customClass = TM_ClassUtility.CustomClasses[i];
                            }
                            break;
                        }
                    }
                }
                if (!customClass && !advancedClass && user.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted))
                {
                    if (parent.def.defName == "BookOfInnerFire" || parent.def.defName == "Torn_BookOfInnerFire")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.InnerFire, 0, false));
                        if (parent.def.defName == "BookOfInnerFire")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        //this.parent.Destroy(DestroyMode.Vanish);
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfHeartOfFrost" || parent.def.defName == "Torn_BookOfHeartOfFrost")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.HeartOfFrost, 0, false));
                        if (parent.def.defName == "BookOfHeartOfFrost")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfStormBorn" || parent.def.defName == "Torn_BookOfStormBorn")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.StormBorn, 0, false));
                        if (parent.def.defName == "BookOfStormBorn")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfArcanist" || parent.def.defName == "Torn_BookOfArcanist")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Arcanist, 0, false));
                        if (parent.def.defName == "BookOfArcanist")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfValiant" || parent.def.defName == "Torn_BookOfValiant")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Paladin, 0, false));
                        if (parent.def.defName == "BookOfValiant")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfSummoner" || parent.def.defName == "Torn_BookOfSummoner")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Summoner, 0, false));
                        if (parent.def.defName == "BookOfSummoner")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfDruid" || parent.def.defName == "Torn_BookOfNature")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Druid, 0, false));
                        if (parent.def.defName == "BookOfDruid")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfNecromancer" || parent.def.defName == "Torn_BookOfUndead")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Necromancer, 0, false));
                        if (parent.def.defName == "BookOfNecromancer")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfPriest" || parent.def.defName == "Torn_BookOfPriest")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        FixPriestSkills(user);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Priest, 0, false));
                        if (parent.def.defName == "BookOfPriest")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfBard" || parent.def.defName == "Torn_BookOfBard")
                    {
                        if (user.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Social)
                        {
                            FixTrait(user, user.story.traits.allTraits);
                            FixBardSkills(user);
                            user.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Bard, 0, false));
                            if (parent.def.defName == "BookOfBard")
                            {
                                HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                            }
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else
                        {
                            Messages.Message("TM_NotSocialCapable".Translate(
                                user.LabelShort
                            ), MessageTypeDefOf.RejectInput);
                        }

                    }
                    else if (parent.def.defName == "BookOfDemons" || parent.def.defName == "Torn_BookOfDemons")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        if (user.gender == Gender.Male)
                        {
                            user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 0, false));
                        }
                        else if (user.gender == Gender.Female)
                        {
                            user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 0, false));
                        }
                        else
                        {
                            Log.Message("No gender found - assigning random trait.");
                            if (Rand.Chance(.5f))
                            {
                                user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 0, false));
                            }
                            else
                            {
                                user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 0, false));
                            }
                        }
                        if (parent.def.defName == "BookOfDemons")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfEarth" || parent.def.defName == "Torn_BookOfEarth")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Geomancer, 0, false));
                        if (parent.def.defName == "BookOfEarth")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfMagitech" || parent.def.defName == "Torn_BookOfMagitech")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Technomancer, 0, false));
                        if (parent.def.defName == "BookOfMagitech")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfHemomancy" || parent.def.defName == "Torn_BookOfHemomancy")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.BloodMage, 0, false));
                        if (parent.def.defName == "BookOfHemomancy")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfEnchanter" || parent.def.defName == "Torn_BookOfEnchanter")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Enchanter, 0, false));
                        if (parent.def.defName == "BookOfEnchanter")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfChronomancer" || parent.def.defName == "Torn_BookOfChronomancer")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Chronomancer, 0, false));
                        if (parent.def.defName == "BookOfChronomancer")
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def == TorannMagicDefOf.BookOfChaos || parent.def == TorannMagicDefOf.Torn_BookOfChaos)
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.ChaosMage, 0, false));
                        if (parent.def == TorannMagicDefOf.BookOfChaos)
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfQuestion")
                    {
                        int attempts = 0;
                        ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                        FixTrait(user, user.story.traits.allTraits);
                        RetryBookOfQuestion:;
                        if (attempts < 50)
                        {
                            int baseClassCount = 17;
                            int customClassCount = TM_ClassUtility.CustomMageClasses.Count;
                            int rnd = Mathf.RoundToInt(Rand.RangeInclusive(0, baseClassCount + customClassCount));
                            switch (rnd)
                            {
                                case 0:
                                    if (settingsRef.Demonkin)
                                    {
                                        if (user.gender == Gender.Male)
                                        {
                                            user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 0, false));
                                        }
                                        else if (user.gender == Gender.Female)
                                        {
                                            user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 0, false));
                                        }
                                        else
                                        {
                                            Log.Message("No gender found.");
                                        }
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 15:
                                    if (settingsRef.Demonkin)
                                    {
                                        if (user.gender == Gender.Male)
                                        {
                                            user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 0, false));
                                        }
                                        else if (user.gender == Gender.Female)
                                        {
                                            user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 0, false));
                                        }
                                        else
                                        {
                                            Log.Message("No gender found.");
                                        }
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 1:
                                    if (settingsRef.Necromancer)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Necromancer, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 2:
                                    if (settingsRef.Druid)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Druid, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 3:
                                    if (settingsRef.Summoner)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Summoner, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 4:
                                    if (settingsRef.FireMage)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.InnerFire, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 5:
                                    if (settingsRef.IceMage)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.HeartOfFrost, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 6:
                                    if (settingsRef.LitMage)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.StormBorn, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 7:
                                    if (settingsRef.Arcanist)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Arcanist, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 8:
                                    if (settingsRef.Priest)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Priest, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 9:
                                    if (settingsRef.Bard)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Bard, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 10:
                                    if (settingsRef.Paladin)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Paladin, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 11:
                                    if (settingsRef.Geomancer)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Geomancer, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 12:
                                    if (settingsRef.Technomancer)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Technomancer, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 13:
                                    if (settingsRef.BloodMage)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.BloodMage, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 14:
                                    if (settingsRef.Technomancer)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Enchanter, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 16:
                                    if (settingsRef.Chronomancer)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Chronomancer, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case 17:
                                    if (settingsRef.ChaosMage)
                                    {
                                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.ChaosMage, 0, false));
                                    }
                                    else
                                    {
                                        attempts++;
                                        goto RetryBookOfQuestion;
                                    }
                                    break;
                                case int val when rnd > baseClassCount:
                                    val = val - baseClassCount - 1;
                                    TMDefs.TM_CustomClass cc = TM_ClassUtility.GetRandomCustomMage();
                                    user.story.traits.GainTrait(new Trait(cc.classTrait, cc.traitDegree));
                                    break;

                            }
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else
                        {
                            user.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Gifted, 0, false));
                            Messages.Message("Unable to find a valid class to assign after 50 attempts - ending attempt", MessageTypeDefOf.RejectInput);
                        }
                    }
                    else
                    {
                        Messages.Message("NotArcaneBook".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else if(!customClass && !advancedClass && !user.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted))
                {
                    if (failMessage != "")
                    {
                        Messages.Message("TM_UnableToLearnAdvancedClass".Translate(
                                user.LabelShort,
                                this.parent.def.label,
                                failMessage
                            ), MessageTypeDefOf.RejectInput);
                    }
                    else
                    {
                        Messages.Message("NotGiftedPawn".Translate(
                                user.LabelShort
                            ), MessageTypeDefOf.RejectInput);
                    }
                }
            }
            else
            {
                Messages.Message("TM_InvalidAction".Translate(
                        user.LabelShort,
                        "magic book"
                    ), MessageTypeDefOf.RejectInput);
            }
		}

        public static void ApplyAdvancedTrait(Pawn p, TMDefs.TM_CustomClass cc)
        {
            TraitDef trait = cc.classTrait;
            if (trait == TorannMagicDefOf.Warlock || trait == TorannMagicDefOf.Succubus)
            {
                if (p.gender == Gender.Male)
                {
                    p.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 0, false));
                }
                else if (p.gender == Gender.Female)
                {
                    p.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 0, false));
                }
                else
                {
                    if (Rand.Chance(.5f))
                    {
                        p.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 0, false));
                    }
                    else
                    {
                        p.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 0, false));
                    }
                }
            }
            p.story.traits.GainTrait(new Trait(cc.classTrait, cc.traitDegree));            
            
            if (cc.advancedClassOptions != null && cc.advancedClassOptions.removesRequiredTrait)
            {
                List<Trait> pawnTraits = p.story.traits.allTraits;
                foreach (TraitDef td in cc.advancedClassOptions.requiredTraits)
                {
                    foreach (Trait t in pawnTraits)
                    {
                        if(t.def == td)
                        {
                            p.story.traits.RemoveTrait(t);
                            goto TraitGainEnd;
                        }
                    }
                }
            }            
            TraitGainEnd:;
        }

        public static void FixTrait(Pawn pawn, List<Trait> traits)
        {
            TraitStart:;
            for (int i = 0; i < traits.Count; i++)
            {
                if (traits[i].def == TorannMagicDefOf.TM_Gifted)
                {
                    traits.Remove(traits[i]);
                    goto TraitStart;
                }
                if(traits[i].def == TorannMagicDefOf.PhysicalProdigy)
                {
                    traits.Remove(traits[i]);
                    goto TraitStart;
                }
            }
        }

        public static void FixPriestSkills(Pawn pawn)
        {
            SkillRecord skill;
            skill = pawn.skills.GetSkill(SkillDefOf.Shooting);
            skill.passion = Passion.None;
            skill = pawn.skills.GetSkill(SkillDefOf.Melee);
            skill.passion = Passion.None;
            pawn.workSettings.SetPriority(WorkTypeDefOf.Hunting, 0);
            skill = pawn.skills.GetSkill(SkillDefOf.Medicine);
            if(skill.passion == Passion.None)
            {
                skill.passion = Passion.Minor;
            }
        }

        public static void FixBardSkills(Pawn pawn)
        {
            SkillRecord skill;
            pawn.workSettings.SetPriority(TorannMagicDefOf.Cleaning, 0);
            pawn.workSettings.SetPriority(TorannMagicDefOf.Hauling, 0);
            pawn.workSettings.SetPriority(TorannMagicDefOf.PlantCutting, 0);
            skill = pawn.skills.GetSkill(SkillDefOf.Social);
            if(skill.passion == Passion.Minor)
            {
                skill.passion = Passion.Major;
            }
            if (skill.passion == Passion.None)
            {
                skill.passion = Passion.Minor;
            }
        }

        public void ApplyTrait(Pawn user, TraitDef trait, int degree)
        {
            FixTrait(user, user.story.traits.allTraits);
            if(trait == TorannMagicDefOf.Warlock || trait == TorannMagicDefOf.Succubus)
            {
                if (user.gender == Gender.Male)
                {
                    user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 0, false));
                }
                else if (user.gender == Gender.Female)
                {
                    user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 0, false));
                }
                else
                {
                    Log.Message("No gender found - assigning random trait.");
                    if (Rand.Chance(.5f))
                    {
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 0, false));
                    }
                    else
                    {
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 0, false));
                    }
                }
            }
            user.story.traits.GainTrait(new Trait(trait, degree));
        }

        public void ApplyTraitAdjustments(Pawn pawn, TraitDef trait)
        {
            if (trait == TorannMagicDefOf.Priest)
            {
                FixPriestSkills(pawn);
            }
            if(trait == TorannMagicDefOf.TM_Bard)
            {
                FixBardSkills(pawn);
            }
        }
    }
}
