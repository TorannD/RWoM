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
    class Verb_SnapFreeze : Verb_UseAbility  
    {
        private float arcaneDmg = 1f;
        List<Pawn> pawns = new List<Pawn>();
        List<Plant> plants = new List<Plant>();

        bool validTarg;

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
            if (map != null)
            {
                CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
                pawns.Clear();
                plants.Clear();
                GenClamor.DoClamor(p, this.UseAbilityProps.TargetAoEProperties.range, ClamorDefOf.Ability);
                Effecter snapeFreezeED = TorannMagicDefOf.TM_SnapFreezeED.Spawn();
                snapeFreezeED.Trigger(new TargetInfo(this.currentTarget.Cell, map, false), new TargetInfo(this.currentTarget.Cell, map, false));
                snapeFreezeED.Cleanup();
                SoundInfo info = SoundInfo.InMap(new TargetInfo(this.currentTarget.Cell, map, false), MaintenanceType.None);
                info.pitchFactor = .4f;
                info.volumeFactor = 1.2f;
                TorannMagicDefOf.TM_WindLowSD.PlayOneShot(info);
                TargetInfo ti = new TargetInfo(this.currentTarget.Cell, map, false);
                TM_MoteMaker.MakeOverlay(ti, TorannMagicDefOf.TM_Mote_PsycastAreaEffect, map, Vector3.zero, 3f, 0f, .1f, .4f, 1.2f, -3f);
                float classBonus = 1f;
                if (p.story != null && p.story.traits != null && p.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost))
                {
                    classBonus = 1.5f;
                }
                if (this.currentTarget != null && p != null && comp != null)
                {
                    this.arcaneDmg = comp.arcaneDmg;
                    this.TargetsAoE.Clear();
                    this.FindTargets();
                    float energy = -125000 * this.arcaneDmg * classBonus;
                    GenTemperature.PushHeat(this.currentTarget.Cell, p.Map, energy);
                    for (int i = 0; i < pawns.Count; i++)
                    {
                        if (!pawns[i].RaceProps.IsMechanoid && pawns[i].RaceProps.body.AllPartsVulnerableToFrostbite.Count > 0)
                        {
                            float distanceModifier = 1f / (pawns[i].Position - currentTarget.Cell).LengthHorizontal;
                            if (distanceModifier > 1f)
                            {
                                distanceModifier = 1f;
                            }
                            int bites = Mathf.RoundToInt(Rand.Range(1f, 5f) * classBonus);
                            for (int j = 0; j < bites; j++)
                            {
                                if (Rand.Chance(TM_Calc.GetSpellSuccessChance(this.CasterPawn, pawns[i], true)) && Rand.Chance(distanceModifier))
                                {
                                    TM_Action.DamageEntities(pawns[i], pawns[i].def.race.body.AllPartsVulnerableToFrostbite.RandomElement(), Rand.Range(10, 20) * distanceModifier, 1f, TMDamageDefOf.DamageDefOf.TM_FreezingWindsDD, p);
                                }
                                if (Rand.Chance(TM_Calc.GetSpellSuccessChance(this.CasterPawn, pawns[i], true)) && Rand.Chance(distanceModifier))
                                {
                                    HealthUtility.AdjustSeverity(pawns[i], HediffDefOf.Hypothermia, distanceModifier / 5f);
                                }
                            }
                        }
                    }
                    for (int i = 0; i < plants.Count; i++)
                    {
                        float distanceModifier = 1f / (plants[i].Position - currentTarget.Cell).LengthHorizontal;
                        if (distanceModifier > 1f)
                        {
                            distanceModifier = 1f;
                        }
                        if (plants[i].def.plant != null && plants[i].def.plant.IsTree)
                        {
                            if (Rand.Chance(distanceModifier / 2f))
                            {
                                plants[i].MakeLeafless(Plant.LeaflessCause.Cold);
                            }
                        }
                        else
                        {
                            if (Rand.Chance(distanceModifier))
                            {
                                plants[i].MakeLeafless(Plant.LeaflessCause.Cold);
                            }
                        }
                        plants[i].Notify_ColorChanged();
                    }
                    List<IntVec3> cellList = GenRadial.RadialCellsAround(this.currentTarget.Cell, this.UseAbilityProps.TargetAoEProperties.range, true).ToList();
                    if (cellList != null && cellList.Count > 0 && map.weatherManager != null)
                    {
                        bool raining = map.weatherManager.RainRate > 0f || map.weatherManager.SnowRate > 0f;
                        for (int i = 0; i < cellList.Count; i++)
                        {
                            cellList[i] = cellList[i].ClampInsideMap(map);
                            SnowUtility.AddSnowRadial(cellList[i], map, 2.4f, Rand.Range(.08f, .13f));
                            TM_MoteMaker.ThrowGenericFleck(FleckDefOf.AirPuff, cellList[i].ToVector3Shifted(), map, 2.5f, .05f, .05f, Rand.Range(2f, 3f), Rand.Range(-60, 60), .5f, -70, Rand.Range(0, 360));
                        }
                        List<IntVec3> windList = GenRadial.RadialCellsAround(this.currentTarget.Cell, this.UseAbilityProps.TargetAoEProperties.range + 1, true).Except(cellList).ToList();
                        for (int i = 0; i < windList.Count; i++)
                        {
                            windList[i] = windList[i].ClampInsideMap(map);
                            Vector3 angle = TM_Calc.GetVector(windList[i], this.currentTarget.Cell);
                            TM_MoteMaker.ThrowGenericFleck(FleckDefOf.AirPuff, windList[i].ToVector3Shifted(), map, Rand.Range(1.2f, 2f), .45f, Rand.Range(0f, .25f), .5f, -200, Rand.Range(3, 5), (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(), Rand.Range(0, 360));
                        }
                    }
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

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i] is Pawn)
                {
                    pawns.Add(list[i] as Pawn);
                }
                if (list[i] != null && list[i] is Plant)
                {
                    plants.Add(list[i] as Plant);
                }
                if (list[i].def == ThingDefOf.Fire || list[i].def == ThingDefOf.Spark)
                {
                    list[i].Destroy(DestroyMode.Vanish);
                }
            }

            list.Clear();
        }
    }
}
