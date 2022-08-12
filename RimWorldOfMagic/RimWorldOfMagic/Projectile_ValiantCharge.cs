using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using RimWorld;
using System.Collections.Generic;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Projectile_ValiantCharge : Projectile_AbilityBase
    {
        private static readonly Color wingColor = new Color(160f, 160f, 160f);
        private static readonly Material wingsNS = MaterialPool.MatFrom("Other/angelwings_up", ShaderDatabase.Transparent, Projectile_ValiantCharge.wingColor);
        private static readonly Material wingsE = MaterialPool.MatFrom("Other/angelwings_up_east", ShaderDatabase.Transparent, Projectile_ValiantCharge.wingColor);
        private static readonly Material wingsW = MaterialPool.MatFrom("Other/angelwings_up_west", ShaderDatabase.Transparent, Projectile_ValiantCharge.wingColor);

        bool arg_40_0;
        bool arg_41_0;
        bool arg_42_0;
        bool vflag;

        private bool destinationReached = false;

        float hyp = 0;
        float angleRad = 0;
        float angleDeg = 0;
        float xProb;

        float xProbOrigin;
        IntVec3 originPos;
        
        
        bool xflag = false;
        bool zflag = false;

        public int lastMove = 0;
        public int moveRate = 2;
        private int age = -1;
        IntVec3 newPos;

        private bool initialize = true;

        Pawn pawn;

        protected override void Impact(Thing hitThing)
        {

            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;

            CellRect cellRect = CellRect.CenteredOn(base.Position, 1); 
            cellRect.ClipInsideMap(map);
            IntVec3 centerCell = cellRect.CenterCell;
            IntVec3 expCell1 = centerCell;
            IntVec3 expCell2 = centerCell;
            IntVec3 target = base.Position;

            Hediff invul = new Hediff();
            invul.def = TorannMagicDefOf.TM_HediffInvulnerable;
            invul.Severity = 5;

            pawn = this.launcher as Pawn;
            pawn.health.AddHediff(invul, null, null);

            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ValiantCharge.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_ValiantCharge_pwr");
            MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ValiantCharge.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_ValiantCharge_ver");

            if (initialize)
            {
                Initialize(target, pawn);
                pawn.Rotation = pawn.Rotation.Opposite;
            }

            bool flag = arg_40_0 && arg_41_0 && arg_42_0;
            if (flag)
            {
                if (!destinationReached && this.age >= lastMove + moveRate)
                {
                    lastMove = this.age;
                    XProb(target, pawn);
                    if (target.x == pawn.Position.x)
                    {
                        xflag = true;
                    }
                    if (target.z == pawn.Position.z)
                    {
                        zflag = true;
                    }
                    FleckMaker.ThrowDustPuff(newPos, pawn.Map, Rand.Range(0.8f, 1.2f));
                    newPos = GetNewPos(pawn.Position, pawn.Position.x <= target.x, pawn.Position.z <= target.z, false, 0, 0, xProb, 1 - xProb);
                    pawn.SetPositionDirect(newPos);
                    pawn.Rotation = pawn.Rotation.Opposite;
                    pawn.mindState.priorityWork.ClearPrioritizedWorkAndJobQueue();

                    if (xflag && zflag)
                    {
                        destinationReached = true;
                    }
                }
                DrawWings(pawn, 10);
            }
            else
            {
               // Log.Message("arg_40_0:" + arg_40_0 + " arg_41_0:" + arg_41_0 + " arg_42_0:" + arg_42_0);
                Messages.Message("InvalidTargetLocation".Translate(), MessageTypeDefOf.RejectInput);
                destinationReached = true;                
            }

            if (destinationReached)
            {
                zflag = false;
                xflag = false;
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                this.FireExplosion(pwr.level, ver.level, centerCell, map, (1.2f + (float)(ver.level * .8f)));
                FleckMaker.ThrowSmoke(pawn.Position.ToVector3(), map, (0.8f + (float)(ver.level * .8f)));

                pawn.mindState.priorityWork.ClearPrioritizedWorkAndJobQueue();
                pawn.Map.pawnDestinationReservationManager.ReleaseAllClaimedBy(pawn);
                pawn.Map.physicalInteractionReservationManager.ReleaseAllClaimedBy(pawn);
                //pawn.Map.pawnDestinationManager.UnreserveAllFor(pawn);
                pawn.jobs.StopAll();
                RemoveInvul(pawn);

                for (int i = 0; i < (2 + ver.level); i++)
                {
                    expCell1 = GetNewPos(expCell1, originPos.x <= target.x, originPos.z <= target.z, false, 0, 0, xProbOrigin, 1 - xProbOrigin);
                    FleckMaker.ThrowSmoke(expCell1.ToVector3(), map, 1.6f);
                    expCell2 = GetNewPos(expCell2, originPos.x <= target.x, originPos.z <= target.z, false, 0, 0, 1 - xProbOrigin, xProbOrigin);
                    FleckMaker.ThrowSmoke(expCell2.ToVector3(), map, 1.6f);
                }
                for (int i = 0; i < (4 + (3*ver.level)); i++)
                {
                    cellRect = CellRect.CenteredOn(expCell1, (1 + ver.level));
                    IntVec3 randomCell = cellRect.RandomCell;
                    this.FireExplosion(pwr.level, ver.level, randomCell, map, .4f);
                    cellRect = CellRect.CenteredOn(expCell2, (1 + ver.level));
                    randomCell = cellRect.RandomCell;
                    this.FireExplosion(pwr.level, ver.level, randomCell, map, .4f);
                }
                
            }

        }

        private void Initialize(IntVec3 target, Pawn pawn)
        {
            if (target != null && pawn != null)
            {
                arg_40_0 = target.IsValid;
                arg_41_0 = target.ToVector3().InBoundsWithNullCheck(pawn.Map);
                arg_42_0 = true; // target.Standable(pawn.Map);
                vflag = arg_40_0 && arg_41_0 && arg_42_0;
                if(vflag)
                {                    
                    newPos = pawn.Position;
                    originPos = pawn.Position;
                    FleckMaker.Static(pawn.TrueCenter(), pawn.Map, FleckDefOf.ExplosionFlash, 12f);
                    SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                    FleckMaker.ThrowDustPuff(originPos, pawn.Map, Rand.Range(1.2f, 1.8f));
                    XProb(target, pawn);
                    xProbOrigin = xProb;
                } 
            }
            else
            {
                arg_40_0 = false;
                destinationReached = true;
            }
            initialize = false;
        }

        private void XProb (IntVec3 target, Pawn pawn)
        {
            hyp = Mathf.Sqrt((Mathf.Pow(pawn.Position.x - target.x, 2)) + (Mathf.Pow(pawn.Position.z - target.z, 2)));
            angleRad = Mathf.Asin(Mathf.Abs(pawn.Position.x - target.x) / hyp);
            angleDeg = Mathf.Rad2Deg * angleRad;
            xProb = angleDeg / 90;
        }

        private IntVec3 GetNewPos(IntVec3 curPos, bool xdir, bool zdir, bool halfway, float zvar, float xvar, float xguide, float zguide)
        {
            float rand = (float)Rand.Range(0, 100);
            bool flagx = rand <= ((xguide + Mathf.Abs(xvar)) * 100) && !xflag;
            bool flagz = rand <= ((zguide + Mathf.Abs(zvar)) * 100) && !zflag;

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
                if (flagz)
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
                if (flagz)
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
                if (flagz)
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
                if (flagz)
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
            //return curPos;
        }

        protected void FireExplosion(int pwr, int ver, IntVec3 pos, Map map, float radius)
        {
            ThingDef def = this.def;

            Explosion(pwr, pos, map, radius, TMDamageDefOf.DamageDefOf.TM_Holy, this.launcher, null, def, this.equipmentDef, null, 0.3f, 1, false, null, 0f, 1);

            if (ver >= 2)
            {
                int stunProb = Rand.Range(1, 10);
                if (stunProb > (4 + ver))
                {
                    Explosion(pwr, pos, map, radius, DamageDefOf.Stun, this.launcher, null, def, this.equipmentDef, null, 0.3f, 1, false, null, 0f, 1);
                }
            }

        }

        public static void Explosion(int pwr, IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, SoundDef explosionSound = null, ThingDef projectile = null, ThingDef source = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1)
        {

            System.Random rnd = new System.Random();
            int modDamAmountRand = (1 + pwr) * GenMath.RoundRandom(rnd.Next(1, projectile.projectile.GetDamageAmount(1,null) / 2));
            if (map == null)
            {
                Log.Warning("Tried to do explosion in a null map.");
                return;
            }
            Explosion explosion = (Explosion)GenSpawn.Spawn(ThingDefOf.Explosion, center, map);
            explosion.damageFalloff = false;
            explosion.chanceToStartFire = 0.0f;
            explosion.Position = center;
            explosion.radius = radius;
            explosion.damType = damType;
            explosion.instigator = instigator;
            explosion.damAmount = ((projectile == null) ? GenMath.RoundRandom((float)damType.defaultDamage) : modDamAmountRand);
            explosion.weapon = source;
            explosion.preExplosionSpawnThingDef = preExplosionSpawnThingDef;
            explosion.preExplosionSpawnChance = preExplosionSpawnChance;
            explosion.preExplosionSpawnThingCount = preExplosionSpawnThingCount;
            explosion.postExplosionSpawnThingDef = postExplosionSpawnThingDef;
            explosion.postExplosionSpawnChance = postExplosionSpawnChance;
            explosion.postExplosionSpawnThingCount = postExplosionSpawnThingCount;
            explosion.applyDamageToExplosionCellsNeighbors = applyDamageToExplosionCellsNeighbors;
            explosion.StartExplosion(explosionSound, null);
        }

        private void DrawWings(Pawn shieldedPawn, int magnitude)
        {
            bool flag = !shieldedPawn.Dead && !shieldedPawn.Downed;
            if (flag)
            {
                float num = Mathf.Lerp(1.2f, 1.55f, magnitude);
                Vector3 vector = shieldedPawn.Drawer.DrawPos;
                vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                float angle = (float)Rand.Range(0, 360);
                Vector3 s = new Vector3(3f, 3f, 3f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(vector, Quaternion.AngleAxis(0f, Vector3.up), s);
                if (shieldedPawn.Rotation == Rot4.South || shieldedPawn.Rotation == Rot4.North)
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, Projectile_ValiantCharge.wingsNS, 0);
                }
                if (shieldedPawn.Rotation == Rot4.East)
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, Projectile_ValiantCharge.wingsE, 0);
                }
                if (shieldedPawn.Rotation == Rot4.West)
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, Projectile_ValiantCharge.wingsW, 0);
                }
            }
        }

        private void RemoveInvul(Pawn abilityUser)
        {
            List<Hediff> list = new List<Hediff>();
            List<Hediff> arg_32_0 = list;
            IEnumerable<Hediff> arg_32_1;
            if (abilityUser == null)
            {
                arg_32_1 = null;
            }
            else
            {
                Pawn_HealthTracker expr_1A = abilityUser.health;
                if (expr_1A == null)
                {
                    arg_32_1 = null;
                }
                else
                {
                    HediffSet expr_26 = expr_1A.hediffSet;
                    arg_32_1 = ((expr_26 != null) ? expr_26.hediffs : null);
                }
            }
            arg_32_0.AddRange(arg_32_1);
            Pawn expr_3E = abilityUser;
            int? arg_84_0;
            if (expr_3E == null)
            {
                arg_84_0 = null;
            }
            else
            {
                Pawn_HealthTracker expr_52 = expr_3E.health;
                if (expr_52 == null)
                {
                    arg_84_0 = null;
                }
                else
                {
                    HediffSet expr_66 = expr_52.hediffSet;
                    arg_84_0 = ((expr_66 != null) ? new int?(expr_66.hediffs.Count<Hediff>()) : null);
                }
            }
            bool flag = (arg_84_0 ?? 0) > 0;
            if (flag)
            {
                foreach (Hediff current in list)
                {
                    if (current.Label == "Burn")
                    {
                        current.Severity = -5;
                    }
                    if (current.def.defName == "TM_HediffInvulnerable")
                    {
                        current.Severity = -5;
                        return;
                    }
                }
            }
            list.Clear();
            list = null;
        }

        public override void Tick()
        {
            base.Tick();
            age++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.destinationReached;
            if (flag)
            {
                base.Destroy(mode);
            }
        }
    }
}
