using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;
using AbilityUser;
using Verse.AI.Group;

namespace TorannMagic
{
    public class Projectile_SummonMinion : Projectile_Ability
    {

        private int age = -1;

        private bool initialized = false;
        private bool destroyed = false;

        private int duration = 72000;
        private int durationMultiplier = 36000;

        private int verVal;
        private int pwrVal;

        TMPawnSummoned newPawn = new TMPawnSummoned();
        CompAbilityUserMagic comp;
        Pawn pawn;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
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
            //GenClamor.DoClamor(this, 2.1f, ClamorDefOf.Impact);
            //base.Impact(hitThing);

            if (!initialized)
            {
                SpawnThings spawnThing = new SpawnThings();
                pawn = this.launcher as Pawn;
                comp = pawn.GetCompAbilityUserMagic();
                MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonMinion.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SummonMinion_pwr");
                MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonMinion.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SummonMinion_ver");
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                pwrVal = pwr.level;
                verVal = ver.level;
                if (settingsRef.AIHardMode && !pawn.IsColonist)
                {
                    pwrVal = 3;
                    verVal = 3;
                }
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer) || (comp.customClass != null && comp.customClass.classMageAbilities.Contains(TorannMagicDefOf.TM_Cantrips)))
                {
                    int tmpPwrVal = (int)((pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr").level) / 5);
                    pwrVal = (tmpPwrVal > pwrVal) ? tmpPwrVal : pwrVal;
                    int tmpVerVal = (int)((pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_ver").level) / 5);
                    verVal = (tmpVerVal > verVal) ? tmpVerVal : verVal;
                }
                CellRect cellRect = CellRect.CenteredOn(this.Position, 1);
                cellRect.ClipInsideMap(map);

                IntVec3 centerCell = cellRect.CenterCell;
                System.Random random = new System.Random();
                random = new System.Random();

                duration += (verVal * durationMultiplier);
                duration = (int)(duration * comp.arcaneDmg);
                
                spawnThing.factionDef = TorannMagicDefOf.TM_SummonedFaction;
                spawnThing.spawnCount = 1;
                spawnThing.temporary = false;

                if (pwrVal >= 2)
                {
                    for (int i = 0; i < pwrVal - 1; i++)
                    {
                        spawnThing.def = TorannMagicDefOf.TM_GreaterMinionR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_GreaterMinion");
                        SingleSpawnLoop(spawnThing, centerCell, map);                        
                    }
                    FleckMaker.ThrowSmoke(centerCell.ToVector3(), map, 2 + pwrVal);
                    FleckMaker.ThrowMicroSparks(centerCell.ToVector3(), map);
                    FleckMaker.ThrowHeatGlow(centerCell, map, 2 + pwrVal);
                }
                else
                {
                    for (int i = 0; i < pwrVal + 1; i++)
                    {
                        spawnThing.def = TorannMagicDefOf.TM_MinionR;
                        spawnThing.kindDef = PawnKindDef.Named("TM_Minion");
                       
                        SingleSpawnLoop(spawnThing, centerCell, map);

                    }
                    FleckMaker.ThrowSmoke(centerCell.ToVector3(), map, 1 + pwrVal);
                    FleckMaker.ThrowMicroSparks(centerCell.ToVector3(), map);
                    FleckMaker.ThrowHeatGlow(centerCell, map, 1 + pwrVal);
                }

                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                this.initialized = true;
            }
            else
            {
                this.age = this.duration;
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
                        //newPawn.playerSettings.master = this.Caster;
                        if (comp.summonedMinions.Count >= 4)
                        {
                            Thing dismissMinion = comp.summonedMinions[0];
                            if (dismissMinion != null && dismissMinion.Position.IsValid)
                            {
                                FleckMaker.ThrowSmoke(dismissMinion.Position.ToVector3(), base.Map, 1);
                                FleckMaker.ThrowHeatGlow(dismissMinion.Position, base.Map, 1);
                            }
                            comp.summonedMinions.Remove(comp.summonedMinions[0]);
                            if (!dismissMinion.Destroyed)
                            {
                                dismissMinion.Destroy();
                                Messages.Message("TM_SummonedCreatureLimitExceeded".Translate(
                                    this.launcher.LabelShort
                                ), MessageTypeDefOf.NeutralEvent);
                            }
                            if (comp.summonedMinions.Count > 4)
                            {
                                while (comp.summonedMinions.Count > 4)
                                {
                                    Pawn excessMinion = comp.summonedMinions[comp.summonedMinions.Count - 1] as Pawn;
                                    comp.summonedMinions.Remove(excessMinion);
                                    if (excessMinion != null && !excessMinion.Dead && !excessMinion.Destroyed)
                                    {
                                        excessMinion.Destroy();
                                    }
                                }
                            }

                        }
                        try
                        {
                            GenSpawn.Spawn(newPawn, position, this.Map);
                        }
                        catch
                        {
                            this.age = this.duration;
                            comp.Mana.CurLevel += comp.ActualManaCost(TorannMagicDefOf.TM_SummonMinion);
                            Log.Message("TM_Exception".Translate(
                                pawn.LabelShort,
                                this.def.defName
                                ));
                            this.Destroy(DestroyMode.Vanish);
                        }
                        
                        comp.summonedMinions.Add(newPawn);
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
                                lord = LordMaker.MakeNewLord(faction, lordJob, this.Map, null);
                            }
                            else
                            {
                                try
                                {
                                    //PawnDuty duty = this.pawn.mindState.duty;
                                    //newPawn.mindState.duty = duty;
                                    newPawn.mindState.duty = new PawnDuty(DutyDefOf.Defend);
                                }
                                catch
                                {
                                    Log.Message("error attempting to assign a duty to minion");
                                }                                
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
