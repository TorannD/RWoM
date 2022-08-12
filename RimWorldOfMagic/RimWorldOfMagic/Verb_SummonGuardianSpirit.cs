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
    public class Verb_SummonGuardianSpirit : Verb_UseAbility
    {
        bool validTarg;
        //Used for non-unique abilities that can be used with shieldbelt
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == this.caster)
            {
                return this.verbProps.targetParams.canTargetSelf;
            }
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    ShootLine shootLine;
                    validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
                }
                else
                {
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        int pwrVal = 0;
        int verVal = 0;
        int effVal = 0;
        Thing spirit = null;

        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Map map = caster.Map;
            IntVec3 cell = currentTarget.Cell;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            //pwrVal = TM_Calc.GetMagicSkillLevel(caster, comp.MagicData.MagicPowerSkill_GuardianSpirit, "TM_GuardianSpirit", "_pwr", true);
            //verVal = TM_Calc.GetMagicSkillLevel(caster, comp.MagicData.MagicPowerSkill_GuardianSpirit, "TM_GuardianSpirit", "_ver", true);
            //effVal = TM_Calc.GetMagicSkillLevel(caster, comp.MagicData.MagicPowerSkill_GuardianSpirit, "TM_GuardianSpirit", "_eff", true);
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
            effVal = TM_Calc.GetSkillEfficiencyLevel(caster, this.Ability.Def as TMAbilityDef);

            if (cell != null && (cell.IsValid && cell.Walkable(map)))
            {
                AbilityUser.SpawnThings tempPod = new SpawnThings();
                IntVec3 shiftPos = cell;

                tempPod.def = comp.GuardianSpiritType;
                if(comp.GuardianSpiritType == TorannMagicDefOf.TM_SpiritBearR)
                {
                    tempPod.kindDef = PawnKindDef.Named("TM_SpiritBear");
                }
                else if (comp.GuardianSpiritType == TorannMagicDefOf.TM_SpiritMongooseR)
                {
                    tempPod.kindDef = PawnKindDef.Named("TM_SpiritMongoose");
                }
                else
                {
                    tempPod.kindDef = PawnKindDef.Named("TM_SpiritCrow");
                }
                tempPod.spawnCount = 1;

                if (shiftPos != default(IntVec3))
                {
                    try
                    {
                        if(comp.bondedSpirit != null)
                        {
                            if (comp.bondedSpirit.Map != null)
                            {
                                FleckMaker.ThrowSmoke(comp.bondedSpirit.DrawPos, comp.bondedSpirit.Map, 1f);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ghost, comp.bondedSpirit.DrawPos, comp.bondedSpirit.Map, 1.3f, .25f, .1f, .45f, 0, Rand.Range(1f, 2f), 0, 0);
                            }                            
                            comp.bondedSpirit.Destroy(DestroyMode.Vanish);
                        }
                        this.spirit = TM_Action.SingleSpawnLoop(caster, tempPod, shiftPos, map, 5, false, false, caster.Faction, false);
                        Pawn animal = this.spirit as Pawn;
                        TM_Action.TrainAnimalFull(animal, caster);
                        HealthUtility.AdjustSeverity(animal, TorannMagicDefOf.TM_SpiritBondHD, -4f);
                        HealthUtility.AdjustSeverity(animal, TorannMagicDefOf.TM_SpiritBondHD, .5f + verVal);
                        if(animal.def == TorannMagicDefOf.TM_SpiritCrowR)
                        {
                            HealthUtility.AdjustSeverity(animal, TorannMagicDefOf.TM_BirdflightHD, .5f);
                        }
                        comp.bondedSpirit = animal;
                        CompAnimalController animalComp = animal.TryGetComp<CompAnimalController>();
                        if(animalComp != null)
                        {
                            animalComp.summonerPawn = caster;
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            Vector3 rndPos = this.spirit.DrawPos;
                            rndPos.x += Rand.Range(-.5f, .5f);
                            rndPos.z += Rand.Range(-.5f, .5f);
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Healing_Small, rndPos, map, Rand.Range(.6f, .8f), .1f, .05f, .05f, 0, 0, 0, Rand.Range(0, 360));
                            FleckMaker.ThrowSmoke(rndPos, map, Rand.Range(.8f, 1.2f));
                        }
                    }
                    catch
                    {
                        comp.Mana.CurLevel += comp.ActualManaCost(TorannMagicDefOf.TM_GuardianSpirit);
                        Log.Message("TM_Exception".Translate(
                                caster.LabelShort,
                                "Guardian Spirit"
                            ));
                    }
                }
                else
                {
                    Messages.Message("InvalidSummon".Translate(), MessageTypeDefOf.RejectInput);
                    comp.Mana.GainNeed(comp.ActualManaCost(TorannMagicDefOf.TM_GuardianSpirit));
                }
            }
            else
            {
                Messages.Message("InvalidSummon".Translate(), MessageTypeDefOf.RejectInput);
                comp.Mana.GainNeed(comp.ActualManaCost(TorannMagicDefOf.TM_GuardianSpirit));
            }
            return false;
        }
    }
}
