using RimWorld;
using Verse;
using System.Collections.Generic;

namespace TorannMagic.Ideology
{
    public class TM_RitualRoleGifted : RitualRole
    {
        public override bool AppliesToPawn(Pawn p, out string reason, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
        {
            reason = null;
            if (!(p.Faction.IsPlayerSafe() || p.IsPrisoner))
            {
                if (!skipReason)
                {
                    reason = "TM_MessageRitualRoleMustBeColonistOrPrisoner".Translate(p.LabelShort);
                }
                return false;
            }
            if (!p.RaceProps.Humanlike)
            {
                if (!skipReason)
                {
                    reason = "MessageRitualRoleMustBeHumanlike".Translate(base.LabelCap);
                }
                return false;
            }
            if(!p.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted))
            {
                if (!skipReason)
                {
                    reason = "TM_MessageRitualRoleMustHaveTrait".Translate(p.LabelShort, TorannMagicDefOf.TM_Gifted.degreeDatas[0].label);
                }
                return false;
            }            
            return true;
        }

        public override bool AppliesToRole(Precept_Role role, out string reason, Precept_Ritual ritual = null, Pawn p = null, bool skipReason = false)
        {
            reason = null;
            return false;
        }
    }
}
