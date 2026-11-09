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

        internal const string Order = "TM_SpiritCastAnimalFriend";
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            return ValidTarget(CasterPawn,targ.Pawn,root,verbProps.range);
        }

        internal static bool ValidTarget(Pawn body,Pawn animal,IntVec3 origin,float range) =>
            body?.Spawned==true && animal?.Spawned==true && body!=animal && !animal.Dead && animal.Map==body.Map &&
            animal.RaceProps.Animal && animal.RaceProps.IsFlesh && !animal.Position.Fogged(body.Map) &&
            (origin-animal.Position).LengthHorizontal < range;

        protected override bool TryCastShot()
        {
            bool attempted=TryBond(CasterPawn,CasterPawn,currentTarget.Pawn,verbProps.range);
            PostCastShot(attempted,out bool result);
            return result;
        }

        internal static bool TryBond(Pawn pawn,Pawn owner,Pawn animal,float range)
        {
            if (pawn?.Spawned!=true || !ValidTarget(pawn,animal,pawn.Position,range)) return false;
            var comp=owner?.GetCompAbilityUserMight();
            var skills=comp?.MightData.MightPowerSkill_AnimalFriend;
            var pwr=skills?.FirstOrDefault(s=>s.label=="TM_AnimalFriend_pwr");
            var ver=skills?.FirstOrDefault(s=>s.label=="TM_AnimalFriend_ver");
            if (pwr==null || ver==null) return false;
            bool attempted=false;
            Pawn oldbond = comp.bondedPet;
            if (animal == comp.bondedPet)
            {
                attempted=true;
                Hediff animalBondHD = oldbond.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_RangerBondHD"));
                if (animalBondHD != null && Rand.Chance(((1f + animalBondHD.ageTicks / 6000000) - oldbond.GetStatValue(StatDefOf.Wildness))))
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
                comp.bondedPet=null;
                ClassProgression.RangerCareer.BondChanged(owner);
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
                    if ((animal.GetStatValue(StatDefOf.Wildness) <= .7f) || (animal.GetStatValue(StatDefOf.Wildness) <= .8f && pwr.level == 1) || (animal.GetStatValue(StatDefOf.Wildness) <= .9f && pwr.level == 2) || pwr.level == 3)
                    {
                        attempted=true;
                        if (Rand.Chance(.6f + (.05f *pwr.level)) && Rand.Chance(((.7f + (.1f *pwr.level)) - animal.GetStatValue(StatDefOf.Wildness)) * 10))
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
                                            oldbond.holdingOwner?.Remove(oldbond);
                                        }
                                        oldbond.Destroy();
                                    }
                                }
                            }
                            animal.SetFaction(pawn.Faction);
                            comp.bondedPet = animal;
                            HediffComp_RangerBond.ApplyPetBond(owner,animal,.5f + ver.level);
                            ClassProgression.RangerCareer.BondChanged(owner);
                            MoteMaker.MakeInteractionBubble(animal, pawn, InteractionDefOf.Nuzzle.interactionMote, InteractionDefOf.Nuzzle.GetSymbol());
                            TM_Action.TrainAnimalFull(animal, pawn);
                        }
                        else
                        {
                            Messages.Message("TM_FailedRangerBond".Translate(
                            animal.LabelShort,
                            pawn.LabelShort,
                            (Mathf.Clamp(Mathf.RoundToInt(((.7f + (.1f * pwr.level)) - animal.GetStatValue(StatDefOf.Wildness)) * 1000f), 0, 100 - (40 - (5 * pwr.level))))
                            ), MessageTypeDefOf.NeutralEvent);
                            if(animal.Faction == null && Rand.Chance(animal.RaceProps.manhunterOnTameFailChance))
                            {
                                animal.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, true, false, false, null);
                            }
                        }
                    }
                    else
                    {
                        Messages.Message("TM_RangerNotExperienced".Translate(
                            animal.LabelShort,
                            pawn.LabelShort,
                            (animal.GetStatValue(StatDefOf.Wildness) * 100).ToString("F"),
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
            return attempted;
        }
    }
}
