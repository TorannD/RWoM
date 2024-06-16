using Verse;

namespace TorannMagic
{
    public class TM_DamageWorker_None : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            Pawn p = victim as Pawn;
            if(p == null)
            {
                return base.Apply(dinfo, victim);
            }
            return new DamageResult();
        }
    }
}
