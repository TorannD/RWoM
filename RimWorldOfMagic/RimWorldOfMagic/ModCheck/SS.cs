using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using SimpleSidearms;
using SimpleSidearms.rimworld;

namespace TorannMagic.ModCheck
{
    public static class SS
    {
        public static void ClearWeaponMemory(Pawn p)
        {
            CompSidearmMemory csm = p.TryGetComp<CompSidearmMemory>();
            if (csm != null && csm.rememberedWeapons != null && csm.RememberedWeapons.Count > 0 && p.equipment != null)
            {
                while(csm.RememberedWeapons.Count > 0)
                {
                    csm.ForgetSidearmMemory(csm.RememberedWeapons[0]);
                }
                csm.generateRememberedWeaponsFromEquipped();
            }
        }
    }
}
