using System;
using Verse;
namespace TorannMagic.ClassProgression
{
    public sealed class ShamanTrialProgress : IExposable
    {
        public float Healing;
        public void ExposeData()
        {
            Scribe_Values.Look(ref Healing, "healing", 0f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                Healing = float.IsNaN(Healing) || float.IsInfinity(Healing) ? 0 : Math.Max(0, Math.Min(8, Healing));
        }
    }
}
