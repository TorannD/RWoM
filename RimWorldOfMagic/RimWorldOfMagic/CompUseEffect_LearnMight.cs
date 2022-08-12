using RimWorld;
using Verse;
using System.Collections.Generic;


namespace TorannMagic
{
	public class CompUseEffect_LearnMight : CompUseEffect
	{

		public override void DoEffect(Pawn user)
		{
            if (parent.def != null)
            {
                bool customClass = false;
                bool advancedClass = false;
                string failMessage = "";
                TMDefs.TM_CustomClass cc = null;
                CompAbilityUserMight comp = user.GetCompAbilityUserMight();

                for (int i = 0; i < TM_ClassUtility.CustomClasses.Count; i++)
                {
                    cc = TM_ClassUtility.CustomClasses[i];
                    if (cc.isFighter && cc.isAdvancedClass && comp != null)
                    {
                        if (parent.def == cc.tornScript || parent.def == cc.fullScript)
                        {
                            if (TM_Calc.HasAdvancedFighterRequirements(user, cc, out failMessage))
                            {
                                advancedClass = true;
                                ApplyAdvancedTrait(user, cc);
                                this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                            }
                            break;
                        }

                    }
                    else if ((cc.isFighter && user.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy)) || (cc.isFighter && TM_ClassUtility.CustomClasses[i].isMage && (user.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted) || user.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy))))
                    {
                        if (parent.def == cc.tornScript || parent.def == cc.fullScript)
                        {
                            customClass = true;
                            if (parent.def == cc.fullScript)
                            {
                                HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_Uncertainty, 0.2f);
                            }
                            FixTrait(user, user.story.traits.allTraits);
                            user.story.traits.GainTrait(new Trait(cc.classTrait, cc.traitDegree));
                            //Unique actions hook
                            ApplyTraitAdjustments(user, cc.classTrait);
                            //
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                            if (comp != null)
                            {
                                comp.customIndex = i;
                                comp.customClass = cc;
                            }
                            else
                            {
                                Log.Message("failed to initialize custom might class comp");
                            }
                            CompAbilityUserMagic mComp = user.GetCompAbilityUserMagic();
                            if(mComp != null && cc.isMage)
                            {
                                mComp.customIndex = i;
                                mComp.customClass = cc;
                            }
                            break;
                        }
                    }
                }
                if (!customClass && !advancedClass && user.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy))
                {
                    if (parent.def.defName == "BookOfGladiator")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Gladiator, 0, false));
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        comp.skill_Sprint = true;
                    }
                    else if (parent.def.defName == "BookOfSniper")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Sniper, 0, false));
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfBladedancer")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Bladedancer, 0, false));
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfRanger")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Ranger, 0, false));
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfFaceless")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.Faceless, 0, false));
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfDeathKnight")
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.DeathKnight, 0, false));
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def.defName == "BookOfPsionic")
                    {
                        if (user.GetStatValue(StatDefOf.PsychicSensitivity, false) > 1)
                        {
                            FixTrait(user, user.story.traits.allTraits);
                            user.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Psionic, 0, false));
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else if (user.Map.GameConditionManager.ConditionIsActive(GameConditionDefOf.PsychicDrone) || user.Map.GameConditionManager.ConditionIsActive(GameConditionDefOf.PsychicSoothe))
                        {
                            FixTrait(user, user.story.traits.allTraits);
                            user.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Psionic, 0, false));
                            this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                        else
                        {
                            Messages.Message("TM_NotPsychicSensitive".Translate(
                                user.LabelShort
                            ), MessageTypeDefOf.RejectInput);
                        }
                    }
                    else if (parent.def == TorannMagicDefOf.BookOfMonk)
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Monk, 0, false));
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def == TorannMagicDefOf.BookOfCommander)
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Commander, 0, false));
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else if (parent.def == TorannMagicDefOf.BookOfSuperSoldier)
                    {
                        FixTrait(user, user.story.traits.allTraits);
                        user.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_SuperSoldier, 0, false));
                        if (user.health != null && user.health.hediffSet != null)
                        {
                            HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_SS_SerumHD, .1f);
                        }
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                    else
                    {
                        Messages.Message("NotCombatBook".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else if (parent.def == TorannMagicDefOf.BookOfSuperSoldier)
                {
                    if (user.health != null && user.health.hediffSet != null)
                    {
                        HealthUtility.AdjustSeverity(user, TorannMagicDefOf.TM_SS_SerumHD, .1f);
                    }
                    if (cc != null && (cc.fullScript == TorannMagicDefOf.BookOfSuperSoldier || cc.tornScript == TorannMagicDefOf.BookOfSuperSoldier))
                    {
                        
                    }
                    else
                    {
                        this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                    }
                }
                else if(!customClass && !advancedClass && !user.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy))
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
                        Messages.Message("NotPhyAdeptPawn".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                //ResolveClassPassions(user);  currently disabled

            }
            else
            {
                Messages.Message("TM_InvalidAction".Translate(
                        user.LabelShort,
                        "might book"
                    ), MessageTypeDefOf.RejectInput);
            }           

		}

        public static void ApplyAdvancedTrait(Pawn p, TMDefs.TM_CustomClass cc)
        {
            TraitDef trait = cc.classTrait;            
            p.story.traits.GainTrait(new Trait(cc.classTrait, cc.traitDegree));

            if (cc.advancedClassOptions != null && cc.advancedClassOptions.removesRequiredTrait)
            {
                List<Trait> pawnTraits = p.story.traits.allTraits;
                foreach (TraitDef td in cc.advancedClassOptions.requiredTraits)
                {
                    foreach (Trait t in pawnTraits)
                    {
                        if (t.def == td)
                        {
                            p.story.traits.RemoveTrait(t);
                            goto TraitGainEnd;
                        }
                    }
                }
            }
            TraitGainEnd:;
        }

        private void FixTrait(Pawn pawn, List<Trait> traits)
        {
            TraitStart:;
            for (int i = 0; i < traits.Count; i++)
            {
                if (traits[i].def == TorannMagicDefOf.PhysicalProdigy)
                {
                    traits.Remove(traits[i]);
                    goto TraitStart;
                }
                if (traits[i].def == TorannMagicDefOf.TM_Gifted)
                {
                    traits.Remove(traits[i]);
                    goto TraitStart;
                }                
            }
        }

        private void ResolveClassPassions(Pawn p)
        {
            SkillRecord skill;
            if (p.story.traits.HasTrait(TorannMagicDefOf.Bladedancer))
            {
                skill = p.skills.GetSkill(SkillDefOf.Melee);
                if (skill.passion != Passion.Major)
                {
                    skill.passion = Passion.Major;
                }
                skill = p.skills.GetSkill(SkillDefOf.Shooting);
                if (skill.passion != Passion.None)
                {
                    skill.passion = Passion.None;
                }
            }
            if (p.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper))
            {
                skill = p.skills.GetSkill(SkillDefOf.Melee);
                if (skill.passion != Passion.None)
                {
                    skill.passion = Passion.None;
                }
                skill = p.skills.GetSkill(SkillDefOf.Shooting);
                if (skill.passion != Passion.Major)
                {
                    skill.passion = Passion.Major;
                }
            }
            if (p.story.traits.HasTrait(TorannMagicDefOf.Gladiator))
            {
                skill = p.skills.GetSkill(SkillDefOf.Melee);
                if (skill.passion == Passion.None)
                {
                    skill.passion = Passion.Minor;
                }
                skill = p.skills.GetSkill(SkillDefOf.Shooting);
                if (skill.passion == Passion.None)
                {
                    skill.passion = Passion.Minor;
                }
            }
            if (p.story.traits.HasTrait(TorannMagicDefOf.Ranger))
            {
                skill = p.skills.GetSkill(SkillDefOf.Melee);
                if (skill.passion == Passion.None)
                {
                    skill.passion = Passion.Minor;
                }
                skill = p.skills.GetSkill(SkillDefOf.Shooting);
                if (skill.passion == Passion.None)
                {
                    skill.passion = Passion.Minor;
                }
            }
            if (p.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier))
            {
                skill = p.skills.GetSkill(SkillDefOf.Melee);
                if (skill.passion == Passion.None)
                {
                    skill.passion = Passion.Minor;
                }
                skill = p.skills.GetSkill(SkillDefOf.Shooting);
                if (skill.passion == Passion.Minor)
                {
                    skill.passion = Passion.Major;
                }
                if (skill.passion == Passion.None)
                {
                    skill.passion = Passion.Minor;
                }
            }
            if (p.story.traits.HasTrait(TorannMagicDefOf.TM_Commander))
            {
                skill = p.skills.GetSkill(SkillDefOf.Social);
                if (skill.passion == Passion.Minor)
                {
                    skill.passion = Passion.Major;
                }
                if (skill.passion == Passion.None)
                {
                    skill.passion = Passion.Minor;
                }
            }
        }

        public void ApplyTraitAdjustments(Pawn pawn, TraitDef traitDef)
        {
            
        }
    }
}
