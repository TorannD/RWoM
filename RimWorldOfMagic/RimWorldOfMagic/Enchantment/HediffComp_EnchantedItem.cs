using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace TorannMagic.Enchantment
{
    public class HediffComp_EnchantedItem : HediffComp
    {
        public bool initialized = false;
        public bool removeNow = false;
        public Apparel enchantedItem;
        public Thing enchantedWeapon;

        public int checkActiveRate = 60;
        public int hediffActionRate = 1;

        public override void CompExposeData()
        {            
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.checkActiveRate, "checkActiveRate", 60, false);
            Scribe_Values.Look<int>(ref this.hediffActionRate, "hediffActionRate", 1, false);
            base.CompExposeData();
        }

        public string labelCap
        {
            get
            {
                if (base.Def.LabelCap != null)
                {
                    return base.Def.LabelCap;
                }
                else
                {
                    return "";
                }
            }
        }

        public string label
        {
            get
            {
                if (base.Def.label != null)
                {
                    return base.Def.label;
                }
                else
                {
                    return "";
                }
            }
        }


        public override string CompLabelInBracketsExtra
        {
            get
            {
                if (this.enchantedItem != null && this.enchantedItem.def != null && this.enchantedItem.def.label != null)
                {
                    return this.enchantedItem.def.label;
                }
                else if(this.enchantedWeapon != null && this.enchantedWeapon.def != null && this.enchantedWeapon.def.label != null)
                {
                    return this.enchantedWeapon.def.label;
                }
                else
                {
                    return "";
                }
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned)
            {
                PostInitialize();
            }
        }

        public virtual void PostInitialize()
        {
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }
            }
            if(Find.TickManager.TicksGame % this.checkActiveRate == 0)
            {
                if(CheckActiveApparel() && CheckActiveEquipment())
                {
                    this.removeNow = true;
                }                
            }
            if(this.hediffActionRate != 0 && Find.TickManager.TicksGame % this.hediffActionRate == 0)
            {
                HediffActionTick();
            }
        }
        
        public bool CheckActiveApparel()
        {
            bool remove = true;
            List<Apparel> apparel = this.Pawn.apparel.WornApparel;
            if (apparel != null)
            {
                if(apparel.Contains(this.enchantedItem))
                {
                    remove = false;
                }
            }
            return remove;
        }

        public bool CheckActiveEquipment()
        {
            bool remove = true;
            Thing primary = this.Pawn.equipment.Primary;
            if (primary != null && primary == enchantedWeapon)
            {                
                remove = false;                
            }
            return remove;
        }

        public override bool CompShouldRemove => base.CompShouldRemove || this.removeNow;

        public virtual void HediffActionTick()
        {
        }
    }
}
