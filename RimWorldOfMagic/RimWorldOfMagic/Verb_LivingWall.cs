using System.Collections.Generic;
using RimWorld;
using TorannMagic.ClassProgression;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public class Verb_LivingWall : Verb_SpiritTargetable
    {
        internal static Thing Segment(Pawn body, LocalTargetInfo target)
        {
            if (body?.Spawned != true || !target.IsValid || !target.Cell.InBounds(body.Map)) return null;
            if (target.HasThing) return target.Thing.Map == body.Map && target.Thing.Faction == body.Faction &&
                TM_Calc.IsWall(target.Thing) ? target.Thing : null;
            return target.Cell.GetThingList(body.Map).Find(t => t.Faction == body.Faction && TM_Calc.IsWall(t));
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
            => target.IsValid && target.Cell.InBounds(CasterPawn.Map) && !target.Cell.Fogged(CasterPawn.Map)
                && (root-target.Cell).LengthHorizontal < verbProps.range;

        protected override bool TryCastShot()
        {
            bool applied = ApplyHostedEffect();
            if (!applied) Messages.Message("TM_InvalidTarget".Translate(CasterPawn.LabelShort, Ability.Def.label), MessageTypeDefOf.NegativeEvent);
            return applied;
        }

        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            var owner = SpiritRebirthCasting.SkillOwner(body, Ability);
            var magic = owner?.GetCompAbilityUserMagic();
            var segment = Segment(body, currentTarget);
            if (magic == null || segment == null) return false;
            int versatility = TM_Calc.GetSkillVersatilityLevel(owner, TorannMagicDefOf.TM_LivingWall);
            var position = segment.Position.ToVector3Shifted();
            var old = magic.livingWall;
            var wall = (FlyingObject_LivingWall)GenSpawn.Spawn(TorannMagicDefOf.FlyingObject_LivingWall,segment.Position,body.Map);
            try
            {
                wall.ExactLaunch(null,0,false,new List<Vector3> {position,position},owner,position,segment,
                    new Thing { def = TorannMagicDefOf.FlyingObject_LivingWall },15+3*versatility,0);
                wall.BindToCasterBody(body);
            }
            catch
            {
                if (!wall.Destroyed) wall.Destroy(DestroyMode.Vanish);
                throw;
            }
            magic.livingWall = wall;
            if (!old.DestroyedOrNull() && old.CasterPawn == owner) old.Destroy(DestroyMode.Vanish);
            GolemancerLifecycle.SyncWallControl(magic);
            FleckMaker.ThrowLightningGlow(segment.DrawPos, body.Map, 1f);
            return true;
        }
    }
}
