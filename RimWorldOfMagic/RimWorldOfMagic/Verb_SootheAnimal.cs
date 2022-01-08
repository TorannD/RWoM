using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;


namespace TorannMagic
{
    public class Verb_SootheAnimal : Verb_UseAbility
    {

        private int verVal;
        private int pwrVal;

        bool validTarg;
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = true;
                }
                else
                {
                    //out of range
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot()
        {
            bool flag = false;
            this.TargetsAoE.Clear();
            //this.UpdateTargets();
            this.FindTargets();
            //MagicPowerSkill pwr = base.CasterPawn.GetComp<CompAbilityUserMagic>().MagicData.MagicPowerSkill_SootheAnimal.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SootheAnimal_pwr");
            //pwrVal = pwr.level;
            //if (base.CasterPawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mpwr = base.CasterPawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    pwrVal = mpwr.level;
            //}
            pwrVal = TM_Calc.GetSkillPowerLevel(CasterPawn, this.Ability.Def as TMAbilityDef);
            bool flag2 = this.UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetAoE && this.TargetsAoE.Count > 1;
            if (flag2)
            {
                this.TargetsAoE.RemoveRange(0, this.TargetsAoE.Count - 1);
            }
            for (int i = 0; i < this.TargetsAoE.Count; i++)
            {
                if (this.TargetsAoE[i].Thing.Faction != this.CasterPawn.Faction)
                {
                    Pawn newPawn = this.TargetsAoE[i].Thing as Pawn;

                    bool flag1 = (newPawn.mindState.mentalStateHandler.CurStateDef == MentalStateDefOf.ManhunterPermanent) || (newPawn.mindState.mentalStateHandler.CurStateDef == MentalStateDefOf.Manhunter);
                    if (flag1)
                    {
                        if(newPawn.kindDef.RaceProps.Animal)
                        {
                            newPawn.mindState.mentalStateHandler.Reset();
                            newPawn.jobs.StopAll();
                            FleckMaker.ThrowMicroSparks(newPawn.Position.ToVector3().normalized, newPawn.Map);
                            float sev = Rand.Range(pwrVal, 2 * pwrVal);
                            HealthUtility.AdjustSeverity(newPawn, TorannMagicDefOf.TM_AntiManipulation, sev);
                            sev = Rand.Range(pwrVal, 2 * pwrVal);
                            HealthUtility.AdjustSeverity(newPawn, TorannMagicDefOf.TM_AntiMovement, sev);
                            sev = Rand.Range(pwrVal, 2 * pwrVal);
                            HealthUtility.AdjustSeverity(newPawn, TorannMagicDefOf.TM_AntiBreathing, sev);
                            sev = Rand.Range(pwrVal, 2 * pwrVal);
                            HealthUtility.AdjustSeverity(newPawn, TorannMagicDefOf.TM_AntiSight, sev);
                            if (pwrVal > 0)
                            {
                                TM_MoteMaker.ThrowSiphonMote(newPawn.Position.ToVector3(), newPawn.Map, 1f);
                            }
                        }
                    }
                    if(!flag1)
                    {
                        if (newPawn.kindDef.RaceProps.Animal && (this.TargetsAoE[i].Thing.Faction == null || this.TargetsAoE[i].Thing.HostileTo(base.CasterPawn.Faction)))
                        {
                            newPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, true, false, null);
                            float sev = Rand.Range(pwrVal, 2 * pwrVal);
                            HealthUtility.AdjustSeverity(newPawn, TorannMagicDefOf.TM_Manipulation, sev);
                            sev = Rand.Range(pwrVal, 2 * pwrVal);
                            HealthUtility.AdjustSeverity(newPawn, TorannMagicDefOf.TM_Movement, sev);
                            sev = Rand.Range(pwrVal, 2 * pwrVal);
                            HealthUtility.AdjustSeverity(newPawn, TorannMagicDefOf.TM_Breathing, sev);
                            sev = Rand.Range(pwrVal, 2 * pwrVal);
                            HealthUtility.AdjustSeverity(newPawn, TorannMagicDefOf.TM_Sight, sev);
                            FleckMaker.ThrowMicroSparks(newPawn.Position.ToVector3().normalized, newPawn.Map);
                            if (pwrVal > 0)
                            {
                                TM_MoteMaker.ThrowManaPuff(newPawn.Position.ToVector3(), newPawn.Map, 1f);
                            }
                            List<Pawn> potentialHostiles = new List<Pawn>();
                            potentialHostiles.Clear();
                            for (int j = 0; j < newPawn.Map.mapPawns.AllPawnsSpawned.Count; j++)
                            {
                                Pawn hostile = newPawn.Map.mapPawns.AllPawnsSpawned[j];
                                if (hostile.Faction.HostileTo(Faction.OfPlayerSilentFail))
                                {
                                    potentialHostiles.AddDistinct(hostile);
                                }
                            }
                            if (potentialHostiles.Count > 0)
                            {
                                Job job = new Job(JobDefOf.AttackMelee, potentialHostiles.RandomElement());
                                newPawn.jobs.TryTakeOrderedJob(job, JobTag.InMentalState);
                            }
                        }
                    }
                }
            }
            this.PostCastShot(flag, out flag);
            return flag;
        }

        private void FindTargets()
        {
            bool flag = this.UseAbilityProps.AbilityTargetCategory == AbilityTargetCategory.TargetAoE;
            if (flag)
            {
                bool flag2 = this.UseAbilityProps.TargetAoEProperties == null;
                if (flag2)
                {
                    Log.Error("Tried to Cast AoE-Ability without defining a target class");
                }
                List<Thing> list = new List<Thing>();
                IntVec3 aoeStartPosition = this.caster.PositionHeld;
                bool flag3 = !this.UseAbilityProps.TargetAoEProperties.startsFromCaster;
                if (flag3)
                {
                    aoeStartPosition = this.currentTarget.Cell;
                }
                bool flag4 = !this.UseAbilityProps.TargetAoEProperties.friendlyFire;
                if (flag4)
                {
                    list = (from x in this.caster.Map.listerThings.AllThings
                            where x.Position.InHorDistOf(aoeStartPosition, (float)this.UseAbilityProps.TargetAoEProperties.range) && this.UseAbilityProps.TargetAoEProperties.targetClass.IsAssignableFrom(x.GetType()) && x.Faction != Faction.OfPlayer
                            select x).ToList<Thing>();
                }
                else
                {
                    bool flag5 = this.UseAbilityProps.TargetAoEProperties.targetClass == typeof(Plant) || this.UseAbilityProps.TargetAoEProperties.targetClass == typeof(Building);
                    if (flag5)
                    {
                        list = (from x in this.caster.Map.listerThings.AllThings
                                where x.Position.InHorDistOf(aoeStartPosition, (float)this.UseAbilityProps.TargetAoEProperties.range) && this.UseAbilityProps.TargetAoEProperties.targetClass.IsAssignableFrom(x.GetType())
                                select x).ToList<Thing>();
                        foreach (Thing current in list)
                        {
                            LocalTargetInfo item = new LocalTargetInfo(current);
                            this.TargetsAoE.Add(item);
                        }
                        return;
                    }
                    list.Clear();
                    list = (from x in this.caster.Map.listerThings.AllThings
                            where x.Position.InHorDistOf(aoeStartPosition, (float)this.UseAbilityProps.TargetAoEProperties.range) && this.UseAbilityProps.TargetAoEProperties.targetClass.IsAssignableFrom(x.GetType()) && (x.HostileTo(Faction.OfPlayer) || this.UseAbilityProps.TargetAoEProperties.friendlyFire)
                            select x).ToList<Thing>();
                }
                int maxTargets = this.UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties.maxTargets;
                List<Thing> list2 = new List<Thing>(list.InRandomOrder(null));
                int num = 0;
                while (num < maxTargets && num < list2.Count<Thing>())
                {
                    TargetInfo targ = new TargetInfo(list2[num]);
                    bool flag6 = this.UseAbilityProps.targetParams.CanTarget(targ);
                    if (flag6)
                    {
                        this.TargetsAoE.Add(new LocalTargetInfo(list2[num]));
                    }
                    num++;
                }
            }
            else
            {
                this.TargetsAoE.Clear();
                this.TargetsAoE.Add(this.currentTarget);
            }
        }
    }
}
