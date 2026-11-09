using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TorannMagic.Odyssey
{
    public class CompProperties_MagicalCombat : CompProperties
    {
        public float hitPoints = 1000f, regenerationPerSecond = 25f, manaPerHitPoint = .01f, manaPerCellSecond = .0005f;
        public int regenerationDelayTicks = 600, rebootTicks = 1800;
        public CompProperties_MagicalCombat()
        {
            compClass = typeof(CompMagicalCombat);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef))
                yield return error;
            if (!CompMagofuelReservoir.PositiveFinite(hitPoints) || !CompMagofuelReservoir.PositiveFinite(regenerationPerSecond) || !CompMagofuelReservoir.PositiveFinite(manaPerHitPoint) || !CompMagofuelReservoir.PositiveFinite(manaPerCellSecond) || regenerationDelayTicks < 0 || rebootTicks < 0)
                yield return "Combat shield requires finite positive HP/payment settings and nonnegative delays.";
            if (!parentDef.HasComp(typeof(CompShieldManaBuffer)))
                yield return "Combat shield requires a shield mana buffer.";
        }
    }

    public class CompProperties_MagicalCombatBooster : CompProperties
    {
        public float hitPoints = 250f;
        public CompProperties_MagicalCombatBooster()
        {
            compClass = typeof(CompMagicalCombatBooster);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef))
                yield return error;
            if (!CompMagofuelReservoir.PositiveFinite(hitPoints))
                yield return "Combat booster requires finite positive HP.";
        }
    }

    // Charge and receipts belong to the physical building, never a transient network identity.
    public abstract class CompMagicalCombatSource : ThingComp
    {
        protected float currentHitPoints;
        protected int lastDamageTick = -1, lastRegenerationTick = -1;
        public float CurrentHitPoints => currentHitPoints;
        public abstract float MaxHitPoints { get; }
        public int LastDamageTick => lastDamageTick;
        public virtual bool CanOperate => parent != null && parent.Spawned && !parent.Destroyed && parent.Faction != null && parent.Faction.IsPlayer && FlickUtility.WantsToBeOn(parent) && !parent.IsBrokenDown() && CompMagofuelReservoir.PositiveFinite(MaxHitPoints);

        internal bool MayRegenerate(int now) => now >= 60 && (lastRegenerationTick < 0 || (long)now - lastRegenerationTick >= 60);
        internal void MarkRegeneration(int now)
        {
            lastRegenerationTick = now;
        }

        internal void Restore(float amount)
        {
            currentHitPoints = Math.Min(MaxHitPoints, Math.Max(0f, currentHitPoints) + amount);
        }

        internal void NotifyHit(int now)
        {
            lastDamageTick = now;
        }

        internal float Spend(float amount)
        {
            float paid = Math.Min(currentHitPoints, amount);
            currentHitPoints -= paid;
            parent.GetComp<CompCrystalGlow>()?.NotifyHit(paid, Find.TickManager.TicksGame);
            return paid;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentHitPoints, "TM_combatHitPoints", 0f);
            Scribe_Values.Look(ref lastDamageTick, "TM_combatLastDamageTick", -1);
            Scribe_Values.Look(ref lastRegenerationTick, "TM_combatLastRegenerationTick", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                currentHitPoints = float.IsNaN(currentHitPoints) || float.IsInfinity(currentHitPoints) ? 0f : Math.Max(0f, Math.Min(MaxHitPoints, currentHitPoints));
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComponent<MapComponent_MagicalCombat>().Register(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map.GetComponent<MapComponent_MagicalCombat>().Unregister(this);
            base.PostDeSpawn(map, mode);
        }

        public override string CompInspectStringExtra() => "TM_CombatShieldSourceHP".Translate(CurrentHitPoints.ToString("0.#"), MaxHitPoints.ToString("0.#"));
    }

    public class CompMagicalCombatBooster : CompMagicalCombatSource
    {
        public override float MaxHitPoints => ((CompProperties_MagicalCombatBooster)props).hitPoints;
    }

    public class CompMagicalCombat : CompMagicalCombatSource
    {
        private int lastServiceTick = -1, rebootUntilTick = -1;
        public CompProperties_MagicalCombat Props => (CompProperties_MagicalCombat)props;
        public override float MaxHitPoints => Props.hitPoints;
        public override bool CanOperate => base.CanOperate && CompMagofuelReservoir.PositiveFinite(Props.regenerationPerSecond) && CompMagofuelReservoir.PositiveFinite(Props.manaPerHitPoint) && CompMagofuelReservoir.PositiveFinite(Props.manaPerCellSecond) && Props.regenerationDelayTicks >= 0 && Props.rebootTicks >= 0;
        public int RebootUntilTick => rebootUntilTick;

        internal bool BeginService(int now)
        {
            if (now < 60 || (lastServiceTick >= 0 && (long)now - lastServiceTick < 60))
                return false;
            lastServiceTick = now;
            return true;
        }

        internal void Collapse(int now)
        {
            rebootUntilTick = (int)Math.Min(int.MaxValue, (long)now + Props.rebootTicks);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastServiceTick, "TM_combatLastServiceTick", -1);
            Scribe_Values.Look(ref rebootUntilTick, "TM_combatRebootUntilTick", -1);
        }

        public override string CompInspectStringExtra()
        {
            var field = parent.Map?.GetComponent<MapComponent_MagicalCombat>();
            if (CanOperate && field != null && field.IsStandby(this))
                return "TM_CombatShieldStatusStandby".Translate();
            string text = base.CompInspectStringExtra();
            if (field != null && CanOperate)
            {
                text += "\n" + "TM_CombatShieldNetworkHP".Translate(field.HitPointsFor(this).ToString("0.#"), field.MaxHitPointsFor(this).ToString("0.#"));
                text += "\n" + (field.IsPeaceMode(parent.Faction) ? "TM_CombatShieldPeace" : "TM_CombatShieldCombat").Translate();
            }
            if (Find.TickManager.TicksGame < rebootUntilTick)
                text += "\n" + "TM_CombatShieldReboot".Translate(((rebootUntilTick - Find.TickManager.TicksGame) / 60f).ToString("0.#"));
            return text;
        }
    }
}
