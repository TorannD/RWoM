using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TorannMagic
{
    class Verb_Meteor : Verb_UseAbility
    {

        int pwrVal = 0;
        int verVal = 0;

        public override bool CanHitTargetFrom(IntVec3 casterPos, LocalTargetInfo targ)
        {
            bool flag = base.UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetThing;
            bool result;
            if (flag)
            {
                result = base.CanHitTargetFrom(casterPos, targ);
            }
            else
            {
                if( targ.Cell.IsValid && !targ.Cell.Fogged(base.caster.Map))
                {
                    if ((casterPos - targ.Cell).LengthHorizontal > this.verbProps.range)
                    {
                        result = false;
                    }
                    else
                    {
                        ShootLine shootLine;
                        result = base.TryFindShootLineFromTo(casterPos, targ, out shootLine);
                    }
                }
                else
                {
                    result = false;
                }
                             
            }
            return result;
        }

        protected override bool TryCastShot()
        {
            Map map = base.CasterPawn.Map;
            IntVec3 centerCell = this.currentTarget.Cell;

            verVal = this.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Meteor.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Meteor_ver").level;
            pwrVal = this.UseAbilityProps.TargetAoEProperties.range;

            bool result = false;
            bool arg_40_0;
            if (this.currentTarget != null && base.CasterPawn != null)
            {
                IntVec3 arg_29_0 = this.currentTarget.Cell;
                arg_40_0 = this.currentTarget.Cell.IsValid;
            }
            else
            {
                arg_40_0 = false;
            }
            bool flag = arg_40_0;
            if (flag)
            {
                if(pwrVal == 5)
                {
                    List<Thing> list = GenerateMeteoriteComposition(pwrVal);
                    SkyfallerMaker.SpawnSkyfaller(ThingDef.Named("TM_Meteorite_III"), list, centerCell, map);
                }
                else if(pwrVal == 4)
                {
                    List<Thing> list = GenerateMeteoriteComposition(pwrVal);
                    SkyfallerMaker.SpawnSkyfaller(ThingDef.Named("TM_Meteorite_II"), list, centerCell, map);
                }
                else if(pwrVal == 3)
                {
                    List<Thing> list = GenerateMeteoriteComposition(pwrVal);
                    SkyfallerMaker.SpawnSkyfaller(ThingDef.Named("TM_Meteorite_I"), list, centerCell, map);
                }
                else
                {
                    List<Thing> list = GenerateMeteoriteComposition(pwrVal);
                    SkyfallerMaker.SpawnSkyfaller(ThingDef.Named("TM_Meteorite"), list, centerCell, map);
                }
                
            }
            else
            {
                Log.Warning("failed to TryCastShot");
            }
            this.burstShotsLeft = 0;
            return result;
        }

        public List<Thing> GenerateMeteoriteComposition(int radius)
        {
            List<Thing> meteoriteComposition = new List<Thing>();
            meteoriteComposition.Clear();
            int compositionCount = GenRadial.RadialCellsAround(caster.Position, radius, false).ToList().Count;
            int rnd = Rand.RangeInclusive(0, 4);
            ThingDef thingDef = null;
            try
            {
                switch (rnd)
                {
                    case 0:
                        thingDef = ThingDef.Named("Sandstone");
                        break;
                    case 1:
                        thingDef = ThingDef.Named("Granite");
                        break;
                    case 2:
                        thingDef = ThingDef.Named("Limestone");
                        break;
                    case 3:
                        thingDef = ThingDef.Named("Slate");
                        break;
                    case 4:
                        thingDef = ThingDef.Named("Marble");
                        break;
                }
            }
            catch
            {
                //if someone took these out...
                IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                                   where (def.thingCategories != null && def.thingCategories.Contains(ThingCategoryDefOf.StoneChunks))
                                                   select def;
                thingDef = enumerable.RandomElement();
            }

            ThingDef preciousThingDef = null;
            try
            {
                rnd = Rand.RangeInclusive(0, 5);
                switch (rnd)
                {
                    case 0:
                        preciousThingDef = ThingDef.Named("MineableSilver");
                        break;
                    case 1:
                        preciousThingDef = ThingDef.Named("MineableGold");
                        break;
                    case 2:
                        preciousThingDef = ThingDef.Named("MineableUranium");
                        break;
                    case 3:
                        preciousThingDef = ThingDef.Named("MineablePlasteel");
                        break;
                    case 4:
                        preciousThingDef = ThingDef.Named("MineableJade");
                        break;
                    case 5:
                        preciousThingDef = ThingDef.Named("MineableComponentsIndustrial");
                        break;
                }
            }
            catch
            {
                //if someone took these out...
                IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                                   where (def.mineable)
                                                   select def;
                thingDef = enumerable.RandomElement();
            }

            for (int i =0; i < compositionCount; i++)
            {
                Thing tmpThing = null;
                if(i == compositionCount - 1)
                {
                    tmpThing = ThingMaker.MakeThing(preciousThingDef, null);
                }
                else if(i > compositionCount - 9)
                {
                    if(Rand.Chance((.15f * verVal)))
                    {
                        tmpThing = ThingMaker.MakeThing(preciousThingDef, null);
                    }
                    else
                    {
                        tmpThing = ThingMaker.MakeThing(thingDef, null);
                    }
                }
                else
                {
                    if (Rand.Chance(.05f))
                    {
                        tmpThing = ThingMaker.MakeThing(ThingDef.Named("MineableSteel"), null);
                    }
                    else
                    {
                        tmpThing = ThingMaker.MakeThing(thingDef, null);
                    }
                }
                meteoriteComposition.Add(tmpThing);
            }

            return meteoriteComposition;
        }

    }
}
