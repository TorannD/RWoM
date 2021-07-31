using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;
using UnityEngine;
using System;


namespace TorannMagic
{
    public class JobDriver_DoMagicBill : JobDriver_DoBill
    {
        private int age = -1;
        public int durationTicks = 60;

        public float workLeft;

        public int billStartTick;

        public int ticksSpentDoingRecipeWork;

        public const PathEndMode GotoIngredientPathEndMode = PathEndMode.ClosestTouch;

        public const TargetIndex BillGiverInd = TargetIndex.A;

        public const TargetIndex IngredientInd = TargetIndex.B;

        public const TargetIndex IngredientPlaceCellInd = TargetIndex.C;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workLeft, "workLeft", 0f);
            Scribe_Values.Look(ref billStartTick, "billStartTick", 0);
            Scribe_Values.Look(ref ticksSpentDoingRecipeWork, "ticksSpentDoingRecipeWork", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = base.pawn;
            LocalTargetInfo target = base.job.GetTarget(TargetIndex.A);
            Job job = base.job;
            bool errorOnFailed2 = errorOnFailed;
            if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed2))
            {
                return false;
            }
            base.pawn.ReserveAsManyAsPossible(base.job.GetTargetQueue(TargetIndex.B), base.job);
            return true;
        }

        public IBillGiver BillGiver
        {
            get
            {
                IBillGiver billGiver = job.GetTarget(TargetIndex.A).Thing as IBillGiver;
                if (billGiver == null)
                {
                    throw new InvalidOperationException("DoBill on non-Billgiver.");
                }
                return billGiver;
            }
        }

        public override string GetReport()
        {
            if (job.RecipeDef != null)
            {
                return ReportStringProcessed(job.RecipeDef.jobString);
            }
            return base.GetReport();
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Log.Message("doing magic bill");
            //Log.Message("actor is " + this.GetActor().LabelShort);
            //Log.Message("doing job " + this.GetActor().CurJobDef);
            //Log.Message("bill thing is " + this.GetActor().CurJob.GetTarget(TargetIndex.A).Thing.Label);
            //if(this.GetActor().CurJob.targetA.Thing is Building_TMMagicCircle)
            //{
            //    Log.Message("target building is a magic circle");
            //}
            //Log.Message("toil is " + base.MakeNewToils().ToString());
            return base.MakeNewToils();
        }

        private static Toil JumpToCollectNextIntoHandsForBill(Toil gotoGetTargetToil, TargetIndex ind)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                if (actor.carryTracker.CarriedThing == null)
                {
                    Log.Error("JumpToAlsoCollectTargetInQueue run on " + actor + " who is not carrying something.");
                }
                else if (!actor.carryTracker.Full)
                {
                    Job curJob = actor.jobs.curJob;
                    List<LocalTargetInfo> targetQueue = curJob.GetTargetQueue(ind);
                    if (!targetQueue.NullOrEmpty())
                    {
                        int num = 0;
                        int a;
                        while (true)
                        {
                            if (num >= targetQueue.Count)
                            {
                                return;
                            }
                            if (GenAI.CanUseItemForWork(actor, targetQueue[num].Thing) && targetQueue[num].Thing.CanStackWith(actor.carryTracker.CarriedThing) && !((float)(actor.Position - targetQueue[num].Thing.Position).LengthHorizontalSquared > 64f))
                            {
                                int num2 = (actor.carryTracker.CarriedThing != null) ? actor.carryTracker.CarriedThing.stackCount : 0;
                                a = curJob.countQueue[num];
                                a = Mathf.Min(a, targetQueue[num].Thing.def.stackLimit - num2);
                                a = Mathf.Min(a, actor.carryTracker.AvailableStackSpace(targetQueue[num].Thing.def));
                                if (a > 0)
                                {
                                    break;
                                }
                            }
                            num++;
                        }
                        curJob.count = a;
                        curJob.SetTarget(ind, targetQueue[num].Thing);
                        List<int> countQueue;
                        int index;
                        (countQueue = curJob.countQueue)[index = num] = countQueue[index] - a;
                        if (curJob.countQueue[num] <= 0)
                        {
                            curJob.countQueue.RemoveAt(num);
                            targetQueue.RemoveAt(num);
                        }
                        actor.jobs.curDriver.JumpToToil(gotoGetTargetToil);
                    }
                }
            };
            return toil;
        }
    }
}