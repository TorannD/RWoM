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
    public class ReadingOutcomeDoerGainMightExp : BookOutcomeDoer
    {

        public override bool DoesProvidesOutcome(Pawn reader)
        {
            return reader.GetCompAbilityUserMight().MightData != null;
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
            new CurvePoint(0f, .1f),
            new CurvePoint(1f, .13f),
            new CurvePoint(2f, .165f),
            new CurvePoint(3f, .2f),
            new CurvePoint(4f, .24f),
            new CurvePoint(5f, .29f),
            new CurvePoint(6f, .35f)
        };

        public override void OnReadingTick(Pawn reader, float factor)
        {
            base.OnReadingTick(reader, factor);
            if (Find.TickManager.TicksGame % tickRate == 0)
            {
                CompAbilityUserMight comp = reader.GetCompAbilityUserMight();
                if (comp?.MightData != null && CanProgress(reader, comp, base.Quality))
                {
                    comp.MightData.MightUserXP += Mathf.RoundToInt(10f * (float)comp.xpGain * factor * QualityLearnRate.Evaluate((float)(int)base.Quality));
                }
            }
        }

        private static bool CanProgress(Pawn pawn, CompAbilityUserMight comp, QualityCategory quality)
        {
            return comp.MightUserLevel < GetMaxSkillLevel(quality);
        }

        private static int GetMaxSkillLevel(QualityCategory quality)
        {
            return Mathf.RoundToInt(QualityMaxLevel.Evaluate((float)(int)quality));
        }

        public override string GetBenefitsString(Pawn reader = null)
        {
            StringBuilder stringBuilder = new StringBuilder();

            float num = ((xpRate * 2500f) / tickRate) * QualityLearnRate.Evaluate((float)(int)base.Quality);

            string text = string.Format("{0}: {1}", "TM_BookExperienceCombat".Translate(), "PerHour".Translate(num.ToStringDecimalIfSmall()));
            stringBuilder.AppendLine(" - " + text);

            return stringBuilder.ToString();
        }
    }
}
