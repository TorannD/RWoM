using AbilityUser;
using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace TorannMagic
{
    public class Verb_TargetSpiritHost : Verb_SB
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
            => !ClassProgression.SpiritSummonedHost.BlocksTarget(target, caster?.Map) && base.CanHitTargetFrom(root, target);
    }

	public class Projectile_SpiritPossession : Projectile_AbilityBase
	{
        private int verVal;

		protected override void Impact(Thing hitThing, bool blockedByShield = false)
		{
            
            Map map = base.Map;
			base.Impact(hitThing);
			ThingDef def = this.def;            
            Pawn caster = this.launcher as Pawn;
            Pawn hitPawn = hitThing as Pawn;
            bool validTarget = false;
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_SpiritPossession, false);
            if (ClassProgression.SpiritSummonedHost.IsSummoned(hitPawn)) return;
            if (hitPawn != null && !hitPawn.Dead && Possess(caster, hitPawn))
            {
                validTarget = true;
            }
            else
            {
                Corpse corpse = null;
                Thing corpseThing = null;
                List<Thing> thingList;
                thingList = this.DestinationCell.GetThingList(map);
                int z = 0;
                while (z < thingList.Count)
                {                    
                    corpseThing = thingList[z];
                    if (corpseThing != null)
                    {
                        bool validator = corpseThing is Corpse;
                        Pawn innerPawn = null;
                        if (validator)
                        {
                            corpse = corpseThing as Corpse;
                            innerPawn = corpse.InnerPawn;                            
                        }
                        else if (corpseThing is Pawn)
                        {
                            innerPawn = corpseThing as Pawn;                            
                        }
                        if (innerPawn != null && Possess(caster, innerPawn))
                        {
                            validTarget = true;
                            break;
                        }
                    }
                    z++;
                }
                if(!validTarget)
                {
                    Messages.Message("TM_InvalidTarget".Translate(
                        caster.LabelShort,
                        TorannMagicDefOf.TM_SpiritPossession.label
                    ), MessageTypeDefOf.RejectInput);
                }                
            }
        }

        public bool Possess(Pawn caster, Pawn pawn)
        {
            if (pawn == null || ClassProgression.SpiritSummonedHost.IsSummoned(pawn)) return false;
            if (ClassProgression.SpiritRebirthEnergy.Marked(caster))
            {
                if (!ClassProgression.SpiritRebirthEnergy.CanCast(caster) || caster == pawn ||
                    pawn == null || (pawn.Dead ? pawn.Corpse?.Map : pawn.Map) != caster.Map || pawn.RaceProps?.IsFlesh != true ||
                    TM_Calc.IsUndeadNotVamp(pawn) ||
                    TM_Calc.IsPossessedByOrIsSpirit(pawn) || ClassProgression.SpiritRebirthEnergy.Marked(pawn)) return false;
                if (!Rand.Chance(TM_Calc.GetSpellSuccessChance(caster,pawn,true) - .4f + .2f * verVal))
                {
                    MoteMaker.ThrowText(caster.DrawPos,caster.Map,"TM_ResistedSpell".Translate(),-1);
                    return true;
                }
                return ClassProgression.SoulStoneImplant.EnterFromMap(caster,pawn);
            }
            bool wasDead = false;
            FactionDef previousFaction = caster.Faction.def;
            if (!TM_Calc.IsPossessedByOrIsSpirit(pawn) && pawn.RaceProps != null && pawn.RaceProps.IsFlesh && !TM_Calc.IsUndeadNotVamp(pawn))
            {
                if (Rand.Chance(TM_Calc.GetSpellSuccessChance(caster, pawn, true) - .4f + (.2f * verVal)))
                {
                    if (pawn.Faction != caster.Faction)
                    {
                        if (pawn.Faction != null)
                        {
                            previousFaction = pawn.Faction.def;
                        }
                        else
                        {
                            previousFaction = null;
                        }
                        pawn.SetFaction(caster.Faction);
                    }
                    if (pawn.Dead)
                    {
                        wasDead = true;
                        ResurrectionUtility.TryResurrect(pawn);
                    }
                    TM_Action.PossessPawn(caster, pawn, wasDead, previousFaction);
                }
                else
                {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TM_ResistedSpell".Translate(), -1);
                }
                return true;
            }
            return false;
        }

    }	
}


