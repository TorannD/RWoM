using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class BladeWeaponWear
    {
        private sealed class DeliberateDamage { public int Points; }
        private static readonly ConditionalWeakTable<Thing, DeliberateDamage> excluded = new();

        private static bool Weapon(Thing thing) => thing.def?.category == ThingCategory.Item &&
            thing.def.useHitPoints && thing.def.stackLimit == 1 && thing.def.IsWeapon;

        internal static int Repairable(Thing weapon, int maximum)
        {
            int missing = maximum - weapon.HitPoints;
            if (excluded.TryGetValue(weapon, out var damage)) missing -= damage.Points;
            return Math.Max(0, missing);
        }

        // Native item damage reports the number of HP actually removed. Environmental
        // deterioration and damage from enemies have no player-faction instigator.
        [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
        private static class PlayerWeaponDamage
        {
            private static void Postfix(Thing __instance, DamageInfo dinfo, DamageWorker.DamageResult __result)
            {
                if (!ClassProgressionFeature.Enabled || __instance.def?.category != ThingCategory.Item ||
                    __result == null || __result.totalDamageDealt <= 0 || __instance.Destroyed ||
                    !Weapon(__instance) || dinfo.Instigator?.Faction?.IsPlayer != true) return;
                int missing = __instance.MaxHitPoints - __instance.HitPoints;
                var entry = excluded.GetOrCreateValue(__instance);
                entry.Points = Math.Min(missing, entry.Points + UnityEngine.Mathf.RoundToInt(__result.totalDamageDealt));
            }
        }

        [HarmonyPatch(typeof(Thing), nameof(Thing.HitPoints), MethodType.Setter)]
        private static class RepairedWeapon
        {
            private static void Prefix(Thing __instance, int value)
            {
                if (!excluded.TryGetValue(__instance, out var entry) || value <= __instance.HitPoints) return;
                // Repair natural wear first. A fully restored weapon starts clean.
                entry.Points = Math.Min(entry.Points, Math.Max(0, __instance.MaxHitPoints - value));
                if (entry.Points == 0) excluded.Remove(__instance);
            }
        }

        [HarmonyPatch(typeof(Thing), nameof(Thing.ExposeData))]
        private static class SavedWear
        {
            private static void Postfix(Thing __instance)
            {
                if (Scribe.mode != LoadSaveMode.LoadingVars && Scribe.mode != LoadSaveMode.Saving) return;
                if (!Weapon(__instance)) return;
                int points = excluded.TryGetValue(__instance, out var entry) ? entry.Points : 0;
                if (Scribe.mode == LoadSaveMode.Saving && points == 0) return;
                Scribe_Values.Look(ref points, "rwomBladeDeliberateWear");
                if (Scribe.mode == LoadSaveMode.LoadingVars && points > 0)
                    excluded.GetOrCreateValue(__instance).Points = points;
            }
        }
    }

    public sealed class JobDriver_BladeWeaponService : JobDriver
    {
        private static int Amount(Thing weapon)
        {
            int maximum = weapon.MaxHitPoints;
            int service = Math.Max(1, UnityEngine.Mathf.CeilToInt(maximum * .1f));
            return BladeWeaponWear.Repairable(weapon, maximum) >= service ? service : 0;
        }

        internal static bool CanProject(Pawn actor,Pawn source) =>
            actor?.IsColonistPlayerControlled==true && source!=null && source!=actor &&
            ((BladeCareer.TrialOwner(actor)==source && BladeCareer.Path(source)?.Stage==InitiationStage.Foundation && !BladeCareer.Ready(source)) ||
             (SuperSoldierMartialPath.Owner(actor)==source && SuperSoldierMartialPath.Path(source)?.Stage==InitiationStage.Foundation));

        private static bool CanService(Pawn actor,Pawn source,Thing weapon) => source==actor
            ? BladeCareer.CanService(actor,weapon) || SuperSoldierMartialPath.CanService(actor,weapon)
            : CanProject(actor,source) &&
              ((BladeCareer.TrialOwner(actor)==source && BladeCareer.CanService(source,weapon)) ||
               (SuperSoldierMartialPath.Owner(actor)==source && SuperSoldierMartialPath.CanService(source,weapon)));

        private static bool Valid(Pawn actor, Thing weapon, Pawn source) =>
            actor?.Spawned == true && !actor.Dead && !actor.Downed && actor.IsColonistPlayerControlled &&
            weapon?.Spawned == true && weapon.Map == actor.Map && weapon.stackCount == 1 &&
            weapon.def.stackLimit == 1 && weapon.def.useHitPoints && weapon.def.IsWeapon &&
            CanService(actor,source,weapon) &&
            !weapon.IsForbidden(actor) && !weapon.IsBurning() && Amount(weapon) > 0;

        internal static void Target(Pawn source)
        {
            if (source.Spawned) TargetFrom(source,source);
            else if (SpiritRebirthState.TryGet(source,out var state) && CanProject(state.Host,source)) TargetFrom(state.Host,source);
        }

        internal static void TargetFrom(Pawn actor,Pawn source)
        {
            var map = actor.Map;
            bool Selectable(Thing weapon) => actor.Map == map && Find.CurrentMap == map && Valid(actor, weapon,source) &&
                actor.CanReserveAndReach(weapon, PathEndMode.Touch, Danger.Some);
            Find.Targeter.BeginTargeting(new TargetingParameters {
                canTargetItems = true, canTargetBuildings = false, canTargetPawns = false, canTargetLocations = false,
                mapObjectTargetsMustBeAutoAttackable = false, validator = t => Selectable(t.Thing)
            }, target => {
                if (!Selectable(target.Thing)) return;
                var job=JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_BladeWeaponService"),target);
                if (actor!=source) { job.targetB=source; job.targetC=actor.Position; }
                actor.jobs.TryTakeOrderedJob(job,JobTag.Misc);
            }, actor, requiresCastedSelected: false);
        }

        // The origin distinguishes a projected order even if its source no longer resolves on load.
        private Pawn Source => job.targetC.IsValid ? job.targetB.Pawn : pawn;
        public override bool TryMakePreToilReservations(bool errorOnFailed) => Valid(pawn, TargetThingA,Source) &&
            pawn.Reserve(TargetThingA, job, 1, 1, null, errorOnFailed);
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !Valid(pawn, TargetThingA,Source));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(600,TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A)
                .FailOn(() => !pawn.CanReachImmediate(TargetThingA,PathEndMode.Touch));
            yield return Toils_General.Do(() => {
                var weapon = TargetThingA;
                var source=Source;
                if (!Valid(pawn, weapon,source) || !pawn.CanReachImmediate(weapon,PathEndMode.Touch))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                int before = weapon.HitPoints;
                weapon.HitPoints = before + Amount(weapon);
                if (weapon.HitPoints > before)
                {
                    if (source==pawn || BladeCareer.TrialOwner(pawn)==source) BladeCareer.Service(source,weapon);
                    if (source==pawn || SuperSoldierMartialPath.Owner(pawn)==source) SuperSoldierMartialPath.Serviced(source,weapon);
                }
            });
        }
    }
}
