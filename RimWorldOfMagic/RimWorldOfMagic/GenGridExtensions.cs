using Verse;
using System;
using UnityEngine;

namespace TorannMagic
{
    public static class GenGridExtensions
    {
        public static bool InBoundsWithNullCheck(this IntVec3 c, Map map)
        {
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

        public static bool InBoundsWithNullCheck(this Vector3 v, Map map)
        {
            try
            {
                return v.InBounds(map);
            }
            catch (NullReferenceException e)
            {
                Log.Warning($"NullReference during InBounds call: {e.ToString()}");
                return false;
            }
        }
    }
}
