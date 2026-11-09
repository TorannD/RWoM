using System.Linq;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;

namespace TorannMagic
{
    public class Verb_Entertain : Verb_SpiritTargetable
    {
        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            var owner = Ability?.Pawn ?? body;
            var comp = owner?.GetCompAbilityUserMagic();
            var def = TorannMagicDefOf.TM_Entertain;
            if (currentTarget.Thing != body || body?.Spawned != true || body.Dead || comp == null) return false;
            var song = def.abilityHediff;
            var active = owner.health.hediffSet.GetFirstHediffOfDef(song);
            var power = comp.MagicData.MagicPowersB.FirstOrDefault(p => p.abilityDef == def);
            if (active != null)
            {
                owner.health.RemoveHediff(active);
                AutoCast.AutocastPolicy.SetAutocast(comp, power, false);
                return true;
            }
            if (comp.maxMP < def.upkeepEnergyCost)
            {
                Messages.Message("TM_NotEnoughManaToSustain".Translate(body.LabelShort, def.label), MessageTypeDefOf.RejectInput);
                return false;
            }
            HealthUtility.AdjustSeverity(owner, song, .95f);
            TM_MoteMaker.ThrowNoteMote(body.DrawPos, body.Map, .8f);
            AutoCast.AutocastPolicy.SetAutocast(comp, power, true);
            return true;
        }

        protected override bool TryCastShot()
        {
            ApplyHostedEffect();
            return true;
        }
    }
}
