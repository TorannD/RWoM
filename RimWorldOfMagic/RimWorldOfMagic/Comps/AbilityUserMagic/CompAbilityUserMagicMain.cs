using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using RimWorld;
using UnityEngine;
using AbilityUser;
using Verse;
using Verse.AI;
using Verse.Sound;
using AbilityUserAI;
using TorannMagic.Ideology;
using TorannMagic.TMDefs;
using TorannMagic.Utils;

namespace TorannMagic
{
    [CompilerGenerated]
    [Serializable]
    [StaticConstructorOnStartup]
    public partial class CompAbilityUserMagic : CompAbilityUserTMBase
    {
        public static List<TMAbilityDef> MagicAbilities = null;
        
        public string LabelKey = "TM_Magic";

        public bool firstTick = false;
        public bool magicPowersInitialized = false;
        public bool magicPowersInitializedForColonist = true;
        private bool colonistPowerCheck = true;
        private int resMitigationDelay = 0;
        private int damageMitigationDelay = 0;
        private int damageMitigationDelayMS = 0;
        public int magicXPRate = 1000;
        public int lastXPGain = 0;
        
        private bool doOnce = true;
        private List<IntVec3> deathRing = new List<IntVec3>();
        public float weaponCritChance = 0f;
        public LocalTargetInfo SecondTarget = null;
        public List<TM_EventRecords> magicUsed = new List<TM_EventRecords>();

        public bool spell_Rain = false;
        public bool spell_Blink = false;
        public bool spell_Teleport = false;
        public bool spell_Heal = false;
        public bool spell_Heater = false;
        public bool spell_Cooler = false;
        public bool spell_DryGround = false;
        public bool spell_WetGround = false;
        public bool spell_ChargeBattery = false;
        public bool spell_SmokeCloud = false;
        public bool spell_Extinguish = false;
        public bool spell_EMP = false;
        public bool spell_Firestorm = false;
        public bool spell_Blizzard = false;
        public bool spell_SummonMinion = false;
        public bool spell_TransferMana = false;
        public bool spell_SiphonMana = false;
        public bool spell_RegrowLimb = false;
        public bool spell_EyeOfTheStorm = false;
        public bool spell_ManaShield = false;
        public bool spell_FoldReality = false;
        public bool spell_Resurrection = false;
        public bool spell_PowerNode = false;
        public bool spell_Sunlight = false;
        public bool spell_HolyWrath = false;
        public bool spell_LichForm = false;
        public bool spell_Flight = false;
        public bool spell_SummonPoppi = false;
        public bool spell_BattleHymn = false;
        public bool spell_CauterizeWound = false;
        public bool spell_FertileLands = false;
        public bool spell_SpellMending = false;
        public bool spell_ShadowStep = false;
        public bool spell_ShadowCall = false;
        public bool spell_Scorn = false;
        public bool spell_PsychicShock = false;
        public bool spell_SummonDemon = false;
        public bool spell_Meteor = false;
        public bool spell_Teach = false;
        public bool spell_OrbitalStrike = false;
        public bool spell_BloodMoon = false;
        public bool spell_EnchantedAura = false;
        public bool spell_Shapeshift = false;
        public bool spell_ShapeshiftDW = false;
        public bool spell_Blur = false;
        public bool spell_BlankMind = false;
        public bool spell_DirtDevil = false;
        public bool spell_MechaniteReprogramming = false;
        public bool spell_ArcaneBolt = false;
        public bool spell_LightningTrap = false;
        public bool spell_Invisibility = false;
        public bool spell_BriarPatch = false;
        public bool spell_Recall = false;
        public bool spell_MageLight = false;
        public bool spell_SnapFreeze = false;
        public bool spell_Ignite = false;
        public bool spell_CreateLight = false;
        public bool spell_EqualizeLight = false;
        public bool spell_HeatShield = false;

        private bool item_StaffOfDefender = false;

        public float maxMP = 1;
        public float mpRegenRate = 1;
        public float mpCost = 1;
        public float arcaneDmg = 1;

        public List<TM_ChaosPowers> chaosPowers = new List<TM_ChaosPowers>();
        public TMAbilityDef mimicAbility = null;
        public List<Thing> summonedMinions = new List<Thing>();
        public List<Thing> supportedUndead = new List<Thing>();
        public List<Thing> summonedSentinels = new List<Thing>();
        public List<Pawn> stoneskinPawns = new List<Pawn>();
        public IntVec3 earthSprites = default(IntVec3);
        public bool earthSpritesInArea = false;
        public Map earthSpriteMap = null;
        public int nextEarthSpriteAction = 0;
        public int nextEarthSpriteMote = 0;
        public int earthSpriteType = 0;
        private bool dismissEarthSpriteSpell = false;
        public List<Thing> summonedLights = new List<Thing>();
        public List<Thing> summonedHeaters = new List<Thing>();
        public List<Thing> summonedCoolers = new List<Thing>();
        public List<Thing> summonedPowerNodes = new List<Thing>();
        public ThingDef guardianSpiritType = null;
        public Pawn soulBondPawn = null;
        private bool dismissMinionSpell = false;
        private bool dismissUndeadSpell = false;
        private bool dismissSunlightSpell = false;
        private bool dispelStoneskin = false;
        private bool dismissCoolerSpell = false;
        private bool dismissHeaterSpell = false;
        private bool dismissPowerNodeSpell = false;
        private bool dispelEnchantWeapon = false;
        private bool dismissEnchanterStones = false;
        private bool dismissLightningTrap = false;
        private bool shatterSentinel = false;
        private bool dismissGuardianSpirit = false;
        private bool dispelLivingWall = false;
        private bool dispelBrandings = false;
        public List<IntVec3> fertileLands = new List<IntVec3>();
        public Thing mageLightThing = null;
        public bool mageLightActive = false;
        public bool mageLightSet = false;
        public bool useTechnoBitToggle = true;
        public bool useTechnoBitRepairToggle = true;
        public Vector3 bitPosition = Vector3.zero;
        private bool bitFloatingDown = true;
        private float bitOffset = .45f;
        public int technoWeaponDefNum = -1;
        public Thing technoWeaponThing = null;
        public ThingDef technoWeaponThingDef = null;
        public QualityCategory technoWeaponQC = QualityCategory.Normal;
        public bool useElementalShotToggle = true;
        public Building overdriveBuilding = null;
        public int overdriveDuration = 0;
        public float overdrivePowerOutput = 0;
        public int overdriveFrequency = 100;
        public Building sabotageBuilding = null;
        public bool ArcaneForging = false;
        public List<Pawn> weaponEnchants = new List<Pawn>();
        public Thing enchanterStone = null;
        public List<Thing> enchanterStones = new List<Thing>();
        public List<Thing> lightningTraps = new List<Thing>();        
        public IncidentDef predictionIncidentDef = null;
        public int predictionTick = 0;
        public int predictionHash = 0;
        private List<Pawn> hexedPawns = new List<Pawn>();
        //Recall fields
        //position, hediffs, needs, mana, manual recall bool, recall duration
        public IntVec3 recallPosition = default(IntVec3);
        public Map recallMap = null;
        public List<string> recallNeedDefnames = null;
        public List<float> recallNeedValues = null;
        public List<Hediff> recallHediffList = null;
        public List<float> recallHediffDefSeverityList = null;
        public List<int> recallHediffDefTicksRemainingList = null;
        public List<Hediff_Injury> recallInjuriesList = null;
        public bool recallSet = false;
        public int recallExpiration = 0;
        public bool recallSpell = false;
        public FlyingObject_SpiritOfLight SoL = null;
        public Pawn bondedSpirit = null;
        //public List<TMDefs.TM_Branding> brandings = new List<TMDefs.TM_Branding>();
        public List<Pawn> brandedPawns = new List<Pawn>();
        public List<Pawn> brands = new List<Pawn>();
        public List<HediffDef> brandDefs = new List<HediffDef>();
        public bool sigilSurging = false;
        public bool sigilDraining = false;
        public FlyingObject_LivingWall livingWall = null;
        public int lastChaosTraditionTick = 0;
        public ThingOwner<ThingWithComps> magicWardrobe;
        public SkillRecord incitePassionSkill = null;

        // Cached values calculated in TM_PawnTracker
        private bool initializedIsMagicUser;
        private bool isMagicUser;  // Cached version

        private static HashSet<ushort> magicTraitIndexes = new HashSet<ushort>()
        {
            TorannMagicDefOf.Enchanter.index,
            TorannMagicDefOf.BloodMage.index,
            TorannMagicDefOf.Technomancer.index,
            TorannMagicDefOf.Geomancer.index,
            TorannMagicDefOf.Warlock.index,
            TorannMagicDefOf.Succubus.index,
            TorannMagicDefOf.Faceless.index,
            TorannMagicDefOf.InnerFire.index,
            TorannMagicDefOf.HeartOfFrost.index,
            TorannMagicDefOf.StormBorn.index,
            TorannMagicDefOf.Arcanist.index,
            TorannMagicDefOf.Paladin.index,
            TorannMagicDefOf.Summoner.index,
            TorannMagicDefOf.Druid.index,
            TorannMagicDefOf.Necromancer.index,
            TorannMagicDefOf.Lich.index,
            TorannMagicDefOf.Priest.index,
            TorannMagicDefOf.TM_Bard.index,
            TorannMagicDefOf.Chronomancer.index,
            TorannMagicDefOf.ChaosMage.index,
            TorannMagicDefOf.TM_Wanderer.index
        };

        public class ChainedMagicAbility
        {
            public ChainedMagicAbility(TMAbilityDef _ability, int _expirationTicks, bool _expires)
            {
                abilityDef = _ability;
                expirationTicks = _expirationTicks;
                expires = _expires;
            }
            public TMAbilityDef abilityDef = null;
            public int expirationTicks = 0;
            public bool expires = true;
        }
        public List<ChainedMagicAbility> chainedAbilitiesList = new List<ChainedMagicAbility>();

        private Effecter powerEffecter = null;
        private int powerModifier = 0;
        private int maxPower = 10;
        private int previousHexedPawns = 0;
        public int nextEntertainTick = -1;
        public int nextSuccubusLovinTick = -1;

        public List<Pawn> BrandPawns
        {
            get
            {
                if (brands == null)
                {
                    brands = new List<Pawn>();
                    brands.Clear();
                }
                return brands;
            }
        }

        public List<HediffDef> BrandDefs
        {
            get
            {
                if (brandDefs == null)
                {
                    brandDefs = new List<HediffDef>();
                    brandDefs.Clear();
                }
                return brandDefs;
            }
        }

        public ThingOwner<ThingWithComps> MagicWardrobe
        {
            get
            {
                if(magicWardrobe == null)
                {
                    magicWardrobe = new ThingOwner<ThingWithComps>();
                }
                return magicWardrobe;
            }
        }

        public List<TM_EventRecords> MagicUsed
        {
            get
            {
                if (magicUsed == null)
                {
                    magicUsed = new List<TM_EventRecords>();
                }
                return magicUsed;
            }
            set
            {
                if (magicUsed == null)
                {
                    magicUsed = new List<TM_EventRecords>();
                }
                magicUsed = value;                
            }
        }

        public List<Pawn> StoneskinPawns
        {
            get
            {
                if(stoneskinPawns == null)
                {
                    stoneskinPawns = new List<Pawn>();
                }
                List<Pawn> tmpList = new List<Pawn>();
                foreach(Pawn p in stoneskinPawns)
                {
                    if(p.DestroyedOrNull() || p.Dead)
                    {
                        tmpList.Add(p);
                    }
                }
                for(int i = 0; i < tmpList.Count; i++)
                {
                    stoneskinPawns.Remove(tmpList[i]);
                }
                return stoneskinPawns;
            }
        }

        public ThingDef GuardianSpiritType
        {
            get
            {
                if(this.guardianSpiritType == null)
                {
                    float rnd = Rand.Value;
                    
                    if(rnd < .34f)
                    {
                        this.guardianSpiritType = TorannMagicDefOf.TM_SpiritBearR;
                    }
                    else if (rnd < .67f)
                    {
                        this.guardianSpiritType = TorannMagicDefOf.TM_SpiritMongooseR;
                    }
                    else
                    {
                        this.guardianSpiritType = TorannMagicDefOf.TM_SpiritCrowR;
                    }
                }
                return this.guardianSpiritType;
            }
        }

        public bool HasTechnoBit
        {
            get
            {
                return this.IsMagicUser && this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TechnoBit).learned;
            }
        }
        public bool HasTechnoTurret
        {
            get
            {
                return this.IsMagicUser && this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TechnoTurret).learned;
            }
        }

        public bool HasTechnoWeapon
        {
            get
            {
                return this.IsMagicUser && this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TechnoWeapon).learned;
            }
        }

        public int PowerModifier
        {
            get => powerModifier;
            set
            {
                TM_MoteMaker.ThrowSiphonMote(Pawn.DrawPos, Pawn.Map, 1f);
                powerModifier = Mathf.Clamp(value, 0, maxPower);

                if (powerModifier != 0 || powerEffecter == null) return;
                powerEffecter.Cleanup();
                powerEffecter = null;
            }
        }

        public float GetSkillDamage()
        {
            float result;
            float strFactor = 1f;
            if (IsMagicUser)
            {
                strFactor = arcaneDmg;
            }

            if (Pawn.equipment?.Primary != null)
            {
                if (Pawn.equipment.Primary.def.IsMeleeWeapon)
                {
                    result = TM_Calc.GetSkillDamage_Melee(Pawn, strFactor);
                    weaponCritChance = TM_Calc.GetWeaponCritChance(Pawn.equipment.Primary);
                }
                else
                {
                    result = TM_Calc.GetSkillDamage_Range(Pawn, strFactor);
                    weaponCritChance = 0f;
                }
            }
            else
            {
                result = Pawn.GetStatValue(StatDefOf.MeleeDPS, false) * strFactor;
            }

            return result;
        }

        private MagicData magicData = null;
        public MagicData MagicData
        {
            get
            {
                bool flag = this.magicData == null && this.IsMagicUser;
                if (flag)
                {
                    this.magicData = new MagicData(this);
                }
                return this.magicData;
            }
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            bool flag = this.powerEffecter != null;
            if (flag)
            {
                this.powerEffecter.Cleanup();
            }
        }

        public List<Pawn> HexedPawns
        {
            get
            {
                if(hexedPawns == null)
                {
                    this.hexedPawns = new List<Pawn>();
                    this.hexedPawns.Clear();
                }
                List<Pawn> validPawns = new List<Pawn>();
                validPawns.Clear();
                foreach (Pawn p in this.hexedPawns)
                {
                    if (p != null && !p.Destroyed && !p.Dead)
                    {
                        if (p.health != null && p.health.hediffSet != null && p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HexHD))
                        {
                            validPawns.Add(p);
                        }
                    }
                }
                this.hexedPawns = validPawns;
                return this.hexedPawns;
            }
        }

        public bool shouldDraw = true;
        public override void PostDraw()
        {
            if (this.Pawn.DestroyedOrNull()) return;
            if (this.Pawn.Dead) return;
            if (this.Pawn.Map == null) return;
            if (shouldDraw && IsMagicUser)
            {
                
                if (ModOptions.Settings.Instance.AIFriendlyMarking && base.Pawn.IsColonist && this.IsMagicUser)
                {
                    if (!this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                    {
                        DrawMark();
                    }
                }
                if (ModOptions.Settings.Instance.AIMarking && !base.Pawn.IsColonist && this.IsMagicUser)
                {
                    if (!this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                    {
                        DrawMark();
                    }
                }

                if (this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower mp) => mp.abilityDef == TorannMagicDefOf.TM_TechnoBit).learned == true && this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TechnoBitHD))
                {
                    DrawTechnoBit();
                }

                if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DemonScornHD) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DemonScornHD_I) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DemonScornHD_II) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DemonScornHD_III))
                {
                    DrawScornWings();
                }

                if (this.mageLightActive)
                {
                    DrawMageLight();
                }

                Enchantment.CompEnchant compEnchant = this.Pawn.GetComp<Enchantment.CompEnchant>();

                if (this.IsMagicUser && compEnchant != null && compEnchant.enchantingContainer != null && compEnchant.enchantingContainer.Count > 0)
                {
                    DrawEnchantMark();
                }
            }
            base.PostDraw();
        }
        
        private void SingleEvent()
        {
            this.doOnce = false;
        }

        private void DoOncePerLoad()
        {
            if (this.spell_FertileLands == true)
            {
                if (this.fertileLands.Count > 0)
                {
                    List<IntVec3> cellList = ModOptions.Constants.GetGrowthCells();
                    if (cellList.Count != 0)
                    {
                        for (int i = 0; i < fertileLands.Count; i++)
                        {
                            ModOptions.Constants.RemoveGrowthCell(fertileLands[i]);
                        }
                    }
                    ModOptions.Constants.SetGrowthCells(fertileLands);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_FertileLands);
                    this.AddPawnAbility(TorannMagicDefOf.TM_DismissFertileLands);
                }
            }
            //to fix filtering of succubus abilities
            if(this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus))
            {
                for(int i = 0; i < this.MagicData.MagicPowersWD.Count; i++)
                {
                    MagicPower wd = this.MagicData.MagicPowersWD[i];
                    if (wd.learned && wd.abilityDef == TorannMagicDefOf.TM_SoulBond)
                    {
                        this.MagicData.MagicPowersSD.FirstOrDefault((MagicPower p) => p.abilityDef == TorannMagicDefOf.TM_SoulBond).learned = true;
                    }
                    else if(wd.learned && wd.abilityDef == TorannMagicDefOf.TM_ShadowBolt)
                    {
                        this.MagicData.MagicPowersSD.FirstOrDefault((MagicPower p) => p.abilityDef == TorannMagicDefOf.TM_ShadowBolt).learned = true;
                    }
                    else if (wd.learned && wd.abilityDef == TorannMagicDefOf.TM_Dominate)
                    {
                        this.MagicData.MagicPowersSD.FirstOrDefault((MagicPower p) => p.abilityDef == TorannMagicDefOf.TM_Dominate).learned = true;
                    }
                }
            }
        }

        public override void CompTick()
        {

            Pawn pawn = this.Pawn;
            if (pawn?.story == null) return;
            if (this.Pawn.IsShambler || this.Pawn.IsGhoul)
            {
                if (this.magicData != null)
                {
                    RemoveAbilityUser();
                }
                return;
            }

            // If we aren't on map, handle ability cooldown per long tick
            if (!pawn.Spawned)
            {
                if (pawn.Map != null || Find.TickManager.TicksGame % 600 != 0) return;  // Not time to caravan tick.
                if (!this.IsMagicUser) return;  // We won't tick at all if we aren't a magic user

                var allPowers = AbilityData.AllPowers;
                for (int i = allPowers.Count - 1; i >= 0; i--)
                {
                    allPowers[i].CooldownTicksLeft = Math.Max(allPowers[i].CooldownTicksLeft - 600, 0);
                }
                return;
            }

            // If we aren't magic, check if we can be inspired
            if (!this.IsMagicUser)
            {
                if (!ModsConfig.IdeologyActive) return;
                if (Find.TickManager.TicksGame % 2501 != 0) return;
                if (!pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted)) return;

                if (!pawn.Inspired && pawn.CurJobDef == JobDefOf.LayDown && Rand.Chance(.025f))
                {
                    pawn.mindState.inspirationHandler.TryStartInspiration(TorannMagicDefOf.ID_ArcanePathways);
                }
                return;
            }

            if (!this.TickConditionsMet) return;  // Cached in TM_PawnTracker

            // Finally, let's do a magic tick!
            if (!this.firstTick) this.PostInitializeTick();
            base.CompTick();
            this.age++;
            if (this.chainedAbilitiesList != null && this.chainedAbilitiesList.Count > 0)
            {
                for (int i = 0; i < chainedAbilitiesList.Count; i++)
                {
                    chainedAbilitiesList[i].expirationTicks--;
                    if (chainedAbilitiesList[i].expires && chainedAbilitiesList[i].expirationTicks <= 0)
                    {
                        this.RemovePawnAbility(chainedAbilitiesList[i].abilityDef);
                        this.chainedAbilitiesList.Remove(chainedAbilitiesList[i]);
                        break;
                    }
                }
            }
            if (this.Mana != null)
            {
                if (Find.TickManager.TicksGame % 4 == 0 && this.Pawn.CurJob != null && this.Pawn.CurJobDef == JobDefOf.DoBill && this.Pawn.CurJob.targetA != null && this.Pawn.CurJob.targetA.Thing != null)
                {
                    DoArcaneForging();
                }
                if (this.Mana.CurLevel >= (.99f * this.Mana.MaxLevel))
                {
                    if (this.age > (lastXPGain + magicXPRate))
                    {
                        MagicData.MagicUserXP++;
                        lastXPGain = this.age;
                    }
                }
                if (Find.TickManager.TicksGame % 30 == 0)
                {
                    bool flag5 = this.MagicUserXP > this.MagicUserXPTillNextLevel;
                    if (flag5)
                    {
                        this.LevelUp(false);
                    }
                }
                if (Find.TickManager.TicksGame % 60 == 0)
                {
                    if (this.Pawn.IsColonist && !this.magicPowersInitializedForColonist)
                    {
                        ResolveFactionChange();
                    }
                    else if (!this.Pawn.IsColonist)
                    {
                        this.magicPowersInitializedForColonist = false;
                    }
                    if (this.Pawn.IsColonist)
                    {
                        ResolveEnchantments();
                        for (int i = 0; i < this.summonedMinions.Count; i++)
                        {
                            Pawn evaluateMinion = this.summonedMinions[i] as Pawn;
                            if (evaluateMinion == null || evaluateMinion.Dead || evaluateMinion.Destroyed)
                            {
                                this.summonedMinions.Remove(this.summonedMinions[i]);
                            }
                        }
                        ResolveMinions();
                        ResolveSustainers();
                        if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Lich) || (this.customClass != null && this.customClass.isNecromancer))
                        {
                            ResolveUndead();
                        }
                        ResolveEffecter();
                        ResolveClassSkills();
                        ResolveSpiritOfLight();
                        ResolveChronomancerTimeMark();
                    }
                }

                if (this.autocastTick < Find.TickManager.TicksGame)  //180 default
                {
                    if (!this.Pawn.Dead && !this.Pawn.Downed && this.Pawn.Map != null && this.Pawn.story != null && this.Pawn.story.traits != null && this.MagicData != null && this.AbilityData != null && !this.Pawn.InMentalState)
                    {
                        if (this.Pawn.IsColonist)
                        {
                            this.autocastTick = Find.TickManager.TicksGame + (int)Rand.Range(.8f * ModOptions.Settings.Instance.autocastEvaluationFrequency, 1.2f * ModOptions.Settings.Instance.autocastEvaluationFrequency);
                            ResolveAutoCast();
                        }
                        else if (ModOptions.Settings.Instance.AICasting && (!this.Pawn.IsPrisoner || this.Pawn.IsFighting()) && (this.Pawn.guest != null && !this.Pawn.IsSlave))
                        {
                            float tickMult = ModOptions.Settings.Instance.AIAggressiveCasting ? 1f : 2f;
                            this.autocastTick = Find.TickManager.TicksGame + (int)(Rand.Range(.75f * ModOptions.Settings.Instance.autocastEvaluationFrequency, 1.25f * ModOptions.Settings.Instance.autocastEvaluationFrequency) * tickMult);
                            ResolveAIAutoCast();
                        }
                    }
                }
                if (!this.Pawn.IsColonist && ModOptions.Settings.Instance.AICasting && ModOptions.Settings.Instance.AIAggressiveCasting && Find.TickManager.TicksGame > this.nextAICastAttemptTick) //Aggressive AI Casting
                {
                    this.nextAICastAttemptTick = Find.TickManager.TicksGame + Rand.Range(300, 500);
                    if (this.Pawn.jobs != null && this.Pawn.CurJobDef != TorannMagicDefOf.TMCastAbilitySelf && this.Pawn.CurJobDef != TorannMagicDefOf.TMCastAbilityVerb)
                    {
                        IEnumerable<AbilityUserAIProfileDef> enumerable = this.Pawn.EligibleAIProfiles();
                        if (enumerable != null && enumerable.Count() > 0)
                        {
                            foreach (AbilityUserAIProfileDef item in enumerable)
                            {
                                if (item != null)
                                {
                                    AbilityAIDef useThisAbility = null;
                                    if (item.decisionTree != null)
                                    {
                                        useThisAbility = item.decisionTree.RecursivelyGetAbility(this.Pawn);
                                    }
                                    if (useThisAbility != null)
                                    {
                                        ThingComp val = this.Pawn.AllComps.First((ThingComp comp) => ((object)comp).GetType() == item.compAbilityUserClass);
                                        CompAbilityUser compAbilityUser = val as CompAbilityUser;
                                        if (compAbilityUser != null)
                                        {
                                            PawnAbility pawnAbility = compAbilityUser.AbilityData.AllPowers.First((PawnAbility ability) => ability.Def == useThisAbility.ability);
                                            string reason = "";
                                            if (pawnAbility.CanCastPowerCheck(AbilityContext.AI, out reason))
                                            {
                                                LocalTargetInfo target = useThisAbility.Worker.TargetAbilityFor(useThisAbility, this.Pawn);
                                                if (target.IsValid)
                                                {
                                                    pawnAbility.UseAbility(AbilityContext.Player, target);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (Find.TickManager.TicksGame % this.overdriveFrequency == 0)
            {
                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || (TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_Overdrive)))
                {
                    ResolveTechnomancerOverdrive();
                }
            }
            if (Find.TickManager.TicksGame % 299 == 0) //cache weapon damage for tooltip and damage calculations
            {
                this.weaponDamage = GetSkillDamage(); // TM_Calc.GetSkillDamage(this.Pawn);
            }
            if (Find.TickManager.TicksGame % 601 == 0)
            {
                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock))
                {
                    ResolveWarlockEmpathy();
                }
            }
            if (Find.TickManager.TicksGame % 602 == 0)
            {
                ResolveMagicUseEvents();
            }
            if (Find.TickManager.TicksGame % 2001 == 0)
            {
                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus))
                {
                    ResolveSuccubusLovin();
                }
            }
            if (deathRetaliating)
            {
                DoDeathRetaliation();
            }
            else if (Find.TickManager.TicksGame % 67 == 0 && !this.Pawn.IsColonist && this.Pawn.Downed)
            {
                DoDeathRetaliation();
            }
        }

        public void PostInitializeTick()
        {
            if (this.doOnce) SingleEvent();
            Trait t = this.Pawn.story.traits.GetTrait(TorannMagicDefOf.TM_Possessed);
            if (t != null && !this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SpiritPossessionHD))
            {
                this.Pawn.story.traits.RemoveTrait(t);
            }
            else
            {
                this.firstTick = true;
                this.Initialize();
                this.ResolveMagicTab();
                this.ResolveMagicPowers();
                this.ResolveMana();
                this.DoOncePerLoad();
            }
        }

        public bool IsMagicUser => this.initializedIsMagicUser ? this.isMagicUser : this.SetIsMagicUser();
        public bool SetIsMagicUser()
        {
            if (Pawn?.story == null) return this.isMagicUser = false;
            this.initializedIsMagicUser = true;
            if (this.customClass != null) return this.isMagicUser = true;
            if (this.customClass == null && this.customIndex == -2)
            {
                this.customIndex = TM_ClassUtility.CustomClassIndexOfBaseMageClass(this.Pawn.story.traits.allTraits);
                if (this.customIndex >= 0)
                {
                    TM_CustomClass foundCustomClass = TM_ClassUtility.CustomClasses[customIndex];
                    if (!foundCustomClass.isMage)
                    {
                        this.customIndex = -1;
                        return this.isMagicUser = false;
                    }
                    this.customClass = foundCustomClass;
                    return this.isMagicUser = true;
                }
            }
            // If any traits are in our generated set of magic traits, we are magic.
            for (int i = Pawn.story.traits.allTraits.Count - 1; i >= 0; i--)
            {
                if (magicTraitIndexes.Contains(Pawn.story.traits.allTraits[i].def.index))
                    return this.isMagicUser = true;
            }
            if (AdvancedClasses.Count > 0 || TM_Calc.IsWanderer(Pawn)) return this.isMagicUser = true;
            if (TM_Calc.HasAdvancedClass(this.Pawn))
            {
                foreach (TMDefs.TM_CustomClass cc in TM_ClassUtility.GetAdvancedClassesForPawn(this.Pawn))
                {
                    if (cc.isMage)
                    {
                        this.AdvancedClasses.Add(cc);
                        return this.isMagicUser = true;
                    }
                }
            }
            return this.isMagicUser = false;
        }

        private Dictionary<int, int> cacheXPFL = new Dictionary<int, int>();
        public int GetXPForLevel(int lvl)
        {
            if (!cacheXPFL.ContainsKey(lvl))
            {
                IntVec2 c1 = new IntVec2(0, 40); 
                IntVec2 c2 = new IntVec2(5, 30);
                IntVec2 c3 = new IntVec2(15, 20); 
                IntVec2 c4 = new IntVec2(30, 10);
                IntVec2 c5 = new IntVec2(200, 0);

                int val = 0;

                for (int i = 0; i < lvl + 1; i++)
                {
                    val += (Mathf.Clamp(i, c1.x, c2.x - 1) * c1.z) + c1.z;
                    if (i >= c2.x)
                    {
                        val += (Mathf.Clamp(i, c2.x, c3.x - 1) * c2.z) + c2.z;
                    }
                    if (i >= c3.x)
                    {
                        val += (Mathf.Clamp(i, c3.x, c4.x - 1) * c3.z) + c3.z;
                    }
                    if (i >= c4.x)
                    {
                        val += (Mathf.Clamp(i, c4.x, c5.x - 1) * c4.z) + c4.z;
                    }
                }
                cacheXPFL.Add(lvl, val);
            }
            if (cacheXPFL.ContainsKey(lvl))
            {
                return cacheXPFL[lvl];
            }
            else
            {
                return 0;
            }
        }

        public int MagicUserLevel
        {
            get
            {                
                return this.MagicData.MagicUserLevel;
            }
            set
            {
                bool flag = value > this.MagicData.MagicUserLevel;
                if (flag)
                {
                    this.MagicData.MagicAbilityPoints++;
                    bool flag2 = this.MagicData.MagicUserXP < GetXPForLevel(value - 1);
                    if (flag2)
                    {
                        this.MagicData.MagicUserXP = GetXPForLevel(value - 1);
                    }
                }
                this.MagicData.MagicUserLevel = value;
            }
        }

        public int MagicUserXP
        {
            get
            {
                return this.MagicData.MagicUserXP;
            }
            set
            {
                this.MagicData.MagicUserXP = value;
            }
        }
        
        public float XPLastLevel
        {
            get
            {
                float result = 0f;
                bool flag = this.MagicUserLevel > 0;
                if (flag)
                {
                    
                    result = GetXPForLevel(this.MagicUserLevel - 1);
                }
                return result;
            }
        }

        public float XPTillNextLevelPercent
        {
            get
            {
                return ((float)this.MagicData.MagicUserXP - this.XPLastLevel) / ((float)this.MagicUserXPTillNextLevel - this.XPLastLevel);
            }
        }

        public int MagicUserXPTillNextLevel
        {
            get
            {
                if(MagicUserXP < XPLastLevel)
                {
                    MagicUserXP = (int)XPLastLevel;
                }
                return GetXPForLevel(this.MagicUserLevel);
            }
        }

        public void LevelUp(bool hideNotification = false)
        {
            if (!(this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer)))
            {
                if (this.MagicUserLevel < (this.customClass?.maxMageLevel ?? 200))
                {
                    this.MagicUserLevel++;
                    bool flag = !hideNotification;
                    if (flag)
                    {                        
                        if (Pawn.IsColonist && ModOptions.Settings.Instance.showLevelUpMessage)
                        {
                            Messages.Message(TranslatorFormattedStringExtensions.Translate("TM_MagicLevelUp",
                        this.parent.Label
                            ), this.Pawn, MessageTypeDefOf.PositiveEvent);
                        }
                    }
                }
                else
                {
                    this.MagicUserXP = (int)this.XPLastLevel;
                }
            }
        }

        public void LevelUpPower(MagicPower power)
        {
            foreach (AbilityUser.AbilityDef current in power.TMabilityDefs)
            {
                base.RemovePawnAbility(current);
            }
            power.level++;
            base.AddPawnAbility(power.abilityDef, true, -1f);
            this.UpdateAbilities();
        }

        public Need_Mana Mana
        {
            get
            {
                if (!this.Pawn.DestroyedOrNull() && !this.Pawn.Dead)
                {
                    return base.Pawn.needs.TryGetNeed<Need_Mana>();
                }
                return null;
            }
        }

        public void ResolveFactionChange()
        {
            if (!this.colonistPowerCheck)
            {
                RemovePowers();
                this.spell_BattleHymn = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_BattleHymn);
                this.spell_Blizzard = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_Blizzard);
                this.spell_BloodMoon = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_BloodMoon);
                this.spell_EyeOfTheStorm = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_EyeOfTheStorm);
                this.spell_Firestorm = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_Firestorm);
                this.spell_FoldReality = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_FoldReality);
                this.spell_HolyWrath = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_HolyWrath);
                this.spell_LichForm = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_BattleHymn);
                this.spell_Meteor = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_Meteor);
                this.spell_OrbitalStrike = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_OrbitalStrike);
                this.spell_PsychicShock = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_PsychicShock);
                this.spell_RegrowLimb = false;
                this.spell_Resurrection = false;
                this.spell_Scorn = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_Scorn);
                this.spell_Shapeshift = false;
                this.spell_SummonPoppi = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_SummonPoppi);
                this.RemovePawnAbility(TorannMagicDefOf.TM_Recall);
                this.spell_Recall = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_LightSkipGlobal);
                this.RemovePawnAbility(TorannMagicDefOf.TM_LightSkipMass);
                this.RemovePawnAbility(TorannMagicDefOf.TM_SpiritOfLight);
                AssignAbilities();
            }
            this.magicPowersInitializedForColonist = true;
            this.colonistPowerCheck = true;
        }

        public override void PostInitialize()
        {
            base.PostInitialize();
            bool flag = CompAbilityUserMagic.MagicAbilities == null;
            if (flag)
            {
                if (this.magicPowersInitialized == false && this.MagicData != null)
                {
                    MagicData.MagicUserLevel = 0;
                    MagicData.MagicAbilityPoints = 0;
                    AssignAbilities();
                    if (!this.Pawn.IsColonist)
                    {
                        InitializeSpell();
                        this.colonistPowerCheck = false;
                    }
                }
                this.magicPowersInitialized = true;
                base.UpdateAbilities();
            }
        }

        public void AssignAdvancedClassAbilities(bool firstAssignment = false)
        {
            if (this.AdvancedClasses != null && this.AdvancedClasses.Count > 0)
            {
                for (int z = 0; z < this.MagicData.AllMagicPowers.Count; z++)
                {
                    TMAbilityDef ability = (TMAbilityDef)this.MagicData.AllMagicPowers[z].abilityDef;
                    foreach (TMDefs.TM_CustomClass cc in this.AdvancedClasses)
                    {
                        if (cc.classMageAbilities.Contains(ability))
                        {
                            this.MagicData.AllMagicPowers[z].learned = true;
                        }
                        if (this.MagicData.AllMagicPowers[z] == this.MagicData.MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SoulBond) ||
                        this.MagicData.AllMagicPowers[z] == this.MagicData.MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ShadowBolt) ||
                        this.MagicData.AllMagicPowers[z] == this.MagicData.MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Dominate))
                        {
                            this.MagicData.AllMagicPowers[z].learned = false;
                        }
                        if (this.MagicData.AllMagicPowers[z].requiresScroll)
                        {
                            this.MagicData.AllMagicPowers[z].learned = false;
                        }
                        if (!this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false) && !Rand.Chance(ability.learnChance))
                        {
                            this.MagicData.AllMagicPowers[z].learned = false;
                        }
                        if (this.MagicData.AllMagicPowers[z].learned)
                        {
                            if (ability.shouldInitialize)
                            {
                                this.AddPawnAbility(ability);
                            }
                            if (ability.childAbilities != null && ability.childAbilities.Count > 0)
                            {
                                for (int c = 0; c < ability.childAbilities.Count; c++)
                                {
                                    if (ability.childAbilities[c].shouldInitialize)
                                    {
                                        this.AddPawnAbility(ability.childAbilities[c]);
                                    }
                                }
                            }
                        }
                        if (cc.classHediff != null)
                        {
                            HealthUtility.AdjustSeverity(this.Pawn, this.customClass.classHediff, this.customClass.hediffSeverity);
                        }
                    }
                }
                MagicPower branding = this.MagicData.AllMagicPowers.FirstOrDefault((MagicPower p) => p.abilityDef == TorannMagicDefOf.TM_Branding);
                if (branding != null && branding.learned && firstAssignment)
                {
                    int count = 0;
                    while (count < 2)
                    {
                        TMAbilityDef tmpAbility = TM_Data.BrandList().RandomElement();
                        for (int i = 0; i < this.MagicData.AllMagicPowers.Count; i++)
                        {
                            TMAbilityDef ad = (TMAbilityDef)this.MagicData.AllMagicPowers[i].abilityDef;
                            if (!this.MagicData.AllMagicPowers[i].learned && ad == tmpAbility)
                            {
                                count++;
                                this.MagicData.AllMagicPowers[i].learned = true;
                                this.RemovePawnAbility(ad);
                                this.TryAddPawnAbility(ad);
                            }
                        }
                    }
                }                
            }
        }

        public override bool TryTransformPawn()
        {
            return this.IsMagicUser;
        }

        public bool TryAddPawnAbility(TMAbilityDef ability)
        {
            //add check to verify no ability is already added
            bool result = false;
            base.AddPawnAbility(ability, true, -1f);
            result = true;
            return result;
        }

        private void ClearPower(MagicPower current)
        {
            Log.Message("Removing ability: " + current.abilityDef.defName.ToString());
            base.RemovePawnAbility(current.abilityDef);
            base.UpdateAbilities();
        }

        public void ResetSkills()
        {
            this.MagicData.MagicPowerSkill_global_regen.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_global_regen_pwr").level = 0;
            this.MagicData.MagicPowerSkill_global_eff.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_global_eff_pwr").level = 0;
            this.MagicData.MagicPowerSkill_global_spirit.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_global_spirit_pwr").level = 0;
            for (int i = 0; i < this.MagicData.AllMagicPowersWithSkills.Count; i++)
            {
                this.MagicData.AllMagicPowersWithSkills[i].level = 0;
                this.MagicData.AllMagicPowersWithSkills[i].learned = false;
                this.MagicData.AllMagicPowersWithSkills[i].autocast = false;
                TMAbilityDef ability = (TMAbilityDef)this.MagicData.AllMagicPowersWithSkills[i].abilityDef;
                MagicPowerSkill mps = this.MagicData.GetSkill_Efficiency(ability);
                if (mps != null)
                {
                    mps.level = 0;
                }
                mps = this.MagicData.GetSkill_Power(ability);
                if (mps != null)
                {
                    mps.level = 0;
                }
                mps = this.MagicData.GetSkill_Versatility(ability);
                if (mps != null)
                {
                    mps.level = 0;
                }
            }
            for(int i = 0; i < this.MagicData.AllMagicPowers.Count; i++)
            {                
                for(int j = 0; j < this.MagicData.AllMagicPowers[i].TMabilityDefs.Count; j++)
                {
                    TMAbilityDef ability = (TMAbilityDef)this.MagicData.AllMagicPowers[i].TMabilityDefs[j];
                    this.RemovePawnAbility(ability);
                }
                this.MagicData.AllMagicPowers[i].learned = false;
                this.MagicData.AllMagicPowers[i].autocast = false;
            }
            this.MagicUserLevel = 0;
            this.MagicUserXP = 0;
            this.MagicData.MagicAbilityPoints = 0;
            this.AssignAbilities();
          
        }

        public void RemoveTraits()
        {
            List<Trait> traits = this.Pawn.story.traits.allTraits;
            for (int i = 0; i < traits.Count; i++)
            {
                if (traits[i].def == TorannMagicDefOf.InnerFire || traits[i].def == TorannMagicDefOf.HeartOfFrost || traits[i].def == TorannMagicDefOf.StormBorn || traits[i].def == TorannMagicDefOf.Arcanist || traits[i].def == TorannMagicDefOf.Paladin ||
                    traits[i].def == TorannMagicDefOf.Druid || traits[i].def == TorannMagicDefOf.Priest || traits[i].def == TorannMagicDefOf.Necromancer || traits[i].def == TorannMagicDefOf.Warlock || traits[i].def == TorannMagicDefOf.Succubus ||
                    traits[i].def == TorannMagicDefOf.TM_Bard || traits[i].def == TorannMagicDefOf.Geomancer || traits[i].def == TorannMagicDefOf.Technomancer || traits[i].def == TorannMagicDefOf.BloodMage || traits[i].def == TorannMagicDefOf.Enchanter ||
                    traits[i].def == TorannMagicDefOf.Chronomancer || traits[i].def == TorannMagicDefOf.ChaosMage || traits[i].def == TorannMagicDefOf.TM_Wanderer)
                {
                    Log.Message("Removing trait " + traits[i].Label);
                    traits.Remove(traits[i]);
                    i--;
                }
                if (this.customClass != null)
                {
                    traits.Remove(this.Pawn.story.traits.GetTrait(this.customClass.classTrait));
                    this.customClass = null;
                    this.customIndex = -2;
                }
            }
        }

        public void RemoveTMagicHediffs()
        {
            List<Hediff> allHediffs = this.Pawn.health.hediffSet.hediffs;
            for (int i = 0; i < allHediffs.Count(); i++)
            {
                if (allHediffs[i].def.defName.StartsWith("TM_"))
                {
                    this.Pawn.health.RemoveHediff(allHediffs[i]);
                }
            }
        }

        public void RemoveAbilityUser()
        {
            this.RemovePowers();
            this.RemoveTMagicHediffs();
            this.RemoveTraits();
            this.magicData = null;
            base.Initialized = false;
            this.isMagicUser = false;
        }     

        public override List<HediffDef> IgnoredHediffs()
        {
            return new List<HediffDef>
            {
                TorannMagicDefOf.TM_MightUserHD
            };
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PostPreApplyDamage(ref dinfo, out absorbed);
        }        

        public void ResolveMagicUseEvents()
        {
            int expiryTick = Find.TickManager.TicksGame - 150000;
            for (int i = MagicUsed.Count - 1; i >= 0; i--)
            {
                if (expiryTick > MagicUsed[i].eventTick) MagicUsed.RemoveAt(i);
            }
        }

        public void ResolveAIAutoCast()
        {
            
            if (ModOptions.Settings.Instance.AICasting && this.Pawn.jobs != null && this.Pawn.CurJob != null && this.Pawn.CurJob.def != TorannMagicDefOf.TMCastAbilityVerb && this.Pawn.CurJob.def != TorannMagicDefOf.TMCastAbilitySelf && 
                this.Pawn.CurJob.def != JobDefOf.Ingest && this.Pawn.CurJob.def != JobDefOf.ManTurret && this.Pawn.GetPosture() == PawnPosture.Standing)
            {
                bool castSuccess = false;
                if (this.Mana != null && this.Mana.CurLevelPercentage >= ModOptions.Settings.Instance.autocastMinThreshold)
                {
                    foreach (MagicPower mp in this.MagicData.AllMagicPowersWithSkills)
                    {
                        if (mp.learned && mp.autocasting != null && mp.autocasting.magicUser && mp.autocasting.AIUsable)
                        {                            
                            //try
                            //{                             
                            TMAbilityDef tmad = mp.TMabilityDefs[mp.level] as TMAbilityDef; // issues with index?
                            bool canUseWithEquippedWeapon = true;
                            bool canUseIfViolentAbility = this.Pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent) ? !tmad.MainVerb.isViolent : true;
                            if (!TM_Calc.HasResourcesForAbility(this.Pawn, tmad))
                            {
                                continue;
                            }
                            if (canUseWithEquippedWeapon && canUseIfViolentAbility)
                            {
                                PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                LocalTargetInfo currentTarget = this.Pawn.TargetCurrentlyAimingAt != null ? this.Pawn.TargetCurrentlyAimingAt : (this.Pawn.CurJob != null ? this.Pawn.CurJob.targetA : null);
                                if (mp.autocasting.type == TMDefs.AutocastType.OnTarget && currentTarget != null)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, currentTarget);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        Thing targetThing = localTarget.Thing;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(this.Pawn.Position, targetThing, this.Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (this.Pawn.Position - targetThing.Position).LengthHorizontal)
                                        {
                                            continue;
                                        }
                                        bool TE = mp.autocasting.targetEnemy && targetThing.Faction != null && targetThing.Faction.HostileTo(this.Pawn.Faction);
                                        if(TE && targetThing is Pawn)
                                        {
                                            Pawn targetPawn = targetThing as Pawn;
                                            if(targetPawn.Downed || targetPawn.IsPrisoner)
                                            {
                                                continue;
                                            }
                                        }
                                        bool TN = mp.autocasting.targetNeutral && targetThing.Faction != null && !targetThing.Faction.HostileTo(this.Pawn.Faction);                                        
                                        if (TN && targetThing is Pawn)
                                        {
                                            Pawn targetPawn = targetThing as Pawn;
                                            if (targetPawn.Downed || targetPawn.IsPrisoner)
                                            {
                                                continue;
                                            }
                                            if (mp.abilityDef.MainVerb.isViolent && !targetPawn.InMentalState)
                                            {
                                                continue;
                                            }
                                        }
                                        bool TNF = mp.autocasting.targetNoFaction && targetThing.Faction == null;
                                        if (TNF && targetThing is Pawn)
                                        {
                                            Pawn targetPawn = targetThing as Pawn;
                                            if (targetPawn.Downed || targetPawn.IsPrisoner)
                                            {
                                                continue;
                                            }                                            
                                        }
                                        bool TF = mp.autocasting.targetFriendly && targetThing.Faction == this.Pawn.Faction;
                                        if (!(TE || TN || TF || TNF))
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(this.Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if (mp.autocasting.type == TMDefs.AutocastType.OnSelf)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        Pawn targetThing = localTarget.Pawn;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(this.Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnSelf.Evaluate(this, tmad, ability, mp, out castSuccess);
                                    }
                                }
                                if (mp.autocasting.type == TMDefs.AutocastType.OnCell && currentTarget != null)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, currentTarget);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        IntVec3 targetThing = localTarget.Cell;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(this.Pawn.Position, targetThing, this.Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (this.Pawn.Position - targetThing).LengthHorizontal)
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(this.Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnCell.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if (mp.autocasting.type == TMDefs.AutocastType.OnNearby)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, currentTarget);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        Thing targetThing = localTarget.Thing;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(this.Pawn.Position, targetThing, this.Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (this.Pawn.Position - targetThing.Position).LengthHorizontal)
                                        {
                                            continue;
                                        }
                                        bool TE = mp.autocasting.targetEnemy && targetThing.Faction != null && targetThing.Faction.HostileTo(this.Pawn.Faction);
                                        if (TE && targetThing is Pawn)
                                        {
                                            Pawn targetPawn = targetThing as Pawn;
                                            if (targetPawn.Downed || targetPawn.IsPrisoner)
                                            {
                                                continue;
                                            }
                                        }
                                        bool TN = mp.autocasting.targetNeutral && targetThing.Faction != null && !targetThing.Faction.HostileTo(this.Pawn.Faction);
                                        if (TN && targetThing is Pawn)
                                        {
                                            Pawn targetPawn = targetThing as Pawn;
                                            if (targetPawn.Downed || targetPawn.IsPrisoner)
                                            {
                                                continue;
                                            }
                                            if (mp.abilityDef.MainVerb.isViolent && !targetPawn.InMentalState)
                                            {
                                                continue;
                                            }
                                        }
                                        bool TNF = mp.autocasting.targetNoFaction && targetThing.Faction == null;
                                        if (TNF && targetThing is Pawn)
                                        {
                                            Pawn targetPawn = targetThing as Pawn;
                                            if (targetPawn.Downed || targetPawn.IsPrisoner)
                                            {
                                                continue;
                                            }
                                        }
                                        bool TF = mp.autocasting.targetFriendly && targetThing.Faction == this.Pawn.Faction;
                                        if (!(TE || TN || TF || TNF))
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(this.Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                            }
                            //}
                            //catch
                            //{
                            //    Log.Message("no index found at " + mp.level + " for " + mp.abilityDef.defName);
                            //}
                        }
                        if (castSuccess) goto AIAutoCastExit;
                    }
                    AIAutoCastExit:;
                }
            }
        }

        private void ResolveSpiritOfLight()
        {
            if(this.SoL != null)
            {
                //if(!this.spell_CreateLight)
                //{
                //    this.RemovePawnAbility(TorannMagicDefOf.TM_SoL_CreateLight);
                //    this.AddPawnAbility(TorannMagicDefOf.TM_SoL_CreateLight);
                //    this.spell_CreateLight = true;
                //}
                if(!this.spell_EqualizeLight)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_SoL_Equalize);
                    this.AddPawnAbility(TorannMagicDefOf.TM_SoL_Equalize);
                    this.spell_EqualizeLight = true;
                }
            }
            if(this.SoL == null)
            {
                if(this.spell_CreateLight || this.spell_EqualizeLight)
                {
                    //this.RemovePawnAbility(TorannMagicDefOf.TM_SoL_CreateLight);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_SoL_Equalize);
                    this.spell_EqualizeLight = false;
                    //this.spell_CreateLight = false;
                }
            }
        }

        private void ResolveEarthSpriteAction()
        {
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_EarthSprites.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EarthSprites_pwr");
            //Log.Message("resolving sprites");
            if (this.earthSpriteMap == null)
            {
                this.earthSpriteMap = this.Pawn.Map;
            }
            if (this.earthSpriteType == 1) //mining stone
            {
                //Log.Message("stone");
                Building mineTarget = this.earthSprites.GetFirstBuilding(this.earthSpriteMap);
                this.nextEarthSpriteAction = Find.TickManager.TicksGame + Mathf.RoundToInt((300 * (1 - (.1f * magicPowerSkill.level))) / this.arcaneDmg);
                TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.SparkFlash, this.earthSprites.ToVector3Shifted(), this.earthSpriteMap, Rand.Range(2f, 5f), .05f, 0f, .1f, 0, 0f, 0f, 0f);
                var mineable = mineTarget as Mineable;
                int num = 80;
                if (mineable != null && mineTarget.HitPoints > num)
                {
                    var dinfo = new DamageInfo(DamageDefOf.Mining, num, 0, -1f, this.Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                    mineTarget.TakeDamage(dinfo);
                    
                    if (Rand.Chance(ModOptions.Settings.Instance.magicyteChance * 2))
                    {
                        Thing thing = null;
                        thing = ThingMaker.MakeThing(TorannMagicDefOf.RawMagicyte);
                        thing.stackCount = Rand.Range(8, 16);
                        if (thing != null)
                        {
                            GenPlace.TryPlaceThing(thing, this.earthSprites, this.earthSpriteMap, ThingPlaceMode.Near, null);
                        }
                    }
                }
                else if (mineable != null && mineTarget.HitPoints <= num)
                {
                    mineable.DestroyMined(this.Pawn);
                }

                if (mineable.DestroyedOrNull())
                {
                    IntVec3 oldEarthSpriteLoc = this.earthSprites;
                    Building newMineSpot = null;
                    if (this.earthSpritesInArea)
                    {
                        //Log.Message("moving in area");
                        List<IntVec3> spriteAreaCells = GenRadial.RadialCellsAround(oldEarthSpriteLoc, 6f, false).ToList();
                        spriteAreaCells.Shuffle();
                        for (int i = 0; i < spriteAreaCells.Count; i++)
                        {
                            IntVec3 intVec = spriteAreaCells[i];
                            newMineSpot = intVec.GetFirstBuilding(this.earthSpriteMap);
                            if (newMineSpot != null && !intVec.Fogged(earthSpriteMap) && TM_Calc.GetSpriteArea() != null && TM_Calc.GetSpriteArea().ActiveCells.Contains(intVec))
                            {
                                mineable = newMineSpot as Mineable;
                                if (mineable != null)
                                {
                                    this.earthSprites = intVec;
                                    //Log.Message("assigning");
                                    break;
                                }
                                newMineSpot = null;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            IntVec3 intVec = earthSprites + GenAdj.AdjacentCells.RandomElement();
                            newMineSpot = intVec.GetFirstBuilding(this.earthSpriteMap);
                            if (newMineSpot != null)
                            {
                                mineable = newMineSpot as Mineable;
                                if (mineable != null)
                                {
                                    this.earthSprites = intVec;
                                    i = 20;
                                }
                                newMineSpot = null;
                            }
                        }
                    }

                    if (oldEarthSpriteLoc == this.earthSprites)
                    {
                        this.earthSpriteType = 0;
                        this.earthSprites = IntVec3.Invalid;
                        this.earthSpritesInArea = false;
                    }
                }
            }
            else if (this.earthSpriteType == 2) //transforming soil
            {
                //Log.Message("earth");
                this.nextEarthSpriteAction = Find.TickManager.TicksGame + Mathf.RoundToInt((24000 * (1 - (.1f * magicPowerSkill.level))) / this.arcaneDmg);
                for (int m = 0; m < 4; m++)
                {
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, this.earthSprites.ToVector3Shifted(), this.earthSpriteMap, Rand.Range(.3f, .5f), Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), Rand.Range(-20, 20), Rand.Range(.5f, 1f), Rand.Range(0, 360), Rand.Range(0, 360));
                }
                Map map = this.earthSpriteMap;
                IntVec3 curCell = this.earthSprites;
                TerrainDef terrain = curCell.GetTerrain(map);
                if (Rand.Chance(.8f))
                {
                    Thing thing = null;
                    thing = ThingMaker.MakeThing(TorannMagicDefOf.RawMagicyte);
                    thing.stackCount = Rand.Range(10, 20);
                    if (thing != null)
                    {
                        GenPlace.TryPlaceThing(thing, this.earthSprites, this.earthSpriteMap, ThingPlaceMode.Near, null);
                    }
                }
                if (curCell.InBoundsWithNullCheck(map) && curCell.IsValid && terrain != null)
                {
                    if (terrain.defName == "MarshyTerrain" || terrain.defName == "Mud" || terrain.defName == "Marsh")
                    {
                        map.terrainGrid.SetTerrain(curCell, terrain.driesTo);
                    }
                    else if (terrain.defName == "WaterShallow")
                    {
                        map.terrainGrid.SetTerrain(curCell, TerrainDef.Named("Marsh"));
                    }
                    else if (terrain.defName == "Ice")
                    {
                        map.terrainGrid.SetTerrain(curCell, TerrainDef.Named("Mud"));
                    }
                    else if (terrain.defName == "Soil")
                    {
                        map.terrainGrid.SetTerrain(curCell, TerrainDef.Named("SoilRich"));
                    }
                    else if (terrain.defName == "Sand" || terrain.defName == "Gravel" || terrain.defName == "MossyTerrain")
                    {
                        map.terrainGrid.SetTerrain(curCell, TerrainDef.Named("Soil"));
                    }
                    else if (terrain.defName == "SoftSand")
                    {
                        map.terrainGrid.SetTerrain(curCell, TerrainDef.Named("Sand"));
                    }
                    else
                    {
                        Log.Message("unable to resolve terraindef - resetting earth sprite parameters");
                        this.earthSprites = IntVec3.Invalid;
                        this.earthSpriteMap = null;
                        this.earthSpriteType = 0;
                        this.earthSpritesInArea = false;
                    }

                    terrain = curCell.GetTerrain(map);
                    if (terrain.defName == "SoilRich")
                    {
                        //look for new spot to transform
                        IntVec3 oldEarthSpriteLoc = this.earthSprites;
                        if (this.earthSpritesInArea)
                        {
                            //Log.Message("moving in area");
                            List<IntVec3> spriteAreaCells = GenRadial.RadialCellsAround(oldEarthSpriteLoc, 6f, false).ToList();
                            spriteAreaCells.Shuffle();
                            for (int i = 0; i < spriteAreaCells.Count; i++)
                            {
                                IntVec3 intVec = spriteAreaCells[i];
                                terrain = intVec.GetTerrain(map);
                                if (terrain.defName == "MarshyTerrain" || terrain.defName == "Mud" || terrain.defName == "Marsh" || terrain.defName == "WaterShallow" || terrain.defName == "Ice" ||
                            terrain.defName == "Sand" || terrain.defName == "Gravel" || terrain.defName == "Soil" || terrain.defName == "MossyTerrain" || terrain.defName == "SoftSand")
                                {
                                    Building terrainHasBuilding = null;
                                    terrainHasBuilding = intVec.GetFirstBuilding(this.earthSpriteMap);
                                    if (TM_Calc.GetSpriteArea() != null && TM_Calc.GetSpriteArea().ActiveCells.Contains(intVec)) //dont transform terrain underneath buildings
                                    {
                                        //Log.Message("assigning");
                                        this.earthSprites = intVec;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                IntVec3 intVec = earthSprites + GenAdj.AdjacentCells.RandomElement();
                                terrain = intVec.GetTerrain(map);
                                if (terrain.defName == "MarshyTerrain" || terrain.defName == "Mud" || terrain.defName == "Marsh" || terrain.defName == "WaterShallow" || terrain.defName == "Ice" ||
                            terrain.defName == "Sand" || terrain.defName == "Gravel" || terrain.defName == "Soil" || terrain.defName == "MossyTerrain" || terrain.defName == "SoftSand")
                                {
                                    Building terrainHasBuilding = null;
                                    terrainHasBuilding = intVec.GetFirstBuilding(this.earthSpriteMap);
                                    if (terrainHasBuilding == null) //dont transform terrain underneath buildings
                                    {
                                        this.earthSprites = intVec;
                                        i = 20;
                                    }
                                }
                            }
                        }

                        if (oldEarthSpriteLoc == earthSprites)
                        {
                            this.earthSpriteType = 0;
                            this.earthSpriteMap = null;
                            this.earthSprites = IntVec3.Invalid;
                            this.earthSpritesInArea = false;
                            //Log.Message("ending");
                        }
                    }
                }
            }
        }

        public void ResolveEffecter()
        {
            if (PowerModifier <= 0) return;

            bool spawned = this.Pawn.Spawned;
            if (spawned)
            {
                if (this.powerEffecter != null)
                {
                    powerEffecter = EffecterDefOf.ProgressBar.Spawn();
                    powerEffecter.EffectTick(Pawn, TargetInfo.Invalid);
                    MoteProgressBar mote = ((SubEffecter_ProgressBar)powerEffecter.children[0]).mote;
                    if (mote == null) return;

                    mote.progress = Mathf.Clamp01((float)powerModifier / maxPower);
                    mote.offsetZ = +0.85f;                    
                }
            }
        }

        public void ResolveUndead()
        {
            if (supportedUndead != null)
            {
                List<Thing> tmpList = new List<Thing>();
                tmpList.Clear();
                for(int i =0; i < supportedUndead.Count; i++)
                {
                    Pawn p = supportedUndead[i] as Pawn;
                    if(p.DestroyedOrNull() || p.Dead)
                    {
                        tmpList.Add(p);
                    }
                }
                for(int i = 0; i < tmpList.Count; i++)
                {
                    supportedUndead.Remove(tmpList[i]);
                }
                if (this.supportedUndead.Count > 0 && this.dismissUndeadSpell == false)
                {
                    this.AddPawnAbility(TorannMagicDefOf.TM_DismissUndead);
                    this.dismissUndeadSpell = true;
                }
                if (this.supportedUndead.Count <= 0 && this.dismissUndeadSpell == true)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_DismissUndead);
                    this.dismissUndeadSpell = false;
                }
            }
            else
            {
                this.supportedUndead = new List<Thing>();
            }
        }

        public void ResolveSuccubusLovin()
        {
            if (this.Pawn.CurrentBed() != null && this.Pawn.ageTracker.AgeBiologicalYears > 17 && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_VitalityBoostHD"), false))
            {
                Pawn pawnInMyBed = TM_Calc.FindNearbyOtherPawn(this.Pawn, 1);
                if (pawnInMyBed != null)
                {
                    if (pawnInMyBed.CurrentBed() != null && pawnInMyBed.RaceProps.Humanlike && pawnInMyBed.ageTracker.AgeBiologicalYears > 17)
                    {
                        Job job = new Job(JobDefOf.Lovin, pawnInMyBed, this.Pawn.CurrentBed());
                        this.Pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        HealthUtility.AdjustSeverity(pawnInMyBed, HediffDef.Named("TM_VitalityDrainHD"), 8);
                        HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_VitalityBoostHD"), 6);
                    }
                }
            }
        }

        public void ResolveWarlockEmpathy()
        {
            //strange bug observed where other pawns will get the old offset of the previous pawn's offset unless other pawn has no empathy existing
            //in other words, empathy base mood effect seems to carry over from last otherpawn instead of using current otherpawn values
            if (Rand.Chance(this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity, false) - 1))
            {
                Pawn otherPawn = TM_Calc.FindNearbyOtherPawn(this.Pawn, 5);
                if (otherPawn != null && otherPawn.RaceProps.Humanlike && otherPawn.IsColonist)
                {
                    if (Rand.Chance(otherPawn.GetStatValue(StatDefOf.PsychicSensitivity, false) - .3f))
                    {
                        ThoughtHandler pawnThoughtHandler = this.Pawn.needs.mood.thoughts;
                        List<Thought> pawnThoughts = new List<Thought>();
                        pawnThoughtHandler.GetAllMoodThoughts(pawnThoughts);
                        List<Thought> otherThoughts = new List<Thought>();
                        otherPawn.needs.mood.thoughts.GetAllMoodThoughts(otherThoughts);
                        List<Thought_Memory> memoryThoughts = new List<Thought_Memory>();
                        memoryThoughts.Clear();
                        float oldMemoryOffset = 0;
                        if (Rand.Chance(.3f)) //empathy absorbed by warlock
                        {
                            ThoughtDef empathyThought = ThoughtDef.Named("WarlockEmpathy");
                            memoryThoughts = this.Pawn.needs.mood.thoughts.memories.Memories;
                            for (int i = 0; i < memoryThoughts.Count; i++)
                            {
                                if (memoryThoughts[i].def.defName == "WarlockEmpathy")
                                {
                                    oldMemoryOffset = memoryThoughts[i].MoodOffset();
                                    if (oldMemoryOffset > 30)
                                    {
                                        oldMemoryOffset = 30;
                                    }
                                    else if (oldMemoryOffset < -30)
                                    {
                                        oldMemoryOffset = -30;
                                    }
                                    this.Pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(memoryThoughts[i].def);
                                }
                            }
                            Thought transferThought = otherThoughts.RandomElement();
                            float newOffset = Mathf.RoundToInt(transferThought.CurStage.baseMoodEffect / 2);
                            empathyThought.stages.FirstOrDefault().baseMoodEffect = newOffset + oldMemoryOffset;

                            this.Pawn.needs.mood.thoughts.memories.TryGainMemory(empathyThought, null);
                            Vector3 drawPosOffset = this.Pawn.DrawPos;
                            drawPosOffset.z += .3f;
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ArcaneCircle, drawPosOffset, this.Pawn.Map, newOffset / 20, .2f, .1f, .1f, Rand.Range(100, 200), 0, 0, Rand.Range(0, 360));
                        }
                        else //empathy bleeding to other pawn
                        {
                            ThoughtDef empathyThought = ThoughtDef.Named("PsychicEmpathy");
                            memoryThoughts = otherPawn.needs.mood.thoughts.memories.Memories;
                            for (int i = 0; i < memoryThoughts.Count; i++)
                            {
                                if (memoryThoughts[i].def.defName == "PsychicEmpathy")
                                {
                                    oldMemoryOffset = memoryThoughts[i].CurStage.baseMoodEffect;
                                    if (oldMemoryOffset > 30)
                                    {
                                        oldMemoryOffset = 30;
                                    }
                                    else if (oldMemoryOffset < -30)
                                    {
                                        oldMemoryOffset = -30;
                                    }
                                    otherPawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(memoryThoughts[i].def);
                                }
                            }
                            Thought transferThought = pawnThoughts.RandomElement();
                            float newOffset = Mathf.RoundToInt(transferThought.CurStage.baseMoodEffect / 2);
                            empathyThought.stages.FirstOrDefault().baseMoodEffect = newOffset + oldMemoryOffset;

                            otherPawn.needs.mood.thoughts.memories.TryGainMemory(empathyThought, null);
                            Vector3 drawPosOffset = otherPawn.DrawPos;
                            drawPosOffset.z += .3f;
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ArcaneCircle, drawPosOffset, otherPawn.Map, newOffset / 20, .2f, .1f, .1f, Rand.Range(100, 200), 0, 0, Rand.Range(0, 360));
                        }
                    }
                }
            }
        }

        public void ResolveTechnomancerOverdrive()
        {
            if (this.overdriveBuilding != null)
            {
                List<Pawn> odPawns = ModOptions.Constants.GetOverdrivePawnList();

                if (!odPawns.Contains(this.Pawn))
                {
                    odPawns.Add(this.Pawn);
                    ModOptions.Constants.SetOverdrivePawnList(odPawns);
                }
                Vector3 rndPos = this.overdriveBuilding.DrawPos;
                rndPos.x += Rand.Range(-.4f, .4f);
                rndPos.z += Rand.Range(-.4f, .4f);
                TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.SparkFlash, rndPos, this.overdriveBuilding.Map, Rand.Range(.6f, .8f), .1f, .05f, .05f, 0, 0, 0, Rand.Range(0, 360));
                FleckMaker.ThrowSmoke(rndPos, this.overdriveBuilding.Map, Rand.Range(.8f, 1.2f));
                rndPos = this.overdriveBuilding.DrawPos;
                rndPos.x += Rand.Range(-.4f, .4f);
                rndPos.z += Rand.Range(-.4f, .4f);
                TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.ElectricalSpark, rndPos, this.overdriveBuilding.Map, Rand.Range(.4f, .7f), .2f, .05f, .1f, 0, 0, 0, Rand.Range(0, 360));
                SoundInfo info = SoundInfo.InMap(new TargetInfo(this.overdriveBuilding.Position, this.overdriveBuilding.Map, false), MaintenanceType.None);
                info.pitchFactor = .4f;
                info.volumeFactor = .3f;
                SoundDefOf.TurretAcquireTarget.PlayOneShot(info);
                MagicPowerSkill damageControl = this.MagicData.MagicPowerSkill_Overdrive.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Overdrive_ver");
                if (Rand.Chance(.6f - (.06f * damageControl.level)))
                {
                    TM_Action.DamageEntities(this.overdriveBuilding, null, Rand.Range(3f, (7f - (1f * damageControl.level))), DamageDefOf.Burn, this.overdriveBuilding);
                }
                this.overdriveFrequency = 100 + (10 * damageControl.level);
                if (Rand.Chance(.4f))
                {
                    this.overdriveFrequency /= 2;
                }
                this.overdriveDuration--;
                if (this.overdriveDuration <= 0)
                {
                    if (odPawns != null && odPawns.Contains(this.Pawn))
                    {
                        ModOptions.Constants.ClearOverdrivePawns();
                        odPawns.Remove(this.Pawn);
                        ModOptions.Constants.SetOverdrivePawnList(odPawns);
                    }
                    this.overdrivePowerOutput = 0;
                    this.overdriveBuilding = null;
                }
            }
        }

        public void ResolveChronomancerTimeMark()
        {
            //Log.Message("pawn " + this.Pawn.LabelShort + " recallset: " + this.recallSet + " expiration: " + this.recallExpiration + " / " + Find.TickManager.TicksGame + " recallSpell: " + this.recallSpell + " position: " + this.recallPosition);
            if(this.customClass != null && MagicData.MagicPowersC.FirstOrDefault((MagicPower x ) => x.abilityDef == TorannMagicDefOf.TM_Recall).learned && !MagicData.MagicPowersStandalone.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TimeMark).learned)
            {
                MagicData.MagicPowersStandalone.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TimeMark).learned = true;
                this.RemovePawnAbility(TorannMagicDefOf.TM_TimeMark);
                this.AddPawnAbility(TorannMagicDefOf.TM_TimeMark);
            }
            if (this.recallExpiration <= Find.TickManager.TicksGame)
            {
                this.recallSet = false;
            }
            if (this.recallSet && !this.recallSpell)
            {
                this.AddPawnAbility(TorannMagicDefOf.TM_Recall);
                this.recallSpell = true;
            }
            if (this.recallSpell && (!this.recallSet || this.recallPosition == default(IntVec3)))
            {
                this.recallSpell = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_Recall);
            }
        }

        public void ResolveSustainers()
        {
            if(this.BrandPawns != null && this.BrandPawns.Count > 0)
            {
                if(!this.dispelBrandings)
                {
                    this.AddPawnAbility(TorannMagicDefOf.TM_DispelBranding);
                    this.dispelBrandings = true;
                }
                List<Pawn> tmpBrands = new List<Pawn>();
                tmpBrands.Clear();
                for(int i = 0; i < BrandPawns.Count; i++)
                {
                    Pawn p = BrandPawns[i];
                    if(p != null && (p.Destroyed || p.Dead))
                    {
                        BrandPawns.Remove(BrandPawns[i]);
                        BrandDefs.Remove(BrandDefs[i]);
                        break;
                    }
                }
                if(sigilSurging && this.Mana.CurLevel <= .01f)
                {
                    this.sigilSurging = false;
                }
            }
            else if(dispelBrandings)
            {
                this.dispelBrandings = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_DispelBranding);
            }
            if (this.livingWall != null)
            {
                if (!this.dispelLivingWall)
                {
                    this.dispelLivingWall = true;
                    this.RemovePawnAbility(TorannMagicDefOf.TM_DispelLivingWall);
                    this.AddPawnAbility(TorannMagicDefOf.TM_DispelLivingWall);
                }
            }
            else if(this.dispelLivingWall)
            {
                this.dispelLivingWall = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_DispelLivingWall);
            }

            if (this.stoneskinPawns.Count() > 0)
            {
                if (!this.dispelStoneskin)
                {
                    this.dispelStoneskin = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_DispelStoneskin);
                }
                for (int i = 0; i < this.stoneskinPawns.Count(); i++)
                {
                    if (this.stoneskinPawns[i].DestroyedOrNull() || this.stoneskinPawns[i].Dead)
                    {
                        this.stoneskinPawns.Remove(this.stoneskinPawns[i]);
                    }
                    else
                    {
                        if (!this.stoneskinPawns[i].health.hediffSet.HasHediff(HediffDef.Named("TM_StoneskinHD"), false))
                        {
                            this.stoneskinPawns.Remove(this.stoneskinPawns[i]);
                        }
                    }
                }
            }
            else if (this.dispelStoneskin)
            {
                this.dispelStoneskin = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_DispelStoneskin);
            }

            if(this.bondedSpirit != null && !this.dismissGuardianSpirit)
            {
                this.AddPawnAbility(TorannMagicDefOf.TM_DismissGuardianSpirit);
                this.dismissGuardianSpirit = true;
            }
            if (this.bondedSpirit == null && this.dismissGuardianSpirit)
            {
                this.RemovePawnAbility(TorannMagicDefOf.TM_DismissGuardianSpirit);
                this.dismissGuardianSpirit = false;
            }

            if (this.summonedLights.Count > 0 && dismissSunlightSpell == false)
            {
                this.AddPawnAbility(TorannMagicDefOf.TM_DismissSunlight);
                dismissSunlightSpell = true;
            }

            if (this.summonedLights.Count <= 0 && dismissSunlightSpell == true)
            {
                this.RemovePawnAbility(TorannMagicDefOf.TM_DismissSunlight);
                dismissSunlightSpell = false;
            }

            if (this.summonedPowerNodes.Count > 0 && this.dismissPowerNodeSpell == false)
            {
                this.AddPawnAbility(TorannMagicDefOf.TM_DismissPowerNode);
                dismissPowerNodeSpell = true;
            }

            if (this.summonedPowerNodes.Count <= 0 && dismissPowerNodeSpell == true)
            {
                this.RemovePawnAbility(TorannMagicDefOf.TM_DismissPowerNode);
                dismissPowerNodeSpell = false;
            }

            if (this.summonedCoolers.Count > 0 && dismissCoolerSpell == false)
            {
                this.AddPawnAbility(TorannMagicDefOf.TM_DismissCooler);
                dismissCoolerSpell = true;
            }

            if (this.summonedCoolers.Count <= 0 && dismissCoolerSpell == true)
            {
                this.RemovePawnAbility(TorannMagicDefOf.TM_DismissCooler);
                dismissCoolerSpell = false;
            }

            if (this.summonedHeaters.Count > 0 && dismissHeaterSpell == false)
            {
                this.AddPawnAbility(TorannMagicDefOf.TM_DismissHeater);
                dismissHeaterSpell = true;
            }

            if (this.summonedHeaters.Count <= 0 && dismissHeaterSpell == true)
            {
                this.RemovePawnAbility(TorannMagicDefOf.TM_DismissHeater);
                dismissHeaterSpell = false;
            }

            if (this.enchanterStones.Count > 0 && this.dismissEnchanterStones == false)
            {
                this.AddPawnAbility(TorannMagicDefOf.TM_DismissEnchanterStones);
                dismissEnchanterStones = true;
            }
            if (this.enchanterStones.Count <= 0 && this.dismissEnchanterStones == true)
            {
                this.RemovePawnAbility(TorannMagicDefOf.TM_DismissEnchanterStones);
                dismissEnchanterStones = false;
            }

            if (this.lightningTraps.Count > 0 && this.dismissLightningTrap == false)
            {
                this.AddPawnAbility(TorannMagicDefOf.TM_DismissLightningTrap);
                dismissLightningTrap = true;
            }
            if (this.lightningTraps.Count <= 0 && this.dismissLightningTrap == true)
            {
                this.RemovePawnAbility(TorannMagicDefOf.TM_DismissLightningTrap);
                dismissLightningTrap = false;
            }

            if (this.summonedSentinels.Count > 0 && this.shatterSentinel == false)
            {
                this.AddPawnAbility(TorannMagicDefOf.TM_ShatterSentinel);
                shatterSentinel = true;
            }
            if (this.summonedSentinels.Count <= 0 && this.shatterSentinel == true)
            {
                this.RemovePawnAbility(TorannMagicDefOf.TM_ShatterSentinel);
                shatterSentinel = false;
            }

            if (this.soulBondPawn.DestroyedOrNull() && (this.spell_ShadowStep == true || this.spell_ShadowCall == true))
            {
                this.soulBondPawn = null;
                this.spell_ShadowCall = false;
                this.spell_ShadowStep = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_ShadowCall);
                this.RemovePawnAbility(TorannMagicDefOf.TM_ShadowStep);
            }
            if (this.soulBondPawn != null)
            {
                if (this.spell_ShadowStep == false)
                {
                    this.spell_ShadowStep = true;
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ShadowStep);
                    this.AddPawnAbility(TorannMagicDefOf.TM_ShadowStep);
                }
                if (this.spell_ShadowCall == false)
                {
                    this.spell_ShadowCall = true;
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ShadowCall);
                    this.AddPawnAbility(TorannMagicDefOf.TM_ShadowCall);
                }
            }

            if (this.weaponEnchants != null && this.weaponEnchants.Count > 0)
            {
                for (int i = 0; i < this.weaponEnchants.Count; i++)
                {
                    Pawn ewPawn = weaponEnchants[i];
                    if (ewPawn.DestroyedOrNull() || ewPawn.Dead)
                    {
                        this.weaponEnchants.Remove(ewPawn);
                    }
                }

                if (this.dispelEnchantWeapon == false)
                {
                    this.dispelEnchantWeapon = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_DispelEnchantWeapon);
                }
            }
            else if (this.dispelEnchantWeapon == true)
            {
                this.dispelEnchantWeapon = false;
                this.RemovePawnAbility(TorannMagicDefOf.TM_DispelEnchantWeapon);
            }

            if (this.mageLightActive)
            {
                if (this.Pawn.Map == null && this.mageLightSet)
                {
                    this.mageLightActive = false;
                    this.mageLightThing = null;
                    this.mageLightSet = false;
                }
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MageLightHD);
                if (hediff == null && !mageLightSet)
                {
                    HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_MageLightHD, .5f);
                }
                if (mageLightSet && this.mageLightThing == null)
                {
                    this.mageLightActive = false;
                }
            }
            else
            {
                Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MageLightHD);
                if (hediff != null)
                {
                    this.Pawn.health.RemoveHediff(hediff);
                }
                if (!this.mageLightThing.DestroyedOrNull())
                {
                    this.mageLightThing.Destroy(DestroyMode.Vanish);
                    this.mageLightThing = null;
                }
                this.mageLightSet = false;
            }            
        }

        public void ResolveMinions()
        {
            if (this.summonedMinions.Count > 0 && dismissMinionSpell == false)
            {
                this.AddPawnAbility(TorannMagicDefOf.TM_DismissMinion);
                dismissMinionSpell = true;
            }

            if (this.summonedMinions.Count <= 0 && dismissMinionSpell == true)
            {
                this.RemovePawnAbility(TorannMagicDefOf.TM_DismissMinion);
                dismissMinionSpell = false;
            }

            if (this.summonedMinions.Count > 0)
            {
                for (int i = 0; i < this.summonedMinions.Count(); i++)
                {
                    Pawn minion = this.summonedMinions[i] as Pawn;
                    if (minion != null)
                    {
                        if (minion.DestroyedOrNull() || minion.Dead)
                        {
                            this.summonedMinions.Remove(this.summonedMinions[i]);
                            i--;
                        }
                    }
                    else
                    {
                        this.summonedMinions.Remove(this.summonedMinions[i]);
                        i--;
                    }
                }
            }

            if (this.earthSpriteType != 0 && dismissEarthSpriteSpell == false)
            {
                this.AddPawnAbility(TorannMagicDefOf.TM_DismissEarthSprites);
                dismissEarthSpriteSpell = true;
            }

            if (this.earthSpriteType == 0 && dismissEarthSpriteSpell == true)
            {
                this.RemovePawnAbility(TorannMagicDefOf.TM_DismissEarthSprites);
                dismissEarthSpriteSpell = false;
            }
        }

        public void ResolveMana()
        {
            bool flag = this.Mana == null;
            if (flag)
            {                
                Hediff firstHediffOfDef = base.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MagicUserHD, false);
                bool flag2 = firstHediffOfDef != null;
                if (flag2)
                {
                    firstHediffOfDef.Severity = 1f;
                }
                else
                {
                    Hediff hediff = HediffMaker.MakeHediff(TorannMagicDefOf.TM_MagicUserHD, base.Pawn, null);
                    hediff.Severity = 1f;
                    base.Pawn.health.AddHediff(hediff, null, null);
                }
                this.Pawn.needs.AddOrRemoveNeedsAsAppropriate();
            }
        }
        public void ResolveMagicPowers()
        {
            bool flag = this.magicPowersInitialized;
            if (!flag)
            {
                this.magicPowersInitialized = true;
            }
        }
        public void ResolveMagicTab()
        {
            if (!this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                InspectTabBase inspectTabsx = base.Pawn.GetInspectTabs().FirstOrDefault((InspectTabBase x) => x.labelKey == "TM_TabMagic");
                IEnumerable<InspectTabBase> inspectTabs = base.Pawn.GetInspectTabs();
                bool flag = inspectTabs != null && inspectTabs.Count<InspectTabBase>() > 0;
                if (flag)
                {
                    if (inspectTabsx == null)
                    {
                        try
                        {
                            base.Pawn.def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_Pawn_Magic)));
                        }
                        catch (Exception ex)
                        {
                            Log.Error(string.Concat(new object[]
                            {
                            "Could not instantiate inspector tab of type ",
                            typeof(ITab_Pawn_Magic),
                            ": ",
                            ex
                            }));
                        }
                    }
                }
            }
        }

        public void ResolveClassSkills()
        {
            bool flagCM = this.Pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage);
            bool isCustom = this.customClass != null;

            if(isCustom && this.customClass.classHediff != null && !this.Pawn.health.hediffSet.HasHediff(this.customClass.classHediff))
            {
                HealthUtility.AdjustSeverity(this.Pawn, this.customClass.classHediff, this.customClass.hediffSeverity);                
            }

            if(this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_CursedTD) && !this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CursedHD))
            {
                HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_CursedHD, .1f);
            }

            if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.BloodMage) || (isCustom && (this.customClass.classMageAbilities.Contains(TorannMagicDefOf.TM_BloodGift) || this.customClass.classHediff == TorannMagicDefOf.TM_BloodHD)))
            {
                if (!this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_BloodHD")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_BloodHD"), .1f);
                    for (int i = 0; i < 4; i++)
                    {
                        TM_MoteMaker.ThrowBloodSquirt(this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.5f, .8f));
                    }
                }
            }

            if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer) || flagCM || (isCustom && this.customClass.classMageAbilities.Contains(TorannMagicDefOf.TM_Prediction)))
            {
                if (this.predictionIncidentDef != null && (this.predictionTick + 30) < Find.TickManager.TicksGame)
                {
                    this.predictionIncidentDef = null;
                    Find.Storyteller.incidentQueue.Clear();
                    //Log.Message("prediction failed to execute, clearing prediction");
                }
            }


            if(HexedPawns != null && HexedPawns.Count <= 0 && previousHexedPawns > 0)
            {
                if (HexedPawns.Count > 0)
                {
                    previousHexedPawns = HexedPawns.Count;
                }
                else if (previousHexedPawns > 0)
                {
                    //remove abilities
                    previousHexedPawns = 0;
                    RemovePawnAbility(TorannMagicDefOf.TM_Hex_Pain);
                    RemovePawnAbility(TorannMagicDefOf.TM_Hex_MentalAssault);
                    RemovePawnAbility(TorannMagicDefOf.TM_Hex_CriticalFail);
                }
            }

            if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter) || flagCM || isCustom)
            {
                if (this.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchantedBody).learned && (this.spell_EnchantedAura == false || !this.MagicData.MagicPowersStandalone.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchantedAura).learned))
                {
                    this.spell_EnchantedAura = true;
                    this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchantedAura).learned = true;
                    this.InitializeSpell();
                }

                if (this.MagicData.MagicPowerSkill_Shapeshift.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Shapeshift_ver").level >= 3 && (this.spell_ShapeshiftDW != true || !this.MagicData.MagicPowersStandalone.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ShapeshiftDW).learned))
                {
                    this.spell_ShapeshiftDW = true;
                    this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ShapeshiftDW).learned = true;
                    this.InitializeSpell();
                }
            }

            if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || flagCM || isCustom)
            {
                if (this.HasTechnoBit)
                {
                    if (!this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TechnoBitHD))
                    {
                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_TechnoBitHD, .5f);
                        Vector3 bitDrawPos = this.Pawn.DrawPos;
                        bitDrawPos.x -= .5f;
                        bitDrawPos.z += .45f;
                        for (int i = 0; i < 4; i++)
                        {
                            FleckMaker.ThrowSmoke(bitDrawPos, this.Pawn.Map, Rand.Range(.6f, .8f));
                        }
                    }
                }
                if (this.HasTechnoWeapon && this.Pawn.equipment != null && this.Pawn.equipment.Primary != null)
                {
                    if (this.Pawn.equipment.Primary.def.defName.Contains("TM_TechnoWeapon_Base") && this.Pawn.equipment.Primary.def.Verbs != null && this.Pawn.equipment.Primary.def.Verbs.FirstOrDefault().range < 2)
                    {
                        TM_Action.DoAction_TechnoWeaponCopy(this.Pawn, this.technoWeaponThing, this.technoWeaponThingDef, this.technoWeaponQC);
                    }

                    if (!this.Pawn.equipment.Primary.def.defName.Contains("TM_TechnoWeapon_Base") && (this.technoWeaponThing != null || this.technoWeaponThingDef != null))
                    {
                        this.technoWeaponThing = null;
                        this.technoWeaponThingDef = null;
                    }
                }
            }

            if (this.MagicUserLevel >= 20 && (this.spell_Teach == false || !this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TeachMagic).learned))
            {
                this.AddPawnAbility(TorannMagicDefOf.TM_TeachMagic);
                this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TeachMagic).learned = true;
                this.spell_Teach = true;
            }

            if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Geomancer) || flagCM || isCustom) && this.earthSpriteType != 0 && this.earthSprites.IsValid)
            {
                if (this.nextEarthSpriteAction < Find.TickManager.TicksGame)
                {
                    ResolveEarthSpriteAction();
                }

                if (this.nextEarthSpriteMote < Find.TickManager.TicksGame)
                {
                    this.nextEarthSpriteMote += Rand.Range(7, 12);
                    Vector3 shiftLoc = this.earthSprites.ToVector3Shifted();
                    shiftLoc.x += Rand.Range(-.3f, .3f);
                    shiftLoc.z += Rand.Range(-.3f, .3f);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Twinkle, shiftLoc, this.Pawn.Map, Rand.Range(.6f, 1.4f), .15f, Rand.Range(.2f, .5f), Rand.Range(.2f, .5f), Rand.Range(-100, 100), Rand.Range(0f, .3f), Rand.Range(0, 360), 0);
                    if(Rand.Chance(.3f))
                    {
                        shiftLoc = this.earthSprites.ToVector3Shifted();
                        shiftLoc.x += Rand.Range(-.3f, .3f);
                        shiftLoc.z += Rand.Range(-.3f, .3f);
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_GreenTwinkle, shiftLoc, this.Pawn.Map, Rand.Range(.6f, 1f), .15f, Rand.Range(.2f, .9f), Rand.Range(.5f, .9f), Rand.Range(-200, 200), Rand.Range(0f, .3f), Rand.Range(0, 360), 0);
                    }
                }
            }

            if (this.summonedSentinels.Count > 0)
            {
                for (int i = 0; i < this.summonedSentinels.Count(); i++)
                {
                    if (summonedSentinels[i].DestroyedOrNull())
                    {
                        this.summonedSentinels.Remove(this.summonedSentinels[i]);
                    }
                }
            }

            if (this.lightningTraps.Count > 0)
            {
                for (int i = 0; i < this.lightningTraps.Count(); i++)
                {
                    if (lightningTraps[i].DestroyedOrNull())
                    {
                        this.lightningTraps.Remove(this.lightningTraps[i]);
                    }
                }
            }

            if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Lich))
            {
                if (!this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_LichHD")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_LichHD"), .5f);
                }
                if (this.spell_Flight != true)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_DeathBolt);
                    this.AddPawnAbility(TorannMagicDefOf.TM_DeathBolt);
                    this.spell_Flight = true;
                    this.InitializeSpell();
                }
            }

            if (this.IsMagicUser && !this.Pawn.Dead && !this.Pawn.Downed)
            {
                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard))
                {
                    MagicPowerSkill bardtraining_pwr = this.Pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BardTraining.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BardTraining_pwr");

                    List<Trait> traits = this.Pawn.story.traits.allTraits;
                    for (int i = 0; i < traits.Count; i++)
                    {
                        if (traits[i].def.defName == "TM_Bard")
                        {
                            if (traits[i].Degree != bardtraining_pwr.level)
                            {
                                traits.Remove(traits[i]);
                                this.Pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Bard, bardtraining_pwr.level, false));
                                FleckMaker.ThrowHeatGlow(this.Pawn.Position, this.Pawn.Map, 2);
                            }
                        }
                    }
                }

                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus) || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock))
                {
                    if (this.soulBondPawn != null)
                    {
                        if (!this.soulBondPawn.Spawned)
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_SummonDemon);
                            this.spell_SummonDemon = false;
                        }
                        else if (this.soulBondPawn.health.hediffSet.HasHediff(HediffDef.Named("TM_DemonicPriceHD"), false))
                        {
                            if (this.spell_SummonDemon == true)
                            {
                                this.RemovePawnAbility(TorannMagicDefOf.TM_SummonDemon);
                                this.spell_SummonDemon = false;
                            }
                        }
                        else if (this.soulBondPawn.health.hediffSet.HasHediff(HediffDef.Named("TM_SoulBondMentalHD")) && this.soulBondPawn.health.hediffSet.HasHediff(HediffDef.Named("TM_SoulBondPhysicalHD")))
                        {
                            if (this.spell_SummonDemon == false)
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_SummonDemon);
                                this.spell_SummonDemon = true;
                            }
                        }
                        else
                        {
                            if (this.spell_SummonDemon == true)
                            {
                                this.RemovePawnAbility(TorannMagicDefOf.TM_SummonDemon);
                                this.spell_SummonDemon = false;
                            }
                        }
                    }
                    else if (this.spell_SummonDemon == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_SummonDemon);
                        this.spell_SummonDemon = false;
                    }
                }
            }

            if (this.IsMagicUser && !this.Pawn.Dead & !this.Pawn.Downed && (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard) || (isCustom && this.customClass.classMageAbilities.Contains(TorannMagicDefOf.TM_Inspire))))
            {
                if (!this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_InspirationalHD")) && this.MagicData.MagicPowersB.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Inspire).learned)
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_InspirationalHD"), 0.95f);
                }
            }
        }

        public void ResolveEnchantments()
        {
            float _maxMP = 0;
            float _maxMPUpkeep = 0;
            float _mpRegenRate = 0;
            float _mpRegenRateUpkeep = 0;
            float _coolDown = 0;
            float _xpGain = 0;
            float _mpCost = 0;
            float _arcaneRes = 0;
            float _arcaneDmg = 0;
            bool _arcaneSpectre = false;
            bool _phantomShift = false;
            float _arcalleumCooldown = 0f;

            //Determine trait adjustments
            IEnumerable<TMDefs.DefModExtension_TraitEnchantments> traitEnum = this.Pawn.story.traits.allTraits
                .Select((Trait t) => t.def.GetModExtension<TMDefs.DefModExtension_TraitEnchantments>());
            foreach (TMDefs.DefModExtension_TraitEnchantments e in traitEnum)
            {
                if (e != null)
                {
                    _maxMP += e.maxMP;
                    _mpCost += e.mpCost;
                    _mpRegenRate += e.mpRegenRate;
                    _coolDown += e.magicCooldown;
                    _xpGain += e.xpGain;
                    _arcaneRes += e.arcaneRes;
                    _arcaneDmg += e.arcaneDmg;
                }
            }

            //Determine hediff adjustments
            foreach(Hediff hd in this.Pawn.health.hediffSet.hediffs)
            {
                if(hd.def.GetModExtension<TMDefs.DefModExtension_HediffEnchantments>() != null)
                {                    
                    foreach(TMDefs.HediffEnchantment hdStage in hd.def.GetModExtension<TMDefs.DefModExtension_HediffEnchantments>().stages)
                    {
                        if(hd.Severity >= hdStage.minSeverity && hd.Severity < hdStage.maxSeverity)
                        {
                            TMDefs.DefModExtension_TraitEnchantments e = hdStage.enchantments;
                            if (e != null)
                            {
                                _maxMP += e.maxMP;
                                _mpCost += e.mpCost;
                                _mpRegenRate += e.mpRegenRate;
                                _coolDown += e.magicCooldown;
                                _xpGain += e.xpGain;
                                _arcaneRes += e.arcaneRes;
                                _arcaneDmg += e.arcaneDmg;
                            }
                            break;
                        }
                    }
                }
            }

            List<Apparel> apparel = this.Pawn.apparel.WornApparel;
            if (apparel != null)
            {
                for (int i = 0; i < this.Pawn.apparel.WornApparelCount; i++)
                {
                    Enchantment.CompEnchantedItem item = apparel[i].GetComp<Enchantment.CompEnchantedItem>();
                    if (item != null)
                    {
                        if (item.HasEnchantment)
                        {
                            float enchantmentFactor = 1f;
                            if (item.MadeFromEnchantedStuff)
                            {
                                enchantmentFactor = item.EnchantedStuff.enchantmentBonusMultiplier;

                                float arcalleumFactor = item.EnchantedStuff.arcalleumCooldownPerMass;
                                float apparelWeight = apparel[i].def.GetStatValueAbstract(StatDefOf.Mass, apparel[i].Stuff);
                                if (apparel[i].Stuff.defName == "TM_Arcalleum")
                                {
                                    _arcaneRes += .05f;
                                }
                                _arcalleumCooldown += (apparelWeight * (arcalleumFactor / 100));

                            }
                            _maxMP += item.maxMP * enchantmentFactor;
                            _mpRegenRate += item.mpRegenRate * enchantmentFactor;
                            _coolDown += item.coolDown * enchantmentFactor;
                            _xpGain += item.xpGain * enchantmentFactor;
                            _mpCost += item.mpCost * enchantmentFactor;
                            _arcaneRes += item.arcaneRes * enchantmentFactor;
                            _arcaneDmg += item.arcaneDmg * enchantmentFactor;

                            if (item.arcaneSpectre == true)
                            {
                                _arcaneSpectre = true;
                            }
                            if (item.phantomShift == true)
                            {
                                _phantomShift = true;
                            }
                        }
                    }
                }
            }
            if (this.Pawn.equipment != null && this.Pawn.equipment.Primary != null)
            {
                Enchantment.CompEnchantedItem item = this.Pawn.equipment.Primary.GetComp<Enchantment.CompEnchantedItem>();
                if (item != null)
                {
                    if (item.HasEnchantment)
                    {
                        float enchantmentFactor = 1f;
                        if (item.MadeFromEnchantedStuff)
                        {
                            Enchantment.CompProperties_EnchantedStuff compES = Pawn.equipment.Primary.Stuff.GetCompProperties<Enchantment.CompProperties_EnchantedStuff>();
                            enchantmentFactor = compES.enchantmentBonusMultiplier;

                            float arcalleumFactor = compES.arcalleumCooldownPerMass;
                            if (Pawn.equipment.Primary.Stuff.defName == "TM_Arcalleum")
                            {
                                _arcaneDmg += .1f;
                            }
                            _arcalleumCooldown += (this.Pawn.equipment.Primary.def.GetStatValueAbstract(StatDefOf.Mass, this.Pawn.equipment.Primary.Stuff) * (arcalleumFactor / 100f));

                        }
                        else
                        {
                            _maxMP += item.maxMP * enchantmentFactor;
                            _mpRegenRate += item.mpRegenRate * enchantmentFactor;
                            _coolDown += item.coolDown * enchantmentFactor;
                            _xpGain += item.xpGain * enchantmentFactor;
                            _mpCost += item.mpCost * enchantmentFactor;
                            _arcaneRes += item.arcaneRes * enchantmentFactor;
                            _arcaneDmg += item.arcaneDmg * enchantmentFactor;
                        }
                    }
                }
                if (this.Pawn.equipment.Primary.def.defName == "TM_DefenderStaff")
                {
                    if (this.item_StaffOfDefender == false)
                    {
                        this.AddPawnAbility(TorannMagicDefOf.TM_ArcaneBarrier);
                        this.item_StaffOfDefender = true;
                    }
                }
                else
                {
                    if (this.item_StaffOfDefender == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_ArcaneBarrier);
                        this.item_StaffOfDefender = false;
                    }
                }
            }
            CleanupSummonedStructures();

            //Determine active or sustained hediffs and abilities
            if(this.SoL != null)
            {
                _maxMPUpkeep += (TorannMagicDefOf.TM_SpiritOfLight.upkeepEnergyCost * (1 - (TorannMagicDefOf.TM_SpiritOfLight.upkeepEfficiencyPercent * this.MagicData.MagicPowerSkill_SpiritOfLight.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SpiritOfLight_eff").level)));
                _mpRegenRateUpkeep += (TorannMagicDefOf.TM_SpiritOfLight.upkeepRegenCost * (1 - (TorannMagicDefOf.TM_SpiritOfLight.upkeepEfficiencyPercent * this.MagicData.MagicPowerSkill_SpiritOfLight.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SpiritOfLight_eff").level)));
            }
            if (this.summonedLights.Count > 0)
            {
                _maxMPUpkeep += (this.summonedLights.Count * TorannMagicDefOf.TM_Sunlight.upkeepEnergyCost);
                _mpRegenRateUpkeep += (this.summonedLights.Count * TorannMagicDefOf.TM_Sunlight.upkeepRegenCost);
            }
            if (this.summonedHeaters.Count > 0)
            {
                _maxMPUpkeep += (this.summonedHeaters.Count * TorannMagicDefOf.TM_Heater.upkeepEnergyCost);
            }
            if (this.summonedCoolers.Count > 0)
            {
                _maxMPUpkeep += (this.summonedCoolers.Count * TorannMagicDefOf.TM_Cooler.upkeepEnergyCost);
            }
            if (this.summonedPowerNodes.Count > 0)
            {
                _maxMPUpkeep += (this.summonedPowerNodes.Count * TorannMagicDefOf.TM_PowerNode.upkeepEnergyCost);
                _mpRegenRateUpkeep += (this.summonedPowerNodes.Count * TorannMagicDefOf.TM_PowerNode.upkeepRegenCost);
            }
            if (this.weaponEnchants.Count > 0)
            {
                _maxMPUpkeep += (this.weaponEnchants.Count * ActualManaCost(TorannMagicDefOf.TM_EnchantWeapon));
            }
            if (this.stoneskinPawns != null && this.stoneskinPawns.Count > 0)
            {
                _maxMPUpkeep += (this.stoneskinPawns.Count * (TorannMagicDefOf.TM_Stoneskin.upkeepEnergyCost - (.02f * this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Stoneskin).level)));
            }
            if (this.summonedSentinels != null && this.summonedSentinels.Count > 0)
            {
                MagicPowerSkill heartofstone = this.MagicData.MagicPowerSkill_Sentinel.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Sentinel_eff");

                if (heartofstone.level == 3)
                {
                    _maxMPUpkeep += (.15f * this.summonedSentinels.Count);
                }
                else
                {
                    _maxMPUpkeep += ((.2f - (.02f * heartofstone.level)) * this.summonedSentinels.Count);
                }
            }
            if(this.BrandPawns != null && this.BrandPawns.Count > 0)
            {
                float brandCost = this.BrandPawns.Count * (TorannMagicDefOf.TM_Branding.upkeepRegenCost * (1f - (TorannMagicDefOf.TM_Branding.upkeepEfficiencyPercent * this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Branding).level)));
                if(sigilSurging)
                {
                    brandCost *= (5f * (1f - (.1f * this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_SigilSurge).level)));
                }
                if(sigilDraining)
                {
                    brandCost *= (1.5f * (1f - (.2f * this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_SigilDrain).level)));
                }
                _mpRegenRateUpkeep += brandCost; 
            }
            if(this.livingWall != null && livingWall.Spawned)
            {
                _maxMPUpkeep += (TorannMagicDefOf.TM_LivingWall.upkeepEnergyCost * (1f - (TorannMagicDefOf.TM_LivingWall.upkeepEfficiencyPercent * this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_LivingWall).level)));
            }
            //Bonded spirit animal
            if (this.bondedSpirit != null)
            {
                _maxMPUpkeep += (TorannMagicDefOf.TM_GuardianSpirit.upkeepEnergyCost * (1f - (TorannMagicDefOf.TM_GuardianSpirit.upkeepEfficiencyPercent * this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_GuardianSpirit).level)));
                _mpRegenRateUpkeep += (TorannMagicDefOf.TM_GuardianSpirit.upkeepRegenCost * (1f - (TorannMagicDefOf.TM_GuardianSpirit.upkeepEfficiencyPercent * this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_GuardianSpirit).level)));
                if (this.bondedSpirit.Dead || this.bondedSpirit.Destroyed)
                {
                    this.bondedSpirit = null;
                }
                else if (this.bondedSpirit.Faction != null && this.bondedSpirit.Faction != this.Pawn.Faction)
                {
                    this.bondedSpirit = null;
                }
                else if (!this.bondedSpirit.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SpiritBondHD))
                {
                    HealthUtility.AdjustSeverity(this.bondedSpirit, TorannMagicDefOf.TM_SpiritBondHD, .5f);
                }
                if(TorannMagicDefOf.TM_SpiritCrowR == this.GuardianSpiritType)
                {
                    Hediff hd = this.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_CrowInsightHD);
                    if(hd != null && hd.Severity != (.5f + this.MagicData.GetSkill_Power(TorannMagicDefOf.TM_GuardianSpirit).level))
                    {
                        this.Pawn.health.RemoveHediff(hd);
                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_CrowInsightHD, .5f + this.MagicData.GetSkill_Power(TorannMagicDefOf.TM_GuardianSpirit).level);
                    }
                    else
                    {
                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_CrowInsightHD, .5f + this.MagicData.GetSkill_Power(TorannMagicDefOf.TM_GuardianSpirit).level);
                    }
                }
            }
            if (this.enchanterStones != null && this.enchanterStones.Count > 0)
            {
                for (int i = 0; i < this.enchanterStones.Count; i++)
                {
                    if (this.enchanterStones[i].DestroyedOrNull())
                    {
                        this.enchanterStones.Remove(this.enchanterStones[i]);
                    }
                }
                _maxMPUpkeep += (this.enchanterStones.Count * (TorannMagicDefOf.TM_EnchanterStone.upkeepEnergyCost * (1f - TorannMagicDefOf.TM_EnchanterStone.upkeepEfficiencyPercent * this.MagicData.MagicPowerSkill_EnchanterStone.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EnchanterStone_eff").level)));
            }
            try
            {
                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Druid) && this.fertileLands.Count > 0)
                {
                    _mpRegenRateUpkeep += TorannMagicDefOf.TM_FertileLands.upkeepRegenCost;
                }
            }
            catch
            {

            }
            if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Lich))
            {
                if (this.spell_LichForm == true || (this.customClass != null && this.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_LichForm).learned))
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_LichForm);
                    this.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_LichForm).learned = false;
                    this.spell_LichForm = false;
                }
                _maxMP += .5f;
                _mpRegenRate += .5f;
            }
            if (this.Pawn.Inspired && this.Pawn.Inspiration.def == TorannMagicDefOf.ID_ManaRegen)
            {
                _mpRegenRate += 1f;
            }
            if (this.recallSet)
            {
                _maxMPUpkeep += TorannMagicDefOf.TM_Recall.upkeepEnergyCost * (1 - (TorannMagicDefOf.TM_Recall.upkeepEfficiencyPercent * this.MagicData.MagicPowerSkill_Recall.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Recall_eff").level));
                _mpRegenRateUpkeep += TorannMagicDefOf.TM_Recall.upkeepRegenCost * (1 - (TorannMagicDefOf.TM_Recall.upkeepEfficiencyPercent * this.MagicData.MagicPowerSkill_Recall.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Recall_eff").level));
            }
            using (IEnumerator<Hediff> enumerator = this.Pawn.health.hediffSet.hediffs.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Hediff rec = enumerator.Current;
                    TMAbilityDef ability = this.MagicData.GetHediffAbility(rec);
                    if (ability != null)
                    {
                        MagicPowerSkill skill = this.MagicData.GetSkill_Efficiency(ability);
                        int level = 0;
                        if (skill != null)
                        {
                            level = skill.level;
                        }
                        if (ability == TorannMagicDefOf.TM_EnchantedAura || ability == TorannMagicDefOf.TM_EnchantedBody)
                        {
                            level = this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_EnchantedBody).level;
                        }

                        _maxMPUpkeep += (ability.upkeepEnergyCost * (1f - (ability.upkeepEfficiencyPercent * level)));

                        if (ability == TorannMagicDefOf.TM_EnchantedAura || ability == TorannMagicDefOf.TM_EnchantedBody)
                        {
                            level = this.MagicData.GetSkill_Versatility(TorannMagicDefOf.TM_EnchantedBody).level;
                        }
                        _mpRegenRateUpkeep += (ability.upkeepRegenCost * (1f - (ability.upkeepEfficiencyPercent * level)));
                    }
                    //else
                    //{
                    //    if (this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_EntertainingHD"), false))
                    //    {
                    //        _maxMPUpkeep += .3f;
                    //    }
                    //    if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PredictionHD, false))
                    //    {
                    //        _mpRegenRateUpkeep += .5f * (1 - (.10f * this.MagicData.MagicPowerSkill_Prediction.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Prediction_eff").level));
                    //    }
                    //    if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Shadow_AuraHD, false))
                    //    {
                    //        _maxMPUpkeep += .4f * (1 - (.08f * this.MagicData.MagicPowerSkill_Shadow.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Shadow_eff").level));
                    //        _mpRegenRateUpkeep += .3f * (1 - (.08f * this.MagicData.MagicPowerSkill_Shadow.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Shadow_eff").level));
                    //    }
                    //    if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_RayOfHope_AuraHD, false))
                    //    {
                    //        _maxMPUpkeep += .4f * (1 - (.08f * this.MagicData.MagicPowerSkill_RayofHope.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_RayofHope_eff").level));
                    //        _mpRegenRateUpkeep += .3f * (1 - (.08f * this.MagicData.MagicPowerSkill_RayofHope.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_RayofHope_eff").level));
                    //    }
                    //    if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SoothingBreeze_AuraHD, false))
                    //    {
                    //        _maxMPUpkeep += .4f * (1 - (.08f * this.MagicData.MagicPowerSkill_Soothe.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Soothe_eff").level));
                    //        _mpRegenRateUpkeep += .3f * (1 - (.08f * this.MagicData.MagicPowerSkill_Soothe.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Soothe_eff").level));
                    //    }
                    //    if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnchantedAuraHD) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnchantedBodyHD))
                    //    {
                    //        _maxMPUpkeep += .2f + (1f - (.04f * this.MagicData.MagicPowerSkill_EnchantedBody.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EnchantedBody_eff").level));
                    //        _mpRegenRateUpkeep += .4f + (1f - (.04f * this.MagicData.MagicPowerSkill_EnchantedBody.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EnchantedBody_ver").level));
                    //    }
                    //    if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BlurHD))
                    //    {
                    //        _maxMPUpkeep += .2f;
                    //    }
                    //    if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MageLightHD))
                    //    {
                    //        _maxMPUpkeep += .1f;
                    //        _mpRegenRateUpkeep += .05f;
                    //    }
                    //}
                }
            }

            if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SS_SerumHD))
            {
                Hediff def = this.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SS_SerumHD, false);
                _mpRegenRate -= (float)(.15f * def.CurStageIndex);
                _maxMP -= .25f;
                _arcaneRes += (float)(.15f * def.CurStageIndex);
                _arcaneDmg -= (float)(.1f * def.CurStageIndex);
            }

            //class and global modifiers
            _arcaneDmg += (.01f * this.MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_WandererCraft_pwr").level);
            _arcaneRes += (.02f * this.MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_WandererCraft_pwr").level);
            _mpCost -= (.01f * this.MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_WandererCraft_eff").level);
            _xpGain += (.02f * this.MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_WandererCraft_eff").level);
            _coolDown -= (.01f * this.MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_WandererCraft_ver").level);
            _mpRegenRate += (.01f * this.MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_WandererCraft_ver").level);
            _maxMP += (.02f * this.MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_WandererCraft_ver").level);

            _maxMP += (.04f * this.MagicData.MagicPowerSkill_global_spirit.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_global_spirit_pwr").level);
            _mpRegenRate += (.05f * this.MagicData.MagicPowerSkill_global_regen.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_global_regen_pwr").level);
            _mpCost -= (.025f * this.MagicData.MagicPowerSkill_global_eff.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_global_eff_pwr").level);
            _arcaneRes += ((1f - this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity, false)) / 2f);
            _arcaneDmg += ((this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity, false) - 1f) / 4f);

            if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_BoundlessTD))
            {
                this.arcalleumCooldown = Mathf.Clamp(0f + _arcalleumCooldown, 0f, .1f);
            }
            else
            {
                this.arcalleumCooldown = Mathf.Clamp(0f + _arcalleumCooldown, 0f, .5f);
            }

            float val = (1f - (.03f * this.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_eff").level));
            _maxMPUpkeep *= val;
            _mpRegenRateUpkeep *= val;

            //resolve upkeep costs
            _maxMP -= (_maxMPUpkeep);
            _mpRegenRate -= (_mpRegenRateUpkeep);

            //finalize
            this.maxMP = Mathf.Clamp(1f + _maxMP, 0f, 5f);
            this.mpRegenRate = 1f + _mpRegenRate;
            this.coolDown = Mathf.Clamp(1f + _coolDown, 0.25f, 10f);
            this.xpGain = Mathf.Clamp(1f + _xpGain, 0.01f, 5f);
            this.mpCost = Mathf.Clamp(1f + _mpCost, 0.1f, 5f);
            this.arcaneRes = Mathf.Clamp(1 + _arcaneRes, 0.01f, 5f);
            this.arcaneDmg = 1 + _arcaneDmg;

            if (this.IsMagicUser && !TM_Calc.IsCrossClass(this.Pawn, true))
            {
                if (this.maxMP != 1f && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_maxEnergy")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_maxEnergy"), .5f);
                }
                if (this.mpRegenRate != 1f && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_energyRegen")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_energyRegen"), .5f);
                }
                if (this.coolDown != 1f && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_coolDown")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_coolDown"), .5f);
                }
                if (this.xpGain != 1f && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_xpGain")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_xpGain"), .5f);
                }
                if (this.mpCost != 1f && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_energyCost")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_energyCost"), .5f);
                }
                if (this.arcaneRes != 1f && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_dmgResistance")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_dmgResistance"), .5f);
                }
                if (this.arcaneDmg != 1f && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_dmgBonus")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_dmgBonus"), .5f);
                }
                if(_arcalleumCooldown != 0 && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_arcalleumCooldown")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_arcalleumCooldown"), .5f);
                }
                if (_arcaneSpectre == true && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_arcaneSpectre")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_arcaneSpectre"), .5f);
                }
                else if(_arcaneSpectre == false && this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_arcaneSpectre")))
                {
                    this.Pawn.health.RemoveHediff(this.Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_HediffEnchantment_arcaneSpectre")));
                }
                if (_phantomShift == true)
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_phantomShift"), .5f);
                }
                else if (_phantomShift == false && this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_phantomShift")))
                {
                    this.Pawn.health.RemoveHediff(this.Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_HediffEnchantment_phantomShift")));
                }               
            }
        }

        private void CleanupSummonedStructures()
        {
            for (int i = 0; i < this.summonedLights.Count; i++)
            {
                if (this.summonedLights[i].DestroyedOrNull())
                {
                    this.summonedLights.Remove(this.summonedLights[i]);
                    i--;
                }
            }
            for (int i = 0; i < this.summonedHeaters.Count; i++)
            {
                if (this.summonedHeaters[i].DestroyedOrNull())
                {
                    this.summonedHeaters.Remove(this.summonedHeaters[i]);
                    i--;
                }
            }
            for (int i = 0; i < this.summonedCoolers.Count; i++)
            {
                if (this.summonedCoolers[i].DestroyedOrNull())
                {
                    this.summonedCoolers.Remove(this.summonedCoolers[i]);
                    i--;
                }
            }
            for (int i = 0; i < this.summonedPowerNodes.Count; i++)
            {
                if (this.summonedPowerNodes[i].DestroyedOrNull())
                {
                    this.summonedPowerNodes.Remove(this.summonedPowerNodes[i]);
                    i--;
                }
            }
            for (int i = 0; i < this.lightningTraps.Count; i++)
            {
                if (this.lightningTraps[i].DestroyedOrNull())
                {
                    this.lightningTraps.Remove(this.lightningTraps[i]);
                    i--;
                }
            }
        }

        public override void PostExposeData()
        {
            //base.PostExposeData();            
            Scribe_Values.Look<bool>(ref this.magicPowersInitialized, "magicPowersInitialized", false, false);
            Scribe_Values.Look<bool>(ref this.magicPowersInitializedForColonist, "magicPowersInitializedForColonist", true, false);
            Scribe_Values.Look<bool>(ref this.colonistPowerCheck, "colonistPowerCheck", true, false);
            Scribe_Values.Look<bool>(ref this.spell_Rain, "spell_Rain", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Blink, "spell_Blink", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Teleport, "spell_Teleport", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Heal, "spell_Heal", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Heater, "spell_Heater", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Cooler, "spell_Cooler", false, false);
            Scribe_Values.Look<bool>(ref this.spell_PowerNode, "spell_PowerNode", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Sunlight, "spell_Sunlight", false, false);
            Scribe_Values.Look<bool>(ref this.spell_DryGround, "spell_DryGround", false, false);
            Scribe_Values.Look<bool>(ref this.spell_WetGround, "spell_WetGround", false, false);
            Scribe_Values.Look<bool>(ref this.spell_ChargeBattery, "spell_ChargeBattery", false, false);
            Scribe_Values.Look<bool>(ref this.spell_SmokeCloud, "spell_SmokeCloud", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Extinguish, "spell_Extinguish", false, false);
            Scribe_Values.Look<bool>(ref this.spell_EMP, "spell_EMP", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Blizzard, "spell_Blizzard", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Firestorm, "spell_Firestorm", false, false);
            Scribe_Values.Look<bool>(ref this.spell_EyeOfTheStorm, "spell_EyeOfTheStorm", false, false);
            Scribe_Values.Look<bool>(ref this.spell_SummonMinion, "spell_SummonMinion", false, false);
            Scribe_Values.Look<bool>(ref this.spell_TransferMana, "spell_TransferMana", false, false);
            Scribe_Values.Look<bool>(ref this.spell_SiphonMana, "spell_SiphonMana", false, false);
            Scribe_Values.Look<bool>(ref this.spell_RegrowLimb, "spell_RegrowLimb", false, false);
            Scribe_Values.Look<bool>(ref this.spell_ManaShield, "spell_ManaShield", false, false);
            Scribe_Values.Look<bool>(ref this.spell_FoldReality, "spell_FoldReality", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Resurrection, "spell_Resurrection", false, false);
            Scribe_Values.Look<bool>(ref this.spell_HolyWrath, "spell_HolyWrath", false, false);
            Scribe_Values.Look<bool>(ref this.spell_LichForm, "spell_LichForm", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Flight, "spell_Flight", false, false);
            Scribe_Values.Look<bool>(ref this.spell_SummonPoppi, "spell_SummonPoppi", false, false);
            Scribe_Values.Look<bool>(ref this.spell_BattleHymn, "spell_BattleHymn", false, false);
            Scribe_Values.Look<bool>(ref this.spell_FertileLands, "spell_FertileLands", false, false);
            Scribe_Values.Look<bool>(ref this.spell_CauterizeWound, "spell_CauterizeWound", false, false);
            Scribe_Values.Look<bool>(ref this.spell_SpellMending, "spell_SpellMending", false, false);
            Scribe_Values.Look<bool>(ref this.spell_PsychicShock, "spell_PsychicShock", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Scorn, "spell_Scorn", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Meteor, "spell_Meteor", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Teach, "spell_Teach", false, false);
            Scribe_Values.Look<bool>(ref this.spell_OrbitalStrike, "spell_OrbitalStrike", false, false);
            Scribe_Values.Look<bool>(ref this.spell_BloodMoon, "spell_BloodMoon", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Shapeshift, "spell_Shapeshift", false, false);
            Scribe_Values.Look<bool>(ref this.spell_ShapeshiftDW, "spell_ShapeshiftDW", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Blur, "spell_Blur", false, false);
            Scribe_Values.Look<bool>(ref this.spell_BlankMind, "spell_BlankMind", false, false);
            Scribe_Values.Look<bool>(ref this.spell_DirtDevil, "spell_DirtDevil", false, false);
            Scribe_Values.Look<bool>(ref this.spell_ArcaneBolt, "spell_ArcaneBolt", false, false);
            Scribe_Values.Look<bool>(ref this.spell_LightningTrap, "spell_LightningTrap", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Invisibility, "spell_Invisibility", false, false);
            Scribe_Values.Look<bool>(ref this.spell_BriarPatch, "spell_BriarPatch", false, false);
            Scribe_Values.Look<bool>(ref this.spell_MechaniteReprogramming, "spell_MechaniteReprogramming", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Recall, "spell_Recall", false, false);
            Scribe_Values.Look<bool>(ref this.spell_MageLight, "spell_MageLight", false, false);
            Scribe_Values.Look<bool>(ref this.spell_SnapFreeze, "spell_SnapFreeze", false, false);
            Scribe_Values.Look<bool>(ref this.spell_Ignite, "spell_Ignite", false, false);
            Scribe_Values.Look<bool>(ref this.spell_HeatShield, "spell_HeatShield", false, false);
            Scribe_Values.Look<bool>(ref this.useTechnoBitToggle, "useTechnoBitToggle", true, false);
            Scribe_Values.Look<bool>(ref this.useTechnoBitRepairToggle, "useTechnoBitRepairToggle", true, false);
            Scribe_Values.Look<bool>(ref this.useElementalShotToggle, "useElementalShotToggle", true, false);
            Scribe_Values.Look<int>(ref this.powerModifier, "powerModifier", 0, false);
            Scribe_Values.Look<int>(ref this.technoWeaponDefNum, "technoWeaponDefNum");
            Scribe_Values.Look<bool>(ref this.doOnce, "doOnce", true, false);
            Scribe_Values.Look<int>(ref this.predictionTick, "predictionTick", 0, false);
            Scribe_Values.Look<int>(ref this.predictionHash, "predictionHash", 0, false);
            Scribe_References.Look<Thing>(ref this.mageLightThing, "mageLightThing", false);
            Scribe_Values.Look<bool>(ref this.mageLightActive, "mageLightActive", false, false);
            Scribe_Values.Look<bool>(ref this.mageLightSet, "mageLightSet", false, false);
            Scribe_Values.Look<bool>(ref this.deathRetaliating, "deathRetaliating", false, false);
            Scribe_Values.Look<bool>(ref this.canDeathRetaliate, "canDeathRetaliate", false, false);
            Scribe_Values.Look<int>(ref this.ticksTillRetaliation, "ticksTillRetaliation", 600, false);
            Scribe_Defs.Look<IncidentDef>(ref this.predictionIncidentDef, "predictionIncidentDef");
            Scribe_References.Look<Pawn>(ref this.soulBondPawn, "soulBondPawn", false);
            //Scribe_References.Look<Thing>(ref this.technoWeaponThing, "technoWeaponThing", false);
            Scribe_Defs.Look<ThingDef>(ref this.technoWeaponThingDef, "technoWeaponThingDef");
            Scribe_Values.Look<QualityCategory>(ref this.technoWeaponQC, "technoWeaponQC");
            Scribe_References.Look<Thing>(ref this.enchanterStone, "enchanterStone", false);
            Scribe_Collections.Look<Thing>(ref this.enchanterStones, "enchanterStones", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref this.summonedMinions, "summonedMinions", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref this.supportedUndead, "supportedUndead", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref this.summonedLights, "summonedLights", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref this.summonedPowerNodes, "summonedPowerNodes", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref this.summonedCoolers, "summonedCoolers", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref this.summonedHeaters, "summonedHeaters", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref this.summonedSentinels, "summonedSentinels", LookMode.Reference);
            Scribe_Collections.Look<Pawn>(ref this.stoneskinPawns, "stoneskinPawns", LookMode.Reference);
            Scribe_Collections.Look<Pawn>(ref this.weaponEnchants, "weaponEnchants", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref this.lightningTraps, "lightningTraps", LookMode.Reference);
            Scribe_Collections.Look<Pawn>(ref this.hexedPawns, "hexedPawns", LookMode.Reference);
            Scribe_Values.Look<IntVec3>(ref this.earthSprites, "earthSprites", default(IntVec3), false);
            Scribe_Values.Look<int>(ref this.earthSpriteType, "earthSpriteType", 0, false);
            Scribe_References.Look<Map>(ref this.earthSpriteMap, "earthSpriteMap", false);
            Scribe_Values.Look<bool>(ref this.earthSpritesInArea, "earthSpritesInArea", false, false);
            Scribe_Values.Look<int>(ref this.nextEarthSpriteAction, "nextEarthSpriteAction", 0, false);
            Scribe_Collections.Look<IntVec3>(ref this.fertileLands, "fertileLands", LookMode.Value);
            Scribe_Values.Look<float>(ref this.maxMP, "maxMP", 1f, false);
            Scribe_Values.Look<int>(ref this.lastChaosTraditionTick, "lastChaosTraditionTick", 0);
            //Scribe_Collections.Look<TM_ChaosPowers>(ref this.chaosPowers, "chaosPowers", LookMode.Deep, new object[0]);
            //Recall variables 
            Scribe_Values.Look<bool>(ref this.recallSet, "recallSet", false, false);
            Scribe_Values.Look<bool>(ref this.recallSpell, "recallSpell", false, false);
            Scribe_Values.Look<int>(ref this.recallExpiration, "recallExpiration", 0, false);
            Scribe_Values.Look<IntVec3>(ref this.recallPosition, "recallPosition", default(IntVec3), false);
            Scribe_References.Look<Map>(ref this.recallMap, "recallMap", false);
            Scribe_Collections.Look<string>(ref this.recallNeedDefnames, "recallNeedDefnames", LookMode.Value);
            Scribe_Collections.Look<float>(ref this.recallNeedValues, "recallNeedValues", LookMode.Value);
            Scribe_Collections.Look<Hediff>(ref this.recallHediffList, "recallHediffList", LookMode.Deep);
            Scribe_Collections.Look<float>(ref this.recallHediffDefSeverityList, "recallHediffSeverityList", LookMode.Value);
            Scribe_Collections.Look<int>(ref this.recallHediffDefTicksRemainingList, "recallHediffDefTicksRemainingList", LookMode.Value);
            Scribe_Collections.Look<Hediff_Injury>(ref this.recallInjuriesList, "recallInjuriesList", LookMode.Deep);
            Scribe_References.Look<FlyingObject_SpiritOfLight>(ref SoL, "SoL", false);
            Scribe_Defs.Look<ThingDef>(ref this.guardianSpiritType, "guardianSpiritType");
            Scribe_References.Look<Pawn>(ref this.bondedSpirit, "bondedSpirit", false);
            Scribe_Collections.Look<Pawn>(ref this.brands, "brands", LookMode.Reference);
            Scribe_Collections.Look<HediffDef>(ref this.brandDefs, "brandDefs", LookMode.Def);
            Scribe_Values.Look<bool>(ref this.sigilSurging, "sigilSurging", false, false);
            Scribe_Values.Look<bool>(ref this.sigilDraining, "sigilDraining", false, false);
            Scribe_References.Look<FlyingObject_LivingWall>(ref this.livingWall, "livingWall");
            Scribe_Deep.Look(ref this.magicWardrobe, "magicWardrobe");
            Scribe_Deep.Look<MagicData>(ref this.magicData, "magicData", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                TM_PawnTracker.ResolveMagicComp(this);
                Pawn abilityUser = base.Pawn;
                int index = TM_ClassUtility.CustomClassIndexOfBaseMageClass(abilityUser.story.traits.allTraits);
                if (index >= 0)
                {                   
                    this.customClass = TM_ClassUtility.CustomClasses[index];
                    this.customIndex = index;
                    LoadCustomClassAbilities(this.customClass);                    
                }                
                else
                {
                    bool flagCM = abilityUser.story.traits.HasTrait(TorannMagicDefOf.ChaosMage);
                    bool flag40 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.InnerFire) || flagCM;
                    if (flag40)
                    {
                        bool flag14 = !this.MagicData.MagicPowersIF.NullOrEmpty<MagicPower>();
                        if (flag14)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current3 in this.MagicData.MagicPowersIF)
                            {
                                bool flag15 = current3.abilityDef != null;
                                if (flag15)
                                {
                                    if (current3.learned == true && (current3.abilityDef == TorannMagicDefOf.TM_RayofHope || current3.abilityDef == TorannMagicDefOf.TM_RayofHope_I || current3.abilityDef == TorannMagicDefOf.TM_RayofHope_II || current3.abilityDef == TorannMagicDefOf.TM_RayofHope_III))
                                    {
                                        if (current3.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_RayofHope);
                                        }
                                        else if (current3.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_RayofHope_I);
                                        }
                                        else if (current3.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_RayofHope_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_RayofHope_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag41 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost) || flagCM;
                    if (flag41)
                    {
                        bool flag17 = !this.MagicData.MagicPowersHoF.NullOrEmpty<MagicPower>();
                        if (flag17)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current4 in this.MagicData.MagicPowersHoF)
                            {
                                bool flag18 = current4.abilityDef != null;
                                if (flag18)
                                {
                                    if (current4.learned == true && (current4.abilityDef == TorannMagicDefOf.TM_Soothe || current4.abilityDef == TorannMagicDefOf.TM_Soothe_I || current4.abilityDef == TorannMagicDefOf.TM_Soothe_II || current4.abilityDef == TorannMagicDefOf.TM_Soothe_III))
                                    {
                                        if (current4.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Soothe);
                                        }
                                        else if (current4.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Soothe_I);
                                        }
                                        else if (current4.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Soothe_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Soothe_III);
                                        }
                                    }
                                    if (current4.learned == true && (current4.abilityDef == TorannMagicDefOf.TM_FrostRay || current4.abilityDef == TorannMagicDefOf.TM_FrostRay_I || current4.abilityDef == TorannMagicDefOf.TM_FrostRay_II || current4.abilityDef == TorannMagicDefOf.TM_FrostRay_III))
                                    {
                                        if (current4.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_FrostRay);
                                        }
                                        else if (current4.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_FrostRay_I);
                                        }
                                        else if (current4.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_FrostRay_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_FrostRay_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag42 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.StormBorn) || flagCM;
                    if (flag42)
                    {
                        bool flag20 = !this.MagicData.MagicPowersSB.NullOrEmpty<MagicPower>();
                        if (flag20)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current5 in this.MagicData.MagicPowersSB)
                            {
                                bool flag21 = current5.abilityDef != null;
                                if (current5.learned == true && (current5.abilityDef == TorannMagicDefOf.TM_AMP || current5.abilityDef == TorannMagicDefOf.TM_AMP_I || current5.abilityDef == TorannMagicDefOf.TM_AMP_II || current5.abilityDef == TorannMagicDefOf.TM_AMP_III))
                                {
                                    if (current5.level == 0)
                                    {
                                        base.AddPawnAbility(TorannMagicDefOf.TM_AMP);
                                    }
                                    else if (current5.level == 1)
                                    {
                                        base.AddPawnAbility(TorannMagicDefOf.TM_AMP_I);
                                    }
                                    else if (current5.level == 2)
                                    {
                                        base.AddPawnAbility(TorannMagicDefOf.TM_AMP_II);
                                    }
                                    else
                                    {
                                        base.AddPawnAbility(TorannMagicDefOf.TM_AMP_III);
                                    }
                                }
                            }
                        }
                    }
                    bool flag43 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Arcanist) || flagCM;
                    if (flag43)
                    {
                        bool flag23 = !this.MagicData.MagicPowersA.NullOrEmpty<MagicPower>();
                        if (flag23)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current6 in this.MagicData.MagicPowersA)
                            {
                                bool flag24 = current6.abilityDef != null;
                                if (flag24)
                                {
                                    if (current6.learned == true && (current6.abilityDef == TorannMagicDefOf.TM_Shadow || current6.abilityDef == TorannMagicDefOf.TM_Shadow_I || current6.abilityDef == TorannMagicDefOf.TM_Shadow_II || current6.abilityDef == TorannMagicDefOf.TM_Shadow_III))
                                    {
                                        if (current6.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Shadow);
                                        }
                                        else if (current6.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Shadow_I);
                                        }
                                        else if (current6.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Shadow_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Shadow_III);
                                        }
                                    }
                                    if (current6.learned == true && (current6.abilityDef == TorannMagicDefOf.TM_MagicMissile || current6.abilityDef == TorannMagicDefOf.TM_MagicMissile_I || current6.abilityDef == TorannMagicDefOf.TM_MagicMissile_II || current6.abilityDef == TorannMagicDefOf.TM_MagicMissile_III))
                                    {
                                        if (current6.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_MagicMissile);
                                        }
                                        else if (current6.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_MagicMissile_I);
                                        }
                                        else if (current6.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_MagicMissile_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_MagicMissile_III);
                                        }
                                    }
                                    if (current6.learned == true && (current6.abilityDef == TorannMagicDefOf.TM_Blink || current6.abilityDef == TorannMagicDefOf.TM_Blink_I || current6.abilityDef == TorannMagicDefOf.TM_Blink_II || current6.abilityDef == TorannMagicDefOf.TM_Blink_III))
                                    {
                                        if (current6.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Blink);
                                        }
                                        else if (current6.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Blink_I);
                                        }
                                        else if (current6.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Blink_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Blink_III);
                                        }
                                    }
                                    if (current6.learned == true && (current6.abilityDef == TorannMagicDefOf.TM_Summon || current6.abilityDef == TorannMagicDefOf.TM_Summon_I || current6.abilityDef == TorannMagicDefOf.TM_Summon_II || current6.abilityDef == TorannMagicDefOf.TM_Summon_III))
                                    {
                                        if (current6.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Summon);
                                        }
                                        else if (current6.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Summon_I);
                                        }
                                        else if (current6.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Summon_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Summon_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag44 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Paladin) || flagCM;
                    if (flag44)
                    {
                        bool flag26 = !this.MagicData.MagicPowersP.NullOrEmpty<MagicPower>();
                        if (flag26)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current7 in this.MagicData.MagicPowersP)
                            {
                                bool flag27 = current7.abilityDef != null;
                                if (flag27)
                                {
                                    if (current7.learned == true && (current7.abilityDef == TorannMagicDefOf.TM_Shield || current7.abilityDef == TorannMagicDefOf.TM_Shield_I || current7.abilityDef == TorannMagicDefOf.TM_Shield_II || current7.abilityDef == TorannMagicDefOf.TM_Shield_III))
                                    {
                                        if (current7.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Shield);
                                        }
                                        else if (current7.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Shield_I);
                                        }
                                        else if (current7.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Shield_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Shield_III);
                                        }
                                    }
                                    if (current7.learned == true && (current7.abilityDef == TorannMagicDefOf.TM_P_RayofHope || current7.abilityDef == TorannMagicDefOf.TM_P_RayofHope_I || current7.abilityDef == TorannMagicDefOf.TM_P_RayofHope_II || current7.abilityDef == TorannMagicDefOf.TM_P_RayofHope_III))
                                    {
                                        if (current7.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_P_RayofHope);
                                        }
                                        else if (current7.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_P_RayofHope_I);
                                        }
                                        else if (current7.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_P_RayofHope_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_P_RayofHope_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag45 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Summoner) || flagCM;
                    if (flag45)
                    {
                        bool flag28 = !this.MagicData.MagicPowersS.NullOrEmpty<MagicPower>();
                        if (flag28)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current8 in this.MagicData.MagicPowersS)
                            {
                                bool flag29 = current8.abilityDef != null;
                                if (flag29)
                                {
                                    //if ((current7.abilityDef == TorannMagicDefOf.TM_Shield || current7.abilityDef == TorannMagicDefOf.TM_Shield_I || current7.abilityDef == TorannMagicDefOf.TM_Shield_II || current7.abilityDef == TorannMagicDefOf.TM_Shield_III))
                                    //{
                                    //    if (current7.level == 0)
                                    //    {
                                    //        base.AddPawnAbility(TorannMagicDefOf.TM_Shield);
                                    //    }
                                    //    else if (current7.level == 1)
                                    //    {
                                    //        base.AddPawnAbility(TorannMagicDefOf.TM_Shield_I);
                                    //    }
                                    //    else if (current7.level == 2)
                                    //    {
                                    //        base.AddPawnAbility(TorannMagicDefOf.TM_Shield_II);
                                    //    }
                                    //    else
                                    //    {
                                    //        base.AddPawnAbility(TorannMagicDefOf.TM_Shield_III);
                                    //    }
                                    //}
                                }
                            }
                        }
                    }
                    bool flag46 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Druid) || flagCM;
                    if (flag46)
                    {
                        bool flag30 = !this.MagicData.MagicPowersD.NullOrEmpty<MagicPower>();
                        if (flag30)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current9 in this.MagicData.MagicPowersD)
                            {
                                bool flag31 = current9.abilityDef != null;
                                if (flag31)
                                {
                                    if (current9.learned == true && (current9.abilityDef == TorannMagicDefOf.TM_SootheAnimal || current9.abilityDef == TorannMagicDefOf.TM_SootheAnimal_I || current9.abilityDef == TorannMagicDefOf.TM_SootheAnimal_II || current9.abilityDef == TorannMagicDefOf.TM_SootheAnimal_III))
                                    {
                                        if (current9.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_SootheAnimal);
                                        }
                                        else if (current9.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_SootheAnimal_I);
                                        }
                                        else if (current9.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_SootheAnimal_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_SootheAnimal_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag47 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || abilityUser.story.traits.HasTrait(TorannMagicDefOf.Lich) || flagCM;
                    if (flag47)
                    {
                        bool flag32 = !this.MagicData.MagicPowersN.NullOrEmpty<MagicPower>();
                        if (flag32)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current10 in this.MagicData.MagicPowersN)
                            {
                                bool flag33 = current10.abilityDef != null;
                                if (flag33)
                                {
                                    if (current10.learned == true && (current10.abilityDef == TorannMagicDefOf.TM_DeathMark || current10.abilityDef == TorannMagicDefOf.TM_DeathMark_I || current10.abilityDef == TorannMagicDefOf.TM_DeathMark_II || current10.abilityDef == TorannMagicDefOf.TM_DeathMark_III))
                                    {
                                        if (current10.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_DeathMark);
                                        }
                                        else if (current10.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_DeathMark_I);
                                        }
                                        else if (current10.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_DeathMark_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_DeathMark_III);
                                        }
                                    }
                                    if (current10.learned == true && (current10.abilityDef == TorannMagicDefOf.TM_ConsumeCorpse || current10.abilityDef == TorannMagicDefOf.TM_ConsumeCorpse_I || current10.abilityDef == TorannMagicDefOf.TM_ConsumeCorpse_II || current10.abilityDef == TorannMagicDefOf.TM_ConsumeCorpse_III))
                                    {
                                        if (current10.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ConsumeCorpse);
                                        }
                                        else if (current10.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ConsumeCorpse_I);
                                        }
                                        else if (current10.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ConsumeCorpse_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ConsumeCorpse_III);
                                        }
                                    }
                                    if (current10.learned == true && (current10.abilityDef == TorannMagicDefOf.TM_CorpseExplosion || current10.abilityDef == TorannMagicDefOf.TM_CorpseExplosion_I || current10.abilityDef == TorannMagicDefOf.TM_CorpseExplosion_II || current10.abilityDef == TorannMagicDefOf.TM_CorpseExplosion_III))
                                    {
                                        if (current10.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_CorpseExplosion);
                                        }
                                        else if (current10.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_CorpseExplosion_I);
                                        }
                                        else if (current10.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_CorpseExplosion_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_CorpseExplosion_III);
                                        }
                                    }
                                    if (abilityUser.story.traits.HasTrait(TorannMagicDefOf.Lich) && (current10.learned == true && (current10.abilityDef == TorannMagicDefOf.TM_DeathBolt || current10.abilityDef == TorannMagicDefOf.TM_DeathBolt_I || current10.abilityDef == TorannMagicDefOf.TM_DeathBolt_II || current10.abilityDef == TorannMagicDefOf.TM_DeathBolt_III)))
                                    {
                                        if (current10.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_DeathBolt);
                                        }
                                        else if (current10.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_DeathBolt_I);
                                        }
                                        else if (current10.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_DeathBolt_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_DeathBolt_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag48 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Priest) || flagCM;
                    if (flag48)
                    {
                        bool flag34 = !this.MagicData.MagicPowersPR.NullOrEmpty<MagicPower>();
                        if (flag34)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current11 in this.MagicData.MagicPowersPR)
                            {
                                bool flag33 = current11.abilityDef != null;
                                if (flag33)
                                {
                                    if (current11.learned == true && (current11.abilityDef == TorannMagicDefOf.TM_HealingCircle || current11.abilityDef == TorannMagicDefOf.TM_HealingCircle_I || current11.abilityDef == TorannMagicDefOf.TM_HealingCircle_II || current11.abilityDef == TorannMagicDefOf.TM_HealingCircle_III))
                                    {
                                        if (current11.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_HealingCircle);
                                        }
                                        else if (current11.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_HealingCircle_I);
                                        }
                                        else if (current11.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_HealingCircle_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_HealingCircle_III);
                                        }
                                    }
                                    if (current11.learned == true && (current11.abilityDef == TorannMagicDefOf.TM_BestowMight || current11.abilityDef == TorannMagicDefOf.TM_BestowMight_I || current11.abilityDef == TorannMagicDefOf.TM_BestowMight_II || current11.abilityDef == TorannMagicDefOf.TM_BestowMight_III))
                                    {
                                        if (current11.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_BestowMight);
                                        }
                                        else if (current11.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_BestowMight_I);
                                        }
                                        else if (current11.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_BestowMight_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_BestowMight_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag49 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Bard) || flagCM;
                    if (flag49)
                    {
                        bool flag35 = !this.MagicData.MagicPowersB.NullOrEmpty<MagicPower>();
                        if (flag35)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current12 in this.MagicData.MagicPowersB)
                            {
                                bool flag36 = current12.abilityDef != null;
                                if (flag36)
                                {
                                    if (current12.learned == true && (current12.abilityDef == TorannMagicDefOf.TM_Lullaby || current12.abilityDef == TorannMagicDefOf.TM_Lullaby_I || current12.abilityDef == TorannMagicDefOf.TM_Lullaby_II || current12.abilityDef == TorannMagicDefOf.TM_Lullaby_III))
                                    {
                                        if (current12.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Lullaby);
                                        }
                                        else if (current12.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Lullaby_I);
                                        }
                                        else if (current12.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Lullaby_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Lullaby_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag50 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Succubus) || flagCM;
                    if (flag50)
                    {
                        bool flag37 = !this.MagicData.MagicPowersSD.NullOrEmpty<MagicPower>();
                        if (flag37)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current13 in this.MagicData.MagicPowersSD)
                            {
                                bool flag38 = current13.abilityDef != null;
                                if (flag38)
                                {
                                    if (current13.learned == true && (current13.abilityDef == TorannMagicDefOf.TM_ShadowBolt || current13.abilityDef == TorannMagicDefOf.TM_ShadowBolt_I || current13.abilityDef == TorannMagicDefOf.TM_ShadowBolt_II || current13.abilityDef == TorannMagicDefOf.TM_ShadowBolt_III))
                                    {
                                        if (current13.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt);
                                        }
                                        else if (current13.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt_I);
                                        }
                                        else if (current13.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt_III);
                                        }
                                    }
                                    if (current13.learned == true && (current13.abilityDef == TorannMagicDefOf.TM_Attraction || current13.abilityDef == TorannMagicDefOf.TM_Attraction_I || current13.abilityDef == TorannMagicDefOf.TM_Attraction_II || current13.abilityDef == TorannMagicDefOf.TM_Attraction_III))
                                    {
                                        if (current13.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Attraction);
                                        }
                                        else if (current13.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Attraction_I);
                                        }
                                        else if (current13.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Attraction_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Attraction_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag51 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Warlock) || flagCM;
                    if (flag51)
                    {
                        bool flagWD1 = !this.MagicData.MagicPowersWD.NullOrEmpty<MagicPower>();
                        if (flagWD1)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current14 in this.MagicData.MagicPowersWD)
                            {
                                bool flagWD2 = current14.abilityDef != null;
                                if (flagWD2)
                                {
                                    if (current14.learned == true && (current14.abilityDef == TorannMagicDefOf.TM_ShadowBolt || current14.abilityDef == TorannMagicDefOf.TM_ShadowBolt_I || current14.abilityDef == TorannMagicDefOf.TM_ShadowBolt_II || current14.abilityDef == TorannMagicDefOf.TM_ShadowBolt_III))
                                    {
                                        if (current14.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt);
                                        }
                                        else if (current14.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt_I);
                                        }
                                        else if (current14.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt_III);
                                        }
                                    }
                                    if (current14.learned == true && (current14.abilityDef == TorannMagicDefOf.TM_Repulsion || current14.abilityDef == TorannMagicDefOf.TM_Repulsion_I || current14.abilityDef == TorannMagicDefOf.TM_Repulsion_II || current14.abilityDef == TorannMagicDefOf.TM_Repulsion_III))
                                    {
                                        if (current14.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Repulsion);
                                        }
                                        else if (current14.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Repulsion_I);
                                        }
                                        else if (current14.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Repulsion_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Repulsion_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag52 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Geomancer) || flagCM;
                    if (flag52)
                    {
                        bool flagG = !this.MagicData.MagicPowersG.NullOrEmpty<MagicPower>();
                        if (flagG)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current15 in this.MagicData.MagicPowersG)
                            {
                                bool flagWD2 = current15.abilityDef != null;
                                if (flagWD2)
                                {
                                    if (current15.learned == true && (current15.abilityDef == TorannMagicDefOf.TM_Encase || current15.abilityDef == TorannMagicDefOf.TM_Encase_I || current15.abilityDef == TorannMagicDefOf.TM_Encase_II || current15.abilityDef == TorannMagicDefOf.TM_Encase_III))
                                    {
                                        if (current15.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Encase);
                                        }
                                        else if (current15.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Encase_I);
                                        }
                                        else if (current15.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Encase_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Encase_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag53 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || flagCM;
                    if (flag53)
                    {
                        bool flagT = !this.MagicData.MagicPowersT.NullOrEmpty<MagicPower>();
                        if (flagT)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current16 in this.MagicData.MagicPowersT)
                            {
                                bool flagT2 = current16.abilityDef != null;
                                if (flagT2)
                                {

                                }
                            }
                        }
                    }
                    bool flag54 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.BloodMage);
                    if (flag54)
                    {
                        bool flagBM = !this.MagicData.MagicPowersBM.NullOrEmpty<MagicPower>();
                        if (flagBM)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current16 in this.MagicData.MagicPowersBM)
                            {
                                bool flagBM2 = current16.abilityDef != null;
                                if (flagBM2)
                                {
                                    if (current16.learned == true && (current16.abilityDef == TorannMagicDefOf.TM_Rend || current16.abilityDef == TorannMagicDefOf.TM_Rend_I || current16.abilityDef == TorannMagicDefOf.TM_Rend_II || current16.abilityDef == TorannMagicDefOf.TM_Rend_III))
                                    {
                                        if (current16.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Rend);
                                        }
                                        else if (current16.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Rend_I);
                                        }
                                        else if (current16.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Rend_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Rend_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag55 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Enchanter) || flagCM;
                    if (flag55)
                    {
                        bool flagE = !this.MagicData.MagicPowersE.NullOrEmpty<MagicPower>();
                        if (flagE)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current17 in this.MagicData.MagicPowersE)
                            {
                                bool flagE2 = current17.abilityDef != null;
                                if (flagE2)
                                {
                                    if (current17.learned == true && (current17.abilityDef == TorannMagicDefOf.TM_Polymorph || current17.abilityDef == TorannMagicDefOf.TM_Polymorph_I || current17.abilityDef == TorannMagicDefOf.TM_Polymorph_II || current17.abilityDef == TorannMagicDefOf.TM_Polymorph_III))
                                    {
                                        if (current17.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Polymorph);
                                        }
                                        else if (current17.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Polymorph_I);
                                        }
                                        else if (current17.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Polymorph_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Polymorph_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag56 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Chronomancer) || flagCM;
                    if (flag56)
                    {
                        bool flagC = !this.MagicData.MagicPowersC.NullOrEmpty<MagicPower>();
                        if (flagC)
                        {
                            //this.LoadPowers();
                            foreach (MagicPower current18 in this.MagicData.MagicPowersC)
                            {
                                bool flagC2 = current18.abilityDef != null;
                                if (flagC2)
                                {
                                    if (current18.learned == true && (current18.abilityDef == TorannMagicDefOf.TM_ChronostaticField || current18.abilityDef == TorannMagicDefOf.TM_ChronostaticField_I || current18.abilityDef == TorannMagicDefOf.TM_ChronostaticField_II || current18.abilityDef == TorannMagicDefOf.TM_ChronostaticField_III))
                                    {
                                        if (current18.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ChronostaticField);
                                        }
                                        else if (current18.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ChronostaticField_I);
                                        }
                                        else if (current18.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ChronostaticField_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ChronostaticField_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (flag40)
                    {
                        //Log.Message("Loading Inner Fire Abilities");
                        MagicPower mpIF = this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Firebolt);
                        if (mpIF.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Firebolt);
                        }
                        mpIF = this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Fireclaw);
                        if (mpIF.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Fireclaw);
                        }
                        mpIF = this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Fireball);
                        if (mpIF.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Fireball);
                        }
                    }
                    if (flag41)
                    {
                        //Log.Message("Loading Heart of Frost Abilities");
                        MagicPower mpHoF = this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Icebolt);
                        if (mpHoF.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Icebolt);
                        }
                        mpHoF = this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Snowball);
                        if (mpHoF.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Snowball);
                        }
                        mpHoF = this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Rainmaker);
                        if (mpHoF.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Rainmaker);
                        }

                    }
                    if (flag42)
                    {
                        //Log.Message("Loading Storm Born Abilities");
                        MagicPower mpSB = this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LightningBolt);
                        if (mpSB.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_LightningBolt);
                        }
                        mpSB = this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LightningCloud);
                        if (mpSB.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_LightningCloud);
                        }
                        mpSB = this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LightningStorm);
                        if (mpSB.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_LightningStorm);
                        }
                    }
                    if (flag43)
                    {
                        //Log.Message("Loading Arcane Abilities");
                        MagicPower mpA = this.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Teleport);
                        if (mpA.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Teleport);
                        }
                    }
                    if (flag44)
                    {
                        //Log.Message("Loading Paladin Abilities");
                        MagicPower mpP = this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Heal);
                        if (mpP.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Heal);
                        }
                        mpP = this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ValiantCharge);
                        if (mpP.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_ValiantCharge);
                        }
                        mpP = this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Overwhelm);
                        if (mpP.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Overwhelm);
                        }
                    }
                    if (flag45)
                    {
                        //Log.Message("Loading Summoner Abilities");
                        MagicPower mpS = this.MagicData.MagicPowersS.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonMinion);
                        if (mpS.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_SummonMinion);
                        }
                        mpS = this.MagicData.MagicPowersS.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonPylon);
                        if (mpS.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_SummonPylon);
                        }
                        mpS = this.MagicData.MagicPowersS.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonExplosive);
                        if (mpS.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_SummonExplosive);
                        }
                        mpS = this.MagicData.MagicPowersS.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonElemental);
                        if (mpS.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_SummonElemental);
                        }
                    }
                    if (flag46)
                    {
                        //Log.Message("Loading Druid Abilities");
                        MagicPower mpD = this.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Poison);
                        if (mpD.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Poison);
                        }
                        mpD = this.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Regenerate);
                        if (mpD.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Regenerate);
                        }
                        mpD = this.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_CureDisease);
                        if (mpD.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_CureDisease);
                        }
                    }
                    if (flag47)
                    {
                        //Log.Message("Loading Necromancer Abilities");
                        MagicPower mpN = this.MagicData.MagicPowersN.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RaiseUndead);
                        if (mpN.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_RaiseUndead);
                        }
                        mpN = this.MagicData.MagicPowersN.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_FogOfTorment);
                        if (mpN.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_FogOfTorment);
                        }
                    }
                    if (flag48)
                    {
                        //Log.Message("Loading Priest Abilities");
                        MagicPower mpPR = this.MagicData.MagicPowersPR.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_AdvancedHeal);
                        if (mpPR.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_AdvancedHeal);
                        }
                        mpPR = this.MagicData.MagicPowersPR.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Purify);
                        if (mpPR.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Purify);
                        }
                    }
                    if (flag49)
                    {
                        //Log.Message("Loading Bard Abilities");
                        MagicPower mpB = this.MagicData.MagicPowersB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BardTraining);
                        //if (mpB.learned == true)
                        //{
                        //    this.AddPawnAbility(TorannMagicDefOf.TM_BardTraining);
                        //}
                        mpB = this.MagicData.MagicPowersB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Entertain);
                        if (mpB.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Entertain);
                        }
                        //mpB = this.MagicData.MagicPowersB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Inspire);
                        //if (mpB.learned == true)
                        //{
                        //    this.AddPawnAbility(TorannMagicDefOf.TM_Inspire);
                        //}
                    }
                    if (flag50)
                    {
                        //Log.Message("Loading Succubus Abilities");
                        MagicPower mpSD = this.MagicData.MagicPowersSD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Dominate);
                        if (mpSD.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Dominate);
                        }
                        mpSD = this.MagicData.MagicPowersSD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SoulBond);
                        if (mpSD.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_SoulBond);
                        }
                    }
                    if (flag51)
                    {
                        //Log.Message("Loading Warlock Abilities");
                        MagicPower mpWD = this.MagicData.MagicPowersWD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Dominate);
                        if (mpWD.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Dominate);
                        }
                        mpWD = this.MagicData.MagicPowersWD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SoulBond);
                        if (mpWD.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_SoulBond);
                        }
                    }
                    if (flag52)
                    {
                        //Log.Message("Loading Geomancer Abilities");
                        MagicPower mpG = this.MagicData.MagicPowersG.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Stoneskin);
                        if (mpG.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Stoneskin);
                        }
                        mpG = this.MagicData.MagicPowersG.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EarthSprites);
                        if (mpG.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_EarthSprites);
                        }
                        mpG = this.MagicData.MagicPowersG.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EarthernHammer);
                        if (mpG.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_EarthernHammer);
                        }
                        mpG = this.MagicData.MagicPowersG.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Sentinel);
                        if (mpG.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Sentinel);
                        }
                    }
                    if (flag53)
                    {
                        //Log.Message("Loading Geomancer Abilities");
                        MagicPower mpT = this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TechnoTurret);
                        if (mpT.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_TechnoTurret);
                        }
                        mpT = this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TechnoWeapon);
                        if (mpT.learned == true)
                        {
                            //nano weapon applies only when equipping a new weapon
                            this.AddPawnAbility(TorannMagicDefOf.TM_TechnoWeapon);
                            this.AddPawnAbility(TorannMagicDefOf.TM_NanoStimulant);
                        }
                        mpT = this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TechnoShield);
                        if (mpT.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_TechnoShield);
                        }
                        mpT = this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Sabotage);
                        if (mpT.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Sabotage);
                        }
                        mpT = this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Overdrive);
                        if (mpT.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Overdrive);
                        }
                    }
                    if (flag54)
                    {
                        //Log.Message("Loading BloodMage Abilities");
                        MagicPower mpBM = this.MagicData.MagicPowersBM.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BloodGift);
                        if (mpBM.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_BloodGift);
                        }
                        mpBM = this.MagicData.MagicPowersBM.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_IgniteBlood);
                        if (mpBM.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_IgniteBlood);
                        }
                        mpBM = this.MagicData.MagicPowersBM.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BloodForBlood);
                        if (mpBM.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_BloodForBlood);
                        }
                        mpBM = this.MagicData.MagicPowersBM.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BloodShield);
                        if (mpBM.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_BloodShield);
                        }
                    }
                    if (flag55)
                    {
                        //Log.Message("Loading Enchanter Abilities");
                        MagicPower mpE = this.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchantedBody);
                        if (mpE.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_EnchantedBody);
                            this.spell_EnchantedAura = true;
                        }
                        mpE = this.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Transmutate);
                        if (mpE.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Transmutate);
                        }
                        mpE = this.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchanterStone);
                        if (mpE.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_EnchanterStone);
                        }
                        mpE = this.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchantWeapon);
                        if (mpE.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_EnchantWeapon);
                        }
                    }
                    if (flag56)
                    {
                        //Log.Message("Loading Chronomancer Abilities");
                        MagicPower mpC = this.MagicData.MagicPowersC.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Prediction);
                        if (mpC.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_Prediction);
                        }
                        mpC = this.MagicData.MagicPowersC.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_AlterFate);
                        if (mpC.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_AlterFate);
                        }
                        mpC = this.MagicData.MagicPowersC.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_AccelerateTime);
                        if (mpC.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_AccelerateTime);
                        }
                        mpC = this.MagicData.MagicPowersC.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ReverseTime);
                        if (mpC.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_ReverseTime);
                        }
                    }
                    if (flagCM)
                    {
                        //Log.Message("Loading Chaos Mage Abilities");
                        MagicPower mpCM = this.MagicData.MagicPowersCM.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ChaosTradition);
                        if (mpCM.learned == true)
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_ChaosTradition);
                            this.chaosPowers = new List<TM_ChaosPowers>();
                            this.chaosPowers.Clear();
                            List<MagicPower> learnedList = new List<MagicPower>();
                            learnedList.Clear();
                            for (int i = 0; i < this.MagicData.AllMagicPowersForChaosMage.Count; i++)
                            {
                                MagicPower mp = this.MagicData.AllMagicPowersForChaosMage[i];
                                if (mp.learned)
                                {
                                    learnedList.Add(mp);
                                }
                            }
                            int count = learnedList.Count;
                            for (int i = 0; i < 5; i++)
                            {
                                if (i < count)
                                {
                                    this.chaosPowers.Add(new TM_ChaosPowers((TMAbilityDef)learnedList[i].GetAbilityDef(0), TM_ClassUtility.GetAssociatedMagicPowerSkill(this, learnedList[i])));
                                    foreach(MagicPower mp in learnedList)
                                    {
                                        for (int j = 0; j < mp.TMabilityDefs.Count; j++)
                                        {
                                            TMAbilityDef tmad = mp.TMabilityDefs[j] as TMAbilityDef;
                                            if(tmad.shouldInitialize)
                                            {
                                                int level = mp.level;
                                                if (mp.TMabilityDefs[level] == TorannMagicDefOf.TM_LightSkip)
                                                {
                                                    if (TM_Calc.GetSkillPowerLevel(this.Pawn, TorannMagicDefOf.TM_LightSkip) >= 1)
                                                    {
                                                        base.AddPawnAbility(TorannMagicDefOf.TM_LightSkipMass);
                                                    }
                                                    if (TM_Calc.GetSkillPowerLevel(this.Pawn, TorannMagicDefOf.TM_LightSkip) >= 2)
                                                    {
                                                        base.AddPawnAbility(TorannMagicDefOf.TM_LightSkipGlobal);
                                                    }
                                                }
                                                if (tmad == TorannMagicDefOf.TM_Hex && this.HexedPawns.Count > 0)
                                                {
                                                    RemovePawnAbility(TorannMagicDefOf.TM_Hex_CriticalFail);
                                                    RemovePawnAbility(TorannMagicDefOf.TM_Hex_MentalAssault);
                                                    RemovePawnAbility(TorannMagicDefOf.TM_Hex_Pain);
                                                    AddPawnAbility(TorannMagicDefOf.TM_Hex_CriticalFail);
                                                    AddPawnAbility(TorannMagicDefOf.TM_Hex_MentalAssault);
                                                    AddPawnAbility(TorannMagicDefOf.TM_Hex_Pain);
                                                }
                                                
                                                RemovePawnAbility(tmad);
                                                AddPawnAbility(tmad);
                                            }
                                            if(tmad.childAbilities != null && tmad.childAbilities.Count > 0)
                                            {
                                                foreach(TMAbilityDef ad in tmad.childAbilities)
                                                {
                                                    if(ad.shouldInitialize)
                                                    {
                                                        RemovePawnAbility(ad);
                                                        AddPawnAbility(ad);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    this.chaosPowers.Add(new TM_ChaosPowers((TMAbilityDef)TM_Calc.GetRandomMagicPower(this).abilityDef, null));
                                }
                            }
                        }
                    }
                }
                if(TM_Calc.HasAdvancedClass(this.Pawn))
                {
                    List<TMDefs.TM_CustomClass> ccList = TM_ClassUtility.GetAdvancedClassesForPawn(this.Pawn);
                    foreach(TMDefs.TM_CustomClass cc in ccList)
                    {
                        if(cc.isMage)
                        {
                            this.AdvancedClasses.Add(cc);
                            LoadCustomClassAbilities(cc);
                        }
                    }                    
                }
                this.UpdateAutocastDef();
                this.InitializeSpell();
                //base.UpdateAbilities();
            }
        }

        public void LoadCustomClassAbilities(TMDefs.TM_CustomClass cc, Pawn fromPawn = null)
        {
            for (int i = 0; i < cc.classMageAbilities.Count; i++)
            {
                TMAbilityDef ability = cc.classMageAbilities[i];
                MagicData fromData = null;
                if (fromPawn != null)
                {
                   fromData = fromPawn.GetCompAbilityUserMagic().MagicData;
                }
                if (fromData != null)
                {
                    foreach (MagicPower fp in fromData.AllMagicPowers)
                    {
                        if (fp.learned && cc.classMageAbilities.Contains(fp.abilityDef))
                        {
                            MagicPower mp = this.MagicData.AllMagicPowers.FirstOrDefault((MagicPower x) => x.abilityDef == fp.TMabilityDefs[0]);                            
                            if (mp != null)
                            {
                                mp.learned = true;
                                mp.level = fp.level;
                            }
                        }
                    }
                }

                for (int j = 0; j < this.MagicData.AllMagicPowers.Count; j++)
                {
                    if (this.MagicData.AllMagicPowers[j] == this.MagicData.MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SoulBond) ||
                            this.MagicData.AllMagicPowers[j] == this.MagicData.MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ShadowBolt) ||
                            this.MagicData.AllMagicPowers[j] == this.MagicData.MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Dominate))
                    {
                        this.MagicData.AllMagicPowers[j].learned = false;
                    }
                    
                    if (this.MagicData.AllMagicPowers[j].TMabilityDefs.Contains(cc.classMageAbilities[i]) && this.MagicData.AllMagicPowers[j].learned)
                    {
                        if (cc.classMageAbilities[i].shouldInitialize)
                        {
                            int level = this.MagicData.AllMagicPowers[j].level;                                                        
                            base.AddPawnAbility(this.MagicData.AllMagicPowers[j].TMabilityDefs[level]);
                            if (this.magicData.AllMagicPowers[j].TMabilityDefs[level] == TorannMagicDefOf.TM_LightSkip)
                            {
                                if (TM_Calc.GetSkillPowerLevel(this.Pawn, TorannMagicDefOf.TM_LightSkip) >= 1)
                                {
                                    base.AddPawnAbility(TorannMagicDefOf.TM_LightSkipMass);
                                }
                                if (TM_Calc.GetSkillPowerLevel(this.Pawn, TorannMagicDefOf.TM_LightSkip) >= 2)
                                {
                                    base.AddPawnAbility(TorannMagicDefOf.TM_LightSkipGlobal);
                                }
                            }
                            if (cc.classMageAbilities[i] == TorannMagicDefOf.TM_Hex && this.HexedPawns.Count > 0)
                            {
                                RemovePawnAbility(TorannMagicDefOf.TM_Hex_CriticalFail);
                                RemovePawnAbility(TorannMagicDefOf.TM_Hex_MentalAssault);
                                RemovePawnAbility(TorannMagicDefOf.TM_Hex_Pain);
                                AddPawnAbility(TorannMagicDefOf.TM_Hex_CriticalFail);
                                AddPawnAbility(TorannMagicDefOf.TM_Hex_MentalAssault);
                                AddPawnAbility(TorannMagicDefOf.TM_Hex_Pain);
                            }
                        }
                        if (ability.childAbilities != null && ability.childAbilities.Count > 0)
                        {
                            for (int c = 0; c < ability.childAbilities.Count; c++)
                            {
                                if (ability.childAbilities[c].shouldInitialize)
                                {
                                    this.AddPawnAbility(ability.childAbilities[c]);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void AddAdvancedClass(TMDefs.TM_CustomClass ac, Pawn fromPawn = null)
        {
            if (ac != null && ac.isMage && ac.isAdvancedClass)
            {
                Trait t = base.Pawn.story.traits.GetTrait(TorannMagicDefOf.TM_Possessed);
                if (t != null && !base.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SpiritPossessionHD))
                {
                    base.Pawn.story.traits.RemoveTrait(t);
                    return;
                }
                if (!this.AdvancedClasses.Contains(ac))
                {
                    this.AdvancedClasses.Add(ac);
                }
                else // clear all abilities and re-add
                {
                    foreach (TMAbilityDef ability in ac.classMageAbilities)
                    {
                        RemovePawnAbility(ability);
                        if (ability.childAbilities != null && ability.childAbilities.Count > 0)
                        {
                            foreach (TMAbilityDef cab in ability.childAbilities)
                            {
                                RemovePawnAbility(cab);
                            }
                        }
                    }
                }
                if(fromPawn != null)
                {
                    MagicData fromData = fromPawn.GetCompAbilityUserMagic().MagicData;
                    if(fromData != null)
                    {
                        foreach(TMAbilityDef ability in ac.classMageAbilities)
                        {
                            MagicPowerSkill mps_e = this.MagicData.GetSkill_Efficiency(ability);
                            MagicPowerSkill fps_e = fromData.GetSkill_Efficiency(ability);
                            if (mps_e != null && fps_e != null)
                            {
                                mps_e.level = fps_e.level;
                            }
                            MagicPowerSkill mps_p = this.MagicData.GetSkill_Power(ability);
                            MagicPowerSkill fps_p = fromData.GetSkill_Power(ability);
                            if (mps_p != null && fps_p != null)
                            {
                                mps_p.level = fps_p.level;
                            }
                            MagicPowerSkill mps_v = this.MagicData.GetSkill_Versatility(ability);
                            MagicPowerSkill fps_v = fromData.GetSkill_Versatility(ability);
                            if (mps_v != null && fps_v != null)
                            {
                                mps_v.level = fps_v.level;
                            }
                        }
                    }
                }
                LoadCustomClassAbilities(ac, fromPawn);
            }
        }

        public void RemoveAdvancedClass(TMDefs.TM_CustomClass ac)
        {
            for (int i = 0; i < ac.classMageAbilities.Count; i++)
            {
                TMAbilityDef ability = ac.classMageAbilities[i];

                for (int j = 0; j < this.MagicData.AllMagicPowers.Count; j++)
                {
                    MagicPower power = this.MagicData.AllMagicPowers[j];
                    if (power.abilityDef == ability)
                    {
                        if (this.magicData.AllMagicPowers[j].TMabilityDefs[power.level] == TorannMagicDefOf.TM_LightSkip)
                        {
                            if (TM_Calc.GetSkillPowerLevel(this.Pawn, TorannMagicDefOf.TM_LightSkip) >= 1)
                            {
                                base.RemovePawnAbility(TorannMagicDefOf.TM_LightSkipMass);
                            }
                            if (TM_Calc.GetSkillPowerLevel(this.Pawn, TorannMagicDefOf.TM_LightSkip) >= 2)
                            {
                                base.RemovePawnAbility(TorannMagicDefOf.TM_LightSkipGlobal);
                            }
                        }
                        if (ac.classMageAbilities[i] == TorannMagicDefOf.TM_Hex && this.HexedPawns.Count > 0)
                        {
                            RemovePawnAbility(TorannMagicDefOf.TM_Hex_CriticalFail);
                            RemovePawnAbility(TorannMagicDefOf.TM_Hex_MentalAssault);
                            RemovePawnAbility(TorannMagicDefOf.TM_Hex_Pain);
                        }                        
                        power.autocast = false;
                        power.learned = false;
                        power.level = 0;

                        if (ability.childAbilities != null && ability.childAbilities.Count > 0)
                        {
                            for (int c = 0; c < ability.childAbilities.Count; c++)
                            {
                                this.RemovePawnAbility(ability.childAbilities[c]);
                            }
                        }
                    }
                    base.RemovePawnAbility(ability);
                }
            }
            if (ac != null && ac.isMage && ac.isAdvancedClass)
            {
                foreach (TMAbilityDef ability in ac.classMageAbilities)
                {
                    MagicPowerSkill mps_e = this.MagicData.GetSkill_Efficiency(ability);
                    if (mps_e != null)
                    {
                        mps_e.level = 0;
                    }
                    MagicPowerSkill mps_p = this.MagicData.GetSkill_Power(ability);
                    if (mps_p != null)
                    {
                        mps_p.level = 0;
                    }
                    MagicPowerSkill mps_v = this.MagicData.GetSkill_Versatility(ability);
                    if (mps_v != null)
                    {
                        mps_v.level = 0;
                    }
                }
            }
            if(this.AdvancedClasses.Contains(ac))
            {
                this.AdvancedClasses.Remove(ac);
            }
        }

        public void UpdateAutocastDef()
        {
            IEnumerable<TM_CustomPowerDef> mpDefs = TM_Data.CustomMagePowerDefs();
            if (this.IsMagicUser && this.MagicData != null && this.MagicData.MagicPowersCustom != null)
            {
                foreach (MagicPower mp in this.MagicData.MagicPowersCustom)
                {
                    foreach (TM_CustomPowerDef mpDef in mpDefs)
                    {                        
                        if (mpDef.customPower.abilityDefs[0].ToString() == mp.GetAbilityDef(0).ToString())
                        {
                            if (mpDef.customPower.autocasting != null)
                            {
                                mp.autocasting = mpDef.customPower.autocasting;
                            }
                        }
                    }
                }
            }
        }

        private Dictionary<string, Command> gizmoCommands = new Dictionary<string, Command>();
        public Command GetGizmoCommands(string key)
        {
            if (!gizmoCommands.ContainsKey(key))
            {
                Pawn p = this.Pawn;
                if (key == "symbiosis")
                {
                    Command_Action itemSymbiosis = new Command_Action
                    {
                        action = new Action(delegate
                        {
                            TM_Action.RemoveSymbiosisCommand(p);
                        }),
                        Order = 61,
                        defaultLabel = TM_TextPool.TM_RemoveSymbiosis,
                        defaultDesc = TM_TextPool.TM_RemoveSymbiosisDesc,
                        icon = ContentFinder<Texture2D>.Get("UI/remove_spiritpossession", true),
                    };
                    gizmoCommands.Add(key, itemSymbiosis);
                }
                if (key == "wanderer")
                {
                    Command_Action itemWanderer = new Command_Action
                    {
                        action = new Action(delegate
                        {
                            TM_Action.PromoteWanderer(p);
                        }),
                        Order = 51,
                        defaultLabel = TM_TextPool.TM_PromoteWanderer,
                        defaultDesc = TM_TextPool.TM_PromoteWandererDesc,
                        icon = ContentFinder<Texture2D>.Get("UI/wanderer", true),
                    };
                    gizmoCommands.Add(key, itemWanderer);
                }
                if(key == "technoBit")
                {
                    String toggle = "bit_c";
                    String label = "TM_TechnoBitEnabled".Translate();
                    String desc = "TM_TechnoBitToggleDesc".Translate();
                    if (!this.useTechnoBitToggle)
                    {
                        toggle = "bit_off";
                        label = "TM_TechnoBitDisabled".Translate();
                    }
                    var item = new Command_Toggle
                    {
                        isActive = () => this.useTechnoBitToggle,
                        toggleAction = () =>
                        {
                            this.useTechnoBitToggle = !this.useTechnoBitToggle;
                        },
                        defaultLabel = label,
                        defaultDesc = desc,
                        Order = -89,
                        icon = ContentFinder<Texture2D>.Get("UI/" + toggle, true)
                    };
                    gizmoCommands.Add(key, item);
                }
                if(key == "technoRepair")
                {
                    String toggle_repair = "bit_repairon";
                    String label_repair = "TM_TechnoBitRepair".Translate();
                    String desc_repair = "TM_TechnoBitRepairDesc".Translate();
                    if (!this.useTechnoBitRepairToggle)
                    {
                        toggle_repair = "bit_repairoff";
                    }
                    var item_repair = new Command_Toggle
                    {
                        isActive = () => this.useTechnoBitRepairToggle,
                        toggleAction = () =>
                        {
                            this.useTechnoBitRepairToggle = !this.useTechnoBitRepairToggle;
                        },
                        defaultLabel = label_repair,
                        defaultDesc = desc_repair,
                        Order = -88,
                        icon = ContentFinder<Texture2D>.Get("UI/" + toggle_repair, true)
                    };
                    gizmoCommands.Add(key, item_repair);
                }
                if(key == "elementalShot")
                {
                    String toggle = "elementalshot";
                    String label = "TM_TechnoWeapon_ver".Translate();
                    String desc = "TM_ElementalShotToggleDesc".Translate();
                    if (!this.useElementalShotToggle)
                    {
                        toggle = "elementalshot_off";
                    }
                    var item = new Command_Toggle
                    {
                        isActive = () => this.useElementalShotToggle,
                        toggleAction = () =>
                        {
                            this.useElementalShotToggle = !this.useElementalShotToggle;
                        },
                        defaultLabel = label,
                        defaultDesc = desc,
                        Order = -88,
                        icon = ContentFinder<Texture2D>.Get("UI/" + toggle, true)
                    };
                    gizmoCommands.Add(key, item);
                }
            }
            if (gizmoCommands.ContainsKey(key))
            {
                return gizmoCommands[key];
            }
            else
            {
                return null;
            }
        }
    }
}
