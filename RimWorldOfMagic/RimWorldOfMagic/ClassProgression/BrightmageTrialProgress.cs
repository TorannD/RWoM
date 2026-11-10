using System;
using Verse;
namespace TorannMagic.ClassProgression
{
    public sealed class BrightmageTrialProgress : IExposable
    {
        public float LanceDamage;
        public void ExposeData()
        {
            Scribe_Values.Look(ref LanceDamage, "lanceDamage", 0f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                LanceDamage = float.IsNaN(LanceDamage) || float.IsInfinity(LanceDamage) ? 0 : Math.Max(0, Math.Min(20, LanceDamage));
        }
    }
}
