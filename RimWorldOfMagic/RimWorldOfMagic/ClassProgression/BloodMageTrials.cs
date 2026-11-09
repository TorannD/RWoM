using System;
using System.Linq;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public sealed class BloodMageTrialProgress : IExposable
    {
        public float IgniteDamage;
        public void ExposeData()
        {
            Scribe_Values.Look(ref IgniteDamage, "igniteDamage", 0f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                IgniteDamage = float.IsNaN(IgniteDamage) || float.IsInfinity(IgniteDamage) ? 0 : Math.Max(0, Math.Min(20, IgniteDamage));
        }
    }
    public static class BloodMageTrials
    {
        public const string FoundationTrialId = "rwom.bloodmage.foundation", MasteryTrialId = "rwom.bloodmage.mastery";
        static CompAbilityUserMagic Magic(Pawn pawn) => pawn?.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
        static MagicPower Owner(Pawn pawn, bool ignite) => Magic(pawn)?.MagicData.MagicPowersBM.FirstOrDefault(p => p.learned && p.TMabilityDefs.Contains(ignite ? TorannMagicDefOf.TM_IgniteBlood : TorannMagicDefOf.TM_BloodShield));
        private static Pawn ShieldBody(Pawn owner)
        {
            if (!SpiritRebirthState.TryGet(owner,out var state)) return owner?.Spawned == true ? owner : null;
            return !state.DepletionPending && state.HasValidHost(owner) && state.Host.Spawned ? state.Host : null;
        }
        static DisciplineProgress Owned(Pawn pawn, bool ignite, Map effectMap = null)
        {
            if (pawn == null || pawn.Dead || (ignite ? !pawn.Spawned && (effectMap == null || !SpiritRebirthState.TryGet(pawn,out _)) : ShieldBody(pawn) == null) || pawn.Faction == null
                || pawn.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true
                || !ClassIdentity.TryGetState(pawn, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.bloodmage");
            return path?.Domain == DisciplineDomain.Magic && path.Stage == (ignite ? InitiationStage.Adept : InitiationStage.Foundation)
                && !path.CompletedTrials.Contains(ignite ? MasteryTrialId : FoundationTrialId) && Owner(pawn, ignite) != null ? path : null;
        }
        static bool RuntimeOwned(PawnAbility ability, Pawn caster, bool ignite) => ability != null && ability.Pawn == caster
            && ability.Def == (ignite ? TorannMagicDefOf.TM_IgniteBlood : TorannMagicDefOf.TM_BloodShield)
            && ability.AbilityUser == Magic(caster) && Magic(caster)?.AbilityData.Powers.Any(p => ReferenceEquals(p, ability)) == true;
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool Living(Pawn caster, Pawn target, Map map = null) => target != null && target != caster && !target.Dead && target.Spawned
            && target.Map == (map ?? caster.Map) && !target.IsPrisoner && !target.IsSlave && !TM_Calc.IsUndead(target) && target.health?.hediffSet != null;
        static bool Ally(Pawn caster, Pawn target) => Living(caster, target) && target.RaceProps?.Humanlike == true && target.Faction == caster.Faction;
        static Hediff Shield(Pawn pawn) => pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.def.defName == "TM_BloodShieldHD");
        public sealed class ShieldSnapshot
        {
            internal Pawn caster, target, body;
            internal PawnAbility ability;
            internal MagicPower owner;
            internal DisciplineProgress path;
            internal float severity;
            internal bool used;
        }
        public static ShieldSnapshot CaptureShield(PawnAbility ability, Pawn target)
        {
            var caster = ability?.Pawn;
            var path = Owned(caster, false);
            if (path == null || !RuntimeOwned(ability, caster, false) || !Ally(ShieldBody(caster), target)
                || !target.health.hediffSet.hediffs.OfType<Hediff_Injury>().Any(h => Finite(h.Severity) && h.Severity > 0)) return null;
            float before = Shield(target)?.Severity ?? 0;
            return Finite(before) ? new ShieldSnapshot { caster = caster, body = ShieldBody(caster), target = target, ability = ability, owner = Owner(caster, false), path = path, severity = before } : null;
        }
        public static bool CompleteShield(ShieldSnapshot snapshot)
        {
            if (snapshot == null || snapshot.used) return false;
            snapshot.used = true;
            if (Owned(snapshot.caster, false) != snapshot.path || Owner(snapshot.caster, false) != snapshot.owner
                || !RuntimeOwned(snapshot.ability, snapshot.caster, false) || ShieldBody(snapshot.caster) != snapshot.body || !Ally(snapshot.body, snapshot.target)) return false;
            var shield = Shield(snapshot.target);
            return shield != null && Finite(shield.Severity) && shield.Severity > 0 && shield.Severity > snapshot.severity
                && shield.TryGetComp<HediffComp_BloodShield>()?.linkedPawn == snapshot.body && snapshot.path.RecordCompletedTrial(FoundationTrialId);
        }
        public sealed class Tag : IExposable
        {
            public int casterId;
            public string abilityId;
            internal bool projected;
            internal MagicPower owner;
            public void ExposeData() { Scribe_Values.Look(ref casterId, "casterId", 0); Scribe_Values.Look(ref abilityId, "abilityId"); Scribe_Values.Look(ref projected,"TM_projected"); }
        }
        static bool Valid(Tag tag) => tag != null && tag.casterId > 0 && tag.abilityId == "TM_IgniteBlood";
        static readonly ConditionalWeakTable<Projectile_AbilityBase, Tag> projectileTags = new ConditionalWeakTable<Projectile_AbilityBase, Tag>();
        static readonly ConditionalWeakTable<Explosion, Tag> explosionTags = new ConditionalWeakTable<Explosion, Tag>();
        [ThreadStatic] static Tag launching;
        public static Tag BeginLaunch(Verb_UseAbility verb, ThingDef projectileDef)
        {
            var previous = launching; launching = null;
            if (projectileDef?.defName == "Projectile_IgniteBlood" && Owned(verb?.CasterPawn, true) != null && RuntimeOwned(verb.Ability, verb.CasterPawn, true))
                launching = new Tag { casterId = verb.CasterPawn.thingIDNumber, abilityId = "TM_IgniteBlood", owner = Owner(verb.CasterPawn, true) };
            return previous;
        }
        public static void EndLaunch(Tag previous) => launching = previous;
        public static void Launched(Projectile_AbilityBase projectile, Thing caster, AbilityUser.AbilityDef ability)
        {
            var tag = launching; launching = null; projectileTags.Remove(projectile);
            if (Valid(tag) && projectile is Projectile_IgniteBlood && projectile.def?.defName == "Projectile_IgniteBlood"
                && caster?.thingIDNumber == tag.casterId && ability == TorannMagicDefOf.TM_IgniteBlood) projectileTags.Add(projectile, tag);
        }
        internal static void StampHosted(Projectile_IgniteBlood field, MagicAbility ability)
        {
            var owner = ability.Pawn;
            if (field.SpellOwner != owner || !SpiritRebirthState.TryGetPossessingSpirit(field.Caster, out var held) ||
                held != owner || !RuntimeOwned(ability,owner,true) || Owned(owner,true,field.Map) == null) return;
            projectileTags.Remove(field);
            projectileTags.Add(field,new Tag { casterId=owner.thingIDNumber, abilityId=ability.Def.defName, owner=Owner(owner,true), projected=true });
        }
        static Tag Source(Projectile_AbilityBase projectile)
        {
            if (projectile == null) return null;
            projectileTags.TryGetValue(projectile, out var tag);
            return Valid(tag) && projectile is Projectile_IgniteBlood field && field.SpellOwner?.thingIDNumber == tag.casterId
                && projectile.def?.defName == "Projectile_IgniteBlood" && projectile.localAbilityDef == TorannMagicDefOf.TM_IgniteBlood
                && (tag.owner == null || tag.owner == Owner(field.SpellOwner, true)) ? tag : null;
        }
        public static void ExposeProjectile(Projectile_AbilityBase projectile)
        {
            projectileTags.TryGetValue(projectile, out var tag);
            if (Scribe.mode == LoadSaveMode.Saving && tag == null) return;
            Scribe_Deep.Look(ref tag, "TM_bloodMageTrialSource");
            if (Scribe.mode != LoadSaveMode.Saving) { projectileTags.Remove(projectile); if (Valid(tag)) projectileTags.Add(projectile, tag); }
        }
        public sealed class CreationScope { internal Tag tag; internal Map map; internal IntVec3 center; internal Pawn caster; }
        [ThreadStatic] static CreationScope creating;
        public static CreationScope BeginExplosion(Projectile_AbilityBase projectile, IntVec3 center)
        {
            var previous = creating; var tag = Source(projectile);
            var caster = (projectile as Projectile_IgniteBlood)?.SpellOwner;
            creating = tag != null && Owned(caster,true,projectile.Map) != null ? new CreationScope { tag = tag, map = projectile.Map, center = center, caster = caster } : null;
            return previous;
        }
        public static void EndExplosion(CreationScope previous) => creating = previous;
        public static void StartExplosion(Explosion explosion)
        {
            var scope = creating; creating = null;
            if (scope != null && explosion.instigator == scope.caster && explosion.Map == scope.map && explosion.Position == scope.center
                && explosion.damType == TMDamageDefOf.DamageDefOf.TM_BloodBurn)
            { explosionTags.Remove(explosion); explosionTags.Add(explosion, scope.tag); }
        }
        public static void ExposeExplosion(Explosion explosion)
        {
            explosionTags.TryGetValue(explosion, out var tag);
            if (Scribe.mode == LoadSaveMode.Saving && tag == null) return;
            Scribe_Deep.Look(ref tag, "TM_bloodMageTrialSource");
            if (Scribe.mode != LoadSaveMode.Saving) { explosionTags.Remove(explosion); if (Valid(tag)) explosionTags.Add(explosion, tag); }
        }
        public sealed class DamageScope { internal Explosion explosion; internal Thing victim; internal Tag tag; internal bool used; }
        public sealed class HitScope { internal DamageScope previous; internal Pawn caster; internal MagicPower owner; internal DisciplineProgress path; internal bool consumed; }
        [ThreadStatic] static DamageScope current;
        public static DamageScope EnterExplosion(Explosion explosion, Thing victim)
        {
            var previous = current; explosionTags.TryGetValue(explosion, out var tag);
            current = Valid(tag) ? new DamageScope { explosion = explosion, victim = victim, tag = tag } : null;
            return previous;
        }
        public static void ExitExplosion(DamageScope previous) => current = previous;
        public static HitScope BeginHit(Thing victim, DamageInfo damage)
        {
            var scope = current; if (scope == null) return null;
            current = null; var hit = new HitScope { previous = scope };
            if (scope.used || scope.victim != victim || damage.Instigator != scope.explosion.instigator || damage.Instigator?.thingIDNumber != scope.tag.casterId
                || damage.Def != scope.explosion.damType || damage.Def != TMDamageDefOf.DamageDefOf.TM_BloodBurn) return hit;
            scope.used = true;
            var caster = damage.Instigator as Pawn; var target = victim as Pawn; var map = scope.tag.projected ? scope.explosion.Map : null; var path = Owned(caster, true, map);
            if (path == null || (scope.tag.owner != null && scope.tag.owner != Owner(caster, true)) || !Living(caster, target, map) || !target.HostileTo(caster)
                || (target.Faction == null ? !target.InAggroMentalState : target.Faction == caster.Faction || !target.Faction.HostileTo(caster.Faction))) return hit;
            hit.caster = caster; hit.owner = Owner(caster, true); hit.path = path; return hit;
        }
        public static void CompleteHit(HitScope hit, DamageWorker.DamageResult result)
        {
            if (hit == null || hit.consumed) return;
            hit.consumed = true;
            float damage = result?.totalDamageDealt ?? 0;
            if (hit.path == null || !Finite(damage) || damage <= 0 || Owned(hit.caster, true, hit.previous.tag.projected ? hit.previous.explosion.Map : null) != hit.path || Owner(hit.caster, true) != hit.owner) return;
            var progress = hit.path.BloodMageTrials;
            progress.IgniteDamage = Math.Min(20, progress.IgniteDamage + damage);
            if (progress.IgniteDamage >= 20) hit.path.RecordCompletedTrial(MasteryTrialId);
        }
        public static void EndHit(HitScope hit) { if (hit != null) current = hit.previous; }
    }
}
