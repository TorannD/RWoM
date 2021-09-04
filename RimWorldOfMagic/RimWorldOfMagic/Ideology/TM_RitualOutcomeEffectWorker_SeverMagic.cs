using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Ideology
{
    public class TM_RitualOutcomeEffectWorker_SeverMagic : RitualOutcomeEffectWorker_FromQuality
    {
        public TM_RitualOutcomeEffectWorker_SeverMagic()
        {
        }

        public TM_RitualOutcomeEffectWorker_SeverMagic(RitualOutcomeEffectDef def)
            : base(def)
        {
        }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            float quality = GetQuality(jobRitual, progress);
            OutcomeChance outcome = GetOutcome(quality, jobRitual);
            LookTargets letterLookTargets = jobRitual.selectedTarget;
            string extraLetterText = null;
            if (jobRitual.Ritual != null)
            {
                ApplyAttachableOutcome(totalPresence, jobRitual, outcome, out extraLetterText, ref letterLookTargets);
            }
            bool flag = false;
            foreach (Pawn key in totalPresence.Keys)
            {
                if (key.IsSlave)
                {
                    Need_Suppression need_Suppression = key.needs.TryGetNeed<Need_Suppression>();
                    if (need_Suppression != null)
                    {
                        need_Suppression.CurLevel = 1f;
                    }
                    flag = true;
                }
                else
                {
                    GiveMemoryToPawn(key, outcome.memory, jobRitual);
                }
            }
            string text = outcome.description.Formatted(jobRitual.Ritual.Label).CapitalizeFirst() + "\n\n" + OutcomeQualityBreakdownDesc(quality, progress, jobRitual);
            string text2 = def.OutcomeMoodBreakdown(outcome);
            if (!text2.NullOrEmpty())
            {
                text = text + "\n\n" + text2;
            }
            //if (flag)
            //{
            //    text += "\n\n" + "RitualOutcomeExtraDesc_Execution".Translate();
            //}
            
            if (extraLetterText != null)
            {
                text = text + "\n\n" + extraLetterText;
            }
            Find.LetterStack.ReceiveLetter("OutcomeLetterLabel".Translate(outcome.label.Named("OUTCOMELABEL"), jobRitual.Ritual.Label.Named("RITUALLABEL")), text, outcome.Positive ? LetterDefOf.RitualOutcomePositive : LetterDefOf.RitualOutcomeNegative, letterLookTargets);
            List<Pawn> ritualPawns = new List<Pawn>();
            ritualPawns.Clear();
            if (!outcome.Positive)
            {
                Pawn p = TM_Calc.GetPawnForSeverenceRetaliation(Faction.OfPlayer);
                
                if (p != null)
                {
                    ritualPawns.Add(p);
                    if (outcome.positivityIndex == -2)
                    {
                        DoCatastrophicSeverMagicOutcome(p);
                    }
                }
            }
            else
            {
                Pawn p = TM_Calc.GetPawnForSeverenceRetaliation(Faction.OfPlayer);
                if(p != null)
                {
                    ritualPawns.Add(p);
                    Hediff hd = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MagicSeverenceHD);
                    p.health.RemoveHediff(hd);
                }
            }

            bool flagApprove = false;
            bool flagVenerated = false;
            List<Pawn> colonyPawns = PawnsFinder.AllMaps_SpawnedPawnsInFaction(Faction.OfPlayer);
            foreach (Pawn p in colonyPawns)
            {
                if (p.Ideo?.GetRole(p)?.def == TorannMagicDefOf.TM_IdeoRole_VoidSeeker)
                {
                    if (p.Ideo.HasPrecept(TorannMagicDefOf.TM_Mages_Approve))
                    {
                        flagApprove = true;
                        ritualPawns.Add(p);
                        break;
                    }
                    if (p.Ideo.HasPrecept(TorannMagicDefOf.TM_Mages_Venerated))
                    {
                        flagVenerated = true;
                        ritualPawns.Add(p);
                        break;
                    }
                }
            }
            if (flagApprove || flagVenerated)
            {
                List<Pawn> remainingPawns = colonyPawns.Except(ritualPawns).ToList();
                foreach (Pawn p in remainingPawns)
                {
                    if (p.needs.mood?.thoughts?.memories != null)
                    {
                        if (flagApprove && p.Ideo.HasPrecept(TorannMagicDefOf.TM_Mages_Approve))
                        {
                            p.needs.mood.thoughts.memories.TryGainMemory(TorannMagicDefOf.TM_SeverMagic_ForApproveTD, null);
                        }
                        else if (flagVenerated && p.Ideo.HasPrecept(TorannMagicDefOf.TM_Mages_Venerated))
                        {
                            p.needs.mood.thoughts.memories.TryGainMemory(TorannMagicDefOf.TM_SeverMagic_ForVeneratedTD, null);
                        }
                    }
                }
            }
        }

        private void DoCatastrophicSeverMagicOutcome(Pawn p)
        {
            TM_Action.CreateMagicDeathEffect(p, p.Position, false, true);
        }
    }
}
 