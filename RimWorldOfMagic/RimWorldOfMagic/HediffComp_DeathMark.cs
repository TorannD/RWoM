using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_DeathMark : HediffComp
    {
        Pawn instigator;
        private bool initializing = true;

        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned)
            {
                FleckMaker.ThrowLightningGlow(base.Pawn.TrueCenter(), base.Pawn.Map, 3f);
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (initializing)
                {
                    initializing = false;
                    this.Initialize();
                }
            }
            
            if (Find.TickManager.TicksGame % 60 == 0)
            {                
                
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_DeathMarkHD, false);
                if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD))
                {
                    this.Pawn.health.RemoveHediff(hediff);
                }
                else
                {
                    severityAdjustment--;
                    if (hediff.Severity < 1)
                    {
                        foreach (Pawn current in this.Pawn.Map.mapPawns.AllPawns)
                        {
                            if (current.RaceProps.Humanlike)
                            {
                                if (current.story.traits.HasTrait(TorannMagicDefOf.Necromancer))
                                {
                                    instigator = current;
                                }
                            }
                        }
                        Reanimate(this.Pawn, instigator);
                        this.Pawn.health.RemoveHediff(hediff);
                    }
                }
            }

        }

        public void Reanimate(Pawn undeadPawn, Pawn instigator)
        {
            if (!instigator.Dead && !instigator.Downed)
            {
                MagicPowerSkill ver = instigator.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_RaiseUndead.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_RaiseUndead_ver");
                undeadPawn.SetFaction(instigator.Faction);
                IntVec3 screamPos = undeadPawn.Position;
                screamPos.z += 2;
                screamPos.x -= 1;
                TM_MoteMaker.ThrowScreamMote(screamPos.ToVector3(), undeadPawn.Map, 2f, 216, 255, 0);                

                if (!undeadPawn.kindDef.RaceProps.Animal && undeadPawn.kindDef.RaceProps.Humanlike)
                {
                    HealthUtility.AdjustSeverity(undeadPawn, TorannMagicDefOf.TM_UndeadHD, -4f);
                    HealthUtility.AdjustSeverity(undeadPawn, TorannMagicDefOf.TM_UndeadHD, .5f + ver.level);
                    //RemoveTraits(undeadPawn, undeadPawn.story.traits.allTraits);
                    //RedoSkills(undeadPawn);
                    undeadPawn.story.traits.GainTrait(new Trait(TraitDef.Named("Undead"), 0, false));
                    CompAbilityUserMagic undeadComp = undeadPawn.GetCompAbilityUserMagic();
                    if (undeadComp.IsMagicUser)
                    {
                        //undeadComp.ClearPowers();
                    }
                    List<SkillRecord> skills = undeadPawn.skills.skills;
                    for (int j = 0; j < skills.Count; j++)
                    {
                        skills[j].passion = Passion.None;
                    }
                    undeadPawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
                    if(!undeadPawn.IsColonist)
                    {
                        undeadPawn.ClearMind();
                        undeadPawn.mindState.enemyTarget = instigator.TargetCurrentlyAimingAt.Thing;
                        undeadPawn.HostileTo(Faction.OfPlayer);
                        undeadPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, true, false, null);
                    }
                }
                if (undeadPawn.kindDef.RaceProps.Animal)
                {
                    HealthUtility.AdjustSeverity(undeadPawn, TorannMagicDefOf.TM_UndeadAnimalHD, -4f);
                    HealthUtility.AdjustSeverity(undeadPawn, TorannMagicDefOf.TM_UndeadAnimalHD, .5f + ver.level);
                    
                    //if (undeadPawn.RaceProps.TrainableIntelligence == TrainableIntelligenceDefOf.Intermediate)
                    //{
                    //    while (!undeadPawn.training.IsCompleted(TrainableDefOf.Obedience))
                    //    {
                    //        undeadPawn.training.Train(TrainableDefOf.Obedience, instigator);
                    //    }
                    //    while (!undeadPawn.training.IsCompleted(TrainableDefOf.Release))
                    //    {
                    //        undeadPawn.training.Train(TrainableDefOf.Release, instigator);
                    //    }
                    //}

                    //if (undeadPawn.RaceProps.TrainableIntelligence == TrainableIntelligenceDefOf.Advanced)
                    //{
                    //    while (!undeadPawn.training.IsCompleted(TrainableDefOf.Obedience))
                    //    {
                    //        undeadPawn.training.Train(TrainableDefOf.Obedience, instigator);
                    //    }
                    //    while (!undeadPawn.training.IsCompleted(TrainableDefOf.Release))
                    //    {
                    //        undeadPawn.training.Train(TrainableDefOf.Release, instigator);
                    //    }
                    //    if (undeadPawn.BodySize > .4)
                    //    {
                    //        while (!undeadPawn.training.IsCompleted(TorannMagicDefOf.Haul))
                    //        {
                    //            undeadPawn.training.Train(TorannMagicDefOf.Haul, instigator);
                    //        }
                    //    }
                    //}
                }
            }
        }

        private void RedoSkills(Pawn undeadPawn)
        {
            undeadPawn.story.childhood = null;
            undeadPawn.story.adulthood = null;
            //undeadPawn.story.DisabledWorkTypes.Clear();
            //undeadPawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Warden);
            //undeadPawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Hunting);
            //undeadPawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Handling);
            //undeadPawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Doctor);
            undeadPawn.skills.Learn(SkillDefOf.Shooting, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Animals, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Artistic, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Cooking, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Cooking, Rand.Range(10000, 20000), true);
            undeadPawn.skills.Learn(SkillDefOf.Crafting, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Crafting, Rand.Range(10000, 50000), true);
            undeadPawn.skills.Learn(SkillDefOf.Plants, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Plants, Rand.Range(40000, 60000), true);
            undeadPawn.skills.Learn(SkillDefOf.Intellectual, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Medicine, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Melee, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Melee, Rand.Range(60000, 100000), true);
            undeadPawn.skills.Learn(SkillDefOf.Mining, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Mining, Rand.Range(40000, 80000), true);
            undeadPawn.skills.Learn(SkillDefOf.Social, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Construction, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Construction, Rand.Range(40000, 60000), true);
        }

        private void RemoveTraits(Pawn pawn, List<Trait> traits)
        {
            for (int i = 0; i < traits.Count; i++)
            {
                traits.Remove(traits[i]);
                i--;
            }
        }

    }
}
