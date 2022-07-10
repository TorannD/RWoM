using System;
using Verse;

namespace TorannMagic
{
    public static class GenGridExtensions
    {
        public static bool InBoundsWithNullCheck(this IntVec3 c, Map map)
        {
            // Use try catch as there are only rare exceptions when c is deleted after call but before Inbounds is called
            try
            {
                return c.InBounds(map);
            }
            catch (NullReferenceException e)
            {
                Log.Warning($"NullReference during InBounds call: {e.ToString()}");
                return false;
            }
        }
    }
}