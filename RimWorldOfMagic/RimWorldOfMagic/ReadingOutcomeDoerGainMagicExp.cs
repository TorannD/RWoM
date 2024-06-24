using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    public class ReadingOutcomeDoerGainMagicExp : BookOutcomeDoer
    {

        public override bool DoesProvidesOutcome(Pawn reader)
        {
            return reader.GetCompAbilityUserMagic().MagicData != null;
        }

        private int tickRate = 120;
        private float xpRate = 10f;

        public static readonly SimpleCurve QualityMaxLevel = new SimpleCurve
        {
            new CurvePoint(0f, 8f),
            new CurvePoint(1f, 12f),
            new CurvePoint(2f, 15f),
            new CurvePoint(3f, 20f),
            new CurvePoint(4f, 30f),
            new CurvePoint(5f, 50f),
            new CurvePoint(6f, 100f)
        };

        public static readonly SimpleCurve QualityLearnRate = new SimpleCurve
        {
            new CurvePoint(0f, .3f),
            new CurvePoint(1f, .35f),
            new CurvePoint(2f, .45f),
            new CurvePoint(3f, .5f),
            new CurvePoint(4f, .55f),
            new CurvePoint(5f, .625f),
            new CurvePoint(6f, .75f)
        };

        public override void OnReadingTick(Pawn reader, float factor)
        {
            base.OnReadingTick(reader, factor);
            if (Find.TickManager.TicksGame % tickRate == 0)
            {
                CompAbilityUserMagic comp = reader.GetCompAbilityUserMagic();
                if (comp?.MagicData != null && CanProgress(reader, comp, base.Quality))
                {
                    comp.MagicData.MagicUserXP += Mathf.RoundToInt(10f * (float)comp.xpGain * factor * QualityLearnRate.Evaluate((float)(int)base.Quality));
                    HealthUtility.AdjustSeverity(reader, TorannMagicDefOf.TM_ArcaneWeakness, Rand.Range(.05f, .1f) / factor);
                }
            }
        }

        private static bool CanProgress(Pawn pawn, CompAbilityUserMagic comp, QualityCategory quality)
        {
            return comp.MagicUserLevel < GetMaxSkillLevel(quality);
        }

        private static int GetMaxSkillLevel(QualityCategory quality)
        {
            return Mathf.RoundToInt(QualityMaxLevel.Evaluate((float)(int)quality));
        }

        public override string GetBenefitsString(Pawn reader = null)
        {
            StringBuilder stringBuilder = new StringBuilder();

            float num = ((xpRate * 2500f) / tickRate) * QualityLearnRate.Evaluate((float)(int)base.Quality);

            string text = string.Format("{0}: {1}", "TM_BookExperienceGrimoire".Translate(), "PerHour".Translate(num.ToStringDecimalIfSmall()));
            stringBuilder.AppendLine(" - " + text + "\n");
            string text2 = "TM_BookCausesWeakness".Translate();
            stringBuilder.AppendLine(text2);

            return stringBuilder.ToString();
        }
    }
}
