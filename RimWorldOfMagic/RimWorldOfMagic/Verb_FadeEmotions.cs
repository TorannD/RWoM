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
    public class Verb_FadeEmotions : ClassProgression.Verb_SpiritTargetable
    {

        bool validTarg;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map))
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

        internal static bool CanFade(Pawn body, Pawn target)
        {
            return body?.Spawned == true && target?.Spawned == true && target != body && !target.Dead &&
                target.Map == body.Map && target.Faction == body.Faction && target.RaceProps.Humanlike &&
                !TM_Calc.IsUndead(target) && target.needs?.mood?.thoughts?.memories?.Memories.Any(m => m.MoodOffset() < 0f) == true;
        }

        protected override bool TryCastShot()
        {
            bool applied = ApplyHostedEffect();
            if (!applied) Messages.Message("TM_InvalidTarget".Translate(CasterPawn.LabelShort, Ability.Def.label), MessageTypeDefOf.RejectInput);
            PostCastShot(applied,out var fired);
            return fired;
        }

        protected override bool ApplyHostedEffect()
        {
            var target = currentTarget.Pawn;
            if (!CanFade(CasterPawn,target)) return false;
            var mood = target.needs.mood;
            var memories = mood.thoughts.memories;
            foreach (var memory in memories.Memories.Where(m => m.MoodOffset() < 0f).ToList())
            {
                memories.RemoveMemory(memory);
                mood.CurLevel -= .3f;
                if (mood.CurLevel < .3f) break;
            }
            Effects(target.Position);
            return true;
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
