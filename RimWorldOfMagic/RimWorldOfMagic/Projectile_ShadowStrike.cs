using Verse;
using Verse.AI;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using RimWorld;


namespace TorannMagic
{
    public class Projectile_ShadowStrike : Projectile_AbilityBase
    {
        int age = -1;
        int duration = 300;
        int verVal = 0;
        bool initialized = false;
        IntVec3 startPos;
        Pawn caster;
        float weaponDamage = 5f;
        float critChance = 0f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 65, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_References.Look<Pawn>(ref this.caster, "caster", false);
            Scribe_Values.Look<IntVec3>(ref this.startPos, "startPos", default(IntVec3), false);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < duration;
            if (!flag)
            {
                if (!caster.DestroyedOrNull() && !caster.Dead)
                {
                    ReturnMove();
                }
                base.Destroy(mode);
            }
            if(caster.DestroyedOrNull() || caster.Dead)
            {
                base.Destroy(DestroyMode.Vanish);
            }
        }

        protected override void Impact(Thing hitThing)
        {           
            if (!this.initialized && !hitThing.DestroyedOrNull())
            {
                this.initialized = true;
                this.caster = this.launcher as Pawn;               
                CompAbilityUserMight comp = caster.GetCompAbilityUserMight();
                //verVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_ShadowStrike, "TM_ShadowStrike", "_ver", true);    
                verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_ShadowStrike);
                this.startPos = caster.Position;
                this.age = 0;
                this.weaponDamage = GetWeaponDmg(caster);
                this.critChance = comp.weaponCritChance;
                
                GenClamor.DoClamor(caster, 2f, ClamorDefOf.Ability);
                if (DoMove(hitThing))
                {
                    DoStrike(hitThing);
                }
            }

            if(age >=0)
            {
                age++;
            }

            Destroy(DestroyMode.Vanish);
        }        

        public bool DoMove(Thing target)
        {
            Map map = caster.Map;
            this.startPos = caster.Position;
            IntVec3 targetPos = target.Position;
            IntVec3 tmpPos = targetPos;
            if(!target.DestroyedOrNull() && target is Pawn p)
            {
                if(p.Rotation == Rot4.East)
                {
                    tmpPos.x--;
                }
                else if(p.Rotation == Rot4.West)
                {
                    tmpPos.x++;
                }
                else if(p.Rotation == Rot4.North)
                {
                    tmpPos.z--;
                }
                else
                {
                    tmpPos.z++;
                }
                if(tmpPos.IsValid && tmpPos.InBoundsWithNullCheck(map) && tmpPos.Walkable(map))
                {
                    targetPos = tmpPos;
                }
            }
            else
            {
                return false;
            }
            bool draftFlag = caster.Drafted;
            try
            {
                if (caster.IsColonist)
                {
                    ModOptions.Constants.SetPawnInFlight(true);
                    caster.DeSpawn();
                    GenSpawn.Spawn(caster, targetPos, map);
                    caster.drafter.Drafted = draftFlag;
                    if (ModOptions.Settings.Instance.cameraSnap)
                    {
                        CameraJumper.TryJumpAndSelect(caster);
                    }
                    ModOptions.Constants.SetPawnInFlight(false);
                    return true;
                }
                else
                {
                    caster.DeSpawn();
                    GenSpawn.Spawn(caster, targetPos, map);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public void DoStrike(Thing target)
        {
            if (target != null && target is Pawn t)
            {
                if (t.Faction == null || (t.Faction != null && t.Faction != caster.Faction))
                {
                    //List<BodyPartRecord> partList = new List<BodyPartRecord>();
                    //partList.Clear();
                    //for (int i = 0; i < t.RaceProps.body.AllParts.Count; i++)
                    //{
                    //    BodyPartRecord part = t.RaceProps.body.AllParts[i];
                    //    if (part.depth == BodyPartDepth.Outside)
                    //    {
                    //        partList.Add(part);
                    //    }
                    //}
                    for (int i = 0; i < 4; i++)
                    {
                        if (!t.DestroyedOrNull() && !t.Dead && t.Map != null)
                        {
                            int dmg = Mathf.RoundToInt(this.weaponDamage);
                            if (Rand.Chance(critChance))
                            {
                                dmg *= 3;
                            }
                            BodyPartRecord bpr = t.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Stab, BodyPartHeight.Undefined, BodyPartDepth.Outside);
                            TM_Action.DamageEntities(target, bpr, dmg, Rand.Range(0f, .5f), DamageDefOf.Stab, this.caster);
                            Vector3 rndPos = t.DrawPos;
                            rndPos.x += Rand.Range(-.2f, .2f);
                            rndPos.z += Rand.Range(-.2f, .2f);
                            TM_MoteMaker.ThrowBloodSquirt(rndPos, t.Map, Rand.Range(.6f, 1f));
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_CrossStrike, rndPos, t.Map, Rand.Range(.6f, 1f), .4f, 0f, Rand.Range(.2f, .5f), 0, 0, 0, Rand.Range(0, 360));
                        }
                    }
                    if (!t.DestroyedOrNull() && !t.Dead && !t.Downed && caster.IsColonist)
                    {
                        caster.drafter.Drafted = true;
                        Job job = new Job(JobDefOf.AttackMelee, t);
                        caster.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
                    }
                }
            }
            if (verVal >= 1)
            {
                int invisDuration = 120;
                if(verVal >= 2)
                {
                    invisDuration = 180;
                }
                HealthUtility.AdjustSeverity(caster, TorannMagicDefOf.TM_ShadowCloakHD, .2f);

                HediffComp_Disappears hdComp = caster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ShadowCloakHD).TryGetComp<HediffComp_Disappears>();
                if (hdComp != null)
                {
                    hdComp.ticksToDisappear = invisDuration;
                }
            }
            if(verVal >= 3)
            {
                int radius = 2;
                if(verVal >= 5)
                {
                    radius = 3;
                }
                float sev = 1.5f;
                if(verVal >= 4)
                {
                    sev = 2.2f;
                }
                List<Pawn> targetList = TM_Calc.FindPawnsNearTarget(caster, radius, caster.Position, true);
                if (targetList != null && targetList.Count > 0)
                {
                    for (int i = 0; i < targetList.Count; i++)
                    {
                        if (targetList[i].RaceProps.IsFlesh)
                        {
                            HealthUtility.AdjustSeverity(targetList[i], TorannMagicDefOf.TM_NightshadeToxinHD, Rand.Range(.7f * sev, 1.3f * sev));
                        }
                    }
                }
                ThingDef fog = TorannMagicDefOf.Fog_Shadows;
                fog.gas.expireSeconds.min = 2;
                fog.gas.expireSeconds.max = 3;
                GenExplosion.DoExplosion(caster.Position, caster.Map, radius, TMDamageDefOf.DamageDefOf.TM_Toxin, caster, 0, 0, TMDamageDefOf.DamageDefOf.TM_Toxin.soundExplosion, null, null, null, fog, 1f, 1, false, null, 0f, 0, 0.0f, false);

                for (int i = 0; i < 6; i++)
                {
                    Vector3 rndPos = caster.DrawPos;
                    rndPos.x += Rand.Range(-.5f, .5f);
                    rndPos.z += Rand.Range(-.5f, .5f);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ShadowCloud, rndPos, caster.Map, Rand.Range(.6f, 1f), .4f, .05f, Rand.Range(.2f, .5f), Rand.Range(-40, 40), Rand.Range(1, 2f), Rand.Range(0, 360), Rand.Range(0, 360));
                }
            }            
            ApplyHaste(caster);
        }

        public void ReturnMove()
        {
            Map map = caster.Map;
            bool draftFlag = caster.Drafted;
            Thing t = Find.Selector.SingleSelectedThing;
            bool selectedFlag = t == caster;
            try
            {
                if (caster.IsColonist)
                {
                    ModOptions.Constants.SetPawnInFlight(true);
                    caster.DeSpawn();
                    GenSpawn.Spawn(caster, startPos, map);
                    caster.drafter.Drafted = draftFlag;
                    ModOptions.Constants.SetPawnInFlight(false);
                    if(selectedFlag)
                    {
                        if (ModOptions.Settings.Instance.cameraSnap)
                        {
                            CameraJumper.TryJumpAndSelect(caster);
                        }
                    }
                }
                else
                {
                    caster.DeSpawn();
                    GenSpawn.Spawn(caster, startPos, map);
                }
            }
            catch
            {
                caster.DeSpawn();
                GenSpawn.Spawn(caster, startPos, map);
            }
        }

        public void ApplyHaste(Pawn p)
        {
            HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_HasteHD, .5f);
            HediffComp_Disappears hdComp = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HasteHD).TryGetComp<HediffComp_Disappears>();
            if(hdComp != null)
            {
                hdComp.ticksToDisappear = 300;
            }
        }

        public static int GetWeaponDmg(Pawn pawn)
        {
            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
            int pwrVal = comp.MightData.MightPowerSkill_ShadowStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_ShadowStrike_pwr").level;
            float dmg = comp.weaponDamage;
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (!pawn.IsColonist && settingsRef.AIHardMode)
            {
                dmg += 5;
            }

            dmg = Mathf.RoundToInt(dmg * TorannMagicDefOf.TM_ShadowStrike.weaponDamageFactor * (1f + (.05f * pwrVal)));
            return (int)Mathf.Clamp(dmg, 0, 30);
        }
    }    
}