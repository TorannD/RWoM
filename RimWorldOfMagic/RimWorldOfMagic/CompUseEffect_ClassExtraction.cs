using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TorannMagic.Enchantment;

namespace TorannMagic
{
    public class CompUseEffect_ClassExtraction : CompUseEffect
    {
        public override void DoEffect(Pawn user)
        {
            CompAbilityUserMagic compMagic = user.GetCompAbilityUserMagic();
            CompAbilityUserMight compMight = user.GetCompAbilityUserMight();
            if (TM_Calc.IsPossessedBySpirit(user))
            {
                TM_Action.RemovePossession(user, parent.Position, false);
            }
            else
            {
                int essenceXP = 0;
                if (compMagic != null && compMagic.IsMagicUser && compMagic.MagicUserXP != 0 && compMagic.MagicData != null)
                {
                    essenceXP += Rand.Range(300, 500);
                    for (int i = 0; i < compMagic.MagicData.MagicPowersStandalone.Count; i++)
                    {
                        MagicPower mp = compMagic.MagicData.MagicPowersStandalone[i];
                        if (mp.learned)
                        {
                            essenceXP += Rand.Range(80, 120);
                        }
                    }
                    essenceXP += Mathf.RoundToInt(compMagic.MagicUserXP / Rand.Range(1.7f, 2.3f));
                    Thing mes = ThingMaker.MakeThing(TorannMagicDefOf.TM_MagicArtifact_MagicEssence, null);
                    CompEnchantedItem itemComp = mes.TryGetComp<CompEnchantedItem>();
                    if (itemComp != null && itemComp.HasEnchantment)
                    {
                        itemComp.magicEssence = essenceXP;
                    }
                    GenPlace.TryPlaceThing(mes, this.parent.Position, this.parent.Map, ThingPlaceMode.Near);
                    essenceXP = 0;
                }
                if (compMight != null && compMight.IsMightUser && compMight.MightUserXP != 0 && compMight.MightData != null)
                {
                    essenceXP += Rand.Range(300, 500);
                    for (int i = 0; i < compMight.MightData.MightPowersStandalone.Count; i++)
                    {
                        MightPower mp = compMight.MightData.MightPowersStandalone[i];
                        if (mp.learned)
                        {
                            essenceXP += Rand.Range(80, 120);
                        }
                    }
                    essenceXP += Mathf.RoundToInt(compMight.MightUserXP / Rand.Range(1.7f, 2.3f));
                    Thing mes = ThingMaker.MakeThing(TorannMagicDefOf.TM_MagicArtifact_MightEssence, null);
                    CompEnchantedItem itemComp = mes.TryGetComp<CompEnchantedItem>();
                    if (itemComp != null && itemComp.HasEnchantment)
                    {
                        itemComp.mightEssence = essenceXP;
                    }
                    GenPlace.TryPlaceThing(mes, this.parent.Position, this.parent.Map, ThingPlaceMode.Near);
                    essenceXP = 0;
                }
                ModOptions.TM_DebugTools.RemoveClass(user);
            }
            TM_Action.TransmutateEffects(user.Position, user);
            TM_Action.TransmutateEffects(parent.Position, user);

        }
    }
}
