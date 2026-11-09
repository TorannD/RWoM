using System;
using Verse;
using Verse.AI;

namespace TorannMagic.Integrations
{
    // Optional framework ownership is separate from combat evidence. A support
    // cast must never make an otherwise peaceful NPC eligible for combat AI.
    public static class VefCastOwnership
    {
        public static bool IsCasting(Pawn pawn)
        {
            if (pawn == null) return false;
            if (IsFrameworkType(pawn.CurJob?.def?.driverClass, "VEF.Abilities.JobDriver_CastAbilityOnce")
                || IsFrameworkType(pawn.CurJob?.def?.driverClass, "VEF.Abilities.JobDriver_GotoTargetAndCastAbilityOnce")) return true;
            // Protect an active framework warmup independently of the current job.
            // CompAbilities.currentlyCasting is not ownership evidence: it can be stale.
            return pawn.stances?.curStance is Stance_Warmup warmup
                && IsFrameworkType(warmup.verb?.GetType(), "VEF.Abilities.Verb_CastAbility");
        }

        private static bool IsFrameworkType(Type type, string name)
        {
            for (; type != null; type = type.BaseType)
                if (type.FullName == name && type.Assembly.GetName().Name == "VEF") return true;
            return false;
        }
    }
}

