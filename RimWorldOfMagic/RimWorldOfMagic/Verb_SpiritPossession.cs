using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_SpiritPossession : Verb_SB
    {        

        protected override bool TryCastShot()
        {
            bool flag = false;

            Map map = base.CasterPawn.Map;

            Pawn hitPawn = (Pawn)this.currentTarget;
            Pawn caster = base.CasterPawn;
            
            if (hitPawn != null && !hitPawn.Dead && hitPawn.Spawned && hitPawn.story != null && hitPawn.story.traits != null && hitPawn.jobs != null && hitPawn != caster && !TM_Calc.IsPossessedByOrIsSpirit(hitPawn) && hitPawn.RaceProps != null && hitPawn.RaceProps.IsFlesh)
            {
                CompAbilityUserMagic targetComp = hitPawn.GetCompAbilityUserMagic();
                if (targetComp != null)
                {
                    TryLaunchProjectile(base.verbProps.defaultProjectile, hitPawn);
                }
                else
                {
                    Messages.Message("TM_InvalidTarget".Translate(
                        this.CasterPawn.LabelShort,
                        this.Ability.Def.label
                    ), MessageTypeDefOf.RejectInput);
                }
            }
            else
            {
                Messages.Message("TM_InvalidTarget".Translate(
                    this.CasterPawn.LabelShort,
                    this.Ability.Def.label
                ), MessageTypeDefOf.RejectInput);
            }
            this.PostCastShot(flag, out flag);
            return flag;
        }
    }
}
