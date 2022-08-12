using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;

namespace TorannMagic
{
    public class Recipe_RegrowUniversalBodyPart : Recipe_InstallArtificialBodyPart
    {

        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            IEnumerable<BodyPartRecord> regrowthParts = from parts in pawn.def.race.body.AllParts
                                                        where (!TorannMagicDefOf.Regrowth.appliedOnFixedBodyParts.Contains(parts.def))
                                                        select parts;
            if (pawn.def != ThingDefOf.Human)
            {
                regrowthParts = pawn.def.race.body.AllParts;
            }

            return TM_MedicalRecipesUtility.GetAdjustedPartsToApplyOn(regrowthParts.ToList(), pawn, delegate (BodyPartRecord record)
            {
                if (!pawn.health.hediffSet.hediffs.Any((Hediff x) => x.Part == record))
                {
                    return false;
                }
                if (!pawn.health.hediffSet.PartIsMissing(record))
                {
                    return false;
                }
                if (record.parent != null && !pawn.health.hediffSet.GetNotMissingParts().Contains(record.parent))
                {
                    return false;
                }
                if (pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(record) && !pawn.health.hediffSet.HasDirectlyAddedPartFor(record))
                {
                    return false;
                }
                if (record.IsCorePart)
                {
                    return false;
                }
                return true;
            }).Distinct();
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null)
            {
                if (base.CheckSurgeryFail(billDoer, pawn, ingredients, part, bill) || this.CheckDruidSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, new object[]
                {
                    billDoer,
                    pawn
                });
                TM_MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts(pawn, part, billDoer.Position, billDoer.Map);
            }
            else if (pawn.Map != null)
            {
                TM_MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts(pawn, part, pawn.Position, pawn.Map);
            }
            else
            {
                pawn.health.RestorePart(part, null, true);
            }
            ApplyHediff(pawn, part, billDoer);
            //pawn.health.AddHediff(this.recipe.addsHediff, part, null);
        }

        public bool CheckDruidSurgeryFail(Pawn surgeon, Pawn patient, List<Thing> ingredients, BodyPartRecord part, Bill bill)
        {

            CompAbilityUserMagic comp = surgeon.GetCompAbilityUserMagic();

            string reason;
            if (comp.IsMagicUser)
            {
                bool canRegrow = false;
                if(comp.spell_RegrowLimb)
                {
                    canRegrow = true;
                }
                if(comp.customClass != null && comp.MagicData.MagicPowersD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RegrowLimb).learned)
                {
                    canRegrow = true;
                }
                if (canRegrow)
                {
                    MagicPowerSkill eff = surgeon.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_RegrowLimb.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_RegrowLimb_eff");
                    if (comp.Mana.CurLevel < (.9f - ((eff.level * .08f) * .9f)))
                    {
                        comp.Mana.CurLevel = comp.Mana.CurLevel / 2;

                        //TM_MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts(patient, part, patient.Position, patient.Map);
                        reason = "TM_InsufficientManaForSurgery".Translate();
                        Find.LetterStack.ReceiveLetter("LetterLabelRegrowthSurgeryFail".Translate(), "LetterRegrowthSurgeryFail".Translate(
                            surgeon.LabelCap,
                            this.recipe.defName,
                            patient.Label,
                            reason,
                            surgeon.LabelShort
                        ), LetterDefOf.NegativeEvent, null);
                        return true;
                    }
                    else // regrowth surgery success
                    {
                        comp.Mana.CurLevel -= ((.9f - ((eff.level * .08f) * .9f)) / comp.arcaneDmg);
                        int num = Mathf.RoundToInt(Rand.Range(160, 280) * comp.xpGain);
                        comp.MagicUserXP += num;
                        MoteMaker.ThrowText(surgeon.DrawPos, surgeon.MapHeld, "XP +" + num, -1f);
                        TM_MoteMaker.ThrowRegenMote(patient.Position.ToVector3(), patient.Map, 1.2f);
                        TM_MoteMaker.ThrowRegenMote(patient.Position.ToVector3(), patient.Map, .8f);
                        TM_MoteMaker.ThrowRegenMote(patient.Position.ToVector3(), patient.Map, .8f);
                        return false;
                    }
                }
                else
                {
                    comp.Mana.CurLevel = comp.Mana.CurLevel / 2;
                    //TM_MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts(patient, part, patient.Position, patient.Map);
                    reason = "TM_NoRegrowthSpell".Translate();
                    Find.LetterStack.ReceiveLetter("LetterLabelRegrowthSurgeryFail".Translate(), "LetterRegrowthSurgeryFail".Translate(
                        surgeon.LabelCap,
                        this.recipe.defName,
                        patient.Label,
                        reason,
                        surgeon.LabelShort
                    ), LetterDefOf.NegativeEvent, null);
                    return true;
                }
            }
            reason = "TM_NotMagicUser".Translate();
            Find.LetterStack.ReceiveLetter("LetterLabelRegrowthSurgeryFail".Translate(), "LetterRegrowthSurgeryFail".Translate(
                        surgeon.LabelCap,
                        this.recipe.defName,
                        patient.Label,
                        reason,
                        surgeon.LabelShort
                ), LetterDefOf.NegativeEvent, null);
            return true;

        }

        public void ApplyHediff(Pawn patient, BodyPartRecord part, Pawn billdoer)
        {
            patient.health.AddHediff(HediffDef.Named("TM_StandardRegrowth"), part, null);            
        }
    }
}