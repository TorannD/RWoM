using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic.ClassProgression;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace TorannMagic
{
    public class Projectile_SummonDemon : Projectile_Ability, IThingHolder
    {
        private const int SummoningDuration = 300;
        private const int DemonDuration = 6660;
        private int age, nextBlackLightning, retryTick;
        private bool initialized, summoningComplete, returning, legacy, replacing;
        private Pawn warlock, succubus, supportBody, sacrificedPawn, demonPawn;
        private Thing cargo;
        private IntVec3 site = IntVec3.Invalid;
        private List<IntVec3> summoningCircle;
        private ThingOwner<Pawn> offeringHolder;

        public Projectile_SummonDemon() => offeringHolder = new ThingOwner<Pawn>(this,true);
        public ThingOwner GetDirectlyHeldThings() => offeringHolder;
        public void GetChildHolders(List<IThingHolder> children) => ThingOwnerUtility.AppendThingHoldersFromThings(children,offeringHolder);

        internal static bool Claims(Map map,Pawn offering)
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Projectile_SummonDemon");
            return offering != null && def != null && map?.listerThings != null && map.listerThings.ThingsOfDef(def)
                .Any(t => t is Projectile_SummonDemon ritual && ritual.sacrificedPawn == offering);
        }

        internal static bool ProtectsMap(Map map)
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Projectile_SummonDemon");
            return def != null && map?.listerThings != null && map.listerThings.ThingsOfDef(def)
                .Any(t=>t is Projectile_SummonDemon pact && (pact.replacing || pact.offeringHolder.Count>0));
        }

        internal bool HoldsFor(Pawn support,Pawn offering)
            => summoningComplete && supportBody == support && offeringHolder.Contains(offering);

        internal static bool Begin(Pawn body,Pawn warlock,Pawn succubus,Pawn offering,MagicAbility power)
        {
            if (!DemonRitual.CanStart(body,warlock,out var current,out var physical,out var mental) ||
                current != offering || physical != succubus || mental != warlock || power.Pawn != warlock) return false;
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Projectile_SummonDemon");
            if (def == null || !(ThingMaker.MakeThing(def) is Projectile_SummonDemon shot)) return false;
            shot.warlock = warlock;
            shot.succubus = succubus;
            shot.supportBody = DemonRitual.BodyOf(succubus);
            shot.sacrificedPawn = offering;
            try
            {
                GenSpawn.Spawn(shot,body.Position,body.Map);
                var props = power.magicDef.MainVerb;
                shot.Launch(body,power.Def,body.DrawPos,body,ProjectileHitFlags.IntendedTarget,false,null,
                    props.hediffsToApply,props.mentalStatesToApply,props.thingsToSpawn);
                return true;
            }
            catch (Exception ex)
            {
                Compatibility.TM_Log.Error("DemonRitual","Could not start the pact: " + ex);
                if (!shot.Destroyed) shot.Destroy();
                return false;
            }
        }

        protected override void Impact(Thing hitThing,bool blockedByShield = false)
        {
            if (initialized) return;
            if (blockedByShield || !ValidChannel()) { Destroy(); return; }
            landed = true;
            initialized = true;
            age = 0;
            site = sacrificedPawn.Position;
            summoningCircle = GenRadial.RadialCellsAround(site,5,true).Where(c=>c.InBounds(Map)).ToList();
            sacrificedPawn.TakeDamage(new DamageInfo(DamageDefOf.Stun,50,50,-1,null,null,null,DamageInfo.SourceCategory.ThingOrUnknown,sacrificedPawn));
            HealthUtility.AdjustSeverity(sacrificedPawn,TorannMagicDefOf.TM_HediffTimedInvulnerable,2);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_SummoningCircle,site.ToVector3Shifted(),Map,3f,2f,1f,2f,0,0,0,Rand.Range(0,360));
        }

        protected override void TickInterval(int delta)
        {
            if (!initialized) base.TickInterval(delta);
        }

        protected override void Tick()
        {
            if (legacy) { RecoverLegacy(); if (Destroyed || !Spawned) return; }
            if (!initialized) { base.Tick(); return; }
            if (returning) { Destroy(); return; }
            age++;
            if (summoningComplete)
            {
                if (demonPawn == null || demonPawn.Dead || demonPawn.Destroyed || age >= SummoningDuration + DemonDuration) Destroy();
                return;
            }
            if (!ValidChannel()) { Destroy(); return; }
            if (Find.TickManager.TicksGame % 2 == 0 && !summoningCircle.NullOrEmpty())
            {
                var cell = summoningCircle.RandomElement();
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Demon_Flame,cell.ToVector3(),Map,Rand.Range(.5f,.9f),
                    Rand.Range(.2f,.3f),.05f,Rand.Range(.2f,.4f),Rand.Range(-400,400),Rand.Range(.8f,1.2f)*(cell-sacrificedPawn.Position).LengthHorizontal,
                    (Quaternion.AngleAxis(90,Vector3.up)*GetVector(cell,sacrificedPawn.Position)).ToAngleFlat(),Rand.Range(0,359));
            }
            if (age > nextBlackLightning) { DoLightningStrike(); nextBlackLightning=age+Rand.Range(45,70); }
            if (age > SummoningDuration) CompleteSummon();
        }

        private bool ValidChannel()
        {
            return casterBody?.Map == Map && DemonRitual.ReadyOnMap(casterBody,warlock,sacrificedPawn) &&
                DemonRitual.ChannelMatches(supportBody,succubus,warlock,sacrificedPawn);
        }

        private Pawn casterBody => launcher as Pawn;

        private void RecoverLegacy()
        {
            legacy = false;
            warlock ??= casterBody;
            if (!summoningComplete)
            {
                if (!DemonRitual.TryPair(warlock,out var partner,out succubus,out var mental) || mental != warlock ||
                    sacrificedPawn != null && sacrificedPawn != partner) { Destroy(); return; }
                sacrificedPawn = partner;
                supportBody = DemonRitual.BodyOf(succubus);
                site = partner.Position;
                return;
            }
            site = !summoningCircle.NullOrEmpty() ? summoningCircle[0] : Position;
            if (sacrificedPawn != null && !sacrificedPawn.Spawned && !sacrificedPawn.Destroyed &&
                sacrificedPawn.holdingOwner == null && offeringHolder.TryAdd(sacrificedPawn,false) &&
                Find.WorldPawns.Contains(sacrificedPawn)) Find.WorldPawns.RemovePawn(sacrificedPawn);
        }

        private void CompleteSummon()
        {
            site = sacrificedPawn.Position;
            try
            {
                cargo = sacrificedPawn.carryTracker?.CarriedThing;
                if (cargo != null && (sacrificedPawn.inventory == null || !sacrificedPawn.carryTracker.innerContainer.TryTransferToContainer(cargo,sacrificedPawn.inventory.innerContainer,false)))
                { Destroy(); return; }
                replacing = true;
                sacrificedPawn.DeSpawn();
                if (!offeringHolder.TryAdd(sacrificedPawn,false)) throw new InvalidOperationException("Could not hold the offering.");
                var spawn = new SpawnThings { def = TorannMagicDefOf.TM_DemonR, kindDef = PawnKindDef.Named("TM_Demon"), spawnCount = 1 };
                SingleSpawnLoop(spawn,site,Map);
                if (demonPawn?.Spawned != true || demonPawn.Map != Map) throw new InvalidOperationException("Demon did not spawn.");
                summoningComplete = true;
                for(int i=0;i<3;i++) DoLightningStrike();
                FleckMaker.ThrowSmoke(site.ToVector3(),Map,2);
                FleckMaker.ThrowLightningGlow(site.ToVector3(),Map,10f);
                var sound = SoundInfo.InMap(new TargetInfo(site,Map),MaintenanceType.None);
                sound.pitchFactor = 1.3f; sound.volumeFactor = 1.6f; TorannMagicDefOf.TM_DemonDeath.PlayOneShot(sound);
            }
            catch (Exception ex)
            {
                Compatibility.TM_Log.Error("DemonRitual","Demon summon failed: " + ex);
                if (!sacrificedPawn.Spawned && sacrificedPawn.holdingOwner==null && !sacrificedPawn.Destroyed)
                    offeringHolder.TryAdd(sacrificedPawn,false);
                Destroy();
            }
            finally { replacing = false; }
        }

        public void SingleSpawnLoop(SpawnThings spawnables,IntVec3 position,Map map)
        {
            if (spawnables?.def?.race == null || spawnables.kindDef == null || map == null || demonPawn?.Spawned == true) return;
            var faction = casterBody?.Faction;
            var summoned = (TMPawnSummoned)PawnGenerator.GeneratePawn(spawnables.kindDef,faction);
            demonPawn = summoned;
            summoned.validSummoning = true;
            summoned.Spawner = warlock ?? casterBody;
            summoned.Temporary = true;
            summoned.TicksToDestroy = DemonDuration;
            GenSpawn.Spawn(summoned,position,map);
            if (summoned.playerSettings != null)
            {
                summoned.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
                summoned.playerSettings.medCare = MedicalCareCategory.NoCare;
            }
            if (faction != null && faction != Faction.OfPlayer)
            {
                var lord = map.mapPawns.SpawnedPawnsInFaction(faction).FirstOrDefault(p=>p!=summoned && p.GetLord()!=null)?.GetLord()
                    ?? LordMaker.MakeNewLord(faction,new LordJob_DefendPoint(position),map);
                lord.AddPawn(summoned);
            }
        }
        public void DoLightningStrike()
        {
            if (Spawned && !summoningCircle.NullOrEmpty())
                Map.weatherManager.eventHandler.AddEvent(new TM_WeatherEvent_MeshFlash(Map,summoningCircle.RandomElement(),TM_MatPool.redLightning));
        }

        public Vector3 GetVector(IntVec3 center,IntVec3 objectPos) => (objectPos-center).ToVector3().normalized;

        private bool DropAt(IntVec3 cell)
        {
            if (!cell.InBounds(Map) || !cell.Standable(Map) || cell.GetFirstPawn(Map)!=null) return false;
            var pawn = offeringHolder[0];
            try
            {
                if (!offeringHolder.TryDrop(pawn,cell,Map,ThingPlaceMode.Direct,out _,playDropSound:false)) return false;
            }
            catch (Exception ex)
            {
                Compatibility.TM_Log.Error("DemonRitual","Offering return failed: " + ex);
                if (!pawn.Spawned && !pawn.Destroyed && pawn.holdingOwner==null) offeringHolder.TryAdd(pawn,false);
                if (pawn.Map != Map || pawn.Position != cell) return false;
            }
            if (cargo != null && pawn.carryTracker?.CarriedThing == null && pawn.inventory?.innerContainer.Contains(cargo)==true)
                pawn.inventory.innerContainer.TryTransferToContainer(cargo,pawn.carryTracker.innerContainer,false);
            if (summoningComplete && !pawn.Dead)
                HealthUtility.AdjustSeverity(pawn,HediffDef.Named("TM_DemonicPriceHD"),2f);
            return true;
        }

        public override void Destroy(DestroyMode mode=DestroyMode.Vanish)
        {
            if (returning && Find.TickManager.TicksGame<retryTick) return;
            returning = true;
            retryTick = Find.TickManager.TicksGame+60;
            if (demonPawn is TMPawnSummoned summoned && !summoned.Dead && !summoned.Destroyed)
            {
                if (!SpiritSummonedHost.CanDismiss(summoned)) return;
                summoned.Destroy();
            }
            if (offeringHolder.Count>0 && Spawned && !Map.Disposed)
            {
                var center = site.InBounds(Map) ? site : Position;
                foreach(var cell in GenRadial.RadialCellsAround(center,12,true)) if (DropAt(cell)) break;
                if (offeringHolder.Count>0) foreach(var cell in Map.AllCells) if (DropAt(cell)) break;
                if (offeringHolder.Count>0) return;
            }
            base.Destroy(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref age,"age");
            Scribe_Values.Look(ref initialized,"initialized");
            Scribe_Values.Look(ref summoningComplete,"summoningComplete");
            Scribe_Values.Look(ref site,"TM_demonPactSite",IntVec3.Invalid);
            Scribe_Values.Look(ref nextBlackLightning,"TM_demonPactLightning");
            Scribe_Values.Look(ref returning,"TM_demonPactReturning");
            Scribe_Values.Look(ref retryTick,"TM_demonPactRetry");
            Scribe_References.Look(ref warlock,"TM_demonPactWarlock");
            Scribe_References.Look(ref succubus,"TM_demonPactSuccubus");
            Scribe_References.Look(ref supportBody,"TM_demonPactSupportBody");
            Scribe_References.Look(ref cargo,"TM_demonPactCargo");
            Scribe_References.Look(ref demonPawn,"demonPawn");
            Scribe_References.Look(ref sacrificedPawn,"sacrificedPawn");
            Scribe_Collections.Look(ref summoningCircle,"summoningCircle",LookMode.Value);
            Scribe_Deep.Look(ref offeringHolder,"TM_demonPactOffering",this);
            offeringHolder ??= new ThingOwner<Pawn>(this,true);
            bool savedPact=true;
            Scribe_Values.Look(ref savedPact,"TM_demonPact",false,true);
            if (Scribe.mode==LoadSaveMode.LoadingVars && !savedPact) legacy=true;
        }
    }
}
