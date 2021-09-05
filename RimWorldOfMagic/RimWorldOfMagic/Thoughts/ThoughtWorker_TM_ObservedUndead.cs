using Verse;
using RimWorld;
using System.Collections.Generic;
using HarmonyLib;

namespace TorannMagic.Thoughts
{
    public class ThoughtWorker_TM_ObservedUndead : ThoughtWorker
    {
        private const float radius = 12f;

        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            if(!pawn.Spawned || !pawn.RaceProps.Humanlike)
            {
                return false;
            }
            if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_OKWithDeath) || pawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight) || pawn.story.traits.HasTrait(TorannMagicDefOf.Undead) || pawn.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.Lich) || pawn.story.traits.HasTrait(TraitDefOf.Psychopath) || pawn.story.traits.HasTrait(TraitDefOf.Bloodlust) || pawn.story.traits.HasTrait(TraitDef.Named("Masochist")))
            {
                return false;
            }
            if(ModsConfig.IdeologyActive)
            {
                List<Precept> pList = Traverse.Create(root: pawn.Ideo).Field(name: "precepts").GetValue<List<Precept>>();
                foreach (Precept prec in pList)
                {
                    if (prec.def.defName == "Corpses_DontCare")
                    {
                        return false;
                    }
                }                
            }
            List<Pawn> mapPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < mapPawns.Count; i++)
            {
                if (mapPawns[i].Spawned && mapPawns[i].RaceProps.Humanlike)
                {
                    if (mapPawns[i].story.traits.HasTrait(TorannMagicDefOf.Undead))
                    {
                        if (pawn.Position.InHorDistOf(mapPawns[i].Position, radius))
                        {
                            return true;
                        }
                    }
                }
            }            
            return false;
        }
    }
}
