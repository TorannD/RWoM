using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using RimWorld;


namespace TorannMagic
{
    public class Verb_ApplyRandomHediffFromList_Properties : VerbProperties_Ability
    {
        public List<HediffDef> hediffDefs;
        public int baseCount = 1;
        public string countUpgrade;
        public float baseSeverity = 0.5f;
        public string severityUpgrade;
        public float severityIncreasePerUpgrade = 0.1f;
        public bool useEachOnceMax = false;

        public Verb_ApplyRandomHediffFromList_Properties() : base()
        {
            this.verbClass = verbClass ?? typeof(Verb_ApplyRandomHediffFromList);
        }
    }

    public class Verb_ApplyRandomHediffFromList : Verb_SB
    {
        Pawn target => currentTarget.Pawn;
        Verb_ApplyRandomHediffFromList_Properties Properties => this.verbProps as Verb_ApplyRandomHediffFromList_Properties;
        List<HediffDef> Effects => Properties?.hediffDefs;
        int CountUpgrade => Properties.countUpgrade == null ? 0
            : TM_ClassUtility.GetMightPowerSkillFromLabel(this.CasterPawn.GetCompAbilityUserMight(), Properties.countUpgrade).level;
        int SeverityUpgrade => Properties.severityUpgrade == null ? 0
            : TM_ClassUtility.GetMightPowerSkillFromLabel(this.CasterPawn.GetCompAbilityUserMight(), Properties.severityUpgrade).level;

        protected override bool TryCastShot()
        {
            if (Properties == null)
            {
                Log.Warning("The TorannMagic.Verb_ApplyRandomHediffFromList cannot be used outside a TorannMagic.Verb_ApplyRandomHediffFromList_Properties, check abilityDef " + this.Ability.Def.defName);
                return false;
            }
            if (target != null)
            {
                int amountToApply = Properties.baseCount + CountUpgrade;
                float severity = Properties.baseSeverity + Properties.severityIncreasePerUpgrade * SeverityUpgrade;

                if (Properties.useEachOnceMax)
                {
                    List<HediffDef> toApply;
                    if (amountToApply >= Effects.Count)
                    {
                        toApply = Effects;
                    }
                    else
                    {
                        toApply = new List<HediffDef>(amountToApply);
                        for (int i = 0; toApply.Count < amountToApply; ++i)
                        {
                            float rand = Rand.Value;
                            //Log.Message("rand: " + rand);
                            //Log.Message("target: " + (amountToApply - toApply.Count) / (float)(Effects.Count - i));
                            if (rand < (amountToApply - toApply.Count) / (float)(Effects.Count - i))
                            {
                                toApply.Add(Effects[i]);
                            }
                        }
                    }
                    foreach (HediffDef h in toApply)
                    {
                        HealthUtility.AdjustSeverity(target, h, severity);
                    }
                }
                else
                {
                    Dictionary<HediffDef, float> toApply = new Dictionary<HediffDef, float>(Properties.hediffDefs.Count);
                    for (int i = 0; i < amountToApply; ++i)
                    {
                        HediffDef def = Effects.RandomElement();
                        toApply.SetOrAdd(def, toApply.TryGetValue(def) + severity);
                    }
                    foreach (HediffDef d in Effects)
                    {
                        HealthUtility.AdjustSeverity(target, d, toApply.TryGetValue(d));
                    }
                }
            }
            return false;
        }
    }
}
