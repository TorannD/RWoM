using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using HarmonyLib;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_AnimalFriend : Verb_UseAbility
    {

        bool validTarg;
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = true;
                }
                else
                {
                    //out of range
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot()
        {
            bool flag = false;
            CompAbilityUserMight comp = base.CasterPawn.GetCompAbilityUserMight();
            MightPowerSkill pwr = base.CasterPawn.GetCompAbilityUserMight().MightData.MightPowerSkill_AnimalFriend.FirstOrDefault((MightPowerSkill x) => x.label == "TM_AnimalFriend_pwr");
            MightPowerSkill ver = base.CasterPawn.GetCompAbilityUserMight().MightData.MightPowerSkill_AnimalFriend.FirstOrDefault((MightPowerSkill x) => x.label == "TM_AnimalFriend_ver");
            Pawn pawn = this.CasterPawn;
            Pawn animal = this.currentTarget.Thing as Pawn;

            if(animal !=null && animal.RaceProps.Animal && animal.RaceProps.IsFlesh)
            {
                Pawn oldbond = comp.bondedPet;
                if (animal == comp.bondedPet)
                {
                    Hediff animalBondHD = oldbond.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_RangerBondHD"));
                    if (animalBondHD != null && Rand.Chance(((1f + animalBondHD.ageTicks / 6000000) - oldbond.RaceProps.wildness)))
                    {
                        Messages.Message("TM_BondedAnimalReleaseToColony".Translate(
                                                oldbond.LabelShort,
                                                pawn.LabelShort
                                            ), MessageTypeDefOf.NeutralEvent);
                        MoteMaker.MakeInteractionBubble(oldbond, pawn, InteractionDefOf.Nuzzle.interactionMote, InteractionDefOf.Nuzzle.GetSymbol());
                        oldbond.health.RemoveHediff(animalBondHD);
                        DefMap<TrainableDef, int> steps = Traverse.Create(root: oldbond.training).Field(name: "steps").GetValue<DefMap<TrainableDef, int>>();
                        steps[TrainableDefOf.Tameness] = 1;
                        steps[TrainableDefOf.Obedience] = 0;
                        steps[TrainableDefOf.Release] = 0;
                        steps[TorannMagicDefOf.Haul] = 0;
                        steps[TorannMagicDefOf.Rescue] = 0;
                        comp.bondedPet = null;
                    }
                    else
                    {
                        Messages.Message("TM_BondedAnimalRelease".Translate(
                                                oldbond.LabelShort,
                                                pawn.LabelShort
                                            ), MessageTypeDefOf.NeutralEvent);
                        FleckMaker.ThrowSmoke(oldbond.DrawPos, oldbond.Map, 3f);
                        oldbond.Destroy();
                    }
                }
                else if(animal.Faction != null && animal.Faction != pawn.Faction)
                {
                    Messages.Message("TM_AnimalHasAllegience".Translate(
                                        ), MessageTypeDefOf.RejectInput);
                }
                else if(animal.health != null && animal.health.hediffSet != null && animal.health.hediffSet.HasHediff(HediffDef.Named("TM_RangerBondHD")))
                {
                    Messages.Message("TM_AnimalAlreadyHasBond".Translate(animal.LabelShort
                                        ), MessageTypeDefOf.RejectInput);
                }
                else
                {
                    if (animal.RaceProps.intelligence == Intelligence.Animal) // == TrainableIntelligenceDefOf.Intermediate || animal.RaceProps.TrainableIntelligence == TrainableIntelligenceDefOf.Advanced)
                    {
                        if ((animal.RaceProps.wildness <= .7f) || (animal.RaceProps.wildness <= .8f && pwr.level == 1) || (animal.RaceProps.wildness <= .9f && pwr.level == 2) || pwr.level == 3)
                        {
                            if (Rand.Chance(.6f + (.05f *pwr.level)) && Rand.Chance(((.7f + (.1f *pwr.level)) - animal.RaceProps.wildness) * 10))
                            {
                                if (comp.bondedPet != null && comp.bondedPet != animal)
                                {                                    
                                    if (!oldbond.Destroyed)
                                    {
                                        if (!comp.bondedPet.Dead)
                                        {
                                            //bonding with another pet without first pet being dead or destroyed
                                            comp.bondedPet = null;
                                            Messages.Message("TM_BondedAnimalRelease".Translate(
                                            oldbond.LabelShort,
                                            pawn.LabelShort
                                            ), MessageTypeDefOf.NeutralEvent);
                                            if (oldbond.Map != null)
                                            {
                                                FleckMaker.ThrowSmoke(oldbond.DrawPos, oldbond.Map, 3f);
                                            }
                                            else
                                            {
                                                oldbond.ParentHolder.GetDirectlyHeldThings().Remove(oldbond);
                                            }
                                            oldbond.Destroy();
                                        }
                                    }
                                }
                                animal.SetFaction(pawn.Faction);
                                HealthUtility.AdjustSeverity(animal, TorannMagicDefOf.TM_RangerBondHD, -4f);
                                HealthUtility.AdjustSeverity(animal, TorannMagicDefOf.TM_RangerBondHD, .5f + ver.level);
                                comp.bondedPet = animal;
                                MoteMaker.MakeInteractionBubble(animal, pawn, InteractionDefOf.Nuzzle.interactionMote, InteractionDefOf.Nuzzle.GetSymbol());
                                TM_Action.TrainAnimalFull(animal, pawn);                           
                            }
                            else
                            {
                                Messages.Message("TM_FailedRangerBond".Translate(
                                animal.LabelShort,
                                pawn.LabelShort,
                                (Mathf.Clamp(Mathf.RoundToInt(((.7f + (.1f * pwr.level)) - animal.RaceProps.wildness) * 1000f), 0, 100 - (40 - (5 * pwr.level))))
                                ), MessageTypeDefOf.NeutralEvent);
                                if(animal.Faction == null && Rand.Chance(animal.RaceProps.manhunterOnTameFailChance))
                                {
                                    animal.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, true, false, null);
                                }
                            }
                        }
                        else
                        {
                            Messages.Message("TM_RangerNotExperienced".Translate(
                                animal.LabelShort,
                                pawn.LabelShort,
                                (animal.RaceProps.wildness * 100).ToString("F"),
                                (.7f + .1f*pwr.level) * 100
                            ), MessageTypeDefOf.NeutralEvent);
                        }
                    }
                    else
                    {
                        Messages.Message("TM_AnimalIncapableOfBond".Translate(
                            animal.LabelShort,
                            pawn.LabelShort
                            ), MessageTypeDefOf.NeutralEvent);
                    }
                }

            }
            this.PostCastShot(flag, out flag);
            return flag;
        }
    }
}
