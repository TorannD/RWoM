using RimWorld;
using Verse;

namespace TorannMagic
{
    public class Verb_SummonTotemLightning : Verb_SummonTotem
    {
        protected override ThingDef TotemDef => TorannMagicDefOf.TM_LightningTotem;
        protected override FleckDef SummonFleck => TorannMagicDefOf.SparkFlash;

        protected override void Configure(Building totem, Pawn owner, int power, int versatility)
        {
            var structure = (Building_TMTotem_Lightning)totem;
            structure.pwrVal = power;
            structure.verVal = versatility;
        }
    }
}
