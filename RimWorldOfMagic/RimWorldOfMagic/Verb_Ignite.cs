using RimWorld;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Verse.Sound;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class Verb_Ignite : Verb_UseAbility  
    {
        private float arcaneDmg = 1f;
        bool validTarg;

        List<Corpse> corpses = new List<Corpse>();
        List<Pawn> pawns = new List<Pawn>();
        List<Plant> plants = new List<Plant>();

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    //out of range
                    validTarg = true;
                }
                else
                {
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot()
        {
            Pawn p = this.CasterPawn;
            Map map = this.CasterPawn.Map;
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
            corpses.Clear();
            pawns.Clear();
            plants.Clear();
            GenClamor.DoClamor(p, this.UseAbilityProps.TargetAoEProperties.range, ClamorDefOf.Ability);
            Effecter igniteED = TorannMagicDefOf.TM_IgniteED.Spawn();
            igniteED.Trigger(new TargetInfo(this.currentTarget.Cell, map, false), new TargetInfo(this.currentTarget.Cell, map, false));
            igniteED.Cleanup();
            SoundInfo info = SoundInfo.InMap(new TargetInfo(this.currentTarget.Cell, map, false), MaintenanceType.None);            
            info.pitchFactor = 1.1f;
            info.volumeFactor = 1.8f;
            TorannMagicDefOf.TM_FireWooshSD.PlayOneShot(info);
            TargetInfo ti = new TargetInfo(this.currentTarget.Cell, map, false);            
            TM_MoteMaker.MakeOverlay(ti, TorannMagicDefOf.TM_Mote_PsycastAreaEffect, map, Vector3.zero, .2f, 0f, .1f, .4f, .4f, 4.3f);
            float classBonus = 1f;
            if (p.story != null && p.story.traits != null && p.story.traits.HasTrait(TorannMagicDefOf.InnerFire))
            {
                classBonus = 1.5f;
            }
            if (this.currentTarget != null && p != null && comp != null)
            {
                this.arcaneDmg = comp.arcaneDmg;                
                this.TargetsAoE.Clear();
                this.FindTargets();
                float energy = 200000 * this.arcaneDmg * classBonus;
                GenTemperature.PushHeat(this.currentTarget.Cell, p.Map, energy);
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                for (int i = 0; i < pawns.Count; i++)
                {
                    if (!pawns[i].RaceProps.IsMechanoid)
                    {
                        float distanceModifier = (classBonus) / (pawns[i].Position - currentTarget.Cell).LengthHorizontal;
                        if (distanceModifier > 1f)
                        {
                            distanceModifier = 1f;
                        }
                        if (Rand.Chance(TM_Calc.GetSpellSuccessChance(this.CasterPawn, pawns[i], true)) && Rand.Chance(distanceModifier))
                        {
                            pawns[i].TryAttachFire(Rand.Range(distanceModifier / 2f, distanceModifier));
                        }
                    }
                }
                for (int i = 0; i < corpses.Count; i++)
                {
                    //if (corpses[i].CanEverAttachFire())
                    //{
                    float distanceModifier = 1f / (corpses[i].Position - currentTarget.Cell).LengthHorizontal;
                    //    corpses[i].TryAttachFire(Rand.Range(distanceModifier / 2f, distanceModifier));
                        FireUtility.TryStartFireIn(corpses[i].Position, map, Rand.Range(distanceModifier / 2f, distanceModifier));
                    //}
                }
                for (int i = 0; i < plants.Count; i++)
                {
                    float distanceModifier = 1f / (plants[i].Position - currentTarget.Cell).LengthHorizontal;
                    if (distanceModifier > 1f)
                    {
                        distanceModifier = 1f;
                    }
                    if (plants[i].def.plant.IsTree)
                    {
                        if (Rand.Chance(distanceModifier/2f))
                        {
                            plants[i].TryAttachFire(Rand.Range(distanceModifier / 3f, distanceModifier/2f));
                            FireUtility.TryStartFireIn(plants[i].Position, map, Rand.Range(distanceModifier / 3f, distanceModifier / 2f));
                        }
                    }
                    else
                    {
                        if(Rand.Chance(distanceModifier))
                        {
                            plants[i].TryAttachFire(Rand.Range(distanceModifier / 2f, distanceModifier));
                            FireUtility.TryStartFireIn(plants[i].Position, map, Rand.Range(distanceModifier / 2f, distanceModifier));
                        }
                    }                    
                }
                List<IntVec3> cellList = GenRadial.RadialCellsAround(this.currentTarget.Cell, this.UseAbilityProps.TargetAoEProperties.range, true).ToList();
                bool raining = map.weatherManager.RainRate > 0f || map.weatherManager.SnowRate > 0f;
                for(int i = 0; i< cellList.Count; i++)
                {
                    cellList[i] = cellList[i].ClampInsideMap(map);
                    if(cellList[i].GetSnowDepth(map) > 0f)
                    {
                        map.snowGrid.SetDepth(cellList[i], 0f);
                        FleckMaker.ThrowSmoke(cellList[i].ToVector3Shifted(), map, Rand.Range(.8f, 1.6f));
                        Thing smoke = ThingMaker.MakeThing(TorannMagicDefOf.Mote_Base_Smoke, null);
                        GenSpawn.Spawn(smoke, cellList[i], map, WipeMode.Vanish);
                    }
                    else if(raining || cellList[i].GetTerrain(map).IsWater)
                    {
                        Thing smoke = ThingMaker.MakeThing(TorannMagicDefOf.Mote_Base_Smoke, null);
                        GenSpawn.Spawn(smoke, cellList[i], map, WipeMode.Vanish);
                    }
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.AirPuff, cellList[i].ToVector3Shifted(), map, 2.5f, .05f, .05f, Rand.Range(2f,3f), Rand.Range(-60, 60), .5f, -70, Rand.Range(0, 360));
                }
            }

            this.burstShotsLeft = 0;
            return true;
        }


        private void FindTargets()
        {

            IntVec3 aoeStartPosition = this.currentTarget.Cell;
            int radius = this.UseAbilityProps.TargetAoEProperties.range;

            List<Thing> list = new List<Thing>();            
            
            bool flag4 = !this.UseAbilityProps.TargetAoEProperties.friendlyFire;

            list = (from x in this.caster.Map.listerThings.AllThings
                    where x.Position.InHorDistOf(aoeStartPosition, (float)radius)
                    select x).ToList<Thing>();

            for(int i = 0; i < list.Count; i++)
            {
                if(list[i] != null && list[i] is Pawn)
                {
                    pawns.Add(list[i] as Pawn);
                }
                if(list[i] != null && list[i] is Plant)
                {
                    plants.Add(list[i] as Plant);
                }
                if(list[i] != null && list[i] is Corpse)
                {
                    corpses.Add(list[i] as Corpse);
                }
            }

            list.Clear();                      
        }
    }
}
