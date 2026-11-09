namespace TorannMagic.VGE
{
    // The magical engine's fuel tank stores flight fuel. Vanilla VGE engines have
    // no refuelable comp, so their power plant is independent of flight reserves.
    public class CompMagicalGravEnginePower : VanillaGravshipExpanded.CompPowerPlantGravEngine
    {
        public override void UpdateDesiredPowerOutput()
        {
            refuelableComp = null;
            base.UpdateDesiredPowerOutput();
        }
    }
}

