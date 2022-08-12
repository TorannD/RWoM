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
    public class Verb_SummonSpiritWolves : Verb_UseAbility
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
        List<IntVec3> cellList = new List<IntVec3>();

        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Map map = caster.Map;
            cellList.Clear();
            List<IntVec3> tmpList = GenRadial.RadialCellsAround(currentTarget.Cell, this.Projectile.projectile.explosionRadius, true).ToList();
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            //pwrVal = TM_Calc.GetMagicSkillLevel(caster, comp.MagicData.MagicPowerSkill_SpiritWolves, "TM_SpiritWolves", "_pwr", true);
            //verVal = TM_Calc.GetMagicSkillLevel(caster, comp.MagicData.MagicPowerSkill_SpiritWolves, "TM_SpiritWolves", "_ver", true);
            //effVal = TM_Calc.GetMagicSkillLevel(caster, comp.MagicData.MagicPowerSkill_SpiritWolves, "TM_SpiritWolves", "_eff", true);
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
            effVal = TM_Calc.GetSkillEfficiencyLevel(caster, this.Ability.Def as TMAbilityDef);
            if (tmpList != null && tmpList.Count > 0)
            {
                foreach (IntVec3 c in tmpList)
                {
                    if (c != null && (c.IsValid && c.Standable(map) && c.InBoundsWithNullCheck(map)))
                    {
                        cellList.Add(c);
                    }
                }
                int summonCount = 5 + verVal;
                for (int i = 0; i < summonCount; i++)
                {
                    FleckMaker.ThrowSmoke(cellList.RandomElement().ToVector3Shifted(), map, Rand.Range(3f, 4f));
                    IntVec3 cell = cellList.RandomElement();

                    AbilityUser.SpawnThings tempPod = new SpawnThings();
                    tempPod.def = TorannMagicDefOf.TM_SpiritWolfR;
                    tempPod.kindDef = TorannMagicDefOf.TM_SpiritWolf;
                    tempPod.spawnCount = 1;
                    tempPod.temporary = true;
                    
                    Thing newPawn = null;
                    newPawn = TM_Action.SingleSpawnLoop(caster, tempPod, cell, map, Rand.Range(1000,1200) + (120 * effVal), true, false, caster.Faction, false);
                    Pawn animal = newPawn as Pawn;
                    HealthUtility.AdjustSeverity(animal, TorannMagicDefOf.TM_EnrageHD, .2f + (.1f * pwrVal));
                    for (int j = 0; j < 3; j++)
                    {
                        FleckMaker.ThrowSmoke(animal.DrawPos, map, Rand.Range(.5f, 1.1f));
                    }
                    Pawn enemy = TM_Calc.FindNearbyEnemy(animal, 30);
                    if (enemy != null)
                    {
                        Job defendJob = new Job(JobDefOf.AttackMelee, enemy);
                        animal.jobs.TryTakeOrderedJob(defendJob);
                    }
                }
            }
            else
            {
                Messages.Message("InvalidSummon".Translate(), MessageTypeDefOf.RejectInput);
                comp.Mana.GainNeed(comp.ActualManaCost(TorannMagicDefOf.TM_SpiritWolves));
            }      
            return false;
        }
    }
}
