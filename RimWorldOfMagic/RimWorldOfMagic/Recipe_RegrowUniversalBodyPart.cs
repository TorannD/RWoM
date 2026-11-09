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
            => ClassProgression.RegrowthSurgery.Fails(surgeon, patient, recipe, .08f);

        public void ApplyHediff(Pawn patient, BodyPartRecord part, Pawn billdoer)
        {
            patient.health.AddHediff(HediffDef.Named("TM_StandardRegrowth"), part, null);            
        }
    }
}