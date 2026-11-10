using RimWorld;
using TorannMagic.ClassProgression;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public class Verb_SoL_Equalize : Verb_SpiritTargetable
    {
        internal static bool Available(CompAbilityUserMagic owner, Pawn body) =>
            owner?.SoL?.Spawned == true && owner.SoL.Map == body?.Map && owner.SoL.OwnerPawn == owner.Pawn &&
            owner.Pawn.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightCapacitanceHD)?.TryGetComp<HediffComp_LightCapacitance>() != null;

        private bool Exchange()
        {
            var body = CasterPawn;
            var owner = Ability?.Pawn ?? body;
            var comp = owner?.GetCompAbilityUserMagic();
            var spirit = comp?.SoL;
            var light = owner?.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightCapacitanceHD)?.TryGetComp<HediffComp_LightCapacitance>();
            if (body == null || body.Downed || spirit == null || light == null || owner != body && !Available(comp,body)) return false;
            float total = spirit.LightEnergy + light.LightEnergy;
            light.LightEnergy = Mathf.Clamp(total * .5f, Mathf.Max(0,total - spirit.LightEnergyMax), light.LightEnergyMax);
            spirit.LightEnergy = total - light.LightEnergy;
            FleckMaker.ThrowLightningGlow(body.DrawPos,body.Map,1.2f);
            FleckMaker.ThrowLightningGlow(spirit.DrawPos,spirit.Map,1.2f);
            return true;
        }

        protected override bool ApplyHostedEffect() => Exchange();

        protected override bool TryCastShot()
        {
            Exchange();
            burstShotsLeft = 0;
            return false;
        }
    }
}
