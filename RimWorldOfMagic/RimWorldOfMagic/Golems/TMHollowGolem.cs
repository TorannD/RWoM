using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class TMHollowGolem : TMPawnGolem
    {


        public TMHollowGolem()
        {

        }

        public override Vector3 EyeVector
        {
            get
            {
                Vector3 startVec = this.DrawPos;
                if(this.Rotation == Rot4.East)
                {
                    startVec.x += .1f;
                }
                else if(this.Rotation == Rot4.West)
                {
                    startVec.x -= .1f;
                }
                startVec.z += .13f;
                return startVec;
            }
        }

        public Vector3 MoteDrawPos
        {
            get
            {
                Vector3 drawPos = this.DrawPos;
                drawPos.z -= .54f;
                if (this.Rotation == Rot4.East)
                {
                    drawPos.x += Rand.Range(-.2f, 0f);
                }
                else if(this.Rotation == Rot4.West)
                {
                    drawPos.x += Rand.Range(0f, .2f);
                }
                return drawPos;
            }
        }

        public bool shouldDrawHoverMote = true;
        public bool hasSlipStreamUpgrade = false;
        public bool hasDeathCloakUpgrade = false;
        public bool doomFieldHediffEnabled = true;
        public bool deathFieldHediffEnabled = true;
        public override void PostGolemActivate()
        {
            foreach(TM_GolemUpgrade gu in Golem.Upgrades)
            {
                if(gu.golemUpgradeDef.defName == "TM_Golem_HollowCloak" && gu.currentLevel > 0)
                {
                    shouldDrawHoverMote = false;
                }
                if(gu.golemUpgradeDef.defName == "TM_Golem_HollowPGIce" && gu.currentLevel > 0)
                {
                    hasSlipStreamUpgrade = gu.enabled;
                }
                if(gu.golemUpgradeDef.defName == "TM_Golem_HollowPGDeath" && gu.currentLevel > 0)
                {
                    hasDeathCloakUpgrade = gu.enabled;
                }
                if (gu.golemUpgradeDef.defName == "TM_Golem_HollowPGDoom" && gu.currentLevel > 0)
                {
                    doomFieldHediffEnabled = gu.enabled;
                }
                if (gu.golemUpgradeDef.defName == "TM_Golem_HollowFGDeath" && gu.currentLevel > 0)
                {
                    deathFieldHediffEnabled = gu.enabled;
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            if(shouldDrawHoverMote && this.Map != null)
            {
                if (Find.TickManager.TicksGame % 4 == 0 && (this.Rotation == Rot4.West || this.Rotation == Rot4.East))
                {
                    ThingDef rndMote = TorannMagicDefOf.Mote_BlackSmokeLow;
                    TM_MoteMaker.ThrowGenericMote(rndMote, MoteDrawPos, this.Map, Rand.Range(.2f, .4f), .1f, 0f, Rand.Range(.1f, .2f), Rand.Range(-40, 40), Rand.Range(.2f, .3f), Rand.Range(150, 180), Rand.Range(0, 360));
                    TM_MoteMaker.ThrowGenericMote(rndMote, MoteDrawPos, this.Map, Rand.Range(.2f, .4f), .1f, 0f, Rand.Range(.1f, .2f), Rand.Range(-40, 40), Rand.Range(.2f, .3f), Rand.Range(180, 210), Rand.Range(0, 360));
                }
            }
            if(Find.TickManager.TicksGame % 360 == 0)
            {
                if(hasDeathCloakUpgrade && this.Golem.Energy.CurEnergyPercent > this.Golem.minEnergyPctForAbilities)
                {
                    bool reduceEnergy = false;
                    foreach(Pawn p in this.Map.mapPawns.AllPawnsSpawned)
                    {
                        if(!p.Dead && !p.Downed && p.HostileTo(this.Faction) && (p.Position - this.Position).LengthHorizontal <= 10)
                        {
                            HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_DeathMarkCurse, Rand.Range(1f, 2f));
                            TM_MoteMaker.ThrowDiseaseMote(p.DrawPos, p.Map, 1f, .5f, .2f, .4f);
                            reduceEnergy = true;
                        }
                    }
                    if(reduceEnergy)
                    {
                        this.Golem.Energy.SubtractEnergy(10);
                    }
                }
            }
        }

        public override void DrawGolemTick()
        {
            if(Find.TickManager.TicksGame % 7 == 0)
            {
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Casting, this.DrawPos, this.Map, 2.2f, TorannMagicDefOf.Mote_Casting.mote.solidTime, TorannMagicDefOf.Mote_Casting.mote.fadeInTime,
                        TorannMagicDefOf.Mote_Casting.mote.fadeOutTime, Rand.Range(-50, 50),0, 0, Rand.Range(0, 360));
            }
        }        

        public override void Draw()
        {
            base.Draw();
            
            if(this.Rotation != Rot4.North && this.RaceProps.lifeStageAges.FirstOrDefault().def == TorannMagicDefOf.TM_HollowGolemLS)
            {
                string extraDrawPath = "Golems/Hollow/Hollow_LS";
                string directionFacing = "_south";
                if (this.Rotation == Rot4.West)
                {
                    directionFacing = "_west";
                }
                else if (this.Rotation == Rot4.East)
                {
                    directionFacing = "_east";
                }
                extraDrawPath += directionFacing;
                Material mat = MaterialPool.MatFrom(extraDrawPath, ShaderDatabase.Transparent, Color.white);

                Vector3 vector = this.DrawPos;
                vector.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn);
                vector += new Vector3(0, -.11f, 0);

                Vector3 s = new Vector3(TorannMagicDefOf.TM_HollowGolemK.lifeStages.FirstOrDefault().bodyGraphicData.drawSize.x, this.DrawPos.y, TorannMagicDefOf.TM_HollowGolemK.lifeStages.FirstOrDefault().bodyGraphicData.drawSize.y);
                Matrix4x4 matrix = default(Matrix4x4);
                Quaternion q = Quaternion.AngleAxis(0, Vector3.up);

                matrix.SetTRS(vector, q, s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
            }
        }

        
        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            if(hasSlipStreamUpgrade && Rand.Chance(this.Golem.Energy.CurEnergyPercent) && dinfo.Instigator != null)
            {                
                absorbed = true;
                this.Golem.Energy.SubtractEnergy(5);
                Vector3 dir = TM_Calc.GetVector(dinfo.Instigator.DrawPos, this.DrawPos);
                Vector3 rndPos = this.DrawPos;
                rndPos.x += Rand.Range(-.2f, .2f);
                rndPos.z += Rand.Range(-.2f, .2f);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ice, rndPos, this.Map, Rand.Range(.2f, .3f), 0.3f, Rand.Range(.1f, .2f), Rand.Range(.2f, .5f), Rand.Range(-300, 300), Rand.Range(1.2f, 2f), (Quaternion.AngleAxis(-90, Vector3.up) * dir).ToAngleFlat(), Rand.Range(0, 360));
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ice, rndPos, this.Map, Rand.Range(.1f, .2f), 0.3f, Rand.Range(.1f, .2f), Rand.Range(.2f, .5f), Rand.Range(-300, 300), Rand.Range(.8f, 1.2f), (Quaternion.AngleAxis(-90, Vector3.up) * dir).ToAngleFlat(), Rand.Range(0, 360));
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ice, rndPos, this.Map, Rand.Range(.1f, .2f), 0.3f, Rand.Range(.1f, .2f), Rand.Range(.2f, .5f), Rand.Range(-300, 300), Rand.Range(.8f, 1.2f), (Quaternion.AngleAxis(-90, Vector3.up) * dir).ToAngleFlat(), Rand.Range(0, 360));
                TM_MoteMaker.ThrowGenericFleck(FleckDefOf.LightningGlow, rndPos, this.Map, dinfo.Amount / 6f, .2f, .1f, .3f, 0, 0, 0, Rand.Range(0, 360));
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Shadow, rndPos, this.Map, Rand.Range(.1f, .3f), 0.3f, Rand.Range(.1f, .2f), Rand.Range(.2f, .5f), Rand.Range(-300, 300), Rand.Range(.6f, 1f), (Quaternion.AngleAxis(-90, Vector3.up) * dir).ToAngleFlat(), Rand.Range(0, 360));
            }
            base.PreApplyDamage(ref dinfo, out absorbed);
        }
    }
}
