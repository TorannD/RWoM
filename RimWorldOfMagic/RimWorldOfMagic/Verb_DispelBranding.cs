using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;
using TorannMagic.TMDefs;


namespace TorannMagic
{
    public class Verb_DispelBranding : ClassProgression.Verb_SpiritTargetable
    {
        protected override bool TryCastShot()
        {
            ApplyHostedEffect();
            bool result = false;
            PostCastShot(result,out result);
            return result;
        }

        protected override bool ApplyHostedEffect()
        {
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn,Ability);
            var magic = owner?.GetCompAbilityUserMagic();
            if (currentTarget.Pawn != CasterPawn || magic == null || magic.BrandPawns.Count == 0) return false;
            for (int i = ClassProgression.GolemancerLifecycle.AlignBrandPairs(magic)-1; i >= 0; i--)
            {
                var recipient = magic.BrandPawns[i];
                if (RemoveOwnedBrand(owner,recipient,magic.BrandDefs[i])) RemoveBrandHediffGraphics(recipient);
            }
            magic.BrandPawns.Clear();
            magic.BrandDefs.Clear();
            if (owner != CasterPawn) ClassProgression.GolemancerLifecycle.SyncBrandControl(magic);
            if (CasterPawn.Map != null)
            {
                RemoveBrandCasterGraphics();
                RemoveBrandCasterRingGraphics();
            }
            return true;
        }

        private void RemoveBrandCasterGraphics()
        {
            TargetInfo ti = new TargetInfo(CasterPawn.Position, CasterPawn.Map, false);
            TM_MoteMaker.MakeOverlay(ti, TorannMagicDefOf.TM_Mote_PsycastAreaEffect, CasterPawn.Map, Vector3.zero, 1f, 0f, .1f, .4f, 1.2f, -3f);
        }

        private void RemoveBrandCasterRingGraphics()
        {
            List<IntVec3> ring = TM_Calc.GetOuterRing(CasterPawn.Position, 1f, 2f);
            if (ring != null && ring.Count > 2)
            {
                for (int i = 0; i < 16; i++)
                {
                    Vector3 moteVec = ring.RandomElement().ToVector3Shifted();
                    moteVec.x += Rand.Range(-.5f, .5f);
                    moteVec.z += Rand.Range(-.5f, .5f);
                    float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(moteVec, CasterPawn.DrawPos)).ToAngleFlat();
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi_Yellow, moteVec, CasterPawn.Map, Rand.Range(.25f, .6f), .1f, .05f, .05f, 0, Rand.Range(4f, 6f), angle, angle);
                }
            }
            FleckMaker.ThrowLightningGlow(CasterPawn.DrawPos, CasterPawn.Map, 1.2f);
        }

        internal static bool RemoveOwnedBrand(Pawn caster, Pawn recipient, HediffDef def)
        {
            var effect = def == null ? null : recipient?.health?.hediffSet?.GetFirstHediffOfDef(def);
            if (caster == null || effect?.TryGetComp<HediffComp_BrandingBase>()?.BranderPawn != caster) return false;
            recipient.health.RemoveHediff(effect);
            return true;
        }

        private void RemoveBrandHediffGraphics(Pawn br)
        {
            if (br.Map != null && CasterPawn.Map != null && br.Map == CasterPawn.Map)
            {
                TargetInfo ti = new TargetInfo(br.Position, br.Map, false);
                TM_MoteMaker.MakeOverlay(ti, TorannMagicDefOf.TM_Mote_PsycastAreaEffect, br.Map, Vector3.zero, .2f, 0f, .1f, .4f, 1.2f, 3f);
            }
        }
    }
}
