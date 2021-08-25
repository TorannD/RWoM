using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;

namespace TorannMagic.Ideology
{
    public class TM_RitualObligationTrigger_SeverMagicProperties : RitualObligationTriggerProperties
    {
        public TM_RitualObligationTrigger_SeverMagicProperties()
        {
            triggerClass = typeof(TM_RitualObligationTrigger_SeverMagic);
        }
    }
}
