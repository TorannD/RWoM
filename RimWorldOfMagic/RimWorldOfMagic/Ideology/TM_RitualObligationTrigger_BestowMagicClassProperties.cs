using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;

namespace TorannMagic.Ideology
{
    public class TM_RitualObligationTrigger_BestowMagicClassProperties : RitualObligationTriggerProperties
    {
        public TM_RitualObligationTrigger_BestowMagicClassProperties()
        {
            triggerClass = typeof(TM_RitualObligationTrigger_BestowMagicClass);
        }
    }
}
