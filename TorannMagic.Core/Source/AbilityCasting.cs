using System;
using Verse;

namespace TorannMagic.Core
{
    public enum CastMode { Manual, Automatic }
    public enum CastAdmission
    {
        Allowed, ProviderUnavailable, UnsupportedAbility, InvalidRequest, OffMap, Busy,
        ViolenceDisabled, CasterRestricted, InvalidTarget, AutomaticCastDenied, AbilityNotReady
    }

    public static class AbilityCasting
    {
        internal static Func<Verb, LocalTargetInfo, bool, string> HostReader { private get; set; }

        public static CastAdmission Check(Verb verb, LocalTargetInfo target, CastMode mode)
        {
            if (verb == null || (mode != CastMode.Manual && mode != CastMode.Automatic)) return CastAdmission.InvalidRequest;
            if (HostReader == null) return CastAdmission.ProviderUnavailable;
            return Enum.TryParse<CastAdmission>(HostReader(verb, target, mode == CastMode.Automatic), out var result)
                ? result : CastAdmission.ProviderUnavailable;
        }
    }
}
