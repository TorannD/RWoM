using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TorannMagic
{
    public class Verb_LichForm : Verb_UseAbility
    {

        protected override bool TryCastShot()
        {
            Map map = base.CasterPawn.Map;
            Pawn pawn = base.CasterPawn;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();

            List<Trait> traits = pawn.story.traits.allTraits;
            for (int i = 0; i < traits.Count; i++)
            {
                if (traits[i].def.defName == "Necromancer")
                {
                    FixTrait(pawn, pawn.story.traits.allTraits);
                    AdjustPlayerSettings(pawn);
                    pawn.story.traits.GainTrait(new Trait(TraitDef.Named("Lich"), 0, false));
                    HealthUtility.AdjustSeverity(pawn, HediffDef.Named("TM_LichHD"), .5f);
                    for (int h = 0; h < 24; h++)
                    {
                        pawn.timetable.SetAssignment(h, TimeAssignmentDefOf.Work);
                    }
                    pawn.needs.AddOrRemoveNeedsAsAppropriate();
                    comp.MagicData.MagicPowersN.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_DeathBolt).learned = true;
                    comp.AddPawnAbility(TorannMagicDefOf.TM_DeathBolt, false);
                    comp.spell_Flight = true;
                    comp.InitializeSpell();
                    TM_MoteMaker.ThrowScreamMote(pawn.Position.ToVector3(), pawn.Map, 2f, 216, 255, 0);
                }
                else
                {
                    //Log.Message("Necromancer trait not found.");
                }
            }
            
            this.burstShotsLeft = 0;
            return false;
        }

        private void FixTrait(Pawn pawn, List<Trait> traits)
        {
            for (int i = 0; i < traits.Count; i++)
            {
                if (traits[i].def.defName == "Necromancer")
                {
                    traits.Remove(traits[i]);
                    i--;
                }
            }
        }

        private void AdjustPlayerSettings(Pawn lich)
        {
            SkillRecord skill;
            skill = lich.skills.GetSkill(SkillDefOf.Animals);
            skill.passion = Passion.None;
            skill = lich.skills.GetSkill(SkillDefOf.Artistic);
            skill.passion = Passion.None;
            skill = lich.skills.GetSkill(SkillDefOf.Construction);
            skill.passion = Passion.None;
            skill = lich.skills.GetSkill(SkillDefOf.Cooking);
            skill.passion = Passion.None;
            skill = lich.skills.GetSkill(SkillDefOf.Crafting);
            skill.passion = Passion.None;
            skill = lich.skills.GetSkill(SkillDefOf.Plants);
            skill.passion = Passion.None;
            skill = lich.skills.GetSkill(SkillDefOf.Medicine);
            skill.passion = Passion.None;
            skill = lich.skills.GetSkill(SkillDefOf.Mining);
            skill.passion = Passion.None;
            skill = lich.skills.GetSkill(SkillDefOf.Social);
            skill.passion = Passion.None;
            //lich.story.DisabledWorkTypes.Clear();
            lich.workSettings.SetPriority(WorkTypeDefOf.Doctor, 0);
            lich.workSettings.SetPriority(WorkTypeDefOf.Warden, 0);
            lich.workSettings.SetPriority(WorkTypeDefOf.Handling, 0);
            //lich.workSettings.SetPriority(WorkTypeDefOf.Construction, 0);
            //lich.workSettings.SetPriority(WorkTypeDefOf.Crafting, 0);
            lich.workSettings.SetPriority(WorkTypeDefOf.Firefighter, 0);
            //lich.workSettings.SetPriority(WorkTypeDefOf.Growing, 0);
            //lich.workSettings.SetPriority(WorkTypeDefOf.Mining, 0);
            lich.workSettings.SetPriority(TorannMagicDefOf.Cleaning, 0);
            lich.workSettings.SetPriority(TorannMagicDefOf.Hauling, 0);
            //lich.workSettings.SetPriority(TorannMagicDefOf.Tailoring, 0);
            //lich.workSettings.SetPriority(TorannMagicDefOf.Smithing, 0);
            //lich.workSettings.SetPriority(TorannMagicDefOf.PlantCutting, 0);
            lich.workSettings.SetPriority(TorannMagicDefOf.Cooking, 0);
            lich.workSettings.SetPriority(TorannMagicDefOf.Art, 0);

            skill = lich.skills.GetSkill(SkillDefOf.Intellectual);
            if(skill.passion == Passion.None)
            {
                skill.passion = Passion.Minor;
            }
            else if( skill.passion == Passion.Minor)
            {
                skill.passion = Passion.Major;
            }

            skill = lich.skills.GetSkill(SkillDefOf.Shooting);
            if(skill.passion == Passion.None)
            {
                skill.passion = Passion.Minor;
            }

        }
    }
}
