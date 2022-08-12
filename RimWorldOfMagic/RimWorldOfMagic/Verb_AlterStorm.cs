using RimWorld;
using System;
using Verse;
using AbilityUser;
using System.Linq;
using TorannMagic.Conditions;

namespace TorannMagic
{
    public class Verb_AlterStorm : Verb_UseAbility
    {
        public Type eventClass;

        protected override bool TryCastShot()
        {
            Map map = base.CasterPawn.Map; 
            WeatherDef alterDef = new WeatherDef();
            CompAbilityUserMagic comp = base.CasterPawn.GetCompAbilityUserMagic();
            if (map != null && comp != null && comp.MagicData != null)
            {
                WeatherDef w = null;
                if(TM_Calc.IsAlterableWeather(map, out w))
                {
                    if(w.defName == "SnowHard" || w.defName == "SnowGentle")
                    {
                        MagicMapComponent mmc = map.GetComponent<MagicMapComponent>();
                        if(mmc != null)
                        {
                            mmc.weatherControlExpiration = 12000 + Find.TickManager.TicksGame;
                        }
                        WeatherDef newWeather = TorannMagicDefOf.TM_HailstormWD;
                        map.weatherDecider.DisableRainFor(0);
                        map.weatherManager.TransitionTo(newWeather);
                        
                    }
                    else
                    {
                        WeatherDef newWeather = TorannMagicDefOf.TM_HealingRainWD;
                        map.weatherDecider.DisableRainFor(0);
                        map.weatherManager.TransitionTo(newWeather);
                    }
                }
                else
                {
                    Messages.Message("TM_CannotAlterWeatherType".Translate(
                    w.label
                    ), MessageTypeDefOf.RejectInput);
                }
            }
            return true;
        }
    }
}
