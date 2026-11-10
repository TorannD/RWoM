using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TorannMagic
{
    public class Verb_BloodGift : Verb_UseAbility  
    {
        protected override bool TryCastShot()
        {
            Apply(CasterPawn, Ability);
            burstShotsLeft = 0;
            // This self verb handles its effect directly and emits no projectile.
            return false;
        }

        internal static bool Apply(Pawn pawn, PawnAbility ability, Action beginEffect = null)
        {
            // This ritual consumes map filth and inflicts physical cuts. A caravan
            // or an interrupted cast must never execute it without a live caster.
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead || pawn.Downed) return false;
            Map map = pawn.Map;
            var skillOwner = ClassProgression.SpiritRebirthCasting.SkillOwner(pawn, ability);
            var comp = skillOwner?.GetCompAbilityUserMagic();
            if (comp == null) return false;
            int verVal = comp.MagicData.MagicPowerSkill_BloodGift.FirstOrDefault(x => x.label == "TM_BloodGift_ver")?.level ?? 0;
            int bloodGain = 0;
            List<BodyPartRecord> bodyparts = new List<BodyPartRecord>();
            bodyparts.Clear();
            bodyparts = pawn.def.race.body.AllParts;
            List<ThingDef> bloodDefs = TM_Calc.GetAllRaceBloodTypes();
            
            if (pawn != null && !pawn.Downed && bodyparts != null && bloodDefs != null)
            {
                if (verVal > 0)
                {
                    List <IntVec3> cellList = GenRadial.RadialCellsAround(pawn.Position, (2*verVal)-1, true).ToList();
                    for (int i = 0; i < cellList.Count; i++)
                    {
                        IntVec3 curcell = cellList[i];
                        if (!curcell.InBounds(map)) continue;
                        List<Thing> thingList = curcell.GetThingList(map);
                        for (int j = thingList.Count - 1; j >= 0; j--)
                        {
                            if(thingList[j].def == ThingDefOf.Filth_Blood || (ModOptions.Settings.Instance.unrestrictedBloodTypes && bloodDefs.Contains(thingList[j].def)))
                            {                                
                                beginEffect?.Invoke();
                                beginEffect = null;
                                bloodGain += thingList[j].stackCount;
                                thingList[j].Destroy(DestroyMode.Vanish);                                
                            }
                        }                        
                    }
                }

                List<BodyPartRecord> validParts = new List<BodyPartRecord>();
                validParts.Clear();
                for(int i = 0; i < bodyparts.Count; i++)
                {
                    if(BloodGiftSafety.CanCut(pawn, bodyparts[i], 6f))
                    {
                        validParts.Add(bodyparts[i]);
                    }                    
                }
                if(validParts.Count > 0)
                {
                    if (pawn.RaceProps.BloodDef != null && (pawn.RaceProps.BloodDef == ThingDefOf.Filth_Blood || ModOptions.Settings.Instance.unrestrictedBloodTypes))
                    {
                        beginEffect?.Invoke();
                        beginEffect = null;
                        BodyPartRecord damagePart = validParts.RandomElement();
                        TM_Action.DamageEntities(pawn, damagePart, 4f, 10f, TMDamageDefOf.DamageDefOf.TM_BloodyCut, pawn);
                        damagePart = validParts.RandomElement();
                        TM_Action.DamageEntities(pawn, damagePart, 2f, 10f, TMDamageDefOf.DamageDefOf.TM_BloodyCut, pawn);
                        bloodGain += 18;
                        List<Need> needs = pawn.needs.AllNeeds;
                        for (int n = 0; n < needs.Count; n++)
                        {
                            Need need = needs[n];
                            if (need.def.defName == "ROMV_Blood")
                            {
                                need.CurLevel--;
                            }
                        }
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_CrossStrike, pawn.DrawPos, pawn.Map, Rand.Range(.4f, 0.6f), .45f, .05f, .20f, 0, 0, 0, Rand.Range(0, 360));
                        for (int j = 0; j < 4; j++)
                        {
                            IntVec3 rndPos = pawn.Position;
                            rndPos.x += Mathf.RoundToInt(Rand.Range(-1.5f, 1.5f));
                            rndPos.z += Mathf.RoundToInt(Rand.Range(-1.5f, 1.5f));
                            FilthMaker.TryMakeFilth(rndPos, pawn.Map, pawn.RaceProps.BloodDef, Rand.RangeInclusive(1, 2));
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodSquirt, pawn.DrawPos, pawn.Map, Rand.Range(.7f, 1.1f), .15f, .05f, .66f, Rand.Range(-100, 100), Rand.Range(1, 2), Rand.Range(0, 360), Rand.Range(0, 360));
                        }
                    }
                }
                else
                {
                    if (bloodGain == 0)
                    {
                        Messages.Message("TM_NoExternalBodyPartsCanBleed".Translate(pawn.LabelShort), MessageTypeDefOf.RejectInput, false);
                    }
                }
                HealthUtility.AdjustSeverity(pawn, HediffDef.Named("TM_BloodHD"), bloodGain * (1 + (.1f * verVal)) * comp.arcaneDmg);
                return bloodGain > 0;
            }
            return false;
        }
    }
}