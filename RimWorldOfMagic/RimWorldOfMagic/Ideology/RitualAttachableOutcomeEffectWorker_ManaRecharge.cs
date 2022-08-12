using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace TorannMagic.Ideology
{
    public class RitualAttachableOutcomeEffectWorker_ManaRecharge : RitualAttachableOutcomeEffectWorker
    {
		public static readonly IntRange ManaRange = new IntRange(50, 100);
		public override void Apply(Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual, OutcomeChance outcome, out string extraOutcomeDesc, ref LookTargets letterLookTargets)
		{
			extraOutcomeDesc = "No mages participated.";
			int mageCount = 0;
			List<OutcomeChance> outcomeChances = jobRitual.Ritual.outcomeEffect.def.outcomeChances;
			int positivityIndex = outcomeChances.MaxBy((OutcomeChance c) => c.positivityIndex).positivityIndex;
			int positivityIndex2 = outcomeChances.Where((OutcomeChance c) => c.positivityIndex >= 0).MinBy((OutcomeChance c) => c.positivityIndex).positivityIndex;
			int num = ManaRange.Lerped((float)(outcome.positivityIndex - positivityIndex2) / (float)(positivityIndex - positivityIndex2));
			float numf = (float)num;
			foreach (Pawn key in totalPresence.Keys)
			{
				if(TM_Calc.IsMagicUser(key))
                {
					CompAbilityUserMagic comp = key.GetCompAbilityUserMagic();
					if(comp != null)
                    {
						mageCount++;
						comp.Mana.CurLevel += (numf / 100f);
                    }
                }
			}
			if (mageCount > 0)
			{
				extraOutcomeDesc = def.letterInfoText.Formatted(num.Named("AMOUNT"));
			}
		}
	}
}
