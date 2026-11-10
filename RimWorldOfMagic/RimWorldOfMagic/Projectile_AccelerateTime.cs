using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using RimWorld;
using HarmonyLib;

namespace TorannMagic
{
    public class Projectile_AccelerateTime : Projectile_AbilityBase
    {
        private int verVal;
        private int pwrVal;
        private float arcaneDmg = 1f;
        private float penetration;
        private Pawn projectedOwner;
        private bool resolved;

        internal void PrepareHosted(MagicAbility power)
        {
            projectedOwner = power.Pawn;
            var data = power.MagicUser.ExistingMagicData;
            verVal = ClassProgression.SpiritRebirthRuntime.SavedRank(data,"TM_AccelerateTime_ver");
            pwrVal = ClassProgression.SpiritRebirthRuntime.SavedRank(data,"TM_AccelerateTime_pwr");
            arcaneDmg = power.MagicUser.arcaneDmg;
            penetration = TM_Calc.GetSpellPenetration(projectedOwner);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref projectedOwner,"TM_accelerationOwner");
            Scribe_Values.Look(ref resolved,"TM_accelerationResolved");
            Scribe_Values.Look(ref verVal,"verVal");
            Scribe_Values.Look(ref pwrVal,"pwrVal");
            Scribe_Values.Look(ref arcaneDmg,"TM_accelerationPotency",1f);
            Scribe_Values.Look(ref penetration,"TM_accelerationPenetration");
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            try
            {
                if (resolved || blockedByShield || Map == null || launcher is not Pawn body) return;
                resolved = true;
                if (projectedOwner == null)
                {
                    var magic = body.GetCompAbilityUserMagic();
                    if (magic == null) return;
                    var ranks = magic.MagicData.MagicPowerSkill_AccelerateTime;
                    pwrVal = ranks.FirstOrDefault(s => s.label == "TM_AccelerateTime_pwr")?.level ?? 0;
                    verVal = ranks.FirstOrDefault(s => s.label == "TM_AccelerateTime_ver")?.level ?? 0;
                    if (body.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true)
                    {
                        var mimic = body.GetCompAbilityUserMight()?.MightData.MightPowerSkill_Mimic;
                        pwrVal = mimic?.FirstOrDefault(s => s.label == "TM_Mimic_pwr")?.level ?? 0;
                        verVal = mimic?.FirstOrDefault(s => s.label == "TM_Mimic_ver")?.level ?? 0;
                    }
                    arcaneDmg = magic.arcaneDmg;
                    penetration = TM_Calc.GetSpellPenetration(body);
                    if (ModOptions.Settings.Instance.AIHardMode && !body.IsColonist) pwrVal = verVal = 3;
                }
                var target = intendedTarget.Thing;
                if (target != null && (!target.Spawned || target.Map != Map || target.Position != Position)) return;
                if (target?.def.category == ThingCategory.Item)
                {
                    if (CanAgeItem(target)) AgeThing(target);
                }
                else if (target is Pawn friend && friend.Faction != null && friend.Faction == body.Faction)
                    AgePawn(friend,Mathf.RoundToInt(60000 * (1 + .1f * verVal)),false);
                else AgeArea(body);
                var effect = TorannMagicDefOf.TM_TimeAccelerationAreaEffecter.Spawn();
                try { effect.Trigger(this,this); }
                finally { effect.Cleanup(); }
            }
            finally { if (!Destroyed) Destroy(DestroyMode.Vanish); }
        }

        internal static bool CanAgeItem(Thing thing) => thing.def.useHitPoints && thing is not Corpse &&
            (thing.def.MadeFromStuff || thing.TryGetComp<CompQuality>() != null);

        private void AgeArea(Pawn body)
        {
            float radius = def.projectile.explosionRadius;
            foreach (var target in TM_Calc.FindAllPawnsAround(Map,Position,radius,body.Faction,false)?.Take(2 + verVal) ?? Enumerable.Empty<Pawn>())
            {
                if (target.Faction != null && target.Faction != body.Faction && body.Faction != null && !target.Faction.HostileTo(body.Faction))
                    target.Faction.TryAffectGoodwillWith(body.Faction,-25);
                AgePawn(target,Mathf.RoundToInt(2500 * (1 + .1f * verVal)),true);
                if (target.IsColonist && !body.IsColonist) TM_Action.SpellAffectedPlayerWarning(target);
            }
            foreach (var cell in GenRadial.RadialCellsAround(Position,radius,true))
            {
                if (!cell.InBounds(Map)) continue;
                foreach (var thing in cell.GetThingList(Map))
                {
                    if (thing is Plant plant && plant.def.plant.growDays > 0)
                        plant.Growth += Rand.Range(2 + pwrVal,4 + pwrVal) * arcaneDmg / plant.def.plant.growDays;
                    if (thing.TryGetComp<CompHatcher>() is CompHatcher hatcher)
                    {
                        var progress = Traverse.Create(hatcher).Field("gestateProgress");
                        progress.SetValue((progress.GetValue<float>() + Rand.Range(.3f + .1f * pwrVal,.7f + .1f * pwrVal)) * arcaneDmg);
                    }
                }
            }
        }

        private void AgePawn(Pawn pawn, int duration, bool isBad)
        {
            duration = Mathf.RoundToInt(duration * this.arcaneDmg);
            if (pawn != null && !pawn.DestroyedOrNull() && !pawn.Dead && pawn.health != null && pawn.health.hediffSet != null)
            {
                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ReverseTimeHD))
                {
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ReverseTimeHD);
                    pawn.health.RemoveHediff(hediff);
                }
                else
                {
                    if (Rand.Chance((.5f + verVal) * (1f + penetration - TM_Calc.GetArcaneResistance(pawn,false))))
                    {
                        HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_AccelerateTimeHD, .5f + pwrVal);
                        HediffComp_AccelerateTime hediffComp = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_AccelerateTimeHD, false).TryGetComp<HediffComp_AccelerateTime>();
                        if (hediffComp != null)
                        {
                            hediffComp.durationTicks = (duration);
                            hediffComp.isBad = isBad;
                            AccelerateEffects(pawn, 1);
                        }
                    }
                    else
                    {
                        if (pawn.Map != null)
                        {
                            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TM_ResistedSpell".Translate(), -1);
                        }
                    }
                }
            }
        }

        private void AgeThing(Thing thing)
        {

            thing.HitPoints -= Mathf.RoundToInt((200 + (100 * pwrVal)) * this.arcaneDmg);
            if (thing.HitPoints <= 0)
            {
                float fraction = .5f + .1f * pwrVal;
                void Recover(ThingDef material,int cost)
                {
                    int count = Mathf.RoundToInt(cost * fraction);
                    if (material == null || count <= 0) return;
                    var recovered = ThingMaker.MakeThing(material);
                    recovered.stackCount = count;
                    GenPlace.TryPlaceThing(recovered,thing.Position,Map,ThingPlaceMode.Near);
                }
                if (thing.def.costList != null)
                    foreach (var ingredient in thing.def.costList) Recover(ingredient.thingDef, ingredient.count);
                if (thing.def.MadeFromStuff) Recover(thing.Stuff, thing.def.costStuffCount);
                TransmutateEffects(thing.Position, 6 );
                thing.Destroy(DestroyMode.Vanish);                
            }            
        }

        public void TransmutateEffects(IntVec3 position, int intensity)
        {
            Vector3 rndPos = position.ToVector3Shifted();
            FleckMaker.ThrowHeatGlow(position, this.Map, 1f);
            for (int i = 0; i < intensity; i++)
            {
                rndPos.x += Rand.Range(-.5f, .5f);
                rndPos.z += Rand.Range(-.5f, .5f);
                rndPos.y += Rand.Range(.3f, 1.3f);
                FleckMaker.ThrowSmoke(rndPos, this.Map, Rand.Range(.7f, 1.1f));
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Shadow, position.ToVector3(), this.Map, Rand.Range(.8f, 1.2f), .1f, .1f, .4f, Rand.RangeInclusive((int)-4, (int)4) * 100, Rand.Range(0, 1), Rand.Range(0, 360), Rand.Range(0, 360));
            }
        }

        public void AccelerateEffects(Pawn pawn, int intensity)
        {
            Effecter AccelEffect = TorannMagicDefOf.TM_TimeAccelerationEffecter.Spawn();
            AccelEffect.Trigger(new TargetInfo(pawn), new TargetInfo(pawn));
            AccelEffect.Cleanup();
        }
    }    
}