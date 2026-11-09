using System;
using System.Linq;
using TorannMagic.ClassProgression;
using Verse;

namespace TorannMagic
{
    internal static class CoreDisciplineProvider
    {
        internal static void Read(Pawn pawn, Action<string, int, int> add)
        {
            bool progressed = ClassIdentity.TryGetState(pawn, out var state);
            if (progressed)
                foreach (var path in state.Disciplines)
                    if (path != null) add(path.ClassId, (int)path.Domain, (int)path.Stage);

            var traits = pawn.story?.traits?.allTraits;
            if (traits == null) return;
            foreach (var trait in traits)
            {
                if (trait?.def == null || trait.Suppressed) continue;
                var declared = TM_ClassUtility.CustomClasses?.Where(c => c?.classTrait == trait.def).ToList();
                int kind = 0;
                if (declared?.Count > 0)
                {
                    foreach (var cls in declared)
                        if (cls.traitDegree == trait.Degree) kind |= (cls.isMage ? 1 : 0) | (cls.isFighter ? 2 : 0);
                }
                else
                {
                    if (progressed) continue;
                    var profile = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.FirstOrDefault(d => d.legacyTrait == trait.def && d.profileId != "rwom.wanderer");
                    if (profile != null) { add(profile.profileId, (int)profile.domain, 0); continue; }
                    kind = (TM_Data.MagicTraits.Contains(trait.def) ? 1 : 0) | (TM_Data.MightTraits.Contains(trait.def) ? 2 : 0);
                }
                if (kind != 0) add("legacy." + trait.def.defName + "." + trait.Degree.ToString(System.Globalization.CultureInfo.InvariantCulture), kind, 0);
            }
        }
    }
}
