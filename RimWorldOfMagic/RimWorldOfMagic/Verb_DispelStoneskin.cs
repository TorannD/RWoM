using System.Linq;
using TorannMagic.ClassProgression;
using Verse;

namespace TorannMagic
{
    public class Verb_DispelStoneskin : Verb_SpiritTargetable
    {
        protected override bool ApplyHostedEffect()
        {
            var owner = SpiritRebirthCasting.SkillOwner(CasterPawn,Ability);
            var comp = owner?.GetCompAbilityUserMagic();
            if (comp == null) return false;
            var recipients = comp.StoneskinPawns;
            bool dispelled = false;
            foreach (var recipient in recipients.ToArray())
            {
                var ward = recipient.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_StoneskinHD);
                var actualOwner = ward?.TryGetComp<HediffComp_Stoneskin>()?.Owner;
                if (ward != null && (actualOwner == null || actualOwner == owner))
                {
                    recipient.health.RemoveHediff(ward);
                    dispelled = true;
                }
                recipients.RemoveAll(p => p == recipient);
            }
            return dispelled;
        }

        protected override bool TryCastShot()
        {
            ApplyHostedEffect();
            PostCastShot(false,out bool fired);
            return fired;
        }
    }
}
