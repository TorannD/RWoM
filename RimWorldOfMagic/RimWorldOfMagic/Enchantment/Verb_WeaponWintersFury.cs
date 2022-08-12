using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using AbilityUser;
using UnityEngine;
using TorannMagic.Conditions;

namespace TorannMagic.Enchantment
{
    public class Verb_WeaponWintersFury : Verb_UseAbility  
    {        

        protected override bool TryCastShot()
        {
            Map map = base.CasterPawn.Map;
            WeatherDef rainMakerDef = new WeatherDef();
            CompAbilityUserMagic comp = base.CasterPawn.GetCompAbilityUserMagic();
            if (map != null && map.weatherManager != null && comp != null && comp.MagicData != null)
            {
                MagicMapComponent mmc = map.GetComponent<MagicMapComponent>();
                if (mmc != null)
                {
                    mmc.weatherControlExpiration = 8000 + Find.TickManager.TicksGame;
                }
                WeatherDef newWeather = TorannMagicDefOf.TM_HailstormWD;
                map.weatherDecider.DisableRainFor(0);
                map.weatherManager.TransitionTo(newWeather);
                Effects(CasterPawn.Position);
                InitializeConditions();
            }
            return true;
        }

        public void Effects(IntVec3 position)
        {
            Vector3 rndPos = position.ToVector3Shifted();
            FleckMaker.ThrowHeatGlow(position, this.CasterPawn.Map, 1f);
            for (int i = 0; i < 3; i++)
            {
                rndPos.x += Rand.Range(-.5f, .5f);
                rndPos.z += Rand.Range(-.5f, .5f);
                rndPos.y += Rand.Range(.3f, 1.3f);
                FleckMaker.ThrowSmoke(rndPos, this.CasterPawn.Map, Rand.Range(.7f, 1.1f));
                FleckMaker.ThrowLightningGlow(position.ToVector3Shifted(), this.CasterPawn.Map, 1.4f);
            }
        }

        private void InitializeConditions()
        {            
            GameConditionManager gameConditionManager = base.CasterPawn.Map.GameConditionManager;            
            GameCondition cond2 = GameConditionMaker.MakeCondition(GameConditionDefOf.ColdSnap, 16000);
            gameConditionManager.RegisterCondition(cond2);
        }

    }
}
