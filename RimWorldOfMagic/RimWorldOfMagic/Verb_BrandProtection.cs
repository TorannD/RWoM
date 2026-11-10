using Verse;
using UnityEngine;

namespace TorannMagic
{
    public class Verb_BrandProtection : Verb_Brand
    {
        protected override HediffDef Effect => TorannMagicDefOf.TM_ProtectionBrandHD;
        protected override void DrawBrand(Pawn hitPawn)
        {
            TargetInfo ti = new TargetInfo(hitPawn.Position, hitPawn.Map, false);
            TM_MoteMaker.MakeOverlay(ti, TorannMagicDefOf.TM_Mote_PsycastAreaEffect, hitPawn.Map, Vector3.zero, 1f, 0f, .1f, .3f, .3f, -2f);

            for (int i = 0; i < 15; i++)
            {
                Vector3 drawPos = hitPawn.DrawPos;
                drawPos.x += Rand.Range(-.6f, .6f);
                drawPos.z += Rand.Range(-.6f, .6f);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_GlowingRuneA, drawPos, hitPawn.Map, Rand.Range(.05f, .15f), Rand.Range(.2f, .3f), Rand.Range(.1f, .25f), Rand.Range(.2f, .7f), Rand.Range(-20, 20), 0f, 0f, Rand.Range(0, 360));
            }
        }
    }
}
