using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace TorannMagic
{
    public class TM_WeatherEvent_MeshFlash : WeatherEvent
    {
        private static List<Mesh> boltMeshes = new List<Mesh>();
        private const int NumBoltMeshesMax = 20;

        private IntVec3 strikeLoc = IntVec3.Invalid;
        private Mesh boltMesh = null;

        Material weatherMeshMat;
        //SkyColorSet weatherSkyColors;
        //private static readonly Material LightningMat = MatLoader.LoadMat("Weather/LightningBolt", -1);

        private Vector2 shadowVector;
        public int duration;
        private int age = 0;
        private DamageDef damageType = TMDamageDefOf.DamageDefOf.TM_Shadow;
        private float averageRadius = 1.75f;
        private int damageAmount = -1;
        private float soundVolume = 1f;
        private float soundPitch = 1f;
        Thing instigator = null;

        private const int FlashFadeInTicks = 3;
        private const int MinFlashDuration = 15;
        private const int MaxFlashDuration = 60;
        private const float FlashShadowDistance = 5f;

        private static readonly SkyColorSet MeshFlashColors = new SkyColorSet(new Color(1.2f, 0.8f, 1.2f), new Color(0.784313738f, 0.8235294f, 0.847058833f), new Color(0.9f, 0.8f, 0.8f), 1.15f);

        public override bool Expired
        {
            get
            {
                return this.age > this.duration;
            }
        }

        public override SkyTarget SkyTarget
        {
            get
            {
                return new SkyTarget(1f, TM_WeatherEvent_MeshFlash.MeshFlashColors, 1f, 1f);
            }
        }

        public override Vector2? OverrideShadowVector
        {
            get
            {
                return new Vector2?(this.shadowVector);
            }
        }

        public override float SkyTargetLerpFactor
        {
            get
            {
                return this.LightningBrightness;
            }
        }

        protected float LightningBrightness
        {
            get
            {
                float result;
                if (this.age <= 3)
                {
                    result = (float)this.age / 3f;
                }
                else
                {
                    result = 1f - (float)this.age / (float)this.duration;
                }
                return result;
            }
        }

        public TM_WeatherEvent_MeshFlash(Map map, IntVec3 forcedStrikeLoc, Material meshMat) : base(map)
		{
            this.weatherMeshMat = meshMat;
            this.strikeLoc = forcedStrikeLoc;
            this.duration = Rand.Range(15, 60);
            this.shadowVector = new Vector2(Rand.Range(-5f, 5f), Rand.Range(-5f, 0f));            
        }

        public TM_WeatherEvent_MeshFlash(Map map, IntVec3 forcedStrikeLoc, Material meshMat, DamageDef dmgType, Thing instigator, int dmgAmt, float averageRad, float _soundVol = 1f, float _soundPitch = 1f) : base(map)
        {
            this.weatherMeshMat = meshMat;
            this.strikeLoc = forcedStrikeLoc;
            this.duration = Rand.Range(15, 60);
            this.shadowVector = new Vector2(Rand.Range(-5f, 5f), Rand.Range(-5f, 0f));
            this.damageType = dmgType;
            this.damageAmount = dmgAmt;
            this.averageRadius = averageRad;
            this.instigator = instigator;
            this.soundVolume = _soundVol;
            this.soundPitch = _soundPitch;
        }

        public override void FireEvent()
        {
            //SoundDefOf.Thunder_OffMap.PlayOneShotOnCamera(this.map);
            if (!this.strikeLoc.IsValid)
            {
                this.strikeLoc = CellFinderLoose.RandomCellWith((IntVec3 sq) => sq.Standable(this.map) && !this.map.roofGrid.Roofed(sq), this.map, 1000);
            }
            this.boltMesh = RandomBoltMesh;
            if (!this.strikeLoc.Fogged(this.map))
            {
                SoundDef exp = TorannMagicDefOf.TM_FireBombSD;
                GenExplosion.DoExplosion(this.strikeLoc, this.map, (Rand.Range(.8f, 1.2f) * this.averageRadius), this.damageType, instigator, this.damageAmount, -1f, exp, null, null, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
                Vector3 loc = this.strikeLoc.ToVector3Shifted();
                for (int i = 0; i < 4; i++)
                {
                    FleckMaker.ThrowSmoke(loc, this.map, 1.3f);
                    FleckMaker.ThrowMicroSparks(loc, this.map);
                    FleckMaker.ThrowLightningGlow(loc, this.map, 1.2f);
                }
            }
            SoundInfo info = SoundInfo.InMap(new TargetInfo(this.strikeLoc, this.map, false), MaintenanceType.None);
            info.volumeFactor = this.soundVolume;
            info.pitchFactor = this.soundPitch;
            TorannMagicDefOf.TM_Thunder_OnMap.PlayOneShot(info);
        }

        public override void WeatherEventDraw()
        {
            Graphics.DrawMesh(this.boltMesh, this.strikeLoc.ToVector3ShiftedWithAltitude(AltitudeLayer.Weather), Quaternion.identity, FadedMaterialPool.FadedVersionOf(weatherMeshMat, this.LightningBrightness), 0);
        }

        public override void WeatherEventTick()
        {
            this.age++;
        }

        public static Mesh RandomBoltMesh
        {
            get
            {
                Mesh result;
                if (TM_WeatherEvent_MeshFlash.boltMeshes.Count < 20)
                {
                    Mesh mesh = TM_MeshMaker.NewBoltMesh(200f, 20);
                    TM_WeatherEvent_MeshFlash.boltMeshes.Add(mesh);
                    result = mesh;
                }
                else
                {
                    result = TM_WeatherEvent_MeshFlash.boltMeshes.RandomElement<Mesh>();
                }
                return result;
            }
        }
    }
}
