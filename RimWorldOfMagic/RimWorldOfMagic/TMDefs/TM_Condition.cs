using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace TorannMagic.TMDefs
{
    public class TM_Condition
    {
        //Game condition the recipe generates
        public GameConditionDef resultCondition = new GameConditionDef();
        public int conditionDuration = -1;
        public bool conditionPermanent = false;
        public bool conditionRemove = false;
        public bool conditionAdd = false;
        public bool conditionReduceByDuration = false;
        public bool conditionIncreaseByDuration = false;
        public bool conditionRandom = false;
        //How many times to execute
        public IntRange countRange = new IntRange(1, 1);

        //Weather condition the recipe generates
        public WeatherDef resultWeather = new WeatherDef();
    }
}
