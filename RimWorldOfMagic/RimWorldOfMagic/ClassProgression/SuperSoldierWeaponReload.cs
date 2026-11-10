using System;
using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SuperSoldierWeaponReload
    {
        internal static bool Rebuild(CompAbilityUserMight comp)
        {
            var equipment=comp?.Pawn?.equipment;
            var container=comp?.equipmentContainer;
            if(equipment==null || container==null || container.Count==0) return false;
            if(SuperSoldierWeaponProfile.UsesOriginalEquipment(comp.Pawn)) return RestoreOriginal(comp);
            if(comp.specWpnRegNum!=-1) return false;
            var previous=equipment.Primary;
            // Never replace ordinary equipment, or guess among unrelated stored items.
            if(previous!=null && previous.def?.defName?.Contains("Spec_Base")!=true) return false;
            // A failed cycle leaves the binding's copy reference unresolved while
            // the pawn still wears an orphan proxy; hand the stashed original back
            // instead of stacking a fresh copy on a broken slot.
            if(previous!=null && comp.specializedWeapon.CopyLost) return RestoreOriginal(comp);
            var original=comp.specializedWeapon.ForReload(previous,container);
            if(original==null || original.Destroyed) return false;
            // A proxy whose holder disagrees with the tracker survives every
            // Contains-keyed removal, so the slot must be reclaimed before the
            // factory or its AddEquipment call leaks the fresh copy.
            if(previous!=null && !Detach(previous,equipment,container)) return false;
            Action<Pawn,ThingWithComps> factory=null;
            foreach(var power in comp.MightData.MightPowersSS.Where(p=>p.learned))
            {
                switch(power.abilityDef?.defName)
                {
                    case "TM_PistolSpec": factory=TM_Action.DoAction_PistolSpecCopy;break;
                    case "TM_RifleSpec": factory=TM_Action.DoAction_RifleSpecCopy;break;
                    case "TM_ShotgunSpec": factory=TM_Action.DoAction_ShotgunSpecCopy;break;
                }
                if(factory!=null) break;
            }
            if(factory==null) return false;
            int registry=comp.specWpnRegNum;
            int originalCondition=original.HitPoints;
            bool committed=false;
            try
            {
                if(previous!=null) original.HitPoints=previous.HitPoints;
                factory(comp.Pawn,original);
                var replacement=equipment.Primary;
                if(replacement==null || replacement==previous || comp.specializedWeapon.Find(replacement,container)!=original)
                    throw new InvalidOperationException("Specialized weapon rebuild did not attach the expected replacement.");
                committed=true;
                previous?.Destroy(DestroyMode.Vanish);
                return true;
            }
            finally
            {
                if(!committed)
                {
                    comp.specWpnRegNum=registry;
                    original.HitPoints=originalCondition;
                    if(previous!=null && !previous.Destroyed && previous.holdingOwner==null)
                    {
                        if(equipment.Primary==null) equipment.AddEquipment(previous);
                        else container.TryAdd(previous); // Preserve it if an addon occupied the slot during failure.
                    }
                    if(previous!=null) comp.specializedWeapon.Bind(original,previous);
                }
            }
        }

        // 1.7 ThingOwner.Remove/Contains key on holdingOwner, so a proxy that
        // an earlier failure left with a stale holder stays equipped through
        // every removal path while the Primary getter keeps serving it from
        // the tracker list. Reclaim the physical membership from both the
        // tracker and the stash container, then detach wherever it now lives.
        private static bool Detach(ThingWithComps proxy,Pawn_EquipmentTracker equipment,ThingOwner<ThingWithComps> container)
        {
            var held=equipment.GetDirectlyHeldThings();
            if(held!=null) foreach(Thing item in held) if(item==proxy)
            {
                proxy.holdingOwner=held;
                held.Remove(proxy);
                break;
            }
            if(container!=null) foreach(Thing item in container) if(item==proxy)
            {
                proxy.holdingOwner=container;
                container.Remove(proxy);
                break;
            }
            if(proxy.holdingOwner!=null)
            {
                proxy.holdingOwner.Remove(proxy);
            }
            return equipment.Primary!=proxy;
        }

        private static bool RestoreOriginal(CompAbilityUserMight comp)
        {
            var equipment=comp.Pawn.equipment;var container=comp.equipmentContainer;
            var previous=equipment.Primary;
            if(previous!=null && previous.def?.defName?.Contains("Spec_Base")!=true) return false;
            var original=comp.specializedWeapon.ForReload(previous,container);
            if(original==null || original.Destroyed) return false;
            int condition=original.HitPoints;bool committed=false;
            if(previous!=null && !Detach(previous,equipment,container)) return false;
            try
            {
                if(previous!=null) original.HitPoints=previous.HitPoints;
                if(container.TryTransferToContainer(original,equipment.GetDirectlyHeldThings(),1)!=1 || equipment.Primary!=original) return false;
                committed=true;
                comp.specWpnRegNum=-1;
                comp.specializedWeapon.Released(original);
                // Old copies could alias original components with an original parent.
                // Destroying the discarded proxy must not notify those original components.
                if(previous!=null)
                {
                    previous.AllComps.RemoveAll(c=>c.parent!=previous);
                    previous.Destroy(DestroyMode.Vanish);
                }
                return true;
            }
            finally
            {
                if(!committed)
                {
                    original.HitPoints=condition;
                    if(equipment.GetDirectlyHeldThings().Contains(original))
                        equipment.GetDirectlyHeldThings().TryTransferToContainer(original,container,1);
                    else if(original.holdingOwner==null) container.TryAdd(original);
                    if(previous!=null && !previous.Destroyed && previous.holdingOwner==null)
                    {
                        if(equipment.Primary==null) equipment.GetDirectlyHeldThings().TryAdd(previous);
                        else container.TryAdd(previous);
                    }
                }
            }
        }
    }
}
