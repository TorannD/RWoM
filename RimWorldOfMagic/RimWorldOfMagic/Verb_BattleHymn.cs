using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using Verse.Sound;

namespace TorannMagic
{
    public class Verb_BattleHymn : Verb_SpiritTargetable
    {
        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            var owner = Ability?.Pawn ?? body;
            if (body?.Spawned != true || body.Dead || currentTarget.Pawn != body || owner?.health == null) return false;
            var def = HediffDef.Named("TM_SingBattleHymnHD");
            var song = owner.health.hediffSet.GetFirstHediffOfDef(def);
            if (song != null)
            {
                owner.health.RemoveHediff(song);
                TM_MoteMaker.ThrowSiphonMote(body.DrawPos, body.Map, 1f);
            }
            else
            {
                TorannMagicDefOf.TM_BattleHymnSD.PlayOneShotOnCamera(body.Map);
                HealthUtility.AdjustSeverity(owner, def, .95f);
                TM_MoteMaker.ThrowNoteMote(body.DrawPos, body.Map, .8f);
                TM_MoteMaker.ThrowNoteMote(body.DrawPos, body.Map, .6f);
                TM_MoteMaker.ThrowNoteMote(body.DrawPos, body.Map, .4f);
            }
            return true;
        }

        protected override bool TryCastShot() => ApplyHostedEffect();
    }
}
