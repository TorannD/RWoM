using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_LivingWall : Verb_UseAbility
    {
        private int verVal;
        private int pwrVal;
        float arcaneDmg = 1f;

        bool validTarg;
        //Used for non-unique abilities that can be used with shieldbelt
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == this.caster)
            {
                return this.verbProps.targetParams.canTargetSelf;
            }
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    return true;
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
            Pawn caster = this.CasterPawn;
            //Pawn pawn = this.currentTarget.Thing as Pawn;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            //verVal = TM_Calc.GetMagicSkillLevel(caster, comp.MagicData.MagicPowerSkill_LivingWall, "TM_LivingWall", "_ver", true);
            //pwrVal = TM_Calc.GetMagicSkillLevel(caster, comp.MagicData.MagicPowerSkill_LivingWall, "TM_LivingWall", "_pwr", true);
            if(comp != null)
            {
                List<Thing> tList = this.currentTarget.Cell.GetThingList(caster.Map);
                if(tList != null && tList.Count > 0)
                {
                    bool wallDetected = false;
                    foreach(Thing t in tList)
                    {
                        if(t.Faction == caster.Faction && TM_Calc.IsWall(t))
                        {
                            if(comp.livingWall != null && !comp.livingWall.DestroyedOrNull())
                            {
                                comp.livingWall.Destroy(DestroyMode.Vanish);
                            }
                            FleckMaker.ThrowLightningGlow(t.DrawPos, caster.Map, 1f);
                            Thing launchedThing = new Thing()
                            {
                                def = TorannMagicDefOf.FlyingObject_LivingWall
                            };
                            Pawn casterPawn = base.CasterPawn;                            
                            FlyingObject_LivingWall flyingObject = (FlyingObject_LivingWall)GenSpawn.Spawn(TorannMagicDefOf.FlyingObject_LivingWall, t.Position, this.CasterPawn.Map);
                            List<Vector3> path = new List<Vector3>();
                            Vector3 newVec = t.Position.ToVector3Shifted();
                            path.Add(newVec);
                            path.Add(newVec);
                            flyingObject.ExactLaunch(null, 0, false, path, caster, newVec, t, launchedThing, 15+(3*verVal), 0);
                            comp.livingWall = flyingObject;
                            wallDetected = true;
                            break;
                        }
                    }
                    if(!wallDetected)
                    {
                        Messages.Message("TM_InvalidTarget".Translate(caster.LabelShort, Ability.Def.label), MessageTypeDefOf.NegativeEvent);
                        MoteMaker.ThrowText(caster.DrawPos, caster.Map, "Living Wall: " + StringsToTranslate.AU_CastFailure, -1f);
                    }
                }
                else
                {
                    Messages.Message("TM_InvalidTarget".Translate(caster.LabelShort, Ability.Def.label), MessageTypeDefOf.NegativeEvent);
                    MoteMaker.ThrowText(caster.DrawPos, caster.Map, "Living Wall: " + StringsToTranslate.AU_CastFailure, -1f);
                }
            }
            
            return true;
        }
    }
}
