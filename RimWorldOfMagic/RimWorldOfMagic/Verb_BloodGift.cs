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
            bool result = false;
            bool arg_40_0;

            Pawn pawn = this.CasterPawn;
            Map map = this.CasterPawn.Map;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            int verVal = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BloodGift.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BloodGift_ver").level;
            int bloodGain = 0;
            List<BodyPartRecord> bodyparts = new List<BodyPartRecord>();
            bodyparts.Clear();
            bodyparts = this.CasterPawn.def.race.body.AllParts;
            List<ThingDef> bloodDefs = TM_Calc.GetAllRaceBloodTypes();
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (pawn != null && !pawn.Downed && bodyparts != null && bloodDefs != null)
            {
                if (verVal > 0)
                {
                    List <IntVec3> cellList = GenRadial.RadialCellsAround(this.CasterPawn.Position, (2*verVal)-1, true).ToList();
                    for (int i = 0; i < cellList.Count; i++)
                    {
                        IntVec3 curcell = cellList[i];
                        List<Thing> thingList = curcell.GetThingList(this.CasterPawn.Map);
                        for (int j = 0; j < thingList.Count; j++)
                        {
                            if(thingList[j].def == ThingDefOf.Filth_Blood || (settingsRef.unrestrictedBloodTypes && bloodDefs.Contains(thingList[j].def)))
                            {                                
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
                    if(bodyparts[i].def.bleedRate != 0 && bodyparts[i].depth == BodyPartDepth.Outside && bodyparts[i].coverageAbs > 0)
                    {
                        validParts.Add(bodyparts[i]);
                    }                    
                }
                if(validParts.Count > 0)
                {
                    if (this.CasterPawn.RaceProps.BloodDef != null && (this.CasterPawn.RaceProps.BloodDef == ThingDefOf.Filth_Blood || settingsRef.unrestrictedBloodTypes))
                    {
                        BodyPartRecord damagePart = validParts.RandomElement();
                        TM_Action.DamageEntities(this.CasterPawn, damagePart, 4f, 10f, TMDamageDefOf.DamageDefOf.TM_BloodyCut, this.CasterPawn);
                        damagePart = validParts.RandomElement();
                        TM_Action.DamageEntities(this.CasterPawn, damagePart, 2f, 10f, TMDamageDefOf.DamageDefOf.TM_BloodyCut, this.CasterPawn);
                        bloodGain += 18;
                        List<Need> needs = this.CasterPawn.needs.AllNeeds;
                        for (int n = 0; n < needs.Count; n++)
                        {
                            Need need = needs[n];
                            if (need.def.defName == "ROMV_Blood")
                            {
                                need.CurLevel--;
                            }
                        }
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_CrossStrike, this.CasterPawn.DrawPos, this.CasterPawn.Map, Rand.Range(.4f, 0.6f), .45f, .05f, .20f, 0, 0, 0, Rand.Range(0, 360));
                        for (int j = 0; j < 4; j++)
                        {
                            IntVec3 rndPos = this.CasterPawn.Position;
                            rndPos.x += Mathf.RoundToInt(Rand.Range(-1.5f, 1.5f));
                            rndPos.z += Mathf.RoundToInt(Rand.Range(-1.5f, 1.5f));
                            FilthMaker.TryMakeFilth(rndPos, this.CasterPawn.Map, this.CasterPawn.RaceProps.BloodDef, Rand.RangeInclusive(1, 2));
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodSquirt, this.CasterPawn.DrawPos, this.CasterPawn.Map, Rand.Range(.7f, 1.1f), .15f, .05f, .66f, Rand.Range(-100, 100), Rand.Range(1, 2), Rand.Range(0, 360), Rand.Range(0, 360));
                        }
                    }
                }
                else
                {
                    if (bloodGain == 0)
                    {
                        Messages.Message("TM_NoExternalBodyPartsCanBleed".Translate(this.CasterPawn.LabelShort), MessageTypeDefOf.RejectInput, false);
                    }
                }
                HealthUtility.AdjustSeverity(this.CasterPawn, HediffDef.Named("TM_BloodHD"), bloodGain * (1 + (.1f * verVal)) * comp.arcaneDmg);
                arg_40_0 = true;
            }
            else
            {
                arg_40_0 = false;
            }
            bool flag = arg_40_0;
            if (flag)
            {
                
            }
            else
            {
                Log.Warning("failed to TryCastShot");
            }
            this.burstShotsLeft = 0;

            return result;
        }
    }
}
