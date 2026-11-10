using System.Linq;
using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;

namespace TorannMagic
{
    class Projectile_CorpseExplosion : ClassProgression.Projectile_SpiritImpact
    {
        int age = 300;
        protected override string SkillStem => "TM_CorpseExplosion";
        protected override System.Collections.Generic.List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_CorpseExplosion;
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

        protected override void Tick()
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

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing, blockedByShield);
            if (Destroyed || map == null) return;
            ThingDef def = this.def;
            if (!initialized)
            {
                if (!projected)
                {
                    var pawn = SourcePawn;
                    var comp = pawn?.GetCompAbilityUserMagic();
                    if (comp?.MagicData == null) { age = 0; Destroy(); return; }
                    var skills = Skills(comp.MagicData);
                    pwrVal = SpellRank(pawn, skills, SkillStem, "_pwr");
                    verVal = SpellRank(pawn, skills, SkillStem, "_ver");
                    arcaneDmg = comp.arcaneDmg;
                }
                Initialize();
                if (Position.InBounds(map))
                {
                    var things = Position.GetThingList(map);
                    targetPawn = things.OfType<Pawn>().FirstOrDefault(p => !p.Dead && TM_Calc.IsUndead(p));
                    targetCorpse = things.OfType<Corpse>().FirstOrDefault(ValidCorpse);
                }
            }

            if (targetPawn?.Spawned == true && targetPawn.Map == map)
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
                    IntVec3 position = targetPawn.Position;
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
                    GenExplosion.DoExplosion(position, map, this.radius, TMDamageDefOf.DamageDefOf.TM_CorpseExplosion, SpellInstigator, Mathf.RoundToInt((Rand.Range(22f, 36f) + (5f * pwrVal)) * this.arcaneDmg), 0, this.def.projectile.soundExplode, def, this.equipmentDef, null, null, 0f, 01, null, null, 0, false, null, 0f, 0, 0.0f, true);

                }
            }

            if (targetCorpse?.Spawned == true && targetCorpse.Map == map)
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
                    GenExplosion.DoExplosion(this.targetCorpse.Position, map, this.radius, TMDamageDefOf.DamageDefOf.TM_CorpseExplosion, SpellInstigator, Mathf.RoundToInt((Rand.Range(18f, 30f) + (5f * pwrVal))*this.arcaneDmg), 0, this.def.projectile.soundExplode, def, this.equipmentDef, null, null, 0f, 01, null, null, 0, false, null, 0f, 0, 0.0f, true);
                    if (!this.targetCorpse.Destroyed)
                    {
                        this.targetCorpse.Destroy();
                    }
                }
            }

        }

        private static bool ValidCorpse(Corpse corpse)
        {
            var pawn = corpse.InnerPawn;
            return pawn != null && pawn.RaceProps.IsFlesh &&
                (pawn.RaceProps.Humanlike || pawn.RaceProps.Animal || TM_Calc.IsUndead(pawn));
        }

        internal static bool HasTarget(Map map, IntVec3 cell) => cell.GetThingList(map).Any(t =>
            t is Corpse corpse && ValidCorpse(corpse) || t is Pawn p && !p.Dead && TM_Calc.IsUndead(p));

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref targetPawn, "TM_armedUndead");
            Scribe_References.Look(ref targetCorpse, "TM_armedCorpse");
            Scribe_Values.Look(ref arcaneDmg, "TM_explosionScale", 1f);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", 360, false);
            Scribe_Values.Look<float>(ref this.radius, "radius", 2.4f, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
        }

    }    
}


