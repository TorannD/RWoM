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
                IEnumerable<Hediff> hdEnum = this.Pawn.health.hediffSet.GetHediffs<Hediff>();
                foreach (Hediff hd in hdEnum)
                {
                    if (hd.def.defName == "SpaceHypoxia")
                    {
                        this.Pawn.health.RemoveHediff(hd);
                        break;
                    }
                }
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
                Pawn pawn = base.Pawn;
                int num = 1;
                int num2 = 1;
                using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        BodyPartRecord rec = enumerator.Current;
                        bool flag2 = num > 0;

                        if (flag2)
                        {
                            IEnumerable<Hediff_Injury> arg_BB_0 = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                            Func<Hediff_Injury, bool> arg_BB_1;

                            arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                            foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                            {
                                bool flag3 = num2 > 0;
                                if (flag3)
                                {
                                    bool flag5 = current.CanHealNaturally() && !current.IsPermanent();
                                    if (flag5)
                                    {
                                        current.Heal(2.0f);
                                        num--;
                                        num2--;
                                    }
                                    else
                                    {
                                        current.Heal(1.0f);
                                        num--;
                                        num2--;
                                    }
                                }
                            }
                        }
                    }
                }

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
                                num--;
                            }
                            else
                            {
                                TM_Action.TendWithoutNotice(rec, 1f, 1f);
                            }
                        }
                    }
                }

                using (IEnumerator<Hediff> enumerator = pawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Hediff rec = enumerator.Current;
                        if (!rec.IsPermanent())
                        {
                            if (rec.def.defName == "Cataract" || rec.def.defName == "HearingLoss" || rec.def.defName.Contains("ToxicBuildup"))
                            {
                                pawn.health.RemoveHediff(rec);
                            }
                            if ((rec.def.defName == "Blindness" || rec.def.defName.Contains("Asthma") || rec.def.defName == "Cirrhosis" || rec.def.defName == "ChemicalDamageModerate"))
                            {
                                pawn.health.RemoveHediff(rec);
                            }
                            if ((rec.def.defName == "Frail" || rec.def.defName == "BadBack" || rec.def.defName.Contains("Carcinoma") || rec.def.defName == "ChemicalDamageSevere"))
                            {
                                pawn.health.RemoveHediff(rec);
                            }
                            if ((rec.def.defName.Contains("Alzheimers") || rec.def.defName == "Dementia" || rec.def.defName.Contains("HeartArteryBlockage") || rec.def.defName == "CatatonicBreakdown"))
                            {
                                pawn.health.RemoveHediff(rec);
                            }
                        }
                        if (rec.def.makesSickThought)
                        {
                            pawn.health.RemoveHediff(rec);
                        }
                    }
                }                
            }
        }
    }
}
