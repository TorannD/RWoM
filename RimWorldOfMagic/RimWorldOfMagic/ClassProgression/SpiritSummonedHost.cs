using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritSummonedHost
    {
        internal static bool IsSummoned(Pawn pawn)
            => pawn is TMPawnSummoned || pawn is Odyssey.Pawn_EtherServant ||
                pawn?.TryGetComp<CompSummoned>() is { } summon && (summon.Temporary || summon.sustained);

        internal static bool BlocksTarget(LocalTargetInfo target, Map map)
        {
            if (target.HasThing)
                return IsSummoned(target.Thing is Corpse corpse ? corpse.InnerPawn : target.Pawn);
            if (map == null || !target.IsValid || !target.Cell.InBounds(map)) return false;
            bool foundSummon = false;
            foreach (var thing in target.Cell.GetThingList(map))
            {
                var occupant = thing is Corpse remains ? remains.InnerPawn : thing as Pawn;
                if (occupant == null) continue;
                if (!IsSummoned(occupant)) return false;
                foundSummon = true;
            }
            return foundSummon;
        }

        // Old saves may already have a Spirit inside a temporary host.
        internal static bool CanDismiss(Pawn host)
        {
            if (!IsSummoned(host)) return true;
            var link = Compatibility.TM_SpiritCommands.Possession(host);
            var soul = link?.SpiritPawn;
            if (soul == null) return true;
            if (SpiritRebirthState.IsPaused(soul)) return host.Map != null && SpiritHostLoss.Secure(host,true);
            var map = host.Map;
            if (map == null) return false;
            if (SpiritRebirthEnergy.Marked(soul)) return SpiritHostLoss.Secure(host, true);
            TM_Action.RemovePossession(host, host.Position);
            return soul.Spawned && soul.Map == map && !link.GetDirectlyHeldThings().Contains(soul);
        }
    }
}
