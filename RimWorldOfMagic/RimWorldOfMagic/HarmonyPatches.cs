using HarmonyLib;
using RimWorld;
using AbilityUser;
using RimWorld.Planet;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.AI;
using AbilityUserAI;
using TorannMagic.Conditions;
using TorannMagic.TMDefs;
using TorannMagic.Golems;
using RimWorld.QuestGen;
using System.Diagnostics;

namespace TorannMagic
{
    //[StaticConstructorOnStartup]
    //internal class HarmonyPatches
    //{
    //    private static readonly Type patchType = typeof(HarmonyPatches);

    //    static HarmonyPatches()
    //    {

    //*notes, replacing HarmonyPatches with TorannMagicMod
    public class TorannMagicMod : Mod
    {
        private static readonly Type patchType = typeof(TorannMagicMod);

        public TorannMagicMod(ModContentPack content) : base(content)
        {
            var harmonyInstance = new Harmony("rimworld.torann.tmagic");

            harmonyInstance.Patch(AccessTools.Method(typeof(IncidentWorker_SelfTame), "Candidates"), null,
                 new HarmonyMethod(patchType, nameof(SelfTame_Candidates_Patch)), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(IncidentWorker_DiseaseHuman), "PotentialVictimCandidates"), null,
                 new HarmonyMethod(patchType, nameof(DiseaseHuman_Candidates_Patch)), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(IncidentWorker_DiseaseAnimal), "PotentialVictimCandidates"), null,  //calls the same patch as human, which includes hediff for undead animals
                 new HarmonyMethod(patchType, nameof(DiseaseHuman_Candidates_Patch)), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(Pawn), "GetGizmos"), null,
                 new HarmonyMethod(patchType, nameof(Pawn_Gizmo_TogglePatch)), null);
            //harmonyInstance.Patch(AccessTools.Method(typeof(Pawn), "GetGizmos"), null,
            //     new HarmonyMethod(patchType, nameof(Pawn_Gizmo_ActionPatch)), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(CompPowerPlant), "CompTick"), null,
                 new HarmonyMethod(patchType, nameof(PowerCompTick_Overdrive_Postfix)), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(Building_TurretGun), "Tick"), null,
                 new HarmonyMethod(patchType, nameof(TurretGunTick_Overdrive_Postfix)), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(CompRefuelable), "PostDraw"), new HarmonyMethod(patchType, nameof(CompRefuelable_DrawBar_Prefix)),
                 null, null);
            harmonyInstance.Patch(AccessTools.Method(typeof(AutoUndrafter), "ShouldAutoUndraft"), new HarmonyMethod(patchType, nameof(AutoUndrafter_Undead_Prefix)),
                 null, null);
            harmonyInstance.Patch(AccessTools.Method(typeof(PawnUtility), "IsTravelingInTransportPodWorldObject"),
                 new HarmonyMethod(patchType, nameof(IsTravelingInTeleportPod_Prefix)), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), null,
                 new HarmonyMethod(patchType, nameof(AddHumanLikeOrders_RestrictEquipmentPatch)), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(CompAbilityItem), "PostDrawExtraSelectionOverlays"), new HarmonyMethod(patchType, nameof(CompAbilityItem_Overlay_Prefix)),
                 null, null);
            harmonyInstance.Patch(AccessTools.Method(typeof(Verb), "CanHitCellFromCellIgnoringRange"), new HarmonyMethod(patchType, nameof(RimmuNation_CHCFCIR_Patch)),
                 null, null);
            harmonyInstance.Patch(AccessTools.Method(typeof(WealthWatcher), "ForceRecount"), null,
                 new HarmonyMethod(patchType, nameof(WealthWatcher_ClassAdjustment_Postfix)), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), "TryDropEquipment"), null,
                 new HarmonyMethod(patchType, nameof(PawnEquipment_Drop_Postfix)), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), "AddEquipment"), null,
                 new HarmonyMethod(patchType, nameof(PawnEquipment_Add_Postfix)), null);
            //harmonyInstance.Patch(AccessTools.Method(typeof(TraitDef), "ConflictsWith", new Type[]
            //    {
            //        typeof(TraitDef)
            //    }), null, new HarmonyMethod(patchType, nameof(TraitConflicts_TM_Collection_Postfix)), null);
            //harmonyInstance.Patch(AccessTools.Method(typeof(Pawn_ApparelTracker), "Notify_ApparelAdded"), null, new HarmonyMethod(typeof(TorannMagicMod), "Notify_ApparelAdded_PostFix"));
            //harmonyInstance.Patch(AccessTools.Method(typeof(Pawn_ApparelTracker), "Notify_ApparelRemoved"), null, new HarmonyMethod(typeof(TorannMagicMod), "Notify_ApparelRemoved_PostFix"));

            harmonyInstance.Patch(AccessTools.Method(typeof(Pawn), "get_IsColonist", null, null), new HarmonyMethod(typeof(TorannMagicMod), "Get_IsColonist_Polymorphed", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(Caravan), "get_NightResting", null, null), new HarmonyMethod(typeof(TorannMagicMod), "Get_NightResting_Undead", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(Pawn_StanceTracker), "get_Staggered", null, null), new HarmonyMethod(typeof(TorannMagicMod), "Get_Staggered", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(Verb_LaunchProjectile), "get_Projectile", null, null), new HarmonyMethod(typeof(TorannMagicMod), "Get_Projectile_ES", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(WindManager), "get_WindSpeed", null, null), new HarmonyMethod(typeof(TorannMagicMod), "Get_WindSpeed", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(MentalBreaker), "get_CanDoRandomMentalBreaks", null, null), null, new HarmonyMethod(typeof(TorannMagicMod), "Get_CanDoRandomMentalBreaks", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(Pawn), "get_IsFreeNonSlaveColonist", null, null), null, new HarmonyMethod(typeof(TorannMagicMod), "Get_IsFreeNonSlaveColonist_Golem", null));
            //harmonyInstance.Patch(AccessTools.Method(typeof(RaceProperties), "get_Humanlike", null, null), new HarmonyMethod(typeof(TorannMagicMod), "Get_Humanlike_Golem", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(MainTabWindow_Animals), "get_Pawns", null, null), null, new HarmonyMethod(typeof(TorannMagicMod), "Get_GolemsAsAnimals", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(RecipeDef), "get_AvailableNow", null, null), null, new HarmonyMethod(typeof(TorannMagicMod), "Get_GolemsRecipeAvailable", null), null);

            harmonyInstance.Patch(AccessTools.Method(typeof(GenDraw), "DrawRadiusRing", new Type[]
                {
                    typeof(IntVec3),
                    typeof(float),
                    typeof(Color),
                    typeof(Func<IntVec3, bool>)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "DrawRadiusRing_Patch"), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new Type[]
                {
                    typeof(Pawn),
                    typeof(IntVec3)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "Pawn_PathFollower_Pathfinder_Prefix", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(Pawn_StanceTracker), "StaggerFor", new Type[]
                {
                    typeof(int)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "StaggerFor_Patch", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(MemoryThoughtHandler), "TryGainMemory", new Type[]
                {
                    typeof(ThoughtDef),
                    typeof(Pawn),
                    typeof(Precept)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "MemoryThoughtHandler_PreventDisturbedRest_Prefix", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal", new Type[]
                {
                    typeof(Vector3),
                    typeof(float),
                    typeof(bool),
                    typeof(Rot4),
                    typeof(RotDrawMode),
                    typeof(PawnRenderFlags)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "PawnRenderer_UndeadInternal_Prefix", null), null);
            //harmonyInstance.Patch(AccessTools.Method(typeof(PawnRenderer), "DrawPawnBody", new Type[]
            //    {
            //        typeof(Vector3),
            //        typeof(float),
            //        typeof(Rot4),
            //        typeof(RotDrawMode),
            //        typeof(PawnRenderFlags),
            //        typeof(Mesh)
            //    }, null), new HarmonyMethod(typeof(TorannMagicMod), "PawnRenderer_Undead_Prefix", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(PawnRenderer), "RenderPawnAt", new Type[]
                {
                    typeof(Vector3),
                    typeof(Rot4?),
                    typeof(bool)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "PawnRenderer_Blur_Prefix", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(PawnDiedOrDownedThoughtsUtility), "AppendThoughts_Relations", new Type[]
                {
                    typeof(Pawn),
                    typeof(DamageInfo?),
                    typeof(PawnDiedOrDownedThoughtsKind),
                    typeof(List<IndividualThoughtToAdd>),
                    typeof(List<ThoughtToAddToAll>)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "AppendThoughts_Relations_PrefixPatch", null), null, null);
            harmonyInstance.Patch(AccessTools.Method(typeof(PawnDiedOrDownedThoughtsUtility), "AppendThoughts_ForHumanlike", new Type[]
                {
                    typeof(Pawn),
                    typeof(DamageInfo?),
                    typeof(PawnDiedOrDownedThoughtsKind),
                    typeof(List<IndividualThoughtToAdd>),
                    typeof(List<ThoughtToAddToAll>)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "AppendThoughts_ForHumanlike_PrefixPatch", null), null, null);
            harmonyInstance.Patch(AccessTools.Method(typeof(PawnDiedOrDownedThoughtsUtility), "TryGiveThoughts", new Type[]
                {
                    typeof(Pawn),
                    typeof(DamageInfo?),
                    typeof(PawnDiedOrDownedThoughtsKind)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "TryGiveThoughts_PrefixPatch", null), null, null);
            //harmonyInstance.Patch(AccessTools.Method(typeof(DaysWorthOfFoodCalculator), "ApproxDaysWorthOfFood", new Type[]
            //    {
            //        typeof(List<Pawn>),
            //        typeof(List<ThingDefCount>),
            //        typeof(int),
            //        typeof(IgnorePawnsInventoryMode),
            //        typeof(Faction),
            //        typeof(WorldPath),
            //        typeof(float),
            //        typeof(int),
            //        typeof(bool)
            //    }, null), null, new HarmonyMethod(typeof(TorannMagicMod), "DaysWorthOfFoodCalc_Undead_Postfix", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(Targeter), "TargeterOnGUI", new Type[]
                {
                }, null), null, new HarmonyMethod(typeof(TorannMagicMod), "Targeter_Casting_Postfix", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(QuestPart_LendColonistsToFaction), "Enable", new Type[]
                {
                    typeof(SignalArgs)
                }, null), null, new HarmonyMethod(typeof(TorannMagicMod), "QuestPart_LendColonists_Enable_NoUndead"));
            harmonyInstance.Patch(AccessTools.Method(typeof(HealthUtility), "AdjustSeverity", new Type[]
                {
                    typeof(Pawn),
                    typeof(HediffDef),
                    typeof(float)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "HealthUtility_HeatCold_HediffGiverForUndead"), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(ThingFilter), "SetFromPreset", new Type[]
                {
                    typeof(StorageSettingsPreset)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "DefaultStorageSettings_IncludeMagicItems"), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(AreaManager), "AddStartingAreas", new Type[]
                {
                }, null), null, new HarmonyMethod(typeof(TorannMagicMod), "AreaManager_AddMagicZonesToStartingAreas"));
            harmonyInstance.Patch(AccessTools.Method(typeof(Projectile), "Launch", new Type[]
                {
                    typeof(Thing),
                    typeof(Vector3),
                    typeof(LocalTargetInfo),
                    typeof(LocalTargetInfo),
                    typeof(ProjectileHitFlags),
                    typeof(bool),
                    typeof(Thing),
                    typeof(ThingDef)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "Projectile_Launch_Prefix", null), null, null);
            harmonyInstance.Patch(AccessTools.Method(typeof(Verb), "TryStartCastOn", new Type[]
                {
                    typeof(LocalTargetInfo),
                    typeof(LocalTargetInfo),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool)
                }, null), null, new HarmonyMethod(typeof(TorannMagicMod), "TryStartCastOn_Prefix", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(GenGrid), "InBounds", new Type[]
                {
                    typeof(IntVec3),
                    typeof(Map)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "IntVec3Inbounds_NullCheck_Prefix", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(VerbProperties), "AdjustedCooldown", new Type[]
                {
                    typeof(Tool),
                    typeof(Pawn),
                    typeof(Thing)
                }, null), null, new HarmonyMethod(typeof(TorannMagicMod), "GolemVerb_AdjustedCooldown_Postfix", null), null);
            //harmonyInstance.Patch(AccessTools.Method(typeof(GenGrid), "InBounds", new Type[]
            //    {
            //        typeof(IntVec3),
            //        typeof(Map)
            //    }, null), new HarmonyMethod(typeof(TorannMagicMod), "IntVec3Inbounds_NullCheck_Prefix", null), null);
            ////harmonyInstance.Patch(AccessTools.Method(typeof(AbilityUser.PawnAbility), "GetJob"),
            ////    new HarmonyMethod(typeof(TorannMagicMod), "PawnAbility_GetJob_Prefix"));
            ////harmonyInstance.Patch(AccessTools.Method(typeof(QuestNode_RaceProperty), "Matches", new Type[]
            ////    {
            ////        typeof(object)
            ////    }, null), null, new HarmonyMethod(typeof(TorannMagicMod), "QuestNode_RaceProperties_ExcludeSummoned"));

            ////harmonyInstance.Patch(original: AccessTools.Method(type: typeof(Pawn_DraftController), name: "Notify_PrimaryWeaponChanged"), prefix: null,
            ////    postfix: new HarmonyMethod(methodType: patchType, methodName: nameof(PawnEquipment_Change_Postfix)), transpiler: null);
            ////harmonyInstance.Patch(original: AccessTools.Method(type: typeof(Pawn_EquipmentTracker), name: "TryTransferEquipmentToContainer"), prefix: null,
            ////    postfix: new HarmonyMethod(methodType: patchType, methodName: nameof(PawnEquipment_Transfer_Postfix)), transpiler: null);
            ////harmonyInstance.Patch(AccessTools.Method(typeof(Thing), "get_Suspended", null, null), new HarmonyMethod(typeof(TorannMagicMod), "Get_Suspended_Polymorphed", null), null);
            ////harmonyInstance.Patch(AccessTools.Method(typeof(Toils_Recipe), "DoRecipeWork", new Type[]
            ////    {
            ////    }, null), new HarmonyMethod(typeof(TorannMagicMod), "DoMagicRecipeWork", null), null);
            ////harmonyInstance.Patch(AccessTools.Method(typeof(CaravanArrivalTimeEstimator), "EstimatedTicksToArrive", new Type[]
            ////    {
            ////        typeof(int),
            ////        typeof(int),
            ////        typeof(WorldPath),
            ////        typeof(float),
            ////        typeof(int),
            ////        typeof(int)
            ////    }, null), null, new HarmonyMethod(typeof(TorannMagicMod), "EstimatedTicksToArrive_Wayfarer_Postfix", null), null);

            ////#region PrisonLabor
            ////{
            ////    try
            ////    {
            ////        ((Action)(() =>
            ////        {
            ////            if (ModCheck.Validate.PrisonLaborOutdated.IsInitialized())
            ////            {
            ////                harmonyInstance.Patch(AccessTools.Method(typeof(PrisonLabor.JobDriver_Mine_Tweak), "ResetTicksToPickHit"), null, new HarmonyMethod(typeof(TorannMagicMod), "TM_PrisonLabor_JobDriver_Mine_Tweak"));
            ////            }
            ////        }))();
            ////    }
            ////    catch (TypeLoadException) { }

            ////    try
            ////    {
            ////        ((Action)(() =>
            ////        {
            ////            if (ModCheck.Validate.PrisonLabor.IsInitialized())
            ////            {
            ////                harmonyInstance.Patch(AccessTools.Method(typeof(PrisonLabor.Tweaks.JobDriver_Mine_Tweak), "ResetTicksToPickHit"), null, new HarmonyMethod(typeof(TorannMagicMod), "TM_PrisonLabor_JobDriver_Mine_Tweak"));
            ////            }
            ////        }))();
            ////    }
            ////    catch (TypeLoadException) { }

            ////}
            ////#endregion PrisonLabor

            ////#region Children
            ////{
            ////    try
            ////    {
            ////        ((Action)(() =>
            ////        {
            ////            if (ModCheck.Validate.ChildrenSchoolLearning.IsInitialized())
            ////            {
            ////                harmonyInstance.Patch(AccessTools.Method(typeof(PawnUtility), "TrySpawnHatchedOrBornPawn"), null, new HarmonyMethod(typeof(TorannMagicMod), "TM_Children_TrySpawnHatchedOrBornPawn_Tweak"));
            ////            }
            ////        }))();
            ////    }
            ////    catch (TypeLoadException) { }
            ////}
            ////#endregion Children

            ////#region Dual Wield
            ////{
            ////    try
            ////    {
            ////        ((Action)(() =>
            ////        {
            ////            if (ModCheck.Validate.DualWield.IsInitialized())
            ////            {
            ////                harmonyInstance.Patch(AccessTools.Method(typeof(DualWield.Harmony.Verb_TryStartCastOn), "Postfix"), new HarmonyMethod(typeof(TorannMagicMod), "TM_DualWield_NotForCasting"), null);
            ////            }
            ////        }))();
            ////    }
            ////    catch (TypeLoadException) { }
            ////}
            ////#endregion Dual Wield

        }

        //public static bool PawnAbility_GetJob_Prefix(PawnAbility __instance, AbilityContext context, LocalTargetInfo target, ref Job __result)
        //{
        //    Job job;
        //    Log.Message("target is " + target.Thing.LabelShort);
        //    AbilityUser.AbilityDef abilityDef = Traverse.Create(root: __instance).Field(name: "powerdef").GetValue<AbilityUser.AbilityDef>();
        //    Log.Message("ability def is " + abilityDef.defName);
        //    Verb_UseAbility verb = Traverse.Create(root: __instance).Field(name: "verb").GetValue<Verb_UseAbility>();

        //    Log.Message("verb is " + verb);
        //    if (verb == null)
        //    {
        //        Verb_UseAbility verb_UseAbility = (Verb_UseAbility)Activator.CreateInstance(abilityDef.MainVerb.verbClass);
        //        verb_UseAbility.caster = __instance.Pawn;
        //        verb_UseAbility.Ability = __instance;
        //        verb_UseAbility.verbProps = abilityDef.MainVerb;
        //        verb = verb_UseAbility;
        //    }
        //    if (verb != null)
        //    {
        //        Log.Message("verb is no longer null");
        //    }
        //    job = abilityDef.GetJob(verb.UseAbilityProps.AbilityTargetCategory, target);
        //    job.playerForced = true;
        //    job.verbToUse = verb;
        //    job.count = context == AbilityContext.Player ? 1 : 0; //Count 1 for Player : 0 for AI
        //    if (target != null)
        //        if (target.Thing is Pawn pawn2)
        //            job.killIncappedTarget = pawn2.Downed;
        //    __result = job;
        //    return false;
        //}

        //[HarmonyPatch(typeof(Verb_UseAbility), "TryLaunchProjectileCheck", null)]
        //public class Troubleshooting_Patch
        //{
        //    public static bool Prefix(Verb_UseAbility __instance, ThingDef projectileDef, LocalTargetInfo launchTarget, VerbProperties ___verbProps, Thing ___caster, ref bool __result)
        //    {
        //        Log.Message("verb props " + ___verbProps);
        //        Log.Message("caster " + ___caster);
        //        Log.Message("launch taqrget " + launchTarget);
        //        Log.Message("projectile def " + projectileDef);
        //        return true;
        //    }
        //}

        //public static bool Get_Humanlike_Golem(RaceProperties __instance, ref bool __result)
        //{
        //    if (__instance.thinkTreeMain == TorannMagicDefOf.TM_GolemMain)
        //    {
        //        __result = false;
        //        return false;
        //    }
        //    return true;
        //}

        [HarmonyPatch(typeof(Projectile), "Impact", null)]
        public class Projectile_Impact_NoClamorForMagic_Patch
        {
            private static bool Prefix(Projectile __instance)
            {                
                if(__instance.ContentSource != null)
                {
                    if(__instance.ContentSource.PackageId == "kure.arom" || __instance.ContentSource.PackageId == "torann.arimworldofmagic")
                    {
                        __instance.Destroy();
                        return false;
                    }
                }
                return true;
            }
        }

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

        [HarmonyPatch(typeof(ThoughtWorker_Precept_HasAutomatedTurrets), "ResetStaticData", null)]
        public class NoSummonedTurretsThought_Patch
        {
            private static void Postfix(ThoughtWorker_Precept_HasAutomatedTurrets __instance, ref List<ThingDef> ___automatedTurretDefs)
            {
                List<ThingDef> tmpList = new List<ThingDef>();
                tmpList.Clear();
                if(___automatedTurretDefs != null && ___automatedTurretDefs.Count > 0)
                {
                    foreach(ThingDef td in ___automatedTurretDefs)
                    {
                        if(td.defName.StartsWith("DefensePylon") || td.defName.StartsWith("TM_TechnoTurret"))
                        {
                            tmpList.Add(td);
                        }
                    }
                    if(tmpList.Count > 0)
                    {
                        foreach(ThingDef td in tmpList)
                        {
                            ___automatedTurretDefs.Remove(td);
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

        [HarmonyPatch(typeof(ThoughtUtility), "CanGetThought", null)]
        public class ThoughtSuppression_Patch
        {
            private static void Postfix(Pawn pawn, ThoughtDef def, ref bool __result)
            {
                if (__result && pawn != null && pawn.health != null && pawn.health.hediffSet != null && pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EmotionSuppressionHD))
                {
                    Hediff hd = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EmotionSuppressionHD);
                    if (hd != null && def.stages != null && def.stages.FirstOrDefault() != null && def.stages.FirstOrDefault().baseMoodEffect != 0)
                    {
                        if(Rand.Chance(Mathf.Clamp01(hd.Severity - .7f)) && def.stages.FirstOrDefault().baseMoodEffect > 0)
                        {
                            //can gain positive thoughts
                        }
                        else
                        {
                            __result = false;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MentalBreakWorker), "BreakCanOccur", null)]
        public class MentalBreakSuppression_Patch
        {
            private static void Postfix(MentalBreakWorker __instance, Pawn pawn, ref bool __result)
            {
                if(__result && pawn != null && pawn.health != null && pawn.health.hediffSet != null && pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EmotionSuppressionHD))
                {
                    __result = false;
                }
            }
        }

        private static void Get_CanDoRandomMentalBreaks(MentalBreaker __instance, Pawn ___pawn, ref bool __result)
        {
            if(___pawn != null && __result)
            {
                if(___pawn.health != null && ___pawn.health.hediffSet != null && ___pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EmotionSuppressionHD))
                {
                    __result = false;
                }
            }
        }        

        [HarmonyPatch(typeof(Plant), "PlantCollected", null)]
        public class ApothecaryHarvest_Patch
        {
            private static void Postfix(Plant __instance, Pawn by)
            {
                if(by != null && by.health != null && by.health.hediffSet != null)
                {
                    Pawn p = by;
                    CompAbilityUserMight comp = p.TryGetComp<CompAbilityUserMight>();
                    Hediff_ApothecaryHerbs hd = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ApothecaryHerbsHD) as Hediff_ApothecaryHerbs;
                    if(hd != null)
                    {
                        float multiplier = 1f;
                        if(__instance.Blighted)
                        {
                            multiplier += .5f;
                        }
                        if(__instance.LeaflessNow)
                        {
                            multiplier -= .4f;
                        }
                        if(!__instance.HarvestableNow)
                        {
                            multiplier -= .5f;
                        }
                        if(__instance.def.plant != null && __instance.def.plant.harvestYield > 0)
                        {
                            multiplier += 1f;
                        }
                        if(comp != null && comp.MightData != null)
                        {
                            MightPowerSkill mps = comp.MightData.GetSkill_Versatility(TorannMagicDefOf.TM_Herbalist);
                            if (mps != null)
                            {
                                multiplier += (.1f * mps.level);
                            }
                        }
                        hd.Severity += Rand.Range(1f, 2f) * multiplier * __instance.Growth;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MusicManagerPlay), "AppropriateNow", null)]
        public class MusicManager_RoyaltyNullCheck_Patch
        {
            private static bool Prefix(MusicManagerPlay __instance, SongDef song, Queue<SongDef> ___recentSongs, ref bool __result)
            {
                __result = false;
                if (!song.playOnMap)
                {
                    return false;
                }
                if (TM_Calc.DangerMusicMode)
                {
                    if (!song.tense)
                    {
                        return false;
                    }
                }
                else if (song.tense)
                {
                    return false;
                }
                Map map = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
                if (!song.allowedSeasons.NullOrEmpty())
                {
                    if (map == null)
                    {
                        return false;
                    }
                    if (!song.allowedSeasons.Contains(GenLocalDate.Season(map)))
                    {
                        return false;
                    }
                }
                if (song.minRoyalTitle != null && !PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists.Any(delegate (Pawn p)
                {
                    if (p.royalty != null && p.royalty.AllTitlesForReading.Any() && p.royalty.MostSeniorTitle.def.seniority >= song.minRoyalTitle.seniority)
                    {
                        return true;
                    }
                    return false;
                }))
                {
                    return false;
                }
                if (___recentSongs.Contains(song))
                {
                    return false;
                }
                if (song.allowedTimeOfDay != TimeOfDay.Any)
                {
                    if (map == null)
                    {
                        __result = true;
                        return false;
                    }
                    if (song.allowedTimeOfDay == TimeOfDay.Night)
                    {
                        if (!(GenLocalDate.DayPercent(map) < 0.2f))
                        {
                            __result = GenLocalDate.DayPercent(map) > 0.7f;
                            return false;
                        }
                        return true;
                    }
                    if (GenLocalDate.DayPercent(map) > 0.2f)
                    {
                        __result = GenLocalDate.DayPercent(map) < 0.7f;
                        return false;
                    }
                    return false;
                }
                __result = true;
                return false;
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

        [HarmonyPatch(typeof(PawnGraphicSet), "ResolveAllGraphics", null)]
        public class SkeletonSkull_Patch
        {
            private static void Postfix(ref PawnGraphicSet __instance)
            {
                if (__instance.pawn.RaceProps.Humanlike)
                {
                    __instance.skullGraphic = SkullDatabase.GetSkullFor(__instance.pawn.gender, __instance.pawn.story.crownType);
                }
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "CanTakeOrder", null)]
        public class GolemOrders_Patch
        {
            public static bool Prefix(Pawn pawn, ref bool __result)
            {
                if ((pawn is TMPawnGolem || pawn is TMHollowGolem) && pawn.Faction == Faction.OfPlayerSilentFail)
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
                if(pawn is TMPawnGolem || pawn is TMHollowGolem)
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
                if (pawn is TMPawnGolem || pawn is TMHollowGolem)
                {
                    IntVec3 clickCell = IntVec3.FromVector3(clickPos);
                    foreach (LocalTargetInfo item6 in GenUI.TargetsAt(clickPos, TargetingParameters.ForAttackHostile(), thingsOnly: true))
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
                    if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    {
                        foreach (LocalTargetInfo item7 in GenUI.TargetsAt(clickPos, TargetingParameters.ForCarry(pawn), thingsOnly: true))
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
                        if (!carriedPawn.IsPrisonerOfColony)
                        {
                            foreach (LocalTargetInfo item8 in GenUI.TargetsAt(clickPos, TargetingParameters.ForDraftedCarryBed(carriedPawn, pawn, carriedPawn.GuestStatus), thingsOnly: true))
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
                        foreach (LocalTargetInfo item9 in GenUI.TargetsAt(clickPos, TargetingParameters.ForDraftedCarryBed(carriedPawn, pawn, GuestStatus.Prisoner), thingsOnly: true))
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
                        foreach (LocalTargetInfo item10 in GenUI.TargetsAt(clickPos, TargetingParameters.ForDraftedCarryTransporter(carriedPawn), thingsOnly: true))
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
                        foreach (LocalTargetInfo item11 in GenUI.TargetsAt(clickPos, TargetingParameters.ForDraftedCarryCryptosleepCasket(pawn), thingsOnly: true))
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

        public static bool Get_WindSpeed(WindManager __instance, Map ___map, ref float __result)
        {
            if (___map != null)
            {
                MagicMapComponent mmc = ___map.GetComponent<MagicMapComponent>();
                if (mmc != null && mmc.windSpeedEndTick > Find.TickManager.TicksGame)
                {
                    __result = mmc.windSpeed;
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(WorkGiver_Warden_EmancipateSlave), "JobOnThing", null)]
        public class Undead_DoNotGiveEmancipateJob_Patch
        {
            public static bool Prefix(Pawn pawn, Thing t, ref Job __result)
            {
                Pawn p = t as Pawn;
                if(p != null && ModsConfig.IdeologyActive && TM_Calc.IsUndeadNotVamp(p))
                {
                    __result = null;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(SlaveRebellionUtility), "CanParticipateInSlaveRebellion", null)]
        public class Undead_DoNotPartcicipateInSlaveRebellion_Patch
        {
            public static void Postfix(Pawn pawn, ref bool __result)
            {
                if (__result && TM_Calc.IsUndeadNotVamp(pawn))
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(PawnCapacityUtility), "CalculatePartEfficiency", null)]
        public class CalculatePartEfficiency_NullCheck_Patch
        {
            public static bool Prefix(BodyPartRecord part, ref float __result)
            {
                if (part != null)
                {
                    return true;
                }
                __result = 0f;
                return false;
            }
        }

        [HarmonyPatch(typeof(Verb), "ValidateTarget", null)]
        public class Validate_NonViolent_VeneratedAnimal_Patch
        {
            public static bool Prefix(Verb __instance, LocalTargetInfo target, ref bool __result)
            {
                Verb_UseAbility ability = __instance as Verb_UseAbility;
                if(!__result && ability != null && ability.Ability != null && !ability.Ability.Def.MainVerb.isViolent)
                {                    
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(PawnUtility), "GainComfortFromThingIfPossible", null)]
        public class GainComfortFromThingIfPossible_Manaweave_Patch
        {
            public static void Postfix(Pawn p, Thing from)
            {
                float statValue = from.GetStatValue(StatDefOf.Comfort);
                CompAbilityUserMagic comp = p.TryGetComp<CompAbilityUserMagic>();
                if (statValue >= 0f && comp != null && comp.Mana != null)
                {
                    comp.Mana.CurLevel += .0001f * statValue;
                }
            }
        }

        [HarmonyPatch(typeof(Need_Rest), "NeedInterval", null)]
        public class RestReduceArcaneWeakness_Patch
        {
            public static void Postfix(Need_Rest __instance, Pawn ___pawn, int ___lastRestTick)
            {
                if (Find.TickManager.TicksGame < (___lastRestTick + 2) && ___pawn != null && ___pawn.health != null && ___pawn.health.hediffSet != null)
                {
                    Hediff hd = ___pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ArcaneWeakness);
                    if (hd != null)
                    {
                        hd.Severity -= .01f;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Building_MusicalInstrument), "Tick", null)]
        public class BardPlaysMusicalInstrument_Patch
        {
            public static void Postfix(Building_MusicalInstrument __instance, Pawn ___currentPlayer)
            {
                if (__instance != null && ___currentPlayer != null && Find.TickManager.TicksGame % 131 == 0)
                {
                    CompAbilityUserMagic comp = ___currentPlayer.TryGetComp<CompAbilityUserMagic>();
                    if (comp != null && comp.MagicData != null && comp.IsMagicUser)
                    {
                        if (comp.MagicData.MagicPowersB.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Entertain).learned)
                        {
                            foreach (Pawn p in __instance.Map.mapPawns.AllPawnsSpawned)
                            {
                                if (p.RaceProps.Humanlike && Building_MusicalInstrument.IsAffectedByInstrument(__instance.def, __instance.Position, p.Position, __instance.Map))
                                {
                                    CompAbilityUserMagic compListener = p.TryGetComp<CompAbilityUserMagic>();
                                    if (compListener != null && compListener.IsMagicUser && compListener.Mana != null)
                                    {
                                        compListener.Mana.CurLevel += .0075f;
                                    }
                                    if (p.needs != null && p.needs.joy != null)
                                    {
                                        p.needs.joy.CurLevel += .01f;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void GolemVerb_AdjustedCooldown_Postfix(VerbProperties __instance, Pawn attacker, ref float __result)
        {
            if (attacker is TMPawnGolem && __instance != null && __instance.range >= 2 && __instance.defaultProjectile != null)
            {
                __result = .1f;
            }
        }

        [HarmonyPriority(2000)] //Go first to make sure the right verb is copied
        private static void TryStartCastOn_Prefix(Verb __instance)
        {
            transferVerb = __instance;
        }
        public static Verb transferVerb = null;
        public static bool TM_DualWield_NotForCasting(Verb __instance, LocalTargetInfo castTarg)
        {
            if (transferVerb != null && (transferVerb.GetType().ToString().StartsWith("TorannMagic") || transferVerb.GetType().ToString().StartsWith("AbilityUser")))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(FireUtility), "CanEverAttachFire", null)]
        public class FireImmunity_Hediff_Prefix
        {
            private static bool Prefix(Thing t, ref bool __result)
            {
                if (t is Pawn)
                {
                    Pawn p = t as Pawn;
                    if (p.health != null && p.health.hediffSet != null && (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffEnchantment_fireImmunity) || p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HeatShieldHD)))
                    {
                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(AttackTargetFinder), "BestAttackTarget", null)]
        public class Taunted_TargetSelection_Patch
        {
            public static bool Prefix(IAttackTargetSearcher searcher, ref IAttackTarget __result)
            {
                if (searcher != null && searcher.Thing != null)
                {
                    if (searcher.Thing is Pawn)
                    {
                        Pawn p = searcher.Thing as Pawn;
                        if (p.health != null && p.health.hediffSet != null && p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TauntHD))
                        {
                            HediffComp_Taunt hdc_t = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_TauntHD).TryGetComp<HediffComp_Taunt>();
                            if (hdc_t != null && hdc_t.tauntTarget != null)
                            {
                                __result = (IAttackTarget)hdc_t.tauntTarget;
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
        }

        public static bool Projectile_Launch_Prefix(Projectile __instance, Thing launcher, Vector3 origin, ref LocalTargetInfo usedTarget, ref LocalTargetInfo intendedTarget)
        {
            if (launcher is Pawn)
            {
                Pawn launcherPawn = (Pawn)launcher;
                if (launcherPawn.health != null && launcherPawn.health.hediffSet != null)
                {
                    Hediff hd = launcherPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightBurstHD);
                    if (hd != null && hd.Severity >= .7)
                    {
                        if (launcherPawn.equipment.PrimaryEq != null && launcherPawn.equipment.Primary.def.IsRangedWeapon)
                        {
                            float maxRange = launcherPawn.equipment.Primary.def.Verbs.FirstOrDefault().range;
                            List<Pawn> doomTargets = new List<Pawn>();
                            List<Pawn> mapPawns = launcherPawn.Map.mapPawns.AllPawnsSpawned;
                            doomTargets.Clear();
                            for (int i = 0; i < mapPawns.Count; i++)
                            {
                                float distance = (mapPawns[i].Position - launcherPawn.Position).LengthHorizontal;
                                if (mapPawns[i].Faction == launcherPawn.Faction && mapPawns[i] != launcherPawn && distance < maxRange && distance > 3)
                                {
                                    doomTargets.Add(mapPawns[i]);
                                }
                            }
                            if (doomTargets.Count > 0 && Rand.Chance(.6f))
                            {
                                LocalTargetInfo doomTarget = doomTargets.RandomElement();
                                usedTarget = doomTarget;
                                intendedTarget = doomTarget;
                            }
                        }
                    }

                    if (launcherPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Hex_CriticalFailHD))
                    {
                        if (TM_Calc.IsUsingRanged(launcherPawn) && Rand.Chance(.5f))
                        {
                            Vector3 centerVec = TM_Calc.GetVectorBetween(launcherPawn.DrawPos, usedTarget.CenterVector3);
                            List<Pawn> targetList = TM_Calc.FindAllPawnsAround(launcherPawn.Map, centerVec.ToIntVec3(), 6, null, false);
                            if (targetList != null && targetList.Count > 0)
                            {
                                LocalTargetInfo target = targetList.RandomElement();
                                usedTarget = target;
                                intendedTarget = target;
                            }
                        }
                        else if (TM_Calc.IsUsingMelee(launcherPawn) && Rand.Chance(.5f))
                        {
                            usedTarget = launcherPawn;
                            intendedTarget = launcherPawn;
                        }
                    }
                }
            }
            return true;
        }


        [HarmonyPatch(typeof(MassUtility), "Capacity", null)]
        public class StrongBack_InventoryMassCapacity_Patch
        {
            public static void Postfix(Pawn p, ref float __result)
            {
                if (p != null && p.health != null && p.health.hediffSet != null && p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffStrongBack))
                {
                    __result *= 1.5f;
                }
            }

        }

        [HarmonyPatch(typeof(WeatherWorker), "WeatherTick", null)]
        public class WeatherWorker_Patch
        {
            public static void Postfix(WeatherManager __instance, Map map)
            {
                if (map.weatherManager.curWeather == TorannMagicDefOf.TM_HealingRainWD)
                {
                    if (Find.TickManager.TicksGame % 20 == 0)
                    {
                        for (int i = 0; i < map.mapPawns.AllPawnsSpawned.Count; i++)
                        {
                            Pawn pawn = map.mapPawns.AllPawnsSpawned[i];
                            if (!pawn.Position.Roofed(map) && Rand.Chance(.3f))
                            {
                                IEnumerable<Hediff_Injury> injuries = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                                if (injuries != null && injuries.Count() > 0)
                                {
                                    Hediff_Injury injury = injuries.RandomElement();
                                    if (injury.CanHealNaturally() && !injury.IsPermanent())
                                    {
                                        float healAmt = Rand.Range(.025f, .15f);

                                        injury.Heal(healAmt);
                                        Vector3 rndPos = pawn.DrawPos;
                                        rndPos.x += Rand.Range(-.25f, .25f);
                                        rndPos.z += Rand.Range(-.3f, .3f);
                                        ThingDef mote = TorannMagicDefOf.Mote_BlueTwinkle;
                                        if (Rand.Chance(.7f))
                                        {
                                            mote = TorannMagicDefOf.Mote_GreenTwinkle;
                                        }
                                        TM_MoteMaker.ThrowGenericMote(mote, rndPos, map, healAmt * 3f, Rand.Range(.2f, .35f), Rand.Range(0, .25f), Rand.Range(.25f, .75f), Rand.Range(-250, 250), Rand.Range(.2f, .6f), Rand.Range(-15f, 15f), Rand.Range(0f, 360f));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Reward_Items), "InitFromValue", null)]
        public class Reward_Items_TMQuests_Patch
        {
            private static bool Prefix(Reward_Items __instance, float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
            {
                float num = 0f;
                if (parms.chosenPawnSignal != null && parms.chosenPawnSignal == "TM_ArcaneRewards")
                {
                    int value = Mathf.RoundToInt(rewardValue * Rand.Range(0.7f, 1.3f));
                    __instance.items.Clear();
                    __instance.items.AddRange(ItemCollectionGenerator_Internal_Arcane.Generate(value));
                    for (int i = 0; i < __instance.items.Count; i++)
                    {
                        num += __instance.items[i].MarketValue * (float)__instance.items[i].stackCount;
                    }
                    valueActuallyUsed = num;
                    return false;
                }
                valueActuallyUsed = num;
                return true;
            }
        }

        public static void AreaManager_AddMagicZonesToStartingAreas(AreaManager __instance)
        {
            TM_Calc.GetSpriteArea(__instance.map);
            TM_Calc.GetTransmutateArea(__instance.map);
            TM_Calc.GetSeedOfRegrowthArea(__instance.map);
        }

        public static bool DefaultStorageSettings_IncludeMagicItems(ThingFilter __instance, StorageSettingsPreset preset)
        {
            MethodBase SetAllow = AccessTools.Method(typeof(ThingFilter), "SetAllow", new Type[]
                {
                    typeof(ThingCategoryDef),
                    typeof(bool),
                    typeof(IEnumerable<ThingDef>),
                    typeof(IEnumerable<SpecialThingFilterDef>)
                }, null);
            if (preset == StorageSettingsPreset.DefaultStockpile)
            {
                SetAllow.Invoke(__instance, new object[]
                {
                    TorannMagicDefOf.TM_MagicItems,
                    true,
                    null,
                    null
                });
            }
            return true;
        }

        public static bool HealthUtility_HeatCold_HediffGiverForUndead(Pawn pawn, ref HediffDef hdDef, float sevOffset)
        {
            if (hdDef != null && hdDef == HediffDefOf.Heatstroke)
            {
                if (TM_Calc.IsUndead(pawn))
                {
                    hdDef = TorannMagicDefOf.TM_SlaggedHD;
                }
            }
            if (hdDef != null && hdDef == HediffDefOf.Hypothermia)
            {
                if (TM_Calc.IsUndead(pawn))
                {
                    hdDef = TorannMagicDefOf.TM_BrittleBonesHD;
                }
            }
            return true;
        }

        public static void QuestPart_LendColonists_Enable_NoUndead(QuestPart_LendColonistsToFaction __instance, ref SignalArgs receivedArgs)
        {
            //MethodBase Complete = AccessTools.Method(typeof(QuestPart_LendColonistsToFaction), "Complete", null, null);

            if (__instance.LentColonistsListForReading != null && __instance.LentColonistsListForReading.Count > 0)
            {
                bool undeadSent = false;
                for (int i = 0; i < __instance.LentColonistsListForReading.Count; i++)
                {
                    Thing lentColonist = __instance.LentColonistsListForReading[i];
                    if (lentColonist is Pawn && ((Pawn)lentColonist).health != null && ((Pawn)lentColonist).health.hediffSet != null && (((Pawn)lentColonist).health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD) || ((Pawn)lentColonist).health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD)))
                    {
                        undeadSent = true;
                        break;
                    }
                }
                if (undeadSent)
                {
                    for (int i = 0; i < __instance.LentColonistsListForReading.Count; i++)
                    {
                        Thing t = __instance.LentColonistsListForReading[i];
                        GenPlace.TryPlaceThing(t, __instance.shuttle.Position, __instance.shuttle.Map, ThingPlaceMode.Near);
                        if (t is Pawn)
                        {
                            Pawn p = t as Pawn;
                            if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD) || p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD))
                            {
                                p.Kill(null, null);
                            }
                        }
                    }
                    Messages.Message("TM_LendColonist_UndeadFail".Translate(), MessageTypeDefOf.SilentInput, false);
                    __instance.quest.End(QuestEndOutcome.Fail, true);
                }
            }
        }

        public static void PawnEquipment_Drop_Postfix(Pawn_EquipmentTracker __instance, ThingWithComps eq, ref bool __result)
        {
            Pawn p = __instance.pawn;
            CompAbilityUserMight comp = p.TryGetComp<CompAbilityUserMight>();
            if (p != null && comp != null && (p.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) || (comp.customClass != null)) && comp.equipmentContainer != null && __result)
            {
                if (comp.equipmentContainer.Count > 0)
                {
                    for (int i = 0; i < comp.equipmentContainer.Count; i++)
                    {
                        if (eq.def.label.Contains(comp.equipmentContainer[i].def.label))
                        {
                            //Log.Message("dropping specialized weapon base: " + eq.def.defName);
                            Thing outThing = null;
                            comp.equipmentContainer[i].HitPoints = eq.HitPoints;
                            comp.equipmentContainer.TryDrop(comp.equipmentContainer[i], p.Position, p.Map, ThingPlaceMode.Near, out outThing);
                            //eq.Destroy(DestroyMode.Vanish);
                            comp.equipmentContainer.Clear();
                        }
                    }
                }
            }
            if (p != null && p.Spawned && p.Map != null)
            {
                if (comp != null)
                {
                    comp.weaponDamage = TM_Calc.GetSkillDamage(p);
                }
                CompAbilityUserMagic mComp = p.TryGetComp<CompAbilityUserMagic>();
                if (mComp != null)
                {
                    mComp.weaponDamage = TM_Calc.GetSkillDamage(p);
                }
            }
        }

        //public static void PawnEquipment_Transfer_Postfix(Pawn_EquipmentTracker __instance, ThingWithComps eq, ref bool __result)
        //{
        //    Pawn p = __instance.pawn;
        //    CompAbilityUserMight comp = p.TryGetComp<CompAbilityUserMight>();
        //    if (p != null && comp != null && p.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) && __result)
        //    {

        //    }
        //}

        public static void PawnEquipment_Add_Postfix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
        {
            if (!newEq.def.defName.Contains("Spec_Base"))
            {
                Pawn p = __instance.pawn;
                CompAbilityUserMight comp = p.TryGetComp<CompAbilityUserMight>();
                if (p != null && comp != null && (p.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) || (comp.customClass != null)))
                {
                    if (comp.equipmentContainer == null)
                    {
                        comp.equipmentContainer = new ThingOwner<ThingWithComps>();
                        comp.equipmentContainer.Clear();
                    }

                    if (newEq == p.equipment.Primary)
                    {
                        //Log.Message("adding primary weapon");
                        if (comp.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_PistolSpec).learned && TM_Calc.IsUsingPistol(p))
                        {
                            //Log.Message("weapon is pistol specialized: " + newEq.def.defName);                            
                            __instance.TryTransferEquipmentToContainer(newEq, comp.equipmentContainer);
                            TM_Action.DoAction_PistolSpecCopy(p, newEq);
                        }
                        else if (comp.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_RifleSpec).learned && TM_Calc.IsUsingRifle(p))
                        {
                            //Log.Message("weapon is rifle specialized: " + newEq.def.defName);
                            __instance.TryTransferEquipmentToContainer(newEq, comp.equipmentContainer);
                            TM_Action.DoAction_RifleSpecCopy(p, newEq);
                        }
                        else if (comp.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_ShotgunSpec).learned && TM_Calc.IsUsingShotgun(p))
                        {
                            //Log.Message("weapon is shotgun specialized: " + newEq.def.defName);
                            __instance.TryTransferEquipmentToContainer(newEq, comp.equipmentContainer);
                            TM_Action.DoAction_ShotgunSpecCopy(p, newEq);
                        }
                    }
                }
                if (p != null && p.Spawned && p.Map != null)
                {
                    if (comp != null)
                    {
                        comp.weaponDamage = TM_Calc.GetSkillDamage(p);
                    }
                    CompAbilityUserMagic mComp = p.TryGetComp<CompAbilityUserMagic>();
                    if (mComp != null)
                    {
                        mComp.weaponDamage = TM_Calc.GetSkillDamage(p);
                    }
                }

            }
        }

        [HarmonyPatch(typeof(Caravan_PathFollower), "CostToPayThisTick", null)]
        public class CostToPayThisTick_Base_Patch
        {
            private static void Postfix(Caravan_PathFollower __instance, ref float __result)
            {
                Caravan caravan = Traverse.Create(root: __instance).Field(name: "caravan").GetValue<Caravan>();
                if (caravan != null)
                {
                    IEnumerable<Pawn> pList = caravan.PlayerPawnsForStoryteller;
                    bool hasWayfarer = false;
                    if (pList != null && pList.Count() > 0)
                    {
                        foreach (Pawn p in pList)
                        {
                            if (p.story != null && p.story.traits != null && p.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer))
                            {
                                hasWayfarer = true;
                                break;
                            }
                        }
                    }
                    if (hasWayfarer)
                    {
                        __result *= 1.25f;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Caravan_PathFollower), "CostToMove", new Type[]
        {
            typeof(Caravan),
            typeof(int),
            typeof(int),
            typeof(int?)
        })]
        public static class CostToMove_Caravan_Patch
        {
            [HarmonyPostfix]
            public static void CostToMove_Caravan_Postfix(Caravan_PathFollower __instance, Caravan caravan, int start, int end, ref int __result, int? ticksAbs = default(int?))
            {
                if (caravan != null)
                {
                    IEnumerable<Pawn> pList = caravan.PlayerPawnsForStoryteller;
                    bool hasWanderer = false;
                    if (pList != null && pList.Count() > 0)
                    {
                        foreach (Pawn p in pList)
                        {
                            if (p.story != null && p.story.traits != null && p.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer))
                            {
                                hasWanderer = true;
                                break;
                            }
                        }
                    }
                    if (hasWanderer)
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        float num = WorldPathGrid.CalculatedMovementDifficultyAt(end, false, ticksAbs, stringBuilder);
                        __result = (int)(__result / num);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ApparelUtility), "HasPartsToWear", null)]
        public class BracersOfPacifist_Wear_Prevention
        {
            public static void Postfix(Pawn p, ThingDef apparel, ref bool __result)
            {
                if (p != null && p.story != null && !p.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent) && apparel == TorannMagicDefOf.TM_Artifact_BracersOfThePacifist)
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(WeatherEvent_LightningStrike), "FireEvent", null)]
        public class LightningStrike_DarkThunderstorm_Patch
        {
            public static bool Prefix(ref WeatherEvent_LightningStrike __instance)
            {
                //Log.Message("thunder strike");
                Map map = Traverse.Create(root: __instance).Field(name: "map").GetValue<Map>();
                if (map != null && map.GameConditionManager != null)
                {
                    //Log.Message("checking condition");
                    GameCondition_DarkThunderstorm gcdt = map.GameConditionManager.GetActiveCondition(TorannMagicDefOf.DarkThunderstorm) as GameCondition_DarkThunderstorm;
                    if (gcdt != null && gcdt.enemyPawns != null && gcdt.enemyPawns.Count > 0)
                    {
                        //Log.Message("setting strike loc to...");
                        Pawn e = gcdt.enemyPawns.RandomElement();
                        IntVec3 rndLoc = e.Position;
                        rndLoc.x += (Rand.Range(-2, 2));
                        rndLoc.z += (Rand.Range(-2, 2));
                        //Log.Message("setting rndloc " + rndLoc + " to strike loc...");
                        Traverse.Create(root: __instance).Field(name: "strikeLoc").SetValue(rndLoc);
                        //IntVec3 loc = Traverse.Create(root: __instance).Field(name: "strikeLoc").GetValue<IntVec3>();
                        //Log.Message("" + loc);
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(WeatherManager), "TransitionTo", null)]
        public class WeatherManager_RemoveDarkThunderstorm_Postfix
        {
            public static void Postfix(WeatherManager __instance, WeatherDef newWeather)
            {
                Map map = Traverse.Create(root: __instance).Field(name: "map").GetValue<Map>();
                if (map != null && map.GameConditionManager != null)
                {
                    GameCondition_DarkThunderstorm gcdt = map.GameConditionManager.GetActiveCondition(TorannMagicDefOf.DarkThunderstorm) as GameCondition_DarkThunderstorm;
                    if (gcdt != null)
                    {
                        //Log.Message("ending thunderstorm");
                        gcdt.End();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(InteractionWorker), "Interacted", null)]
        public class GearRepair_InteractionWorker_Postfix
        {
            public static void Postfix(InteractionWorker __instance, Pawn initiator, Pawn recipient)
            {
                if (recipient != null && initiator != null)
                {
                    CompAbilityUserMight comp = initiator.GetComp<CompAbilityUserMight>();
                    if (__instance.interaction == InteractionDefOf.Chitchat)
                    {
                        if (initiator.story != null && comp != null && initiator.story.traits != null && (initiator.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_FieldTraining, null, comp)))
                        {
                            if (initiator.health != null && initiator.health.hediffSet != null && initiator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffGearRepair))
                            {
                                if (comp.MightData != null && comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_ver").level >= 10)
                                {
                                    if (recipient.equipment != null && recipient.equipment.Primary != null)
                                    {
                                        Thing weapon = recipient.equipment.Primary;
                                        if (weapon != null && (weapon.def.IsRangedWeapon || weapon.def.IsMeleeWeapon))
                                        {
                                            if (weapon.HitPoints < weapon.MaxHitPoints)
                                            {
                                                weapon.HitPoints++;
                                            }
                                        }
                                    }
                                    if (recipient.apparel != null)
                                    {
                                        List<Apparel> gear = recipient.apparel.WornApparel;
                                        for (int i = 0; i < gear.Count; i++)
                                        {
                                            if (gear[i].HitPoints < gear[i].MaxHitPoints)
                                            {
                                                gear[i].HitPoints++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (recipient.story != null && recipient.story.traits != null && (recipient.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_FieldTraining, null, comp)))
                        {
                            if (recipient.health != null && recipient.health.hediffSet != null && recipient.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffGearRepair))
                            {
                                if (comp.MightData != null && comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_ver").level >= 10)
                                {
                                    if (initiator.equipment != null && initiator.equipment.Primary != null)
                                    {
                                        Thing weapon = initiator.equipment.Primary;
                                        if (weapon != null && (weapon.def.IsRangedWeapon || weapon.def.IsMeleeWeapon))
                                        {
                                            if (weapon.HitPoints < weapon.MaxHitPoints)
                                            {
                                                weapon.HitPoints++;
                                            }
                                        }
                                    }
                                    if (initiator.apparel != null)
                                    {
                                        List<Apparel> gear = initiator.apparel.WornApparel;
                                        for (int i = 0; i < gear.Count; i++)
                                        {
                                            if (gear[i].HitPoints < gear[i].MaxHitPoints)
                                            {
                                                gear[i].HitPoints++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (__instance.interaction == InteractionDefOf.DeepTalk)
                    {
                        if (initiator.story != null && comp != null && initiator.story.traits != null && (initiator.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_FieldTraining, null, comp)))
                        {
                            if (initiator.health != null && initiator.health.hediffSet != null && initiator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffGearRepair))
                            {
                                if (comp.MightData != null && comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_ver").level >= 10)
                                {
                                    if (recipient.equipment != null && recipient.equipment.Primary != null)
                                    {
                                        Thing weapon = recipient.equipment.Primary;
                                        if (weapon != null && (weapon.def.IsRangedWeapon || weapon.def.IsMeleeWeapon))
                                        {
                                            if (weapon.HitPoints < weapon.MaxHitPoints)
                                            {
                                                weapon.HitPoints++;
                                            }
                                            if (weapon.HitPoints < weapon.MaxHitPoints)
                                            {
                                                weapon.HitPoints++;
                                            }
                                            if (weapon.HitPoints < weapon.MaxHitPoints)
                                            {
                                                weapon.HitPoints++;
                                            }
                                        }
                                    }
                                    if (recipient.apparel != null)
                                    {
                                        List<Apparel> gear = recipient.apparel.WornApparel;
                                        for (int i = 0; i < gear.Count; i++)
                                        {
                                            if (gear[i].HitPoints < gear[i].MaxHitPoints)
                                            {
                                                gear[i].HitPoints++;
                                            }
                                            if (gear[i].HitPoints < gear[i].MaxHitPoints)
                                            {
                                                gear[i].HitPoints++;
                                            }
                                            if (gear[i].HitPoints < gear[i].MaxHitPoints)
                                            {
                                                gear[i].HitPoints++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (recipient.story != null && comp != null && recipient.story.traits != null && (recipient.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_FieldTraining, null, comp)))
                        {
                            if (recipient.health != null && recipient.health.hediffSet != null && recipient.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffGearRepair))
                            {
                                if (comp.MightData != null && comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_ver").level >= 10)
                                {
                                    if (initiator.equipment != null && initiator.equipment.Primary != null)
                                    {
                                        Thing weapon = initiator.equipment.Primary;
                                        if (weapon != null && (weapon.def.IsRangedWeapon || weapon.def.IsMeleeWeapon))
                                        {
                                            if (weapon.HitPoints < weapon.MaxHitPoints)
                                            {
                                                weapon.HitPoints++;
                                            }
                                            if (weapon.HitPoints < weapon.MaxHitPoints)
                                            {
                                                weapon.HitPoints++;
                                            }
                                            if (weapon.HitPoints < weapon.MaxHitPoints)
                                            {
                                                weapon.HitPoints++;
                                            }
                                        }
                                    }
                                    if (initiator.apparel != null)
                                    {
                                        List<Apparel> gear = initiator.apparel.WornApparel;
                                        for (int i = 0; i < gear.Count; i++)
                                        {
                                            if (gear[i].HitPoints < gear[i].MaxHitPoints)
                                            {
                                                gear[i].HitPoints++;
                                            }
                                            if (gear[i].HitPoints < gear[i].MaxHitPoints)
                                            {
                                                gear[i].HitPoints++;
                                            }
                                            if (gear[i].HitPoints < gear[i].MaxHitPoints)
                                            {
                                                gear[i].HitPoints++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Thing), "IDNumberFromThingID", null)]
        public class TechnoWeapon_ThingID
        {
            public static void Postfix(string thingID, ref int __result)
            {
                if (thingID.Contains("TM_TechnoWeapon") || thingID.Contains("Spec_Base"))
                {
                    __result = Rand.Range(0, 50000);
                    //Log.Message("changing thing id to " + __result);
                }
            }
        }

        [HarmonyPatch(typeof(Pawn), "VerifyReservations", null)]
        public class VerifyReservations_Prefix_Patch
        {
            public static void Prefix(Pawn __instance)
            {
                if (__instance.jobs != null && __instance.CurJob == null && __instance.jobs.jobQueue.Count <= 0 && !__instance.jobs.startingNewJob)
                {
                    List<Map> maps = Find.Maps;
                    for (int i = 0; i < maps.Count; i++)
                    {
                        IntVec3 obj3 = maps[i].pawnDestinationReservationManager.FirstObsoleteReservationFor(__instance);
                        if (obj3.IsValid)
                        {
                            Job job = maps[i].pawnDestinationReservationManager.FirstObsoleteReservationJobFor(__instance);
                            __instance.ClearAllReservations();
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(IncidentWorker), "TryExecute", null)]
        public class IncidentWorker_TryExecute_Prefix_Patch
        {
            public static bool Prefix(IncidentWorker __instance, ref IncidentParms parms, ref bool __result)
            {
                if (__instance != null && __instance.def != null && parms != null && __instance.def.defName != "VisitorGroup" && __instance.def.defName != "VisitorGroupMax" && !__instance.def.defName.Contains("Cult") && parms.quest == null && !parms.forced && !__instance.def.workerClass.ToString().StartsWith("Rumor_Code"))
                {
                    try
                    {
                        List<Map> allMaps = Find.Maps;
                        if (allMaps != null && allMaps.Count > 0)
                        {
                            for (int i = 0; i < allMaps.Count; i++)
                            {
                                if (parms != null && parms.target != null && allMaps[i].Tile == parms.target.Tile)
                                {
                                    List<Pawn> mapPawns = allMaps[i].mapPawns.AllPawnsSpawned.InRandomOrder().ToList();
                                    if (mapPawns != null && mapPawns.Count > 0)
                                    {

                                        List<Pawn> predictingPawnsAvailable = new List<Pawn>();
                                        predictingPawnsAvailable.Clear();
                                        for (int j = 0; j < mapPawns.Count; j++)
                                        {
                                            if (mapPawns[j].health != null && mapPawns[j].health.hediffSet != null && mapPawns[j].health.hediffSet.HasHediff(TorannMagicDefOf.TM_PredictionHD, false) && mapPawns[j].IsColonist)
                                            {
                                                CompAbilityUserMagic comp = mapPawns[j].GetComp<CompAbilityUserMagic>();
                                                if (comp != null && comp.MagicData != null)
                                                {
                                                    if (comp.predictionIncidentDef != null)
                                                    {
                                                        //Log.Message("attempt to execute prediction " + comp.predictionIncidentDef.defName);
                                                        if (comp.predictionIncidentDef == __instance.def && parms.GetHashCode() == comp.predictionHash)
                                                        {
                                                            comp.predictionIncidentDef = null;
                                                            if (__instance.def.letterLabel != null)
                                                            {
                                                                parms.customLetterLabel = "Predicted " + __instance.def.letterLabel;
                                                            }
                                                            else
                                                            {
                                                                parms.customLetterLabel = "Predicted " + __instance.def.label;
                                                            }
                                                            return true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        predictingPawnsAvailable.AddDistinct(mapPawns[j]);
                                                    }
                                                }
                                            }
                                        }
                                        for (int j = 0; j < predictingPawnsAvailable.Count; j++)
                                        {
                                            CompAbilityUserMagic comp = predictingPawnsAvailable[j].GetComp<CompAbilityUserMagic>();
                                            MagicPowerSkill ver = comp.MagicData.MagicPowerSkill_Prediction.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Prediction_ver");
                                            if (__instance.CanFireNow(parms) && !ModOptions.Constants.GetBypassPrediction() && Rand.Chance(.25f + (.05f * ver.level))) //up to 40% chance to predict, per chronomancer
                                            {
                                                if (__instance.def.category != null && (__instance.def.category == IncidentCategoryDefOf.ThreatBig || __instance.def.category == IncidentCategoryDefOf.ThreatSmall || __instance.def.category == IncidentCategoryDefOf.DeepDrillInfestation ||
                                                    __instance.def.category == IncidentCategoryDefOf.DiseaseAnimal || __instance.def.category == IncidentCategoryDefOf.DiseaseHuman || __instance.def.category == IncidentCategoryDefOf.Misc))
                                                {
                                                    //Log.Message("prediction is " + __instance.def.defName + " and can fire now: " + __instance.CanFireNow(parms, false));
                                                    int ticksTillIncident = Mathf.RoundToInt((Rand.Range(1800, 3600) * (1 + (.15f * ver.level))));  // from .72 to 1.44 hours, plus bonus (1.05 - 2.1)
                                                    //Log.Message("prediction of " + parms.GetHashCode());                                                                             //Log.Message("pushing " + __instance.def.defName + " to iq for " + ticksTillIncident  + " ticks");
                                                    comp.predictionIncidentDef = __instance.def;
                                                    comp.predictionTick = Find.TickManager.TicksGame + ticksTillIncident;
                                                    comp.predictionHash = parms.GetHashCode();
                                                    QueuedIncident iq = new QueuedIncident(new FiringIncident(__instance.def, null, parms), comp.predictionTick);
                                                    Find.Storyteller.incidentQueue.Add(iq);
                                                    string labelText = "TM_PredictionLetter".Translate(__instance.def.label);
                                                    string text = "TM_PredictionText".Translate(predictingPawnsAvailable[j].LabelShort, __instance.def.label, Mathf.RoundToInt(ticksTillIncident / 2500));
                                                    //Log.Message("attempting to push letter");
                                                    Find.LetterStack.ReceiveLetter(labelText, text, LetterDefOf.NeutralEvent, null);
                                                    int xpNum = Rand.Range(60, 120);
                                                    comp.MagicUserXP += xpNum;
                                                    MoteMaker.ThrowText(comp.Pawn.DrawPos, comp.Pawn.Map, "XP +" + xpNum, -1f);
                                                    __result = true;
                                                    return false;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (NullReferenceException ex)
                    {
                        return true;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(AbilityDecisionConditionalNode_CasterHealth), "CanContinueTraversing", null)]
        public class CasterHealth_Check_Patch
        {
            public static bool Prefix(AbilityDecisionConditionalNode_CasterHealth __instance, Pawn caster, ref bool __result)
            {
                bool flag = caster.health.summaryHealth.SummaryHealthPercent >= __instance.minHealth && caster.health.summaryHealth.SummaryHealthPercent <= __instance.maxHealth;
                if (__instance.invert)
                {
                    __result = !flag;
                }
                __result = flag;
                return false;
                //Log.Message("caster healthscale is " + caster.HealthScale + " and summary health pct is " + caster.health.summaryHealth.SummaryHealthPercent);
                //return true;
            }
        }

        [HarmonyPatch(typeof(HealthAIUtility), "ShouldSeekMedicalRest", null)]
        public class Arcane_ManaWeakness_BedRest_Patch
        {
            public static void Postfix(Pawn pawn, ref bool __result)
            {
                if (pawn.DestroyedOrNull() && pawn.health != null && pawn.health.hediffSet != null)
                {
                    if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ArcaneWeakness, false))
                    {
                        Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ArcaneWeakness);
                        if (hediff.Severity >= 10)
                        {
                            __result = true;
                        }
                    }
                    if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ManaSickness, false))
                    {
                        Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ManaSickness);
                        if (hediff.Severity >= 4)
                        {
                            __result = true;
                        }
                    }
                }
            }
        }

        //public static bool DaysWorthOfFoodCalc_Undead_Prefix(ref List<Pawn> pawns, List<ThingDefCount> extraFood, int tile, IgnorePawnsInventoryMode ignoreInventory, Faction faction, ref float __result, WorldPath path = null, float nextTileCostLeft = 0f, int caravanTicksPerMove = 3300, bool assumeCaravanMoving = true)
        //{
        //    for(int i = 0; i < pawns.Count; i++)
        //    {
        //        if (TM_Calc.IsUndead(pawns[i]))
        //        {
        //            pawns.Remove(pawns[i]);
        //            i--;
        //        }
        //    }
        //    return true;
        //}

        public static void Targeter_Casting_Postfix(Targeter __instance)
        {
            if (__instance.targetingSource != null && __instance.targetingSource.CasterIsPawn)
            {
                Pawn caster = __instance.targetingSource.CasterPawn;
                if (caster != null)
                {
                    IntVec3 targ = UI.MouseMapPosition().ToIntVec3();
                    if (targ != null && __instance.targetingSource.GetVerb != null && __instance.targetingSource.GetVerb.EquipmentSource == null && __instance.targetingSource.GetVerb.loadID == null) // && __instance.targetingSource.GetVerb.EquipmentSource == null)
                    {

                        if ((caster.Position - targ).LengthHorizontal > __instance.targetingSource.GetVerb.verbProps.range)
                        {
                            Texture2D icon = TexCommand.CannotShoot; // TM_RenderQueue.losIcon;
                            GenUI.DrawMouseAttachment(icon);
                        }
                        if (__instance.targetingSource.GetVerb.verbProps.requireLineOfSight && !__instance.targetingSource.GetVerb.TryFindShootLineFromTo(caster.Position, targ, out ShootLine resultingLine))
                        {
                            Texture2D icon = TM_RenderQueue.losIcon;
                            GenUI.DrawMouseAttachment(icon);
                        }
                        if (__instance.targetingSource.GetVerb.GetType() == typeof(Verb_LightSkip) && targ.InBounds(caster.Map) && targ.Roofed(caster.Map))
                        {
                            Texture2D icon = TexCommand.CannotShoot;
                            GenUI.DrawMouseAttachment(icon);
                        }
                    }
                }
            }
        }

        //public static void DaysWorthOfFoodCalc_Undead_Postfix(List<Pawn> pawns, List<ThingDefCount> extraFood, int tile, IgnorePawnsInventoryMode ignoreInventory, Faction faction, ref float __result, WorldPath path = null, float nextTileCostLeft = 0f, int caravanTicksPerMove = 3300, bool assumeCaravanMoving = true)
        //{
        //    if (pawns.Count != 0)
        //    {
        //        float undeadCount = 0;
        //        float undeadRatio = 0;
        //        for (int i = 0; i < pawns.Count; i++)
        //        {
        //            if (TM_Calc.IsUndead(pawns[i]))
        //            {
        //                undeadCount++;
        //            }
        //        }
        //        undeadRatio = undeadCount / pawns.Count;
        //        if (undeadRatio != 0)
        //        {
        //            __result = __result / (1 - undeadRatio);
        //        }
        //    }
        //}

        public static void WealthWatcher_ClassAdjustment_Postfix(WealthWatcher __instance, bool allowDuringInit = false)
        {
            float wealthPawns = Traverse.Create(root: __instance).Field(name: "wealthPawns").GetValue<float>();
            Map map = Traverse.Create(root: __instance).Field(name: "map").GetValue<Map>();
            if (wealthPawns != 0 && map != null)
            {
                foreach (Pawn item in map.mapPawns.PawnsInFaction(Faction.OfPlayer))
                {
                    CompAbilityUserMagic compMagic = item.GetComp<CompAbilityUserMagic>();
                    CompAbilityUserMight compMight = item.GetComp<CompAbilityUserMight>();
                    if (compMight != null && compMight.IsMightUser)
                    {
                        wealthPawns += 400 + (compMight.MightUserLevel * 20);
                    }
                    else if (compMagic != null && compMagic.IsMagicUser)
                    {
                        wealthPawns += 500 + (compMagic.MagicUserLevel * 15);
                    }
                }
                Traverse.Create(root: __instance).Field(name: "wealthPawns").SetValue(wealthPawns);
            }
        }

        [HarmonyPatch(typeof(CaravanFormingUtility), "AllSendablePawns", null)]
        public class AddNullException_Alerts_Patch
        {
            public static void Postfix(ref List<Pawn> __result)
            {
                if (__result != null)
                {
                    for (int i = 0; i < __result.Count; i++)
                    {
                        CompPolymorph comp = __result[i].GetComp<CompPolymorph>();
                        if (comp != null && comp.Original != null)
                        {
                            __result.Remove(__result[i]);
                            i--;
                        }
                    }
                }
            }
        }

        public static bool Get_IsColonist_Polymorphed(Pawn __instance, ref bool __result)
        {
            Pawn p = __instance;
            if (p != null && p.Faction == Faction.OfPlayerSilentFail)// __instance.GetComp<CompPolymorph>() != null && __instance.GetComp<CompPolymorph>().Original != null && __instance.GetComp<CompPolymorph>().Original.RaceProps.Humanlike)
            {
                CompPolymorph cp = __instance.GetComp<CompPolymorph>();
                if (cp != null && cp.Original != null && cp.Original.RaceProps.Humanlike)
                {
                    __result = true;
                    return false;
                }
                //if(__instance is TMPawnGolem)
                //{
                //    __result = true;
                //    return false;
                //}
                //__result = __instance.Faction != null && __instance.Faction.IsPlayer;
                //return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddJobGiverWorkOrders", null)]
        public class SkipPolymorph_UndraftedOrders_Patch
        {
            public static bool Prefix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts, bool drafted)
            {
                if (pawn.GetComp<CompPolymorph>() != null && pawn.GetComp<CompPolymorph>().Original != null)
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPriority(2000)]
        public static bool RimmuNation_CHCFCIR_Patch(Verb __instance, IntVec3 sourceSq, IntVec3 targetLoc, bool includeCorners, ref bool __result)
        {
            if (__instance != null && (__instance.verbProps.verbClass.ToString().Contains("AbilityUser") || __instance.verbProps.verbClass.ToString().Contains("TorannMagic")))
            {
                __result = true;
                VerbProperties verbProps = Traverse.Create(root: __instance).Field(name: "verbProps").GetValue<VerbProperties>();
                Thing caster = Traverse.Create(root: __instance).Field(name: "caster").GetValue<Thing>();
                if (verbProps.mustCastOnOpenGround && (!targetLoc.Standable(caster.Map) || caster.Map.thingGrid.CellContains(targetLoc, ThingCategory.Pawn)))
                {
                    __result = false;
                }
                if (verbProps.requireLineOfSight)
                {
                    if (!includeCorners)
                    {
                        if (!GenSight.LineOfSight(sourceSq, targetLoc, caster.Map, skipFirstCell: true))
                        {
                            __result = false;
                        }
                    }
                    else if (!GenSight.LineOfSightToEdges(sourceSq, targetLoc, caster.Map, skipFirstCell: true))
                    {
                        __result = false;
                    }
                }
                return false;
            }
            return true;
        }

        public static bool Pawn_PathFollower_Pathfinder_Prefix(Pawn pawn, IntVec3 c, ref int __result)
        {
            if (pawn != null && pawn.health != null && pawn.health.hediffSet != null && pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ArtifactPathfindingHD))
            {
                int x = c.x;
                IntVec3 position = pawn.Position;
                int num;
                if (x != position.x)
                {
                    int z = c.z;
                    IntVec3 position2 = pawn.Position;
                    if (z != position2.z)
                    {
                        num = pawn.TicksPerMoveDiagonal;
                        goto IL_0047;
                    }
                }
                num = pawn.TicksPerMoveCardinal;
                goto IL_0047;
                IL_0047:
                if (num > 450)
                {
                    num = 450;
                }
                if (pawn.CurJob != null)
                {
                    switch (pawn.jobs.curJob.locomotionUrgency)
                    {
                        case LocomotionUrgency.Amble:
                            num *= 3;
                            if (num < 60)
                            {
                                num = 60;
                            }
                            break;
                        case LocomotionUrgency.Walk:
                            num *= 2;
                            if (num < 50)
                            {
                                num = 50;
                            }
                            break;
                        case LocomotionUrgency.Jog:
                            num = num;
                            break;
                        case LocomotionUrgency.Sprint:
                            num = Mathf.RoundToInt((float)num * 0.75f);
                            break;
                    }
                }
                __result = num;
                return false;
            }
            return true;
        }


        public static bool MemoryThoughtHandler_PreventDisturbedRest_Prefix(MemoryThoughtHandler __instance, ThoughtDef def, Pawn ___pawn, Pawn otherPawn = null, Precept sourcePrecept = null)
        {
            //Pawn pawn = Traverse.Create(root: __instance).Field(name: "pawn").GetValue<Pawn>();
            if (___pawn != null && ___pawn.health != null && ___pawn.health.hediffSet != null && ___pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_ArtifactRenewalHD"), false))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(GenDraw), "DrawMeshNowOrLater", new Type[]
        {
            typeof(Mesh),
            typeof(Vector3),
            typeof(Quaternion),
            typeof(Material),
            typeof(bool)
        })]
        public class DrawMesh_Cloaks_Patch
        {
            public static bool Prefix(Mesh mesh, Vector3 loc, Quaternion quat, Material mat, bool drawNow)
            {
                if (mesh != null && loc != null && quat != null && mat != null)
                {
                    //Log.Message("item is " + mat.mainTexture.ToString() + " at y: " + loc.y);
                    //if (mat.mainTexture != null && mat.mainTexture.name != null)
                    //{
                    //    Log.Message("thing: " + mat.mainTexture.name + " at loc.y:" + loc.y);
                    //}
                    if (mat.mainTexture != null && ModOptions.Constants.GetCloaks().Contains(mat.mainTexture))//mat.mainTexture.name != null && mat.mainTexture.ToString() != null && (mat.mainTexture.ToString().Contains("demonlordcloak") || mat.mainTexture.name.Contains("opencloak")))
                    {
                        //Log.Message("main texture is: " + mat.mainTexture);
                        //Log.Message("pool contains " + ModOptions.Constants.GetCloaks()[0]);
                        //Log.Message("item is " + mat.mainTexture.ToString() + " at y: " + loc.y);
                        loc.y = 8.17f;  ///8.205f
                        //loc.y += .010f; //was 0.015f
                        if (ModOptions.Constants.GetCloaksNorth().Contains(mat.mainTexture))
                        {
                            //loc.y += .00175f; //was 0.006f
                            loc.y = 8.75f; //7.9961f; 8.209, 8.309
                        }

                        if (drawNow)
                        {
                            mat.SetPass(0);
                            Graphics.DrawMeshNow(mesh, loc, quat);
                        }
                        else
                        {
                            Graphics.DrawMesh(mesh, loc, quat, mat, 0);
                        }
                        return false;
                    }
                }
                return true;
            }
        }

        //code crashes linux and mac when a pawn dies
        //[HarmonyPatch(typeof(DeathActionWorker_Simple), "PawnDied", null)]
        //public class Undead_DeathActionWorker_Patch
        //{
        //    public static bool Prefix(Corpse corpse)
        //    {
        //        if(corpse != null && corpse.InnerPawn != null)
        //        {
        //            Pawn innerPawn = corpse.InnerPawn;
        //            if(innerPawn.health != null && innerPawn.health.hediffSet != null && (innerPawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD"), false) || innerPawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD"), false)))
        //            {
        //                Faction faction = new Faction();
        //                faction.def = TorannMagicDefOf.TM_SummonedFaction;
        //                innerPawn.SetFactionDirect(faction);
        //            }
        //        }
        //        return true;
        //    }
        //}

        [HarmonyPatch(typeof(Pawn), "CheckAcceptArrest", null)]
        public class CheckArrest_Undead_Patch
        {
            public static bool Prefix(Pawn __instance, Pawn arrester, ref bool __result)
            {
                if (TM_Calc.IsUndead(__instance))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Corpse), "PostMake", null)]
        public class Corpse_UndeadStage_Patch
        {
            public static void Postfix(Corpse __instance)
            {
                CompRottable compRottable = __instance.GetComp<CompRottable>();
                Pawn undeadPawn = __instance.InnerPawn;
                if (compRottable != null && undeadPawn != null)
                {
                    Hediff hediff = undeadPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_UndeadStageHD"), false);
                    if (hediff != null)
                    {
                        compRottable.RotProgress = hediff.Severity;
                    }
                }
            }
        }

        public static bool IntVec3Inbounds_NullCheck_Prefix(IntVec3 c, Map map, ref bool __result)
        {
            if (c != null && map != null)
            {
                return true;
            }
            __result = false;
            return false;
        }

        public static bool CompAbilityItem_Overlay_Prefix(CompAbilityItem __instance)
        {
            Graphic Overlay = Traverse.Create(root: __instance).Field(name: "Overlay").GetValue<Graphic>();
            if (Overlay != null)
            {
                return true;
            }
            return false;
        }

        public static bool CompRefuelable_DrawBar_Prefix(CompRefuelable __instance)
        {
            if (__instance.parent.def.defName == "TM_ArcaneCapacitor")
            {
                if (!__instance.HasFuel && __instance.Props.drawOutOfFuelOverlay)
                {
                    __instance.parent.Map.overlayDrawer.DrawOverlay(__instance.parent, OverlayTypes.OutOfFuel);
                }
                if (__instance.Props.drawFuelGaugeInMap)
                {
                    GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
                    r.center = __instance.parent.DrawPos + Vector3.up * 0.1f;
                    r.center.z -= .2f;
                    r.size = new Vector2(.5f, .15f);
                    r.fillPercent = __instance.FuelPercentOfMax;
                    r.filledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.6f, 0.0f, 0.6f));
                    r.unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f));
                    r.margin = 0.15f;
                    Rot4 rotation = __instance.parent.Rotation;
                    if (!rotation.IsHorizontal)
                    {
                        rotation.Rotate(RotationDirection.Clockwise);
                    }
                    r.rotation = rotation;
                    GenDraw.DrawFillableBar(r);
                }
                return false;
            }
            if (__instance.parent.def.defName == "TM_DimensionalManaPocket")
            {
                if (!__instance.HasFuel && __instance.Props.drawOutOfFuelOverlay)
                {
                    __instance.parent.Map.overlayDrawer.DrawOverlay(__instance.parent, OverlayTypes.OutOfFuel);
                }
                if (__instance.Props.drawFuelGaugeInMap)
                {
                    GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
                    r.center = __instance.parent.DrawPos + Vector3.up * 0.1f;
                    r.center.z -= .6f;
                    r.size = new Vector2(.5f, .15f);
                    r.fillPercent = __instance.FuelPercentOfMax;
                    r.filledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.6f, 0.0f, 0.6f));
                    r.unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f));
                    r.margin = 0.15f;
                    Rot4 rotation = __instance.parent.Rotation;
                    if (rotation.IsHorizontal)
                    {
                        rotation.Rotate(RotationDirection.Clockwise);
                    }
                    r.rotation = rotation;
                    GenDraw.DrawFillableBar(r);
                }
                return false;
            }
            return true;
        }

        public static bool AutoUndrafter_Undead_Prefix(AutoUndrafter __instance, Pawn ___pawn, ref bool __result)
        {
            //Pawn pawn = ___pawn; // Traverse.Create(root: __instance).Field(name: "pawn").GetValue<Pawn>();
            if (TM_Calc.IsUndead(___pawn))
            {
                __result = false;
                return false;
            }
            return true;
        }

        public static bool PawnRenderer_Blur_Prefix(PawnRenderer __instance, ref Vector3 drawLoc, Pawn ___pawn, Rot4? rotOverride = default(Rot4?), bool neverAimWeapon = false)
        {
            Pawn pawn = ___pawn; // Traverse.Create(root: __instance).Field(name: "pawn").GetValue<Pawn>();
            if (!pawn.DestroyedOrNull() && !pawn.Dead && !pawn.Downed)
            {
                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BlurHD))
                {
                    int blurTick = 0;
                    try
                    {
                        blurTick = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_BlurHD).TryGetComp<HediffComp_Blur>().blurTick;
                    }
                    catch (NullReferenceException ex)
                    {
                        return true;
                    }
                    if (blurTick > Find.TickManager.TicksGame - 10)
                    {
                        float blurMagnitude = (10 / (Find.TickManager.TicksGame - blurTick + 1)) + 5f;
                        Vector3 blurLoc = drawLoc;
                        blurLoc.x += Rand.Range(-.03f, .03f) * blurMagnitude;
                        //blurLoc.z += Rand.Range(-.01f, .01f) * blurMagnitude;
                        drawLoc = blurLoc;
                    }
                }

                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DiscordHD))
                {
                    Hediff hd = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_DiscordHD);
                    if (hd.Severity > 6f)
                    {
                        float blurMagnitude = (hd.Severity - 5f) * .03f;
                        Vector3 blurLoc = drawLoc;
                        blurLoc.x += Rand.Range(-blurMagnitude, blurMagnitude);
                        drawLoc = blurLoc;
                    }
                }

                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PredictionHD))
                {
                    int blurTick = 0;
                    try
                    {
                        blurTick = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_PredictionHD).TryGetComp<HediffComp_Prediction>().blurTick;
                    }
                    catch (NullReferenceException ex)
                    {
                        return true;
                    }
                    if (blurTick > Find.TickManager.TicksGame - 10)
                    {
                        float blurMagnitude = (10 / (Find.TickManager.TicksGame - blurTick + 1)) + 5f;
                        Vector3 blurLoc = drawLoc;
                        blurLoc.x += Rand.Range(-.03f, .03f) * blurMagnitude;
                        //blurLoc.z += Rand.Range(-.01f, .01f) * blurMagnitude;
                        drawLoc = blurLoc;
                    }
                }

                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_InvisibilityHD))
                {
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(PawnRenderer), "DrawPawnBody", null)]
        public class PawnRenderer_Undead_Prefix
        {
            private static bool Prefix(PawnRenderer __instance, Vector3 rootLoc, float angle, Rot4 facing, ref RotDrawMode bodyDrawType, PawnRenderFlags flags, Pawn ___pawn, out Mesh bodyMesh)
            {
                Pawn pawn = ___pawn; // Traverse.Create(root: __instance).Field(name: "pawn").GetValue<Pawn>();
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadStageHD")))
                {
                    if (settingsRef.changeUndeadPawnAppearance && pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")))
                    {
                        Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_UndeadStageHD"));
                        if (hediff.Severity < 1)
                        {
                            bodyDrawType = RotDrawMode.Rotting;
                        }
                        else
                        {
                            bodyDrawType = RotDrawMode.Dessicated;
                        }
                    }
                    if (settingsRef.changeUndeadAnimalAppearance && pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")))
                    {
                        Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_UndeadStageHD"));
                        //if (hediff.Severity < 1)
                        //{
                            bodyDrawType = RotDrawMode.Rotting;
                        //    }
                        //    else
                        //    {
                        //        bodyDrawType = RotDrawMode.Dessicated;
                        //    }
                    }
                }
                bodyMesh = null;
                return true;
            }
        }

        //public static bool PawnRenderer_Undead_Prefix(PawnRenderer __instance, Vector3 rootLoc, float angle, Rot4 facing, ref RotDrawMode bodyDrawType, PawnRenderFlags flags, Pawn ___pawn, Mesh bodyMesh)
        //{
        //    Pawn pawn = ___pawn; // Traverse.Create(root: __instance).Field(name: "pawn").GetValue<Pawn>();
        //    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
        //    if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadStageHD")))
        //    {
        //        if (settingsRef.changeUndeadPawnAppearance && pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")))
        //        {
        //            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_UndeadStageHD"));
        //            if (hediff.Severity < 1)
        //            {
        //                bodyDrawType = RotDrawMode.Rotting;
        //            }
        //            else
        //            {
        //                bodyDrawType = RotDrawMode.Dessicated;
        //            }
        //        }
        //        if (settingsRef.changeUndeadAnimalAppearance && pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")))
        //        {
        //            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_UndeadStageHD"));
        //            if (hediff.Severity < 1)
        //            {
        //                bodyDrawType = RotDrawMode.Rotting;
        //            }
        //            else
        //            {
        //                bodyDrawType = RotDrawMode.Dessicated;
        //            }
        //        }
        //    }
        //    bodyMesh = null;
        //    return true;
        //}

        public static bool PawnRenderer_UndeadInternal_Prefix(PawnRenderer __instance, ref Vector3 rootLoc, float angle, bool renderBody, Rot4 bodyFacing, ref RotDrawMode bodyDrawType, PawnRenderFlags flags, Pawn ___pawn, PawnGraphicSet ___graphics)
        {
            //Pawn pawn = Traverse.Create(root: __instance).Field(name: "pawn").GetValue<Pawn>();
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (___pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadStageHD")))
            {
                if (settingsRef.changeUndeadPawnAppearance && ___pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")))
                {
                    Hediff hediff = ___pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_UndeadStageHD"));
                    if (hediff.Severity < 1)
                    {
                        bodyDrawType = RotDrawMode.Rotting;
                    }
                    else
                    {
                        bodyDrawType = RotDrawMode.Dessicated;
                    }
                }
                if (settingsRef.changeUndeadAnimalAppearance && ___pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")))
                {
                    //Hediff hediff = ___pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_UndeadStageHD"));
                    //if (hediff.Severity < 1)
                    //{
                        bodyDrawType = RotDrawMode.Rotting;
                    //}
                    //else
                    //{
                    //    bodyDrawType = RotDrawMode.Dessicated;
                    //}
                }
            }
            if (___pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BirdflightHD))
            {
                Hediff hd = ___pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_BirdflightHD, false);
                ___graphics.ClearCache();
                HediffComp_LowFlight hd_lf = hd.TryGetComp<HediffComp_LowFlight>();
                ___graphics.nakedGraphic = hd_lf.GetActiveGraphic;
                Thing carriedThing = ___pawn.carryTracker.CarriedThing;
                if (carriedThing != null)
                {
                    rootLoc.y += 0.037f;
                }
            }
            return true;
        }

        //[HarmonyPatch(typeof(PawnGraphicSet), "ResolveAllGraphics", null)]
        //public class ResolveFlyingPawn_Graphics_Postfix
        //{
        //    public static void Postfix(PawnGraphicSet __instance, Pawn ___pawn, Graphic ___nakedGraphic)
        //    {
        //        if (___pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BirdflightHD))
        //        {
        //            Hediff hd = ___pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_BirdflightHD, false);
        //            HediffComp_LowFlight hd_lf = hd.TryGetComp<HediffComp_LowFlight>();
        //            ___nakedGraphic = hd_lf.GetActiveGraphic;
        //        }
        //    }
        //}

        public static void TurretGunTick_Overdrive_Postfix(Building_TurretGun __instance, ref int ___burstCooldownTicksLeft, ref int ___burstWarmupTicksLeft)
        {
            Thing overdriveThing = __instance;
            if (!overdriveThing.DestroyedOrNull() && overdriveThing.Map != null)
            {
                //int burstCooldownTicksLeft = Traverse.Create(root: __instance).Field(name: "burstCooldownTicksLeft").GetValue<int>();
                //int burstWarmupTicksLeft = Traverse.Create(root: __instance).Field(name: "burstWarmupTicksLeft").GetValue<int>();
                List<Pawn> mapPawns = ModOptions.Constants.GetOverdrivePawnList();
                if (mapPawns != null && mapPawns.Count > 0)
                {
                    for (int i = 0; i < mapPawns.Count; i++)
                    {
                        Pawn pawn = mapPawns[i];
                        if (!pawn.DestroyedOrNull() && pawn.RaceProps.Humanlike && pawn.story != null)
                        {
                            CompAbilityUserMagic comp = pawn.GetComp<CompAbilityUserMagic>();
                            if (comp.IsMagicUser && comp.overdriveBuilding != null)
                            {
                                if (overdriveThing == comp.overdriveBuilding)
                                {
                                    if (___burstCooldownTicksLeft >= 5)
                                    {
                                        //Traverse.Create(root: __instance).Field(name: "burstCooldownTicksLeft").SetValue(burstCooldownTicksLeft -= 1 + Rand.Range(0, comp.MagicData.MagicPowerSkill_Overdrive.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Overdrive_pwr").level));
                                        ___burstCooldownTicksLeft -= 1 + Rand.Range(0, comp.MagicData.MagicPowerSkill_Overdrive.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Overdrive_pwr").level);
                                    }
                                    if (___burstWarmupTicksLeft >= 5)
                                    {
                                        //Traverse.Create(root: __instance).Field(name: "burstWarmupTicksLeft").SetValue(burstCooldownTicksLeft -= 5);
                                        ___burstWarmupTicksLeft -= 5;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void PowerCompTick_Overdrive_Postfix(CompPowerPlant __instance)
        {
            Thing overdriveThing = __instance.parent;
            if (overdriveThing != null && __instance.PowerOn && __instance.powerOutputInt != 0)
            {
                List<Pawn> mapPawns = ModOptions.Constants.GetOverdrivePawnList();
                for (int i = 0; i < mapPawns.Count; i++)
                {
                    Pawn pawn = mapPawns[i];
                    if (!pawn.DestroyedOrNull() && pawn.RaceProps.Humanlike && pawn.story != null)
                    {
                        CompAbilityUserMagic comp = pawn.GetComp<CompAbilityUserMagic>();
                        if (comp.IsMagicUser && comp.overdriveBuilding != null)
                        {
                            if (overdriveThing == comp.overdriveBuilding)
                            {
                                __instance.powerOutputInt = comp.overdrivePowerOutput;
                            }
                        }
                    }
                }
            }
        }

        public static void DiseaseHuman_Candidates_Patch(ref IEnumerable<Pawn> __result)
        {
            List<Pawn> tempList = __result.ToList();
            List<Pawn> removalList = new List<Pawn>();
            removalList.Clear();
            for (int i = 0; i < tempList.Count(); i++)
            {
                if (tempList[i].health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")) || tempList[i].health.hediffSet.HasHediff(HediffDef.Named("TM_LichHD")) || tempList[i].health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")))
                {
                    removalList.Add(tempList[i]);
                }
                TMPawnGolem pg = tempList[i] as TMPawnGolem;
                if(pg != null)
                {
                    removalList.Add(tempList[i]);
                }
            }
            __result = tempList.Except(removalList);
        }

        public static void SelfTame_Candidates_Patch(Map map, ref IEnumerable<Pawn> __result)
        {
            List<Pawn> tempList = __result.ToList();
            List<Pawn> removalList = new List<Pawn>();
            removalList.Clear();
            for (int i = 0; i < tempList.Count(); i++)
            {
                if (tempList[i].def.thingClass.ToString() == "TorannMagic.TMPawnSummoned")
                {
                    removalList.Add(tempList[i]);
                }
            }
            __result = tempList.Except(removalList);
        }

        public static bool DrawRadiusRing_Patch(IntVec3 center, float radius)
        {
            if (radius > GenRadial.MaxRadialPatternRadius)
            {
                return false;
            }
            return true;
        }

        //public static void Get_MaxDrawRadius_Patch(ref float __result)
        //{
        //    __result = 250f;
        //}

        public static bool TryGiveThoughts_PrefixPatch(ref Pawn victim)
        {
            if (victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD, false) || victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD) || victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_I) || victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_II) || victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_III))
            {
                return false;
            }
            return true;
        }

        public static bool AppendThoughts_ForHumanlike_PrefixPatch(ref Pawn victim)
        {
            if (victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD, false) || victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD) || victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_I) || victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_II) || victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_III))
            {
                return false;
            }
            return true;
        }

        public static bool AppendThoughts_Relations_PrefixPatch(ref Pawn victim)
        {
            if (victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD, false) || victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD) || victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_I) || victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_II) || victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_III))
            {
                return false;
            }
            return true;
        }

        public static void TM_PrisonLabor_JobDriver_Mine_Tweak(JobDriver __instance)
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (Rand.Chance(settingsRef.magicyteChance))
            {
                Thing thing = null;
                thing = ThingMaker.MakeThing(TorannMagicDefOf.RawMagicyte);
                thing.stackCount = Rand.Range(5, 12);
                if (thing != null)
                {
                    GenPlace.TryPlaceThing(thing, __instance.pawn.Position, __instance.pawn.Map, ThingPlaceMode.Near, null);
                }
            }
        }

        [HarmonyPriority(10)]
        public static void TM_Children_TrySpawnHatchedOrBornPawn_Tweak(ref Pawn pawn, Thing motherOrEgg, ref bool __result)
        {
            if (pawn.story != null && pawn.story.traits != null)
            {
                bool hasMagicTrait = false;
                bool hasFighterTrait = false;
                List<Trait> pawnTraits = pawn.story.traits.allTraits;
                for (int i = 0; i < pawnTraits.Count(); i++)
                {
                    if (pawnTraits[i].def == TorannMagicDefOf.Arcanist || pawnTraits[i].def == TorannMagicDefOf.Geomancer || pawnTraits[i].def == TorannMagicDefOf.Warlock || pawnTraits[i].def == TorannMagicDefOf.Succubus ||
                        pawnTraits[i].def == TorannMagicDefOf.InnerFire || pawnTraits[i].def == TorannMagicDefOf.HeartOfFrost || pawnTraits[i].def == TorannMagicDefOf.StormBorn || pawnTraits[i].def == TorannMagicDefOf.Technomancer ||
                        pawnTraits[i].def == TorannMagicDefOf.Paladin || pawnTraits[i].def == TorannMagicDefOf.Summoner || pawnTraits[i].def == TorannMagicDefOf.Druid || pawnTraits[i].def == TorannMagicDefOf.Necromancer ||
                        pawnTraits[i].def == TorannMagicDefOf.Lich || pawnTraits[i].def == TorannMagicDefOf.Priest || pawnTraits[i].def == TorannMagicDefOf.TM_Bard || pawnTraits[i].def == TorannMagicDefOf.TM_Gifted ||
                        pawnTraits[i].def == TorannMagicDefOf.Technomancer || pawnTraits[i].def == TorannMagicDefOf.BloodMage || pawnTraits[i].def == TorannMagicDefOf.Enchanter || pawnTraits[i].def == TorannMagicDefOf.Chronomancer ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_Wanderer || pawnTraits[i].def == TorannMagicDefOf.ChaosMage)
                    {
                        pawnTraits.Remove(pawnTraits[i]);
                        i--;
                        hasMagicTrait = true;
                    }
                    if (pawnTraits[i].def == TorannMagicDefOf.Gladiator || pawnTraits[i].def == TorannMagicDefOf.Bladedancer || pawnTraits[i].def == TorannMagicDefOf.TM_Sniper || pawnTraits[i].def == TorannMagicDefOf.Ranger ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_Psionic || pawnTraits[i].def == TorannMagicDefOf.Faceless || pawnTraits[i].def == TorannMagicDefOf.DeathKnight || pawnTraits[i].def == TorannMagicDefOf.PhysicalProdigy ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_Monk || pawnTraits[i].def == TorannMagicDefOf.TM_Wayfarer || pawnTraits[i].def == TorannMagicDefOf.TM_Commander || pawnTraits[i].def == TorannMagicDefOf.TM_SuperSoldier)
                    {
                        pawnTraits.Remove(pawnTraits[i]);
                        i--;
                        hasFighterTrait = true;
                    }
                    for (int j = 0; j < TM_ClassUtility.CustomClasses().Count; j++)
                    {
                        if (TM_ClassUtility.CustomClasses()[j].classTrait == pawnTraits[i].def)
                        {
                            pawnTraits.Remove(pawnTraits[i]);
                            i--;
                            if (TM_ClassUtility.CustomClasses()[j].isFighter)
                            {
                                hasFighterTrait = true;
                            }
                            if (TM_ClassUtility.CustomClasses()[j].isMage)
                            {
                                hasMagicTrait = true;
                            }
                        }
                    }
                }

                if (hasFighterTrait && hasMagicTrait)
                {
                    if (Rand.Chance(.5f))
                    {
                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Gifted, 0, false));
                    }
                    else
                    {
                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.PhysicalProdigy, 0, false));
                    }
                }
                else if (hasFighterTrait)
                {
                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.PhysicalProdigy, 0, false));
                }
                else if (hasMagicTrait)
                {
                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Gifted, 0, false));
                }
            }
        }

        public static bool Get_Staggered(Pawn_StanceTracker __instance, ref bool __result)
        {
            if (__instance.pawn.def == TorannMagicDefOf.TM_DemonR || __instance.pawn.def == TorannMagicDefOf.TM_HollowGolem)
            {
                __result = false;
                return false;
            }
            if (__instance.pawn.health != null && __instance.pawn.health.hediffSet != null)
            {
                if (__instance.pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BurningFuryHD) || __instance.pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MoveOutHD) || __instance.pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnrageHD))
                {
                    __result = false;
                    return false;
                }
            }
            return true;
        }

        public static bool StaggerFor_Patch(Pawn_StanceTracker __instance, int ticks)
        {
            if (__instance.pawn.def == TorannMagicDefOf.TM_DemonR || __instance.pawn.def == TorannMagicDefOf.TM_HollowGolem)
            {
                return false;
            }
            if (__instance.pawn.health != null && __instance.pawn.health.hediffSet != null && __instance.pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BurningFuryHD))
            {
                return false;
            }
            return true;
        }

        public static bool Get_Projectile_ES(Verb_LaunchProjectile __instance, ref ThingDef __result)
        {
            if (__instance.caster != null && __instance.caster is Pawn && __instance.Bursting)
            {
                Pawn pawn = __instance.caster as Pawn;
                CompAbilityUserMagic comp = pawn.TryGetComp<CompAbilityUserMagic>();
                if (comp != null && pawn.RaceProps.Humanlike && pawn.GetPosture() == PawnPosture.Standing && comp.HasTechnoWeapon && (pawn.story != null && pawn.story.traits != null &&
                    ((pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_TechnoWeapon, comp, null)))) &&
                    comp.useElementalShotToggle && pawn.equipment.Primary.def.IsRangedWeapon && pawn.equipment.Primary.def.techLevel >= TechLevel.Industrial)
                {
                    int verVal = comp.MagicData.MagicPowerSkill_TechnoWeapon.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoWeapon_ver").level;
                    if (Rand.Chance(.2f + .01f * verVal) && comp.Mana.CurLevel >= .02f)
                    {
                        ThingDef projectile = null;
                        float rnd = Rand.Range(0f, 1f);
                        if (rnd <= .33f) //fire
                        {
                            projectile = ThingDef.Named("Bullet_ES_Fire");
                            projectile.projectile.explosionRadius = __instance.verbProps.defaultProjectile.projectile.explosionRadius + (1 + .05f * verVal);
                            comp.Mana.CurLevel -= (.02f - .0008f * verVal);
                            comp.MagicUserXP += 4;
                        }
                        else if (rnd <= .66f) //ice
                        {
                            projectile = ThingDef.Named("Bullet_ES_Ice");
                            comp.Mana.CurLevel -= (.01f - .0004f * verVal);
                            comp.MagicUserXP += 2;
                        }
                        else //stun
                        {
                            projectile = ThingDef.Named("Bullet_ES_Lit");
                            comp.Mana.CurLevel -= (.015f - .0006f * verVal);
                            comp.MagicUserXP += 3;
                        }
                        __result = projectile;
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool Get_NightResting_Undead(Caravan __instance, ref bool __result)
        {
            List<Pawn> undeadCaravan = __instance.PawnsListForReading;
            bool allUndead = true;
            for (int i = 0; i < undeadCaravan.Count; i++)
            {
                if (undeadCaravan[i].IsColonist && !(undeadCaravan[i].health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")) || undeadCaravan[i].health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")) || undeadCaravan[i].health.hediffSet.HasHediff(HediffDef.Named("TM_LichHD"))))
                {
                    allUndead = false;
                    break;
                }
            }
            __result = !allUndead;
            return !allUndead;
        }

        [HarmonyPriority(2000)]
        public static void Pawn_Gizmo_ActionPatch(ref IEnumerable<Gizmo> __result, Pawn __instance)
        {
            if (Find.Selector.NumSelected == 1)
            {
                if (__instance == null || !__instance.RaceProps.Humanlike)
                {
                    return;
                }
                if ((__instance.Faction != null && !__instance.Faction.Equals(Faction.OfPlayer)) || __instance.story == null || __instance.story.traits == null || __instance.story.traits.allTraits.Count < 1)
                {
                    return;
                }
                if (__instance.IsColonist)
                {
                    CompAbilityUserMight compMight = __instance.TryGetComp<CompAbilityUserMight>();
                    if (compMight == null && compMight.IsMightUser)
                    {
                        return;
                    }
                    CompAbilityUserMagic compMagic = __instance.TryGetComp<CompAbilityUserMagic>();
                    if (compMagic == null && compMagic.IsMagicUser)
                    {
                        return;
                    }

                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();

                    var gizmoList = __result.ToList();
                    if (settingsRef.Wanderer && __instance.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted))
                    {
                        //Pawn p = __instance;
                        //Command_Action itemWanderer = new Command_Action
                        //{
                        //    action = new Action(delegate
                        //    {
                        //        TM_Action.PromoteWanderer(p);
                        //    }),
                        //    order = 51,
                        //    defaultLabel = TM_TextPool.TM_PromoteWanderer,
                        //    defaultDesc = TM_TextPool.TM_PromoteWandererDesc,
                        //    icon = ContentFinder<Texture2D>.Get("UI/wanderer", true),
                        //};
                        Command_Action itemWanderer = (Command_Action)compMagic.GetGizmoCommands("wanderer");
                        if (itemWanderer != null)
                        {
                            gizmoList.Add(itemWanderer);
                        }
                    }

                    if (settingsRef.Wayfarer && __instance.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy))
                    {
                        //Pawn p = __instance;
                        //Command_Action itemWayfarer = new Command_Action
                        //{

                        //    action = new Action(delegate
                        //    {
                        //        TM_Action.PromoteWayfarer(p);
                        //    }),
                        //    order = 52,
                        //    defaultLabel = TM_TextPool.TM_PromoteWayfarer,
                        //    defaultDesc = TM_TextPool.TM_PromoteWayfarerDesc,
                        //    icon = ContentFinder<Texture2D>.Get("UI/wayfarer", true),
                        //};
                        Command_Action itemWayfarer = (Command_Action)compMight.GetGizmoCommands("wayfarer");
                        if (itemWayfarer != null)
                        {
                            gizmoList.Add(itemWayfarer);
                        }
                    }
                    __result = gizmoList;
                }
            }
        }

        public static void Pawn_Gizmo_TogglePatch(ref IEnumerable<Gizmo> __result, ref Pawn __instance)
        {
            if (!(__instance != null && __result != null))
            {
                return;
            }
            if (__instance.Faction != Faction.OfPlayer || !(__instance.story != null && __instance.story.traits != null) || !__instance.RaceProps.Humanlike)
            {
                return;
            }

            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (Find.Selector.NumSelected == 1)
            {
                CompAbilityUserMagic compMagic = __instance.GetComp<CompAbilityUserMagic>();
                CompAbilityUserMight compMight = __instance.GetComp<CompAbilityUserMight>();
                var gizmoList = __result.ToList();
                bool canBecomeClassless = false;
                if (settingsRef.Wanderer && __instance.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted))
                {
                    //Pawn p = __instance;
                    //Command_Action itemWanderer = new Command_Action
                    //{
                    //    action = new Action(delegate
                    //    {
                    //        TM_Action.PromoteWanderer(p);
                    //    }),
                    //    order = 51,
                    //    defaultLabel = TM_TextPool.TM_PromoteWanderer,
                    //    defaultDesc = TM_TextPool.TM_PromoteWandererDesc,
                    //    icon = ContentFinder<Texture2D>.Get("UI/wanderer", true),
                    //};
                    Command_Action itemWanderer = (Command_Action)compMagic.GetGizmoCommands("wanderer");
                    if (itemWanderer != null)
                    {
                        canBecomeClassless = true;
                        gizmoList.Add(itemWanderer);
                    }
                }

                if (settingsRef.Wayfarer && __instance.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy))
                {
                    //Pawn p = __instance;
                    //Command_Action itemWayfarer = new Command_Action
                    //{

                    //    action = new Action(delegate
                    //    {
                    //        TM_Action.PromoteWayfarer(p);
                    //    }),
                    //    order = 52,
                    //    defaultLabel = TM_TextPool.TM_PromoteWayfarer,
                    //    defaultDesc = TM_TextPool.TM_PromoteWayfarerDesc,
                    //    icon = ContentFinder<Texture2D>.Get("UI/wayfarer", true),
                    //};
                    Command_Action itemWayfarer = (Command_Action)compMight.GetGizmoCommands("wayfarer");
                    if (itemWayfarer != null)
                    {
                        canBecomeClassless = true;
                        gizmoList.Add(itemWayfarer);
                    }
                }

                if (settingsRef.showGizmo)
                {
                    Enchantment.CompEnchantedItem itemComp = null;
                    if (__instance.apparel != null && __instance.apparel.WornApparel != null)
                    {
                        for (int i = 0; i < __instance.apparel.WornApparel.Count; i++)
                        {
                            if (__instance.apparel.WornApparel[i].def == TorannMagicDefOf.TM_Artifact_NecroticOrb)
                            {
                                itemComp = __instance.apparel.WornApparel[i].TryGetComp<Enchantment.CompEnchantedItem>();
                            }
                        }
                    }
                    if (compMagic == null && compMight == null && itemComp == null)
                    {
                        return;
                    }
                    if (!compMagic.IsMagicUser && !compMight.IsMightUser && itemComp == null && !canBecomeClassless)
                    {
                        return;
                    }

                    if (!canBecomeClassless)
                    {
                        Gizmo_EnergyStatus energyGizmo = new Gizmo_EnergyStatus
                        {
                            //All gizmo properties done in Gizmo_EnergyStatus
                            //Make it the first thing you see
                            pawn = __instance,
                            iComp = itemComp,
                            order = -101f
                        };

                        gizmoList.Add(energyGizmo);
                    }

                }
                if (__instance.story.traits.HasTrait(TorannMagicDefOf.Gladiator) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_Cleave, null, compMight))
                {
                    Command_Toggle ct = (Command_Toggle)compMight.GetGizmoCommands("cleave");
                    if (ct != null)
                    {
                        gizmoList.Add(ct);
                    }
                }
                if (__instance.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_CQC, null, compMight))
                {
                    Command_Toggle ct = (Command_Toggle)compMight.GetGizmoCommands("cqc");
                    if (ct != null)
                    {
                        gizmoList.Add(ct);
                    }
                }
                if (__instance.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic) || TM_ClassUtility.ClassHasHediff(TorannMagicDefOf.TM_PsionicHD, compMagic, compMight))
                {
                    Command_Toggle ct = (Command_Toggle)compMight.GetGizmoCommands("psiAugmentation");
                    if (ct != null)
                    {
                        gizmoList.Add(ct);
                    }
                    ct = (Command_Toggle)compMight.GetGizmoCommands("psiMindAttack");
                    if (ct != null)
                    {
                        gizmoList.Add(ct);
                    }
                }
                if ((__instance.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || __instance.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_TechnoBit, compMagic, compMight)) && compMagic.HasTechnoBit)
                {
                    Command_Toggle ct = (Command_Toggle)compMagic.GetGizmoCommands("technoBit");
                    if (ct != null)
                    {
                        gizmoList.Add(ct);
                    }
                    ct = (Command_Toggle)compMagic.GetGizmoCommands("technoRepair");
                    if (ct != null)
                    {
                        gizmoList.Add(ct);
                    }
                }

                if ((__instance.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || __instance.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_TechnoWeapon, compMagic, compMight)) && compMagic.HasTechnoWeapon)
                {
                    Command_Toggle ct = (Command_Toggle)compMagic.GetGizmoCommands("elementalShot");
                    if (ct != null)
                    {
                        gizmoList.Add(ct);
                    }
                }

                List<Gizmo> reorderGizmo = new List<Gizmo>();
                reorderGizmo.Clear();
                for (int i = 0; i < gizmoList.Count; i++)
                {
                    if (gizmoList[i] is Command_PawnAbility)
                    {
                        gizmoList[i].order = 500f;
                        reorderGizmo.Add(gizmoList[i]);
                        gizmoList.Remove(gizmoList[i]);
                        i--;
                    }
                }
                if (reorderGizmo.Count > 0)
                {
                    gizmoList.AddRange(reorderGizmo);
                }
                __result = gizmoList;
            }
        }

        [HarmonyPatch(typeof(Pawn), "Kill", null)]
        public static class Undead_Kill_Prefix
        {
            public static bool Prefix(ref Pawn __instance, DamageInfo? dinfo)
            {
                if (__instance != null)
                {
                    if ( __instance.health != null && __instance.Faction != null && (__instance.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD) || __instance.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD)))
                    {
                        __instance.SetFaction(null, null);
                    }
                    if (__instance.RaceProps != null && __instance.Faction != null && __instance.RaceProps.DeathActionWorker.GetType() == typeof(DeathWorker_Poppi))
                    {
                        __instance.SetFaction(null, null);
                    }
                    if(__instance.def.thingClass == typeof(TMPawnSummoned) && __instance.Faction != null)
                    {
                        __instance.SetFaction(null, null);
                    }
                    if (TM_Calc.IsMagicUser(__instance) && dinfo.HasValue && dinfo.Value.Instigator != null && dinfo.Value.Instigator is Pawn)
                    {
                        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(TorannMagicDefOf.TM_KilledMage, dinfo.Value.Instigator.Named(HistoryEventArgsNames.Doer), __instance.Named(HistoryEventArgsNames.Victim)));
                    }
                    if (TM_Calc.IsMightUser(__instance) && dinfo.HasValue && dinfo.Value.Instigator != null && dinfo.Value.Instigator is Pawn)
                    {
                        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(TorannMagicDefOf.TM_KilledFighter, dinfo.Value.Instigator.Named(HistoryEventArgsNames.Doer), __instance.Named(HistoryEventArgsNames.Victim)));
                    }
                    if (__instance.RaceProps != null && __instance.RaceProps.Humanlike && __instance.Faction != null && !__instance.Faction.IsPlayer && dinfo.HasValue && dinfo.Value.Instigator != null && (dinfo.Value.Instigator is Pawn) && dinfo.Value.Instigator.Faction != null && dinfo.Value.Instigator.Faction.IsPlayerSafe())
                    {
                        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(TorannMagicDefOf.TM_KilledHumanlike, dinfo.Value.Instigator.Named(HistoryEventArgsNames.Doer), __instance.Named(HistoryEventArgsNames.Victim)));
                    }
                    if (__instance.Map != null)
                    {
                        if (__instance.Map.mapPawns != null)
                        {
                            List<Pawn> mapPawns = __instance.Map.mapPawns.AllPawnsSpawned;
                            if (mapPawns != null && mapPawns.Count > 0)
                            {
                                foreach (Pawn p in mapPawns)
                                {
                                    if (p.health != null && p.health.hediffSet != null && p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DeathFieldHD))
                                    {
                                        p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_DeathFieldHD).TryGetComp<HediffComp_DeathField>().shouldStrike = true;
                                    }
                                }
                            }                            
                        }
                        List<Building> tmGolemBuildings = __instance.Map.listerBuildings.AllBuildingsColonistOfDef(TorannMagicDefOf.TM_HollowGolem_Workstation).ToList();
                        if(tmGolemBuildings != null && tmGolemBuildings.Count > 0)
                        {
                            foreach(Building b in tmGolemBuildings)
                            {
                                Building_TMGolemBase gb = b as Building_TMGolemBase;
                                if(gb != null && gb.GolemComp != null)
                                {
                                    foreach(TM_GolemUpgrade gu in gb.GolemComp.Upgrades)
                                    {
                                        if(gu.currentLevel > 0 && gu.enabled && gu.golemUpgradeDef == TorannMagicDefOf.TM_Golem_HollowOrbOfExtinguishedFlames)
                                        {
                                            float modifier = 1f;
                                            if(__instance.RaceProps != null)
                                            {
                                                if(__instance.RaceProps.IsMechanoid)
                                                {
                                                    modifier = 1.5f;
                                                }
                                                else if(__instance.RaceProps.Animal)
                                                {
                                                    modifier = .3f;
                                                }
                                            }
                                            
                                            Find.ResearchManager.ResearchPerformed(50*100 * modifier, null);
                                        }
                                    }
                                }
                            }
                        }                                                                
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Pawn), "MakeCorpse", null)]
        public static class DecomposeUndeadOnDeath
        {
            public static void Postfix(Pawn __instance, ref Corpse __result)
            {
                if (__result != null && __result.InnerPawn != null && __result.InnerPawn.health != null && __result.InnerPawn.health.hediffSet != null && __result.InnerPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadStageHD))
                {
                    CompRottable cr = __result.TryGetComp<CompRottable>();
                    Hediff hd = __result.InnerPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_UndeadStageHD);
                    if (cr != null && hd != null)
                    {
                        cr.RotImmediately();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_HealthTracker), "CheckForStateChange", null)]
        public static class CheckForStateChange_Patch
        {
            public static FieldInfo pawn = typeof(Pawn_HealthTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            public static MethodBase MakeDowned = typeof(Pawn_HealthTracker).GetMethod("MakeDowned", BindingFlags.Instance | BindingFlags.NonPublic);
            public static MethodBase MakeUnDowned = typeof(Pawn_HealthTracker).GetMethod("MakeUnDowned", BindingFlags.Instance | BindingFlags.NonPublic);

            public static bool Prefix(Pawn_HealthTracker __instance, DamageInfo? dinfo, Hediff hediff) //CheckForStateChange_
            {
                Traverse traverse = Traverse.Create(__instance);
                Pawn pawn = (Pawn)CheckForStateChange_Patch.pawn.GetValue(__instance);

                bool flag = pawn != null && dinfo.HasValue && hediff != null;
                bool result;
                if (flag)
                {
                    CompAbilityUserMagic comp = pawn.GetComp<CompAbilityUserMagic>();
                    bool flagChrono = comp != null && comp.IsMagicUser && comp.recallSet;
                    if (flagChrono || (dinfo.Value.Def == TMDamageDefOf.DamageDefOf.TM_DisablingBlow || dinfo.Value.Def == TMDamageDefOf.DamageDefOf.TM_Whirlwind || dinfo.Value.Def == TMDamageDefOf.DamageDefOf.TM_GrapplingHook || dinfo.Value.Def == TMDamageDefOf.DamageDefOf.TM_DisablingShot || dinfo.Value.Def == TMDamageDefOf.DamageDefOf.TM_Tranquilizer) || TM_Calc.IsUndeadNotVamp(pawn))
                    {
                        bool flag2 = !__instance.Dead;
                        if (flag2)
                        {
                            bool flag3 = traverse.Method("ShouldBeDead", new object[0]).GetValue<bool>() && CheckForStateChange_Patch.pawn != null;
                            if (flag3)
                            {
                                bool flag4 = !pawn.Destroyed;
                                if (flag4)
                                {
                                    if (comp != null && comp.IsMagicUser && comp.recallSet)
                                    {
                                        int pwrVal = comp.MagicData.MagicPowerSkill_Recall.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Recall_pwr").level;
                                        if (pwrVal == 3 || (pwrVal >= 1 && Rand.Chance(.5f)))
                                        {
                                            TM_Action.DoRecall(pawn, comp, true);
                                            result = false;
                                            return false;
                                        }
                                    }
                                    pawn.Kill(dinfo, hediff);
                                }
                                result = false;
                                return result;
                            }
                            bool flag5 = !__instance.Downed;
                            if (flag5)
                            {
                                bool flag6 = traverse.Method("ShouldBeDowned", new object[0]).GetValue<bool>() && CheckForStateChange_Patch.pawn != null;
                                if (flag6)
                                {
                                    if (comp != null && comp.IsMagicUser && comp.recallSet)
                                    {
                                        int pwrVal = comp.MagicData.MagicPowerSkill_Recall.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Recall_pwr").level;
                                        if (pwrVal == 3 || (pwrVal >= 2 && Rand.Chance(.5f)))
                                        {
                                            TM_Action.DoRecall(pawn, comp, false);
                                            result = false;
                                            return false;
                                        }
                                    }
                                    float num = (!pawn.RaceProps.Animal) ? 0f : 0f;
                                    bool flag7 = !__instance.forceIncap && dinfo.HasValue && dinfo.Value.Def.ExternalViolenceFor(pawn) && (pawn.Faction == null || !pawn.Faction.IsPlayer) && !pawn.IsPrisonerOfColony && pawn.RaceProps.IsFlesh && Rand.Value < num;
                                    if (flag7)
                                    {
                                        pawn.Kill(dinfo, null);
                                        result = false;
                                        return result;
                                    }
                                    bool flagUndead = dinfo.HasValue && TM_Calc.IsUndeadNotVamp(pawn) && !pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_LichHD);
                                    if (flagUndead)
                                    {
                                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ghost, pawn.DrawPos, pawn.Map, .65f, .05f, .05f, .4f, 0, Rand.Range(3, 4), Rand.Range(-15, 15), 0);
                                        pawn.Kill(dinfo, null);
                                        result = false;
                                        return result;
                                    }
                                    __instance.forceIncap = false;
                                    CheckForStateChange_Patch.MakeDowned.Invoke(__instance, new object[]
                                    {
                                    dinfo,
                                    hediff
                                    });
                                    result = false;
                                    return result;
                                }
                                else
                                {
                                    bool flag8 = !__instance.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
                                    if (flag8)
                                    {
                                        bool flag9 = pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null && pawn.jobs != null && pawn.CurJob != null;
                                        if (flag9)
                                        {
                                            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                                        }
                                        bool flag10 = pawn.equipment != null && pawn.equipment.Primary != null;
                                        if (flag10)
                                        {
                                            bool inContainerEnclosed = pawn.InContainerEnclosed;
                                            if (inContainerEnclosed)
                                            {
                                                pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment.Primary, pawn.holdingOwner);
                                            }
                                            else
                                            {
                                                bool spawnedOrAnyParentSpawned = pawn.SpawnedOrAnyParentSpawned;
                                                if (spawnedOrAnyParentSpawned)
                                                {
                                                    ThingWithComps thingWithComps;
                                                    pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out thingWithComps, pawn.PositionHeld, true);
                                                }
                                                else
                                                {
                                                    pawn.equipment.DestroyEquipment(pawn.equipment.Primary);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                bool flag11 = !traverse.Method("ShouldBeDowned", new object[0]).GetValue<bool>() && CheckForStateChange_Patch.pawn != null;
                                if (flag11)
                                {
                                    CheckForStateChange_Patch.MakeUnDowned.Invoke(__instance, null);
                                    result = false;
                                    return result;
                                }
                            }
                        }
                        result = false;
                    }
                    else
                    {
                        result = true;
                    }
                }
                else
                {
                    result = true;
                }
                return result;
            }

            private static void Postfix(Pawn_HealthTracker __instance, DamageInfo? dinfo, Hediff hediff)
            {
                Traverse traverse = Traverse.Create(__instance);
                Pawn pawn = (Pawn)CheckForStateChange_Patch.pawn.GetValue(__instance);
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();

                bool flag = pawn != null && dinfo.HasValue && hediff != null;
                if (flag)
                {
                    if (pawn != null && !pawn.IsColonist)
                    {
                        if (pawn.Downed && !pawn.Dead && !pawn.IsPrisoner)
                        {
                            if(pawn is TMPawnGolem)
                            {
                                pawn.Kill(dinfo, hediff);
                            }
                            if (pawn.Map == null)
                            {
                                //Log.Message("Tried to do death retaliation in a null map.");
                            }
                            else
                            {
                                float chc = 1f * settingsRef.deathRetaliationChance;
                                if (Rand.Chance(chc))
                                {
                                    CompAbilityUserMagic compMagic = pawn.GetComp<CompAbilityUserMagic>();
                                    CompAbilityUserMight compMight = pawn.GetComp<CompAbilityUserMight>();
                                    if (compMagic != null && compMagic.IsMagicUser)
                                    {
                                        compMagic.canDeathRetaliate = true;
                                    }
                                    else if (compMight != null && compMight.IsMightUser)
                                    {
                                        compMight.canDeathRetaliate = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        //[HarmonyPatch(typeof(Hediff_Injury), "PostAdd", null)]
        //public class Hediff_Injury_RemoveError_Prefix
        //{            
        //    public static bool Prefix(Hediff_Injury __instance, DamageInfo? dinfo)
        //    {
        //        return true;
        //        if(__instance.Part != null && __instance.Part.coverageAbs <= 0f)
        //        {
        //            __instance.PostAdd(dinfo);
        //            return false;
        //        }
        //        return true;
        //    }
        //}

        [HarmonyPatch(typeof(Pawn), "PreApplyDamage", null)]
        public class Pawn_PreApplyDamage
        {
            public static bool Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
            {
                Thing instigator = dinfo.Instigator as Thing;
                absorbed = false;
                if (instigator != null && !absorbed)
                {
                    if (__instance.health != null && __instance.health.hediffSet != null)
                    {
                        if (__instance.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BurningFuryHD, false))
                        {
                            dinfo.SetAmount(dinfo.Amount * 0.65f);
                        }
                        if(__instance.health.hediffSet.HasHediff(TorannMagicDefOf.TM_FrailtyHD, false))
                        {
                            Hediff hd = __instance.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_FrailtyHD);
                            if (hd != null)
                            {
                                dinfo.SetAmount(dinfo.Amount + (dinfo.Amount * hd.Severity));
                            }
                        }
                        //symbiosis shell
                        if(__instance.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SymbiosisHD))
                        {
                            Hediff hd = __instance.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SymbiosisHD);
                            HediffComp_SymbiosisHost hdh = hd.TryGetComp<HediffComp_SymbiosisHost>();
                            if(hdh != null && hdh.symbiote != null)
                            {
                                float verVal = TM_Calc.GetSkillVersatilityLevel(hdh.symbiote, TorannMagicDefOf.TM_Symbiosis);
                                float sAmt = dinfo.Amount;
                                if(verVal >= 3)
                                {
                                    sAmt = dinfo.Amount * .5f;
                                }
                                else if(verVal >= 2)
                                {
                                    sAmt = dinfo.Amount * .6f;
                                }
                                else if(verVal >= 1)
                                {
                                    sAmt = dinfo.Amount * .8f;
                                }

                                DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_SymbiosisDD, sAmt, 2, -1, dinfo.Instigator, dinfo.HitPart, null);
                                hdh.symbiote.TakeDamage(dinfo2);
                                dinfo.SetAmount(dinfo.Amount * (.75f - (.05f * verVal)));
                                if (__instance.Map != null)
                                {
                                    TM_Action.DisplayShield(__instance, dinfo.Amount);
                                }
                                TM_Action.DisplayShieldHit(hdh.symbiote, dinfo2);
                            }
                        }
                    }
                    if (dinfo.Def != null && dinfo.Instigator != null && dinfo.Instigator.Map != null && dinfo.Instigator is Pawn)
                    {
                        Pawn p = dinfo.Instigator as Pawn;
                        if (p.story != null && p.story.traits != null)
                        {
                            if (p.story.traits.HasTrait(TorannMagicDefOf.TM_GiantsBloodTD) && TM_Calc.IsUsingMelee(p))
                            {
                                float amt = dinfo.Amount;
                                amt *= 1.25f;
                                dinfo.SetAmount(amt);
                            }
                        }
                        if (p.health != null && p.health.hediffSet != null)
                        {
                            if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MindOverBodyHD) && dinfo.Def == DamageDefOf.Blunt && dinfo.Weapon != null && dinfo.Weapon.defName == "Human")
                            {
                                Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MindOverBodyHD);
                                dinfo.SetAmount(Mathf.RoundToInt(dinfo.Amount + hediff.Severity + Rand.Range(0f, 3f)));
                                dinfo.Def = TMDamageDefOf.DamageDefOf.TM_ChiFist;
                            }

                            if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnrageHD) && TM_Calc.IsUsingMelee(p))
                            {
                                Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnrageHD);
                                dinfo.SetAmount(Mathf.RoundToInt(dinfo.Amount * (1f + hediff.Severity)));
                            }

                            if (p.equipment != null && p.equipment.Primary != null)
                            {
                                Thing wpn = p.equipment.Primary;
                                if (wpn.def.IsRangedWeapon)
                                {                                    
                                    if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BowTrainingHD))
                                    {
                                        if (wpn.def.Verbs.FirstOrDefault<VerbProperties>().defaultProjectile.projectile.damageDef.defName == "Arrow" || wpn.def.defName.Contains("Bow") || wpn.def.defName.Contains("bow") || wpn.def.Verbs.FirstOrDefault<VerbProperties>().defaultProjectile.projectile.damageDef.defName.Contains("Arrow") || wpn.def.Verbs.FirstOrDefault<VerbProperties>().defaultProjectile.projectile.damageDef.defName.Contains("arrow"))
                                        {
                                            Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_BowTrainingHD);
                                            float amt = dinfo.Amount;
                                            if (hediff.Severity < 1)
                                            {
                                                amt = dinfo.Amount * 1.2f;
                                            }
                                            else if (hediff.Severity < 2)
                                            {
                                                amt = dinfo.Amount * 1.4f;
                                            }
                                            else if (hediff.Severity < 3)
                                            {
                                                amt = dinfo.Amount * 1.6f;
                                            }
                                            else
                                            {
                                                amt = dinfo.Amount * 1.8f;
                                            }
                                            dinfo.SetAmount(amt);
                                        }
                                    }

                                    CompAbilityUserMight compMight = p.TryGetComp<CompAbilityUserMight>();
                                    if (p.IsInvisible() && compMight != null && compMight.IsMightUser && compMight.MightData != null)
                                    {
                                        MightPowerSkill mps = compMight.MightData.GetSkill_Power(TorannMagicDefOf.TM_ShadowSlayer);
                                        if (mps != null)
                                        {
                                            int skillLevel = (2 * mps.level);
                                            dinfo.SetAmount(dinfo.Amount + skillLevel);
                                        }
                                    }
                                }                           
                            }
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Pawn_HealthTracker), "PreApplyDamage", null)]
        public class PreApplyDamage_Patch
        {
            public static FieldInfo pawn = typeof(Pawn_HealthTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            public static bool Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
            {
                Traverse traverse = Traverse.Create(__instance);
                Pawn pawn = (Pawn)PreApplyDamage_Patch.pawn.GetValue(__instance);
                if (dinfo.Def != null && pawn != null && !pawn.Downed)
                {
                    if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffTimedInvulnerable")))
                    {
                        absorbed = true;
                        return false;
                    }
                    if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ArtifactBlockHD) && Rand.Chance(.4f))
                    {
                        Thing instigator = dinfo.Instigator as Thing;
                        if (instigator != null)
                        {
                            if ((dinfo.Weapon != null && !dinfo.Def.isExplosive) || dinfo.WeaponBodyPartGroup != null)
                            {
                                Vector3 drawPos = pawn.DrawPos;
                                float drawAngle = (instigator.DrawPos - drawPos).AngleFlat();
                                drawPos.x += Mathf.Clamp(((instigator.DrawPos.x - drawPos.x) / 5f) + Rand.Range(-.1f, .1f), -.45f, .45f);
                                drawPos.z += Mathf.Clamp(((instigator.DrawPos.z - drawPos.z) / 5f) + Rand.Range(-.1f, .1f), -.45f, .45f);
                                TM_MoteMaker.ThrowSparkFlashMote(drawPos, pawn.Map, 1f);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BracerBlock_NoFlash, drawPos, pawn.Map, .45f, .23f, 0f, .07f, 0, 0, 0, drawAngle);
                                SoundInfo info = SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map, false), MaintenanceType.None);
                                TorannMagicDefOf.TM_MetalImpact.PlayOneShot(info);
                                dinfo.SetAmount(0);
                                absorbed = true;
                                return false;
                            }
                        }
                    }
                    if(pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_FleshGolem_BracerGuardHD))
                    {
                        int count = 0;
                        Hediff hd = null;
                        foreach (Hediff h in pawn.health.hediffSet.hediffs)
                        {
                            if(h.def == TorannMagicDefOf.TM_FleshGolem_BracerGuardHD)
                            {
                                hd = h;
                                count++;
                            }
                        }
                        if(hd != null && Rand.Chance(hd.Severity * count))
                        {
                            Thing instigator = dinfo.Instigator as Thing;
                            if (instigator != null)
                            {
                                if ((dinfo.Weapon != null && !dinfo.Def.isExplosive) || dinfo.WeaponBodyPartGroup != null)
                                {
                                    Vector3 drawPos = pawn.DrawPos;
                                    float drawAngle = (instigator.DrawPos - drawPos).AngleFlat();
                                    drawPos.x += Mathf.Clamp(((instigator.DrawPos.x - drawPos.x) / 5f) + Rand.Range(-.1f, .1f), -.45f, .45f);
                                    drawPos.z += Mathf.Clamp(((instigator.DrawPos.z - drawPos.z) / 5f) + Rand.Range(-.1f, .1f), -.45f, .45f);
                                    TM_MoteMaker.ThrowSparkFlashMote(drawPos, pawn.Map, 1f);
                                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BracerBlock_NoFlash, drawPos, pawn.Map, .45f, .23f, 0f, .07f, 0, 0, 0, drawAngle);
                                    SoundInfo info = SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map, false), MaintenanceType.None);
                                    TorannMagicDefOf.TM_MetalImpact.PlayOneShot(info);
                                    dinfo.SetAmount(0);
                                    absorbed = true;
                                    return false;
                                }
                            }
                        }
                    }
                    if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ArtifactDeflectHD) && Rand.Chance(.3f))
                    {
                        Thing instigator = dinfo.Instigator as Thing;
                        if (instigator != null)
                        {
                            if ((dinfo.Weapon != null && !dinfo.Def.isExplosive) || dinfo.WeaponBodyPartGroup != null)
                            {
                                Vector3 drawPos = pawn.DrawPos;
                                float drawAngle = (instigator.DrawPos - drawPos).AngleFlat();
                                drawPos.x += Mathf.Clamp(((instigator.DrawPos.x - drawPos.x) / 5f) + Rand.Range(-.1f, .1f), -.45f, .45f);
                                drawPos.z += Mathf.Clamp(((instigator.DrawPos.z - drawPos.z) / 5f) + Rand.Range(-.1f, .1f), -.45f, .45f);
                                TM_MoteMaker.ThrowSparkFlashMote(drawPos, pawn.Map, 1f);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BracerBlock, drawPos, pawn.Map, .45f, .23f, 0f, .07f, 0, 0, 0, drawAngle);
                                SoundInfo info = SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map, false), MaintenanceType.None);
                                TM_Action.DoReversalRandomTarget(dinfo, pawn, 0, 8f);
                                TorannMagicDefOf.TM_MetalImpact.PlayOneShot(info);
                                dinfo.SetAmount(0);
                                absorbed = true;
                                return false;
                            }
                        }
                    }
                    if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HTLShieldHD, false))
                    {
                        HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_HTLShieldHD, -dinfo.Amount);
                        TM_Action.DisplayShieldHit(pawn, dinfo);
                        absorbed = true;
                        return false;
                    }
                    if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MagicShieldHD, false))
                    {
                        HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_MagicShieldHD, -(dinfo.Amount * .004f));
                        TM_Action.DisplayShieldHit(pawn, dinfo);
                        absorbed = true;
                        return false;
                    }
                    if ((pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TechnoShieldHD) && dinfo.Amount <= 10) || (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TechnoShieldHD_I) && dinfo.Amount <= 13) || (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TechnoShieldHD_II) && dinfo.Amount <= 18) || (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TechnoShieldHD_III) && dinfo.Amount <= 30))
                    {
                        Thing instigator = dinfo.Instigator as Thing;
                        if (instigator != null && dinfo.Weapon != null && dinfo.Weapon.IsRangedWeapon)
                        {
                            Vector3 drawPos = pawn.DrawPos;
                            drawPos.x += ((instigator.DrawPos.x - drawPos.x) / 20f) + Rand.Range(-.2f, .2f);
                            drawPos.z += ((instigator.DrawPos.z - drawPos.z) / 20f) + Rand.Range(-.2f, .2f);
                            TM_MoteMaker.ThrowSparkFlashMote(drawPos, pawn.Map, 2f);
                            TM_Action.DoReversal(dinfo, pawn);
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_TechnoShield, pawn.DrawPos, pawn.Map, .9f, .1f, 0f, .05f, Rand.Range(-500, 500), 0, 0, Rand.Range(0, 360));
                            HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_TechnoShieldHD, -dinfo.Amount);
                            HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_TechnoShieldHD_I, -dinfo.Amount);
                            HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_TechnoShieldHD_II, -dinfo.Amount);
                            HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_TechnoShieldHD_III, -dinfo.Amount);
                            dinfo.SetAmount(0);
                            absorbed = true;
                            return false;

                        }
                    }
                    if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_StoneskinHD"), false))
                    {
                        HealthUtility.AdjustSeverity(pawn, HediffDef.Named("TM_StoneskinHD"), -1);
                        for (int m = 0; m < 4; m++)
                        {
                            Vector3 vectorOffset = pawn.DrawPos;
                            vectorOffset.x += (Rand.Range(-.3f, .3f));
                            vectorOffset.z += Rand.Range(-.3f, .3f);
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, vectorOffset, pawn.Map, Rand.Range(.15f, .35f), Rand.Range(.1f, .15f), 0, Rand.Range(.1f, .2f), Rand.Range(-20, 20), Rand.Range(.3f, .5f), Rand.Range(0, 360), Rand.Range(0, 360));
                        }
                        absorbed = true;
                        return false;
                    }
                    if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ProtectionBrandHD, false))
                    {
                        Hediff hd = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ProtectionBrandHD);
                        if (hd != null)
                        {
                            HediffComp_BrandingProtection hd_bp = hd.TryGetComp<HediffComp_BrandingProtection>();
                            if(hd_bp != null && hd_bp.canProtect)
                            {
                                hd_bp.TakeHit();
                                for (int m = 0; m < 4; m++)
                                {
                                    Vector3 vectorOffset = pawn.DrawPos;
                                    vectorOffset.x += (Rand.Range(-.3f, .3f));
                                    vectorOffset.z += Rand.Range(-.3f, .3f);
                                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_GlowingRuneA, vectorOffset, pawn.Map, Rand.Range(.1f, .25f), Rand.Range(.1f, .15f), 0, Rand.Range(.1f, .2f), Rand.Range(-10, 10), Rand.Range(.1f, .15f), Rand.Range(0, 360), Rand.Range(0, 360));
                                }
                                absorbed = true;
                                return false;
                            }
                        }                        
                    }
                    if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_BloodShieldHD"), false))
                    {
                        HealthUtility.AdjustSeverity(pawn, HediffDef.Named("TM_BloodShieldHD"), -dinfo.Amount);
                        for (int m = 0; m < 4; m++)
                        {
                            Effecter BloodShieldEffect = TorannMagicDefOf.TM_BloodShieldEffecter.Spawn();
                            BloodShieldEffect.Trigger(new TargetInfo(pawn.Position, pawn.Map, false), new TargetInfo(pawn.Position, pawn.Map, false));
                            BloodShieldEffect.Cleanup();
                        }
                        absorbed = true;
                        return false;
                    }
                    if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BlurHD, false) && !dinfo.Def.isExplosive)
                    {
                        float blurVal = .2f;
                        if (pawn.TryGetComp<CompAbilityUserMagic>()?.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_ver").level >= 11)
                        {
                            blurVal = .3f;
                        }
                        if (Rand.Chance(blurVal))
                        {
                            Hediff blur = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_BlurHD);
                            blur.TryGetComp<HediffComp_Blur>().blurTick = Find.TickManager.TicksGame;
                            absorbed = true;
                            return false;
                        }
                    }
                    if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PredictionHD, false) && !dinfo.Def.isExplosive)
                    {
                        Hediff prediction = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_PredictionHD);
                        if (Rand.Chance(prediction.Severity / 10f))
                        {
                            prediction.TryGetComp<HediffComp_Prediction>().blurTick = Find.TickManager.TicksGame;
                            absorbed = true;
                            return false;
                        }
                    }
                    if (TM_Calc.HasHateHediff(pawn) && dinfo.Amount > 0)
                    {
                        int hatePwr = 0;
                        int hateVer = 0;
                        int hateEff = 0;

                        CompAbilityUserMight comp = pawn.GetComp<CompAbilityUserMight>();
                        if (comp != null && comp.IsMightUser)
                        {
                            hatePwr = comp.MightData.MightPowerSkill_Shroud.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Shroud_pwr").level;
                            hateVer = comp.MightData.MightPowerSkill_Shroud.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Shroud_ver").level;
                            hateEff = comp.MightData.MightPowerSkill_Shroud.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Shroud_eff").level;
                        }

                        Hediff hediff = null;
                        for (int h = 0; h < pawn.health.hediffSet.hediffs.Count; h++)
                        {
                            if (pawn.health.hediffSet.hediffs[h].def.defName.Contains("TM_HateHD"))
                            {
                                hediff = pawn.health.hediffSet.hediffs[h];
                            }
                        }

                        HealthUtility.AdjustSeverity(pawn, hediff.def, (dinfo.Amount * (1 + (.1f * hateEff))));
                        if (hediff != null && hediff.Severity >= 20 && Rand.Chance(.1f * hateVer) && dinfo.Instigator != null && dinfo.Instigator is Pawn && dinfo.Instigator != pawn && (dinfo.Instigator.Position - pawn.Position).LengthHorizontal < 2)
                        {
                            TM_Action.DamageEntities(dinfo.Instigator, null, (dinfo.Amount * (1 + .2f * hatePwr)), TMDamageDefOf.DamageDefOf.TM_Spirit, pawn);
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_SpiritRetaliation, pawn.DrawPos, pawn.Map, Rand.Range(1f, 1.2f), Rand.Range(.1f, .15f), 0, Rand.Range(.1f, .2f), -600, 0, 0, Rand.Range(0, 360));
                            HealthUtility.AdjustSeverity(pawn, hediff.def, -(dinfo.Amount * (.8f - (.1f * hateEff))));
                        }

                    }
                    if (dinfo.Instigator != null && dinfo.Instigator is Pawn)
                    {
                        Pawn attacker = dinfo.Instigator as Pawn;
                        CompAbilityUserMight comp = attacker.GetComp<CompAbilityUserMight>();

                        if (attacker != null && !attacker.Destroyed && !attacker.Downed && !attacker.Dead && attacker != pawn && dinfo.Weapon != null && dinfo.Weapon.IsMeleeWeapon)
                        {
                            if ((attacker.story != null && attacker.story.traits != null && attacker.story.traits.HasTrait(TorannMagicDefOf.DeathKnight)) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_LifeSteal, null, comp))
                            {
                                Hediff hediff = null;
                                for (int h = 0; h < attacker.health.hediffSet.hediffs.Count; h++)
                                {
                                    if (attacker.health.hediffSet.hediffs[h].def.defName.Contains("TM_HateHD"))
                                    {
                                        hediff = attacker.health.hediffSet.hediffs[h];
                                    }
                                }
                                //&& TM_Calc.HasHateHediff(attacker)
                                int lifestealPwr = comp.MightData.MightPowerSkill_LifeSteal.FirstOrDefault((MightPowerSkill x) => x.label == "TM_LifeSteal_pwr").level;
                                int lifestealEff = comp.MightData.MightPowerSkill_LifeSteal.FirstOrDefault((MightPowerSkill x) => x.label == "TM_LifeSteal_eff").level;
                                int lifestealVer = comp.MightData.MightPowerSkill_LifeSteal.FirstOrDefault((MightPowerSkill x) => x.label == "TM_LifeSteal_ver").level;

                                TM_Action.DoAction_HealPawn(attacker, attacker, 1, dinfo.Amount * (TorannMagicDefOf.TM_LifeSteal.weaponDamageFactor + (.02f * lifestealPwr)));
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Siphon, attacker.DrawPos, attacker.Map, 1f, .1f, .15f, .5f, 600, 0, 0, Rand.Range(0, 360));
                                TM_MoteMaker.ThrowSiphonMote(attacker.DrawPos, attacker.Map, 1f);

                                if (hediff != null && lifestealEff > 0)
                                {
                                    HealthUtility.AdjustSeverity(attacker, hediff.def, dinfo.Amount * (.25f + .05f * lifestealEff));
                                    comp.Stamina.CurLevel += (dinfo.Amount * (float)(.1f * lifestealEff)) / 100;
                                }
                                if (lifestealVer > 0)
                                {
                                    Pawn ally = TM_Calc.FindNearbyInjuredPawnOther(attacker, 3, 0);
                                    if (ally != null)
                                    {
                                        TM_Action.DoAction_HealPawn(attacker, ally, 1, dinfo.Amount * ((TorannMagicDefOf.TM_LifeSteal.weaponDamageFactor - .05f) + (.01f * lifestealVer)));
                                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Siphon, ally.DrawPos, ally.Map, 1f, .1f, .15f, .5f, 600, 0, 0, Rand.Range(0, 360));
                                    }
                                }
                            }
                        }
                    }
                    CompAbilityUserMagic compMagic = pawn.TryGetComp<CompAbilityUserMagic>();
                    if (compMagic != null && compMagic.SoL != null && compMagic.SoL.solAction == SoLAction.Hovering)
                    {
                        FlyingObject_SpiritOfLight sol = compMagic.SoL;
                        if (sol.LightEnergy > (dinfo.Amount / 10f) && Rand.Chance(sol.LightEnergy / 100f))
                        {
                            Thing instigator = dinfo.Instigator as Thing;
                            if (instigator != null)
                            {
                                if ((dinfo.Weapon != null && !dinfo.Def.isExplosive) || dinfo.WeaponBodyPartGroup != null)
                                {
                                    Vector3 drawPos = pawn.DrawPos;
                                    float drawAngle = (instigator.DrawPos - drawPos).AngleFlat();
                                    drawPos.x += Mathf.Clamp(((instigator.DrawPos.x - drawPos.x) / 20f) + Rand.Range(-.1f, .1f), -.75f, .75f);
                                    drawPos.z += Mathf.Clamp(((instigator.DrawPos.z - drawPos.z) / 10f) + Rand.Range(-.1f, .1f), -1f, 1f);
                                    FleckMaker.Static(drawPos, pawn.Map, TorannMagicDefOf.SparkFlash, 1f);
                                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_LightShield_Glow, drawPos, pawn.Map, .65f, .27f, 0f, .13f, 0, 0, 0, drawAngle - 180);
                                    SoundInfo info = SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map, false), MaintenanceType.None);
                                    info.volumeFactor = .5f;
                                    info.pitchFactor = 3f;
                                    TM_Action.DoReversalRandomTarget(dinfo, pawn, 0, 40f);
                                    TorannMagicDefOf.TM_MetalImpact.PlayOneShot(info);
                                    sol.ActualLightCost(dinfo.Amount / 10f);
                                    dinfo.SetAmount(0);
                                    absorbed = true;
                                    return false;
                                }
                            }
                        }
                    }
                    //concept damage mitigation from psychic sensitivity - completely mitigates some damage types
                    //if (pawn.RaceProps.Humanlike && pawn.GetStatValue(StatDefOf.PsychicSensitivity, true) < 1)
                    //{
                    //    if ((dinfo.Def.defName.Contains("TM_") || dinfo.Def.defName == "FrostRay" || dinfo.Def.defName == "Snowball" || dinfo.Def.defName == "Iceshard" || dinfo.Def.defName == "Firebolt") && Rand.Chance(1 - pawn.GetStatValue(StatDefOf.PsychicSensitivity, true)))
                    //    {
                    //        absorbed = true;
                    //        return false;
                    //    }
                    //}
                }
                absorbed = false;
                return true;
            }
        }

        [HarmonyPatch(typeof(Pawn_HealthTracker), "PostApplyDamage", null)]
        public static class PostApplyDamage_Patch
        {
            public static FieldInfo pawn = typeof(Pawn_HealthTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

            public static void Postfix(Pawn_HealthTracker __instance, DamageInfo dinfo, Pawn ___pawn)
            {
                //Traverse traverse = Traverse.Create(__instance);
                Pawn pawn = ___pawn;
                if (dinfo.Def != null)
                {
                    if (dinfo.Instigator != null && pawn != null && dinfo.Instigator != pawn && !pawn.Destroyed && !pawn.Dead && pawn.Map != null)
                    {
                        Pawn instigator = dinfo.Instigator as Pawn;
                        if (instigator != null && dinfo.Def != TMDamageDefOf.DamageDefOf.TM_ArcaneSpectre && dinfo.Def.harmsHealth && dinfo.Def.canInterruptJobs)
                        {
                            if (instigator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffEnchantment_arcaneSpectre) && Rand.Chance(.5f))
                            {
                                DamageInfo dinfo2;
                                float amt;
                                amt = dinfo.Amount * .2f;
                                dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_ArcaneSpectre, (int)amt, 0, (float)-1, instigator, dinfo.HitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
                                dinfo2.SetAllowDamagePropagation(false);
                                pawn.TakeDamage(dinfo2);
                                Vector3 displayVec = pawn.Position.ToVector3Shifted();
                                displayVec.x += Rand.Range(-.2f, .2f);
                                displayVec.z += Rand.Range(-.2f, .2f);
                                TM_MoteMaker.ThrowArcaneDaggers(displayVec, pawn.Map, .7f);
                            }
                        }

                        if(instigator != null && pawn.health != null && pawn.health.hediffSet != null && pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_Mecha-Golem_LightningCoreHD"), false))
                        {
                            Hediff hd = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_Mecha-Golem_LightningCoreHD"));
                            DamageInfo dinfo2;
                            float amt = hd.Severity * 2f;
                            dinfo2 = new DamageInfo(DamageDefOf.Stun, (int)amt, 0, (float)-1, pawn, dinfo.HitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
                            dinfo2.SetAllowDamagePropagation(false);
                            instigator.TakeDamage(dinfo2);
                            FleckMaker.ThrowLightningGlow(instigator.DrawPos, pawn.Map, Rand.Range(.4f, .6f));
                        }

                        if (TM_Calc.IsUndead(pawn))
                        {
                            //Log.Message("undead taking damage");
                            if (dinfo.Def != null && dinfo.Def.armorCategory != null && dinfo.Def.armorCategory.defName == "Light" && Rand.Chance(.35f))
                            {
                                //Log.Message("taking light damage");
                                dinfo.SetAmount(dinfo.Amount * .7f);
                                pawn.TakeDamage(dinfo);
                            }
                        }

                        if (instigator != null && dinfo.Def != TMDamageDefOf.DamageDefOf.TM_Cleave && dinfo.Def != TMDamageDefOf.DamageDefOf.TM_DragonStrike && dinfo.Def != TMDamageDefOf.DamageDefOf.TM_ChiBurn && dinfo.Def != DamageDefOf.Stun && dinfo.Def != TMDamageDefOf.DamageDefOf.TM_CQC)
                        {
                            if (instigator.RaceProps.Humanlike && instigator.story != null)
                            {
                                //Log.Message("checking class bonus damage");
                                CompAbilityUserMight comp = instigator.GetComp<CompAbilityUserMight>();
                                if ((instigator.story.traits.HasTrait(TorannMagicDefOf.Gladiator) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_Cleave, null, comp)) && instigator.equipment.Primary != null && instigator.equipment.Primary.def.IsMeleeWeapon)
                                {
                                    float cleaveChance = Mathf.Min(instigator.equipment.Primary.def.BaseMass * .15f, .75f);
                                    if (comp.useCleaveToggle && Rand.Chance(cleaveChance) && comp.Stamina.CurLevel >= comp.ActualStaminaCost(TorannMagicDefOf.TM_Cleave) && (pawn.Position - instigator.Position).LengthHorizontal <= 1.6f)
                                    {
                                        MightPowerSkill pwr = comp.MightData.MightPowerSkill_Cleave.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Cleave_pwr");
                                        MightPowerSkill str = comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_strength_pwr");
                                        MightPowerSkill ver = comp.MightData.MightPowerSkill_Cleave.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Cleave_ver");
                                        int dmgNum = Mathf.RoundToInt(dinfo.Amount * (TorannMagicDefOf.TM_Cleave.weaponDamageFactor + (.05f * pwr.level)));
                                        DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Cleave, dmgNum, 0, (float)-1, instigator, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                                        Verb_Cleave.ApplyCleaveDamage(dinfo2, instigator, pawn, pawn.Map, ver.level);
                                        comp.Stamina.CurLevel -= comp.ActualStaminaCost(TorannMagicDefOf.TM_Cleave);
                                        comp.MightUserXP += Rand.Range(10, 15);
                                    }
                                }
                                if (instigator.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_CQC, null, comp))
                                {
                                    if (comp != null && comp.useCQCToggle && comp.Stamina.CurLevel >= comp.ActualStaminaCost(TorannMagicDefOf.TM_CQC) && (pawn.Position - instigator.Position).LengthHorizontal <= 1.6f)
                                    {
                                        MightPowerSkill pwr = comp.MightData.MightPowerSkill_CQC.FirstOrDefault((MightPowerSkill x) => x.label == "TM_CQC_pwr");
                                        MightPowerSkill str = comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_strength_pwr");
                                        int verVal = comp.MightData.MightPowerSkill_CQC.FirstOrDefault((MightPowerSkill x) => x.label == "TM_CQC_ver").level;
                                        float cqcChance = .2f;
                                        if (verVal == 1)
                                        {
                                            cqcChance = .25f;
                                        }
                                        else if (verVal == 2)
                                        {
                                            cqcChance = .28f;
                                        }
                                        else if (verVal == 3)
                                        {
                                            cqcChance = .3f;
                                        }
                                        if (Rand.Chance(cqcChance))
                                        {
                                            int dmgNum = Mathf.RoundToInt(comp.weaponDamage * TorannMagicDefOf.TM_CQC.weaponDamageFactor * Rand.Range(.7f, 1.2f)) + (2 * pwr.level);
                                            Vector3 strikeEndVec = pawn.DrawPos;
                                            strikeEndVec.x += Rand.Range(-.2f, .2f);
                                            strikeEndVec.z += Rand.Range(-.2f, .2f);
                                            Vector3 strikeStartVec = instigator.DrawPos;
                                            strikeStartVec.z += Rand.Range(-.2f, .2f);
                                            strikeStartVec.x += Rand.Range(-.2f, .2f);
                                            Vector3 angle = TM_Calc.GetVector(strikeStartVec, strikeEndVec);
                                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_CQC, strikeStartVec, instigator.Map, .35f, .08f, .03f, .05f, 0, 8f, (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(), (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat());
                                            TM_Action.DamageEntities(pawn, dinfo.HitPart, dmgNum, TMDamageDefOf.DamageDefOf.TM_CQC, instigator);
                                            comp.Stamina.CurLevel -= comp.ActualStaminaCost(TorannMagicDefOf.TM_CQC);
                                            comp.MightUserXP += Rand.Range(10, 15);
                                        }
                                    }
                                }
                            }

                            if (instigator.RaceProps.Humanlike && instigator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MindOverBodyHD) && instigator.equipment.Primary == null)
                            {
                                CompAbilityUserMight comp = instigator.GetComp<CompAbilityUserMight>();
                                MightPowerSkill ver = comp.MightData.MightPowerSkill_DragonStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_DragonStrike_ver");
                                if (Rand.Chance(.3f + (.05f * ver.level)) && comp != null)
                                {
                                    MightPowerSkill pwr = comp.MightData.MightPowerSkill_DragonStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_DragonStrike_pwr");
                                    MightPowerSkill str = comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_strength_pwr");
                                    int dmgNum = Mathf.RoundToInt(Rand.Range(6f, 10f) * (1 + (.1f * pwr.level) + (.05f * str.level)));
                                    DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_DragonStrike, dmgNum, 0, (float)-1, instigator, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                                    TM_Action.DoAction_ApplySplashDamage(dinfo2, instigator, pawn, instigator.Map, 0);
                                }
                            }

                            if (instigator.RaceProps.Humanlike && instigator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_NightshadeHD) && dinfo.Amount > 0 && instigator.Faction != pawn.Faction)
                            {
                                Hediff hd = instigator.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_NightshadeHD);
                                HediffComp_Nightshade hdComp = hd.TryGetComp<HediffComp_Nightshade>();
                                float applySev = hdComp.GetApplicationSeverity;
                                if (hdComp.GetDoseCount > 0 && instigator.equipment != null && instigator.equipment.Primary != null)
                                {
                                    if (!instigator.equipment.Primary.def.IsMeleeWeapon)
                                    {
                                        applySev *= .40f;
                                    }

                                    Hediff toxinHD = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_NightshadeToxinHD);
                                    if (toxinHD != null)
                                    {
                                        toxinHD.Severity += applySev;
                                    }
                                    else
                                    {
                                        HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_NightshadeToxinHD, applySev);
                                    }
                                    hd.Severity -= applySev;
                                }
                            }

                        }

                        if (instigator != null && instigator.health.hediffSet.HasHediff(HediffDef.Named("TM_PsionicHD"), false))
                        {
                            if (instigator.equipment.Primary == null && dinfo.Def != TMDamageDefOf.DamageDefOf.TM_PsionicInjury && dinfo.Def != DamageDefOf.Stun)
                            {
                                CompAbilityUserMight comp = instigator.GetComp<CompAbilityUserMight>();
                                MightPowerSkill pwr = comp.MightData.MightPowerSkill_PsionicAugmentation.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicAugmentation_pwr");
                                float dmgNum = dinfo.Amount;
                                float pawnDPS = instigator.GetStatValue(StatDefOf.MeleeDPS, false);
                                float psiEnergy = instigator.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_PsionicHD"), false).Severity;
                                if (psiEnergy > 20f && Rand.Chance(.3f + (.05f * pwr.level)) && !pawn.Downed)
                                {
                                    DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_PsionicInjury, (dmgNum + pawnDPS) + 2 * pwr.level, dinfo.ArmorPenetrationInt, dinfo.Angle, instigator, dinfo.HitPart, dinfo.Weapon, dinfo.Category, dinfo.intendedTargetInt);
                                    TM_MoteMaker.MakePowerBeamMotePsionic(pawn.DrawPos.ToIntVec3(), pawn.Map, 2.5f, 2f, .7f, .1f, .6f);
                                    pawn.TakeDamage(dinfo2);
                                    HealthUtility.AdjustSeverity(instigator, HediffDef.Named("TM_PsionicHD"), -2f);
                                    comp.Stamina.CurLevel -= .02f;
                                    comp.MightUserXP += Rand.Range(2, 4);
                                    if (psiEnergy > 60f && !pawn.Dead && Rand.Chance(.2f + (.03f * pwr.level)))
                                    {
                                        for (int i = 0; i < 6; i++)
                                        {
                                            float moteDirection = Rand.Range(0, 360);
                                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi, instigator.DrawPos, instigator.Map, Rand.Range(.3f, .5f), 0.25f, .05f, .1f, 0, Rand.Range(6, 8), moteDirection, moteDirection);
                                        }
                                        Vector3 heading = (pawn.Position - instigator.Position).ToVector3();
                                        float distance = heading.magnitude;
                                        Vector3 direction = heading / distance;
                                        IntVec3 destinationCell = pawn.Position + (direction * (Rand.Range(5, 8) + (2 * pwr.level))).ToIntVec3();
                                        FlyingObject_Spinning flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Spinning"), pawn.Position, pawn.Map);
                                        flyingObject.speed = 35;
                                        flyingObject.Launch(instigator, destinationCell, pawn);
                                        HealthUtility.AdjustSeverity(instigator, HediffDef.Named("TM_PsionicHD"), -2f);
                                        comp.Stamina.CurLevel -= .02f;
                                        comp.MightUserXP += Rand.Range(3, 5);
                                    }
                                    else if (psiEnergy > 40f && !pawn.Dead && Rand.Chance(.4f + (.05f * pwr.level)))
                                    {
                                        DamageInfo dinfo3 = new DamageInfo(DamageDefOf.Stun, dmgNum / 2, dinfo.ArmorPenetrationInt, dinfo.Angle, instigator, dinfo.HitPart, dinfo.Weapon, dinfo.Category, dinfo.intendedTargetInt);
                                        pawn.TakeDamage(dinfo3);
                                        HealthUtility.AdjustSeverity(instigator, HediffDef.Named("TM_PsionicHD"), -2f);
                                        comp.Stamina.CurLevel -= .01f;
                                        comp.MightUserXP += Rand.Range(2, 3);
                                    }
                                }
                            }
                        }

                        if (instigator != null && instigator.equipment != null && instigator.equipment.Primary != null && instigator.equipment.Primary.def.IsMeleeWeapon)
                        {
                            //Log.Message("checking instigator melee bonus ");                            
                            if (Rand.Chance(.2f) && instigator.story != null && instigator.story.traits != null && instigator.story.traits.HasTrait(TorannMagicDefOf.Paladin))
                            {
                                CompAbilityUserMagic comp = instigator.GetComp<CompAbilityUserMagic>();
                                if (comp != null)
                                {
                                    float amount = Rand.Range(2f, 4f) + Rand.Range(0f, .1f * comp.MagicUserLevel);
                                    DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Holy, amount, 0, dinfo.Angle, instigator, dinfo.HitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
                                    TM_Action.DamageUndead(pawn, amount, instigator);
                                }
                            }
                            if (instigator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffFightersFocus) && Rand.Chance(.2f))
                            {
                                CompAbilityUserMight comp = instigator.GetComp<CompAbilityUserMight>();
                                if (comp != null && comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_pwr").level >= 7)
                                {
                                    if (pawn.equipment != null && pawn.equipment.Primary != null && (pawn.equipment.Primary.def.IsRangedWeapon || pawn.equipment.Primary.def.IsMeleeWeapon))
                                    {
                                        ThingWithComps outThing = new ThingWithComps();
                                        pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out outThing, pawn.Position, false);
                                        MoteMaker.ThrowText(pawn.DrawPos, pawn.MapHeld, "disarmed!", -1);
                                    }
                                }
                            }
                            if (pawn.health != null && pawn.health.hediffSet != null && pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffThickSkin))
                            {
                                Hediff hd = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HediffThickSkin);
                                if (hd.Severity >= 3)
                                {
                                    bool flagDmg = false;
                                    if (dinfo.WeaponBodyPartGroup != null)
                                    {
                                        List<BodyPartRecord> bpr = new List<BodyPartRecord>();
                                        bpr.Clear();
                                        bpr.Add(instigator.RaceProps.body.AllParts.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbSegment)));
                                        bpr.Add(instigator.RaceProps.body.AllParts.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbCore)));
                                        bpr.Add(instigator.RaceProps.body.AllParts.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbDigit)));
                                        if (bpr != null && bpr.Count > 0)
                                        {
                                            TM_Action.DamageEntities(instigator, bpr.RandomElement(), Rand.Range(1f, 4f), DamageDefOf.Scratch, pawn);
                                            flagDmg = true;
                                        }
                                    }
                                    if (!flagDmg)
                                    {
                                        TM_Action.DamageEntities(instigator, null, Rand.Range(1f, 4f), DamageDefOf.Scratch, pawn);
                                    }
                                }
                            }
                        }

                        if (instigator != null)
                        {
                            //Log.Message("checking enchantment damage");
                            if (instigator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_WeaponEnchantment_FireHD) && dinfo.Def != TMDamageDefOf.DamageDefOf.TM_Enchanted_Fire && Rand.Chance(.5f))
                            {
                                float sev = instigator.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_WeaponEnchantment_FireHD).Severity;
                                DamageInfo dinfo3 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Enchanted_Fire, Rand.Range(1f + sev, 5f + sev), 1, -1, instigator, dinfo.HitPart, dinfo.Weapon, dinfo.Category, dinfo.intendedTargetInt);
                                pawn.TakeDamage(dinfo3);
                            }
                            if (instigator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_WeaponEnchantment_IceHD) && dinfo.Def != TMDamageDefOf.DamageDefOf.TM_Enchanted_Ice && Rand.Chance(.4f))
                            {
                                float sev = instigator.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_WeaponEnchantment_IceHD).Severity;
                                DamageInfo dinfo3 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Enchanted_Ice, Mathf.RoundToInt(Rand.Range(3f + sev, 5f + sev) / 2), 1, -1, instigator, dinfo.HitPart, dinfo.Weapon, dinfo.Category, dinfo.intendedTargetInt);
                                pawn.TakeDamage(dinfo3);
                            }
                            if (instigator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_WeaponEnchantment_LitHD) && dinfo.Def != DamageDefOf.Stun && Rand.Chance(.3f))
                            {
                                float sev = instigator.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_WeaponEnchantment_LitHD).Severity;
                                DamageInfo dinfo3 = new DamageInfo(DamageDefOf.Stun, Rand.Range(1f + (.5f * sev), 3f + (.5f * sev)), 1, -1, instigator, dinfo.HitPart, dinfo.Weapon, dinfo.Category, dinfo.intendedTargetInt);
                                pawn.TakeDamage(dinfo3);
                            }
                            if (instigator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_WeaponEnchantment_DarkHD))
                            {
                                float sev = instigator.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_WeaponEnchantment_DarkHD).Severity;
                                if (Rand.Chance(.3f + (.1f * sev)))
                                {
                                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_Blind, Rand.Range(.05f, .2f));
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Verb), "TryCastNextBurstShot", null)]
        public static class TryCastNextBurstShot_Monk_Patch
        {
            public static void Postfix(Verb __instance, LocalTargetInfo ___currentTarget, int ___burstShotsLeft)
            {
                if (__instance.CasterIsPawn)
                {
                    CompAbilityUserMight comp = __instance.CasterPawn.TryGetComp<CompAbilityUserMight>();
                    if (comp != null && comp.MightData != null && comp.Stamina != null && __instance.CasterPawn.health != null && __instance.CasterPawn.health.hediffSet != null)
                    {
                        if (__instance.CasterPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MindOverBodyHD, false) && __instance.CasterPawn.equipment.Primary == null && ___burstShotsLeft <= 0)
                        {
                            //LocalTargetInfo currentTarget = Traverse.Create(root: __instance).Field(name: "currentTarget").GetValue<LocalTargetInfo>();
                            //int burstShotsLeft = Traverse.Create(root: __instance).Field(name: "burstShotsLeft").GetValue<int>();
                            MightPowerSkill pwr = comp.MightData.MightPowerSkill_TigerStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_TigerStrike_pwr");
                            MightPowerSkill eff = comp.MightData.MightPowerSkill_TigerStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_TigerStrike_eff");
                            MightPowerSkill globalSkill = comp.MightData.MightPowerSkill_global_seff.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_seff_pwr");
                            float actualStaminaCost = .06f * (1 - (.1f * eff.level) * (1 - (.03f * globalSkill.level)));
                            if (comp.Stamina != null && comp.Stamina.CurLevel >= actualStaminaCost && Rand.Chance(.3f + (.05f * pwr.level)))
                            {
                                Vector3 strikeEndVec = ___currentTarget.CenterVector3;
                                strikeEndVec.x += Rand.Range(-.2f, .2f);
                                strikeEndVec.z += Rand.Range(-.2f, .2f);
                                Vector3 strikeStartVec = __instance.CasterPawn.DrawPos;
                                strikeStartVec.z += Rand.Range(-.2f, .2f);
                                strikeStartVec.x += Rand.Range(-.2f, .2f);
                                Vector3 angle = TM_Calc.GetVector(strikeStartVec, strikeEndVec);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Strike, strikeStartVec, __instance.CasterPawn.Map, .2f, .08f, .03f, .05f, 0, 8f, (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(), (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat());
                                __instance.CasterPawn.stances.SetStance(new Stance_Cooldown(5, ___currentTarget, __instance));
                                comp.Stamina.CurLevel -= actualStaminaCost;
                                comp.MightUserXP += (int)(.06f * 180);
                            }
                        }

                        if ((__instance.CasterPawn.story != null && __instance.CasterPawn.story.traits != null && __instance.CasterPawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier)) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_PistolSpec, null, comp))
                        {
                            //LocalTargetInfo currentTarget = Traverse.Create(root: __instance).Field(name: "currentTarget").GetValue<LocalTargetInfo>();
                            //int burstShotsLeft = Traverse.Create(root: __instance).Field(name: "burstShotsLeft").GetValue<int>();
                            if (__instance.CasterPawn.equipment != null && __instance.CasterPawn.equipment.Primary != null && ___burstShotsLeft <= 0)
                            {
                                if (comp.specWpnRegNum != -1 && comp.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_PistolSpec).learned)
                                {
                                    int doubleTapPwr = comp.MightData.MightPowerSkill_PistolSpec.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PistolSpec_eff").level;
                                    MightPowerSkill globalSkill = comp.MightData.MightPowerSkill_global_seff.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_seff_pwr");
                                    float actualStaminaCost = .03f * (1 - (.1f * doubleTapPwr) * (1 - (.03f * globalSkill.level)));
                                    if (comp.Stamina.CurLevel >= actualStaminaCost && Rand.Chance(.25f + (.05f * doubleTapPwr)))
                                    {
                                        __instance.CasterPawn.stances.SetStance(new Stance_Cooldown(5, ___currentTarget, __instance));
                                        comp.Stamina.CurLevel -= actualStaminaCost;
                                        comp.MightUserXP += (int)(.03f * 180);
                                    }
                                }
                            }
                        }
                    }
                    if (__instance.CasterPawn.stances != null && (__instance.CasterPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HasteHD) || __instance.CasterPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SeverityHasteHD)))
                    {
                        //LocalTargetInfo currentTarget = Traverse.Create(root: __instance).Field(name: "currentTarget").GetValue<LocalTargetInfo>();
                        int ticksRemaining = 30;
                        if (__instance.CasterPawn.stances.curStance is Stance_Busy)
                        {
                            Stance_Busy st = (Stance_Busy)__instance.CasterPawn.stances.curStance;
                            ticksRemaining = Mathf.RoundToInt(st.ticksLeft / 2f);
                        }
                        __instance.CasterPawn.stances.SetStance(new Stance_Cooldown(ticksRemaining, ___currentTarget, __instance));
                    }

                    if (__instance.CasterPawn.stances != null && __instance.CasterPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnrageHD))
                    {
                        int ticksRemaining = 30;
                        if (__instance.CasterPawn.stances.curStance is Stance_Busy)
                        {
                            Hediff hd = __instance.CasterPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnrageHD);
                            Stance_Busy st = (Stance_Busy)__instance.CasterPawn.stances.curStance;
                            ticksRemaining = Mathf.RoundToInt(st.ticksLeft * (1f - hd.Severity));
                        }
                        __instance.CasterPawn.stances.SetStance(new Stance_Cooldown(ticksRemaining, ___currentTarget, __instance));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Recipe_Surgery), "CheckSurgeryFail", null)]
        public static class CheckSurgeryFail_Base_Patch
        {
            public static bool Prefix(Recipe_Surgery __instance, Pawn surgeon, Pawn patient, List<Thing> ingredients, BodyPartRecord part, ref bool __result)
            {
                if (patient.health.hediffSet.HasHediff(HediffDef.Named("TM_StoneskinHD")) || patient.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ProtectionBrandHD))
                {
                    Hediff hediff = patient.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_StoneskinHD"), false);
                    if (hediff != null)
                    {
                        patient.health.RemoveHediff(hediff);
                    }
                    hediff = patient.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ProtectionBrandHD, false);
                    if(hediff != null)
                    {
                        patient.health.RemoveHediff(hediff);
                    }
                }

                if (patient.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")) || patient.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")))
                {
                    Messages.Message("Something went horribly wrong while trying to perform a surgery on " + patient.LabelShort + ", perhaps it's best to leave the bodies of the living dead alone.", MessageTypeDefOf.NegativeHealthEvent);
                    GenExplosion.DoExplosion(surgeon.Position, surgeon.Map, 2f, TMDamageDefOf.DamageDefOf.TM_CorpseExplosion, patient, Rand.Range(6, 12), 10, TMDamageDefOf.DamageDefOf.TM_CorpseExplosion.soundExplosion, null, null, null, null, 0, 0, false, null, 0, 0, 0, false);
                    __result = true;
                    return false;
                }

                return true;

            }
        }

        [HarmonyPatch(typeof(Verb), "TryFindShootLineFromTo", null)]
        public static class TryFindShootLineFromTo_Base_Patch
        {
            public static bool Prefix(Verb __instance, IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine, ref bool __result)
            {
                if (__instance.verbProps.IsMeleeAttack)
                {
                    resultingLine = new ShootLine(root, targ.Cell);
                    __result = ReachabilityImmediate.CanReachImmediate(root, targ, __instance.caster.Map, PathEndMode.Touch, null);
                    return false;
                }
                if (__instance.verbProps.range == 0 && __instance.CasterPawn != null && !__instance.CasterPawn.IsColonist) // allows ai to autocast on themselves
                {
                    resultingLine = default(ShootLine);
                    __result = true;
                    return false;
                }
                if (__instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Blink" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_BLOS" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_LightSkip" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Summon" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_SootheAnimal" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Effect_EyeOfTheStorm" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_PhaseStrike" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Effect_Flight" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Regenerate" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_SpellMending" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_CauterizeWound" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Transpose" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Disguise" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_SoulBond" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_SummonDemon" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_EarthSprites" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Overdrive" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_BlankMind" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_MechaniteReprogramming" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_ShadowWalk" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_SoL_CreateLight" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Enrage" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Hex" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_Discord" ||
                    __instance.verbProps.verbClass.ToString() == "TorannMagic.Verb_AdvancedHeal")
                {
                    //Ignores line of sight
                    //                    
                    if (__instance.CasterPawn.RaceProps.Humanlike)
                    {
                        Pawn pawn = __instance.CasterPawn;
                        CompAbilityUserMight comp = pawn.GetComp<CompAbilityUserMight>();
                        if (comp != null && (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_Transpose, null, comp)))
                        {

                            MightPowerSkill ver = comp.MightData.MightPowerSkill_Transpose.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Transpose_ver");
                            if (ver.level < 3)
                            {
                                __result = true;
                                resultingLine = default(ShootLine);
                                return true;
                            }
                        }
                    }

                    resultingLine = new ShootLine(root, targ.Cell);
                    __result = true;
                    return false;
                }
                resultingLine = default(ShootLine);
                __result = true;
                return true;
            }
        }

        [HarmonyPatch(typeof(CastPositionFinder), "TryFindCastPosition", null)]
        public static class TryFindCastPosition_Base_Patch
        {

            private static bool Prefix(CastPositionRequest newReq, out IntVec3 dest) //, ref IntVec3 __result)
            {
                CastPositionRequest req = newReq;
                IntVec3 casterLoc = req.caster.Position;
                IntVec3 targetLoc = req.target.Position;
                Verb verb = req.verb;
                dest = IntVec3.Invalid;
                bool isTMAbility = verb.verbProps.verbClass.ToString().Contains("TorannMagic") || verb.verbProps.verbClass.ToString().Contains("AbilityUser");


                if (verb.CanHitTargetFrom(casterLoc, req.target) && (req.caster.Position - req.target.Position).LengthHorizontal < verb.verbProps.range && isTMAbility)
                {
                    //If in range and in los, cast immediately
                    dest = casterLoc;
                    //__result = dest;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Pawn_SkillTracker), "Learn", null)]
        public static class Pawn_SkillTracker_Base_Patch
        {
            public static FieldInfo pawn = typeof(Pawn_SkillTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            private static bool Prefix(Pawn_SkillTracker __instance)
            {
                Traverse traverse = Traverse.Create(__instance);
                Pawn pawn = (Pawn)Pawn_SkillTracker_Base_Patch.pawn.GetValue(__instance);
                if (pawn != null)
                {
                    if (pawn.story.traits.HasTrait(TorannMagicDefOf.Undead))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(SkillRecord), "Learn", null)]
        public static class SkillRecord_Patch
        {
            public static FieldInfo pawn = typeof(SkillRecord).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            private static bool Prefix(SkillRecord __instance)
            {
                Traverse traverse = Traverse.Create(__instance);
                Pawn pawn = (Pawn)SkillRecord_Patch.pawn.GetValue(__instance);

                if (pawn != null)
                {
                    if (pawn.story.traits.HasTrait(TorannMagicDefOf.Undead))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(StockGenerator_Animals), "HandlesThingDef", null)]
        public static class StockGenerator_Animals_Patch
        {
            private static bool Prefix(ThingDef thingDef, ref bool __result)
            {
                if (thingDef != null && thingDef.thingClass != null && thingDef.thingClass.ToString() == "TorannMagic.TMPawnSummoned")
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(FertilityGrid), "CalculateFertilityAt", null)]
        public static class FertilityGrid_Patch
        {
            public static FieldInfo map = typeof(FertilityGrid).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

            private static void Postfix(FertilityGrid __instance, IntVec3 loc, ref float __result)
            {
                if (ModOptions.Constants.GetGrowthCells().Count > 0)
                {
                    List<IntVec3> growthCells = ModOptions.Constants.GetGrowthCells();
                    for (int i = 0; i < growthCells.Count; i++)
                    {
                        if (loc == growthCells[i])
                        {
                            Traverse traverse = Traverse.Create(__instance);
                            Map map = (Map)FertilityGrid_Patch.map.GetValue(__instance);
                            __result *= 2f;
                            if (Rand.Chance(.6f) && (ModOptions.Constants.GetLastGrowthMoteTick() + 5) < Find.TickManager.TicksGame)
                            {
                                TM_MoteMaker.ThrowTwinkle(growthCells[i].ToVector3Shifted(), map, Rand.Range(.3f, .7f), Rand.Range(100, 300), Rand.Range(.5f, 1.5f), Rand.Range(.1f, .5f), .05f, Rand.Range(.8f, 1.8f));
                                ModOptions.Constants.SetLastGrowthMoteTick(Find.TickManager.TicksGame);
                            }

                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(AbilityWorker), "TargetAbilityFor", null)]
        public static class AbilityWorker_TargetAbilityFor_Patch
        {
            public static bool Prefix(AbilityAIDef abilityDef, Pawn pawn, ref LocalTargetInfo __result)
            {
                bool flagComp = false;
                //CompAbilityUserMagic magicComp = pawn.TryGetComp<CompAbilityUserMagic>();
                //if(magicComp != null && magicComp.customClass != null)
                //{

                //}
                CompAbilityUserMight mightComp = pawn.TryGetComp<CompAbilityUserMight>();
                if (mightComp != null && mightComp.customClass != null)
                {
                    flagComp = true;
                }
                CompAbilityUserMagic magicComp = pawn.TryGetComp<CompAbilityUserMagic>();
                if (magicComp != null && magicComp.customClass != null)
                {
                    flagComp = true;
                }
                if (flagComp || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Empath) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Apothecary) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Commander) || pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer) || pawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk) || pawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight) || pawn.story.traits.HasTrait(TorannMagicDefOf.BloodMage) || pawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter) || pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.Geomancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic) || pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock) || pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus) || pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) || pawn.story.traits.HasTrait(TorannMagicDefOf.Ranger) || pawn.story.traits.HasTrait(TorannMagicDefOf.Priest) || pawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator) || pawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper) || pawn.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.Lich) || pawn.story.traits.HasTrait(TorannMagicDefOf.InnerFire) || pawn.story.traits.HasTrait(TorannMagicDefOf.StormBorn) || pawn.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost))
                {
                    bool usedOnCaster = abilityDef.usedOnCaster;
                    if (usedOnCaster)
                    {
                        __result = pawn;
                    }
                    else
                    {
                        bool canTargetAlly = abilityDef.canTargetAlly;
                        if (canTargetAlly)
                        {
                            __result = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell, TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false), abilityDef.maxRange, (Thing thing) => AbilityUserAI.AbilityUtility.AreAllies(pawn, thing), null, 0, -1, false, RegionType.Set_Passable, false);
                        }
                        else
                        {

                            Pawn pawn2 = pawn.mindState.enemyTarget as Pawn;
                            Building bldg = pawn.mindState.enemyTarget as Building;
                            Corpse corpse = pawn.mindState.enemyTarget as Corpse;
                            bool flag = pawn.mindState.enemyTarget != null && pawn2 != null;
                            bool flag1 = pawn.mindState.enemyTarget != null && bldg != null;
                            bool flag11 = pawn.mindState.enemyTarget != null && corpse != null;
                            if (flag)
                            {
                                bool flag2 = !pawn2.Dead;
                                if (flag2)
                                {
                                    __result = pawn.mindState.enemyTarget;
                                    return false;
                                }
                            }
                            else if (flag1)
                            {
                                bool flag2 = !bldg.Destroyed;
                                if (flag2)
                                {
                                    __result = pawn.mindState.enemyTarget;
                                    return false;
                                }
                            }
                            else if (flag11)
                            {
                                bool flag2 = !corpse.IsNotFresh();
                                if (flag2)
                                {
                                    __result = pawn.mindState.enemyTarget;
                                    return false;
                                }
                            }
                            else
                            {
                                bool flag3 = pawn.mindState.enemyTarget != null && !(pawn.mindState.enemyTarget is Corpse);
                                if (flag3)
                                {
                                    __result = pawn.mindState.enemyTarget;
                                    return false;
                                }
                            }
                            __result = null;
                        }
                    }
                    return false;
                }
                else
                {
                    return true;
                }

            }
        }

        [HarmonyPatch(typeof(AbilityWorker), "CanPawnUseThisAbility", null)]
        public static class AbilityWorker_CanPawnUseThisAbility_Patch
        {
            public static bool Prefix(AbilityAIDef abilityDef, Pawn pawn, LocalTargetInfo target, ref bool __result)
            {
                bool flagComp = false;
                //CompAbilityUserMagic magicComp = pawn.TryGetComp<CompAbilityUserMagic>();
                //if(magicComp != null && magicComp.customClass != null)
                //{

                //}
                CompAbilityUserMight mightComp = pawn.TryGetComp<CompAbilityUserMight>();
                if (mightComp != null && mightComp.customClass != null)
                {
                    flagComp = true;
                }
                CompAbilityUserMagic magicComp = pawn.TryGetComp<CompAbilityUserMagic>();
                if (magicComp != null && magicComp.customClass != null)
                {
                    flagComp = true;
                }
                if (pawn.story != null && flagComp || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Empath) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Apothecary) || (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Commander) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) || pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer) || pawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk) || pawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight) || pawn.story.traits.HasTrait(TorannMagicDefOf.BloodMage) || pawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter) || pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.Geomancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic) || pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus) || pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock) || pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) || pawn.story.traits.HasTrait(TorannMagicDefOf.Ranger) || pawn.story.traits.HasTrait(TorannMagicDefOf.Priest) || pawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator) || pawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper) || pawn.story.traits.HasTrait(TorannMagicDefOf.Druid) || pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin) || pawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist) || pawn.story.traits.HasTrait(TorannMagicDefOf.Summoner) || pawn.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.Lich) || pawn.story.traits.HasTrait(TorannMagicDefOf.InnerFire) || pawn.story.traits.HasTrait(TorannMagicDefOf.StormBorn) || pawn.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost)))
                {
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    if (!settingsRef.AICasting)
                    {
                        __result = false;
                        return false;
                    }
                    if (pawn.IsPrisoner || pawn.Downed)
                    {
                        __result = false;
                        return false;
                    }
                    bool hasThing = target != null && target.HasThing;
                    if (hasThing)
                    {
                        if (abilityDef.needSeeingTarget && !TM_Calc.HasLoSFromTo(pawn.Position, target, pawn, abilityDef.minRange, abilityDef.maxRange))
                        {
                            __result = false;
                            return false;
                        }
                        Pawn pawn2 = target.Thing as Pawn;
                        if (pawn2 != null)
                        {
                            if (abilityDef.ability == TorannMagicDefOf.TM_Possess && pawn2.RaceProps.Animal)
                            {
                                __result = false;
                                return false;
                            }
                            bool flag = !abilityDef.canTargetAlly;
                            if (flag)
                            {
                                __result = !pawn2.Downed;
                                return false;
                            }
                        }
                        Building bldg2 = target.Thing as Building;
                        if (bldg2 != null)
                        {
                            if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Empath) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Apothecary) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Shaman) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Commander) || pawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk) || pawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight) || pawn.story.traits.HasTrait(TorannMagicDefOf.BloodMage) || pawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter) || pawn.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.Ranger) || pawn.story.traits.HasTrait(TorannMagicDefOf.Priest) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard) || pawn.story.traits.HasTrait(TorannMagicDefOf.Geomancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) || pawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator) || pawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer) || pawn.story.traits.HasTrait(TorannMagicDefOf.Druid) || pawn.story.traits.HasTrait(TorannMagicDefOf.Summoner))
                            {
                                __result = false;
                                return false;
                            }
                            __result = !bldg2.Destroyed;
                            return false;
                        }
                        Corpse corpse2 = target.Thing as Corpse;
                        if (corpse2 != null)
                        {
                            __result = true; //!corpse2.IsNotFresh();
                            return false;
                        }
                    }
                    __result = true;
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(WorkGiver_Researcher), "ShouldSkip", null)]
        public static class WorkGiver_Researcher_Patch
        {
            private static bool Prefix(Pawn pawn, ref bool __result)
            {
                if (pawn != null && pawn.story != null && pawn.story.traits != null && pawn.story.traits.HasTrait(TorannMagicDefOf.Undead))
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(WorkGiver_Tend), "HasJobOnThing", null)]
        public static class WorkGiver_Tend_Patch
        {
            private static void Postfix(Pawn pawn, ref bool __result)
            {
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.Undead))
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(JobGiver_SocialFighting), "TryGiveJob", null)]
        public static class JobGiver_SocialFighting_Patch
        {
            private static void Postfix(Pawn pawn, ref Job __result)
            {
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.Undead))
                {
                    __result = null;
                }
            }
        }

        [HarmonyPatch(typeof(LordToil_Siege), "CanBeBuilder", null)]
        public static class CanBeBuilder_Patch
        {
            private static bool Prefix(Pawn p, ref bool __result)
            {
                if (p?.def?.thingClass?.ToString() == "TorannMagic.TMPawnSummoned")
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(LordToil_Siege), "Notify_PawnLost", null)]
        public static class Notify_PawnLost_Patch
        {
            private static bool Prefix(Pawn victim)
            {
                if (victim?.def?.thingClass?.ToString() == "TorannMagic.TMPawnSummoned")
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(AbilityAIDef), "CanPawnUseThisAbility", null)]
        public static class CanPawnUseThisAbility_Patch
        {
            private static bool Prefix(AbilityAIDef __instance, Pawn caster, LocalTargetInfo target, ref bool __result)
            {
                bool flag = __instance.appliedHediffs.Count > 0 && __instance.appliedHediffs.Any((HediffDef hediffDef) => caster.health.hediffSet.HasHediff(hediffDef, false));
                //bool result;
                if (flag)
                {
                    __result = false;
                }
                else
                {
                    bool flag2 = !__instance.Worker.CanPawnUseThisAbility(__instance, caster, target);
                    if (flag2)
                    {
                        __result = false;
                    }
                    else
                    {
                        bool flag3 = !__instance.needEnemyTarget;
                        if (flag3)
                        {
                            __result = true;
                        }
                        else
                        {
                            bool flag4 = !__instance.usedOnCaster && target.IsValid;
                            if (flag4)
                            {
                                float num = Math.Abs(caster.Position.DistanceTo(target.Cell));
                                bool flag5 = num < __instance.minRange || num > __instance.maxRange;
                                if (flag5)
                                {
                                    __result = false;
                                    return false;
                                }
                                bool flag6 = __instance.needSeeingTarget && !AbilityUserAI.AbilityUtility.LineOfSightLocalTarget(caster, target, true, null);
                                if (flag6)
                                {
                                    __result = false;
                                    return false;
                                }
                            }
                            //Log.Message("caster " + caster.LabelShort + " attempting to case " + __instance.ability.defName + " on target " + target.Thing.LabelShort);
                            if (__instance.ability.defName == "TM_ArrowStorm" && !caster.equipment.Primary.def.weaponTags.Contains("Neolithic"))
                            {
                                __result = false;
                                return false;
                            }
                            if (__instance.ability.defName == "TM_DisablingShot" || __instance.ability.defName == "TM_Headshot" && caster.equipment.Primary.def.weaponTags.Contains("Neolithic"))
                            {
                                __result = false;
                                return false;
                            }

                            if (target.IsValid && !target.Thing.Destroyed && target.Thing.Map == caster.Map && target.Thing.Spawned)
                            {
                                Pawn targetPawn = target.Thing as Pawn;
                                if (targetPawn != null && targetPawn.Dead)
                                {
                                    __result = false;
                                    return false;
                                }
                                else
                                {
                                    __result = true;
                                }
                            }
                            else
                            {
                                __result = false;
                            }
                        }
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(PawnGenerator), "GenerateTraits", null)]
        public static class PawnGenerator_Patch
        {
            private static void Postfix(Pawn pawn)
            {
                List<TraitDef> allTraits = DefDatabase<TraitDef>.AllDefsListForReading;
                List<Trait> pawnTraits = pawn.story.traits.allTraits;
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                bool flag = pawnTraits != null;
                bool anyFightersEnabled = false;
                bool anyMagesEnabled = false;
                int baseCount = 6;
                int mageCount = 18;
                int fighterCount = 11;
                int supportingFighterCount = 2;
                int supportingMageCount = 5;
                float fighterFactor = 1f;
                float mageFactor = 1f;
                if (pawn.Faction != null)
                {
                    if (settingsRef.FactionFighterSettings.ContainsKey(pawn.Faction.def.defName))
                    {
                        fighterFactor = settingsRef.FactionFighterSettings[pawn.Faction.def.defName];
                    }
                    if (settingsRef.FactionMageSettings.ContainsKey(pawn.Faction.def.defName))
                    {
                        mageFactor = settingsRef.FactionMageSettings[pawn.Faction.def.defName];
                    }
                }
                List<TMDefs.TM_CustomClass> customFighters = TM_ClassUtility.CustomFighterClasses;
                List<TMDefs.TM_CustomClass> customMages = TM_ClassUtility.CustomMageClasses;

                mageCount += customMages.Count;
                fighterCount += customFighters.Count;
                if (customFighters.Count > 0 || settingsRef.Gladiator || settingsRef.Bladedancer || settingsRef.Ranger || settingsRef.Sniper || settingsRef.Faceless || settingsRef.DeathKnight || settingsRef.Psionic || settingsRef.Monk || settingsRef.Wayfarer || settingsRef.Commander || settingsRef.SuperSoldier)
                {
                    anyFightersEnabled = true;
                }
                if (customMages.Count > 0 || settingsRef.Arcanist || settingsRef.FireMage || settingsRef.IceMage || settingsRef.LitMage || settingsRef.Druid || settingsRef.Paladin || settingsRef.Summoner || settingsRef.Priest || settingsRef.Necromancer || settingsRef.Bard || settingsRef.Demonkin || settingsRef.Geomancer || settingsRef.Technomancer || settingsRef.BloodMage || settingsRef.Enchanter || settingsRef.Chronomancer || settingsRef.Wanderer || settingsRef.ChaosMage)
                {
                    anyMagesEnabled = true;
                }
                if (flag)
                {
                    float baseMageChance = mageFactor * settingsRef.baseMageChance * baseCount;
                    float baseFighterChance = fighterFactor * settingsRef.baseFighterChance * baseCount;
                    float advMageChance = mageCount * settingsRef.advMageChance * mageFactor;
                    float advFighterChance = fighterCount * settingsRef.advFighterChance * fighterFactor;

                    if (false) //ModCheck.Validate.AlienHumanoidRaces.IsInitialized())
                    {
                        if (Rand.Chance(((baseFighterChance) + (baseMageChance) + (advFighterChance) + (advMageChance)) / (allTraits.Count)))
                        {
                            if (pawnTraits.Count > 0)
                            {
                                pawnTraits.Remove(pawnTraits[pawnTraits.Count - 1]);
                            }
                            float rnd = Rand.Range(0, baseMageChance + baseFighterChance + advMageChance + advFighterChance);
                            if (rnd < (baseMageChance) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Gifted, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Gifted) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Gifted)))
                            {
                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Gifted, 0, false));
                            }
                            else if (rnd >= baseMageChance && rnd < (baseMageChance + baseFighterChance) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.PhysicalProdigy, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.PhysicalProdigy) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.PhysicalProdigy)))
                            {
                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.PhysicalProdigy, 0, false));
                            }
                            else if (rnd >= (baseMageChance + baseFighterChance) && rnd < (baseMageChance + baseFighterChance + advFighterChance))
                            {
                                if (anyFightersEnabled)
                                {
                                    int rndF = Rand.RangeInclusive(1, fighterCount);
                                    switch (rndF)
                                    {
                                        case 1:
                                            //Gladiator:;
                                            if (settingsRef.Gladiator && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Gladiator, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Gladiator) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Gladiator)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Gladiator, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Sniper;
                                            //}
                                            break;
                                        case 2:
                                            //Sniper:;
                                            if (settingsRef.Sniper && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Sniper, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Sniper) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Sniper)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Sniper, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Bladedancer;
                                            //}
                                            break;
                                        case 3:
                                            Bladedancer:;
                                            if (settingsRef.Bladedancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Bladedancer, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Bladedancer) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Bladedancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Bladedancer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Ranger;
                                            //}
                                            break;
                                        case 4:
                                            Ranger:;
                                            if (settingsRef.Ranger && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Ranger, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Ranger) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Ranger)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Ranger, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Faceless;
                                            //}
                                            break;
                                        case 5:
                                            Faceless:;
                                            if (settingsRef.Faceless && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Faceless, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Faceless) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Faceless)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Faceless, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Psionic;
                                            //}
                                            break;
                                        case 6:
                                            Psionic:;
                                            if (settingsRef.Psionic && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Psionic, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Psionic) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Psionic)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Psionic, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto DeathKnight;
                                            //}
                                            break;
                                        case 7:
                                            DeathKnight:;
                                            if (settingsRef.DeathKnight && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.DeathKnight, 0)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.DeathKnight) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.DeathKnight)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.DeathKnight, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Monk;
                                            //}
                                            break;
                                        case 8:
                                            Monk:;
                                            if (settingsRef.Monk && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Monk, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Monk) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Monk)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Monk, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Wayfarer;
                                            //}
                                            break;
                                        case 9:
                                            Wayfarer:;
                                            if (settingsRef.Wayfarer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Wayfarer, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Wayfarer) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Wayfarer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Wayfarer, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Commander;
                                            //}
                                            break;
                                        case 10:
                                            Commander:;
                                            if (settingsRef.Commander && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Commander, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Commander) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Commander)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Commander, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto SuperSoldier;
                                            //}
                                            break;
                                        case 11:
                                            SuperSoldier:;
                                            if (settingsRef.SuperSoldier && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_SuperSoldier, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_SuperSoldier) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_SuperSoldier)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_SuperSoldier, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Gladiator;
                                            //}
                                            break;
                                        case int val when rndF > 11:
                                            TMDefs.TM_CustomClass cFighter = TM_ClassUtility.GetRandomCustomFighter();
                                            if (!pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(cFighter.classTrait, cFighter.traitDegree)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, cFighter.classTrait) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(cFighter.classTrait)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(cFighter.classTrait, cFighter.traitDegree, false));
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    goto TraitEnd;
                                }
                            }
                            else
                            {
                                if (anyMagesEnabled)
                                {
                                    int rndM = Rand.RangeInclusive(1, (mageCount + 1));
                                    switch (rndM)
                                    {
                                        case 1:
                                            FireMage:;
                                            if (settingsRef.FireMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.InnerFire, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.InnerFire) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.InnerFire)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.InnerFire, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto IceMage;
                                            //}
                                            break;
                                        case 2:
                                            IceMage:;
                                            if (settingsRef.IceMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.HeartOfFrost, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.HeartOfFrost) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.HeartOfFrost)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.HeartOfFrost, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto LitMage;
                                            //}
                                            break;
                                        case 3:
                                            LitMage:;
                                            if (settingsRef.LitMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.StormBorn, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.StormBorn) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.StormBorn)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.StormBorn, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Arcanist;
                                            //}
                                            break;
                                        case 4:
                                            Arcanist:;
                                            if (settingsRef.Arcanist && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Arcanist, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Arcanist) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Arcanist)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Arcanist, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Druid;
                                            //}
                                            break;
                                        case 5:
                                            Druid:;
                                            if (settingsRef.Druid && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Druid, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Druid) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Druid)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Druid, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Paladin;
                                            //}
                                            break;
                                        case 6:
                                            Paladin:;
                                            if (settingsRef.Paladin && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Paladin, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Paladin) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Paladin)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Paladin, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Summoner;
                                            //}
                                            break;
                                        case 7:
                                            Summoner:;
                                            if (settingsRef.Summoner && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Summoner, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Summoner) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Summoner)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Summoner, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Necromancer;
                                            //}
                                            break;
                                        case 8:
                                            Necromancer:;
                                            if (settingsRef.Necromancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Necromancer, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Necromancer) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Necromancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Necromancer, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Priest;
                                            //}
                                            break;
                                        case 9:
                                            Priest:;
                                            if (settingsRef.Priest && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Priest, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Priest) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Priest)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Priest, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Demonkin;
                                            //}
                                            break;
                                        case 10:
                                            Demonkin:;
                                            if (settingsRef.Demonkin && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Warlock, 4)) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Succubus, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Succubus) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Warlock) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Succubus)))
                                            {
                                                if (pawn.gender != Gender.Female)
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 4, false));
                                                }
                                                else
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 4, false));
                                                }
                                            }
                                            //else
                                            //{
                                            //    goto Bard;
                                            //}
                                            break;
                                        case 11:
                                            if (settingsRef.Demonkin && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Warlock, 4)) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Succubus, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Succubus) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Warlock) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Warlock)))
                                            {
                                                if (pawn.gender != Gender.Male)
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 4, false));
                                                }
                                                else
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 4, false));
                                                }
                                            }
                                            //else
                                            //{
                                            //    goto Bard;
                                            //}
                                            break;
                                        case 12:
                                            Bard:;
                                            if (settingsRef.Bard && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Bard, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Bard) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Bard)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Bard, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Geomancer;
                                            //}
                                            break;
                                        case 13:
                                            Geomancer:;
                                            if (settingsRef.Geomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Geomancer, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Geomancer) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Geomancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Geomancer, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Technomancer;
                                            //}
                                            break;
                                        case 14:
                                            Technomancer:;
                                            if (settingsRef.Technomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Technomancer, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Technomancer) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Technomancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Technomancer, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto BloodMage;
                                            //}
                                            break;
                                        case 15:
                                            BloodMage:;
                                            if (settingsRef.Technomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.BloodMage, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.BloodMage) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.BloodMage)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.BloodMage, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Enchanter;
                                            //}
                                            break;
                                        case 16:
                                            Enchanter:;
                                            if (settingsRef.Technomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Enchanter, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Enchanter) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Enchanter)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Enchanter, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Chronomancer;
                                            //}
                                            break;
                                        case 17:
                                            Chronomancer:;
                                            if (settingsRef.Chronomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Chronomancer, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.Chronomancer) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Chronomancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Chronomancer, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto Wanderer;
                                            //}
                                            break;
                                        case 18:
                                            Wanderer:;
                                            if (settingsRef.Wanderer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Wanderer, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.TM_Wanderer) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Wanderer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Wanderer, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto ChaosMage;
                                            //}
                                            break;
                                        case 19:
                                            ChaosMage:;
                                            if (settingsRef.ChaosMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.ChaosMage, 4)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, TorannMagicDefOf.ChaosMage) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.ChaosMage)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.ChaosMage, 4, false));
                                            }
                                            //else
                                            //{
                                            //    goto FireMage;
                                            //}
                                            break;
                                        case int val when rndM > 19:
                                            TMDefs.TM_CustomClass cMage = customMages.RandomElement();
                                            if (!pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(cMage.classTrait, cMage.traitDegree)) && ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(pawn.def, pawn, cMage.classTrait) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(cMage.classTrait)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(cMage.classTrait, cMage.traitDegree, false));
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    goto TraitEnd;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Rand.Chance((baseMageChance + baseFighterChance + advMageChance + advFighterChance) / (allTraits.Count)))
                        {

                            if (pawnTraits.Count > 0)
                            {
                                pawnTraits.Remove(pawnTraits[pawnTraits.Count - 1]);
                            }
                            float rnd = Rand.Range(0, baseMageChance + baseFighterChance + advFighterChance + advMageChance);
                            if (rnd < (baseMageChance) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Gifted, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Gifted)))
                            {
                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Gifted, 0, false));
                            }
                            else if (rnd >= baseMageChance && rnd < (baseMageChance + baseFighterChance) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.PhysicalProdigy, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.PhysicalProdigy)))
                            {
                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.PhysicalProdigy, 0, false));
                            }
                            else if (rnd >= (baseMageChance + baseFighterChance) && rnd < (baseMageChance + baseFighterChance + advFighterChance))
                            {
                                if (anyFightersEnabled)
                                {
                                    int rndF = Rand.RangeInclusive(1, fighterCount);
                                    switch (rndF)
                                    {
                                        case 1:
                                            //Gladiator:;
                                            if (settingsRef.Gladiator && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Gladiator, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Gladiator)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Gladiator, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Sniper;
                                            //}
                                            break;
                                        case 2:
                                            //Sniper:;
                                            if (settingsRef.Sniper && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Sniper, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Sniper)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Sniper, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Bladedancer;
                                            //}
                                            break;
                                        case 3:
                                            Bladedancer:;
                                            if (settingsRef.Bladedancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Bladedancer, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Bladedancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Bladedancer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Ranger;
                                            //}
                                            break;
                                        case 4:
                                            Ranger:;
                                            if (settingsRef.Ranger && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Ranger, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Ranger)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Ranger, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Faceless;
                                            //}
                                            break;
                                        case 5:
                                            Faceless:;
                                            if (settingsRef.Faceless && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Faceless, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Faceless)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Faceless, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Psionic;
                                            //}
                                            break;
                                        case 6:
                                            Psionic:;
                                            if (settingsRef.Psionic && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Psionic, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Psionic)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Psionic, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto DeathKnight;
                                            //}
                                            break;
                                        case 7:
                                            DeathKnight:;
                                            if (settingsRef.DeathKnight && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.DeathKnight, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.DeathKnight)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.DeathKnight, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Monk;
                                            //}
                                            break;
                                        case 8:
                                            Monk:;
                                            if (settingsRef.Monk && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Monk, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Monk)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Monk, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Wayfarer;
                                            //}
                                            break;
                                        case 9:
                                            Wayfarer:;
                                            if (settingsRef.Wayfarer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Wayfarer, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Wayfarer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Wayfarer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Commander;
                                            //}
                                            break;
                                        case 10:
                                            Commander:;
                                            if (settingsRef.Commander && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Commander, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Commander)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Commander, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto SuperSoldier;
                                            //}
                                            break;
                                        case 11:
                                            SuperSoldier:;
                                            if (settingsRef.SuperSoldier && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_SuperSoldier, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_SuperSoldier)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_SuperSoldier, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Gladiator;
                                            //}
                                            break;
                                        case int val when rndF > 11:
                                            TMDefs.TM_CustomClass cFighter = customFighters.RandomElement();
                                            if (!pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(cFighter.classTrait, cFighter.traitDegree)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(cFighter.classTrait)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(cFighter.classTrait, cFighter.traitDegree, false));
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    goto TraitEnd;
                                }
                            }
                            else
                            {
                                if (anyMagesEnabled)
                                {
                                    int rndM = Rand.RangeInclusive(1, (mageCount + 1));
                                    switch (rndM)
                                    {
                                        case 1:
                                            FireMage:;
                                            if (settingsRef.FireMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.InnerFire, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.InnerFire)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.InnerFire, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto IceMage;
                                            //}
                                            break;
                                        case 2:
                                            IceMage:;
                                            if (settingsRef.IceMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.HeartOfFrost, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.HeartOfFrost)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.HeartOfFrost, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto LitMage;
                                            //}
                                            break;
                                        case 3:
                                            LitMage:;
                                            if (settingsRef.LitMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.StormBorn, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.StormBorn)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.StormBorn, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Arcanist;
                                            //}
                                            break;
                                        case 4:
                                            Arcanist:;
                                            if (settingsRef.Arcanist && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Arcanist, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Arcanist)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Arcanist, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Druid;
                                            //}
                                            break;
                                        case 5:
                                            Druid:;
                                            if (settingsRef.Druid && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Druid, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Druid)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Druid, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Paladin;
                                            //}
                                            break;
                                        case 6:
                                            Paladin:;
                                            if (settingsRef.Paladin && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Paladin, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Paladin)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Paladin, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Summoner;
                                            //}
                                            break;
                                        case 7:
                                            Summoner:;
                                            if (settingsRef.Summoner && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Summoner, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Summoner)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Summoner, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Necromancer;
                                            //}
                                            break;
                                        case 8:
                                            Necromancer:;
                                            if (settingsRef.Necromancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Necromancer, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Necromancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Necromancer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Priest;
                                            //}
                                            break;
                                        case 9:
                                            Priest:;
                                            if (settingsRef.Priest && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Priest, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Priest)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Priest, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Demonkin;
                                            //}
                                            break;
                                        case 10:
                                            Demonkin:;
                                            if (settingsRef.Demonkin && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Warlock, 0)) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Succubus, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Succubus)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Warlock)))
                                            {
                                                if (pawn.gender != Gender.Female)
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 0, false));
                                                }
                                                else
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 0, false));
                                                }
                                            }
                                            //else
                                            //{
                                            //    goto Bard;
                                            //}
                                            break;
                                        case 11:
                                            if (settingsRef.Demonkin && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Warlock, 0)) && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Succubus, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Succubus)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Warlock)))
                                            {
                                                if (pawn.gender != Gender.Male)
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Succubus, 0, false));
                                                }
                                                else
                                                {
                                                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Warlock, 0, false));
                                                }
                                            }
                                            //else
                                            //{
                                            //    goto Bard;
                                            //}
                                            break;
                                        case 12:
                                            Bard:;
                                            if (settingsRef.Bard && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Bard, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Bard)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Bard, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Geomancer;
                                            //}
                                            break;
                                        case 13:
                                            Geomancer:;
                                            if (settingsRef.Geomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Geomancer, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Geomancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Geomancer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Technomancer;
                                            //}
                                            break;
                                        case 14:
                                            Technomancer:;
                                            if (settingsRef.Technomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Technomancer, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Technomancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Technomancer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto BloodMage;
                                            //}
                                            break;
                                        case 15:
                                            BloodMage:;
                                            if (settingsRef.Technomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.BloodMage, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.BloodMage)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.BloodMage, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Enchanter;
                                            //}
                                            break;
                                        case 16:
                                            Enchanter:;
                                            if (settingsRef.Technomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Enchanter, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Enchanter)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Enchanter, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Chronomancer;
                                            //}
                                            break;
                                        case 17:
                                            Chronomancer:;
                                            if (settingsRef.Chronomancer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.Chronomancer, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.Chronomancer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.Chronomancer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto Wanderer;
                                            //}
                                            break;
                                        case 18:
                                            Wanderer:;
                                            if (settingsRef.Wanderer && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_Wanderer, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_Wanderer)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Wanderer, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto ChaosMage;
                                            //}
                                            break;
                                        case 19:
                                            ChaosMage:;
                                            if (settingsRef.ChaosMage && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.ChaosMage, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.ChaosMage)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.ChaosMage, 0, false));
                                            }
                                            //else
                                            //{
                                            //    goto FireMage;
                                            //}
                                            break;
                                        case int val when rndM > 19:
                                            TMDefs.TM_CustomClass cMage = customMages.RandomElement();
                                            if (!pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(cMage.classTrait, cMage.traitDegree)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(cMage.classTrait)))
                                            {
                                                pawn.story.traits.GainTrait(new Trait(cMage.classTrait, cMage.traitDegree, false));
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    goto TraitEnd;
                                }
                            }
                        }
                    }

                    if (Rand.Chance(settingsRef.supportTraitChance))
                    {
                        if (TM_Calc.IsMagicUser(pawn) || pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted))
                        {
                            int rndS = Rand.RangeInclusive(1, supportingMageCount);
                            switch (rndS)
                            {
                                case 1:
                                    if (settingsRef.ArcaneConduit && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_ArcaneConduitTD, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_ArcaneConduitTD)))
                                    {
                                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_ArcaneConduitTD, 0, false));
                                    }
                                    break;
                                case 2:
                                    if (settingsRef.ManaWell && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_ManaWellTD, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_ManaWellTD)))
                                    {
                                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_ManaWellTD, 0, false));
                                    }
                                    break;
                                case 3:
                                    if(settingsRef.FaeBlood && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_FaeBloodTD, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_FaeBloodTD)))
                                    {
                                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_FaeBloodTD, 0, false));
                                    }
                                    break;
                                case 4:
                                    if (settingsRef.Enlightened && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_EnlightenedTD, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_EnlightenedTD)))
                                    {
                                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_EnlightenedTD, 0, false));
                                    }
                                    break;
                                case 5:
                                    if (settingsRef.Cursed && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_CursedTD, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_CursedTD)))
                                    {
                                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_CursedTD, 0, false));
                                    }
                                    break;
                            }
                        }
                        else if (TM_Calc.IsMightUser(pawn) || pawn.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy))
                        {
                            int rndS = Rand.RangeInclusive(1, supportingFighterCount);
                            switch (rndS)
                            {
                                case 1:
                                    if (settingsRef.Boundless && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_BoundlessTD, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_BoundlessTD)))
                                    {
                                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_BoundlessTD, 0, false));
                                    }
                                    break;
                                case 2:
                                    if (settingsRef.GiantsBlood && !pawn.story.AllBackstories.Any(bs => bs.DisallowsTrait(TorannMagicDefOf.TM_GiantsBloodTD, 0)) && !pawn.story.traits.allTraits.Any(td => td.def.conflictingTraits.Contains(TorannMagicDefOf.TM_GiantsBloodTD)))
                                    {
                                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_GiantsBloodTD, 0, false));
                                    }
                                    break;
                            }
                        }
                    }
                }
                TraitEnd:;
            }
        }

        [HarmonyPatch(typeof(IncidentParmsUtility), "GetDefaultPawnGroupMakerParms", null)]
        public static class GetDefaultPawnGroupMakerParms_Patch
        {
            public static void Postfix(ref PawnGroupMakerParms __result)
            {
                if (__result.faction != null && __result.faction.def.defName == "Seers")
                {
                    __result.points *= 1.65f;
                }

            }
        }

        [HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob", null)]
        public static class JobGiver_GetFood_Patch
        {
            public static bool Prefix(Pawn pawn, ref Job __result)
            {
                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD) || pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD))
                {
                    __result = null;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(JobGiver_EatRandom), "TryGiveJob", null)]
        public static class JobGiver_EatRandom_Patch
        {
            public static bool Prefix(Pawn pawn, ref Job __result)
            {
                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD) || pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD))
                {
                    __result = null;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(JobGiver_Haul), "TryGiveJob", null)]
        public static class JobGiver_MinionHaul_Patch
        {
            public static bool Prefix(Pawn pawn, ref Job __result)
            {
                if (pawn != null && (pawn.def == TorannMagicDefOf.TM_MinionR || pawn.def == TorannMagicDefOf.TM_GreaterMinionR))
                {
                    if (pawn.jobs != null && pawn.CurJob != null && (pawn.CurJob.def == JobDefOf.HaulToCell || pawn.CurJob.def == JobDefOf.HaulToContainer || pawn.CurJob.def == JobDefOf.HaulToTransporter))
                    {
                        __result = null;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(AbilityUser.AbilityDef), "GetJob", null)]
        public static class AbilityDef_Patch
        {
            private static bool Prefix(AbilityUser.AbilityDef __instance, AbilityTargetCategory cat, LocalTargetInfo target, ref Job __result)
            {
                if (__instance.abilityClass.FullName == "TorannMagic.MagicAbility" || __instance.abilityClass.FullName == "TorannMagic.MightAbility" || __instance.defName.Contains("TM_Artifact"))
                {
                    Job result;
                    switch (cat)
                    {
                        case AbilityTargetCategory.TargetSelf:
                            result = new Job(TorannMagicDefOf.TMCastAbilitySelf, target);
                            __result = result;
                            return false;
                        case AbilityTargetCategory.TargetThing:
                            result = new Job(TorannMagicDefOf.TMCastAbilityVerb, target);
                            __result = result;
                            return false;
                        case AbilityTargetCategory.TargetAoE:
                            result = new Job(TorannMagicDefOf.TMCastAbilityVerb, target);
                            __result = result;
                            return false;
                    }
                    result = new Job(TorannMagicDefOf.TMCastAbilityVerb, target);
                    __result = result;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(JobDriver_Mine), "ResetTicksToPickHit", null)]
        public static class JobDriver_Mine_Patch
        {
            private static void Postfix(JobDriver_Mine __instance)
            {
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (Rand.Chance(settingsRef.magicyteChance))
                {
                    Thing thing = null;
                    thing = ThingMaker.MakeThing(TorannMagicDefOf.RawMagicyte);
                    thing.stackCount = Rand.Range(9, 25);
                    if (thing != null)
                    {
                        GenPlace.TryPlaceThing(thing, __instance.pawn.Position, __instance.pawn.Map, ThingPlaceMode.Near, null);
                        if(!__instance.pawn.Faction.IsPlayer)
                        {
                            thing.SetForbidden(true, false);
                        }
                    }
                }
            }
        }

        //[HarmonyPatch(typeof(Pawn), "GetGizmos", null)]
        //public class Pawn_DraftController_GetGizmos_Patch
        //{
        //    public static void Postfix(ref IEnumerable<Gizmo> __result, ref Pawn __instance)
        //    {
        //        ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
        //        if (!settingsRef.showIconsMultiSelect)
        //        {
        //            int count = Find.Selector.SelectedObjects.Count;
        //            if ( count >= 2)
        //            {
        //                bool flag = __instance != null || __instance.Faction.Equals(Faction.OfPlayer);
        //                if (flag)
        //                {
        //                    bool flag2 = __result == null || !__result.Any<Gizmo>();
        //                    if (!flag2)
        //                    {
        //                        __result = ModOptions.Constants.GetReducedDraftGizmos(__instance.LabelShort, __result);
        //                    }
        //                }
        //            }
        //            else if (Find.TickManager.TicksGame % 500 == 0)
        //            {
        //                ModOptions.Constants.GetReducedDraftGizmos("clear", null);
        //            }
        //        }

        //    }
        //}

        //[HarmonyPatch(typeof(FloatMenuMakerMap))]
        //[HarmonyPatch("CanTakeOrder")]
        //public static class FloatMenuMakerMap_CanTakeOrder_Patch
        //{
        //    [HarmonyPostfix]
        //    public static void MakePawnControllable(Pawn pawn, ref bool __result)

        //    {
        //        bool flagIsCreatureMine = pawn.Faction != null && pawn.Faction.IsPlayer;
        //        bool flagIsCreatureDraftable = (pawn.TryGetComp<CompPolymorph>() != null);

        //        if (flagIsCreatureDraftable && flagIsCreatureMine)
        //        {
        //            //Log.Message("You should be controllable now");
        //            __result = true;
        //        }

        //    }
        //}

        [HarmonyPriority(100)] //Go last
        public static void AddHumanLikeOrders_RestrictEquipmentPatch(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);
            if (pawn.equipment != null)
            {
                ThingWithComps equipment = null;
                List<Thing> thingList = c.GetThingList(pawn.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i].def == TorannMagicDefOf.TM_Artifact_BracersOfThePacifist)
                    {
                        equipment = (ThingWithComps)thingList[i];
                        break;
                    }
                }
                if (equipment != null)
                {
                    string labelShort = equipment.LabelShort;
                    FloatMenuOption nve_option;
                    if (!(pawn.story.traits.HasTrait(TorannMagicDefOf.Priest) || pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent)))
                    {
                        for (int j = 0; j < opts.Count; j++)
                        {
                            if (opts[j].Label.Contains("wear"))
                            {
                                opts.Remove(opts[j]);
                            }
                        }
                        nve_option = new FloatMenuOption("TM_ViolentCannotEquip".Translate(pawn.LabelShort, labelShort), null);
                        opts.Add(nve_option);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders", null)]
        public static class FloatMenuMakerMap_Patch
        {
            public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
            {
                if (pawn == null)
                {
                    return;
                }
                IntVec3 c = IntVec3.FromVector3(clickPos);
                Enchantment.CompEnchant comp = pawn.TryGetComp<Enchantment.CompEnchant>();
                CompAbilityUserMagic pawnComp = pawn.TryGetComp<CompAbilityUserMagic>();
                if (comp != null && pawnComp != null && pawnComp.IsMagicUser && pawn.story != null && pawn.story.traits != null && !pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    if (comp.enchantingContainer == null)
                    {
                        Log.Warning($"Enchanting container is null for {pawn}, initializing.");
                        comp.enchantingContainer = new ThingOwner<Thing>();
                        //comp.enchantingContainer = new ThingOwner<Thing>(comp);
                    }
                    bool emptyGround = true;
                    foreach (Thing current in c.GetThingList(pawn.Map))
                    {
                        if (current != null && current.def.EverHaulable)
                        {
                            emptyGround = false;
                        }
                    }
                    if (emptyGround && !pawn.Drafted) //c.GetThingList(pawn.Map).Count == 0 &&
                    {
                        if (comp.enchantingContainer?.Count > 0)
                        {
                            if (!pawn.CanReach(c, PathEndMode.ClosestTouch, Danger.Deadly, false, false, TraverseMode.ByPawn))
                            {
                                opts.Add(new FloatMenuOption("TM_CannotDrop".Translate(
                                    comp.enchantingContainer[0].Label
                                ) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else
                            {
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TM_DropGem".Translate(
                                comp.enchantingContainer.ContentsString
                                ), delegate
                                {
                                    Job job = new Job(TorannMagicDefOf.JobDriver_RemoveEnchantingGem, c);
                                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, c, "ReservedBy"));
                            }
                        }

                    }
                    foreach (Thing current in c.GetThingList(pawn.Map))
                    {
                        Thing t = current;
                        if (t != null && t.def.EverHaulable && t.def.defName.ToString().Contains("TM_EStone_"))
                        {
                            if (!pawn.CanReach(t, PathEndMode.ClosestTouch, Danger.Deadly, false, false, TraverseMode.ByPawn))
                            {
                                opts.Add(new FloatMenuOption("CannotPickUp".Translate(
                                t.Label
                                ) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, t, 1))
                            {
                                opts.Add(new FloatMenuOption("CannotPickUp".Translate(
                                t.Label
                                ) + " (" + "TooHeavy".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else// if (item.stackCount == 1)
                            {
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TM_PickupGem".Translate(
                                t.Label
                                ), delegate
                                {
                                    t.SetForbidden(false, false);
                                    Job job = new Job(TorannMagicDefOf.JobDriver_AddEnchantingGem, t);
                                    job.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, t, "ReservedBy"));
                            }
                        }
                        else if ((current.def.IsApparel || current.def.IsWeapon || current.def.IsRangedWeapon) && comp.enchantingContainer?.Count > 0)
                        {
                            if (!pawn.CanReach(t, PathEndMode.ClosestTouch, Danger.Deadly, false, false, TraverseMode.ByPawn))
                            {
                                opts.Add(new FloatMenuOption("TM_CannotReach".Translate(
                                t.Label
                                ) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else if (pawnComp.Mana.CurLevel < .5f)
                            {
                                opts.Add(new FloatMenuOption("TM_NeedManaForEnchant".Translate(
                                pawnComp.Mana.CurLevel.ToString("0.000")
                                ), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else// if (item.stackCount == 1)
                            {
                                if (current.stackCount == 1)
                                {
                                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TM_EnchantItem".Translate(
                                        t.Label
                                    ), delegate
                                    {
                                        t.SetForbidden(true, false);
                                        Job job = new Job(TorannMagicDefOf.JobDriver_EnchantItem, t);
                                        job.count = 1;
                                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                    }, MenuOptionPriority.High, null, null, 0f, null, null), pawn, t, "ReservedBy"));
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddJobGiverWorkOrders", null)]
        public static class FloatMenuMakerMap_MagicJobGiver_Patch
        {
            public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts, bool drafted)
            {
                JobGiver_Work jobGiver_Work = pawn.thinker.TryGetMainTreeThinkNode<JobGiver_Work>();
                if (jobGiver_Work != null)
                {
                    foreach (Thing item in pawn.Map.thingGrid.ThingsAt(clickPos.ToIntVec3()))
                    {
                        if (item is Building && (item.def == TorannMagicDefOf.TableArcaneForge))
                        {
                            CompAbilityUserMagic comp = pawn.GetComp<CompAbilityUserMagic>();
                            if (comp != null && comp.Mana != null && comp.Mana.CurLevel < .5f)
                            {
                                string text = null;
                                Action action = null;
                                text = "TM_InsufficientManaForJob".Translate((comp.Mana.CurLevel * 100).ToString("0.##"));
                                FloatMenuOption menuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, action), pawn, item);
                                if (!opts.Any((FloatMenuOption op) => op.Label == menuOption.Label))
                                {
                                    menuOption.Disabled = true;
                                    opts.Add(menuOption);
                                }
                            }
                        }
                    }
                }
            }
        }

        //[HarmonyPatch(typeof(PawnUtility), "IsTravelingInTransportPodWorldObject", null), HarmonyPriority(1000)]
        //[HarmonyBefore(new string[] { "TheThirdAge.RemoveModernStuffHarmony.IsTravelingInTransportPodWorldObject", "rimworld.PawnUtility.IsTravelingInTransportPodWorldObject" })]        
        //[HarmonyPatch(typeof(PawnUtility), "IsTravelingInTransportPodWorldObject", null)]

        [HarmonyPriority(2000)]
        public static bool IsTravelingInTeleportPod_Prefix(Pawn pawn, ref bool __result)
        {
            if (pawn.IsColonist || (pawn.Faction != null && pawn.Faction.IsPlayer))
            {
                if(ModsConfig.IdeologyActive && pawn.IsSlaveOfColony)
                {
                    return true;
                }
                __result = pawn.IsWorldPawn() && ThingOwnerUtility.AnyParentIs<ActiveDropPodInfo>(pawn);
                return false;
            }
            return true;
        }


        //[HarmonyPatch(typeof(PawnAbility), "PostAbilityAttempt", null)]
        //public class PawnAbility_Patch
        //{
        //    public static bool Prefix(PawnAbility __instance)
        //    {
        //        if (__instance.Def.defName.Contains("TM_"))
        //        {
        //            CompAbilityUserMagic comp = __instance.Pawn.GetComp<CompAbilityUserMagic>();
        //            CompAbilityUserMight mightComp = __instance.Pawn.GetComp<CompAbilityUserMight>();
        //            if (comp.IsMagicUser && !__instance.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
        //            {
        //                __instance.CooldownTicksLeft = Mathf.RoundToInt((float)__instance.MaxCastingTicks * comp.coolDown);
        //                if (!__instance.Pawn.IsColonist)
        //                {
        //                    __instance.CooldownTicksLeft = (int)(__instance.CooldownTicksLeft / 2f);
        //                }
        //            }
        //            else if (mightComp.IsMightUser)
        //            {
        //                __instance.CooldownTicksLeft = Mathf.RoundToInt((float)__instance.MaxCastingTicks * mightComp.coolDown);
        //            }
        //            return false;
        //        }
        //        return true;
        //    }
        //}

        [HarmonyPatch(typeof(GenGrid), "Standable", null)]
        public class Standable_Patch
        {
            public static bool Prefix(ref IntVec3 c, ref Map map, ref bool __result)
            {
                if (map != null && c != default(IntVec3))
                {
                    return true;
                }
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(AttackTargetFinder), "CanSee", null)]
        public class AttackTargetFinder_CanSee_Patch
        {
            public static bool Prefix(Thing target, ref bool __result)
            {
                if (target is Pawn)
                {
                    Pawn targetPawn = target as Pawn;
                    if (targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_I, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_II, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_III, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_InvisibilityHD, false))
                    {
                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(AttackTargetFinder), "CanReach", null)]
        public class AttackTargetFinder_CanReach_Patch
        {
            public static bool Prefix(Thing target, ref bool __result)
            {
                if (target is Pawn)
                {
                    Pawn targetPawn = target as Pawn;
                    if (targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_I, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_II, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_III, false) || targetPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_InvisibilityHD, false))
                    {
                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(JobGiver_Mate), "TryGiveJob", null)]
        public class JobGiver_Mate_Patch
        {
            public static void Postfix(Pawn pawn, ref Job __result)
            {
                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD"), false) || pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")))
                {
                    __result = null;
                }
            }
        }

        [HarmonyPatch(typeof(MapParent), "CheckRemoveMapNow", null)]
        public class CheckRemoveMapNow_Patch
        {
            public static bool Prefix()
            {
                bool inFlight = ModOptions.Constants.GetPawnInFlight();
                if (inFlight)
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(CompMilkable), "CompInspectStringExtra", null)]
        public class CompMilkable_Patch
        {
            public static void Postfix(CompMilkable __instance, ref string __result)
            {
                if (__instance.parent.def.defName == "Poppi")
                {
                    __result = "Poppi_fuelGrowth".Translate() + ": " + __instance.Fullness.ToStringPercent();
                }
            }
        }

        [HarmonyPatch(typeof(JobGiver_Kidnap), "TryGiveJob", null)]
        public class JobGiver_Kidnap_Patch
        {
            public static void Postfix(Pawn pawn, ref Job __result)
            {
                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")))
                {
                    __result = null;
                }
            }
        }

        [HarmonyPatch(typeof(ITab_Pawn_Gear), "DrawThingRow", null)]
        public class ITab_Pawn_Gear_Patch
        {
            public static Rect GetRowRect(Rect inRect, int row)
            {
                float y = 20f * (float)row;
                Rect result = new Rect(inRect.x, y, inRect.width, 18f);
                return result;
            }

            public static void Postfix(ref float y, float width, Thing thing)
            {
                bool valid = !thing.DestroyedOrNull() && thing.TryGetQuality(out QualityCategory qc);
                if (valid)
                {
                    if (((thing.def.thingClass != null && thing.def.thingClass.ToString() == "RimWorld.Apparel") || thing.TryGetComp<CompEquippable>() != null) && thing.TryGetComp<Enchantment.CompEnchantedItem>() != null)
                    {
                        if (thing.TryGetComp<Enchantment.CompEnchantedItem>().HasEnchantment)
                        {
                            Text.Font = GameFont.Tiny;
                            string str1 = "-- Enchanted (";
                            string str2 = "Enchanted \n\n";

                            Enchantment.CompEnchantedItem enchantedItem = thing.TryGetComp<Enchantment.CompEnchantedItem>();
                            if (enchantedItem.maxMP != 0)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.maxMPTier);
                                str1 += "M";
                                str2 += enchantedItem.MaxMPLabel + "\n";
                            }
                            if (enchantedItem.mpCost != 0)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.mpCostTier);
                                str1 += "C";
                                str2 += enchantedItem.MPCostLabel + "\n";
                            }
                            if (enchantedItem.mpRegenRate != 0)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.mpRegenRateTier);
                                str1 += "R";
                                str2 += enchantedItem.MPRegenRateLabel + "\n";
                            }
                            if (enchantedItem.coolDown != 0)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.coolDownTier);
                                str1 += "D";
                                str2 += enchantedItem.CoolDownLabel + "\n";
                            }
                            if (enchantedItem.xpGain != 0)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.xpGainTier);
                                str1 += "G";
                                str2 += enchantedItem.XPGainLabel + "\n";
                            }
                            if (enchantedItem.arcaneRes != 0)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.arcaneResTier);
                                str1 += "X";
                                str2 += enchantedItem.ArcaneResLabel + "\n";
                            }
                            if (enchantedItem.arcaneDmg != 0)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.arcaneDmgTier);
                                str1 += "Z";
                                str2 += enchantedItem.ArcaneDmgLabel + "\n";
                            }
                            if (enchantedItem.arcaneSpectre != false)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.skillTier);
                                str1 += "*S";
                                str2 += enchantedItem.ArcaneSpectreLabel + "\n";
                            }
                            if (enchantedItem.phantomShift != false)
                            {
                                GUI.color = Enchantment.GenEnchantmentColor.EnchantmentColor(enchantedItem.skillTier);
                                str1 += "*P";
                                str2 += enchantedItem.PhantomShiftLabel + "\n";
                            }
                            str1 += ")";
                            y -= 6f;
                            Rect rect = new Rect(48f, y, width - 36f, 28f);
                            Widgets.Label(rect, str1);

                            TooltipHandler.TipRegion(rect, () => string.Concat(new string[]
                            {
                            str2,
                            }), 398512);

                            y += 28f;
                            GUI.color = Color.white;
                            Text.Font = GameFont.Small;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ITab_Pawn_Gear), "TryDrawOverallArmor", null)]
        public class ITab_Pawn_GearFillTab_Patch
        {
            //public static FieldInfo pawn = typeof(ITab_Pawn_Gear).GetField("SelPawnForGear", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

            public static void Postfix(ITab_Pawn_Gear __instance, ref float curY, float width, StatDef stat, string label)
            {
                if (stat.defName == "ArmorRating_Heat")
                {
                    //Traverse traverse = Traverse.Create(__instance);
                    Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
                    if (!pawn.DestroyedOrNull() && !pawn.Dead)
                    {
                        stat = StatDef.Named("ArmorRating_Alignment");
                        label = "TM_ArmorHarmony".Translate();
                        float num = 0f;
                        float num2 = Mathf.Clamp01(pawn.GetStatValue(stat, true) / 2f);
                        List<BodyPartRecord> allParts = pawn.RaceProps.body.AllParts;
                        List<Apparel> list = (pawn.apparel == null) ? null : pawn.apparel.WornApparel;
                        for (int i = 0; i < allParts.Count; i++)
                        {
                            float num3 = 1f - num2;
                            if (list != null)
                            {
                                for (int j = 0; j < list.Count; j++)
                                {
                                    if (list[j].def.apparel.CoversBodyPart(allParts[i]))
                                    {
                                        float num4 = Mathf.Clamp01(list[j].GetStatValue(stat, true) / 2f);
                                        num3 *= 1f - num4;
                                    }
                                }
                            }
                            num += allParts[i].coverageAbs * (1f - num3);
                        }
                        num = Mathf.Clamp(num * 2f, 0f, 2f);
                        Rect rect = new Rect(0f, curY, width, 100f);
                        Widgets.Label(rect, label.Truncate(120f, null));
                        rect.xMin += 120f;
                        Widgets.Label(rect, num.ToStringPercent());
                        curY += 22f;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(NegativeInteractionUtility), "NegativeInteractionChanceFactor", null)]
        public class NegativeInteractionChanceFactor_Patch
        {
            public static void Postfix(Pawn initiator, Pawn recipient, ref float __result)
            {
                CompAbilityUserMagic comp = initiator.GetComp<CompAbilityUserMagic>();
                if (comp != null && (initiator.story.traits.HasTrait(TorannMagicDefOf.TM_Bard) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_Entertain, comp, null)))
                {
                    MagicPowerSkill ver = comp.MagicData.MagicPowerSkill_Entertain.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Entertain_ver");
                    __result = __result / (1 + ver.level);

                }
                if (initiator.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper))
                {
                    __result *= 1.2f;
                }
                if (initiator.Inspired && initiator.InspirationDef.defName == "Outgoing")
                {
                    __result = __result * .5f;
                }
            }
        }

        [HarmonyPatch(typeof(InspirationHandler), "TryStartInspiration", null)]
        public class InspirationHandler_Patch
        {
            public static bool Prefix(InspirationHandler __instance, ref bool __result)
            {
                if (__instance.pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")) || __instance.pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_LichHD")))
                {
                    __result = false;
                    return false;
                }
                return true;
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
                        List<Pawn> mapPawns = m.mapPawns.AllPawnsSpawned;
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

        [HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawIcons", null)]
        public class ColonistBarColonistDrawer_Patch
        {
            public static void Postfix(ColonistBarColonistDrawer __instance, ref Rect rect, Pawn colonist)
            {
                if (!colonist.Dead)
                {
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    if (colonist.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")))
                    {
                        float num = 20f * Find.ColonistBar.Scale * settingsRef.classIconSize;
                        Vector2 vector = new Vector2(rect.x + 1f, rect.yMin + 1f);
                        rect = new Rect(vector.x, vector.y, num, num);
                        GUI.DrawTexture(rect, TM_MatPool.Icon_Undead);
                        TooltipHandler.TipRegion(rect, "TM_Icon_Undead".Translate());
                        vector.x += num;
                        //rect = new Rect(vector.x, vector.y, num, num);
                    }
                    else if (settingsRef.showClassIconOnColonistBar && colonist.story != null)
                    {
                        float num = 20f * Find.ColonistBar.Scale * settingsRef.classIconSize;
                        Vector2 vector = new Vector2(rect.x + 1f, rect.yMin + 1f);
                        rect = new Rect(vector.x, vector.y, num, num);
                        CompAbilityUserMight compMight = colonist.TryGetComp<CompAbilityUserMight>();
                        CompAbilityUserMagic compMagic = colonist.TryGetComp<CompAbilityUserMagic>();
                        if (compMagic != null && compMagic.customClass != null)
                        {
                            Texture2D customIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/CustomMageMark", true);
                            if (compMagic.customClass.classTexturePath != "")
                            {
                                customIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/" + compMagic.customClass.classTexturePath, true);
                            }
                            GUI.DrawTexture(rect, customIcon);
                            TooltipHandler.TipRegion(rect, "TM_Icon_Custom".Translate());
                            vector.x += num;
                        }
                        else if (compMight != null && compMight.customClass != null)
                        {
                            Texture2D customIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/CustomFighterMark", true);
                            if (compMight.customClass.classTexturePath != "")
                            {
                                customIcon = ContentFinder<Texture2D>.Get("Other/ClassTextures/" + compMight.customClass.classTexturePath, true);
                            }
                            GUI.DrawTexture(rect, customIcon);
                            TooltipHandler.TipRegion(rect, "TM_Icon_Custom".Translate());
                            vector.x += num;

                        }
                        else
                        {

                            if (colonist.story.traits.HasTrait(TorannMagicDefOf.InnerFire))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.fireIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.iceIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.StormBorn))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.lightningIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Arcanist))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.arcanistIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Paladin))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.paladinIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Summoner))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.summonerIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Druid))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.druidIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || colonist.story.traits.HasTrait(TorannMagicDefOf.Lich))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.necroIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Priest))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.priestIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.TM_Bard))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.bardIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Succubus) || colonist.story.traits.HasTrait(TorannMagicDefOf.Warlock))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.demonkinIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Geomancer))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.earthIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Technomancer))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.technoIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.BloodMage))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.bloodmageIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Enchanter))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.enchanterIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Chronomancer))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.chronoIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Gladiator))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.gladiatorIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Fighter".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.sniperIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Fighter".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Bladedancer))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.bladedancerIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Fighter".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Ranger))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.rangerIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Fighter".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.facelessIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Fighter".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.psiIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Fighter".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.DeathKnight))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.deathknightIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Fighter".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.TM_Monk))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.monkIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Fighter".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.wandererIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.wayfarerIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Fighter".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.chaosIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Mage".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.TM_Commander))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.commanderIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Fighter".Translate());
                                vector.x += num;
                            }
                            else if (colonist.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier))
                            {
                                GUI.DrawTexture(rect, TM_MatPool.SSIcon);
                                TooltipHandler.TipRegion(rect, "TM_Icon_Fighter".Translate());
                                vector.x += num;
                            }
                        }
                        //rect = new Rect(vector.x, vector.y, num, num);
                    }
                }
                //return true;
            }
        }

        [HarmonyPatch(typeof(Pawn_InteractionsTracker), "InteractionsTrackerTick", null)]
        public class InteractionsTrackerTick_Patch
        {
            public static FieldInfo pawn = typeof(Pawn_InteractionsTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            public static FieldInfo wantsRandomInteract = typeof(Pawn_InteractionsTracker).GetField("wantsRandomInteract", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            public static FieldInfo lastInteractionTime = typeof(Pawn_InteractionsTracker).GetField("lastInteractionTime", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

            public static void Postfix(Pawn_InteractionsTracker __instance)
            {
                if (Find.TickManager.TicksGame % 1200 == 0)
                {
                    Traverse traverse = Traverse.Create(__instance);
                    Pawn pawn = (Pawn)InteractionsTrackerTick_Patch.pawn.GetValue(__instance);
                    if (pawn.IsColonist && !pawn.Downed && !pawn.Dead && pawn.RaceProps.Humanlike)
                    {
                        CompAbilityUserMagic comp = pawn.GetComp<CompAbilityUserMagic>();
                        int lastInteractionTime = (int)InteractionsTrackerTick_Patch.lastInteractionTime.GetValue(__instance);
                        if (comp != null && comp.IsMagicUser && (comp.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_Entertain, comp, null)))
                        {
                            MagicPowerSkill pwr = comp.MagicData.MagicPowerSkill_Entertain.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Entertain_pwr");
                            if ((Find.TickManager.TicksGame - lastInteractionTime) > (3000 - (450 * pwr.level)))
                            {
                                InteractionsTrackerTick_Patch.wantsRandomInteract.SetValue(__instance, true);
                            }
                        }
                        if (pawn.Inspired && pawn.InspirationDef.defName == "ID_Outgoing")
                        {
                            if ((Find.TickManager.TicksGame - lastInteractionTime) > (1800))
                            {
                                InteractionsTrackerTick_Patch.wantsRandomInteract.SetValue(__instance, true);
                            }
                        }
                        if (pawn.health != null && pawn.health.hediffSet != null && pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TaskMasterHD))
                        {
                            if ((Find.TickManager.TicksGame - lastInteractionTime) < 30000)
                            {
                                InteractionsTrackerTick_Patch.wantsRandomInteract.SetValue(__instance, false);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState", null)]
        public class MentalStateHandler_Patch
        {
            public static FieldInfo pawn = typeof(MentalStateHandler).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

            public static bool Prefix(MentalStateHandler __instance, MentalStateDef stateDef, Pawn otherPawn, ref bool __result)
            {
                Traverse traverse = Traverse.Create(__instance);
                Pawn pawn = (Pawn)MentalStateHandler_Patch.pawn.GetValue(__instance);

                if (pawn.RaceProps.Humanlike && (TM_Calc.IsUndeadNotVamp(pawn)))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPriority(100)]
        [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed", null)]
        public static class Pawn_NeedsTracker_Patch
        {
            //public static FieldInfo pawn = typeof(Pawn_NeedsTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            public static bool Prefix(Pawn_NeedsTracker __instance, NeedDef nd, Pawn ___pawn, ref bool __result)
            {
                //Traverse traverse = Traverse.Create(__instance);
                Pawn pawn = ___pawn;
                if (pawn != null)
                {
                    if (nd.defName == "ROMV_Blood" && (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")) || pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")) || pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_LichHD"))))
                    {
                        bool hasVampHediff = pawn.health.hediffSet.HasHediff(HediffDef.Named("ROM_Vampirism"));
                        if (hasVampHediff)
                        {
                            return true;
                        }
                        __result = false;
                        return false;
                    }
                    if ((nd.defName == "TM_Mana" || nd.defName == "TM_Stamina") && pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")))
                    {
                        __result = false;
                        return false;
                    }
                    if (nd == TorannMagicDefOf.TM_Travel)// && pawn.story != null && pawn.story.traits != null)
                    {
                        __result = false;
                        return false;
                    }
                    if (TM_GolemUtility.GolemPawns.Contains(pawn.def))
                    {
                        foreach (NeedDef n in TM_GolemUtility.GetGolemDefFromThing(___pawn).needs)
                        {
                            if (n != null && n == nd)
                            {
                                __result = true;
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor", null), HarmonyPriority(100)]
        public static class FloatMenuMakerMap_ROMV_Undead_Patch
        {
            public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> __result)
            {
                IntVec3 c = IntVec3.FromVector3(clickPos);
                Pawn target = c.GetFirstPawn(pawn.Map);
                if (target != null)
                {
                    if ((target.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")) || target.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")) || target.health.hediffSet.HasHediff(HediffDef.Named("TM_LichHD"))))
                    {
                        for (int i = 0; i < __result.Count(); i++)
                        {
                            string name = target.LabelShort;
                            if (__result[i].Label.Contains("Feed on") || __result[i].Label.Contains("Sip") || __result[i].Label.Contains("Embrace") || __result[i].Label.Contains("Give vampirism") || __result[i].Label.Contains("Create Ghoul") || __result[i].Label.Contains("Give vitae") || __result[i].Label == "Embrace " + name + " (Give vampirism)")
                            {
                                __result.Remove(__result[i]);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(DamageWorker_AddInjury), "Apply", null)]
        public static class DamageWorker_ApplyEnchantmentAction_Patch
        {
            public static void Postfix(DamageWorker_AddInjury __instance, DamageInfo dinfo, Thing thing, DamageWorker.DamageResult __result)
            {
                if (dinfo.Instigator != null && dinfo.Instigator is Pawn && dinfo.Amount != 0 && dinfo.Weapon != null && dinfo.Weapon.HasComp(typeof(TorannMagic.Enchantment.CompEnchantedItem)))
                {
                    Pawn instigator = dinfo.Instigator as Pawn;
                    if (instigator.equipment != null && instigator.equipment.Primary != null)
                    {
                        ThingWithComps eq = instigator.equipment.Primary;
                        TorannMagic.Enchantment.CompEnchantedItem enchantment = eq.TryGetComp<TorannMagic.Enchantment.CompEnchantedItem>();
                        if (enchantment != null && enchantment.enchantmentAction != null)
                        {
                            if (enchantment.enchantmentAction.type == Enchantment.EnchantmentActionType.ApplyHediff && enchantment.enchantmentAction.hediffDef != null)
                            {
                                if (Rand.Chance(enchantment.enchantmentAction.hediffChance))
                                {
                                    if (enchantment.enchantmentAction.onSelf)
                                    {
                                        List<Pawn> plist = TM_Calc.FindPawnsNearTarget(instigator, Mathf.RoundToInt(enchantment.enchantmentAction.splashRadius), instigator.Position, enchantment.enchantmentAction.friendlyFire);
                                        if (plist != null && plist.Count > 0)
                                        {
                                            for (int i = 0; i < plist.Count; i++)
                                            {
                                                HealthUtility.AdjustSeverity(plist[i], enchantment.enchantmentAction.hediffDef, enchantment.enchantmentAction.hediffSeverity);
                                                if (enchantment.enchantmentAction.hediffDurationTicks != 0)
                                                {
                                                    HediffComp_Disappears hdc = plist[i].health.hediffSet.GetFirstHediffOfDef(enchantment.enchantmentAction.hediffDef).TryGetComp<HediffComp_Disappears>();
                                                    hdc.ticksToDisappear = enchantment.enchantmentAction.hediffDurationTicks;
                                                }

                                            }
                                        }
                                        HealthUtility.AdjustSeverity(instigator, enchantment.enchantmentAction.hediffDef, enchantment.enchantmentAction.hediffSeverity);
                                        if (enchantment.enchantmentAction.hediffDurationTicks != 0)
                                        {
                                            HediffComp_Disappears hdc = instigator.health.hediffSet.GetFirstHediffOfDef(enchantment.enchantmentAction.hediffDef).TryGetComp<HediffComp_Disappears>();
                                            hdc.ticksToDisappear = enchantment.enchantmentAction.hediffDurationTicks;
                                        }
                                    }
                                    else if (thing is Pawn)
                                    {
                                        Pawn p = thing as Pawn;
                                        if (enchantment.enchantmentAction.splashRadius > 0)
                                        {
                                            List<Pawn> plist = TM_Calc.FindPawnsNearTarget(p, Mathf.RoundToInt(enchantment.enchantmentAction.splashRadius), p.Position, enchantment.enchantmentAction.friendlyFire);
                                            if (plist != null && plist.Count > 0)
                                            {
                                                for (int i = 0; i < plist.Count; i++)
                                                {
                                                    HealthUtility.AdjustSeverity(plist[i], enchantment.enchantmentAction.hediffDef, enchantment.enchantmentAction.hediffSeverity);
                                                    if (enchantment.enchantmentAction.hediffDurationTicks != 0)
                                                    {
                                                        HediffComp_Disappears hdc = plist[i].health.hediffSet.GetFirstHediffOfDef(enchantment.enchantmentAction.hediffDef).TryGetComp<HediffComp_Disappears>();
                                                        hdc.ticksToDisappear = enchantment.enchantmentAction.hediffDurationTicks;
                                                    }
                                                }
                                            }
                                        }
                                        HealthUtility.AdjustSeverity(p, enchantment.enchantmentAction.hediffDef, enchantment.enchantmentAction.hediffSeverity);
                                        if (enchantment.enchantmentAction.hediffDurationTicks != 0)
                                        {
                                            HediffComp_Disappears hdc = instigator.health.hediffSet.GetFirstHediffOfDef(enchantment.enchantmentAction.hediffDef).TryGetComp<HediffComp_Disappears>();
                                            hdc.ticksToDisappear = enchantment.enchantmentAction.hediffDurationTicks;
                                        }
                                    }
                                }
                            }
                            if (enchantment.enchantmentAction.type == Enchantment.EnchantmentActionType.ApplyDamage && enchantment.enchantmentAction.damageDef != null && dinfo.Def != enchantment.enchantmentAction.damageDef)
                            {
                                if (Rand.Chance(enchantment.enchantmentAction.damageChance))
                                {
                                    DamageInfo dinfo2 = new DamageInfo(enchantment.enchantmentAction.damageDef, Rand.Range(enchantment.enchantmentAction.damageAmount - enchantment.enchantmentAction.damageVariation, enchantment.enchantmentAction.damageAmount + enchantment.enchantmentAction.damageVariation), enchantment.enchantmentAction.armorPenetration, -1f, instigator, null, dinfo.Weapon, DamageInfo.SourceCategory.ThingOrUnknown);

                                    if (enchantment.enchantmentAction.onSelf)
                                    {
                                        List<Pawn> plist = TM_Calc.FindPawnsNearTarget(instigator, Mathf.RoundToInt(enchantment.enchantmentAction.splashRadius), instigator.Position, enchantment.enchantmentAction.friendlyFire);
                                        if (plist != null && plist.Count > 0)
                                        {
                                            for (int i = 0; i < plist.Count; i++)
                                            {
                                                plist[i].TakeDamage(dinfo2);
                                            }
                                        }
                                        instigator.TakeDamage(dinfo2);
                                    }
                                    else if (thing is Pawn)
                                    {
                                        Pawn p = thing as Pawn;
                                        List<Pawn> plist = TM_Calc.FindPawnsNearTarget(p, Mathf.RoundToInt(enchantment.enchantmentAction.splashRadius), p.Position, enchantment.enchantmentAction.friendlyFire);
                                        if (plist != null && plist.Count > 0)
                                        {
                                            for (int i = 0; i < plist.Count; i++)
                                            {
                                                plist[i].TakeDamage(dinfo2);
                                            }
                                        }
                                        p.TakeDamage(dinfo2);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(DamageWorker), "ExplosionStart", null)]
        public static class ExplosionNoShaker_Patch
        {
            public static bool Prefix(DamageWorker __instance, Explosion explosion)
            {
                if (explosion.damType == TMDamageDefOf.DamageDefOf.TM_BlazingPower || explosion.damType == TMDamageDefOf.DamageDefOf.TM_BloodBurn || explosion.damType == TMDamageDefOf.DamageDefOf.TM_HailDD)
                {
                    float radMod = 6f;
                    if (explosion.damType == TMDamageDefOf.DamageDefOf.TM_HailDD)
                    {
                        radMod = 1f;
                    }
                    FleckMaker.Static(explosion.Position, explosion.Map, FleckDefOf.ExplosionFlash, explosion.radius * radMod);
                    if (explosion.damType.explosionSnowMeltAmount < 0)
                    {
                        Projectile_Snowball.AddSnowRadial(explosion.Position, explosion.Map, explosion.radius, explosion.radius);
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        if (explosion.damType == TMDamageDefOf.DamageDefOf.TM_BloodBurn)
                        {
                            if (i < 1)
                            {                                
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodMist, explosion.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(explosion.radius * 0.7f), explosion.Map, Rand.Range(1f, 1.5f), .2f, 0.6f, 2f, Rand.Range(-30, 30), Rand.Range(.5f, .7f), Rand.Range(30f, 40f), Rand.Range(0, 360));
                            }
                        }
                        else
                        {
                            FleckMaker.ThrowSmoke(explosion.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(explosion.radius * 0.7f), explosion.Map, explosion.radius * 0.6f);
                        }
                    }
                    if (__instance.def.explosionInteriorMote != null)
                    {
                        int num = Mathf.RoundToInt(3.14159274f * explosion.radius * explosion.radius / 6f);
                        for (int j = 0; j < num; j++)
                        {
                            MoteMaker.ThrowExplosionInteriorMote(explosion.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(explosion.radius * 0.7f), explosion.Map, __instance.def.explosionInteriorMote);
                        }
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(QualityUtility), "GenerateQualityCreatedByPawn", null)]
        [HarmonyPatch(new Type[]
        {
            typeof(Pawn),
            typeof(SkillDef)
        })]
        public static class ArcaneForge_Quality_Patch
        {
            public static void Postfix(Pawn pawn, SkillDef relevantSkill, ref QualityCategory __result)
            {
                CompAbilityUserMagic comp = pawn.TryGetComp<CompAbilityUserMagic>();
                if (comp != null && comp.IsMagicUser && pawn.story.traits != null && !pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) && comp.ArcaneForging)
                {
                    List<IntVec3> cellList = GenRadial.RadialCellsAround(pawn.Position, 2, true).ToList();
                    bool forgeNearby = false;
                    for (int i = 0; i < cellList.Count; i++)
                    {
                        List<Thing> thingList = cellList[i].GetThingList(pawn.Map);
                        if (thingList != null && thingList.Count > 0)
                        {
                            for (int j = 0; j < thingList.Count; j++)
                            {
                                if (thingList[j] != null && thingList[j] is Building)
                                {
                                    Building bldg = thingList[j] as Building;
                                    if (bldg.def == TorannMagicDefOf.TableArcaneForge)
                                    {
                                        forgeNearby = true;
                                        break;
                                    }
                                }
                            }
                            if (forgeNearby)
                            {
                                break;
                            }
                        }
                    }
                    if (forgeNearby)
                    {
                        int mageLevel = Rand.Range(0, Mathf.RoundToInt(comp.MagicUserLevel / 15));
                        __result = (QualityCategory)Mathf.Min((int)__result + mageLevel, 6);
                        SoundInfo info = SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map, false), MaintenanceType.None);
                        info.pitchFactor = .6f;
                        info.volumeFactor = 1.6f;
                        TorannMagicDefOf.TM_Gong.PlayOneShot(info);
                        cellList.Clear();
                        cellList = GenRadial.RadialCellsAround(pawn.Position, (int)__result, false).ToList<IntVec3>();
                        for (int i = 0; i < cellList.Count; i++)
                        {
                            IntVec3 curCell = cellList[i];
                            Vector3 angle = TM_Calc.GetVector(pawn.Position, curCell);
                            TM_MoteMaker.ThrowArcaneWaveMote(curCell.ToVector3(), pawn.Map, .4f * (curCell - pawn.Position).LengthHorizontal, .1f, .05f, .1f, 0, 3, (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(), (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat());
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(WorkGiver_DoBill), "JobOnThing", null)]
        public static class RuneCarving_WorkGiver_Patch
        {
            public static void Postfix(ref Job __result, Pawn pawn, Thing thing, bool forced = false)
            {
                if (!((__result == null || pawn == null || thing == null) | forced))
                {
                    IBillGiver billGiver = thing as IBillGiver;
                    if (billGiver != null)
                    {
                        Bill bill = __result.bill;
                        if (bill != null)
                        {
                            RecipeDef recipe = bill.recipe;
                            if (recipe != null && ConfirmRuneCarving(pawn, billGiver, recipe))
                            {
                                __result = null;
                            }
                        }
                    }
                }
            }

            private static bool ConfirmRuneCarving(Pawn pawn, IBillGiver billGiver, RecipeDef recipe)
            {
                if (recipe == null || !recipe.IsSurgery)
                {
                    return false;
                }
                if (!(recipe == TorannMagicDefOf.TM_RuneCarveBodyPart))
                {
                    return false;
                }
                return !IsCapableRuneCarver(pawn, recipe);
            }

            private static bool IsCapableRuneCarver(Pawn p, RecipeDef recipe)
            {
                if (p.RaceProps.Humanlike && p.skills != null)
                {
                    CompAbilityUserMagic comp = p.GetComp<CompAbilityUserMagic>();
                    if (p.workSettings.WorkIsActive(WorkTypeDefOf.Doctor) && comp != null && (p.story.traits.HasTrait(TorannMagicDefOf.TM_Golemancer) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_RuneCarving, comp, null)))
                    {
                        if (comp.MagicData.MagicPowersGolemancer.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RuneCarving).learned && recipe.PawnSatisfiesSkillRequirements(p) && p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && p.health.capacities.CapableOf(PawnCapacityDefOf.Moving) && !p.skills.GetSkill(SkillDefOf.Artistic).TotallyDisabled && !p.skills.GetSkill(SkillDefOf.Crafting).TotallyDisabled)
                        {
                            if (comp.Mana.CurLevel > TorannMagicDefOf.TM_RuneCarving.manaCost)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(WorkGiver_DoBill), "JobOnThing", null)]
        public static class RegrowthSurgery_WorkGiver_Patch
        {
            public static void Postfix(ref Job __result, Pawn pawn, Thing thing, bool forced = false)
            {
                if (!((__result == null || pawn == null || thing == null) | forced))
                {
                    IBillGiver billGiver = thing as IBillGiver;
                    if (billGiver != null)
                    {
                        Bill bill = __result.bill;
                        if (bill != null)
                        {
                            RecipeDef recipe = bill.recipe;
                            if (recipe != null && ConfirmRegrowthSurgery(pawn, billGiver, recipe))
                            {
                                __result = null;
                            }
                        }
                    }
                }
            }

            private static Pawn GiverPawn(IBillGiver billGiver)
            {
                Pawn pawn = null;
                if (billGiver is Corpse)
                {
                    Corpse corpse = billGiver as Corpse;
                    pawn = corpse.InnerPawn;
                }
                if (billGiver is Pawn)
                {
                    pawn = billGiver as Pawn;
                }
                return pawn;
            }

            private static bool ConfirmRegrowthSurgery(Pawn pawn, IBillGiver billGiver, RecipeDef recipe)
            {
                if (recipe == null || !recipe.IsSurgery)
                {
                    return false;
                }
                if (!(recipe == TorannMagicDefOf.Regrowth || recipe == TorannMagicDefOf.UniversalRegrowth))
                {
                    return false;
                }
                //Pawn giverPawn = GiverPawn(billGiver);
                //if (giverPawn.RaceProps.IsMechanoid)
                //{
                //    return false;
                //}
                return !IsCapableDruid(pawn, recipe);
            }

            private static bool IsCapableDruid(Pawn p, RecipeDef recipe)
            {
                if (p.RaceProps.Humanlike && p.skills != null)
                {
                    CompAbilityUserMagic comp = p.GetComp<CompAbilityUserMagic>();
                    if (p.workSettings.WorkIsActive(WorkTypeDefOf.Doctor) && comp != null && (p.story.traits.HasTrait(TorannMagicDefOf.Druid) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_RegrowLimb, comp, null)))
                    {
                        if (comp.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RegrowLimb).learned && recipe.PawnSatisfiesSkillRequirements(p) && p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && p.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
                        {
                            if (comp.Mana.CurLevel > .9f)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
        }

        public static float sX = 0;
        [HarmonyPatch(typeof(GizmoGridDrawer), "DrawGizmoGrid", null)]
        public static class DrawGizmo_Patch
        {
            private static bool Prefix(IEnumerable<Gizmo> gizmos, float startX)//, out Gizmo mouseoverGizmo)
            {
                // Log.Message("startx: " + startX + " count: " + gizmos.Count());
                if (ModOptions.Settings.Instance.shrinkIcons)
                {
                    foreach (Gizmo g in gizmos)
                    {
                        Command_PawnAbility com = g as Command_PawnAbility;
                        if (com != null)
                        {
                            com.shrinkable = true;
                        }
                    }
                    sX = startX;
                }
                //GizmoGridDrawerMod.DrawGizmoGrid(gizmos, startX, out mouseoverGizmo);
                return true;
            }
        }

        [HarmonyPatch(typeof(Command), "GizmoOnGUIInt", null)]
        public static class GizmoOnGUIInt_Prefix_Patch
        {
            public static bool Prefix(Command __instance, Rect butRect, GizmoRenderParms parms, ref GizmoResult __result)
            {
                if (ModOptions.Settings.Instance.autocastEnabled)
                {
                    Command_PawnAbility com = __instance as Command_PawnAbility;
                    if (com != null && com.pawnAbility != null && com.pawnAbility.Def.defName.StartsWith("TM_"))
                    {
                        //Log.Message("patching command for pawn ability with butRect " + butRect.x + " " + butRect.y + " " + butRect.width + " " + butRect.height + " shrunk: " + shrunk);
                        __result = TM_Action.DrawAutoCastForGizmo(com, butRect, parms.shrunk, __result);
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Command_PawnAbility), "GizmoOnGUIInt", null)]
        public static class GizmoOnGUI_Prefix_Patch
        {
            public static bool Prefix(Command_PawnAbility __instance, Rect butRect, GizmoRenderParms parms, ref GizmoResult __result)
            {
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (settingsRef.autocastEnabled && __instance.pawnAbility.Def.defName.StartsWith("TM_"))
                {
                    //Rect rect = new Rect(topLeft.x, topLeft.y, __instance.GetWidth(maxWidth), 75f);
                    __result = TM_Action.DrawAutoCastForGizmo(__instance, butRect, parms.shrunk, __result);
                    return false;
                }
                return true;
            }
        }

    }
}
