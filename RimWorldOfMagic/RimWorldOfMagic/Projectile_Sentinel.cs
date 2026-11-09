using Verse;
using Verse.Sound;
using RimWorld;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;

namespace TorannMagic
{
    class Projectile_Sentinel : Projectile_AbilityBase
    {

        private int verVal;
        Pawn caster;
        private Pawn projectedOwner;
        private int powerRank;
        internal void PrepareHosted(MagicAbility ability)
        {
            projectedOwner = ability.Pawn;
            powerRank = ClassProgression.SpiritRebirthRuntime.SavedRank(ability.MagicUser.ExistingMagicData,"TM_Sentinel_pwr");
            verVal = ClassProgression.SpiritRebirthRuntime.SavedRank(ability.MagicUser.ExistingMagicData,"TM_Sentinel_ver");
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref projectedOwner,"TM_sentinelOwner");
            Scribe_Values.Look(ref powerRank,"TM_sentinelPower");
            Scribe_Values.Look(ref verVal,"TM_sentinelVersatility");
        }
        Thing spawnedThing = null;

        //non-saved vars

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            GenClamor.DoClamor(this, 2.1f, ClamorDefOf.Impact);
            caster = this.launcher as Pawn;
            var owner = projectedOwner ?? caster;
            var comp = owner?.GetCompAbilityUserMagic();
            if (comp == null || caster?.Spawned != true || caster.Map != Map ||
                projectedOwner != null && (!ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(caster,out var held) || held != owner))
            {
                Destroy();
                return;
            }
            comp.PruneSentinels();
            Building existingSentinel = base.Position.GetFirstBuilding(Map);
            if (existingSentinel != null)
            {
                if (existingSentinel is Building_TMSentinel && comp.summonedSentinels.Contains(existingSentinel))
                {
                    for (int m = 0; m < 5; m++)
                    {
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, base.Position.ToVector3Shifted(), caster.Map, Rand.Range(.4f, .7f), Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), Rand.Range(-20, 20), Rand.Range(.5f, 1f), Rand.Range(0, 360), Rand.Range(0, 360));
                    }
                    existingSentinel.Destroy(DestroyMode.Vanish);
                    comp.summonedSentinels.Remove(existingSentinel);
                }
                else
                {
                    Messages.Message("TM_NoSpawnSentinelOnBuilding".Translate(
                            caster.LabelShort
                        ), MessageTypeDefOf.RejectInput, false);
                }
            }
            else
            {
                if (projectedOwner == null)
                {
                    powerRank = ClassProgression.SpiritRebirthRuntime.SavedRank(comp.MagicData, "TM_Sentinel_pwr");
                    verVal = ClassProgression.SpiritRebirthRuntime.SavedRank(comp.MagicData, "TM_Sentinel_ver");
                }

                AbilityUser.SpawnThings tempPod = new SpawnThings();
                tempPod.def = ThingDef.Named("TM_Sentinel");
                tempPod.spawnCount = 1;
                SingleSpawnLoop(tempPod, base.Position, caster.Map);

                float magnitude = (base.Position.ToVector3Shifted() - Find.Camera.transform.position).magnitude;
                Find.CameraDriver.shaker.DoShake(4 / magnitude);

                for (int m = 0; m < 5; m++)
                {
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, base.Position.ToVector3Shifted(), caster.Map, Rand.Range(.4f, .7f), Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), Rand.Range(-20, 20), Rand.Range(.5f, 1f), Rand.Range(0, 360), Rand.Range(0, 360));
                }

                if (this.spawnedThing != null)
                {
                    int limit = verVal + 2;
                    while (comp.summonedSentinels.Count >= limit)
                    {
                        var oldest = comp.summonedSentinels[0];
                        comp.summonedSentinels.RemoveAt(0);
                        if (!oldest.DestroyedOrNull()) oldest.Destroy();
                    }
                    ((Building_TMSentinel)spawnedThing).Bind(owner,powerRank);
                    comp.summonedSentinels.Add(spawnedThing);
                }
            }
            Destroy();
        }

        public void SingleSpawnLoop(SpawnThings spawnables, IntVec3 position, Map map)
        {
            bool flag = spawnables.def != null;
            if (flag)
            {
                Faction faction = TM_Action.ResolveFaction(this.launcher as Pawn, spawnables, this.launcher.Faction);
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
                        TM_Action.SpawnPawn(this.launcher as Pawn, spawnables, faction, position, 0, map);
                    }
                }
                else
                {
                    ThingDef def = spawnables.def;
                    ThingDef stuff = null;
                    bool madeFromStuff = def.MadeFromStuff;
                    if (madeFromStuff)
                    {
                        stuff = ThingDef.Named("BlocksGranite");
                    }
                    spawnedThing = ThingMaker.MakeThing(def, stuff);
                    this.caster.rotationTracker.FaceCell(position);
                    GenSpawn.Spawn(spawnedThing, position, map, this.caster.Rotation, WipeMode.Vanish, false);
                }
            }
        }

    }
}
