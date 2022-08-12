using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public class IncidentWorker_ArcaneSickness : IncidentWorker
    {

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            int num = this.PotentialVictimCandidates(parms.target).Count<Pawn>();
            IntRange intRange = new IntRange(Mathf.RoundToInt((float)num * this.def.diseaseVictimFractionRange.min), Mathf.RoundToInt((float)num * this.def.diseaseVictimFractionRange.max));
            int num2 = intRange.RandomInRange;
            num2 = Mathf.Clamp(num2, 1, this.def.diseaseMaxVictims);
            for (int i = 0; i < num2; i++)
            {
                if (!this.PotentialVictims(parms.target).TryRandomElementByWeight((Pawn x) => x.health.immunity.DiseaseContractChanceFactor(this.def.diseaseIncident, null), out Pawn pawn))
                {
                    break;
                }
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                if (comp.IsMagicUser)
                {
                    HediffGiverUtility.TryApply(pawn, this.def.diseaseIncident, this.def.diseasePartsToAffect, false, 1, null);
                }                
            }
            return true;
        }

        private IEnumerable<Pawn> PotentialVictimCandidates(IIncidentTarget target)
        {
            if (target is Map map)
            {
                return map.mapPawns.FreeColonistsAndPrisoners;
            }
            return from x in ((Caravan)target).PawnsListForReading
                    where x.IsFreeColonist || x.IsPrisonerOfColony
                    select x;
        }

        private IEnumerable<Pawn> PotentialVictims(IIncidentTarget target)
        {
            return this.PotentialVictimCandidates(target).Where(delegate (Pawn p)
            {
                if (p.ParentHolder is Building_CryptosleepCasket)
                {
                    return false;
                }
                CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();
                if (comp != null && !comp.IsMagicUser)
                {
                    return false;
                }
                if (!this.def.diseasePartsToAffect.NullOrEmpty<BodyPartDef>())
                {
                    bool flag = false;
                    for (int i = 0; i < this.def.diseasePartsToAffect.Count; i++)
                    {
                        if (IncidentWorker_ArcaneSickness.CanAddHediffToAnyPartOfDef(p, this.def.diseaseIncident, this.def.diseasePartsToAffect[i]))
                        {
                            flag = true;
                            break;                                                              
                        }
                        return false;
                    }
                    if (!flag)
                    {
                        return false;
                    }
                }
                return p.health.immunity.DiseaseContractChanceFactor(this.def.diseaseIncident, null) > 0f;
            });
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return this.PotentialVictims(parms.target).Any<Pawn>();
        }


        private static bool CanAddHediffToAnyPartOfDef(Pawn pawn, HediffDef hediffDef, BodyPartDef partDef)
        {
            List<BodyPartRecord> allParts = pawn.def.race.body.AllParts;
            for (int i = 0; i < allParts.Count; i++)
            {
                BodyPartRecord bodyPartRecord = allParts[i];
                if (bodyPartRecord.def == partDef && !pawn.health.hediffSet.PartIsMissing(bodyPartRecord) && !pawn.health.hediffSet.HasHediff(hediffDef, bodyPartRecord, false))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
