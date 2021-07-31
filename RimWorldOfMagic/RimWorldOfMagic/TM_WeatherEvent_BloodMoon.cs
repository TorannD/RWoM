using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using System;

namespace TorannMagic
{
    public class TM_WeatherEvent_BloodMoon : WeatherEvent
    {

        private IntVec3 strikeLoc = IntVec3.Invalid;

        SkyColorSet weatherSkyColors;
        //private static readonly Material LightningMat = MatLoader.LoadMat("Weather/LightningBolt", -1);

        private Vector2 shadowVector;
        public int duration;
        public float darkness;
        private int age = 0;

        private const int FlashFadeInTicks = 3;
        private const int MinFlashDuration = 15;
        private const int MaxFlashDuration = 60;
        private const float FlashShadowDistance = 5f;

        //private static readonly SkyColorSet MeshFlashColors = new SkyColorSet(new Color(0.6f, 0.8f, 1.2f), new Color(0.784313738f, 0.8235294f, 0.847058833f), new Color(0.9f, 0.8f, 0.8f), 1.15f);
        private static readonly SkyColorSet MeshFlashColors = new SkyColorSet(new Color(0.4f, 0.0f, 0.03f, .2f), new Color(0.5f, 0.6f, 0.61f), new Color(0.0f, 0.0f, 0.0f, 1f), .8f);

        public override float SkyTargetLerpFactor => LightningBrightness;

        protected float LightningBrightness
        {
            get
            {
                float num = 1f;
                if (age <= (.1f * duration))
                {
                    num = (float)age / (.1f * duration);
                }
                else if(age > (.9f * duration))
                {
                    num = ((float)(duration - age) / (.1f * duration));
                }
                return num/darkness; // - (float)age / ((float)duration);
            }
        }

        public override bool Expired
        {
            get
            {
                return this.age > this.duration;
            }
        }

        public override SkyTarget SkyTarget
        {
            get
            {
                return new SkyTarget(1f, TM_WeatherEvent_BloodMoon.MeshFlashColors, 1f, 1f);
            }
        }

        public override Vector2? OverrideShadowVector
        {
            get
            {
                return new Vector2?(this.shadowVector);
            }
        }

        public TM_WeatherEvent_BloodMoon(Map map, int duration, float darkness) : base(map)
		{
            this.duration = duration;
            this.darkness = darkness;
            this.shadowVector = new Vector2(Rand.Range(-5f, 5f), Rand.Range(-5f, 0f));
        }

        public override void FireEvent()
        {

        }

        public override void WeatherEventTick()
        {
            this.age++;
        }        
    }
}
