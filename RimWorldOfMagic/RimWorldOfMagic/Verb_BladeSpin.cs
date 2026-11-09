using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;
using System.Linq;
using System.Collections.Generic;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Verb_BladeSpin : Verb_UseAbility
    {

        private int verVal = 0;
        private int pwrVal = 0;

        private static readonly Color bladeColor = new Color(160f, 160f, 160f);
        private static readonly Material bladeMat = MaterialPool.MatFrom("Spells/cleave", false);

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

        public static int GetWeaponDmg(Pawn pawn) => ClassProgression.BladeMartialCast.WeaponDamage(pawn,pawn,.6f);

        protected override bool TryCastShot()
        {
            burstShotsLeft=0;
            PostCastShot(Spin(),out bool success);
            return success;
        }

        internal bool Spin()
        {
            var body=CasterPawn;
            var owner=Ability?.Pawn ?? body;
            if (!ClassProgression.BladeMartialCast.HasWeapon(body)) return false;
            var def=Ability?.Def as TMAbilityDef ?? TorannMagicDefOf.TM_BladeSpin;
            verVal=TM_Calc.GetSkillVersatilityLevel(owner,def);
            pwrVal=TM_Calc.GetSkillPowerLevel(owner,def);
            SearchForTargets(body.Position,2f+.5f*verVal,body.Map);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BladeSweep,body.DrawPos,body.Map,1.6f+.4f*verVal,.04f,0f,.18f,1000,0,0,Rand.Range(0,360));
            return true;
        }

        public void SearchForTargets(IntVec3 center,float radius,Map map)
        {
            var body=CasterPawn;
            DrawBlade(body.Position.ToVector3Shifted(),4f+verVal);
            int damage=ClassProgression.BladeMartialCast.WeaponDamage(body,Ability?.Pawn ?? body,.6f);
            foreach(var cell in GenRadial.RadialCellsAround(center,radius,true))
            {
                if(!cell.InBounds(map))continue;
                var victim=cell.GetFirstPawn(map);
                if(victim==null || victim==body || victim.Faction==body.Faction)continue;
                for(int j=0;j<2+pwrVal;j++)
                {
                    if(victim.Dead || !victim.Spawned)break;
                    if(!Rand.Chance(.5f+.04f*(pwrVal+verVal)))continue;
                    DrawStrike(center,victim.Position.ToVector3(),map);
                    damageEntities(victim,null,damage,DamageDefOf.Cut);
                }
            }
        }

        public void DrawStrike(IntVec3 center, Vector3 strikePos, Map map)
        {
            TM_MoteMaker.ThrowCrossStrike(strikePos, map, 1f);
            TM_MoteMaker.ThrowBloodSquirt(strikePos, map, 1.5f);
        }

        private void DrawBlade(Vector3 center, float magnitude)
        {

            Vector3 vector = center;
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            Vector3 s = new Vector3(magnitude, magnitude, (1.5f * magnitude));
            Matrix4x4 matrix = default(Matrix4x4);

            for (int i = 0; i < 6; i++)
            {
                float angle = Rand.Range(0, 360);
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, Verb_BladeSpin.bladeMat, 0);
            }
        }

        public void damageEntities(Pawn victim, BodyPartRecord hitPart, int amt, DamageDef type)
        {
            DamageInfo dinfo;
            amt = (int)((float)amt * Rand.Range(.7f, 1.3f));
            dinfo = new DamageInfo(type, amt, 0, (float)-1, this.CasterPawn, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);            
            dinfo.SetAllowDamagePropagation(false);
            victim.TakeDamage(dinfo);
        }
    }
}
