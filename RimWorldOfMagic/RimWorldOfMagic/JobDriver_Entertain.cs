using System.Collections.Generic;
using Verse.AI;
using Verse;
using RimWorld;
using System;
using UnityEngine;


namespace TorannMagic
{
    internal class JobDriver_Entertain : JobDriver
    {
        private const TargetIndex entertaineeTI = TargetIndex.A;
        private Pawn SongOwner => job.targetB.Pawn ?? pawn;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref age, "TM_entertainWorkTicks", -1);
        }

        int age = -1;
        int duration = 120;

        protected Pawn entertaineePawn
        {
            get
            {
                return (Pawn)this.job.targetA.Thing;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => ClassProgression.BardTrials.EntertainmentOwner(pawn) != SongOwner);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            this.FailOn(() => entertaineePawn?.Spawned != true || !entertaineePawn.Awake());
            this.FailOnDowned(TargetIndex.A);
            this.FailOnMentalState(TargetIndex.A);
            Toil entertain = new Toil()
            {
                tickAction = () =>
                {
                    if (age > duration)
                    {
                        ClassProgression.BardTrials.Entertain(this.pawn, entertaineePawn);
                        FleckMaker.ThrowMicroSparks(this.pawn.DrawPos, this.pawn.Map);
                        SongOwner.GetCompAbilityUserMagic().nextEntertainTick = Find.TickManager.TicksGame + 2000;
                        this.EndJobWith(JobCondition.Succeeded);
                        age = 0;
                    }
                    age++;
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };            
            yield return entertain;
        }
    }
}