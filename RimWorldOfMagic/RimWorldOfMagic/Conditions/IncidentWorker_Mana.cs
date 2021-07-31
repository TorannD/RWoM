using RimWorld;
using Verse;

namespace TorannMagic
{
    public class IncidentWorker_Mana : IncidentWorker_MakeGameCondition
    {       
        protected override bool CanFireNowSub(IncidentParms target)
        {
            if (!base.CanFireNowSub(target))
            {
                return false;
            }
            return true;
        }
    }
}
