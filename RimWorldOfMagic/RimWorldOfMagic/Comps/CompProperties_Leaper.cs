using System;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    public class CompProperties_Leaper : CompProperties
    {
        public float leapRangeMax = 8f;
        public float leapRangeMin = 2f;

        public float leapChance = .5f;
        public float ticksBetweenLeapChance = 100f;

        public bool bouncingLeaper = false;

        public bool explodingLeaper = false;
        public float explodingLeaperChance = .2f;
        public float explodingLeaperRadius = 2f;

        public bool textMotes = true;

        public float GetLeapChance
        {
            get
            {
                return Mathf.Clamp01(this.leapChance);
            }
        }

        public float GetExplodingLeaperChance
        {
            get
            {
                return Mathf.Clamp01(this.explodingLeaperChance);
            }
        }

        public CompProperties_Leaper()
        {
            this.compClass = typeof(CompLeaper);
        }
    }
    
}
