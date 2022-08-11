using System;
using RimWorld;
using Verse;
using Verse.Sound;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse.AI;
using Verse.AI.Group;

namespace TorannMagic
{
    public class Building_LightningTrap : Building_Trap
    {
        public List<Pawn> touchingPawns = new List<Pawn>();

        private const float KnowerSpringChance = 0.004f;
        private const ushort KnowerPathFindCost = 800;
        private const ushort KnowerPathWalkCost = 30;
        private const float AnimalSpringChanceFactor = 0.1f;

        private int trapSpringDelay = 30;
        private bool trapSprung = false;

        public bool extendedTrap = false;
        public bool iceTrap = false;

        Pawn trapPawn = new Pawn();

        protected override void SpringSub(Pawn p)
        {
            base.GetComp<CompExplosive>().StartWick(null);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.extendedTrap, "extendedTrap",false, false);
            Scribe_Values.Look<bool>(ref this.iceTrap, "iceTrap", false, false);
        }

        private void CheckPawn(IntVec3 position)
        {
            List<Thing> thingList = position.GetThingList(base.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                Pawn pawn = thingList[i] as Pawn;
                if (pawn != null && pawn.Faction != this.Faction && pawn.HostileTo(this.Faction) && !this.touchingPawns.Contains(pawn))
                {
                    this.touchingPawns.Add(pawn);
                    this.CheckSpring(pawn);
                }
            }
        }

        public bool Armed
        {
            get
            {
                return !this.trapSprung;
            }
        }

        public override void Tick()
        {
            if (this.Armed)
            {
                CheckPawn(base.Position);
                for (int j = 0; j < 8; j++)
                {
                    IntVec3 intVec = this.Position + GenAdj.AdjacentCells[j];
                    CheckPawn(intVec);
                }
                for (int j = 0; j < this.touchingPawns.Count; j++)
                {
                    Pawn pawn2 = this.touchingPawns[j];
                    if (!pawn2.Spawned || pawn2.Position != base.Position)
                    {
                        this.touchingPawns.Remove(pawn2);
                    }
                }
            }
            else
            {
                this.trapSpringDelay--;
                if (this.trapSpringDelay <= 0)
                {
                    this.SpringSub(this.trapPawn);
                }
            }

            base.Tick();
        }

        private void CheckSpring(Pawn p)
        {
            if (Rand.Value < this.SpringChance(p))
            {
                this.Spring(p);
                if (p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer)
                {
                    Find.LetterStack.ReceiveLetter("LetterFriendlyTrapSprungLabel".Translate(
                        p.LabelShort
                    ), "LetterFriendlyTrapSprung".Translate(
                        p.LabelShort
                    ), LetterDefOf.NegativeEvent, new TargetInfo(base.Position, base.Map, false), null);
                }
            }
        }

        new public void Spring(Pawn p)
        {
            SoundDefOf.EnergyShield_Broken.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
            this.trapPawn = p;
            this.trapSprung = true;
            IntVec3 targetPos = this.Position;
            targetPos.z += 2;
            LocalTargetInfo t = targetPos;
            bool flag = t.Cell != default(IntVec3);
            float speed = .8f;
            if(extendedTrap)
            {
                speed = .6f;
            }
            if (flag)
            {
                Thing eyeThing = new Thing();
                eyeThing.def = TorannMagicDefOf.FlyingObject_LightningTrap;
                FlyingObject_LightningTrap flyingObject = (FlyingObject_LightningTrap)GenSpawn.Spawn(TorannMagicDefOf.FlyingObject_LightningTrap, this.Position, this.Map);
                flyingObject.Launch(p, this.Position.ToVector3Shifted(), t.Cell, eyeThing, this.Faction, null, speed);
            }
            if(iceTrap)
            {
                AddSnowRadial(this.Position, this.Map, 6, 1.1f);
            }
        }

        public static void AddSnowRadial(IntVec3 center, Map map, float radius, float depth)
        {
            int num = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = center + GenRadial.RadialPattern[i];
                if (intVec.InBoundsWithNullCheck(map))
                {
                    float lengthHorizontal = (center - intVec).LengthHorizontal;
                    float num2 = 1f - lengthHorizontal / radius;
                    map.snowGrid.AddDepth(intVec, num2 * depth);

                }
            }
        }

        protected override float SpringChance(Pawn p)
        {
            float num;
            if (this.KnowsOfTrap(p))
            {
                num = 0.8f;
            }
            else
            {
                num = this.GetStatValue(StatDefOf.TrapSpringChance, true);
            }
            num *= GenMath.LerpDouble(0.4f, 0.8f, 0f, 1f, p.BodySize);
            return Mathf.Clamp01(num);
        }

        public override ushort PathFindCostFor(Pawn p)
        {
            if (!this.Armed)
            {
                return 0;
            }
            if (this.KnowsOfTrap(p))
            {
                return 800;
            }
            return 0;
        }

        public override ushort PathWalkCostFor(Pawn p)
        {
            if (!this.Armed)
            {
                return 0;
            }
            if (this.KnowsOfTrap(p))
            {
                return 50;
            }
            return 0;
        }

        public override bool IsDangerousFor(Pawn p)
        {
            return this.Armed && this.KnowsOfTrap(p) && p.Faction != this.Faction;
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (!text.NullOrEmpty())
            {
                text += "\n";
            }
            if (this.Armed)
            {
                text += "Proximity Trap Armed";
            }
            else
            {
                text += "Trap Not Armed";
            }
            return text;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = base.Map;
            base.Destroy(mode);
            InstallBlueprintUtility.CancelBlueprintsFor(this);
            if (mode == DestroyMode.Deconstruct)
            {
                SoundDef.Named("Building_Deconstructed").PlayOneShot(new TargetInfo(base.Position, map, false));
            }
        }
    }
}