using RimWorld;
using Verse;

namespace TorannMagic.ModCheck
{
    // Retain the public entry point for existing callers and extensions.
    public static class AlienHumanoidRaces
    {
        public static bool TryGetBackstory_DisallowedTrait(ThingDef thingDef, Pawn pawn, TraitDef td)
            => Compatibility.ClassDefinitionCompatibility.AllowsTrait(thingDef, pawn, td);
    }
}