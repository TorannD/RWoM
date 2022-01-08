using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using UnityEngine;
using Verse;
using Verse.AI;


namespace TorannMagic
{
    public class Verb_BlankMind : Verb_UseAbility
    {

        bool validTarg;
        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt)
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
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
            this.TargetsAoE.Clear();
            //this.UpdateTargets();
            this.FindTargets();

            bool friendlyTarget = this.currentTarget != null && this.currentTarget.Thing != null && this.currentTarget.Thing.Faction != null && this.currentTarget.Thing.Faction == this.CasterPawn.Faction && this.currentTarget.Thing != this.CasterPawn;
            bool flag2 = (this.UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetAoE && this.TargetsAoE.Count > 1) || friendlyTarget;
            if (flag2)
            {
                this.TargetsAoE.RemoveRange(0, this.TargetsAoE.Count - 1);
            }
            if (friendlyTarget)
            {
                Pawn pawn = this.currentTarget.Thing as Pawn;
                if (pawn != null && pawn.RaceProps.Humanlike && pawn.needs != null && pawn.needs.mood.thoughts != null)
                {
                    if (Rand.Chance(TM_Calc.GetSpellSuccessChance(this.CasterPawn, pawn, true)))
                    {
                        List<Thought_Memory> thoughts = pawn.needs.mood.thoughts.memories.Memories;
                        pawn.mindState.mentalStateHandler.TryStartMentalState(TorannMagicDefOf.WanderConfused, null, false, false, null, false);
                        for(int i =0; i< thoughts.Count; i++)
                        {
                            pawn.needs.mood.thoughts.memories.RemoveMemory(thoughts[i]);                            
                            i--;
                        }
                        pawn.needs.mood.thoughts.memories.TryGainMemory(TorannMagicDefOf.TM_MemoryWipe, null);
                        Effects(pawn.Position);
                    }
                    else
                    {
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TM_ResistedSpell".Translate(), -1);
                    }                    
                }
            }
            else
            {
                for (int i = 0; i < this.TargetsAoE.Count; i++)
                {
                    Pawn newPawn = this.TargetsAoE[i].Thing as Pawn;
                    if (newPawn.RaceProps.IsFlesh && newPawn.RaceProps.Humanlike && newPawn.Faction != this.CasterPawn.Faction)
                    {
                        if (Rand.Chance(TM_Calc.GetSpellSuccessChance(this.CasterPawn, newPawn, true)))
                        {
                            TM_Action.DamageEntities(newPawn, null, 30, DamageDefOf.Stun, this.CasterPawn);
                            Effects(newPawn.Position);
                        }
                        else
                        {
                            MoteMaker.ThrowText(newPawn.DrawPos, newPawn.Map, "TM_ResistedSpell".Translate(), -1);
                        }
                    }
                }                
            }
            this.PostCastShot(flag, out flag);
            return flag;
        }

        private void FindTargets()
        {
            bool flag = this.UseAbilityProps.AbilityTargetCategory == AbilityTargetCategory.TargetAoE;
            if (flag)
            {
                bool flag2 = this.UseAbilityProps.TargetAoEProperties == null;
                if (flag2)
                {
                    Log.Error("Tried to Cast AoE-Ability without defining a target class");
                }
                List<Thing> list = new List<Thing>();
                IntVec3 aoeStartPosition = this.caster.PositionHeld;
                bool flag3 = !this.UseAbilityProps.TargetAoEProperties.startsFromCaster;
                if (flag3)
                {
                    aoeStartPosition = this.currentTarget.Cell;
                }
                bool flag4 = !this.UseAbilityProps.TargetAoEProperties.friendlyFire;
                if (flag4)
                {
                    list = (from x in this.caster.Map.listerThings.AllThings
                            where x.Position.InHorDistOf(aoeStartPosition, (float)this.UseAbilityProps.TargetAoEProperties.range) && this.UseAbilityProps.TargetAoEProperties.targetClass.IsAssignableFrom(x.GetType()) && x.Faction != Faction.OfPlayer
                            select x).ToList<Thing>();
                }
                else
                {
                    bool flag5 = this.UseAbilityProps.TargetAoEProperties.targetClass == typeof(Plant) || this.UseAbilityProps.TargetAoEProperties.targetClass == typeof(Building);
                    if (flag5)
                    {
                        list = (from x in this.caster.Map.listerThings.AllThings
                                where x.Position.InHorDistOf(aoeStartPosition, (float)this.UseAbilityProps.TargetAoEProperties.range) && this.UseAbilityProps.TargetAoEProperties.targetClass.IsAssignableFrom(x.GetType())
                                select x).ToList<Thing>();
                        foreach (Thing current in list)
                        {
                            LocalTargetInfo item = new LocalTargetInfo(current);
                            this.TargetsAoE.Add(item);
                        }
                        return;
                    }
                    list.Clear();
                    list = (from x in this.caster.Map.listerThings.AllThings
                            where x.Position.InHorDistOf(aoeStartPosition, (float)this.UseAbilityProps.TargetAoEProperties.range) && this.UseAbilityProps.TargetAoEProperties.targetClass.IsAssignableFrom(x.GetType()) && (x.HostileTo(Faction.OfPlayer) || this.UseAbilityProps.TargetAoEProperties.friendlyFire)
                            select x).ToList<Thing>();
                }
                int maxTargets = this.UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties.maxTargets;
                List<Thing> list2 = new List<Thing>(list.InRandomOrder(null));
                int num = 0;
                while (num < maxTargets && num < list2.Count<Thing>())
                {
                    TargetInfo targ = new TargetInfo(list2[num]);
                    bool flag6 = this.UseAbilityProps.targetParams.CanTarget(targ);
                    if (flag6)
                    {
                        this.TargetsAoE.Add(new LocalTargetInfo(list2[num]));
                    }
                    num++;
                }
            }
            else
            {
                this.TargetsAoE.Clear();
                this.TargetsAoE.Add(this.currentTarget);
            }
        }

        public void Effects(IntVec3 position)
        {
            Vector3 rndPos = position.ToVector3Shifted();
            for (int i = 0; i < 3; i++)
            {
                rndPos.x += Rand.Range(-.5f, .5f);
                rndPos.z += Rand.Range(-.5f, .5f);
                rndPos.y += Rand.Range(.3f, 1.3f);
                FleckMaker.ThrowLightningGlow(position.ToVector3Shifted(), this.CasterPawn.Map, 1.4f);
            }
        }

    }
}
