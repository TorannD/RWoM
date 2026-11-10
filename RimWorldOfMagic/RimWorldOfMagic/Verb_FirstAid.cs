using AbilityUser;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TorannMagic
{
    public class Verb_FirstAid : Verb_UseAbility
    {
        internal static IEnumerable<Hediff_Injury> EligibleWounds(Pawn patient) =>
            patient.health.hediffSet.hediffs.OfType<Hediff_Injury>()
                .Where(injury => injury.CanHealNaturally() && injury.TendableNow());

        internal static bool Tend(Pawn patient, int power, int versatility)
        {
            var injuries = EligibleWounds(patient).Take(2 + power).ToList();
            foreach (var injury in injuries)
                injury.Tended(Rand.Range(0f, .4f) + .1f * versatility, 1f);
            return injuries.Count > 0;
        }

        protected override bool TryCastShot()
        {
            var caster = CasterPawn;
            if (caster != null)
            {
                var def = Ability.Def as TMAbilityDef;
                Tend(caster, TM_Calc.GetSkillPowerLevel(caster, def), TM_Calc.GetSkillVersatilityLevel(caster, def));
            }
            return false;
        }
    }
}
