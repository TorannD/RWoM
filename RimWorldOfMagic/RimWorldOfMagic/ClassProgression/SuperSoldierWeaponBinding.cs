using Verse;

namespace TorannMagic.ClassProgression
{
    internal sealed class SuperSoldierWeaponBinding : IExposable
    {
        private bool bound;
        private ThingWithComps original, copy;

        internal void Bind(ThingWithComps original, ThingWithComps copy)
        {
            this.original = original;
            this.copy = copy;
            bound = true;
        }

        internal ThingWithComps Find(ThingWithComps dropped, ThingOwner<ThingWithComps> container)
        {
            if (dropped == null || container == null) return null;
            if (bound) return dropped == copy && original != null && container.Contains(original) ? original : null;
            // Old saves have no identity record. Recover only a unique exact legacy label,
            // never a substring or an arbitrary first match among equally named items.
            if (dropped.def?.defName?.Contains("Spec_Base") != true) return null;
            ThingWithComps candidate = null;
            foreach (var item in container)
            {
                if (string.IsNullOrEmpty(item.def?.label) || dropped.def.label != item.def.label + "++") continue;
                if (candidate != null) return null;
                candidate = item;
            }
            return candidate;
        }

        // True once a failed cycle destroyed the copy its reference points at:
        // the reference no longer resolves on load and the binding cannot pair
        // the equipped orphan proxy with the stashed original by identity.
        internal bool CopyLost => bound && copy == null;

        internal void Released(ThingWithComps item)
        {
            if (item != original) return;
            original = null;
            copy = null;
            // Keep bound: a repeated callback must not fall back to a different legacy item.
        }

        internal ThingWithComps ForReload(ThingWithComps equipped, ThingOwner<ThingWithComps> container)
        {
            if(container==null) return null;
            if(bound)
            {
                if(equipped==null || equipped==copy) return original!=null && container.Contains(original) ? original : null;
                // The equipped orphan is not the bound copy because the copy was
                // destroyed by a failed cycle; a lone stashed original is still
                // an unambiguous restoration for the orphan proxy.
                if(copy==null && equipped.def?.defName?.Contains("Spec_Base")==true)
                    return container.Count==1 && container[0]==original ? original : null;
                return null;
            }
            if(equipped!=null) return Find(equipped,container);
            // Historical saves can lack the generated copy. Only one retained original is unambiguous.
            return container.Count==1 && container[0].def?.defName?.Contains("Spec_Base")!=true ? container[0] : null;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref bound, "bound");
            Scribe_References.Look(ref original, "original");
            Scribe_References.Look(ref copy, "copy");
        }
    }
}
