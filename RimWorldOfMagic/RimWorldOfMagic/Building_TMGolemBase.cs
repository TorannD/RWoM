using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using TorannMagic.TMDefs;

namespace TorannMagic
{
    public class Building_TMGolemBase : Building_WorkTable
    {
        int activationAge = 0;
		private int ticksToActivate = 240;
		private bool activating = false;

        private TM_Golem golem;
        private List<TM_GolemUpgrade> upgrades;

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
        }

		public override void Tick()
		{
			base.Tick();
            if(activating)
            {
                if(Find.TickManager.TicksGame % 12 ==0)
                {
                    Vector3 rndPos = this.DrawPos;
                    rndPos.x += Rand.Range(-.3f, .3f);
                    rndPos.z += Rand.Range(-.3f, .3f);
                    FleckMaker.ThrowSmoke(rndPos, this.Map, Rand.Range(.6f, 1.1f));
                }
                activationAge++;
                if(activationAge >= ticksToActivate)
                {
                    activating = false;
                    SpawnGolem();
                }
            }
		}

        public virtual void SpawnGolem()
        {
            
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
