using System.Linq;
using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.ClassProgression
{
    public sealed class Verb_BladePassage : Verb_UseAbility
    {
        public override float EffectiveRange => BladeCareer.PassageRange(Ability?.Pawn ?? CasterPawn);
        private bool crossed;
        public override void WarmupComplete()
        {
            crossed = false;
            base.WarmupComplete();
            // The generic cast just installed its stat-adjusted cooldown stance.
            // Passage recovery is a fixed 60 ticks, independently of the weapon or ranged cooldown stat.
            if (crossed && CasterPawn?.Spawned == true && !CasterPawn.Downed && !CasterPawn.Dead)
                CasterPawn.stances.SetStance(new Stance_Cooldown(60, currentTarget, this));
        }

        internal static Verb_MeleeAttackDamage Strike(Pawn pawn)
        {
            var weapon = pawn.equipment?.Primary;
            if (weapon?.def.IsMeleeWeapon != true) return null;
            return weapon.TryGetComp<CompEquippable>()?.AllVerbs.OfType<Verb_MeleeAttackDamage>()
                .Where(v => v.tool != null && v.verbProps.meleeDamageDef != null)
                .OrderByDescending(v => v.tool.power).FirstOrDefault();
        }

        private static bool Landing(Pawn actor, IntVec3 root, Pawn victim, out IntVec3 cell)
            => LandingFrom(actor,actor,root,victim,out cell);

        private static bool LandingFrom(Pawn actor,Pawn owner,IntVec3 root,Pawn victim,out IntVec3 cell)
        {
            cell = root;
            int range = BladeCareer.PassageRange(owner);
            if (range <= 0 || actor?.Spawned != true || actor.Dead || actor.Downed ||
                victim?.Spawned != true || victim.Map != actor.Map || victim.Dead || victim.Downed || victim.IsPrisoner ||
                !victim.HostileTo(actor) || !root.InBounds(actor.Map) || root.AdjacentTo8WayOrInside(victim.Position) ||
                root.DistanceToSquared(victim.Position) > range * range || victim.Position.Fogged(actor.Map) ||
                !GenSight.LineOfSight(root, victim.Position, actor.Map)) return false;

            // Walk every crossed cell. A clear line of fire can still cross empty space or a closed door.
            foreach (var step in GenSight.PointsOnLineOfSight(root, victim.Position))
            {
                if (step == root) continue;
                if (step == victim.Position) break;
                if (step.Fogged(actor.Map) || !step.WalkableBy(actor.Map, actor) ||
                    step.GetDoor(actor.Map) is Building_Door door && !door.Open ||
                    step.GetFirstPawn(actor.Map) != null) return false;
                cell = step;
            }
            return cell != root && cell.StandableBy(actor.Map, actor);
        }

        internal bool TryDestination(LocalTargetInfo target, out IntVec3 cell) =>
            LandingFrom(CasterPawn,Ability?.Pawn ?? CasterPawn,CasterPawn.Position,target.Pawn,out cell);

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target) =>
            LandingFrom(CasterPawn,Ability?.Pawn ?? CasterPawn,root,target.Pawn,out _) && Strike(CasterPawn) != null;

        internal static Verb_BladePassage For(Pawn body,MightAbility source) =>
            new Verb_BladePassage { caster=body,Ability=source,verbProps=source.mightDef.MainVerb };

        internal bool Execute(LocalTargetInfo target)
        {
            currentTarget=target;
            if (!TryCastShot()) return false;
            if(CasterPawn.Spawned && !CasterPawn.Downed && !CasterPawn.Dead)
                CasterPawn.stances.SetStance(new Stance_Cooldown(60,target,null));
            return true;
        }

        protected override bool TryCastShot()
        {
            var actor = CasterPawn;
            var victim = currentTarget.Pawn;
            if (actor == null || !TryDestination(currentTarget,out var landing)) return false;
            Odyssey.ManualTravelSafety.BeginAttempt(Ability);
            bool canPay=Ability is MightAbility power && power.Pawn!=actor
                ? SpiritHostedMartial.Find(actor,power.Pawn,BladeMartialCast.Passage)==power &&
                    SpiritHostedMartial.Ready(actor,power,BladeMartialCast.Passage)
                : Odyssey.ManualTravelSafety.CanPay(Ability);
            if (!canPay || !Odyssey.ManualTravelSafety.CanComplete(actor,actor.Map,landing)) return false;
            var strike = Strike(actor);
            if (strike == null) return false;
            var damage = new DamageInfo(strike.verbProps.meleeDamageDef,
                Mathf.Max(1f, .9f * strike.verbProps.AdjustedMeleeDamageAmount(strike, actor)),
                strike.verbProps.AdjustedArmorPenetration(strike, actor), instigator: actor, weapon: actor.equipment.Primary.def);
            var origin = actor.Position;
            actor.Position = landing;
            actor.Notify_Teleported(endCurrentJob: false);
            crossed = true;
            Odyssey.ManualTravelSafety.Settle(Ability);
            if (victim.Spawned && victim.Map == actor.Map && !victim.Dead && !victim.Downed &&
                !victim.IsPrisoner && victim.HostileTo(actor) && actor.Position.AdjacentTo8WayOrInside(victim.Position) &&
                victim.TakeDamage(damage).totalDamageDealt > 0)
            {
                var owner=Ability?.Pawn ?? actor;
                BladeCareer.Pass(owner);
                BladeAscension.StartRouteFrom(owner,actor,victim,origin);
            }
            return true;
        }
    }
}
