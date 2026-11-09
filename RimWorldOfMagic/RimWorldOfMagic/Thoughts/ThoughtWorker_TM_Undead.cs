using System;
using Verse;
using RimWorld;

namespace TorannMagic.Thoughts
{
    public class ThoughtWorker_TM_Undead : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (pawn != null && other != null)
            {
                if (!other.RaceProps.Humanlike || other.Dead)
                {
                    return false;
                }
                if (!RelationsUtility.PawnsKnowEachOther(pawn, other))
                {
                    return false;
                }
                if (!other.story.traits.HasTrait(TorannMagicDefOf.Undead))
                {
                    return false;
                }
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_OKWithDeath) || ClassProgression.DeathKnightMartialPath.IsKnight(pawn) || ClassProgression.ClassIdentity.IsBaseNecromancer(pawn) || ClassProgression.ClassIdentity.IsLich(pawn) || pawn.story.traits.HasTrait(TraitDefOf.Psychopath) || pawn.story.traits.HasTrait(TraitDefOf.Bloodlust) || pawn.story.traits.HasTrait(TraitDef.Named("Masochist")))
                {
                    return false;
                }
                if(!ThoughtUtility.CanGetThought(pawn, TorannMagicDefOf.ObservedLayingCorpse))
                {
                    return false;
                }
                if(!ThoughtUtility.CanGetThought(pawn, TorannMagicDefOf.ObservedLayingRottingCorpse))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
