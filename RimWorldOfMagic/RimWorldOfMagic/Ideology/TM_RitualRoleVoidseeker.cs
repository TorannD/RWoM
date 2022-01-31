using RimWorld;
using Verse;
using System.Collections.Generic;

namespace TorannMagic.Ideology
{
    public class TM_RitualRoleVoidseeker : RitualRole
    {
        public override bool AppliesToPawn(Pawn p, out string reason, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
        {
            reason = null;
            if (p == null || p.Faction == null || p.RaceProps == null || p.Ideo == null)
            {
                return false;
            }
            if (!p.Faction.IsPlayerSafe())
            {
                if (!skipReason)
                {
                    reason = "MessageRitualRoleMustBeColonist".Translate(base.Label);
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
            if (TM_Calc.IsUndeadNotVamp(p))
            {
                if (!skipReason)
                {
                    reason = "TM_MessageRitualRoleCannotBeUndead".Translate(base.LabelCap);
                }
                return false;
            }
            if(!p.IsFreeNonSlaveColonist)
            {
                if (!skipReason)
                {
                    reason = "Cannot be a slave";
                }
                return false;
            }
            if (p.Ideo.GetRole(p) == null)
            {
                if (!skipReason)
                {
                    reason = "TM_MessageRitualRoleMustBeVoidseeker".Translate(p.LabelShort);
                }
                return false;
            }
            if (p.Ideo.GetRole(p).def != TorannMagicDefOf.TM_IdeoRole_VoidSeeker)
            {
                if (!skipReason)
                {
                    reason = "TM_MessageRitualRoleMustBeVoidseeker".Translate(p.LabelShort);
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
