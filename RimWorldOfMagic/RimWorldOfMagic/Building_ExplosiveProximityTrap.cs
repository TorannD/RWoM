using RimWorld;
using Verse;
using Verse.Sound;
using System.Collections.Generic;
using UnityEngine;

namespace TorannMagic
{
    public class Building_ExplosiveProximityTrap : Building_Trap
    {
        public List<Pawn> touchingPawns = new List<Pawn>();

        private const float KnowerSpringChance = 0.004f;
        private const ushort KnowerPathFindCost = 800;
        private const ushort KnowerPathWalkCost = 30;
        private const float AnimalSpringChanceFactor = 0.1f;

        private int trapSpringDelay = 30;
        private bool trapSprung;

        private Pawn trapPawn = new Pawn();

        public override void ExposeData()
        {
            base.ExposeData();
            //Scribe_References.Look<Pawn>(ref this.trapPawn, "trapPawn", false);
        }

        protected override void SpringSub(Pawn p)
        {
            for (int i = 0; i < AllComps.Count; i++)
            {
                if (AllComps[i] is CompExplosive comp)
                {
                    comp.StartWick();
                    return;
                }
            }
        }

        private void CheckPawn(IntVec3 position)
        {
            List<Thing> thingList = position.GetThingList(Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                Pawn pawn = thingList[i] as Pawn;
                if (
                    pawn == null
                    || pawn.Faction == Faction
                    || !pawn.HostileTo(Faction)
                    || touchingPawns.Contains(pawn)) continue;

                touchingPawns.Add(pawn);
                CheckSpring(pawn);
            }
        }

        public bool Armed => !trapSprung;

        public override void Tick()
        {
            if (Armed)
            {
                CheckPawn(Position);
                for (int j = 0; j < 8; j++)
                {
                    IntVec3 intVec = Position + GenAdj.AdjacentCells[j];
                    CheckPawn(intVec);
                }
                for (int j = 0; j < touchingPawns.Count; j++)
                {
                    Pawn pawn2 = touchingPawns[j];
                    if (!pawn2.Spawned || pawn2.Position != Position)
                    {
                        touchingPawns.Remove(pawn2);
                    }
                }
            }
            else
            {
                trapSpringDelay--;
                if (trapSpringDelay <= 0)
                {
                    SpringSub(trapPawn);
                }
            }

            base.Tick();
        }

        // Allow override for disabling letter for Building_PoisonTrap
        protected virtual void CheckSpring(Pawn p)
        {
            if (!(Rand.Value < SpringChance(p))) return;
            Spring(p);
            if (p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer)
            {
                Find.LetterStack.ReceiveLetter("LetterFriendlyTrapSprungLabel".Translate(
                    p.LabelShort
                ), "LetterFriendlyTrapSprung".Translate(
                    p.LabelShort
                ), LetterDefOf.NegativeEvent, new TargetInfo(Position, Map));
            }
        }

        public new virtual void Spring(Pawn p)
        {
            SoundDefOf.EnergyShield_Broken.PlayOneShot(new TargetInfo(Position, Map));
            trapPawn = p;
            trapSprung = true;
        }

        // Helper function to allow overriding SpringChance BEFORE Mathf.Clamp01 is called on it.
        protected virtual float UnclampedSpringChance(Pawn p)
        {
            float num = KnowsOfTrap(p) ? 0.8f : this.GetStatValue(StatDefOf.TrapSpringChance);
            num *= GenMath.LerpDouble(0.4f, 0.8f, 0f, 1f, p.BodySize);
            return num;
        }

        protected override float SpringChance(Pawn p)
        {
            float num = UnclampedSpringChance(p);
            return Mathf.Clamp01(num);
        }

        public override ushort PathFindCostFor(Pawn p)
        {
            if (Armed && KnowsOfTrap(p))
                return 800;
            return 0;
        }

        public override ushort PathWalkCostFor(Pawn p)
        {
            if (Armed && KnowsOfTrap(p))
                return 50;
            return 0;
        }

        public override bool IsDangerousFor(Pawn p)
        {
            return Armed && KnowsOfTrap(p) && p.Faction != Faction;
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (!text.NullOrEmpty())
            {
                text += "\n";
            }
            if (Armed)
            {
                text += "Trap Armed";
            }
            else
            {
                text += "Trap Not Armed";
            }
            return text;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            InstallBlueprintUtility.CancelBlueprintsFor(this);
            if (mode == DestroyMode.Deconstruct)
            {
                SoundDef.Named("Building_Deconstructed").PlayOneShot(new TargetInfo(Position, Map));
            }
        }


    }
}
