using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using TorannMagic.TMDefs;
using AbilityUser;

namespace TorannMagic.Golems
{
    public class Building_TMGolemBase : Building_WorkTable, IThingHolder
    {
        int activationAge = 0;
		private bool activating = false;

        public Pawn tmpGolem;

        ThingOwner innerContainer;

        private TM_Golem golem;
        private List<TM_GolemUpgrade> upgrades;

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public TM_Golem Golem
        {
            get
            {
                if(golem == null)
                {
                    golem = TM_GolemUtility.GetGolemFromThingDef(this.def);
                }
                return golem;
            }
        }

        public List<TM_GolemUpgrade> Upgrades
        {
            get
            {
                if (upgrades == null)
                {
                    upgrades = new List<TM_GolemUpgrade>();
                    upgrades.Clear();
                    upgrades.AddRange(Golem.upgrades);
                }
                return upgrades;
            }
        }

        public bool CanUpgrade(RecipeDef rec)
        {
            foreach (TM_GolemUpgrade gu in upgrades)
            {
                if (gu.recipe == rec)
                {
                    return gu.currentLevel < gu.maxLevel;
                }
            }
            return true;
        }

        public void IncreaseUpgrade_Recipe(RecipeDef rec)
        {
            foreach(TM_GolemUpgrade gu in upgrades)
            {
                if(gu.recipe == rec)
                {
                    gu.currentLevel++;
                }
            }
        }

		public override void ExposeData()
		{
			base.ExposeData();
            Scribe_Values.Look<bool>(ref this.activating, "activating");
            Scribe_Values.Look<int>(ref this.activationAge, "activationAge", 0);
            Scribe_Collections.Look<TM_GolemUpgrade>(ref this.upgrades, "upgrades", LookMode.Deep);
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }

        public Building_TMGolemBase()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

		public override void Tick()
		{
			base.Tick();
            if(tmpGolem != null)
            {
                innerContainer.TryAddOrTransfer(tmpGolem.SplitOff(1), false);
                tmpGolem = null;
            }
            if(activating)
            {
                if(Find.TickManager.TicksGame % Mathf.RoundToInt(Golem.activationTicks * .05f) == 0)
                {
                    Vector3 rndPos = this.DrawPos;
                    rndPos.x += Rand.Range(-.6f, .6f);
                    rndPos.z += Rand.Range(-.6f, .6f);
                    FleckMaker.ThrowSmoke(rndPos, this.Map, Rand.Range(.6f, 1.1f));
                }
                if(activationAge >= (.9f * Golem.activationTicks) && Find.TickManager.TicksGame % 6 == 0)
                {
                    Find.CameraDriver.shaker.DoShake(.05f);
                }
                activationAge++;
                if(activationAge >= Golem.activationTicks)
                {
                    activating = false;
                    SpawnGolem();
                }
            }
		}

        public virtual void SpawnGolem()
        {
            TMPawnSummoned spawnedThing = null;
            if (innerContainer != null && innerContainer.Any)
            {
                Pawn p = innerContainer.FirstOrDefault() as Pawn;
                GenPlace.TryPlaceThing(p, this.Position, this.Map, ThingPlaceMode.Near, null, null, this.Rotation);
                spawnedThing = p as TMPawnSummoned;
                //spawnedThing.validSummoning = true;
            }
            else
            {
                AbilityUser.SpawnThings spawnables = new SpawnThings();
                spawnables.def = Golem.golemDef;
                spawnables.kindDef = Golem.golemKindDef;
                spawnables.spawnCount = 1;
                
                bool flag = spawnables.def != null;
                if (flag)
                {
                    spawnedThing = TM_Action.SingleSpawnLoop(null, spawnables, this.Position, this.Map, 0, false, false, this.Faction) as TMPawnSummoned;
                    spawnedThing.validSummoning = true;
                    spawnedThing.ageTracker.AgeBiologicalTicks = 0;
                    //ThingDef def = spawnables.def;
                    //spawnedThing = ThingMaker.MakeThing(def);
                    //Log.Message("faction is " + this.Faction);
                    //spawnedThing.SetFaction(this.Faction, null);
                    //GenSpawn.Spawn(spawnedThing, this.Position, this.Map, this.Rotation, WipeMode.Vanish, true);
                }
            }
            if(spawnedThing != null)
            {
                CompGolem cg = spawnedThing.TryGetComp<CompGolem>();
                if(cg != null)
                {
                    cg.dormantPosition = this.Position;
                    cg.CopyUpgrades(upgrades);
                    cg.age = 0;
                    cg.dormantThing = this;                    
                }
            }
        }

		public override void Draw()
		{
			base.Draw();
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (Prefs.DevMode)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "TM_ActivateGolem".Translate();
                command_Action.defaultDesc = "TM_ActivateGolemDesc".Translate();
                command_Action.icon = ContentFinder<Texture2D>.Get("UI/MoveOut", true);
                command_Action.action = delegate
                {
                    activating = !activating;
                };
                yield return command_Action;
            }            
        }
	}
}
