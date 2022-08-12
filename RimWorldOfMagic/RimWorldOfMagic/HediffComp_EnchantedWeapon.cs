using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_EnchantedWeapon : HediffComp
    {

        private bool initialized = false;
        private bool removeNow = false;

        private int eventFrequency = 60;

        public Pawn enchanterPawn = null;
        public Thing enchantedWeapon = null;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Pawn>(ref enchanterPawn, "enchanterPawn", false);
            Scribe_References.Look<Thing>(ref enchantedWeapon, "enchantedWeapon", false);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
        }

        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            CompAbilityUserMagic comp = this.enchanterPawn.GetCompAbilityUserMagic();
            if (!spawned || this.enchanterPawn == null)
            {
                this.removeNow = true;
            }
        }        

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null && base.Pawn.Map != null;
            if (flag)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }

                if (Find.TickManager.TicksGame % this.eventFrequency == 0)
                {
                    if(!this.enchanterPawn.DestroyedOrNull() && !this.enchanterPawn.Dead)
                    {
                        CompAbilityUserMagic comp = this.enchanterPawn.GetCompAbilityUserMagic();
                        if(comp != null && comp.weaponEnchants != null && comp.weaponEnchants.Count >0)
                        {
                            bool isRegistered = false;
                            for(int i =0; i < comp.weaponEnchants.Count; i++)
                            {
                                if(comp.weaponEnchants[i] == this.Pawn)
                                {
                                    isRegistered = true;
                                }
                            }
                            if(!isRegistered)
                            {
                                this.removeNow = true;
                            }
                        }
                    }
                    else
                    {
                        this.removeNow = true;
                    }
                    
                    if(!this.enchantedWeapon.DestroyedOrNull() && this.Pawn.equipment.Primary != null && this.Pawn.equipment.Primary == this.enchantedWeapon)
                    {

                    }
                    else
                    {
                        this.removeNow = true;
                    }
                }
            }
        }

        public override void CompPostPostRemoved()
        {
            if(this.enchanterPawn != null)
            {
                CompAbilityUserMagic comp = enchanterPawn.GetCompAbilityUserMagic();
                if(comp != null && comp.weaponEnchants != null && comp.weaponEnchants.Count > 0)
                {
                    if(comp.weaponEnchants.Contains(this.Pawn))
                    {
                        comp.weaponEnchants.Remove(this.Pawn);
                    }
                }
            }
            base.CompPostPostRemoved();
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.removeNow;
            }
        }        
    }
}
