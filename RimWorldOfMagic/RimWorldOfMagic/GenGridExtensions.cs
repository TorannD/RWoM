using Verse;

namespace TorannMagic
{
    public static class GenGridExtensions
    {
        public static bool InBoundsWithNullCheck(this IntVec3 c, Map map)
        {
            return map != null && c.InBounds(map);
        }
    }
}