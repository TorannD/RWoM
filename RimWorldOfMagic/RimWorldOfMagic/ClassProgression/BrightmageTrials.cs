using System;
using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;
namespace TorannMagic.ClassProgression
{
    public static class BrightmageTrials
    {
        public const string FoundationTrialId = "rwom.brightmage.foundation", MasteryTrialId = "rwom.brightmage.mastery";
        static CompAbilityUserMagic Magic(Pawn pawn) => pawn?.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
        static MagicPower Owner(Pawn pawn, bool burst) => Magic(pawn)?.MagicData.MagicPowersBrightmage.FirstOrDefault(p => p.learned && p.TMabilityDefs.Contains(burst ? TorannMagicDefOf.TM_LightBurst : TorannMagicDefOf.TM_LightLance));
        static Pawn CastingBody(Pawn caster)
        {
            if (!SpiritRebirthState.TryGet(caster, out var state)) return caster;
            return !state.DepletionPending && state.HasValidHost(caster) ? state.Host : null;
        }
        static DisciplineProgress Owned(Pawn pawn, bool burst)
        {
            var body = CastingBody(pawn);
            if (pawn == null || pawn.Dead || body?.Spawned != true || pawn.Faction == null
                || pawn.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true
                || !ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.brightmage");
            return path?.Domain == DisciplineDomain.Magic && path.Stage == (burst ? InitiationStage.Adept : InitiationStage.Foundation)
                && !path.CompletedTrials.Contains(burst ? MasteryTrialId : FoundationTrialId) && Owner(pawn, burst) != null ? path : null;
        }
        static bool RuntimeOwned(PawnAbility ability, Pawn caster, bool burst) => ability != null && ability.Pawn == caster
            && ability.Def == (burst ? TorannMagicDefOf.TM_LightBurst : TorannMagicDefOf.TM_LightLance)
            && ability.AbilityUser == Magic(caster) && Magic(caster)?.AbilityData.Powers.Any(p => ReferenceEquals(p, ability)) == true;
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool Enemy(Pawn caster, Pawn target) => target != null && target != caster && !target.Dead && target.Spawned
            && target.Map == caster.Map && !target.IsPrisoner && !target.IsSlave && !TM_Calc.IsUndead(target)
            && target.health?.hediffSet != null && target.HostileTo(caster)
            && (target.Faction == null ? target.InAggroMentalState : target.Faction != caster.Faction && target.Faction.HostileTo(caster.Faction));
        public sealed class LanceSource : IExposable
        {
            public int casterId;
            public string abilityId;
            internal MagicPower owner;
            public void ExposeData() { Scribe_Values.Look(ref casterId, "casterId", 0); Scribe_Values.Look(ref abilityId, "abilityId"); }
        }
        public sealed class SpawnSnapshot
        {
            internal Pawn caster;
            internal PawnAbility ability;
            internal MagicPower owner;
            internal DisciplineProgress path;
            internal bool used;
        }
        public static SpawnSnapshot CaptureSpawn(PawnAbility ability)
        {
            var caster = ability?.Pawn; var path = Owned(caster, false);
            return path != null && RuntimeOwned(ability, caster, false)
                ? new SpawnSnapshot { caster = caster, ability = ability, owner = Owner(caster, false), path = path } : null;
        }
        public static bool CompleteSpawn(SpawnSnapshot snapshot, FlyingObject_LightLance emitter)
        {
            if (snapshot == null || snapshot.used) return false;
            snapshot.used = true;
            if (emitter == null || !emitter.Spawned || emitter.def != TorannMagicDefOf.FlyingObject_LightLance || emitter.TrialCaster != snapshot.caster
                || emitter.Map != CastingBody(snapshot.caster)?.Map || Owned(snapshot.caster, false) != snapshot.path || Owner(snapshot.caster, false) != snapshot.owner
                || !RuntimeOwned(snapshot.ability, snapshot.caster, false)) return false;
            emitter.trialSource = new LanceSource { casterId = snapshot.caster.thingIDNumber, abilityId = "TM_LightLance", owner = snapshot.owner };
            return true;
        }
        static bool ValidSource(FlyingObject_LightLance emitter)
        {
            var tag = emitter?.trialSource; var caster = emitter?.TrialCaster;
            return tag != null && tag.casterId > 0 && tag.abilityId == "TM_LightLance" && caster?.thingIDNumber == tag.casterId
                && emitter.Spawned && emitter.Map == CastingBody(caster)?.Map && emitter.def == TorannMagicDefOf.FlyingObject_LightLance
                && (tag.owner == null || tag.owner == Owner(caster, false)) && Magic(caster)?.AbilityData.Powers.Any(a => RuntimeOwned(a, caster, false)) == true;
        }
        public sealed class DamageScope { internal FlyingObject_LightLance emitter; internal Thing victim; internal bool used; }
        public sealed class HitScope { internal DamageScope previous; internal Pawn caster; internal MagicPower owner; internal DisciplineProgress path; internal bool used; }
        [ThreadStatic] static DamageScope current;
        public static DamageScope EnterEmitter(FlyingObject_LightLance emitter, Thing victim)
        {
            var previous = current;
            current = ValidSource(emitter) ? new DamageScope { emitter = emitter, victim = victim } : null;
            return previous;
        }
        public static void ExitEmitter(DamageScope previous) => current = previous;
        public static HitScope BeginHit(Thing victim, DamageInfo damage)
        {
            var scope = current; if (scope == null) return null;
            current = null; var hit = new HitScope { previous = scope };
            if (scope.used || scope.victim != victim || !ValidSource(scope.emitter)
                || damage.Instigator != scope.emitter.TrialCaster || damage.Def != TMDamageDefOf.DamageDefOf.TM_BurningLight) return hit;
            scope.used = true; var caster = scope.emitter.TrialCaster; var path = Owned(caster, false);
            if (path == null || !Enemy(CastingBody(caster), victim as Pawn)) return hit;
            hit.caster = caster; hit.owner = Owner(caster, false); hit.path = path; return hit;
        }
        public static void CompleteHit(HitScope hit, DamageWorker.DamageResult result)
        {
            if (hit == null || hit.used) return;
            hit.used = true; float amount = result?.totalDamageDealt ?? 0;
            if (hit.path == null || !Finite(amount) || amount <= 0 || Owned(hit.caster, false) != hit.path || Owner(hit.caster, false) != hit.owner
                || Magic(hit.caster)?.AbilityData.Powers.Any(a => RuntimeOwned(a, hit.caster, false)) != true) return;
            var progress = hit.path.BrightmageTrials;
            progress.LanceDamage = Math.Min(20, progress.LanceDamage + amount);
            if (progress.LanceDamage >= 20) hit.path.RecordCompletedTrial(FoundationTrialId);
        }
        public static void EndHit(HitScope hit) { if (hit != null) current = hit.previous; }
        public sealed class BurstSnapshot
        {
            internal Pawn caster, target, body;
            internal PawnAbility ability;
            internal MagicPower owner;
            internal DisciplineProgress path;
            internal float severity;
            internal bool used;
        }
        public static BurstSnapshot CaptureBurst(PawnAbility ability, Pawn target)
        {
            var caster = ability?.Pawn; var path = Owned(caster, true);
            var body = CastingBody(caster);
            if (path == null || !RuntimeOwned(ability, caster, true) || !Enemy(body, target)) return null;
            var before = target.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightBurstHD)?.Severity ?? 0;
            return Finite(before) ? new BurstSnapshot { caster = caster, body = body, target = target, ability = ability, owner = Owner(caster, true), path = path, severity = before } : null;
        }
        public static bool CompleteBurst(BurstSnapshot snapshot)
        {
            if (snapshot == null || snapshot.used) return false;
            snapshot.used = true;
            if (Owned(snapshot.caster, true) != snapshot.path || Owner(snapshot.caster, true) != snapshot.owner
                || !RuntimeOwned(snapshot.ability, snapshot.caster, true) || CastingBody(snapshot.caster) != snapshot.body
                || !Enemy(snapshot.body, snapshot.target)) return false;
            float after = snapshot.target.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightBurstHD)?.Severity ?? 0;
            return Finite(after) && after > 0 && after > snapshot.severity && snapshot.path.RecordCompletedTrial(MasteryTrialId);
        }
    }
}
