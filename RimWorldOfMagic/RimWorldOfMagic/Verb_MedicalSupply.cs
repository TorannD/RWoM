using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using RimWorld;
using System.Collections.Generic;
using System;

namespace TorannMagic
{
    public class Verb_MedicalSupply : Verb_UseAbility
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
            bool result = false;

            Pawn pawn = this.CasterPawn;
            Map map = this.CasterPawn.Map;
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            //if (pawn != null && !pawn.Downed)

            List<BodyPartRecord> validParts = new List<BodyPartRecord>();
            Thing medicalThing = null;
            if (GetMedicalSupplyFromCell(this.currentTarget.Cell, this.CasterPawn, out medicalThing, true))
            {
                float potency = medicalThing.GetStatValue(StatDefOf.MedicalPotency);
                float supplyGain = 20f * potency * potency;
                if (!medicalThing.DestroyedOrNull() && medicalThing.stackCount <= 0)
                {
                    medicalThing.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    medicalThing.SplitOff(1).Destroy(DestroyMode.Vanish);
                }

                HealthUtility.AdjustSeverity(this.CasterPawn, HediffDef.Named("TM_MedicalSupplyHD"), supplyGain);
            }
            else
            {
                //todo: notify when invalid target
                //Messages.Message("TM_NoMedicalSupply".Translate(this.CasterPawn.LabelShort), MessageTypeDefOf.RejectInput, false);
                Log.Warning("failed to TryCastShot");
            }
            this.burstShotsLeft = 0;
            return result;
        }

        public static bool GetMedicalSupplyFromCell(IntVec3 cell, Pawn pawn, out Thing medicalSupply, bool manualCast = false)
        {
            List<Thing> thingList = cell.GetThingList(pawn.Map);
            medicalSupply = null;
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i] != null && !(thingList[i] is Pawn) && !(thingList[i] is Building) && (manualCast || !thingList[i].IsForbidden(pawn)))
                {
                    if (thingList[i].def.statBases != null && thingList[i].GetStatValue(StatDefOf.MedicalPotency) > 0.25f) // random non-medical itens have a medical potency of 0.2
                    {
                        medicalSupply = thingList[i];
                        Log.Message("medicine item: " + medicalSupply.LabelShort + " with potency " + medicalSupply.GetStatValue(StatDefOf.MedicalPotency));
                        break;
                    }
                }
            }
            return medicalSupply != null;
        }
    }
}
