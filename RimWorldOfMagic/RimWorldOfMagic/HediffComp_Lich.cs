using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_Lich : HediffComp
    {
        private static readonly string[] nonStandardNeedsToAutoFulfill = new[] {
            "Bladder", //Dubs Bad Hygiene
            "Hygiene", //Dubs Bad Hygiene
            "DBHThirst" //Dubs Bad Hygiene
        };

        private bool initializing = true;
        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned)
            {
                
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (initializing)
                {
                    initializing = false;
                    this.Initialize();
                }
            }

            if (Find.TickManager.TicksGame % 16 == 0)
            {
                Hediff hediffToRemove =
                    Pawn?.health.hediffSet.hediffs.FirstOrDefault(hd => hd.def.defName == "SpaceHypoxia");
                if (hediffToRemove != default)
                    Pawn.health.RemoveHediff(hediffToRemove);
            }

            bool flag4 = Find.TickManager.TicksGame % 600 == 0;
            if (flag4)
            {
                List<Need> needs = base.Pawn.needs.AllNeeds;
                for (int i = 0; i < needs.Count; i++)
                {
                    if(needs[i].def.defName != "Joy" && needs[i].def.defName != "Mood" && needs[i].def.defName != "TM_Mana" && needs[i].def.defName != "TM_Stamina" && needs[i].def.defName != "ROMV_Blood" || nonStandardNeedsToAutoFulfill.Contains(needs[i]?.def?.defName))
                    { 
                        needs[i].CurLevel = needs[i].MaxLevel;
                    }
                    
                }
                Pawn pawn = Pawn;
                Hediff_Injury injuryToHeal = pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().FirstOrDefault();
                injuryToHeal?.Heal(injuryToHeal.CanHealNaturally() ? 2.0f : 1.0f);

                using (IEnumerator<Hediff> enumerator = pawn.health.hediffSet.GetHediffsTendable().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Hediff rec = enumerator.Current;
                        if (rec.TendableNow()) // && !currentTendable.IsPermanent()
                        {
                            if (rec.Bleeding && rec is Hediff_MissingPart)
                            {
                                Traverse.Create(root: rec).Field(name: "isFreshInt").SetValue(false);
                            }
                            else
                            {
                                TM_Action.TendWithoutNotice(rec, 1f, 1f);
                            }
                        }
                    }
                }

                foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                {
                    if (!hediff.IsPermanent())
                    {
                        if (hediff.def.defName == "Cataract" || hediff.def.defName == "HearingLoss" || hediff.def.defName.Contains("ToxicBuildup"))
                        {
                            pawn.health.RemoveHediff(hediff);
                        }
                        if ((hediff.def.defName == "Blindness" || hediff.def.defName.Contains("Asthma") || hediff.def.defName == "Cirrhosis" || hediff.def.defName == "ChemicalDamageModerate"))
                        {
                            pawn.health.RemoveHediff(hediff);
                        }
                        if ((hediff.def.defName == "Frail" || hediff.def.defName == "BadBack" || hediff.def.defName.Contains("Carcinoma") || hediff.def.defName == "ChemicalDamageSevere"))
                        {
                            pawn.health.RemoveHediff(hediff);
                        }
                        if ((hediff.def.defName.Contains("Alzheimers") || hediff.def.defName == "Dementia" || hediff.def.defName.Contains("HeartArteryBlockage") || hediff.def.defName == "CatatonicBreakdown"))
                        {
                            pawn.health.RemoveHediff(hediff);
                        }
                    }
                    if (hediff.def.makesSickThought)
                    {
                        pawn.health.RemoveHediff(hediff);
                    }
                }
            }
        }
    }
}
