using Verse;
using AbilityUser;

namespace TorannMagic
{    
    public class Effect_ValiantCharge : Verb_UseAbility
    {
        bool validTarg;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.Cell == root)
            {
                return false;
            }
            if ( targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            { //targ.Cell.DistanceToEdge(base.CasterPawn.Map) >= 4 &&
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    ShootLine shootLine;
                    validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
                }
                else
                {
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            TargetAoEProperties targetAoEProperties = UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties;
            if (targetAoEProperties == null || !targetAoEProperties.showRangeOnSelect)
            {
                var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn, Ability);
                var comp = owner?.GetCompAbilityUserMagic();
                float adjustedRadius = verbProps.defaultProjectile?.projectile?.explosionRadius ?? 1.2f;
                if (comp?.MagicData != null)
                {
                    int verVal = owner == CasterPawn ? TM_Calc.GetSkillVersatilityLevel(owner, Ability.Def as TMAbilityDef)
                        : ClassProgression.Projectile_SpiritBolt.SpellRank(owner, comp.MagicData.MagicPowerSkill_ValiantCharge, "TM_ValiantCharge", "_ver");
                    adjustedRadius = 1.2f + .8f * verVal;
                }
                return adjustedRadius;
            }
            return (float)targetAoEProperties.range;
        }

        public virtual void Effect()
        {
            if (TargetsAoE.Count == 0) return;
            Pawn body = CasterPawn;
            Map map = body.Map;
            LocalTargetInfo target = TargetsAoE[0];
            LongEventHandler.QueueLongEvent(() => {
                if (body.Spawned && body.Map == map) StartCharge(body, body, target);
            }, "LaunchingFlyer", false, null);
        }

        internal static bool StartCharge(Pawn body, Pawn owner, LocalTargetInfo target)
        {
            if (body?.Spawned != true || !target.IsValid || target.Cell == body.Position ||
                !target.Cell.InBounds(body.Map) || !target.Cell.Walkable(body.Map)) return false;
            if (ModCheck.Validate.GiddyUp.Core_IsInitialized()) ModCheck.GiddyUp.ForceDismount(body);
            var flyer = (FlyingObject_ValiantCharge)GenSpawn.Spawn(ThingDef.Named("FlyingObject_ValiantCharge"), body.Position, body.Map);
            if (flyer.Begin(body, owner, body.Position.ToVector3Shifted(), target)) return true;
            flyer.Destroy();
            return false;
        }

        public override void PostCastShot(bool inResult, out bool outResult)
        {
            if (inResult)
            {
                this.Effect();
                outResult = true;
            }
            outResult = inResult;
        }
    }    
}
