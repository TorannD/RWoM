using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace TorannMagic.Thoughts
{
    public class TM_Inspiration_ArcanePathway : Inspiration
    {
        public int mageIndex = 0;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.mageIndex, "mageIndex", 0, false);
        }

        public override string LetterText
        {
            get
            {                        
                //TaggedString taggedString = def.beginLetter.Formatted(pawn.LabelShortCap, TM_Data.EnabledMagicTraits[mageIndex].degreeDatas.FirstOrDefault().label).AdjustedFor(pawn);
                TraitDef traitDef = TM_Data.EnabledMagicTraits[mageIndex];
                TaggedString taggedString = def.beginLetter.Formatted(pawn.LabelShortCap, traitDef.degreeDatas[0].label).AdjustedFor(pawn);
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    taggedString = reason.Formatted(pawn.LabelCap, traitDef.degreeDatas[0].LabelCap, pawn.Named("PAWN")).AdjustedFor(pawn) + "\n\n" + taggedString;
                }
                return taggedString;
            }
        }

        public override void PostStart(bool sendLetter = true)
        {
            mageIndex = TM_Calc.GetRandomAcceptableMagicClassIndex(pawn);
            base.PostStart(sendLetter);
        }

        public override string InspectLine
        {
            get 
            {
                int numTicks = (int)((def.baseDurationDays - AgeDays) * 60000f);
                return def.baseInspectLine + " - " + TM_Data.EnabledMagicTraits[mageIndex].degreeDatas[0].label + " (" + "ExpiresIn".Translate() + ": " + numTicks.ToStringTicksToPeriod() + ")";
            }
        }

        protected override void SendBeginLetter()
        {
            if (!def.beginLetter.NullOrEmpty() && PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                string text = (def.beginLetterLabel ?? ((string)def.LabelCap)).CapitalizeFirst() + ": " + pawn.LabelShortCap;
                Find.LetterStack.ReceiveLetter(text, LetterText, def.beginLetterDef, pawn);
            }
        }

        protected override void AddEndMessage()
        {
            if (!def.endMessage.NullOrEmpty() && PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                Messages.Message(def.endMessage.Formatted(pawn.LabelCap, TM_Data.EnabledMagicTraits[mageIndex].degreeDatas[TM_Data.EnabledMagicTraits[mageIndex].degreeDatas.FirstOrDefault().degree].label, pawn.Named("PAWN")).AdjustedFor(pawn), pawn, MessageTypeDefOf.NeutralEvent);
            }
        }
    }
}
