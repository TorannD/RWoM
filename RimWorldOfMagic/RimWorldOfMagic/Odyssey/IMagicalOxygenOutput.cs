namespace TorannMagic.Odyssey
{
    // Content-owned extension point: an optional output comp on the same building.
    // The pump retains its serialized mode, pulse clock and already-paid output.
    // Implementations must debit pending only for oxygen actually delivered and
    // obtain any new supply exclusively through pump.SupplyPulse.
    public interface IMagicalOxygenOutput
    {
        bool Available { get; }
        void Supply(CompMagicalOxygen pump, ref double pending);
    }
}
