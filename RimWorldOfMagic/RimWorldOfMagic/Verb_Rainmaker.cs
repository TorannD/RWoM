using RimWorld;
using System;
using Verse;
using AbilityUser;
using System.Linq;
using TorannMagic.Conditions;

namespace TorannMagic
{
    public class Verb_Rainmaker : ClassProgression.Verb_SpiritTargetable
    {
        public Type eventClass;
        private bool isViolent = false;

        protected override bool TryCastShot()
        {
            Map map = base.CasterPawn.Map; 
            WeatherDef rainMakerDef = new WeatherDef();
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn, Ability);
            if (owner == null) return false;
            CompAbilityUserMagic comp = owner.GetCompAbilityUserMagic();
            if (map != null && comp != null && comp.MagicData != null)
            {

                if (comp.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr").level >= 14)
                {
                    isViolent = true;
                }

                if (map.mapTemperature.OutdoorTemp < 0)
                {
                    if (map.weatherManager.curWeather != TorannMagicDefOf.TM_HailstormWD)
                    {
                        if (map.weatherManager.curWeather.defName == "SnowHard" || map.weatherManager.curWeather.defName == "SnowGentle")
                        {
                            rainMakerDef = WeatherDef.Named("Clear");
                            ClassProgression.IceTrials.TransitionWeather(owner, map, rainMakerDef);
                            return true;
                        }
                        else
                        {
                            if (Rand.Chance(.5f))
                            {
                                rainMakerDef = WeatherDef.Named("SnowGentle");
                            }
                            else
                            {
                                rainMakerDef = WeatherDef.Named("SnowHard");
                            }
                            map.weatherDecider.DisableRainFor(0);
                            ClassProgression.IceTrials.TransitionWeather(owner, map, rainMakerDef);
                            return true;
                        }
                    }
                    else
                    {
                        Messages.Message("TM_CannotAlterWeatherType".Translate(
                        TorannMagicDefOf.TM_HailstormWD.label
                        ), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    if (map.weatherManager.curWeather.defName == "Rain" || map.weatherManager.curWeather.defName == "RainyThunderstorm" || map.weatherManager.curWeather.defName == "FoggyRain")
                    {
                        if (isViolent && !(map.weatherManager.curWeather.defName == "RainyThunderstorm"))
                        {
                            rainMakerDef = WeatherDef.Named("RainyThunderstorm");
                        }
                        else
                        {
                            rainMakerDef = WeatherDef.Named("Clear");
                            isViolent = false;
                        }
                        ClassProgression.IceTrials.TransitionWeather(owner, map, rainMakerDef);

                    }
                    else
                    {
                        if (isViolent)
                        {
                            rainMakerDef = WeatherDef.Named("RainyThunderstorm");
                        }
                        else
                        {
                            int rnd = Rand.RangeInclusive(1, 3);
                            switch (rnd)
                            {
                                case 1:
                                    rainMakerDef = WeatherDef.Named("Rain");
                                    break;
                                case 2:
                                    rainMakerDef = WeatherDef.Named("RainyThunderstorm");
                                    break;
                                case 3:
                                    rainMakerDef = WeatherDef.Named("FoggyRain");
                                    break;
                            }
                        }
                        map.weatherDecider.DisableRainFor(0);
                        ClassProgression.IceTrials.TransitionWeather(owner, map, rainMakerDef);
                    }
                    if (isViolent)
                    {
                        InitializeDarkStorms();
                    }
                }
            }
            return true;
        }

        private void InitializeDarkStorms()
        {
            GameConditionManager gameConditionManager = base.CasterPawn.Map.gameConditionManager;
            int duration = 45000;
            GameCondition cond = GameConditionMaker.MakeCondition(TorannMagicDefOf.DarkThunderstorm, duration);
            gameConditionManager.RegisterCondition(cond);
            GameCondition_DarkThunderstorm gcdt = cond as GameCondition_DarkThunderstorm;
            if(gcdt != null)
            {
                gcdt.faction = base.CasterPawn.Faction;
            }
        }
    }
}
