using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

using Verse.Sound;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class GolemWorkstationEffect_ApplyHediff : GolemWorkstationEffect
    {
        public HediffDef hediff;
        public float severity;
        public bool canStack;
        public float maxRange;
        public bool requiresLoS;
        public bool targetNeutral;
        public bool targetFriendly;
        public bool targetEnemy;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.severity, "severity");
            Scribe_Values.Look<float>(ref this.maxRange, "maxRange");
            Scribe_Values.Look<HediffDef>(ref this.hediff, "hediff");
            Scribe_Values.Look<bool>(ref this.canStack, "canStack");
            Scribe_Values.Look<bool>(ref this.requiresLoS, "requiresLoS");
            Scribe_Values.Look<bool>(ref this.targetNeutral, "targetNeutral");
            Scribe_Values.Look<bool>(ref this.targetFriendly, "targetFriendly");
            Scribe_Values.Look<bool>(ref this.targetEnemy, "targetEnemy");

        }

        public override void StartEffect(Building_TMGolemBase golem_building, TM_GolemUpgrade upgrade, float effectLevel = 1)
        {
            base.StartEffect(golem_building, upgrade, effectLevel);
        }

        public override bool CanDoEffect(Building_TMGolemBase golem_building)
        {
            if (target != null && target.HasThing && !golem_building.holdFire && golem_building.GolemComp.TargetIsValid(golem_building, target.Thing))
            {
                Pawn p = target.Thing as Pawn;
                if(p == null)
                {
                    return false;
                }
                if(p.health == null)
                {
                    return false;
                }
                if(p.health.hediffSet == null)
                {
                    return false;
                }
                if((p.Position - golem_building.Position).LengthHorizontal > maxRange)
                {
                    return false;
                }
                if(requiresLoS && !TM_Calc.HasLoSFromTo(golem_building.Position, p, golem_building, 0f, maxRange))
                {
                    return false;
                }
                if(targetEnemy && !p.HostileTo(golem_building.Faction))
                {
                    return false;
                }
                if(targetFriendly && !p.HostileTo(golem_building.Faction))
                {
                    return false;
                }
                if(targetNeutral && p.Faction != null)
                {
                    return false;
                }
                return base.CanDoEffect(golem_building);
            }
            return false;
        }
    }
}
