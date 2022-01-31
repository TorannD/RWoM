using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

using Verse.Sound;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class GolemWorkstationEffect_CreateItem : GolemWorkstationEffect
    {        
        public override void StartEffect(Building_TMGolemBase golem_building, TM_GolemUpgrade upgrade, float effectLevel = 1)
        {
                     
        }

        public override void ContinueEffect(Building_TMGolemBase golem_building)
        {
            base.StartEffect(golem_building, null, 0);
            TM_GolemItemRecipeDef recipe = golem_building.creationRecipes[0];
            if (recipes.Contains(recipe))
            {
                Thing item = ThingMaker.MakeThing(recipe.outputThing);
                item.stackCount = recipe.outputCount;
                GenPlace.TryPlaceThing(item, golem_building.InteractionCell, golem_building.Map, ThingPlaceMode.Near);
                FleckMaker.ThrowDustPuff(golem_building.InteractionCell, golem_building.Map, .8f);
                golem_building.creationRecipes.Remove(recipe);
            }
            else
            {
                Log.Message("Golem was unable to create recipe " + recipe.defName);
            }
        }

        public override bool CanDoEffect(Building_TMGolemBase golem_building)
        {
            if (!recipes.Contains(golem_building.creationRecipes[0]))
            {
                return false;                
            }
            return base.CanDoEffect(golem_building);
        }
    }
}
