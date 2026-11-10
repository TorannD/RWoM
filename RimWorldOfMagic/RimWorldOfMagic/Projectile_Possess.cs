using Verse;
using AbilityUser;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse.AI;
using Verse.AI.Group;
using System;


namespace TorannMagic
{
    public class Projectile_Possess : Projectile_AbilityBase, IThingHolder
    {
        private bool initialized = false;
        private int age = 0;
        private int duration = 1200;
        private int inventoryCount = 0;
        private IntVec3 oldPosition;
        private bool possessedFlag = false;
        Faction pFaction = null;
        Pawn hitPawn = null;
        Pawn caster = null;
        List<int> hitPawnWorkSetting = new List<int>();
        private List<string> workTypeNames;
        private bool prisoner = false;

        private int verVal;
        private int pwrVal;

        private readonly ThingOwner<Pawn> passenger;
        private bool takingBody, resolvingImpact, returning;
        private int retryAt;
        private bool removeDisguise=true;

        public Projectile_Possess() => passenger=new ThingOwner<Pawn>(this,true);
        public ThingOwner GetDirectlyHeldThings() => passenger;
        public void GetChildHolders(List<IThingHolder> children) =>
            ThingOwnerUtility.AppendThingHoldersFromThings(children,passenger);

        internal static bool ProtectsMap(Map map)
        {
            var def=TorannMagicDefOf.TM_Possess?.MainVerb?.defaultProjectile;
            return def!=null && map?.listerThings?.ThingsOfDef(def).Any(t=>t is Projectile_Possess control &&
                (control.takingBody || control.passenger.Count!=0) &&
                (control.caster?.Faction?.IsPlayer==true || control.pFaction?.IsPlayer==true))==true;
        }

        internal const string Order="TM_SpiritCastFacelessControl";
        private Pawn skillSource;
        private bool projected;

        internal void BindSource(Pawn owner) { skillSource=owner; projected=true; }
        internal Pawn SourceFor(Pawn body) => !projected ? body :
            ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(body,out var current) && current==skillSource ? current : null;

        internal static bool ValidTarget(Pawn body, MightAbility power, Pawn target)
        {
            if (power==null || body?.Spawned!=true || target?.Spawned!=true || body==target || body.Map!=target.Map ||
                target.Dead || target.Downed || target.IsPrisoner || target.Faction==null || !target.RaceProps.Humanlike ||
                ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(target,out _) ||
                target.Position.Fogged(body.Map) || !target.Position.InHorDistOf(body.Position,power.mightDef.MainVerb.range))
                return false;
            return new Verb_UseAbility {caster=body,Ability=power,verbProps=power.mightDef.MainVerb}
                .TryFindShootLineFromTo(body.Position,target,out _);
        }

        internal static bool LaunchFrom(Pawn body,MightAbility power,Pawn target)
        {
            if (!ValidTarget(body,power,target)) return false;
            var props=power.mightDef.MainVerb;
            if (ThingMaker.MakeThing(props.defaultProjectile) is not Projectile_Possess shot) return false;
            shot.BindSource(power.Pawn);
            GenSpawn.Spawn(shot,body.Position,body.Map);
            shot.extraDamages=props.extraDamages;
            shot.Launch(body,power.Def,body.DrawPos,target,ProjectileHitFlags.IntendedTarget|ProjectileHitFlags.NonTargetPawns,
                false,null,props.hediffsToApply,props.mentalStatesToApply,props.thingsToSpawn);
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref projected,"facelessControlProjected");
            Scribe_References.Look(ref skillSource,"facelessControlSource");
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.prisoner, "prisoner", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", 0, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1200, false);
            Scribe_Values.Look<int>(ref this.inventoryCount, "inventoryCount", 0, false);
            Scribe_References.Look<Faction>(ref this.pFaction, "pFaction", false);
            Scribe_Values.Look<IntVec3>(ref this.oldPosition, "oldPosition", default(IntVec3), false);
            Scribe_References.Look<Pawn>(ref this.hitPawn, "hitPawn", false);
            // Keep the historical deep record as the sole serialized owner of the physical pawn.
            Scribe_Deep.Look(ref caster,"caster");
            Scribe_Values.Look(ref returning,"facelessControlReturning");
            Scribe_Values.Look(ref retryAt,"facelessControlRetryAt");
            Scribe_Values.Look(ref removeDisguise,"facelessControlRemoveDisguise",true);
            if (Scribe.mode==LoadSaveMode.PostLoadInit && initialized) AttachBody();
            Scribe_Collections.Look(ref hitPawnWorkSetting,"hitPawnWorkSettings",LookMode.Value);
            Scribe_Collections.Look(ref workTypeNames,"facelessWorkTypes",LookMode.Value);
            if (Scribe.mode==LoadSaveMode.PostLoadInit && workTypeNames==null && hitPawnWorkSetting?.Count>0)
                workTypeNames=WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder.Take(hitPawnWorkSetting.Count).Select(w=>w.defName).ToList();
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (!initialized && (blockedByShield || launcher is not Pawn physical || SourceFor(physical)==null || hitThing is not Pawn victim ||
                projected && ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(victim,out _)))
            {
                age=duration;
                base.Impact(hitThing,blockedByShield);
                if (!Destroyed) Destroy();
                return;
            }
            resolvingImpact=true;
            try { base.Impact(hitThing,blockedByShield); }
            finally { resolvingImpact=false; }
            if (!initialized && this.age < this.duration && hitThing != null)
            {
                caster = this.launcher as Pawn;
                hitPawn = hitThing as Pawn;
                this.oldPosition = caster.Position;
                var owner=SourceFor(caster);
                var skills=owner.GetCompAbilityUserMight()?.MightData.MightPowerSkill_Possess;
                pwrVal=skills?.FirstOrDefault(skill => skill.label=="TM_Possess_pwr")?.level ?? 0;
                verVal=skills?.FirstOrDefault(skill => skill.label=="TM_Possess_ver")?.level ?? 0;
                if (ModOptions.Settings.Instance.AIHardMode && !caster.IsColonist)
                {
                    pwrVal = 3;
                    verVal = 3;
                }
                this.duration += pwrVal * 300;
                if (hitPawn != null && hitPawn.Faction != null && hitPawn.RaceProps.Humanlike)
                {
                    possessedFlag = (hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CoOpPossessionHD) || hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CoOpPossessionHD_I) || hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CoOpPossessionHD_II) || hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CoOpPossessionHD_III) ||
                        hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD) || hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_I) || hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_II) || hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_III));
                    if (!hitPawn.Downed && !hitPawn.Dead && !possessedFlag && !hitPawn.IsPrisoner)
                    {
                        this.pFaction = hitPawn.Faction;
                        this.prisoner = hitPawn.IsPrisoner;
                        if(!caster.IsColonist && hitPawn.IsColonist && hitPawn.workSettings!=null)
                        {
                            List<WorkTypeDef> allWorkTypes = WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder.ToList();
                            this.hitPawnWorkSetting = new List<int>();
                            workTypeNames=allWorkTypes.Select(w=>w.defName).ToList();
                            for(int i = 0; i < allWorkTypes.Count(); i++)
                            {
                                hitPawnWorkSetting.Add(hitPawn.workSettings.GetPriority(allWorkTypes[i]));
                            }
                        }
                        
                        if (this.pFaction != caster.Faction)
                        {
                            
                            if (Rand.Chance(TM_Calc.GetSpellSuccessChance(owner, hitPawn, true)))
                            {
                                //possess enemy or neutral
                                int weaponCount = 0;
                                if (hitPawn.equipment.PrimaryEq != null)
                                {
                                    weaponCount = 1;
                                }
                                this.inventoryCount = hitPawn.inventory.innerContainer.Count + hitPawn.apparel.WornApparelCount + weaponCount;
                                if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                                {
                                    ModCheck.GiddyUp.ForceDismount(caster);
                                    ModCheck.GiddyUp.ForceDismount(hitPawn);
                                }
                                hitPawn.SetFaction(caster.Faction, null);
                                ApplyDisguise();
                                switch (verVal)
                                {
                                    case 0:
                                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_PossessionHD, 20f + 5f * pwrVal);
                                        break;
                                    case 1:
                                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_PossessionHD_I, 20f + 5f * pwrVal);
                                        break;
                                    case 2:
                                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_PossessionHD_II, 20f + 5f * pwrVal);
                                        break;
                                    case 3:
                                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_PossessionHD_III, 20f + 5f * pwrVal);
                                        break;
                                }
                                initialized = true;
                                FleckMaker.ThrowSmoke(caster.DrawPos, caster.Map, 1f);
                                FleckMaker.ThrowSmoke(caster.DrawPos, caster.Map, 1.2f);
                                FleckMaker.ThrowHeatGlow(caster.Position, caster.Map, .8f);
                                if (!caster.IsColonist)
                                {
                                    var lord = caster.GetLord();
                                    if (lord != null)
                                    {
                                        hitPawn.mindState.duty = caster.mindState.duty;
                                        lord.AddPawn(hitPawn);
                                    }
                                }
                                if (hitPawn.IsColonist && !caster.IsColonist)
                                {
                                    TM_Action.SpellAffectedPlayerWarning(hitPawn);
                                }
                                TakeBody();
                                
                            }
                            else
                            {
                                MoteMaker.ThrowText(hitThing.DrawPos, hitThing.Map, "TM_ResistedSpell".Translate(), -1);
                                this.age = this.duration;
                                this.Destroy(DestroyMode.Vanish);
                            }
                        }
                        else
                        {
                            //possess friendly
                            if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                            {
                                ModCheck.GiddyUp.ForceDismount(caster);
                            }
                            ApplyDisguise();
                            switch (verVal)
                            {
                                case 0:
                                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_CoOpPossessionHD, 20f + 5f * pwrVal);
                                    break;
                                case 1:
                                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_CoOpPossessionHD_I, 20f + 5f * pwrVal);
                                    break;
                                case 2:
                                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_CoOpPossessionHD_II, 20f + 5f * pwrVal);
                                    break;
                                case 3:
                                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_CoOpPossessionHD_III, 20f + 5f * pwrVal);
                                    break;
                            }
                            initialized = true;
                            FleckMaker.ThrowSmoke(caster.DrawPos, caster.Map, 1f);
                            FleckMaker.ThrowSmoke(caster.DrawPos, caster.Map, 1.2f);
                            FleckMaker.ThrowHeatGlow(caster.Position, caster.Map, .8f);
                            TakeBody();
                        }
                    }
                    else
                    {
                        Messages.Message("TM_CannotPossessNow".Translate(
                                caster.LabelShort,
                                hitPawn.LabelShort
                            ), MessageTypeDefOf.RejectInput);
                        this.age = this.duration;
                        this.Destroy(DestroyMode.Vanish);
                    }
                }
                else
                {
                    Messages.Message("TM_CannotPossess".Translate(
                                caster.LabelShort,
                                hitThing.LabelShort
                            ), MessageTypeDefOf.RejectInput);
                    this.age = this.duration;                    
                    this.Destroy(DestroyMode.Vanish);
                }
            }
            else
            {
                if (!this.initialized)
                {
                    this.age = this.duration;
                    Destroy(DestroyMode.Vanish);
                }
            }

            if(hitPawn != null && (hitPawn.Downed || hitPawn.Dead))
            {
                this.age = this.duration;
            }
        }

        private void ApplyDisguise()
        {
            removeDisguise=!hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_II);
            if (removeDisguise) HealthUtility.AdjustSeverity(hitPawn,TorannMagicDefOf.TM_DisguiseHD_II,20f+5f*pwrVal);
        }

        private void AttachBody()
        {
            if (caster!=null && !caster.Spawned && !caster.Destroyed && caster.holdingOwner==null &&
                passenger.TryAdd(caster,false) && Find.WorldPawns?.Contains(caster)==true)
                Find.WorldPawns.RemovePawn(caster);
        }

        private void TakeBody()
        {
            takingBody=true;
            try { caster.DeSpawn(); }
            finally
            {
                AttachBody();
                takingBody=false;
                if (!passenger.Contains(caster)) returning=true;
            }
        }

        protected override void Tick()
        {
            age++;
            if (!initialized) { base.Tick(); return; }
            if (returning || age>=duration || hitPawn?.Spawned!=true || hitPawn.Map!=Map || hitPawn.Downed || hitPawn.Dead)
                Destroy();
        }

        private bool DropBody(IntVec3 cell)
        {
            if (cell.GetFirstPawn(Map)!=null || !cell.Standable(Map) || cell.ContainsStaticFire(Map)) return false;
            try { return passenger.TryDrop(caster,cell,Map,ThingPlaceMode.Direct,out _,playDropSound:false) && caster.Spawned; }
            finally { AttachBody(); }
        }

        private bool ReturnBody()
        {
            if (caster==null || caster.Destroyed || caster.Spawned) return true;
            if (!Spawned || Map.Disposed) return false;
            AttachBody();
            if (!passenger.Contains(caster)) return false;
            var origin=oldPosition.InBounds(Map)?oldPosition:Position;
            foreach (var cell in GenRadial.RadialCellsAround(origin,12f,true))
                if (cell.InBounds(Map) && DropBody(cell)) return true;
            foreach (var cell in Map.AllCells) if (DropBody(cell)) return true;
            return false;
        }

        public override void Destroy(DestroyMode mode=DestroyMode.Vanish)
        {
            if (Destroyed || resolvingImpact || takingBody) return;
            if (initialized)
            {
                int tick=Find.TickManager.TicksGame;
                if (returning && tick<retryAt) return;
                returning=true;
                retryAt=tick+60;
                if (!ReturnBody()) return;
                RestoreTarget();
            }
            base.Destroy(mode);
        }

        private void RestoreTarget()
        {
            if (hitPawn==null || hitPawn.Destroyed || caster==null || pFaction==null) return;
            bool foreign=pFaction!=caster.Faction;
            if ((hitPawn.Downed || hitPawn.Dead) && foreign && !pFaction.HostileTo(caster.Faction))
                pFaction.TryAffectGoodwillWith(caster.Faction,-100,true,true,TorannMagicDefOf.TM_OffensiveMagic,hitPawn);
            if (foreign && hitPawn.Faction==caster.Faction)
            {
                if (prisoner) hitPawn.guest?.SetGuestStatus(caster.Faction,GuestStatus.Guest);
                else hitPawn.SetFaction(pFaction);
            }
            int items=(hitPawn.equipment?.PrimaryEq!=null ? 1 : 0) + (hitPawn.inventory?.innerContainer.Count ?? 0) +
                (hitPawn.apparel?.WornApparelCount ?? 0);
            if (items<inventoryCount && foreign && !pFaction.HostileTo(caster.Faction))
            {
                pFaction.TryAffectGoodwillWith(caster.Faction,-200,true,true,TorannMagicDefOf.TM_OffensiveMagic,hitPawn);
                Find.LetterStack.ReceiveLetter("LetterLabelPossessedCaughtStealing".Translate(),
                    "TM_PossessedCaughtStealing".Translate(hitPawn.Faction,hitPawn.LabelShort),LetterDefOf.NegativeEvent);
            }
            if (!caster.IsColonist && hitPawn.IsColonist && hitPawn.workSettings!=null)
            {
                for (int i=0;i<Math.Min(hitPawnWorkSetting?.Count ?? 0,workTypeNames?.Count ?? 0);i++)
                {
                    var type=DefDatabase<WorkTypeDef>.GetNamedSilentFail(workTypeNames[i]);
                    if (type!=null && !hitPawn.WorkTypeIsDisabled(type)) hitPawn.workSettings.SetPriority(type,hitPawnWorkSetting[i]);
                }
                var magic=hitPawn.GetCompAbilityUserMagic();
                if (magic?.IsMagicUser==true) magic.magicPowersInitializedForColonist=true;
            }
            RemoveHediffs();
        }

        public void RemoveHediffs()
        {
            Hediff disguiseHD = null;
            Hediff possessHD = null;
            Hediff possessCHD = null;
            using (IEnumerator<Hediff> enumerator = hitPawn.health.hediffSet.hediffs.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Hediff rec = enumerator.Current;
                    if (rec.def == TorannMagicDefOf.TM_DisguiseHD_II)
                    {
                        disguiseHD = rec;
                    }
                    if (rec.def == TorannMagicDefOf.TM_CoOpPossessionHD || rec.def == TorannMagicDefOf.TM_CoOpPossessionHD_I || rec.def == TorannMagicDefOf.TM_CoOpPossessionHD_II || rec.def == TorannMagicDefOf.TM_CoOpPossessionHD_III)
                    {
                        possessCHD = rec;
                    }
                    if (rec.def == TorannMagicDefOf.TM_PossessionHD || rec.def == TorannMagicDefOf.TM_PossessionHD_I || rec.def == TorannMagicDefOf.TM_PossessionHD_II || rec.def == TorannMagicDefOf.TM_PossessionHD_III)
                    {
                        possessHD = rec;
                    }
                }
            }
            if(removeDisguise && disguiseHD != null)
            {
                this.hitPawn.health.RemoveHediff(disguiseHD);
            }
            if(possessHD != null)
            {
                this.hitPawn.health.RemoveHediff(possessHD);
            }
            if (possessCHD != null)
            {
                this.hitPawn.health.RemoveHediff(possessCHD);
            }
            
        }
    }
}
