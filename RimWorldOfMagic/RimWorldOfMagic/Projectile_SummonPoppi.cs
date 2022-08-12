using RimWorld;
using System;
using System.Linq;
using Verse;
using AbilityUser;
using UnityEngine;
using Verse.AI.Group;

namespace TorannMagic
{
    public class Projectile_SummonPoppi : Projectile_Ability
    {

        private bool initialized = false;
        TMPawnSummoned newPawn = new TMPawnSummoned();
        Pawn pawn;

        private int pwrVal = 0;
        private int verVal = 0;
        private float arcaneDmg = 1f;

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);            
        }        

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            GenClamor.DoClamor(this, 5.1f, ClamorDefOf.Impact);
            if (initialized)
            {
                Destroy();
            }

            if (!initialized)
            {
                this.initialized = true;
                SpawnThings spawnThing = new SpawnThings();
                pawn = this.launcher as Pawn;
                MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonPoppi.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SummonPoppi_pwr");
                MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonPoppi.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SummonPoppi_ver");
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                pwrVal = pwr.level;
                verVal = ver.level;
                this.arcaneDmg = pawn.GetCompAbilityUserMagic().arcaneDmg;
                if (settingsRef.AIHardMode && !pawn.IsColonist)
                {
                    pwrVal = 1;
                    verVal = 1;
                }
                CellRect cellRect = CellRect.CenteredOn(this.Position, 4);
                cellRect.ClipInsideMap(map);

                IntVec3 centerCell = cellRect.CenterCell;
                System.Random random = new System.Random();
                random = new System.Random();

                for (int i = 0; i < 4 + pwrVal; i++)
                {
                    centerCell = cellRect.RandomCell;
                    if (centerCell.IsValid && centerCell.InBoundsWithNullCheck(pawn.Map) && centerCell.Standable(pawn.Map) && !centerCell.Fogged(pawn.Map))
                    {
                        spawnThing.factionDef = TorannMagicDefOf.TM_ElementalFaction;
                        spawnThing.spawnCount = 1;
                        spawnThing.temporary = false;

                        spawnThing.def = TorannMagicDefOf.TM_Poppi;
                        spawnThing.kindDef = PawnKindDef.Named("TM_Poppi");

                        FleckMaker.ThrowSmoke(centerCell.ToVector3(), map, 1+pwrVal);
                        SingleSpawnLoop(spawnThing, centerCell, map);
                    }
                    else
                    {
                        i--;
                    }
                }
            }
            
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
                        newPawn.TicksToDestroy = Mathf.RoundToInt(1800 * this.arcaneDmg);

                        try
                        {
                            GenSpawn.Spawn(newPawn, position, map);
                            CompLeaper comp = newPawn.GetComp<CompLeaper>();
                            comp.explosionRadius += ((verVal * .2f) * this.arcaneDmg);
                        }
                        catch
                        {
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
                            lord.AddPawn(newPawn);
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