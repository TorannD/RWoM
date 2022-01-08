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
    public class Projectile_GraveBlade : Projectile_AbilityBase
    {
        int age = -1;
        int duration = 65;
        int verVal = 0;
        int pwrVal = 0;
        float arcaneDmg = 1;
        int strikeDelay = 15;
        int effectIndex1 = 0;
        int effectIndex2 = 0;
        float radius = 3;
        bool initialized = false;
        List<IntVec3> ringCellList;
        List<IntVec3> innerCellList;
        Pawn caster;

        int effectDelay = 1;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 65, false);
            Scribe_Values.Look<int>(ref this.strikeDelay, "strikeDelay", 0, false);
            Scribe_Values.Look<int>(ref this.effectIndex1, "effectIndex1", 0, false);
            Scribe_Values.Look<int>(ref this.effectIndex2, "effectIndex2", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_References.Look<Pawn>(ref this.caster, "caster", false);
            Scribe_Collections.Look<IntVec3>(ref this.innerCellList, "innerCellList", LookMode.Value);
            Scribe_Collections.Look<IntVec3>(ref this.ringCellList, "ringCellList", LookMode.Value);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        protected override void Impact(Thing hitThing)
        {            
            base.Impact(hitThing);
           
            ThingDef def = this.def;          

            if (!this.initialized)
            {
                this.caster = this.launcher as Pawn;               
                CompAbilityUserMight comp = caster.GetComp<CompAbilityUserMight>();
                //pwrVal = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_GraveBlade.FirstOrDefault((MightPowerSkill x) => x.label == "TM_GraveBlade_pwr").level;
                //verVal = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_GraveBlade.FirstOrDefault((MightPowerSkill x) => x.label == "TM_GraveBlade_ver").level;
                //verVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_GraveBlade, "TM_GraveBlade", "_ver", true);
                //pwrVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_GraveBlade, "TM_GraveBlade", "_pwr", true);
                //ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                this.arcaneDmg = comp.mightPwr;
                //if (settingsRef.AIHardMode && !caster.IsColonist)
                //{
                //    pwrVal = 3;
                //    verVal = 3;
                //}
                verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_GraveBlade);
                pwrVal = TM_Calc.GetSkillPowerLevel(caster, TorannMagicDefOf.TM_GraveBlade);
                this.radius = this.def.projectile.explosionRadius;
                this.duration = 10 + (int)(this.radius * 20);
                this.innerCellList = GenRadial.RadialCellsAround(base.Position, this.radius, true).ToList();
                this.ringCellList = GenRadial.RadialCellsAround(base.Position, this.radius+1, false).Except(innerCellList).ToList();
                this.effectIndex2 = ringCellList.Count / 2;
                this.initialized = true;
            }

            if (this.Map != null)
            {
                if (Find.TickManager.TicksGame % this.effectDelay == 0)
                {
                    Vector3 drawIndex1 = this.ringCellList[effectIndex1].ToVector3Shifted();
                    drawIndex1.x += Rand.Range(-.35f, .35f);
                    drawIndex1.z += Rand.Range(-.35f, .35f);
                    Vector3 drawIndex2 = this.ringCellList[effectIndex2].ToVector3Shifted();
                    drawIndex2.x += Rand.Range(-.35f, .35f);
                    drawIndex2.z += Rand.Range(-.35f, .35f);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_SpiritFlame, drawIndex1 , this.Map, Rand.Range(.4f, .8f), .1f, 0, .6f, 0, Rand.Range(.4f, 1f), Rand.Range(-20, 20), Rand.Range(-20, 20));
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_SpiritFlame, drawIndex2, this.Map, Rand.Range(.4f, .8f), .1f, 0, .6f, 0, Rand.Range(.4f, 1f), Rand.Range(-20, 20), Rand.Range(-20, 20));
                    this.effectIndex1++;
                    this.effectIndex2++;
                    if (this.effectIndex1 >= ringCellList.Count)
                    {
                        this.effectIndex1 = 0;
                    }
                    if (this.effectIndex2 >= ringCellList.Count)
                    {
                        this.effectIndex2 = 0;
                    }                    
                }
                if (Find.TickManager.TicksGame % this.strikeDelay == 0 && !this.caster.DestroyedOrNull())
                {
                    IntVec3 centerCell = innerCellList.RandomElement();
                    List<IntVec3> targetCells = GenRadial.RadialCellsAround(centerCell, 2f, true).ToList();
                    for (int i = 0; i < targetCells.Count; i++)
                    {
                        IntVec3 curCell = targetCells[i];
                        Pawn victim = curCell.GetFirstPawn(this.Map);
                        if (victim != null && !victim.Destroyed && !victim.Dead && victim != this.caster)
                        {
                            TM_Action.DamageEntities(victim, null, (Rand.Range(10, 16) + (2 * pwrVal)) * this.arcaneDmg, TMDamageDefOf.DamageDefOf.TM_Spirit, this.launcher);
                            if (!this.caster.DestroyedOrNull() && !this.caster.Dead && Rand.Chance(verVal))
                            {
                                TM_Action.DoAction_HealPawn(this.caster, this.caster, 1, (Rand.Range(6, 10) + (2 * verVal)) * this.arcaneDmg);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_SpiritRetaliation, this.caster.DrawPos, this.caster.Map, Rand.Range(1f, 1.2f), Rand.Range(.1f, .15f), 0, Rand.Range(.1f, .2f), -600, 0, 0, Rand.Range(0, 360));
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_SpiritRetaliation, this.caster.DrawPos, this.caster.Map, Rand.Range(1f, 1.2f), Rand.Range(.1f, .15f), 0, Rand.Range(.1f, .2f), 600, 0, 0, Rand.Range(0, 360));
                            }
                            if (Rand.Chance(verVal))
                            {
                                if (!victim.IsWildMan() && victim.RaceProps.Humanlike && victim.mindState != null && !victim.InMentalState)
                                {
                                    try
                                    {
                                        Job job = new Job(JobDefOf.FleeAndCower);
                                        //victim.mindState.mentalStateHandler.TryStartMentalState(TorannMagicDefOf.TM_PanicFlee);
                                    }
                                    catch (NullReferenceException ex)
                                    {

                                    }
                                }
                            }
                        }
                    }
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_GraveBlade, centerCell.ToVector3Shifted(), this.Map, Rand.Range(1f, 1.6f), .15f, .1f, .2f, 0, Rand.Range(4f, 6f), 0, 0);

                }
            }
        }        
    }    
}