using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TorannMagic
{
    public class Command_LocalTargetInfo : Command
    {
        public Action<LocalTargetInfo> action;

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
