using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using Verse;

namespace TorannMagic
{
    public static class TM_ChaosProgression
    {
        // learned owners are serialized; chaosPowers also contains ungranted preview
        // slots and is rebuilt on load, so it cannot be the progression authority.
        public static List<MagicPower> ActivePowers(CompAbilityUserMagic comp)
        {
            if (comp?.ExistingMagicData != null && ClassProgression.ChaosDiscipline.Path(comp.Pawn) is ClassProgression.DisciplineProgress path)
                return ClassProgression.ChaosDiscipline.Active(comp,path);
            if (comp?.MagicData == null || (comp.Pawn?.story?.traits?.HasTrait(TorannMagicDefOf.ChaosMage) != true
                && comp.customClass?.classMageAbilities?.Contains(TorannMagicDefOf.TM_ChaosTradition) != true))
                return new List<MagicPower>();
            return comp.MagicData.AllMagicPowersForChaosMage.Where(p => p != null && p.learned
                && p.TMabilityDefs != null && p.TMabilityDefs.Count > 0).Distinct().ToList();
        }

        private static IEnumerable<MagicPowerSkill> Skills(MagicData data, MagicPower power)
        {
            // Use the same lookup as the grimoire (including custom XML aliases).
            var ability = power.TMabilityDefs[0] as TMAbilityDef;
            return new[] { data.GetSkill_Power(ability), data.GetSkill_Efficiency(ability), data.GetSkill_Versatility(ability) }
                .Where(s => s != null).Distinct();
        }

        public static void RestorePower(CompAbilityUserMagic comp, MagicPower power)
        {
            foreach (var rank in power.TMabilityDefs) comp.RemovePawnAbility(rank);
            var ability = power.abilityDef as TMAbilityDef;
            if (ability == null) return;
            if (ability.shouldInitialize) comp.AddPawnAbility(ability);
            if (ability.childAbilities != null)
                foreach (var child in ability.childAbilities.Where(a => a.shouldInitialize))
                {
                    comp.RemovePawnAbility(child);
                    comp.AddPawnAbility(child);
                }
            if (ability == TorannMagicDefOf.TM_LightSkip)
            {
                int level = TM_Calc.GetSkillPowerLevel(comp.Pawn, ability);
                if (level >= 1) comp.AddPawnAbility(TorannMagicDefOf.TM_LightSkipMass);
                if (level >= 2) comp.AddPawnAbility(TorannMagicDefOf.TM_LightSkipGlobal);
            }
            if (ability == TorannMagicDefOf.TM_Hex && comp.HexedPawns.Count > 0)
                foreach (var child in new[] { TorannMagicDefOf.TM_Hex_CriticalFail, TorannMagicDefOf.TM_Hex_MentalAssault, TorannMagicDefOf.TM_Hex_Pain })
                {
                    comp.RemovePawnAbility(child);
                    comp.AddPawnAbility(child);
                }
        }

        public static void ReleasePowers(CompAbilityUserMagic comp)
        {
            var data = comp.MagicData;
            var outgoing = ActivePowers(comp);
            var skills = new HashSet<MagicPowerSkill>(outgoing.SelectMany(p => Skills(data, p)));
            // A shared skill still used by a permanent learned spell is not leaving.
            foreach (var power in data.AllMagicPowers.Where(p => p.learned && !outgoing.Contains(p)))
                skills.ExceptWith(Skills(data, power));
            skills.ExceptWith(data.MagicPowerSkill_ChaosTradition);
            skills.ExceptWith(data.MagicPowerSkill_global_regen);
            skills.ExceptWith(data.MagicPowerSkill_global_eff);
            skills.ExceptWith(data.MagicPowerSkill_global_spirit);

            int refund = 0;
            var remove = new HashSet<AbilityDef>();
            foreach (var power in outgoing)
            {
                refund += Math.Max(0, power.level) * Math.Max(0, power.costToLevel);
                power.level = 0; power.learned = false; power.autocast = false;
                remove.UnionWith(power.TMabilityDefs);
            }
            foreach (var skill in skills)
            {
                refund += Math.Max(0, skill.level) * Math.Max(0, skill.costToLevel);
                skill.level = 0;
            }
            var pending = new Queue<AbilityDef>(remove);
            while (pending.Count > 0)
            {
                var ability = pending.Dequeue() as TMAbilityDef;
                if (ability?.childAbilities == null) continue;
                foreach (var child in ability.childAbilities)
                    if (child != null && remove.Add(child)) pending.Enqueue(child);
            }
            AddLegacyChildren(remove, "TM_SoulBond", "TM_ShadowCall", "TM_ShadowStep");
            AddLegacyChildren(remove, "TM_LightSkip", "TM_LightSkipMass", "TM_LightSkipGlobal");
            AddLegacyChildren(remove, "TM_Hex", "TM_Hex_CriticalFail", "TM_Hex_MentalAssault", "TM_Hex_Pain");
            if (remove.Contains(TorannMagicDefOf.TM_SoulBond))
            {
                comp.spell_ShadowCall = false;
                comp.spell_ShadowStep = false;
            }
            comp.AbilityData.Powers.RemoveAll(a => a != null && remove.Contains(a.Def));
            comp.AbilityData.AllPowers = null;
            var knowledge = comp.Pawn?.TryGetComp<AbilityKnowledgeComp>();
            if (knowledge != null)
                foreach (var def in remove)
                    if (def != null && !data.AllMagicPowers.Any(p => p?.learned == true && p.abilityDef == def))
                        knowledge.Remove(SpellIds.Of(def));
            data.MagicAbilityPoints += refund;
            comp.UpdateAbilities();
        }

        private static void AddLegacyChildren(HashSet<AbilityDef> remove, string parent, params string[] children)
        {
            if (!remove.Any(a => a.defName == parent)) return;
            foreach (var name in children)
            {
                var child = DefDatabase<TMAbilityDef>.GetNamedSilentFail(name);
                if (child != null) remove.Add(child);
            }
        }
    }
}
