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
    public class Building_PoisonTrap : Building_ExplosiveProximityTrap
    {
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
                matrix.SetTRS(DrawPos, Quaternion.identity, new Vector3(1f, 1f, 1f));   //drawer for beam
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_PoisonTrap.trap_rearming, 0, null, 0, Building_PoisonTrap.MatPropertyBlock);
            }
            else
            {
                base.Draw();
            }
        }

        public override void Tick()
        {
            if (triggered)
            {
                if(age >= lastStrike + strikeDelay)
                {
                    try
                    {
                        IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(Position, radius, true);
                        foreach (IntVec3 curCell in targets)
                        {
                            if (!curCell.InBoundsWithNullCheck(Map) || !curCell.IsValid) continue;

                            Pawn victim = curCell.GetFirstPawn(Map);
                            if (victim == null || victim.Dead || !victim.RaceProps.IsFlesh) continue;

                            BodyPartRecord bpr = victim.def.race.body.AllParts.InRandomOrder().FirstOrDefault(
                                x => x.def.tags.Contains(BodyPartTagDefOf.BreathingSource));
                            TM_Action.DamageEntities(victim, bpr, Rand.Range(1f, 2f), 2f, TMDamageDefOf.DamageDefOf.TM_Poison, this);
                        }
                    }
                    catch
                    {
                        Log.Message("Debug: poison trap failed to process triggered event - terminating poison trap");
                        Destroy();
                    }
                    lastStrike = age;
                }
                age++;
                if(age > duration)
                {
                    CheckForAgent();
                    if(destroyAfterUse)
                    {
                        Destroy();
                    }
                    else
                    {
                        age = 0;
                        triggered = false;
                        rearming = true;
                        lastStrike = 0;
                    }
                }
            }
            else if(rearming)
            {
                age++;
                if(age > ticksTillReArm)
                {
                    age = 0;
                    rearming = false;
                    triggered = false;
                }
            }
            else
            {
                try
                { 
                    if (Armed)
                    {
                        IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(Position, 2, true);
                        foreach (IntVec3 curCell in targets)
                        {
                            List<Thing> thingList = curCell.GetThingList(Map);
                            for (int j = 0; j < thingList.Count; j++)
                            {
                                Pawn pawn = thingList[j] as Pawn;
                                if (
                                    pawn == null
                                    || pawn.RaceProps.Animal
                                    || pawn.Faction == null
                                    || pawn.Faction == Faction
                                    || !pawn.HostileTo(Faction)
                                    || touchingPawns.Contains(pawn)
                                )
                                    continue;
                                touchingPawns.Add(pawn);
                                CheckSpring(pawn);
                            }
                        }
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
                catch
                {
                    Log.Message("Debug: poison trap failed to process armed event - terminating poison trap");
                    Destroy();
                }
            }
            base.Tick();
        }

        private void CheckForAgent()
        {
            destroyAfterUse = true;
            List<Pawn> pList = Map.mapPawns.AllPawnsSpawned;
            if (pList == null || pList.Count <= 0) return;
            for (int i = 0; i < pList.Count; i++)
            {
                Pawn p = pList[i];
                CompAbilityUserMight comp = p.GetCompAbilityUserMight();
                if (comp?.combatItems == null || comp.combatItems.Count <= 0) continue;

                if(comp.combatItems.Contains(this))
                {
                    destroyAfterUse = false;
                }
            }
        }

        protected override void CheckSpring(Pawn p)
        {
            if (Rand.Value < SpringChance(p))
                Spring(p);
        }

        public override void Spring(Pawn p)
        {
            SoundDef.Named("DeadfallSpring").PlayOneShot(new TargetInfo(base.Position, base.Map, false));
            fog = TorannMagicDefOf.Fog_Poison;
            fog.gas.expireSeconds.min = duration / 60;
            fog.gas.expireSeconds.max = duration / 60;
            GenExplosion.DoExplosion(Position, Map, radius, TMDamageDefOf.DamageDefOf.TM_Poison, this, 0, 0, SoundDef.Named("TinyBell"), def, null, null, fog, 1f, 1, false, null, 0f, 0, 0.0f, false);
            triggered = true;
        }

        protected override float UnclampedSpringChance(Pawn p)
        {
            float num = base.UnclampedSpringChance(p);
            if (p.RaceProps.Animal)
                num *= 0.1f;
            return num;
        }

        public new bool KnowsOfTrap(Pawn p)
        {
            if (
                p.Faction != null && !p.Faction.HostileTo(Faction)
                || p.Faction == null && p.RaceProps.Animal && !p.InAggroMentalState
                || p.guest != null && p.guest.Released
            )
                return true;

            Lord lord = p.GetLord();
            return p.RaceProps.Humanlike && lord?.LordJob is LordJob_FormAndSendCaravan;
        }

        public override bool IsDangerousFor(Pawn p)
        {
            return Armed && KnowsOfTrap(p);
        }
    }
}
