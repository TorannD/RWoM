using System.Collections.Generic;
using Verse;

namespace TorannMagic.Odyssey
{
    public class MapComponent_GrandAtmosphereDome : MapComponent
    {
        private readonly List<CompGrandAtmosphereDome> sources = new List<CompGrandAtmosphereDome>();
        private CompGrandAtmosphereDome selected;
        private CompGrandAtmosphereDome paidSource;
        private long reservedUntil = -1;
        public MapComponent_GrandAtmosphereDome(Map map) : base(map) { }
        public void Register(CompGrandAtmosphereDome source)
        {
            if (!sources.Contains(source)) sources.Add(source);
            InvalidateSelection();
        }
        public void Unregister(CompGrandAtmosphereDome source)
        {
            sources.Remove(source);
            if (paidSource == source) paidSource = null;
            InvalidateSelection();
        }
        public void InvalidateSelection() { selected = null; }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref reservedUntil, "TM_grandDomeMapReceiptEnd", -1L);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) { selected = null; paidSource = null; }
        }
        internal bool ReservePayment(CompGrandAtmosphereDome source, int now, int delta)
        {
            if (now < reservedUntil) return false;
            reservedUntil = (long)now + delta;
            paidSource = source;
            return true;
        }
        private CompGrandAtmosphereDome Selected
        {
            get
            {
                // A ritual may activate a lower-ID source midway through native thing ticking.
                // Finish the already purchased interval before electing its successor.
                if (Find.TickManager.TicksGame < reservedUntil && paidSource != null && paidSource.parent.Map == map && paidSource.ValidateArmed()) return paidSource;
                if (selected != null && selected.parent.Map == map && selected.ValidateArmed()) return selected;
                selected = null;
                foreach (var source in sources)
                    if (source.parent.Map == map && source.ValidateArmed() && (selected == null || source.parent.thingIDNumber < selected.parent.thingIDNumber)) selected = source;
                return selected;
            }
        }
        public bool IsSelected(CompGrandAtmosphereDome source) => Selected == source;
        public void PrepareTick(int now) => Selected?.PrepareTick(now);
        public float EffectiveVacuum(IntVec3 cell, float nativeVacuum) => nativeVacuum > 0f && cell.InBounds(map) && Selected?.HasPaidCoverage == true ? 0f : nativeVacuum;
    }

}
