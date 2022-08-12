using Verse;
using RimWorld;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;

namespace TorannMagic.Thoughts
{
    public class ThoughtWorker_TM_ObservedUndead : ThoughtWorker
    {
        private const float radius = 12f;

        private readonly HashSet<TraitDef> unaffectedByUndeadTraits = new HashSet<TraitDef>()
        {
            TorannMagicDefOf.TM_OKWithDeath,
            TorannMagicDefOf.DeathKnight,
            TorannMagicDefOf.Undead,
            TorannMagicDefOf.Necromancer,
            TorannMagicDefOf.Lich,
            TraitDefOf.Psychopath,
            TraitDefOf.Bloodlust,
            TraitDefOf.Masochist
        };

        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            if(!pawn.Spawned || !pawn.RaceProps.Humanlike)
            {
                return false;
            }
            if (pawn.story.traits.allTraits.Any(t => unaffectedByUndeadTraits.Contains(t.def)))
            {
                return false;
            }
            if (ModsConfig.IdeologyActive && pawn.Ideo != null)
            {                       
                Precept p = pawn.Ideo.GetAllPreceptsOfType<Precept>().FirstOrDefault((Precept x) => x.def.defName == "Corpses_DontCare");
                if(p != null)
                {
                    return false;
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
