using HarmonyLib;
using RimWorld;
using AbilityUser;
using RimWorld.Planet;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.AI;
using AbilityUserAI;
using TorannMagic.Conditions;
using TorannMagic.Golems;
using TorannMagic.Weapon;

namespace TorannMagic
{
    public partial class TorannMagicMod : Mod
    {
        private static readonly Type patchType = typeof(TorannMagicMod);

        private void PatchMethod(HarmonyLib.Harmony harmonyInstance,
            MethodBase original,
            HarmonyMethod prefix = null,
            HarmonyMethod postfix = null,
            HarmonyMethod transpiler = null,
            HarmonyMethod finalizer = null,
            string context = "")
        {
            if (original == null)
            {
                Log.Error("TM-Forked: Could not find " + context);
            }

            harmonyInstance.Patch(original, prefix, postfix, transpiler, finalizer);
        }

        public TorannMagicMod(ModContentPack content) : base(content)
        {
            var harmonyInstance = new Harmony("rimworld.torann.tmagic");
            // Harmony.DEBUG = true;
            PatchMethod(harmonyInstance, AccessTools.Method(typeof(Pawn), "CanTakeOrder"),
                new HarmonyMethod(patchType, nameof(GolemOrders_Patch)), null,
                 null, null,
                "Pawn_CanTakeOrder");
            PatchMethod(harmonyInstance, AccessTools.Method(typeof(Pawn), "AddUndraftedOrders"),
                new HarmonyMethod(patchType, nameof(GolemUndraftedOrder_Patch)), null,
                 null, null,
                "Pawn_AddUndraftedOrders");
            
            PatchMethod(harmonyInstance, AccessTools.Method(typeof(IncidentWorker_SelfTame), "Candidates"),
                null,
                new HarmonyMethod(patchType, nameof(SelfTame_Candidates_Patch)), null, null,
                "IncidentWorker_SelfTame_Candidates");
            PatchMethod(harmonyInstance,
                AccessTools.Method(typeof(IncidentWorker_DiseaseHuman), "PotentialVictimCandidates"), null,
                new HarmonyMethod(patchType, nameof(DiseaseHuman_Candidates_Patch)), null, null,
                "IncidentWorker_DiseaseHuman_PotentialVictimCandidates");

            PatchMethod(harmonyInstance,
                AccessTools.Method(typeof(IncidentWorker_DiseaseAnimal), "PotentialVictimCandidates"), null,
                new HarmonyMethod(patchType, nameof(DiseaseHuman_Candidates_Patch)), null, null,
                "IncidentWorker_DiseaseAnimal_PotentialVictimCandidates");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(Pawn), "GetGizmos"), null,
                new HarmonyMethod(patchType, nameof(Pawn_Gizmo_TogglePatch)), null, null, "Pawn_GetGizmos");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(CompPowerPlant), "CompTick"), null,
                new HarmonyMethod(patchType, nameof(PowerCompTick_Overdrive_Postfix)), null, null,
                "CompPowerPlant_CompTick");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(Building_TurretGun), "Tick"), null,
                new HarmonyMethod(patchType, nameof(TurretGunTick_Overdrive_Postfix)), null, null,
                "Building_TurretGun_Tick");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(CompRefuelable), "PostDraw"),
                new HarmonyMethod(patchType, nameof(CompRefuelable_DrawBar_Prefix)), null, null, null,
                "CompRefuelable_PostDraw");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(AutoUndrafter), "ShouldAutoUndraft"),
                new HarmonyMethod(patchType, nameof(AutoUndrafter_Undead_Prefix)), null, null, null,
                "AutoUndrafter_ShouldAutoUndraft");

            // PatchMethod(harmonyInstance, AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"),
            //     null,
            //     new HarmonyMethod(patchType, nameof(AddHumanLikeOrders_RestrictEquipmentPatch)), null, null,
            //     "FloatMenuMakerMap_AddHumanlikeOrders");

            PatchMethod(harmonyInstance,
                AccessTools.Method(typeof(CompAbilityItem), "PostDrawExtraSelectionOverlays"),
                new HarmonyMethod(patchType, nameof(CompAbilityItem_Overlay_Prefix)), null, null, null,
                "CompAbilityItem_PostDrawExtraSelectionOverlays");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(Verb), "CanHitCellFromCellIgnoringRange"),
                new HarmonyMethod(patchType, nameof(RimmuNation_CHCFCIR_Patch)), null, null, null,
                "Verb_CanHitCellFromCellIgnoringRange");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(WealthWatcher), "ForceRecount"), null,
                new HarmonyMethod(patchType, nameof(WealthWatcher_ClassAdjustment_Postfix)), null, null,
                "WealthWatcher_ForceRecount");

            PatchMethod(harmonyInstance,
                AccessTools.Method(typeof(Pawn_EquipmentTracker), "TryDropEquipment"), null,
                new HarmonyMethod(patchType, nameof(PawnEquipment_Drop_Postfix)), null, null,
                "Pawn_EquipmentTracker_TryDropEquipment");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(Pawn_EquipmentTracker), "AddEquipment"),
                null,
                new HarmonyMethod(patchType, nameof(PawnEquipment_Add_Postfix)), null, null,
                "Pawn_EquipmentTracker_AddEquipment");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(Caravan), "get_NightResting", null, null),
                new HarmonyMethod(typeof(TorannMagicMod), "Get_NightResting_Undead", null), null, null, null,
                "Caravan_get_NightResting");

            PatchMethod(harmonyInstance,
                AccessTools.Method(typeof(StaggerHandler), "get_Staggered", null, null),
                null, new HarmonyMethod(typeof(TorannMagicMod), "Get_Staggered", null), null, null,
                "StaggerHandler_get_Staggered");

            PatchMethod(harmonyInstance,
                AccessTools.Method(typeof(Verb_LaunchProjectile), "get_Projectile", null, null),
                new HarmonyMethod(typeof(TorannMagicMod), "Get_Projectile_ES", null), null, null, null,
                "Verb_LaunchProjectile_get_Projectile");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(WindManager), "get_WindSpeed", null, null),
                new HarmonyMethod(typeof(TorannMagicMod), "Get_WindSpeed", null), null, null, null,
                "WindManager_get_WindSpeed");

            PatchMethod(harmonyInstance,
                AccessTools.Method(typeof(Pawn), "get_IsFreeNonSlaveColonist", null, null),
                null, new HarmonyMethod(typeof(TorannMagicMod), "Get_IsFreeNonSlaveColonist_Golem", null),
                null, null, "Pawn_get_IsFreeNonSlaveColonist");

            PatchMethod(harmonyInstance,
                AccessTools.Method(typeof(MainTabWindow_Animals), "get_Pawns", null, null),
                null, new HarmonyMethod(typeof(TorannMagicMod), "Get_GolemsAsAnimals", null), null, null,
                "MainTabWindow_Animals_get_Pawns");

            PatchMethod(harmonyInstance,
                AccessTools.Method(typeof(RecipeDef), "get_AvailableNow", null, null),
                null, new HarmonyMethod(typeof(TorannMagicMod), "Get_GolemsRecipeAvailable", null), null,
                null, "RecipeDef_get_AvailableNow");

            PatchMethod(harmonyInstance,
                AccessTools.Method(typeof(Pawn), "get_ShouldAvoidFences", null, null),
                new HarmonyMethod(typeof(TorannMagicMod), "Get_GolemShouldAvoidFences"), null, null, null,
                "Pawn_get_ShouldAvoidFences");

            PatchMethod(harmonyInstance,
                AccessTools.Method(typeof(PawnRenderer), "get_CurRotDrawMode", null, null),
                null, new HarmonyMethod(typeof(TorannMagicMod), "Get_RotBodyForUndead"), null, null,
                "PawnRenderer_get_CurRotDrawMode");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(GenDraw), "DrawRadiusRing", new Type[]
                {
                    typeof(IntVec3),
                    typeof(float),
                    typeof(Color),
                    typeof(Func<IntVec3, bool>)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "DrawRadiusRing_Patch"), null, null, null,
                "GenDraw_DrawRadiusRing");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(Pawn_PathFollower), "CostToMoveIntoCell",
                    new Type[]
                    {
                        typeof(Pawn),
                        typeof(IntVec3)
                    }, null),
                new HarmonyMethod(typeof(TorannMagicMod), "Pawn_PathFollower_Pathfinder_Prefix", null), null,
                null, null, "Pawn_PathFollower_CostToMoveIntoCell");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(StaggerHandler), "StaggerFor", new Type[]
                {
                    typeof(int),
                    typeof(float)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "StaggerFor_Patch", null), null, null,
                null,
                "StaggerHandler_StaggerFor");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(MemoryThoughtHandler), "TryGainMemory",
                    new Type[]
                    {
                        typeof(ThoughtDef),
                        typeof(Pawn),
                        typeof(Precept)
                    }, null),
                new HarmonyMethod(typeof(TorannMagicMod), "MemoryThoughtHandler_PreventDisturbedRest_Prefix",
                    null), null, null, null, "MemoryThoughtHandler_TryGainMemory");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(PawnRenderer), "RenderPawnAt", new Type[]
                {
                    typeof(Vector3),
                    typeof(Rot4?),
                    typeof(bool)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "PawnRenderer_Blur_Prefix", null), null,
                null,
                null, "PawnRenderer_RenderPawnAt");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(PawnDiedOrDownedThoughtsUtility),
                    "AppendThoughts_Relations", new Type[]
                    {
                        typeof(Pawn),
                        typeof(DamageInfo?),
                        typeof(PawnDiedOrDownedThoughtsKind),
                        typeof(List<IndividualThoughtToAdd>),
                        typeof(List<ThoughtToAddToAll>)
                    }, null),
                new HarmonyMethod(typeof(TorannMagicMod), "AppendThoughts_Relations_PrefixPatch", null), null,
                null, null, "PawnDiedOrDownedThoughtsUtility_AppendThoughts_Relations");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(PawnDiedOrDownedThoughtsUtility),
                    "AppendThoughts_ForHumanlike", new Type[]
                    {
                        typeof(Pawn),
                        typeof(DamageInfo?),
                        typeof(PawnDiedOrDownedThoughtsKind),
                        typeof(List<IndividualThoughtToAdd>),
                        typeof(List<ThoughtToAddToAll>)
                    }, null),
                new HarmonyMethod(typeof(TorannMagicMod), "AppendThoughts_ForHumanlike_PrefixPatch", null),
                null,
                null, null, "PawnDiedOrDownedThoughtsUtility_AppendThoughts_ForHumanlike");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(PawnDiedOrDownedThoughtsUtility),
                    "TryGiveThoughts", new Type[]
                    {
                        typeof(Pawn),
                        typeof(DamageInfo?),
                        typeof(PawnDiedOrDownedThoughtsKind)
                    }, null), new HarmonyMethod(typeof(TorannMagicMod), "TryGiveThoughts_PrefixPatch", null),
                null, null, null, "PawnDiedOrDownedThoughtsUtility_TryGiveThoughts");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(Targeter), "TargeterOnGUI", new Type[]
                {
                }, null), null, new HarmonyMethod(typeof(TorannMagicMod), "Targeter_Casting_Postfix", null),
                null,
                null, "Targeter_TargeterOnGUI");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(QuestPart_LendColonistsToFaction),
                    "Enable", new Type[]
                    {
                        typeof(SignalArgs)
                    }, null), null,
                new HarmonyMethod(typeof(TorannMagicMod), "QuestPart_LendColonists_Enable_NoUndead"), null,
                null,
                "QuestPart_LendColonistsToFaction_Enable");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(HealthUtility), "AdjustSeverity",
                    new Type[]
                    {
                        typeof(Pawn),
                        typeof(HediffDef),
                        typeof(float)
                    }, null),
                new HarmonyMethod(typeof(TorannMagicMod), "HealthUtility_HeatCold_HediffGiverForUndead"),
                null,
                null, null, "HealthUtility_AdjustSeverity");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(ThingFilter), "SetFromPreset", new Type[]
                {
                    typeof(StorageSettingsPreset)
                }, null),
                new HarmonyMethod(typeof(TorannMagicMod), "DefaultStorageSettings_IncludeMagicItems"),
                null, null, null, "ThingFilter_SetFromPreset");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(AreaManager), "AddStartingAreas",
                    new Type[]
                    {
                    }, null), null,
                new HarmonyMethod(typeof(TorannMagicMod), "AreaManager_AddMagicZonesToStartingAreas"), null,
                null,
                "AreaManager_AddStartingAreas");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(Projectile), "Launch", new Type[]
                {
                    typeof(Thing),
                    typeof(Vector3),
                    typeof(LocalTargetInfo),
                    typeof(LocalTargetInfo),
                    typeof(ProjectileHitFlags),
                    typeof(bool),
                    typeof(Thing),
                    typeof(ThingDef)
                }, null), new HarmonyMethod(typeof(TorannMagicMod), "Projectile_Launch_Prefix", null), null,
                null,
                null, "Projectile_Launch");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(Verb), "TryStartCastOn", new Type[]
                {
                    typeof(LocalTargetInfo),
                    typeof(LocalTargetInfo),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool)
                }, null), null, new HarmonyMethod(typeof(TorannMagicMod), "TryStartCastOn_Prefix", null),
                null,
                null, "Verb_TryStartCastOn");

            PatchMethod(harmonyInstance, AccessTools.Method(typeof(VerbProperties), "AdjustedCooldown",
                    new Type[]
                    {
                        typeof(Tool),
                        typeof(Pawn),
                        typeof(Thing)
                    }, null), null,
                new HarmonyMethod(typeof(TorannMagicMod), "GolemVerb_AdjustedCooldown_Postfix", null), null,
                null,
                "VerbProperties_AdjustedCooldown");

            #region Children

            {
                try
                {
                    ((Action)(() =>
                    {
                        if (ModCheck.Validate.ChildrenSchoolLearning.IsInitialized())
                        {
                            harmonyInstance.Patch(
                                AccessTools.Method(typeof(PawnUtility), "TrySpawnHatchedOrBornPawn"), null,
                                new HarmonyMethod(typeof(TorannMagicMod),
                                    "TM_Children_TrySpawnHatchedOrBornPawn_Tweak"));
                        }
                    }))();
                }
                catch (TypeLoadException)
                {
                }
            }

            #endregion Children
        }

        [HarmonyPatch(typeof(BookUtility), "CanReadBook", null)]
        public static class OnlyClasses_CanReadBook
        {
            public static bool Prefix(Book book, Pawn reader, ref bool __result, out string reason)
            {
                if (book.def == TorannMagicDefOf.TM_CombatManual)
                {
                    if (!TM_Calc.IsMightUser(reader))
                    {
                        reason = "BookCantRead".Translate(reader.Named("PAWN"));
                        return false;
                    }
                }

                if (book.def == TorannMagicDefOf.TM_Grimoire)
                {
                    if (!TM_Calc.IsMagicUser(reader))
                    {
                        reason = "BookCantRead".Translate(reader.Named("PAWN"));
                        return false;
                    }

                    Hediff hd =
                        reader.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ArcaneWeakness);
                    if (hd != null && hd.Severity >= 10f)
                    {
                        reason = "TM_TooWeak".Translate(reader.Named("PAWN"));
                        return false;
                    }
                }

                reason = null;
                return true;
            }
        }

        [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[]
        {
            typeof(PawnGenerationRequest)
        })]
        public static class RemoveClassFromEntity
        {
            public static void Postfix(ref Pawn __result)
            {
                if (__result != null)
                {
                    if (__result.IsShambler || __result.IsGhoul)
                    {
                        if (__result.story?.traits == null) return;
                        ModOptions.TM_DebugTools.RemoveClass(__result);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MeditationUtility), "CanMeditateNow", null)]
        public class Meditation_NoUndeadMeditation_Patch
        {
            private static void Postfix(Pawn pawn, ref bool __result)
            {
                if (__result && (TM_Calc.IsUndead(pawn) || TM_Calc.IsSpirit(pawn)) &&
                    !pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_LichHD))
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(MeditationUtility), "PsyfocusGainPerTick", null)]
        public class Undead_NoPsyfocusGain_Patch
        {
            private static void Postfix(Pawn pawn, ref float __result, Thing focus = null)
            {
                if ((TM_Calc.IsUndead(pawn) || TM_Calc.IsSpirit(pawn)) &&
                    !pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_LichHD))
                {
                    __result = 0f;
                }
            }
        }

        [HarmonyPatch(typeof(RimWorld.AbilityUtility), "ValidateMustBeHumanOrWildMan", null)]
        public class Hemogen_NoBloodfeedOnUndead_Patch
        {
            private static void Postfix(Pawn targetPawn, bool showMessage, Ability ability, ref bool __result)
            {
                if (__result && (TM_Calc.IsUndead(targetPawn) || TM_Calc.IsSpirit(targetPawn) ||
                                 TM_Calc.IsRobotPawn(targetPawn) || TM_Calc.IsGolem(targetPawn)))
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(JobGiver_GetHemogen), "CanFeedOnPrisoner", null)]
        public class Hemogen_CannotFeedOnUndead_Patch
        {
            private static void Postfix(Pawn bloodfeeder, Pawn prisoner, ref AcceptanceReport __result)
            {
                if (__result && (TM_Calc.IsUndead(prisoner) || TM_Calc.IsSpirit(prisoner) ||
                                 TM_Calc.IsRobotPawn(prisoner) || TM_Calc.IsGolem(prisoner)))
                {
                    __result = false;
                }
            }
        }

        public static void Get_Undead_CanBleed(Pawn_HealthTracker __instance, Pawn ___pawn, ref bool __result)
        {
            if (TM_Calc.IsUndead(___pawn))
            {
                __result = false;
            }
        }

        public static void Get_RotBodyForUndead(PawnRenderer __instance, Pawn ___pawn,
            ref RotDrawMode __result)
        {
            Pawn pawn = ___pawn;
            if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadStageHD")))
            {
                if (ModOptions.Settings.Instance.changeUndeadPawnAppearance &&
                    pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")))
                {
                    Hediff hediff =
                        pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_UndeadStageHD"));
                    if (hediff.Severity < 1)
                    {
                        __result = RotDrawMode.Rotting;
                    }
                    else
                    {
                        __result = RotDrawMode.Dessicated;
                    }
                }

                if (ModOptions.Settings.Instance.changeUndeadAnimalAppearance &&
                    pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")))
                {
                    __result = RotDrawMode.Rotting;
                }
            }
        }

        public static bool Get_UndeadIsCharging(Pawn p, ref bool __result)
        {
            if (TM_Calc.IsUndeadNotVamp(p))
            {
                try
                {
                    if (p.needs.energy != null)
                    {
                        __result = p.needs.energy.currentCharger != null;
                    }

                    __result = false;
                }
                catch
                {
                    __result = false;
                }

                return false;
            }

            return true;
        }

        public static bool Get_UndeadIsGuilty(Pawn_GuiltTracker __instance, Pawn ___pawn, ref bool __result)
        {
            if (TM_Calc.IsUndeadNotVamp(___pawn))
            {
                __result = false;
                return false;
            }

            return true;
        }

        public static bool Get_UndeadAggroMentalState(Pawn __instance, Pawn_MindState ___mindState,
            ref bool __result)
        {
            if (TM_Calc.IsUndeadNotVamp(__instance))
            {
                __result = false;
                return false;
            }

            return true;
        }

        public static bool Get_UndeadMentalState(Pawn __instance, Pawn_MindState ___mindState,
            ref bool __result)
        {
            if (TM_Calc.IsUndeadNotVamp(__instance))
            {
                __result = false;
                return false;
            }

            return true;
        }

        public static bool Get_GolemShouldAvoidFences(Pawn __instance, ref bool __result)
        {
            if (TM_Calc.IsGolem(__instance) || __instance.def == TorannMagicDefOf.TM_DemonR ||
                __instance.def == TorannMagicDefOf.TM_LesserDemonR)
            {
                __result = false;
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(RimWorld.JobGiver_Work), "PawnCanUseWorkGiver", null)]
        public class Golem_NoDisabledWorkTypes_Patch
        {
            private static bool Prefix(RimWorld.JobGiver_Work __instance, Pawn pawn, WorkGiver giver,
                ref bool __result)
            {
                if (pawn is TMPawnGolem)
                {
                    try
                    {
                        __result = !ThingUtility.DestroyedOrNull(pawn) && pawn.Spawned &&
                                   giver.MissingRequiredCapacity(pawn) == null &&
                                   !giver.ShouldSkip(pawn, false);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Golem caught error in PawnCanUseWorkGiver: Golem " + pawn.def.defName +
                                    " on WorkGiver '" + giver.def.defName +
                                    "', this exception thrown in a try_catch \n" + ex.ToString());
                    }

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(SlaveRebellionUtility), "CanParticipateInSlaveRebellion", null)]
        public class Undead_NoSlaveRebellion_Patch
        {
            private static void Postfix(Pawn pawn, ref bool __result)
            {
                if (__result && TM_Calc.IsUndeadNotVamp(pawn))
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn), "Kill", null)]
        public class KillPossessed_Patch
        {
            private static bool Prefix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit = null)
            {
                if (dinfo.HasValue && dinfo.Value.Instigator != null &&
                    dinfo.Value.Instigator is Pawn pPawn && TM_Calc.IsPossessedByOrIsSpirit(pPawn))
                {
                    Pawn s = dinfo.Value.Instigator as Pawn;
                    if (s.needs != null)
                    {
                        Need_Spirit ns = s.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND) as Need_Spirit;
                        if (ns != null)
                        {
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ghost, __instance.DrawPos,
                                s.Map, .8f, .25f, 0f, .25f, 0, Rand.Range(2f, 3f), 0, 0);
                            ns.GainNeed(Rand.Range(5f, 8f));
                        }
                    }
                }

                if (TM_Calc.IsPossessedBySpirit(__instance))
                {
                    TM_Action.RemovePossession(__instance, __instance.Position, false, true);
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Need_Joy), "GainJoy", null)]
        public class JoyGainsSpirit_Patch
        {
            private static void Postfix(float amount, JoyKindDef joyKind, Pawn ___pawn)
            {
                if (amount > 0 && joyKind.defName == "Meditative")
                {
                    if (TM_Calc.IsPossessedByOrIsSpirit(___pawn))
                    {
                        Need_Spirit ns =
                            ___pawn.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND) as Need_Spirit;
                        if (ns != null)
                        {
                            int verVal = 0;
                            CompAbilityUserMagic comp = ___pawn.GetCompAbilityUserMagic();
                            if (comp != null)
                            {
                                verVal = comp.MagicData
                                    .GetSkill_Versatility(TorannMagicDefOf.TM_SpiritPossession).level;
                            }

                            ns.GainNeed(amount * 10 * (1f + (.15f * verVal)));
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(TraitSet), "GainTrait", null)]
        public class AdvancedClass_TraitGain_Patch
        {
            private static bool Prefix(TraitSet __instance, Trait trait, Pawn ___pawn)
            {
                if (trait.def == TorannMagicDefOf.TM_Possessor && ___pawn.def != TorannMagicDefOf.TM_SpiritTD)
                {
                    return false;
                }

                if (___pawn.def == TorannMagicDefOf.TM_SpiritTD && trait.def != TorannMagicDefOf.TM_Possessor)
                {
                    return false;
                }

                return true;
            }

            private static void Postfix(TraitSet __instance, Trait trait, Pawn ___pawn)
            {
                List<TMDefs.TM_CustomClass> acList = TM_ClassUtility.CustomAdvancedClasses;
                for (int i = 0; i < acList.Count; i++)
                {
                    if (trait.def == acList[i].classTrait)
                    {
                        if (acList[i].isMage)
                        {
                            CompAbilityUserMagic targetComp = ___pawn.GetCompAbilityUserMagic();
                            targetComp.CompTick();
                            targetComp.AddAdvancedClass(TM_ClassUtility.GetCustomClassOfTrait(trait.def));
                        }

                        if (acList[i].isFighter)
                        {
                            CompAbilityUserMight targetComp = ___pawn.GetCompAbilityUserMight();
                            targetComp.CompTick();
                            targetComp.AddAdvancedClass(TM_ClassUtility.GetCustomClassOfTrait(trait.def));
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(TraitSet), "RemoveTrait", null)]
        public class AdvancedClass_TraitLoss_Patch
        {
            private static void Postfix(TraitSet __instance, Trait trait, Pawn ___pawn)
            {
                List<TMDefs.TM_CustomClass> acList = TM_ClassUtility.CustomAdvancedClasses.ToList();
                for (int i = 0; i < acList.Count; i++)
                {
                    if (trait.def == acList[i].classTrait)
                    {
                        if (acList[i].isMage)
                        {
                            CompAbilityUserMagic mComp = ___pawn.GetCompAbilityUserMagic();
                            if (mComp != null)
                            {
                                mComp.RemoveAdvancedClass(TM_ClassUtility.GetCustomClassOfTrait(trait.def));
                            }

                            //clean up magic comp if this is the last remaining item keeping the pawn a magic user
                            if (!mComp.IsMagicUser &&
                                ___pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MagicUserHD))
                            {
                                bool addMagicComp = false;
                                ModOptions.TM_DebugTools.RemoveMagicComp(mComp);
                                try
                                {
                                    ___pawn.AllComps.Remove(mComp);
                                    addMagicComp = true;
                                }
                                catch (NullReferenceException ex)
                                {
                                    Log.Warning("failed to remove magic comp");
                                }

                                ModOptions.TM_DebugTools.RemoveClassHediffs(___pawn);
                                if (addMagicComp)
                                {
                                    mComp = new CompAbilityUserMagic();
                                    mComp.parent = ___pawn;
                                    ___pawn.AllComps.Add(mComp);
                                }
                            }
                        }

                        if (acList[i].isFighter)
                        {
                            CompAbilityUserMight pComp = ___pawn.GetCompAbilityUserMight();
                            if (pComp != null)
                            {
                                pComp.RemoveAdvancedClass(TM_ClassUtility.GetCustomClassOfTrait(trait.def));
                            }

                            if (!pComp.IsMightUser &&
                                ___pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MightUserHD))
                            {
                                bool addMightComp = false;
                                ModOptions.TM_DebugTools.RemoveMightComp(pComp);
                                try
                                {
                                    ___pawn.AllComps.Remove(pComp);
                                    addMightComp = true;
                                }
                                catch (NullReferenceException ex)
                                {
                                    Log.Warning("failed to remove might comp");
                                }

                                ModOptions.TM_DebugTools.RemoveClassHediffs(___pawn);
                                if (addMightComp)
                                {
                                    pComp = new CompAbilityUserMight();
                                    pComp.parent = ___pawn;
                                    ___pawn.AllComps.Add(pComp);
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_IdeoTracker), "IdeoTrackerTick", null)]
        public class NoIdeoForSpirits_Patch
        {
            private static bool Prefix(Pawn_IdeoTracker __instance, Pawn ___pawn)
            {
                if (___pawn?.needs?.mood == null)
                {
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Projectile), "Impact", null)]
        public class Projectile_Impact_NoClamorForMagic_Patch
        {
            private static bool Prefix(Projectile __instance)
            {
                if (__instance.ContentSource != null)
                {
                    if (__instance.ContentSource.PackageId == "kure.arom" ||
                        __instance.ContentSource.PackageId == "torann.arimworldofmagic")
                    {
                        __instance.Destroy();
                        return false;
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ThoughtWorker_Precept_HasAutomatedTurrets), "ResetStaticData", null)]
        public class NoSummonedTurretsThought_Patch
        {
            private static void Postfix(ThoughtWorker_Precept_HasAutomatedTurrets __instance,
                ref List<ThingDef> ___automatedTurretDefs)
            {
                List<ThingDef> tmpList = new List<ThingDef>();
                tmpList.Clear();
                if (___automatedTurretDefs != null && ___automatedTurretDefs.Count > 0)
                {
                    foreach (ThingDef td in ___automatedTurretDefs)
                    {
                        if (td.defName.StartsWith("DefensePylon") || td.defName.StartsWith("TM_TechnoTurret"))
                        {
                            tmpList.Add(td);
                        }
                    }

                    if (tmpList.Count > 0)
                    {
                        foreach (ThingDef td in tmpList)
                        {
                            ___automatedTurretDefs.Remove(td);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ThoughtUtility), "CanGetThought", null)]
        public class ThoughtSuppression_Patch
        {
            private static void Postfix(Pawn pawn, ThoughtDef def, ref bool __result)
            {
                if (__result && pawn != null && pawn.health != null && pawn.health.hediffSet != null &&
                    pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EmotionSuppressionHD))
                {
                    Hediff hd =
                        pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EmotionSuppressionHD);
                    if (hd != null && def.stages != null && def.stages.FirstOrDefault() != null &&
                        def.stages.FirstOrDefault().baseMoodEffect != 0)
                    {
                        if (Rand.Chance(Mathf.Clamp01(hd.Severity - .7f)) &&
                            def.stages.FirstOrDefault().baseMoodEffect > 0)
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
                if (__result && pawn != null && pawn.health != null && pawn.health.hediffSet != null &&
                    pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EmotionSuppressionHD))
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(MentalBreaker), "TestMoodMentalBreak", null)]
        public class MentalBreaker_TestMoodMentalBreak
        {
            static void Postfix(Pawn ___pawn, ref bool __result)
            {
                if (!__result) return;
                if (___pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EmotionSuppressionHD))
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(Need_Mood), "DrawOnGUI", null)]
        public static class Need_Mood_DrawOnGUI
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                ILGenerator generator)
            {
                CodeInstruction[] codes = instructions.ToArray();
                Label afterMoodTicks = generator.DefineLabel();
                for (int i = 0; i < codes.Length; i++)
                {
                    // Unfortunately this is the easiest way to grab after the if statement in Need_Mood.DrawOnGUI.
                    // These opcodes were grabbed directly from dnspy
                    if (codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].opcode == OpCodes.Ldarg_1)
                    {
                        // We need to mark the code statement after the if logic with a label so we can jump to it
                        if (codes[i].labels == null)
                        {
                            codes[i].labels = new List<Label>();
                        }

                        codes[i].labels.Add(afterMoodTicks);
                    }

                    yield return codes[i];

                    // this is the end of the if logic that draws the mood ticks. We shall add a new condition.
                    if (codes[i].opcode == OpCodes.Brfalse_S)
                    {
                        // Load pawn.health.hediffSet onto stack
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return CodeInstruction.LoadField(typeof(Need), "pawn");
                        yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.health));
                        yield return CodeInstruction.LoadField(typeof(Pawn_HealthTracker),
                            nameof(Pawn_HealthTracker.hediffSet));
                        // Load TM_EmotionSuppressionHD HediffDef onto stack
                        yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(
                            typeof(TorannMagicDefOf), nameof(TorannMagicDefOf.TM_EmotionSuppressionHD)));
                        // Load 0 (aka false) onto stack
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                        // pawn.health.hediffSet.HasHediff(...TM_EmotionSuppressionHD, bool(0))
                        yield return CodeInstruction.Call(typeof(HediffSet), nameof(HediffSet.HasHediff),
                            new Type[] { typeof(HediffDef), typeof(bool) });
                        // if we have the hediff, jump to the end of the if statement
                        yield return new CodeInstruction(OpCodes.Brtrue_S, afterMoodTicks);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Plant), "PlantCollected", null)]
        public class ApothecaryHarvest_Patch
        {
            private static void Postfix(Plant __instance, Pawn by)
            {
                if (by != null && by.health != null && by.health.hediffSet != null)
                {
                    Pawn p = by;
                    CompAbilityUserMight comp = p.GetCompAbilityUserMight();
                    Hediff_ApothecaryHerbs hd =
                        p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ApothecaryHerbsHD) as
                            Hediff_ApothecaryHerbs;
                    if (hd != null)
                    {
                        float multiplier = 1f;
                        if (__instance.Blighted)
                        {
                            multiplier += .5f;
                        }

                        if (__instance.LeaflessNow)
                        {
                            multiplier -= .4f;
                        }

                        if (!__instance.HarvestableNow)
                        {
                            multiplier -= .5f;
                        }

                        if (__instance.def.plant != null && __instance.def.plant.harvestYield > 0)
                        {
                            multiplier += 1f;
                        }

                        if (comp != null && comp.MightData != null)
                        {
                            MightPowerSkill mps =
                                comp.MightData.GetSkill_Versatility(TorannMagicDefOf.TM_Herbalist);
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
            private static bool Prefix(MusicManagerPlay __instance, SongDef song,
                Queue<SongDef> ___recentSongs, ref bool __result)
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

                if (song.minRoyalTitle != null &&
                    !PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists.Any(delegate(Pawn p)
                    {
                        if (p.royalty != null && p.royalty.AllTitlesForReading.Any() &&
                            p.royalty.MostSeniorTitle.def.seniority >= song.minRoyalTitle.seniority)
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
                if (p != null && ModsConfig.IdeologyActive && TM_Calc.IsUndeadNotVamp(p))
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
                if (!__result && ability != null && ability.Ability != null &&
                    !ability.Ability.Def.MainVerb.isViolent)
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
                CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();
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
                if (Find.TickManager.TicksGame < (___lastRestTick + 2) && ___pawn != null &&
                    ___pawn.health != null && ___pawn.health.hediffSet != null)
                {
                    Hediff hd =
                        ___pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ArcaneWeakness);
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
                    CompAbilityUserMagic comp = ___currentPlayer.GetCompAbilityUserMagic();
                    if (comp != null && comp.MagicData != null && comp.IsMagicUser)
                    {
                        if (comp.MagicData.MagicPowersB.FirstOrDefault((MagicPower x) =>
                                x.abilityDef == TorannMagicDefOf.TM_Entertain).learned)
                        {
                            foreach (Pawn p in __instance.Map.mapPawns.AllPawnsSpawned)
                            {
                                if (p.RaceProps.Humanlike &&
                                    Building_MusicalInstrument.IsAffectedByInstrument(__instance.def,
                                        __instance.Position, p.Position, __instance.Map))
                                {
                                    CompAbilityUserMagic compListener = p.GetCompAbilityUserMagic();
                                    if (compListener != null && compListener.IsMagicUser &&
                                        compListener.Mana != null)
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

        [HarmonyPriority(2000)] //Go first to make sure the right verb is copied
        private static void TryStartCastOn_Prefix(Verb __instance)
        {
            transferVerb = __instance;
        }

        public static Verb transferVerb = null;

        public static bool TM_DualWield_NotForCasting(Verb __instance, LocalTargetInfo castTarg)
        {
            if (transferVerb != null && (transferVerb.GetType().ToString().StartsWith("TorannMagic") ||
                                         transferVerb.GetType().ToString().StartsWith("AbilityUser")))
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
                    if (p.health != null && p.health.hediffSet != null &&
                        (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffEnchantment_fireImmunity) ||
                         p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HeatShieldHD)))
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
                        if (p.health != null && p.health.hediffSet != null &&
                            p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TauntHD))
                        {
                            HediffComp_Taunt hdc_t = p.health.hediffSet
                                .GetFirstHediffOfDef(TorannMagicDefOf.TM_TauntHD)
                                .TryGetComp<HediffComp_Taunt>();
                            if (hdc_t != null && !hdc_t.tauntTarget.DestroyedOrNull() &&
                                hdc_t.tauntTarget.Spawned && !hdc_t.tauntTarget.Dead &&
                                !hdc_t.tauntTarget.Downed && hdc_t.tauntTarget.Map == p.Map)
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

        public static bool Projectile_Launch_Prefix(Projectile __instance, Thing launcher, Vector3 origin,
            ref LocalTargetInfo usedTarget, ref LocalTargetInfo intendedTarget)
        {
            if (launcher is Pawn)
            {
                Pawn launcherPawn = (Pawn)launcher;
                if (launcherPawn.health != null && launcherPawn.health.hediffSet != null)
                {
                    Hediff hd =
                        launcherPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightBurstHD);
                    if (hd != null && hd.Severity >= .7)
                    {
                        if (launcherPawn.equipment.PrimaryEq != null &&
                            launcherPawn.equipment.Primary.def.IsRangedWeapon)
                        {
                            float maxRange = launcherPawn.equipment.Primary.def.Verbs.FirstOrDefault().range;
                            List<Pawn> doomTargets = new List<Pawn>();
                            List<Pawn> mapPawns = launcherPawn.Map.mapPawns.AllPawnsSpawned.ToList();
                            doomTargets.Clear();
                            for (int i = 0; i < mapPawns.Count; i++)
                            {
                                float distance = (mapPawns[i].Position - launcherPawn.Position)
                                    .LengthHorizontal;
                                if (mapPawns[i].Faction == launcherPawn.Faction &&
                                    mapPawns[i] != launcherPawn && distance < maxRange && distance > 3)
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
                            Vector3 centerVec = TM_Calc.GetVectorBetween(launcherPawn.DrawPos,
                                usedTarget.CenterVector3);
                            List<Pawn> targetList = TM_Calc.FindAllPawnsAround(launcherPawn.Map,
                                centerVec.ToIntVec3(), 6, null, false);
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
                if (p != null && p.health != null && p.health.hediffSet != null)
                {
                    if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffStrongBack))
                    {
                        __result *= 1.5f;
                    }

                    if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SpiritPossessorHD))
                    {
                        __result = 0;
                    }
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
                                IEnumerable<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs
                                    .OfType<Hediff_Injury>()
                                    .Where(injury => injury.CanHealNaturally());
                                foreach (Hediff_Injury injury in injuries)
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

                                    TM_MoteMaker.ThrowGenericMote(mote, rndPos, map, healAmt * 3f,
                                        Rand.Range(.2f, .35f), Rand.Range(0, .25f), Rand.Range(.25f, .75f),
                                        Rand.Range(-250, 250), Rand.Range(.2f, .6f), Rand.Range(-15f, 15f),
                                        Rand.Range(0f, 360f));
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
            private static bool Prefix(Reward_Items __instance, float rewardValue,
                RewardsGeneratorParams parms, out float valueActuallyUsed)
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

        public static void AreaManager_AddMagicZonesToStartingAreas(AreaManager __instance,
            List<Area> ___areas)
        {
            if (ModOptions.Settings.Instance.autoCreateAreas)
            {
                if (TM_Calc.GetSpriteArea(__instance.map) == null)
                {
                    ___areas.Add(new Area_TMSprite(__instance));
                }

                if (TM_Calc.GetTransmutateArea(__instance.map) == null)
                {
                    ___areas.Add(new Area_TMTransmutate(__instance));
                }

                if (TM_Calc.GetSeedOfRegrowthArea(__instance.map) == null)
                {
                    ___areas.Add(new Area_TMRegrowth(__instance));
                }
            }
        }

        public static bool DefaultStorageSettings_IncludeMagicItems(ThingFilter __instance,
            StorageSettingsPreset preset)
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

        public static bool HealthUtility_HeatCold_HediffGiverForUndead(Pawn pawn, ref HediffDef hdDef,
            float sevOffset)
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

        public static void QuestPart_LendColonists_Enable_NoUndead(
            QuestPart_LendColonistsToFaction __instance, ref SignalArgs receivedArgs)
        {
            //MethodBase Complete = AccessTools.Method(typeof(QuestPart_LendColonistsToFaction), "Complete", null, null);

            if (__instance.LentColonistsListForReading != null &&
                __instance.LentColonistsListForReading.Count > 0)
            {
                bool undeadSent = false;
                for (int i = 0; i < __instance.LentColonistsListForReading.Count; i++)
                {
                    Thing lentColonist = __instance.LentColonistsListForReading[i];
                    if (lentColonist is Pawn && ((Pawn)lentColonist).health != null &&
                        ((Pawn)lentColonist).health.hediffSet != null &&
                        (((Pawn)lentColonist).health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD) ||
                         ((Pawn)lentColonist).health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD)))
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
                        GenPlace.TryPlaceThing(t, __instance.shuttle.Position, __instance.shuttle.Map,
                            ThingPlaceMode.Near);
                        if (t is Pawn)
                        {
                            Pawn p = t as Pawn;
                            if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD) ||
                                p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD))
                            {
                                p.Kill(null, null);
                            }
                        }
                    }

                    Messages.Message("TM_LendColonist_UndeadFail".Translate(), MessageTypeDefOf.SilentInput,
                        false);
                    __instance.quest.End(QuestEndOutcome.Fail, true);
                }
            }
        }

        public static void PawnEquipment_Drop_Postfix(Pawn_EquipmentTracker __instance, ThingWithComps eq,
            ref bool __result)
        {
            Pawn p = __instance.pawn;
            TM_Calc.DamageCache.Remove(p);
            TM_Calc.DamageCache_Melee.Remove(p);
            TM_Calc.DamageCache_Ranged.Remove(p);
            CompAbilityUserMight comp = p.GetCompAbilityUserMight();
            if (p != null && comp != null &&
                (p.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) || (comp.customClass != null)) &&
                comp.equipmentContainer != null && __result)
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
                            comp.equipmentContainer.TryDrop(comp.equipmentContainer[i], p.Position, p.Map,
                                ThingPlaceMode.Near, out outThing);
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

                CompAbilityUserMagic mComp = p.GetCompAbilityUserMagic();
                if (mComp != null)
                {
                    mComp.weaponDamage = TM_Calc.GetSkillDamage(p);
                }
            }
        }

        public static void PawnEquipment_Add_Postfix(Pawn_EquipmentTracker __instance, ThingWithComps newEq,
            Pawn ___pawn)
        {
            Pawn p = ___pawn;
            if (!newEq.def.defName.Contains("Spec_Base"))
            {
                CompAbilityUserMight comp = p.GetCompAbilityUserMight();
                if (p != null && comp != null && (p.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) ||
                                                  (comp.customClass != null)))
                {
                    if (comp.equipmentContainer == null)
                    {
                        comp.equipmentContainer = new ThingOwner<ThingWithComps>();
                        comp.equipmentContainer.Clear();
                    }

                    if (newEq == p.equipment.Primary)
                    {
                        //Log.Message("adding primary weapon");
                        if (comp.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) =>
                                x.abilityDef == TorannMagicDefOf.TM_PistolSpec).learned &&
                            TM_Calc.IsUsingPistol(p))
                        {
                            //Log.Message("weapon is pistol specialized: " + newEq.def.defName);                            
                            __instance.TryTransferEquipmentToContainer(newEq, comp.equipmentContainer);
                            TM_Action.DoAction_PistolSpecCopy(p, newEq);
                        }
                        else if (comp.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) =>
                                     x.abilityDef == TorannMagicDefOf.TM_RifleSpec).learned &&
                                 TM_Calc.IsUsingRifle(p))
                        {
                            //Log.Message("weapon is rifle specialized: " + newEq.def.defName);
                            __instance.TryTransferEquipmentToContainer(newEq, comp.equipmentContainer);
                            TM_Action.DoAction_RifleSpecCopy(p, newEq);
                        }
                        else if (comp.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) =>
                                     x.abilityDef == TorannMagicDefOf.TM_ShotgunSpec).learned &&
                                 TM_Calc.IsUsingShotgun(p))
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

                    CompAbilityUserMagic mComp = p.GetCompAbilityUserMagic();
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
                Caravan caravan = Traverse.Create(root: __instance).Field(name: "caravan")
                    .GetValue<Caravan>();
                if (caravan != null)
                {
                    IEnumerable<Pawn> pList = caravan.PlayerPawnsForStoryteller;
                    bool hasWayfarer = false;
                    if (pList != null && pList.Count() > 0)
                    {
                        foreach (Pawn p in pList)
                        {
                            if (p.story != null && p.story.traits != null &&
                                p.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer))
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
            public static void CostToMove_Caravan_Postfix(Caravan_PathFollower __instance, Caravan caravan,
                int start, int end, ref int __result, int? ticksAbs = default(int?))
            {
                if (caravan != null)
                {
                    IEnumerable<Pawn> pList = caravan.PlayerPawnsForStoryteller;
                    bool hasWanderer = false;
                    if (pList != null && pList.Count() > 0)
                    {
                        foreach (Pawn p in pList)
                        {
                            if (p.story != null && p.story.traits != null &&
                                p.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer))
                            {
                                hasWanderer = true;
                                break;
                            }
                        }
                    }

                    if (hasWanderer)
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        float num =
                            WorldPathGrid.CalculatedMovementDifficultyAt(end, false, ticksAbs, stringBuilder);
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
                if (p != null && p.story != null && !p.WorkTagIsDisabled(WorkTags.Violent) &&
                    apparel == TorannMagicDefOf.TM_Artifact_BracersOfThePacifist)
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
                    GameCondition_DarkThunderstorm gcdt =
                        map.GameConditionManager.GetActiveCondition(TorannMagicDefOf.DarkThunderstorm) as
                            GameCondition_DarkThunderstorm;
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
                    GameCondition_DarkThunderstorm gcdt =
                        map.GameConditionManager.GetActiveCondition(TorannMagicDefOf.DarkThunderstorm) as
                            GameCondition_DarkThunderstorm;
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
                    CompAbilityUserMight comp = initiator.GetCompAbilityUserMight();
                    if (__instance.interaction == InteractionDefOf.Chitchat)
                    {
                        if (initiator.story != null && comp != null && initiator.story.traits != null &&
                            (initiator.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) ||
                             TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_FieldTraining, null, comp)))
                        {
                            if (initiator.health != null && initiator.health.hediffSet != null &&
                                initiator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffGearRepair))
                            {
                                if (comp.MightData != null && comp.MightData.MightPowerSkill_FieldTraining
                                        .FirstOrDefault((MightPowerSkill x) =>
                                            x.label == "TM_FieldTraining_ver").level >= 10)
                                {
                                    if (recipient.equipment != null && recipient.equipment.Primary != null)
                                    {
                                        Thing weapon = recipient.equipment.Primary;
                                        if (weapon != null && (weapon.def.IsRangedWeapon ||
                                                               weapon.def.IsMeleeWeapon))
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

                        if (recipient.story != null && recipient.story.traits != null &&
                            (recipient.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) ||
                             TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_FieldTraining, null, comp)))
                        {
                            if (recipient.health != null && recipient.health.hediffSet != null &&
                                recipient.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffGearRepair))
                            {
                                if (comp.MightData != null && comp.MightData.MightPowerSkill_FieldTraining
                                        .FirstOrDefault((MightPowerSkill x) =>
                                            x.label == "TM_FieldTraining_ver").level >= 10)
                                {
                                    if (initiator.equipment != null && initiator.equipment.Primary != null)
                                    {
                                        Thing weapon = initiator.equipment.Primary;
                                        if (weapon != null && (weapon.def.IsRangedWeapon ||
                                                               weapon.def.IsMeleeWeapon))
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
                        if (initiator.story != null && comp != null && initiator.story.traits != null &&
                            (initiator.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) ||
                             TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_FieldTraining, null, comp)))
                        {
                            if (initiator.health != null && initiator.health.hediffSet != null &&
                                initiator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffGearRepair))
                            {
                                if (comp.MightData != null && comp.MightData.MightPowerSkill_FieldTraining
                                        .FirstOrDefault((MightPowerSkill x) =>
                                            x.label == "TM_FieldTraining_ver").level >= 10)
                                {
                                    if (recipient.equipment != null && recipient.equipment.Primary != null)
                                    {
                                        Thing weapon = recipient.equipment.Primary;
                                        if (weapon != null && (weapon.def.IsRangedWeapon ||
                                                               weapon.def.IsMeleeWeapon))
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

                        if (recipient.story != null && comp != null && recipient.story.traits != null &&
                            (recipient.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) ||
                             TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_FieldTraining, null, comp)))
                        {
                            if (recipient.health != null && recipient.health.hediffSet != null &&
                                recipient.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffGearRepair))
                            {
                                if (comp.MightData != null && comp.MightData.MightPowerSkill_FieldTraining
                                        .FirstOrDefault((MightPowerSkill x) =>
                                            x.label == "TM_FieldTraining_ver").level >= 10)
                                {
                                    if (initiator.equipment != null && initiator.equipment.Primary != null)
                                    {
                                        Thing weapon = initiator.equipment.Primary;
                                        if (weapon != null && (weapon.def.IsRangedWeapon ||
                                                               weapon.def.IsMeleeWeapon))
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
                if (__instance.jobs != null && __instance.CurJob == null &&
                    __instance.jobs.jobQueue.Count <= 0 && !__instance.jobs.startingNewJob)
                {
                    List<Map> maps = Find.Maps;
                    for (int i = 0; i < maps.Count; i++)
                    {
                        IntVec3 obj3 = maps[i].pawnDestinationReservationManager
                            .FirstObsoleteReservationFor(__instance);
                        if (obj3.IsValid)
                        {
                            Job job = maps[i].pawnDestinationReservationManager
                                .FirstObsoleteReservationJobFor(__instance);
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
                if (__instance != null && __instance.def != null && parms != null &&
                    __instance.def.defName != "VisitorGroup" && __instance.def.defName != "VisitorGroupMax" &&
                    !__instance.def.defName.Contains("Cult") && parms.quest == null && !parms.forced &&
                    !__instance.def.workerClass.ToString().StartsWith("Rumor_Code"))
                {
                    try
                    {
                        List<Map> allMaps = Find.Maps;
                        if (allMaps != null && allMaps.Count > 0)
                        {
                            for (int i = 0; i < allMaps.Count; i++)
                            {
                                if (parms != null && parms.target != null &&
                                    allMaps[i].Tile == parms.target.Tile)
                                {
                                    List<Pawn> mapPawns = allMaps[i].mapPawns.AllPawnsSpawned.InRandomOrder()
                                        .ToList();
                                    if (mapPawns != null && mapPawns.Count > 0)
                                    {
                                        List<Pawn> predictingPawnsAvailable = new List<Pawn>();
                                        predictingPawnsAvailable.Clear();
                                        for (int j = 0; j < mapPawns.Count; j++)
                                        {
                                            if (mapPawns[j].health != null &&
                                                mapPawns[j].health.hediffSet != null &&
                                                mapPawns[j].health.hediffSet
                                                    .HasHediff(TorannMagicDefOf.TM_PredictionHD, false) &&
                                                mapPawns[j].IsColonist)
                                            {
                                                CompAbilityUserMagic comp =
                                                    mapPawns[j].GetCompAbilityUserMagic();
                                                if (comp != null && comp.MagicData != null)
                                                {
                                                    if (comp.predictionIncidentDef != null)
                                                    {
                                                        //Log.Message("attempt to execute prediction " + comp.predictionIncidentDef.defName);
                                                        if (comp.predictionIncidentDef == __instance.def &&
                                                            parms.GetHashCode() == comp.predictionHash)
                                                        {
                                                            comp.predictionIncidentDef = null;
                                                            if (__instance.def.letterLabel != null)
                                                            {
                                                                parms.customLetterLabel =
                                                                    "Predicted " + __instance.def.letterLabel;
                                                            }
                                                            else
                                                            {
                                                                parms.customLetterLabel =
                                                                    "Predicted " + __instance.def.label;
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
                                            CompAbilityUserMagic comp = predictingPawnsAvailable[j]
                                                .GetCompAbilityUserMagic();
                                            MagicPowerSkill ver =
                                                comp.MagicData.MagicPowerSkill_Prediction.FirstOrDefault((
                                                    MagicPowerSkill x) => x.label == "TM_Prediction_ver");
                                            if (__instance.CanFireNow(parms) &&
                                                !ModOptions.Constants.GetBypassPrediction() &&
                                                Rand.Chance(.25f +
                                                            (.05f * ver
                                                                .level))) //up to 40% chance to predict, per chronomancer
                                            {
                                                if (__instance.def.category != null &&
                                                    (__instance.def.category ==
                                                     IncidentCategoryDefOf.ThreatBig ||
                                                     __instance.def.category ==
                                                     IncidentCategoryDefOf.ThreatSmall ||
                                                     __instance.def.category == IncidentCategoryDefOf
                                                         .DeepDrillInfestation ||
                                                     __instance.def.category ==
                                                     IncidentCategoryDefOf.DiseaseHuman ||
                                                     __instance.def.category == IncidentCategoryDefOf.Misc))
                                                {
                                                    //Log.Message("prediction is " + __instance.def.defName + " and can fire now: " + __instance.CanFireNow(parms, false));
                                                    int ticksTillIncident =
                                                        Mathf.RoundToInt((Rand.Range(1800, 3600) *
                                                            (1 + (.15f *
                                                                  ver
                                                                      .level)))); // from .72 to 1.44 hours, plus bonus (1.05 - 2.1)
                                                    //Log.Message("prediction of " + parms.GetHashCode());                                                                             //Log.Message("pushing " + __instance.def.defName + " to iq for " + ticksTillIncident  + " ticks");
                                                    comp.predictionIncidentDef = __instance.def;
                                                    comp.predictionTick = Find.TickManager.TicksGame +
                                                        ticksTillIncident;
                                                    comp.predictionHash = parms.GetHashCode();
                                                    QueuedIncident iq =
                                                        new QueuedIncident(
                                                            new FiringIncident(__instance.def, null, parms),
                                                            comp.predictionTick);
                                                    Find.Storyteller.incidentQueue.Add(iq);
                                                    string labelText =
                                                        "TM_PredictionLetter".Translate(__instance.def.label);
                                                    string text =
                                                        "TM_PredictionText".Translate(
                                                            predictingPawnsAvailable[j].LabelShort,
                                                            __instance.def.label,
                                                            Mathf.RoundToInt(ticksTillIncident / 2500));
                                                    //Log.Message("attempting to push letter");
                                                    Find.LetterStack.ReceiveLetter(labelText, text,
                                                        LetterDefOf.NeutralEvent);
                                                    int xpNum = Rand.Range(60, 120);
                                                    comp.MagicUserXP += xpNum;
                                                    MoteMaker.ThrowText(comp.Pawn.DrawPos, comp.Pawn.Map,
                                                        "XP +" + xpNum, -1f);
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
            public static bool Prefix(AbilityDecisionConditionalNode_CasterHealth __instance, Pawn caster,
                ref bool __result)
            {
                bool flag = caster.health.summaryHealth.SummaryHealthPercent >= __instance.minHealth &&
                            caster.health.summaryHealth.SummaryHealthPercent <= __instance.maxHealth;
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
                        Hediff hediff =
                            pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ArcaneWeakness);
                        if (hediff.Severity >= 10)
                        {
                            __result = true;
                        }
                    }

                    if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ManaSickness, false))
                    {
                        Hediff hediff =
                            pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ManaSickness);
                        if (hediff.Severity >= 4)
                        {
                            __result = true;
                        }
                    }
                }
            }
        }

        public static void Targeter_Casting_Postfix(Targeter __instance)
        {
            if (__instance.targetingSource != null && __instance.targetingSource.CasterIsPawn)
            {
                Pawn caster = __instance.targetingSource.CasterPawn;
                if (caster != null)
                {
                    IntVec3 targ = UI.MouseMapPosition().ToIntVec3();
                    if (targ != IntVec3.Invalid && __instance.targetingSource.GetVerb != null &&
                        __instance.targetingSource.GetVerb.EquipmentSource == null &&
                        __instance.targetingSource.GetVerb.loadID ==
                        null) // && __instance.targetingSource.GetVerb.EquipmentSource == null)
                    {
                        if ((caster.Position - targ).LengthHorizontal >
                            __instance.targetingSource.GetVerb.verbProps.range)
                        {
                            Texture2D icon = TexCommand.CannotShoot; // TM_RenderQueue.losIcon;
                            GenUI.DrawMouseAttachment(icon);
                        }

                        if (__instance.targetingSource.GetVerb.verbProps.requireLineOfSight &&
                            !__instance.targetingSource.GetVerb.TryFindShootLineFromTo(caster.Position, targ,
                                out ShootLine resultingLine))
                        {
                            Texture2D icon = TM_RenderQueue.losIcon;
                            GenUI.DrawMouseAttachment(icon);
                        }

                        if (__instance.targetingSource.GetVerb.GetType() == typeof(Verb_LightSkip) &&
                            targ.InBoundsWithNullCheck(caster.Map) && targ.Roofed(caster.Map))
                        {
                            Texture2D icon = TexCommand.CannotShoot;
                            GenUI.DrawMouseAttachment(icon);
                        }
                    }
                }
            }
        }

        public static void WealthWatcher_ClassAdjustment_Postfix(WealthWatcher __instance,
            bool allowDuringInit = false)
        {
            float wealthPawns =
                Traverse.Create(root: __instance).Field(name: "wealthPawns").GetValue<float>();
            Map map = Traverse.Create(root: __instance).Field(name: "map").GetValue<Map>();
            if (wealthPawns != 0 && map != null)
            {
                foreach (Pawn item in map.mapPawns.PawnsInFaction(Faction.OfPlayer))
                {
                    CompAbilityUserMagic compMagic = item.GetCompAbilityUserMagic();
                    CompAbilityUserMight compMight = item.GetCompAbilityUserMight();
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

        //added draft gizmo to polymorph comp
        [HarmonyPatch(typeof(Pawn), "get_IsColonist", null)]
        public class IsColonist_Patch
        {
            public static void Postfix(Pawn __instance, ref bool __result)
            {
                if (__result || __instance.Faction == null ||
                    __instance.Faction != Faction.OfPlayerSilentFail) return;
                // TryGetComp but faster by avoiding generic isInst
                CompPolymorph cp = null;
                for (int i = 0; i < __instance.AllComps.Count; i++)
                {
                    if (__instance.AllComps[i] is CompPolymorph)
                    {
                        cp = __instance.AllComps[i] as CompPolymorph;
                        break;
                    }
                }

                if (cp?.Original != null && cp.Original.RaceProps.Humanlike)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddJobGiverWorkOrders", null)]
        public class SkipPolymorph_UndraftedOrders_Patch
        {
            public static bool Prefix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts, bool drafted)
            {
                if ((pawn.GetComp<CompPolymorph>() != null &&
                     pawn.GetComp<CompPolymorph>().Original != null) ||
                    pawn.def == TorannMagicDefOf.TM_SpiritTD)
                {
                    return false;
                }

                return true;
            }
        }

        [HarmonyPriority(2000)]
        public static bool RimmuNation_CHCFCIR_Patch(Verb __instance, IntVec3 sourceSq, IntVec3 targetLoc,
            bool includeCorners, ref bool __result)
        {
            if (__instance != null && (__instance.verbProps.verbClass.ToString().Contains("AbilityUser") ||
                                       __instance.verbProps.verbClass.ToString().Contains("TorannMagic")))
            {
                __result = true;
                VerbProperties verbProps = Traverse.Create(root: __instance).Field(name: "verbProps")
                    .GetValue<VerbProperties>();
                Thing caster = Traverse.Create(root: __instance).Field(name: "caster").GetValue<Thing>();
                if (verbProps.mustCastOnOpenGround && (!targetLoc.Standable(caster.Map) ||
                                                       caster.Map.thingGrid.CellContains(targetLoc,
                                                           ThingCategory.Pawn)))
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
                    else if (!GenSight.LineOfSightToEdges(sourceSq, targetLoc, caster.Map,
                                 skipFirstCell: true))
                    {
                        __result = false;
                    }
                }

                return false;
            }

            return true;
        }

        public static bool Pawn_PathFollower_Pathfinder_Prefix(Pawn pawn, IntVec3 c, ref float __result)
        {
            if (pawn != null && pawn.health != null && pawn.health.hediffSet != null &&
                pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ArtifactPathfindingHD))
            {
                float x = c.x;
                IntVec3 position = pawn.Position;
                float num;
                if (x != position.x)
                {
                    int z = c.z;
                    IntVec3 position2 = pawn.Position;
                    if (z != position2.z)
                    {
                        num = (int)pawn.TicksPerMoveDiagonal;
                        goto IL_0047;
                    }
                }

                num = (int)pawn.TicksPerMoveCardinal;
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


        public static bool MemoryThoughtHandler_PreventDisturbedRest_Prefix(MemoryThoughtHandler __instance,
            ThoughtDef def, Pawn ___pawn, Pawn otherPawn = null, Precept sourcePrecept = null)
        {
            //Pawn pawn = Traverse.Create(root: __instance).Field(name: "pawn").GetValue<Pawn>();
            if (___pawn != null && ___pawn.health != null && ___pawn.health.hediffSet != null &&
                ___pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_ArtifactRenewalHD"), false))
            {
                return false;
            }

            return true;
        }

        //applied
        [HarmonyPatch(typeof(PawnRenderNodeWorker), "AltitudeFor", null),
         HarmonyPriority(10)] //go last to ensure cloaks draw over everything else
        public class DrawMesh_Cloaks_Patch2
        {
            public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref float __result)
            {
                if (node?.apparel != null && ModOptions.Settings.Instance.offSetClothing)
                {
                    //Log.Message("pawn " + parms.pawn.LabelShort + " for apparel " + node.apparel.def.defName + " the layer is " + ((node.Props.drawData?.LayerForRot(parms.facing, node.Props.baseLayer) ?? node.Props.baseLayer) + node.debugLayerOffset).ToString() + " with altitude of " + __result);

                    if (__result >= ModOptions.Settings.Instance.offsetApplyAtValue)
                    {
                        __result = __result + ModOptions.Settings.Instance.offsetMultiLayerClothingAmount;
                    }

                    if (ModOptions.Constants.GetCloaks().Contains(node.PrimaryGraphic.MatSingle.mainTexture))
                    {
                        //Log.Message("found cloak " + node.Graphic.MatSingle.mainTexture + " north ?:" + (parms.pawn.Rotation == Rot4.North) + " debug offset set to: " + node.debugLayerOffset);
                        //__result += ModOptions.Constants.GetCloaksNorth().Contains(node.Graphic.MatNorth.mainTexture) ? ModOptions.Settings.Instance.cloakDepthNorth : ModOptions.Settings.Instance.cloakDepth;
                        __result += (parms.pawn.Rotation == Rot4.North)
                            ? ModOptions.Settings.Instance.cloakDepthNorth
                            : ModOptions.Settings.Instance.cloakDepth;
                    }
                }
            }
        }

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
                    Hediff hediff =
                        undeadPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_UndeadStageHD"),
                            false);
                    if (hediff != null)
                    {
                        compRottable.RotProgress = hediff.Severity;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PawnUtility), "ShouldSendNotificationAbout", null)]
        public class NoNotificationForSummons
        {
            public static void Postfix(Pawn p, ref bool __result)
            {
                if (p is TMPawnSummoned || TM_Calc.IsUndead(p))
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_JobTracker), "JobTrackerTick", null)]
        public class Demon_NoJobWhileInFlight
        {
            public static bool Prefix(Pawn ___pawn)
            {
                if (___pawn.def == TorannMagicDefOf.TM_LesserDemonR ||
                    ___pawn.def == TorannMagicDefOf.TM_DemonR || ___pawn.def == TorannMagicDefOf.TM_Poppi)
                {
                    if (___pawn.Map == null || !___pawn.Spawned)
                    {
                        return false;
                    }
                }

                return true;
            }
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
                    __instance.parent.Map.overlayDrawer.DrawOverlay(__instance.parent,
                        OverlayTypes.OutOfFuel);
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
                    __instance.parent.Map.overlayDrawer.DrawOverlay(__instance.parent,
                        OverlayTypes.OutOfFuel);
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

        public static bool AutoUndrafter_Undead_Prefix(AutoUndrafter __instance, Pawn ___pawn,
            ref bool __result)
        {
            //Pawn pawn = ___pawn; // Traverse.Create(root: __instance).Field(name: "pawn").GetValue<Pawn>();
            if (TM_Calc.IsUndead(___pawn))
            {
                __result = false;
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(PawnRenderer), "GetBodyPos")]
        public static class GetBodyPosBlurPatch
        {
            public static void Postfix(PawnRenderer __instance, ref Vector3 __result, Vector3 drawLoc,
                ref bool showBody, Pawn ___pawn)
            {
                Pawn
                    pawn = ___pawn; // Traverse.Create(root: __instance).Field(name: "pawn").GetValue<Pawn>();
                if (!pawn.DestroyedOrNull() && !pawn.Dead && !pawn.Downed)
                {
                    if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BlurHD))
                    {
                        int blurTick = 0;
                        try
                        {
                            blurTick = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_BlurHD)
                                .TryGetComp<HediffComp_Blur>().blurTick;
                        }
                        catch (NullReferenceException ex)
                        {
                            return;
                        }

                        if (blurTick > Find.TickManager.TicksGame - 10)
                        {
                            float blurMagnitude = (10 / (Find.TickManager.TicksGame - blurTick + 1)) + 5f;
                            Vector3 blurLoc = __result;
                            blurLoc.x += Rand.Range(-.03f, .03f) * blurMagnitude;
                            //blurLoc.z += Rand.Range(-.01f, .01f) * blurMagnitude;
                            __result = blurLoc;
                        }
                    }

                    if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DiscordHD))
                    {
                        Hediff hd = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_DiscordHD);
                        if (hd.Severity > 6f)
                        {
                            float blurMagnitude = (hd.Severity - 5f) * .03f;
                            Vector3 blurLoc = __result;
                            blurLoc.x += Rand.Range(-blurMagnitude, blurMagnitude);
                            __result = blurLoc;
                        }
                    }

                    if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PredictionHD))
                    {
                        int blurTick = 0;
                        try
                        {
                            blurTick = pawn.health.hediffSet
                                .GetFirstHediffOfDef(TorannMagicDefOf.TM_PredictionHD)
                                .TryGetComp<HediffComp_Prediction>().blurTick;
                        }
                        catch (NullReferenceException ex)
                        {
                            return;
                        }

                        if (blurTick > Find.TickManager.TicksGame - 10)
                        {
                            float blurMagnitude = (10 / (Find.TickManager.TicksGame - blurTick + 1)) + 5f;
                            Vector3 blurLoc = __result;
                            blurLoc.x += Rand.Range(-.03f, .03f) * blurMagnitude;
                            //blurLoc.z += Rand.Range(-.01f, .01f) * blurMagnitude;
                            __result = blurLoc;
                        }
                    }
                }
            }
        }


        public static bool PawnRenderer_Blur_Prefix(PawnRenderer __instance, Vector3 drawLoc,
            Rot4? rotOverride, Pawn ___pawn, bool neverAimWeapon = false)
        {
            Pawn pawn = ___pawn; // Traverse.Create(root: __instance).Field(name: "pawn").GetValue<Pawn>();
            if (!pawn.DestroyedOrNull() && !pawn.Dead && !pawn.Downed)
            {
                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_InvisibilityHD))
                {
                    return false;
                }
            }

            return true;
        }

        public static void TurretGunTick_Overdrive_Postfix(Building_TurretGun __instance,
            ref int ___burstCooldownTicksLeft, ref int ___burstWarmupTicksLeft)
        {
            Thing overdriveThing = __instance;
            if (!overdriveThing.DestroyedOrNull() && overdriveThing.Map != null)
            {
                List<Pawn> mapPawns = ModOptions.Constants.GetOverdrivePawnList();
                if (mapPawns != null && mapPawns.Count > 0)
                {
                    for (int i = 0; i < mapPawns.Count; i++)
                    {
                        Pawn pawn = mapPawns[i];
                        if (!pawn.DestroyedOrNull() && pawn.RaceProps.Humanlike && pawn.story != null)
                        {
                            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                            if (comp.IsMagicUser && comp.overdriveBuilding != null)
                            {
                                if (overdriveThing == comp.overdriveBuilding)
                                {
                                    if (___burstCooldownTicksLeft >= 5)
                                    {
                                        //Traverse.Create(root: __instance).Field(name: "burstCooldownTicksLeft").SetValue(burstCooldownTicksLeft -= 1 + Rand.Range(0, comp.MagicData.MagicPowerSkill_Overdrive.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Overdrive_pwr").level));
                                        ___burstCooldownTicksLeft -= 1 + Rand.Range(0,
                                            comp.MagicData.MagicPowerSkill_Overdrive
                                                .FirstOrDefault((MagicPowerSkill x) =>
                                                    x.label == "TM_Overdrive_pwr").level);
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
                        CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
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
                if (tempList[i].health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")) ||
                    tempList[i].health.hediffSet.HasHediff(HediffDef.Named("TM_LichHD")) || tempList[i].health
                        .hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")))
                {
                    removalList.Add(tempList[i]);
                }

                TMPawnGolem pg = tempList[i] as TMPawnGolem;
                if (pg != null)
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

        public static bool TryGiveThoughts_PrefixPatch(ref Pawn victim,
            PawnDiedOrDownedThoughtsKind thoughtsKind)
        {
            if (victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD, false) ||
                victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD) ||
                victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_I) ||
                victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_II) ||
                victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_III))
            {
                return false;
            }

            return true;
        }

        public static bool AppendThoughts_ForHumanlike_PrefixPatch(ref Pawn victim,
            PawnDiedOrDownedThoughtsKind thoughtsKind)
        {
            if (victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD, false) ||
                victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD) ||
                victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_I) ||
                victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_II) ||
                victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_III))
            {
                return false;
            }

            return true;
        }

        public static bool AppendThoughts_Relations_PrefixPatch(ref Pawn victim)
        {
            if (victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD, false) ||
                victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD) ||
                victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_I) ||
                victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_II) ||
                victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_III))
            {
                return false;
            }

            return true;
        }

        public static void TM_PrisonLabor_JobDriver_Mine_Tweak(JobDriver __instance)
        {
            if (Rand.Chance(ModOptions.Settings.Instance.magicyteChance))
            {
                Thing thing = null;
                thing = ThingMaker.MakeThing(TorannMagicDefOf.RawMagicyte);
                thing.stackCount = Rand.Range(5, 12);
                if (thing != null)
                {
                    GenPlace.TryPlaceThing(thing, __instance.pawn.Position, __instance.pawn.Map,
                        ThingPlaceMode.Near, null);
                }
            }
        }

        [HarmonyPriority(10)]
        public static void TM_Children_TrySpawnHatchedOrBornPawn_Tweak(ref Pawn pawn, Thing motherOrEgg,
            ref bool __result)
        {
            if (pawn.story != null && pawn.story.traits != null)
            {
                bool hasMagicTrait = false;
                bool hasFighterTrait = false;
                List<Trait> pawnTraits = pawn.story.traits.allTraits;
                for (int i = 0; i < pawnTraits.Count; i++)
                {
                    if (pawnTraits[i].def == TorannMagicDefOf.Arcanist ||
                        pawnTraits[i].def == TorannMagicDefOf.Geomancer ||
                        pawnTraits[i].def == TorannMagicDefOf.Warlock ||
                        pawnTraits[i].def == TorannMagicDefOf.Succubus ||
                        pawnTraits[i].def == TorannMagicDefOf.InnerFire ||
                        pawnTraits[i].def == TorannMagicDefOf.HeartOfFrost ||
                        pawnTraits[i].def == TorannMagicDefOf.StormBorn ||
                        pawnTraits[i].def == TorannMagicDefOf.Technomancer ||
                        pawnTraits[i].def == TorannMagicDefOf.Paladin ||
                        pawnTraits[i].def == TorannMagicDefOf.Summoner ||
                        pawnTraits[i].def == TorannMagicDefOf.Druid ||
                        pawnTraits[i].def == TorannMagicDefOf.Necromancer ||
                        pawnTraits[i].def == TorannMagicDefOf.Lich ||
                        pawnTraits[i].def == TorannMagicDefOf.Priest ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_Bard ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_Gifted ||
                        pawnTraits[i].def == TorannMagicDefOf.Technomancer ||
                        pawnTraits[i].def == TorannMagicDefOf.BloodMage ||
                        pawnTraits[i].def == TorannMagicDefOf.Enchanter ||
                        pawnTraits[i].def == TorannMagicDefOf.Chronomancer ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_Wanderer ||
                        pawnTraits[i].def == TorannMagicDefOf.ChaosMage)
                    {
                        pawnTraits.Remove(pawnTraits[i]);
                        i--;
                        hasMagicTrait = true;
                    }

                    if (pawnTraits[i].def == TorannMagicDefOf.Gladiator ||
                        pawnTraits[i].def == TorannMagicDefOf.Bladedancer ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_Sniper ||
                        pawnTraits[i].def == TorannMagicDefOf.Ranger ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_Psionic ||
                        pawnTraits[i].def == TorannMagicDefOf.Faceless ||
                        pawnTraits[i].def == TorannMagicDefOf.DeathKnight ||
                        pawnTraits[i].def == TorannMagicDefOf.PhysicalProdigy ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_Monk ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_Wayfarer ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_Commander ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_SuperSoldier)
                    {
                        pawnTraits.Remove(pawnTraits[i]);
                        i--;
                        hasFighterTrait = true;
                    }

                    for (int j = 0; j < TM_ClassUtility.CustomClasses.Count; j++)
                    {
                        if (TM_ClassUtility.CustomClasses[j].classTrait == pawnTraits[i].def)
                        {
                            pawnTraits.Remove(pawnTraits[i]);
                            i--;
                            if (TM_ClassUtility.CustomClasses[j].isFighter)
                            {
                                hasFighterTrait = true;
                            }

                            if (TM_ClassUtility.CustomClasses[j].isMage)
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

        public static void Get_Staggered(StaggerHandler __instance, ref bool __result)
        {
            if (!__result) return;
            Pawn p = __instance.parent;
            if (p.def == TorannMagicDefOf.TM_DemonR
                || p.def == TorannMagicDefOf.TM_HollowGolem
                || !p.health.hediffSet.hediffs.Any(hd =>
                    hd.def == TorannMagicDefOf.TM_BurningFuryHD
                    || hd.def == TorannMagicDefOf.TM_MoveOutHD
                    || hd.def == TorannMagicDefOf.TM_EnrageHD
                ))
            {
                __result = false;
            }
        }

        public static bool StaggerFor_Patch(StaggerHandler __instance, int ticks)
        {
            Pawn p = __instance.parent;
            return p.def != TorannMagicDefOf.TM_DemonR
                   && p.def != TorannMagicDefOf.TM_HollowGolem
                   && !p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BurningFuryHD);
        }

        public static bool Get_Projectile_ES(Verb_LaunchProjectile __instance, ref ThingDef __result)
        {
            if (__instance.caster != null && __instance.caster is Pawn && __instance.Bursting)
            {
                Pawn pawn = __instance.caster as Pawn;
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                if (comp != null && pawn.RaceProps.Humanlike && pawn.GetPosture() == PawnPosture.Standing &&
                    comp.HasTechnoWeapon && (pawn.story != null && pawn.story.traits != null &&
                                             ((pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer) ||
                                               pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) ||
                                               TM_ClassUtility.ClassHasAbility(
                                                   TorannMagicDefOf.TM_TechnoWeapon, comp, null)))) &&
                    comp.useElementalShotToggle && pawn.equipment.Primary.def.IsRangedWeapon &&
                    pawn.equipment.Primary.def.techLevel >= TechLevel.Industrial)
                {
                    int verVal = comp.MagicData.MagicPowerSkill_TechnoWeapon
                        .FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoWeapon_ver").level;
                    if (Rand.Chance(.2f + .01f * verVal) && comp.Mana.CurLevel >= .02f)
                    {
                        ThingDef projectile = null;
                        float rnd = Rand.Range(0f, 1f);
                        if (rnd <= .33f) //fire
                        {
                            projectile = ThingDef.Named("Bullet_ES_Fire");
                            projectile.projectile.explosionRadius =
                                __instance.verbProps.defaultProjectile.projectile.explosionRadius +
                                (1 + .05f * verVal);
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

        private static readonly HashSet<HediffDef> UndeadHediffDefs = new HashSet<HediffDef>()
        {
            TorannMagicDefOf.TM_UndeadHD,
            TorannMagicDefOf.TM_UndeadAnimalHD,
            TorannMagicDefOf.TM_LichHD
        };

        public static bool Get_NightResting_Undead(Caravan __instance, ref bool __result)
        {
            List<Pawn> caravan = __instance.PawnsListForReading;
            bool anyLivingColonists = caravan.Any(pawn =>
                pawn.IsColonist
                && !pawn.health.hediffSet.hediffs.Any(hediff => UndeadHediffDefs.Contains(hediff.def))
            );
            __result = anyLivingColonists;
            return anyLivingColonists;
        }

        //never called
        [HarmonyPriority(2000)]
        public static void Pawn_Gizmo_ActionPatch(ref IEnumerable<Gizmo> __result, Pawn __instance)
        {
            if (Find.Selector.NumSelected == 1)
            {
                if (__instance == null || !__instance.RaceProps.Humanlike)
                {
                    return;
                }

                if ((__instance.Faction != null && !__instance.Faction.Equals(Faction.OfPlayer)) ||
                    __instance.story == null || __instance.story.traits == null ||
                    __instance.story.traits.allTraits.Count < 1)
                {
                    return;
                }

                if (__instance.IsColonist)
                {
                    CompAbilityUserMight compMight = __instance.GetCompAbilityUserMight();
                    if (compMight == null && compMight.IsMightUser)
                    {
                        return;
                    }

                    CompAbilityUserMagic compMagic = __instance.GetCompAbilityUserMagic();
                    if (compMagic == null && compMagic.IsMagicUser)
                    {
                        return;
                    }

                    var gizmoList = __result.ToList();
                    if (ModOptions.Settings.Instance.Wanderer &&
                        __instance.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted) &&
                        __instance.ageTracker.AgeBiologicalYears >= 4)
                    {
                        Command_Action itemWanderer = (Command_Action)compMagic.GetGizmoCommands("wanderer");
                        if (itemWanderer != null)
                        {
                            gizmoList.Add(itemWanderer);
                        }
                    }

                    if (ModOptions.Settings.Instance.Wayfarer &&
                        __instance.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy) &&
                        __instance.ageTracker.AgeBiologicalYears >= 4)
                    {
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
            if (__instance == null) return;
            if (__result == null) return;
            if (__instance.Faction != Faction.OfPlayer) return;
            if (!__instance.Spawned) return;

            if (__instance.story != null && __instance.story.traits != null && __instance.RaceProps.Humanlike)
            {
                if (Find.Selector.NumSelected == 1)
                {
                    CompAbilityUserMagic compMagic = __instance.GetCompAbilityUserMagic();
                    CompAbilityUserMight compMight = __instance.GetCompAbilityUserMight();
                    var gizmoList = __result.ToList();
                    bool canBecomeClassless = false;
                    if (ModOptions.Settings.Instance.Wanderer &&
                        __instance.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted) &&
                        __instance.ageTracker.AgeBiologicalYears >= 4)
                    {
                        Command_Action itemWanderer = (Command_Action)compMagic.GetGizmoCommands("wanderer");
                        if (itemWanderer != null)
                        {
                            canBecomeClassless = true;
                            gizmoList.Add(itemWanderer);
                        }
                    }

                    if (ModOptions.Settings.Instance.Wayfarer &&
                        __instance.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy) &&
                        __instance.ageTracker.AgeBiologicalYears >= 4)
                    {
                        Command_Action itemWayfarer = (Command_Action)compMight.GetGizmoCommands("wayfarer");
                        if (itemWayfarer != null)
                        {
                            canBecomeClassless = true;
                            gizmoList.Add(itemWayfarer);
                        }
                    }

                    if (__instance.health.hediffSet.HasHediff(TorannMagicDefOf.TM_OutOfBodyHD))
                    {
                        HediffComp_SymbiosisCaster hdc = __instance.health.hediffSet
                            .GetFirstHediffOfDef(TorannMagicDefOf.TM_OutOfBodyHD)
                            .TryGetComp<HediffComp_SymbiosisCaster>();
                        if (hdc != null && hdc.symbiosisHost != null &&
                            hdc.symbiosisHost.Map == __instance.Map &&
                            (hdc.symbiosisHost.Position - __instance.Position).LengthHorizontal <= 2f)
                        {
                            Command_Action itemSymbiosis =
                                (Command_Action)compMagic.GetGizmoCommands("symbiosis");
                            if (itemSymbiosis != null)
                            {
                                gizmoList.Add(itemSymbiosis);
                            }
                        }
                    }

                    if (ModOptions.Settings.Instance.showGizmo)
                    {
                        Enchantment.CompEnchantedItem itemComp = null;
                        if (__instance.apparel != null && __instance.apparel.WornApparel != null)
                        {
                            for (int i = 0; i < __instance.apparel.WornApparel.Count; i++)
                            {
                                if (__instance.apparel.WornApparel[i].def ==
                                    TorannMagicDefOf.TM_Artifact_NecroticOrb)
                                {
                                    itemComp = __instance.apparel.WornApparel[i]
                                        .TryGetComp<Enchantment.CompEnchantedItem>();
                                }
                            }
                        }

                        if (compMagic == null && compMight == null && itemComp == null)
                        {
                            return;
                        }

                        if (!compMagic.IsMagicUser && !compMight.IsMightUser && itemComp == null &&
                            !canBecomeClassless)
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
                                Order = -101f
                            };

                            gizmoList.Add(energyGizmo);
                        }
                    }

                    if (__instance.story.traits.HasTrait(TorannMagicDefOf.Gladiator) ||
                        TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_Cleave, null, compMight))
                    {
                        Command_Toggle ct = (Command_Toggle)compMight.GetGizmoCommands("cleave");
                        if (ct != null)
                        {
                            gizmoList.Add(ct);
                        }
                    }

                    if (__instance.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) ||
                        TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_CQC, null, compMight))
                    {
                        Command_Toggle ct = (Command_Toggle)compMight.GetGizmoCommands("cqc");
                        if (ct != null)
                        {
                            gizmoList.Add(ct);
                        }
                    }

                    if (__instance.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic) ||
                        TM_ClassUtility.ClassHasHediff(TorannMagicDefOf.TM_PsionicHD, compMagic, compMight))
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

                    if ((__instance.story.traits.HasTrait(TorannMagicDefOf.Technomancer) ||
                         __instance.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) ||
                         TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_TechnoBit, compMagic,
                             compMight)) && compMagic.HasTechnoBit)
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

                    if ((__instance.story.traits.HasTrait(TorannMagicDefOf.Technomancer) ||
                         __instance.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) ||
                         TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_TechnoWeapon, compMagic,
                             compMight)) && compMagic.HasTechnoWeapon)
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
                            gizmoList[i].Order = 500f;
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
            else if (TM_Calc.IsPossessedBySpirit(__instance) && __instance.RaceProps.Animal)
            {
                if (Find.Selector.NumSelected == 1)
                {
                    var gizmoList = __result.ToList();
                    Pawn p = __instance;
                    Command_Action itemUnpossess = new Command_Action
                    {
                        action = new Action(delegate
                        {
                            p.drafter.Drafted = false;
                            TM_Action.RemovePossession(p, p.Position, false);
                        }),
                        Order = 49,
                        defaultLabel = TM_TextPool.TM_RemovePossess,
                        defaultDesc = TM_TextPool.TM_RemovePossessDesc,
                        icon = ContentFinder<Texture2D>.Get("UI/remove_spiritpossession", true),
                    };
                    gizmoList.Add(itemUnpossess);
                    __result = gizmoList;
                }
            }
        }

        [HarmonyPatch(typeof(Pawn), "Kill", null)]
        public static class Undead_Kill_Prefix
        {
            public static bool Prefix(ref Pawn __instance, DamageInfo? dinfo)
            {
                if (__instance != null)
                {
                    if (__instance.health != null && __instance.Faction != null &&
                        (__instance.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD) ||
                         __instance.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD)))
                    {
                        __instance.SetFaction(null, null);
                    }

                    if (__instance.RaceProps != null && __instance.Faction != null &&
                        __instance.RaceProps.DeathActionWorker.GetType() == typeof(DeathWorker_Poppi))
                    {
                        __instance.SetFaction(null, null);
                    }

                    if (__instance.def.thingClass == typeof(TMPawnSummoned) && __instance.Faction != null)
                    {
                        __instance.SetFaction(null, null);
                    }

                    if (TM_Calc.IsMagicUser(__instance) && dinfo.HasValue && dinfo.Value.Instigator != null &&
                        dinfo.Value.Instigator is Pawn)
                    {
                        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(TorannMagicDefOf.TM_KilledMage,
                            dinfo.Value.Instigator.Named(HistoryEventArgsNames.Doer),
                            __instance.Named(HistoryEventArgsNames.Victim)));
                    }

                    if (TM_Calc.IsMightUser(__instance) && dinfo.HasValue && dinfo.Value.Instigator != null &&
                        dinfo.Value.Instigator is Pawn)
                    {
                        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(
                            TorannMagicDefOf.TM_KilledFighter,
                            dinfo.Value.Instigator.Named(HistoryEventArgsNames.Doer),
                            __instance.Named(HistoryEventArgsNames.Victim)));
                    }

                    if (__instance.RaceProps != null && __instance.RaceProps.Humanlike &&
                        __instance.Faction != null && !__instance.Faction.IsPlayer && dinfo.HasValue &&
                        dinfo.Value.Instigator != null && (dinfo.Value.Instigator is Pawn) &&
                        dinfo.Value.Instigator.Faction != null &&
                        dinfo.Value.Instigator.Faction.IsPlayerSafe())
                    {
                        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(
                            TorannMagicDefOf.TM_KilledHumanlike,
                            dinfo.Value.Instigator.Named(HistoryEventArgsNames.Doer),
                            __instance.Named(HistoryEventArgsNames.Victim)));
                    }

                    if (__instance.Map != null)
                    {
                        if (__instance.Map.mapPawns != null)
                        {
                            List<Pawn> mapPawns = __instance.Map.mapPawns.AllPawnsSpawned.ToList();
                            if (mapPawns != null && mapPawns.Count > 0)
                            {
                                foreach (Pawn p in mapPawns)
                                {
                                    if (p.health != null && p.health.hediffSet != null &&
                                        p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DeathFieldHD))
                                    {
                                        p.health.hediffSet
                                            .GetFirstHediffOfDef(TorannMagicDefOf.TM_DeathFieldHD)
                                            .TryGetComp<HediffComp_DeathField>().shouldStrike = true;
                                    }
                                }
                            }
                        }

                        List<Building> tmGolemBuildings = __instance.Map.listerBuildings
                            .AllBuildingsColonistOfDef(TorannMagicDefOf.TM_HollowGolem_Workstation).ToList();
                        if (tmGolemBuildings != null && tmGolemBuildings.Count > 0)
                        {
                            foreach (Building b in tmGolemBuildings)
                            {
                                Building_TMGolemBase gb = b as Building_TMGolemBase;
                                if (gb != null && gb.GolemComp != null)
                                {
                                    foreach (TM_GolemUpgrade gu in gb.GolemComp.Upgrades)
                                    {
                                        if (gu.currentLevel > 0 && gu.enabled && gu.golemUpgradeDef ==
                                            TorannMagicDefOf.TM_Golem_HollowOrbOfExtinguishedFlames)
                                        {
                                            float modifier = 1f;
                                            if (__instance.RaceProps != null)
                                            {
                                                if (__instance.RaceProps.IsMechanoid)
                                                {
                                                    modifier = 1.5f;
                                                }
                                                else if (__instance.RaceProps.Animal)
                                                {
                                                    modifier = .3f;
                                                }
                                            }

                                            Find.ResearchManager.ResearchPerformed(50 * 100 * modifier, null);
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

        [HarmonyPatch(typeof(Pawn), "MakeCorpse", new Type[]
        {
            typeof(Building_Grave),
            typeof(bool),
            typeof(float)
        })]
        public static class DecomposeUndeadOnDeath
        {
            public static void Postfix(Pawn __instance, ref Corpse __result)
            {
                if (__result != null && __result.InnerPawn != null && __result.InnerPawn.health != null &&
                    __result.InnerPawn.health.hediffSet != null &&
                    __result.InnerPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadStageHD))
                {
                    CompRottable cr = __result.TryGetComp<CompRottable>();
                    Hediff hd =
                        __result.InnerPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf
                            .TM_UndeadStageHD);
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
            //public static FieldInfo pawn = typeof(Pawn_HealthTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
            public static MethodBase MakeDowned =
                typeof(Pawn_HealthTracker).GetMethod("MakeDowned",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            public static MethodBase MakeUnDowned =
                typeof(Pawn_HealthTracker).GetMethod("MakeUnDowned",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            public static bool Prefix(Pawn_HealthTracker __instance, Pawn ___pawn, DamageInfo? dinfo,
                Hediff hediff) //CheckForStateChange_
            {
                Traverse traverse = Traverse.Create(__instance);
                Pawn pawn = ___pawn; // (Pawn)CheckForStateChange_Patch.pawn.GetValue(__instance);

                bool result;
                if (pawn != null && dinfo.HasValue && hediff != null)
                {
                    if (pawn.IsColonistPlayerControlled && pawn.genes != null && ModsConfig.BiotechActive &&
                        pawn.genes.HasActiveGene(
                            DefDatabase<GeneDef>.GetNamed("Deathless",
                                true))) //undead bug with deathless without this
                    {
                        return true;
                    }

                    CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                    bool flagChrono = comp != null && comp.IsMagicUser && comp.recallSet;
                    if (flagChrono || (dinfo.Value.Def == TMDamageDefOf.DamageDefOf.TM_DisablingBlow ||
                                       dinfo.Value.Def == TMDamageDefOf.DamageDefOf.TM_Whirlwind ||
                                       dinfo.Value.Def == TMDamageDefOf.DamageDefOf.TM_GrapplingHook ||
                                       dinfo.Value.Def == TMDamageDefOf.DamageDefOf.TM_DisablingShot ||
                                       dinfo.Value.Def == TMDamageDefOf.DamageDefOf.TM_Tranquilizer) ||
                        TM_Calc.IsUndeadNotVamp(pawn) || TM_Calc.IsPolymorphed(pawn))
                    {
                        if (TM_Calc.IsPolymorphed(pawn) && pawn.IsColonist)
                        {
                            //force friendly pawn out of polymorph
                            CompPolymorph poly = pawn.GetComp<CompPolymorph>();
                            poly.Temporary = true;
                            poly.TicksLeft = 0;
                            return false; //take no further action
                        }

                        if (!__instance.Dead)
                        {
                            //bool flag3 = traverse.Method("ShouldBeDead", new object[0]).GetValue<bool>() && CheckForStateChange_Patch.pawn != null;
                            if (__instance.ShouldBeDead())
                            {
                                if (!pawn.Destroyed)
                                {
                                    if (comp != null && comp.IsMagicUser && comp.recallSet)
                                    {
                                        int pwrVal = comp.MagicData.MagicPowerSkill_Recall
                                            .FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Recall_pwr")
                                            .level;
                                        if (pwrVal == 3 || (pwrVal >= 1 && Rand.Chance(.5f)))
                                        {
                                            TM_Action.DoRecall(pawn, comp, true);
                                            result = false;
                                            return false;
                                        }
                                    }

                                    pawn.Kill(dinfo, hediff);
                                }

                                return false;
                            }

                            bool flag5 = !__instance.Downed;
                            if (flag5)
                            {
                                bool flag6 = traverse.Method("ShouldBeDowned")
                                    .GetValue<
                                        bool>(); //, new object[0]).GetValue<bool>() && CheckForStateChange_Patch.pawn != null;
                                if (flag6)
                                {
                                    if (comp != null && comp.IsMagicUser && comp.recallSet)
                                    {
                                        int pwrVal = comp.MagicData.MagicPowerSkill_Recall
                                            .FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Recall_pwr")
                                            .level;
                                        if (pwrVal == 3 || (pwrVal >= 2 && Rand.Chance(.5f)))
                                        {
                                            TM_Action.DoRecall(pawn, comp, false);
                                            result = false;
                                            return false;
                                        }
                                    }

                                    float num = (!pawn.RaceProps.Animal) ? 0f : 0f;
                                    bool flag7 = !__instance.forceDowned && dinfo.HasValue &&
                                                 dinfo.Value.Def.ExternalViolenceFor(pawn) &&
                                                 (pawn.Faction == null || !pawn.Faction.IsPlayer) &&
                                                 !pawn.IsPrisonerOfColony && pawn.RaceProps.IsFlesh &&
                                                 Rand.Value < num;
                                    if (flag7)
                                    {
                                        pawn.Kill(dinfo, null);
                                        result = false;
                                        return result;
                                    }

                                    bool flagUndead = dinfo.HasValue && TM_Calc.IsUndeadNotVamp(pawn) &&
                                                      !pawn.health.hediffSet.HasHediff(TorannMagicDefOf
                                                          .TM_LichHD);
                                    if (flagUndead)
                                    {
                                        if (pawn.Map != null)
                                        {
                                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ghost,
                                                pawn.DrawPos, pawn.Map, .65f, .05f, .05f, .4f, 0,
                                                Rand.Range(3, 4), Rand.Range(-15, 15), 0);
                                        }

                                        pawn.Kill(dinfo, null);
                                        result = false;
                                        return result;
                                    }

                                    __instance.forceDowned = false;
                                    CheckForStateChange_Patch.MakeDowned.Invoke(__instance, new object[]
                                    {
                                        dinfo,
                                        hediff
                                    });
                                    return false;
                                }
                                else
                                {
                                    if (!__instance.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                                    {
                                        if (pawn.carryTracker != null &&
                                            pawn.carryTracker.CarriedThing != null && pawn.jobs != null &&
                                            pawn.CurJob != null)
                                        {
                                            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                                        }

                                        if (pawn.equipment != null && pawn.equipment.Primary != null)
                                        {
                                            bool inContainerEnclosed = pawn.InContainerEnclosed;
                                            if (inContainerEnclosed)
                                            {
                                                pawn.equipment.TryTransferEquipmentToContainer(
                                                    pawn.equipment.Primary, pawn.holdingOwner);
                                            }
                                            else
                                            {
                                                bool spawnedOrAnyParentSpawned =
                                                    pawn.SpawnedOrAnyParentSpawned;
                                                if (spawnedOrAnyParentSpawned)
                                                {
                                                    ThingWithComps thingWithComps;
                                                    pawn.equipment.TryDropEquipment(pawn.equipment.Primary,
                                                        out thingWithComps, pawn.PositionHeld, true);
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
                                bool flag11 =
                                    !traverse.Method("ShouldBeDowned", new object[0]).GetValue<bool>() &&
                                    ___pawn != null;
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

            private static void Postfix(Pawn_HealthTracker __instance, Pawn ___pawn, DamageInfo? dinfo,
                Hediff hediff)
            {
                //Traverse traverse = Traverse.Create(__instance);
                Pawn pawn = ___pawn; // (Pawn)CheckForStateChange_Patch.pawn.GetValue(__instance);

                if (pawn != null && dinfo.HasValue && hediff != null)
                {
                    if (pawn != null && !pawn.IsColonist)
                    {
                        if (pawn.Downed && !pawn.Dead && !pawn.IsPrisoner)
                        {
                            if (pawn is TMPawnGolem)
                            {
                                pawn.Kill(dinfo, hediff);
                            }

                            if (pawn.Map == null)
                            {
                                //Log.Message("Tried to do death retaliation in a null map.");
                            }
                            else
                            {
                                float chc = 1f * ModOptions.Settings.Instance.deathRetaliationChance;
                                if (Rand.Chance(chc))
                                {
                                    CompAbilityUserMagic compMagic = pawn.GetCompAbilityUserMagic();
                                    CompAbilityUserMight compMight = pawn.GetCompAbilityUserMight();
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

        /// <summary>
        ///
        /// Order and location of damage adjustments:
        /// Thing_TakeDamage nests all other calls
        /// 
        /// *********************************************************************
        /// 1. Applies internal def damage multipliers
        /// 2. PreApplyDamage (Thing->ThingWithComps->Pawn->Pawn_HealthTracker)
        ///   a. Anything that modifies dinfo must be set here
        /// 3. DamageWorker.Apply
        ///   a. Some category based multipliers here
        ///   b. Calculates Armor mitigation
        ///   c. Sets Hitpoints / kills / destroys / damage propogation
        ///   d. Applies injuries or effects to parts
        /// 4. PostApplyDamage (Thing->ThingWithComps->Pawn->Pawn_HealthTracker)
        ///   a. Primarily records, notifications and effects
        ///   b. Evaluates pawn "ShouldBeDead"
        ///   c. Applies damageDef additional hediffs
        /// *********************************************************************
        /// 
        /// Pawn.PreApplyDamage.Prefix
        /// o Invulnerabilities
        /// o Damage dealt (flat amount)
        /// o Damage dealt (percent)
        /// o Damage taken (flat amount)
        /// o Damage taken (percent)   
        ///
        /// Pawn.PreApplyDamage 
        /// o Calculates Gene damage factors
        /// o Calculates Apparel damage absorption
        ///
        /// Pawn_HealthTracker.PreApplyDamage.Prefix
        /// o Absorb/deflect damage taken (full) 
        /// 
        /// Pawn_HealthTracker.PreApplyDamage
        /// o Checks for Ignore armor damage types
        /// o Calculates Apparel/Armor mitigation factors and apparel comps
        ///
        /// Pawn_HealthTracker.PreApplyDamage.Postfix
        /// o Special effects, extra damages, and other
        /// 
        /// Pawn.PreApplyDamage.Postfix
        /// 
        /// DamageWorker.Apply (Prefix/Postfix)
        /// 
        /// Pawn.PostApplyDamage.Prefix
        /// 
        /// Pawn.PostApplyDamage
        /// 
        /// Pawn_HealthTracker.PostApplyDamage.Prefix
        /// 
        /// Pawn_HealthTracker.PostApplyDamage
        /// 
        /// Pawn_HealthTracker.PostApplyDamage.Postfix
        /// 
        /// </summary>
        [HarmonyPatch(typeof(Pawn), "PreApplyDamage", null)]
        public class Pawn_PreApplyDamage
        {
            public static bool Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
            {
                Thing instigator = dinfo.Instigator as Thing;
                absorbed = false;
                if (instigator != null && __instance?.health?.hediffSet != null)
                {
                    foreach (Hediff hd in __instance.health.hediffSet.hediffs)
                    {
                        //invulnerability blocks all damage; different tracking mechanism (time vs severity)
                        if (hd.def == TorannMagicDefOf.TM_HediffInvulnerable ||
                            hd.def == TorannMagicDefOf.TM_HediffTimedInvulnerable)
                        {
                            absorbed = true;
                            if (__instance.Map != null)
                                FleckMaker.Static(__instance.Position, __instance.Map,
                                    FleckDefOf.ExplosionFlash, 10);
                            dinfo.SetAmount(0);
                            return false;
                        }

                        //Phantom shift (from cloak) has a 20% chance to mitigate all damage
                        if (hd.def == TorannMagicDefOf.TM_HediffEnchantment_phantomShift && Rand.Chance(.2f))
                        {
                            absorbed = true;
                            if (__instance.Map != null)
                            {
                                FleckMaker.Static(__instance.Position, __instance.Map,
                                    FleckDefOf.ExplosionFlash, 8);
                                FleckMaker.ThrowSmoke(__instance.Position.ToVector3Shifted(), __instance.Map,
                                    1.2f);
                            }

                            dinfo.SetAmount(0);
                            return false;
                        }

                        //prevents psionic from taking psionic injuries
                        if (hd.def == TorannMagicDefOf.TM_PsionicHD)
                        {
                            if (dinfo.Def == TMDamageDefOf.DamageDefOf.TM_PsionicInjury)
                            {
                                absorbed = true;
                                dinfo.SetAmount(0);
                                return false;
                            }
                        }
                    }

                    //pseudo magical ability to deflect attacks without taking damage but may not work well if damage is otherwise magically mitigated
                    //placed here to act before other physical damage mitigation
                    if (__instance.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ReversalHD) &&
                        instigator is Pawn pinstigator)
                    {
                        CompAbilityUserMight comp = __instance.GetCompAbilityUserMight();
                        if (pinstigator.equipment?.PrimaryEq?.PrimaryVerb != null)
                        {
                            absorbed = true;
                            if (__instance.Map != null)
                            {
                                Vector3 drawPos = __instance.DrawPos;
                                drawPos.x += ((pinstigator.DrawPos.x - drawPos.x) / 20f) +
                                             Rand.Range(-.2f, .2f);
                                drawPos.z += ((pinstigator.DrawPos.z - drawPos.z) / 20f) +
                                             Rand.Range(-.2f, .2f);
                                TM_MoteMaker.ThrowSparkFlashMote(drawPos, __instance.Map, 2f);
                                TM_Action.DoReversal(dinfo, __instance);
                            }

                            MightPowerSkill ver =
                                comp.MightData.MightPowerSkill_Reversal.FirstOrDefault((MightPowerSkill x) =>
                                    x.label == "TM_Reversal_ver");
                            if (ver.level > 0)
                            {
                                comp.SiphonReversal(ver.level);
                            }

                            return false;
                        }
                        else if (pinstigator.RaceProps.Animal && dinfo.Amount != 0 &&
                                 (pinstigator.Position - __instance.Position).LengthHorizontal <= 2)
                        {
                            absorbed = true;
                            if (__instance.Map != null)
                            {
                                Vector3 drawPos = __instance.DrawPos;
                                drawPos.x += ((instigator.DrawPos.x - drawPos.x) / 20f) +
                                             Rand.Range(-.2f, .2f);
                                drawPos.z += ((instigator.DrawPos.z - drawPos.z) / 20f) +
                                             Rand.Range(-.2f, .2f);
                                TM_MoteMaker.ThrowSparkFlashMote(drawPos, __instance.Map, 2f);
                            }

                            comp.DoMeleeReversal(dinfo);
                            dinfo.SetAmount(0);
                            MightPowerSkill ver =
                                comp.MightData.MightPowerSkill_Reversal.FirstOrDefault((MightPowerSkill x) =>
                                    x.label == "TM_Reversal_ver");
                            if (ver.level > 0)
                            {
                                comp.SiphonReversal(ver.level);
                            }

                            return false;
                        }

                        Building instigatorBldg = dinfo.Instigator as Building;
                        if (instigatorBldg != null)
                        {
                            if (instigatorBldg.def.Verbs != null)
                            {
                                absorbed = true;
                                if (__instance.Map != null)
                                {
                                    Vector3 drawPos = __instance.DrawPos;
                                    drawPos.x += ((instigatorBldg.DrawPos.x - drawPos.x) / 20f) +
                                                 Rand.Range(-.2f, .2f);
                                    drawPos.z += ((instigatorBldg.DrawPos.z - drawPos.z) / 20f) +
                                                 Rand.Range(-.2f, .2f);
                                    TM_MoteMaker.ThrowSparkFlashMote(drawPos, __instance.Map, 2f);
                                }

                                comp.DoReversal(dinfo);
                                dinfo.SetAmount(0);
                                MightPowerSkill ver =
                                    comp.MightData.MightPowerSkill_Reversal
                                        .FirstOrDefault((MightPowerSkill x) => x.label == "TM_Reversal_ver");
                                if (ver.level > 0)
                                {
                                    comp.SiphonReversal(ver.level);
                                }

                                return false;
                            }
                        }
                    }

                    //checks pawn that dealt damage for damage increases, must apply before damage absorption
                    if (dinfo.Instigator.Map != null && dinfo.Instigator is Pawn)
                    {
                        Pawn p = dinfo.Instigator as Pawn;
                        if (p.health?.hediffSet != null)
                        {
                            //flat amount instigator
                            if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MindOverBodyHD) &&
                                dinfo.Def == DamageDefOf.Blunt && dinfo.Weapon != null &&
                                dinfo.Weapon.defName == "Human")
                            {
                                Hediff hediff =
                                    p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf
                                        .TM_MindOverBodyHD);
                                dinfo.SetAmount(Mathf.RoundToInt(dinfo.Amount + hediff.Severity +
                                                                 Rand.Range(0f, 3f)));
                                dinfo.Def = TMDamageDefOf.DamageDefOf.TM_ChiFist;
                            }

                            CompAbilityUserMight compMight = p.GetCompAbilityUserMight();
                            if (p.IsPsychologicallyInvisible() && compMight != null &&
                                compMight.IsMightUser && compMight.MightData != null)
                            {
                                MightPowerSkill mps =
                                    compMight.MightData.GetSkill_Power(TorannMagicDefOf.TM_ShadowSlayer);
                                if (mps != null)
                                {
                                    int skillLevel = (2 * mps.level);
                                    dinfo.SetAmount(dinfo.Amount + skillLevel);
                                }
                            }

                            //damage multiplier instigator
                            if (p.story != null && p.story.traits != null)
                            {
                                if (p.story.traits.HasTrait(TorannMagicDefOf.TM_GiantsBloodTD) &&
                                    TM_Calc.IsUsingMelee(p))
                                {
                                    float amt = dinfo.Amount;
                                    amt *= 1.25f;
                                    dinfo.SetAmount(amt);
                                }
                            }

                            if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadShroudHD))
                            {
                                Hediff hd =
                                    p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf
                                        .TM_UndeadShroudHD);
                                dinfo.SetAmount(Mathf.RoundToInt(dinfo.Amount * (1f + hd.Severity)));
                            }

                            if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnrageHD) &&
                                TM_Calc.IsUsingMelee(p))
                            {
                                Hediff hediff =
                                    p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnrageHD);
                                dinfo.SetAmount(Mathf.RoundToInt(dinfo.Amount * (1f + hediff.Severity)));
                            }

                            if (p.equipment != null && p.equipment.Primary != null)
                            {
                                if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BowTrainingHD))
                                {
                                    Thing wpn = p.equipment.Primary;
                                    if (TM_Data.BowList().Contains(wpn.def))
                                    {
                                        Hediff hediff =
                                            p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf
                                                .TM_BowTrainingHD);
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
                            }

                            //flat amount reciever
                            //lich form reduces all damage by 4
                            foreach (Hediff hd in __instance.health.hediffSet.hediffs)
                            {
                                if (hd.def == TorannMagicDefOf.TM_LichHD)
                                {
                                    HediffComp_Lich hdc = hd.TryGetComp<HediffComp_Lich>();
                                    if (hdc != null &&
                                        ((hdc.lastDamageTick + 6) <= Find.TickManager.TicksGame))
                                    {
                                        float mitigationAmt = 4f;
                                        float actualDmg = 0;
                                        float dmgAmt = dinfo.Amount;
                                        if (dmgAmt < mitigationAmt)
                                        {
                                            if (__instance.Map != null)
                                                MoteMaker.ThrowText(__instance.DrawPos, __instance.Map,
                                                    "TM_DamageAbsorbedAll".Translate(), -1);
                                            dinfo.SetAmount(0);
                                            absorbed = true;
                                            return false;
                                        }
                                        else
                                        {
                                            if (__instance.Map != null)
                                                MoteMaker.ThrowText(__instance.DrawPos, __instance.Map,
                                                    "TM_DamageAbsorbed".Translate(
                                                        dmgAmt,
                                                        mitigationAmt
                                                    ), -1);
                                            actualDmg = dmgAmt - mitigationAmt;
                                        }

                                        hdc.lastDamageTick = Find.TickManager.TicksGame;
                                        dinfo.SetAmount(actualDmg);
                                        continue;
                                    }
                                }

                                //monk reduces damage by a flat amount depending on hediff severity and clothing weight
                                if (hd.def == TorannMagicDefOf.TM_MindOverBodyHD)
                                {
                                    CompAbilityUserMight comp = __instance.GetCompAbilityUserMight();
                                    MightPowerSkill ver =
                                        comp.MightData.MightPowerSkill_MindOverBody.FirstOrDefault((
                                            MightPowerSkill x) => x.label == "TM_MindOverBody_ver");
                                    int mitigationAmt =
                                        Mathf.Clamp(
                                            (7 + (2 * ver.level) -
                                             Mathf.RoundToInt(comp.totalApparelWeight / 2)), 0, 13);

                                    if (ModOptions.Settings.Instance.AIHardMode && !__instance.IsColonist)
                                    {
                                        mitigationAmt = 10;
                                    }

                                    float actualDmg;
                                    float dmgAmt = dinfo.Amount;
                                    if (dmgAmt < mitigationAmt)
                                    {
                                        Vector3 drawPos = __instance.DrawPos;
                                        if (instigator.Map == __instance.Map)
                                        {
                                            float drawAngle = (instigator.DrawPos - drawPos).AngleFlat();
                                            drawPos.x +=
                                                Mathf.Clamp(
                                                    ((instigator.DrawPos.x - drawPos.x) / 5f) +
                                                    Rand.Range(-.1f, .1f), -.45f, .45f);
                                            drawPos.z +=
                                                Mathf.Clamp(
                                                    ((instigator.DrawPos.z - drawPos.z) / 5f) +
                                                    Rand.Range(-.1f, .1f), -.45f, .45f);
                                            TM_MoteMaker.ThrowSparkFlashMote(drawPos, __instance.Map, 1f);
                                        }

                                        dinfo.SetAmount(0);
                                        absorbed = true;
                                        return false;
                                    }
                                    else
                                    {
                                        actualDmg = dmgAmt - mitigationAmt;
                                    }

                                    dinfo.SetAmount(actualDmg);
                                    continue;
                                }

                                //gladiator reduces all damage by a flat amount based on toughness severity
                                if (hd.def == TorannMagicDefOf.TM_HediffFortitude)
                                {
                                    CompAbilityUserMight comp = __instance.GetCompAbilityUserMight();
                                    MightPowerSkill pwr =
                                        comp.MightData.MightPowerSkill_Fortitude.FirstOrDefault((
                                            MightPowerSkill x) => x.label == "TM_Fortitude_pwr");
                                    MightPowerSkill ver =
                                        comp.MightData.MightPowerSkill_Fortitude.FirstOrDefault((
                                            MightPowerSkill x) => x.label == "TM_Fortitude_ver");
                                    absorbed = true;
                                    float mitigationAmt = 5f + pwr.level;

                                    if (ModOptions.Settings.Instance.AIHardMode && !__instance.IsColonist)
                                    {
                                        mitigationAmt = 8;
                                    }

                                    float actualDmg;
                                    float dmgAmt = dinfo.Amount;
                                    comp.Stamina.GainNeed((.01f * dmgAmt) + (.005f * (float)ver.level));
                                    if (dmgAmt < mitigationAmt)
                                    {
                                        dinfo.SetAmount(0);
                                        absorbed = true;
                                        return false;
                                    }
                                    else
                                    {
                                        actualDmg = dmgAmt - mitigationAmt;
                                    }

                                    dinfo.SetAmount(actualDmg);
                                    continue;
                                }

                                //Shield (paladin shield) absorbs damage and reduces shield strength (severity) but can break and applies remaining damage to pawn
                                if (hd.def == TorannMagicDefOf.TM_HediffShield && hd.Severity > 0)
                                {
                                    float sev = hd.Severity;

                                    float dmgAmt = (float)dinfo.Amount;
                                    float dmgToSev = 0.004f;

                                    sev = sev - (dmgAmt * dmgToSev);
                                    TM_Action.DisplayShieldHit(__instance, dinfo);
                                    hd.Severity = sev;
                                    if (sev > 0)
                                    {
                                        absorbed = true;
                                        return false;
                                    }

                                    dinfo.SetAmount((int)Mathf.RoundToInt(Mathf.Abs(sev / dmgToSev)));
                                    TM_Action.BreakShield(__instance);
                                    continue;
                                }

                                //Scorn (succubus shield) also applies remaining damage to pawn
                                if ((hd.def == TorannMagicDefOf.TM_DemonScornHD ||
                                     hd.def == TorannMagicDefOf.TM_DemonScornHD_I ||
                                     hd.def == TorannMagicDefOf.TM_DemonScornHD_II ||
                                     hd.def == TorannMagicDefOf.TM_DemonScornHD_III) && hd.Severity > 0)
                                {
                                    float sev = hd.Severity;

                                    float dmgAmt = (float)dinfo.Amount;
                                    float dmgToSev = 1f;
                                    if (!__instance.IsColonist && ModOptions.Settings.Instance.AIHardMode)
                                        dmgToSev = 0.75f;

                                    sev = sev - (dmgAmt * dmgToSev);
                                    TM_Action.DisplayShieldHit(__instance, dinfo);
                                    hd.Severity = sev;
                                    if (sev > 0)
                                    {
                                        absorbed = true;
                                        return false;
                                    }

                                    dinfo.SetAmount((int)Mathf.RoundToInt(Mathf.Abs(sev / dmgToSev)));
                                    TM_Action.BreakShield(__instance);
                                    continue;
                                }

                                //mana shield - blocks damage using mana, flat damage reduction
                                if (hd.def == TorannMagicDefOf.TM_ManaShieldHD)
                                {
                                    bool canAbsorb = true;
                                    HediffComp_ManaShield hdc = hd.TryGetComp<HediffComp_ManaShield>();
                                    if (hdc != null)
                                    {
                                        canAbsorb = (hdc.lastHitTick + 2) < Find.TickManager.TicksGame;
                                        hdc.lastHitTick = Find.TickManager.TicksGame;
                                    }

                                    if (canAbsorb && __instance.GetCompAbilityUserMagic()?.Mana != null &&
                                        __instance.GetCompAbilityUserMagic().MagicData != null)
                                    {
                                        CompAbilityUserMagic comp = __instance.GetCompAbilityUserMagic();
                                        float sev = comp.Mana.CurLevel;
                                        if (sev < 0.1f)
                                        {
                                            continue;
                                        }

                                        int actualDmg = 0;
                                        float dmgAmt = (float)dinfo.Amount;
                                        float dmgToSev = 0.02f;
                                        float maxDmg = 11f;

                                        if (comp.MagicData.MagicPowerSkill_Cantrips
                                                .FirstOrDefault((MagicPowerSkill x) =>
                                                    x.label == "TM_Cantrips_ver").level >= 3)
                                        {
                                            dmgToSev = 0.015f;
                                            maxDmg = 14f;
                                            if (comp.MagicData.MagicPowerSkill_Cantrips
                                                    .FirstOrDefault((MagicPowerSkill x) =>
                                                        x.label == "TM_Cantrips_ver").level >= 7)
                                            {
                                                dmgToSev = 0.012f;
                                                maxDmg = 17f;
                                            }
                                        }

                                        TM_Action.DisplayShieldHit(__instance, dinfo);
                                        if (dmgAmt >= maxDmg)
                                        {
                                            dinfo.SetAmount(Mathf.RoundToInt(dmgAmt - maxDmg));
                                            sev = sev - (maxDmg * dmgToSev);
                                        }
                                        else
                                        {
                                            sev = sev - (dmgAmt * dmgToSev);
                                        }

                                        comp.Mana.CurLevel = sev;
                                        if (sev < 0)
                                        {
                                            actualDmg = (int)Mathf.RoundToInt(Mathf.Abs(sev / dmgToSev));
                                            TM_Action.BreakShield(__instance);
                                            hd.Severity = sev;
                                        }

                                        dinfo.SetAmount(actualDmg);
                                    }

                                    continue;
                                }
                            }


                            //mitigation multipliers
                            //Arcane resistance can reduce damage as a multiplier like armor against magic damage types
                            foreach (Hediff hd in __instance.health.hediffSet.hediffs)
                            {
                                if (hd.def == TorannMagicDefOf.TM_HediffEnchantment_arcaneRes)
                                {
                                    if ((dinfo.Def.armorCategory != null &&
                                         (dinfo.Def.armorCategory == TorannMagicDefOf.Dark ||
                                          dinfo.Def.armorCategory == TorannMagicDefOf.Light)) ||
                                        dinfo.Def.defName.Contains("TM_") ||
                                        dinfo.Def.defName == "FrostRay" || dinfo.Def.defName == "Snowball" ||
                                        dinfo.Def.defName == "Iceshard" || dinfo.Def.defName == "Firebolt")
                                    {
                                        float actualDmg = dinfo.Amount / hd.Severity;
                                        if (actualDmg > 0)
                                        {
                                            dinfo.SetAmount(actualDmg);
                                            continue;
                                        }

                                        dinfo.SetAmount(0);
                                        absorbed = true;
                                        return false;
                                    }
                                }

                                //burning fury reduces damage taken
                                if (hd.def == TorannMagicDefOf.TM_BurningFuryHD)
                                {
                                    dinfo.SetAmount(dinfo.Amount * 0.65f);
                                    continue;
                                }

                                //frailty increases damage taken
                                if (hd.def == TorannMagicDefOf.TM_FrailtyHD)
                                {
                                    dinfo.SetAmount(dinfo.Amount + (dinfo.Amount * hd.Severity));
                                    continue;
                                }

                                //spirit distortion increases damage taken
                                if (hd.def == TorannMagicDefOf.TM_SpiritDistortionHD)
                                {
                                    dinfo.SetAmount(dinfo.Amount + (dinfo.Amount * hd.Severity));
                                    continue;
                                }
                            }
                        }

                        //spirit reduces all damage taken by 75% but applies it to spirit energy
                        if (__instance.def == TorannMagicDefOf.TM_SpiritTD)
                        {
                            float amt = dinfo.Amount * .25f;
                            Need nd = __instance.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND);
                            if (nd != null)
                            {
                                nd.CurLevel -= amt;
                            }

                            absorbed = true;
                            return false;
                        }

                        //Unique placement for SoL deflection
                        CompAbilityUserMagic compMagic = __instance.GetCompAbilityUserMagic();
                        if (compMagic != null && compMagic.SoL != null &&
                            compMagic.SoL.solAction == SoLAction.Hovering)
                        {
                            FlyingObject_SpiritOfLight sol = compMagic.SoL;
                            if (sol.LightEnergy > (dinfo.Amount / 10f) && Rand.Chance(sol.LightEnergy / 100f))
                            {
                                if (instigator != null)
                                {
                                    if ((dinfo.Weapon != null && !dinfo.Def.isExplosive) ||
                                        dinfo.WeaponBodyPartGroup != null)
                                    {
                                        if (__instance.Map != null)
                                        {
                                            Vector3 drawPos = __instance.DrawPos;
                                            float drawAngle = (instigator.DrawPos - drawPos).AngleFlat();
                                            drawPos.x +=
                                                Mathf.Clamp(
                                                    ((instigator.DrawPos.x - drawPos.x) / 20f) +
                                                    Rand.Range(-.1f, .1f), -.75f, .75f);
                                            drawPos.z +=
                                                Mathf.Clamp(
                                                    ((instigator.DrawPos.z - drawPos.z) / 10f) +
                                                    Rand.Range(-.1f, .1f), -1f, 1f);
                                            FleckMaker.Static(drawPos, __instance.Map,
                                                TorannMagicDefOf.SparkFlash, 1f);
                                            TM_MoteMaker.ThrowGenericMote(
                                                TorannMagicDefOf.Mote_LightShield_Glow, drawPos,
                                                __instance.Map, .65f, .27f, 0f, .13f, 0, 0, 0,
                                                drawAngle - 180);
                                            SoundInfo info = SoundInfo.InMap(
                                                new TargetInfo(__instance.Position, __instance.Map, false),
                                                MaintenanceType.None);
                                            info.volumeFactor = .5f;
                                            info.pitchFactor = 3f;
                                            TM_Action.DoReversalRandomTarget(dinfo, __instance, 0, 40f);
                                            TorannMagicDefOf.TM_MetalImpact.PlayOneShot(info);
                                        }

                                        sol.ActualLightCost(dinfo.Amount / 10f);
                                        dinfo.SetAmount(0);
                                        absorbed = true;
                                        return false;
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
            //Damage received by pawn
            //Order of application:
            //Absorb / Deflect damage (physical) - exit
            //Increase damage taken (flat amount) - continue;
            //Increase damage taken (percent) - continue

            public static bool Prefix(Pawn_HealthTracker __instance, Pawn ___pawn, ref DamageInfo dinfo,
                out bool absorbed)
            {
                Pawn pawn = ___pawn;
                if (dinfo.Def != null && pawn != null)
                {
                    Thing instigator = dinfo.Instigator;
                    if (!pawn.Downed && pawn.health?.hediffSet != null)
                    {
                        //Hate need grows when attacked; death knight retaliation/parry
                        //takes precedence over other mitigations
                        Hediff hateHediff = TM_Calc.GetHateHediff(pawn);
                        if (hateHediff != null && !dinfo.Def.isExplosive && dinfo.Amount > 0)
                        {
                            int hatePwr = 0;
                            int hateVer = 0;
                            int hateEff = 0;

                            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
                            if (comp != null && comp.IsMightUser)
                            {
                                hatePwr = comp.MightData.MightPowerSkill_Shroud
                                    .First((MightPowerSkill x) => x.label == "TM_Shroud_pwr").level;
                                hateVer = comp.MightData.MightPowerSkill_Shroud
                                    .First((MightPowerSkill x) => x.label == "TM_Shroud_ver").level;
                                hateEff = comp.MightData.MightPowerSkill_Shroud
                                    .First((MightPowerSkill x) => x.label == "TM_Shroud_eff").level;
                            }

                            HealthUtility.AdjustSeverity(pawn, hateHediff.def,
                                (dinfo.Amount * (1 + (.1f * hateEff))));
                            if (hateHediff.Severity >= 20 && Rand.Chance(.1f * hateVer) &&
                                dinfo.Instigator != null && dinfo.Instigator is Pawn &&
                                dinfo.Instigator != pawn &&
                                (dinfo.Instigator.Position - pawn.Position).LengthHorizontal < 2)
                            {
                                TM_Action.DamageEntities(dinfo.Instigator, null,
                                    (dinfo.Amount * (1 + .2f * hatePwr)), TMDamageDefOf.DamageDefOf.TM_Spirit,
                                    pawn);
                                if (pawn.Map != null)
                                {
                                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_SpiritRetaliation,
                                        pawn.DrawPos, pawn.Map, Rand.Range(1f, 1.2f), Rand.Range(.1f, .15f),
                                        0, Rand.Range(.1f, .2f), -600, 0, 0, Rand.Range(0, 360));
                                }

                                HealthUtility.AdjustSeverity(pawn, hateHediff.def,
                                    -(dinfo.Amount * (.8f - (.1f * hateEff))));
                            }
                        }

                        //All attacks that are completely mitigated
                        foreach (Hediff hd in pawn.health.hediffSet.hediffs)
                        {
                            if (!dinfo.Def.isExplosive && dinfo.Amount > 0)
                            {
                                //Blur evades attack
                                if (hd.def == TorannMagicDefOf.TM_BlurHD)
                                {
                                    float blurVal = .2f;
                                    if (pawn.GetCompAbilityUserMagic()?.MagicData.MagicPowerSkill_Cantrips
                                            .FirstOrDefault((MagicPowerSkill x) =>
                                                x.label == "TM_Cantrips_ver").level >= 11)
                                    {
                                        blurVal = .3f;
                                    }

                                    if (Rand.Chance(blurVal))
                                    {
                                        hd.TryGetComp<HediffComp_Blur>().blurTick =
                                            Find.TickManager.TicksGame;
                                        absorbed = true;
                                        return false;
                                    }
                                }

                                //Generic evasion
                                if (hd.def == TorannMagicDefOf.TM_EvasionHD)
                                {
                                    if (Rand.Chance(hd.Severity))
                                    {
                                        absorbed = true;
                                        return false;
                                    }
                                }

                                //Chronomancer foresees attack and evades
                                if (hd.def == TorannMagicDefOf.TM_PredictionHD)
                                {
                                    if (Rand.Chance(hd.Severity / 10f))
                                    {
                                        hd.TryGetComp<HediffComp_Prediction>().blurTick =
                                            Find.TickManager.TicksGame;
                                        absorbed = true;
                                        return false;
                                    }
                                }

                                //Bracer of defense blocks attack
                                if (hd.def == TorannMagicDefOf.TM_ArtifactBlockHD && Rand.Chance(.4f))
                                {
                                    if (instigator != null)
                                    {
                                        if (dinfo.Weapon != null || dinfo.WeaponBodyPartGroup != null)
                                        {
                                            if (instigator.Map == pawn.Map)
                                            {
                                                Vector3 drawPos = pawn.DrawPos;
                                                float drawAngle = (instigator.DrawPos - drawPos).AngleFlat();
                                                drawPos.x += Mathf.Clamp(
                                                    ((instigator.DrawPos.x - drawPos.x) / 5f) +
                                                    Rand.Range(-.1f, .1f), -.45f, .45f);
                                                drawPos.z += Mathf.Clamp(
                                                    ((instigator.DrawPos.z - drawPos.z) / 5f) +
                                                    Rand.Range(-.1f, .1f), -.45f, .45f);
                                                TM_MoteMaker.ThrowSparkFlashMote(drawPos, pawn.Map, 1f);
                                                TM_MoteMaker.ThrowGenericMote(
                                                    TorannMagicDefOf.Mote_BracerBlock_NoFlash, drawPos,
                                                    pawn.Map, .45f, .23f, 0f, .07f, 0, 0, 0, drawAngle);
                                                SoundInfo info = SoundInfo.InMap(
                                                    new TargetInfo(pawn.Position, pawn.Map, false),
                                                    MaintenanceType.None);
                                                TorannMagicDefOf.TM_MetalImpact.PlayOneShot(info);
                                            }

                                            dinfo.SetAmount(0);
                                            absorbed = true;
                                            return false;
                                        }
                                    }
                                }

                                //Flesh golem bracer acts like a bracer of defense
                                if (hd.def == TorannMagicDefOf.TM_FleshGolem_BracerGuardHD)
                                {
                                    if (Rand.Chance(hd.Severity))
                                    {
                                        if (instigator != null)
                                        {
                                            if (dinfo.Weapon != null || dinfo.WeaponBodyPartGroup != null)
                                            {
                                                if (instigator.Map == pawn.Map)
                                                {
                                                    Vector3 drawPos = pawn.DrawPos;
                                                    float drawAngle =
                                                        (instigator.DrawPos - drawPos).AngleFlat();
                                                    drawPos.x += Mathf.Clamp(
                                                        ((instigator.DrawPos.x - drawPos.x) / 5f) +
                                                        Rand.Range(-.1f, .1f), -.45f, .45f);
                                                    drawPos.z += Mathf.Clamp(
                                                        ((instigator.DrawPos.z - drawPos.z) / 5f) +
                                                        Rand.Range(-.1f, .1f), -.45f, .45f);
                                                    TM_MoteMaker.ThrowSparkFlashMote(drawPos, pawn.Map, 1f);
                                                    TM_MoteMaker.ThrowGenericMote(
                                                        TorannMagicDefOf.Mote_BracerBlock_NoFlash, drawPos,
                                                        pawn.Map, .45f, .23f, 0f, .07f, 0, 0, 0, drawAngle);
                                                    SoundInfo info =
                                                        SoundInfo.InMap(
                                                            new TargetInfo(pawn.Position, pawn.Map, false),
                                                            MaintenanceType.None);
                                                    TorannMagicDefOf.TM_MetalImpact.PlayOneShot(info);
                                                }

                                                dinfo.SetAmount(0);
                                                absorbed = true;
                                                return false;
                                            }
                                        }
                                    }
                                }

                                //bracer of deflection blocks and reflects attack
                                if (hd.def == TorannMagicDefOf.TM_ArtifactDeflectHD && Rand.Chance(.3f))
                                {
                                    if (instigator != null)
                                    {
                                        if (dinfo.Weapon != null || dinfo.WeaponBodyPartGroup != null)
                                        {
                                            if (instigator.Map == pawn.Map)
                                            {
                                                Vector3 drawPos = pawn.DrawPos;
                                                float drawAngle = (instigator.DrawPos - drawPos).AngleFlat();
                                                drawPos.x += Mathf.Clamp(
                                                    ((instigator.DrawPos.x - drawPos.x) / 5f) +
                                                    Rand.Range(-.1f, .1f), -.45f, .45f);
                                                drawPos.z += Mathf.Clamp(
                                                    ((instigator.DrawPos.z - drawPos.z) / 5f) +
                                                    Rand.Range(-.1f, .1f), -.45f, .45f);
                                                TM_MoteMaker.ThrowSparkFlashMote(drawPos, pawn.Map, 1f);
                                                TM_MoteMaker.ThrowGenericMote(
                                                    TorannMagicDefOf.Mote_BracerBlock, drawPos, pawn.Map,
                                                    .45f, .23f, 0f, .07f, 0, 0, 0, drawAngle);
                                                SoundInfo info = SoundInfo.InMap(
                                                    new TargetInfo(pawn.Position, pawn.Map, false),
                                                    MaintenanceType.None);
                                                TM_Action.DoReversalRandomTarget(dinfo, pawn, 0, 8f);
                                                TorannMagicDefOf.TM_MetalImpact.PlayOneShot(info);
                                            }

                                            dinfo.SetAmount(0);
                                            absorbed = true;
                                            return false;
                                        }
                                    }
                                }
                            }

                            //Commander "hold the line" shield mitigates damage at the physical level (shrugs off damage)
                            if (hd.def == TorannMagicDefOf.TM_HTLShieldHD)
                            {
                                HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_HTLShieldHD,
                                    -dinfo.Amount);
                                TM_Action.DisplayShieldHit(pawn, dinfo);
                                absorbed = true;
                                return false;
                            }

                            //stoneskin absorbs all damage that hits the pawn
                            if (hd.def == TorannMagicDefOf.TM_StoneskinHD)
                            {
                                HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_StoneskinHD, -1);
                                for (int m = 0; m < 4; m++)
                                {
                                    Vector3 vectorOffset = pawn.DrawPos;
                                    vectorOffset.x += (Rand.Range(-.3f, .3f));
                                    vectorOffset.z += Rand.Range(-.3f, .3f);
                                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust,
                                        vectorOffset, pawn.Map, Rand.Range(.15f, .35f), Rand.Range(.1f, .15f),
                                        0, Rand.Range(.1f, .2f), Rand.Range(-20, 20), Rand.Range(.3f, .5f),
                                        Rand.Range(0, 360), Rand.Range(0, 360));
                                }

                                absorbed = true;
                                return false;
                            }

                            //protection brand is like stoneskin but embedded into the skin instead of being a magical layer of skin
                            if (hd.def == TorannMagicDefOf.TM_ProtectionBrandHD)
                            {
                                HediffComp_BrandingProtection hd_bp =
                                    hd.TryGetComp<HediffComp_BrandingProtection>();
                                if (hd_bp != null && hd_bp.canProtect)
                                {
                                    hd_bp.TakeHit();
                                    if (pawn.Map != null)
                                    {
                                        for (int m = 0; m < 4; m++)
                                        {
                                            Vector3 vectorOffset = pawn.DrawPos;
                                            vectorOffset.x += (Rand.Range(-.3f, .3f));
                                            vectorOffset.z += Rand.Range(-.3f, .3f);
                                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_GlowingRuneA,
                                                vectorOffset, pawn.Map, Rand.Range(.1f, .25f),
                                                Rand.Range(.1f, .15f), 0, Rand.Range(.1f, .2f),
                                                Rand.Range(-10, 10), Rand.Range(.1f, .15f),
                                                Rand.Range(0, 360), Rand.Range(0, 360));
                                        }
                                    }

                                    absorbed = true;
                                    return false;
                                }
                            }

                            //Technoshield - reflects projectiles but only for ranged attacks less than a specific damage value, all or nothing
                            if ((hd.def == TorannMagicDefOf.TM_TechnoShieldHD && dinfo.Amount <= 10) ||
                                (hd.def == TorannMagicDefOf.TM_TechnoShieldHD_I && dinfo.Amount <= 13) ||
                                (hd.def == TorannMagicDefOf.TM_TechnoShieldHD_II && dinfo.Amount <= 18) ||
                                (hd.def == TorannMagicDefOf.TM_TechnoShieldHD_III && dinfo.Amount <= 30))
                            {
                                if (dinfo.Weapon != null && dinfo.Weapon.IsRangedWeapon)
                                {
                                    if (pawn.Map != null)
                                    {
                                        Vector3 drawPos = pawn.DrawPos;
                                        drawPos.x += ((instigator.DrawPos.x - drawPos.x) / 20f) +
                                                     Rand.Range(-.2f, .2f);
                                        drawPos.z += ((instigator.DrawPos.z - drawPos.z) / 20f) +
                                                     Rand.Range(-.2f, .2f);
                                        TM_MoteMaker.ThrowSparkFlashMote(drawPos, pawn.Map, 2f);
                                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_TechnoShield,
                                            pawn.DrawPos, pawn.Map, .9f, .1f, 0f, .05f, Rand.Range(-500, 500),
                                            0, 0, Rand.Range(0, 360));
                                    }

                                    TM_Action.DoReversal(dinfo, pawn);
                                    hd.Severity -= dinfo.Amount;
                                    dinfo.SetAmount(0);
                                    absorbed = true;
                                    return false;
                                }
                            }

                            //General magic shield - unlike paladin shield, ths one always fully blocks the last hit
                            if (hd.def == TorannMagicDefOf.TM_MagicShieldHD)
                            {
                                HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_MagicShieldHD,
                                    -(dinfo.Amount * .004f));
                                TM_Action.DisplayShieldHit(pawn, dinfo);
                                absorbed = true;
                                return false;
                            }

                            //blood mage blood shield
                            if (hd.def == TorannMagicDefOf.TM_BloodShieldHD)
                            {
                                HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_BloodShieldHD,
                                    -dinfo.Amount);
                                if (pawn.Map != null)
                                {
                                    for (int m = 0; m < 4; m++)
                                    {
                                        Effecter BloodShieldEffect =
                                            TorannMagicDefOf.TM_BloodShieldEffecter.Spawn();
                                        BloodShieldEffect.Trigger(
                                            new TargetInfo(pawn.Position, pawn.Map, false),
                                            new TargetInfo(pawn.Position, pawn.Map, false));
                                        BloodShieldEffect.Cleanup();
                                    }
                                }

                                dinfo.SetAmount(0);
                                absorbed = true;
                                return false;
                            }
                        }

                        //Deathknight can life tap if their attack strikes and damages the target
                        if (dinfo.Instigator is Pawn attacker)
                        {
                            CompAbilityUserMight comp = attacker.GetCompAbilityUserMight();

                            if (!attacker.Destroyed && !attacker.Downed && !attacker.Dead &&
                                attacker != pawn && dinfo.Weapon != null && dinfo.Weapon.IsMeleeWeapon &&
                                comp != null && comp.MightData != null)
                            {
                                if ((attacker.story != null && attacker.story.traits != null &&
                                     attacker.story.traits.HasTrait(TorannMagicDefOf.DeathKnight)) ||
                                    TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_LifeSteal, null,
                                        comp))
                                {
                                    int lifestealPwr = comp.MightData.MightPowerSkill_LifeSteal
                                        .First((MightPowerSkill x) => x.label == "TM_LifeSteal_pwr").level;
                                    int lifestealEff = comp.MightData.MightPowerSkill_LifeSteal
                                        .First((MightPowerSkill x) => x.label == "TM_LifeSteal_eff").level;
                                    int lifestealVer = comp.MightData.MightPowerSkill_LifeSteal
                                        .First((MightPowerSkill x) => x.label == "TM_LifeSteal_ver").level;

                                    TM_Action.DoAction_HealPawn(attacker, attacker, 1,
                                        dinfo.Amount * (TorannMagicDefOf.TM_LifeSteal.weaponDamageFactor +
                                                        (.02f * lifestealPwr)));
                                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Siphon,
                                        attacker.DrawPos, attacker.Map, 1f, .1f, .15f, .5f, 600, 0, 0,
                                        Rand.Range(0, 360));
                                    TM_MoteMaker.ThrowSiphonMote(attacker.DrawPos, attacker.Map, 1f);

                                    if (hateHediff != null && lifestealEff > 0)
                                    {
                                        HealthUtility.AdjustSeverity(attacker, hateHediff.def,
                                            dinfo.Amount * (.25f + .05f * lifestealEff));
                                        comp.Stamina.CurLevel +=
                                            (dinfo.Amount * (float)(.1f * lifestealEff)) / 100;
                                    }

                                    if (lifestealVer > 0)
                                    {
                                        Pawn ally = TM_Calc.FindNearbyInjuredPawnOther(attacker, 3, 0);
                                        if (ally != null)
                                        {
                                            TM_Action.DoAction_HealPawn(attacker, ally, 1,
                                                dinfo.Amount *
                                                ((TorannMagicDefOf.TM_LifeSteal.weaponDamageFactor - .05f) +
                                                 (.01f * lifestealVer)));
                                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Siphon,
                                                ally.DrawPos, ally.Map, 1f, .1f, .15f, .5f, 600, 0, 0,
                                                Rand.Range(0, 360));
                                        }
                                    }
                                }
                            }
                        }

                        //symbiosis shell splits damage between symbiote and host and should only occur if the pawn takes damage
                        Hediff symb =
                            ___pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SymbiosisHD);
                        if (symb != null && dinfo.Def.harmsHealth)
                        {
                            HediffComp_SymbiosisHost hdh = symb.TryGetComp<HediffComp_SymbiosisHost>();
                            if (hdh != null && hdh.symbiote != null &&
                                hdh.lastDamageTick < Find.TickManager.TicksGame)
                            {
                                hdh.lastDamageTick = Find.TickManager.TicksGame + 2;
                                float verVal = TM_Calc.GetSkillVersatilityLevel(hdh.symbiote,
                                    TorannMagicDefOf.TM_Symbiosis);
                                float sAmt = dinfo.Amount * (.25f + (.05f * verVal));
                                if (verVal >= 3)
                                {
                                    sAmt *= .5f;
                                }
                                else if (verVal >= 2)
                                {
                                    sAmt *= .6f;
                                }
                                else if (verVal >= 1)
                                {
                                    sAmt *= .8f;
                                }

                                DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_SymbiosisDD,
                                    sAmt, 2, -1, dinfo.Instigator, dinfo.HitPart, null);
                                hdh.symbiote.TakeDamage(dinfo2);
                                dinfo.SetAmount(dinfo.Amount * (.75f - (.05f * verVal)));
                                DamageInfo dinfo3 = new DamageInfo(dinfo.Def, dinfo.Amount,
                                    dinfo.ArmorPenetrationInt, dinfo.Angle, dinfo.Instigator, dinfo.HitPart,
                                    null);
                                ___pawn.TakeDamage(dinfo3);

                                TM_Action.DisplayShield(___pawn, dinfo.Amount);
                                TM_Action.DisplayShieldHit(hdh.symbiote, dinfo2);
                            }
                        }
                    }
                }

                absorbed = false;
                return true;
            }
        }

        [HarmonyPatch(typeof(Pawn_HealthTracker), "PostApplyDamage", null)]
        public static class PostApplyDamage_Patch
        {
            public static void Postfix(Pawn_HealthTracker __instance, ref DamageInfo dinfo, Pawn ___pawn)
            {
                if (dinfo.Def != null && ___pawn?.health?.hediffSet != null && !___pawn.Destroyed &&
                    !___pawn.Dead && ___pawn.Map != null)
                {
                    if (dinfo.Instigator != null && dinfo.Instigator != ___pawn)
                    {
                        Pawn instigator = dinfo.Instigator as Pawn;
                        //Arcane spectre deals bonus damage
                        if (instigator != null && dinfo.Def != TMDamageDefOf.DamageDefOf.TM_ArcaneSpectre &&
                            dinfo.Def.harmsHealth && dinfo.Def.canInterruptJobs)
                        {
                            if (instigator.health.hediffSet.HasHediff(TorannMagicDefOf
                                    .TM_HediffEnchantment_arcaneSpectre) && Rand.Chance(.5f))
                            {
                                DamageInfo dinfo2;
                                float amt;
                                amt = dinfo.Amount * .2f;
                                dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_ArcaneSpectre, (int)amt,
                                    0, (float)-1, instigator, dinfo.HitPart, null,
                                    DamageInfo.SourceCategory.ThingOrUnknown);
                                dinfo2.SetAllowDamagePropagation(false);
                                ___pawn.TakeDamage(dinfo2);
                                Vector3 displayVec = ___pawn.Position.ToVector3Shifted();
                                displayVec.x += Rand.Range(-.2f, .2f);
                                displayVec.z += Rand.Range(-.2f, .2f);
                                TM_MoteMaker.ThrowArcaneDaggers(displayVec, ___pawn.Map, .7f);
                            }
                        }

                        //drains spirit energy
                        if (instigator != null && instigator.needs != null &&
                            TM_Calc.IsPossessedBySpirit(instigator) && !TM_Calc.IsRobotPawn(___pawn) &&
                            TM_Calc.IsUsingMelee(instigator) &&
                            (instigator.Position - ___pawn.Position).LengthHorizontal <= 1.8f)
                        {
                            Need_Spirit ns =
                                instigator.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND) as Need_Spirit;
                            if (ns != null)
                            {
                                float rnd = Rand.Range(.05f, .08f);
                                ns.GainNeed(dinfo.Amount * rnd);
                                HealthUtility.AdjustSeverity(___pawn, TorannMagicDefOf.TM_SpiritDrainHD,
                                    dinfo.Amount * rnd * .025f);
                                float angle =
                                    (Quaternion.AngleAxis(-90, Vector3.up) *
                                     TM_Calc.GetVector(instigator.Position, ___pawn.Position)).ToAngleFlat();
                                float moteSize =
                                    dinfo.Amount > 20 ? 20 * rnd * .4f : dinfo.Amount * rnd * .4f;
                                for (int i = 0; i < 4; i++)
                                {
                                    Vector3 startPos = ___pawn.DrawPos;
                                    startPos.x += Rand.Range(-.2f, .2f);
                                    startPos.z += Rand.Range(-.2f, .2f);
                                    ThingDef moteDef = TorannMagicDefOf.Mote_Shadow;
                                    if (Rand.Chance(.5f))
                                    {
                                        moteDef = TorannMagicDefOf.Mote_Regen;
                                    }

                                    TM_MoteMaker.ThrowGenericMote(moteDef, startPos, ___pawn.Map, moteSize,
                                        .2f, .05f, .1f, 0, 5, (angle + Rand.Range(-20, 20)),
                                        Rand.Range(0, 360));
                                }
                            }
                        }

                        //stuns attacker
                        if (instigator != null && ___pawn.health != null &&
                            ___pawn.health.hediffSet != null &&
                            ___pawn.health.hediffSet.HasHediff(
                                HediffDef.Named("TM_Mecha-Golem_LightningCoreHD"), false))
                        {
                            Hediff hd =
                                ___pawn.health.hediffSet.GetFirstHediffOfDef(
                                    HediffDef.Named("TM_Mecha-Golem_LightningCoreHD"));
                            DamageInfo dinfo2;
                            float amt = hd.Severity * 2f;
                            dinfo2 = new DamageInfo(DamageDefOf.Stun, (int)amt, 0, (float)-1, ___pawn,
                                dinfo.HitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
                            dinfo2.SetAllowDamagePropagation(false);
                            instigator.TakeDamage(dinfo2);
                            FleckMaker.ThrowLightningGlow(instigator.DrawPos, ___pawn.Map,
                                Rand.Range(.4f, .6f));
                        }

                        //undead take repeated light damage
                        if (TM_Calc.IsUndead(___pawn))
                        {
                            //Log.Message("undead taking damage");
                            if (dinfo.Def != null && dinfo.Def.armorCategory != null &&
                                dinfo.Def.armorCategory.defName == "Light" && Rand.Chance(.35f))
                            {
                                //Log.Message("taking light damage");
                                dinfo.SetAmount(dinfo.Amount * .7f);
                                ___pawn.TakeDamage(dinfo);
                            }
                        }

                        //attacks that deal additional damage, poison, etc (this assumes the original attack has enough oomph to push through the original targets defenses)
                        if (instigator != null && dinfo.Def != TMDamageDefOf.DamageDefOf.TM_Cleave &&
                            dinfo.Def != TMDamageDefOf.DamageDefOf.TM_DragonStrike &&
                            dinfo.Def != TMDamageDefOf.DamageDefOf.TM_ChiBurn &&
                            dinfo.Def != DamageDefOf.Stun && dinfo.Def != TMDamageDefOf.DamageDefOf.TM_CQC)
                        {
                            if (instigator.RaceProps.Humanlike && instigator.story != null)
                            {
                                //Log.Message("checking class bonus damage");
                                CompAbilityUserMight comp = instigator.GetCompAbilityUserMight();
                                if ((instigator.story.traits.HasTrait(TorannMagicDefOf.Gladiator) ||
                                     TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_Cleave, null,
                                         comp)) && instigator.equipment.Primary != null &&
                                    instigator.equipment.Primary.def.IsMeleeWeapon)
                                {
                                    float cleaveChance =
                                        Mathf.Min(instigator.equipment.Primary.def.BaseMass * .15f, .75f);
                                    if (comp.useCleaveToggle && Rand.Chance(cleaveChance) &&
                                        comp.Stamina.CurLevel >=
                                        comp.ActualStaminaCost(TorannMagicDefOf.TM_Cleave) &&
                                        (___pawn.Position - instigator.Position).LengthHorizontal <= 1.6f)
                                    {
                                        MightPowerSkill pwr =
                                            comp.MightData.MightPowerSkill_Cleave.FirstOrDefault((
                                                MightPowerSkill x) => x.label == "TM_Cleave_pwr");
                                        MightPowerSkill str =
                                            comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((
                                                MightPowerSkill x) => x.label == "TM_global_strength_pwr");
                                        MightPowerSkill ver =
                                            comp.MightData.MightPowerSkill_Cleave.FirstOrDefault((
                                                MightPowerSkill x) => x.label == "TM_Cleave_ver");
                                        int dmgNum = Mathf.RoundToInt(dinfo.Amount *
                                                                      (TorannMagicDefOf.TM_Cleave
                                                                           .weaponDamageFactor +
                                                                       (.05f * pwr.level)));
                                        DamageInfo dinfo2 = new DamageInfo(
                                            TMDamageDefOf.DamageDefOf.TM_Cleave, dmgNum, 0, (float)-1,
                                            instigator, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                                        Verb_Cleave.ApplyCleaveDamage(dinfo2, instigator, ___pawn,
                                            ___pawn.Map, ver.level);
                                        comp.Stamina.CurLevel -=
                                            comp.ActualStaminaCost(TorannMagicDefOf.TM_Cleave);
                                        comp.MightUserXP += Rand.Range(10, 15);
                                    }
                                }

                                if (instigator.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) ||
                                    TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_CQC, null, comp))
                                {
                                    if (comp != null && comp.useCQCToggle &&
                                        comp.Stamina.CurLevel >=
                                        comp.ActualStaminaCost(TorannMagicDefOf.TM_CQC) &&
                                        (___pawn.Position - instigator.Position).LengthHorizontal <= 1.6f)
                                    {
                                        MightPowerSkill pwr =
                                            comp.MightData.MightPowerSkill_CQC.FirstOrDefault((
                                                MightPowerSkill x) => x.label == "TM_CQC_pwr");
                                        MightPowerSkill str =
                                            comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((
                                                MightPowerSkill x) => x.label == "TM_global_strength_pwr");
                                        int verVal = comp.MightData.MightPowerSkill_CQC
                                            .First((MightPowerSkill x) => x.label == "TM_CQC_ver").level;
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
                                            int dmgNum =
                                                Mathf.RoundToInt(comp.weaponDamage *
                                                                 TorannMagicDefOf.TM_CQC.weaponDamageFactor *
                                                                 Rand.Range(.7f, 1.2f)) + (2 * pwr.level);
                                            Vector3 strikeEndVec = ___pawn.DrawPos;
                                            strikeEndVec.x += Rand.Range(-.2f, .2f);
                                            strikeEndVec.z += Rand.Range(-.2f, .2f);
                                            Vector3 strikeStartVec = instigator.DrawPos;
                                            strikeStartVec.z += Rand.Range(-.2f, .2f);
                                            strikeStartVec.x += Rand.Range(-.2f, .2f);
                                            Vector3 angle = TM_Calc.GetVector(strikeStartVec, strikeEndVec);
                                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_CQC,
                                                strikeStartVec, instigator.Map, .35f, .08f, .03f, .05f, 0, 8f,
                                                (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(),
                                                (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat());
                                            TM_Action.DamageEntities(___pawn, dinfo.HitPart, dmgNum,
                                                TMDamageDefOf.DamageDefOf.TM_CQC, instigator);
                                            comp.Stamina.CurLevel -=
                                                comp.ActualStaminaCost(TorannMagicDefOf.TM_CQC);
                                            comp.MightUserXP += Rand.Range(10, 15);
                                        }
                                    }
                                }
                            }

                            if (instigator.RaceProps.Humanlike &&
                                instigator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MindOverBodyHD) &&
                                instigator.equipment.Primary == null)
                            {
                                CompAbilityUserMight comp = instigator.GetCompAbilityUserMight();
                                MightPowerSkill ver =
                                    comp.MightData.MightPowerSkill_DragonStrike.FirstOrDefault((
                                        MightPowerSkill x) => x.label == "TM_DragonStrike_ver");
                                if (Rand.Chance(.3f + (.05f * ver.level)) && comp != null)
                                {
                                    MightPowerSkill pwr =
                                        comp.MightData.MightPowerSkill_DragonStrike.FirstOrDefault((
                                            MightPowerSkill x) => x.label == "TM_DragonStrike_pwr");
                                    MightPowerSkill str =
                                        comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((
                                            MightPowerSkill x) => x.label == "TM_global_strength_pwr");
                                    int dmgNum = Mathf.RoundToInt(Rand.Range(6f, 10f) *
                                                                  (1 + (.1f * pwr.level) +
                                                                   (.05f * str.level)));
                                    DamageInfo dinfo2 = new DamageInfo(
                                        TMDamageDefOf.DamageDefOf.TM_DragonStrike, dmgNum, 0, (float)-1,
                                        instigator, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                                    TM_Action.DoAction_ApplySplashDamage(dinfo2, instigator, ___pawn,
                                        instigator.Map, 0);
                                }
                            }

                            if (instigator.RaceProps.Humanlike &&
                                instigator.health.hediffSet.HasHediff(TorannMagicDefOf.TM_NightshadeHD) &&
                                dinfo.Amount > 0 && instigator.Faction != ___pawn.Faction)
                            {
                                Hediff hd =
                                    instigator.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf
                                        .TM_NightshadeHD);
                                HediffComp_Nightshade hdComp = hd.TryGetComp<HediffComp_Nightshade>();
                                float applySev = hdComp.GetApplicationSeverity;
                                if (hdComp.GetDoseCount > 0 && instigator.equipment != null &&
                                    instigator.equipment.Primary != null)
                                {
                                    if (!instigator.equipment.Primary.def.IsMeleeWeapon)
                                    {
                                        applySev *= .40f;
                                    }

                                    Hediff toxinHD =
                                        ___pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf
                                            .TM_NightshadeToxinHD);
                                    if (toxinHD != null)
                                    {
                                        toxinHD.Severity += applySev;
                                    }
                                    else
                                    {
                                        HealthUtility.AdjustSeverity(___pawn,
                                            TorannMagicDefOf.TM_NightshadeToxinHD, applySev);
                                    }

                                    hd.Severity -= applySev;
                                }
                            }
                        }

                        //bonus damage potential for unarmed psionics
                        if (instigator != null &&
                            instigator.health.hediffSet.HasHediff(HediffDef.Named("TM_PsionicHD"), false))
                        {
                            if (instigator.equipment.Primary == null &&
                                dinfo.Def != TMDamageDefOf.DamageDefOf.TM_PsionicInjury &&
                                dinfo.Def != DamageDefOf.Stun)
                            {
                                CompAbilityUserMight comp = instigator.GetCompAbilityUserMight();
                                MightPowerSkill pwr =
                                    comp.MightData.MightPowerSkill_PsionicAugmentation.FirstOrDefault((
                                        MightPowerSkill x) => x.label == "TM_PsionicAugmentation_pwr");
                                float dmgNum = dinfo.Amount;
                                float pawnDPS = instigator.GetStatValue(StatDefOf.MeleeDPS, false);
                                float psiEnergy = instigator.health.hediffSet
                                    .GetFirstHediffOfDef(HediffDef.Named("TM_PsionicHD"), false).Severity;
                                if (psiEnergy > 20f && Rand.Chance(.3f + (.05f * pwr.level)) &&
                                    !___pawn.Downed)
                                {
                                    DamageInfo dinfo2 = new DamageInfo(
                                        TMDamageDefOf.DamageDefOf.TM_PsionicInjury,
                                        (dmgNum + pawnDPS) + 2 * pwr.level, dinfo.ArmorPenetrationInt,
                                        dinfo.Angle, instigator, dinfo.HitPart, dinfo.Weapon, dinfo.Category,
                                        dinfo.intendedTargetInt);
                                    TM_MoteMaker.MakePowerBeamMotePsionic(___pawn.DrawPos.ToIntVec3(),
                                        ___pawn.Map, 2.5f, 2f, .7f, .1f, .6f);
                                    ___pawn.TakeDamage(dinfo2);
                                    HealthUtility.AdjustSeverity(instigator, HediffDef.Named("TM_PsionicHD"),
                                        -2f);
                                    comp.Stamina.CurLevel -= .02f;
                                    comp.MightUserXP += Rand.Range(2, 4);
                                    if (psiEnergy > 60f && !___pawn.Dead &&
                                        Rand.Chance(.2f + (.03f * pwr.level)))
                                    {
                                        for (int i = 0; i < 6; i++)
                                        {
                                            float moteDirection = Rand.Range(0, 360);
                                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi,
                                                instigator.DrawPos, instigator.Map, Rand.Range(.3f, .5f),
                                                0.25f, .05f, .1f, 0, Rand.Range(6, 8), moteDirection,
                                                moteDirection);
                                        }

                                        Vector3 heading =
                                            (___pawn.Position - instigator.Position).ToVector3();
                                        float distance = heading.magnitude;
                                        Vector3 direction = heading / distance;
                                        IntVec3 destinationCell = ___pawn.Position +
                                                                  (direction * (Rand.Range(5, 8) +
                                                                      (2 * pwr.level))).ToIntVec3();
                                        FlyingObject_Spinning flyingObject =
                                            (FlyingObject_Spinning)GenSpawn.Spawn(
                                                ThingDef.Named("FlyingObject_Spinning"), ___pawn.Position,
                                                ___pawn.Map);
                                        flyingObject.speed = 35;
                                        flyingObject.Launch(instigator, destinationCell, ___pawn);
                                        HealthUtility.AdjustSeverity(instigator,
                                            HediffDef.Named("TM_PsionicHD"), -2f);
                                        comp.Stamina.CurLevel -= .02f;
                                        comp.MightUserXP += Rand.Range(3, 5);
                                    }
                                    else if (psiEnergy > 40f && !___pawn.Dead &&
                                             Rand.Chance(.4f + (.05f * pwr.level)))
                                    {
                                        DamageInfo dinfo3 = new DamageInfo(DamageDefOf.Stun, dmgNum / 2,
                                            dinfo.ArmorPenetrationInt, dinfo.Angle, instigator, dinfo.HitPart,
                                            dinfo.Weapon, dinfo.Category, dinfo.intendedTargetInt);
                                        ___pawn.TakeDamage(dinfo3);
                                        HealthUtility.AdjustSeverity(instigator,
                                            HediffDef.Named("TM_PsionicHD"), -2f);
                                        comp.Stamina.CurLevel -= .01f;
                                        comp.MightUserXP += Rand.Range(2, 3);
                                    }
                                }
                            }
                        }

                        //bonus damage for paladins using melee weapons and a wayfarer's chance to disarm an opponent
                        if (instigator != null && instigator.equipment != null &&
                            instigator.equipment.Primary != null &&
                            instigator.equipment.Primary.def.IsMeleeWeapon)
                        {
                            //Log.Message("checking instigator melee bonus ");                            
                            if (Rand.Chance(.2f) && instigator.story != null &&
                                instigator.story.traits != null &&
                                instigator.story.traits.HasTrait(TorannMagicDefOf.Paladin))
                            {
                                CompAbilityUserMagic comp = instigator.GetCompAbilityUserMagic();
                                if (comp != null)
                                {
                                    float amount = Rand.Range(2f, 4f) +
                                                   Rand.Range(0f, .1f * comp.MagicUserLevel);
                                    DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Holy,
                                        amount, 0, dinfo.Angle, instigator, dinfo.HitPart, null,
                                        DamageInfo.SourceCategory.ThingOrUnknown);
                                    TM_Action.DamageUndead(___pawn, amount, instigator);
                                }
                            }

                            if (instigator.health.hediffSet.HasHediff(TorannMagicDefOf
                                    .TM_HediffFightersFocus) && Rand.Chance(.2f))
                            {
                                CompAbilityUserMight comp = instigator.GetCompAbilityUserMight();
                                if (comp != null && comp.MightData.MightPowerSkill_FieldTraining
                                        .FirstOrDefault((MightPowerSkill x) =>
                                            x.label == "TM_FieldTraining_pwr").level >= 7)
                                {
                                    if (___pawn.equipment != null && ___pawn.equipment.Primary != null &&
                                        (___pawn.equipment.Primary.def.IsRangedWeapon ||
                                         ___pawn.equipment.Primary.def.IsMeleeWeapon))
                                    {
                                        ThingWithComps outThing = new ThingWithComps();
                                        ___pawn.equipment.TryDropEquipment(___pawn.equipment.Primary,
                                            out outThing, ___pawn.Position, false);
                                        MoteMaker.ThrowText(___pawn.DrawPos, ___pawn.MapHeld, "disarmed!",
                                            -1);
                                    }
                                }
                            }

                            if (___pawn.health != null && ___pawn.health.hediffSet != null &&
                                ___pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffThickSkin))
                            {
                                Hediff hd =
                                    ___pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf
                                        .TM_HediffThickSkin);
                                if (hd.Severity >= 3)
                                {
                                    bool flagDmg = false;
                                    if (dinfo.WeaponBodyPartGroup != null)
                                    {
                                        List<BodyPartRecord> bpr = new List<BodyPartRecord>();
                                        bpr.Clear();
                                        bpr.Add(instigator.RaceProps.body.AllParts
                                            .FirstOrDefault<BodyPartRecord>((BodyPartRecord x) =>
                                                x.def.tags.Contains(BodyPartTagDefOf
                                                    .ManipulationLimbSegment)));
                                        bpr.Add(instigator.RaceProps.body.AllParts
                                            .FirstOrDefault<BodyPartRecord>((BodyPartRecord x) =>
                                                x.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbCore)));
                                        bpr.Add(instigator.RaceProps.body.AllParts
                                            .FirstOrDefault<BodyPartRecord>((BodyPartRecord x) =>
                                                x.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbDigit)));
                                        if (bpr != null && bpr.Count > 0)
                                        {
                                            TM_Action.DamageEntities(instigator, bpr.RandomElement(),
                                                Rand.Range(1f, 4f), DamageDefOf.Scratch, ___pawn);
                                            flagDmg = true;
                                        }
                                    }

                                    if (!flagDmg)
                                    {
                                        TM_Action.DamageEntities(instigator, null, Rand.Range(1f, 4f),
                                            DamageDefOf.Scratch, ___pawn);
                                    }
                                }
                            }
                        }

                        //extra damage or effects from enchanted weapons
                        if (instigator != null)
                        {
                            //Log.Message("checking enchantment damage");
                            if (instigator.health.hediffSet.HasHediff(TorannMagicDefOf
                                    .TM_WeaponEnchantment_FireHD) &&
                                dinfo.Def != TMDamageDefOf.DamageDefOf.TM_Enchanted_Fire && Rand.Chance(.5f))
                            {
                                float sev = instigator.health.hediffSet
                                    .GetFirstHediffOfDef(TorannMagicDefOf.TM_WeaponEnchantment_FireHD)
                                    .Severity;
                                DamageInfo dinfo3 = new DamageInfo(
                                    TMDamageDefOf.DamageDefOf.TM_Enchanted_Fire,
                                    Rand.Range(1f + sev, 5f + sev), 1, -1, instigator, dinfo.HitPart,
                                    dinfo.Weapon, dinfo.Category, dinfo.intendedTargetInt);
                                ___pawn.TakeDamage(dinfo3);
                            }

                            if (instigator.health.hediffSet.HasHediff(TorannMagicDefOf
                                    .TM_WeaponEnchantment_IceHD) &&
                                dinfo.Def != TMDamageDefOf.DamageDefOf.TM_Enchanted_Ice && Rand.Chance(.4f))
                            {
                                float sev = instigator.health.hediffSet
                                    .GetFirstHediffOfDef(TorannMagicDefOf.TM_WeaponEnchantment_IceHD)
                                    .Severity;
                                DamageInfo dinfo3 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Enchanted_Ice,
                                    Mathf.RoundToInt(Rand.Range(3f + sev, 5f + sev) / 2), 1, -1, instigator,
                                    dinfo.HitPart, dinfo.Weapon, dinfo.Category, dinfo.intendedTargetInt);
                                ___pawn.TakeDamage(dinfo3);
                            }

                            if (instigator.health.hediffSet.HasHediff(TorannMagicDefOf
                                    .TM_WeaponEnchantment_LitHD) && dinfo.Def != DamageDefOf.Stun &&
                                Rand.Chance(.3f))
                            {
                                float sev = instigator.health.hediffSet
                                    .GetFirstHediffOfDef(TorannMagicDefOf.TM_WeaponEnchantment_LitHD)
                                    .Severity;
                                DamageInfo dinfo3 = new DamageInfo(DamageDefOf.Stun,
                                    Rand.Range(1f + (.5f * sev), 3f + (.5f * sev)), 1, -1, instigator,
                                    dinfo.HitPart, dinfo.Weapon, dinfo.Category, dinfo.intendedTargetInt);
                                ___pawn.TakeDamage(dinfo3);
                            }

                            if (instigator.health.hediffSet.HasHediff(TorannMagicDefOf
                                    .TM_WeaponEnchantment_DarkHD))
                            {
                                float sev = instigator.health.hediffSet
                                    .GetFirstHediffOfDef(TorannMagicDefOf.TM_WeaponEnchantment_DarkHD)
                                    .Severity;
                                if (Rand.Chance(.3f + (.1f * sev)))
                                {
                                    HealthUtility.AdjustSeverity(___pawn, TorannMagicDefOf.TM_Blind,
                                        Rand.Range(.05f, .2f));
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
            public static void Postfix(Verb __instance, LocalTargetInfo ___currentTarget,
                int ___burstShotsLeft)
            {
                if (__instance.CasterIsPawn)
                {
                    CompAbilityUserMight comp = __instance.CasterPawn.GetCompAbilityUserMight();
                    if (comp != null && comp.MightData != null && comp.Stamina != null &&
                        __instance.CasterPawn.health != null &&
                        __instance.CasterPawn.health.hediffSet != null)
                    {
                        if (__instance.CasterPawn.health.hediffSet.HasHediff(
                                TorannMagicDefOf.TM_MindOverBodyHD, false) &&
                            __instance.CasterPawn.equipment.Primary == null && ___burstShotsLeft <= 0)
                        {
                            //LocalTargetInfo currentTarget = Traverse.Create(root: __instance).Field(name: "currentTarget").GetValue<LocalTargetInfo>();
                            //int burstShotsLeft = Traverse.Create(root: __instance).Field(name: "burstShotsLeft").GetValue<int>();
                            MightPowerSkill pwr =
                                comp.MightData.MightPowerSkill_TigerStrike
                                    .FirstOrDefault((MightPowerSkill x) => x.label == "TM_TigerStrike_pwr");
                            MightPowerSkill eff =
                                comp.MightData.MightPowerSkill_TigerStrike
                                    .FirstOrDefault((MightPowerSkill x) => x.label == "TM_TigerStrike_eff");
                            MightPowerSkill globalSkill =
                                comp.MightData.MightPowerSkill_global_seff
                                    .FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_seff_pwr");
                            float actualStaminaCost =
                                .06f * (1 - (.1f * eff.level) * (1 - (.03f * globalSkill.level)));
                            if (comp.Stamina != null && comp.Stamina.CurLevel >= actualStaminaCost &&
                                Rand.Chance(.3f + (.05f * pwr.level)))
                            {
                                Vector3 strikeEndVec = ___currentTarget.CenterVector3;
                                strikeEndVec.x += Rand.Range(-.2f, .2f);
                                strikeEndVec.z += Rand.Range(-.2f, .2f);
                                Vector3 strikeStartVec = __instance.CasterPawn.DrawPos;
                                strikeStartVec.z += Rand.Range(-.2f, .2f);
                                strikeStartVec.x += Rand.Range(-.2f, .2f);
                                Vector3 angle = TM_Calc.GetVector(strikeStartVec, strikeEndVec);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Strike, strikeStartVec,
                                    __instance.CasterPawn.Map, .2f, .08f, .03f, .05f, 0, 8f,
                                    (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(),
                                    (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat());
                                __instance.CasterPawn.stances.SetStance(
                                    new Stance_Cooldown(5, ___currentTarget, __instance));
                                comp.Stamina.CurLevel -= actualStaminaCost;
                                comp.MightUserXP += (int)(.06f * 180);
                            }
                        }

                        if ((__instance.CasterPawn.story != null &&
                             __instance.CasterPawn.story.traits != null &&
                             __instance.CasterPawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier)) ||
                            TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_PistolSpec, null, comp))
                        {
                            //LocalTargetInfo currentTarget = Traverse.Create(root: __instance).Field(name: "currentTarget").GetValue<LocalTargetInfo>();
                            //int burstShotsLeft = Traverse.Create(root: __instance).Field(name: "burstShotsLeft").GetValue<int>();
                            if (__instance.CasterPawn.equipment != null &&
                                __instance.CasterPawn.equipment.Primary != null && ___burstShotsLeft <= 0)
                            {
                                if (comp.specWpnRegNum != -1 && comp.MightData.MightPowersSS
                                        .FirstOrDefault<MightPower>((MightPower x) =>
                                            x.abilityDef == TorannMagicDefOf.TM_PistolSpec).learned)
                                {
                                    int doubleTapPwr = comp.MightData.MightPowerSkill_PistolSpec
                                        .FirstOrDefault((MightPowerSkill x) => x.label == "TM_PistolSpec_eff")
                                        .level;
                                    MightPowerSkill globalSkill =
                                        comp.MightData.MightPowerSkill_global_seff.FirstOrDefault((
                                            MightPowerSkill x) => x.label == "TM_global_seff_pwr");
                                    float actualStaminaCost = .03f * (1 - (.1f * doubleTapPwr) *
                                        (1 - (.03f * globalSkill.level)));
                                    if (comp.Stamina.CurLevel >= actualStaminaCost &&
                                        Rand.Chance(.25f + (.05f * doubleTapPwr)))
                                    {
                                        __instance.CasterPawn.stances.SetStance(
                                            new Stance_Cooldown(5, ___currentTarget, __instance));
                                        comp.Stamina.CurLevel -= actualStaminaCost;
                                        comp.MightUserXP += (int)(.03f * 180);
                                    }
                                }
                            }
                        }
                    }

                    if (__instance.CasterPawn.stances != null &&
                        (__instance.CasterPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HasteHD) ||
                         __instance.CasterPawn.health.hediffSet.HasHediff(TorannMagicDefOf
                             .TM_SeverityHasteHD)))
                    {
                        //LocalTargetInfo currentTarget = Traverse.Create(root: __instance).Field(name: "currentTarget").GetValue<LocalTargetInfo>();
                        int ticksRemaining = 30;
                        if (__instance.CasterPawn.stances.curStance is Stance_Busy)
                        {
                            Stance_Busy st = (Stance_Busy)__instance.CasterPawn.stances.curStance;
                            ticksRemaining = Mathf.RoundToInt(st.ticksLeft / 2f);
                        }

                        __instance.CasterPawn.stances.SetStance(new Stance_Cooldown(ticksRemaining,
                            ___currentTarget, __instance));
                    }

                    if (__instance.CasterPawn.stances != null &&
                        __instance.CasterPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnrageHD))
                    {
                        int ticksRemaining = 30;
                        if (__instance.CasterPawn.stances.curStance is Stance_Busy)
                        {
                            Hediff hd =
                                __instance.CasterPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf
                                    .TM_EnrageHD);
                            Stance_Busy st = (Stance_Busy)__instance.CasterPawn.stances.curStance;
                            ticksRemaining = Mathf.RoundToInt(st.ticksLeft * (1f - hd.Severity));
                        }

                        __instance.CasterPawn.stances.SetStance(new Stance_Cooldown(ticksRemaining,
                            ___currentTarget, __instance));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Recipe_Surgery), "CheckSurgeryFail", null)]
        public static class CheckSurgeryFail_Base_Patch
        {
            public static bool Prefix(Recipe_Surgery __instance, Pawn surgeon, Pawn patient,
                List<Thing> ingredients, BodyPartRecord part, ref bool __result)
            {
                if (patient.health.hediffSet.HasHediff(HediffDef.Named("TM_StoneskinHD")) ||
                    patient.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ProtectionBrandHD))
                {
                    Hediff hediff =
                        patient.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_StoneskinHD"),
                            false);
                    if (hediff != null)
                    {
                        patient.health.RemoveHediff(hediff);
                    }

                    hediff = patient.health.hediffSet.GetFirstHediffOfDef(
                        TorannMagicDefOf.TM_ProtectionBrandHD, false);
                    if (hediff != null)
                    {
                        patient.health.RemoveHediff(hediff);
                    }
                }

                if (patient.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadHD")) ||
                    patient.health.hediffSet.HasHediff(HediffDef.Named("TM_UndeadAnimalHD")))
                {
                    Messages.Message(
                        "Something went horribly wrong while trying to perform a surgery on " +
                        patient.LabelShort + ", perhaps it's best to leave the bodies of the undead alone.",
                        MessageTypeDefOf.NegativeHealthEvent);
                    ExplosionHelper.Explode(surgeon.Position, surgeon.Map, 2f,
                        TMDamageDefOf.DamageDefOf.TM_CorpseExplosion, patient, Rand.Range(6, 12), 10,
                        TMDamageDefOf.DamageDefOf.TM_CorpseExplosion.soundExplosion, null, null, null, null,
                        0, 0, null, false, null, 0, 0, 0, false);
                    __result = true;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(StockGenerator_Animals), "HandlesThingDef", null)]
        public static class StockGenerator_Animals_Patch
        {
            private static bool Prefix(ThingDef thingDef, ref bool __result)
            {
                if (thingDef?.thingClass != null &&
                    thingDef.thingClass.ToString() == "TorannMagic.TMPawnSummoned")
                {
                    __result = false;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(WorkGiver_Researcher), "ShouldSkip", null)]
        public static class WorkGiver_Researcher_Patch
        {
            private static bool Prefix(Pawn pawn, ref bool __result)
            {
                if (pawn?.story?.traits != null && pawn.story.traits.HasTrait(TorannMagicDefOf.Undead))
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
                if (pawn?.story?.traits != null && pawn.story.traits.HasTrait(TorannMagicDefOf.Undead))
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
                if (pawn?.story?.traits != null && pawn.story.traits.HasTrait(TorannMagicDefOf.Undead))
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
                    CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();
                    if (p.workSettings.WorkIsActive(WorkTypeDefOf.Doctor) && comp != null &&
                        (p.story.traits.HasTrait(TorannMagicDefOf.TM_Golemancer) ||
                         TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_RuneCarving, comp, null)))
                    {
                        if (comp.MagicData.MagicPowersGolemancer.FirstOrDefault<MagicPower>((MagicPower x) =>
                                x.abilityDef == TorannMagicDefOf.TM_RuneCarving).learned &&
                            recipe.PawnSatisfiesSkillRequirements(p) &&
                            p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) &&
                            p.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                            !p.skills.GetSkill(SkillDefOf.Artistic).TotallyDisabled &&
                            !p.skills.GetSkill(SkillDefOf.Crafting).TotallyDisabled)
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
                    CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();
                    if (p.workSettings.WorkIsActive(WorkTypeDefOf.Doctor) && comp != null &&
                        (p.story.traits.HasTrait(TorannMagicDefOf.Druid) ||
                         TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_RegrowLimb, comp, null)))
                    {
                        if (comp.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) =>
                                x.abilityDef == TorannMagicDefOf.TM_RegrowLimb).learned &&
                            recipe.PawnSatisfiesSkillRequirements(p) &&
                            p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) &&
                            p.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
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
            private static bool Prefix(IEnumerable<Gizmo> gizmos, float startX) //, out Gizmo mouseoverGizmo)
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
            public static bool Prefix(Command __instance, Rect butRect, GizmoRenderParms parms,
                ref GizmoResult __result)
            {
                if (ModOptions.Settings.Instance.autocastEnabled)
                {
                    Command_PawnAbility com = __instance as Command_PawnAbility;
                    if (com != null && com.pawnAbility != null &&
                        com.pawnAbility.Def.defName.StartsWith("TM_"))
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
            public static bool Prefix(Command_PawnAbility __instance, Rect butRect, GizmoRenderParms parms,
                ref GizmoResult __result)
            {
                if (ModOptions.Settings.Instance.autocastEnabled &&
                    __instance.pawnAbility.Def.defName.StartsWith("TM_"))
                {
                    //Rect rect = new Rect(topLeft.x, topLeft.y, __instance.GetWidth(maxWidth), 75f);
                    __result = TM_Action.DrawAutoCastForGizmo(__instance, butRect, parms.shrunk, __result);
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(FloatMenuMap), "StillValid", null)]
        public static class IncitePassion_MenuValid
        {
            public static bool Prefix(FloatMenuOption opt, List<FloatMenuOption> curOpts, Pawn forPawn,
                ref bool __result)
            {
                if (opt.orderInPriority == 991)
                {
                    __result = true;
                    return false;
                }

                return true;
            }
        }
    }
}