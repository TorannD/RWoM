using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    public sealed class SpiritStormSkyfaller : Skyfaller
    {
        internal Pawn source;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref source, "TM_stormSoul");
        }
    }

    [HarmonyPatch(typeof(Skyfaller), "Impact")]
    internal static class SpiritStormSkyfallerImpactPatch
    {
        private static Thing Source(Skyfaller skyfaller) => (skyfaller as SpiritStormSkyfaller)?.source;

        // In 1.7 the native explosion pushes explosionDamage followed by a null instigator.
        // Replace only that argument; roof impacts, shrapnel and other mods retain the vanilla call.
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var damage = AccessTools.Field(typeof(SkyfallerProperties), nameof(SkyfallerProperties.explosionDamage));
            var indices = Enumerable.Range(1, code.Count - 1)
                .Where(i => code[i].opcode == OpCodes.Ldnull && code[i - 1].LoadsField(damage)).ToList();
            if (indices.Count != 1)
            {
                Compatibility.TM_Log.Warning("SpiritStorm", "Could not identify the native skyfaller explosion instigator.");
                return code;
            }
            int index = indices[0];
            code[index].opcode = OpCodes.Ldarg_0;
            code.Insert(index + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SpiritStormSkyfallerImpactPatch), nameof(Source))));
            return code;
        }
    }
}
