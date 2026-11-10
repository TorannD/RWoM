using RimWorld;
using Verse;

namespace TorannMagic
{
    public class Verb_SummonTotemEarth : Verb_SummonTotem
    {
        protected override ThingDef TotemDef => TorannMagicDefOf.TM_EarthTotem;
        protected override ThingDef Material => ThingDefOf.BlocksGranite;

        protected override void Configure(Building totem, Pawn owner, int power, int versatility)
        {
            var structure = (Building_TMTotem_Earth)totem;
            structure.pwrVal = power;
            structure.verVal = versatility;
        }
    }
}
