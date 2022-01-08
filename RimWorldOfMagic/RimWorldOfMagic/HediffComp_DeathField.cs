using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using UnityEngine;
using TorannMagic.Golems;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_DeathField : HediffComp
    {

        int nextTick = 0;
        bool shouldRemove = false;
        public bool shouldStrike = false;
        private int lastStrikeTick = 0;
        private int strikeDelay = 6;

        Pawn target = null;

        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }


        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned)
            {
                
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            bool flag = base.Pawn.DestroyedOrNull();
            if (!flag)
            {
                if (base.Pawn.Spawned)
                {                    
                    if ((Find.TickManager.TicksGame > nextTick || shouldStrike) && !base.Pawn.Dead && Pawn.Map != null)
                    {
                        TMHollowGolem caster = Pawn as TMHollowGolem;
                        nextTick = Find.TickManager.TicksGame + Rand.Range(300, 600);
                        if (caster != null && caster.deathFieldHediffEnabled && caster.Golem.Energy.CurEnergyPercent >= caster.Golem.minEnergyPctForAbilities)
                        {
                            Pawn target = TM_Calc.FindNearbyEnemy(Pawn, 10);
                            if (target != null)
                            {
                                shouldStrike = false;
                                TryDeathFieldStrike(caster, target);
                            }
                        }
                    }
                } 
            }
            base.CompPostTick(ref severityAdjustment);
        }

        public void TryDeathFieldStrike(TMPawnGolem caster, Pawn target)
        {            
            int rndCount = Rand.RangeInclusive(3, 5);
            for (int i = 0; i < rndCount; i++)
            {
                TM_Action.DamageEntities(target, null, 8f, TMDamageDefOf.DamageDefOf.TM_DecayDD, caster);
                float range = Vector3.Distance(target.DrawPos, this.Pawn.DrawPos);
                Vector3 dir = TM_Calc.GetVector(caster.Position, target.Position);
                float angle = (Quaternion.AngleAxis(90, Vector3.up) * dir).ToAngleFlat();
                Mesh mesh = RandomBoltMesh(range);
                GolemDrawClass gdc = new GolemDrawClass(TM_MatPool.deathbeam, mesh, caster.Position, angle, Find.TickManager.TicksGame + Rand.RangeInclusive(-4,4), Rand.Range(12, 18));
                caster.drawTempMesh.Add(gdc);
                //Pawn.Map.weatherManager.eventHandler.AddEvent(new TM_WeatherEvent_MeshGeneric(Pawn.Map, TM_MatPool.deathlightning3, Pawn.Position, target.Position, 2f, AltitudeLayer.MoteLow, Rand.Range(8, 12), Rand.Range(25, 35), Rand.Range(2, 5)));
            }

            if (caster != null && caster.Golem != null && caster.Golem.Energy != null)
            {
                caster.Golem.Energy.SubtractEnergy(20);
            }
            //Vector3 dir = TM_Calc.GetVector(caster.Position, target.Position);
            //float angle = (Quaternion.AngleAxis(90, Vector3.up) * dir).ToAngleFlat();
            //float range = (caster.Position - target.Position).LengthHorizontal;
            //Mesh mesh = RandomBoltMesh(range);
            //DrawBolt(mesh, TM_MatPool.standardLightning, pos, angle, GetFadedBrightness);

            SoundInfo info = SoundInfo.InMap(new TargetInfo(Pawn.Position, Pawn.Map, false), MaintenanceType.None);
            info.pitchFactor = 1.8f;
            info.volumeFactor = .6f;
            TorannMagicDefOf.TM_Lightning.PlayOneShot(info);
        }

        public Mesh RandomBoltMesh(float range)
        {
            return TM_MeshMaker.NewBoltMesh(range, 5);
        }

        public override bool CompShouldRemove => base.CompShouldRemove || shouldRemove;
    }
}
