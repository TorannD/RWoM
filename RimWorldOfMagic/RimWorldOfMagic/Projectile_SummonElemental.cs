using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;
using AbilityUser;
using UnityEngine;
using Verse.AI.Group;


namespace TorannMagic
{
    public class Projectile_SummonElemental : Projectile_Ability
    {

        private int age = -1;
        private bool initialized = false;
        private bool destroyed = false;
        private int duration = 1800;
        TMPawnSummoned newPawn = new TMPawnSummoned();
        Pawn pawn;
        private int verVal;
        private int pwrVal;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 3600, false);
            Scribe_Values.Look<bool>(ref this.destroyed, "destroyed", false, false);
        }        

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }        

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            //base.Impact(hitThing);
            GenClamor.DoClamor(this, 2.1f, ClamorDefOf.Impact);
            if (!initialized)
            {
                SpawnThings spawnThing = new SpawnThings();
                pawn = this.launcher as Pawn;
                MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonElemental.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SummonElemental_pwr");
                MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonElemental.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SummonElemental_ver");
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
                if (settingsRef.AIHardMode && !pawn.IsColonist)
                {
                    pwrVal = 3;
                    verVal = 3;
                }
                CellRect cellRect = CellRect.CenteredOn(this.Position, 1);
                cellRect.ClipInsideMap(map);

                IntVec3 centerCell = cellRect.CenterCell;
                System.Random random = new System.Random();
                random = new System.Random();
 
                duration += (verVal * 900);
                duration = Mathf.RoundToInt(duration * pawn.GetCompAbilityUserMagic().arcaneDmg);

                int rnd = GenMath.RoundRandom(random.Next(0, 8));
                if (rnd < 2)
                {
                    spawnThing.factionDef = TorannMagicDefOf.TM_ElementalFaction;
                    spawnThing.spawnCount = 1;
                    spawnThing.temporary = false;

                    if (pwrVal == 3)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_Earth_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_Earth_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_GreaterEarth_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_GreaterEarth_Elemental");
                    }
                    else if (pwrVal == 2)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_LesserEarth_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_LesserEarth_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_GreaterEarth_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_GreaterEarth_Elemental");
                    }
                    else if (pwrVal == 1)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_LesserEarth_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_LesserEarth_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_Earth_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_Earth_Elemental");
                    }
                    else
                    {
                        for (int i = 0; i < 1; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_LesserEarth_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_LesserEarth_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_LesserEarth_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_LesserEarth_Elemental");
                    }
                    FleckMaker.ThrowSmoke(centerCell.ToVector3(), map, pwrVal);
                    FleckMaker.ThrowMicroSparks(centerCell.ToVector3(), map);
                    SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                }
                else if (rnd >= 2 && rnd < 4)
                {
                    spawnThing.factionDef = TorannMagicDefOf.TM_ElementalFaction;
                    spawnThing.spawnCount = 1;
                    spawnThing.temporary = false;

                    if (pwrVal == 3)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_Fire_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_Fire_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_GreaterFire_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_GreaterFire_Elemental");
                    }
                    else if (pwrVal == 2)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_LesserFire_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_LesserFire_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_GreaterFire_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_GreaterFire_Elemental");
                    }
                    else if (pwrVal == 1)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_LesserFire_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_LesserFire_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_Fire_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_Fire_Elemental");
                    }
                    else
                    {
                        for (int i = 0; i < 1; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_LesserFire_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_LesserFire_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_LesserFire_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_LesserFire_Elemental");
                    }
                    FleckMaker.ThrowSmoke(centerCell.ToVector3(), map, pwrVal);
                    FleckMaker.ThrowMicroSparks(centerCell.ToVector3(), map);
                    FleckMaker.ThrowFireGlow(centerCell.ToVector3Shifted(), map, pwrVal);
                    FleckMaker.ThrowHeatGlow(centerCell, map, pwrVal);
                }
                else if (rnd >= 4 && rnd < 6)
                {
                    spawnThing.factionDef = TorannMagicDefOf.TM_ElementalFaction;
                    spawnThing.spawnCount = 1;
                    spawnThing.temporary = false;

                    if (pwrVal == 3)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_Water_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_Water_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_GreaterWater_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_GreaterWater_Elemental");
                    }
                    else if (pwrVal == 2)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_LesserWater_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_LesserWater_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_GreaterWater_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_GreaterWater_Elemental");
                    }
                    else if (pwrVal == 1)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_LesserWater_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_LesserWater_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_Water_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_Water_Elemental");
                    }
                    else
                    {
                        for (int i = 0; i < 1; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_LesserWater_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_LesserWater_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_LesserWater_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_LesserWater_Elemental");
                    }
                    FleckMaker.ThrowSmoke(centerCell.ToVector3(), map, pwrVal);
                    SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                    FleckMaker.ThrowTornadoDustPuff(centerCell.ToVector3(), map, pwrVal, Color.blue);
                    FleckMaker.ThrowTornadoDustPuff(centerCell.ToVector3(), map, pwrVal, Color.blue);
                    FleckMaker.ThrowTornadoDustPuff(centerCell.ToVector3(), map, pwrVal, Color.blue);
                }
                else
                {
                    spawnThing.factionDef = TorannMagicDefOf.TM_ElementalFaction;
                    spawnThing.spawnCount = 1;
                    spawnThing.temporary = false;

                    if (pwrVal == 3)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_LesserWind_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_Wind_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_GreaterWind_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_GreaterWind_Elemental");
                    }
                    else if (pwrVal == 2)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_LesserWind_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_LesserWind_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_GreaterWind_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_GreaterWind_Elemental");
                    }
                    else if (pwrVal == 1)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_LesserWind_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_LesserWind_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_Wind_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_Wind_Elemental");
                    }
                    else
                    {
                        for (int i = 0; i < 1; i++)
                        {
                            spawnThing.def = TorannMagicDefOf.TM_LesserWind_ElementalR;
                            spawnThing.kindDef = PawnKindDef.Named("TM_LesserWind_Elemental");
                            SingleSpawnLoop(spawnThing, centerCell, map);
                        }
                        spawnThing.def = TorannMagicDefOf.TM_LesserWind_ElementalR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_LesserWind_Elemental");
                    }
                    FleckMaker.ThrowSmoke(centerCell.ToVector3(), map, 1 + pwrVal * 2);
                    SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                    FleckMaker.ThrowTornadoDustPuff(centerCell.ToVector3(), map, pwrVal, Color.white);
                }

                SingleSpawnLoop(spawnThing, centerCell, map);

                this.age = this.duration;                
                this.initialized = true;
            }
            Destroy();
        }

        public void SingleSpawnLoop(SpawnThings spawnables, IntVec3 position, Map map)
        {
            bool flag = spawnables.def != null;
            if (flag)
            {
                Faction faction = pawn.Faction;
                bool flag2 = spawnables.def.race != null;
                if (flag2)
                {
                    bool flag3 = spawnables.kindDef == null;
                    if (flag3)
                    {
                        Log.Error("Missing kinddef");
                    }
                    else
                    {
                        newPawn = (TMPawnSummoned)PawnGenerator.GeneratePawn(spawnables.kindDef, faction);
                        newPawn.validSummoning = true;
                        newPawn.Spawner = this.Caster;
                        newPawn.Temporary = true;
                        newPawn.TicksToDestroy = this.duration;
                        //if (newPawn.Faction != Faction.OfPlayerSilentFail)
                        //{
                        //    newPawn.SetFaction(this.Caster.Faction, null);
                        //}
                        try
                        {
                            GenSpawn.Spawn(newPawn, position, map, Rot4.North, WipeMode.Vanish, false);
                            //GenPlace.TryPlaceThing(newPawn, position, map, ThingPlaceMode.Near, null, null);
                        }
                        catch
                        {
                            pawn.GetCompAbilityUserMagic().Mana.CurLevel += pawn.GetCompAbilityUserMagic().ActualManaCost(TorannMagicDefOf.TM_SummonElemental);
                            Log.Message("TM_Exception".Translate(
                                pawn.LabelShort,
                                this.def.defName
                                ));
                            this.Destroy(DestroyMode.Vanish);
                        }
                        if (newPawn.Faction != null && newPawn.Faction != Faction.OfPlayer)
                        {
                            Lord lord = null;
                            if (newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction).Any((Pawn p) => p != newPawn))
                            {
                                Predicate<Thing> validator = (Thing p) => p != newPawn && ((Pawn)p).GetLord() != null;
                                Pawn p2 = (Pawn)GenClosest.ClosestThing_Global(newPawn.Position, newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction), 99999f, validator, null);
                                lord = p2.GetLord();
                            }
                            bool flag4 = lord == null;
                            if (flag4)
                            {
                                LordJob_DefendPoint lordJob = new LordJob_DefendPoint(newPawn.Position);
                                lord = LordMaker.MakeNewLord(faction, lordJob, map, null);
                            }
                            else
                            {
                                try
                                {
                                    //PawnDuty duty = this.pawn.mindState.duty;
                                    newPawn.mindState.duty = new PawnDuty(DutyDefOf.Defend);
                                }
                                catch
                                {
                                    Log.Message("error attempting to assign a duty to elemental");
                                }
                            }
                            lord.AddPawn(newPawn);
                        }
                        Pawn enemy = TM_Calc.FindNearbyEnemy(newPawn, 30);
                        if (enemy != null)
                        {
                            Job defendJob = new Job(JobDefOf.AttackMelee, enemy);
                            newPawn.jobs.TryTakeOrderedJob(defendJob);
                        }
                    }
                }
                else
                {
                    Log.Message("Missing race");
                }
            }
        }
    }
}