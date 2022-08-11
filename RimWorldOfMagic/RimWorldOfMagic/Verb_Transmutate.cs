using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace TorannMagic
{
    public class Verb_Transmutate : Verb_UseAbility
    {

        private int verVal;
        private int pwrVal;

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
            

            bool flagRawResource = false;
            bool flagStuffItem = false;
            bool flagNoStuffItem = false;
            bool flagNutrition = false;
            bool flagCorpse = false;

            
            Thing transmutateThing = TM_Calc.GetTransmutableThingFromCell(this.currentTarget.Cell, this.CasterPawn, out flagRawResource, out flagStuffItem, out flagNoStuffItem, out flagNutrition, out flagCorpse, true);

            //List<Thing> thingList = this.currentTarget.Cell.GetThingList(caster.Map);

            //for (int i = 0; i < thingList.Count; i++)
            //{
            //    if (thingList[i] != null && !(thingList[i] is Pawn) && !(thingList[i] is Building))
            //    {
            //        //if (thingList[i].def.thingCategories != null && thingList[i].def.thingCategories.Count > 0 && (thingList[i].def.thingCategories.Contains(ThingCategoryDefOf.ResourcesRaw) || thingList[i].def.thingCategories.Contains(ThingCategoryDefOf.StoneBlocks) || thingList[i].def.defName == "RawMagicyte"))                    
            //        if (thingList[i].def.MadeFromStuff && verVal >= 3)
            //        {
            //            //Log.Message("stuff item");
            //            flagStuffItem = true;
            //            transmutateThing = thingList[i];
            //            break;
            //        }
            //        if(!thingList[i].def.MadeFromStuff && thingList[i].TryGetComp<CompQuality>() != null && verVal >= 3)
            //        {
            //            //Log.Message("non stuff item");
            //            flagNoStuffItem = true;
            //            transmutateThing = thingList[i];
            //            break;
            //        }
            //        if ((thingList[i].def.statBases != null && thingList[i].GetStatValue(StatDefOf.Nutrition) > 0) && !(thingList[i] is Corpse) && verVal >= 1)
            //        {
            //            //Log.Message("food item");
            //            flagNutrition = true;
            //            transmutateThing = thingList[i];
            //            break;
            //        }
            //        if(thingList[i] is Corpse && verVal >= 2)
            //        {
            //            //Log.Message("corpse");
            //            flagCorpse = true;
            //            transmutateThing = thingList[i];
            //            break;
            //        }
            //        if (thingList[i].def != null && !thingList[i].def.IsIngestible && ((thingList[i].def.stuffProps != null && thingList[i].def.stuffProps.categories != null && thingList[i].def.stuffProps.categories.Count > 0) || thingList[i].def.defName == "RawMagicyte" || thingList[i].def.IsWithinCategory(ThingCategoryDefOf.ResourcesRaw) || thingList[i].def.IsWithinCategory(ThingCategoryDefOf.Leathers)))
            //        {
            //            //Log.Message("resource");
            //            flagRawResource = true;
            //            transmutateThing = thingList[i];
            //            break;
            //        }
            //    }
            //}

            if(transmutateThing != null)
            {
                TM_Action.DoTransmutate(this.CasterPawn, transmutateThing, flagNoStuffItem, flagRawResource, flagStuffItem, flagNutrition, flagCorpse);
            }
            else
            {
                Messages.Message("TM_NoThingToTransmutate".Translate(
                    this.CasterPawn.LabelShort
                ), MessageTypeDefOf.RejectInput);
            }

            this.burstShotsLeft = 0;
            return false;
        }

        
    }
}