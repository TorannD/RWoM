using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_Undead : HediffComp
    {
        private bool necroValid = true;
        private int lichStrike = 0;
        private bool initialized = false;

        public Pawn linkedPawn = null;
        private static readonly string[] nonStandardNeedsToAutoFulfill = new[] {
            "Mood",
            "Suppression",
            "Bladder", //Dubs Bad Hygiene
            "Hygiene", //Dubs Bad Hygiene
            "DBHThirst" //Dubs Bad Hygiene
        };

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Pawn>(ref linkedPawn, "linkedPawn", false);
        }

        public override string CompLabelInBracketsExtra => linkedPawn != null ? linkedPawn.LabelShort + base.CompLabelInBracketsExtra : base.CompLabelInBracketsExtra;

        public string labelCap
        {
            get
            {
                if (this.linkedPawn != null)
                {
                    return base.Def.LabelCap + "(" + this.linkedPawn.LabelShort + ")";
                }
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                if (this.linkedPawn != null)
                {
                    return base.Def.label + "(" + this.linkedPawn.LabelShort + ")";
                }
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if(this.Pawn.IsSlave)
            {
                this.Pawn.guest.SetGuestStatus(null);
            }
            if (spawned)
            {
                //FleckMaker.ThrowLightningGlow(base.Pawn.TrueCenter(), base.Pawn.Map, 3f);
            }            
        }

        private void UpdateHediff()
        {
            if(this.linkedPawn != null)
            {
                CompAbilityUserMagic comp = linkedPawn.GetCompAbilityUserMagic();
                try
                {
                    if (comp != null)
                    {
                        //int ver = TM_Calc.GetMagicSkillLevel(linkedPawn, comp.MagicData.MagicPowerSkill_RaiseUndead, "TM_RaiseUndead", "_ver");
                        int ver = TM_Calc.GetSkillVersatilityLevel(linkedPawn, TorannMagicDefOf.TM_RaiseUndead, false);
                        if (this.parent.Severity != ver + .5f)
                        {
                            this.parent.Severity = .5f + ver;
                        }
                        if(this.Pawn.IsPrisoner || this.Pawn.Faction != linkedPawn.Faction)
                        {
                            base.Pawn.Kill(null, null);
                        }
                    }
                }
                catch
                {
                    base.Pawn.Kill(null, null);
                }

            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }
            }
            if(Find.TickManager.TicksGame % 16 == 0)
            {
                foreach(Hediff hd in Pawn.health.hediffSet.hediffs)
                {
                    if(hd.def.defName == "SpaceHypoxia")
                    {
                        this.Pawn.health.RemoveHediff(hd);
                        break;
                    }
                }
            }
            if (Find.TickManager.TicksGame % 6000 == 0 && linkedPawn != null)
            {
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(TorannMagicDefOf.TM_UsedMagic, linkedPawn.Named(HistoryEventArgsNames.Doer), linkedPawn.Named(HistoryEventArgsNames.Subject), linkedPawn.Named(HistoryEventArgsNames.AffectedFaction), linkedPawn.Named(HistoryEventArgsNames.Victim)), true);
                TM_Action.UpdateAnimalTraining(base.Pawn);                
            }
            bool flag4 = Find.TickManager.TicksGame % 600 == 0 && this.Pawn.def != TorannMagicDefOf.TM_SkeletonR && this.Pawn.def != TorannMagicDefOf.TM_GiantSkeletonR;
            if (flag4 && !this.Pawn.Dead)
            {                
                UpdateHediff();
                Pawn pawn = base.Pawn;
                necroValid = false;
                if (base.Pawn != null && !linkedPawn.DestroyedOrNull())
                {
                    necroValid = true;
                    lichStrike = 0;

                    if (ModsConfig.IdeologyActive && !this.Pawn.Downed && this.Pawn.guest != null && this.Pawn.IsColonist)
                    {
                        TM_Action.TryCopyIdeo(linkedPawn, this.Pawn);
                        if (this.Pawn.guest.GuestStatus != GuestStatus.Slave)
                        {
                            this.Pawn.guest.SetGuestStatus(linkedPawn.Faction, GuestStatus.Slave);
                        }                        
                    }                    
                }
                else
                {
                    lichStrike++;
                }   
                
                if (!necroValid && !Pawn.Dead && ((pawn.IsColonist && lichStrike > 2) || (!pawn.IsColonist && lichStrike >= 1)))
                {
                    if (base.Pawn.Map != null)
                    {
                        TM_MoteMaker.ThrowScreamMote(base.Pawn.Position.ToVector3(), base.Pawn.Map, .8f, 255, 255, 255);
                    }
                    base.Pawn.Kill(null, null);
                }
                else
                {
                    List<Need> needs = base.Pawn?.needs?.AllNeeds;
                    if (needs != null && needs.Count > 0)
                    { 
                        for (int i = 0; i < needs.Count; i++)
                        {
                            if (needs[i]?.def == NeedDefOf.Food || nonStandardNeedsToAutoFulfill.Contains(needs[i]?.def?.defName))
                            {
                                needs[i].CurLevel = needs[i].MaxLevel;
                            }
                        }
                    }
                    
                    //if (base.Pawn.needs.food != null)
                    //{
                    //    base.Pawn.needs.food.CurLevel = base.Pawn.needs.food.MaxLevel;
                    //}
                    //if (base.Pawn.needs.rest != null)
                    //{
                    //    base.Pawn.needs.rest.CurLevel = base.Pawn.needs.rest.MaxLevel;
                    //}

                    //if (base.Pawn.IsColonist)
                    //{
                    //    base.Pawn.needs.beauty.CurLevel = .5f;
                    //    base.Pawn.needs.comfort.CurLevel = .5f;
                    //    base.Pawn.needs.joy.CurLevel = .5f;
                    //    base.Pawn.needs.mood.CurLevel = .5f;
                    //    base.Pawn.needs.space.CurLevel = .5f;
                    //}

                    Hediff_Injury injuryToHeal = Pawn.health.hediffSet.hediffs
                        .OfType<Hediff_Injury>()
                        .FirstOrDefault();
                    injuryToHeal?.Heal(injuryToHeal.CanHealNaturally() ? 2.0f : 1.0f);


                    foreach (Hediff hediff in pawn.health.hediffSet.GetHediffsTendable())
                    {
                        if (hediff.Bleeding && hediff is Hediff_MissingPart)
                            Traverse.Create(root: hediff).Field(name: "isFreshInt").SetValue(false);
                        else
                            TM_Action.TendWithoutNotice(hediff, 1f, 1f);
                    }

                    List<Hediff> removeHDList = new List<Hediff>();
                    removeHDList.Clear();

                    using (IEnumerator<Hediff> enumerator = pawn.health.hediffSet.hediffs.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Hediff rec = enumerator.Current;
                            if (rec.def.makesSickThought)
                            {
                                removeHDList.Add(rec);
                            }
                            else if (!rec.IsPermanent())
                            {
                                if (rec.def.defName == "Cataract"
                                    || rec.def.defName == "HearingLoss"
                                    || rec.def.defName.Contains("ToxicBuildup")
                                    || rec.def.defName == "Blindness"
                                    || rec.def.defName.Contains("Asthma")
                                    || rec.def.defName == "Abasia" 
                                    || rec.def.defName == "BloodRot"
                                    || rec.def.defName == "Scaria"
                                    || rec.def.defName == "Cirrhosis"
                                    || rec.def.defName == "ChemicalDamageModerate"
                                    || rec.def.defName == "Frail"
                                    || rec.def.defName == "BadBack"
                                    || rec.def.defName.Contains("Carcinoma")
                                    || rec.def.defName == "ChemicalDamageSevere"
                                    || rec.def.defName.Contains("Alzheimers")
                                    || rec.def.defName == "Dementia"
                                    || rec.def.defName.Contains("HeartArteryBlockage")
                                    || rec.def.defName == "CatatonicBreakdown"
                                    || rec.def.defName.Contains("Pregnant")
                                    || rec.def.defName == "DrugOverdose")
                                {
                                    removeHDList.Add(rec);
                                }

                            }
                        }
                    }

                    foreach (Hediff hd in removeHDList)
                    {
                        pawn.health.RemoveHediff(hd);
                    }

                    CompHatcher cp_h = this.Pawn.TryGetComp<CompHatcher>();
                    if (cp_h != null)
                    {
                        Traverse.Create(root: cp_h).Field(name: "gestateProgress").SetValue(0);
                    }
                    CompMilkable cp_m = this.Pawn.TryGetComp<CompMilkable>();
                    if (cp_m != null)
                    {
                        Traverse.Create(root: cp_m).Field(name: "fullness").SetValue(0);
                    }

                }
                
            }
            
        }
    }
}
