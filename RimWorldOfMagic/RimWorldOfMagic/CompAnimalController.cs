using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;
using Verse.Sound;
using AbilityUser;

namespace TorannMagic
{
    public class CompAnimalController : ThingComp
	{
        private bool initialized = false;

        List<Pawn> threatList = new List<Pawn>();
        List<Pawn> closeThreats = new List<Pawn>();
        List<Pawn> farThreats = new List<Pawn>();

        private int rangedBurstShots = 0;
        private int rangedNextBurst = 0;
        private LocalTargetInfo rangedTarget = null;
        private Thing launchableThing = null;

        private int scanTick = 281;
        private int age = -1;

        public Pawn summonerPawn = null;
        private int verVal = 0;
        private int pwrVal = 0;
        private int tauntRange = 15;
        private int tauntTargetsMax = 4;
        private float tauntChance = .6f;
        private int damageMitigation = 4;
        private int damageMitigationDelay = 0;
        private int shadowStrikeDamage = 11;
        private float shadowStrikeCritChance = .4f;
        private int invisDuration = 90;
        private int hasteDuration = 120;
        private float hexChance = .5f;
        private int nextEvalTick = 0;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look<Pawn>(ref this.summonerPawn, "summonerPawn");
        }

        //private int actionReady = 0;
        //private int actionTick = 0;

        //private LocalTargetInfo universalTarget = null;

        public override void PostDraw()
        {
            base.PostDraw();
           
        }

        private Pawn Pawn
        {
            get
            {
                Pawn pawn = this.parent as Pawn;
                bool flag = pawn == null;
                if (flag)
                {
                    Log.Error("pawn is null");
                }
                return pawn;
            }
        }

        private List<Pawn> PawnThreatList
        {
            get
            {
                return closeThreats.Union(farThreats).ToList();
            }
        }

        public CompProperties_AnimalController Props
        {
            get
            {
                return (CompProperties_AnimalController)this.props;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
        }

        public override void CompTick()
        {
            if (this.age > 0)
            {
                if (!this.initialized)
                {
                    if (summonerPawn != null)
                    {
                        CompAbilityUserMagic comp = summonerPawn.GetCompAbilityUserMagic();
                        if (comp != null)
                        {
                            verVal = TM_Calc.GetSkillVersatilityLevel(summonerPawn, TorannMagicDefOf.TM_GuardianSpirit);
                            pwrVal = TM_Calc.GetSkillPowerLevel(summonerPawn, TorannMagicDefOf.TM_GuardianSpirit);
                            tauntRange = 15 + pwrVal;
                            tauntTargetsMax = 4 + pwrVal;
                            tauntChance = .6f + (.03f * pwrVal);
                            damageMitigation = 4 + Mathf.RoundToInt((float)pwrVal/1.5f);
                            shadowStrikeDamage = 11 + pwrVal;
                            shadowStrikeCritChance = .4f + (.05f * pwrVal);
                            invisDuration = 90 + (10 * pwrVal);
                            hasteDuration = 120 + (10 * pwrVal);
                            hexChance = .5f + (.05f * pwrVal);
                        }                        
                     }
                    this.initialized = true;
                }
                
                if (this.Pawn.Spawned && this.Props.abilities != null)
                {
                    if (!this.Pawn.Downed && Find.TickManager.TicksGame >= this.nextEvalTick)
                    {
                        this.nextEvalTick = Find.TickManager.TicksGame + Mathf.RoundToInt(Rand.Range(.8f, 1.2f) * this.Props.abilityAttemptFrequency);
                        DetermineThreats();
                        if (closeThreats != null && closeThreats.Count >= 1)
                        {
                            if (this.Props.abilities.Contains(TorannMagicDefOf.TM_Taunt) && this.Pawn.needs?.food?.CurLevel > .06f)
                            {
                                SoundInfo info = SoundInfo.InMap(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false), MaintenanceType.None);
                                info.pitchFactor = Rand.Range(.3f, .4f);
                                info.volumeFactor = .7f;
                                TorannMagicDefOf.TM_Roar.PlayOneShot(info);
                                Effecter RageWave = TorannMagicDefOf.TM_RageWaveED.Spawn();
                                RageWave.Trigger(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false), new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
                                RageWave.Cleanup();
                                SearchAndTaunt();
                                this.Pawn.needs.food.CurLevel -= .3f;
                            }
                        }
                        if(farThreats != null && farThreats.Count >= 1 && this.Pawn.needs?.food?.CurLevel > .05f)
                        {
                            if(this.Props.abilities.Contains(TorannMagicDefOf.TM_ShadowStrike))
                            {
                                Thing target = farThreats.RandomElement();
                                if (DoMove(target))
                                {
                                    DoStrike(target);
                                    this.Pawn.needs.food.CurLevel -= .3f;
                                }
                            }
                        }
                        if(PawnThreatList != null && PawnThreatList.Count > 0 && this.Pawn.needs?.food?.CurLevel > .025f)
                        {
                            if(this.Props.abilities.Contains(TorannMagicDefOf.TM_Hex))
                            {
                                Pawn p = PawnThreatList.RandomElement();
                                if(p.health != null && p.health.hediffSet != null && !p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HexHD))
                                {
                                    if(Rand.Chance(hexChance))
                                    {
                                        HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_HexHD, 1f);
                                        CompAbilityUserMagic bondedMagicComp = this.summonerPawn.GetCompAbilityUserMagic();

                                        if (bondedMagicComp != null && !bondedMagicComp.HexedPawns.Contains(p) && bondedMagicComp.MagicData != null && bondedMagicComp.MagicData.MagicPowersShaman.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Hex).learned)
                                        {
                                            bool addAbilities = false;
                                            bool shouldAddAbilities = bondedMagicComp.HexedPawns.Count <= 0;
                                            if (!bondedMagicComp.HexedPawns.Contains(p))
                                            {
                                                bondedMagicComp.HexedPawns.Add(p);
                                                addAbilities = true;
                                            }
                                            if (shouldAddAbilities && addAbilities)
                                            {
                                                bondedMagicComp.AddPawnAbility(TorannMagicDefOf.TM_Hex_CriticalFail);
                                                bondedMagicComp.AddPawnAbility(TorannMagicDefOf.TM_Hex_Pain);
                                                bondedMagicComp.AddPawnAbility(TorannMagicDefOf.TM_Hex_MentalAssault);
                                            }
                                        }
                                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Hex, p.DrawPos, p.Map, .6f, .1f, .2f, .2f, 0, 0, 0, 0);
                                    }
                                    this.Pawn.needs.food.CurLevel -= .1f;
                                }
                            }
                        }
                    }
                }
            }
            age++;
        }

        public void SearchAndTaunt()
        {
            List<Pawn> mapPawns = this.Pawn.Map.mapPawns.AllPawnsSpawned;
            List<Pawn> tauntTargets = new List<Pawn>();
            tauntTargets.Clear();
            if (mapPawns != null && mapPawns.Count > 0)
            {
                for (int i = 0; i < mapPawns.Count; i++)
                {
                    Pawn victim = mapPawns[i];
                    if (!victim.DestroyedOrNull() && !victim.Dead && victim.Map != null && !victim.Downed && victim.mindState != null && !victim.InMentalState && victim.jobs != null)
                    {
                        if (this.Pawn.Faction.HostileTo(victim.Faction) && (victim.Position - this.Pawn.Position).LengthHorizontal < tauntRange)
                        {
                            tauntTargets.Add(victim);
                        }
                    }
                    if (tauntTargets.Count >= tauntTargetsMax)
                    {
                        break;
                    }
                }
                for (int i = 0; i < tauntTargets.Count; i++)
                {
                    if (Rand.Chance(tauntChance))
                    {
                        //Log.Message("taunting " + threatPawns[i].LabelShort + " doing job " + threatPawns[i].CurJobDef.defName + " with follow radius of " + threatPawns[i].CurJob.followRadius);
                        if (tauntTargets[i].CurJobDef == JobDefOf.Follow || tauntTargets[i].CurJobDef == JobDefOf.FollowClose)
                        {
                            Job job = new Job(JobDefOf.AttackMelee, this.Pawn);
                            tauntTargets[i].jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        }
                        HealthUtility.AdjustSeverity(tauntTargets[i], TorannMagicDefOf.TM_TauntHD, 1);
                        Hediff hd = tauntTargets[i].health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_TauntHD);
                        HediffComp_Disappears comp_d = hd.TryGetComp<HediffComp_Disappears>();
                        if (comp_d != null)
                        {
                            comp_d.ticksToDisappear = 600;
                        }
                        HediffComp_Taunt comp_t = hd.TryGetComp<HediffComp_Taunt>();
                        if (comp_t != null)
                        {
                            comp_t.tauntTarget = this.Pawn;
                        }
                        MoteMaker.ThrowText(tauntTargets[i].DrawPos, tauntTargets[i].Map, "Taunted!", -1);
                    }
                    else
                    {
                        MoteMaker.ThrowText(tauntTargets[i].DrawPos, tauntTargets[i].Map, "TM_ResistedSpell".Translate(), -1);
                    }
                }
            }
        }

        public bool DoMove(Thing target)
        {
            Map map = this.Pawn.Map;
            IntVec3 targetPos = target.Position;
            IntVec3 tmpPos = targetPos;
            if (!target.DestroyedOrNull() && target is Pawn targetPawn)
            {
                if (targetPawn.Rotation == Rot4.East)
                {
                    tmpPos.x--;
                }
                else if (targetPawn.Rotation == Rot4.West)
                {
                    tmpPos.x++;
                }
                else if (targetPawn.Rotation == Rot4.North)
                {
                    tmpPos.z--;
                }
                else
                {
                    tmpPos.z++;
                }
                if (tmpPos.IsValid && tmpPos.InBoundsWithNullCheck(map) && tmpPos.Walkable(map))
                {
                    targetPos = tmpPos;
                    this.Pawn.DeSpawn();
                    GenSpawn.Spawn(this.Pawn, targetPos, map);
                    return true;
                }
            }
            return false;
        }

        public void DoStrike(Thing target)
        {
            if (target != null && target is Pawn t)
            {
                if (t.Faction == null || (t.Faction != null && t.Faction != this.Pawn.Faction))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (!t.DestroyedOrNull() && !t.Dead && t.Map != null)
                        {
                            int dmg = shadowStrikeDamage + pwrVal;
                            if (Rand.Chance(shadowStrikeCritChance))
                            {
                                dmg *= 3;
                            }
                            BodyPartRecord bpr = t.health.hediffSet.GetRandomNotMissingPart(DamageDefOf.Stab, BodyPartHeight.Undefined, BodyPartDepth.Outside);
                            TM_Action.DamageEntities(target, bpr, dmg, Rand.Range(0f, .5f), DamageDefOf.Stab, this.Pawn);
                            Vector3 rndPos = t.DrawPos;
                            rndPos.x += Rand.Range(-.2f, .2f);
                            rndPos.z += Rand.Range(-.2f, .2f);
                            TM_MoteMaker.ThrowBloodSquirt(rndPos, t.Map, Rand.Range(.6f, 1f));
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_CrossStrike, rndPos, t.Map, Rand.Range(.6f, 1f), .4f, 0f, Rand.Range(.2f, .5f), 0, 0, 0, Rand.Range(0, 360));
                        }
                    }
                    if (!t.DestroyedOrNull() && !t.Dead && !t.Downed)
                    {
                        Job job = new Job(JobDefOf.AttackMelee, t);
                        this.Pawn.jobs.TryTakeOrderedJob(job);
                    }
                }
            }
            HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_ShadowCloakHD, .2f);
            HediffComp_Disappears hdComp = this.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ShadowCloakHD).TryGetComp<HediffComp_Disappears>();
            if (hdComp != null)
            {
                hdComp.ticksToDisappear = this.invisDuration;
            }            
            ApplyHaste(this.Pawn);
        }

        public void ApplyHaste(Pawn p)
        {
            HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_HasteHD, .5f);
            HediffComp_Disappears hdComp = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HasteHD).TryGetComp<HediffComp_Disappears>();
            if (hdComp != null)
            {
                hdComp.ticksToDisappear = this.hasteDuration;
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
        }

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            base.PostPreApplyDamage(dinfo, out absorbed);
            if(dinfo.Instigator != null)
            {
                Thing instigatorThing = dinfo.Instigator;
                if (this.Props.abilities != null && this.Props.abilities.Contains(TorannMagicDefOf.TM_Fortitude))
                {
                    if (damageMitigationDelay < this.age)
                    {
                        absorbed = true;
                        this.damageMitigation = 4 + (1 * pwrVal);
                        float actualDmg;
                        float dmgAmt = dinfo.Amount;
                        if (dmgAmt < damageMitigation)
                        {
                            actualDmg = 0;
                            return;
                        }
                        else
                        {
                            actualDmg = dmgAmt - damageMitigation;
                        }
                        damageMitigationDelay = this.age + 5;
                        dinfo.SetAmount(actualDmg);
                        this.Pawn.TakeDamage(dinfo);
                        return;
                        
                    }
                }
            }
        }

        private void DetermineThreats()
        {
            this.closeThreats.Clear();
            this.farThreats.Clear();
            List<Pawn> allPawns = this.Pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                if (!allPawns[i].DestroyedOrNull() && allPawns[i] != this.Pawn)
                {
                    if (!allPawns[i].Dead && !allPawns[i].Downed)
                    {
                        if (allPawns[i].Faction != null && (allPawns[i].Faction.HostileTo(this.Pawn.Faction)) && !allPawns[i].IsPrisoner)
                        {
                            if ((allPawns[i].Position - this.Pawn.Position).LengthHorizontal <= this.Props.maxRangeForCloseThreat)
                            {
                                this.closeThreats.Add(allPawns[i]);
                            }
                            else if ((allPawns[i].Position - this.Pawn.Position).LengthHorizontal <= this.Props.maxRangeForFarThreat)
                            {
                                this.farThreats.Add(allPawns[i]);
                            }
                        }
                        if (allPawns[i].Faction == null && allPawns[i].InMentalState)
                        {
                            if ((allPawns[i].Position - this.Pawn.Position).LengthHorizontal <= this.Props.maxRangeForCloseThreat)
                            {
                                this.closeThreats.Add(allPawns[i]);
                            }
                            else if ((allPawns[i].Position - this.Pawn.Position).LengthHorizontal <= this.Props.maxRangeForFarThreat)
                            {
                                this.farThreats.Add(allPawns[i]);
                            }
                        }
                    }
                }
            }
        }

        public bool TargetIsValid(Thing target)
        {
            if(target.DestroyedOrNull())
            {
                return false;
            }
            if(!target.Spawned)
            {
                return false;
            }
            if(target is Pawn targetPawn)
            {
                return !targetPawn.Downed;
            }
            if(target.Position.DistanceToEdge(this.Pawn.Map) < 8)
            {
                return false;
            }
            if(target.Faction != null)
            {
                return target.Faction != this.Pawn.Faction && target.Faction.HostileTo(this.Pawn.Faction);
            }
            return true;
        }
    }
}