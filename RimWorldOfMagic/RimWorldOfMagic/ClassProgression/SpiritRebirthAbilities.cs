using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritRebirthAbilities
    {
        // Deliberately not wired to lifecycle/activation yet.
        internal static bool Ensure(Pawn original)
        {
            if (!SpiritRebirthFormPolicy.IsEthereal(original) && !SpiritRebirthRitualTransaction.IsPreparing(original)) return false;
            var comp = original.GetComp<CompAbilityUserMagic>();
            if (comp == null || TorannMagicDefOf.TM_Possessor == null) return false;
            var classes = DefDatabase<TM_CustomClassDef>.GetNamedSilentFail("TM_CustomClasses")?.customClasses;
            var matches = classes?.Where(c => c != null && c.classTrait == TorannMagicDefOf.TM_Possessor).ToList();
            if (matches?.Count != 1 || !matches[0].isMage || matches[0].classMageAbilities == null) return false;
            var family = matches[0].classMageAbilities;
            var definitions = DefDatabase<TM_CustomPowerDef>.AllDefsListForReading
                .Where(d => d?.customPower?.forMage == true && d.customPower.abilityDefs?.Count > 0
                    && family.Contains(d.customPower.abilityDefs[0] as TMAbilityDef)).ToList();
            // Reject ambiguous/mixed definitions before creating or changing data.
            if (definitions.Count == 0 || definitions.Any(d => d.customPower.abilityDefs.Any(a => !(a is TMAbilityDef t) || !family.Contains(t)))
                || definitions.GroupBy(d => d.customPower.abilityDefs[0]).Any(g => g.Count() != 1)
                || family.Any(a => a == null || !definitions.Any(d => d.customPower.abilityDefs.Contains(a)))) return false;
            var data = comp.InitializeMissingSpiritData();
            foreach (var definition in definitions)
            {
                var owner = data.EnsureSpiritOwner(definition.customPower);
                // A classed pawn keeps its retained MagicData, which already
                // holds unlearned owners for the guaranteed spirit powers —
                // the fresh-owner guarantee inside EnsureSpiritOwner never ran
                // for them (classed-spirit report: released without drain or
                // possession). Apply the same guarantee here, and only here:
                // chance-learned powers keep their never-rerolled contract.
                if (!owner.learned && !definition.customPower.requiresScroll
                    && definition.customPower.abilityDefs.Count > 0
                    && definition.customPower.abilityDefs[0] is TMAbilityDef guaranteed
                    && guaranteed.learnChance >= 1f)
                {
                    owner.learned = true;
                }
                if (!owner.learned || owner.TMabilityDefs == null || owner.TMabilityDefs.Count == 0) continue;
                var current = owner.TMabilityDefs[System.Math.Max(0, System.Math.Min(owner.level, owner.TMabilityDefs.Count - 1))] as TMAbilityDef;
                if (current != null && family.Contains(current)
                    && definition.customPower.abilityDefs[0] is TMAbilityDef root && root.shouldInitialize)
                    TM_ClassState.AddRuntimeAbility(comp, current);
            }
            return true;
        }
    }
}
