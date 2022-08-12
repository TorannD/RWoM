using System.Linq;
using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;

namespace TorannMagic
{
    class Projectile_CorpseExplosion : Projectile_AbilityBase
    {
        int age = 300;
        int verVal = 0;
        int pwrVal = 0;
        float arcaneDmg = 1;
        float radius = 3f;
        bool initialized = false;
        Corpse targetCorpse = null;
        Pawn targetPawn = null;

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age > 0;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        public override void Tick()
        {
            base.Tick();
            this.age--;
        }

        public void Initialize()
        {
            radius = 3f + (.3f * (verVal+pwrVal));
            age = age - (60 * verVal);
            this.initialized = true;
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            Pawn pawn = this.launcher as Pawn;

            if (!this.initialized)
            {
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_CorpseExplosion.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_CorpseExplosion_pwr");
                MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_CorpseExplosion.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_CorpseExplosion_ver");
                this.arcaneDmg = comp.arcaneDmg;
                pwrVal = pwr.level;
                verVal = ver.level;
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    MightPowerSkill mpwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                    MightPowerSkill mver = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                    pwrVal = mpwr.level;
                    verVal = mver.level;
                }
                Initialize();

                CellRect cellRect = CellRect.CenteredOn(base.Position, 1);
                cellRect.ClipInsideMap(map);
                IntVec3 curCell = cellRect.CenterCell;
                if (curCell.InBoundsWithNullCheck(map) && curCell.IsValid)
                {
                    Pawn undead = curCell.GetFirstPawn(map);
                    bool flag = undead != null && !undead.Dead;
                    if (flag)
                    {
                        if(TM_Calc.IsUndead(undead))
                        {
                            this.targetPawn = undead;
                        }
                        //if (undead.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD))
                        //{
                        //    this.targetPawn = undead;
                        //}
                        //if (undead.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD))
                        //{
                        //    this.targetPawn = undead;
                        //}
                    }

                    Thing corpseThing = curCell.GetFirstItem(map);
                    Corpse corpse = null;
                    if (corpseThing != null)
                    {
                        bool validator = corpseThing is Corpse;
                        if (validator)
                        {                            
                            corpse = corpseThing as Corpse;
                            Pawn corpsePawn = corpse.InnerPawn;
                            if (corpsePawn.RaceProps.IsFlesh)
                            {
                                if (corpsePawn.RaceProps.Humanlike || corpsePawn.RaceProps.Animal || TM_Calc.IsUndead(corpsePawn))
                                {
                                    this.targetCorpse = corpse;
                                }
                            }
                        }
                    }
                }
            }     
            
            if(this.targetPawn != null && !this.targetPawn.Destroyed)
            {
                if (this.age == 360)
                {
                    MoteMaker.ThrowText(this.targetPawn.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead), map, "6", -.5f);
                }
                if (this.age == 300)
                {
                    MoteMaker.ThrowText(this.targetPawn.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead), map, "5", -.5f);
                }
                if (this.age == 240)
                {
                    MoteMaker.ThrowText(this.targetPawn.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead), map, "4", -.5f);
                }
                if (this.age == 180)
                {
                    MoteMaker.ThrowText(this.targetPawn.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead), map, "3", -.5f);
                }
                if (this.age == 120)
                {
                    MoteMaker.ThrowText(this.targetPawn.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead), map, "2", -.5f);
                }
                if (this.age == 60)
                {
                    MoteMaker.ThrowText(this.targetPawn.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead), map, "1", -.5f);
                }
                if(this.age == 1)
                {
                    //explode
                    TM_MoteMaker.ThrowBloodSquirt(this.targetPawn.Position.ToVector3Shifted(), map, 1.2f);
                    TM_MoteMaker.ThrowBloodSquirt(this.targetPawn.Position.ToVector3Shifted(), map, .6f);
                    TM_MoteMaker.ThrowBloodSquirt(this.targetPawn.Position.ToVector3Shifted(), map, .8f);
                    if (targetPawn.def != TorannMagicDefOf.TM_SkeletonLichR && targetPawn.def != TorannMagicDefOf.TM_GiantSkeletonR)
                    {
                        if (this.targetPawn.RaceProps.Humanlike)
                        {
                            this.targetPawn.inventory.DropAllNearPawn(this.targetPawn.Position, false, true);
                            this.targetPawn.equipment.DropAllEquipment(this.targetPawn.Position, false);
                            this.targetPawn.apparel.DropAll(this.targetPawn.Position, false);
                        }
                        if (!this.targetPawn.Destroyed)
                        {
                            this.targetPawn.Destroy();
                        }
                    }
                    GenExplosion.DoExplosion(this.targetPawn.Position, map, this.radius, TMDamageDefOf.DamageDefOf.TM_CorpseExplosion, this.launcher, Mathf.RoundToInt((Rand.Range(22f, 36f) + (5f * pwrVal)) * this.arcaneDmg), 0, this.def.projectile.soundExplode, def, this.equipmentDef, null, null, 0f, 01, false, null, 0f, 0, 0.0f, true);

                }
            }

            if (this.targetCorpse != null && !this.targetCorpse.Destroyed)
            {
                if (this.age == 360)
                {
                    MoteMaker.ThrowText(this.targetCorpse.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead), map, "6", -.5f);
                }
                if (this.age == 300)
                {
                    MoteMaker.ThrowText(this.targetCorpse.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead), map, "5", -.5f);
                }
                if (this.age == 240)
                {
                    MoteMaker.ThrowText(this.targetCorpse.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead), map, "4", -.5f);
                }
                if (this.age == 180)
                {
                    MoteMaker.ThrowText(this.targetCorpse.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead), map, "3", -.5f);
                }
                if (this.age == 120)
                {
                    MoteMaker.ThrowText(this.targetCorpse.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead), map, "2", -.5f);
                }
                if (this.age == 60)
                {
                    MoteMaker.ThrowText(this.targetCorpse.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead), map, "1", -.5f);
                }
                if (this.age == 1)
                {
                    //explode
                    TM_MoteMaker.ThrowBloodSquirt(this.targetCorpse.Position.ToVector3Shifted(), map, 1.2f);
                    TM_MoteMaker.ThrowBloodSquirt(this.targetCorpse.Position.ToVector3Shifted(), map, .6f);
                    TM_MoteMaker.ThrowBloodSquirt(this.targetCorpse.Position.ToVector3Shifted(), map, .8f);
                    Pawn corpsePawn = this.targetCorpse.InnerPawn;
                    if (corpsePawn.RaceProps.Humanlike)
                    {
                        corpsePawn.inventory.DropAllNearPawn(this.targetCorpse.Position, false, true);
                        corpsePawn.equipment.DropAllEquipment(this.targetCorpse.Position, false);
                        corpsePawn.apparel.DropAll(this.targetCorpse.Position, false);
                    }
                    GenExplosion.DoExplosion(this.targetCorpse.Position, map, this.radius, TMDamageDefOf.DamageDefOf.TM_CorpseExplosion, this.launcher, Mathf.RoundToInt((Rand.Range(18f, 30f) + (5f * pwrVal))*this.arcaneDmg), 0, this.def.projectile.soundExplode, def, this.equipmentDef, null, null, 0f, 01, false, null, 0f, 0, 0.0f, true);
                    if (!this.targetCorpse.Destroyed)
                    {
                        this.targetCorpse.Destroy();
                    }
                }
            }

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", 360, false);
            Scribe_Values.Look<float>(ref this.radius, "radius", 2.4f, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
        }

    }    
}


