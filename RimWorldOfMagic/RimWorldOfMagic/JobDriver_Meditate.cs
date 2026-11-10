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
        private int focusedTicks;
        private bool cultivationCompleted;
        private bool activationPaid;
        internal const string HostedJob = "TM_SpiritMeditate";
        private bool Hosted => job.def.defName == HostedJob;
        private Pawn SkillOwner => Hosted ? job.targetB.Pawn : pawn;
        private bool ValidSkillOwner => !Hosted ||
            SkillOwner != null && ClassProgression.MonkMartialPath.Owner(pawn) == SkillOwner;
        private bool BeginHosted()
        {
            if (!Hosted) return true;
            if (!ValidSkillOwner || !pawn.Spawned || pawn.DeadOrDowned) return false;
            if (activationPaid) return true;
            var power = ClassProgression.SpiritHostedMartial.Find(pawn, SkillOwner, HostedJob);
            if (!ClassProgression.SpiritHostedMartial.Ready(pawn, power, HostedJob)) return false;
            power.PostAbilityAttempt();
            activationPaid = true;
            return true;
        }
        private void CompleteCultivation()
        {
            if (cultivationCompleted || !ValidSkillOwner) return;
            cultivationCompleted = ClassProgression.MonkMeditation.Finish(pawn);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref focusedTicks,"monkFocusedTicks");
            Scribe_Values.Look(ref age,"monkMeditationAge",-1);
            Scribe_Values.Look(ref cultivationCompleted,"monkCultivationCompleted");
            Scribe_Values.Look(ref activationPaid,"monkHostedActivationPaid");
        }
        public int durationTicks = 8000;
        Hediff chiHD = null;
        int effVal = 0;
        int verVal = 0;
        int pwrVal = 0;
        int chiMultiplier = 1;

        private bool RefreshTraining()
        {
            chiHD = SkillOwner?.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD, false);
            var comp = SkillOwner?.GetCompAbilityUserMight();
            if (comp == null || chiHD == null) return false;
            effVal = comp.MightData.MightPowerSkill_Meditate.FirstOrDefault(x => x.label == "TM_Meditate_eff")?.level ?? 0;
            pwrVal = comp.MightData.MightPowerSkill_Meditate.FirstOrDefault(x => x.label == "TM_Meditate_pwr")?.level ?? 0;
            verVal = comp.MightData.MightPowerSkill_Meditate.FirstOrDefault(x => x.label == "TM_Meditate_ver")?.level ?? 0;
            return true;
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return ValidSkillOwner;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !ValidSkillOwner);
            Toil gotoSpot = new Toil()
            {
                initAction = () =>
                {
                    if (!BeginHosted()) { EndJobWith(JobCondition.Incompletable); return; }
                    pawn.pather.StartPath(TargetLocA, PathEndMode.OnCell);
                },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            yield return gotoSpot;

            if (ClassProgression.MonkMartialPath.Path(SkillOwner) is ClassProgression.DisciplineProgress career)
            {
                this.FailOn(() => pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.Position != TargetLocA && pawn.jobs.curDriver == this && CurToilIndex > 0);
                if (career.MartialAlternate)
                {
                    yield return Toils_General.Wait(600).WithProgressBarToilDelay(TargetIndex.A);
                    yield return Toils_General.Do(CompleteCultivation);
                    yield break;
                }
            }
            Toil doFor = new Toil()
            {
                initAction = () =>
                {
                    if (!RefreshTraining()) { EndJobWith(JobCondition.Incompletable); return; }
                    if(this.age > this.durationTicks)
                    {
                        this.EndJobWith(JobCondition.InterruptForced);
                    }
                },
                tickAction = () =>
                {
                    if (!ValidSkillOwner || chiHD == null && !RefreshTraining()) { EndJobWith(JobCondition.Incompletable); return; }
                    if (ClassProgression.MonkMartialPath.Path(SkillOwner)?.Stage == ClassProgression.InitiationStage.Adept && ++focusedTicks >= 600)
                    {
                        ClassProgression.MonkMartialPath.Cultivated(SkillOwner);
                        focusedTicks = 0;
                    }
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
                            chiHD = SkillOwner?.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD, false);
                            if(chiHD == null)
                            {
                                Log.Warning("No chi found on pawn performing meditate job");
                                this.EndJobWith(JobCondition.InterruptForced);
                            }
                        }
                        CompAbilityUserMight comp = SkillOwner?.GetCompAbilityUserMight();
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