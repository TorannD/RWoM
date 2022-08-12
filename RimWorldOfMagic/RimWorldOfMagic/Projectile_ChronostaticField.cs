using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using RimWorld;

namespace TorannMagic
{
    class Projectile_ChronostaticField : Projectile_AbilityBase
    {
        int age = -1;
        int duration = 20;
        int verVal = 0;
        int pwrVal = 0;
        float arcaneDmg = 1;
        int strikeDelay = 4;
        int strikeNum = 1;
        float radius = 5;
        bool initialized = false;
        List<IntVec3> cellList;
        Pawn casterPawn;
        IEnumerable<IntVec3> targets;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1800, false);
            Scribe_Values.Look<int>(ref this.strikeDelay, "strikeDelay", 0, false);
            Scribe_Values.Look<int>(ref this.strikeNum, "strikeNum", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_References.Look<Pawn>(ref this.casterPawn, "casterPawn", false);
            Scribe_Collections.Look<IntVec3>(ref this.cellList, "cellList", LookMode.Value);
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
                this.casterPawn = this.launcher as Pawn;
                CompAbilityUserMagic comp = casterPawn.GetCompAbilityUserMagic();
                MagicPowerSkill pwr = comp.MagicData.MagicPowerSkill_ChronostaticField.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_ChronostaticField_pwr");
                MagicPowerSkill ver = comp.MagicData.MagicPowerSkill_ChronostaticField.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_ChronostaticField_ver");
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                pwrVal = pwr.level;
                verVal = ver.level;
                if (this.casterPawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    MightPowerSkill mpwr = casterPawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                    MightPowerSkill mver = casterPawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                    pwrVal = mpwr.level;
                    verVal = mver.level;
                }
                this.arcaneDmg = comp.arcaneDmg;
                if (settingsRef.AIHardMode && !casterPawn.IsColonist)
                {
                    pwrVal = 3;
                    verVal = 3;
                }
                this.strikeDelay = this.strikeDelay - verVal;
                this.radius = this.def.projectile.explosionRadius;
                this.duration = Mathf.RoundToInt(this.radius * this.strikeDelay);
                this.initialized = true;
                //this.targets = GenRadial.RadialCellsAround(base.Position, this.radius, true);
                //cellList = targets.ToList<IntVec3>();
            }

            cellList = new List<IntVec3>();
            cellList.Clear();
            cellList = GenRadial.RadialCellsAround(base.Position, this.radius, true).ToList(); //this.radius instead of 2
            for (int i = 0; i < cellList.Count; i++)
            {
                if (cellList[i].IsValid && cellList[i].InBoundsWithNullCheck(this.Map))
                {
                    List<Thing> thingList = cellList[i].GetThingList(this.Map);
                    if (thingList != null && thingList.Count > 0)
                    {
                        for (int j = 0; j < thingList.Count; j++)
                        {
                            Pawn pawn = thingList[j] as Pawn;
                            if (pawn != null)
                            {
                                RemoveFireAt(thingList[j].Position);
                                if (Rand.Chance(TM_Calc.GetSpellSuccessChance(this.casterPawn, pawn, false) * (.6f + (.1f * verVal))))
                                {
                                    IntVec3 targetCell = pawn.Position;
                                    targetCell.z++;
                                    LaunchFlyingObect(targetCell, pawn, 1, Mathf.RoundToInt(Rand.Range(1400, 1800) * (1f + (.2f * pwrVal)) * this.arcaneDmg));
                                }
                                else
                                {
                                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TM_ResistedSpell".Translate(), -1);
                                }
                            }
                        }
                    }
                }
            }
            this.age = this.duration;
            this.Destroy(DestroyMode.Vanish);
        }

        public void LaunchFlyingObect(IntVec3 targetCell, Pawn pawn, int force, int duration)
        {
            bool flag = targetCell != null && targetCell != default(IntVec3);
            if (flag)
            {
                if (pawn != null && pawn.Position.IsValid && pawn.Spawned && pawn.Map != null && !pawn.Downed && !pawn.Dead)
                {
                    if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                    {
                        ModCheck.GiddyUp.ForceDismount(pawn);
                    }
                    FlyingObject_TimeDelay flyingObject = (FlyingObject_TimeDelay)GenSpawn.Spawn(ThingDef.Named("FlyingObject_TimeDelay"), pawn.Position, pawn.Map);
                    flyingObject.speed = .01f;
                    flyingObject.duration = duration;
                    flyingObject.Launch(this.casterPawn, targetCell, pawn);
                }
            }
        }
        
        private void RemoveFireAt(IntVec3 position)
        {            
            List<Thing> thingList = position.GetThingList(this.Map);
            if (thingList != null && thingList.Count > 0)
            {
                for (int i = 0; i < thingList.Count; i++)
                {
                    if(thingList[i].def == ThingDefOf.Fire)
                    {
                        //Log.Message("removing fire at " + position);
                        FleckMaker.ThrowHeatGlow(position, this.Map, .6f);
                        thingList[i].Destroy(DestroyMode.Vanish);
                        i--;
                    }
                }
            }
        }
    }    
}