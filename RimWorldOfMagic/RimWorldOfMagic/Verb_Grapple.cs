using AbilityUser;
using RimWorld;
using Verse;

namespace TorannMagic
{
    public class Verb_Grapple : Verb_UseAbility_TrueBurst
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            var body = CasterPawn;
            if (body?.Spawned != true || ClassProgression.ClassIdentity.Component(body)?.Circle != null) return false;
            if (target.Thing == body) return verbProps.targetParams.canTargetSelf;
            return target.IsValid && target.Cell.InBounds(body.Map) && !target.Cell.Fogged(body.Map) &&
                target.Cell.Walkable(body.Map) && (root-target.Cell).LengthHorizontal < verbProps.range &&
                TryFindShootLineFromTo(root,target,out _);
        }

        internal static Verb_Grapple ForHost(Pawn body,MightAbility ability) =>
            new Verb_Grapple { caster=body, Ability=ability, verbProps=ability.mightDef.MainVerb };

        internal bool FireFromHost(Pawn target)
        {
            if (target?.Spawned!=true || target.Map!=CasterPawn.Map || !CanHitTargetFrom(CasterPawn.Position,target)) return false;
            currentTarget=target;
            return TryCastShot();
        }

        protected override bool TryCastShot()
        {
            var body=CasterPawn;
            if (body?.Spawned!=true || !CanHitTargetFrom(body.Position,currentTarget)) return false;
            bool launched = false;
            UpdateTargets();
            if (UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetAoE && TargetsAoE.Count > 1)
                TargetsAoE.RemoveRange(0, TargetsAoE.Count - 1);
            foreach (var target in TargetsAoE)
            {
                if (TryLaunchProjectile(verbProps.defaultProjectile,target) == true) launched = true;
            }

            if (launched && currentTarget.Pawn is Pawn victim && victim.Spawned && victim.Map==body.Map)
            {
                var owner=Ability?.Pawn ?? body;
                if (!victim.RaceProps.Humanlike || victim.Faction==body.Faction ||
                    Rand.Chance(TM_Calc.GetSpellSuccessChance(owner,victim,true)))
                {
                    if (ModCheck.Validate.GiddyUp.Core_IsInitialized()) ModCheck.GiddyUp.ForceDismount(victim);
                    victim.Position=body.Position;
                    victim.Notify_Teleported();
                    victim.TakeDamage(new DamageInfo(DamageDefOf.Stun,10,10,-1,body,null,null,DamageInfo.SourceCategory.ThingOrUnknown,victim));
                }
                else MoteMaker.ThrowText(victim.DrawPos,victim.Map,"TM_ResistedSpell".Translate(),-1);
            }
            PostCastShot(launched,out var result);
            return result;
        }
    }
}
