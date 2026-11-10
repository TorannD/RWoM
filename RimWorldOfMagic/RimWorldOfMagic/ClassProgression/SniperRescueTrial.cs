using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SniperRescueTrial
    {
        // Called once for a completed rescue after the shared bed, injury and threat checks.
        internal static void Delivered(Pawn rescuer, Pawn rescued)
        {
            var map = rescuer.Map;
            if (!rescuer.Faction.IsPlayer || map.mapPawns == null) return;
            foreach (var sniper in map.mapPawns.SpawnedPawnsInFaction(rescuer.Faction))
            {
                if (sniper == rescuer || sniper == rescued || !sniper.Spawned || sniper.Map != map ||
                    sniper.Dead || sniper.Downed || !sniper.Drafted || SniperSurvey.Path(sniper)?.Stage != InitiationStage.Master ||
                    !sniper.Position.InHorDistOf(rescued.Position, 35f)) continue;
                var gun = sniper.equipment?.Primary?.TryGetComp<CompEquippable>()?.PrimaryVerb;
                if (gun is not Verb_LaunchProjectile ranged || ranged.Projectile == null ||
                    !sniper.Position.InHorDistOf(rescued.Position, gun.EffectiveRange) ||
                    !GenSight.LineOfSight(sniper.Position, rescued.Position, map)) continue;
                SniperAscension.Rescue(sniper, rescued);
            }
        }
    }
}
