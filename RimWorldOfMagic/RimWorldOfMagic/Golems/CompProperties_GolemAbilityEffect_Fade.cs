using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using TorannMagic.TMDefs;
using UnityEngine;

namespace TorannMagic.Golems
{
    public class CompProperties_GolemAbilityEffect_Fade : CompProperties_GolemAbilityEffect
    {
        public float radius = 1f;
        public float hitChance = 1f;
        public float minDistance = 10f;
        public float maxDistance = 200f;
        public float severity80 = .5f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.radius, "radius", 0f);
            Scribe_Values.Look<float>(ref this.hitChance, "hitChance", 1f);
        }

        public void ApplyFadeTouch(Pawn caster, IntVec3 cell, float effectBonus)
        {
            List<Pawn> enemyPawns = TM_Calc.FindAllHostilePawnsAround(caster.Map, cell, radius, caster.Faction);
            if (enemyPawns != null && enemyPawns.Count > 0)
            {
                foreach (Pawn p in enemyPawns)
                {
                    if (Rand.Chance(hitChance))
                    {
                        BodyPartRecord hitPart = null;
                        if (p != null && p.health != null && p.health.hediffSet != null)
                        {
                            List<BodyPartRecord> outsideParts = p.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Outside).Where(def => def.coverage > 0).ToList();
                            if (outsideParts != null && outsideParts.Count > 0)
                            {
                                hitPart = outsideParts.RandomElement();

                                Hediff hd = HediffMaker.MakeHediff(TorannMagicDefOf.TM_DecayHD, p, hitPart);
                                hd.Severity = Rand.Range(severity80 * .8f, severity80 * 1.2f);
                                hd.Severity *= effectBonus;
                                p.health.AddHediff(hd);
                            }
                        }

                        if (p.Map != null)
                        {
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Shadow, p.DrawPos, p.Map, Rand.Range(.6f, .8f), .15f, .05f, .1f, 0, .2f, Rand.Range(0, 360), Rand.Range(0, 360));
                            for (int i = 0; i < 3; i++)
                            {
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Disease, p.DrawPos, p.Map, Rand.Range(.2f + (.1f * i), .2f + (.2f * i)), .15f, .05f, .1f, 0, .2f, Rand.Range(0, 360), Rand.Range(0, 360));
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BlackSmoke, p.DrawPos, p.Map, Rand.Range(.2f + (.1f * i), .2f + (.2f * i)), .15f, .05f, .1f, 0, .2f, Rand.Range(0, 360), Rand.Range(0, 360));
                            }
                        }
                    }
                }
            }
        }

        public override void Apply(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability, float effectLevel = 1f, float effectBonus = 1f)
        {
            base.Apply(target, caster, ability);
            bool success = false;
            TargetInfo ti = new TargetInfo(caster.Position, caster.Map, false);
            TM_MoteMaker.MakeOverlay(ti, TorannMagicDefOf.TM_Mote_PsycastAreaEffect, caster.Map, Vector3.zero, 4f, 0f, .1f, .4f, 1.2f, -6f);
            for (int i = 0; i < 3; i++)
            {
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PurpleSmoke, caster.DrawPos, caster.Map, Rand.Range(.8f + (.2f * i), 1.2f + (.3f * i)), .15f, .05f, .1f, Rand.Range(-20,20), .05f, Rand.Range(0, 360), Rand.Range(0, 360));
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BlackSmoke, caster.DrawPos, caster.Map, Rand.Range(.8f + (.2f * i), 1.2f + (.3f * i)), .15f, .05f, .1f, Rand.Range(-20,20), .05f, Rand.Range(0, 360), Rand.Range(0, 360));
            }
            AutoCast.GolemBlink.Evaluate(caster as TMPawnGolem, ability, minDistance, maxDistance, out success);
            if(success)
            {
                ApplyFadeTouch(caster, caster.Position, effectBonus);
                Effecter effecterED = TorannMagicDefOf.TM_FadeEffecterED.Spawn();
                effecterED.Trigger(new TargetInfo(caster.Position, caster.Map, false), new TargetInfo(caster.Position, caster.Map, false));
                effecterED.Cleanup();
                Effecter swirlED = TorannMagicDefOf.TM_FadeEffecter2ED.Spawn();
                swirlED.Trigger(new TargetInfo(caster.Position, caster.Map, false), new TargetInfo(caster.Position, caster.Map, false));
                swirlED.Cleanup();
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability)
        {
            if(caster.CurJob == null)
            {
                return false;
            }
            if(caster.CurJobDef == TorannMagicDefOf.JobDriver_GolemAttackStatic)
            {
                return false;
            }           
            if(caster.CurJob.locomotionUrgency < LocomotionUrgency.Jog)
            {
                return false;
            }
            float distanceToTarget = (target.Cell - caster.Position).LengthHorizontal;            
            if (distanceToTarget > maxDistance || distanceToTarget <= minDistance)
            {
                return false;
            }
            if (target.Cell == default(IntVec3))
            {
                return false;
            }
            if (base.CanApplyOn(target, caster, ability))
            {
                return true;
            }
            return false;
        }
    }
}
