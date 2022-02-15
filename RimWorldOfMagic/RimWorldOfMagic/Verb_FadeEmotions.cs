using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_FadeEmotions : Verb_UseAbility
    {

        bool validTarg;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = true;
                }
                else
                {
                    //out of range
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
            bool flag = false;
            Pawn caster = this.CasterPawn;
            Pawn hitPawn = this.currentTarget.Thing as Pawn;

            if(hitPawn != null && hitPawn.RaceProps != null && hitPawn.needs != null && hitPawn.needs.mood != null && hitPawn.needs.mood.thoughts != null && 
                hitPawn.RaceProps.Humanlike && !TM_Calc.IsUndead(hitPawn) && hitPawn.Faction == caster.Faction)
            {
                List<Thought_Memory> thoughts = hitPawn.needs.mood.thoughts.memories.Memories;
                Need n = hitPawn.needs.mood;
                for (int i = 0; i < thoughts.Count; i++)
                {
                    hitPawn.needs.mood.thoughts.memories.RemoveMemory(thoughts[i]);
                    n.CurLevel -= .3f;
                    i--;
                    if(n.CurLevel < .3f)
                    {
                        break;
                    }
                }
                Effects(hitPawn.Position);                                        
            }
            else
            {
                Messages.Message("TM_InvalidTarget".Translate(CasterPawn.LabelShort, this.Ability.Def.label), MessageTypeDefOf.RejectInput);
            }

            this.PostCastShot(flag, out flag);
            return flag;
        }

        public void Effects(IntVec3 position)
        {
            Vector3 rndPos = position.ToVector3Shifted();
            for (int i = 0; i < 3; i++)
            {
                rndPos.x += Rand.Range(-.5f, .5f);
                rndPos.z += Rand.Range(.1f, .5f);
                rndPos.y += Rand.Range(-.3f, .3f);
                FleckMaker.ThrowLightningGlow(position.ToVector3Shifted(), this.CasterPawn.Map, Rand.Range(.5f, .8f));
            }
        }
    }
}
