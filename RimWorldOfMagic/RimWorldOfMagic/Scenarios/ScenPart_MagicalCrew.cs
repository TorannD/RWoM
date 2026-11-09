using System.Collections.Generic;
using System.Linq;
using RimWorld;
using TorannMagic.Compatibility;
using TorannMagic.Utils;
using Verse;

namespace TorannMagic
{
    // Only this scenario opts into class assignment, after the player confirms the
    // selected crew. Pawn-generation previews and discarded candidates stay intact.
    public class ScenPart_MagicalCrew : ScenPart
    {
        private bool crewAssigned;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref crewAssigned, "crewAssigned");
        }

        private static void RemoveStartingPotential(Pawn pawn)
        {
            // Potentials are prerequisites for a career, not a second career.
            // Use native removal so trait caches refresh; preserve real classes and other gifts.
            foreach (var trait in pawn.story.traits.allTraits.Where(t =>
                t.def == TorannMagicDefOf.TM_Gifted || t.def == TorannMagicDefOf.PhysicalProdigy).ToList())
                pawn.story.traits.RemoveTrait(trait);
        }
        public override void PostGameStart()
        {
            var init = Find.GameInitData;
            if (crewAssigned || init?.startingAndOptionalPawns == null) return;
            // Receipt belongs to the scenario instance, not the current traits: a
            // later class removal must never make a loaded start eligible again.
            crewAssigned = true;
            var crew = init.startingAndOptionalPawns.Take(init.startingPawnCount).Where(p => p?.story?.traits != null).Distinct().ToList();
            // Even with the beta switched off the stored identity belongs to this
            // pawn. In particular a novice may still own both unspent potentials.
            var legacyCrew = crew.Where(p => p.AllComps?.OfType<ClassProgression.CompClassProgression>()
                .Any(c => c.State != null) != true).ToList();
            foreach (var pawn in legacyCrew.Where(p => TM_ClassState.HasCurrentClass(p, true))) RemoveStartingPotential(pawn);
            var candidates = legacyCrew.Where(p => !TM_ClassState.HasCurrentClass(p, true)).ToList();
            if (candidates.Count == 0) return;

            // Builtin power owners are the authoritative base-mod roster. Lich is
            // a necromancer transformation, never a starting career. Custom and
            // advanced registries deliberately do not enlarge this random pool.
            var reserved = new HashSet<TraitDef>(crew.SelectMany(p => p.story.traits.allTraits.Select(t => t.def)));
            foreach (var pawn in crew.Except(legacyCrew))
                foreach (var path in pawn.AllComps.OfType<ClassProgression.CompClassProgression>().First(c => c.State != null).State.Disciplines)
                {
                    var trait = ClassProgression.ClassIdentity.Profile(path.ClassId)?.legacyTrait;
                    if (trait != null) reserved.Add(trait);
                }
            var available = TM_ClassGroups.MagicEntriesField.Where(e => e.trait != "Lich")
                .Select(e => DefDatabase<TraitDef>.GetNamedSilentFail(e.trait)).Where(t => t != null)
                .Where(t => !reserved.Contains(t)).Distinct().ToList();
            foreach (var pawn in candidates)
            {
                if (available.Count == 0) break;
                int index = Rand.Range(0, available.Count);
                var trait = available[index];
                available.RemoveAt(index);
                if (ClassProgression.ClassInitiation.TryGrantStartingFoundation(pawn, trait)) continue;
                // Vanilla's default does not suppress or remove conflicting
                // traits. In particular an existing might career stays owned.
                pawn.story.traits.GainTrait(new Trait(trait));
                if (TM_ClassState.HasCurrentClass(pawn, true)) RemoveStartingPotential(pawn);
                TM_PawnTracker.EnsureClassComps(pawn);
                var magic = pawn.GetCompAbilityUserMagic();
                TM_PawnTracker.ResolveMagicComp(magic);
                if (magic == null) continue;
                magic.RecoverMissingProgression();
                TM_ClassState.GrantCurrentMagicStarts(magic);
                magic.magicPowersInitialized = true;
                TM_LegacyRecovery.RestoreNeed(pawn, TorannMagicDefOf.TM_MagicUserHD, TorannMagicDefOf.TM_Mana);
            }
        }
    }
}

