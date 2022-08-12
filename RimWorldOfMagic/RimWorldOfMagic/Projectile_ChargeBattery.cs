using AbilityUser;
using RimWorld;
using Verse;
using TorannMagic.Golems;

namespace TorannMagic
{
    public class Projectile_ChargeBattery : Projectile_AbilityBase
    {
        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            CellRect cellRect = CellRect.CenteredOn(base.Position, 1);
            cellRect.ClipInsideMap(map);
            Building bldg = new Building();
            Pawn caster = this.launcher as Pawn;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();

            IntVec3 c = cellRect.CenterCell;

            TMPawnGolem pg = c.GetFirstPawn(map) as TMPawnGolem;
            bldg = cellRect.CenterCell.GetFirstBuilding(map);            
            if (bldg != null)
            {
                if (bldg is Building_TMGolemBase gb)
                {
                    gb.Energy.AddEnergyFlat(400 * comp.arcaneDmg);
                }
                else if (bldg.GetComp<CompPowerBattery>() != null)
                {
                    float energyAdded = 400f * comp.arcaneDmg;
                    bldg.GetComp<CompPowerBattery>().AddEnergy(energyAdded);
                    if (energyAdded > bldg.GetComp<CompPowerBattery>().AmountCanAccept)
                    {
                        bldg.GetComp<CompPowerBattery>().AddEnergy(bldg.GetComp<CompPowerBattery>().AmountCanAccept);
                    }
                    else
                    {
                        bldg.GetComp<CompPowerBattery>().AddEnergy(energyAdded);
                    }
                }
                else
                {
                    Messages.Message("InvalidBattery".Translate(), MessageTypeDefOf.NegativeEvent);
                }                    
            }
            else if(pg != null)
            {
                pg.Golem.Energy.AddEnergy(400f * comp.arcaneDmg);
            }
            else
            {
                Messages.Message("NotABuilding".Translate(), MessageTypeDefOf.NegativeEvent);
            }            

        }
    }
}
