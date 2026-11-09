using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace TorannMagic
{
    public class Recipe_RuneCarving : Recipe_Surgery
    {
		public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
		{
            if (!TM_Calc.HasRuneCarverOnMap(pawn.HostFaction ?? pawn.Faction, pawn.Map, true)) yield break;
            var existing = pawn.health.hediffSet.hediffs.Where(h => h.Part != null &&
                (h.def == TorannMagicDefOf.TM_RuneCarvedPartHD || h.def == TorannMagicDefOf.TM_ArcaneTatooPartHD))
                .Select(h => h.Part).ToHashSet();
            foreach (var part in pawn.health.hediffSet.GetNotMissingParts())
            {
                if (part.depth != BodyPartDepth.Outside || existing.Contains(part)) continue;
                if (part.IsCorePart || (part.coverageAbsWithChildren > part.coverageAbs && part.parent != null &&
                    (part.def.canSuggestAmputation || part.def.forceAlwaysRemovable))) yield return part;
            }
		}

		public override bool IsViolationOnPawn(Pawn pawn, BodyPartRecord part, Faction billDoerFaction)
		{
			if ((pawn.Faction == billDoerFaction || pawn.Faction == null) && !pawn.IsQuestLodger())
			{
				return false;
			}
			if (HealthUtility.PartRemovalIntent(pawn, part) == BodyPartRemovalIntent.Harvest)
			{
				return true;
			}
			return false;
		}

		public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{
			if (billDoer == null || bill?.recipe == null || part == null || pawn?.health?.hediffSet == null) return;
            var trial = ClassProgression.GolemancerTrials.CaptureRune(this, billDoer, pawn, part, bill);
            if (CheckRuneCarvingFail(billDoer, pawn, ingredients, part, bill, trial)) return;
            TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
			RunePart(pawn, billDoer, part);
			ClassProgression.GolemancerTrials.CompleteRune(trial);
		}

		public bool CheckRuneCarvingFail(Pawn surgeon, Pawn patient, List<Thing> ingredients, BodyPartRecord part, Bill bill)
		{
            return CheckRuneCarvingFail(surgeon, patient, ingredients, part, bill, null);
        }

        private bool CheckRuneCarvingFail(Pawn surgeon, Pawn patient, List<Thing> ingredients, BodyPartRecord part, Bill bill, ClassProgression.GolemancerTrials.RuneSnapshot trial)
        {
			var owner = ClassProgression.GolemancerLifecycle.RuneOwner(surgeon);
			var comp = owner ?? surgeon?.GetCompAbilityUserMagic();
			if (bill.recipe.surgerySuccessChanceFactor >= 1f)
			{
				return false;
			}
						
			string reason;
            if (comp?.Mana != null)
            {
                bool canRuneCarve = owner != null;
                if(surgeon.skills.GetSkill(SkillDefOf.Artistic).TotallyDisabled)
                {
                    canRuneCarve = false;
                }
                if(surgeon.skills.GetSkill(SkillDefOf.Crafting).TotallyDisabled)
                {
                    canRuneCarve = false;
                }
				if (canRuneCarve)
				{
                    float manaNeeded = ClassProgression.GolemancerLifecycle.RuneCost(comp);
                    if (comp.Mana.CurLevel < manaNeeded)
					{
						comp.Mana.CurLevel = comp.Mana.CurLevel / 2;

						//TM_MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts(patient, part, patient.Position, patient.Map);
						reason = "TM_InsufficientManaForCarving".Translate(
							surgeon.LabelShort,
							(manaNeeded * 100f).ToString("#.#")
						);
						Find.LetterStack.ReceiveLetter("LetterLabelRuneCarvingFail".Translate(), "LetterRuneCarvingFail".Translate(
							surgeon.LabelCap,
							this.recipe.defName,
							patient.Label,
							reason,
							surgeon.LabelShort
						), LetterDefOf.NegativeEvent, null, null);
						return true;
					}
					else // rune carving success chance is calculated by .7f * ([crafting] .75 + .025 * level) * ([artistic] .8 + .03 * level) - ([skill power] level * .05) + ([skill versatility] level * .1)
                    // a pawn with 12 crafting, 6 art, 1 skill power, and 2 skill efficiency will have a 87.03% chance of success
					{						
						float runeChance = surgeon.GetStatValue(TorannMagicDefOf.TM_RuneCarvingEfficiency);
						float num = bill.recipe.surgerySuccessChanceFactor * runeChance;
						int pwrVal = comp.MagicData.MagicPowerSkill_RuneCarving.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_RuneCarving_pwr").level;
						int verVal = comp.MagicData.MagicPowerSkill_RuneCarving.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_RuneCarving_ver").level;
						float successChance = num + (pwrVal * -.05f) + (verVal * .1f);
						float trialManaBefore = comp.Mana.CurLevel;
                        comp.Mana.CurLevel -= manaNeeded;
                        ClassProgression.GolemancerTrials.RecordRunePayment(trial, trialManaBefore, comp.Mana.CurLevel, manaNeeded);
						int xpGain = Mathf.RoundToInt(TorannMagicDefOf.TM_RuneCarving.manaCost * 180 * comp.xpGain);
						comp.MagicUserXP += xpGain;
						MoteMaker.ThrowText(surgeon.DrawPos, surgeon.MapHeld, "XP +" + xpGain, -1f);
						if (Rand.Chance(successChance))
						{
							//successful
							FleckMaker.ThrowLightningGlow(patient.DrawPos, patient.Map, 1.5f);
							Effecter stoneskinEffecter = TorannMagicDefOf.TM_RuneCarving_EffecterED.Spawn();
							stoneskinEffecter.def.offsetTowardsTarget = FloatRange.Zero;
							stoneskinEffecter.Trigger(new TargetInfo(patient.Position, patient.Map, false), new TargetInfo(patient.Position, patient.Map, false));
							stoneskinEffecter.Cleanup();
							return false;
						}
						else
                        {
							reason = "TM_RuneCarvingFailed".Translate();
							Find.LetterStack.ReceiveLetter("LetterLabelRuneCarvingFail".Translate(), "TM_RuneCarvingFailed".Translate(
								surgeon.LabelShort,
								patient.LabelShort,
								part.Label,
								(successChance * 100f).ToString("#.#")
							), LetterDefOf.NegativeEvent, null, null);
							return true;
                        }
					}
				}
				else
				{
					comp.Mana.CurLevel = comp.Mana.CurLevel / 2;
					reason = "TM_NoRuneCarvingSpell".Translate();
					Find.LetterStack.ReceiveLetter("LetterLabelRuneCarvingFail".Translate(), "LetterRuneCarvingFail".Translate(
						surgeon.LabelCap,
						this.recipe.label,
						patient.Label,
						reason,
						surgeon.LabelShort,
						TorannMagicDefOf.TM_RuneCarving.manaCost * 100f
					), LetterDefOf.NegativeEvent, null, null);
					return true;
				}
			}
			reason = "TM_NotMagicUser".Translate();
			Find.LetterStack.ReceiveLetter("LetterLabelRuneCarvingFail".Translate(), "LetterRuneCarvingFail".Translate(
						surgeon.LabelCap,
						this.recipe.defName,
						patient.Label,
						reason,
						surgeon.LabelShort,
						TorannMagicDefOf.TM_RuneCarving.manaCost * 100f
				), LetterDefOf.NegativeEvent, null, null);
			return true;

		}

		public virtual void RunePart(Pawn pawn, Pawn carver, BodyPartRecord part)
		{
			var magic = ClassProgression.GolemancerLifecycle.RuneOwner(carver) ?? carver?.GetCompAbilityUserMagic();
            if (magic == null) return;
            int pwrVal = magic.MagicData.MagicPowerSkill_RuneCarving.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_RuneCarving_pwr").level;
            Hediff hd = null;
            if (part.IsCorePart)
            {
                hd = HediffMaker.MakeHediff(TorannMagicDefOf.TM_ArcaneTatooPartHD, pawn, part);
                hd.Severity = .5f + pwrVal;
                if(ModsConfig.IdeologyActive)
                {
                    TattooDef tat = null;                    
                    
                    if (pawn.gender == Gender.Male)
                    {
                        IEnumerable<TattooDef> tatoos = from cp in DefDatabase<TattooDef>.AllDefs
                                                        where (cp.styleGender == StyleGender.MaleUsually || cp.styleGender == StyleGender.Male) && cp.tattooType == TattooType.Body
                                                        select cp;
                        if(tatoos != null && tatoos.Count() > 0)
                        {
                            tat = tatoos.RandomElement();
                        }
                    }
                    else if(pawn.gender == Gender.Female)
                    {
                        IEnumerable<TattooDef> tatoos = from cp in DefDatabase<TattooDef>.AllDefs
                                                        where (cp.styleGender == StyleGender.Female || cp.styleGender == StyleGender.FemaleUsually) && cp.tattooType == TattooType.Body
                                                        select cp;
                        if (tatoos != null && tatoos.Count() > 0)
                        {
                            tat = tatoos.RandomElement();
                        }
                    }
                    if((pawn.gender != Gender.Female && pawn.gender != Gender.Male) || tat == null)
                    {
                        IEnumerable<TattooDef> tatoos = from cp in DefDatabase<TattooDef>.AllDefs
                                                        where cp.tattooType == TattooType.Body
                                                        select cp;
                        if (tatoos != null && tatoos.Count() > 0)
                        {
                            tat = tatoos.RandomElement();
                        }
                    }
                                       
                    pawn.style.BodyTattoo = tat;
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                    //pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
                    PortraitsCache.SetDirty(pawn);
                }
            }
            else
            {
                hd = HediffMaker.MakeHediff(TorannMagicDefOf.TM_RuneCarvedPartHD, pawn, part);
                hd.Severity = .5f + pwrVal;
            }
			pawn.health.AddHediff(hd, part, null, null);
		}

		public virtual void ApplyThoughts(Pawn pawn, Pawn billDoer)
		{
			//if (pawn.Dead)
			//{
			//	ThoughtUtility.GiveThoughtsForPawnExecuted(pawn, billDoer, PawnExecutionKind.OrganHarvesting);
			//}
			//else
			//{
			//	ThoughtUtility.GiveThoughtsForPawnOrganHarvested(pawn, billDoer);
			//}
		}

		public override string GetLabelWhenUsedOn(Pawn pawn, BodyPartRecord part)
		{
            if(part.IsCorePart)
            {
                return "TM_ArcaneTatoo".Translate();
            }
			return "TM_RuneCarving".Translate();
		}
	}
}
