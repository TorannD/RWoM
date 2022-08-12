using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace TorannMagic
{
    public class HediffComp_SpiritPossession : HediffComp, IThingHolder
    {
        private bool initializing = true;
        private bool shouldRemove = false;
        private bool shouldUnPossess = false;
        private int failCheck = 0;

        ThingOwner innerContainer = null;

        public IThingHolder ParentHolder => ((IThingHolder)SpiritPawn).ParentHolder;

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public Pawn SpiritPawn
        {
            get
            {
                if(innerContainer != null && innerContainer.Any)
                {
                    return innerContainer.FirstOrDefault() as Pawn;
                }
                return null;
            }
            set
            {
                if(innerContainer == null)
                {
                    innerContainer = new ThingOwner<Thing>();
                    innerContainer.Clear();
                }
                innerContainer.TryAddOrTransfer(value.SplitOff(1), false);
            }
        }

        private CompAbilityUserMagic magicComp = null;
        private int spiritCheckFrequency = 2500;
        public int lastSpiritCheckTick = 0;
        private int spiritLevel = 5;
        public int SpiritLevel => spiritLevel;
        private float compatibilityRatio = -5f;
        private float effVal = 0;
        public float MaxLevelBonus = 0;
        public float CRatio => compatibilityRatio + (.5f * effVal);  

        private void UpdateSpiritCompatibilityRatio()
        {
            int matchingCount = 0;
            if (this.Pawn.story != null && this.Pawn.story.traits != null)
            {              
                foreach(TraitDef td in SpiritPawn_Hediff.traitCompatibilityList)
                {
                    foreach(Trait t in this.Pawn.story.traits.allTraits)
                    {
                        if(t.def == td)
                        {
                            matchingCount++;
                        }
                    }
                }
                foreach (Backstory bs in SpiritPawn_Hediff.BackstoryCompatibilityList)
                {
                    if(this.Pawn.story.childhood == bs || this.Pawn.story.adulthood == bs)
                    {
                        matchingCount += 2;
                    }
                }
            }
            if(this.Pawn.RaceProps != null && this.Pawn.RaceProps.Humanlike)
            {
                if(this.Pawn.gender == SpiritPawn.gender)
                {
                    matchingCount++;
                }
            }
            if (SpiritPawn != null && SpiritPawn.story != null && SpiritPawn.story.adulthood != null)
            {
                if (SpiritPawn.story.adulthood.identifier == "tm_lost_spirit")
                {
                    matchingCount += 2;
                }
                if (SpiritPawn.story.adulthood.identifier == "tm_vengeful_spirit")
                {
                    matchingCount -= 1;
                }
            }
            //assuming 
            //maximum match count of 11
            //average match count 4
            compatibilityRatio = matchingCount;
        }

        private void UpdateSpiritLevel()
        {            
            if (magicComp == null)
            {
                magicComp = this.Pawn.GetCompAbilityUserMagic();
            }            
            if (magicComp != null)
            {
                MaxLevelBonus = magicComp.MagicData.GetSkill_Versatility(TorannMagicDefOf.TM_SpiritPossession).level * 15;
                effVal = magicComp.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_SpiritPossession).level;
                lastSpiritCheckTick = Find.TickManager.TicksGame;
                spiritLevel = -4;
                TMDefs.TM_CustomClass cc = TM_ClassUtility.GetCustomClassOfTrait(TorannMagicDefOf.TM_Possessed);
                for (int i = 0; i < cc.classMageAbilities.Count; i++)
                {
                    for (int j = 0; j < magicComp.MagicData.AllMagicPowers.Count; j++)
                    {
                        MagicPower power = magicComp.MagicData.AllMagicPowers[j];
                        if (power != null && power.TMabilityDefs.Contains(cc.classMageAbilities[i]))
                        {
                            if (power.learned)
                            {
                                spiritLevel += power.learnCost;
                                spiritLevel += power.costToLevel * power.level;
                            }
                        }
                    }
                }
                foreach (TMAbilityDef ability in cc.classMageAbilities)
                {
                    MagicPowerSkill mps_e = magicComp.MagicData.GetSkill_Efficiency(ability);
                    if (mps_e != null)
                    {
                        spiritLevel += mps_e.level * mps_e.costToLevel;
                    }
                    MagicPowerSkill mps_p = magicComp.MagicData.GetSkill_Power(ability);
                    if (mps_p != null)
                    {
                        spiritLevel += mps_p.level * mps_p.costToLevel;
                    }
                    MagicPowerSkill mps_v = magicComp.MagicData.GetSkill_Versatility(ability);
                    if (mps_v != null)
                    {
                        spiritLevel += mps_v.level * mps_v.costToLevel;
                    }
                }
            }
        }

        public void UpdateSpiritEnergy()
        {
            Need nds = SpiritPawn_Need;
            Need ndp = this.Pawn.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND);
            if (nds != null && ndp != null)
            {
                nds.CurLevel = ndp.CurLevel;
            }
        }

        public Hediff_Possessor SpiritPawn_Hediff => SpiritPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessorHD) as Hediff_Possessor;
        public Need_Spirit SpiritPawn_Need => SpiritPawn.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND) as Need_Spirit;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<float>(ref this.compatibilityRatio, "compatibilityRatio", -5f);
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }

        public override string CompLabelInBracketsExtra => SpiritPawn != null ? SpiritPawn.LabelShort + ": " + CRatio.ToString("#.#") + base.CompLabelInBracketsExtra : base.CompLabelInBracketsExtra;

        public string labelCap
        {
            get
            {
                if (SpiritPawn != null)
                {
                    return base.Def.LabelCap + "(" + SpiritPawn.LabelShort + ")";
                }
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                if (SpiritPawn != null)
                {
                    return base.Def.label + "(" + SpiritPawn.LabelShort + ")";
                }
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned && base.Pawn.Map != null)
            {
                
            }
            UpdateSpiritCompatibilityRatio();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null && SpiritPawn != null;
            if (flag)
            {
                if (initializing)
                {
                    initializing = false;
                    this.Initialize();
                }
                if (Find.TickManager.TicksGame > (lastSpiritCheckTick + spiritCheckFrequency))
                {
                    lastSpiritCheckTick = Find.TickManager.TicksGame;
                    UpdateSpiritLevel();
                    if(Rand.Chance(.35f))
                    {
                        AdjustAnimalTraining();
                    }
                    if(Rand.Chance(.35f))
                    {
                        AdjustHostIdeo();
                    }
                }
                if (Find.TickManager.TicksGame % 71 == 0)
                {
                    if (SpiritPawn.DestroyedOrNull())
                    {
                        this.shouldUnPossess = true;
                    }
                    if (SpiritPawn.Dead)
                    {
                        this.shouldUnPossess = true;
                    }          
                    if(this.Pawn.Dead)
                    {
                        this.shouldUnPossess = true;
                    }

                    UpdateSpiritEnergy();
                }
                if(Find.TickManager.TicksGame % 1201 == 0)
                {
                    if(compatibilityRatio < -2f)
                    {

                    }
                    float amt = (CRatio - this.parent.Severity) * Rand.Range(.01f, .015f);
                    severityAdjustment = amt;
                }
                //do possession harmony sync
                failCheck = 0;
            }
            else
            {
                failCheck++;
                if (failCheck > 5)
                {
                    this.shouldRemove = true;
                }
            }
            if(shouldUnPossess)
            {
                this.shouldRemove = true;
            }
        }

        public void AdjustHostIdeo()
        {
            if (this.Pawn.story != null && Pawn.story.traits != null && Pawn.jobs != null)
            {
                if(ModsConfig.IdeologyActive)
                {
                    if(this.Pawn.ideo != null && this.Pawn.Ideo != SpiritPawn.Ideo)
                    {
                        this.Pawn.ideo.OffsetCertainty(Rand.Range(-.01f, -.03f));
                        if(this.Pawn.ideo.Certainty <= 0)
                        {
                            this.Pawn.ideo.IdeoConversionAttempt(-.5f, SpiritPawn.Ideo);
                        }
                    }
                }
            }            
        }
        public void AdjustAnimalTraining()
        {
            if (Pawn.kindDef != null && Pawn.kindDef.RaceProps.Animal)
            {
                bool learned = false;
                if (Pawn.training.CanAssignToTrain(TrainableDefOf.Tameness).Accepted)
                {
                    if (!Pawn.training.HasLearned(TrainableDefOf.Tameness))
                    {
                        Pawn.training.Train(TrainableDefOf.Tameness, SpiritPawn);
                        learned = true;
                    }
                }
                if (!learned && Pawn.training.CanAssignToTrain(TrainableDefOf.Obedience).Accepted)
                {
                    if (!Pawn.training.HasLearned(TrainableDefOf.Obedience))
                    {
                        Pawn.training.Train(TrainableDefOf.Obedience, SpiritPawn);
                        learned = true;
                    }
                }

                if (!learned && Pawn.training.CanAssignToTrain(TrainableDefOf.Release).Accepted)
                {
                    if (!Pawn.training.HasLearned(TrainableDefOf.Release))
                    {
                        Pawn.training.Train(TrainableDefOf.Release, SpiritPawn);
                        learned = true;
                    }
                }

                if (!learned && Pawn.training.CanAssignToTrain(TorannMagicDefOf.Haul).Accepted)
                {
                    if (!Pawn.training.HasLearned(TorannMagicDefOf.Haul))
                    {
                        Pawn.training.Train(TorannMagicDefOf.Haul, SpiritPawn);
                        learned = true;
                    }
                }

                if (!learned && Pawn.training.CanAssignToTrain(TorannMagicDefOf.Rescue).Accepted)
                {
                    if (!Pawn.training.HasLearned(TorannMagicDefOf.Rescue))
                    {
                        Pawn.training.Train(TorannMagicDefOf.Rescue, SpiritPawn);
                        learned = true;
                    }
                }
            }
        }

        public override bool CompShouldRemove => base.CompShouldRemove || this.shouldRemove;
        
    }
}
