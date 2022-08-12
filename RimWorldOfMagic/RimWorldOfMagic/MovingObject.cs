using RimWorld;
using UnityEngine;
using Verse;
using System.Linq;
using System.Collections.Generic;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class MovingObject : ThingWithComps
    {

        private static readonly Color cleaveColor = new Color(160f, 160f, 160f);
        private static readonly Material cleavingMat = MaterialPool.MatFrom("Spells/cleave", ShaderDatabase.Transparent, MovingObject.cleaveColor);

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
        int weaponDmg = 0;


        bool xflag = false;
        bool zflag = false;

        public int lastMove = 0;
        public int moveRate = 2;
        private int age = -5;
        IntVec3 newPos;

        private bool initialize = true;

        Pawn pawn;
        Map map;
        IntVec3 origin;
        IntVec3 target;

        public void Launch(Pawn pawn_, Map map_, IntVec3 origin_, IntVec3 target_)
        {
            this.pawn = pawn_;
            bool spawned = pawn_.Spawned;
            if (spawned)
            {
                pawn_.DeSpawn();
            }            
            this.map = map_;
            this.origin = origin_;
            this.target = target_;
            GenSpawn.Spawn(pawn, origin_, map_);


            //Log.Message("Launching from " + origin + " to " + target);
            ThingDef def = this.def;

            CellRect cellRect = CellRect.CenteredOn(origin, 1);
            cellRect.ClipInsideMap(map);
            IntVec3 centerCell = cellRect.CenterCell;
            IntVec3 expCell1 = centerCell;
            IntVec3 expCell2 = centerCell;

            Hediff invul = new Hediff();
            invul.def = TorannMagicDefOf.TM_HediffInvulnerable;
            invul.Severity = 5;

            if (initialize)
            {
                //ThingSelectionUtility.SelectNextColonist();
                //this.launcher.DeSpawn();
                //GenSpawn.Spawn(pawn, centerCell, map);
                //pawn.drafter.Drafted = true;
                //ThingSelectionUtility.SelectPreviousColonist();
                pawn.health.AddHediff(invul, null, null);
                Initialize(target, pawn);
                pawn.Rotation = pawn.Rotation.Opposite;
                Log.Message("moving object initialized");
            }   
        }

        public void Move()
        {
            bool flag = arg_40_0 && arg_41_0 && arg_42_0;
            Log.Message("Flag is " + flag);
            if (flag)
            {
                Log.Message("age is " + age + " lastmove: " + lastMove + " moverate: " + moveRate);

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
                    Log.Message("new pos is " + newPos);
                    pawn.SetPositionDirect(newPos);
                    pawn.Rotation = pawn.Rotation.Opposite;
                    pawn.mindState.priorityWork.ClearPrioritizedWorkAndJobQueue();
                    pawn.ClearMind();
                    DoWhirlwindDamage(pawn);

                    if (xflag && zflag)
                    {
                        destinationReached = true;
                        Log.Message("Destination reached");
                    }
                }
                DrawCleaving(pawn, 10);
            }
            else
            {
                Log.Message("arg_40_0:" + arg_40_0 + " arg_41_0:" + arg_41_0 + " arg_42_0:" + arg_42_0);
                Messages.Message("Invalid target location.", MessageTypeDefOf.RejectInput);
                destinationReached = true;
            }

            if (destinationReached)
            {
                zflag = false;
                xflag = false;
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                FleckMaker.ThrowSmoke(pawn.Position.ToVector3(), map, 1.2f);
                DoWhirlwindDamage(pawn);
                DrawCleaving(pawn, 10);

                //Pawn p;
                //p = pawn;
                //pawn.DeSpawn();
                //GenSpawn.Spawn(p, newPos, map);
                //p.drafter.Drafted = true;
                //pawn.mindState.priorityWork.ClearPrioritizedWorkAndJobQueue();
                //pawn.Map.pawnDestinationManager.UnreserveAllFor(pawn);
                //pawn.jobs.StopAll();
                RemoveInvul(pawn);
                Destroy();

            }
        }

        public void DoWhirlwindDamage(Pawn caster)
        {
            if (caster != null & weaponDmg != 0)
            {
                IntVec3 arg_29_0 = caster.Position;
                arg_40_0 = caster.Position.IsValid;
                DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Cleave, weaponDmg, 0, (float)-1, caster, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                ApplyWhirlwindDamage(dinfo, caster, caster.Map);

            }
            else
            {
                Log.Warning("failed to do whirlwind");
            }
        }

        public void ApplyWhirlwindDamage(DamageInfo dinfo, Pawn caster, Map map)
        {

            bool flag = !dinfo.InstantPermanentInjury;
            if (flag)
            {
                bool flag2 = dinfo.Instigator != null;
                if (flag2)
                {
                    float num2 = dinfo.Amount;
                    bool flag3 = caster != null && caster.PositionHeld != default(IntVec3) && !caster.Downed;
                    if (flag3)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            IntVec3 intVec = caster.PositionHeld + GenAdj.AdjacentCells[i];
                            Pawn cleaveVictim = new Pawn();
                            cleaveVictim = intVec.GetFirstPawn(map);
                            if (cleaveVictim != null && cleaveVictim.Faction != caster.Faction)
                            {
                                cleaveVictim.TakeDamage(dinfo);
                                FleckMaker.ThrowMicroSparks(cleaveVictim.Position.ToVector3(), map);
                                CompAbilityUserMight comp = caster.GetCompAbilityUserMight();
                                MightPowerSkill ver = comp.MightData.MightPowerSkill_Whirlwind.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Whirlwind_ver");
                                DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Whirlwind, weaponDmg, 0, (float)-1, caster, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                                System.Random random = new System.Random();
                                int rnd = GenMath.RoundRandom(random.Next(0, 100));
                                if (rnd < (ver.level * 5))
                                {
                                    cleaveVictim.TakeDamage(dinfo2);
                                    FleckMaker.ThrowMicroSparks(cleaveVictim.Position.ToVector3(), map);
                                }
                            }
                        }
                    }
                }
            }
        }

        private int GetWeaponDmg(Pawn caster)
        {
            int dmgNum = 1;
            CompAbilityUserMight comp = caster.GetCompAbilityUserMight();
            MightPowerSkill pwr = comp.MightData.MightPowerSkill_Whirlwind.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Whirlwind_pwr");
            ThingWithComps arg_3C_0;
            if (caster == null)
            {
                arg_3C_0 = null;
            }
            else
            {
                Pawn_EquipmentTracker expr_eq = caster.equipment;
                arg_3C_0 = ((expr_eq != null) ? expr_eq.Primary : null);
            }
            ThingWithComps thing;
            bool flag31 = (thing = arg_3C_0) != null;
            if (flag31)
            {
                int value = Mathf.RoundToInt(thing.GetStatValue(StatDefOf.MarketValue));
                if (value < 400)
                {
                    dmgNum = 4;
                }
                else if (value >= 400 && value < (1000 + 100 * pwr.level))
                {
                    dmgNum = Mathf.RoundToInt(thing.GetStatValue(StatDefOf.MarketValue) / 80);
                }
                else if (value >= (1000 + 100 * pwr.level) && value < (10000 + 1000 * pwr.level))
                {
                    dmgNum = 12 + Mathf.RoundToInt(thing.GetStatValue(StatDefOf.MarketValue) / 600);
                }
                else
                {
                    dmgNum = 28 + Mathf.RoundToInt(thing.GetStatValue(StatDefOf.MarketValue) / 1500);
                }

            }
            else
            {
                dmgNum = 4;
            }
            return dmgNum;
        }

        private void Initialize(IntVec3 target, Pawn pawn)
        {
            if (target != null && pawn != null)
            {
                arg_40_0 = target.IsValid;
                arg_41_0 = target.ToVector3().InBoundsWithNullCheck(pawn.Map);
                arg_42_0 = true; // target.Standable(pawn.Map);
                vflag = arg_40_0 && arg_41_0 && arg_42_0;
                if (vflag)
                {
                    newPos = pawn.Position;
                    originPos = pawn.Position;
                    FleckMaker.Static(pawn.TrueCenter(), pawn.Map, FleckDefOf.ExplosionFlash, 12f);
                    SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                    FleckMaker.ThrowDustPuff(originPos, pawn.Map, Rand.Range(1.2f, 1.8f));
                    XProb(target, pawn);
                    xProbOrigin = xProb;
                    weaponDmg = GetWeaponDmg(pawn);                   
                }
            }
            else
            {
                arg_40_0 = false;
                destinationReached = true;
            }
            initialize = false;
        }

        private void XProb(IntVec3 target, Pawn pawn)
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

        private void DrawCleaving(Pawn shieldedPawn, int magnitude)
        {
            bool flag = !shieldedPawn.Dead && !shieldedPawn.Downed;
            if (flag)
            {
                Vector3 vector = shieldedPawn.Drawer.DrawPos;
                vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                float angle = (float)Rand.Range(0, 360);
                Vector3 s = new Vector3(3f, 3f, 5f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, MovingObject.cleavingMat, 0);

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
                    if (current.Label == "Invulnerable")
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
            Move();
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
