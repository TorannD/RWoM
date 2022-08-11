using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;
using Verse.AI;


namespace TorannMagic
{
    public class Verb_IncitePassion : Verb_UseAbility
    {
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

        public List<SkillRecord> validSkillPassions = new List<SkillRecord>();
        private int rotationDegree = 0;

        protected override bool TryCastShot()
        {
            bool flag = false;
            Pawn caster = this.CasterPawn;
            Pawn hitPawn = this.currentTarget.Thing as Pawn;

            if (hitPawn != null && hitPawn.RaceProps != null && hitPawn.RaceProps.Humanlike && !TM_Calc.IsUndead(hitPawn))
            {
                if (CheckAnyPassions(caster, hitPawn))
                {
                    SkillRecord sr = validSkillPassions.RandomElement();
                    string count = "+";
                    if (sr.passion == Passion.Major)
                    {
                        count = "++";
                    }
                    Vector3 aboveHead = hitPawn.DrawPos;
                    aboveHead.z += .3f;
                    MoteMaker.ThrowText(aboveHead, hitPawn.Map, "Incite " + sr.def.defName.CapitalizeFirst() + count.ToString());
                    aboveHead = caster.DrawPos;
                    aboveHead.z += .3f;
                    MoteMaker.ThrowText(aboveHead, caster.Map, "Lost " + sr.def.defName.CapitalizeFirst() + count.ToString());
                    hitPawn.skills.GetSkill(sr.def).passion = sr.passion;
                    sr.passion = Passion.None;

                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 headOffset = caster.DrawPos;
                        headOffset.z += Rand.Range(-.2f, .2f);
                        headOffset.x += Rand.Range(-.2f, .2f);
                        rotationDegree += Rand.Range(25, 45);
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_RedSwirl, headOffset, hitPawn.Map, .6f, 0.2f, .1f, .2f, 30 + rotationDegree, 0, 0, rotationDegree);

                        Vector3 throwVec = TM_Calc.GetVector(caster.DrawPos, hitPawn.DrawPos);
                        float throwAngle = (Quaternion.AngleAxis(90, Vector3.up) * throwVec).ToAngleFlat();
                        float magnitude = (caster.Position - hitPawn.Position).LengthHorizontal;
                        for (int j = 0; j < 4; j++)
                        {
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_EnergyStream, headOffset, caster.Map, Rand.Range(.4f, .8f), 0.15f, .02f + (.04f * i), .3f - (.06f * i), Rand.Range(-10, 10), magnitude + (magnitude * .5f * i), throwAngle, Rand.Chance(.5f) ? throwAngle : throwAngle - 180);
                        }
                    }
                }
                else
                {
                    Messages.Message("TM_NoPassionsToTransfer".Translate(CasterPawn.LabelShort, hitPawn.LabelShort), MessageTypeDefOf.RejectInput);
                }
            }
            else
            {
                Messages.Message("TM_InvalidTarget".Translate(CasterPawn.LabelShort, this.Ability.Def.label), MessageTypeDefOf.RejectInput);
            }

            this.PostCastShot(flag, out flag);
            return flag;
        }

        public bool CheckAnyPassions(Pawn source, Pawn target)
        {
            validSkillPassions = new List<SkillRecord>();
            validSkillPassions.Clear();
            foreach (SkillRecord skill in source.skills.skills)
            {
                if (skill != null)
                {
                    if (target.skills.GetSkill(skill.def).passion < skill.passion)
                    {
                        validSkillPassions.Add(skill);
                    }
                }
            }
            return validSkillPassions.Count > 0;
        }
    }
}
