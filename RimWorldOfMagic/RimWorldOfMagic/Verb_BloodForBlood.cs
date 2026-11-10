using System.Linq;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;

namespace TorannMagic
{
    public class Verb_BloodForBlood : Verb_SpiritTargetable
    {
        internal static bool ValidVictim(Pawn body, Pawn victim) =>
            body?.Spawned == true && victim?.Spawned == true && !victim.Dead &&
            victim != body && victim.Map == body.Map && victim.RaceProps.BloodDef != null;

        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            var victim = currentTarget.Pawn;
            var owner = SpiritRebirthCasting.SkillOwner(body, Ability);
            var comp = owner?.GetCompAbilityUserMagic();
            if (comp == null || !ValidVictim(body,victim)) return false;
            var data = comp.MagicData;
            int power = data.MagicPowerSkill_BloodForBlood.FirstOrDefault(x => x.label == "TM_BloodForBlood_pwr")?.level ?? 0;
            if (owner.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true)
                power = owner.GetCompAbilityUserMight()?.MightData.MightPowerSkill_Mimic.FirstOrDefault(x => x.label == "TM_Mimic_pwr")?.level ?? 0;
            int gift = data.MagicPowerSkill_BloodGift.FirstOrDefault(x => x.label == "TM_BloodGift_pwr")?.level ?? 0;
            float strength = (.5f + .1f * power) * comp.arcaneDmg * (1f + .1f * gift);
            HealthUtility.AdjustSeverity(victim, TorannMagicDefOf.TM_BloodForBloodHD, strength);
            var link = victim.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_BloodForBloodHD)?.TryGetComp<HediffComp_BloodForBlood>();
            if (link == null) return false;
            link.Link(body, owner);
            for (int i = 0; i < 4; i++)
                TM_MoteMaker.ThrowBloodSquirt(victim.DrawPos, victim.Map, Rand.Range(.6f,.9f));
            if (victim.Faction == null && victim.RaceProps.Animal && victim.mindState != null)
                victim.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, true, false, false, null);
            if (victim.IsColonist && !body.IsColonist) TM_Action.SpellAffectedPlayerWarning(victim);
            return true;
        }

        protected override bool TryCastShot()
        {
            if (!ApplyHostedEffect() && CasterPawn != null)
                Messages.Message("TM_InvalidTarget".Translate(CasterPawn.LabelShort,TorannMagicDefOf.TM_BloodForBlood.label), MessageTypeDefOf.RejectInput);
            PostCastShot(false, out bool fired);
            return fired;
        }
    }
}
