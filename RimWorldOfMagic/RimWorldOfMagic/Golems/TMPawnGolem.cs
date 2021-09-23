using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class TMPawnGolem : TMPawnSummoned
    {

        public CompGolem Golem
        {
            get
            {
                return this.TryGetComp<CompGolem>();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public TMPawnGolem()
        {
            
        }

        public override void Tick()
        {
            if (Spawned && this.drafter == null)
            {
                this.drafter = new Pawn_DraftController(this);
            }
            if (this.abilities == null)
            {
                this.abilities = new Pawn_AbilityTracker(this);
            }
            base.Tick();
        }

        public override void Draw()
        {
            base.Draw();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            //var gizmoList = new List<Gizmo>();

            var gizmoList = base.GetGizmos().ToList();

            Command_Action command_Despawn = new Command_Action();
            command_Despawn.defaultLabel = "TM_DeActivateGolem".Translate();
            command_Despawn.defaultDesc = "TM_DeActivateGolemDesc".Translate();
            command_Despawn.icon = ContentFinder<Texture2D>.Get("UI/MoveOut", true);
            command_Despawn.action = delegate
            {
                if (Golem.shouldDespawn)
                {
                    Golem.despawnNow = true;
                }
                else
                {
                    if (Golem.dormantPosition.Walkable(this.Map) && Golem.dormantPosition.Standable(this.Map))
                    {
                        Job job = new Job(JobDefOf.Goto, Golem.dormantPosition);
                        this.jobs.StartJob(job, JobCondition.InterruptForced);
                        Golem.shouldDespawn = true;
                    }
                    else
                    {
                        Messages.Message("TM_GolemCannotReturn".Translate(
                        ), MessageTypeDefOf.RejectInput, false);
                        Golem.despawnNow = true;
                    }
                }
            };
            gizmoList.Add(command_Despawn);

            return gizmoList;
        }
    }
}
