using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using RimWorld;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using AbilityUser;
using TorannMagic.Enchantment;
using System.Text;
using TorannMagic.TMDefs;
using System.Reflection;
using TorannMagic.Golems;


namespace TorannMagic
{
    public static class TM_Calc
    {
        //Extensions
        // Non-generic GetComp<CompAbilityUserMagic> for performance since isInst against generic T is slow
        public static CompAbilityUserMagic GetCompAbilityUserMagic(this ThingWithComps thingWithComps)
        {
            for (int i = 0; i < thingWithComps.AllComps.Count; i++)
            {
                if (thingWithComps.AllComps[i] is CompAbilityUserMagic comp)
                    return comp;
            }

            return null;
        }

        // Non-generic GetComp<CompAbilityUserMight> for performance since isInst against generic T is slow
        public static CompAbilityUserMight GetCompAbilityUserMight(this ThingWithComps thingWithComps)
        {
            for (int i = 0; i < thingWithComps.AllComps.Count; i++)
            {
                if (thingWithComps.AllComps[i] is CompAbilityUserMight comp)
                    return comp;
            }

            return null;
        }


        public static bool IsRobotPawn(Pawn pawn)
        {
            bool flag_Core = pawn.RaceProps.IsMechanoid;
            bool flag_AndroidTiers = (pawn.def.defName.StartsWith("Android") || pawn.def.defName == "M7Mech" || pawn.def.defName == "MicroScyther");
            bool flag_Androids = pawn.RaceProps.FleshType.defName == "ChJDroid" || pawn.def.defName == "ChjAndroid";
            bool flag_AndroidClass = false;
            CompAbilityUserMight compMight = pawn.GetCompAbilityUserMight();
            if (compMight != null && compMight.customClass != null && compMight.customClass.isAndroid)
            {
                flag_AndroidClass = true;
            }
            CompAbilityUserMagic compMagic = pawn.GetCompAbilityUserMagic();
            if (compMagic != null && compMagic.customClass != null && compMagic.customClass.isAndroid)
            {
                flag_AndroidClass = true;
            }
            bool isRobot = flag_Core || flag_AndroidTiers || flag_Androids || flag_AndroidClass;
            return isRobot;
        }

        public static bool IsNecromancer(Pawn pawn)
        {
            if (pawn != null)
            {                
                bool flag_DefName = false;
                if (pawn.def == TorannMagicDefOf.TM_SkeletonLichR)
                {
                    flag_DefName = true;
                }
                bool flag_Trait = false;
                if (pawn.story != null && pawn.story.traits != null)
                {
                    if (pawn.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.Lich))
                    {
                        flag_Trait = true;
                    }
                }
                bool flag_Class = false;
                CompAbilityUserMight compMight = pawn.GetCompAbilityUserMight();
                if (compMight != null && compMight.customClass != null && compMight.customClass.isNecromancer)
                {
                    flag_Class = true;
                }
                CompAbilityUserMagic compMagic = pawn.GetCompAbilityUserMagic();
                if (compMagic != null && compMagic.customClass != null && compMagic.customClass.isNecromancer)
                {
                    flag_Class = true;
                }
                bool isNecromancer = flag_DefName || flag_Trait || flag_Class;
                return isNecromancer;
            }
            return false;
        }

        public static bool IsUndead(Pawn pawn)
        {
            if (pawn == null) return false;

            if (pawn.health?.hediffSet != null && pawn.health.hediffSet.hediffs.Any(hediff =>
                hediff.def == TorannMagicDefOf.TM_UndeadHD
                || hediff.def == TorannMagicDefOf.TM_UndeadAnimalHD
                || hediff.def == TorannMagicDefOf.TM_LichHD
                || hediff.def == TorannMagicDefOf.TM_UndeadStageHD
                || hediff.def.defName.StartsWith("ROM_Vamp")
            ))
            {
                return true;
            }

            if (pawn.def.defName == "SL_Runner" || pawn.def.defName == "SL_Peon" || pawn.def.defName == "SL_Archer" || pawn.def.defName == "SL_Hero" ||
                pawn.def == TorannMagicDefOf.TM_GiantSkeletonR || pawn.def == TorannMagicDefOf.TM_SkeletonR || pawn.def == TorannMagicDefOf.TM_SkeletonLichR)
            {
                return true;
            }

            if (pawn.story?.traits != null && pawn.story.traits.HasTrait(TorannMagicDefOf.Undead)) return true;

            for (int i = 0; i < pawn.AllComps.Count; i++)
            {
                if (pawn.AllComps[i] is CompAbilityUserTMBase comp)
                {
                    if (comp.customClass != null && comp.customClass.isUndead) return true;
                }
            }

            return false;
        }

        public static bool IsElemental(Pawn pawn)
        {
            return pawn?.def != null && (
                pawn.def == TorannMagicDefOf.TM_LesserEarth_ElementalR
                || pawn.def == TorannMagicDefOf.TM_LesserFire_ElementalR
                || pawn.def == TorannMagicDefOf.TM_LesserWater_ElementalR
                || pawn.def == TorannMagicDefOf.TM_LesserWind_ElementalR
                || pawn.def == TorannMagicDefOf.TM_Earth_ElementalR
                || pawn.def == TorannMagicDefOf.TM_Fire_ElementalR
                || pawn.def == TorannMagicDefOf.TM_Water_ElementalR
                || pawn.def == TorannMagicDefOf.TM_Wind_ElementalR
                || pawn.def == TorannMagicDefOf.TM_GreaterEarth_ElementalR
                || pawn.def == TorannMagicDefOf.TM_GreaterFire_ElementalR
                || pawn.def == TorannMagicDefOf.TM_GreaterWater_ElementalR
                || pawn.def == TorannMagicDefOf.TM_GreaterWind_ElementalR
            );
        }

        public static bool IsUndeadNotVamp(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return false;
            if (pawn.health.hediffSet.hediffs.Any((Hediff x) => x.def.defName.StartsWith("ROM_Vamp"))) return false;
            return IsUndead(pawn);
        }

        public static bool IsGolem(Pawn p)
        {
            if (p?.health.hediffSet == null) return false;

            TMPawnGolem pg = p as TMPawnGolem;
            if(pg != null)
            {
                return true;
            }
            CompGolem cg = p.TryGetComp<CompGolem>();
            if(cg != null)
            {
                return true;
            }
            if(p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_GolemHD))
            {
                return true;
            }

            return false;
        }

        public static bool IsGolemBuilding(Thing b)
        {
            if(b != null && b is Building gb)
            {
                if(gb != null)
                {
                    return true;
                }
            }
            return false;
        }

        public static Hediff GetHateHediff(Pawn pawn)
        {
            for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
            {
                if (pawn.health.hediffSet.hediffs[i].def == TorannMagicDefOf.TM_HateHD
                    || pawn.health.hediffSet.hediffs[i].def == TorannMagicDefOf.TM_HateHD_I
                    || pawn.health.hediffSet.hediffs[i].def == TorannMagicDefOf.TM_HateHD_II
                    || pawn.health.hediffSet.hediffs[i].def == TorannMagicDefOf.TM_HateHD_III
                    || pawn.health.hediffSet.hediffs[i].def == TorannMagicDefOf.TM_HateHD_IV
                    || pawn.health.hediffSet.hediffs[i].def == TorannMagicDefOf.TM_HateHD_V
                )
                    return pawn.health.hediffSet.hediffs[i];
            }
            return null;
        }

        public static bool HasHateHediff(Pawn pawn)
        {
            for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
            {
                if (pawn.health.hediffSet.hediffs[i].def == TorannMagicDefOf.TM_HateHD
                    || pawn.health.hediffSet.hediffs[i].def == TorannMagicDefOf.TM_HateHD_I
                    || pawn.health.hediffSet.hediffs[i].def == TorannMagicDefOf.TM_HateHD_II
                    || pawn.health.hediffSet.hediffs[i].def == TorannMagicDefOf.TM_HateHD_III
                    || pawn.health.hediffSet.hediffs[i].def == TorannMagicDefOf.TM_HateHD_IV
                    || pawn.health.hediffSet.hediffs[i].def == TorannMagicDefOf.TM_HateHD_V
                )
                    return true;
            }
            return false;
        }

        public static bool IsPolymorphed(Pawn p)
        {
            CompPolymorph cp = p.GetComp<CompPolymorph>();
            if (cp != null && cp.Original != null && cp.Original.RaceProps.Humanlike)
            {
                return true;
            }
            return false;            
        }

        public static bool IsSpirit(Pawn p)
        {
            if(p.def == TorannMagicDefOf.TM_SpiritTD)
            {
                return true;
            }
            if (p != null && p.health != null && p.health.hediffSet != null)
            {
                if (p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessorHD) != null)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        public static bool IsPossessedBySpirit(Pawn p)
        {
            if(p != null && p.health != null && p.health.hediffSet != null)
            {                
                if(p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD) != null)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        public static bool IsPossessedByOrIsSpirit(Pawn p)
        {
            return IsSpirit(p) || IsPossessedBySpirit(p);
        }

        public static Hediff GetLinkedHediff(Pawn p, HediffDef starter)
        {
            if(starter == null)
            {
                return null;
            }
            Hediff outHediff = null;
            if(p != null && p.health != null && p.health.hediffSet != null)
            {
                if(p.health.hediffSet.HasHediff(starter, false))
                {
                    return p.health.hediffSet.GetFirstHediffOfDef(starter);
                }
                else
                {
                    foreach(Hediff h in p.health.hediffSet.hediffs)
                    {
                        if(h.def.defName.StartsWith(starter.defName))
                        {
                            return h;
                        }
                    }
                }
            }
            return outHediff;
        }

        public static bool IsWall(Thing t)
        {
            if(t != null && t is Building b)
            {
                if (b.def.passability == Traversability.Impassable && b.def.holdsRoof)
                {
                    if (t.def.defName.ToLower().Contains("wall") || (t.def.label.ToLower().Contains("wall")))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsMightUser(Pawn pawn)
        {
            if (pawn != null)
            {
                CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
                if (comp != null && comp.IsMightUser && comp.MightData != null && comp.Stamina != null)
                {
                    return true;
                }
                if (pawn.needs != null)
                {
                    List<Need> needs = pawn.needs.AllNeeds;
                    for (int i = 0; i < needs.Count; i++)
                    {
                        if (needs[i].def.defName == "TM_Stamina")
                        {
                            return true;
                        }
                    }
                }
                if (pawn.story != null && pawn.story.traits != null && pawn.story.traits.allTraits != null)
                {
                    for (int i = 0; i < pawn.story.traits.allTraits.Count; i++)
                    {
                        if (TM_Data.MightTraits.Contains(pawn.story.traits.allTraits[i].def))
                        {
                            return true;
                        }
                    }                    
                }
            }
            return false;
        }

        public static TraitDef GetMightTrait(Pawn pawn)
        {
            if (pawn.story != null && pawn.story.traits != null)
            {
                for (int i = 0; i < pawn.story.traits.allTraits.Count; i++)
                {
                    TraitDef td = pawn.story.traits.allTraits[i].def;
                    if(td == TorannMagicDefOf.Bladedancer || td == TorannMagicDefOf.Gladiator || td == TorannMagicDefOf.Faceless || td == TorannMagicDefOf.TM_Sniper || td == TorannMagicDefOf.Ranger ||
                        td == TorannMagicDefOf.TM_Psionic || td == TorannMagicDefOf.TM_Monk || td == TorannMagicDefOf.TM_Wayfarer || td == TorannMagicDefOf.TM_SuperSoldier || td == TorannMagicDefOf.TM_Commander)
                    {
                        return td;
                    }
                }
                CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
                if(comp != null && comp.customClass != null)
                {
                    return comp.customClass.classTrait;
                }
            }
            return null;
        }

        public static bool IsMagicUser(Pawn pawn)
        {
            if (pawn != null)
            {
                if (pawn.story != null && pawn.story.traits != null && pawn.story.traits.allTraits != null)
                {
                    if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                    {
                        return false;
                    }
                    for (int i = 0; i < pawn.story.traits.allTraits.Count; i++)
                    {
                        if (TM_Data.MagicTraits.Contains(pawn.story.traits.allTraits[i].def))
                        {
                            return true;
                        }
                    }                    
                }
                else
                {
                    return false;
                }
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                if(comp != null && comp.IsMagicUser && comp.MagicData != null && comp.Mana != null)
                {
                    return true;
                }
                if (pawn.needs != null)
                {
                    List<Need> needs = pawn.needs.AllNeeds;
                    for (int i = 0; i < needs.Count; i++)
                    {
                        if (needs[i].def.defName == "TM_Mana")
                        {
                            return true;
                        }
                    }
                }                                
            }
            return false;
        }

        public static bool HasAdvancedClass(Pawn p)
        {               
            if (p != null && p.story != null && p.story.traits != null)
            {
                List<TM_CustomClass> customClasses = TM_ClassUtility.CustomClasses;
                for (int i = 0; i < customClasses.Count; i++)
                {
                    if (customClasses[i].isAdvancedClass)
                    {
                        foreach(Trait t in p.story.traits.allTraits)
                        {
                            if(t.def == customClasses[i].classTrait)
                            {
                                return true;
                            }
                        }                          
                    }
                }
            }
            return false;            
        }

        public static bool HasAdvancedMageRequirements(Pawn p, TMDefs.TM_CustomClass cc, out string failMessage)
        {
            bool hasReqTrait = true;
            failMessage = "";
            if (cc.advancedClassOptions != null)
            {
                CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();                
                if (comp == null)
                {
                    failMessage = "null magic comp"; //this should never happen
                    return false;
                }
                if (cc.advancedClassOptions.requiresBaseClass && !comp.IsMagicUser)
                {
                    failMessage = "TM_LearnFail_NotClass".Translate();
                    return false;
                }
                if (cc.advancedClassOptions.requiredTraits != null && cc.advancedClassOptions.requiredTraits.Count > 0)
                {
                    hasReqTrait = false;
                    foreach(TraitDef td in cc.advancedClassOptions.requiredTraits)
                    {
                        foreach(Trait t in p.story.traits.allTraits)
                        {
                            if(t.def == td)
                            {
                                hasReqTrait = true;
                                break;
                            }
                        }
                        if(hasReqTrait)
                        {
                            break;
                        }
                    }                    
                }
                if(cc.advancedClassOptions.disallowedTraits != null && cc.advancedClassOptions.disallowedTraits.Count > 0)
                {
                    foreach(TraitDef td in cc.advancedClassOptions.disallowedTraits)
                    {
                        foreach(Trait t in p.story.traits.allTraits)
                        {
                            if(t.def == td)
                            {
                                failMessage = "TM_LearnFail_DisallowedTrait".Translate();
                                return false;
                            }
                        }
                    }
                }
            }
            if(!hasReqTrait)
            {
                failMessage = "TM_LearnFail_NoRequiredTrait".Translate();
            }
            return hasReqTrait;
        }

        public static bool HasAdvancedFighterRequirements(Pawn p, TMDefs.TM_CustomClass cc, out string failMessage)
        {
            bool hasReqTrait = true;
            failMessage = "";
            if (cc.advancedClassOptions != null)
            {
                CompAbilityUserMight comp = p.GetCompAbilityUserMight();
                if (comp == null)
                {
                    failMessage = "null might comp"; //this should never happen
                    return false;
                }
                if (cc.advancedClassOptions.requiresBaseClass && !comp.IsMightUser)
                {
                    failMessage = "TM_LearnFail_NotClass".Translate();
                    return false;
                }
                if (cc.advancedClassOptions.requiredTraits != null && cc.advancedClassOptions.requiredTraits.Count > 0)
                {
                    hasReqTrait = false;
                    foreach (TraitDef td in cc.advancedClassOptions.requiredTraits)
                    {
                        foreach (Trait t in p.story.traits.allTraits)
                        {
                            if (t.def == td)
                            {
                                hasReqTrait = true;
                                break;
                            }
                        }
                        if (hasReqTrait)
                        {
                            break;
                        }
                    }
                }
                if (cc.advancedClassOptions.disallowedTraits != null && cc.advancedClassOptions.disallowedTraits.Count > 0)
                {
                    foreach (TraitDef td in cc.advancedClassOptions.disallowedTraits)
                    {
                        foreach (Trait t in p.story.traits.allTraits)
                        {
                            if (t.def == td)
                            {
                                failMessage = "TM_LearnFail_DisallowedTrait".Translate();
                                return false;
                            }
                        }
                    }
                }
            }
            if (!hasReqTrait)
            {
                failMessage = "TM_LearnFail_NoRequiredTrait".Translate();
            }
            return hasReqTrait;
        }

        public static bool IsWanderer(Pawn pawn)
        {
            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
            if (comp != null)
            {
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer))
                {
                    return true;
                }
                else if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) || (comp.customClass != null && comp.customClass.classFighterAbilities.Contains(TorannMagicDefOf.TM_FieldTraining))) //pawn is a wayfarer with appropriate skill level
                {
                    int lvl = comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_eff").level;
                    if (lvl >= 15)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsWayfarer(Pawn pawn)
        {
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if (comp != null)
            {
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer))
                {
                    return true;
                }
                else if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer) || (comp.customClass != null && comp.customClass.classMageAbilities.Contains(TorannMagicDefOf.TM_Cantrips))) //pawn is a wanderer with appropriate skill level
                {
                    int lvl = comp.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_eff").level;
                    if (lvl >= 15)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsEmpath(Pawn pawn)
        {
            if (pawn.story != null && pawn.story.traits != null && pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Empath))
            {
                return true;
            }
            if (pawn.health != null && pawn.health.hediffSet != null && pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EmpathHD) != null)
            {
                return true;
            }
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if(comp != null && comp.customClass != null && comp.customClass.classAbilities.Contains(TorannMagicDefOf.TM_Empathy))
            {
                return true;
            }
            return false;
        }

        public static bool IsCrossClass(Pawn pawn, bool forMagic)
        {
            if (pawn.story != null && pawn.story.traits != null)
            {
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) && forMagic)
                {
                    return true;
                }

                if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer) && !forMagic)
                {
                    return true;
                }

                if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) && forMagic)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasResourcesForAbility(Pawn p, TMAbilityDef ability)
        {
            if(ability.manaCost > 0)
            {
                CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();
                if(comp == null)
                {
                    return false;
                }
                if(comp.Mana == null)
                {
                    return false;
                }
                if(comp.Mana.CurLevel < comp.ActualManaCost(ability))
                {
                    return false;
                }
            }
            if(ability.staminaCost > 0)
            {
                CompAbilityUserMight comp = p.GetCompAbilityUserMight();
                if (comp == null)
                {
                    return false;
                }
                if (comp.Stamina == null)
                {
                    return false;
                }
                if (comp.Stamina.CurLevel < comp.ActualStaminaCost(ability))
                {
                    return false;
                }
            }
            if(ability.chiCost > 0)
            {
                CompAbilityUserMight comp = p.GetCompAbilityUserMight();
                if (comp == null)
                {
                    return false;
                }
                if(p.health == null || p.health.hediffSet == null)
                {
                    return false;
                }
                Hediff chi = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD);
                if(chi == null)
                {
                    return false;
                }
                if(chi.Severity < comp.ActualChiCost(ability))
                {
                    return false;
                }
            }
            if(ability.bloodCost > 0)
            {
                CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();
                if (comp == null)
                {
                    return false;
                }
                if (p.health == null || p.health.hediffSet == null)
                {
                    return false;
                }
                Hediff blood = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_BloodHD);
                if (blood == null)
                {
                    return false;
                }
                MagicAbility ma = (MagicAbility)comp.AbilityData.AllPowers.FirstOrDefault((PawnAbility x) => x.Def == ability);
                if(ma == null)
                {
                    return false;
                }
                if (blood.Severity < ma.ActualBloodCost)
                {
                    return false;
                }
            }
            if(ability.requiredHediff != null)
            {
                if(p.health == null || p.health.hediffSet == null)
                {
                    return false;
                }
                Hediff hd = p.health.hediffSet.GetFirstHediffOfDef(ability.requiredHediff);
                if (hd == null)
                {
                    return false;
                }
                if(hd.Severity < ability.hediffCost)
                {
                    return false;
                }
            }
            if(ability.requiredNeed != null)
            {
                if(p.needs == null || p.needs.AllNeeds == null)
                {
                    return false;
                }
                Need nd = p.needs.TryGetNeed(ability.requiredNeed);
                if(nd == null)
                {
                    return false;
                }
                if(nd.CurLevel < ability.needCost)
                {
                    return false;
                }
            }
            if(ability.requiredWeaponsOrCategories != null && ability.requiredWeaponsOrCategories.Count > 0)
            {
                if(p.equipment == null)
                {
                    return false;
                }
                if(ability.IsRestrictedByEquipment(p))
                {
                    return false;
                }
            }
            if(ability.requiredInspiration != null)
            {
                if(!p.Inspired)
                {
                    return false;
                }
                if(p.InspirationDef != ability.requiredInspiration)
                {
                    return false;
                }
            }
            if(ability.requiresAnyInspiration)
            {
                if(!p.Inspired)
                {
                    return false;
                }
            }
            return true;
        }

        public static int GetMagesInFactionCount(Faction faction, bool countSlaves = false)
        {
            if (faction == null)
            {
                return 0;
            }
            int num = 0;
            foreach (Pawn p in PawnsFinder.AllMaps_SpawnedPawnsInFaction(faction))
            {
                if (IsMagicUser(p) && (p.IsSlave ? countSlaves : true))
                {
                    num++;
                }
            }
            return num;
        }

        public static bool HasMageInFaction(Faction faction, bool countSlaves = false, bool countOnlySlaves = false)
        {
            if (faction == null)
            {
                return false;
            }
            foreach (Pawn p in PawnsFinder.AllMaps_SpawnedPawnsInFaction(faction))
            {
                if(countOnlySlaves)
                {
                    if(IsMagicUser(p) && p.IsSlave)
                    {
                        return true;
                    }
                }
                else if (IsMagicUser(p) && p.IsSlave ? countSlaves : false)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasRuneCarverOnMap(Faction faction, Map map, bool countSlaves = false)
        {
            if (faction == null)
            {
                return false;
            }
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if(IsMagicUser(p) && p.IsSlave ? countSlaves : true)
                {
                    CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();
                    if(comp!= null && comp.MagicData != null)
                    {
                        MagicPower mp = comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_RuneCarving);
                        if(mp != null && mp.learned)
                        {
                            return true;
                        }                            
                    }
                }
            }
            return false;
        }

        public static List<Pawn> GolemancersInFaction(Faction faction)
        {
            if (faction == null)
            {
                return null;
            }
            List<Pawn> tmpList = new List<Pawn>();
            tmpList.Clear();
            foreach (Pawn p in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists)
            {
                if (IsMagicUser(p))
                {
                    CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();
                    if (comp != null && comp.MagicData != null)
                    {
                        MagicPower mp = comp.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Golemancy);
                        if (mp != null && mp.learned)
                        {
                            tmpList.Add(p);
                        }
                    }
                }
            }
            return tmpList;
        }

        public static int GetFightersInFactionCount(Faction faction, bool countSlaves = false)
        {
            if (faction == null)
            {
                return 0;
            }
            int num = 0;
            foreach (Pawn p in PawnsFinder.AllMaps_SpawnedPawnsInFaction(faction))
            {
                if (IsMightUser(p) && p.IsSlave ? countSlaves : true)
                {
                    num++;
                }
            }
            return num;
        }

        public static bool HasFighterInFaction(Faction faction, bool countSlaves = false, bool countOnlySlaves = false)
        {
            if (faction == null)
            {
                return false;
            }
            foreach (Pawn p in PawnsFinder.AllMaps_SpawnedPawnsInFaction(faction))
            {
                if (countOnlySlaves)
                {
                    if (IsMagicUser(p) && p.IsSlave)
                    {
                        return true;
                    }
                }
                else if (IsMightUser(p) && p.IsSlave ? countSlaves : true)
                {
                    return true;
                }
            }
            return false;
        }

        public static Pawn GetPawnForSeverenceRetaliation(Faction f)
        {
            Pawn pawn = null;
            List<Pawn> allPawns = PawnsFinder.AllMaps_SpawnedPawnsInFaction(f);
            foreach(Pawn p in allPawns)
            {
                if(p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MagicSeverenceHD))
                {
                    TorannMagic.Ideology.HediffComp_MagicSeverence hd_ms = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MagicSeverenceHD).TryGetComp<TorannMagic.Ideology.HediffComp_MagicSeverence>();
                    if(hd_ms.selectableForRetaliation)
                    {
                        pawn = p;
                        hd_ms.selectableForRetaliation = false;
                        hd_ms.delayedMindburn = true;
                        break;
                    }
                }
            }
            return pawn;
        }

        public static Pawn GetPawnForBestowInspiration(Faction f, int index)
        {
            Pawn pawn = null;
            List<Pawn> allPawns = PawnsFinder.AllMaps_SpawnedPawnsInFaction(f);
            foreach (Pawn p in allPawns)
            {
                if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BestowMagicClassHD))
                {
                    TorannMagic.Ideology.HediffComp_BestowMagicClass hd_ms = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_BestowMagicClassHD).TryGetComp<TorannMagic.Ideology.HediffComp_BestowMagicClass>();
                    if (hd_ms.selectableForInspiration)
                    {
                        hd_ms.selectableForInspiration = false;
                        if (index == 2)
                        {
                            hd_ms.delayedInspiration = true;
                        }
                        if(index == -2)
                        {
                            hd_ms.botchedRitual = true;
                        }
                        break;
                    }
                }
            }
            return pawn;
        }

        public static int GetRandomAcceptableMagicClassIndex(Pawn p)
        {
            int result = -1;
            while(result < 0)
            {
                int tmpIndex = Rand.RangeInclusive(0, TM_Data.EnabledMagicTraits.Count - 1);                
                TraitDef td = TM_Data.EnabledMagicTraits[tmpIndex];
                if (td == TorannMagicDefOf.TM_Wanderer) { }
                else if (TM_ClassUtility.CustomAdvancedClasses.Any((TM_CustomClass x) => x.classTrait == td)) { }
                else if (td == TorannMagicDefOf.Lich) { }
                else if (td == TorannMagicDefOf.Warlock && p.gender == Gender.Female) { }
                else if (td == TorannMagicDefOf.Succubus && p.gender == Gender.Male) { }
                else
                {
                    result = tmpIndex;
                }                
            }
            return result;
        }

        public static bool IsPawnInjured(Pawn targetPawn, float minInjurySeverity = 0)
        {
            float injurySeverity = 0;
            using (IEnumerator<BodyPartRecord> enumerator = targetPawn.health.hediffSet.GetInjuredParts().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    BodyPartRecord rec = enumerator.Current;
                    IEnumerable<Hediff_Injury> arg_BB_0 = targetPawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                    Func<Hediff_Injury, bool> arg_BB_1;
                    arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                    foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                    {
                        bool flag5 = current.CanHealNaturally() && !current.IsPermanent();
                        if (flag5)
                        {
                            injurySeverity += current.Severity;
                        }
                    }
                }
            }
            return injurySeverity > minInjurySeverity;
        }

        public static List<Hediff> GetPawnAfflictions(Pawn targetPawn)
        {
            List<Hediff> afflictionList = new List<Hediff>();
            afflictionList.Clear();
            using (IEnumerator<Hediff> enumerator = targetPawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Hediff rec = enumerator.Current;
                    if (rec.def.isBad && rec.def.makesSickThought)
                    {
                        afflictionList.Add(rec);
                    }
                }
            }
            return afflictionList;
        }

        public static List<Hediff> GetPawnAddictions(Pawn targetPawn)
        {
            List<Hediff> addictionList = new List<Hediff>();
            addictionList.Clear();
            using (IEnumerator<Hediff_Addiction> enumerator = targetPawn.health.hediffSet.GetHediffs<Hediff_Addiction>().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Hediff_Addiction rec = enumerator.Current;
                    if (rec.Chemical.addictionHediff != null)
                    {
                        addictionList.Add(rec);
                    }
                }
            }
            return addictionList;
        }

        public static Vector3 GetVectorBetween(Vector3 v1, Vector3 v2)
        {
            Vector3 vectorBetween = v1 + ((v1 - v2).magnitude * .5f * TM_Calc.GetVector(v1, v2));
            return vectorBetween;
        }

        public static Vector3 GetVector(IntVec3 from, IntVec3 to)
        {
            Vector3 heading = (to - from).ToVector3();
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }

        public static Vector3 GetVector(Vector3 from, Vector3 to)
        {
            Vector3 heading = (to - from);
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }

        public static Pawn FindNearbyOtherPawn(Pawn pawn, int radius)
        {
            List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && !targetPawn.Downed)
                {
                    if (targetPawn != pawn && !targetPawn.HostileTo(pawn.Faction) && (pawn.Position - targetPawn.Position).LengthHorizontal <= radius)
                    {
                        pawnList.Add(targetPawn);
                        targetPawn = null;
                    }
                    else
                    {
                        targetPawn = null;
                    }
                }
            }
            if (pawnList.Count > 0)
            {
                return pawnList.RandomElement();
            }
            else
            {
                return null;
            }
        }

        public static Pawn FindNearbyPawn(Pawn pawn, int radius)
        {
            List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && !targetPawn.Downed)
                {
                    if (!targetPawn.HostileTo(pawn.Faction) && (pawn.Position - targetPawn.Position).LengthHorizontal <= radius)
                    {
                        pawnList.Add(targetPawn);
                        targetPawn = null;
                    }
                    else
                    {
                        targetPawn = null;
                    }
                }
            }
            if (pawnList.Count > 0)
            {
                return pawnList.RandomElement();
            }
            else
            {
                return null;
            }
        }

        public static Pawn FindNearbyFactionPawn(Pawn pawn, Faction faction, int radius)
        {
            List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && !targetPawn.Downed)
                {
                    if (targetPawn.Faction == faction && (pawn.Position - targetPawn.Position).LengthHorizontal <= radius)
                    {
                        pawnList.Add(targetPawn);
                        targetPawn = null;
                    }
                    else
                    {
                        targetPawn = null;
                    }
                }
            }
            if (pawnList.Count > 0)
            {
                return pawnList.RandomElement();
            }
            else
            {
                return null;
            }
        }

        public static Pawn FindNearbyOtherFactionPawn(Pawn pawn, Faction faction, int radius)
        {
            List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && !targetPawn.Downed && targetPawn != pawn)
                {
                    if (targetPawn.Faction == faction && (pawn.Position - targetPawn.Position).LengthHorizontal <= radius)
                    {
                        pawnList.Add(targetPawn);
                        targetPawn = null;
                    }
                    else
                    {
                        targetPawn = null;
                    }
                }
            }
            if (pawnList.Count > 0)
            {
                return pawnList.RandomElement();
            }
            else
            {
                return null;
            }
        }

        public static Pawn FindNearbyMage(Pawn pawn, int radius, bool inCombat)
        {
            List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && !targetPawn.Downed && targetPawn.Faction != null)
                {
                    if(inCombat)
                    {
                        if (pawn != targetPawn && targetPawn.HostileTo(pawn.Faction) && (pawn.Position - targetPawn.Position).LengthHorizontal <= radius)
                        {
                            CompAbilityUserMagic targetComp = targetPawn.GetCompAbilityUserMagic();
                            if (targetComp != null && targetComp.IsMagicUser && !TM_Calc.IsCrossClass(targetPawn, true))
                            {
                                pawnList.Add(targetPawn);
                            }
                        }
                    }                    
                    else
                    {
                        if (pawn != targetPawn && !targetPawn.HostileTo(pawn.Faction) && (pawn.Position - targetPawn.Position).LengthHorizontal <= radius)
                        {
                            CompAbilityUserMagic targetComp = targetPawn.GetCompAbilityUserMagic();
                            if (targetComp != null && targetComp.IsMagicUser && !TM_Calc.IsCrossClass(targetPawn, true))
                            {
                                pawnList.Add(targetPawn);                                
                            }
                        }
                    }                    
                }
                targetPawn = null;
            }
            if (pawnList.Count > 0)
            {
                return pawnList.RandomElement();
            }
            else
            {
                return null;
            }
        }

        public static List<Pawn> FindNearbyMages(IntVec3 center, Map map, Faction faction, int radius, bool friendly)
        {
            List<Pawn> mapPawns = map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && !targetPawn.Downed && targetPawn.Faction != null)
                {
                    if (targetPawn.Drafted)
                    {
                        continue;
                    }
                    if(friendly && targetPawn.Faction != faction)
                    {
                        continue;
                    }
                    else if(!friendly && targetPawn.Faction == faction)
                    {
                        continue;
                    }
                    if((targetPawn.Position - center).LengthHorizontal > radius)
                    {
                        continue;
                    }
                    if(!TM_Calc.IsMagicUser(targetPawn))
                    {
                        continue;
                    }
                    if(TM_Calc.IsCrossClass(targetPawn, true))
                    {
                        continue;
                    }
                    pawnList.Add(targetPawn);
                }
            }
            return pawnList;
        }

        public static Pawn FindNearbyFighter(Pawn pawn, int radius, bool inCombat)
        {
            List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (pawn != targetPawn && targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && !targetPawn.Downed && targetPawn.Faction != null)
                {
                    if (inCombat)
                    {
                        if (targetPawn.HostileTo(pawn.Faction) && (pawn.Position - targetPawn.Position).LengthHorizontal <= radius)
                        {
                            CompAbilityUserMight targetComp = targetPawn.GetCompAbilityUserMight();
                            if (targetComp != null && targetComp.IsMightUser && !TM_Calc.IsCrossClass(targetPawn, false))
                            {
                                pawnList.Add(targetPawn);
                            }
                        }
                    }
                    else
                    {
                        if (pawn != targetPawn && !targetPawn.HostileTo(pawn.Faction) && (pawn.Position - targetPawn.Position).LengthHorizontal <= radius)
                        {
                            CompAbilityUserMight targetComp = targetPawn.GetCompAbilityUserMight();
                            if (targetComp != null && targetComp.IsMightUser && !TM_Calc.IsCrossClass(targetPawn, false))
                            {
                                pawnList.Add(targetPawn);
                            }
                        }
                    }
                }
                targetPawn = null;
            }
            if (pawnList.Count > 0)
            {
                return pawnList.RandomElement();
            }
            else
            {
                return null;
            }
        }

        public static Pawn FindNearbyInjuredPawn(IntVec3 center, Map map, Faction fac, int radius, float minSeverity, bool includeAnimals = false)
        {
            List<Pawn> mapPawns = map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && !TM_Calc.IsUndead(targetPawn) && targetPawn.Faction != null && targetPawn.Faction == fac)
                {
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    if ((targetPawn.RaceProps.Humanlike || settingsRef.autocastAnimals || includeAnimals) && (center - targetPawn.Position).LengthHorizontal <= radius)
                    {
                        float injurySeverity = 0;
                        using (IEnumerator<BodyPartRecord> enumerator = targetPawn.health.hediffSet.GetInjuredParts().GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                BodyPartRecord rec = enumerator.Current;
                                IEnumerable<Hediff_Injury> arg_BB_0 = targetPawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                                Func<Hediff_Injury, bool> arg_BB_1;
                                arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                                foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                                {
                                    bool flag5 = current.CanHealNaturally() && !current.IsPermanent();
                                    if (flag5)
                                    {
                                        injurySeverity += current.Severity;
                                    }
                                }
                            }
                        }
                        if (minSeverity != 0)
                        {
                            if (injurySeverity >= minSeverity)
                            {
                                pawnList.Add(targetPawn);
                            }
                        }
                        else
                        {
                            if (injurySeverity != 0)
                            {
                                pawnList.Add(targetPawn);
                            }
                        }
                        targetPawn = null;
                    }
                    else
                    {
                        targetPawn = null;
                    }
                }
            }
            if (pawnList.Count > 0)
            {
                return pawnList.RandomElement();
            }
            else
            {
                return null;
            }
        }

        public static Pawn FindNearbyInjuredPawn(Pawn pawn, int radius, float minSeverity, bool includeAnimals = false)
        {
            return FindNearbyInjuredPawn(pawn.Position, pawn.Map, pawn.Faction, radius, minSeverity, includeAnimals);
        }

        public static Pawn FindNearbyInjuredPawnOther(Pawn pawn, int radius, float minSeverity)
        {
            List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && !TM_Calc.IsUndead(targetPawn) && targetPawn.Faction != null && targetPawn.Faction == pawn.Faction)
                {
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    if ((targetPawn.RaceProps.Humanlike || settingsRef.autocastAnimals) && (pawn.Position - targetPawn.Position).LengthHorizontal <= radius && targetPawn != pawn)
                    {
                        float injurySeverity = 0;
                        using (IEnumerator<BodyPartRecord> enumerator = targetPawn.health.hediffSet.GetInjuredParts().GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                BodyPartRecord rec = enumerator.Current;
                                IEnumerable<Hediff_Injury> arg_BB_0 = targetPawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                                Func<Hediff_Injury, bool> arg_BB_1;
                                arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                                foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                                {
                                    bool flag5 = current.CanHealNaturally() && !current.IsPermanent();
                                    if (flag5)
                                    {
                                        injurySeverity += current.Severity;
                                    }
                                }
                            }
                        }
                        if (minSeverity != 0)
                        {
                            if (injurySeverity >= minSeverity)
                            {
                                pawnList.Add(targetPawn);
                            }
                        }
                        else
                        {
                            if (injurySeverity != 0)
                            {
                                pawnList.Add(targetPawn);
                            }
                        }
                        targetPawn = null;
                    }
                    else
                    {
                        targetPawn = null;
                    }
                }
            }
            if (pawnList.Count > 0)
            {
                return pawnList.RandomElement();
            }
            else
            {
                return null;
            }
        }

        public static Pawn FindNearbyPermanentlyInjuredPawn(Pawn pawn, int radius, float minSeverity)
        {
            List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && !TM_Calc.IsUndead(targetPawn) && targetPawn.Faction != null && targetPawn.Faction == pawn.Faction)
                {
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    if ((targetPawn.RaceProps.Humanlike || settingsRef.autocastAnimals) && (pawn.Position - targetPawn.Position).LengthHorizontal <= radius)
                    {
                        float injurySeverity = 0;
                        using (IEnumerator<BodyPartRecord> enumerator = targetPawn.health.hediffSet.GetInjuredParts().GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                BodyPartRecord rec = enumerator.Current;
                                IEnumerable<Hediff_Injury> arg_BB_0 = targetPawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                                Func<Hediff_Injury, bool> arg_BB_1;
                                arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                                foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                                {
                                    bool flag5 = !current.CanHealNaturally() && current.IsPermanent();
                                    if (flag5)
                                    {
                                        injurySeverity += current.Severity;
                                    }
                                }
                            }
                        }
                        if (minSeverity != 0)
                        {
                            if (injurySeverity >= minSeverity)
                            {
                                pawnList.Add(targetPawn);
                            }
                        }
                        else
                        {
                            if (injurySeverity != 0)
                            {
                                pawnList.Add(targetPawn);
                            }
                        }
                        targetPawn = null;
                    }
                    else
                    {
                        targetPawn = null;
                    }
                }
            }
            if (pawnList.Count > 0)
            {
                return pawnList.RandomElement();
            }
            else
            {
                return null;
            }
        }

        public static Pawn FindNearbyAfflictedPawn(Pawn pawn, int radius, List<string> validAfflictionDefnames)
        {
            List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && targetPawn.Faction != null && targetPawn.Faction == pawn.Faction)
                {
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    if ((targetPawn.RaceProps.Humanlike || settingsRef.autocastAnimals) && (pawn.Position - targetPawn.Position).LengthHorizontal <= radius)
                    {
                        using (IEnumerator<Hediff> enumerator = targetPawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                Hediff rec = enumerator.Current;
                                for(int j =0; j < validAfflictionDefnames.Count; j++)
                                {
                                    if (rec.def.defName.Contains(validAfflictionDefnames[j]) && (rec.def.isBad || rec.def.makesSickThought))
                                    {
                                        pawnList.Add(targetPawn);
                                    }
                                }                                    
                                
                            }
                        }
                        targetPawn = null;
                    }
                    else
                    {
                        targetPawn = null;
                    }
                }
            }
            if (pawnList.Count > 0)
            {
                return pawnList.RandomElement();
            }
            else
            {
                return null;
            }
        }

        public static Pawn FindNearbyAfflictedPawnAny(Pawn pawn, int radius)
        {
            List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && targetPawn.Faction != null && targetPawn.Faction == pawn.Faction)
                {
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    if ((targetPawn.RaceProps.Humanlike || settingsRef.autocastAnimals) && (pawn.Position - targetPawn.Position).LengthHorizontal <= radius)
                    {
                        using (IEnumerator<Hediff> enumerator = targetPawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                Hediff rec = enumerator.Current;
                                if (rec.def.PossibleToDevelopImmunityNaturally() && (rec.def.isBad || rec.def.makesSickThought))
                                {
                                    pawnList.Add(targetPawn);
                                }
                                if (rec.def.defName == "BloodRot")
                                {
                                    pawnList.Add(targetPawn);
                                }
                            }
                        }
                        targetPawn = null;
                    }
                    else
                    {
                        targetPawn = null;
                    }
                }
            }
            if (pawnList.Count > 0)
            {
                //Log.Message("returning pawn list containing " + pawnList.RandomElement().LabelShort);
                return pawnList.RandomElement();
            }
            else
            {
                return null;
            }
        }

        public static Pawn FindNearbyAddictedPawn(Pawn pawn, int radius, List<string> validAddictionDefnames)
        {
            List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && targetPawn.Faction != null && targetPawn.Faction == pawn.Faction)
                {
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    if ((targetPawn.RaceProps.Humanlike || settingsRef.autocastAnimals) && (pawn.Position - targetPawn.Position).LengthHorizontal <= radius)
                    {
                        using (IEnumerator<Hediff_Addiction> enumerator = targetPawn.health.hediffSet.GetHediffs<Hediff_Addiction>().GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                Hediff_Addiction rec = enumerator.Current;
                                for (int j = 0; j < validAddictionDefnames.Count; j++)
                                {
                                    if (rec.Chemical.defName.Contains(validAddictionDefnames[j]))
                                    {
                                        pawnList.Add(targetPawn);
                                    }
                                }

                            }
                        }
                        targetPawn = null;
                    }
                    else
                    {
                        targetPawn = null;
                    }
                }
            }
            if (pawnList.Count > 0)
            {
                return pawnList.RandomElement();
            }
            else
            {
                return null;
            }
        }

        public static Pawn FindNearbyEnemy(Pawn pawn, int radius)
        {
            return FindNearbyEnemy(pawn.Position, pawn.Map, pawn.Faction, radius, 0);
        }

        public static Pawn FindNearbyEnemy(IntVec3 position, Map map, Faction faction, float radius, float minRange)
        {
            List<Pawn> mapPawns = map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && !targetPawn.Downed)
                {
                    if (targetPawn.Position != position && targetPawn.Faction.HostileTo(faction) && !targetPawn.IsPrisoner && (position - targetPawn.Position).LengthHorizontal <= radius && (position - targetPawn.Position).LengthHorizontal > minRange)
                    {
                        pawnList.Add(targetPawn);
                        targetPawn = null;
                    }
                    else
                    {
                        targetPawn = null;
                    }
                }                
            }
            if (pawnList.Count > 0)
            {
                //Log.Message("returning a pawn");
                return pawnList.RandomElement();
            }
            else
            {
                return null;
            }
        }

        public static List<Pawn> FindPawnsNearTarget(Pawn pawn, int radius, IntVec3 targetCell, bool hostile)
        {
            if (!pawn.DestroyedOrNull() && pawn.Spawned && pawn.Map != null)
            {
                List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
                List<Pawn> pawnList = new List<Pawn>();
                Pawn targetPawn = null;
                pawnList.Clear();
                for (int i = 0; i < mapPawns.Count; i++)
                {
                    targetPawn = mapPawns[i];
                    if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed && !targetPawn.Downed)
                    {
                        if (targetPawn != pawn && (targetCell - targetPawn.Position).LengthHorizontal <= radius)
                        {
                            if (hostile && targetPawn.HostileTo(pawn.Faction))
                            {
                                pawnList.Add(targetPawn);
                            }
                            else if (!hostile && !targetPawn.HostileTo(pawn.Faction))
                            {
                                pawnList.Add(targetPawn);
                            }
                        }
                        targetPawn = null;
                    }
                }
                if (pawnList.Count > 0)
                {
                    return pawnList;
                }
            }
            return null;
        }

        public static bool HasLoSFromTo(IntVec3 root, LocalTargetInfo targ, Thing caster, float minRange, float maxRange)
        {
            float range = (targ.Cell - root).LengthHorizontal;
            if (targ.HasThing && targ.Thing.Map != caster.Map)
            {
                return false;
            }
            if (range <= minRange || range >= maxRange)
            {
                return false;
            }
            CellRect cellRect = (!targ.HasThing) ? CellRect.SingleCell(targ.Cell) : targ.Thing.OccupiedRect();
            if (caster is Pawn)
            {
                if (GenSight.LineOfSight(caster.Position, targ.Cell, caster.Map, skipFirstCell: true))
                {
                    return true;
                }
                List<IntVec3> tempLeanShootSources = new List<IntVec3>();
                ShootLeanUtility.LeanShootingSourcesFromTo(root, cellRect.ClosestCellTo(root), caster.Map, tempLeanShootSources);
                for (int i = 0; i < tempLeanShootSources.Count; i++)
                {
                    IntVec3 intVec = tempLeanShootSources[i];
                    if (GenSight.LineOfSight(intVec, targ.Cell, caster.Map, skipFirstCell: true))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if(GenSight.LineOfSight(root, targ.Cell, caster.Map, skipFirstCell: true))
                {
                    return true;
                }
            }
            return false;
        }

        public static List<Thing> FindNearbyDamagedBuilding(Pawn pawn, int radius)
        {
            List<Thing> mapBuildings = pawn.Map.listerBuildingsRepairable.RepairableBuildings(pawn.Faction);
            List<Thing> buildingList = new List<Thing>();
            Building building= null;
            buildingList.Clear();
            for (int i = 0; i < mapBuildings.Count; i++)
            {
                building = mapBuildings[i] as Building;
                if (building != null && (building.Position - pawn.Position).LengthHorizontal <= radius && building.def.useHitPoints && building.HitPoints != building.MaxHitPoints)
                {
                    if (pawn.Drafted && building.def.designationCategory == DesignationCategoryDefOf.Security || building.def.building.ai_combatDangerous)
                    {
                        buildingList.Add(building);
                    }
                    else if(!pawn.Drafted)
                    {
                        buildingList.Add(building);
                    }
                }
                building = null;                
            }

            if (buildingList.Count > 0)
            {
                return buildingList;
            }
            else
            {
                return null;
            }
        }

        public static Thing FindNearbyDamagedThing(Pawn pawn, int radius)
        {
            List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            List<Thing> pawnList = new List<Thing>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && targetPawn.Spawned && !targetPawn.Dead && !targetPawn.Destroyed)
                {
                    //Log.Message("evaluating targetpawn " + targetPawn.LabelShort);
                    //Log.Message("pawn faction is " + targetPawn.Faction);
                    //Log.Message("pawn position is " + targetPawn.Position);
                    //Log.Message("pawn is robot: " + TM_Calc.IsRobotPawn(targetPawn));
                    if (targetPawn.Faction != null && targetPawn.Faction == pawn.Faction && (pawn.Position - targetPawn.Position).LengthHorizontal <= radius && TM_Calc.IsRobotPawn(targetPawn))
                    {
                        float injurySeverity = 0;
                        using (IEnumerator<BodyPartRecord> enumerator = targetPawn.health.hediffSet.GetInjuredParts().GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                BodyPartRecord rec = enumerator.Current;
                                IEnumerable<Hediff_Injury> arg_BB_0 = targetPawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                                Func<Hediff_Injury, bool> arg_BB_1;
                                arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                                foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                                {
                                    bool flag5 = !current.IsPermanent();
                                    if (flag5)
                                    {
                                        injurySeverity += current.Severity;
                                    }
                                }
                            }
                        }
                        
                        if (injurySeverity != 0)
                        {
                            pawnList.Add(targetPawn as Thing);
                        }
                    }
                    targetPawn = null;                    
                }
            }

            List<Thing> buildingList = TM_Calc.FindNearbyDamagedBuilding(pawn, radius);
            if (buildingList != null)
            {
                for (int i = 0; i < buildingList.Count; i++)
                {
                    pawnList.Add(buildingList[i]);
                }
            }

            if (pawnList.Count > 0)
            {
                return pawnList.RandomElement();
            }
            else
            {
                return null;
            }
        }

        public static List<Pawn> FindAllPawnsAround(Map map, IntVec3 center, float radius, Faction faction = null, bool sameFaction = false)
        {
            List<Pawn> mapPawns = map.mapPawns.AllPawnsSpawned;
            List<Pawn> pawnList = new List<Pawn>();
            Pawn targetPawn = null;
            pawnList.Clear();
            for (int i = 0; i < mapPawns.Count; i++)
            {
                targetPawn = mapPawns[i];
                if (targetPawn != null && !targetPawn.Dead && !targetPawn.Destroyed)
                {                    
                    if (faction != null && !sameFaction)
                    {                        
                        if ((targetPawn.Position - center).LengthHorizontal <= radius)
                        {
                            if (targetPawn.Faction != null)
                            {
                                if (targetPawn.Faction != faction)
                                {
                                    pawnList.Add(targetPawn);
                                    targetPawn = null;
                                }
                                else
                                {
                                    targetPawn = null;
                                }
                            }
                            else
                            {
                                pawnList.Add(targetPawn);
                                targetPawn = null;
                            }
                        }
                        else
                        {
                            targetPawn = null;
                        }
                    }
                    else if(faction != null && sameFaction)
                    {                        
                        if (targetPawn.Faction != null && targetPawn.Faction == faction && (targetPawn.Position - center).LengthHorizontal <= radius)
                        {
                            if (!targetPawn.IsQuestLodger() && !targetPawn.IsQuestHelper())
                            {
                                pawnList.Add(targetPawn);
                            } 
                            targetPawn = null;
                        }
                        else
                        {
                            targetPawn = null;
                        }
                    }
                    else
                    {
                        if((targetPawn.Position - center).LengthHorizontal <= radius)
                        {
                            pawnList.Add(targetPawn);
                            targetPawn = null;
                        }
                        else
                        {
                            targetPawn = null;
                        }
                    }
                }
            }
            if (pawnList.Count > 0)
            {
                return pawnList;
            }
            else
            {
                return null;
            }
        }

        public static Pawn FindNearestEnemy(Map map, IntVec3 position, Faction faction, bool dead = false, bool downed = false)
        {
            Pawn closest = null;
            float closestDistance = 1000f;
            List<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
            foreach(Pawn p in allPawns)
            {
                if(!p.DestroyedOrNull())
                {
                    if(p.Dead && !dead)
                    {
                        continue;
                    }
                    if(p.Downed && !downed)
                    {
                        continue;
                    }
                    if(!p.Faction.HostileTo(faction))
                    {
                        continue;
                    }
                    if((p.Position - position).LengthHorizontal > closestDistance)
                    {
                        continue;
                    }
                    closest = p;
                    closestDistance = (p.Position - position).LengthHorizontal;
                }
            }
            return closest;
        }

        public static List<Pawn> FindAllHostilePawnsAround(Map map, IntVec3 center, float radius, Faction faction)
        {
            List<Pawn> tmpList = FindAllPawnsAround(map, center, radius);
            if(tmpList != null && tmpList.Count > 0)
            {
                List<Pawn> enemyList = new List<Pawn>();
                enemyList.Clear();
                foreach(Pawn p in tmpList)
                {
                    if(p.HostileTo(faction))
                    {
                        enemyList.Add(p);
                    }
                }
                return enemyList;
            }
            return tmpList;
        }

        public static Building FindNearestWall(Map map, IntVec3 center, Faction faction = null)
        {
            List<Thing> mapBuildings = map.listerThings.AllThings.Where((Thing x) => x is Building && (x.Position - center).LengthHorizontal <= 1.4f).ToList();
            if(mapBuildings != null && mapBuildings.Count > 0 )
            {
                foreach(Thing t in mapBuildings)
                {
                    Building b = t as Building;
                    if(TM_Calc.IsWall(t))
                    {
                        if(faction != null)
                        {
                            if (faction == b.Faction)
                            {
                                return b;
                            }
                        }
                        else
                        {
                            return b;
                        }
                    }
                }
            }
            return null;
        }

        public static Thing GetTransmutableThingFromCell(IntVec3 cell, Pawn enchanter, out bool flagRawResource, out bool flagStuffItem, out bool flagNoStuffItem, out bool flagNutrition, out bool flagCorpse, bool manualCast = false)
        {
            CompAbilityUserMagic comp = enchanter.GetCompAbilityUserMagic();
            int pwrVal = enchanter.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Transmutate.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Transmutate_pwr").level;
            int verVal = enchanter.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Transmutate.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Transmutate_ver").level;

            List<Thing> thingList = cell.GetThingList(enchanter.Map);
            Thing transmutateThing = null;

            flagRawResource = false;
            flagStuffItem = false;
            flagNoStuffItem = false;
            flagNutrition = false;
            flagCorpse = false;

            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i] != null && !(thingList[i] is Pawn) && !(thingList[i] is Building) && (manualCast || !thingList[i].IsForbidden(enchanter)))
                {
                    //if (thingList[i].def.thingCategories != null && thingList[i].def.thingCategories.Count > 0 && (thingList[i].def.thingCategories.Contains(ThingCategoryDefOf.ResourcesRaw) || thingList[i].def.thingCategories.Contains(ThingCategoryDefOf.StoneBlocks) || thingList[i].def.defName == "RawMagicyte"))                    
                    if (thingList[i].def.MadeFromStuff && verVal >= 3)
                    {
                        //Log.Message("stuff item");
                        flagStuffItem = true;
                        transmutateThing = thingList[i];
                        break;
                    }
                    if (!thingList[i].def.MadeFromStuff && thingList[i].TryGetComp<CompQuality>() != null && verVal >= 3)
                    {
                        //Log.Message("non stuff item");
                        flagNoStuffItem = true;
                        transmutateThing = thingList[i];
                        break;
                    }
                    if ((thingList[i].def.statBases != null && thingList[i].GetStatValue(StatDefOf.Nutrition) > 0) && !(thingList[i] is Corpse) && verVal >= 1)
                    {
                        //Log.Message("food item");
                        if (thingList[i].def.IsIngestible && thingList[i].def.ingestible.preferability > FoodPreferability.MealAwful)
                        {
                            flagNutrition = false;
                        }
                        else
                        {
                            flagNutrition = true;
                            transmutateThing = thingList[i];
                        }                        
                        break;
                    }
                    if (thingList[i] is Corpse && verVal >= 2)
                    {
                        //Log.Message("corpse");
                        flagCorpse = true;
                        transmutateThing = thingList[i];
                        break;
                    }
                    if (thingList[i].def != null && !thingList[i].def.IsIngestible && ((thingList[i].def.stuffProps != null && thingList[i].def.stuffProps.categories != null && thingList[i].def.stuffProps.categories.Count > 0) || thingList[i].def.defName == "RawMagicyte" || thingList[i].def.IsWithinCategory(ThingCategoryDefOf.ResourcesRaw) || thingList[i].def.IsWithinCategory(ThingCategoryDefOf.Leathers)))
                    {
                        //Log.Message("resource");
                        flagRawResource = true;
                        transmutateThing = thingList[i];
                        break;
                    }
                }
            }
            return transmutateThing;
        }

        public static IntVec3 TryFindSafeCell(Pawn pawn, IntVec3 currentPos, int radius, int maxThreats, int attempts = 1)
        {
            //Log.Message("attempting to find safe cell");
            for (int i = 0; i < attempts; i++)
            {
                IntVec3 tmp = currentPos;
                tmp.x += (Rand.Range(-radius, radius));
                tmp.z += Rand.Range(-radius, radius);
                if (tmp.InBoundsWithNullCheck(pawn.Map) && tmp.IsValid && tmp.Walkable(pawn.Map) && tmp.DistanceToEdge(pawn.Map) > 8)
                {
                    List<Pawn> threatCount = TM_Calc.FindPawnsNearTarget(pawn, 4, tmp, true);
                    if (threatCount != null)
                    {
                        if (threatCount.Count <= maxThreats)
                        {
                            //Log.Message("returning safe cell  (pawns)" + tmp);
                            return tmp;
                        }
                    }
                    else
                    {
                        //Log.Message("returning safe cell  " + tmp);
                        return tmp;
                    }
                }
            }
            return default(IntVec3);
        }


        public static IntVec3 GetEmptyCellForNewBuilding(IntVec3 pos, Map map, float radius, bool useCenter, float exludeInnerRadius = 0, bool allowRoofed = false)
        {
            List<IntVec3> outerCells = GenRadial.RadialCellsAround(pos, radius, useCenter).ToList();
            if (exludeInnerRadius != 0)
            {
                List<IntVec3> innerCells = GenRadial.RadialCellsAround(pos, exludeInnerRadius, useCenter).ToList();
                outerCells = outerCells.Except(innerCells).ToList();
            }

            for (int k = 0; k < outerCells.Count; k++)
            {
                IntVec3 wall = outerCells[k];
                if (wall.IsValid && wall.InBoundsWithNullCheck(map) && !wall.Fogged(map) && wall.Standable(map) && (!wall.Roofed(map) || allowRoofed))
                {
                    List<Thing> cellList = new List<Thing>();
                    try
                    {
                        cellList = wall.GetThingList(map);
                        if (cellList != null && cellList.Count > 0)
                        {
                            bool hasThing = false;
                            for (int i = 0; i < cellList.Count(); i++)
                            {
                                if (cellList[i].def.designationCategory != null && cellList[i].def.designationCategory == DesignationCategoryDefOf.Structure)
                                {
                                    hasThing = true;
                                    break;
                                }
                                if (cellList[i].def.altitudeLayer == AltitudeLayer.Building || cellList[i].def.altitudeLayer == AltitudeLayer.Item || cellList[i].def.altitudeLayer == AltitudeLayer.ItemImportant)
                                {
                                    hasThing = true;
                                    break;
                                }
                                if (cellList[i].def.EverHaulable)
                                {
                                    hasThing = true;
                                    break;
                                }
                            }
                            if (!hasThing)
                            {
                                return wall;
                            }
                        }
                        else
                        {
                            return wall;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return default(IntVec3);
        }

        public static float GetArcaneResistance(Pawn pawn, bool includePsychicSensitivity)
        {
            float resistance = 0;
            CompAbilityUserMagic compMagic = pawn.GetCompAbilityUserMagic();
            if(compMagic != null)
            {
                resistance += (compMagic.arcaneRes - 1);
            }

            CompAbilityUserMight compMight = pawn.GetCompAbilityUserMight();
            if(compMight != null && resistance == 0)
            {
                resistance += (compMight.arcaneRes - 1);
            }

            if (includePsychicSensitivity && resistance == 0f)
            {
                resistance += (1 - pawn.GetStatValue(StatDefOf.PsychicSensitivity, false))/2;
            }

            if (pawn.health != null && pawn.health.capacities != null)
            {
                resistance += (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) - 1);
            }

            return resistance;
        }

        public static float GetSpellPenetration(Pawn pawn)
        {
            float penetration = 0;
            CompAbilityUserMagic compMagic = pawn.GetCompAbilityUserMagic();
            if (compMagic != null)
            {
                penetration += (compMagic.arcaneDmg - 1);
                if(compMagic.MagicData != null && compMagic.MagicData.GetSkill_Versatility(TorannMagicDefOf.TM_Empathy) != null)
                {
                    penetration += compMagic.MagicData.GetSkill_Versatility(TorannMagicDefOf.TM_Empathy).level * .1f;
                }
            }

            CompAbilityUserMight compMight = pawn.GetCompAbilityUserMight();
            if (compMight != null && penetration == 0)
            {
                penetration += (compMight.mightPwr - 1);
            }

            if (pawn.health != null && pawn.health.capacities != null)
            {
                penetration += (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) - 1);
            }

            return penetration;
        }

        public static float GetSpellSuccessChance(Pawn caster, Pawn victim, bool usePsychicSensitivity = true)
        {
            float successChance;
            float penetration = TM_Calc.GetSpellPenetration(caster);
            float resistance = TM_Calc.GetArcaneResistance(victim, usePsychicSensitivity);
            successChance = 1f + penetration - resistance;
            return successChance;
        }

        public static List<ThingDef> GetAllRaceBloodTypes()
        {
            List<ThingDef> bloodTypes = new List<ThingDef>();
            bloodTypes.Clear();

            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                                  where (def.race != null && def.race.BloodDef != null)
                                                  select def;

            foreach (ThingDef current in enumerable)
            {
                bloodTypes.AddDistinct(current.race.BloodDef);                
            }

            //for(int i =0; i< bloodTypes.Count; i++)
            //{
            //    Log.Message("blood type includes " + bloodTypes[i].defName);
            //}
            return bloodTypes;
        }

        public static HediffDef GetBloodLossTypeDef(List<Hediff> hediffList)
        {
            List<TM_CustomDef> bltd = DefDatabase<TM_CustomDef>.AllDefsListForReading;
            List<string> bltdHediffs = TM_CustomDef.Named("TM_CustomDef").BloodLossHediffs;
            for (int i = 0; i < bltdHediffs.Count; i++)
            {
                for (int j = 0; j < hediffList.Count; j++)
                {
                    if (bltdHediffs[i].ToString() == hediffList[j].def.defName.ToString())
                    {
                        return hediffList[j].def;
                    }
                }                
            }
            return null;
        }

        public static T Clone<T>(T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            // Don't serialize a null object, simply return the default for that object
            if (System.Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }

        //Rand.Chance(((settingsRef.baseFighterChance * 6) + (settingsRef.baseMageChance * 6) + (8 * settingsRef.advFighterChance) + (16 * settingsRef.advMageChance)) / (allTraits.Count))
        public static float GetRWoMTraitChance()
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            List<TraitDef> allTraits = DefDatabase<TraitDef>.AllDefsListForReading;
            float chance = ((settingsRef.baseFighterChance * 6) + (settingsRef.baseMageChance * 6) + (9 * settingsRef.advFighterChance) + (18 * settingsRef.advMageChance)) / (allTraits.Count);
            return Mathf.Clamp01(chance);
        }
        
        public static float GetMagePrecurserChance()
        {
            float chance = 0f;
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            chance = (settingsRef.baseMageChance * 6) / ((settingsRef.baseFighterChance * 6) + (settingsRef.baseMageChance * 6) + (9 * settingsRef.advFighterChance) + (18 * settingsRef.advMageChance));
            chance *= GetRWoMTraitChance();
            return chance;
        }

        public static float GetFighterPrecurserChance()
        {
            float chance = 0f;
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            chance = (settingsRef.baseFighterChance * 6) / ((settingsRef.baseFighterChance * 6) + (settingsRef.baseMageChance * 6) + (9 * settingsRef.advFighterChance) + (18 * settingsRef.advMageChance));
            chance *= GetRWoMTraitChance();
            return chance;
        }

        public static float GetMageSpawnChance()
        {
            float chance = 0f;
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            chance = (settingsRef.advMageChance * 16) / ((settingsRef.baseFighterChance * 6) + (settingsRef.baseMageChance * 6) + (9 * settingsRef.advFighterChance) + (18 * settingsRef.advMageChance));
            chance *= GetRWoMTraitChance();
            return chance;
        }

        public static float GetFighterSpawnChance()
        {
            float chance = 0f;
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            chance = (settingsRef.advFighterChance * 8) / ((settingsRef.baseFighterChance * 6) + (settingsRef.baseMageChance * 6) + (9 * settingsRef.advFighterChance) + (18 * settingsRef.advMageChance));
            chance *= GetRWoMTraitChance();
            return chance;
        }

        public static Area GetSpriteArea(Map map = null, bool makeNewArea = true)
        {
            Area spriteArea = null;
            if (map == null)
            {
                map = Find.CurrentMap;
            }
            if (map != null)
            {
                List<Area> allAreas = map.areaManager.AllAreas;
                if (allAreas != null && allAreas.Count > 0)
                {
                    for (int i = 0; i < allAreas.Count; i++)
                    {
                        if (allAreas[i].Label == "earth sprites")
                        {
                            spriteArea = allAreas[i];
                        }
                    }
                }
                if (spriteArea == null && makeNewArea)
                {
                    Area_Allowed newArea = null;
                    if (map.areaManager.TryMakeNewAllowed(out newArea))
                    {
                        newArea.SetLabel("earth sprites");
                    }
                }
            }
            return spriteArea;
        }

        public static Area GetTransmutateArea(Map map = null, bool makeNewArea = true)
        {
            Area transmutateArea = null;
            if(map == null)
            {
                map = Find.CurrentMap;
            }
            if (map != null)
            {
                List<Area> allAreas = map.areaManager.AllAreas;
                if (allAreas != null && allAreas.Count > 0)
                {
                    for (int i = 0; i < allAreas.Count; i++)
                    {
                        if (allAreas[i].Label == "transmutate")
                        {
                            transmutateArea = allAreas[i];
                        }
                    }
                }
                if (transmutateArea == null && makeNewArea)
                {
                    Area_Allowed newArea = null;
                    if (map.areaManager.TryMakeNewAllowed(out newArea))
                    {
                        newArea.SetLabel("transmutate");
                    }
                }
            }
            return transmutateArea;
        }

        public static Area GetSeedOfRegrowthArea(Map map = null, bool makeNewArea = true)
        {
            Area regrowthSeedArea = null;
            if (map == null)
            {
                map = Find.CurrentMap;
            }
            if (map != null)
            {
                List<Area> allAreas = map.areaManager.AllAreas;
                if (allAreas != null && allAreas.Count > 0)
                {
                    for (int i = 0; i < allAreas.Count; i++)
                    {
                        if (allAreas[i].Label == "regrowth seed")
                        {
                            regrowthSeedArea = allAreas[i];
                        }
                    }
                }
                if (regrowthSeedArea == null && makeNewArea)
                {
                    Area_Allowed newArea = null;
                    if (map.areaManager.TryMakeNewAllowed(out newArea))
                    {
                        newArea.SetLabel("regrowth seed");
                    }
                }
            }
            return regrowthSeedArea;
        }

        public static Apparel GetNecroticOrb(Pawn pawn)
        {
            if (pawn.apparel != null && pawn.apparel.WornApparelCount > 0)
            {
                List<Apparel> apparelList = pawn.apparel.WornApparel;
                for (int j = 0; j < apparelList.Count; j++)
                {
                    if (apparelList[j].def == TorannMagicDefOf.TM_Artifact_NecroticOrb)
                    {
                        return apparelList[j];
                    }
                }
            }
            return null;
        }

        public static List<Apparel> GetAllMapNecroticOrbs(Pawn pawn)
        {
            List<Apparel> orbs = new List<Apparel>();
            orbs.Clear();
            if (pawn.Map != null)
            {
                List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < mapPawns.Count; i++)
                {
                    if (mapPawns[i].RaceProps.Humanlike && mapPawns[i].apparel != null && mapPawns[i].Faction == pawn.Faction && mapPawns[i].apparel.WornApparelCount > 0)
                    {
                        List<Apparel> apparelList = mapPawns[i].apparel.WornApparel;
                        for (int j = 0; j < apparelList.Count; j++)
                        {
                            if (apparelList[j].def == TorannMagicDefOf.TM_Artifact_NecroticOrb)
                            {
                                orbs.Add(apparelList[j]);
                            }
                        }
                    }
                }
            }
            else if (pawn.ParentHolder.ToString().Contains("Caravan"))
            {
                foreach (Thing currentThing in pawn.holdingOwner)
                {
                    if (currentThing is Pawn current)
                    {
                        if (current.RaceProps.Humanlike && current.Faction == pawn.Faction && current.apparel != null && current.apparel.WornApparelCount > 0)
                        {
                            List<Apparel> apparelList = current.apparel.WornApparel;
                            for (int j = 0; j < apparelList.Count; j++)
                            {
                                if (apparelList[j].def == TorannMagicDefOf.TM_Artifact_NecroticOrb)
                                {
                                    orbs.Add(apparelList[j]);
                                }
                            }
                        }
                    }
                }
            }
            return orbs;
        }

        public static float GetRelationsFactor(Pawn p1, Pawn p2)
        {
            float factor = 0f;
            if(p1.relations != null && p2.relations != null)
            {               
                float p1R = p1.relations.OpinionOf(p2);
                float p2R = p2.relations.OpinionOf(p1);
                factor = (p1R + p2R) / 100; // -2 to 2
            }
            return factor;
        }

        public static TMAbilityDef GetCopiedMightAbility(Pawn targetPawn, Pawn caster)
        {
            CompAbilityUserMight mightPawn = targetPawn.GetCompAbilityUserMight();
            CompAbilityUserMight casterComp = caster.GetCompAbilityUserMight();
            TMAbilityDef tempAbility = null;
            if (mightPawn.customClass != null && mightPawn.customClass.isFighter)
            {
                List<TMAbilityDef> copyAbilities = new List<TMAbilityDef>();
                copyAbilities.Clear();
                for (int i = 0; i < mightPawn.customClass.classFighterAbilities.Count; i++)
                {
                    if (mightPawn.customClass.classFighterAbilities[i].canCopy)
                    {
                        if (!casterComp.MightData.ReturnMatchingMightPower(mightPawn.customClass.classFighterAbilities[i]).learned)
                        {
                            copyAbilities.AddDistinct(mightPawn.customClass.classFighterAbilities[i]);
                        }
                    }
                }
                if (copyAbilities.Count > 0)
                {
                    tempAbility = copyAbilities.RandomElement();
                }
            }
            else
            {
                if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator) && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                {                    
                    int rnd = Rand.RangeInclusive(0, 1);
                    if (rnd == 0)
                    {
                        int level = mightPawn.MightData.MightPowersG[2].level;
                        caster.GetCompAbilityUserMight().MightData.MightPowersG[2].level = level;
                        switch (level)
                        {
                            case 0:
                                tempAbility = TorannMagicDefOf.TM_Grapple;
                                break;
                            case 1:
                                tempAbility = TorannMagicDefOf.TM_Grapple_I;
                                break;
                            case 2:
                                tempAbility = TorannMagicDefOf.TM_Grapple_II;
                                break;
                            case 3:
                                tempAbility = TorannMagicDefOf.TM_Grapple_III;
                                break;
                        }
                    }
                    else
                    {
                        tempAbility = TorannMagicDefOf.TM_Whirlwind;
                    }
                }
                else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper) && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                {
                    int rnd = Rand.RangeInclusive(0, 2);
                    if (rnd == 0)
                    {
                        tempAbility = TorannMagicDefOf.TM_AntiArmor;
                    }
                    else if (rnd == 1)
                    {
                        int level = mightPawn.MightData.MightPowersS[2].level;
                        caster.GetCompAbilityUserMight().MightData.MightPowersS[2].level = level;
                        switch (level)
                        {
                            case 0:
                                tempAbility = TorannMagicDefOf.TM_DisablingShot;
                                break;
                            case 1:
                                tempAbility = TorannMagicDefOf.TM_DisablingShot_I;
                                break;
                            case 2:
                                tempAbility = TorannMagicDefOf.TM_DisablingShot_II;
                                break;
                            case 3:
                                tempAbility = TorannMagicDefOf.TM_DisablingShot_III;
                                break;
                        }
                    }
                    else
                    {
                        tempAbility = TorannMagicDefOf.TM_Headshot;
                    }

                }
                else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer) && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                {
                    int rnd = Rand.RangeInclusive(0, 2);
                    if (rnd == 0)
                    {
                        tempAbility = TorannMagicDefOf.TM_SeismicSlash;
                    }
                    else if (rnd == 1)
                    {
                        int level = mightPawn.MightData.MightPowersB[4].level;
                        caster.GetCompAbilityUserMight().MightData.MightPowersB[4].level = level;
                        switch (level)
                        {
                            case 0:
                                tempAbility = TorannMagicDefOf.TM_PhaseStrike;
                                break;
                            case 1:
                                tempAbility = TorannMagicDefOf.TM_PhaseStrike_I;
                                break;
                            case 2:
                                tempAbility = TorannMagicDefOf.TM_PhaseStrike_II;
                                break;
                            case 3:
                                tempAbility = TorannMagicDefOf.TM_PhaseStrike_III;
                                break;
                        }
                    }
                    else
                    {
                        tempAbility = TorannMagicDefOf.TM_BladeSpin;
                    }
                }
                else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Ranger) && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                {
                    int rnd = Rand.RangeInclusive(0, 1);
                    if (rnd == 0)
                    {
                        int level = mightPawn.MightData.MightPowersR[4].level;
                        caster.GetCompAbilityUserMight().MightData.MightPowersR[4].level = level;
                        switch (level)
                        {
                            case 0:
                                tempAbility = TorannMagicDefOf.TM_ArrowStorm;
                                break;
                            case 1:
                                tempAbility = TorannMagicDefOf.TM_ArrowStorm_I;
                                break;
                            case 2:
                                tempAbility = TorannMagicDefOf.TM_ArrowStorm_II;
                                break;
                            case 3:
                                tempAbility = TorannMagicDefOf.TM_ArrowStorm_III;
                                break;
                        }
                    }
                    else
                    {
                        tempAbility = TorannMagicDefOf.TM_PoisonTrap;
                    }
                }
                else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic))
                {
                    int rnd = Rand.RangeInclusive(0, 3);
                    if ((rnd == 0 || rnd == 3) && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        int level = mightPawn.MightData.MightPowersP[1].level;
                        caster.GetCompAbilityUserMight().MightData.MightPowersP[1].level = level;
                        switch (level)
                        {
                            case 0:
                                tempAbility = TorannMagicDefOf.TM_PsionicBlast;
                                break;
                            case 1:
                                tempAbility = TorannMagicDefOf.TM_PsionicBlast_I;
                                break;
                            case 2:
                                tempAbility = TorannMagicDefOf.TM_PsionicBlast_II;
                                break;
                            case 3:
                                tempAbility = TorannMagicDefOf.TM_PsionicBlast_III;
                                break;
                        }
                    }
                    else if (rnd == 1 && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        tempAbility = TorannMagicDefOf.TM_PsionicDash;
                    }
                    else
                    {
                        int level = mightPawn.MightData.MightPowersP[3].level;
                        caster.GetCompAbilityUserMight().MightData.MightPowersP[3].level = level;
                        switch (level)
                        {
                            case 0:
                                tempAbility = TorannMagicDefOf.TM_PsionicBarrier;
                                break;
                            case 1:
                                tempAbility = TorannMagicDefOf.TM_PsionicBarrier_Projected;
                                break;
                        }
                    }
                }
                else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight))
                {
                    int rnd = Rand.RangeInclusive(1, 2);
                    if (rnd == 1 || caster.story.DisabledWorkTagsBackstoryAndTraits == WorkTags.Violent)
                    {
                        tempAbility = TorannMagicDefOf.TM_WaveOfFear;
                    }
                    else
                    {
                        int level = mightPawn.MightData.MightPowersDK[4].level;
                        caster.GetCompAbilityUserMight().MightData.MightPowersDK[4].level = level;
                        switch (level)
                        {
                            case 0:
                                tempAbility = TorannMagicDefOf.TM_GraveBlade;
                                break;
                            case 1:
                                tempAbility = TorannMagicDefOf.TM_GraveBlade_I;
                                break;
                            case 2:
                                tempAbility = TorannMagicDefOf.TM_GraveBlade_II;
                                break;
                            case 3:
                                tempAbility = TorannMagicDefOf.TM_GraveBlade_III;
                                break;
                        }
                    }
                }
                else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk))
                {
                    int rnd = Rand.RangeInclusive(3, 5);
                    if (rnd == 3)
                    {
                        tempAbility = TorannMagicDefOf.TM_TigerStrike;
                    }
                    else if (rnd == 4)
                    {
                        tempAbility = TorannMagicDefOf.TM_DragonStrike;
                    }
                    else
                    {
                        tempAbility = TorannMagicDefOf.TM_ThunderStrike;
                    }
                }
                else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.TM_Commander))
                {
                    int rnd = Rand.RangeInclusive(3, 5);
                    if (rnd == 3)
                    {
                        int level = mightPawn.MightData.MightPowersC[3].level;
                        caster.GetCompAbilityUserMight().MightData.MightPowersC[3].level = level;
                        switch (level)
                        {
                            case 0:
                                tempAbility = TorannMagicDefOf.TM_StayAlert;
                                break;
                            case 1:
                                tempAbility = TorannMagicDefOf.TM_StayAlert_I;
                                break;
                            case 2:
                                tempAbility = TorannMagicDefOf.TM_StayAlert_II;
                                break;
                            case 3:
                                tempAbility = TorannMagicDefOf.TM_StayAlert_III;
                                break;
                        }
                    }
                    else if (rnd == 4)
                    {
                        int level = mightPawn.MightData.MightPowersC[4].level;
                        caster.GetCompAbilityUserMight().MightData.MightPowersC[4].level = level;
                        switch (level)
                        {
                            case 0:
                                tempAbility = TorannMagicDefOf.TM_MoveOut;
                                break;
                            case 1:
                                tempAbility = TorannMagicDefOf.TM_MoveOut_I;
                                break;
                            case 2:
                                tempAbility = TorannMagicDefOf.TM_MoveOut_II;
                                break;
                            case 3:
                                tempAbility = TorannMagicDefOf.TM_MoveOut_III;
                                break;
                        }
                    }
                    else
                    {
                        int level = mightPawn.MightData.MightPowersC[5].level;
                        caster.GetCompAbilityUserMight().MightData.MightPowersC[5].level = level;
                        switch (level)
                        {
                            case 0:
                                tempAbility = TorannMagicDefOf.TM_HoldTheLine;
                                break;
                            case 1:
                                tempAbility = TorannMagicDefOf.TM_HoldTheLine_I;
                                break;
                            case 2:
                                tempAbility = TorannMagicDefOf.TM_HoldTheLine_II;
                                break;
                            case 3:
                                tempAbility = TorannMagicDefOf.TM_HoldTheLine_III;
                                break;
                        }
                    }
                }
                else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier))
                {
                    int rnd = Rand.RangeInclusive(0, 1);
                    if (rnd == 0 && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        tempAbility = TorannMagicDefOf.TM_60mmMortar;
                    }
                    else
                    {
                        tempAbility = TorannMagicDefOf.TM_FirstAid;
                    }
                }
                else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {

                }
            }
            return tempAbility;
        }

        public static TMAbilityDef GetCopiedMagicAbility(Pawn targetPawn, Pawn caster)
        {
            CompAbilityUserMagic magicPawn = targetPawn.GetCompAbilityUserMagic();
            CompAbilityUserMagic casterComp = caster.GetCompAbilityUserMagic();
            TMAbilityDef tempAbility = null;
            if (magicPawn.customClass != null && magicPawn.customClass.isMage)
            {
                List<TMAbilityDef> copyAbilities = new List<TMAbilityDef>();
                copyAbilities.Clear();
                for (int i = 0; i < magicPawn.customClass.classMageAbilities.Count; i++)
                {
                    if (magicPawn.customClass.classMageAbilities[i].canCopy)
                    {
                        if (!casterComp.MagicData.ReturnMatchingMagicPower(magicPawn.customClass.classMageAbilities[i]).learned)
                        {
                            copyAbilities.AddDistinct(magicPawn.customClass.classMageAbilities[i]);
                        }                        
                    }
                }
                if (copyAbilities.Count > 0)
                {
                    tempAbility = copyAbilities.RandomElement();
                }
            }
            else
            {
                if (magicPawn != null)
                {
                    if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int rnd = Rand.RangeInclusive(0, 3);
                            if (rnd == 0 && magicPawn.MagicData.MagicPowersA[rnd].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersA[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersA[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_Shadow;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_Shadow_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_Shadow_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_Shadow_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 1 && magicPawn.MagicData.MagicPowersA[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                int level = magicPawn.MagicData.MagicPowersA[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersA[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_MagicMissile;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_MagicMissile_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_MagicMissile_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_MagicMissile_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 2 && magicPawn.MagicData.MagicPowersA[rnd].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersA[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersA[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_Blink;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_Blink_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_Blink_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_Blink_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 3 && magicPawn.MagicData.MagicPowersA[rnd].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersA[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersA[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_Summon;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_Summon_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_Summon_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_Summon_III;
                                        break;
                                }
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.StormBorn))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int rnd = Rand.RangeInclusive(0, 3);
                            if (rnd == 0 && magicPawn.MagicData.MagicPowersSB[rnd].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersSB[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersSB[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_AMP;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_AMP_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_AMP_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_AMP_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 1 && magicPawn.MagicData.MagicPowersSB[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_LightningBolt;
                                i = 5;
                            }
                            else if (rnd == 2 && magicPawn.MagicData.MagicPowersSB[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_LightningCloud;
                                i = 5;
                            }
                            else if (rnd == 3 && magicPawn.MagicData.MagicPowersSB[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_LightningStorm;
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.InnerFire))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int rnd = Rand.RangeInclusive(0, 3);
                            if (rnd == 0 && magicPawn.MagicData.MagicPowersIF[rnd].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersIF[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersIF[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_RayofHope;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_RayofHope_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_RayofHope_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_RayofHope_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 1 && magicPawn.MagicData.MagicPowersIF[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_Firebolt;
                                i = 5;
                            }
                            else if (rnd == 2 && magicPawn.MagicData.MagicPowersIF[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_Fireclaw;
                                i = 5;
                            }
                            else if (rnd == 3 && magicPawn.MagicData.MagicPowersIF[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_Fireball;
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int rnd = Rand.RangeInclusive(0, 4);
                            if (rnd == 0 && magicPawn.MagicData.MagicPowersHoF[rnd].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersHoF[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersHoF[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_Soothe;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_Soothe_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_Soothe_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_Soothe_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 1 && magicPawn.MagicData.MagicPowersHoF[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_Rainmaker;
                                i = 5;
                            }
                            else if (rnd == 2 && magicPawn.MagicData.MagicPowersHoF[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_Icebolt;
                                i = 5;
                            }
                            else if (rnd == 3 && magicPawn.MagicData.MagicPowersHoF[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                int level = magicPawn.MagicData.MagicPowersHoF[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersHoF[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_FrostRay;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_FrostRay_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_FrostRay_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_FrostRay_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 4 && magicPawn.MagicData.MagicPowersHoF[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_Snowball;
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Druid))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int rnd = Rand.RangeInclusive(0, 3);
                            if (rnd == 0 && magicPawn.MagicData.MagicPowersD[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_Poison;
                                i = 5;
                            }
                            else if (rnd == 1 && magicPawn.MagicData.MagicPowersD[rnd].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersD[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersD[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_SootheAnimal;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_SootheAnimal_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_SootheAnimal_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_SootheAnimal_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 2 && magicPawn.MagicData.MagicPowersD[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_Regenerate;
                                i = 5;
                            }
                            else if (rnd == 3 && magicPawn.MagicData.MagicPowersD[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_CureDisease;
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Necromancer))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int rnd = Rand.RangeInclusive(1, 3);
                            if (rnd == 1 && magicPawn.MagicData.MagicPowersN[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                int level = magicPawn.MagicData.MagicPowersN[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersN[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_DeathMark;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_DeathMark_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_DeathMark_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_DeathMark_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 2 && magicPawn.MagicData.MagicPowersN[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_FogOfTorment;
                                i = 5;
                            }
                            else if (rnd == 3 && magicPawn.MagicData.MagicPowersN[rnd + 1].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                int level = magicPawn.MagicData.MagicPowersN[rnd + 1].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersN[rnd + 1].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_CorpseExplosion;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_CorpseExplosion_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_CorpseExplosion_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_CorpseExplosion_III;
                                        break;
                                }
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Paladin))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int rnd = Rand.RangeInclusive(0, 3);
                            if (rnd == 1 && magicPawn.MagicData.MagicPowersP[rnd].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersP[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersP[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_Shield;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_Shield_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_Shield_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_Shield_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 0 && magicPawn.MagicData.MagicPowersP[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_Heal;
                                i = 5;
                            }
                            else if (rnd == 2 && magicPawn.MagicData.MagicPowersP[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_ValiantCharge;
                                i = 5;
                            }
                            else if (rnd == 3 && magicPawn.MagicData.MagicPowersP[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_Overwhelm;
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Priest))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int rnd = Rand.RangeInclusive(0, 3);
                            if (rnd == 3 && magicPawn.MagicData.MagicPowersPR[rnd].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersPR[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersPR[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_BestowMight;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_BestowMight_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_BestowMight_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_BestowMight_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 2 && magicPawn.MagicData.MagicPowersPR[rnd].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersPR[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersPR[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_HealingCircle;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_HealingCircle_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_HealingCircle_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_HealingCircle_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 1 && magicPawn.MagicData.MagicPowersPR[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_Purify;
                                i = 5;
                            }
                            else if (rnd == 0 && magicPawn.MagicData.MagicPowersPR[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_AdvancedHeal;
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Summoner))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int rnd = Rand.RangeInclusive(1, 3);
                            if (rnd == 1 && magicPawn.MagicData.MagicPowersS[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_SummonPylon;
                                i = 5;
                            }
                            else if (rnd == 2 && magicPawn.MagicData.MagicPowersS[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_SummonExplosive;
                                i = 5;
                            }
                            else if (rnd == 3 && magicPawn.MagicData.MagicPowersS[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_SummonElemental;
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard))
                    {
                        int level = magicPawn.MagicData.MagicPowersB[3].level;
                        caster.GetCompAbilityUserMagic().MagicData.MagicPowersB[3].level = level;
                        switch (level)
                        {
                            case 0:
                                tempAbility = TorannMagicDefOf.TM_Lullaby;
                                break;
                            case 1:
                                tempAbility = TorannMagicDefOf.TM_Lullaby_I;
                                break;
                            case 2:
                                tempAbility = TorannMagicDefOf.TM_Lullaby_II;
                                break;
                            case 3:
                                tempAbility = TorannMagicDefOf.TM_Lullaby_III;
                                break;
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Warlock))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int rnd = Rand.RangeInclusive(1, 3);
                            if (rnd == 1 && magicPawn.MagicData.MagicPowersWD[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                int level = magicPawn.MagicData.MagicPowersWD[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersWD[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_ShadowBolt;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_ShadowBolt_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_ShadowBolt_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_ShadowBolt_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 2 && magicPawn.MagicData.MagicPowersWD[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_Dominate;
                                i = 5;
                            }
                            else if (rnd == 3 && magicPawn.MagicData.MagicPowersWD[rnd].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersWD[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersWD[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_Repulsion;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_Repulsion_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_Repulsion_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_Repulsion_III;
                                        break;
                                }
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Succubus))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int rnd = Rand.RangeInclusive(1, 3);
                            if (rnd == 1 && magicPawn.MagicData.MagicPowersSD[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                int level = magicPawn.MagicData.MagicPowersSD[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersSD[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_ShadowBolt;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_ShadowBolt_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_ShadowBolt_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_ShadowBolt_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 2 && magicPawn.MagicData.MagicPowersSD[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_Dominate;
                                i = 5;
                            }
                            else if (rnd == 3 && magicPawn.MagicData.MagicPowersSD[rnd].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersSD[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersSD[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_Attraction;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_Attraction_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_Attraction_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_Attraction_III;
                                        break;
                                }
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Geomancer))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int rnd = Rand.RangeInclusive(0, 2);
                            if (rnd == 2)
                            {
                                rnd = 3;
                            }
                            if (rnd == 0 && magicPawn.MagicData.MagicPowersG[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_Stoneskin;
                                i = 5;
                            }
                            else if (rnd == 1 && magicPawn.MagicData.MagicPowersG[rnd].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersG[rnd].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersG[rnd].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_Encase;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_Encase_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_Encase_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_Encase_III;
                                        break;
                                }
                                i = 5;
                            }
                            else if (rnd == 3 && magicPawn.MagicData.MagicPowersG[rnd].learned && caster.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                tempAbility = TorannMagicDefOf.TM_EarthernHammer;
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int rnd = Rand.RangeInclusive(3, 5);
                            if (rnd == 3 && magicPawn.MagicData.MagicPowersT[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_TechnoShield;
                                i = 5;
                            }
                            else if (rnd == 4 && magicPawn.MagicData.MagicPowersT[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_Sabotage;
                                i = 5;
                            }
                            else if (rnd == 5 && magicPawn.MagicData.MagicPowersT[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_Overdrive;
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            if (magicPawn.MagicData.MagicPowersE[4].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersE[4].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersE[4].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_Polymorph;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_Polymorph_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_Polymorph_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_Polymorph_III;
                                        break;
                                }
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int rnd = Rand.RangeInclusive(2, 4);
                            if (rnd == 2 && magicPawn.MagicData.MagicPowersC[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_AccelerateTime;
                                i = 5;
                            }
                            else if (rnd == 3 && magicPawn.MagicData.MagicPowersC[rnd].learned)
                            {
                                tempAbility = TorannMagicDefOf.TM_ReverseTime;
                                i = 5;
                            }
                            else if (magicPawn.MagicData.MagicPowersC[4].learned)
                            {
                                int level = magicPawn.MagicData.MagicPowersC[4].level;
                                caster.GetCompAbilityUserMagic().MagicData.MagicPowersC[4].level = level;
                                switch (level)
                                {
                                    case 0:
                                        tempAbility = TorannMagicDefOf.TM_ChronostaticField;
                                        break;
                                    case 1:
                                        tempAbility = TorannMagicDefOf.TM_ChronostaticField_I;
                                        break;
                                    case 2:
                                        tempAbility = TorannMagicDefOf.TM_ChronostaticField_II;
                                        break;
                                    case 3:
                                        tempAbility = TorannMagicDefOf.TM_ChronostaticField_III;
                                        break;
                                }
                                i = 5;
                            }
                        }
                    }
                    else if (targetPawn.story.traits.HasTrait(TorannMagicDefOf.BloodMage))
                    {
                        Messages.Message("TM_CannotMimicBloodMage".Translate(
                            ), MessageTypeDefOf.RejectInput);
                    }
                }
            }
            return tempAbility;
        }

        public static int GetSkillPowerLevel(Pawn caster, TMAbilityDef ability, bool canCopy = true)
        {
            return GetSkillLevel(caster, ability, "_pwr", canCopy);
        }

        public static int GetSkillVersatilityLevel(Pawn caster, TMAbilityDef ability, bool canCopy = true)
        {
            return GetSkillLevel(caster, ability, "_ver", canCopy);
        }

        public static int GetSkillEfficiencyLevel(Pawn caster, TMAbilityDef ability, bool canCopy = true)
        {
            return GetSkillLevel(caster, ability, "_eff", canCopy);
        }

        public static int GetSkillLevel(Pawn caster, TMAbilityDef ability, string suffix, bool canCopy = true)
        {
            int level = 0;
            bool flagMagic = TM_Calc.IsMagicUser(caster);
            bool flagMight = TM_Calc.IsMightUser(caster);            
            
            if(flagMagic && flagMight)
            {
                level = GetMagicSkillLevel(caster, ability, suffix, canCopy);
                int tmpLevel = GetMightSkillLevel(caster, ability, suffix, canCopy);
                level = level >= tmpLevel ? level : tmpLevel;
            }
            else if(flagMagic)
            {
                level = GetMagicSkillLevel(caster, ability, suffix, canCopy);
            }
            else if(flagMight)
            {
                level = GetMightSkillLevel(caster, ability, suffix, canCopy);
            }
            return level;
        }

        public static int GetMightSkillLevel(Pawn caster, TMAbilityDef ability, string suffix, bool canCopy) //, List<MightPowerSkill> power, string skillLabel, string suffix, bool canCopy = true)
        {
            MightPowerSkill mightSkill = null;
            int level = 0;
            CompAbilityUserMight comp = caster.GetCompAbilityUserMight();
            if (comp != null && comp.MightData != null)
            {
                if (suffix == "_pwr")
                {
                    mightSkill = comp.MightData.GetSkill_Power(ability);
                }
                else if (suffix == "_ver")
                {
                    mightSkill = comp.MightData.GetSkill_Versatility(ability);
                }
                else if (suffix == "_eff")
                {
                    mightSkill = comp.MightData.GetSkill_Efficiency(ability);
                }
                else
                {
                    return 0;
                }
                if (mightSkill != null)
                {
                    level = mightSkill.level;
                }
                CompAbilityUserMight mimicComp = caster.GetCompAbilityUserMight();
                if (canCopy && mimicComp != null && mimicComp.IsMightUser && ability == mimicComp.mimicAbility)
                {
                    string mimicLabel = "TM_Mimic" + suffix;
                    level = mimicComp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == mimicLabel).level;
                }
                int ftLevel = 0;
                if (suffix == "_pwr" || suffix == "_ver")
                {
                    string ftLabel = "TM_FieldTraining" + suffix;
                    ftLevel = (int)(comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == ftLabel).level);
                    if (ftLevel >= 13)
                    {
                        level = level >= 2 ? level : 2;
                    }
                    else if (ftLevel >= 9)
                    {
                        level = level >= 1 ? level : 1;
                    }
                }
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (settingsRef.AIHardMode && !caster.IsColonist)
                {
                    level = 3;
                }
            }
            return level;

            //int val = 0;
            //string label = skillLabel + suffix;
            //CompAbilityUserMight comp = caster.GetCompAbilityUserMight();
            //var mps = power.FirstOrDefault((MightPowerSkill x) => x.label == label);
            //if (mps != null)
            //{
            //    val = mps.level;
            //    if (canCopy && val == 0)
            //    {
            //        if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //        {
            //            label = "TM_Mimic" + suffix;
            //            val = comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == label).level;
            //        }
            //        if ((caster.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) || (caster.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) || (comp.customClass != null && comp.customClass.classFighterAbilities.Contains(TorannMagicDefOf.TM_FieldTraining))) && comp.MightData.MightPowersW.FirstOrDefault((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_WayfarerCraft).learned))
            //        {
            //            label = "TM_FieldTraining" + suffix;

            //            if (comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == label).level >= 13)
            //            {
            //                val = 2;
            //            }
            //            else if (comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == label).level >= 9)
            //            {
            //                val = 1;
            //            }
            //        }
            //    }
            //    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            //    if (settingsRef.AIHardMode && !caster.IsColonist)
            //    {
            //        val = 3;
            //    }
            //}
            //return val;
        }

        public static int GetMagicSkillLevel(Pawn caster, TMAbilityDef ability, string suffix, bool canCopy) //, List<MagicPowerSkill> power, string skillLabel, string suffix, bool canCopy = true)
        {
            MagicPowerSkill magicSkill = null;
            int level = 0;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            if (comp != null && comp.MagicData != null)
            {
                if (suffix == "_pwr")
                {
                    magicSkill = comp.MagicData.GetSkill_Power(ability);
                }
                else if (suffix == "_ver")
                {
                    magicSkill = comp.MagicData.GetSkill_Versatility(ability);
                }
                else if (suffix == "_eff")
                {
                    magicSkill = comp.MagicData.GetSkill_Efficiency(ability);
                }
                else
                {
                    return 0;
                }
                if (magicSkill != null)
                {
                    level = magicSkill.level;
                }
                CompAbilityUserMight mimicComp = caster.GetCompAbilityUserMight();
                if (canCopy && mimicComp != null && mimicComp.IsMightUser && ability == mimicComp.mimicAbility)
                {
                    string mimicLabel = "TM_Mimic" + suffix;
                    level = mimicComp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == mimicLabel).level;
                }
                int cantripLevel = 0;
                if (suffix == "_pwr" || suffix == "_ver")
                {
                    string cantripLabel = "TM_Cantrips" + suffix;
                    cantripLevel = (int)((comp.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == cantripLabel).level) / 5);
                }
                level = cantripLevel > level ? cantripLevel : level;
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (settingsRef.AIHardMode && !caster.IsColonist)
                {
                    level = 3;
                }
            }
            return level;

            //int val = 0;
            //string label = skillLabel + suffix;
            //CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            //if (comp != null && comp.IsMagicUser)
            //{
            //    var mps = power.FirstOrDefault((MagicPowerSkill x) => x.label == label);
            //    if (mps != null)
            //    {
            //        val = mps.level;
            //        if (canCopy && val == 0)
            //        {
            //            if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //            {
            //                label = "TM_Mimic" + suffix;
            //                val = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == label).level;
            //            }
            //            if ((caster.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer) || (caster.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) || (comp.customClass != null && comp.customClass.classMageAbilities.Contains(TorannMagicDefOf.TM_Cantrips))) && comp.MagicData.MagicPowersW.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Cantrips).learned))
            //            {
            //                label = "TM_Cantrips" + suffix;
            //                val = (int)((comp.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == label).level) / 5);
            //            }
            //        }
            //        ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            //        if (settingsRef.AIHardMode && !caster.IsColonist)
            //        {
            //            val = 3;
            //        }
            //    }
            //}
            //return val;
        }

        public static bool IsIconAbility_02(AbilityUser.AbilityDef def)
        {
            if((def == TorannMagicDefOf.TM_RayofHope || def == TorannMagicDefOf.TM_RayofHope_I || def == TorannMagicDefOf.TM_RayofHope_II ||
                        def == TorannMagicDefOf.TM_Soothe || def == TorannMagicDefOf.TM_Soothe_I || def == TorannMagicDefOf.TM_Soothe_II ||
                        def == TorannMagicDefOf.TM_Shadow || def == TorannMagicDefOf.TM_Shadow_I || def == TorannMagicDefOf.TM_Shadow_II ||
                        def == TorannMagicDefOf.TM_AMP || def == TorannMagicDefOf.TM_AMP_I || def == TorannMagicDefOf.TM_AMP_II ||
                        def == TorannMagicDefOf.TM_P_RayofHope || def == TorannMagicDefOf.TM_P_RayofHope_I || def == TorannMagicDefOf.TM_P_RayofHope_II ||
                        def == TorannMagicDefOf.TM_Shield || def == TorannMagicDefOf.TM_Shield_I || def == TorannMagicDefOf.TM_Shield_II ||
                        def == TorannMagicDefOf.TM_Blink || def == TorannMagicDefOf.TM_Blink_I || def == TorannMagicDefOf.TM_Blink_II ||
                        def == TorannMagicDefOf.TM_Summon || def == TorannMagicDefOf.TM_Summon_I || def == TorannMagicDefOf.TM_Summon_II ||
                        def == TorannMagicDefOf.TM_MagicMissile || def == TorannMagicDefOf.TM_MagicMissile_I || def == TorannMagicDefOf.TM_MagicMissile_II ||
                        def == TorannMagicDefOf.TM_FrostRay || def == TorannMagicDefOf.TM_FrostRay_I || def == TorannMagicDefOf.TM_FrostRay_II ||
                        def == TorannMagicDefOf.TM_SootheAnimal || def == TorannMagicDefOf.TM_SootheAnimal_I || def == TorannMagicDefOf.TM_SootheAnimal_II ||
                        def == TorannMagicDefOf.TM_DeathMark || def == TorannMagicDefOf.TM_DeathMark_I || def == TorannMagicDefOf.TM_DeathMark_II ||
                        def == TorannMagicDefOf.TM_ConsumeCorpse || def == TorannMagicDefOf.TM_ConsumeCorpse_I || def == TorannMagicDefOf.TM_ConsumeCorpse_II ||
                        def == TorannMagicDefOf.TM_CorpseExplosion || def == TorannMagicDefOf.TM_CorpseExplosion_I || def == TorannMagicDefOf.TM_CorpseExplosion_II ||
                        def == TorannMagicDefOf.TM_DeathBolt || def == TorannMagicDefOf.TM_DeathBolt_I || def == TorannMagicDefOf.TM_DeathBolt_II ||
                        def == TorannMagicDefOf.TM_HealingCircle || def == TorannMagicDefOf.TM_HealingCircle_I || def == TorannMagicDefOf.TM_HealingCircle_II ||
                        def == TorannMagicDefOf.TM_Lullaby || def == TorannMagicDefOf.TM_Lullaby_I || def == TorannMagicDefOf.TM_Lullaby_II ||
                        def == TorannMagicDefOf.TM_ShadowBolt || def == TorannMagicDefOf.TM_ShadowBolt_I || def == TorannMagicDefOf.TM_ShadowBolt_II ||
                        def == TorannMagicDefOf.TM_Attraction || def == TorannMagicDefOf.TM_Attraction_I || def == TorannMagicDefOf.TM_Attraction_II ||
                        def == TorannMagicDefOf.TM_Repulsion || def == TorannMagicDefOf.TM_Repulsion_I || def == TorannMagicDefOf.TM_Repulsion_II ||
                        def == TorannMagicDefOf.TM_Encase || def == TorannMagicDefOf.TM_Encase_I || def == TorannMagicDefOf.TM_Encase_II ||
                        def == TorannMagicDefOf.TM_Meteor || def == TorannMagicDefOf.TM_Meteor_I || def == TorannMagicDefOf.TM_Meteor_II ||
                        def == TorannMagicDefOf.TM_OrbitalStrike || def == TorannMagicDefOf.TM_OrbitalStrike_I || def == TorannMagicDefOf.TM_OrbitalStrike_II ||
                        def == TorannMagicDefOf.TM_Rend || def == TorannMagicDefOf.TM_Rend_I || def == TorannMagicDefOf.TM_Rend_II ||
                        def == TorannMagicDefOf.TM_BloodMoon || def == TorannMagicDefOf.TM_BloodMoon_I || def == TorannMagicDefOf.TM_BloodMoon_II ||
                        def == TorannMagicDefOf.TM_Polymorph || def == TorannMagicDefOf.TM_Polymorph_I || def == TorannMagicDefOf.TM_Polymorph_II ||
                        def == TorannMagicDefOf.TM_BestowMight || def == TorannMagicDefOf.TM_BestowMight_I || def == TorannMagicDefOf.TM_BestowMight_II ||
                        def == TorannMagicDefOf.TM_ChronostaticField || def == TorannMagicDefOf.TM_ChronostaticField_I || def == TorannMagicDefOf.TM_ChronostaticField_II) ||
                        def == TorannMagicDefOf.TM_Sunfire || def == TorannMagicDefOf.TM_Sunfire_I || def == TorannMagicDefOf.TM_Sunfire_II)
            {
                return true;
            }
            return false;
        }

        public static bool IsIconAbility_03(AbilityUser.AbilityDef def)
        {
            if(TM_Calc.IsIconAbility_02(def))
            {
                return true;
            }
            else if ((def == TorannMagicDefOf.TM_RayofHope_III || def == TorannMagicDefOf.TM_Soothe_III || def == TorannMagicDefOf.TM_Shadow_III ||
                        def == TorannMagicDefOf.TM_AMP_III || def == TorannMagicDefOf.TM_Shield_III || def == TorannMagicDefOf.TM_Blink_III ||
                        def == TorannMagicDefOf.TM_Summon_III || def == TorannMagicDefOf.TM_MagicMissile_III || def == TorannMagicDefOf.TM_FrostRay_III ||
                        def == TorannMagicDefOf.TM_SootheAnimal_III || def == TorannMagicDefOf.TM_DeathMark_III || def == TorannMagicDefOf.TM_ConsumeCorpse_III ||
                        def == TorannMagicDefOf.TM_CorpseExplosion_III || def == TorannMagicDefOf.TM_DeathBolt_III || def == TorannMagicDefOf.TM_HealingCircle_III ||
                        def == TorannMagicDefOf.TM_Lullaby_III || def == TorannMagicDefOf.TM_ShadowBolt_III || def == TorannMagicDefOf.TM_Attraction_III ||
                        def == TorannMagicDefOf.TM_Repulsion_III || def == TorannMagicDefOf.TM_Encase_III || def == TorannMagicDefOf.TM_Meteor_III ||
                        def == TorannMagicDefOf.TM_OrbitalStrike_III || def == TorannMagicDefOf.TM_Rend_III || def == TorannMagicDefOf.TM_BloodMoon_III ||
                        def == TorannMagicDefOf.TM_Polymorph_III || def == TorannMagicDefOf.TM_BestowMight_III || def == TorannMagicDefOf.TM_ChronostaticField_III) ||
                        def == TorannMagicDefOf.TM_Sunfire_III || def == TorannMagicDefOf.TM_P_RayofHope_III)
            {
                return true;
            }
            return false;
        }

        public static bool IsMasterAbility(AbilityUser.AbilityDef def)
        {
            if(def == TorannMagicDefOf.TM_Firestorm ||
                def == TorannMagicDefOf.TM_Blizzard  ||
                def == TorannMagicDefOf.TM_EyeOfTheStorm ||
                def == TorannMagicDefOf.TM_FoldReality ||
                def == TorannMagicDefOf.TM_RegrowLimb ||
                def == TorannMagicDefOf.TM_LichForm ||
                def == TorannMagicDefOf.TM_SummonPoppi ||
                def == TorannMagicDefOf.TM_BattleHymn ||
                def == TorannMagicDefOf.TM_Scorn ||
                def == TorannMagicDefOf.TM_PsychicShock  ||
                def == TorannMagicDefOf.TM_Meteor || def == TorannMagicDefOf.TM_Meteor_I || def == TorannMagicDefOf.TM_Meteor_II || def == TorannMagicDefOf.TM_Meteor_III ||
                def == TorannMagicDefOf.TM_OrbitalStrike || def == TorannMagicDefOf.TM_OrbitalStrike_I || def == TorannMagicDefOf.TM_OrbitalStrike_II || def == TorannMagicDefOf.TM_OrbitalStrike_III ||
                def == TorannMagicDefOf.TM_BloodMoon || def == TorannMagicDefOf.TM_BloodMoon_I || def == TorannMagicDefOf.TM_BloodMoon_II || def == TorannMagicDefOf.TM_BloodMoon_III ||
                def == TorannMagicDefOf.TM_Shapeshift ||
                def == TorannMagicDefOf.TM_Recall  ||
                def == TorannMagicDefOf.TM_HolyWrath ||
                def == TorannMagicDefOf.TM_SpiritOfLight ||
                def == TorannMagicDefOf.TM_Resurrection)
            {
                return true;
            }
            return false;
        }

        public static void AssignChaosMagicPowers(CompAbilityUserMagic comp, bool forAI = false)
        {
            
            if (comp != null)
            {
                int count = 2 + comp.MagicData.MagicPowerSkill_ChaosTradition.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_ChaosTradition_ver").level;
                for(int x =0; x < comp.MagicData.AllMagicPowersForChaosMage.Count; x++)
                {
                    MagicPower mp = comp.MagicData.AllMagicPowersForChaosMage[x];
                    mp.level = 0;
                    mp.autocast = false;
                }
                comp.MagicData.ResetAllSkills();
                comp.chaosPowers = new List<TM_ChaosPowers>();
                comp.chaosPowers.Clear();
                comp.RemovePowers(false);
                comp.MagicData.MagicAbilityPoints = comp.MagicData.MagicUserLevel;
                if(forAI)
                {
                    MagicPower p = comp.MagicData.AllMagicPowersForChaosMage.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Heal);
                    p.learned = true;
                    comp.chaosPowers.Add(new TM_ChaosPowers((TMAbilityDef)p.abilityDef, TM_ClassUtility.GetAssociatedMagicPowerSkill(comp, p)));
                    comp.AddPawnAbility(p.abilityDef);
                    p = comp.MagicData.AllMagicPowersForChaosMage.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LightningBolt);
                    p.learned = true;
                    comp.chaosPowers.Add(new TM_ChaosPowers((TMAbilityDef)p.abilityDef, TM_ClassUtility.GetAssociatedMagicPowerSkill(comp, p)));
                    comp.AddPawnAbility(p.abilityDef);
                    p = comp.MagicData.AllMagicPowersForChaosMage.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shield);
                    p.learned = true;
                    comp.chaosPowers.Add(new TM_ChaosPowers((TMAbilityDef)p.abilityDef, TM_ClassUtility.GetAssociatedMagicPowerSkill(comp, p)));
                    comp.AddPawnAbility(p.abilityDef);
                    p = comp.MagicData.AllMagicPowersForChaosMage.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Fireball);
                    p.learned = true;
                    comp.chaosPowers.Add(new TM_ChaosPowers((TMAbilityDef)p.abilityDef, TM_ClassUtility.GetAssociatedMagicPowerSkill(comp, p)));
                    comp.AddPawnAbility(p.abilityDef);
                    p = TM_Calc.GetRandomMagicPower(comp);
                    comp.chaosPowers.Add(new TM_ChaosPowers((TMAbilityDef)p.abilityDef, null));
                }
                for (int i = 0; i < 5; i++)
                {
                    MagicPower power = TM_Calc.GetRandomMagicPower(comp);
                    if (i < count)
                    {
                        power.learned = true;
                        comp.chaosPowers.Add(new TM_ChaosPowers((TMAbilityDef)power.abilityDef, TM_ClassUtility.GetAssociatedMagicPowerSkill(comp, power)));
                        TMAbilityDef tmad = power.abilityDef as TMAbilityDef;
                        if (tmad.shouldInitialize) //power.abilityDef != TorannMagicDefOf.TM_TechnoBit && power.abilityDef != TorannMagicDefOf.TM_WandererCraft && power.abilityDef != TorannMagicDefOf.TM_Cantrips)
                        {
                            comp.AddPawnAbility(power.abilityDef);                            
                        }
                        if (tmad.childAbilities != null && tmad.childAbilities.Count > 0)
                        {
                            foreach (TMAbilityDef ad in tmad.childAbilities)
                            {
                                if (ad.shouldInitialize)
                                {
                                    comp.AddPawnAbility(ad);
                                }
                            }
                        }
                    }
                    else
                    {
                        comp.chaosPowers.Add(new TM_ChaosPowers((TMAbilityDef)power.abilityDef, null));
                    }
                }
                comp.InitializeSpell();
            }
            else
            {
                Log.Message("failed chaos mage ability assignment - null comp");
            }
        }

        public static MagicPower GetRandomMagicPower(CompAbilityUserMagic comp, bool includeMasterPower = false)
        {
            bool isDuplicate = true;
            MagicPower power = null;
            while (isDuplicate)
            {
                isDuplicate = false;
                power = comp.MagicData.AllMagicPowersForChaosMage.RandomElement();
                for (int i = 0; i < comp.chaosPowers.Count; i++)
                {
                    if (comp.chaosPowers[i].Ability == power.abilityDef)
                    {
                        isDuplicate = true;
                    }
                }
            }
            return power;
        }

        public static bool IsUsingRanged(Pawn p)
        {
            bool result = false;
            if (p != null && p.equipment != null && p.equipment.Primary != null && p.equipment.Primary.def.IsRangedWeapon)
            {
                result = true;
            }
            return result;
        }

        public static bool IsUsingMelee(Pawn p)
        {
            bool result = false;
            if (p != null)
            {
                if (p.equipment != null)
                {
                    if (p.equipment.Primary != null && p.equipment.Primary.def.IsMeleeWeapon)
                    {
                        result = true;
                    }
                    if (p.equipment.Primary == null)
                    {
                        result = true;
                    }
                }
            }
            return result;
        }

        public static bool IsUsingPistol(Pawn p)
        {
            bool result = false;
            if (TM_Calc.IsUsingRanged(p))
            {
                Thing wpn = p.equipment.Primary;
                CompAbilityUserMight mightComp = p.GetCompAbilityUserMight();
                //Log.Message("" + p.LabelShort + " is using a " + wpn.def.defName);
                if(mightComp != null && mightComp.equipmentContainer != null && mightComp.equipmentContainer.Count > 0)
                {
                    result = true;
                }
                else if (wpn.def.defName.ToLower().Contains("pistol") || wpn.def.defName.Contains("SMG") || wpn.def.defName.ToLower().Contains("revolver"))
                {
                    //Log.Message("weapon name contains pistol: " + wpn.def.defName);
                    result = true;
                }
                else if(TM_Data.PistolList().Contains(wpn.def))
                {
                    //Log.Message("weapon found in custom defnames");
                    result = true;
                }
                //else
                //{
                //    Messages.Message("TM_MustHaveWeaponType".Translate(
                //    p.LabelShort,
                //    wpn.LabelShort,
                //    "pistol"
                //    ), MessageTypeDefOf.NegativeEvent);                    
                //}
            }
            //else
            //{
            //    Messages.Message("MustHaveRangedWeapon".Translate(
            //        p.LabelCap
            //    ), MessageTypeDefOf.RejectInput);
            //}
            return result;
        }

        public static bool IsUsingRifle(Pawn p)
        {
            bool result = false;
            if (IsUsingRanged(p))
            {
                Thing wpn = p.equipment.Primary;
                CompAbilityUserMight mightComp = p.GetCompAbilityUserMight();
                //Log.Message("" + p.LabelShort + " is using a " + wpn.def.defName);
                if (mightComp != null && mightComp.equipmentContainer != null && mightComp.equipmentContainer.Count > 0)
                {
                    result = true;
                }
                else if ((wpn.def.defName.ToLower().Contains("rifle") || wpn.def.defName.Contains("LMG")) && !(wpn.def.defName.ToLower().Contains("sniper")))
                {
                    //Log.Message("weapon name contains rifle: " + wpn.def.defName);
                    result = true;
                }
                else if (TM_Data.RifleList().Contains(wpn.def))
                {
                    //Log.Message("weapon found in custom defnames");
                    result = true;
                }
                //else
                //{
                //    Messages.Message("TM_MustHaveWeaponType".Translate(
                //    p.LabelShort,
                //    wpn.LabelShort,
                //    "rifle"
                //    ), MessageTypeDefOf.NegativeEvent);
                //}
            }
            //else
            //{
            //    Messages.Message("MustHaveRangedWeapon".Translate(
            //        p.LabelCap
            //    ), MessageTypeDefOf.RejectInput);
            //}
            return result;
        }

        public static bool IsUsingShotgun(Pawn p)
        {
            bool result = false;
            if (IsUsingRanged(p))
            {
                Thing wpn = p.equipment.Primary;
                CompAbilityUserMight mightComp = p.GetCompAbilityUserMight();
                //Log.Message("" + p.LabelShort + " is using a " + wpn.def.defName);
                if (mightComp != null && mightComp.equipmentContainer != null && mightComp.equipmentContainer.Count > 0)
                {
                    result = true;
                }
                else if (wpn.def.defName.ToLower().Contains("shotgun"))
                {
                    //Log.Message("weapon name contains shotgun: " + wpn.def.defName);
                    result = true;
                }
                else if (TM_Data.ShotgunList().Contains(wpn.def))
                {
                    //Log.Message("weapon found in custom defnames");
                    result = true;
                }
                //else
                //{
                //    Messages.Message("TM_MustHaveWeaponType".Translate(
                //    p.LabelShort,
                //    wpn.LabelShort,
                //    "shotgun"
                //    ), MessageTypeDefOf.NegativeEvent);
                //}
            }
            //else
            //{
            //    Messages.Message("MustHaveRangedWeapon".Translate(
            //        p.LabelCap
            //    ), MessageTypeDefOf.RejectInput);
            //}
            return result;
        }

        public static bool IsUsingBow(Pawn p)
        {
            bool result = false;
            if (IsUsingRanged(p))
            {
                Thing wpn = p.equipment.Primary;
                CompAbilityUserMight mightComp = p.GetCompAbilityUserMight();
                //Log.Message("" + p.LabelShort + " is using a " + wpn.def.defName);
                if (mightComp != null && mightComp.equipmentContainer != null && mightComp.equipmentContainer.Count > 0)
                {
                    result = true;
                }
                else if ((wpn.def.Verbs.FirstOrDefault<VerbProperties>().defaultProjectile.projectile.damageDef.defName.ToLower().Contains("arrow") || wpn.def.defName.ToLower().Contains("bow")))
                {
                    //Log.Message("weapon name contains shotgun: " + wpn.def.defName);
                    result = true;
                }
                else if (TM_Data.BowList().Contains(wpn.def))
                {
                    //Log.Message("weapon found in custom defnames");
                    result = true;
                }
            }
            return result;
        }

        public static bool IsUsingMagicalFoci(Pawn p)
        {
            bool result = false;
            if (p != null && p.equipment != null && p.equipment.Primary != null && p.equipment.Primary.def.IsRangedWeapon)
            {
                Thing wpn = p.equipment.Primary;
                CompAbilityUserMight mightComp = p.GetCompAbilityUserMight();
                //Log.Message("" + p.LabelShort + " is using a " + wpn.def.defName);
                if (mightComp != null && mightComp.equipmentContainer != null && mightComp.equipmentContainer.Count > 0)
                {
                    result = true;
                }
                else if (wpn.def.defName.ToLower().Contains("wand") || wpn.def.defName.ToLower().Contains("staff"))
                {
                    //Log.Message("weapon name contains shotgun: " + wpn.def.defName);
                    result = true;
                }
                else if (TM_Data.MagicFociList().Contains(wpn.def))
                {
                    //Log.Message("weapon found in custom defnames");
                    result = true;
                }
            }
            return result;
        }

        public static bool IsUsingCustomWeaponCategory(Pawn p, string str)
        {
            bool result = false;
            if (p != null && p.equipment != null && p.equipment.Primary != null)
            {
                Thing wpn = p.equipment.Primary;
                if (TM_Data.CustomWeaponCategoryList(str).Contains(wpn.def.defName))
                {
                    //Log.Message("weapon found in custom defnames");
                    result = true;
                }
            }
            return result;
        }

        public static float GetSkillDamage(Pawn p)
        {
            float result = 0;

            CompAbilityUserMight compMight = p.GetCompAbilityUserMight();
            CompAbilityUserMagic compMagic = p.GetCompAbilityUserMagic();
            float strFactor = 1f;
            if (compMight != null && compMight.IsMightUser)
            {
                strFactor = compMight.mightPwr;
            }
            else if (compMagic != null && compMagic.IsMagicUser)
            {
                strFactor = compMagic.arcaneDmg;
            }

            if (p.equipment != null && p.equipment.Primary != null)
            {
                if(p.equipment.Primary.def.IsMeleeWeapon)
                {
                    result = GetSkillDamage_Melee(p, strFactor);
                    compMight.weaponCritChance = GetWeaponCritChance(p.equipment.Primary);
                    compMagic.weaponCritChance = GetWeaponCritChance(p.equipment.Primary);
                }
                else
                {
                    result = GetSkillDamage_Range(p, strFactor);
                    compMight.weaponCritChance = 0f;
                    compMagic.weaponCritChance = 0f;
                }
            }
            else
            {
                result = p.GetStatValue(StatDefOf.MeleeDPS, false) * strFactor;
            }

            return result;
        }

        public static float GetWeaponCritChance(ThingWithComps weapon)
        {
            float weaponMass = weapon.GetStatValue(StatDefOf.Mass, false);            
            if(weaponMass != 0)
            {
                QualityCategory qual;
                weapon.TryGetQuality(out qual);
                int q = (int)qual;

                float critChanceByMass = .1f + (.2f / weaponMass);
                float critChanceByQuality = (.05f * (float)q);

                return (critChanceByMass + critChanceByQuality);
            }


            return 0f;
        }

        public static float GetSkillDamage_Melee(Pawn p, float strFactor)
        {
            float weaponDamage = 0f;
            ThingWithComps weaponComp = p.equipment.Primary;
            float weaponDPS = weaponComp.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS, false);
            float pawnDPS = p.GetStatValue(StatDefOf.MeleeDPS, false);

            weaponDamage = Mathf.Max((pawnDPS + weaponDPS) * strFactor, 5f);

            return weaponDamage;
        }

        public static float GetSkillDamage_Range(Pawn p, float strFactor)
        {
            VerbProperties vp = p.equipment.Primary.def.Verbs?.FirstOrDefault();
            if (vp != null)
            {
                QualityCategory qc = QualityCategory.Normal;
                //p.equipment.Primary.TryGetQuality(out qc);
                float qc_m = GetQualityMultiplier(qc);
                float weaponDamage = ((vp.defaultProjectile.projectile.GetDamageAmount(p.equipment.Primary) * qc_m) - (2 * (vp.warmupTime + vp.defaultCooldownTime))) + (3 * vp.defaultProjectile.projectile.stoppingPower);
                weaponDamage *= strFactor;
                return weaponDamage;
            }
            return 0;
        }

        public static float GetQualityMultiplier(QualityCategory qc)
        {
            switch (qc)
            {
                case QualityCategory.Awful:
                    return .9f;
                case QualityCategory.Poor:
                    return .95f;
                case QualityCategory.Normal:
                    return 1f;
                case QualityCategory.Good:
                    return 1.05f;
                case QualityCategory.Excellent:
                    return 1.1f;
                case QualityCategory.Masterwork:
                    return 1.25f;
                case QualityCategory.Legendary:
                    return 1.5f;
                default:
                    return 1f;
            }
        }

        public static float GetOverallArmor(Pawn p, StatDef stat)
        {
            float num = 0f;
            float num2 = Mathf.Clamp01(p.GetStatValue(stat) / 2f);
            List<BodyPartRecord> allParts = p.RaceProps.body.AllParts;
            List<Apparel> list = (p.apparel != null) ? p.apparel.WornApparel : null;
            for (int i = 0; i < allParts.Count; i++)
            {
                float num3 = 1f - num2;
                if (list != null)
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j].def.apparel.CoversBodyPart(allParts[i]))
                        {
                            float num4 = Mathf.Clamp01(list[j].GetStatValue(stat) / 2f);
                            num3 *= 1f - num4;
                        }
                    }
                }
                num += allParts[i].coverageAbs * (1f - num3);
            }
            num = Mathf.Clamp((num * 2f) + num2, 0f, 2f);
            return num;
        }

        public static string GetEnchantmentsString(Thing item)
        {
            CompEnchantedItem enchantedItem = ThingCompUtility.TryGetComp<CompEnchantedItem>(item);
            if (enchantedItem != null)
            {
                StringBuilder stringBuilder = new StringBuilder();
                string rectLabel = "Enchantments:";
                stringBuilder.Append(rectLabel);
                if (enchantedItem.maxMP != 0)
                {
                    stringBuilder.AppendInNewLine(enchantedItem.MaxMPLabel );
                }
                if (enchantedItem.mpCost != 0)
                {
                    stringBuilder.AppendInNewLine(enchantedItem.MPCostLabel );
                }
                if (enchantedItem.mpRegenRate != 0)
                {
                    stringBuilder.AppendInNewLine(enchantedItem.MPRegenRateLabel );
                }
                if (enchantedItem.coolDown != 0)
                {
                    stringBuilder.AppendInNewLine(enchantedItem.CoolDownLabel);
                }
                if (enchantedItem.xpGain != 0)
                {
                    stringBuilder.AppendInNewLine(enchantedItem.XPGainLabel);
                }
                if (enchantedItem.arcaneRes != 0)
                {
                    stringBuilder.AppendInNewLine(enchantedItem.ArcaneResLabel );
                }
                if (enchantedItem.arcaneDmg != 0)
                {
                    stringBuilder.AppendInNewLine(enchantedItem.ArcaneDmgLabel);
                }
                if (enchantedItem.arcaneSpectre != false)
                {
                    stringBuilder.AppendInNewLine(enchantedItem.ArcaneSpectreLabel);
                }
                if (enchantedItem.phantomShift != false)
                {
                    stringBuilder.AppendInNewLine(enchantedItem.PhantomShiftLabel);
                }
                if (enchantedItem.hediff != null)
                {
                    stringBuilder.AppendInNewLine(enchantedItem.HediffLabel);
                }
                if (enchantedItem.MagicAbilities != null && enchantedItem.MagicAbilities.Count > 0)
                {
                    StringBuilder stringBuilder2 = new StringBuilder();
                    GUI.color = GenEnchantmentColor.EnchantmentColor(enchantedItem.skillTier);
                    string abilityLabels = "Abilities: ";
                    stringBuilder2.Append(abilityLabels);
                    for (int i = 0; i < enchantedItem.MagicAbilities.Count; i++)
                    {
                        if (i + 1 < enchantedItem.MagicAbilities.Count)
                        {
                            stringBuilder2.Append(enchantedItem.MagicAbilities[i].LabelCap + ", ");
                        }
                        else
                        {
                            stringBuilder2.Append(enchantedItem.MagicAbilities[i].LabelCap);
                        }
                    }
                    stringBuilder.AppendInNewLine(stringBuilder2.ToString());
                }
                if (enchantedItem.SoulOrbTraits != null && enchantedItem.SoulOrbTraits.Count > 0)
                {
                    StringBuilder stringBuilder3 = new StringBuilder();
                    string abilityLabels = "Absorbed Traits: ";
                    stringBuilder3.Append(abilityLabels);
                    for (int i = 0; i < enchantedItem.SoulOrbTraits.Count; i++)
                    {
                        if (i + 1 < enchantedItem.SoulOrbTraits.Count)
                        {
                            stringBuilder3.Append(enchantedItem.SoulOrbTraits[i].LabelCap + ", ");
                        }
                        else
                        {
                            stringBuilder3.Append(enchantedItem.SoulOrbTraits[i].LabelCap);
                        }
                    }
                    rectLabel = stringBuilder3.ToString();
                    stringBuilder.AppendInNewLine(rectLabel);
                }
                return stringBuilder.ToString();
            }
            return "";
        }

        public static List<IntVec3> GetOuterRing(IntVec3 center, float innerRadius, float outerRadius)
        {
            List<IntVec3> innerRing = GenRadial.RadialCellsAround(center, innerRadius, true).ToList();
            List<IntVec3> outerRing = GenRadial.RadialCellsAround(center, outerRadius, false).Except(innerRing).ToList();
            return outerRing;
        }

        public static bool PawnCanOccupyCell(Pawn pawn, IntVec3 c)
        {
            if (!c.Walkable(pawn.Map))
            {
                return false;
            }
            Building edifice = c.GetEdifice(pawn.Map);
            if (edifice != null)
            {
                Building_Door building_Door = edifice as Building_Door;
                if (building_Door != null && !building_Door.PawnCanOpen(pawn) && !building_Door.Open)
                {
                    return false;
                }
            }
            return true;            
        }

        public static LocalTargetInfo FindClosestCellPlus1VisibleToTarget(Pawn p, LocalTargetInfo t, bool requireLoS = true)
        {
            LocalTargetInfo tmp = p.Position;
            IntVec3 bestCell = p.Position;
            for(int i =0;i < 8;i++)
            {
                IntVec3 cell = p.Position + GenAdj.AdjacentCells8WayRandomized()[i];
                if (cell.Walkable(p.Map) && cell.Standable(p.Map))
                {
                    if ((cell - t.Cell).LengthHorizontal > (bestCell - t.Cell).LengthHorizontal)
                    {
                        if (requireLoS)
                        {
                            if (TM_Calc.HasLoSFromTo(cell, t, p, 0, 5f))
                            {
                                bestCell = cell;
                            }
                        }
                        else
                        {
                            bestCell = cell;
                        }
                    }
                }
            }
            tmp = bestCell;
            
            return tmp;
        }

        public static LocalTargetInfo FindWalkableCellNextTo(IntVec3 cell, Map map)
        {
            List<IntVec3> cellList = GenAdjFast.AdjacentCells8Way(cell);
            for (int i = 0; i < cellList.Count; i++)
            {
                if (cellList[i] != default(IntVec3) && cellList[i].InBoundsWithNullCheck(map) && cellList[i].Walkable(map) && !cellList[i].Fogged(map))
                {
                    cell = cellList[i];
                    break;
                }
            }
            return cell;
        }

        public static LocalTargetInfo FindValidCellWithinRange(IntVec3 cell, Map map, float range)
        {
            List<IntVec3> cellList = GenRadial.RadialCellsAround(cell, range, true).InRandomOrder().ToList();
            for (int i = 0; i < cellList.Count; i++)
            {
                if (cellList[i] != default(IntVec3) && cellList[i].InBoundsWithNullCheck(map) && !cellList[i].Fogged(map))
                {
                    cell = cellList[i];
                    break;
                }
            }
            return cell;
        }

        public static InspirationDef GetRandomAvailableInspirationDef(Pawn pawn)
        {
            return (from x in DefDatabase<InspirationDef>.AllDefsListForReading
                    where x.Worker.InspirationCanOccur(pawn)
                    select x).RandomElementByWeightWithFallback((InspirationDef x) => x.Worker.CommonalityFor(pawn), null);
        }

        public static LocalTargetInfo GetAutocastTarget(Pawn caster, TM_Autocast autocasting, LocalTargetInfo potentialTarget)
        {
            LocalTargetInfo target = new LocalTargetInfo();

            if (autocasting.type == AutocastType.OnTarget && potentialTarget.IsValid)
            {
                target = potentialTarget.Thing;
            }
            else if (autocasting.type == AutocastType.OnSelf)
            {
                target = caster;
            }
            else if (autocasting.type == AutocastType.OnNearby)
            {
                if (autocasting.GetTargetType == typeof(Pawn))
                {
                    IEnumerable<Pawn> nearbyThings = from x in caster.Map.mapPawns.AllPawnsSpawned
                                                     where (x.GetType() == typeof(Pawn) &&
                                                     (x.Position - caster.Position).LengthHorizontal >= autocasting.minRange &&
                                                     autocasting.maxRange > 0 ? (x.Position - caster.Position).LengthHorizontal <= autocasting.maxRange : true &&
                                                     autocasting.includeSelf ? true : x != caster)                                                     
                                                     select x;
                    List<Pawn> potentialPawns = new List<Pawn>();
                    potentialPawns.Clear();
                    foreach(Pawn p in nearbyThings)
                    {
                        if(p.Faction == null)
                        {
                            if(autocasting.targetNeutral)
                            {
                                potentialPawns.Add(p);
                            }
                        }
                        else
                        {
                            if (p.Faction != caster.Faction && !p.IsPrisoner)
                            {
                                if (autocasting.targetNeutral && !p.Faction.HostileTo(caster.Faction))
                                {
                                    potentialPawns.Add(p);
                                }
                                else if (autocasting.targetEnemy && p.Faction.HostileTo(caster.Faction))
                                {
                                    potentialPawns.Add(p);
                                }
                            }
                            else if(autocasting.targetFriendly)
                            {
                                potentialPawns.Add(p);
                            }
                        }
                    }
                    target = potentialPawns.Count > 0 ? potentialPawns.RandomElement() : null;
                }
                else if (autocasting.GetTargetType == typeof(Building))
                {
                    IEnumerable<Building> nearbyThings = from x in caster.Map.listerThings.AllThings
                                                               where (x.GetType() == typeof(Building) && (x.Position - caster.Position).LengthHorizontal >= autocasting.minRange &&
                                                               autocasting.maxRange > 0 ? (x.Position - caster.Position).LengthHorizontal <= autocasting.maxRange : true)
                                                               select x as Building;
                    List<Building> potentialBuildings = new List<Building>();
                    potentialBuildings.Clear();
                    foreach (Building p in nearbyThings)
                    {
                        if (p.Faction == null)
                        {
                            if (autocasting.targetNeutral)
                            {
                                potentialBuildings.Add(p);
                            }
                        }
                        else
                        {
                            if (p.Faction != caster.Faction)
                            {
                                if (autocasting.targetNeutral && !p.Faction.HostileTo(caster.Faction))
                                {
                                    potentialBuildings.Add(p);
                                }
                                else if (autocasting.targetEnemy && p.Faction.HostileTo(caster.Faction))
                                {
                                    potentialBuildings.Add(p);
                                }
                            }
                            else if (autocasting.targetFriendly)
                            {
                                potentialBuildings.Add(p);
                            }
                        }
                    }
                    target = potentialBuildings.Count > 0 ? potentialBuildings.RandomElement() : null;
                }
                else if (autocasting.GetTargetType == typeof(Corpse))
                {
                    IEnumerable<Corpse> nearbyThings = from x in caster.Map.listerThings.AllThings
                                                               where (x.GetType() == typeof(Corpse) && (x.Position - caster.Position).LengthHorizontal >= autocasting.minRange &&
                                                               autocasting.maxRange > 0 ? (x.Position - caster.Position).LengthHorizontal <= autocasting.maxRange : true)
                                                               select x as Corpse;
                    target = nearbyThings?.Count() > 0 ? nearbyThings.RandomElement() : null;
                }
                else if (autocasting.GetTargetType == typeof(ThingWithComps))
                {
                    IEnumerable<ThingWithComps> nearbyThings = from x in caster.Map.listerThings.AllThings
                                                      where (x.GetType() == typeof(ThingWithComps) && (x.Position - caster.Position).LengthHorizontal >= autocasting.minRange &&
                                                      autocasting.maxRange > 0 ? (x.Position - caster.Position).LengthHorizontal <= autocasting.maxRange : true &&
                                                      autocasting.includeSelf ? true : x != caster)
                                                      select x as ThingWithComps;
                    target = nearbyThings?.Count() > 0 ? nearbyThings.RandomElement() : null;
                }
                else
                {
                    IEnumerable<Thing> nearbyThings = from x in caster.Map.listerThings.AllThings
                                                      where ((x.Position - caster.Position).LengthHorizontal >= autocasting.minRange &&
                                                      autocasting.maxRange > 0 ? (x.Position - caster.Position).LengthHorizontal <= autocasting.maxRange : true &&
                                                      autocasting.includeSelf ? true : x != caster)
                                                      select x;
                    target = nearbyThings?.Count() > 0 ? nearbyThings.RandomElement() : null;
                }
                
            }
            else if (autocasting.type == AutocastType.OnCell && potentialTarget.IsValid)
            {
                target = potentialTarget.Cell;
            }
            return target;
        }

        public static List<Building> FindConnectedWalls(Building start, float maxAllowedDistance = 1.4f, float maxDistanceFromStart = 50, bool matchFaction = true)
        {
            Map map = start.Map;
            List<Building> connectedBuildings = new List<Building>();
            connectedBuildings.Clear();
            connectedBuildings.Add(start);
            List<Building> newBuildings = new List<Building>();
            newBuildings.Clear();
            newBuildings.Add(start);
            //List<Building> lastList = new List<Building>();
            //lastList.Clear();
            //lastList.Add(start);
            IEnumerable<Building> allThings = from def in map.listerThings.AllThings
                                       where (def is Building && TM_Calc.IsWall(def) && (def.Position - start.Position).LengthHorizontal <= maxDistanceFromStart)
                                       select def as Building;
            List <Building> addedBuilding = new List<Building>();
            for (int i = 0; i < 200; i++)
            {                
                addedBuilding.Clear();
                foreach (Building b in newBuildings)
                {
                    foreach (Building t in allThings)
                    {
                        if ((t.Position - b.Position).LengthHorizontal <= maxAllowedDistance && !connectedBuildings.Contains(t))
                        {
                            connectedBuildings.Add(t);
                            addedBuilding.Add(t);
                        }
                    }
                    //IEnumerable<Building> tmpList = allThings.Except(connectedBuildings);
                    //allThings = tmpList;
                }
                //lastList.Clear();
                //lastList.AddRange(newBuildings);
                newBuildings.Clear();
                newBuildings.AddRange(addedBuilding);
                if(newBuildings.Count <= 0)
                {
                    break;
                }
            }
            //Log.Message("there are " + connectedBuildings.Count + " connected wall segments");
            return connectedBuildings;
        }

        public static List<IntVec3> FindTPath(Thing from, Thing to,  Faction faction = null) //out List<IntVec3> connectedCells,
        {
            //use a structure to record path parameters
            //look for any transmitter nearby 
            //nearby transmitters are considered a possible path
            //add nearby path to list of paths
            //multiple nearby transmitters create a new path structure that gets enumerated
            //terminate a paths if no nearby transmitter is found

            Map map = from.Map;
            List<IntVec3> allCells = new List<IntVec3>();
            List<IntVec3> startList = new List<IntVec3>();
            List<IntVec3> bestPath = new List<IntVec3>();
            List<TPath> pathFinder = new List<TPath>();

            allCells.Clear();
            pathFinder.Clear();

            startList.Add(from.Position);
            pathFinder.Add(new TPath(0, 0, false, from.Position, startList));

            bool pathFound = false;
            int bestPathIndex = 0;

            for (int i = 0; i < 300; i++) //fail after 300 path attempts
            {
                for (int j = 0; j < pathFinder.Count; j++)
                {
                    if (!pathFinder[j].ended)
                    {
                        List<IntVec3> cellList = GenRadial.RadialCellsAround(pathFinder[j].currentCell, 1f, true).ToList();
                        List<IntVec3> validCells = new List<IntVec3>();
                        validCells.Clear();
                        //Log.Message("" + cellList.Count.ToString());
                        for (int k = 0; k < cellList.Count; k++)
                        {
                            //CELLLIST is all the cells around the CURRENT cell.
                            Building wall = CellWall(cellList[k], map);
                            if (!allCells.Contains(cellList[k]) && wall != null)
                            {
                                if(faction != null)
                                {
                                    if(faction == wall.Faction)
                                    {
                                        allCells.Add(cellList[k]);
                                        validCells.Add(cellList[k]);
                                    }
                                }
                                else
                                {
                                    allCells.Add(cellList[k]);
                                    validCells.Add(cellList[k]);
                                }                                
                            }
                        }
                        if (validCells.Count > 0)
                        {
                            //IF WE FOUND MORE THAN 1 "VALID" cell around the "CURRENTCELL" we loop through those cells
                            for (int k = 0; k < validCells.Count; k++)
                            {
                                if (k == 0)
                                {
                                    //Check the first valid cell
                                    //continue path in a single direction; additional possible paths create a branch
                                    pathFinder[j].pathList.Add(validCells[k]);
                                    pathFinder[j] = new TPath(pathFinder[j].pathParent, pathFinder[j].pathParentSplitIndex, false, validCells[k], pathFinder[j].pathList);

                                    if (to.Position == validCells[k])
                                    {
                                        pathFound = true;
                                        bestPathIndex = j;
                                    }
                                }
                                else
                                {
                                    //create new paths
                                    List<IntVec3> newList = new List<IntVec3>();
                                    newList.Clear();
                                    newList.Add(validCells[k]);
                                    pathFinder.Add(new TPath(j, pathFinder[j].pathList.Count, false, validCells[k], newList));
                                    if (to.Position == validCells[k])
                                    {
                                        pathFound = true;
                                        bestPathIndex = j;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //end path
                            pathFinder[j] = new TPath(pathFinder[j].pathParent, pathFinder[j].pathParentSplitIndex, true, pathFinder[j].currentCell, pathFinder[j].pathList);
                        }
                    }
                }

                if (pathFound)
                {
                    //Evaluate best path, reverse, and return
                    bestPath = GetBestPath(pathFinder, bestPathIndex);
                    break;
                }
            }
            //connectedCells = allCells;
            return bestPath;
        }

        public static Building CellWall(IntVec3 cell, Map map)
        {
            //Determines if a cell has a wall built on it
            Building wall = null;
            if (cell != default(IntVec3) && cell.InBoundsWithNullCheck(map))
            {
                List<Thing> tList = cell.GetThingList(map);
                if(tList != null && tList.Count > 0)
                {
                    foreach(Thing t in tList)
                    {
                        if(IsWall(t))
                        {
                            wall = t as Building;
                            break;
                        }
                    }
                }
            }
            return wall;
        }

        public static List<IntVec3> GetBestPath(List<TPath> pathFinder, int index)
        {
            //Evaluates path structure from ending cell to start cell
            //First evaluated path always uses full list
            //Following paths can be branched; eliminate excess cells from those lists 
            //by recording when the valid path branches
            List<IntVec3> tracebackList = new List<IntVec3>();

            tracebackList.Clear();

            bool tracebackComplete = false;
            int parentIndexCount = pathFinder[index].pathList.Count;

            while (!tracebackComplete)
            {
                List<IntVec3> tmpTrace = new List<IntVec3>();
                tmpTrace.Clear();
                //ignore index 0 (starting point)
                if (index != 0)
                {
                    for (int i = 0; i < parentIndexCount; i++)
                    {
                        //construct the reverse path
                        tmpTrace.Add(pathFinder[index].pathList[i]);
                    }
                }

                if (index == 0)
                {
                    //finished return path
                    tracebackComplete = true;
                }
                else
                {
                    //construct valid reverse path from point path branches from parent
                    tmpTrace.Reverse();
                    tracebackList.AddRange(tmpTrace);
                    parentIndexCount = pathFinder[index].pathParentSplitIndex;
                    index = pathFinder[index].pathParent;
                }
            }
            tracebackList.Reverse();
            tracebackList = SnipPath(tracebackList);
            return tracebackList;
        }

         public static List<IntVec3> SnipPath(List<IntVec3> tracebackList)
        {
            IntVec3 last1Cell = default(IntVec3);
            IntVec3 last2Cell = default(IntVec3);
            for (int i = 0; i < tracebackList.Count; i++)
            {
                if (i > 0)
                {
                    last1Cell = tracebackList[i - 1];
                    if (i > 1)
                    {
                        last2Cell = tracebackList[i - 2];
                    }
                }

                if (last1Cell != default(IntVec3) && last2Cell != default(IntVec3))
                {
                    if ((last2Cell - tracebackList[i]).LengthHorizontal <= (last1Cell - tracebackList[i]).LengthHorizontal)
                    {
                        tracebackList.Remove(last1Cell);
                    }
                }
                last1Cell = default(IntVec3);
                last2Cell = default(IntVec3);
            }

            return tracebackList;
        }

        public static List<Vector3> IntVec3List_To_Vector3List(List<IntVec3> intVecList)
        {
            List<Vector3> vector3List = new List<Vector3>();
            vector3List.Clear();
            for (int i = 0; i < intVecList.Count; i++)
            {
                vector3List.Add(intVecList[i].ToVector3Shifted());
            }
            return vector3List;
        }

        public static bool DangerMusicMode
        {
            get
            {
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    if (maps[i].dangerWatcher.DangerRating == StoryDanger.High)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public static bool IsAlterableWeather(Map map, out WeatherDef w)
        {
            bool result = false;
            w = null;
            if (map != null && map.weatherManager != null && map.weatherManager.curWeather != null)
            {
                w = map.weatherManager.curWeather;
                if (w.defName == "SnowHard" || w.defName == "SnowGentle" || w.defName == "Rain" || w.defName == "RainyThunderstorm" || w.defName == "FoggyRain")
                {
                    result = true;
                }
            }
            return result;
        }
    }
}
