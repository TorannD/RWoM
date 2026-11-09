using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using AbilityUser;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic
{
    /// <summary>
    /// Native Multiplayer (rwmt) compatibility layer.
    /// Activated from <see cref="TorannMagicMod"/> only when the Multiplayer mod is loaded,
    /// so the mod keeps working without it. The AbilityUser sync mirrors the
    /// Multiplayer-Compatibility mod's JecsTools patch: when that mod is present and
    /// already syncs PawnAbility (JecsLite loaded), we defer to it instead of
    /// double-registering.
    /// </summary>
    public static class TMPCompatAdapter
    {
        // Preserve the legacy public array identity for add-ons that update it.
        public static readonly string[] MultiplayerPackageIds = TMPCompat.MultiplayerPackageIds;

        public const string MultiplayerPackageId = "zetrith.multiplayer";

        public static bool Initialized { get; private set; }

        // Called from TMPCompatStartup (StaticConstructorOnStartup) — by that point
        // every active mod class (including the MP fork's) has been constructed and
        // the Multiplayer API is initialized, so RegisterSyncMethod hits the real API.
        public static void Setup()
        {
            if (Initialized)
            {
                return;
            }
            Initialized = true;

            // The steps are independent — one broken registration must not take
            // down the others (this exact cascade happened when a newer fork made
            // a duplicate registration throw and killed the verb hooks below).
            int failed = 0;

            try
            {
                PatchAbilityUserFramework();
            }
            catch (Exception ex)
            {
                failed++;
                Log.Error("[TMPCompat] AbilityUser framework setup failed: " + ex);
            }

            try
            {
                PatchGizmoToggles();
            }
            catch (Exception ex)
            {
                failed++;
                Log.Error("[TMPCompat] Gizmo toggle sync setup failed: " + ex);
            }

            try
            {
                PatchGolemCommands();
            }
            catch (Exception ex)
            {
                failed++;
                Log.Error("[TMPCompat] Golem command sync setup failed: " + ex);
            }

            try
            {
                PatchJobVerbRegistration();
            }
            catch (Exception ex)
            {
                failed++;
                Log.Error("[TMPCompat] Job/verb registration setup failed: " + ex);
            }

            try
            {
                Integrations.FleshMultiplayerRegistration.Register();
                MP.RegisterSyncMethod(typeof(SpellItems.SpellItemCasting), nameof(SpellItems.SpellItemCasting.TryStart));
                MP.RegisterSyncMethod(typeof(SpellItems.SpellItemCasting), nameof(SpellItems.SpellItemCasting.TryStartPortal));
            }
            catch (Exception ex)
            {
                failed++;
                Log.Error("[TMPCompat] Flesh development sync setup failed: " + ex);
            }

            if (failed == 0)
            {
                Log.Message("[TMPCompat] Multiplayer compatibility initialized.");
            }
        }

        // MP sync of TryTakeOrderedJob serializes the Job via ExposeData; verbToUse is
        // a Scribe_References cross-reference that only resolves against the session's
        // SharedCrossRefs. Ability verbs are created at runtime, so register each one
        // there as soon as it is taken (both peers run this prefix during their local
        // UseAbility command, before the host's job-sync command arrives).
        private static void TryTakeOrderedJob_VerbRegister_Prefix(Job job)
        {
            try
            {
                var v = job?.verbToUse;
                if (v != null && !v.loadID.NullOrEmpty())
                {
                    SharedCrossRefsRegister(v);
                }
            }
            catch (Exception)
            {
            }
        }

        private static void InitVerb_Register_Postfix(PawnAbility __instance)
        {
            try
            {
                SharedCrossRefsRegister(__instance.Verb);
            }
            catch (Exception)
            {
            }
        }

        internal static void SharedCrossRefsRegister(Verse.ILoadReferenceable obj)
        {
            if (obj == null || mpSharedCrossRefsRegister == null)
            {
                return;
            }
            try
            {
                mpSharedCrossRefsRegister.Invoke(null, new object[] { obj });
            }
            catch (Exception)
            {
            }
        }

        private static MethodInfo mpSharedCrossRefsGetter;
        private static MethodInfo mpSharedCrossRefsRegister;

        private static void InitSharedCrossRefsHook()
        {
            try
            {
                var scribeUtil = AccessTools.TypeByName("Multiplayer.Client.ScribeUtil");
                mpSharedCrossRefsGetter = AccessTools.PropertyGetter(scribeUtil, "sharedCrossRefs");
                var sharedType = AccessTools.TypeByName("Multiplayer.Client.SharedCrossRefs")
                    ?? mpSharedCrossRefsGetter?.ReturnType;
                mpSharedCrossRefsRegister = AccessTools.Method(sharedType, "RegisterLoaded");
                if (mpSharedCrossRefsGetter == null || mpSharedCrossRefsRegister == null)
                {
                    Log.Warning("[TMPCompat] Multiplayer SharedCrossRefs API not found; verb sync hook disabled.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[TMPCompat] SharedCrossRefs hook failed: " + ex.Message);
            }
        }

        private static void PatchJobVerbRegistration()
        {
            InitSharedCrossRefsHook();
            if (mpSharedCrossRefsRegister == null)
            {
                return;
            }
            var harmony = new Harmony("rimworld.torann.tmagic");
            // MUST be a prefix: registration has to happen before MP's DoSync serializes
            // the job — as a postfix it ran after the sync and the verb reference went
            // out unresolved.
            harmony.Patch(AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryTakeOrderedJob)),
                prefix: new HarmonyMethod(typeof(TMPCompatAdapter), nameof(TryTakeOrderedJob_VerbRegister_Prefix)));
            harmony.Patch(AccessTools.Method(AccessTools.TypeByName("AbilityUser.PawnAbility"), "InitVerb"),
                postfix: new HarmonyMethod(typeof(TMPCompatAdapter), nameof(InitVerb_Register_Postfix)));
        }

        public static bool MultiplayerActive()
        {
            foreach (string id in MultiplayerPackageIds)
            {
                if (ModsConfig.IsActive(id))
                {
                    return true;
                }
            }
            return false;
        }

        #region AbilityUser framework

        private static void PatchAbilityUserFramework()
        {
            // RWoM travel and special orders can bypass the base implementation.
            // Sync before those overrides construct a Job: its runtime verb and
            // travel consent must be created independently on every peer.
            foreach (Type owner in new[] { typeof(MagicAbility), typeof(MightAbility) })
            {
                var method = AccessTools.DeclaredMethod(owner, nameof(PawnAbility.UseAbility));
                if (method != null && !IsSyncMethodRegistered(method))
                    MP.RegisterSyncMethod(owner, nameof(PawnAbility.UseAbility))?.SetPostInvoke(StopTargeting);
            }
            MP.RegisterSyncWorker<MagicAbility>(
                (SyncWorkerDelegate<MagicAbility>)Delegate.CreateDelegate(typeof(SyncWorkerDelegate<MagicAbility>),
                    AccessTools.Method(typeof(TMPCompatAdapter), nameof(SyncMagicAbility))), typeof(MagicAbility));
            MP.RegisterSyncWorker<MightAbility>(
                (SyncWorkerDelegate<MightAbility>)Delegate.CreateDelegate(typeof(SyncWorkerDelegate<MightAbility>),
                    AccessTools.Method(typeof(TMPCompatAdapter), nameof(SyncMightAbility))), typeof(MightAbility));
            // With JecsLite active, the Multiplayer-Compatibility mod syncs the
            // JecsTools ability framework itself (its JecsTools class triggers on
            // zal.jecsliterwom) — including PawnAbility.UseAbility and a sync
            // worker. Newer MP forks report a second registration as an ERROR
            // ("already registered as a SyncMethod") even when they keep going,
            // so ask the fork's registry first and defer to the compat mod
            // without registering anything.
            MethodInfo useAbilityMethod = AccessTools.Method(typeof(PawnAbility), nameof(PawnAbility.UseAbility));
            if (IsSyncMethodRegistered(useAbilityMethod))
            {
                Log.Message("[TMPCompat] PawnAbility.UseAbility is already synced by Multiplayer-Compatibility — deferring to it.");
                return;
            }

            ISyncMethod useAbility;
            try
            {
                useAbility = MP.RegisterSyncMethod(typeof(PawnAbility), nameof(PawnAbility.UseAbility));
            }
            catch (Exception ex)
            {
                Log.Message("[TMPCompat] PawnAbility.UseAbility is already synced by Multiplayer-Compatibility — deferring to it (" + ex.GetType().Name + ").");
                return;
            }
            if (useAbility == null)
            {
                Log.Message("[TMPCompat] PawnAbility.UseAbility is already synced by Multiplayer-Compatibility — deferring to it.");
                return;
            }

            // Casting an ability is the core interaction; sync the job creation call
            // and stop local targeting afterwards so the client does not re-enter it.
            useAbility.SetPostInvoke(StopTargeting);
            // NOTE: if the Multiplayer API is present but uninitialized (e.g. only the
            // API assembly got loaded), MP.* throws and Setup's step handler catches it —
            // the rest of the mod is unaffected.

            // CreateDelegate at runtime instead of a method-group conversion: the
            // compiler would otherwise emit a hidden static field of type
            // SyncWorkerDelegate`1, which cannot resolve when Multiplayer is absent
            // and breaks the game's static-constructor field scan.
            SyncWorkerDelegate<PawnAbility> workerDelegate =
                (SyncWorkerDelegate<PawnAbility>)Delegate.CreateDelegate(
                    typeof(SyncWorkerDelegate<PawnAbility>),
                    AccessTools.Method(typeof(TMPCompatAdapter), nameof(SyncPawnAbility)));
            try
            {
                MP.RegisterSyncWorker<PawnAbility>(workerDelegate, typeof(PawnAbility));
            }
            catch (Exception ex)
            {
                Log.Warning("[TMPCompat] PawnAbility sync worker registration skipped: " + ex.Message);
            }
        }

        // Multiplayer.Client.Sync.methodBaseToInternalId is a public static
        // registry of every registered SyncMethod in the rwmt forks. Returns
        // false when the fork build doesn't expose it — the caller then just
        // registers and the fork reports a duplicate itself.
        private static bool IsSyncMethodRegistered(MethodInfo method)
        {
            if (method == null)
            {
                return false;
            }
            try
            {
                var dict = AccessTools.Field(AccessTools.TypeByName("Multiplayer.Client.Sync"), "methodBaseToInternalId")
                    ?.GetValue(null) as System.Collections.IDictionary;
                return dict != null && dict.Contains(method);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void StopTargeting(object instance, object[] args)
        {
            if (MP.IsExecutingSyncCommandIssuedBySelf)
            {
                Find.Targeter.StopTargeting();
            }
        }

        // PawnAbility has no stable identity across peers (no thing ID), so it is
        // synced by its position inside the pawn's CompAbilityUser power lists.
        private static void SyncPawnAbility(SyncWorker sync, ref PawnAbility ability)
        {
            if (sync.isWriting)
            {
                int compIndex = -1;
                int powerIndex = -1;
                Pawn pawn = null;

                var user = ability?.abilityUser;
                if (user?.parent is Pawn p)
                {
                    var comps = p.GetCompAbilityUsers().ToList();
                    for (int i = 0; i < comps.Count; i++)
                    {
                        var powers = comps[i].AbilityData.AllPowers;
                        int idx = powers.IndexOf(ability);
                        if (idx >= 0)
                        {
                            pawn = p;
                            compIndex = i;
                            powerIndex = idx;
                            break;
                        }
                    }
                }

                sync.Write(compIndex);
                if (compIndex < 0)
                {
                    return;
                }
                sync.Write(powerIndex);
                sync.Write(pawn);
            }
            else
            {
                int compIndex = sync.Read<int>();
                if (compIndex < 0)
                {
                    return;
                }
                int powerIndex = sync.Read<int>();
                Pawn pawn = sync.Read<Pawn>();
                var comps = pawn.GetCompAbilityUsers().ToList();
                if (compIndex < comps.Count)
                {
                    var powers = comps[compIndex].AbilityData.AllPowers;
                    if (powerIndex < powers.Count)
                    {
                        ability = powers[powerIndex];
                    }
                }
            }
        }

        private static void SyncMagicAbility(SyncWorker sync, ref MagicAbility ability)
        {
            PawnAbility value = ability;
            SyncPawnAbility(sync, ref value);
            ability = value as MagicAbility;
        }

        private static void SyncMightAbility(SyncWorker sync, ref MightAbility ability)
        {
            PawnAbility value = ability;
            SyncPawnAbility(sync, ref value);
            ability = value as MightAbility;
        }

        #endregion

        #region Gizmo toggles

        private static void PatchGizmoToggles()
        {
            MP.RegisterSyncMethod(typeof(CompAbilityUserMagic), nameof(CompAbilityUserMagic.ToggleTechnoBit));
            MP.RegisterSyncMethod(typeof(CompAbilityUserMagic), nameof(CompAbilityUserMagic.ToggleTechnoBitRepair));
            MP.RegisterSyncMethod(typeof(CompAbilityUserMagic), nameof(CompAbilityUserMagic.ToggleElementalShot));

            MP.RegisterSyncMethod(typeof(CompAbilityUserMight), nameof(CompAbilityUserMight.ToggleCleave));
            MP.RegisterSyncMethod(typeof(CompAbilityUserMight), nameof(CompAbilityUserMight.ToggleCQC));
            MP.RegisterSyncMethod(typeof(CompAbilityUserMight), nameof(CompAbilityUserMight.TogglePsionicAugmentation));
            MP.RegisterSyncMethod(typeof(CompAbilityUserMight), nameof(CompAbilityUserMight.TogglePsionicMindAttack));

            MP.RegisterSyncMethod(typeof(CompPolymorph), nameof(CompPolymorph.CancelPolymorph));

            MP.RegisterSyncMethod(typeof(TM_Action), nameof(TM_Action.RemoveSymbiosisCommand));
            MP.RegisterSyncMethod(typeof(TM_Action), nameof(TM_Action.PromoteWanderer));
        }

        #endregion

        #region Golems

        private static void PatchGolemCommands()
        {
            MP.RegisterSyncMethod(typeof(Golems.Building_TMGolemBase), nameof(Golems.Building_TMGolemBase.ToggleHoldFire));
            MP.RegisterSyncMethod(typeof(Golems.Building_TMGolemBase), nameof(Golems.Building_TMGolemBase.ToggleGlow));
            MP.RegisterSyncMethod(typeof(Golems.TMPawnGolem), nameof(Golems.TMPawnGolem.ToggleRangedAttacks));
        }

        #endregion
    }
}
