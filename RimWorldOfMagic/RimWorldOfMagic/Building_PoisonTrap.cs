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
    [StaticConstructorOnStartup]
    public class Building_PoisonTrap : Building
    {
        private List<Pawn> touchingPawns = new List<Pawn>();

        private const float KnowerSpringChance = 0.004f;
        private const ushort KnowerPathFindCost = 800;
        private const ushort KnowerPathWalkCost = 30;
        private const float AnimalSpringChanceFactor = 0.1f;

        int age = -1;
        int duration = 480;
        int strikeDelay = 40;
        int lastStrike = 0;
        bool triggered = false;
        float radius = 3f;
        int ticksTillReArm = 15000;
        bool rearming = false;
        ThingDef fog;

        public bool destroyAfterUse = false;

        private static readonly Material trap_rearming = MaterialPool.MatFrom("Other/PoisonTrap_rearming");
        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        public virtual bool Armed
        {
            get
            {
                return true;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<Pawn>(ref this.touchingPawns, "testees", LookMode.Reference, new object[0]);
            Scribe_Values.Look<bool>(ref this.triggered, "triggered", false, false);
            Scribe_Values.Look<bool>(ref this.rearming, "rearming", false, false);
            Scribe_Values.Look<bool>(ref this.destroyAfterUse, "destroyAfterUse", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 600, false);
            Scribe_Values.Look<int>(ref this.strikeDelay, "strikeDelay", 0, false);
            Scribe_Values.Look<int>(ref this.lastStrike, "lastStrike", 0, false);
            Scribe_Defs.Look<ThingDef>(ref this.fog, "fog");        
        }

        public override void Draw()
        {
            if (rearming)
            {
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(this.DrawPos, Quaternion.identity, new Vector3(1f, 1f, 1f));   //drawer for beam
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_PoisonTrap.trap_rearming, 0, null, 0, Building_PoisonTrap.MatPropertyBlock);
            }
            else
            {
                base.Draw();
            }
        }

        public override void Tick()
        {
            if (this.triggered)
            {
                if(this.age >= this.lastStrike + this.strikeDelay)
                {
                    try
                    {
                        IntVec3 curCell;
                        IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(base.Position, this.radius, true);
                        for (int i = 0; i < targets.Count(); i++)
                        {
                            curCell = targets.ToArray<IntVec3>()[i];

                            if (curCell.InBoundsWithNullCheck(base.Map) && curCell.IsValid)
                            {
                                Pawn victim = curCell.GetFirstPawn(base.Map);
                                if (victim != null && !victim.Dead && victim.RaceProps.IsFlesh)
                                {
                                    BodyPartRecord bpr = null;
                                    bpr = victim.def.race.body.AllParts.InRandomOrder().FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.BreathingSource));
                                    TM_Action.DamageEntities(victim, bpr, Rand.Range(1f, 2f), 2f, TMDamageDefOf.DamageDefOf.TM_Poison, this);
                                }
                            }
                        }
                    }
                    catch
                    {
                        Log.Message("Debug: poison trap failed to process triggered event - terminating poison trap");
                        this.Destroy(DestroyMode.Vanish);
                    }
                    this.lastStrike = this.age;
                }
                this.age++;
                if(this.age > this.duration)
                {
                    CheckForAgent();
                    if(destroyAfterUse)
                    {
                        Destroy();
                    }
                    else
                    {
                        this.age = 0;
                        triggered = false;
                        rearming = true;
                        this.lastStrike = 0;
                    }
                }
            }
            else if(rearming)
            {
                this.age++;
                if(this.age > this.ticksTillReArm)
                {
                    this.age = 0;
                    rearming = false;
                    triggered = false;
                }
            }
            else
            {
                try
                { 
                    if (this.Armed)
                    {
                        IntVec3 curCell;
                        IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(base.Position, 2, true);
                        for (int i = 0; i < targets.Count(); i++)
                        {
                            curCell = targets.ToArray<IntVec3>()[i];
                            List<Thing> thingList = curCell.GetThingList(base.Map);
                            for (int j = 0; j < thingList.Count; j++)
                            {
                                Pawn pawn = thingList[j] as Pawn;
                                if (pawn != null && !this.touchingPawns.Contains(pawn))
                                {
                                    if (!pawn.RaceProps.Animal && pawn.Faction != null && pawn.Faction != this.Faction && pawn.HostileTo(this.Faction))
                                    {
                                        this.touchingPawns.Add(pawn);
                                        this.CheckSpring(pawn);
                                    }
                                }
                            }
                        }
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
                catch
                {
                    Log.Message("Debug: poison trap failed to process armed event - terminating poison trap");
                    this.Destroy(DestroyMode.Vanish);
                }
            }
            base.Tick();
        }

        private void CheckForAgent()
        {
            this.destroyAfterUse = true;
            List<Pawn> pList = this.Map.mapPawns.AllPawnsSpawned;
            if (pList != null && pList.Count > 0)
            {
                for (int i = 0; i < pList.Count; i++)
                {
                    Pawn p = pList[i];
                    CompAbilityUserMight comp = p.GetCompAbilityUserMight();
                    if(comp != null && comp.combatItems != null && comp.combatItems.Count > 0)
                    {
                        if(comp.combatItems.Contains(this))
                        {
                            this.destroyAfterUse = false;
                        }
                    }
                }
            }
        }

        private void CheckSpring(Pawn p)
        {
            if (Rand.Value < this.SpringChance(p))
            {
                this.Spring(p);
                //if (p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer)
                //{
                //    Find.LetterStack.ReceiveLetter("LetterFriendlyTrapSprungLabel".Translate(
                //        p.LabelShort
                //    ), "LetterFriendlyTrapSprung".Translate(
                //        p.LabelShort
                //    ), LetterDefOf.NegativeEvent, new TargetInfo(base.Position, base.Map, false), null);
                //}
            }
        }

        public void Spring(Pawn p)
        {
            SoundDef.Named("DeadfallSpring").PlayOneShot(new TargetInfo(base.Position, base.Map, false));
            fog = TorannMagicDefOf.Fog_Poison;
            fog.gas.expireSeconds.min = this.duration / 60;
            fog.gas.expireSeconds.max = this.duration / 60;
            GenExplosion.DoExplosion(base.Position, base.Map, this.radius, TMDamageDefOf.DamageDefOf.TM_Poison, this, 0, 0, SoundDef.Named("TinyBell"), def, null, null, fog, 1f, 1, false, null, 0f, 0, 0.0f, false);
            this.triggered = true;
        }

        protected virtual float SpringChance(Pawn p)
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
            if (p.RaceProps.Animal)
            {
                num *= 0.1f;
            }
            return Mathf.Clamp01(num);
        }

        public bool KnowsOfTrap(Pawn p)
        {
            if (p.Faction != null && !p.Faction.HostileTo(base.Faction))
            {
                return true;
            }
            if (p.Faction == null && p.RaceProps.Animal && !p.InAggroMentalState)
            {
                return true;
            }
            if (p.guest != null && p.guest.Released)
            {
                return true;
            }
            Lord lord = p.GetLord();
            return p.RaceProps.Humanlike && lord != null && lord.LordJob is LordJob_FormAndSendCaravan;
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
                return 30;
            }
            return 0;
        }

        public override bool IsDangerousFor(Pawn p)
        {
            return this.Armed && this.KnowsOfTrap(p);
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
