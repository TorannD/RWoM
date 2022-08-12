using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using AbilityUser;
using UnityEngine;

namespace TorannMagic.Enchantment
{
    public class Verb_OrbTraitThief : Verb_UseAbility  
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
                    if(victim.Faction != null && victim.RaceProps.Humanlike && victim.story != null && victim.story.traits != null && victim.story.traits.allTraits.Count > 0 && !TM_Calc.IsUndead(victim) && !TM_Calc.IsSpirit(victim))
                    {
                        if (Rand.Chance(TM_Calc.GetSpellSuccessChance(this.CasterPawn, victim, true)))
                        {
                            Thing orb = ThingMaker.MakeThing(TorannMagicDefOf.TM_Artifact_OrbOfSouls_Full, null);
                            GenPlace.TryPlaceThing(orb, this.CasterPawn.Position, this.CasterPawn.Map, ThingPlaceMode.Near);
                            CompEnchantedItem orbComp = orb.TryGetComp<CompEnchantedItem>();
                            if (victim.Faction == this.CasterPawn.Faction)
                            {
                                if (orbComp != null)
                                {
                                    orbComp.SoulOrbTraits = new List<Trait>();
                                    orbComp.SoulOrbTraits.Clear();
                                    List<Trait> allTraits = victim.story.traits.allTraits;
                                    int iterations = Mathf.Max(allTraits.Count, 1);
                                    for (int i = 0; i < iterations; i++)
                                    {
                                        Trait transferTrait = allTraits.RandomElement();
                                        if (transferTrait.def == TorannMagicDefOf.TM_Possessed)
                                        {
                                            TM_Action.RemovePossession(victim, victim.Position);
                                        }
                                        else
                                        {
                                            orbComp.SoulOrbTraits.AddDistinct(transferTrait);
                                            RemoveTrait(victim, allTraits, transferTrait.def);
                                        }
                                        
                                        result = true;
                                    }
                                    if(Rand.Chance(.6f))
                                    {
                                        victim.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                                    }
                                    else if(Rand.Chance(.2f))
                                    {
                                        victim.Kill(null, null);
                                    }
                                }
                                else
                                {
                                    Log.Message("no comp found for orb of souls");
                                }
                            }
                            else
                            {
                                if (orbComp != null)
                                {
                                    orbComp.SoulOrbTraits = new List<Trait>();
                                    orbComp.SoulOrbTraits.Clear();
                                    List<Trait> allTraits = victim.story.traits.allTraits;
                                    int iterations = Mathf.Max(allTraits.Count, 1);
                                    for (int i = 0; i < iterations; i++)
                                    {
                                        Trait transferTrait = allTraits.RandomElement();
                                        orbComp.SoulOrbTraits.AddDistinct(transferTrait);
                                        RemoveTrait(victim, allTraits, transferTrait.def);
                                        Effects(victim.Position);
                                        result = true;
                                    }
                                    if (result)
                                    {
                                        if (Rand.Chance(.15f))
                                        {
                                            victim.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                                            int relationChange = Rand.RangeInclusive(-30, -20);
                                            this.CasterPawn.Faction.TryAffectGoodwillWith(victim.Faction, relationChange, true, true, TorannMagicDefOf.TM_OffensiveMagic, null);
                                        }
                                        else
                                        {
                                            victim.Kill(null, null);
                                            int relationChange = Rand.RangeInclusive(-50, -30);
                                            this.CasterPawn.Faction.TryAffectGoodwillWith(victim.Faction, relationChange, true, true, TorannMagicDefOf.TM_OffensiveMagic, null);
                                        }
                                    }
                                }
                                else
                                {
                                    Log.Message("no comp found for orb of souls");
                                }
                            }
                        }
                        else
                        {
                            MoteMaker.ThrowText(victim.DrawPos, victim.Map, "TM_ResistedSpell".Translate(), -1);
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
                        if (item != null && item.def == TorannMagicDefOf.TM_Artifact_OrbOfSouls)
                        {
                            item.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                    }
                }
                CompAbilityUser comp = this.CasterPawn.GetComp<CompAbilityUser>();
                if(comp != null)
                {
                    comp.RemoveApparelAbility(TorannMagicDefOf.TM_Artifact_TraitThief);
                }
            }
        }
    }
}
