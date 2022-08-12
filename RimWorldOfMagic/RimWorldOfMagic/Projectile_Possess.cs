using Verse;
using AbilityUser;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse.AI;
using Verse.AI.Group;
using System;


namespace TorannMagic
{
    public class Projectile_Possess : Projectile_AbilityBase
    {
        private bool initialized = false;
        private int age = 0;
        private int duration = 1200;
        private int inventoryCount = 0;
        private IntVec3 oldPosition;
        private bool possessedFlag = false;
        Faction pFaction = null;
        Pawn hitPawn = null;
        Pawn caster = null;
        List<int> hitPawnWorkSetting = new List<int>();
        private bool prisoner = false;

        Pawn loadPawn = new Pawn();

        private int verVal;
        private int pwrVal;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.prisoner, "prisoner", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", 0, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1200, false);
            Scribe_Values.Look<int>(ref this.inventoryCount, "inventoryCount", 0, false);
            Scribe_References.Look<Faction>(ref this.pFaction, "pFaction", false);
            Scribe_Values.Look<IntVec3>(ref this.oldPosition, "oldPosition", default(IntVec3), false);
            Scribe_References.Look<Pawn>(ref this.hitPawn, "hitPawn", false);
            Scribe_Deep.Look<Pawn>(ref this.caster, "caster", new object[0]);
            Scribe_Collections.Look<int>(ref this.hitPawnWorkSetting, "hitPawnWorkSettings", LookMode.Value);
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            if (!initialized && this.age < this.duration && hitThing != null)
            {
                caster = this.launcher as Pawn;
                hitPawn = hitThing as Pawn;
                this.oldPosition = caster.Position;
                MightPowerSkill pwr = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Possess.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Possess_pwr");
                MightPowerSkill ver = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Possess.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Possess_ver");
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                pwrVal = pwr.level;
                verVal = ver.level;
                if (settingsRef.AIHardMode && !caster.IsColonist)
                {
                    pwrVal = 3;
                    verVal = 3;
                }
                this.duration += pwrVal * 300;
                if (hitPawn != null && hitPawn.Faction != null && hitPawn.RaceProps.Humanlike)
                {
                    possessedFlag = (hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CoOpPossessionHD) || hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CoOpPossessionHD_I) || hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CoOpPossessionHD_II) || hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CoOpPossessionHD_III) ||
                        hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD) || hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_I) || hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_II) || hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_III));
                    if (!hitPawn.Downed && !hitPawn.Dead && !possessedFlag && !hitPawn.IsPrisoner)
                    {
                        this.pFaction = hitPawn.Faction;
                        this.prisoner = hitPawn.IsPrisoner;
                        if(!caster.IsColonist && hitPawn.IsColonist)
                        {
                            List<WorkTypeDef> allWorkTypes = WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder.ToList();
                            this.hitPawnWorkSetting = new List<int>();
                            this.hitPawnWorkSetting.Clear();
                            for(int i = 0; i < allWorkTypes.Count(); i++)
                            {
                                hitPawnWorkSetting.Add(hitPawn.workSettings.GetPriority(allWorkTypes[i]));
                            }
                        }
                        
                        if (this.pFaction != caster.Faction)
                        {
                            
                            if (Rand.Chance(TM_Calc.GetSpellSuccessChance(caster, hitPawn, true)))
                            {
                                //possess enemy or neutral
                                int weaponCount = 0;
                                if (hitPawn.equipment.PrimaryEq != null)
                                {
                                    weaponCount = 1;
                                }
                                this.inventoryCount = hitPawn.inventory.innerContainer.Count + hitPawn.apparel.WornApparelCount + weaponCount;
                                if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                                {
                                    ModCheck.GiddyUp.ForceDismount(caster);
                                    ModCheck.GiddyUp.ForceDismount(hitPawn);
                                }
                                hitPawn.SetFaction(caster.Faction, null);
                                HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_DisguiseHD_II, 20f + 5f * pwrVal);
                                switch (verVal)
                                {
                                    case 0:
                                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_PossessionHD, 20f + 5f * pwrVal);
                                        break;
                                    case 1:
                                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_PossessionHD_I, 20f + 5f * pwrVal);
                                        break;
                                    case 2:
                                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_PossessionHD_II, 20f + 5f * pwrVal);
                                        break;
                                    case 3:
                                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_PossessionHD_III, 20f + 5f * pwrVal);
                                        break;
                                }
                                initialized = true;
                                FleckMaker.ThrowSmoke(caster.DrawPos, caster.Map, 1f);
                                FleckMaker.ThrowSmoke(caster.DrawPos, caster.Map, 1.2f);
                                FleckMaker.ThrowHeatGlow(caster.Position, caster.Map, .8f);
                                if (!caster.IsColonist)
                                {
                                    Lord lord = caster.GetLord();
                                    LordJob lordJob = caster.GetLord().LordJob;
                                    try
                                    {
                                        PawnDuty duty = caster.mindState.duty;
                                        hitPawn.mindState.duty = duty;
                                        lord.AddPawn(hitPawn);
                                    }
                                    catch
                                    {
                                        Log.Message("error attempting to assign a duty to pawn during possession");
                                    }
                                }
                                //loadPawn = caster;
                                //loadPawn.ThingID += Rand.Range(0, 214).ToString();
                                if(caster.IsColonist)
                                {
                                    //
                                    ModOptions.Constants.SetPawnInFlight(true);
                                    //
                                }
                                if (hitPawn.IsColonist && !caster.IsColonist)
                                {
                                    TM_Action.SpellAffectedPlayerWarning(hitPawn);
                                }
                                caster.DeSpawn();
                                
                            }
                            else
                            {
                                MoteMaker.ThrowText(hitThing.DrawPos, hitThing.Map, "TM_ResistedSpell".Translate(), -1);
                                this.age = this.duration;
                                this.Destroy(DestroyMode.Vanish);
                            }
                        }
                        else
                        {
                            //possess friendly
                            if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                            {
                                ModCheck.GiddyUp.ForceDismount(caster);
                            }
                            HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_DisguiseHD_II, 20f + 5f * pwrVal);
                            switch (verVal)
                            {
                                case 0:
                                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_CoOpPossessionHD, 20f + 5f * pwrVal);
                                    break;
                                case 1:
                                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_CoOpPossessionHD_I, 20f + 5f * pwrVal);
                                    break;
                                case 2:
                                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_CoOpPossessionHD_II, 20f + 5f * pwrVal);
                                    break;
                                case 3:
                                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_CoOpPossessionHD_III, 20f + 5f * pwrVal);
                                    break;
                            }
                            initialized = true;
                            FleckMaker.ThrowSmoke(caster.DrawPos, caster.Map, 1f);
                            FleckMaker.ThrowSmoke(caster.DrawPos, caster.Map, 1.2f);
                            FleckMaker.ThrowHeatGlow(caster.Position, caster.Map, .8f);
                            caster.DeSpawn();
                        }
                    }
                    else
                    {
                        Messages.Message("TM_CannotPossessNow".Translate(
                                caster.LabelShort,
                                hitPawn.LabelShort
                            ), MessageTypeDefOf.RejectInput);
                        this.age = this.duration;
                        this.Destroy(DestroyMode.Vanish);
                    }
                }
                else
                {
                    Messages.Message("TM_CannotPossess".Translate(
                                caster.LabelShort,
                                hitThing.LabelShort
                            ), MessageTypeDefOf.RejectInput);
                    this.age = this.duration;                    
                    this.Destroy(DestroyMode.Vanish);
                }
            }
            else
            {
                if (!this.initialized)
                {
                    this.age = this.duration;
                    Destroy(DestroyMode.Vanish);
                }
            }

            if(hitPawn != null && (hitPawn.Downed || hitPawn.Dead))
            {
                this.age = this.duration;
            }
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age >= duration;
            
            if (flag)
            {
                try
                {
                    if (hitPawn.RaceProps.Humanlike && !this.possessedFlag)
                    {
                        if ((hitPawn.Downed || hitPawn.Dead) && !pFaction.HostileTo(caster.Faction) && pFaction != this.caster.Faction)
                        {
                            //pFaction.TrySetRelationKind(this.caster.Faction, FactionRelationKind.Hostile, true, null);
                            pFaction.TryAffectGoodwillWith(this.caster.Faction, -100, true, true, TorannMagicDefOf.TM_OffensiveMagic, hitPawn);
                        }
                        bool flag2 = caster.Spawned;
                        if (!flag2)
                        {
                            GenPlace.TryPlaceThing(caster, this.oldPosition, this.Map, ThingPlaceMode.Near, null, null);
                            if (caster.IsColonist)
                            {
                                //
                                ModOptions.Constants.SetPawnInFlight(false);
                                //
                            }
                        }
                        if(!caster.Spawned)
                        {
                            GenSpawn.Spawn(this.launcher, this.oldPosition, this.Map, WipeMode.Vanish);
                            if (caster.IsColonist)
                            {
                                //
                                ModOptions.Constants.SetPawnInFlight(false);
                                //
                            }
                        }
                        bool flag3 = hitPawn.Faction != pFaction;
                        if (flag3)
                        {
                            if(prisoner)
                            {
                                hitPawn.guest.SetGuestStatus(this.caster.Faction, GuestStatus.Guest);
                            }
                            else
                            {
                                hitPawn.SetFaction(pFaction, null);
                            }
                        }
                        int weaponCount = 0;
                        if (hitPawn.equipment.PrimaryEq != null)
                        {
                            weaponCount = 1;
                        }
                        int tempInvCount = hitPawn.inventory.innerContainer.Count + hitPawn.apparel.WornApparelCount + weaponCount;
                        if (tempInvCount < this.inventoryCount && !pFaction.HostileTo(caster.Faction) && pFaction != this.caster.Faction)
                        {
                            pFaction.TryAffectGoodwillWith(this.caster.Faction, -200, true, true, null, null);

                            Find.LetterStack.ReceiveLetter("LetterLabelPossessedCaughtStealing".Translate(), "TM_PossessedCaughtStealing".Translate(
                                hitPawn.Faction,
                                hitPawn.LabelShort
                                ), LetterDefOf.NegativeEvent, null);
                        }
                        if (hitPawn.IsColonist)
                        {
                            //hitPawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
                        }
                        if(!caster.IsColonist && hitPawn.IsColonist)
                        {
                            List<WorkTypeDef> allWorkTypes = WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder.ToList();
                            for (int i = 0; i < hitPawnWorkSetting.Count(); i++)
                            {
                                hitPawn.workSettings.SetPriority(allWorkTypes[i], hitPawnWorkSetting[i]);
                            }
                            CompAbilityUserMagic comp = hitPawn.GetCompAbilityUserMagic();
                            if(comp != null && comp.IsMagicUser)
                            {
                                comp.magicPowersInitializedForColonist = true;
                            }
                        }
                        RemoveHediffs();
                    }
                    base.Destroy(mode);
                }
                catch(NullReferenceException ex)
                {
                    base.Destroy(mode);
                }
            }
            
        }

        public void RemoveHediffs()
        {
            Hediff disguiseHD = null;
            Hediff possessHD = null;
            Hediff possessCHD = null;
            using (IEnumerator<Hediff> enumerator = hitPawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Hediff rec = enumerator.Current;
                    if (rec.def == TorannMagicDefOf.TM_DisguiseHD_II)
                    {
                        disguiseHD = rec;
                    }
                    if (rec.def == TorannMagicDefOf.TM_CoOpPossessionHD || rec.def == TorannMagicDefOf.TM_CoOpPossessionHD_I || rec.def == TorannMagicDefOf.TM_CoOpPossessionHD_II || rec.def == TorannMagicDefOf.TM_CoOpPossessionHD_III)
                    {
                        possessCHD = rec;
                    }
                    if (rec.def == TorannMagicDefOf.TM_PossessionHD || rec.def == TorannMagicDefOf.TM_PossessionHD_I || rec.def == TorannMagicDefOf.TM_PossessionHD_II || rec.def == TorannMagicDefOf.TM_PossessionHD_III)
                    {
                        possessHD = rec;
                    }
                }
            }
            if(disguiseHD != null)
            {
                this.hitPawn.health.RemoveHediff(disguiseHD);
            }
            if(possessHD != null)
            {
                this.hitPawn.health.RemoveHediff(possessHD);
            }
            if (possessCHD != null)
            {
                this.hitPawn.health.RemoveHediff(possessCHD);
            }
            
        }
    }
}
