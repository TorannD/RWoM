using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using RimWorld.Planet;
using TorannMagic.ClassProgression;
using TorannMagic.Odyssey;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    class Projectile_Scorn : Projectile_DemonField, IThingHolder
    {
        protected override TMAbilityDef RankSpell => TorannMagicDefOf.TM_Scorn;
        private ThingOwner<Pawn> passenger;
        private Skyfaller leaving, incoming;
        private Thing orderedCargo;
        private IntVec3 origin = IntVec3.Invalid, landing = IntVec3.Invalid;
        private bool launched, pivoted, returned, admitted, consent, drafted, takingOff;
        private bool legacyPaid, legacyPassenger, recovering;
        private int nextRecoveryTick;
        private int wave = 1;
        private float angle, strength = 1f;
        private const int SafetyHorizon = 180;

        public Projectile_Scorn()
        {
            duration = 1800;
            passenger = new ThingOwner<Pawn>(this,true);
        }

        public ThingOwner GetDirectlyHeldThings() => passenger;
        public void GetChildHolders(List<IThingHolder> children) => ThingOwnerUtility.AppendThingHoldersFromThings(children,passenger);

        internal static bool ProtectsMap(Map map)
        {
            var def = TorannMagicDefOf.TM_Scorn?.MainVerb?.defaultProjectile;
            return def != null && map?.listerThings != null && map.listerThings.ThingsOfDef(def)
                .Any(t => t is Projectile_Scorn f && (f.takingOff || f.passenger.Count > 0 || f.legacyPassenger && f.pawn != null && !f.pawn.Spawned && f.pawn.holdingOwner == null));
        }

        internal static IntVec3 LandingCell(IntVec3 target, Map map)
        {
            if (map == null || !target.InBounds(map) || map.Size.x < 10 || map.Size.z < 10) return IntVec3.Invalid;
            return new IntVec3(Mathf.Clamp(target.x,5,map.Size.x-5),0,Mathf.Clamp(target.z,5,map.Size.z-5));
        }

        internal static bool Open(Map map,IntVec3 cell) => map != null && cell.IsValid && cell.InBounds(map) &&
            cell.Standable(map) && cell.GetFirstPawn(map) == null && !cell.ContainsStaticFire(map);

        internal void BindLaunch(Pawn body)
        {
            pawn = body;
            var job = body?.CurJob;
            var endpoint = job?.globalTarget ?? GlobalTargetInfo.Invalid;
            orderedCargo = job?.targetC.Thing;
            admitted = job != null && endpoint.Map == Map && endpoint.Cell == LandingCell(usedTarget.Cell,Map) && Open(Map,endpoint.Cell);
            if (!admitted) return;
            landing = endpoint.Cell;
            consent = ManualTravelSafety.HasConsent(body,job,Map,landing);
        }

        protected override void ReadRanks(Pawn owner,TMAbilityDef spell)
        {
            var magic = owner.GetCompAbilityUserMagic();
            pwrVal = magic?.MagicData.GetSkill_Power(spell)?.level ?? 0;
            verVal = magic?.MagicData.GetSkill_Versatility(spell)?.level ?? 0;
            strength = magic?.arcaneDmg ?? 1f;
            if (ModOptions.Settings.Instance.AIHardMode && !owner.IsColonist) pwrVal = verVal = 1;
        }

        private bool SafeLanding()
        {
            if (!Open(Map,landing)) return false;
            var effect = TravelEnvironmentSafety.Evaluate(pawn,Map,landing,SafetyHorizon);
            return effect.IsSafe || (consent && (effect.Hazards & TravelEnvironmentHazard.InvalidDestination) == 0);
        }

        private bool TakeOff()
        {
            if (legacyPaid && !admitted)
            {
                pawn ??= launcher as Pawn;
                landing = LandingCell(Position,Map);
                admitted = pawn?.Spawned == true && pawn.Map == Map && localAbilityDef == RankSpell && SafeLanding();
            }
            if (!admitted || pawn?.Spawned != true || pawn.Map != Map || pawn.Dead || pawn.Downed ||
                pawn.Position == landing || landing != LandingCell(Position,Map) || !SafeLanding()) return false;
            var owner = SkillOwner;
            var power = owner == pawn ? owner.GetCompAbilityUserMagic()?.AbilityData.Powers.OfType<MagicAbility>()
                .FirstOrDefault(a => a.Def == RankSpell) : SpiritHostedBolt.Find(pawn,owner,RankSpell);
            if (!legacyPaid && (power == null || (owner == pawn ? !power.CanCastPowerCheck(pawn.Faction?.IsPlayer == true ?
                AbilityContext.Player : AbilityContext.AI,out _) : !SpiritHostedBolt.Ready(pawn,power)))) return false;

            if (ModCheck.Validate.GiddyUp.Core_IsInitialized()) ModCheck.GiddyUp.ForceDismount(pawn);
            origin = pawn.Position;
            drafted = pawn.Drafted;
            var cargo = pawn.carryTracker?.CarriedThing;
            takingOff = true;
            try
            {
                if (cargo != null)
                {
                    if (pawn.inventory == null || !pawn.carryTracker.innerContainer.TryTransferToContainer(cargo,pawn.inventory.innerContainer,false)) return false;
                    orderedCargo = cargo;
                }
                pawn.DeSpawn();
                if (!passenger.TryAdd(pawn,false)) return false;
                angle = Position.x < origin.x ? Rand.Range(20,40) : Rand.Range(-40,-20);
                leaving = SkyfallerMaker.SpawnSkyfaller(ThingDef.Named("TM_ScornLeaving"),origin,Map);
                leaving.angle = angle;
                launched = true;
                if (!legacyPaid)
                {
                    if (owner == pawn) ManualTravelSafety.Settle(power);
                    else ManualTravelSafety.SettleHosted(power,pawn);
                }
                return true;
            }
            finally
            {
                takingOff = false;
                if (!launched)
                {
                    if (passenger.Contains(pawn)) Release(origin);
                    else if (!pawn.Spawned && pawn.holdingOwner == null) GenSpawn.Spawn(pawn,origin,Map);
                    RestoreCargo();
                }
            }
        }

        private void RestoreCargo()
        {
            if (orderedCargo != null && pawn?.Spawned == true && pawn.carryTracker?.CarriedThing == null &&
                pawn.inventory?.innerContainer.Contains(orderedCargo) == true)
                pawn.inventory.innerContainer.TryTransferToContainer(orderedCargo,pawn.carryTracker.innerContainer,false);
        }

        private bool Release(IntVec3 cell)
        {
            if (!Spawned || Map.Disposed || passenger.Count == 0 || !Open(Map,cell)) return false;
            var released = passenger[0];
            try
            {
                if (!passenger.TryDrop(released,cell,Map,ThingPlaceMode.Direct,out _,playDropSound:false)) return false;
            }
            catch (Exception error)
            {
                Compatibility.TM_Log.Error("Scorn","Scorn landing failed: " + error);
                if (!released.Spawned && !released.Destroyed && released.holdingOwner == null)
                    passenger.TryAdd(released,false);
                if (released.Map != Map || released.Position != cell) return false;
            }
            if (released.drafter != null && released.Faction?.IsPlayer == true) released.drafter.Drafted = drafted;
            RestoreCargo();
            return true;
        }

        protected override void TickField()
        {
            if (recovering) { Destroy(); return; }
            if (!initialized)
            {
                InitializeRanks(RankSpell);
                initialized = true;
            }
            if (!launched)
            {
                if (!TakeOff()) Destroy();
                return;
            }
            if (!returned)
            {
                if (legacyPassenger)
                {
                    legacyPassenger = false;
                    if (!pivoted) landing = LandingCell(Position,Map);
                    if (passenger.Count == 0 && pawn != null && !pawn.Spawned && !pawn.Destroyed && pawn.holdingOwner == null &&
                        passenger.TryAdd(pawn,false) && Find.WorldPawns.Contains(pawn)) Find.WorldPawns.RemovePawn(pawn);
                }
                if (passenger.Count==0 || pawn != passenger[0] || !landing.InBounds(Map)) { Destroy(); return; }
                if (!pivoted)
                {
                    if (leaving?.Spawned==true) return;
                    if (!SafeLanding()) { Destroy(); return; }
                    incoming = SkyfallerMaker.SpawnSkyfaller(ThingDef.Named("TM_ScornIncoming"),landing,Map);
                    incoming.angle = angle;
                    pivoted = true;
                    return;
                }
                if (incoming?.Spawned==true) return;
                if (!SafeLanding() || !Release(landing)) { Destroy(); return; }
                returned = true;
                var buff = verVal switch { 0 => TorannMagicDefOf.TM_DemonScornHD, 1 => TorannMagicDefOf.TM_DemonScornHD_I,
                    2 => TorannMagicDefOf.TM_DemonScornHD_II, _ => TorannMagicDefOf.TM_DemonScornHD_III };
                HealthUtility.AdjustSeverity(pawn,buff,60f + pwrVal * 15);
            }
            if (!landing.IsValid || SkillOwner == null) { Destroy(); return; }
            if (Find.TickManager.TicksGame % 4 != 0) return;
            if (wave > def.projectile.explosionRadius + verVal) { Destroy(); return; }
            foreach (var cell in Projectile_Repulsion.Ring(landing,wave))
                if (cell.InBounds(Map))
                    GenExplosion.DoExplosion(cell,Map,.4f,TMDamageDefOf.DamageDefOf.TM_Shadow,SkillOwner,
                        (int)(def.projectile.GetDamageAmount(1,null)*(1+.15f*pwrVal)*strength*Rand.Range(.75f,1.25f)),
                        0,TorannMagicDefOf.TM_SoftExplosion,def);
            wave++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (ResolvingImpact) return;
            if (!launched) RestoreCargo();
            if (passenger.Count > 0 && Spawned && !Map.Disposed)
            {
                if (recovering && Find.TickManager.TicksGame < nextRecoveryTick) return;
                recovering = true;
                nextRecoveryTick = Find.TickManager.TicksGame + 60;
                var center = origin.InBounds(Map) ? origin : Position;
                if (!Release(center))
                {
                    foreach (var cell in GenRadial.RadialCellsAround(center,12,false)) if (Release(cell)) break;
                    if (passenger.Count > 0) foreach (var cell in Map.AllCells) if (Release(cell)) break;
                    if (passenger.Count > 0) return;
                }
            }
            if (leaving?.Spawned == true) leaving.Destroy();
            if (incoming?.Spawned == true) incoming.Destroy();
            base.Destroy(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref passenger,"TM_scorningPassenger",this);
            passenger ??= new ThingOwner<Pawn>(this,true);
            Scribe_References.Look(ref orderedCargo,"TM_scorningCargo");
            Scribe_References.Look(ref leaving,"TM_scorningLeaving");
            Scribe_References.Look(ref incoming,"TM_scorningIncoming");
            Scribe_Values.Look(ref initialized,"initialized");
            Scribe_Values.Look(ref launched,"launchedFlag");
            Scribe_Values.Look(ref pivoted,"pivotFlag");
            Scribe_Values.Look(ref returned,"landedFlag");
            Scribe_Values.Look(ref landing,"safePos",IntVec3.Invalid);
            Scribe_Values.Look(ref origin,"TM_scorningOrigin",IntVec3.Invalid);
            Scribe_Values.Look(ref angle,"TM_scorningAngle");
            Scribe_Values.Look(ref admitted,"TM_scorningOrder");
            Scribe_Values.Look(ref consent,"TM_scorningConsent");
            Scribe_Values.Look(ref drafted,"TM_scorningDrafted");
            Scribe_Values.Look(ref wave,"strikeNum",1);
            Scribe_Values.Look(ref age,"age");
            Scribe_Values.Look(ref strength,"TM_scorningPower",1f);
            Scribe_Values.Look(ref recovering,"TM_scorningRecovering");
            Scribe_Values.Look(ref nextRecoveryTick,"TM_scorningRecoveryTick");
            Scribe_Values.Look(ref legacyPaid,"TM_scorningLegacyPaid");
            Scribe_Values.Look(ref legacyPassenger,"TM_scorningLegacyPassenger");
            bool hasTransitData = true;
            Scribe_Values.Look(ref hasTransitData,"TM_scorningTransit",false,true);
            if (Scribe.mode == LoadSaveMode.LoadingVars && !hasTransitData)
            {
                legacyPaid = true;
                legacyPassenger = launched && !returned;
                drafted = true;
            }

        }
    }
}
