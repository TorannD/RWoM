using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace TorannMagic
{
    public class MagicRecipeDef : RecipeDef
    {
        //Number of mages required to perform the job
        public int mageCount = 1;

        //Total mana required to perform the job, divided by mageCount for mana cost of each mage
        public float manaCost;

        //Chance spell fails and resulting effects
        public float failChance = 0;
        public float failManaConsumed = 1f; //1=100% of mana cost is consumed on failure
        public float failDamageApplied = 0f; //Max amount of damage that may be applied on a failure; no damage applied on 0

        //Incident the recipe generates
        //public IncidentDef resultIncident = new IncidentDef();
        //public int incidentPoints = 0;
        public List<TMDefs.TM_Incident> resultIncidents = new List<TMDefs.TM_Incident>();
        public bool selectRandomIncident = false;
        public int selectRandomIncidentCount = 0;

        //Game condition the recipe generates
        //public GameConditionDef resultCondition = new GameConditionDef();
        //public int conditionDuration = -1;
        //public bool conditionPermanent = false;
        public List<TMDefs.TM_Condition> resultConditions = new List<TMDefs.TM_Condition>();

        ////Hediff the recipe generates
        //public HediffDef resultHediff = new HediffDef();
        ////What the hediff applies to
        //public bool applyFriendly = false;
        //public bool applyEnemy = false;
        //public bool applyNeutral = false;
        //public bool applyNullFaction = false;
        ////Number of pawns the hediff applies to, 0 applies to all
        //public int maxHediffCount = 0;
        //public float hediffSeverity = .5f;
        public List<TMDefs.TM_Hediff> resultHediffs = new List<TMDefs.TM_Hediff>();

        ////ThingDef the recipe spawns
        //public ThingDef resultSpawnThing = new ThingDef();
        ////Number of things to spawn
        //public int spawnThingCount = 0;
        public List<TMDefs.TM_SpawnThings> resultSpawnThings = new List<TMDefs.TM_SpawnThings>();

        //Magic Map component actions
        public List<string> resultMapComponentConditions = new List<string>();
        public float mapComponentConditionValue = 0f;
    }
}
