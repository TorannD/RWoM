using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class DeathKnightGuard
    {
        private static bool HostileStrike(Pawn victim, DamageInfo damage) =>
            ClassProgressionFeature.Enabled && victim?.Spawned == true && victim.Faction?.IsPlayer == true &&
            !victim.Dead && !victim.Downed && !victim.IsPrisoner && damage.Amount > 0 && damage.Def?.harmsHealth == true &&
            damage.Instigator is Pawn attacker && attacker.Spawned && attacker.Map == victim.Map &&
            !attacker.Downed && !attacker.IsPrisoner && attacker.HostileTo(victim);

        private static bool AllyNear(Pawn knight)
        {
            foreach (var offset in GenAdj.AdjacentCells)
            {
                var cell = knight.Position + offset;
                if (!cell.InBounds(knight.Map)) continue;
                foreach (var thing in cell.GetThingList(knight.Map))
                    if (thing is Pawn ally && ally.Faction == knight.Faction && !ally.Dead && !ally.IsPrisoner) return true;
            }
            return false;
        }

        [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
        private static class GuardTrial
        {
            private static void Prefix(Thing __instance, DamageInfo dinfo, out Pawn __state)
            {
                __state = null;
                if (__instance is not Pawn knight || !HostileStrike(knight,dinfo) || !AllyNear(knight)) return;
                var owner=DeathKnightMartialPath.PassiveOwner(knight);
                if (DeathKnightMartialPath.Path(owner) is { Stage: < InitiationStage.Ascended }) __state=owner;
            }
            private static void Postfix(Pawn __state, DamageWorker.DamageResult __result)
            {
                if (__state != null && __result?.totalDamageDealt > 0) DeathKnightMartialPath.Battle(__state);
            }
        }

        // Once every 10 seconds, spend 10 stored hate to absorb up to 25 damage for an adjacent ally.
        // Honor vanilla shield absorption first; only one knight pays for any hit.
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
        private static class OathShield
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
            {
                if (absorbed || !HostileStrike(__instance, dinfo)) return;
                foreach (var offset in GenAdj.AdjacentCells)
                {
                    var cell = __instance.Position + offset;
                    if (!cell.InBounds(__instance.Map)) continue;
                    foreach (var thing in cell.GetThingList(__instance.Map))
                    {
                        if (thing is not Pawn guard || guard.Dead || guard.Downed || guard.IsPrisoner ||
                            guard.Faction != __instance.Faction || !SpendGuard(guard)) continue;
                        dinfo.SetAmount(Mathf.Max(0f, dinfo.Amount - 25f));
                        absorbed = dinfo.Amount == 0f;
                        return;
                    }
                }
            }
        }
        internal static bool SpendGuard(Pawn guard)
        {
            var owner=DeathKnightMartialPath.PassiveOwner(guard);
            if (owner == null || DeathKnightMartialPath.Path(owner) is not { Stage: InitiationStage.Ascended, MartialData: DeathKnightDisciplineData { Oath: true } data })
                return false;
            var now = Find.TickManager.TicksGame;
            var hate = TM_Calc.GetHateHediff(owner);
            if (now < data.OathReadyAt || hate == null || hate.Severity < 10f) return false;
            data.OathReadyAt = now + 600;
            hate.Severity = Mathf.Max(.001f, hate.Severity - 10f);
            return true;
        }
    }
}
