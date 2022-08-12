using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse.AI;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_60mmMortar : Verb_UseAbility
    {
        int pwrVal = 0;
        int verVal = 0;
        int effVal = 0;
        Thing mortar = null;

        protected override bool TryCastShot()
        {
            Pawn pawn = base.CasterPawn;
            Map map = pawn.Map;

            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
            //pwrVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_60mmMortar, "TM_60mmMortar", "_pwr", true);
            //verVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_60mmMortar, "TM_60mmMortar", "_ver", true);
            //effVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_60mmMortar, "TM_60mmMortar", "_eff", true);
            pwrVal = TM_Calc.GetSkillPowerLevel(pawn, this.Ability.Def as TMAbilityDef);
            verVal = TM_Calc.GetSkillVersatilityLevel(pawn, this.Ability.Def as TMAbilityDef);
            effVal = TM_Calc.GetSkillEfficiencyLevel(pawn, this.Ability.Def as TMAbilityDef);

            if ((pawn.Position.IsValid && pawn.Position.Standable(map)))
            {
                AbilityUser.SpawnThings tempPod = new SpawnThings();
                IntVec3 shiftPos = TM_Calc.GetEmptyCellForNewBuilding(pawn.Position, map, 1.6f, false, 0);

                tempPod.def = TorannMagicDefOf.TM_60mmMortar_Base;
                tempPod.spawnCount = 1;

                if (shiftPos != default(IntVec3))
                {
                    try
                    {
                        this.mortar = TM_Action.SingleSpawnLoop(pawn, tempPod, shiftPos, map, 1200, true, false, pawn.Faction);

                        for (int i = 0; i < 3; i++)
                        {
                            Vector3 rndPos = this.mortar.DrawPos;
                            rndPos.x += Rand.Range(-.5f, .5f);
                            rndPos.z += Rand.Range(-.5f, .5f);
                            TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.SparkFlash, rndPos, map, Rand.Range(.6f, .8f), .1f, .05f, .05f, 0, 0, 0, Rand.Range(0, 360));
                            FleckMaker.ThrowSmoke(rndPos, map, Rand.Range(.8f, 1.2f));
                        }
                    }
                            catch
                    {
                        comp.Stamina.CurLevel += comp.ActualStaminaCost(TorannMagicDefOf.TM_60mmMortar);
                        Log.Message("TM_Exception".Translate(
                                pawn.LabelShort,
                                "60mm Mortar"
                            ));
                    }
                }
                else
                {
                    Messages.Message("InvalidSummon".Translate(), MessageTypeDefOf.RejectInput);
                    comp.Stamina.GainNeed(comp.ActualStaminaCost(TorannMagicDefOf.TM_60mmMortar));
                }
            }
            else
            {
                Messages.Message("InvalidSummon".Translate(), MessageTypeDefOf.RejectInput);
                comp.Stamina.GainNeed(comp.ActualStaminaCost(TorannMagicDefOf.TM_60mmMortar));
            }


            if ((mortar != null && mortar.Spawned && mortar.Position.IsValid))
            {
                this.Ability.PostAbilityAttempt();
                mortar.def.interactionCellOffset = (caster.Position - mortar.Position);
                Job job = new Job(JobDefOf.ManTurret, mortar);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                //this.Ability.PostAbilityAttempt();
            }
            else
            {
                Log.Message("mortar was null");
            }

            return false;
        }
    }
}
