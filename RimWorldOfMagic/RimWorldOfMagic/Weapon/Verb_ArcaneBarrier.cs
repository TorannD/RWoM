using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;


namespace TorannMagic.Weapon
{
    public class Verb_ArcaneBarrier : Verb_UseAbility
    {
        float xProbL = 0;
        float xProbR = 0;

        private IntVec3 currentPosL = IntVec3.Invalid;
        private IntVec3 currentPosR = IntVec3.Invalid;

        bool validTarg;
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == this.caster)
            {
                return this.verbProps.targetParams.canTargetSelf;
            }
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    ShootLine shootLine;
                    validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
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
            Map map = base.CasterPawn.Map;

            IntVec3 destinationRPos = this.currentTarget.Cell;
            IntVec3 destinationLPos = this.currentTarget.Cell;
            currentPosL = this.currentTarget.Cell;
            currentPosR = this.currentTarget.Cell;
            IntVec3 angleVec = (this.currentTarget.Cell - this.CasterPawn.Position).RotatedBy(Rot4.FromAngleFlat(90));
            destinationRPos.x += angleVec.x;
            destinationRPos.z += angleVec.z;
            xProbR = CalculateAngles(this.currentTarget.Cell, destinationRPos);
            angleVec = (this.currentTarget.Cell - this.CasterPawn.Position).RotatedBy(Rot4.FromAngleFlat(-90));
            destinationLPos.x += angleVec.x;
            destinationLPos.z += angleVec.z;
            xProbL = CalculateAngles(this.currentTarget.Cell, destinationLPos);

            AbilityUser.SpawnThings tempPod = new SpawnThings();
            tempPod.def = ThingDef.Named("TM_ArcaneBarrier");
            tempPod.spawnCount = 1;

            SingleSpawnLoop(tempPod, this.currentTarget.Cell, this.CasterPawn.Map);
            FleckMaker.ThrowHeatGlow(this.currentTarget.Cell, map, 1f);

            for (int i = 0; i < 5; i++)
            {
                currentPosR = GetNewPos(currentPosR, this.currentTarget.Cell.x <= destinationRPos.x, this.currentTarget.Cell.z <= destinationRPos.z, false, 0, 0, xProbR, 1 - xProbR);
                if(currentPosR.IsValid && currentPosR.InBoundsWithNullCheck(this.CasterPawn.Map) && !currentPosR.Impassable(this.CasterPawn.Map) && this.currentPosR.Walkable(this.CasterPawn.Map))
                {
                    bool flag = true;
                    foreach (Thing current in currentPosR.GetThingList(this.CasterPawn.Map))
                    {
                        if(current.def.altitudeLayer == AltitudeLayer.Building || current.def.altitudeLayer == AltitudeLayer.Item || current.def.altitudeLayer == AltitudeLayer.ItemImportant)
                        {
                            flag = false;
                        }
                    }
                    if (flag)
                    {
                        SingleSpawnLoop(tempPod, currentPosR, this.CasterPawn.Map);
                        FleckMaker.ThrowHeatGlow(currentPosR, map, .6f);
                    }
                }
                currentPosL = GetNewPos(currentPosL, this.currentTarget.Cell.x <= destinationLPos.x, this.currentTarget.Cell.z <= destinationLPos.z, false, 0, 0, xProbL, 1 - xProbL);
                if (currentPosL.IsValid && currentPosL.InBoundsWithNullCheck(this.CasterPawn.Map) && !currentPosL.Impassable(this.CasterPawn.Map) && this.currentPosL.Walkable(this.CasterPawn.Map))
                {
                    bool flag = true;
                    foreach (Thing current in currentPosL.GetThingList(this.CasterPawn.Map))
                    {
                        if (current.def.altitudeLayer == AltitudeLayer.Building || current.def.altitudeLayer == AltitudeLayer.Item || current.def.altitudeLayer == AltitudeLayer.ItemImportant)
                        {
                            flag = false;
                        }
                    }
                    if (flag)
                    { 
                        SingleSpawnLoop(tempPod, currentPosL, this.CasterPawn.Map);
                        FleckMaker.ThrowHeatGlow(currentPosL, map, .6f);
                    }
                }
            }
            this.burstShotsLeft = 0;
            return true;
        }

        private float CalculateAngles(IntVec3 originPos, IntVec3 destPos)
        {
            float hyp = Mathf.Sqrt((Mathf.Pow(originPos.x - destPos.x, 2)) + (Mathf.Pow(originPos.z - destPos.z, 2)));
            float angleRad = Mathf.Asin(Mathf.Abs(originPos.x - destPos.x) / hyp);
            float angleDeg = Mathf.Rad2Deg * angleRad;
            return angleDeg / 90;
        }

        public void SingleSpawnLoop(SpawnThings spawnables, IntVec3 position, Map map)
        {
            bool flag = spawnables.def != null;
            if (flag)
            {
                Faction faction = this.CasterPawn.Faction;
                ThingDef def = spawnables.def;
                ThingDef stuff = null;
                bool madeFromStuff = def.MadeFromStuff;
                if (madeFromStuff)
                {
                    stuff = ThingDefOf.BlocksGranite;
                }
                Thing thing = ThingMaker.MakeThing(def, stuff);
                GenSpawn.Spawn(thing, position, map, Rot4.North, WipeMode.Vanish, false);                
            }
        }

        private IntVec3 GetNewPos(IntVec3 curPos, bool xdir, bool zdir, bool halfway, float zvar, float xvar, float xguide, float zguide)
        {
            float rand = (float)Rand.Range(0, 100);
            bool flagx = rand <= ((xguide + Mathf.Abs(xvar)) * 100);

            bool flagy = rand <= ((zguide + Mathf.Abs(zvar)) * 100);
            if (halfway)
            {
                xvar = (-1 * xvar);
                zvar = (-1 * zvar);
            }

            if (xdir && zdir)
            {
                //top right
                if (flagx)
                {
                    if (xguide + xvar >= 0) { curPos.x++; }
                    else { curPos.x--; }
                }
                if (flagy)
                {
                    if (zguide + zvar >= 0) { curPos.z++; }
                    else { curPos.z--; }
                }
            }
            if (xdir && !zdir)
            {
                //bottom right
                if (flagx)
                {
                    if (xguide + xvar >= 0) { curPos.x++; }
                    else { curPos.x--; }
                }
                if (flagy)
                {
                    if ((-1 * zguide) + zvar >= 0) { curPos.z++; }
                    else { curPos.z--; }
                }
            }
            if (!xdir && zdir)
            {
                //top left
                if (flagx)
                {
                    if ((-1 * xguide) + xvar >= 0) { curPos.x++; }
                    else { curPos.x--; }
                }
                if (flagy)
                {
                    if (zguide + zvar >= 0) { curPos.z++; }
                    else { curPos.z--; }
                }
            }
            if (!xdir && !zdir)
            {
                //bottom left
                if (flagx)
                {
                    if ((-1 * xguide) + xvar >= 0) { curPos.x++; }
                    else { curPos.x--; }
                }
                if (flagy)
                {
                    if ((-1 * zguide) + zvar >= 0) { curPos.z++; }
                    else { curPos.z--; }
                }
            }
            else
            {
                //no direction identified
            }
            return curPos;
        }
    }
}
