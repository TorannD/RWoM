using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI.Group;


namespace TorannMagic.Ideology
{
    public class TM_StageEndTrigger_PawnIsMage : StageEndTrigger
    {
        [NoTranslate]
        public string roleId;

        public override Trigger MakeTrigger(LordJob_Ritual ritual, TargetInfo spot, IEnumerable<TargetInfo> foci, RitualStage stage)
        {
            if (ritual.Ritual.behavior.def.roles.FirstOrDefault((RitualRole r) => r.id == roleId) == null)
            {
                return null;
            }
            Pawn pawn = ritual.assignments.FirstAssignedPawn(roleId);
            if (pawn == null)
            {
                return null;
            }
            return new Trigger_TickCondition(() => TM_Calc.IsMagicUser(pawn));
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref roleId, "roleId");
        }
    }
}
