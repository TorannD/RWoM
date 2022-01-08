using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;
using TorannMagic.TMDefs;
using AbilityUser;

namespace TorannMagic.Golems
{
    public class CompProperties_GolemAbilityEffect_Awakening : CompProperties_GolemAbilityEffect
    {
        public float damageMultiplier = 100f;
        public float damagePerMana = 1f;
        public float radiusPerManaBonus = .05f;
        public float explosionRadius = 2f;
        public float range = 100f;

        public void AbsorbAbility(Pawn caster, float effectBonus)
        {
            if (caster != null && caster.Map != null)
            {
                Pawn enemyCaster = null;
                foreach (Pawn p in caster.Map.mapPawns.AllPawnsSpawned)
                {
                    if (p.jobs != null && (p.CurJobDef == TorannMagicDefOf.TMCastAbilityVerb || p.CurJobDef == TorannMagicDefOf.TMCastAbilitySelf) && p.HostileTo(caster.Faction) && TM_Calc.IsMagicUser(p) && (p.Position - caster.Position).LengthHorizontal <= range)
                    {
                        enemyCaster = p;
                        break;
                    }
                }
                if (enemyCaster != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Casting, caster.DrawPos, caster.Map, Rand.Range(1.6f, 2.2f), .2f, .05f, Rand.Range(.4f, .6f), Rand.Range(-200, 200), 0, 0, 0);
                    }
                    TargetInfo ti = new TargetInfo(enemyCaster.Position, enemyCaster.Map, false);
                    TM_MoteMaker.MakeOverlay(ti, TorannMagicDefOf.TM_Mote_PsycastAreaEffect, enemyCaster.Map, Vector3.zero, 3f, 0f, .1f, .4f, 1.2f, -6f);

                    float manaDamage = 0;
                    float expRad = explosionRadius;
                    Verb_UseAbility verb = enemyCaster.CurJob.verbToUse as Verb_UseAbility;
                    if (verb != null && verb.Ability != null)
                    {
                        TMAbilityDef tmAbility = (TMAbilityDef)(verb.Ability.Def);
                        if (tmAbility != null && tmAbility.manaCost > 0)
                        {
                            manaDamage = tmAbility.manaCost * damageMultiplier * damagePerMana * effectBonus;
                            expRad += (damageMultiplier * radiusPerManaBonus);
                            TMPawnGolem pg = caster as TMPawnGolem;
                            for(int i = 0; i < 15; i++)
                            {
                                Vector3 moteVec = caster.DrawPos;
                                moteVec.x += Rand.Range(-2.8f, 2.8f);
                                moteVec.z += Rand.Range(-2.8f, 2.8f);
                                float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(moteVec, caster.DrawPos)).ToAngleFlat();
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi_Black, moteVec, caster.Map, Rand.Range(.25f, .6f), .1f, .05f, .05f, 0, Rand.Range(4f, 6f), angle, angle);
                            }
                            pg.Golem.Energy.AddEnergy(tmAbility.manaCost * 1000 * effectBonus);
                        }

                        verb.Ability.PostAbilityAttempt();
                        CompAbilityUserMagic ecomp = enemyCaster.TryGetComp<CompAbilityUserMagic>();
                        if (ecomp != null)
                        {
                            for (int j = 0; j < ecomp.AbilityData.AllPowers.Count; j++)
                            {
                                MagicAbility ma = ecomp.AbilityData.AllPowers[j] as MagicAbility;
                                if (ma != null)
                                {
                                    ma.CooldownTicksLeft = (int)(ma.Def.MainVerb.SecondsToRecharge * 60);
                                }
                            }
                        }
                    }
                    enemyCaster.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false, false);
                    TM_Action.DamageEntities(enemyCaster, null, 10f, DamageDefOf.Stun, caster);
                    if (manaDamage != 0)
                    {
                        GenExplosion.DoExplosion(enemyCaster.Position, enemyCaster.Map, expRad, TMDamageDefOf.DamageDefOf.TM_Shadow, caster, Mathf.RoundToInt(manaDamage), damageFalloff: true);
                    }

                }
            }
        }

        public override void Apply(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability, float effectLevel = 1f, float effectBonus = 1f)
        {
            base.Apply(target, caster, ability);
            AbsorbAbility(caster, effectBonus);
        }

        public override bool CanApplyOn(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability)
        {            
            if (caster != null && caster.Map != null)
            {
                bool flag = false;
                foreach (Pawn p in caster.Map.mapPawns.AllPawnsSpawned)
                {
                    bool isStunned = false;
                    if(p.stances != null && p.stances.stunner != null && p.stances.stunner.Stunned)
                    {
                        isStunned = true;
                    }
                    if (p.jobs != null && !isStunned && (p.CurJobDef == TorannMagicDefOf.TMCastAbilityVerb || p.CurJobDef == TorannMagicDefOf.TMCastAbilitySelf) && p.HostileTo(caster.Faction))
                    {
                        flag = true;
                        break;
                    }
                }
                if (flag)
                {
                    return base.CanApplyOn(target, caster, ability);
                }
            }
            return false;
        }
    }
}
