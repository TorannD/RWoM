using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_DispelEnchantWeapon : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            bool flag = false;
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();

            if (comp.IsMagicUser)
            {
                if (comp.weaponEnchants.Count > 0)
                {
                    for(int i =0; i < comp.weaponEnchants.Count; i++)
                    {
                        Pawn dispellingPawn = comp.weaponEnchants[i];
                        RemoveExistingEnchantment(dispellingPawn);
                        i--;
                    }
                    comp.weaponEnchants.Clear();
                    comp.RemovePawnAbility(TorannMagicDefOf.TM_DispelEnchantWeapon);
                }
            }

            this.PostCastShot(flag, out flag);
            return flag;
        }

        public static void RemoveExistingEnchantment(Pawn pawn)
        {
            Hediff hediff = null;
            List<Hediff> allHediffs = new List<Hediff>();
            allHediffs.Clear();
            allHediffs = pawn.health.hediffSet.GetHediffs<Hediff>().ToList();
            if (allHediffs != null && allHediffs.Count > 0)
            {
                for (int i = 0; i < allHediffs.Count; i++)
                {
                    hediff = allHediffs[i];
                    if (hediff.def.defName.Contains("TM_WeaponEnchantment"))
                    {
                        pawn.health.RemoveHediff(hediff);
                    }
                }
            }
        }
    }
}
