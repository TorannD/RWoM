using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace TorannMagic.Compatibility
{
    public static class ClassDefinitionCompatibility
    {
        // Same normalization point for a roster preview as for actual Trait construction.
        // Specific third-party migrations patch this hook in Integrations.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int CanonicalTraitDegree(TraitDef trait, int degree) => degree;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool AllowsTrait(ThingDef race, Pawn pawn, TraitDef trait) => true;
    }
}
