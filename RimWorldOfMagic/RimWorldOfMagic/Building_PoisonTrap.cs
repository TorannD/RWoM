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
        private Pawn planter;
        private bool ownerChecked;

        public bool destroyAfterUse = false;

        [StaticConstructorOnStartup]
        private static class RearmingGraphic
        {
            internal static readonly Material Material = MaterialPool.MatFrom("Other/PoisonTrap_rearming");
            internal static readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref planter, "TM_poisonTrapPlanter");
            Scribe_Values.Look(ref ownerChecked, "TM_poisonTrapOwnerChecked");
            const string contacts="TM_poisonTrapContacts";
            if (Scribe.mode==LoadSaveMode.LoadingVars)
            {
                // The first legacy entry belongs to Building_Trap; the second was written by this class.
                var record=Scribe.loader.curXmlParent;
                var old=record.SelectNodes("testees");
                if (record[contacts]==null && old.Count>1)
                {
                    var copy=record.OwnerDocument.CreateElement(contacts);
                    var last=old[old.Count-1];
                    foreach (System.Xml.XmlAttribute attribute in last.Attributes)
                        copy.SetAttribute(attribute.Name,attribute.Value);
                    copy.InnerXml=last.InnerXml;
                    record.AppendChild(copy);
                }
            }
            Scribe_Collections.Look(ref touchingPawns,contacts,LookMode.Reference);
            if (Scribe.mode==LoadSaveMode.PostLoadInit)
            {
                touchingPawns ??= new List<Pawn>();
                touchingPawns.RemoveAll(p=>p==null);
            }
            Scribe_Values.Look<bool>(ref this.triggered, "triggered", false, false);
            Scribe_Values.Look<bool>(ref this.rearming, "rearming", false, false);
            Scribe_Values.Look<bool>(ref this.destroyAfterUse, "destroyAfterUse", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 600, false);
            Scribe_Values.Look<int>(ref this.strikeDelay, "strikeDelay", 0, false);
            Scribe_Values.Look<int>(ref this.lastStrike, "lastStrike", 0, false);
            Scribe_Defs.Look<ThingDef>(ref this.fog, "fog");        
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (rearming)
            {
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(this.DrawPos, Quaternion.identity, new Vector3(1f, 1f, 1f));   //drawer for beam
                Graphics.DrawMesh(MeshPool.plane10, matrix, RearmingGraphic.Material, 0, null, 0, RearmingGraphic.Properties);
            }
            else
            {
                base.DrawAt(drawLoc, flip);
            }
        }

        protected override void Tick()
        {
            if (this.triggered)
            {
                if(this.age >= this.lastStrike + this.strikeDelay)
                {
                    try
                    {
                        IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(base.Position, this.radius, true);
                        foreach (IntVec3 curCell in targets)
                        {
                            if (!curCell.InBounds(Map) || !curCell.IsValid) continue;

                            Pawn victim = curCell.GetFirstPawn(base.Map);
                            if (victim != null && !victim.Dead && victim.RaceProps.IsFlesh)
                            {
                                BodyPartRecord bpr = null;
                                bpr = victim.def.race.body.AllParts.InRandomOrder().FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.BreathingSource));
                                TM_Action.DamageEntities(victim, bpr, Rand.Range(1f, 2f), 2f, TMDamageDefOf.DamageDefOf.TM_Poison, this);
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
                        IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(base.Position, 2, true);
                        foreach (IntVec3 curCell in targets)
                        {
                            if (!curCell.InBounds(Map)) continue;
                            List<Thing> thingList = curCell.GetThingList(base.Map);
                            for (int j = 0; j < thingList.Count; j++)
                            {
                                Pawn pawn = thingList[j] as Pawn;
                                if (pawn == null
                                    || pawn.RaceProps.Animal
                                    || pawn.Faction == null
                                    || pawn.Faction == Faction
                                    || !pawn.HostileTo(Faction)
                                    || touchingPawns.Contains(pawn))
                                    continue;

                                this.touchingPawns.Add(pawn);
                                this.CheckSpring(pawn);
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
            for(int i = 0; i < AllComps.Count; i++)
                AllComps[i].CompTick();
        }

        private void CheckForAgent()
        {
            destroyAfterUse = KeeperBody()==null;
        }

        private Pawn KeeperBody()
        {
            if (Map==null || Faction==null) return null;
            if (!ownerChecked)
            {
                foreach(var p in Map.mapPawns.SpawnedPawnsInFaction(Faction))
                {
                    if (ListsTrap(p)) { SetPlanter(p); break; }
                    var soul=HeldRanger(p);
                    if (ListsTrap(soul)) { SetPlanter(soul); break; }
                }
            }            
            if (planter==null || planter.Dead || planter.Destroyed || planter.Faction!=Faction || !ListsTrap(planter)) return null;
            if (planter.Spawned) return planter.Map==Map?planter:null;
            if (!ClassProgression.SpiritRebirthState.TryGetStored(planter,out var state) || state.Host?.Map!=Map ||
                HeldRanger(state.Host)!=planter) return null;
            return state.Host;
        }

        private bool ListsTrap(Pawn pawn) => pawn?.GetCompAbilityUserMight()?.combatItems?.Contains(this)==true;
        private static Pawn HeldRanger(Pawn body)
        {
            var soul=body?.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD)
                ?.TryGetComp<HediffComp_SpiritPossession>()?.SpiritPawn;
            return soul!=null && ClassProgression.SpiritRebirthState.TryGetStored(soul,out var state) &&
                state.Form==ClassProgression.SpiritRebirthForm.Possessed && state.Host==body ? soul : null;
        }

        internal void SetPlanter(Pawn pawn)
        {
            planter = pawn;
            ownerChecked = true;
        }

        internal void CreditSpring(Pawn victim)
        {
            if (!ClassProgression.ClassProgressionFeature.Enabled || victim?.Spawned != true || victim.Map != Map ||
                victim.Dead || victim.Downed || victim.IsPrisoner || victim.RaceProps.Animal || victim.Faction == null ||
                Faction == null || !victim.HostileTo(Faction)) return;
            var body=KeeperBody();
            if (body!=null && AutoCast.TM_AutocastCombat.IsActiveOpponent(body,victim) &&
                (body==planter || ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(body,out var active) && active==planter &&
                    ClassProgression.SpiritRebirthState.TryGet(planter,out var state) && !state.DepletionPending))
                ClassProgression.RangerMastery.Trapped(planter, victim);
        }

        protected override void CheckSpring(Pawn p)
        {
            if (Rand.Value < this.SpringChance(p))
            {
                bool newlySprung = !triggered && !rearming;
                this.Spring(p);
                if (newlySprung && triggered) CreditSpring(p);
            }
        }

        public override void Spring(Pawn p)
        {
            SoundDef.Named("DeadfallSpring").PlayOneShot(new TargetInfo(base.Position, base.Map, false));
            fog = TorannMagicDefOf.Fog_Poison;
            fog.gas.expireSeconds.min = this.duration / 60;
            fog.gas.expireSeconds.max = this.duration / 60;
            GenExplosion.DoExplosion(base.Position, base.Map, this.radius, TMDamageDefOf.DamageDefOf.TM_Poison, this, 0, 0, SoundDef.Named("TinyBell"), def, null, null, fog, 1f, 1, null, null, 0, false, null, 0f, 0, 0.0f, false);
            this.triggered = true;
        }

        protected override float SpringChance(Pawn p)
        {
            float num = UnclampedSpringChance(p);
            if (p.RaceProps.Animal)
            {
                num *= 0.1f;
            }
            return Mathf.Clamp01(num);
        }

        public new bool KnowsOfTrap(Pawn p)
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


        public override bool IsDangerousFor(Pawn p)
        {
            return this.Armed && this.KnowsOfTrap(p);
        }
    }
}
