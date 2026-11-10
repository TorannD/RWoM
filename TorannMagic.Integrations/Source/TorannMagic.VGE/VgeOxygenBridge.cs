using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Verse;

namespace TorannMagic.Odyssey
{
    // The conditional VGE XML adds a native connector. No optional assembly is referenced by RWoM.
    public static class VgeOxygenBridge
    {
        const double OxygenPerMana = 10d;
        static bool PositiveFinite(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        static ThingComp Connector(ThingWithComps parent) => parent?.AllComps?.FirstOrDefault(c => c.GetType().FullName == "PipeSystem.CompResource"
            && (c.props?.GetType().GetField("pipeNet")?.GetValue(c.props) as Def)?.defName == "VGE_OxygenNet");
        public static bool Available(ThingWithComps parent) => Connector(parent) != null;

        public static void Supply(CompMagicalOxygen pump, ref double pending)
        {
            var connector = Connector(pump.parent);
            if (connector == null) return;
            try
            {
                var netProperty = connector.GetType().GetProperty("PipeNet");
                var net = netProperty?.GetValue(connector);
                if (net == null || (net.GetType().GetField("def")?.GetValue(net) as Def)?.defName != "VGE_OxygenNet") return;
                var storages = net.GetType().GetField("storages")?.GetValue(net) as IEnumerable;
                if (storages == null) return;
                var sourceMap = pump.parent.Map;
                var sourcePosition = pump.parent.Position;
                var sourceRotation = pump.parent.Rotation;
                var sourceFaction = pump.parent.Faction;
                bool Current() => pump.CanOperate && pump.OutputMode == OxygenOutputMode.PipeNetwork && pump.parent.Map == sourceMap
                    && pump.parent.Position == sourcePosition && pump.parent.Rotation == sourceRotation && pump.parent.Faction == sourceFaction
                    && ReferenceEquals(netProperty.GetValue(connector), net);
                var targets = storages.Cast<object>().OfType<ThingComp>().Where(c => c.GetType().FullName == "PipeSystem.CompResourceStorage").ToArray();
                bool Connected(ThingComp storage) => storage.parent != null && storage.parent.Spawned && !storage.parent.Destroyed && storage.parent.Map == sourceMap
                    && storage.parent.Faction == sourceFaction && ReferenceEquals(storage.GetType().GetProperty("PipeNet")?.GetValue(storage), net)
                    && !(bool)(storage.GetType().GetField("markedForTransfer")?.GetValue(storage) ?? true);
                double demand = 0d;
                foreach (var storage in targets)
                {
                    if (!Connected(storage)) continue;
                    var available = storage.GetType().GetProperty("AmountCanAccept")?.GetValue(storage);
                    if (available is float capacity && PositiveFinite(capacity)) demand += capacity;
                }
                if (!Current()) return;
                if (demand > pending)
                    pending += pump.SupplyPulse((float)Math.Min(float.MaxValue, (demand - pending) / OxygenPerMana)) * OxygenPerMana;
                foreach (var storage in targets)
                {
                    if (!Current()) return;
                    var type = storage.GetType();
                    var stored = type.GetProperty("AmountStored");
                    var free = type.GetProperty("AmountCanAccept");
                    var add = type.GetMethod("AddResource", new[] { typeof(float) });
                    if (stored == null || free == null || add == null) continue;
                    if (!Connected(storage)) continue;
                    float capacity = (float)free.GetValue(storage);
                    if (!PositiveFinite(capacity)) continue;
                    // Native fuel payment can invoke callbacks. Retain pending output if the destination changes.
                    if (!Current() || !Connected(storage)) return;
                    capacity = (float)free.GetValue(storage);
                    float before = (float)stored.GetValue(storage);
                    if (!PositiveFinite(capacity) || !PositiveFinite((float)pending) || before < 0 || float.IsNaN(before) || float.IsInfinity(before)) continue;
                    float amount = (float)Math.Min(pending, capacity);
                    // Float storage must never round up past the paid credit.
                    float after = before + amount;
                    if ((double)after - before > pending)
                    {
                        after = BitConverter.ToSingle(BitConverter.GetBytes(BitConverter.ToInt32(BitConverter.GetBytes(after), 0) - 1), 0);
                        amount = after - before;
                    }
                    if (amount <= 0 || before + amount <= before) continue;
                    try { add.Invoke(storage, new object[] { amount }); }
                    finally
                    {
                        // AddResource sets the amount before notifying the designation manager.
                        // Even if that notification throws, do not replay credited oxygen.
                        double credited = (double)(float)stored.GetValue(storage) - before;
                        if (credited > 0) pending = Math.Max(0d, pending - credited);
                    }
                }
            }
            catch (Exception e)
            {
                Compatibility.TM_Log.Error("VGE oxygen transfer failed; paid oxygen retained where possible: " + (e.InnerException ?? e));
            }
        }
    }
}
