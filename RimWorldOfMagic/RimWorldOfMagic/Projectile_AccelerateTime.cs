using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using RimWorld;
using HarmonyLib;

namespace TorannMagic
{
    public class Projectile_AccelerateTime : Projectile_AbilityBase
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
        Pawn pawn;
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
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
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

            if (!this.initialized)
            {
                this.pawn = this.launcher as Pawn;
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_AccelerateTime.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_AccelerateTime_pwr");
                MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_AccelerateTime.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_AccelerateTime_ver");
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                pwrVal = pwr.level;
                verVal = ver.level;
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    MightPowerSkill mpwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                    MightPowerSkill mver = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                    pwrVal = mpwr.level;
                    verVal = mver.level;
                }
                this.arcaneDmg = comp.arcaneDmg;
                if (settingsRef.AIHardMode && !pawn.IsColonist)
                {
                    pwrVal = 3;
                    verVal = 3;
                }                
                this.strikeDelay = this.strikeDelay - verVal;
                this.radius = this.def.projectile.explosionRadius;
                this.duration = Mathf.RoundToInt(this.radius * this.strikeDelay);
                this.initialized = true;
                this.targets = GenRadial.RadialCellsAround(base.Position, this.radius, true);
                //cellList = targets.ToList<IntVec3>();
            }


            Pawn targetPawn = this.intendedTarget.Thing as Pawn;
            if (targetPawn == null)
            {
                base.Position.GetFirstPawn(this.Map);
            }

            if(targetPawn != null)
            {
                if (targetPawn.Faction != null && targetPawn.Faction == this.launcher.Faction)
                {
                    AgePawn(targetPawn, Mathf.RoundToInt((24 * 2500)* (1+(.1f * verVal))), false);
                }
                else
                {
                    List<Pawn> pawnList = TM_Calc.FindAllPawnsAround(this.launcher.Map, base.Position, this.radius, this.launcher.Faction, false);
                    if (pawnList != null && pawnList.Count > 0)
                    {
                        for (int i = 0; i < Mathf.Clamp(pawnList.Count, 0, 2+verVal); i++)
                        {
                            if(pawnList[i].Faction != null && !pawnList[i].Faction.HostileTo(this.pawn.Faction))
                            {
                                pawnList[i].Faction.TryAffectGoodwillWith(this.pawn.Faction, -25);
                            }

                            if (pawnList[i].Faction != null && pawnList[i].Faction != this.pawn.Faction)
                            {
                                AgePawn(pawnList[i], Mathf.RoundToInt((2500) * (1 + (.1f * verVal))), true);
                                if (pawnList[i].IsColonist && !this.pawn.IsColonist)
                                {
                                    TM_Action.SpellAffectedPlayerWarning(pawnList[i]);
                                }
                            }
                            else if(pawnList[i].Faction == null)
                            {
                                AgePawn(pawnList[i], Mathf.RoundToInt((2500) * (1 + (.1f * verVal))), true);
                            }
                        }
                    }
                }
            }
            else
            {
                List<Pawn> pawnList = TM_Calc.FindAllPawnsAround(this.launcher.Map, base.Position, this.radius, this.launcher.Faction, false);
                if (pawnList != null && pawnList.Count > 0)
                {                    
                    for (int i = 0; i < Mathf.Clamp(pawnList.Count, 0, 2 + verVal); i++)
                    {
                        if (pawnList[i].Faction != null && !pawnList[i].Faction.HostileTo(this.pawn.Faction))
                        {
                            pawnList[i].Faction.TryAffectGoodwillWith(this.pawn.Faction, -25);
                        }

                        targetPawn = pawnList[i];
                        if (targetPawn.Faction != null && targetPawn.Faction != this.pawn.Faction)
                        {
                            AgePawn(pawnList[i], Mathf.RoundToInt((2500) * (1 + (.1f * verVal))), true);
                        }
                        else if(targetPawn.Faction == null)
                        {
                            AgePawn(pawnList[i], Mathf.RoundToInt((2500) * (1 + (.1f * verVal))), true);
                        }
                    }
                }
            }

            List<Thing> thingList = base.Position.GetThingList(this.Map);
            Thing thing = null;

            if (targetPawn == null && thingList != null && thingList.Count > 0)
            {
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i] != null && !(thingList[i] is Pawn) && !(thingList[i] is Building))
                    {
                        //if (thingList[i].def.thingCategories != null && thingList[i].def.thingCategories.Count > 0 && (thingList[i].def.thingCategories.Contains(ThingCategoryDefOf.ResourcesRaw) || thingList[i].def.thingCategories.Contains(ThingCategoryDefOf.StoneBlocks) || thingList[i].def.defName == "RawMagicyte"))                    
                        if (thingList[i].def.MadeFromStuff)
                        {
                            thing = thingList[i];
                            break;
                        }
                        if (!thingList[i].def.MadeFromStuff && thingList[i].TryGetComp<CompQuality>() != null)
                        {
                            thing = thingList[i];
                            break;
                        }
                    }
                }
            }

            if(thing != null)
            {
                AgeThing(thing);
            }

            List<IntVec3> cellList = targets.ToList();
            if(cellList != null && cellList.Count > 0)
            {
                for(int i =0; i < cellList.Count; i++)
                {
                    thingList = cellList[i].GetThingList(this.Map);
                    if(thingList != null && thingList.Count > 0)
                    {
                        for(int j =0; j < thingList.Count; j++)
                        {
                            if (thingList[j] is Plant)
                            {
                                Plant plant = thingList[j] as Plant;
                                try
                                {
                                    plant.Growth = plant.Growth + ((Rand.Range((2 + pwrVal), (4 + pwrVal)) / plant.def.plant.growDays) * this.arcaneDmg);
                                }
                                catch (NullReferenceException ex)
                                {
                                    plant.Growth *= (1.1f + (.1f * pwrVal));
                                }
                            }
                            CompHatcher compHatcher = thingList[j].TryGetComp<CompHatcher>();
                            if(compHatcher != null)
                            {
                                float gestateProgress = Traverse.Create(root: compHatcher).Field(name: "gestateProgress").GetValue<float>();
                                Traverse.Create(root: compHatcher).Field(name: "gestateProgress").SetValue((gestateProgress + Rand.Range(.3f + (.1f * pwrVal), .7f + (.1f * pwrVal))) * this.arcaneDmg);
                            }
                        }
                    }
                }                    
            }
            
            Effecter AreaAccelEffect = TorannMagicDefOf.TM_TimeAccelerationAreaEffecter.Spawn();
            AreaAccelEffect.Trigger(new TargetInfo(base.Position, this.Map, false), new TargetInfo(base.Position, this.Map, false));
            AreaAccelEffect.Cleanup();

            this.age = this.duration;
            this.Destroy(DestroyMode.Vanish);
        }

        private void AgePawn(Pawn pawn, int duration, bool isBad)
        {
            duration = Mathf.RoundToInt(duration * this.arcaneDmg);
            if (pawn != null && !pawn.DestroyedOrNull() && !pawn.Dead && pawn.health != null && pawn.health.hediffSet != null)
            {
                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ReverseTimeHD))
                {
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ReverseTimeHD);
                    pawn.health.RemoveHediff(hediff);
                }
                else
                {
                    if (Rand.Chance((.5f + verVal) * TM_Calc.GetSpellSuccessChance(this.launcher as Pawn, pawn, false)))
                    {
                        HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_AccelerateTimeHD, .5f + pwrVal);
                        HediffComp_AccelerateTime hediffComp = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_AccelerateTimeHD, false).TryGetComp<HediffComp_AccelerateTime>();
                        if (hediffComp != null)
                        {
                            hediffComp.durationTicks = (duration);
                            hediffComp.isBad = isBad;
                            AccelerateEffects(pawn, 1);
                        }
                    }
                    else
                    {
                        if (pawn.Map != null)
                        {
                            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TM_ResistedSpell".Translate(), -1);
                        }
                    }
                }
            }
        }

        private void AgeThing(Thing thing)
        {

            thing.HitPoints -= Mathf.RoundToInt((200 + (100 * pwrVal)) * this.arcaneDmg);
            if (thing.HitPoints <= 0)
            {
                List<ThingDefCountClass> componentList = thing.def.costList;
                if (componentList != null && componentList.Count > 0)
                {
                    for (int i = 0; i < componentList.Count; i++)
                    {
                        Thing componentThing = ThingMaker.MakeThing(componentList[i].thingDef, null);
                        componentThing.stackCount = Mathf.RoundToInt(componentList[i].count * (.5f + (.1f * pwrVal)));
                        GenPlace.TryPlaceThing(componentThing, thing.Position, this.Map, ThingPlaceMode.Near);
                    }
                }
                if(thing.def.MadeFromStuff && thing.Stuff != null)
                {
                    Thing componentThing = ThingMaker.MakeThing(thing.Stuff, null);
                    componentThing.stackCount = Mathf.RoundToInt(thing.def.costStuffCount * (.5f + (.1f * pwrVal)));
                    GenPlace.TryPlaceThing(componentThing, thing.Position, this.Map, ThingPlaceMode.Near);
                }
                TransmutateEffects(thing.Position, 6 );
                thing.Destroy(DestroyMode.Vanish);                
            }            
        }

        public void TransmutateEffects(IntVec3 position, int intensity)
        {
            Vector3 rndPos = position.ToVector3Shifted();
            FleckMaker.ThrowHeatGlow(position, this.Map, 1f);
            for (int i = 0; i < intensity; i++)
            {
                rndPos.x += Rand.Range(-.5f, .5f);
                rndPos.z += Rand.Range(-.5f, .5f);
                rndPos.y += Rand.Range(.3f, 1.3f);
                FleckMaker.ThrowSmoke(rndPos, this.Map, Rand.Range(.7f, 1.1f));
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Shadow, position.ToVector3(), this.Map, Rand.Range(.8f, 1.2f), .1f, .1f, .4f, Rand.RangeInclusive((int)-4, (int)4) * 100, Rand.Range(0, 1), Rand.Range(0, 360), Rand.Range(0, 360));
            }
        }

        public void AccelerateEffects(Pawn pawn, int intensity)
        {
            Effecter AccelEffect = TorannMagicDefOf.TM_TimeAccelerationEffecter.Spawn();
            AccelEffect.Trigger(new TargetInfo(pawn), new TargetInfo(pawn));
            AccelEffect.Cleanup();
        }
    }    
}