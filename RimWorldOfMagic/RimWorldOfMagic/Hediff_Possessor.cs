using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld.Planet;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Hediff_Possessor : HediffWithCompsExtra
    {
        private bool initialized = false;
        private bool removeNow = false;

        private int eventFrequency = 60;

        private int pwrVal = 0;  //increased maximum hediff value
        private int verVal = 0;  //increased amount of herbs found during harvesting
        private int effVal = 0;  //reduces expiration rate

        private float possessionCompatibility = .5f;
        public float PossessionCompatibility
        {
            get => possessionCompatibility;
            set
            {
                possessionCompatibility = value;
            }
        }

        public FactionDef previousFaction = null;
        public bool wasDead = false;

        private int spiritLevel = 0;
        public int SpiritLevel => spiritLevel;
        public List<TraitDef> traitCompatibilityList = null;
        public List<Backstory> backstoryCompatibilityList = null;
        public List<string> backstoryIdentifiers = null;

        public List<Backstory> BackstoryCompatibilityList
        {
            get
            {
                if (backstoryCompatibilityList != null) return backstoryCompatibilityList;
                PopulateBackstoryList();
                return backstoryCompatibilityList;                
            }
        }

        public float MaxLevelBonus => verVal * 15;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false);
            Scribe_Values.Look<bool>(ref wasDead, "wasDead", false);
            Scribe_Values.Look<int>(ref pwrVal, "pwrVal", 0);
            Scribe_Values.Look<int>(ref verVal, "verVal", 0);
            Scribe_Values.Look<int>(ref effVal, "effVal", 0);
            Scribe_Values.Look<int>(ref spiritLevel, "spiritLevel", 0);
            Scribe_Values.Look<float>(ref possessionCompatibility, "possessionCompatibility", .5f);
            Scribe_Defs.Look<FactionDef>(ref previousFaction, "previousFaction");
            Scribe_Collections.Look<TraitDef>(ref traitCompatibilityList, "traitCompatibilityList", LookMode.Def);
            Scribe_Collections.Look<string>(ref backstoryIdentifiers, "backstoryIdentifiers", LookMode.Value);
        }

        //public override string GizmoLabel => "TM_ApothecaryHerbsGizmoLabel".Translate();
        //public override float MaxSeverity => def.maxSeverity + (10 * herbPwr);
        public override bool ShouldRemove => this.removeNow;

        private void Initialize()
        {
            UpdateSkills();
            GenerateCompatibility();
        }

        public void UpdateSpiritLevel()
        {
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if(comp != null)
            {
                spiritLevel = comp.MagicData.MagicUserLevel;
            }
        }

        public void UpdateSkills()
        {
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if (comp != null)
            {
                pwrVal = TM_Calc.GetMagicSkillLevel(pawn, TorannMagicDefOf.TM_Possess, "_pwr", false);
                verVal = TM_Calc.GetMagicSkillLevel(pawn, TorannMagicDefOf.TM_Possess, "_ver", false);
                effVal = TM_Calc.GetMagicSkillLevel(pawn, TorannMagicDefOf.TM_Possess, "_eff", false);
            }
            this.Severity = .5f + pwrVal;
        }

        public void GenerateCompatibility()
        {
            this.traitCompatibilityList = new List<TraitDef>();
            traitCompatibilityList.Clear();
            foreach(TraitDef def in DefDatabase<TraitDef>.AllDefs)
            {
                if(Rand.Chance(.4f))
                {
                    traitCompatibilityList.Add(def);
                }
            }
            this.backstoryCompatibilityList = new List<Backstory>();
            backstoryCompatibilityList.Clear();
            backstoryIdentifiers = new List<string>();
            backstoryIdentifiers.Clear();
            for(int i = 0; i < BackstoryDatabase.allBackstories.Count; i++)
            {
                Backstory rnd = null;
                if (Rand.Chance(.5f))
                {
                    rnd = BackstoryDatabase.RandomBackstory(BackstorySlot.Adulthood);
                }
                else
                {
                    rnd = BackstoryDatabase.RandomBackstory(BackstorySlot.Childhood);
                }
                if(rnd != null && !backstoryCompatibilityList.Contains(rnd))
                {
                    backstoryIdentifiers.Add(rnd.identifier);
                    backstoryCompatibilityList.Add(rnd);
                }
            }
        }

        public void PopulateBackstoryList()
        {
            if (backstoryIdentifiers != null && backstoryIdentifiers.Count > 0)
            {
                backstoryCompatibilityList = new List<Backstory>();
                backstoryCompatibilityList.Clear();
                foreach (string s in backstoryIdentifiers)
                {
                    Backstory bs = null;
                    BackstoryDatabase.TryGetWithIdentifier(s, out bs, false);
                    if (bs != null)
                    {
                        backstoryCompatibilityList.Add(bs);
                    }
                }
            }
            else
            {
                GenerateCompatibility();
            }
        }

        public override void PostTick()
        {
            base.PostTick();
            if(!initialized)
            {
                this.initialized = true;
                Initialize();
            }            
            if (Find.TickManager.TicksGame % 31 == 0)
            {
                IEnumerable<Hediff> hdEnum = this.pawn.health.hediffSet.GetHediffs<Hediff>();
                foreach (Hediff hd in hdEnum)
                {
                    if (hd.def.defName == "SpaceHypoxia")
                    {
                        this.pawn.health.RemoveHediff(hd);
                        break;
                    }
                }
            }
            if (Find.TickManager.TicksGame % 150 == 0)
            {
                if (backstoryCompatibilityList == null)
                {
                    PopulateBackstoryList();
                }
            }
            if(Find.TickManager.TicksGame % 2501 == 0)
            {
                UpdateSpiritLevel();
                UpdateSkills();
            }
        }        
    }
}
