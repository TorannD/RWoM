using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class BladeFoundation
    {
        private static int Training(Pawn pawn)
            => pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightPowerSkill_BladeFocus
                .FirstOrDefault(s => s.label == "TM_BladeFocus_pwr")?.level ?? 0;

        internal static float Offset(Pawn pawn, StatDef stat)
        {
            if (stat == null) return 0f;
            var path = BladeCareer.ActivePath(pawn,out var owner);
            if (path == null || path.BladeMemory || pawn.story?.traits?.HasTrait(TorannMagicDefOf.Bladedancer) == true)
                return 0f;
            int rank=UnityEngine.Mathf.Clamp(Training(owner),0,3);
            var degree = TorannMagicDefOf.Bladedancer?.degreeDatas?.Find(d => d.degree == rank);
            return degree?.statOffsets?.FirstOrDefault(offset => offset.stat == stat)?.value ?? 0f;
        }

        internal static float MemoryMultiplier(Pawn pawn, Pawn owner, Pawn victim)
        {
            var path = BladeCareer.Path(owner);
            if (path?.BladeMemory != true || BladeCareer.TrialOwner(pawn)!=owner ||
                pawn?.Spawned != true || pawn.Dead || pawn.Downed || victim?.Spawned != true ||
                victim.Map != pawn.Map || victim.Dead || victim.Downed || victim.IsPrisoner || !victim.HostileTo(pawn) ||
                !pawn.Position.AdjacentTo8WayOrInside(victim.Position)) return 1f;
            int last = path.BladeLastTargetId;
            path.BladeLastTargetId = victim.thingIDNumber;
            int now = Find.TickManager.TicksGame;
            if (last <= 0 || last == victim.thingIDNumber || path.BladeMemoryReadyAt > now) return 1f;
            path.BladeMemoryReadyAt = now + 360;
            return 1f + .02f * UnityEngine.Mathf.Clamp(Training(owner) + 1, 1, 5);
        }
    }

    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "DamageInfosToApply")]
    internal static class BladeMemoryDamagePatch
    {
        private static void Postfix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target, ref IEnumerable<DamageInfo> __result)
        {
            var attacker = __instance.CasterPawn;
            if (attacker?.equipment?.Primary is not ThingWithComps weapon ||
                !weapon.def.IsMeleeWeapon || __instance.EquipmentSource != weapon || target.Pawn == null) return;
            if (BladeCareer.ActivePath(attacker,out var owner)?.BladeMemory != true || SpiritRebirthActionPolicy.Marked(attacker)) return;
            __result = PrimaryDamage(__result, attacker, owner, target.Pawn);
        }

        private static IEnumerable<DamageInfo> PrimaryDamage(IEnumerable<DamageInfo> source, Pawn attacker, Pawn owner, Pawn victim)
        {
            bool first = true;
            foreach (var info in source)
            {
                var damage = info;
                if (first && damage.Amount > 0f)
                    damage.SetAmount(damage.Amount * BladeFoundation.MemoryMultiplier(attacker, owner, victim));
                first = false;
                yield return damage;
            }
        }
    }
}
