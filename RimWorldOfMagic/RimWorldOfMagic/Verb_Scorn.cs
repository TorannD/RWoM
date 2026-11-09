using System;
using AbilityUser;
using RimWorld.Planet;
using TorannMagic.ClassProgression;
using TorannMagic.Odyssey;
using Verse;
using Verse.AI;

namespace TorannMagic
{
    public class Verb_Scorn : Verb_BLOS
    {
        internal static bool Submit(Pawn body, Job job, LocalTargetInfo target, bool player, Func<bool> validate, Func<bool> submit)
        {
            if (body?.Spawned != true || job == null) return false;
            var map = body.Map;
            var landing = Projectile_Scorn.LandingCell(target.Cell,map);
            job.globalTarget = new GlobalTargetInfo(landing,map);
            bool accepted = false;
            void Accept()
            {
                var cargo = body.carryTracker?.CarriedThing;
                job.targetC = cargo;
                // The old job and the next vanilla job can both drop carried things.
                // Inventory remains a durable owner while the accepted order warms up.
                bool stowed = player && cargo != null;
                if (stowed && (body.inventory == null || !body.carryTracker.innerContainer.TryTransferToContainer(
                    cargo,body.inventory.innerContainer,false))) return;
                try { accepted = submit(); }
                finally
                {
                    if (!accepted && stowed && body.inventory.innerContainer.Contains(cargo))
                        body.inventory.innerContainer.TryTransferToContainer(cargo,body.carryTracker.innerContainer,false);
                }
            }
            return ManualTravelSafety.Submit(body,job,map,landing,player,
                () => validate() && body.Map == map && body.Position != landing && Projectile_Scorn.Open(map,landing),Accept,180) && accepted;
        }

        internal static Job Order(MagicAbility power, AbilityContext context, LocalTargetInfo target)
        {
            var body = power.Pawn;
            bool Valid() => body?.Spawned == true && target.IsValid && power.CanCastPowerCheck(context,out _) &&
                power.Verb.CanHitTargetFrom(body.Position,target) && (!target.HasThing || power.CanOverpowerTarget(context,target,out _)) &&
                (context != AbilityContext.AI || Compatibility.AbilityCastSafety.CanAutocast(power.Verb,target));
            if (!Valid()) return null;
            var job = power.GetJob(context,target);
            bool player = context == AbilityContext.Player;
            return Submit(body,job,target,player,Valid,() => !player || body.jobs.TryTakeOrderedJob(job)) ? job : null;
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            if (CasterPawn?.Spawned != true || !base.CanHitTargetFrom(root,target)) return false;
            var cell = Projectile_Scorn.LandingCell(target.Cell,CasterPawn.Map);
            return root != cell && Projectile_Scorn.Open(CasterPawn.Map,cell);
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (CasterPawn?.Spawned != true) return;
            if (!target.IsValid) { base.DrawHighlight(target); return; }
            var cell = Projectile_Scorn.LandingCell(target.Cell,CasterPawn.Map);
            if (cell.IsValid) base.DrawHighlight(cell);
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = false;
            TargetAoEProperties targetAoEProperties = UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties;
            if (targetAoEProperties == null || !targetAoEProperties.showRangeOnSelect)
            {
                CompAbilityUserMagic comp = SpiritRebirthCasting.SkillOwner(CasterPawn,Ability)?.GetCompAbilityUserMagic();
                float adjustedRadius = verbProps.defaultProjectile?.projectile?.explosionRadius ?? 1f;
                if (comp != null && comp.MagicData != null)
                {
                    int verVal = TM_Calc.GetSkillVersatilityLevel(comp.Pawn, this.Ability.Def as TMAbilityDef);
                    adjustedRadius += verVal;
                }
                return adjustedRadius;
            }
            return (float)targetAoEProperties.range;
        }
    }
}
