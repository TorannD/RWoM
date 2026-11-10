using AbilityUser;
using Verse;

namespace TorannMagic.ClassProgression
{
    public class Projectile_SpiritNativeAbility : Projectile_Ability
    {
        private Pawn source;
        private Pawn physicalCasterDuringImpact;
        internal void SetSource(Pawn owner) => source = owner;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref source, "TM_nativeSpellSource");
        }
        public override void Impact_Override(Thing hitThing)
        {
            // Jecs applies a missed/self-targeted blessing to its caster. The body is the recipient.
            if (source != null && !(hitThing is Pawn) && localApplyHediffs?.Count > 0)
            {
                if (physicalCasterDuringImpact?.Spawned == true && physicalCasterDuringImpact.Map == Map)
                    ApplyHediffsAndMentalStates(physicalCasterDuringImpact, source, localApplyMentalStates, localAbilityDef);
                return;
            }
            base.Impact_Override(hitThing);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            var body = launcher;
            physicalCasterDuringImpact = body as Pawn;
            if (source != null) launcher = source;
            try { base.Impact(hitThing, blockedByShield); }
            finally { launcher = body; physicalCasterDuringImpact = null; }
        }
    }
}
