using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class DemonRitual
    {
        // The two receiver hediffs identify the actual owners, including souls in hosts.
        internal static bool TryPair(Pawn owner,out Pawn offering,out Pawn succubus,out Pawn warlock)
        {
            offering = owner?.GetCompAbilityUserMagic()?.soulBondPawn;
            succubus = warlock = null;
            if (owner == null || owner.Destroyed || owner.Dead || offering?.Spawned != true || offering.Dead ||
                offering.health?.hediffSet == null) return false;
            var debt = DefDatabase<HediffDef>.GetNamedSilentFail("TM_DemonicPriceHD");
            if (debt != null && offering.health.hediffSet.HasHediff(debt)) return false;
            succubus = Bonder(offering,false);
            warlock = Bonder(offering,true);
            return succubus != null && warlock != null && succubus != warlock && (owner == succubus || owner == warlock);
        }

        private static Pawn Bonder(Pawn offering,bool mental)
        {
            var def = DemonSoulBond.Receiver(mental);
            if (def == null) return null;
            var owner = offering.health.hediffSet.GetFirstHediffOfDef(def)?.TryGetComp<HediffComp_SoulBondHost>()?.BonderPawn;
            if (owner == null || owner == offering || owner.Destroyed || owner.Dead ||
                owner.GetCompAbilityUserMagic()?.soulBondPawn != offering) return null;
            return (mental ? ClassIdentity.IsWarlock(owner) : ClassIdentity.IsSuccubus(owner)) ? owner : null;
        }

        internal static Pawn BodyOf(Pawn owner)
        {
            if (owner == null || owner.Dead || owner.Destroyed) return null;
            if (!SpiritRebirthState.TryGet(owner,out var state)) return owner;
            return state.HasValidHost(owner) ? state.Host : null;
        }

        internal static bool ReadyOnMap(Pawn body,Pawn owner,Pawn offering)
            => body != null && body != offering && body == BodyOf(owner) && body.Spawned && !body.Dead &&
                !body.Downed && !body.InMentalState && offering?.Spawned == true && offering.Map == body.Map && !offering.Dead;

        internal static bool TryChannel(Pawn body, Pawn offering, out Pawn succubus, out Pawn warlock)
        {
            succubus = warlock = null;
            var job = body?.CurJob;
            if (job == null || job.def != TorannMagicDefOf.JobDriver_SummonDemon ||
                offering == null || job.targetA.Pawn != offering) return false;

            var owner = SpiritRebirthState.TryGetPossessingSpirit(body,out var soul) ? soul : body;
            if (!TryPair(owner,out var actual,out succubus,out warlock) || actual != offering ||
                owner != succubus || !ReadyOnMap(body,owner,offering)) return false;

            // Older saves recorded only the offering. The field can tick before the channel job.
            if (!job.targetB.IsValid && !job.targetC.IsValid)
            {
                job.targetB = succubus;
                job.targetC = warlock;
            }
            return job.targetB.Pawn == succubus && job.targetC.Pawn == warlock;
        }

        internal static bool ChannelMatches(Pawn body,Pawn succubus,Pawn warlock,Pawn offering)
            => TryChannel(body,offering,out var physical,out var mental) && physical == succubus && mental == warlock;

        internal static bool CanStart(Pawn body,Pawn owner,out Pawn offering,out Pawn succubus,out Pawn warlock)
        {
            if (!TryPair(owner,out offering,out succubus,out warlock) || !ReadyOnMap(body,owner,offering) ||
                Projectile_SummonDemon.Claims(body.Map,offering)) return false;
            return owner == succubus ? body.CurJobDef != TorannMagicDefOf.JobDriver_SummonDemon :
                ChannelMatches(BodyOf(succubus),succubus,warlock,offering);
        }

        internal static void BondChanged(CompAbilityUserMagic owner, bool mental, Pawn previous, Pawn current)
        {
            RefreshControl(owner);
            // Both founders gain or lose the command when either one changes their bond.
            if (previous?.health?.hediffSet != null)
                RefreshControl(Bonder(previous,!mental)?.GetCompAbilityUserMagic());
            if (current != previous && current?.health?.hediffSet != null)
                RefreshControl(Bonder(current,!mental)?.GetCompAbilityUserMagic());
        }

        internal static void RefreshControl(CompAbilityUserMagic magic)
        {
            if (magic?.Pawn == null || TorannMagicDefOf.TM_SummonDemon == null) return;
            if (TryPair(magic.Pawn,out _,out _,out _))
            {
                magic.spell_SummonDemon = true;
                if (magic.AbilityData.Powers.Count(p=>p?.Def == TorannMagicDefOf.TM_SummonDemon) != 1)
                    TM_ClassState.AddRuntimeAbility(magic,TorannMagicDefOf.TM_SummonDemon);
            }
            else if (magic.spell_SummonDemon)
            {
                magic.spell_SummonDemon = false;
                magic.RemovePawnAbility(TorannMagicDefOf.TM_SummonDemon);
            }
        }
    }
}
