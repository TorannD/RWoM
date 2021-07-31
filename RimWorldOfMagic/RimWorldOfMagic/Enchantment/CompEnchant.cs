using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace TorannMagic.Enchantment
{
    public class CompEnchant : ThingComp //, IThingHolder
    {
        public ThingOwner<Thing> enchantingContainer;

        private bool initialize = true;

        public CompEnchant()
        {
            enchantingContainer = new ThingOwner<Thing>();
        }

        //public override void PostDeSpawn(Map map)
        //{
        //    base.PostDeSpawn(map);
        //    enchantingContainer?.TryDropAll(parent.Position, map, ThingPlaceMode.Near, null, null);
        //}

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            Pawn pawn = this.parent as Pawn;
            CompEnchant comp = pawn.TryGetComp<CompEnchant>();
            //try
            //{
            //    bool flag = comp.enchantingContainer.Count > 0;
            //}
            //catch(NullReferenceException ex)
            //{
            //    this.enchantingContainer = new ThingOwner<Thing>();
            //}
            if (initialize && comp.enchantingContainer == null)
            {
                this.enchantingContainer = new ThingOwner<Thing>();
                this.initialize = false;
            }

        }

        //public ThingOwner GetDirectlyHeldThings()
        //{
        //    return enchantingContainer;
        //}

        //public void GetChildHolders(List<IThingHolder> outChildren)
        //{
        //    if (enchantingContainer != null)
        //    {
        //        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        //    }
        //}

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look<ThingOwner<Thing>>(ref this.enchantingContainer, "enchantingContainer", new object[0]);
            Scribe_Values.Look<bool>(ref this.initialize, "initialize", true, false);
        }

        //public override void PostExposeData()
        //{
        //    base.PostExposeData();
        //    Scribe_Deep.Look(ref enchantingContainer, "enchantingContainer", new object[]
        //    {
        //        this
        //    });
        //}
    }


}

