using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    // The native melee scope already observes each packet after armor and shields.
    // Keep eligibility from before damage so a killing counter still qualifies.
    internal static class BladeRiposteTrial
    {
        internal static ThingDef Weapon(Verb_MeleeAttackDamage verb, Pawn attacker, Pawn victim)
        {
            if (!ClassProgressionFeature.Enabled || attacker?.Spawned != true || victim?.Spawned != true ||
                attacker.Map != victim.Map || attacker.Dead || attacker.Downed || victim.Dead || victim.Downed ||
                attacker.IsPrisoner || victim.IsPrisoner || verb.GetType() != typeof(Verb_MeleeAttackDamage)) return null;
            var weapon = attacker.equipment?.Primary;
            var offense = BladeCareer.Path(BladeCareer.TrialOwner(attacker));
            bool openRoute = offense?.Stage == InitiationStage.Master && offense.BladeRouteOpponent > 0 &&
                Find.TickManager.TicksGame < offense.BladeRouteUntil;
            if (weapon == null || (!openRoute && !BladeMastery.Training(offense) && !BladeMastery.Training(BladeCareer.Path(BladeCareer.TrialOwner(victim)))) ||
                !weapon.def.IsMeleeWeapon || verb.EquipmentSource != weapon || !victim.HostileTo(attacker) ||
                !attacker.Position.AdjacentTo8WayOrInside(victim.Position)) return null;
            return weapon.def;
        }

        internal static void Struck(Pawn attacker, Pawn victim)
        {
            BladeAscension.FinishRoute(attacker, victim);
            int now = Find.TickManager.TicksGame;
            var defender = BladeCareer.Path(BladeCareer.TrialOwner(victim));
            if (BladeMastery.Training(defender) && !victim.Dead && !victim.Downed)
            {
                defender.BladeChallengerId = attacker.thingIDNumber;
                defender.BladeReplyBody=victim.thingIDNumber;
                defender.BladeReplyUntil = now + 240;
            }
            var owner=BladeCareer.TrialOwner(attacker);
            var reply = BladeCareer.Path(owner);
            if (!BladeMastery.Training(reply) || reply.BladeChallengerId <= 0 ||
                reply.BladeChallengerId != victim.thingIDNumber || now >= reply.BladeReplyUntil ||
                attacker.thingIDNumber!=(reply.BladeReplyBody>0?reply.BladeReplyBody:owner.thingIDNumber)) return;
            reply.BladeChallengerId = 0;
            reply.BladeReplyUntil = 0;
            BladeMastery.Riposte(owner);
        }
    }
}
