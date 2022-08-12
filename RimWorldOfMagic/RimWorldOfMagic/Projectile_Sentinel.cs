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
        Thing spawnedThing = null;

        //non-saved vars

        protected override void Impact(Thing hitThing)
        {
            GenClamor.DoClamor(this, 2.1f, ClamorDefOf.Impact);
            caster = this.launcher as Pawn;
            Building existingSentinel = base.Position.GetFirstBuilding(caster.Map);
            if (existingSentinel != null)
            {
                if (existingSentinel.def.defName == "TM_Sentinel")
                {
                    for (int m = 0; m < 5; m++)
                    {
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, base.Position.ToVector3Shifted(), caster.Map, Rand.Range(.4f, .7f), Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), Rand.Range(-20, 20), Rand.Range(.5f, 1f), Rand.Range(0, 360), Rand.Range(0, 360));
                    }
                    existingSentinel.Destroy(DestroyMode.Vanish);
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
                CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
                verVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Sentinel.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Sentinel_ver").level;

                if (comp.summonedSentinels.Count > verVal + 1)
                {
                    Messages.Message("TM_TooManySentinels".Translate(
                                caster.LabelShort,
                                verVal + 2
                    ), MessageTypeDefOf.NeutralEvent);
                    Thing tempThing = comp.summonedSentinels[0];
                    comp.summonedSentinels.Remove(tempThing);
                    if (tempThing != null && !tempThing.Destroyed)
                    {
                        tempThing.Destroy();
                    }
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
