using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    public class Verb_SigilDrain : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            bool flag = false;
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();

            if (comp.IsMagicUser)
            {
                comp.sigilDraining = !comp.sigilDraining;
                List<IntVec3> ring = TM_Calc.GetOuterRing(caster.Position, 1f, 2f);
                for (int i =0; i < 12; i++)
                {
                    Vector3 moteVec =  ring.RandomElement().ToVector3Shifted();
                    moteVec.x += Rand.Range(-.4f, .4f);
                    moteVec.z += Rand.Range(-.4f, .4f);
                    float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(moteVec, caster.DrawPos)).ToAngleFlat();
                    ThingDef mote = TorannMagicDefOf.Mote_Psi_Grayscale;
                    mote.graphicData.color = Color.white;
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi_Yellow, moteVec, caster.Map, Rand.Range(.25f, .6f), .1f, .05f, .05f, 0, Rand.Range(4f, 6f), angle, angle);
                }
                FleckMaker.ThrowLightningGlow(caster.DrawPos, caster.Map, 1.2f);
            }

            this.PostCastShot(flag, out flag);
            return flag;
        }
    }
}