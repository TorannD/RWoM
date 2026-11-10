using TorannMagic.Odyssey;
using Verse;

namespace TorannMagic.VGE
{
    // Stateless adapter added alongside VGE's native connector by conditional XML.
    public class CompVgeOxygenOutput : ThingComp, IMagicalOxygenOutput
    {
        public bool Available => VgeOxygenBridge.Available(parent);
        public void Supply(CompMagicalOxygen pump, ref double pending)
        {
            if (pump?.parent == parent && parent != null)
                VgeOxygenBridge.Supply(pump, ref pending);
        }
    }
}
