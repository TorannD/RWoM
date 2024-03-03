using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using RimWorld;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using AbilityUser;
using TorannMagic.Enchantment;
using System.Text;
using TorannMagic.TMDefs;
using TorannMagic.ModOptions;
using HarmonyLib;

namespace TorannMagic.Golems
{
    public static class TM_GolemUtility
    {

        public static List<TM_GolemDef> GolemTypes()
        {
            IEnumerable<TM_GolemDef> tmpGolemDefs = from def in DefDatabase<TM_GolemDef>.AllDefs
                                             where (def.upgrades != null && def.upgrades.Count > 0)
                                             select def;
            return tmpGolemDefs.ToList();            
        }

        private static List<ThingDef> workstationDefs;
        public static List<ThingDef> GolemWorkstation
        {
            get
            {
                if(workstationDefs == null)
                {
                    workstationDefs = new List<ThingDef>();
                    workstationDefs.Clear();
                    foreach(TM_GolemDef gd in GolemTypes())
                    {
                        workstationDefs.Add(gd.golemWorkstationDef);
                    }
                }
                return workstationDefs;
            }
        }

        private static List<ThingDef> golemRaceDefs;
        public static List<ThingDef> GolemPawns
        {
            get
            {
                if (golemRaceDefs == null)
                {
                    golemRaceDefs = new List<ThingDef>();
                    golemRaceDefs.Clear();
                    foreach (TM_GolemDef gd in GolemTypes())
                    {
                        golemRaceDefs.Add(gd.golemDef);
                    }
                }
                return golemRaceDefs;
            }
        }

        public static TM_GolemDef GetGolemDefFromThing(Thing thing)
        {
            IEnumerable<TM_GolemDef> tmpGolemDefs = from def in DefDatabase<TM_GolemDef>.AllDefs
                                                    where (true)
                                                    select def;
            foreach(TM_GolemDef t in tmpGolemDefs)
            {
                if(t.golemDef == thing.def)
                {
                    return t;
                }
                if(t.golemWorkstationDef == thing.def)
                {
                    return t;
                }
            }
            return null;
        }

        public static PawnKindDef GetStoneGolemKindDef(Thing thing)
        {
            PawnKindDef pkd = null;
            IEnumerable<TM_GolemDef> tmpGolemDefs = from def in DefDatabase<TM_GolemDef>.AllDefs
                                                    where (true)
                                                    select def;
            foreach (TM_GolemDef t in tmpGolemDefs)
            {
                if (t.golemWorkstationDef == thing.def)
                {
                    pkd = t.golemKindDef;
                }
            }
            if (thing.Stuff != null)
            {
                //Log.Message("stuff defname is " + thing.Stuff.defName);
                if (thing.Stuff.defName == "BlocksSandstone")
                {
                    pkd = TorannMagicDefOf.TM_SandstoneGolemK;
                }
                else if (thing.Stuff.defName == "BlocksGranite")
                {
                    pkd = TorannMagicDefOf.TM_GraniteGolemK;
                }
                else if (thing.Stuff.defName == "BlocksLimestone")
                {
                    pkd = TorannMagicDefOf.TM_LimestoneGolemK;
                }
                else if (thing.Stuff.defName == "BlocksSlate")
                {
                    pkd = TorannMagicDefOf.TM_SlateGolemK;
                }
                else if (thing.Stuff.defName == "BlocksMarble")
                {
                    pkd = TorannMagicDefOf.TM_MarbleGolemK;
                }
            }
            return pkd;
        }

        public static Action GetGolemMeleeAttackAction(Pawn pawn, LocalTargetInfo target, out string failStr)
        {
            failStr = "";
            Pawn target2;
            if (!pawn.Drafted)
            {
                failStr = "IsNotDraftedLower".Translate(pawn.LabelShort, pawn);
            }
            else if (target.IsValid && !pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
            {
                failStr = "NoPath".Translate();
            }
            else if (pawn.meleeVerbs.TryGetMeleeVerb(target.Thing) == null)
            {
                failStr = "Incapable".Translate();
            }
            else if (pawn == target.Thing)
            {
                failStr = "CannotAttackSelf".Translate();
            }
            else if ((target2 = (target.Thing as Pawn)) != null && (pawn.InSameExtraFaction(target2, ExtraFactionType.HomeFaction) || pawn.InSameExtraFaction(target2, ExtraFactionType.MiniFaction)))
            {
                failStr = "CannotAttackSameFactionMember".Translate();
            }
            else
            {
                Pawn pawn2;
                if ((pawn2 = (target.Thing as Pawn)) == null || !pawn2.RaceProps.Animal || !HistoryEventUtility.IsKillingInnocentAnimal(pawn, pawn2) || new HistoryEvent(HistoryEventDefOf.KilledInnocentAnimal, pawn.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo())
                {
                    return delegate
                    {
                        Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
                        Pawn pawn3 = target.Thing as Pawn;
                        pawn.TryGetComp<CompGolem>().threatTarget = target.Thing;
                        if (pawn3 != null)
                        {
                            job.killIncappedTarget = pawn3.Downed;
                        }
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    };
                }
                failStr = "IdeoligionForbids".Translate();
            }
            failStr = failStr.CapitalizeFirst();
            return null;
        }

        public static Action GetGolemRangedAttackAction(TMPawnGolem pawn, LocalTargetInfo target, out string failStr)
        {
            failStr = "";
            List<Verb> tmpVerbs = pawn.ValidRangedVerbs();
            if(tmpVerbs == null)
            {
                failStr = "TM_NoRangedAttack".Translate();
                return null;
            }
            if(tmpVerbs.Count <= 0)
            {
                failStr = "TM_NoRangedAttack".Translate();
                return null;
            }
            Verb primaryVerb = tmpVerbs.RandomElement();
            if(primaryVerb == null)
            {
                failStr = "TM_NoRangedAttack".Translate();
                return null;
            }
            Pawn target2;
            Pawn victim;
            if (!pawn.Drafted)
            {
                failStr = "IsNotDraftedLower".Translate(pawn.LabelShort, pawn);
            }
            else if (target.IsValid && !primaryVerb.CanHitTarget(target))
            {
                if (!pawn.Position.InHorDistOf(target.Cell, primaryVerb.verbProps.range))
                {
                    failStr = "OutOfRange".Translate();
                }
                float num = primaryVerb.verbProps.EffectiveMinRange(target, pawn);
                if ((float)pawn.Position.DistanceToSquared(target.Cell) < num * num)
                {
                    failStr = "TooClose".Translate();
                }
                else
                {
                    failStr = "CannotHitTarget".Translate();
                }
            }
            else if (pawn == target.Thing)
            {
                failStr = "CannotAttackSelf".Translate();
            }
            else if ((target2 = (target.Thing as Pawn)) != null && (pawn.InSameExtraFaction(target2, ExtraFactionType.HomeFaction) || pawn.InSameExtraFaction(target2, ExtraFactionType.MiniFaction)))
            {
                failStr = "CannotAttackSameFactionMember".Translate();
            }
            else if ((victim = (target.Thing as Pawn)) != null && HistoryEventUtility.IsKillingInnocentAnimal(pawn, victim) && !new HistoryEvent(HistoryEventDefOf.KilledInnocentAnimal, pawn.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo())
            {
                failStr = "IdeoligionForbids".Translate();
            }
            else
            {
                Pawn pawn2;
                if ((pawn2 = (target.Thing as Pawn)) == null || pawn.Ideo == null || !pawn.Ideo.IsVeneratedAnimal(pawn2) || new HistoryEvent(HistoryEventDefOf.HuntedVeneratedAnimal, pawn.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo())
                {
                    return delegate
                    {
                        Job job = CreateRangedJob(pawn, target, primaryVerb);
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    };
                }
                failStr = "IdeoligionForbids".Translate();
            }
            failStr = failStr.CapitalizeFirst();
            return null;
        }

        public static Job CreateRangedJob(TMPawnGolem pawn, LocalTargetInfo target, Verb attackVerb)
        {
            Job job = JobMaker.MakeJob(TorannMagicDefOf.JobDriver_GolemAttackStatic, target);
            pawn.activeVerb = attackVerb;
            pawn.TryGetComp<CompGolem>().threatTarget = target.Thing;
            return job;
        }

        public static string ShouldAwkenString(float pct)
        {
            if (pct < .1f)
            {
                return "Never";
            }
            return pct.ToString("P2");
        }

        public static void InitializeWorksettings(TMPawnGolem pg)
        {
            pg.workSettings = new Pawn_WorkSettings();            
            if (pg.GolemDef.golemWorkTypes == null) return;
            if (pg.GolemDef.golemWorkTypes.Count <= 0) return;
            if(pg.skills == null) pg.skills = new Pawn_SkillTracker(pg);

            DefMap<WorkTypeDef, int> golemPriorities = Traverse.Create(pg.workSettings).Field(name: "priorities").GetValue<DefMap<WorkTypeDef, int>>();
            if(golemPriorities == null)
            {
                golemPriorities = new DefMap<WorkTypeDef, int>();
            }
            golemPriorities.SetAll(0);
            int num = 0;
            foreach (WorkTypeDef item in DefDatabase<WorkTypeDef>.AllDefs.Where(delegate (WorkTypeDef w)
            {
                if (!w.alwaysStartActive)
                {
                    return !pg.WorkTypeIsDisabled(w);
                }
                return false;
            }).OrderByDescending(delegate (WorkTypeDef w)
            {
                if (pg.skills == null)
                {
                    return 1f;
                }
                return pg.skills.AverageOfRelevantSkillsFor(w);
            }))
            {
                golemPriorities[item] = 3;
                num++;
                if (num >= 6)
                {
                    break;
                }
            }
            foreach (WorkTypeDef item2 in from w in DefDatabase<WorkTypeDef>.AllDefs
                                          where w.alwaysStartActive
                                          select w)
            {
                if (!pg.WorkTypeIsDisabled(item2))
                {
                    golemPriorities[item2] = 3;
                }
            }
            List<WorkTypeDef> disabledWorkTypes = pg.GetDisabledWorkTypes();
            for (int j = 0; j < disabledWorkTypes.Count; j++)
            {
                golemPriorities[j] = 0;
            }




            //DefMap<WorkTypeDef, int> golemPriorities = new DefMap<WorkTypeDef, int>();
            //golemPriorities.SetAll(0);

            //IEnumerable<WorkTypeDef> wtds = from def in DefDatabase<WorkTypeDef>.AllDefs
            //                                where (true)
            //                                select def;

            //foreach (WorkTypeDef wtd in wtds)
            //{
            //    golemPriorities[wtd] = 0;
            //}

            //foreach (TM_GolemDef.GolemWorkTypes gwt in pg.GolemDef.golemWorkTypes)
            //{
            //    golemPriorities[gwt.workTypeDef] = gwt.priority;
            //}

            //Traverse.Create(pg.workSettings).Field(name: "priorities").SetValue(golemPriorities);
            //pg.workSettings.EnableAndInitialize();

            //try
            //{
            //    pg.workSettings.EnableAndInitialize();
            //}
            //catch (NullReferenceException ex)
            //{
            //    Log.Message("failed to initialize work settings for " + pg.Golem.GolemName);
            //    return;
            //}

            //foreach (WorkTypeDef wtd in wtds)
            //{
            //    pg.workSettings.SetPriority(wtd, 0);
            //}

            //foreach (TM_GolemDef.GolemWorkTypes gwt in pg.GolemDef.golemWorkTypes)
            //{
            //    pg.workSettings.SetPriority(gwt.workTypeDef, gwt.priority);
            //}

            //testing/checking
            //foreach (WorkTypeDef wtd in DefDatabase<WorkTypeDef>.AllDefs)
            //{
            //    Log.Message("golem work setting " + wtd.defName + " has priority of " + golemPriorities[wtd]);
            //}
            Traverse.Create(pg.workSettings).Field(name: "priorities").SetValue(golemPriorities);
            //Log.Message("trying to get priority for work type of growing " + pg.workSettings.GetPriority(WorkTypeDefOf.Growing));
        }

        public static void UpdateWorkSkills(TMPawnGolem pg)
        {
            if (pg.skills == null) pg.skills = new Pawn_SkillTracker(pg);
            //Log.Message("trying to get priority for work type of growing " + pg.workSettings.GetPriority(WorkTypeDefOf.Growing) + " + with skill " + pg.skills.GetSkill(SkillDefOf.Plants).Level);
        }
    }
}
