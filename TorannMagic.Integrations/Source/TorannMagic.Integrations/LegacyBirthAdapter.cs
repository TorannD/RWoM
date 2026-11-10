using System.Collections.Generic;
using RimWorld;
using Verse;
namespace TorannMagic.Integrations
{
    internal static class LegacyBirthAdapter
    {
        public static void Apply(Pawn pawn)
        {
            if (pawn?.story?.traits != null)
            {
                bool hasMagicTrait = false;
                bool hasFighterTrait = false;
                List<Trait> pawnTraits = pawn.story.traits.allTraits;
                for (int i = 0; i < pawnTraits.Count; i++)
                {
                    bool removeTrait = false;
                    if (pawnTraits[i].def == TorannMagicDefOf.Arcanist || pawnTraits[i].def == TorannMagicDefOf.Geomancer || pawnTraits[i].def == TorannMagicDefOf.Warlock || pawnTraits[i].def == TorannMagicDefOf.Succubus ||
                        pawnTraits[i].def == TorannMagicDefOf.InnerFire || pawnTraits[i].def == TorannMagicDefOf.HeartOfFrost || pawnTraits[i].def == TorannMagicDefOf.StormBorn || pawnTraits[i].def == TorannMagicDefOf.Technomancer ||
                        pawnTraits[i].def == TorannMagicDefOf.Paladin || pawnTraits[i].def == TorannMagicDefOf.Summoner || pawnTraits[i].def == TorannMagicDefOf.Druid || pawnTraits[i].def == TorannMagicDefOf.Necromancer ||
                        pawnTraits[i].def == TorannMagicDefOf.Lich || pawnTraits[i].def == TorannMagicDefOf.Priest || pawnTraits[i].def == TorannMagicDefOf.TM_Bard || pawnTraits[i].def == TorannMagicDefOf.TM_Gifted ||
                        pawnTraits[i].def == TorannMagicDefOf.Technomancer || pawnTraits[i].def == TorannMagicDefOf.BloodMage || pawnTraits[i].def == TorannMagicDefOf.Enchanter || pawnTraits[i].def == TorannMagicDefOf.Chronomancer ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_Wanderer || pawnTraits[i].def == TorannMagicDefOf.ChaosMage)
                    {
                        removeTrait = true;
                        hasMagicTrait = true;
                    }
                    if (pawnTraits[i].def == TorannMagicDefOf.Gladiator || pawnTraits[i].def == TorannMagicDefOf.Bladedancer || pawnTraits[i].def == TorannMagicDefOf.TM_Sniper || pawnTraits[i].def == TorannMagicDefOf.Ranger ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_Psionic || pawnTraits[i].def == TorannMagicDefOf.Faceless || pawnTraits[i].def == TorannMagicDefOf.DeathKnight || pawnTraits[i].def == TorannMagicDefOf.PhysicalProdigy ||
                        pawnTraits[i].def == TorannMagicDefOf.TM_Monk || pawnTraits[i].def == TorannMagicDefOf.TM_Wayfarer || pawnTraits[i].def == TorannMagicDefOf.TM_Commander || pawnTraits[i].def == TorannMagicDefOf.TM_SuperSoldier)
                    {
                        removeTrait = true;
                        hasFighterTrait = true;
                    }
                    for (int j = 0; j < TM_ClassUtility.CustomClasses.Count; j++)
                    {
                        if (TM_ClassUtility.CustomClasses[j].classTrait == pawnTraits[i].def)
                        {
                            removeTrait = true;
                            if (TM_ClassUtility.CustomClasses[j].isFighter)
                            {
                                hasFighterTrait = true;
                            }
                            if (TM_ClassUtility.CustomClasses[j].isMage)
                            {
                                hasMagicTrait = true;
                            }
                        }
                    }
                    if (removeTrait) { pawnTraits.RemoveAt(i); i--; }
                }

                if (hasFighterTrait && hasMagicTrait)
                {
                    if (Rand.Chance(.5f))
                    {
                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Gifted, 0, false));
                    }
                    else
                    {
                        pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.PhysicalProdigy, 0, false));
                    }
                }
                else if (hasFighterTrait)
                {
                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.PhysicalProdigy, 0, false));
                }
                else if (hasMagicTrait)
                {
                    pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Gifted, 0, false));
                }
            }
        }

    }
}
