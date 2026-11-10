using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class DemonSoulBond
    {
        internal static HediffDef Sender(bool mental) => DefDatabase<HediffDef>.GetNamedSilentFail(mental ? "TM_WDSoulBondMentalHD" : "TM_SDSoulBondPhysicalHD");
        internal static HediffDef Receiver(bool mental) => DefDatabase<HediffDef>.GetNamedSilentFail(mental ? "TM_SoulBondMentalHD" : "TM_SoulBondPhysicalHD");

        internal static Pawn TravelPartner(Pawn owner)
        {
            var original = owner?.GetCompAbilityUserMagic()?.soulBondPawn;
            if (original == null || original.Destroyed || original.Dead) return null;
            foreach (var effect in original.health.hediffSet.hediffs)
            {
                var link = effect.TryGetComp<HediffComp_SoulBondHost>();
                if (link?.BonderPawn != owner) continue;
                if (original.Spawned || original.ParentHolder is RimWorld.Planet.Caravan) return original;
                var body = link.polyHost;
                if (body != null && !body.Destroyed && !body.Dead &&
                    (body.Spawned || body.ParentHolder is RimWorld.Planet.Caravan)) return body;
            }
            return null;
        }

        internal static bool SourceAvailable(Pawn body, Pawn owner, bool mental)
        {
            var sender = Sender(mental);
            if (sender?.comps?.Exists(c => c.compClass == typeof(HediffComp_SoulBond)) != true ||
                Receiver(mental)?.comps?.Exists(c => c.compClass == typeof(HediffComp_SoulBondHost)) != true) return false;
            var source = body.health.hediffSet.GetFirstHediffOfDef(sender);
            return source == null || source.TryGetComp<HediffComp_SoulBond>()?.Owner == owner;
        }

        internal static void ApplySource(Pawn body, Pawn owner, bool mental, float rank)
        {
            var def = Sender(mental);
            var source = body.health.hediffSet.GetFirstHediffOfDef(def);
            if (source == null)
            {
                source = HediffMaker.MakeHediff(def, body);
                source.TryGetComp<HediffComp_SoulBond>().originalId = body != owner ? owner.thingIDNumber : 0;
                source.Severity = rank;
                body.health.AddHediff(source);
            }
            else if (source.TryGetComp<HediffComp_SoulBond>()?.Owner == owner) source.Severity = rank;
        }

        internal static void RemoveSource(Pawn body, Pawn owner, bool mental, bool projectedOnly)
        {
            var sender = Sender(mental);
            if (sender == null) return;
            var source = body?.health?.hediffSet?.GetFirstHediffOfDef(sender);
            var link = source?.TryGetComp<HediffComp_SoulBond>();
            if (link != null && (link.originalId == owner.thingIDNumber || (!projectedOnly && body == owner && link.originalId == 0)))
                body.health.RemoveHediff(source);
        }

        internal static void EndOnDeath(Pawn owner, Pawn partner, bool mental, bool partnerDied)
        {
            var magic = owner?.GetCompAbilityUserMagic();
            if (partner == null || magic?.soulBondPawn != partner) return;
            magic.soulBondPawn = null;
            magic.spell_ShadowStep = magic.spell_ShadowCall = false;
            magic.RemovePawnAbility(TorannMagicDefOf.TM_ShadowStep);
            magic.RemovePawnAbility(TorannMagicDefOf.TM_ShadowCall);
            if (partnerDied)
            {
                var body = SpiritRebirthState.TryGet(owner,out var state) ? state.Host : owner;
                RemoveSource(body,owner,mental,false);
            }
            else
            {
                var effect = partner.health?.hediffSet?.GetFirstHediffOfDef(Receiver(mental));
                if (effect?.TryGetComp<HediffComp_SoulBondHost>()?.BonderPawn == owner) partner.health.RemoveHediff(effect);
            }
            DemonRitual.BondChanged(magic,mental,partner,null);
        }

        internal static void RestoreTravelControls(CompAbilityUserMagic magic)
        {
            var partner = magic.soulBondPawn;
            if (partner == null || partner.Dead || partner.Destroyed || partner.health?.hediffSet == null ||
                !partner.health.hediffSet.hediffs.Any(h => h.TryGetComp<HediffComp_SoulBondHost>()?.BonderPawn == magic.Pawn)) return;
            magic.spell_ShadowStep = magic.spell_ShadowCall = true;
            if (magic.AbilityData.Powers.Count(p => p?.Def == TorannMagicDefOf.TM_ShadowStep) != 1)
                TM_ClassState.AddRuntimeAbility(magic,TorannMagicDefOf.TM_ShadowStep);
            if (magic.AbilityData.Powers.Count(p => p?.Def == TorannMagicDefOf.TM_ShadowCall) != 1)
                TM_ClassState.AddRuntimeAbility(magic,TorannMagicDefOf.TM_ShadowCall);
        }

        internal static void UpdateProjection(Pawn soul, bool ending)
        {
            if (!SpiritRebirthState.TryGet(soul,out var state) || state.Host is not Pawn body || body.health?.hediffSet == null) return;
            var magic = soul.GetCompAbilityUserMagic();
            if (ending)
            {
                RemoveSource(body,soul,true,true);
                RemoveSource(body,soul,false,true);
                return;
            }
            DemonRitual.RefreshControl(magic);
            var partner = magic?.soulBondPawn;
            if (partner == null || state.DepletionPending || !state.HasValidHost(soul)) return;
            if (partner.Destroyed || partner.Dead)
            {
                EndOnDeath(soul,partner,ClassIdentity.DemonEffect(soul,true,soul.gender == Gender.Male),true);
                return;
            }
            bool mental = ClassIdentity.DemonEffect(soul,true,soul.gender == Gender.Male);
            var received = partner.health?.hediffSet?.GetFirstHediffOfDef(Receiver(mental));
            if (received == null && partner.health?.hediffSet != null && magic.AbilityData.Powers.Any(p=>p.Def == TorannMagicDefOf.TM_SoulBond) &&
                SourceAvailable(body,soul,mental))
            {
                // Repair an incomplete saved pair only while both named partners are alive.
                received = HediffMaker.MakeHediff(Receiver(mental),partner);
                received.TryGetComp<HediffComp_SoulBondHost>().BonderPawn = soul;
                received.Severity = .5f + TM_Calc.GetSkillVersatilityLevel(soul,TorannMagicDefOf.TM_SoulBond);
                partner.health.AddHediff(received);
            }
            if (received?.TryGetComp<HediffComp_SoulBondHost>()?.BonderPawn != soul) return;
            RestoreTravelControls(magic);
            if (body.health.hediffSet.HasHediff(Sender(mental)) || !SourceAvailable(body,soul,mental)) return;
            ApplySource(body,soul,mental,.5f + TM_Calc.GetSkillPowerLevel(soul,TorannMagicDefOf.TM_SoulBond));
        }
    }
}
