using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using AbilityUser;
using UnityEngine;

namespace TorannMagic.Enchantment
{
    public class Verb_OrbTraitor : Verb_UseAbility  
    {

        bool validTarg;
        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt)
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
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
            bool result = false;
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
            if (this.currentTarget != null && base.CasterPawn != null && comp != null)
            {
                if(this.currentTarget.Thing != null && this.currentTarget.Thing is Pawn traitor)
                {
                    if(traitor.Faction != null && traitor.Faction != this.CasterPawn.Faction && traitor.RaceProps.Humanlike)
                    {
                        if (Rand.Chance(TM_Calc.GetSpellSuccessChance(this.CasterPawn, traitor, true)))
                        {
                            string letterLabel = "LetterLabelMessageRecruitSuccess".Translate();
                            int relationChange = Rand.RangeInclusive(-50, -20);
                            if (traitor.Faction.leader != null)
                            {
                                Find.LetterStack.ReceiveLetter(letterLabel, "TM_TraitorOrbRecruit".Translate(this.CasterPawn, traitor.LabelShort, traitor.Faction.Name, traitor.Faction.leader.LabelShort, traitor.Faction.leader.gender.GetObjective().ToString(), relationChange), LetterDefOf.PositiveEvent);
                            }
                            this.CasterPawn.Faction.TryAffectGoodwillWith(traitor.Faction, relationChange, true, true, TorannMagicDefOf.TM_OffensiveMagic, null);
                            traitor.SetFaction(caster.Faction, this.CasterPawn);
                            HealthUtility.AdjustSeverity(traitor, HediffDefOf.PsychicShock, 1);
                            Effects(traitor.Position);
                            result = true;
                        }
                        else
                        {
                            MoteMaker.ThrowText(traitor.DrawPos, traitor.Map, "TM_ResistedSpell".Translate(), -1);
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
                        if (item != null && item.def == TorannMagicDefOf.TM_Artifact_OrbOfConviction)
                        {
                            item.SplitOff(1).Destroy(DestroyMode.Vanish);
                        }
                    }
                }
                CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
                if(comp != null)
                {
                    comp.RemovePawnAbility(TorannMagicDefOf.TM_Artifact_Conviction);
                }
            }
        }
    }
}
