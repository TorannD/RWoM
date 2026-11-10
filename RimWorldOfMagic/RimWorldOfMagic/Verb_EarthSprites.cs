using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;
using Verse.Sound;


namespace TorannMagic
{
    public class Verb_EarthSprites : ClassProgression.Verb_SpiritTargetable
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            var map = CasterPawn.Map;
            return map != null && targ.IsValid && targ.Cell.InBounds(map) && !targ.Cell.Fogged(map)
                && (root - targ.Cell).LengthHorizontal < verbProps.range;
        }

        protected override bool TryCastShot()
        {
            ApplyHostedEffect();
            burstShotsLeft = 0;
            return false;
        }

        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body, Ability);
            var comp = owner?.GetCompAbilityUserMagic();
            var map = body?.Map;
            var cell = currentTarget.Cell;
            if (comp == null || map == null || !cell.InBounds(map)) return false;
            int versatility = owner == body ? TM_Calc.GetSkillVersatilityLevel(owner, Ability.Def as TMAbilityDef)
                : ClassProgression.SpiritRebirthRuntime.SavedRank(comp.ExistingMagicData, "TM_EarthSprites_ver");
            if (comp.maxMP < .6f - .07f * versatility)
            {
                Messages.Message("TM_NotEnoughMaxMana".Translate(body.LabelShort, "Earth Sprites"), MessageTypeDefOf.RejectInput);
                return false;
            }
            var building = cell.GetFirstBuilding(map);
            var terrain = cell.GetTerrain(map);
            if (building is Mineable) comp.AssignEarthSprites(map, cell, mining: true);
            else if (building == null && terrain?.defName is "MarshyTerrain" or "Mud" or "Marsh" or "WaterShallow"
                or "Ice" or "Sand" or "Gravel" or "Soil" or "MossyTerrain" or "SoftSand")
                comp.AssignEarthSprites(map, cell, mining: false);
            else if (building == null && terrain != null && terrain.defName.IndexOf("water", StringComparison.OrdinalIgnoreCase) < 0)
                ShatterTerrain(cell, terrain);
            else
            {
                Messages.Message("TM_InvalidTarget".Translate(body.LabelShort, "Earth Sprites"), MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }

        public void ShatterTerrain(IntVec3 center, TerrainDef terrainDef)
        {
            List<IntVec3> cellList = GenRadial.RadialCellsAround(center, 2f, true).ToList();
            Building bldg = null;
            TerrainDef terrain = null;
            for (int i = 0; i < cellList.Count; i++)
            {
                IntVec3 cell = cellList[i];                
                if (!cell.InBounds(CasterPawn.Map)) continue;
                bldg = cell.GetFirstBuilding(this.CasterPawn.Map);
                terrain = cell.GetTerrain(this.CasterPawn.Map);
                if (bldg == null && terrain == terrainDef)
                {
                    this.CasterPawn.Map.terrainGrid.SetTerrain(cell, TerrainDef.Named("Gravel"));
                    FleckMaker.ThrowSmoke(cell.ToVector3Shifted(), this.CasterPawn.Map, Rand.Range(.8f, 1.2f));
                    Vector3 moteDirection = TM_Calc.GetVector(center, cell);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Rubble, cell.ToVector3Shifted(), this.CasterPawn.Map, Rand.Range(.3f, .6f), .2f, .02f, .05f, Rand.Range(-100, 100), Rand.Range(2f, 4f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 0);
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, cell.ToVector3Shifted(), this.CasterPawn.Map, Rand.Range(.9f, 1.2f), .3f, .02f, Rand.Range(.25f, .4f), Rand.Range(-100, 100), Rand.Range(2f, 4f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 0);
                }
                bldg = null;
            }
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_EarthCrack, center.ToVector3Shifted(), this.CasterPawn.Map, Rand.Range(2f, 2.3f), .2f, .25f, 1.5f, 0, 0f, 0f, Rand.Range(0, 360));
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_EarthCrack, center.ToVector3Shifted(), this.CasterPawn.Map, Rand.Range(3f, 3.5f), .2f, .25f, 1.7f, 0, 0f, 0f, Rand.Range(0, 360));
            Find.CameraDriver.shaker.DoShake(5f);
            SoundInfo info = SoundInfo.InMap(new TargetInfo(center, this.CasterPawn.Map, false), MaintenanceType.None);
            info.pitchFactor = .3f;
            info.volumeFactor = 1.9f;
            SoundDef.Named("PunchThroughRoofMetal").PlayOneShot(info);
        }
    }
}
