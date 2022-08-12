using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using AbilityUser;
using UnityEngine;

namespace TorannMagic.Enchantment
{
    public class Verb_OrbTraitInfuse : Verb_UseAbility  
    {

        bool validTarg;
        //Used for non-unique abilities that can be used with shieldbelt
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == this.caster)
            {
                return this.verbProps.targetParams.canTargetSelf;
            }
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    ShootLine shootLine;
                    validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
                }
                else
                {
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
            bool result = false;
            
            if (this.currentTarget != null && base.CasterPawn != null)
            {
                if(this.currentTarget.Thing != null && this.currentTarget.Thing is Pawn victim)
                {
                    if(victim.Faction != null && victim.RaceProps.Humanlike && victim.story != null && victim.story.traits != null && !TM_Calc.IsUndeadNotVamp(victim))
                    {
                        int traitsApplied = 0;
                        List<Apparel> apparel = this.CasterPawn.apparel.WornApparel;
                        List<Trait> orbTraits = new List<Trait>();
                        orbTraits.Clear();
                        CompEnchantedItem itemComp = null;
                        if (apparel != null)
                        {
                            for (int i = 0; i < apparel.Count; i++)
                            {
                                Apparel item = apparel[i];
                                if (item != null && item.def == TorannMagicDefOf.TM_Artifact_OrbOfSouls_Full)
                                {
                                    itemComp = item.GetComp<CompEnchantedItem>();
                                    if(itemComp != null)
                                    {
                                        orbTraits = itemComp.SoulOrbTraits;
                                    }
                                }
                            }
                        }
                        if (orbTraits.Count > 0)
                        {
                            for(int i = 0; i < orbTraits.Count; i++)
                            {
                                bool conflicting = false;
                                for(int j =0; j < victim.story.traits.allTraits.Count; j++)
                                {
                                    if(victim.story.traits.allTraits[j].def.ConflictsWith(orbTraits[i]))
                                    {
                                        conflicting = true;
                                    }
                                }

                                if(!conflicting)
                                {
                                    AddTrait(victim, orbTraits[i]);
                                    traitsApplied++;
                                }
                            }

                            if (traitsApplied > 0)
                            {
                                Effects(victim.Position);
                                result = true;
                            }
                            else
                            {
                                Messages.Message("TM_NoTraitsApplied".Translate(victim), MessageTypeDefOf.RejectInput);
                                result = false;
                            }
                        }
                        else
                        {
                            Log.Message("no traits found in orb - was this dev-mode generated?");
                            result = true; //destroy anyways
                        }                        
                    }
                    else
                    {
                        //invalid target
                        Messages.Message("TM_InvalidTarget".Translate(
                                this.CasterPawn.LabelShort,
                                this.verbProps.label
                            ), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    //invalid target
                    Messages.Message("TM_InvalidTarget".Translate(
                            this.CasterPawn.LabelShort,
                            this.verbProps.label
                        ), MessageTypeDefOf.RejectInput);
                }
            }
            else
            {
                Log.Warning("failed to TryCastShot");
            }
            this.burstShotsLeft = 0;
            //this.ability.TicksUntilCasting = (int)base.UseAbilityProps.SecondsToRecharge * 60;
            PostCastShot(result);
            return false;
        }

        private void AddTrait(Pawn pawn, Trait trait)
        {
            List<Trait> pawnTraits = pawn.story.traits.allTraits;
            if(pawnTraits.Count >= 7)
            {
                if(Rand.Chance(.75f))
                {
                    RemoveTrait(pawn, pawnTraits, pawnTraits.RandomElement().def);
                    pawn.story.traits.GainTrait(trait);
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_TraitInfusionHD, .5f);
                }
            }
            else
            {
                pawn.story.traits.GainTrait(trait);
                HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_TraitInfusionHD, .5f);
            }
        }

        private void RemoveTrait(Pawn pawn, List<Trait> allTraits, TraitDef removeTrait)
        {
            for (int i = 0; i < allTraits.Count; i++)
            {
                if (allTraits[i].def == removeTrait)
                {
                    allTraits.Remove(allTraits[i]);
                    i--;
                }
            }
        }

        public void Effects(IntVec3 position)
        {
            Vector3 rndPos = position.ToVector3Shifted();
            FleckMaker.ThrowHeatGlow(position, this.CasterPawn.Map, 1f);
            for (int i = 0; i < 3; i++)
            {
                rndPos.x += Rand.Range(-.5f, .5f);
                rndPos.z += Rand.Range(-.5f, .5f);
                rndPos.y += Rand.Range(.3f, 1.3f);
                FleckMaker.ThrowSmoke(rndPos, this.CasterPawn.Map, Rand.Range(.7f, 1.1f));
                FleckMaker.ThrowLightningGlow(position.ToVector3Shifted(), this.CasterPawn.Map, 1.4f);
            }
        }

        public void PostCastShot(bool inResult)
        {
            if(inResult)
            {
                List<Apparel> apparel = this.CasterPawn.apparel.WornApparel;
                if (apparel != null)
                {
                    for (int i = 0; i < apparel.Count; i++)
                    {
                        Apparel item = apparel[i];
                        if (item != null && item.def == TorannMagicDefOf.TM_Artifact_OrbOfSouls_Full)
                        {
                            item.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                    }
                }
                CompAbilityUser comp = this.CasterPawn.GetComp<CompAbilityUser>();
                if(comp != null)
                {
                    comp.RemoveApparelAbility(TorannMagicDefOf.TM_Artifact_TraitInfuse);
                }
            }
        }
    }
}
