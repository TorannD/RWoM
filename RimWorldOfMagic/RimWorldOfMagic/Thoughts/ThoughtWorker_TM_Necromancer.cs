using System;
using Verse;
using RimWorld;

namespace TorannMagic.Thoughts
{
    public class ThoughtWorker_TM_Necromancer : ThoughtWorker
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
                if (pawn.RaceProps.Humanlike && other.RaceProps.Humanlike)
                {
                    if ((ClassProgression.ClassIdentity.IsPaladin(pawn) || ClassProgression.ClassIdentity.IsDruid(pawn) || ClassProgression.ClassIdentity.IsPriest(pawn)) && (ClassProgression.ClassIdentity.IsBaseNecromancer(other)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
