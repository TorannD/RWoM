using Verse;
using RimWorld;
using TorannMagic.ClassProgression;
using UnityEngine;

namespace TorannMagic
{
    public abstract class Verb_Brand : Verb_SpiritTargetable
    {
        protected abstract HediffDef Effect { get; }
        internal virtual bool Accepts(Pawn body, Pawn target) => target != null && !target.Dead && target != body &&
            target.Spawned && target.Map == body.Map && target.health?.hediffSet != null;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
            => target.IsValid && target.Cell.InBounds(CasterPawn.Map) && !target.Cell.Fogged(CasterPawn.Map)
                && (root - target.Cell).LengthHorizontal < verbProps.range;

        protected override bool TryCastShot()
        {
            if (!ApplyHostedEffect())
                Messages.Message("TM_InvalidTarget".Translate(CasterPawn.LabelShort, Ability.Def.label), MessageTypeDefOf.RejectInput);
            bool result = false;
            PostCastShot(result, out result);
            return result;
        }

        protected override bool ApplyHostedEffect()
        {
            var owner = SpiritRebirthCasting.SkillOwner(CasterPawn, Ability);
            var magic = owner?.GetCompAbilityUserMagic();
            var target = currentTarget.Pawn;
            if (magic == null || !Accepts(CasterPawn, target)) return false;
            var trial = GolemancerTrials.CaptureBrand(Ability, target);
            TM_Action.UpdateBrand(target, owner, magic, Effect);
            var marker = target.health.hediffSet.GetFirstHediffOfDef(Effect)?.TryGetComp<HediffComp_BrandingBase>();
            if (marker == null) return false;
            marker.BranderPawn = owner;
            GolemancerTrials.CompleteBrand(trial);
            if (owner != CasterPawn) GolemancerLifecycle.SyncBrandControl(magic);
            DrawBrand(target);
            return true;
        }

        protected virtual void DrawBrand(Pawn target)
        {
            TM_MoteMaker.MakeOverlay(new TargetInfo(target.Position, target.Map), TorannMagicDefOf.TM_Mote_PsycastAreaEffect,
                target.Map, Vector3.zero, 1f, 0f, .1f, .4f, 1.2f, -3f);
        }
    }

    public class Verb_BrandAwareness : Verb_Brand
    {
        protected override HediffDef Effect => TorannMagicDefOf.TM_AwarenessBrandHD;

    }
}
