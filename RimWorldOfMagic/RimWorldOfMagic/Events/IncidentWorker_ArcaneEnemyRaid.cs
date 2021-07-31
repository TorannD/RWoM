using RimWorld;
using System;
using Verse;


namespace TorannMagic
{
    //public class IncidentWorker_ArcaneEnemyRaid : IncidentWorker_RaidEnemy
    //{
    //    protected override bool TryExecuteWorker(IncidentParms parms)
    //    {
    //        Map map = (Map)parms.target;
    //        bool result;
    //        if (map == null)
    //        {
    //            result = false;
    //        }
    //        else if (!Find.WorldObjects.AnyMapParentAt(map.Tile))
    //        {
    //            result = false;
    //        }
    //        else if (!Find.WorldObjects.MapParentAt(map.Tile).HasMap)
    //        {
    //            result = false;
    //        }
    //        else
    //        {
    //            try
    //            {
    //                result = base.TryExecuteWorker(parms);
    //            }
    //            catch (NullReferenceException ex)
    //            {
    //                result = false;
    //            }
    //        }
    //        return result;
    //    }
    //}
}
