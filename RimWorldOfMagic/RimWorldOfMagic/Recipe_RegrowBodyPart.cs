using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;

namespace TorannMagic
{
    public class Recipe_RegrowBodyPart : Recipe_InstallArtificialBodyPart
    {
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
                if (comp.spell_RegrowLimb)
                {
                    canRegrow = true;
                }
                if (comp.customClass != null && comp.MagicData.MagicPowersD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RegrowLimb).learned)
                {
                    canRegrow = true;
                }
                if (canRegrow)
                {
                    MagicPowerSkill eff = surgeon.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_RegrowLimb.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_RegrowLimb_eff");
                    if (comp.Mana.CurLevel < (.9f - ((eff.level * TorannMagicDefOf.TM_RegrowLimb.efficiencyReductionPercent) * .9f)))
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
                        comp.Mana.CurLevel -= ((.9f - ((eff.level * TorannMagicDefOf.TM_RegrowLimb.efficiencyReductionPercent) * .9f)) / comp.arcaneDmg);
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

            CompAbilityUserMagic comp = billdoer.GetCompAbilityUserMagic();
            switch (part.def.defName)
            {
                case "Foot":
                    patient.health.AddHediff(HediffDef.Named("TM_FootRegrowth"), part, null);
                    break;
                case "Leg":
                    patient.health.AddHediff(HediffDef.Named("TM_LegRegrowth"), part, null);
                    break;
                case "Shoulder":
                    patient.health.AddHediff(HediffDef.Named("TM_ArmRegrowth"), part, null);
                    break;
                case "Arm":
                    patient.health.AddHediff(HediffDef.Named("TM_ArmRegrowth"), part, null);
                    break;
                case "Hand":
                    patient.health.AddHediff(HediffDef.Named("TM_HandRegrowth"), part, null);
                    comp.Mana.CurLevel += (.4f);
                    break;
                case "Finger":
                    patient.health.AddHediff(HediffDef.Named("TM_FingerRegrowth"), part, null);
                    comp.Mana.CurLevel += (.7f);
                    break;
                case "Toe":
                    patient.health.AddHediff(HediffDef.Named("TM_ToeRegrowth"), part, null);
                    comp.Mana.CurLevel += (.7f);
                    break;
                case "Eye":
                    patient.health.AddHediff(HediffDef.Named("TM_EyeRegrowth"), part, null);
                    comp.Mana.CurLevel += (.3f);
                    break;
                case "Ear":
                    patient.health.AddHediff(HediffDef.Named("TM_EarRegrowth"), part, null);
                    comp.Mana.CurLevel += (.5f);
                    break;
                case "Nose":
                    patient.health.AddHediff(HediffDef.Named("TM_StandardRegrowth"), part, null);
                    comp.Mana.CurLevel += (.6f);
                    break;
                case "Jaw":
                    patient.health.AddHediff(HediffDef.Named("TM_JawRegrowth"), part, null);
                    comp.Mana.CurLevel += (.5f);
                    break;
                case "Ribcage":
                    patient.health.AddHediff(HediffDef.Named("TM_StandardRegrowth"), part, null);
                    comp.Mana.CurLevel += (.6f);
                    break;
                case "Heart":
                    patient.health.AddHediff(HediffDef.Named("TM_HeartRegrowth"), part, null);
                    break;
                case "Lung":
                    patient.health.AddHediff(HediffDef.Named("TM_LungRegrowth"), part, null);
                    break;
                case "Kidney":
                    patient.health.AddHediff(HediffDef.Named("TM_KidneyRegrowth"), part, null);
                    break;
                case "Liver":
                    patient.health.AddHediff(HediffDef.Named("TM_LiverRegrowth"), part, null);
                    break;
                case "Stomach":
                    patient.health.AddHediff(HediffDef.Named("TM_StomachRegrowth"), part, null);
                    comp.Mana.CurLevel += (.3f);
                    break;
                case "Spine":
                    patient.health.AddHediff(HediffDef.Named("TM_SpineRegrowth"), part, null);
                    break;
            }
        }
    }
}

//case "LeftHandRingFinger":
//    patient.health.AddHediff(HediffDef.Named("TM_FingerRegrowth"), part, null);
//    comp.Mana.CurLevel += (.7f);
//    break;
//case "LeftHandMiddleFinger":
//    patient.health.AddHediff(HediffDef.Named("TM_FingerRegrowth"), part, null);
//    comp.Mana.CurLevel += (.7f);
//    break;
//case "LeftHandIndexFinger":
//    patient.health.AddHediff(HediffDef.Named("TM_FingerRegrowth"), part, null);
//    comp.Mana.CurLevel += (.7f);
//    break;
//case "LeftHandThumb":
//    patient.health.AddHediff(HediffDef.Named("TM_FingerRegrowth"), part, null);
//    comp.Mana.CurLevel += (.7f);
//    break;

//case "LeftFootFourthToe":
//    patient.health.AddHediff(HediffDef.Named("TM_ToeRegrowth"), part, null);
//    comp.Mana.CurLevel += (.7f);
//    break;
//case "LeftFootMiddleToe":
//    patient.health.AddHediff(HediffDef.Named("TM_ToeRegrowth"), part, null);
//    comp.Mana.CurLevel += (.7f);
//    break;
//case "LeftFootSecondToe":
//    patient.health.AddHediff(HediffDef.Named("TM_ToeRegrowth"), part, null);
//    comp.Mana.CurLevel += (.7f);
//    break;
//case "LeftFootBigToe":
//    patient.health.AddHediff(HediffDef.Named("TM_ToeRegrowth"), part, null);
//    comp.Mana.CurLevel += (.7f);
//    break;

//case "RightFoot":
//                    patient.health.AddHediff(HediffDef.Named("TM_FootRegrowth"), part, null);
//                    break;
//                case "RightLeg":
//                    patient.health.AddHediff(HediffDef.Named("TM_LegRegrowth"), part, null);
//                    break;
//                case "RightShoulder":
//                    patient.health.AddHediff(HediffDef.Named("TM_ArmRegrowth"), part, null);
//                    break;
//                case "RightArm":
//                    patient.health.AddHediff(HediffDef.Named("TM_ArmRegrowth"), part, null);
//                    break;
//                case "RightHand":
//                    patient.health.AddHediff(HediffDef.Named("TM_HandRegrowth"), part, null);
//                    comp.Mana.CurLevel += (.4f);
//                    break;
//                case "RightHandPinky":
//                    patient.health.AddHediff(HediffDef.Named("TM_FingerRegrowth"), part, null);
//                    comp.Mana.CurLevel += (.7f);
//                    break;
//                case "RightHandRingFinger":
//                    patient.health.AddHediff(HediffDef.Named("TM_FingerRegrowth"), part, null);
//                    comp.Mana.CurLevel += (.7f);
//                    break;
//                case "RightHandMiddleFinger":
//                    patient.health.AddHediff(HediffDef.Named("TM_FingerRegrowth"), part, null);
//                    comp.Mana.CurLevel += (.7f);
//                    break;
//                case "RightHandIndexFinger":
//                    patient.health.AddHediff(HediffDef.Named("TM_FingerRegrowth"), part, null);
//                    comp.Mana.CurLevel += (.7f);
//                    break;
//                case "RightHandThumb":
//                    patient.health.AddHediff(HediffDef.Named("TM_FingerRegrowth"), part, null);
//                    comp.Mana.CurLevel += (.7f);
//                    break;
//                case "RightFootLittleToe":
//                    patient.health.AddHediff(HediffDef.Named("TM_ToeRegrowth"), part, null);
//                    comp.Mana.CurLevel += (.7f);
//                    break;
//                case "RightFootFourthToe":
//                    patient.health.AddHediff(HediffDef.Named("TM_ToeRegrowth"), part, null);
//                    comp.Mana.CurLevel += (.7f);
//                    break;
//                case "RightFootMiddleToe":
//                    patient.health.AddHediff(HediffDef.Named("TM_ToeRegrowth"), part, null);
//                    comp.Mana.CurLevel += (.7f);
//                    break;
//                case "RightFootSecondToe":
//                    patient.health.AddHediff(HediffDef.Named("TM_ToeRegrowth"), part, null);
//                    comp.Mana.CurLevel += (.7f);
//                    break;
//                case "RightFootBigToe":
//                    patient.health.AddHediff(HediffDef.Named("TM_ToeRegrowth"), part, null);
//                    comp.Mana.CurLevel += (.7f);
//                    break;
//                case "RightEye":
//                    patient.health.AddHediff(HediffDef.Named("TM_EyeRegrowth"), part, null);
//                    comp.Mana.CurLevel += (.3f);
//                    break;
//                case "RightEar":
//                    patient.health.AddHediff(HediffDef.Named("TM_EarRegrowth"), part, null);
//                    comp.Mana.CurLevel += (.5f);
//                    break;