using System;
using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorannMagic.Ideology
{
    public class PreceptComp_SelfTookMemoryThought_KilledMage : PreceptComp_SelfTookMemoryThought
    {
        public override void Notify_MemberTookAction(HistoryEvent ev, Precept precept, bool canApplySelfTookThoughts)
        {
            if (ev.def != eventDef || !canApplySelfTookThoughts)
            {
                return;
            }
            Pawn arg = ev.args.GetArg<Pawn>(HistoryEventArgsNames.Doer);
            if (arg.needs != null && arg.needs.mood != null && (!onlyForNonSlaves || !arg.IsSlave) && (thought.minExpectationForNegativeThought == null || ExpectationsUtility.CurrentExpectationFor(arg).order >= thought.minExpectationForNegativeThought.order))
            {
                Thought_Memory thought_Memory = ThoughtMaker.MakeThought(thought, precept);
                if (TM_Calc.IsMagicUser(arg))
                {
                    thought_Memory.SetForcedStage(1);
                }               
                arg.needs.mood.thoughts.memories.TryGainMemory(thought_Memory);
            }
        }
    }
}
