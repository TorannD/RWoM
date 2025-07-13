using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TorannMagic.Golems;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic
{
    public partial class TorannMagicMod
    {
        [HarmonyPatch(typeof(CompPowerBattery), "DrawPower", null)]
        public class DrawPower_Patch
        {
            private static void Postfix(CompPowerBattery __instance, float amount)
            {
                if(__instance.parent != null && __instance.parent is Building_TMGolemBase)
                {
                    Building_TMGolemBase gb = __instance.parent as Building_TMGolemBase;
                    gb.Energy.DrawPowerNew(amount);
                }
            }
        }

        public static void Get_GolemsRecipeAvailable(RecipeDef __instance, ref bool __result)
        {
            if(__result)
            {
                Thing t = Find.Selector.SingleSelectedThing;
                if(t is Building_TMGolemBase)
                {
                    Building_TMGolemBase gb = t as Building_TMGolemBase;
                    foreach(TM_GolemUpgrade gu in gb.Upgrades)
                    {
                        if(gu.golemUpgradeDef.upgradeEnablesRecipes != null && gu.golemUpgradeDef.upgradeEnablesRecipes.Count > 0 && gu.golemUpgradeDef.upgradeEnablesRecipes.Contains(__instance))
                        {
                            if (gu.currentLevel == 0)
                            {
                                __result = false;
                            }
                        }
                        if(gu.golemUpgradeDef.recipe != null && gu.golemUpgradeDef.maxLevel > 0 && gu.golemUpgradeDef.recipe == __instance && gu.currentLevel == gu.golemUpgradeDef.maxLevel)
                        {
                            __result = false;
                        }
                    }
                }
            }
        }
        
        
        public static void Get_IsFreeNonSlaveColonist_Golem(Pawn __instance, ref bool __result)
        {
            if (__instance is TMPawnGolem)
            {
                try
                {
                    StackFrame sf = (new System.Diagnostics.StackTrace()).GetFrame(2);
                    if (sf != null && sf.ToString().StartsWith("<AddPawnsSections>"))
                    {
                        __result = true;
                    }
                }
                catch { }
            }
        }
        
        [HarmonyPatch(typeof(Pawn_JobTracker), "ShouldStartJobFromThinkTree", null)]
        public class GolemAbilityJob_Patch
        {
            private static void Postfix(Pawn_JobTracker __instance,  ref bool __result)
            {
                if(__result && __instance != null && __instance.curJob != null && __instance.curJob.def == TorannMagicDefOf.JobDriver_GolemAbilityJob)
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(StatWorker), "IsDisabledFor", null)]
        public class GolemStatWorker_Patch
        {
            private static bool Prefix(StatWorker __instance, Thing thing, StatDef ___stat, ref bool __result)
            {
                Pawn p = thing as Pawn;
                if(p != null && (p is TMPawnGolem || p is TMHollowGolem))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "CanTakeOrder", null)]
        public class GolemOrders_Patch
        {
            public static bool Prefix(Pawn pawn, ref bool __result)
            {
                if ((pawn is TMPawnGolem) && pawn.Faction == Faction.OfPlayerSilentFail)
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddUndraftedOrders", null)]
        public class GolemUndraftedOrder_Patch
        {
            public static bool Prefix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
            {
                if(pawn is TMPawnGolem || TM_Calc.IsPolymorphed(pawn))
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddDraftedOrders", null)]
        public class GolemMenu_Patch
        {
            public static bool Prefix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts, bool suppressAutoTakeableGoto = false)
            {
                if (pawn is TMPawnGolem || (TM_Calc.IsPossessedBySpirit(pawn) && !pawn.RaceProps.Humanlike) || TM_Calc.IsPolymorphed(pawn))
                {
                    IntVec3 clickCell = IntVec3.FromVector3(clickPos);
                    if (pawn is TMPawnGolem)
                    {                        
                        foreach (LocalTargetInfo item6 in GenUI.TargetsAt(clickPos, TargetingParameters.ForAttackAny(), thingsOnly: true))
                        {
                            LocalTargetInfo attackTarg = item6;
                            if (pawn.VerbTracker.AllVerbs != null && pawn.VerbTracker.AllVerbs.Count > 0)
                            {
                                string failStr;
                                Action rangedAct = TM_GolemUtility.GetGolemRangedAttackAction(pawn as TMPawnGolem, attackTarg, out failStr);
                                string text = "FireAt".Translate(attackTarg.Thing.Label, attackTarg.Thing);
                                FloatMenuOption floatMenuOption = new FloatMenuOption("", null, MenuOptionPriority.High, null, item6.Thing);
                                if (rangedAct == null)
                                {
                                    text = text + ": " + failStr;
                                }
                                else
                                {
                                    floatMenuOption.autoTakeable = (!attackTarg.HasThing || attackTarg.Thing.HostileTo(Faction.OfPlayer));
                                    floatMenuOption.autoTakeablePriority = 40f;
                                    floatMenuOption.action = delegate
                                    {
                                        FleckMaker.Static(attackTarg.Thing.DrawPos, attackTarg.Thing.Map, FleckDefOf.FeedbackShoot);
                                        rangedAct();
                                    };
                                }
                                floatMenuOption.Label = text;
                                opts.Add(floatMenuOption);
                            }
                            string failStr2;
                            Action meleeAct = TM_GolemUtility.GetGolemMeleeAttackAction(pawn, attackTarg, out failStr2);
                            Pawn pawn2 = attackTarg.Thing as Pawn;
                            string text2 = (pawn2 == null || !pawn2.Downed) ? ((string)"MeleeAttack".Translate(attackTarg.Thing.Label, attackTarg.Thing)) : ((string)"MeleeAttackToDeath".Translate(attackTarg.Thing.Label, attackTarg.Thing));
                            MenuOptionPriority priority = (!attackTarg.HasThing || !pawn.HostileTo(attackTarg.Thing)) ? MenuOptionPriority.VeryLow : MenuOptionPriority.AttackEnemy;
                            FloatMenuOption floatMenuOption2 = new FloatMenuOption("", null, priority, null, attackTarg.Thing);
                            if (meleeAct == null)
                            {
                                text2 = text2 + ": " + failStr2.CapitalizeFirst();
                            }
                            else
                            {
                                floatMenuOption2.autoTakeable = (!attackTarg.HasThing || attackTarg.Thing.HostileTo(Faction.OfPlayer));
                                floatMenuOption2.autoTakeablePriority = 30f;
                                floatMenuOption2.action = delegate
                                {
                                    FleckMaker.Static(attackTarg.Thing.DrawPos, attackTarg.Thing.Map, FleckDefOf.FeedbackMelee);
                                    meleeAct();
                                };
                            }
                            floatMenuOption2.Label = text2;
                            opts.Add(floatMenuOption2);
                        }
                    }
                    else if ((TM_Calc.IsPossessedBySpirit(pawn) || TM_Calc.IsPolymorphed(pawn)) && pawn.RaceProps.Animal)
                    {
                        foreach (LocalTargetInfo item6 in GenUI.TargetsAt(clickPos, TargetingParameters.ForAttackAny(), thingsOnly: true))
                        {
                            LocalTargetInfo attackTarg = item6;                            
                            string failStr2;
                            Action meleeAct = FloatMenuUtility.GetMeleeAttackAction(pawn, attackTarg, out failStr2);
                            Pawn pawn2 = attackTarg.Thing as Pawn;
                            string text2 = (pawn2 == null || !pawn2.Downed) ? ((string)"MeleeAttack".Translate(attackTarg.Thing.Label, attackTarg.Thing)) : ((string)"MeleeAttackToDeath".Translate(attackTarg.Thing.Label, attackTarg.Thing));
                            MenuOptionPriority priority = (!attackTarg.HasThing || !pawn.HostileTo(attackTarg.Thing)) ? MenuOptionPriority.VeryLow : MenuOptionPriority.AttackEnemy;
                            FloatMenuOption floatMenuOption2 = new FloatMenuOption("", null, priority, null, attackTarg.Thing);
                            if (meleeAct == null)
                            {
                                text2 = text2 + ": " + failStr2.CapitalizeFirst();
                            }
                            else
                            {
                                floatMenuOption2.autoTakeable = (!attackTarg.HasThing || attackTarg.Thing.HostileTo(Faction.OfPlayer));
                                floatMenuOption2.autoTakeablePriority = 30f;
                                floatMenuOption2.action = delegate
                                {
                                    FleckMaker.Static(attackTarg.Thing.DrawPos, attackTarg.Thing.Map, FleckDefOf.FeedbackMelee);
                                    meleeAct();
                                };
                            }
                            floatMenuOption2.Label = text2;
                            opts.Add(floatMenuOption2);
                        }
                    }
                    if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    {
                        TargetingParameters Params = TargetingParameters.ForColonist();
                        Params.targetSpecificThing = pawn;
                        foreach (LocalTargetInfo item7 in GenUI.TargetsAt(clickPos, Params, thingsOnly: true))
                        {
                            LocalTargetInfo carryTarget = item7;
                            FloatMenuOption item = pawn.CanReach(carryTarget, PathEndMode.ClosestTouch, Danger.Deadly) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Carry".Translate(carryTarget.Thing), delegate
                            {
                                carryTarget.Thing.SetForbidden(value: false, warnOnFail: false);
                                Job job7 = JobMaker.MakeJob(JobDefOf.CarryDownedPawnDrafted, carryTarget);
                                job7.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job7, JobTag.Misc);
                            }), pawn, carryTarget) : new FloatMenuOption("CannotCarry".Translate(carryTarget.Thing) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                            opts.Add(item);
                        }
                    }
                    if (pawn.IsCarryingPawn())
                    {
                        Pawn carriedPawn = (Pawn)pawn.carryTracker.CarriedThing;
                        TargetingParameters Params = TargetingParameters.ForPawns();
                        Params.targetSpecificThing = pawn;
                        if (!carriedPawn.IsPrisonerOfColony)
                        {
                            
                            foreach (LocalTargetInfo item8 in GenUI.TargetsAt(clickPos, Params, thingsOnly: true))
                            {
                                LocalTargetInfo destTarget = item8;
                                FloatMenuOption item2 = pawn.CanReach(destTarget, PathEndMode.ClosestTouch, Danger.Deadly) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PlaceIn".Translate(carriedPawn, destTarget.Thing), delegate
                                {
                                    destTarget.Thing.SetForbidden(value: false, warnOnFail: false);
                                    Job job6 = JobMaker.MakeJob(JobDefOf.TakeDownedPawnToBedDrafted, pawn.carryTracker.CarriedThing, destTarget);
                                    job6.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job6, JobTag.Misc);
                                }), pawn, destTarget) : new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, destTarget.Thing) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                                opts.Add(item2);
                            }
                        }
                        foreach (LocalTargetInfo item9 in GenUI.TargetsAt(clickPos, Params, thingsOnly: true))
                        {
                            LocalTargetInfo destTarget2 = item9;
                            FloatMenuOption item3;
                            if (!pawn.CanReach(destTarget2, PathEndMode.ClosestTouch, Danger.Deadly))
                            {
                                item3 = new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, destTarget2.Thing) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                            }
                            else
                            {
                                TaggedString taggedString = "PlaceIn".Translate(carriedPawn, destTarget2.Thing);
                                if (!carriedPawn.IsPrisonerOfColony)
                                {
                                    taggedString += ": " + "ArrestChance".Translate(carriedPawn.GetAcceptArrestChance(pawn).ToStringPercent());
                                }
                                item3 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString, delegate
                                {
                                    destTarget2.Thing.SetForbidden(value: false, warnOnFail: false);
                                    Job job5 = JobMaker.MakeJob(JobDefOf.CarryToPrisonerBedDrafted, pawn.carryTracker.CarriedThing, destTarget2);
                                    job5.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job5, JobTag.Misc);
                                }), pawn, destTarget2);
                            }
                            opts.Add(item3);
                        }
                        foreach (LocalTargetInfo item10 in GenUI.TargetsAt(clickPos, Params, thingsOnly: true))
                        {
                            Thing transporterThing = item10.Thing;
                            if (transporterThing != null)
                            {
                                CompTransporter compTransporter = transporterThing.TryGetComp<CompTransporter>();
                                if (compTransporter.Shuttle == null || compTransporter.Shuttle.IsAllowedNow(carriedPawn))
                                {
                                    if (!pawn.CanReach(transporterThing, PathEndMode.ClosestTouch, Danger.Deadly))
                                    {
                                        opts.Add(new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, transporterThing) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                                    }
                                    else if (compTransporter.Shuttle == null && !compTransporter.LeftToLoadContains(carriedPawn))
                                    {
                                        opts.Add(new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, transporterThing) + ": " + "NotPartOfLaunchGroup".Translate(), null));
                                    }
                                    else
                                    {
                                        string label = "PlaceIn".Translate(carriedPawn, transporterThing);
                                        Action action = delegate
                                        {
                                            if (!compTransporter.LoadingInProgressOrReadyToLaunch)
                                            {
                                                TransporterUtility.InitiateLoading(Gen.YieldSingle(compTransporter));
                                            }
                                            Job job4 = JobMaker.MakeJob(JobDefOf.HaulToTransporter, carriedPawn, transporterThing);
                                            job4.ignoreForbidden = true;
                                            job4.count = 1;
                                            pawn.jobs.TryTakeOrderedJob(job4, JobTag.Misc);
                                        };
                                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, action), pawn, transporterThing));
                                    }
                                }
                            }
                        }
                        foreach (LocalTargetInfo item11 in GenUI.TargetsAt(clickPos, Params, thingsOnly: true))
                        {
                            Thing casket = item11.Thing;
                            TaggedString taggedString2 = "PlaceIn".Translate(carriedPawn, casket);
                            if (((Building_CryptosleepCasket)casket).HasAnyContents)
                            {
                                opts.Add(new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, casket) + ": " + "CryptosleepCasketOccupied".Translate(), null));
                            }
                            else if (carriedPawn.IsQuestLodger())
                            {
                                opts.Add(new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, casket) + ": " + "CryptosleepCasketGuestsNotAllowed".Translate(), null));
                            }
                            else if (carriedPawn.GetExtraHostFaction() != null)
                            {
                                opts.Add(new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, casket) + ": " + "CryptosleepCasketGuestPrisonersNotAllowed".Translate(), null));
                            }
                            else
                            {
                                Action action2 = delegate
                                {
                                    Job job3 = JobMaker.MakeJob(JobDefOf.CarryToCryptosleepCasketDrafted, carriedPawn, casket);
                                    job3.count = 1;
                                    job3.playerForced = true;
                                    pawn.jobs.TryTakeOrderedJob(job3, JobTag.Misc);
                                };
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString2, action2), pawn, casket));
                            }
                        }
                    }
                    FloatMenuOption floatMenuOption3 = GolemUtility.GotoLocationOption(clickCell, pawn, suppressAutoTakeableGoto);
                    if (floatMenuOption3 != null)
                    {
                        opts.Add(floatMenuOption3);
                    }
                    return false;                    
                }
                return true;                
            }
        }

        [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts", null)]
        public class GolemRecipe_Action_Patch
        {
            public static bool Prefix(RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Thing dominantIngredient, IBillGiver billGiver, ref IEnumerable<Thing> __result)
            {
                Building_TMGolemBase golem_building = billGiver as Building_TMGolemBase;
                if (golem_building != null && golem_building.GolemComp != null && golem_building.Upgrades != null && golem_building.Upgrades.Count > 0)
                {
                    if (golem_building.IsUpgrade(recipeDef))
                    {
                        if (golem_building.CanUpgrade(recipeDef))
                        {
                            golem_building.IncreaseUpgrade_Recipe(recipeDef);
                        }
                        else
                        {
                            Messages.Message("TM_MaxGolemUpgradeReached".Translate(recipeDef.label), MessageTypeDefOf.RejectInput);
                            foreach (Thing ing in ingredients)
                            {
                                Thing t = ThingMaker.MakeThing(ing.def, null);
                                t.stackCount = ing.stackCount;
                                GenPlace.TryPlaceThing(t, golem_building.InteractionCell, golem_building.Map, ThingPlaceMode.Near);
                            }
                        }
                    }
                    else
                    {
                        golem_building.ApplyProductEffects(recipeDef, ingredients);
                    }                    
                }
                return true;
            }
        }

        public static void Get_GolemsAsAnimals(MainTabWindow_Animals __instance, ref IEnumerable<Pawn> __result)
        {
            IEnumerable<Pawn> Golems = from p in Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
                                       where p is TMPawnGolem
                                       select p;
            if (Golems != null)
            {
                __result.ToList().AddRange(Golems);
            }
        }
        
        public static void GolemVerb_AdjustedCooldown_Postfix(VerbProperties __instance, Pawn attacker, ref float __result)
        {
            if (attacker is TMPawnGolem && __instance != null && __instance.range >= 2 && __instance.defaultProjectile != null)
            {
                __result = .1f;
            }
        }
        
        [HarmonyPatch(typeof(PlayerPawnsDisplayOrderUtility), "Sort", null)]
        public class GolemColonistBarInjection_Patch
        {
            public static void Postfix(ref List<Pawn> pawns) 
            {
                if (ModOptions.Settings.Instance.showGolemsOnColonistBar)
                {
                    List<Map> maps = Find.Maps;
                    foreach (Map m in maps)
                    {
                        List<Pawn> mapPawns = m.mapPawns.AllPawnsSpawned.ToList();
                        foreach (Pawn p in mapPawns)
                        {                            
                            TMPawnGolem pg = p as TMPawnGolem;
                            if (pg != null && pg.Faction.IsPlayer && pawns != null && !pawns.Contains(pg) && pawns.Count > 0 && pawns[0].Map == pg.Map)
                            {
                                pawns.Add(pg);                               
                            }                            
                        }
                    }
                }
            }
        }
    }
}