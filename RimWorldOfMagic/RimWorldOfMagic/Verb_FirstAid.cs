using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_FirstAid : Verb_UseAbility
    {

        private int pwrVal = 0;
        private int verVal = 0;

        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;

            CompAbilityUserMight comp = caster.GetCompAbilityUserMight();
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
            //pwrVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_FirstAid, "TM_FirstAid", "_pwr", true);
            //verVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_FirstAid, "TM_FirstAid", "_ver", true);

            bool flag = caster != null;
            if (flag)
            {
                using (IEnumerator<BodyPartRecord> enumerator = caster.health.hediffSet.GetInjuredParts().GetEnumerator())
                {
                    int num = 2 + pwrVal;
                    while (enumerator.MoveNext())
                    {
                        BodyPartRecord rec = enumerator.Current;

                        IEnumerable<Hediff_Injury> arg_BB_0 = caster.health.hediffSet.GetHediffs<Hediff_Injury>();
                        Func<Hediff_Injury, bool> arg_BB_1;

                        arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                        if (num > 0)
                        {
                            int num2 = 2 + pwrVal;
                            foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                            {
                                if (num2 > 0)
                                {
                                    bool flag5 = current.CanHealNaturally() && !current.IsPermanent() && current.TendableNow();
                                    if (flag5)
                                    {
                                        current.Tended(Rand.Range(0,0.4f) + (.1f * verVal), 1f);
                                        num--;
                                        num2--;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
