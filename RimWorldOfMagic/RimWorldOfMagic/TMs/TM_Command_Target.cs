using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TorannMagic
{
    //WTF does this not exist in core???
    public class TM_Command_Target : Command
    {
        public Action<LocalTargetInfo> action;

        public float drawRadius;

        public TargetingParameters targetingParams;

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();          
            Find.Targeter.BeginTargeting(targetingParams, delegate (LocalTargetInfo target)
            {
                action(target);
            });
        }

        public override bool InheritInteractionsFrom(Gizmo other)
        {
            return false;
        }
    }
}
