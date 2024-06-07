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
        private bool trapSprung = false;

        private Pawn trapPawn = new Pawn();

        public bool Armed => !trapSprung;


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

        public override void ExposeData()
        {
            base.ExposeData();
            //Scribe_References.Look<Pawn>(ref this.trapPawn, "trapPawn", false);
        }

        private void CheckPawn(IntVec3 position)
        {
            List<Thing> thingList = position.GetThingList(base.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                Pawn pawn = thingList[i] as Pawn;
                if (pawn == null
                    || pawn.Faction == Faction
                    || !pawn.HostileTo(Faction)
                    || touchingPawns.Contains(pawn)) continue;

                touchingPawns.Add(pawn);
                CheckSpring(pawn);
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

        protected virtual void CheckSpring(Pawn p)
        {
            if (!(Rand.Value < SpringChance(p))) return;

            this.Spring(p);
            if (p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer)
            {
                Find.LetterStack.ReceiveLetter("LetterFriendlyTrapSprungLabel".Translate(
                    p.LabelShort
                ), "LetterFriendlyTrapSprung".Translate(
                    p.LabelShort
                ), LetterDefOf.NegativeEvent, new TargetInfo(base.Position, base.Map));
            }            
        }

        public new virtual void Spring(Pawn p)
        {
            
            TorannMagicDefOf.EnergyShield_Broken.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
            this.trapPawn = p;
            this.trapSprung = true;
        }

        protected virtual float UnclampedSpringChance(Pawn p)
        {
            float num = KnowsOfTrap(p) ? 0.8f : this.GetStatValue(StatDefOf.TrapSpringChance, true);
            num *= GenMath.LerpDouble(0.4f, 0.8f, 0f, 1f, p.BodySize);
            return num;
        }

        protected override float SpringChance(Pawn p)
        {            
            return Mathf.Clamp01(UnclampedSpringChance(p));
        }

        public override ushort PathFindCostFor(Pawn p)
        {
            if (!this.Armed && this.KnowsOfTrap(p)) return 800;
            return 0;
        }

        public override ushort PathWalkCostFor(Pawn p)
        {
            if (!this.Armed && this.KnowsOfTrap(p)) return 50;
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
                SoundDef.Named("Building_Deconstructed").PlayOneShot(new TargetInfo(base.Position, Map, false));
            }
        }
    }
}
