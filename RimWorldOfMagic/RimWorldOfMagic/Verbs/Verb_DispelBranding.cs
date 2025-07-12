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
    public class Verb_DispelBranding : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            bool flag = false;
            CompAbilityUserMagic comp = CasterPawn.GetCompAbilityUserMagic();

            if (comp != null && comp.IsMagicUser && comp.BrandPawns != null && comp.BrandDefs != null)
            {
                if (comp.BrandPawns.Count > 0)
                {
                    for(int i=0; i < comp.BrandPawns.Count; i++)
                    {
                        Pawn br = comp.BrandPawns[i];
                        if (br != null && !br.DestroyedOrNull() && br.health != null && br.health.hediffSet != null)
                        {
                            RemoveBrandHediff(comp, br, i);
                            RemoveBrandHediffGraphics(br);
                        }
                    }
                    comp.BrandPawns.Clear();
                    comp.BrandDefs.Clear();
                    if (CasterPawn.Map != null)
                    {
                        RemoveBrandCasterGraphics();
                        RemoveBrandCasterRingGraphics();                        
                        
                        //Effecter effectExit = EffecterDefOf.Skip_EntryNoDelay.Spawn();
                        //effectExit.Trigger(new TargetInfo(this.CasterPawn), new TargetInfo(this.CasterPawn));
                        //effectExit.Cleanup();                        
                    }                    
                }
            }

            this.PostCastShot(flag, out flag);
            return flag;
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

        private void RemoveBrandHediff(CompAbilityUserMagic comp, Pawn br, int i)
        {
            if (comp.BrandPawns.Count == comp.BrandDefs.Count)
            {
                Hediff hd = br.health.hediffSet.GetFirstHediffOfDef(comp.BrandDefs[i]);
                if (hd != null)
                {
                    br.health.RemoveHediff(hd);
                }
            }
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
