using Verse;
using RimWorld;
using AbilityUser;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using HarmonyLib;

namespace TorannMagic
{
    class Projectile_RaiseUndead : Projectile_AbilityBase
    {
        MagicPowerSkill pwr;
        MagicPowerSkill ver;

        protected override void Impact(Thing hitThing)
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

                TM_MoteMaker.ThrowPoisonMote(curCell.ToVector3Shifted(), map, .3f);
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
                            bool validator = corpseThing is Corpse;
                            if (validator)
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
                                    PawnGenerationRequest pgr = new PawnGenerationRequest(PawnKindDef.Named("Tribesperson"), pawn.Faction, PawnGenerationContext.NonPlayer, -1, true, false, false, false, false, true, 0, false, false, false, false, false, false, false, false, 0, 0, null, 0);
                                    Pawn newUndeadPawn = PawnGenerator.GeneratePawn(pgr);
                                    GenSpawn.Spawn(newUndeadPawn, corpse.Position, corpse.Map, WipeMode.Vanish);
                                    corpse.Strip();
                                    corpse.Destroy(DestroyMode.Vanish);
                                    rotStage = 1f;
                                    flag_SL = true;
                                    undeadPawn = newUndeadPawn;
                                }
                                if (!undeadPawn.def.defName.Contains("ROM_") && undeadPawn.RaceProps.IsFlesh && (undeadPawn.Dead || flag_SL) && !(undeadPawn is TMPawnSummoned) && !(undeadPawn is Golems.TMPawnGolem))
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
                                            ResurrectionUtility.Resurrect(undeadPawn);
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
                                            undeadPawn.playerSettings.medCare = MedicalCareCategory.NoMeds;
                                            undeadPawn.def.tradeability = Tradeability.None;
                                        }
                                        else if (undeadPawn.story != null && undeadPawn.story.traits != null && undeadPawn.needs != null && undeadPawn.playerSettings != null)
                                        {
                                            if (ModsConfig.IdeologyActive && undeadPawn.guest != null)
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
                                            TM_Action.TryCopyIdeo(pawn, undeadPawn);
                                            HealthUtility.AdjustSeverity(undeadPawn, TorannMagicDefOf.TM_UndeadHD, -4f);
                                            HealthUtility.AdjustSeverity(undeadPawn, TorannMagicDefOf.TM_UndeadHD, .5f + ver.level);
                                            undeadPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_UndeadHD).TryGetComp<HediffComp_Undead>().linkedPawn = pawn;
                                            HealthUtility.AdjustSeverity(undeadPawn, HediffDef.Named("TM_UndeadStageHD"), -2f);
                                            HealthUtility.AdjustSeverity(undeadPawn, HediffDef.Named("TM_UndeadStageHD"), rotStage);
                                            RedoSkills(undeadPawn, pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_LichHD")));
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
                                            undeadPawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
                                            undeadPawn.playerSettings.medCare = MedicalCareCategory.NoMeds;
                                            for (int h = 0; h < 24; h++)
                                            {
                                                undeadPawn.timetable.SetAssignment(h, TimeAssignmentDefOf.Work);
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
                                if(undeadPawn != pawn && !TM_Calc.IsNecromancer(undeadPawn) && TM_Calc.IsUndead(undeadPawn))
                                {
                                    RemoveHediffsAddictionsAndPermanentInjuries(undeadPawn);
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
            undeadPawn.story.childhood = null;
            undeadPawn.story.adulthood = null;
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

            undeadPawn.skills.Learn(SkillDefOf.Shooting, -100000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Animals, -100000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Artistic, -100000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Cooking, -100000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Cooking, Rand.Range(10000, 30000)*bonusSkill, true);            
            undeadPawn.skills.Learn(SkillDefOf.Crafting, -100000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Crafting, Rand.Range(10000, 60000) * bonusSkill, true);
            undeadPawn.skills.Learn(SkillDefOf.Plants, -100000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Plants, Rand.Range(25000, 50000) * bonusSkill, true);
            undeadPawn.skills.Learn(SkillDefOf.Intellectual, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Medicine, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Melee, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Melee, Rand.Range(50000, 90000) * bonusSkill, true);
            undeadPawn.skills.Learn(SkillDefOf.Mining, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Mining, Rand.Range(30000, 70000) * bonusSkill, true);
            undeadPawn.skills.Learn(SkillDefOf.Social, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Construction, -10000000, true);
            undeadPawn.skills.Learn(SkillDefOf.Construction, Rand.Range(20000, 50000) * bonusSkill, true);
            undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Doctor, 0);
            undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Warden, 0);
            undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Handling, 0);
            undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Research, 0);
            undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Art, 0);
            undeadPawn.workSettings.SetPriority(TorannMagicDefOf.PatientBedRest, 0);

            SetSmartWorkPriorities(undeadPawn);            
        }

        private static void SetSmartWorkPriorities(Pawn undeadPawn)
        {
            int numSkilled = 0;
            foreach (SkillRecord s in undeadPawn.skills.skills)
            {                
                if (s.def == SkillDefOf.Cooking)
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
                else if (s.def == SkillDefOf.Crafting)
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
                else if (s.def == SkillDefOf.Plants)
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
                else if (s.def == SkillDefOf.Mining)
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
                else if(s.def == SkillDefOf.Construction)
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
            undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Firefighter, 1);
            if (numSkilled <= 2)
            {
                if (Rand.Chance(.5f))
                {
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
                    undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cleaning, 2);
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 2);
                }
                else
                {
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 2);
                    undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cleaning, 1);
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 2);
                }
            }
            else if (numSkilled <= 5)
            {
                if (Rand.Chance(.5f))
                {
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 2);
                    undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cleaning, 3);
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 3);
                }
                else
                {
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 3);
                    undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cleaning, 2);
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 2);
                }
            }
            else
            {
                if (Rand.Chance(.5f))
                {
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 3);
                    undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cleaning, 4);
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 3);
                }
                else
                {
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 4);
                    undeadPawn.workSettings.SetPriority(TorannMagicDefOf.Cleaning, 3);
                    undeadPawn.workSettings.SetPriority(WorkTypeDefOf.Crafting, 4);
                }
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

        public static void RemoveHediffsAddictionsAndPermanentInjuries(Pawn pawn)
        {
            List<Hediff> removeList = new List<Hediff>();
            removeList.Clear();
            using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    BodyPartRecord rec = enumerator.Current;

                    IEnumerable<Hediff_Injury> arg_BB_0 = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
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
            if(removeList.Count > 0)
            {
                for(int i = 0; i < removeList.Count; i++)
                {
                    pawn.health.RemoveHediff(removeList[i]);
                }
            }
            removeList.Clear();

            using (IEnumerator<Hediff_Addiction> enumerator = pawn.health.hediffSet.GetHediffs<Hediff_Addiction>().GetEnumerator())
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

            using (IEnumerator<Hediff> enumerator = pawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
            {
                while(enumerator.MoveNext())
                {
                    Hediff hd = enumerator.Current;
                    if(hd.IsPermanent() || (hd.IsTended() || hd.TendableNow()) || (hd.source == null && hd.sourceBodyPartGroup == null))
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
    }
}


