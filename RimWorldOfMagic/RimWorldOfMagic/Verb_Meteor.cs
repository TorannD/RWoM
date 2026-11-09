using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TorannMagic
{
    class Verb_Meteor : ClassProgression.Verb_SpiritTargetable
    {

        private static readonly string[] rocks = { "Sandstone","Granite","Limestone","Slate","Marble" };
        private static readonly string[] ores = { "MineableSilver","MineableGold","MineableUranium","MineablePlasteel","MineableJade","MineableComponentsIndustrial" };
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
            ApplyHostedEffect();
            burstShotsLeft = 0;
            return false;
        }

        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            var map = body?.Map;
            var cell = currentTarget.Cell;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body,Ability);
            if (owner == null || map == null || !currentTarget.IsValid || !cell.InBounds(map)) return false;
            var data = owner.GetCompAbilityUserMagic()?.MagicData;
            if (data == null) return false;
            verVal = ClassProgression.SpiritRebirthRuntime.SavedRank(data,"TM_Meteor_ver");
            int radius = UseAbilityProps.TargetAoEProperties.range;
            string suffix = radius switch { 5 => "_III", 4 => "_II", 3 => "_I", _ => "" };
            var falling = DefDatabase<ThingDef>.GetNamedSilentFail("TM_Meteorite" + suffix);
            if (falling == null) return false;
            var contents = GenerateMeteoriteComposition(radius);
            if (contents.Count == 0) return false;
            return SkyfallerMaker.SpawnSkyfaller(falling, contents, cell, map) != null;
        }

        public List<Thing> GenerateMeteoriteComposition(int radius)
        {
            var meteoriteComposition = new List<Thing>();
            int compositionCount = GenRadial.NumCellsInRadius(radius) - 1;
            ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(rocks.RandomElement());
            ThingDef preciousThingDef = DefDatabase<ThingDef>.GetNamedSilentFail(ores.RandomElement());
            if (thingDef == null && !DefDatabase<ThingDef>.AllDefsListForReading.Where(d =>
                d.building?.isNaturalRock == true && d.mineable).TryRandomElement(out thingDef)) return meteoriteComposition;
            if (preciousThingDef == null && !DefDatabase<ThingDef>.AllDefsListForReading.Where(d =>
                d.mineable && d.building?.mineableThing != null).TryRandomElement(out preciousThingDef)) return meteoriteComposition;
            var steel = DefDatabase<ThingDef>.GetNamedSilentFail("MineableSteel") ?? thingDef;

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
                        tmpThing = ThingMaker.MakeThing(steel, null);
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
