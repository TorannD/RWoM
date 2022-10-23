using System;
using System.Collections.Generic;
using System.Linq;
using AlienRace;
using Verse;
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

                    if (pawn.story != null && pawn.story.AllBackstories != null)
                    {
                        foreach (RimWorld.BackstoryDef bs in pawn.story.AllBackstories)
                        {
                            foreach(BackstoryTrait bt in bs.disallowedTraits)
                            {
                                if(bt.def == td)
                                {
                                    traitIsAllowed = false;
                                    break;
                                }
                            }
                            if(!traitIsAllowed)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return traitIsAllowed;
        }
    }
}
