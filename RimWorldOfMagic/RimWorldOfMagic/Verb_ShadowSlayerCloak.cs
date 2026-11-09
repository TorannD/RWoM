using AbilityUser;
using System.Linq;
using RimWorld;
using TorannMagic.ClassProgression;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public class Verb_ShadowSlayerCloak : Verb_UseAbility
    {
        protected override bool TryCastShot() => Activate(CasterPawn, CasterPawn);

        internal static bool CanProject(Pawn body)
        {
            var def = TorannMagicDefOf.TM_ShadowSlayerCloakHD;
            if (def?.comps?.Any(c => c.compClass == typeof(HediffComp_SpiritCastOrigin)) != true) return false;
            var existing = body.health.hediffSet.GetFirstHediffOfDef(def);
            return existing == null || existing.TryGetComp<HediffComp_SpiritCastOrigin>() is { Projected: true, Valid: true };
        }

        internal static bool Activate(Pawn body, Pawn source)
        {
            if (body?.Spawned != true || body.Dead || source?.GetCompAbilityUserMight() is not CompAbilityUserMight might ||
                (body != source && !CanProject(body))) return false;
            var versatility = TM_Calc.GetSkillVersatilityLevel(source, TorannMagicDefOf.TM_ShadowSlayer);
            float severity = 40 + might.mightPwr * 3 * versatility;
            var def = TorannMagicDefOf.TM_ShadowSlayerCloakHD;
            HealthUtility.AdjustSeverity(body, def, severity);
            var cloak = body.health.hediffSet.GetFirstHediffOfDef(def);
            if (cloak == null) return false;
            if (body != source) cloak.TryGetComp<HediffComp_SpiritCastOrigin>().Bind(source);
            var timer = cloak.TryGetComp<HediffComp_Disappears>();
            if (timer != null) timer.ticksToDisappear = Mathf.RoundToInt(60 * severity);
            for (int i = 0; i < 3; i++)
            {
                Vector3 pos = body.DrawPos;
                pos.x += Rand.Range(-.5f,.5f);
                pos.z += Rand.Range(-.5f,.5f);
                FleckMaker.ThrowSmoke(pos, body.Map, Rand.Range(.6f,1.2f));
            }
            return true;
        }
    }
}
