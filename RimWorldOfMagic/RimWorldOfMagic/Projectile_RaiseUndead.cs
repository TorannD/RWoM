using Verse;
using RimWorld;
using AbilityUser;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using HarmonyLib;
using TorannMagic.TMDefs;

namespace TorannMagic
{
    class Projectile_RaiseUndead : Projectile_AbilityBase
    {
        MagicPowerSkill pwr;
        MagicPowerSkill ver;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            int raisedPawns = 0;

            Pawn pawn = this.launcher as Pawn;
            Pawn victim = hitThing as Pawn;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            pwr = comp.MagicData.MagicPowerSkill_RaiseUndead.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_RaiseUndead_pwr");
            ver = comp.MagicData.MagicPowerSkill_RaiseUndead.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_RaiseUndead_ver");

            Thing corpseThing = null;
            
            IntVec3 curCell;
            IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(base.Position, this.def.projectile.explosionRadius, true);
            for (int i = 0; i < targets.Count(); i++)
            {
                curCell = targets.ToArray<IntVec3>()[i];

                TM_MoteMaker.ThrowPoisonMote(curCell.ToVector3Shifted(), map, Rand.Range(.3f, .6f));
                if (curCell.InBoundsWithNullCheck(map))
                { 
                    Corpse corpse = null;
                    List<Thing> thingList;
                    thingList = curCell.GetThingList(map);
                    int z = 0;
                    while (z < thingList.Count)
                    {
                        corpseThing = thingList[z];
                        if (corpseThing != null)
                        {
                            if (corpseThing is Corpse)
                            {
                                corpse = corpseThing as Corpse;
                                Pawn undeadPawn = corpse.InnerPawn;
                                CompRottable compRottable = corpse.GetComp<CompRottable>();
                                float rotStage = 0;
                                if (compRottable != null && compRottable.Stage == RotStage.Dessicated)
                                {
                                    rotStage = 1f;
                                }
                                if (compRottable != null)
                                {
                                    rotStage += compRottable.RotProgressPct;
                                }
                                bool flag_SL = false;
                                if (undeadPawn.def.defName == "SL_Runner" || undeadPawn.def.defName == "SL_Peon" || undeadPawn.def.defName == "SL_Archer" || undeadPawn.def.defName == "SL_Hero")
                                {
                                    PawnGenerationRequest pgr = new PawnGenerationRequest(PawnKindDef.Named("Tribesperson"), pawn.Faction, PawnGenerationContext.NonPlayer, -1, true, false, false, false, true, 0, false, false, false, false, false, false, false, false, true, 0, 0, null, 0);
                                    Pawn newUndeadPawn = PawnGenerator.GeneratePawn(pgr);
                                    GenSpawn.Spawn(newUndeadPawn, corpse.Position, corpse.Map, WipeMode.Vanish);
                                    corpse.Strip();
                                    corpse.Destroy(DestroyMode.Vanish);
                                    rotStage = 1f;
                                    flag_SL = true;
                                    undeadPawn = newUndeadPawn;
                                }
                                if (!undeadPawn.def.defName.Contains("ROM_") && !undeadPawn.IsEntity && undeadPawn.RaceProps.IsFlesh && (undeadPawn.Dead || flag_SL) && !(undeadPawn is TMPawnSummoned) && !(undeadPawn is Golems.TMPawnGolem))
                                { 
                                    bool wasVampire = false;

                                    IEnumerable<ThingDef> enumerable = from hd in DefDatabase<HediffDef>.AllDefs
                                                                       where (def.defName == "ROM_Vampirism")
                                                                       select def;
                                    if (enumerable.Count() > 0)
                                    {
                                        bool hasVampHediff = undeadPawn.health.hediffSet.HasHediff(HediffDef.Named("ROM_Vampirism")) || undeadPawn.health.hediffSet.HasHediff(HediffDef.Named("ROM_GhoulHediff"));
                                        if (hasVampHediff)
                                        {
                                            wasVampire = true;
                                        }
                                    }

                                    if (!wasVampire)
                                    {                                        
                                        if (undeadPawn.Faction != pawn.Faction)
                                        {
                                            undeadPawn.SetFaction(pawn.Faction);
                                        }
                                        if (undeadPawn.Dead)
                                        {
                                            //if(undeadPawn.workSettings != null && undeadPawn.story != null && undeadPawn.story.traits != null && undeadPawn.story.traits.HasTrait(TorannMagicDefOf.Undead))
                                            //{
                                            //    Log.Message("copying old priorities");
                                            //    DefMap<WorkTypeDef, int> tmppriorities = Traverse.Create(root: undeadPawn.workSettings).Field(name: "priorities").GetValue<DefMap<WorkTypeDef, int>>();
                                            //    priorities = new DefMap<WorkTypeDef, int>();
                                            //    foreach(WorkTypeDef item in from w in DefDatabase<WorkTypeDef>.AllDefs select w)
                                            //    {
                                            //        priorities[item] = tmppriorities[item];
                                            //    }                                                    
                                            //}
                                            ResurrectionUtility.TryResurrect(undeadPawn);
                                        }
                                        raisedPawns++;
                                        comp.supportedUndead.Add(undeadPawn);
                                        if (undeadPawn.kindDef != null && undeadPawn.kindDef.RaceProps != null && undeadPawn.kindDef.RaceProps.Animal)
                                        {
                                            RemoveHediffsAddictionsAndPermanentInjuries(undeadPawn);
                                            HealthUtility.AdjustSeverity(undeadPawn, TorannMagicDefOf.TM_UndeadAnimalHD, -4f);
                                            HealthUtility.AdjustSeverity(undeadPawn, TorannMagicDefOf.TM_UndeadAnimalHD, .5f + ver.level);
                                            undeadPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_UndeadAnimalHD).TryGetComp<HediffComp_Undead>().linkedPawn = pawn;
                                            HealthUtility.AdjustSeverity(undeadPawn, HediffDef.Named("TM_UndeadStageHD"), -2f);
                                            HealthUtility.AdjustSeverity(undeadPawn, HediffDef.Named("TM_UndeadStageHD"), rotStage);

                                            if (undeadPawn.training.CanAssignToTrain(TrainableDefOf.Tameness).Accepted)
                                            {
                                                while (!undeadPawn.training.HasLearned(TrainableDefOf.Tameness))
                                                {
                                                    undeadPawn.training.Train(TrainableDefOf.Tameness, pawn);
                                                }
                                            }

                                            if (undeadPawn.training.CanAssignToTrain(TrainableDefOf.Obedience).Accepted)
                                            {
                                                while (!undeadPawn.training.HasLearned(TrainableDefOf.Obedience))
                                                {
                                                    undeadPawn.training.Train(TrainableDefOf.Obedience, pawn);
                                                }
                                            }

                                            if (undeadPawn.training.CanAssignToTrain(TrainableDefOf.Release).Accepted)
                                            {
                                                while (!undeadPawn.training.HasLearned(TrainableDefOf.Release))
                                                {
                                                    undeadPawn.training.Train(TrainableDefOf.Release, pawn);
                                                }
                                            }

                                            if (undeadPawn.training.CanAssignToTrain(TorannMagicDefOf.Haul).Accepted)
                                            {
                                                while (!undeadPawn.training.HasLearned(TorannMagicDefOf.Haul))
                                                {
                                                    undeadPawn.training.Train(TorannMagicDefOf.Haul, pawn);
                                                }
                                            }

                                            if (undeadPawn.training.CanAssignToTrain(TorannMagicDefOf.Rescue).Accepted)
                                            {
                                                while (!undeadPawn.training.HasLearned(TorannMagicDefOf.Rescue))
                                                {
                                                    undeadPawn.training.Train(TorannMagicDefOf.Rescue, pawn);
                                                }
                                            }
                                            if (undeadPawn.playerSettings != null)
                                            {
                                                undeadPawn.playerSettings.medCare = MedicalCareCategory.NoMeds;
                                            }
                                            undeadPawn.def.tradeability = Tradeability.None;
                                        }
                                        else if (undeadPawn.story != null && undeadPawn.story.traits != null && undeadPawn.needs != null)
                                        {
                                            if (ModsConfig.IdeologyActive && undeadPawn.guest != null && undeadPawn.IsColonist)
                                            {
                                                undeadPawn.guest.SetGuestStatus(pawn.Faction, GuestStatus.Slave);
                                            }

                                            CompAbilityUserMagic compMagic = undeadPawn.GetCompAbilityUserMagic();
                                            if (compMagic != null && TM_Calc.IsMagicUser(undeadPawn)) //(compMagic.IsMagicUser && !undeadPawn.story.traits.HasTrait(TorannMagicDefOf.Faceless)) || 
                                            {
                                                compMagic.Initialize();
                                                compMagic.RemovePowers(true);
                                            }
                                            CompAbilityUserMight compMight = undeadPawn.GetCompAbilityUserMight();
                                            if (compMight != null && TM_Calc.IsMightUser(undeadPawn)) //compMight.IsMightUser || 
                                            {
                                                compMight.Initialize();
                                                compMight.RemovePowers(true);
                                            }
                                            RemoveHediffsAddictionsAndPermanentInjuries(undeadPawn);
                                            RemovePsylinkAbilities(undeadPawn);
                                            RemoveGenes(undeadPawn);
                                            TM_Action.TryCopyIdeo(pawn, undeadPawn);
                                            HealthUtility.AdjustSeverity(undeadPawn, TorannMagicDefOf.TM_UndeadHD, -4f);
                                            HealthUtility.AdjustSeverity(undeadPawn, TorannMagicDefOf.TM_UndeadHD, .5f + ver.level);
                                            undeadPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_UndeadHD).TryGetComp<HediffComp_Undead>().linkedPawn = pawn;
                                            HealthUtility.AdjustSeverity(undeadPawn, HediffDef.Named("TM_UndeadStageHD"), -2f);
                                            HealthUtility.AdjustSeverity(undeadPawn, HediffDef.Named("TM_UndeadStageHD"), rotStage);
                                            if (!undeadPawn.story.traits.HasTrait(TorannMagicDefOf.Undead))
                                            {
                                                RedoSkills(undeadPawn, pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_LichHD")));
                                            }
                                            SetOutfitRestrictions(undeadPawn);
                                            if (undeadPawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
                                            {
                                                compMagic.RemovePawnAbility(TorannMagicDefOf.TM_ChaosTradition);
                                            }
                                            RemoveTraits(undeadPawn, undeadPawn.story.traits.allTraits);
                                            undeadPawn.story.traits.GainTrait(new Trait(TraitDef.Named("Undead"), 0, false));
                                            undeadPawn.story.traits.GainTrait(new Trait(TraitDef.Named("Psychopath"), 0, false));
                                            undeadPawn.needs.AddOrRemoveNeedsAsAppropriate();
                                            RemoveClassHediff(undeadPawn);
                                            if (undeadPawn.health.hediffSet.HasHediff(HediffDef.Named("DeathAcidifier")))
                                            {
                                                Hediff hd = undeadPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("DeathAcidifier"));
                                                undeadPawn.health.RemoveHediff(hd);
                                            }
                                            
                                            //Color undeadColor = new Color(.2f, .4f, 0);
                                            //undeadPawn.story.hairColor = undeadColor;
                                            //CompAbilityUserMagic undeadComp = undeadPawn.GetCompAbilityUserMagic();
                                            //if (undeadComp.IsMagicUser)
                                            //{
                                            //    undeadComp.ClearPowers();
                                            //}

                                            List<SkillRecord> skills = undeadPawn.skills.skills;
                                            for (int j = 0; j < skills.Count; j++)
                                            {
                                                skills[j].passion = Passion.None;
                                            }
                                            if (undeadPawn.playerSettings != null)
                                            {
                                                undeadPawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
                                                undeadPawn.playerSettings.medCare = MedicalCareCategory.NoMeds;
                                            }
                                            if (undeadPawn.IsColonist)
                                            {
                                                for (int h = 0; h < 24; h++)
                                                {
                                                    undeadPawn.timetable.SetAssignment(h, TimeAssignmentDefOf.Work);
                                                }
                                            }
                                            for(int m = 0; m < 3; m++)
                                            {
                                                TM_MoteMaker.ThrowPoisonMote(undeadPawn.Position.ToVector3Shifted(), map, Rand.Range(.4f, .6f));
                                            }
                                            //if(priorities != null)
                                            //{
                                            //    Log.Message("loading priorities");
                                            //    Traverse.Create(root: undeadPawn).Field(name: "priorities").SetValue(priorities);
                                            //}
                                        }
                                    }
                                    else
                                    {
                                        Messages.Message("Vampiric powers have prevented undead reanimation of " + undeadPawn.LabelShort, MessageTypeDefOf.RejectInput);
                                    }
                                }
                            }
                            else if (corpseThing is Pawn undeadPawn)
                            {
                                if(TM_Calc.IsUndead(undeadPawn))
                                {
                                    RemoveHediffsAddictionsAndPermanentInjuries(undeadPawn);
                                    TM_MoteMaker.ThrowPoisonMote(curCell.ToVector3Shifted(), map, .6f);
                                }
                                if(corpseThing != pawn && !TM_Calc.IsNecromancer(undeadPawn) && !TM_Calc.IsUndead(undeadPawn))
                                {
                                    DisruptiveRemoveHediffs(undeadPawn);
                                    TM_MoteMaker.ThrowPoisonMote(curCell.ToVector3Shifted(), map, .6f);
                                }
                            }
                        }
                        z++;
                    }
                }
                if (raisedPawns > pwr.level + 1)
                {
                    i = targets.Count();
                }
            }
        }

        private void RedoSkills(Pawn undeadPawn, bool lichBonus = false)
        {                       

            //undeadPawn.story.Childhood = null;
            //undeadPawn.story.Adulthood = null;
            float bonusSkill = 1f + (.1f * pwr.level);
            if(lichBonus)
            {
                bonusSkill *= 1.2f;
            }
            //undeadPawn.story.DisabledWorkTypes.Clear();
            //undeadPawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Warden);
            //undeadPawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Hunting);
            //undeadPawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Handling);
            //undeadPawn.story.WorkTypeIsDisabled(WorkTypeDefOf.Doctor);           

            //undeadPawn.skills.Learn(SkillDefOf.Shooting, -100000000, true);            
            //undeadPawn.skills.Learn(SkillDefOf.Animals, -100000000, true);
            //undeadPawn.skills.Learn(SkillDefOf.Artistic, -100000000, true);
            //undeadPawn.skills.Learn(SkillDefOf.Cooking, -100000000, true);
            //undeadPawn.skills.Learn(SkillDefOf.Cooking, Rand.Range(10000, 30000)*bonusSkill, true);            
            //undeadPawn.skills.Learn(SkillDefOf.Crafting, -100000000, true);
            //undeadPawn.skills.Learn(SkillDefOf.Crafting, Rand.Range(10000, 50000) * bonusSkill, true);
            //undeadPawn.skills.Learn(SkillDefOf.Plants, -100000000, true);
            //undeadPawn.skills.Learn(SkillDefOf.Plants, Rand.Range(25000, 50000) * bonusSkill, true);
            //undeadPawn.skills.Learn(SkillDefOf.Intellectual, -10000000, true);
            //undeadPawn.skills.Learn(SkillDefOf.Medicine, -10000000, true);
            //undeadPawn.skills.Learn(SkillDefOf.Melee, -10000000, true);
            //undeadPawn.skills.Learn(SkillDefOf.Melee, Rand.Range(30000, 60000) * bonusSkill, true);
            //undeadPawn.skills.Learn(SkillDefOf.Mining, -10000000, true);
            //undeadPawn.skills.Learn(SkillDefOf.Mining, Rand.Range(20000, 50000) * bonusSkill, true);
            //undeadPawn.skills.Learn(SkillDefOf.Social, -10000000, true);
            //undeadPawn.skills.Learn(SkillDefOf.Construction, -10000000, true);
            //undeadPawn.skills.Learn(SkillDefOf.Construction, Rand.Range(15000, 40000) * bonusSkill, true);

            foreach (SkillRecord sr in undeadPawn.skills.skills)
            {
                if(sr.def == SkillDefOf.Cooking)
                {
                    sr.Level = Mathf.RoundToInt(Rand.Range(2f, 7f) * bonusSkill);
                }
                else if(sr.def == SkillDefOf.Crafting)
                {
                    sr.Level = Mathf.RoundToInt(Rand.Range(1.5f, 8f) * bonusSkill);
                }
                else if (sr.def == SkillDefOf.Plants)
                {
                    sr.Level = Mathf.RoundToInt(Rand.Range(3f, 8f) * bonusSkill);
                }
                else if (sr.def == SkillDefOf.Melee)
                {
                    sr.Level = Mathf.RoundToInt(Rand.Range(4f, 8.5f) * bonusSkill);
                }
                else if (sr.def == SkillDefOf.Mining)
                {
                    sr.Level = Mathf.RoundToInt(Rand.Range(2f, 7f) * bonusSkill);
                }
                else if (sr.def == SkillDefOf.Construction)
                {
                    sr.Level = Mathf.RoundToInt(Rand.Range(1.5f, 6f) * bonusSkill);
                }
                else
                {
                    sr.Level = 0;
                }
                sr.xpSinceLastLevel = 0;
            }

            //if (undeadPawn.story.Adulthood == TorannMagicDefOf.TM_UndeadAdultBS_GhostMind)
            //{
            //    undeadPawn.skills.Learn(SkillDefOf.Crafting, Rand.Range(20000, 30000), true);
            //    undeadPawn.skills.Learn(SkillDefOf.Cooking, Rand.Range(10000, 20000), true);
            //    undeadPawn.skills.Learn(SkillDefOf.Melee, -25000, true);
            //    undeadPawn.skills.Learn(SkillDefOf.Mining, -25000, true);
            //}
            if (undeadPawn.story.Childhood != null)
            {
                undeadPawn.story.Childhood = TorannMagicDefOf.TM_UndeadChildBS;
            }
            if (undeadPawn.story.Adulthood != null)
            {
                float rnd = Rand.Value;
                if (rnd < .15f)
                {
                    undeadPawn.story.Adulthood = TorannMagicDefOf.TM_UndeadAdultBS_GhostEye;
                }
                else if (rnd < .3f)
                {
                    undeadPawn.story.Adulthood = TorannMagicDefOf.TM_UndeadAdultBS_Brute;
                }
                else if (rnd < .45f)
                {
                    undeadPawn.story.Adulthood = TorannMagicDefOf.TM_UndeadAdultBS_GhostMind;
                }
                else if (rnd < .6f)
                {
                    undeadPawn.story.Adulthood = TorannMagicDefOf.TM_UndeadAdultBS_Servant;
                }
                else
                {
                    undeadPawn.story.Adulthood = TorannMagicDefOf.TM_UndeadAdultBS;
                }
            }
            if (undeadPawn.story.Adulthood == TorannMagicDefOf.TM_UndeadAdultBS_GhostEye)
            {
                undeadPawn.skills.Learn(SkillDefOf.Shooting, Rand.Range(10000, 20000) * bonusSkill, true);
            }

            foreach (BackstoryDef item in from bs in undeadPawn.story.AllBackstories
                                          where bs != null
                                          select bs)
            {
                foreach (SkillGain skillGain in item.skillGains)
                {
                    undeadPawn.skills.GetSkill(skillGain.skill).Level += skillGain.amount;
                }
            }
            //if (undeadPawn.story.Adulthood == TorannMagicDefOf.TM_UndeadAdultBS_Brute)
            //{
            //    undeadPawn.skills.Learn(SkillDefOf.Crafting, -45000, true);
            //    undeadPawn.skills.Learn(SkillDefOf.Cooking, -20000, true);
            //    undeadPawn.skills.Learn(SkillDefOf.Melee, Rand.Range(30000, 40000), true);
            //    undeadPawn.skills.Learn(SkillDefOf.Mining, Rand.Range(20000, 40000), true);
            //}
            //if (undeadPawn.story.Adulthood == TorannMagicDefOf.TM_UndeadAdultBS_Servant)
            //{
            //    undeadPawn.skills.Learn(SkillDefOf.Crafting, Rand.Range(18000, 25000), true);
            //    undeadPawn.skills.Learn(SkillDefOf.Construction, Rand.Range(20000, 25000), true);
            //    undeadPawn.skills.Learn(SkillDefOf.Cooking, Rand.Range(10000, 20000), true);
            //    undeadPawn.skills.Learn(SkillDefOf.Plants, Rand.Range(15000, 20000), true);
            //    undeadPawn.skills.Learn(SkillDefOf.Melee, -80000, true);
            //}

            if (undeadPawn.IsColonist)
            {
                if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor))
                {
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Doctor, 0);
                }
                if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Warden))
                {
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Warden, 0);
                }
                if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Handling))
                {
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Handling, 0);
                }
                if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Research))
                {
                    undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Research, 0);
                }
                if (!undeadPawn.WorkTypeIsDisabled(TorannMagicDefOf.Art))
                {
                    undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Art, 0);
                }
                if (!undeadPawn.WorkTypeIsDisabled(TorannMagicDefOf.PatientBedRest))
                {
                    undeadPawn.workSettings.SetPriority(TorannMagicDefOf.PatientBedRest, 0);
                }

                SetSmartWorkPriorities(undeadPawn);
            }
        }

        private static void SetSmartWorkPriorities(Pawn undeadPawn)
        {
            int numSkilled = 0;
            foreach (SkillRecord s in undeadPawn.skills.skills)
            {                
                if (s.def == SkillDefOf.Cooking && !undeadPawn.WorkTypeIsDisabled(TorannMagicDefOf.Cooking))
                {
                    if(s.Level >= 8)
                    {
                        numSkilled += 2;
                        undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cooking, 1);
                    }
                    else if(s.Level > 6)
                    {
                        numSkilled++;
                        undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cooking, 2);
                    }
                    else if (s.Level > 4)
                    {
                        undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cooking, 3);
                    }
                }
                else if (s.def == SkillDefOf.Crafting && !undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Crafting))
                {
                    if (s.Level >= 9)
                    {
                        numSkilled += 2;
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 1);
                    }
                    else if (s.Level > 7)
                    {
                        numSkilled++;
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 2);
                    }
                    else if (s.Level > 4)
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 3);
                    }
                }
                else if (s.def == SkillDefOf.Plants && !undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.PlantCutting) && !undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Growing))
                {
                    if (s.Level >= 10)
                    {
                        numSkilled += 2;
                        undeadPawn.workSettings.SetPriority(TorannMagicDefOf.PlantCutting, 1);
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Growing, 1);
                    }
                    else if (s.Level > 7)
                    {
                        numSkilled++;
                        undeadPawn.workSettings.SetPriority(TorannMagicDefOf.PlantCutting, 2);
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Growing, 2);
                    }
                    else if (s.Level > 4)
                    {
                        undeadPawn.workSettings.SetPriority(TorannMagicDefOf.PlantCutting, 3);
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Growing, 3);
                    }
                    else
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Growing, 4);
                    }
                }
                else if (s.def == SkillDefOf.Mining && !undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Mining))
                {
                    if (s.Level >= 10)
                    {
                        numSkilled += 2;
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Mining, 1);
                    }
                    else if (s.Level > 7)
                    {
                        numSkilled++;
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Mining, 2);
                    }
                    else if (s.Level > 5)
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Mining, 3);
                    }
                    else
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Mining, 4);
                    }
                }
                else if(s.def == SkillDefOf.Construction && !undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Construction))
                {
                    if (s.Level >= 10)
                    {
                        numSkilled += 2;
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);
                    }
                    else if (s.Level > 7)
                    {
                        numSkilled++;
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Construction, 2);
                    }
                    else if (s.Level > 4)
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Construction, 3);
                    }                    
                }                        
            }
            if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Firefighter))
            {
                undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Firefighter, 1);
            }
            if (numSkilled <= 2)
            {
                if (Rand.Chance(.5f))
                {
                    if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
                    }
                    if (!undeadPawn.WorkTypeIsDisabled(TorannMagicDefOf.Cleaning))
                    {
                        undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cleaning, 2);
                    }
                    if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Crafting))
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 2);
                    }
                }
                else
                {
                    if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 2);
                    }
                    if (!undeadPawn.WorkTypeIsDisabled(TorannMagicDefOf.Cleaning))
                    {
                        undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cleaning, 1);
                    }
                    if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Crafting))
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 2);
                    }
                }
            }
            else if (numSkilled <= 5)
            {
                if (Rand.Chance(.5f))
                {
                    if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 2);
                    }
                    if (!undeadPawn.WorkTypeIsDisabled(TorannMagicDefOf.Cleaning))
                    {
                        undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cleaning, 3);
                    }
                    if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Crafting))
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 3);
                    }
                }
                else
                {
                    if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 3);
                    }
                    if (!undeadPawn.WorkTypeIsDisabled(TorannMagicDefOf.Cleaning))
                    {
                        undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cleaning, 2);
                    }
                    if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Crafting))
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 2);
                    }
                }
            }
            else
            {
                if (Rand.Chance(.5f))
                {
                    if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 3);
                    }
                    if (!undeadPawn.WorkTypeIsDisabled(TorannMagicDefOf.Cleaning))
                    {
                        undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cleaning, 4);
                    }
                    if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Crafting))
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 3);
                    }
                }
                else
                {
                    if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 4);
                    }
                    if (!undeadPawn.WorkTypeIsDisabled(TorannMagicDefOf.Cleaning))
                    {
                        undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cleaning, 3);
                    }
                    if (!undeadPawn.WorkTypeIsDisabled(WorkTypeDefOf.Crafting))
                    {
                        undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 4);
                    }
                }
            }
        }

        private void SetOutfitRestrictions(Pawn p)
        {
            if(ModOptions.Constants.GetUndeadApparelPolicy() == null)
            {
                ModOptions.Constants.SetUndeadApparelPolicy();
            }
            if (p.outfits.CurrentApparelPolicy == Current.Game.outfitDatabase.DefaultOutfit())
            {
                p.outfits.CurrentApparelPolicy = ModOptions.Constants.GetUndeadApparelPolicy();
            }
        }

        private void RemoveGenes(Pawn p)
        {
            if (p.genes == null) return;
            List<Gene> pGenes = new List<Gene>();
            foreach (Gene g in p.genes.GenesListForReading)
            {
                if (g.def.endogeneCategory != EndogeneCategory.None) continue;
                if (g.def.abilities != null && g.def.abilities.Count > 0)
                {
                    if(g.def.defName == "Coagulate" || g.def.defName == "Bloodfeeder" || g.def.defName == "XenogermReimplanter")
                    {
                        pGenes.Add(g);
                    }
                    else
                    {
                        continue;
                    }
                }

                pGenes.Add(g);                
            }
            for(int i = 0; i < pGenes.Count; i++)
            {
                p.genes.RemoveGene(pGenes[i]);
            }
        }

        private void RemoveTraits(Pawn pawn, List<Trait> traits)
        {
            for (int i = 0; i < traits.Count; i++)
            {
                traits.Remove(traits[i]);
                i--;
            }
        }

        private void RemoveClassHediff(Pawn pawn)
        {
            if (pawn.health != null && pawn.health.hediffSet != null && pawn.health.hediffSet.hediffs != null && pawn.health.hediffSet.hediffs.Count > 0)
            {
                for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
                {
                    Hediff hediff = pawn.health.hediffSet.hediffs[i];
                    if(hediff.def == TorannMagicDefOf.TM_MagicUserHD)
                    {
                        pawn.health.RemoveHediff(hediff);
                    }
                    if(hediff.def == TorannMagicDefOf.TM_MightUserHD)
                    {
                        pawn.health.RemoveHediff(hediff);
                    }
                }
            }
        }

        public static void RemovePsylinkAbilities(Pawn p)
        {
            if(p.abilities != null && p.abilities.abilities != null && p.abilities.abilities.Count > 0)
            {
                while(p.abilities.abilities.Count > 0)
                {
                    p.abilities.RemoveAbility(p.abilities.abilities[0].def);
                }
            }
        }

        public static void DisruptiveRemoveHediffs(Pawn pawn)
        {
            List<Hediff> removeList = new List<Hediff>();
            removeList.Clear();

            using (IEnumerator<Hediff> enumerator = pawn.health.hediffSet.hediffs.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Hediff hd = enumerator.Current;
                    if (hd.IsPermanent() || (hd.IsTended() || hd.TendableNow()) || (hd.sourceBodyPartGroup == null))
                    {
                        if (hd.def != TorannMagicDefOf.TM_UndeadHD && hd.def != TorannMagicDefOf.TM_UndeadStageHD && hd.def != TorannMagicDefOf.TM_UndeadAnimalHD)
                        {
                            removeList.Add(hd);
                        }
                    }
                }
            }

            if (removeList.Count > 0)
            {
                for (int i = 0; i < removeList.Count; i++)
                {
                    pawn.health.RemoveHediff(removeList[i]);
                }
            }
            removeList.Clear();
        }

        public static void RemoveHediffsAddictionsAndPermanentInjuries(Pawn pawn)
        {
            List<Hediff> removeList = new List<Hediff>();
            removeList.Clear();
            using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    BodyPartRecord rec = enumerator.Current;

                    IEnumerable<Hediff_Injury> arg_BB_0 = pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>();
                    Func<Hediff_Injury, bool> arg_BB_1;

                    arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                    foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                    {
                        bool flag5 = !current.CanHealNaturally() && current.IsPermanent();
                        if (flag5)
                        {
                            removeList.Add(current);
                        }
                    }
                }
            }
            int partCountCap = 0;
            while (pawn.health.hediffSet.GetMissingPartsCommonAncestors().Count > 0 && partCountCap < 10)
            {
                Hediff missingPart = null;
                BodyPartRecord bodyPartRecord = null;
                foreach (Hediff_MissingPart missingPartsCommonAncestor in pawn.health.hediffSet.GetMissingPartsCommonAncestors())
                {
                    if (!pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(missingPartsCommonAncestor.Part) && (bodyPartRecord == null || missingPartsCommonAncestor.Part.coverageAbsWithChildren > bodyPartRecord.coverageAbsWithChildren))
                    {
                        bodyPartRecord = missingPartsCommonAncestor.Part;
                        missingPart = missingPartsCommonAncestor;
                    }
                }
                pawn.health.RemoveHediff(missingPart);
                partCountCap++;                
            }

            if (removeList.Count > 0)
            {
                for (int i = 0; i < removeList.Count; i++)
                {
                    pawn.health.RemoveHediff(removeList[i]);
                }
            }
            removeList.Clear();

            using(IEnumerator <Hediff_Addiction> enumerator = pawn.health.hediffSet.hediffs.OfType<Hediff_Addiction>().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Hediff_Addiction rec = enumerator.Current;
                    removeList.Add(rec);
                }
            }

            if (removeList.Count > 0)
            {
                for (int i = 0; i < removeList.Count; i++)
                {
                    pawn.health.RemoveHediff(removeList[i]);
                }
            }
            removeList.Clear();

            //IEnumerable<Hediff> hediffsToRemove = pawn.health.hediffSet.hediffs.Where(hediff => hediff is Hediff_Injury injury && injury.CanHealNaturally()
            //    || hediff is Hediff_Addiction
            //    || ((hediff.IsPermanent() || hediff.def.tendable || (hediff.source == null && hediff.sourceBodyPartGroup == null))
            //        && hediff.def != TorannMagicDefOf.TM_UndeadHD
            //        && hediff.def != TorannMagicDefOf.TM_UndeadStageHD
            //        && hediff.def != TorannMagicDefOf.TM_UndeadAnimalHD
            //    )
            //);
            //foreach (Hediff hediff in hediffsToRemove)
            //{
            //    pawn.health.RemoveHediff(hediff);
            //}
        }
    }
}


