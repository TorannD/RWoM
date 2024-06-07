﻿using System;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace TorannMagic.WorldTransport
{
    public class TM_ActiveDropPod : Thing, IActiveDropPod, IThingHolder
    {
        public int age;
        public bool draftFlag = false;
        private ActiveDropPodInfo contents;

        public ActiveDropPodInfo Contents
        {
            get
            {
                return contents;
            }
            set
            {
                if (contents != null)
                {
                    contents.parent = null;
                }
                if (value != null)
                {
                    value.parent = this;
                }
                contents = value;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref age, "age", 0);
            Scribe_Deep.Look(ref contents, "contents", this);
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return null;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
            if (contents != null)
            {
                outChildren.Add(contents);
            }
        }
        public override void Tick()
        {
            if (contents != null)
            {
                contents.innerContainer.ThingOwnerTick();
                if (base.Spawned)
                {
                    age++;
                    if (age > contents.openDelay)
                    {
                        PodOpen();
                    }
                }
            }
        }

        private void PodOpen()
        {
            Map map = base.Map;
            if (contents.despawnPodBeforeSpawningThing)
            {
                DeSpawn();
            }
            for (int num = contents.innerContainer.Count - 1; num >= 0; num--)
            {
                Thing thing = contents.innerContainer[num];
                Rot4 rot = contents.setRotation.HasValue ? contents.setRotation.Value : Rot4.North;
                if (contents.moveItemsAsideBeforeSpawning)
                {
                    GenSpawn.CheckMoveItemsAside(base.Position, rot, thing.def, map);
                }
                Thing lastResultingThing = default(Thing);
                if (contents.spawnWipeMode.HasValue)
                {
                    lastResultingThing = ((!contents.setRotation.HasValue) ? GenSpawn.Spawn(thing, base.Position, map, contents.spawnWipeMode.Value) : GenSpawn.Spawn(thing, base.Position, map, contents.setRotation.Value, contents.spawnWipeMode.Value));
                }
                else
                {
                    GenPlace.TryPlaceThing(thing, base.Position, map, ThingPlaceMode.Near, out lastResultingThing, delegate (Thing placedThing, int count)
                    {
                        if (Find.TickManager.TicksGame < 1200 && TutorSystem.TutorialMode && placedThing.def.category == ThingCategory.Item)
                        {
                            Find.TutorialState.AddStartingItem(placedThing);
                        }
                    }, null, rot);
                }
                Pawn pawn = lastResultingThing as Pawn;
                if (pawn != null)
                {                    
                    if (pawn.RaceProps.Humanlike)
                    {
                        TaleRecorder.RecordTale(TaleDefOf.LandedInPod, pawn);
                    }
                    if (pawn.drafter != null && ((pawn.IsColonist && pawn.Spawned && !map.IsPlayerHome) || draftFlag))
                    {                        
                        pawn.drafter.Drafted = true;
                    }
                    if (pawn.guest != null && pawn.guest.IsPrisoner)
                    {
                        pawn.guest.WaitInsteadOfEscapingForDefaultTicks();
                    }
                }
            }
            contents.innerContainer.ClearAndDestroyContents();
            if (contents.leaveSlag)
            {
                for (int i = 0; i < 1; i++)
                {
                    GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel), base.Position, map, ThingPlaceMode.Near);
                }
            }
            TorannMagicDefOf.DropPod_Open.PlayOneShot(new TargetInfo(base.Position, map));
            Destroy();
        }
    }
}
