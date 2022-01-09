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
    }
}
