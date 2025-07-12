using System.Collections.Generic;
using Verse.AI;
using System;
using Verse;
using RimWorld;
using UnityEngine;


namespace TorannMagic
{
    internal class JobDriver_ChargePortal: JobDriver
    {
        private const TargetIndex building = TargetIndex.A;
        Building_TMPortal portalBldg;
        Building_TMArcaneCapacitor arcaneCapacitor;
        Building_TM_DMP dmp;
        Building bldg;
        CompAbilityUserMagic comp;

        int age = -1;
        int chargeAge = 0;
        int ticksTillCharge = 30;
        int effectsAge = 0;
        int ticksTillEffects = 12;
        int duration = 1000;
        int xpNum = 0;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(building);
            Toil reserveTargetA = Toils_Reserve.Reserve(building);
            yield return reserveTargetA;
            comp = this.pawn.GetCompAbilityUserMagic();
            portalBldg = TargetA.Thing as Building_TMPortal;
            arcaneCapacitor = TargetA.Thing as Building_TMArcaneCapacitor;
            dmp = TargetA.Thing as Building_TM_DMP;
            bldg = TargetA.Thing as Building;

            Toil gotoPortal = new Toil()
            {
                initAction = () =>
                {
                    //pawn.pather.StartPath(portalBldg.InteractionCell, PathEndMode.OnCell);
                    if (pawn.pather != null && bldg.InteractionCell != null)
                    {
                        pawn.pather.StartPath(bldg.InteractionCell, PathEndMode.OnCell);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            yield return gotoPortal;

            Toil chargePortal = new Toil()
            {
                initAction = () =>
                {
                    if (age > duration)
                    {
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                    if (comp != null && comp.Mana != null && comp.Mana.CurLevel < .01f)
                    {
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                    else if (bldg.def.defName == "TM_DimensionalManaPocket")
                    {
                        this.duration = 220;
                    }
                },
                tickAction = () =>
                {
                    if (age > (effectsAge + ticksTillEffects))
                    {
                        this.effectsAge = this.age;
                        TM_MoteMaker.ThrowCastingMote(pawn.DrawPos, pawn.Map, Rand.Range(1.2f, 2f));
                        Vector3 moteDirection = TM_Calc.GetVector(this.pawn.Position, bldg.Position);
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Shadow, pawn.DrawPos, pawn.Map, Rand.Range(.4f, .6f), Rand.Range(.1f, .2f), .04f, Rand.Range(.1f, .2f), 300, 5f, (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 0);
                    }
                    if (age > (chargeAge + ticksTillCharge))
                    {                                               
                        if(bldg.def.defName == "TM_Portal")
                        {
                            comp.Mana.CurLevel -= .01f;
                            portalBldg.ArcaneEnergyCur += .01f;
                            xpNum += 3;
                        }
                        else if(bldg.def.defName == "TM_ArcaneCapacitor")
                        {
                            comp.Mana.CurLevel -= .01f;
                            arcaneCapacitor.ArcaneEnergyCur += 1f;
                        }
                        else if (bldg.def.defName == "TM_DimensionalManaPocket")
                        {
                            comp.Mana.CurLevel -= .05f;
                            dmp.ArcaneEnergyCur += 4f;
                            age += 4;
                        }
                        else
                        {
                            age = duration;
                        }                        
                        chargeAge = age;
                    }
                    age++;
                    if (age > duration)
                    {
                        AttributeXP(comp);
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                    if (comp.Mana.CurLevel < .1f)
                    {
                        AttributeXP(comp);
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                    if (comp.Mana.CurLevel < .05f && bldg.def.defName == "TM_DimensionalManaPocket")
                    {
                        //AttributeXP(comp);
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                    if (portalBldg != null && portalBldg.ArcaneEnergyCur >= 1f)
                    {
                        AttributeXP(comp);
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                    if (arcaneCapacitor != null && arcaneCapacitor.ArcaneEnergyCur >= arcaneCapacitor.TargetArcaneEnergyPct)
                    {
                        //AttributeXP(comp);
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                    if (dmp != null && dmp.ArcaneEnergyCur >= dmp.TargetArcaneEnergyPct)
                    {
                        //AttributeXP(comp);
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
            yield return chargePortal;
        }

        private void AttributeXP(CompAbilityUserMagic comp)
        {
            comp.MagicUserXP += xpNum;
            MoteMaker.ThrowText(this.pawn.DrawPos, this.pawn.MapHeld, "XP +" + xpNum, -1f);
        }
    }
}


//Toil chargePortal = new Toil();
//            while (age<duration)
//            {
//                if (portalBldg != null && pawn != null && !pawn.Downed && !pawn.Dead)
//                {
//                    if(age > chargeAge + ticksTillCharge)
//                    {
//                        if (comp.Mana.CurLevel <= 0.01f)
//                        {
//                            age = duration;
//                        }
//                        comp.Mana.CurLevel -= .01f;
//                        portalBldg.ArcaneEnergyCur += .01f;
//                        Log.Message("Portal Energy at " + portalBldg.ArcaneEnergyCur);
//                        chargeAge = age;
//                    }
//                    //chargePortal.initAction = () =>
//                    //{
//                    //    portalBldg = TargetA.Thing as Building_TMPortal;

//                    //    Log.Message("" + pawn.Label + " has " + comp.Mana.CurLevel + " mana");
//                    //};
//                    //chargePortal.AddPreTickAction(() =>
//                    //{
//                    //    if (comp.Mana.CurLevel < 0.01f)
//                    //    {
//                    //        ReadyForNextToil();
//                    //    }
//                    //});
//                    //chargePortal.tickAction = () =>
//                    //{
//                    //    if (comp.Mana.CurLevel >= 0.01f)
//                    //    {
//                    //        comp.Mana.CurLevel -= .01f;
//                    //        portalBldg.ArcaneEnergyCur += .01f;
//                    //        Log.Message("Portal Energy at " + portalBldg.ArcaneEnergyCur);
//                    //    }
//                    //};
//                    //chargePortal.AddFinishAction(() =>
//                    //{
//                    //    Log.Message("Portal charging complete.");
//                    //});
//                }
//                age++;
//                yield return chargePortal;
//    }