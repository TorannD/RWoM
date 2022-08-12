using RimWorld;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace TorannMagic
{
    public class Need_Mana : Need  
    {
        public const float BaseGainPerTickRate = 150f;

        public const float BaseFallPerTick = 1E-05f;

        public const float ThreshVeryLow = 0.1f;

        public const float ThreshLow = 0.3f;

        public const float ThreshSatisfied = 0.5f;

        public const float ThreshHigh = 0.7f;

        public const float ThreshVeryHigh = 0.9f;

        public float lastNeed;
        private float lastCast = 0;

        public float drainMinion;
        public float drainUndead;
        public float drainManaWeakness; //arcane weakness
        public float drainManaSickness;
        public float drainManaDrain;
        public float drainManaSurge;
        public float drainSyrrium;
        public float drainSprites;
        public float drainSustainers; //not used
        public float drainStructures; //not used
        public float drainEnchantments; //not used
        public float drainEnergyHD;
        public float drainSigils;
        public float modifiedManaGain;
        public float baseManaGain;

        public float lastGainPct;

        public int ticksUntilBaseSet = 500;

        private int lastGainTick;

        public float paracyteCountReduction = 0;
        private int lastParacyteCheck = 0;

        protected new float curLevelInt;

        public override float CurLevel
        {            
            get => curLevelInt;
            set => curLevelInt = Mathf.Clamp(value, 0f, 2f*this.pawn.GetCompAbilityUserMagic().maxMP);            
        }

        public override float MaxLevel => this.pawn.GetCompAbilityUserMagic().maxMP;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref curLevelInt, "curLevelInt", 1f);
        }

        public ManaPoolCategory CurCategory
        {
            get
            {
                bool flag = this.CurLevel < 0.1f;
                ManaPoolCategory result;
                if (flag)
                {
                    result = ManaPoolCategory.Drained;
                }
                else
                {
                    bool flag2 = this.CurLevel < 0.3f;
                    if (flag2)
                    {
                        result = ManaPoolCategory.Weakened;
                    }
                    else
                    {
                        bool flag3 = this.CurLevel < 0.5f;
                        if (flag3)
                        {
                            result = ManaPoolCategory.Steady;
                        }
                        else
                        {
                            bool flag4 = this.CurLevel < 0.7f;
                            if (flag4)
                            {
                                result = ManaPoolCategory.Flowing;
                            }
                            else
                            {
                                result = ManaPoolCategory.Surging;
                            }
                        }
                    }
                }
                return result;
            }
        }

        public override int GUIChangeArrow
        {
            get
            {
                return this.GainingNeed ? 1 : -1;
            }
        }

        public override float CurInstantLevel
        {
            get
            {
                return this.CurLevel;
            }
        }

        private bool GainingNeed
        {
            get
            {
                return Find.TickManager.TicksGame < this.lastGainTick + 10;
            }
        }

        static Need_Mana()
        {
        }

        public Need_Mana(Pawn pawn) : base(pawn)
		{
            this.lastGainTick = -999;
            this.threshPercents = new List<float>();
            this.threshPercents.Add((0.25f / this.MaxLevel));
            this.threshPercents.Add((0.5f / this.MaxLevel));
            this.threshPercents.Add((0.75f / this.MaxLevel));         
        }

        private void AdjustThresh()
        {
            this.threshPercents.Clear();
            this.threshPercents.Add((0.25f / this.MaxLevel));
            this.threshPercents.Add((0.5f / this.MaxLevel));
            this.threshPercents.Add((0.75f / this.MaxLevel));
            if (this.MaxLevel > 1)
            {
                this.threshPercents.Add((1f / this.MaxLevel));
            }
            if (this.MaxLevel > 1.25f)
            {
                this.threshPercents.Add((1.25f / this.MaxLevel));
            }
            if (this.MaxLevel > 1.5f)
            {
                this.threshPercents.Add((1.5f / this.MaxLevel));
            }
            if (this.MaxLevel > 1.75f)
            {
                this.threshPercents.Add((1.75f / this.MaxLevel));
            }
            if (this.MaxLevel > 2f)
            {
                this.threshPercents.Add((2f / this.MaxLevel));
            }
            if (this.MaxLevel > 2.5f)
            {
                this.threshPercents.Add((2.5f / this.MaxLevel));
            }
            if (this.MaxLevel > 3f)
            {
                this.threshPercents.Add((3f / this.MaxLevel));
            }
            if (this.MaxLevel > 4f)
            {
                this.threshPercents.Add((4f / this.MaxLevel));
            }
            if (this.MaxLevel > 5f)
            {
                this.threshPercents.Add((5f / this.MaxLevel));
            }
        }

        public override void SetInitialLevel()
        {
            if(this.pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                this.def.showOnNeedList = false;
            }
            this.CurLevel = 0.8f;
        }

        public void GainNeed(float amount)
        {            
            if (!base.pawn.DestroyedOrNull() && !base.pawn.Dead && base.pawn.story != null && base.pawn.story.traits != null && !base.pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {                
                Pawn pawn = base.pawn;
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                if (comp != null && comp.MagicData != null && comp.IsMagicUser && pawn.Faction != null)
                {                    
                    if (!pawn.Faction.IsPlayer && pawn.Map != null)
                    {
                        amount *= (0.025f);
                        amount = Mathf.Min(amount, this.MaxLevel - this.CurLevel);
                        this.curLevelInt += amount;
                    }
                    else
                    {
                        ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();                        
                        MagicPowerSkill manaRegen = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_global_regen.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_global_regen_pwr");
                        this.baseManaGain = (amount * (0.0012f) * settingsRef.needMultiplier);
                        amount *= (((0.0012f * comp.mpRegenRate)) * settingsRef.needMultiplier);
                        this.modifiedManaGain = amount - this.baseManaGain;

                        if (pawn.health != null && pawn.health.hediffSet != null)
                        {
                            if(comp.BrandPawns != null && comp.BrandPawns.Count > 0)
                            {
                                drainSigils = comp.BrandPawns.Count * (TorannMagicDefOf.TM_Branding.upkeepRegenCost * (1f - (TorannMagicDefOf.TM_Branding.upkeepEfficiencyPercent * comp.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Branding).level)));
                                if(comp.sigilSurging)
                                {
                                    drainSigils *= 3f;
                                }
                            }
                            else
                            {
                                drainSigils = 0;
                            }

                            Hediff hdRegen = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnergyRegenHD);
                            if (hdRegen != null)
                            {
                                drainEnergyHD = (amount * hdRegen.Severity) - amount;
                                amount *= hdRegen.Severity;
                            }
                            else
                            {
                                drainEnergyHD = 0;
                            }
                        
                            //Syrrium modifier
                            if (this.pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_SyrriumSenseHD"), false))
                            {
                                this.drainSyrrium = this.baseManaGain * .5f;
                                amount += this.drainSyrrium;                            
                            }
                            else if(this.pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_PomanaSenseHD"), false))
                            {
                                this.drainSyrrium = this.baseManaGain * .2f;
                                amount += this.drainSyrrium;
                            }
                            else
                            {
                                this.drainSyrrium = 0;
                            }

                            //Mana drain modifier
                            if (pawn.Map != null && pawn.Map.GameConditionManager.ConditionIsActive(TorannMagicDefOf.ManaDrain))
                            {
                                this.drainManaDrain = 2*amount;
                                //Arcane weakness modifier
                                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ArcaneWeakness))
                                {
                                    this.drainManaWeakness = .75f * this.modifiedManaGain;
                                }
                                else
                                {
                                    this.drainManaWeakness = 0;
                                }
                                amount = (-1 * amount) - this.drainManaWeakness;                            
                                this.drainManaSurge = 0f;
                            }
                            else if (pawn.Map != null && pawn.Map.GameConditionManager.ConditionIsActive(TorannMagicDefOf.ManaSurge))
                            {
                                //Arcane weakness modifier
                                this.drainManaSurge = (2.25f * amount) - amount;
                                amount = (2.25f * amount);
                                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ArcaneWeakness))
                                {
                                    this.drainManaWeakness = .75f * this.baseManaGain;
                                }
                                else
                                {
                                    this.drainManaWeakness = 0;
                                }
                                amount -= this.drainManaWeakness;
                                this.drainManaDrain = 0;                            
                            }
                            else
                            {
                                //Arcane weakness modifier
                                if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ArcaneWeakness))
                                {
                                    this.drainManaWeakness = .75f * this.baseManaGain;                                
                                }
                                else
                                {
                                    this.drainManaWeakness = 0;
                                }
                                amount -= this.drainManaWeakness;
                                this.drainManaDrain = 0;
                                this.drainManaSurge = 0;
                            }
                        }
                        else
                        {
                            drainManaWeakness = 0;
                            drainManaSurge = 0;
                            drainManaDrain = 0;
                            drainEnergyHD = 0;
                            drainSyrrium = 0;
                            drainSigils = 0;
                        }

                        //Paracyte modifier
                        if (pawn.Map != null && this.lastParacyteCheck < Find.TickManager.TicksGame + 3000)
                        {
                            List<Thing> paracyteBushes = this.pawn.Map.listerThings.ThingsOfDef(TorannMagicDefOf.TM_Plant_Paracyte);
                            int paracyteCount = paracyteBushes.Count;
                            List<Pawn> mapPawns = this.pawn.Map.mapPawns.AllPawnsSpawned;
                            int mageCount = 0;
                            if (settingsRef.paracyteMagesCount)
                            {
                                for (int i = 0; i < mapPawns.Count; i++)
                                {
                                    if (mapPawns[i] != null && mapPawns[i].Spawned && !mapPawns[i].Dead && !mapPawns[i].AnimalOrWildMan())
                                    {
                                        CompAbilityUserMagic mageCheck = mapPawns[i].GetCompAbilityUserMagic();
                                        if (mageCheck != null && mageCheck.IsMagicUser && !mapPawns[i].story.traits.HasTrait(TorannMagicDefOf.Faceless))
                                        {
                                            mageCount++;
                                        }
                                    }
                                }
                            }

                            int mapManaDrainerCount = paracyteCount + (2 * mageCount);
                            if (mapManaDrainerCount > settingsRef.paracyteSoftCap)
                            {
                                mapManaDrainerCount -= Mathf.RoundToInt(settingsRef.paracyteSoftCap);
                            }
                            else
                            {
                                mapManaDrainerCount = 0;
                            }
                            this.paracyteCountReduction = 0.000005f * mapManaDrainerCount;
                            amount -= this.paracyteCountReduction;
                        }

                        //Summoned minion modifier
                        if (comp.summonedMinions.Count > 0)
                        {
                            MagicPowerSkill summonerEff = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonMinion.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SummonMinion_eff");
                            this.drainMinion = (0.0012f * (comp.summonedMinions.Count * (.2f - (.01f * summonerEff.level))));
                            amount -= this.drainMinion;
                        }
                        else
                        {
                            this.drainMinion = 0;
                        }

                        //Earth sprite modifier
                        if(comp.earthSpriteType != 0)
                        {
                            MagicPowerSkill manaDeviant = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EarthSprites.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EarthSprites_ver");
                            this.drainSprites = (0.0012f * (.6f - (.07f * manaDeviant.level)));
                            amount -= this.drainSprites;
                        }
                        else
                        {
                            this.drainSprites = 0;
                        }

                        //Undead modifier
                        float necroReduction = 0;
                        int necroCount = 0;
                        float undeadCount = 0;

                        if (settingsRef.undeadUpkeepMultiplier > 0f && comp.supportedUndead != null && comp.supportedUndead.Count > 0)
                        {
                            Apparel orb = TM_Calc.GetNecroticOrb(this.pawn);
                            float orbEnergy = 0;
                            float orbCount = 0;
                            if (orb != null)
                            {
                                Enchantment.CompEnchantedItem orbComp = orb.GetComp<Enchantment.CompEnchantedItem>();
                                if(orbComp != null && orbComp.NecroticEnergy > 0)
                                {
                                    orbEnergy = orbComp.NecroticEnergy;
                                    orbCount++;
                                }                                
                            }
                            foreach (Pawn current in comp.supportedUndead)
                            {
                                if (current.RaceProps.Humanlike)
                                {
                                    if (current.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD))
                                    {
                                        undeadCount += 2;
                                        if(current.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BrittleBonesHD) || current.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SlaggedHD))
                                        {
                                            undeadCount++;
                                        }
                                    }
                                }
                                if (current.RaceProps.Animal)
                                {
                                    if (current.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD))
                                    {
                                        if (current.kindDef != null && current.kindDef.combatPower != 0)
                                        {
                                            undeadCount += (current.kindDef.combatPower / 100);
                                        }
                                        else
                                        {
                                            undeadCount++;
                                        }
                                    }
                                }
                            }                            
                            MagicPowerSkill eff = comp.MagicData.MagicPowerSkill_RaiseUndead.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_RaiseUndead_eff");
                            if (comp.Mana != null && comp.Mana.CurLevel < 0.01f)
                            {
                                if (orbEnergy > 0)
                                {
                                    Enchantment.CompEnchantedItem itemComp = orb.GetComp<Enchantment.CompEnchantedItem>();
                                    if (itemComp != null)
                                    {
                                        itemComp.NecroticEnergy -= (0.12f * .15f * undeadCount);
                                        undeadCount = 0;
                                    }                                    
                                }
                                else
                                {
                                    if (this.CurLevel < 0.01f && undeadCount > 0)
                                    {
                                        //consume corpse
                                        Pawn current = (Pawn)comp.supportedUndead.RandomElement();
                                        Messages.Message("TM_UndeadCollapsed".Translate(
                                            pawn.LabelShort,
                                            current.LabelShort
                                        ), MessageTypeDefOf.NegativeEvent);
                                        comp.supportedUndead.Remove(current);
                                        if (current.MapHeld != null)
                                        {
                                            if (!current.RaceProps.Animal)
                                            {
                                                current.inventory.DropAllNearPawn(current.Position, false, true);
                                                current.equipment.DropAllEquipment(current.Position, false);
                                                current.apparel.DropAll(current.Position, false);
                                            }
                                            TM_MoteMaker.ThrowBloodSquirt(current.Position.ToVector3Shifted(), current.Map, 2.5f);
                                        }
                                        else if (current.ParentHolder != null && current.ParentHolder is Caravan van)
                                        {                                          
                                            van.RemovePawn(current);
                                        }
                                        current.Destroy();
                                        undeadCount--;
                                        this.curLevelInt = .12f + (.025f * manaRegen.level);
                                    }                                    
                                }
                            }
                            else
                            {
                                if (orb != null && orbEnergy <= 100)
                                {
                                    undeadCount += orbCount * 2;
                                    Enchantment.CompEnchantedItem itemComp = orb.GetComp<Enchantment.CompEnchantedItem>();
                                    if (itemComp != null)
                                    {
                                        itemComp.NecroticEnergy += (0.036f);
                                    }
                                }
                            }
                            float orbReduction = 1f;
                            if(orb != null && orbEnergy > 0)
                            {
                                orbReduction = .75f;
                            }
                            
                            necroReduction = (((0.0012f * (.15f - (.15f * (.1f * eff.level))) * undeadCount) * orbReduction) * settingsRef.undeadUpkeepMultiplier);
                            this.drainUndead = necroReduction;
                            amount -= necroReduction;
                            //Log.Message("" + pawn.LabelShort + " is 1 of " + necroCount + " contributing necros and had necro reduction of " + necroReduction);

                        }
                        else
                        {
                            this.drainUndead = 0;
                        }

                        if (this.CurLevel < .01f && amount < 0)
                        {
                            float pain = pawn.health.hediffSet.PainTotal;
                            float con = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
                            float sev = (.015f * (1 + (3 * pain) + (1 - con)));
                            HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_ManaSickness, sev);
                        }

                        if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ArcaneSickness))
                        {
                            this.drainManaSickness = amount;
                            amount = 0;
                            //no mana gain
                        }
                        else
                        {
                            this.drainManaSickness = 0;
                        }

                        this.lastGainPct = amount;

                        if (comp.Mana.CurLevel < MaxLevel)
                        {
                            comp.Mana.CurLevel = Mathf.Max(comp.Mana.CurLevel + amount, 0f);
                        }
                        //comp.Mana.curLevelInt = Mathf.Clamp(comp.Mana.curLevelInt += amount, 0f, this.MaxLevel);

                        lastNeed = this.CurLevel;
                        this.lastGainTick = Find.TickManager.TicksGame;
                    }

                    if (CurLevel < 0)
                    {
                        CurLevel = 0;
                    }
                    else if (CurLevel > (MaxLevel + .01f))
                    {
                        CurLevel -= .005f;
                    }
                    else if (CurLevel > (MaxLevel))
                    {
                        CurLevel = MaxLevel;
                    }
                }
                else
                {
                    this.curLevelInt = 0;
                }
                AdjustThresh();
            }
            //else
            //{                
            //    Pawn pawn = base.pawn;
            //    CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            //    if (comp != null)
            //    {
            //        if (comp.IsMagicUser && comp.Mana != null)
            //        {
            //            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            //            MagicPowerSkill manaRegen = comp.MagicData.MagicPowerSkill_global_regen.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_global_regen_pwr");
            //            amount *= ((0.0012f + 0.00006f * manaRegen.level) * comp.mpRegenRate * settingsRef.needMultiplier);
            //            amount = Mathf.Min(amount, this.MaxLevel - this.CurLevel);
            //            comp.Mana.CurLevel = Mathf.Max(comp.Mana.CurLevel += amount, 0f);
            //        }
            //    }
            //}
            
        }        

        public void UseMagicPower(float amount)
        {
            this.curLevelInt = Mathf.Clamp(this.curLevelInt - amount, 0f, 2f*this.pawn.GetCompAbilityUserMagic().maxMP);
            if ((amount) > .25f && (amount) < .45f)
            {
                //0.0 to 0.2 max
                float sev = ((amount - .25f) * 10);
                if(pawn.story != null && pawn.story.traits != null)
                {
                    if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_EnlightenedTD))
                    {
                        sev *= .5f;
                    }
                    if(pawn.story.traits.HasTrait(TorannMagicDefOf.TM_CursedTD))
                    {
                        sev = (sev * -1f);
                    }
                }
                HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_ArcaneWeakness, sev);
            }
            else if ((amount) >= .45f && (amount) < .79f)
            {
                //0.0 to 0.34 max
                float sev = 2f + ((amount - .45f) * 30);
                if (pawn.story != null && pawn.story.traits != null)
                {
                    if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_EnlightenedTD))
                    {
                        sev *= .5f;
                    }
                    if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_CursedTD))
                    {
                        sev = (sev * -1f);
                    }
                }
                HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_ArcaneWeakness, sev);
            }
            else if ((amount) >= .79f && (amount) < 5)
            {
                //0.0 to x.x 
                float sev = 12.5f + ((amount - .79f) * 75);              
                if (lastCast != Find.TickManager.TicksGame)
                {
                    this.lastCast = Find.TickManager.TicksGame;
                    if (pawn.story != null && pawn.story.traits != null)
                    {
                        if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_EnlightenedTD))
                        {
                            sev *= .5f;
                        }
                        if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_CursedTD))
                        {
                            sev = (sev * -1f);
                        }
                    }
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_ArcaneWeakness, sev);                    
                }
            }
        }

        public override void NeedInterval()
        {
            this.GainNeed(1f);
        }

        public override string GetTipString()
        {
            //return base.GetTipString();
            return string.Concat(new string[]
            {
                this.LabelCap,
                ": ",
                (this.CurLevel / .01f).ToString("n2"),
                "\n",
                this.def.description
            });
        }

        public override void DrawOnGUI(Rect rect, int maxThresholdMarkers = 2147483647, float customMargin = -1f, bool drawArrows = true, bool doTooltip = true, Rect? rectForTooltip = default(Rect?))
        {
            bool flag = rect.height > 70f;
            if (flag)
            {
                float num = (rect.height - 70f) / 2f;
                rect.height = 70f;
                rect.y += num;
            }
            Rect rect2 = rectForTooltip ?? rect;
            if (Mouse.IsOver(rect2))
            {
                Widgets.DrawHighlight(rect2);
            }
            if (doTooltip && Mouse.IsOver(rect2))
            {
                TooltipHandler.TipRegion(rect2, new TipSignal(() => GetTipString(), rect2.GetHashCode()));
            }
            float num2 = 14f;
            float num3 = num2 + 15f;
            bool flag3 = rect.height < 50f;
            if (flag3)
            {
                num2 *= Mathf.InverseLerp(0f, 50f, rect.height);
            }
            Text.Font = ((rect.height <= 55f) ? GameFont.Tiny : GameFont.Small);
            Text.Anchor = TextAnchor.LowerLeft;
            Rect _rect2 = new Rect(rect.x + num3 + rect.width * 0.1f, rect.y, rect.width - num3 - rect.width * 0.1f, rect.height / 2f);
            Widgets.Label(_rect2, base.LabelCap);
            GUI.color = Color.magenta;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect rect3 = new Rect(rect.x, rect.y + rect.height / 2f, rect.width, rect.height / 2f);
            rect3 = new Rect(rect3.x + num3, rect3.y, rect3.width - num3 * 2f, rect3.height - num2);
            Widgets.FillableBar(rect3, base.CurLevelPercentage);
            bool flag4 = this.threshPercents != null;
            if (flag4)
            {
                for (int i = 0; i < this.threshPercents.Count; i++)
                {
                    this.DrawBarThreshold(rect3, this.threshPercents[i]);
                }
            }
            float curInstantLevelPercentage = Mathf.Clamp(this.CurLevel / this.MaxLevel, 0f, 1f); // base.CurInstantLevelPercentage;
            bool flag5 = curInstantLevelPercentage >= 0f;
            if (flag5)
            {
                base.DrawBarInstantMarkerAt(rect3, curInstantLevelPercentage);
            }
            bool flag6 = !this.def.tutorHighlightTag.NullOrEmpty();
            if (flag6)
            {
                UIHighlighter.HighlightOpportunity(rect, this.def.tutorHighlightTag);
            }
            Text.Font = GameFont.Small;            
        }

        private void DrawBarThreshold(Rect barRect, float threshPct)
        {
            float num = (float)((barRect.width <= 60f) ? 1 : 2);
            Rect position = new Rect(barRect.x + barRect.width * threshPct - (num - 1f), barRect.y + barRect.height / 2f, num, barRect.height / 2f);
            bool flag = threshPct < base.CurLevelPercentage;
            Texture2D image;
            if (flag)
            {
                image = BaseContent.BlackTex;
                GUI.color = new Color(1f, 1f, 1f, 0.9f);
            }
            else
            {
                image = BaseContent.GreyTex;
                GUI.color = new Color(1f, 1f, 1f, 0.5f);
            }
            GUI.DrawTexture(position, image);
            GUI.color = Color.white;            
        }
    }
}
