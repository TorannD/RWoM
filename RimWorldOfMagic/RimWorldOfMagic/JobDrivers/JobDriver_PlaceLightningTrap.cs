using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;


namespace TorannMagic
{
    internal class JobDriver_PlaceLightningTrap: JobDriver
    {

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {

            Toil gotoSpot = new Toil()
            {
                initAction = () =>
                {
                    pawn.pather.StartPath(TargetLocA, PathEndMode.Touch);
                },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            yield return gotoSpot;

            Toil placeTrap = new Toil()
            {
                initAction = () =>
                {
                    SpawnThings tempPod = new SpawnThings();
                    tempPod.def = TorannMagicDefOf.TM_Trap_Lightning;
                    CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                    try
                    {
                        for (int i = 0; i < comp.lightningTraps.Count; i++)
                        {
                            if (comp.lightningTraps[i].Destroyed)
                            {
                                comp.lightningTraps.Remove(comp.lightningTraps[i]);
                                i--;
                            }
                        }
                        if (comp.lightningTraps.Count >= 1)
                        {
                            Messages.Message("TM_TooManyTraps".Translate(
                                pawn.LabelShort,
                                1
                            ), MessageTypeDefOf.NeutralEvent);
                            Thing tempThing = comp.lightningTraps[0];
                            comp.lightningTraps.Remove(tempThing);
                            if (tempThing != null && !tempThing.Destroyed)
                            {
                                tempThing.Destroy();
                            }
                        }
                        this.SingleSpawnLoop(tempPod, pawn, TargetLocA, pawn.Map);
                    }
                    catch
                    {
                        Log.Message("Attempted to place a trap but threw an unknown exception - recovering and ending attempt");
                        comp.lightningTraps.Clear();
                        Log.Message("Reseting trap count - this may result in unassigned traps.");
                        return;
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return placeTrap;         
        }

        public void SingleSpawnLoop(SpawnThings spawnables, Pawn pawn, IntVec3 position, Map map)
        {
            bool flag = spawnables.def != null;
            if (flag)
            {
                Faction faction = pawn.Faction;
                bool flag2 = spawnables.def.race != null;
                if (flag2)
                {
                    Log.Error("Trying to spawn a pawn instead of a building.");
                }
                else
                {
                    ThingDef def = spawnables.def;
                    ThingDef stuff = null;
                    bool madeFromStuff = def.MadeFromStuff;
                    if (madeFromStuff)
                    {
                        stuff = ThingDefOf.WoodLog;
                    }
                    Thing thing = ThingMaker.MakeThing(def, stuff);
                    thing.SetFaction(faction, null);
                    CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                    GenPlace.TryPlaceThing(thing, position, map, ThingPlaceMode.Direct);
                    //GenSpawn.Spawn(thing, position, map, Rot4.North, WipeMode.Vanish, false);
                    comp.lightningTraps.Add(thing);
                    if (comp != null && comp.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr").level >= 9)
                    {
                        Building_LightningTrap trap = thing as Building_LightningTrap;
                        if(trap != null && thing is Building_LightningTrap)
                        {
                            trap.iceTrap = true;
                            trap.extendedTrap = true;
                        }
                    }
                }
            }
            else
            {
                Log.Message("Failed to spawn trap - spawnable not defined.");
            }
        }
    }
}