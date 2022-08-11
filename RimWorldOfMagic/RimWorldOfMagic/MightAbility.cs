using AbilityUser;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using TorannMagic.Ideology;

namespace TorannMagic
{
    public class MightAbility : PawnAbility
    {        

        public int CastingTicks
        {
            get
            {
                return this.MaxCastingTicks;
            }
        }
       
        public CompAbilityUserMight MightUser
        {
            get
            {
                return MightUserUtility.GetMightUser(base.Pawn);
            }
        }

        public TMAbilityDef mightDef
        {
            get
            {
                return base.Def as TMAbilityDef;
            }
        }

        private float ActualStaminaCost
        {
            get
            {
                if (mightDef != null)
                {
                    return this.MightUser.ActualStaminaCost(mightDef);
                }
                return mightDef.staminaCost;         
            }
        }

        private float ActualChiCost
        {
            get
            {
                if (mightDef != null)
                {
                    return this.MightUser.ActualChiCost(mightDef);
                }
                return mightDef.chiCost;
            }
        }

        private static float ActualNeedCost (TMAbilityDef mightDef, CompAbilityUserMight mightUser)
        {

            float num = 1f;
            if (mightDef != null && mightUser.MightData.GetSkill_Efficiency(mightDef) != null)
            {
                num = 1f - (mightDef.efficiencyReductionPercent * mightUser.MightData.GetSkill_Efficiency(mightDef).level);
            }
            return mightDef.needCost * num;
            
        }

        private static float ActualHediffCost (TMAbilityDef mightDef, CompAbilityUserMight mightUser)
        {
            float num = 1f;
            if (mightDef != null && mightUser.MightData.GetSkill_Efficiency(mightDef) != null)
            {
                num = 1f - (mightDef.efficiencyReductionPercent * mightUser.MightData.GetSkill_Efficiency(mightDef).level);
            }
            return mightDef.hediffCost * num;            
        }

        public MightAbility()
        {
        }

        public MightAbility(CompAbilityUser abilityUser) : base(abilityUser)
		{
            this.abilityUser = (abilityUser as CompAbilityUserMight);
        }

        public MightAbility(AbilityData abilityData) : base(abilityData)
		{
            this.abilityUser = (abilityData.Pawn.AllComps.FirstOrDefault((ThingComp x) => x.GetType() == abilityData.AbilityClass) as CompAbilityUserMight);
        }

        public MightAbility(Pawn user, AbilityUser.AbilityDef pdef) : base(user, pdef)
		{

        }

        public override void PostAbilityAttempt()  
        {
            //base.PostAbilityAttempt();
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (!this.Pawn.IsColonist && settingsRef.AIAggressiveCasting)// for AI
            {
                this.CooldownTicksLeft = Mathf.RoundToInt(this.MaxCastingTicks/2f);
            }
            else
            {
                this.CooldownTicksLeft = Mathf.RoundToInt(this.MaxCastingTicks * this.MightUser.coolDown);
            }
            if (Rand.Chance(MightUser.arcalleumCooldown))
            {
                this.CooldownTicksLeft = 4;
            }
            bool flag = this.mightDef != null;
            if (flag)
            {
                if (this.Pawn.IsColonist)
                {
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(TorannMagicDefOf.TM_UsedManeuver, this.Pawn.Named(HistoryEventArgsNames.Doer)));
                }
                if (mightDef.consumeEnergy)
                {
                    bool flag3 = this.MightUser.Stamina != null;
                    if (flag3)
                    {
                        if (!this.Pawn.IsColonist && settingsRef.AIAggressiveCasting)// for AI
                        {
                            this.MightUser.Stamina.UseMightPower(this.MightUser.ActualStaminaCost(mightDef) / 2f);
                        }
                        else
                        {
                            this.MightUser.Stamina.UseMightPower(this.MightUser.ActualStaminaCost(mightDef));
                        }

                        this.MightUser.MightUserXP += (int)((mightDef.staminaCost * 180) * this.MightUser.xpGain * settingsRef.xpMultiplier);

                        TM_EventRecords er = new TM_EventRecords();
                        er.eventPower = this.mightDef.manaCost;
                        er.eventTick = Find.TickManager.TicksGame;
                        this.MightUser.MightUsed.Add(er);                        

                    }
                    if (this.mightDef.chiCost != 0)
                    {
                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_ChiHD, -100 * this.ActualChiCost);
                        this.MightUser.MightUserXP += (int)((mightDef.chiCost * 100) * this.MightUser.xpGain * settingsRef.xpMultiplier);
                    }
                    if(mightDef.requiredHediff != null)
                    {
                        Hediff reqHediff = TM_Calc.GetLinkedHediff(this.Pawn, mightDef.requiredHediff);
                        if (reqHediff != null)
                        {
                            reqHediff.Severity -= ActualHediffCost(mightDef, this.MightUser);
                            this.MightUser.MightUserXP += (int)((mightDef.hediffXPFactor * this.MightUser.xpGain * settingsRef.xpMultiplier) * mightDef.hediffCost);
                        }
                        else
                        {
                            Log.Warning("" + this.Pawn.LabelShort + " attempted to use an ability requiring the hediff " + mightDef.requiredHediff.label + " but does not have the hediff; should never happen since we required the hediff to use the ability.");
                        }
                    }
                    if (mightDef.requiredNeed != null)
                    {
                        if (this.Pawn.needs != null && this.Pawn.needs.AllNeeds != null && this.Pawn.needs.TryGetNeed(this.mightDef.requiredNeed) != null)
                        {
                            Need nd = this.Pawn.needs.TryGetNeed(this.mightDef.requiredNeed);
                            nd.CurLevel -= ActualNeedCost(mightDef, this.MightUser);
                            this.MightUser.MightUserXP += (int)((mightDef.needXPFactor * this.MightUser.xpGain * settingsRef.xpMultiplier) * mightDef.needCost);
                        }
                        else
                        {
                            Log.Warning("" + this.Pawn.LabelShort + " attempted to use an ability requiring the need " + mightDef.requiredNeed.label + " but does not have the need; should never happen since we required the need to use the ability.");
                        }
                    }
                    if ((mightDef.requiredInspiration != null || mightDef.requiresAnyInspiration) && mightDef.consumesInspiration)
                    {
                        this.Pawn.mindState.inspirationHandler.EndInspiration(this.Pawn.Inspiration);
                    }
                }
                if (mightDef.chainedAbility != null)
                {
                    this.MightUser.TryAddPawnAbility(mightDef.chainedAbility);
                    bool expires = false;
                    int expireTicks = -1;
                    if (mightDef.chainedAbilityExpiresAfterTicks >= 0)
                    {
                        expires = true;
                        expireTicks = mightDef.chainedAbilityExpiresAfterTicks;
                    }
                    else if (mightDef.chainedAbilityExpiresAfterCooldown)
                    {
                        expires = true;
                        expireTicks = this.CooldownTicksLeft;
                    }
                    if (expires)
                    {
                        CompAbilityUserMight.ChainedMightAbility cab = new CompAbilityUserMight.ChainedMightAbility(mightDef.chainedAbility, expireTicks, expires);
                        this.MightUser.chainedAbilitiesList.Add(cab);
                    }
                }
                if (mightDef.removeAbilityAfterUse)
                {
                    MightUser.RemovePawnAbility(mightDef);
                }
                if (mightDef.abilitiesRemovedWhenUsed != null && mightDef.abilitiesRemovedWhenUsed.Count > 0)
                {
                    foreach (TMAbilityDef rem in mightDef.abilitiesRemovedWhenUsed)
                    {
                        this.MightUser.RemovePawnAbility(rem);
                    }
                }
            }
        }

        public override string PostAbilityVerbCompDesc(VerbProperties_Ability verbDef)
        {
            TMAbilityDef mightAbilityDef = (TMAbilityDef)verbDef.abilityDef;            
            return PostAbilityDesc(mightAbilityDef, this.MightUser, this.MaxCastingTicks);
        }

        public static string PostAbilityDesc(TMAbilityDef mightAbilityDef, CompAbilityUserMight mightUser, int maxCastingTicks)
        {
            string result = "";
            StringBuilder stringBuilder = new StringBuilder();
            bool flag = mightAbilityDef != null;
            if (flag)
            {
                string text = "";
                string text2 = "";
                string text3 = "";

                float num = 0;
                float num2 = 0;
                num = mightUser.ActualStaminaCost(mightAbilityDef) * 100;
                if (mightAbilityDef == TorannMagicDefOf.TM_Whirlwind)//mightAbilityDef == TorannMagicDefOf.)
                {
                    num2 = FlyingObject_Whirlwind.GetWeaponDmg(mightUser.Pawn);
                    text2 = "TM_WhirlwindDamage".Translate(
                        num2.ToString()
                    );

                }
                else if (mightAbilityDef == TorannMagicDefOf.TM_Cleave)
                {
                    if (mightUser.Pawn.equipment.Primary != null && !mightUser.Pawn.equipment.Primary.def.IsRangedWeapon)
                    {
                        num2 = Mathf.Min((mightUser.Pawn.equipment.Primary.def.BaseMass * .15f) * 100f, 75f);
                        text2 = "TM_CleaveChance".Translate(
                            num2.ToString()
                        );
                    }
                    else
                    {
                        text2 = "TM_CleaveChance".Translate(
                            num2.ToString()
                        );
                    }

                }
                else if (mightAbilityDef == TorannMagicDefOf.TM_ShadowStrike)
                {
                    num2 = Projectile_ShadowStrike.GetWeaponDmg(mightUser.Pawn);
                    text2 = "TM_WeaponDamage".Translate(
                        mightAbilityDef.label,
                        num2.ToString()
                    );
                    if (mightUser.Pawn.equipment.Primary != null && !mightUser.Pawn.equipment.Primary.def.IsRangedWeapon)
                    {                        
                        text3 = "TM_CritChance".Translate(
                            mightAbilityDef.label,
                            mightUser.weaponCritChance.ToString("P0")
                        );
                    }
                    else
                    {
                        text3 = "TM_CritChance".Translate(
                            mightAbilityDef.label,
                            "0"
                        );
                    }

                }
                else if (mightUser.Pawn.equipment.Primary != null && mightUser.Pawn.equipment.Primary.def.IsRangedWeapon)
                {
                    if (mightAbilityDef == TorannMagicDefOf.TM_Headshot)
                    {
                        num2 = Projectile_Headshot.GetWeaponDmg(mightUser.Pawn);
                        text2 = "TM_WeaponDamage".Translate(
                        mightAbilityDef.label,
                        num2.ToString()
                        );
                    }
                    if (mightAbilityDef == TorannMagicDefOf.TM_AntiArmor)
                    {
                        num2 = Projectile_AntiArmor.GetWeaponDmg(mightUser.Pawn);
                        float num3 = Projectile_AntiArmor.GetWeaponDmgMech(mightUser.Pawn, Mathf.RoundToInt(num2));
                        text2 = "TM_AntiArmorDamage".Translate(
                        mightAbilityDef.label,
                        num2.ToString(),
                        num3.ToString()
                        );
                    }
                    if (mightAbilityDef == TorannMagicDefOf.TM_ArrowStorm || mightAbilityDef == TorannMagicDefOf.TM_ArrowStorm_I || mightAbilityDef == TorannMagicDefOf.TM_ArrowStorm_II || mightAbilityDef == TorannMagicDefOf.TM_ArrowStorm_III)
                    {
                        num2 = Projectile_ArrowStorm.GetWeaponDmg(mightUser.Pawn);
                        int num3 = Mathf.RoundToInt(Projectile_ArrowStorm.GetWeaponAccuracy(mightUser.Pawn) * 100f);
                        text2 = "TM_ArrowStormDamage".Translate(
                        mightAbilityDef.label,
                        num2.ToString(),
                        num3.ToString()
                        );
                    }
                    if (mightAbilityDef == TorannMagicDefOf.TM_TempestStrike)
                    {
                        num2 = Projectile_TempestStrike.GetWeaponDmg(mightUser.Pawn);
                        int num3 = Mathf.RoundToInt(Verb_TempestStrike.HitChance(mightUser.Pawn) * 100);
                        text2 = "TM_ArrowStormDamage".Translate(
                        mightAbilityDef.label,
                        (num2.ToString() + " per strike\n"),
                        num3.ToString()
                        );
                    }
                    if (mightAbilityDef == TorannMagicDefOf.TM_SuppressingFire)
                    {
                        num2 = Verb_SuppressingFire.GetShotCount(mightUser.Pawn);
                        text2 = "TM_SuppressingFireCount".Translate(num2).ToString();
                    }
                    if (mightAbilityDef == TorannMagicDefOf.TM_Buckshot)
                    {
                        num2 = Verb_Buckshot.GetShotCount(mightUser.Pawn);
                        text2 = "TM_BuckshotFireCount".Translate(num2).ToString();
                    }

                }
                else if (mightUser.Pawn.equipment.Primary != null && !mightUser.Pawn.equipment.Primary.def.IsRangedWeapon)
                {
                    if (mightAbilityDef == TorannMagicDefOf.TM_PhaseStrike || mightAbilityDef == TorannMagicDefOf.TM_PhaseStrike_I || mightAbilityDef == TorannMagicDefOf.TM_PhaseStrike_II || mightAbilityDef == TorannMagicDefOf.TM_PhaseStrike_III)
                    {
                        num2 = Mathf.RoundToInt(mightUser.weaponDamage * mightAbilityDef.weaponDamageFactor);
                        text2 = "TM_WeaponDamage".Translate(
                        mightAbilityDef.label,
                        num2.ToString()
                        );
                    }
                    if (mightAbilityDef == TorannMagicDefOf.TM_BladeSpin)
                    {
                        num2 = Mathf.RoundToInt(mightUser.weaponDamage * mightAbilityDef.weaponDamageFactor);
                        text2 = "TM_WeaponDamage".Translate(
                        mightAbilityDef.label,
                        num2.ToString()
                        );
                    }
                    if (mightAbilityDef == TorannMagicDefOf.TM_SeismicSlash)
                    {
                        num2 = Mathf.RoundToInt(mightUser.weaponDamage * mightAbilityDef.weaponDamageFactor * (1f + (.1f * mightUser.MightData.GetSkill_Power(mightAbilityDef).level)));
                        text2 = "TM_WeaponDamage".Translate(
                        mightAbilityDef.label,
                        num2.ToString()
                        );
                    }
                    if (mightAbilityDef == TorannMagicDefOf.TM_TempestStrike)
                    {
                        num2 = Projectile_TempestStrike.GetWeaponDmg(mightUser.Pawn);
                        text2 = "TM_WeaponDamage".Translate(
                        mightAbilityDef.label,
                        num2.ToString()
                        ) + " per strike";
                    }                    
                }
                else if (mightUser.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_PsionicHD"), false))
                {
                    if (mightAbilityDef == TorannMagicDefOf.TM_PsionicBlast || mightAbilityDef == TorannMagicDefOf.TM_PsionicBlast_I || mightAbilityDef == TorannMagicDefOf.TM_PsionicBlast_II || mightAbilityDef == TorannMagicDefOf.TM_PsionicBlast_III)
                    {
                        num2 = 4 - (mightUser.MightData.MightPowerSkill_PsionicBlast.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicBlast_ver").level);
                        text2 = "TM_PsionicInitialCost".Translate(
                            20
                        ) + "\n" + "TM_PsionicBlastAddCost".Translate(
                            num2
                        );
                    }
                    if (mightAbilityDef == TorannMagicDefOf.TM_PsionicDash)
                    {
                        num2 = 8 - (mightUser.MightData.MightPowerSkill_PsionicDash.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicDash_eff").level);
                        text2 = "TM_PsionicInitialCost".Translate(
                            num2
                        );
                    }
                    if (mightAbilityDef == TorannMagicDefOf.TM_PsionicBarrier)
                    {
                        num2 = 8 - (mightUser.MightData.MightPowerSkill_PsionicDash.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicDash_eff").level);
                        text2 = "TM_PsionicBarrierMaintenanceCost".Translate(
                            20
                        ) + "\n" + "TM_PsionicBarrierConversionRate".Translate(
                            num2
                        );
                    }
                    if (mightAbilityDef == TorannMagicDefOf.TM_PsionicStorm)
                    {
                        num2 = 65 - (5 * (mightUser.MightData.MightPowerSkill_PsionicStorm.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicStorm_eff").level));
                        text2 = "TM_PsionicInitialCost".Translate(
                            num2
                        );
                    }
                }
                else if (TM_Calc.HasHateHediff(mightUser.Pawn) && (mightAbilityDef == TorannMagicDefOf.TM_Spite || mightAbilityDef == TorannMagicDefOf.TM_Spite_I || mightAbilityDef == TorannMagicDefOf.TM_Spite_II || mightAbilityDef == TorannMagicDefOf.TM_Spite_III))
                {
                    text2 = "TM_RequiresHateAmount".Translate(
                        20
                    );
                }
                //else if (mightUser.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ChiHD, false) && (mightAbilityDef == TorannMagicDefOf.TM_TigerStrike || mightAbilityDef == TorannMagicDefOf.TM_DragonStrike || mightAbilityDef == TorannMagicDefOf.TM_ThunderStrike))
                //{
                //    //displays ability damage for active/passive attacks
                //}
                

                if (mightAbilityDef.chiCost != 0)
                {
                    text = "TM_AbilityDescBaseChiCost".Translate(
                        (mightAbilityDef.chiCost * 100).ToString("n1")
                    ) + "\n" + "TM_AbilityDescAdjustedChiCost".Translate(
                        (mightUser.ActualChiCost(mightAbilityDef)*100).ToString("n1")
                    );
                }
                else if(mightAbilityDef.requiredHediff != null)
                {
                    text = "TM_AbilityDescBaseResourceCost".Translate(mightAbilityDef.requiredHediff.label,
                        ((mightAbilityDef.hediffCost).ToString("n2"))
                    ) + "\n" + "TM_AbilityDescAdjustedResourceCost".Translate(mightAbilityDef.requiredHediff.label,
                        (ActualHediffCost(mightAbilityDef, mightUser).ToString("n2"))
                    );
                }
                else if (mightAbilityDef.requiredNeed != null)
                {
                    text = "TM_AbilityDescBaseResourceCost".Translate(mightAbilityDef.requiredNeed.label,
                        (mightAbilityDef.needCost).ToString("n2")
                    ) + "\n" + "TM_AbilityDescAdjustedResourceCost".Translate(mightAbilityDef.requiredNeed.label,
                        (ActualNeedCost(mightAbilityDef, mightUser).ToString("n2"))
                    );
                }
                else
                {
                    text = "TM_AbilityDescBaseStaminaCost".Translate(
                        (mightAbilityDef.staminaCost * 100).ToString("n1")
                    ) + "\n" + "TM_AbilityDescAdjustedStaminaCost".Translate(
                        num.ToString("n1")
                    );
                }

                if (mightUser.coolDown != 1f && maxCastingTicks != 0)
                {
                    text3 = "TM_AdjustedCooldown".Translate(
                        ((maxCastingTicks * mightUser.coolDown)/60).ToString("0.00")
                    );
                }

                bool flag2 = text != "";
                if (flag2)
                {
                    stringBuilder.AppendLine(text);
                }
                bool flag3 = text2 != "";
                if (flag3)
                {
                    stringBuilder.AppendLine(text2);
                }
                result = stringBuilder.ToString();
                bool flag4 = text3 != "";
                if (flag4)
                {
                    stringBuilder.AppendLine(text3);
                }
                result = stringBuilder.ToString();
            }
            return result;
        }

        public override bool CanCastPowerCheck(AbilityContext context, out string reason)
        {
            bool flag = base.CanCastPowerCheck(context, out reason);
            bool result;
            if (flag)
            {
                reason = "";
                TMAbilityDef tmAbilityDef;
                bool flag1 = base.Def != null && (tmAbilityDef = (base.Def as TMAbilityDef)) != null;
                if (flag1)
                {
                    bool flag4 = this.MightUser.Stamina != null;
                    if (flag4)
                    {
                        bool flag5 = mightDef.staminaCost > 0f && this.ActualStaminaCost > this.MightUser.Stamina.CurLevel;
                        if (flag5)
                        {
                            reason = "TM_NotEnoughStamina".Translate(
                                base.Pawn.LabelShort
                            );
                            result = false;
                            return result;
                        }
                        if (mightDef.chiCost > 0f)
                        {
                            bool flag6 = this.MightUser.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ChiHD, false) ? (this.ActualChiCost * 100) > this.MightUser.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD, false).Severity : true;
                            if (flag6)
                            {
                                reason = "TM_NotEnoughChi".Translate(
                                    base.Pawn.LabelShort
                                );
                                result = false;
                                return result;
                            }
                        }
                        bool flagNeed = mightDef.requiredNeed != null;
                        if (flagNeed)
                        {
                            if (this.MightUser.Pawn.needs.TryGetNeed(mightDef.requiredNeed) != null)
                            {
                                if (this.MightUser.Pawn.needs.TryGetNeed(mightDef.requiredNeed).CurLevel < ActualNeedCost(mightDef, MightUser))
                                {
                                    reason = "TM_NotEnoughEnergy".Translate(
                                        base.Pawn.LabelShort,
                                        mightDef.requiredNeed.label
                                    );
                                    result = false;
                                    return result;
                                }
                                //passes need requirements
                            }
                            else
                            {
                                reason = "TM_NoRequiredNeed".Translate(
                                        base.Pawn.LabelShort,
                                        mightDef.requiredNeed.label
                                    );
                                result = false;
                                return result;
                            }                            
                        }                        
                       
                        bool flagHediff = mightDef.requiredHediff != null;
                        if (flagHediff)
                        {
                            Hediff reqHediff = TM_Calc.GetLinkedHediff(base.Pawn, mightDef.requiredHediff);
                            if (reqHediff != null)
                            {
                                if (reqHediff.Severity < ActualHediffCost(mightDef, MightUser))
                                {
                                    reason = "TM_NotEnoughEnergy".Translate(
                                        base.Pawn.LabelShort,
                                        mightDef.requiredHediff.label
                                    );
                                    result = false;
                                    return result;
                                }
                                //passes hediff requirements
                            }
                            else
                            {
                                reason = "TM_NoRequiredHediff".Translate(
                                        base.Pawn.LabelShort,
                                        mightDef.requiredHediff.label
                                    );
                                result = false;
                                return result;
                            }
                        }
                        
                        bool flagInspiration = mightDef.requiredInspiration != null;
                        if (flagInspiration)
                        {
                            if (base.Pawn.mindState.inspirationHandler != null && base.Pawn.mindState.inspirationHandler.CurStateDef == mightDef.requiredInspiration)
                            {                                
                                //passes hediff requirements
                            }
                            else
                            {
                                reason = "TM_NoRequiredInspiration".Translate(
                                        base.Pawn.LabelShort
                                    );
                                result = false;
                                return result;
                            }
                        }

                        if(mightDef.requiresAnyInspiration)
                        {
                            if(!base.Pawn.Inspired)
                            {
                                reason = "TM_NotInspired".Translate(
                                        base.Pawn.LabelShort,
                                        mightDef.requiredInspiration.label
                                    );
                                result = false;
                                return result;
                            }
                        }
                        
                    }
                }
                if(MightUser.specWpnRegNum == -1 && 
                    (this.mightDef == TorannMagicDefOf.TM_PistolWhip || this.mightDef == TorannMagicDefOf.TM_SuppressingFire || this.mightDef == TorannMagicDefOf.TM_Mk203GL ||
                    this.mightDef == TorannMagicDefOf.TM_Buckshot || this.mightDef == TorannMagicDefOf.TM_BreachingCharge))
                {
                    if (MightUser.Pawn.equipment != null && MightUser.Pawn.equipment.Primary != null)
                    {
                        reason = "TM_MustHaveWeaponType".Translate(
                                base.Pawn.LabelShort,
                                base.Pawn.equipment.Primary.def.label,
                                "specialized weapon"
                            );                        
                    }
                    else
                    {
                        reason = "TM_IncompatibleWeapon".Translate();
                    }
                    return false;
                }
                List<Apparel> wornApparel = base.Pawn.apparel.WornApparel;
                for (int i = 0; i < wornApparel.Count; i++)
                {
                    if (!wornApparel[i].AllowVerbCast(this.Verb) && 
                        (this.mightDef.defName == "TM_Headshot" || 
                        this.mightDef.defName == "TM_DisablingShot" || this.mightDef.defName == "TM_DisablingShot_I" || this.mightDef.defName == "TM_DisablingShot_II" || this.mightDef.defName == "TM_DisablingShot_III" || 
                        this.mightDef.defName == "TM_AntiArmor" || 
                        this.mightDef.defName == "TM_ArrowStorm" || this.mightDef.defName == "TM_ArrowStorm_I" || this.mightDef.defName == "TM_ArrowStorm_II" || this.mightDef.defName == "TM_ArrowStorm_III" ||
                        this.mightDef == TorannMagicDefOf.TM_PsionicStorm ||
                        this.mightDef.defName == "TM_PsionicBlast" || this.mightDef.defName == "TM_PsionicBlast_I" || this.mightDef.defName == "TM_PsionicBlast_II" || this.mightDef.defName == "TM_PsionicBlast_III" || 
                        this.mightDef == TorannMagicDefOf.TM_TempestStrike ||
                        this.mightDef == TorannMagicDefOf.TM_SuppressingFire || this.mightDef == TorannMagicDefOf.TM_Mk203GL ||
                        this.mightDef == TorannMagicDefOf.TM_Buckshot ||
                        this.mightDef.defName == "TM_Mimic"))
                    {
                        reason = "TM_ShieldBlockingPowers".Translate(
                            base.Pawn.Label,
                            wornApparel[i].Label
                        );
                        return false;
                    }
                }
                if(TM_Calc.HasHateHediff(this.MightUser.Pawn) && this.MightUser.Pawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight))
                {
                    Hediff hediff = null;
                    for (int h = 0; h < this.MightUser.Pawn.health.hediffSet.hediffs.Count; h++)
                    {
                        if (this.MightUser.Pawn.health.hediffSet.hediffs[h].def.defName.Contains("TM_HateHD"))
                        {
                            hediff = this.MightUser.Pawn.health.hediffSet.hediffs[h];
                        }
                    }
                    if (hediff != null)
                    {
                        if ((this.mightDef == TorannMagicDefOf.TM_Spite || this.mightDef == TorannMagicDefOf.TM_Spite_I || this.mightDef == TorannMagicDefOf.TM_Spite_II|| this.mightDef == TorannMagicDefOf.TM_Spite_III) && hediff.Severity < 20f)
                        {
                            reason = "TM_NotEnoughHate".Translate(
                            base.Pawn.LabelShort,
                            "Spite"
                            );
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                if (this.MightUser.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_PsionicHD"), false))
                {
                    float psiEnergy = this.MightUser.Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_PsionicHD"), false).Severity;
                    if ((this.mightDef.defName == "TM_PsionicBlast" || this.mightDef.defName == "TM_PsionicBlast_I" || this.mightDef.defName == "TM_PsionicBlast_II" || this.mightDef.defName == "TM_PsionicBlast_III") && psiEnergy < 20f)
                    {
                        reason = "TM_NotEnoughPsionicEnergy".Translate(
                        base.Pawn.Label,
                        "Psionic Blast"
                        );
                        return false;
                    }
                    if ((this.mightDef == TorannMagicDefOf.TM_PsionicDash && psiEnergy < 8f))
                    {
                        reason = "TM_NotEnoughPsionicEnergy".Translate(
                        base.Pawn.Label,
                        "Psionic Dash"
                        );
                        return false;
                    }
                    int stormCost = 65 - (5 * (this.MightUser.MightData.MightPowerSkill_PsionicStorm.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicStorm_eff").level));
                    if ((this.mightDef == TorannMagicDefOf.TM_PsionicStorm && psiEnergy < stormCost))
                    {
                        reason = "TM_NotEnoughPsionicEnergy".Translate(
                        base.Pawn.Label,
                        "Psionic Storm"
                        );
                        return false;
                    }
                }
                TMAbilityDef tmad = this.mightDef;
                if (tmad != null && tmad.requiredWeaponsOrCategories != null && tmad.IsRestrictedByEquipment(this.Pawn))
                {
                    reason = "TM_IncompatibleWeaponType".Translate(
                        base.Pawn.LabelShort,
                        tmad.label);
                    return false;
                }
                result = true;
                
            }
            else
            {
                result = false;
            }
            return result;

        }

    }
}
