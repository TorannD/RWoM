using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_DispelBranding : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            bool flag = false;
            CompAbilityUserMagic comp = this.CasterPawn.GetComp<CompAbilityUserMagic>();

            if (comp != null && comp.IsMagicUser && comp.BrandedPawns != null)
            {
                if (comp.BrandedPawns.Count > 0)
                {
                    foreach(Pawn br in comp.BrandedPawns)
                    {
                        if (br.health != null && br.health.hediffSet != null)
                        {
                            List<Hediff> brands = new List<Hediff>();
                            brands.Clear();
                            foreach (Hediff hd in br.health.hediffSet.hediffs)
                            {
                                HediffComp_BrandingBase hc_bb = hd.TryGetComp<HediffComp_BrandingBase>();
                                if (hc_bb != null && hc_bb.BranderPawn == CasterPawn)
                                {
                                    brands.Add(hd);
                                }
                            }

                            if (brands != null && brands.Count > 0)
                            {
                                foreach (Hediff h in brands)
                                {
                                    br.health.RemoveHediff(h);
                                }
                            }
                            if (br.Map != null && caster.Map != null && br.Map == caster.Map)
                            {
                                Effecter effect = EffecterDefOf.Skip_ExitNoDelay.Spawn();
                                effect.Trigger(new TargetInfo(br), new TargetInfo(caster));
                                effect.Cleanup();
                            }
                        }
                    }
                    comp.brandedPawns.Clear();
                    if (CasterPawn.Map != null)
                    {
                        Effecter effectExit = EffecterDefOf.Skip_EntryNoDelay.Spawn();
                        effectExit.Trigger(new TargetInfo(this.CasterPawn), new TargetInfo(this.CasterPawn));
                        effectExit.Cleanup();

                        List<IntVec3> ring = TM_Calc.GetOuterRing(caster.Position, 1f, 2f);
                        for (int i = 0; i < 16; i++)
                        {
                            Vector3 moteVec = ring.RandomElement().ToVector3Shifted();
                            moteVec.x += Rand.Range(-.5f, .5f);
                            moteVec.z += Rand.Range(-.5f, .5f);
                            float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(moteVec, caster.DrawPos)).ToAngleFlat();
                            ThingDef mote = TorannMagicDefOf.Mote_Psi_Grayscale;
                            mote.graphicData.color = Color.white;
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi_Yellow, moteVec, caster.Map, Rand.Range(.25f, .6f), .1f, .05f, .05f, 0, Rand.Range(4f, 6f), angle, angle);
                        }
                        FleckMaker.ThrowLightningGlow(caster.DrawPos, caster.Map, 1.2f);
                    }                    
                }
            }

            this.PostCastShot(flag, out flag);
            return flag;
        }
    }
}
