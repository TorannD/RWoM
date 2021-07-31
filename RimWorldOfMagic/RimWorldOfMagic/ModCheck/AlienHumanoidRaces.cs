using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using AlienRace;
using RimWorld;

namespace TorannMagic.ModCheck
{
    public static class AlienHumanoidRaces
    {
        public static bool TryGetBackstory_DisallowedTrait(ThingDef thingDef, Pawn pawn, TraitDef td)
        {
            bool traitIsAllowed = true;
            //Log.Message("checking for alien races...");
            if (Validate.AlienHumanoidRaces.IsInitialized())
            {
                //Log.Message("initialized. Checking if " + thingDef.defName + " is an alien race...");
                ThingDef_AlienRace alienDef = thingDef as ThingDef_AlienRace;
                if (alienDef != null && alienDef.alienRace != null && alienDef.alienRace.raceRestriction != null && alienDef.alienRace.raceRestriction.traitList != null)
                {
                    if (alienDef.alienRace.raceRestriction.traitList.Contains(td))
                    {
                        traitIsAllowed = false;
                    }
                    //AlienTraitEntry ate = new AlienTraitEntry();
                    //ate.defName = (from def in DefDatabase<TraitDef>.AllDefs
                    //              where (def.defName == traitString)
                    //              select def).FirstOrDefault();
                    ////Log.Message("alien race. checking if " + traitString + " is allowed for backstory...");
                    //if (alienDef.alienRace.generalSettings != null && alienDef.alienRace.generalSettings. != null && alienDef.alienRace.generalSettings.disallowedTraits.)
                    //{
                    //    traitIsAllowed = false;
                    //}
                    if (pawn.story != null && pawn.story.AllBackstories != null)
                    {
                        foreach (Backstory bs in pawn.story.AllBackstories)
                        {
                            IEnumerable<BackstoryDef> enumerable = from def in DefDatabase<BackstoryDef>.AllDefs
                                                                   where (def.backstory == bs)
                                                                   select def;
                            foreach (BackstoryDef current in enumerable)
                            {
                                //Log.Message(current.LabelCap + " has disallowed traits: " + current.disallowedTraits.Count);
                                for (int i = 0; i < current.disallowedTraits.Count; i++)
                                {
                                    //Log.Message("" + current.disallowedTraits[i].defName);
                                    if (current.disallowedTraits[i].defName == td)
                                    {
                                        //Log.Message("trait is disallowed");
                                        traitIsAllowed = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Log.Message("trait " + traitString + " is allowed: " + traitIsAllowed);
            return traitIsAllowed;
        }
    }
}
