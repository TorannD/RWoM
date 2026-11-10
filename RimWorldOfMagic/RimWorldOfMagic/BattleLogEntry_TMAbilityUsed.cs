using RimWorld;
using Verse;
using Verse.Grammar;

namespace TorannMagic
{
    /// <summary>
    /// Combat log entry for TM ability casts. Mirrors BattleLogEntry_AbilityUsed,
    /// which cannot be used directly because TMAbilityDef derives from the JecsTools
    /// AbilityDef, not Verse.AbilityDef.
    /// </summary>
    public class BattleLogEntry_TMAbilityUsed : BattleLogEntry_Event
    {
        private TMAbilityDef abilityUsed;

        public BattleLogEntry_TMAbilityUsed()
        {
        }

        public BattleLogEntry_TMAbilityUsed(Pawn caster, Thing target, TMAbilityDef ability, RulePackDef eventDef)
            : base(target, eventDef, caster)
        {
            abilityUsed = ability;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref abilityUsed, "abilityUsed");
        }

        protected override GrammarRequest GenerateGrammarRequest()
        {
            GrammarRequest result = base.GenerateGrammarRequest();
            if (abilityUsed != null)
            {
                result.Rules.AddRange(GrammarUtility.RulesForDef("ABILITY", abilityUsed));
            }
            return result;
        }
    }
}
