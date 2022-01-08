using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;


namespace TorannMagic.Golems
{

    public class Verb_DecayTouch : Verb_MeleeAttack
    {

        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
            Pawn p = target.Thing as Pawn;
            BodyPartRecord hitPart = null;
            TMPawnGolem pg = this.CasterPawn as TMPawnGolem;
            float decayBonus = 1f;
            if(p != null && pg != null)
            {
                TM_GolemUpgrade gu = pg.Golem.Upgrades.FirstOrDefault((TM_GolemUpgrade g) => g.golemUpgradeDef.defName == "TM_HollowGolem_CoAFire");
                if(gu != null && gu.currentLevel > 0)
                {
                    TM_Action.DamageEntities(p, null, 10f + (5f * gu.currentLevel), .5f + (.1f * gu.currentLevel) , DamageDefOf.Burn, pg);
                }
                gu = pg.Golem.Upgrades.FirstOrDefault((TM_GolemUpgrade g) => g.golemUpgradeDef.defName == "TM_HollowGolem_CoAIce");
                if (gu != null && gu.currentLevel > 0)
                {
                    TM_Action.DoAction_HealPawn(pg, pg, 1, 8f + (4f * gu.currentLevel));
                    Pawn f = TM_Calc.FindNearbyInjuredPawnOther(pg, 10 + (3 * gu.currentLevel), 1f);
                    if(f != null)
                    {
                        TM_Action.DoAction_HealPawn(pg, f, 1, 8f + (4f * gu.currentLevel));
                    }
                }
                gu = pg.Golem.Upgrades.FirstOrDefault((TM_GolemUpgrade g) => g.golemUpgradeDef.defName == "TM_HollowGolem_CoAArcane");
                if (gu != null && gu.currentLevel > 0)
                {
                    pg.Golem.Energy.AddEnergy(20f + (5f*gu.currentLevel));
                }
                gu = pg.Golem.Upgrades.FirstOrDefault((TM_GolemUpgrade g) => g.golemUpgradeDef.defName == "TM_HollowGolem_CoADeath");
                if(gu != null && gu.currentLevel > 0)
                {
                    decayBonus = 2f;
                }
            }

            if (p != null)
            {
                for (int i = 0; i < decayBonus; i++)
                {
                    List<BodyPartRecord> outsideParts = p.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Outside).Where(def => def.coverage > 0).ToList();
                    if (outsideParts != null && outsideParts.Count > 0)
                    {
                        hitPart = outsideParts.RandomElement();

                        Hediff hd = HediffMaker.MakeHediff(TorannMagicDefOf.TM_DecayHD, p, hitPart);
                        hd.Severity = (this.tool.power / 100f) * (decayBonus * .65f);
                        p.health.AddHediff(hd);
                    }
                }
            }

            DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_DecayDD, (int)(this.tool.power/5f), 2, (float)-1, this.CasterPawn, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
            damageResult.hitThing = target.Thing;
            damageResult.totalDamageDealt = Mathf.Min((float)target.Thing.HitPoints, dinfo.Amount);
            //float angle = (Quaternion.AngleAxis(90, Vector3.up)*TM_Calc.GetVector(this.CasterPawn.Position, target.Thing.Position)).ToAngleFlat();
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Shadow, target.Thing.DrawPos, target.Thing.Map, Rand.Range(.6f, .8f), .15f, .05f, .1f, 0, .2f, Rand.Range(0,360), Rand.Range(0,360));
            for(int i = 0; i < 3; i++)
            {
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Disease, target.Thing.DrawPos, target.Thing.Map, Rand.Range(.2f + (.1f*i), .2f + (.2f *i)), .15f, .05f, .1f, 0, .2f, Rand.Range(0, 360), Rand.Range(0, 360));
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BlackSmoke, target.Thing.DrawPos, target.Thing.Map, Rand.Range(.2f + (.1f * i), .2f + (.2f * i)), .15f, .05f, .1f, 0, .2f, Rand.Range(0, 360), Rand.Range(0, 360));
            }

            target.Thing.TakeDamage(dinfo);
            return damageResult;  
        }
    }
}
