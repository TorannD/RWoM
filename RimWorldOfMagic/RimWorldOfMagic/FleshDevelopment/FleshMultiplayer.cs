using Verse;

namespace TorannMagic.FleshDevelopment
{
    public static class FleshMultiplayer
    {
        public static void Register() => TMPCompat.Setup();
        // Resolve the owner inside the synchronized execution: serializing both pawns
        // would make Multiplayer reject owners and undead on different maps.
        public static void StopFeast(Pawn undead)
        {
            FleshActions.StopFeast(FleshRepository.Owner(undead), undead);
        }

    }
}
