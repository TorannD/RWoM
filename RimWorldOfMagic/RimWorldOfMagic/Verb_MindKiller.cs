using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;
using Verse.Sound;
using Verse.AI;


namespace TorannMagic
{
    public class Verb_MindKiller : ClassProgression.Verb_SpiritTargetable
    {

        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            //Pawn pawn = this.currentTarget.Thing as Pawn;
            bool flag = caster != null && !caster.Dead && !caster.Downed;
            if (flag)
            {
                var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(caster, Ability);
                if (owner == null) return false;
                CompAbilityUserMagic comp = owner.GetCompAbilityUserMagic();
                int pwrVal = 0;
                int verVal = 0;
                if (comp != null && comp.MagicData != null)
                {
                    pwrVal = TM_Calc.GetSkillPowerLevel(owner, this.Ability.Def as TMAbilityDef, false);
                    verVal = TM_Calc.GetSkillVersatilityLevel(owner, this.Ability.Def as TMAbilityDef, false);
                }
                float radius = 15f + verVal * 2;
                float penetrationChance = .5f + verVal * .1f;
                int targetsMax = 8 + pwrVal * 2;
                float maxMoodBurn = .3f + (pwrVal * .08f);

                SoundInfo info = SoundInfo.InMap(new TargetInfo(caster.Position, caster.Map, false), MaintenanceType.None);
                info.volumeFactor = .8f;
                if(this.CasterPawn.gender == Gender.Female)
                {
                    info.pitchFactor = Rand.Range(1.2f, 1.5f); 
                }
                else
                {
                    info.pitchFactor = Rand.Range(1f, 1.2f);
                }
                TorannMagicDefOf.TM_GaspingAir.PlayOneShot(info);
                Effecter MKWave = TorannMagicDefOf.TM_MKWaveED.Spawn();
                MKWave.Trigger(new TargetInfo(caster.Position, caster.Map, false), new TargetInfo(caster.Position, caster.Map, false));
                MKWave.Trigger(new TargetInfo(caster.Position, caster.Map, false), new TargetInfo(caster.Position, caster.Map, false));
                MKWave.Cleanup();
                List<Pawn> pList = new List<Pawn>();
                pList.Clear();
                if (pwrVal < 2)
                {
                    pList = TM_Calc.FindAllPawnsAround(caster.Map, caster.Position, radius, null, false);
                }
                else
                {
                    pList = TM_Calc.FindAllPawnsAround(caster.Map, caster.Position, radius, caster.Faction, false);
                }
                if(pList != null && pList.Count > 0)
                {
                    pList = pList.InRandomOrder().ToList();
                    int count = pList.Count >= targetsMax ? targetsMax : pList.Count;
                    for(int i = 0; i < count; i++)
                    {
                        Pawn p = pList[i];
                        if(p != caster && p.needs != null)
                        {
                            if((p.story != null && p.story.traits != null && p.story.traits.HasTrait(TraitDefOf.Psychopath)) || TM_Calc.IsUndeadNotVamp(p) || p.needs.mood == null)
                            {
                                continue;
                            }
                            float penChance = penetrationChance;
                            if(p.Faction == caster.Faction)
                            {
                                penChance -= .4f;
                            }
                            if(Rand.Chance(TM_Calc.GetSpellSuccessChance(caster, p, false) + penChance))
                            {
                                DrawEffects(caster, p);
                                Need n = p.needs.mood;
                                float curLvl = Mathf.Clamp(n.CurLevel, 0, maxMoodBurn);
                                var trial = ClassProgression.EmpathTrials.CaptureMood(this.Ability, p, caster);
                                n.CurLevel -= curLvl;
                                ClassProgression.EmpathTrials.CompleteMood(trial);
                                TM_Action.DamageEntities(p, null, n.CurLevel * 20f, DamageDefOf.Stun, caster);
                                if(Rand.Chance(curLvl + .25f) && p.Faction != caster.Faction)
                                {
                                    HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_MindKillerHD, Rand.Range(.2f, .5f) + (.2f * pwrVal));
                                }
                                if(Rand.Chance(curLvl + .15f) && p.Faction != caster.Faction)
                                {
                                    BodyPartRecord bpr = p.def.race.body.AllParts.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.BloodPumpingSource));
                                    if (bpr != null)
                                    {
                                        TM_Action.DamageEntities(p, bpr, penChance + curLvl, 2f, TMDamageDefOf.DamageDefOf.TM_Shadow, caster);
                                    }
                                }
                                if(p.mindState != null && p.mindState.mentalStateHandler != null && p.Faction != caster.Faction && Rand.Chance(.2f + (.1f * pwrVal)))
                                {
                                    p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                                }
                            }
                        }
                    }
                }
            }

            return flag;
        }  
        
        public void DrawEffects(Pawn caster, Pawn target)
        {
            Vector3 drawVec = TM_Calc.GetVector(caster.DrawPos, target.DrawPos);
            float throwAngle = (Quaternion.AngleAxis(90, Vector3.up) * drawVec).ToAngleFlat();
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BlackSmoke, target.DrawPos, target.Map, Rand.Range(.6f, .8f), 1f, .1f, 2f, Rand.Range(-20, 20), 0, throwAngle, Rand.Range(0, 360));

            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BlackSmoke, target.DrawPos, target.Map, Rand.Range(.6f, .8f), 1f, .1f, 2f, Rand.Range(-20, 20), Rand.Range(.5f, 1f), throwAngle, Rand.Range(0, 360));
            for (int i = 0; i < 3; i++)
            {
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PurpleSmoke, target.DrawPos, target.Map, Rand.Range(.8f, 1.2f), 1f, .2f + (.04f * i), 2f - (.6f * i), Rand.Range(-10, 10), 1f + Rand.Range(.6f, 1f) * i, throwAngle, Rand.Range(0,360));
            }
        }
    }
}