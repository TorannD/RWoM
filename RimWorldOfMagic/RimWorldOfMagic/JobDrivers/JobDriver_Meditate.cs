using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;
using UnityEngine;
using System;
using HarmonyLib;


namespace TorannMagic
{
    internal class JobDriver_Meditate : JobDriver
    {
        private int age = -1;
        public int durationTicks = 8000;
        Hediff chiHD = null;
        int effVal = 0;
        int verVal = 0;
        int pwrVal = 0;
        int chiMultiplier = 1;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil gotoSpot = new Toil()
            {
                initAction = () =>
                {
                    pawn.pather.StartPath(TargetLocA, PathEndMode.OnCell);
                },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            yield return gotoSpot;

            Toil doFor = new Toil()
            {
                initAction = () =>
                {
                    chiHD = this.pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD, false);
                    CompAbilityUserMight comp = this.pawn.GetCompAbilityUserMight();
                    if(comp != null && chiHD != null)
                    {
                        effVal = comp.MightData.MightPowerSkill_Meditate.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Meditate_eff").level;
                        pwrVal = comp.MightData.MightPowerSkill_Meditate.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Meditate_pwr").level;
                        verVal = comp.MightData.MightPowerSkill_Meditate.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Meditate_ver").level;                        
                    }
                    else
                    {
                        Log.Warning("No Chi Hediff or Might Comp found.");
                        this.EndJobWith(JobCondition.Errored);
                    }
                    if(this.age > this.durationTicks)
                    {
                        this.EndJobWith(JobCondition.InterruptForced);
                    }
                },
                tickAction = () =>
                {
                    if(Find.TickManager.TicksGame % 12 == 0)
                    {
                        Vector3 rndPos = this.pawn.DrawPos;
                        rndPos.x += (Rand.Range(-.5f, .5f));
                        rndPos.z += Rand.Range(-.4f, .6f);
                        float direction = (this.pawn.DrawPos - rndPos).ToAngleFlat();
                        Vector3 startPos = rndPos;
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Chi_Grayscale, startPos, this.pawn.Map, Rand.Range(.1f, .22f), 0.2f, .3f, .2f, 30, .2f * (rndPos - this.pawn.DrawPos).MagnitudeHorizontal(), direction, direction);
                    }
                    if(Find.TickManager.TicksGame % 60 == 0)
                    {
                        List<Hediff> afflictionList = TM_Calc.GetPawnAfflictions(this.pawn);
                        List<Hediff> addictionList = TM_Calc.GetPawnAddictions(this.pawn);

                        if(chiHD != null)
                        {
                            if (chiHD.Severity > 1)
                            {
                                chiMultiplier = 5;
                            }
                            else
                            {
                                chiMultiplier = 1;
                            }
                        }
                        else
                        {
                            chiHD = this.pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD, false);
                            if(chiHD == null)
                            {
                                Log.Warning("No chi found on pawn performing meditate job");
                                this.EndJobWith(JobCondition.InterruptForced);
                            }
                        }
                        CompAbilityUserMight comp = this.pawn.GetCompAbilityUserMight();
                        if (TM_Calc.IsPawnInjured(this.pawn, 0))
                        {
                            TM_Action.DoAction_HealPawn(this.pawn, this.pawn, 1, Rand.Range(.25f, .4f) * chiMultiplier * (1+ (.1f *pwrVal)));
                            chiHD.Severity -= 1f;
                            comp.MightUserXP += (int)(2 * chiMultiplier);
                        }
                        else if (afflictionList != null && afflictionList.Count > 0)
                        {
                            Hediff hediff = afflictionList.RandomElement();
                            hediff.Severity -= .001f * chiMultiplier * (1 + (.1f * pwrVal));
                            if(hediff.Severity <= 0)
                            {
                                this.pawn.health.RemoveHediff(hediff);
                            }
                            HediffComp_Disappears hediffTicks = hediff.TryGetComp<HediffComp_Disappears>();
                            if(hediffTicks != null)
                            {
                                int ticksToDisappear = Traverse.Create(root: hediffTicks).Field(name: "ticksToDisappear").GetValue<int>();
                                ticksToDisappear -= Mathf.RoundToInt(10000 * (chiMultiplier * (1 + (.1f * pwrVal))));
                                Traverse.Create(root: hediffTicks).Field(name: "ticksToDisappear").SetValue(ticksToDisappear);
                            }
                            chiHD.Severity -= 1f;
                            comp.MightUserXP += (int)(2*chiMultiplier);
                        }
                        else if (addictionList != null && addictionList.Count > 0)
                        {
                            Hediff hediff = addictionList.RandomElement();
                            hediff.Severity -= .0015f * chiMultiplier * (1 + (.1f * pwrVal));
                            if (hediff.Severity <= 0)
                            {
                                this.pawn.health.RemoveHediff(hediff);
                            }
                            chiHD.Severity -= 1f;
                            comp.MightUserXP += (int)(2 * chiMultiplier);
                        }
                        else if(BreakRiskAlertUtility.PawnsAtRiskMinor.Contains(this.pawn) || BreakRiskAlertUtility.PawnsAtRiskMajor.Contains(this.pawn) || BreakRiskAlertUtility.PawnsAtRiskExtreme.Contains(this.pawn))
                        {
                            this.pawn.needs.mood.CurLevel += .004f * chiMultiplier * (1 + (.1f * verVal));
                            chiHD.Severity -= 1f;
                            comp.MightUserXP += (int)(2 * chiMultiplier);
                        }
                        else
                        {
                            chiHD.Severity += (Rand.Range(.2f, .3f) * (1 + (effVal * .1f)));
                            try
                            {
                                this.pawn.needs.rest.CurLevel += (.003f * (1 + (.1f * verVal)));
                                this.pawn.needs.joy.CurLevel += (.004f * (1 + (.1f * verVal)));
                                this.pawn.needs.mood.CurLevel += .001f * (1 + (.1f * verVal));
                            }
                            catch(NullReferenceException ex)
                            {
                                //ex
                            }
                        }

                    }
                    if(chiHD != null)
                    {
                        HediffComp_Chi chiComp = chiHD.TryGetComp<HediffComp_Chi>();
                        if(chiComp != null && chiHD.Severity >= chiComp.maxSev)
                        {
                            this.age = durationTicks;
                        }
                    }
                    if (age >= durationTicks)
                    {
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                    age++;
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
            doFor.defaultDuration = this.durationTicks;
            doFor.WithProgressBar(TargetIndex.A, delegate
            {
                if (this.pawn.DestroyedOrNull() || this.pawn.Dead || this.pawn.Downed)
                {
                    return 1f;
                }
                return 1f - (float)doFor.actor.jobs.curDriver.ticksLeftThisToil / this.durationTicks;

            }, false, 0f);
            yield return doFor;         
        }
    }
}