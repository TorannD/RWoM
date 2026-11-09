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

        internal static Hediff ActiveOnHit(Pawn wielder, HediffDef def, ThingDef usedWeapon)
        {
            var weapon = wielder?.equipment?.Primary;
            if (weapon == null || weapon.def != usedWeapon) return null;
            var effect = wielder.health.hediffSet.GetFirstHediffOfDef(def);
            return effect?.TryGetComp<HediffComp_EnchantedWeapon>()?.enchantedWeapon == weapon ? effect : null;
        }

        private void Initialize()
        {
            if (!Pawn.Spawned || enchanterPawn == null) removeNow = true;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn?.Map == null) return;
            if (!initialized)
            {
                initialized = true;
                Initialize();
            }
            if (Find.TickManager.TicksGame % eventFrequency != 0) return;
            if (enchanterPawn.DestroyedOrNull() || enchanterPawn.Dead ||
                enchanterPawn.GetCompAbilityUserMagic()?.weaponEnchants?.Contains(Pawn) != true ||
                enchantedWeapon.DestroyedOrNull() || Pawn.equipment?.Primary != enchantedWeapon)
                removeNow = true;
        }

        public override void CompPostPostRemoved()
        {
            if (enchanterPawn != null)
                enchanterPawn.GetCompAbilityUserMagic()?.weaponEnchants?.RemoveAll(p => p == Pawn);
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
