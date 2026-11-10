using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class FireboltDamageTrial
    {
        public const string TrialId = "rwom.fire.firebolt-damage";
        public sealed class Snapshot
        {
            internal DisciplineProgress path;
            internal bool consumed;
        }
        public static Snapshot Capture(Pawn caster, Pawn victim)
        {
            if (!ClassIdentity.TryGetState(caster, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var location = caster.Spawned ? caster.Map : SpiritRebirthState.TryGet(caster,out var spirit) &&
                spirit.HasValidHost(caster) ? spirit.Host.Map : null;
            if (caster.Dead || victim == null || victim.Dead || !victim.Spawned || location != victim.Map
                || caster.Faction == null || victim.IsPrisoner || victim.IsSlave || !victim.HostileTo(caster)
                || (victim.Faction == null ? !victim.InAggroMentalState
                    : victim.Faction == caster.Faction || !victim.Faction.HostileTo(caster.Faction))) return null;
            var path = state.Find("rwom.fire");
            if (path?.Domain != DisciplineDomain.Magic || path.Stage != InitiationStage.Foundation || path.CompletedTrials.Contains(TrialId)) return null;
            var magic = caster.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
            if (magic?.MagicData.MagicPowersIF.Any(p => p.learned && p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Firebolt)) != true) return null;
            return new Snapshot { path = path };
        }
        public static void Complete(Snapshot snapshot, float damage)
        {
            if (snapshot == null || snapshot.consumed) return;
            snapshot.consumed = true;
            snapshot.path.RecordFireboltDamage(damage);
        }
    }

    // Explosion damage is applied on later ticks, after its projectile has gone.
    // The native damage info retains the projectile definition and instigator.
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class FireboltDamageTrialPatch
    {
        public static void Prefix(Thing __instance, DamageInfo dinfo, out FireboltDamageTrial.Snapshot __state)
        {
            __state = dinfo.Weapon?.defName == "Projectile_Firebolt" && dinfo.Def == TMDamageDefOf.DamageDefOf.Firebolt
                ? FireboltDamageTrial.Capture(dinfo.Instigator as Pawn, __instance as Pawn) : null;
        }
        public static void Postfix(DamageWorker.DamageResult __result, FireboltDamageTrial.Snapshot __state)
            => FireboltDamageTrial.Complete(__state, __result?.totalDamageDealt ?? 0f);
    }
}
