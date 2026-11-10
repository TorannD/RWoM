using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using RimWorld;

namespace TorannMagic
{
    class Projectile_ChronostaticField : Projectile_AbilityBase
    {
        private Pawn casterPawn;
        private Pawn projectedOwner;
        private int pwrVal, verVal;
        private float arcaneDmg = 1f, penetration;
        private bool resolved;

        internal void PrepareHosted(MagicAbility power)
        {
            projectedOwner = power.Pawn;
            var data = power.MagicUser.ExistingMagicData;
            pwrVal = ClassProgression.SpiritRebirthRuntime.SavedRank(data,"TM_ChronostaticField_pwr");
            verVal = ClassProgression.SpiritRebirthRuntime.SavedRank(data,"TM_ChronostaticField_ver");
            arcaneDmg = power.MagicUser.arcaneDmg;
            penetration = TM_Calc.GetSpellPenetration(projectedOwner);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref projectedOwner,"TM_stasisOwner");
            Scribe_Values.Look(ref pwrVal,"pwrVal");
            Scribe_Values.Look(ref verVal,"verVal");
            Scribe_Values.Look(ref arcaneDmg,"TM_stasisPotency",1f);
            Scribe_Values.Look(ref penetration,"TM_stasisPenetration");
            Scribe_Values.Look(ref resolved,"TM_stasisResolved");
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            try
            {
                if (resolved || blockedByShield || Map == null || launcher is not Pawn physical) return;
                resolved = true;
                casterPawn = physical;
                if (projectedOwner == null)
                {
                    var magic = physical.GetCompAbilityUserMagic();
                    if (magic == null) return;
                    var ranks = magic.MagicData.MagicPowerSkill_ChronostaticField;
                    pwrVal = ranks.FirstOrDefault(s=>s.label=="TM_ChronostaticField_pwr")?.level ?? 0;
                    verVal = ranks.FirstOrDefault(s=>s.label=="TM_ChronostaticField_ver")?.level ?? 0;
                    if (physical.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true)
                    {
                        var mimic = physical.GetCompAbilityUserMight()?.MightData.MightPowerSkill_Mimic;
                        pwrVal = mimic?.FirstOrDefault(s=>s.label=="TM_Mimic_pwr")?.level ?? 0;
                        verVal = mimic?.FirstOrDefault(s=>s.label=="TM_Mimic_ver")?.level ?? 0;
                    }
                    if (ModOptions.Settings.Instance.AIHardMode && !physical.IsColonist) pwrVal = verVal = 3;
                    arcaneDmg = magic.arcaneDmg;
                    penetration = TM_Calc.GetSpellPenetration(physical);
                }
                // Taking a pawn off the map mutates the cell's thing list.
                foreach (var cell in GenRadial.RadialCellsAround(Position,def.projectile.explosionRadius,true))
                {
                    if (!cell.InBounds(Map)) continue;
                    foreach (var pawn in cell.GetThingList(Map).OfType<Pawn>().ToArray())
                    {
                        RemoveFireAt(cell);
                        if (Rand.Chance((1f + penetration - TM_Calc.GetArcaneResistance(pawn,false)) * (.6f + .1f * verVal)))
                        {
                            var north = cell + IntVec3.North;
                            LaunchFlyingObect(north.InBounds(Map) ? north : cell,pawn,1,
                                Mathf.RoundToInt(Rand.Range(1400,1800) * (1f + .2f * pwrVal) * arcaneDmg));
                        }
                        else MoteMaker.ThrowText(pawn.DrawPos,Map,"TM_ResistedSpell".Translate(),-1);
                    }
                }
            }
            finally { if (!Destroyed) Destroy(); }
        }

        public void LaunchFlyingObect(IntVec3 targetCell, Pawn pawn, int force, int duration)
        {
            bool flag = targetCell.IsValid && targetCell.InBounds(Map);
            if (flag)
            {
                if (pawn != null && pawn.Position.IsValid && pawn.Spawned && pawn.Map != null && !pawn.Downed && !pawn.Dead)
                {
                    if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                    {
                        ModCheck.GiddyUp.ForceDismount(pawn);
                    }
                    FlyingObject_TimeDelay flyingObject = (FlyingObject_TimeDelay)GenSpawn.Spawn(ThingDef.Named("FlyingObject_TimeDelay"), pawn.Position, pawn.Map);
                    flyingObject.speed = .01f;
                    flyingObject.duration = duration;
                    flyingObject.Launch(this.casterPawn, targetCell, pawn);
                }
            }
        }
        
        private void RemoveFireAt(IntVec3 position)
        {            
            List<Thing> thingList = position.GetThingList(this.Map);
            if (thingList != null && thingList.Count > 0)
            {
                for (int i = 0; i < thingList.Count; i++)
                {
                    if(thingList[i].def == ThingDefOf.Fire)
                    {
                        //Log.Message("removing fire at " + position);
                        FleckMaker.ThrowHeatGlow(position, this.Map, .6f);
                        thingList[i].Destroy(DestroyMode.Vanish);
                        i--;
                    }
                }
            }
        }
    }    
}