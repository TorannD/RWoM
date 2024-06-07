using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace TorannMagic
{
    public class ThinkNode_IsPolymorphed : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            return TM_Calc.IsPolymorphed(pawn);
        }
    }
}
