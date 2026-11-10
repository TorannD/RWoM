using System;
using Verse;
using RimWorld;

namespace TorannMagic.Thoughts
{
    public class ThoughtWorker_TM_HolierThanThou : ThoughtWorker
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
                    if ((ClassProgression.ClassIdentity.IsPaladin(other) || ClassProgression.ClassIdentity.IsDruid(other) || ClassProgression.ClassIdentity.IsPriest(other)) && (ClassProgression.ClassIdentity.IsBaseNecromancer(pawn) || ClassProgression.ClassIdentity.IsLich(pawn) || ClassProgression.DeathKnightMartialPath.IsKnight(pawn)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
