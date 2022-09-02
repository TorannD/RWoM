using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using AbilityUser;
using Verse;
using Verse.AI;
using Verse.Sound;
using AbilityUserAI;
using TorannMagic.Ideology;
using TorannMagic.TMDefs;

namespace TorannMagic
{
    [CompilerGenerated]
    [Serializable]
    [StaticConstructorOnStartup]
    public class CompAbilityUserMagic : CompAbilityUserTMBase
    {
        public string LabelKey = "TM_Magic";

        public bool firstTick;
        public bool magicPowersInitialized;
        public bool magicPowersInitializedForColonist = true;
        private bool colonistPowerCheck = true;
        private int resMitigationDelay;
        private int damageMitigationDelay;
        private int damageMitigationDelayMS;
        public int magicXPRate = 1000;
        public int lastXPGain;
        
        private bool doOnce = true;
        private List<IntVec3> deathRing = new List<IntVec3>();
        public float weaponDamage = 1;
        public float weaponCritChance;
        public LocalTargetInfo SecondTarget = null;
        public List<TM_EventRecords> magicUsed = new List<TM_EventRecords>();

        private float IF_RayofHope_eff = 0.08f;
        private float IF_Firebolt_eff = 0.10f;
        private float IF_Fireclaw_eff = 0.10f;
        private float IF_Fireball_eff = 0.08f;
        private float IF_Firestorm_eff = 0.05f;
        private float HoF_Soothe_eff = 0.08f;
        private float HoF_Icebolt_eff = 0.08f;
        private float HoF_FrostRay_eff = 0.08f;
        private float HoF_Snowball_eff = 0.08f;
        private float HoF_Blizzard_eff = 0.05f;
        private float HoF_Rainmaker_eff = 0.15f;
        private float SB_AMP_eff = 0.08f;
        private float SB_LightningBolt_eff = 0.08f;
        private float SB_LightningCloud_eff = 0.06f;
        private float SB_LightningStorm_eff = 0.06f;
        private float SB_EyeOfTheStorm_eff = 0.05f;
        private float A_Shadow_eff = 0.08f;
        private float A_MagicMissile_eff = 0.08f;
        private float A_Blink_eff = 0.10f;
        private float A_Summon_eff = 0.10f;
        private float A_Teleport_eff = 0.10f;
        private float A_FoldReality_eff = 0.06f;
        private float P_Heal_eff = 0.07f;
        private float P_Shield_eff = 0.08f;
        private float P_ValiantCharge_eff = 0.08f;
        private float P_Overwhelm_eff = 0.10f;
        private float P_HolyWrath_eff = 0.05f;
        private float S_SummonElemental_eff = 0.06f;
        private float S_SummonExplosive_eff = 0.08f;
        private float S_SummonMinion_eff = 0.10f;
        private float S_SummonPylon_eff = 0.08f;
        private float S_SummonPoppi_eff = 0.05f;
        private float D_Poison_eff = 0.10f;
        private float D_SootheAnimal_eff = 0.1f;
        private float D_Regenerate_eff = 0.07f;
        private float D_CureDisease_eff = 0.10f;
        private float D_RegrowLimb_eff = 0.06f;
        private float N_RaiseUndead_eff = 0.05f;
        private float N_DeathMark_eff = 0.08f;
        private float N_FogOfTorment_eff = 0.08f;
        private float N_ConsumeCorpse_eff = 0.0f;
        private float N_CorpseExplosion_eff = 0.08f;
        private float N_DeathBolt_eff = 0.06f;
        private float PR_AdvancedHeal_eff = 0.08f;
        private float PR_Purify_eff = 0.07f;
        private float PR_HealingCircle_eff = 0.07f;
        private float PR_BestowMight_eff = 0.08f;
        private float PR_Resurrection_eff = 0.05f;
        private float B_Lullaby_eff = 0.08f;
        private float B_BattleHymn_eff = 0.06f;
        private float SoulBond_eff = 0.10f;
        private float ShadowBolt_eff = .08f;
        private float Dominate_eff = 0.06f;
        private float WD_Repulsion_eff = .08f;
        private float WD_PsychicShock_eff = .06f;
        private float SD_Attraction_eff = .08f;
        private float SD_Scorn_eff = .06f;
        private float G_Encase_eff = .08f;
        private float G_EarthSprites_eff = 0.06f;
        private float G_EarthernHammer_eff = 0.06f;
        private float G_Meteor_eff = 0.05f;
        private float T_TechnoTurret_eff = 0.02f;
        private float T_TechnoShield_eff = 0.06f;
        private float T_Overdrive_eff = 0.08f;
        private float T_Sabotage_eff = 0.06f;
        private float T_OrbitalStrike_eff = 0.05f;
        private float BM_BloodGift_eff = 0.05f;
        private float BM_IgniteBlood_eff = .06f;
        private float BM_BloodForBlood_eff = .06f;
        private float BM_BloodShield_eff = .06f;
        private float BM_Rend_eff = .08f;
        private float BM_BloodMoon_eff = .05f;
        private float E_EnchantedBody_eff = .15f;
        private float E_Transmutate_eff = .12f;
        private float E_EnchantWeapon_eff = .1f;
        private float E_EnchanterStone_eff = .10f;
        private float E_Polymorph_eff = .06f;
        private float E_Shapeshift_eff = .05f;
        private float C_Prediction_eff = .15f;
        private float C_AlterFate_eff = .1f;
        private float C_AccelerateTime_eff = .08f;
        private float C_ReverseTime_eff = .08f;
        private float C_ChronostaticField_eff = .06f;
        private float C_Recall_eff = .1f;

        private float W_eff = .01f;
        private float global_eff = 0.025f;

        public bool spell_Rain;
        public bool spell_Blink;
        public bool spell_Teleport;
        public bool spell_Heal;
        public bool spell_Heater;
        public bool spell_Cooler;
        public bool spell_DryGround;
        public bool spell_WetGround;
        public bool spell_ChargeBattery;
        public bool spell_SmokeCloud;
        public bool spell_Extinguish;
        public bool spell_EMP;
        public bool spell_Firestorm;
        public bool spell_Blizzard;
        public bool spell_SummonMinion;
        public bool spell_TransferMana;
        public bool spell_SiphonMana;
        public bool spell_RegrowLimb;
        public bool spell_EyeOfTheStorm;
        public bool spell_ManaShield;
        public bool spell_FoldReality;
        public bool spell_Resurrection;
        public bool spell_PowerNode;
        public bool spell_Sunlight;
        public bool spell_HolyWrath;
        public bool spell_LichForm;
        public bool spell_Flight;
        public bool spell_SummonPoppi;
        public bool spell_BattleHymn;
        public bool spell_CauterizeWound;
        public bool spell_FertileLands;
        public bool spell_SpellMending;
        public bool spell_ShadowStep;
        public bool spell_ShadowCall;
        public bool spell_Scorn;
        public bool spell_PsychicShock;
        public bool spell_SummonDemon;
        public bool spell_Meteor;
        public bool spell_Teach;
        public bool spell_OrbitalStrike;
        public bool spell_BloodMoon;
        public bool spell_EnchantedAura;
        public bool spell_Shapeshift;
        public bool spell_ShapeshiftDW;
        public bool spell_Blur;
        public bool spell_BlankMind;
        public bool spell_DirtDevil;
        public bool spell_MechaniteReprogramming;
        public bool spell_ArcaneBolt;
        public bool spell_LightningTrap;
        public bool spell_Invisibility;
        public bool spell_BriarPatch;
        public bool spell_Recall;
        public bool spell_MageLight;
        public bool spell_SnapFreeze;
        public bool spell_Ignite;
        public bool spell_CreateLight;
        public bool spell_EqualizeLight;
        public bool spell_HeatShield;

        private bool item_StaffOfDefender;

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
        public IntVec3 earthSprites = default;
        public bool earthSpritesInArea;
        public Map earthSpriteMap;
        public int nextEarthSpriteAction;
        public int nextEarthSpriteMote;
        public int earthSpriteType;
        private bool dismissEarthSpriteSpell;
        public List<Thing> summonedLights = new List<Thing>();
        public List<Thing> summonedHeaters = new List<Thing>();
        public List<Thing> summonedCoolers = new List<Thing>();
        public List<Thing> summonedPowerNodes = new List<Thing>();
        public ThingDef guardianSpiritType;
        public Pawn soulBondPawn;
        private bool dismissMinionSpell;
        private bool dismissUndeadSpell;
        private bool dismissSunlightSpell;
        private bool dispelStoneskin;
        private bool dismissCoolerSpell;
        private bool dismissHeaterSpell;
        private bool dismissPowerNodeSpell;
        private bool dispelEnchantWeapon;
        private bool dismissEnchanterStones;
        private bool dismissLightningTrap;
        private bool shatterSentinel;
        private bool dismissGuardianSpirit;
        private bool dispelLivingWall;
        private bool dispelBrandings;
        public List<IntVec3> fertileLands = new List<IntVec3>();
        public Thing mageLightThing;
        public bool mageLightActive;
        public bool mageLightSet;
        public bool useTechnoBitToggle = true;
        public bool useTechnoBitRepairToggle = true;
        public Vector3 bitPosition = Vector3.zero;
        private bool bitFloatingDown = true;
        private float bitOffset = .45f;
        public int technoWeaponDefNum = -1;
        public Thing technoWeaponThing;
        public ThingDef technoWeaponThingDef;
        public QualityCategory technoWeaponQC = QualityCategory.Normal;
        public bool useElementalShotToggle = true;
        public Building overdriveBuilding;
        public int overdriveDuration;
        public float overdrivePowerOutput;
        public int overdriveFrequency = 100;
        public Building sabotageBuilding = null;
        public bool ArcaneForging;
        public List<Pawn> weaponEnchants = new List<Pawn>();
        public Thing enchanterStone;
        public List<Thing> enchanterStones = new List<Thing>();
        public List<Thing> lightningTraps = new List<Thing>();        
        public IncidentDef predictionIncidentDef;
        public int predictionTick;
        public int predictionHash;
        private List<Pawn> hexedPawns = new List<Pawn>();
        //Recall fields
        //position, hediffs, needs, mana, manual recall bool, recall duration
        public IntVec3 recallPosition = default;
        public Map recallMap;
        public List<string> recallNeedDefnames;
        public List<float> recallNeedValues;
        public List<Hediff> recallHediffList;
        public List<float> recallHediffDefSeverityList;
        public List<int> recallHediffDefTicksRemainingList;
        public List<Hediff_Injury> recallInjuriesList;
        public bool recallSet;
        public int recallExpiration;
        public bool recallSpell;
        public FlyingObject_SpiritOfLight SoL;
        public Pawn bondedSpirit;
        //public List<TM_Branding> brandings = new List<TM_Branding>();
        public List<Pawn> brandedPawns = new List<Pawn>();
        public List<Pawn> brands = new List<Pawn>();
        public List<HediffDef> brandDefs = new List<HediffDef>();
        public bool sigilSurging;
        public bool sigilDraining;
        public FlyingObject_LivingWall livingWall;
        public int lastChaosTraditionTick;
        public ThingOwner<ThingWithComps> magicWardrobe;

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
                    magicWardrobe.Clear();
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
                    magicUsed.Clear();
                }
                return magicUsed;
            }
            set
            {
                if (magicUsed == null)
                {
                    magicUsed = new List<TM_EventRecords>();
                    magicUsed.Clear();
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
                    stoneskinPawns.Clear();
                }
                List<Pawn> tmpList = new List<Pawn>();
                tmpList.Clear();
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
                if(guardianSpiritType == null)
                {
                    float rnd = Rand.Value;
                    
                    if(rnd < .34f)
                    {
                        guardianSpiritType = TorannMagicDefOf.TM_SpiritBearR;
                    }
                    else if (rnd < .67f)
                    {
                        guardianSpiritType = TorannMagicDefOf.TM_SpiritMongooseR;
                    }
                    else
                    {
                        guardianSpiritType = TorannMagicDefOf.TM_SpiritCrowR;
                    }
                }
                return guardianSpiritType;
            }
        }

        public bool HasTechnoBit
        {
            get
            {
                return IsMagicUser && MagicData.MagicPowersT.First(mp => mp.abilityDef == TorannMagicDefOf.TM_TechnoBit).learned;
            }
        }
        public bool HasTechnoTurret
        {
            get
            {
                return IsMagicUser && MagicData.MagicPowersT.First(mp => mp.abilityDef == TorannMagicDefOf.TM_TechnoTurret).learned;
            }
        }

        public bool HasTechnoWeapon
        {
            get
            {
                return IsMagicUser && MagicData.MagicPowersT.First(mps => mps.abilityDef == TorannMagicDefOf.TM_TechnoWeapon).learned;
            }
        }

        public int PowerModifier
        {
            get
            {
                return powerModifier;
            }
            set
            {
                TM_MoteMaker.ThrowSiphonMote(Pawn.DrawPos, Pawn.Map, 1f);
                powerModifier = Mathf.Clamp(value, 0, maxPower);
            }
        }

        private MagicData magicData = null;
        public MagicData MagicData
        {
            get
            {
                if (magicData == null && IsMagicUser)
                {
                    magicData = new MagicData(this);
                }
                return magicData;
            }
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (powerEffecter != null)
            {
                powerEffecter.Cleanup();
            }
        }

        public List<Pawn> HexedPawns
        {
            get
            {
                if(hexedPawns == null)
                {
                    hexedPawns = new List<Pawn>();
                }
                List<Pawn> validPawns = new List<Pawn>();
                validPawns.Clear();
                foreach (Pawn p in hexedPawns)
                {
                    if (p != null && !p.Destroyed && !p.Dead)
                    {
                        if (p.health != null && p.health.hediffSet != null && p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HexHD))
                        {
                            validPawns.Add(p);
                        }
                    }
                }
                hexedPawns = validPawns;
                return hexedPawns;
            }
        }

        public bool shouldDraw = true;
        public override void PostDraw()
        {
            if (shouldDraw && IsMagicUser)
            {
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (settingsRef.AIFriendlyMarking && Pawn.IsColonist && IsMagicUser)
                {
                    if (!Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                    {
                        DrawMark();
                    }
                }
                if (settingsRef.AIMarking && !Pawn.IsColonist && IsMagicUser)
                {
                    if (!Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                    {
                        DrawMark();
                    }
                }

                if (MagicData.MagicPowersT.First(mp => mp.abilityDef == TorannMagicDefOf.TM_TechnoBit).learned && Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TechnoBitHD))
                {
                    DrawTechnoBit();
                }

                if (Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DemonScornHD) || Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DemonScornHD_I) || Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DemonScornHD_II) || Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DemonScornHD_III))
                {
                    DrawScornWings();
                }

                if (mageLightActive)
                {
                    DrawMageLight();
                }

                Enchantment.CompEnchant compEnchant = Pawn.GetComp<Enchantment.CompEnchant>();
                //try
                //{
                    if (IsMagicUser && compEnchant != null && compEnchant.enchantingContainer.Count > 0)
                    {
                        DrawEnchantMark();
                    }
                //}
                //catch
                //{
                //    Enchantment.CompProperties_Enchant newEnchantComp = new Enchantment.CompProperties_Enchant();
                //    Pawn.def.comps.Add(newEnchantComp);
                //}
            }
            base.PostDraw();
        }


        private void DrawTechnoBit()
        {
            float num = Mathf.Lerp(1.2f, 1.55f, 1f);
            if (bitFloatingDown)
            {
                if (bitOffset < .38f)
                {
                    bitFloatingDown = false;
                }
                bitOffset -= .001f;
            }
            else
            {
                if (bitOffset > .57f)
                {
                    bitFloatingDown = true;
                }
                bitOffset += .001f;
            }

            bitPosition = Pawn.Drawer.DrawPos;
            bitPosition.x -= .5f + Rand.Range(-.01f, .01f);
            bitPosition.z += bitOffset;
            bitPosition.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            float angle = 0f;
            Vector3 s = new Vector3(.35f, 1f, .35f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(bitPosition, Quaternion.AngleAxis(angle, Vector3.up), s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.bitMat, 0);
        }

        private void DrawMageLight()
        {
            if (!mageLightSet)
            {
                Vector3 lightPos = Vector3.zero;

                lightPos = Pawn.Drawer.DrawPos;
                lightPos.x -= .5f;
                lightPos.z += .6f;

                lightPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                float angle = Rand.Range(0, 360);
                Vector3 s = new Vector3(.27f, .5f, .27f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(lightPos, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.mageLightMat, 0);
            }

        }

        public void DrawEnchantMark()
        {
            DrawMark(TM_RenderQueue.enchantMark, new Vector3(.5f, 1f, .5f));
        }

        public void DrawScornWings()
        {
            if (Pawn.Dead || Pawn.Downed) return;

            Vector3 vector = Pawn.Drawer.DrawPos;
            vector.y = AltitudeLayer.Pawn.AltitudeFor();
            if (Pawn.Rotation == Rot4.North)
            {
                vector.y = AltitudeLayer.PawnState.AltitudeFor();
            }
            Vector3 s = new Vector3(3f, 3f, 3f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(vector, Quaternion.AngleAxis(0f, Vector3.up), s);
            if (Pawn.Rotation == Rot4.South || Pawn.Rotation == Rot4.North)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.scornWingsNS, 0);
            }
            if (Pawn.Rotation == Rot4.East)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.scornWingsE, 0);
            }
            if (Pawn.Rotation == Rot4.West)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.scornWingsW, 0);
            }
        }

        public static List<TMAbilityDef> MagicAbilities = null;

        //LevelUpSkill_x is unused TODO: REMOVE
        //public int LevelUpSkill_global_regen(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_global_regen.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_global_eff(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_global_eff.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_global_spirit(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_global_spirit.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}

        //public int LevelUpSkill_RayofHope(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_RayofHope.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Firebolt(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Firebolt.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Fireball(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Fireball.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Fireclaw(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Fireclaw.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Firestorm(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Firestorm.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}

        //public int LevelUpSkill_Soothe(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Soothe.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Icebolt(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Icebolt.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_FrostRay(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_FrostRay.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Snowball(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Snowball.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Rainmaker(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Rainmaker.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Blizzard(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Blizzard.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}

        //public int LevelUpSkill_AMP(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_AMP.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_LightningBolt(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_LightningBolt.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_LightningCloud(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_LightningCloud.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_LightningStorm(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_LightningStorm.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_EyeOfTheStorm(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_EyeOfTheStorm.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}

        //public int LevelUpSkill_Shadow(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Shadow.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_MagicMissile(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_MagicMissile.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Blink(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Blink.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Summon(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Summon.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Teleport(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Teleport.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_FoldReality(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_FoldReality.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}

        //public int LevelUpSkill_Heal(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Heal.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Shield(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Shield.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_ValiantCharge(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_ValiantCharge.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Overwhelm(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Overwhelm.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_HolyWrath(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_HolyWrath.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}

        //public int LevelUpSkill_SummonMinion(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_SummonMinion.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_SummonPylon(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_SummonPylon.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_SummonExplosive(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_SummonExplosive.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_SummonElemental(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_SummonElemental.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_SummonPoppi(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_SummonPoppi.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}

        //public int LevelUpSkill_Poison(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Poison.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_SootheAnimal(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_SootheAnimal.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Regenerate(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Regenerate.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_CureDisease(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_CureDisease.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_RegrowLimb(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_RegrowLimb.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}

        //public int LevelUpSkill_RaiseUndead(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_RaiseUndead.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_DeathMark(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_DeathMark.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_FogOfTorment(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_FogOfTorment.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_ConsumeCorpse(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_ConsumeCorpse.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_CorpseExplosion(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_CorpseExplosion.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_DeathBolt(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_DeathBolt.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}

        //public int LevelUpSkill_AdvancedHeal(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_AdvancedHeal.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Purify(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Purify.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_HealingCircle(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_HealingCircle.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_BestowMight(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_BestowMight.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Resurrection(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Resurrection.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}

        //public int LevelUpSkill_BardTraining(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_BardTraining.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Entertain(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Entertain.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Inspire(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Inspire.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Lullaby(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Lullaby.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_BattleHymn(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_BattleHymn.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}

        //public int LevelUpSkill_SoulBond(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_SoulBond.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_ShadowBolt(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_ShadowBolt.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Dominate(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Dominate.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Attraction(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Attraction.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Repulsion(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Repulsion.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Scorn(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Scorn.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_PsychicShock(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_PsychicShock.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}

        //public int LevelUpSkill_Stoneskin(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Stoneskin.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Encase(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Encase.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_EarthSprites(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_EarthSprites.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_EarthernHammer(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_EarthernHammer.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Meteor(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Meteor.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Sentinel(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Sentinel.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_TechnoBit(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_TechnoBit.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_TechnoTurret(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_TechnoTurret.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_TechnoWeapon(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_TechnoWeapon.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_TechnoShield(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_TechnoShield.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Sabotage(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Sabotage.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Overdrive(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Overdrive.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_OrbitalStrike(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_OrbitalStrike.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_BloodGift(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_BloodGift.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_IgniteBlood(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_IgniteBlood.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_BloodForBlood(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_BloodForBlood.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_BloodShield(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_BloodShield.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Rend(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Rend.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_BloodMoon(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_BloodMoon.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_EnchantedBody(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_EnchantedBody.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Transmutate(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Transmutate.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_EnchanterStone(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_EnchanterStone.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_EnchantWeapon(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_EnchantWeapon.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Polymorph(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Polymorph.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Shapeshift(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Shapeshift.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Prediction(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Prediction.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_AlterFate(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_AlterFate.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_AccelerateTime(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_AccelerateTime.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_ReverseTime(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_ReverseTime.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_ChronostaticField(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_ChronostaticField.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Recall(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Recall.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_ChaosTradition(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_ChaosTradition.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_WandererCraft(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}
        //public int LevelUpSkill_Cantrips(string skillName)
        //{
        //    int result = 0;
        //    MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Cantrips.FirstOrDefault(mps => mps.label == skillName);
        //    bool flag = magicPowerSkill != null;
        //    if (flag)
        //    {
        //        result = magicPowerSkill.level;
        //    }
        //    return result;
        //}

        private void SingleEvent()
        {
            doOnce = false;
        }

        private void DoOncePerLoad()
        {
            if (spell_FertileLands)
            {
                if (fertileLands.Count > 0)
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
                    RemovePawnAbility(TorannMagicDefOf.TM_FertileLands);
                    AddPawnAbility(TorannMagicDefOf.TM_DismissFertileLands);
                }
            }
            //to fix filtering of succubus abilities
            if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus))
            {
                for(int i = 0; i < MagicData.MagicPowersWD.Count; i++)
                {
                    MagicPower wd = MagicData.MagicPowersWD[i];
                    if (wd.learned && wd.abilityDef == TorannMagicDefOf.TM_SoulBond)
                    {
                        MagicData.MagicPowersSD.First(mp => mp.abilityDef == TorannMagicDefOf.TM_SoulBond).learned = true;
                    }
                    else if(wd.learned && wd.abilityDef == TorannMagicDefOf.TM_ShadowBolt)
                    {
                        MagicData.MagicPowersSD.First(mp => mp.abilityDef == TorannMagicDefOf.TM_ShadowBolt).learned = true;
                    }
                    else if (wd.learned && wd.abilityDef == TorannMagicDefOf.TM_Dominate)
                    {
                        MagicData.MagicPowersSD.First(mp => mp.abilityDef == TorannMagicDefOf.TM_Dominate).learned = true;
                    }
                }
            }
        }

        // Helper function handling a Tick when pawn is not spawned.
        private void CompTickNotSpawned()
        {
            // Exit Conditions
            if (Find.TickManager.TicksGame % 600 != 0) return;
            if (Pawn.Map != null) return;
            if (!IsMagicUser) return;
            if (AbilityData?.AllPowers == null || AbilityData.AllPowers.Count <= 0) return;

            // Reduce the cooldowns!
            List<PawnAbility> allPowers = AbilityData.AllPowers;
            for (int i = 0; i < allPowers.Count; i++)
            {
                allPowers[i].CooldownTicksLeft = Math.Max(allPowers[i].CooldownTicksLeft - 600, 0);
            }
        }

        // Helper function for handling a Tick when the Pawn is a colonist
        private void CompTickColonistPawn()
        {
            if (!magicPowersInitializedForColonist)
                ResolveFactionChange();

            ResolveEnchantments();
            ResolveMinions();
            ResolveSustainers();
            if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || Pawn.story.traits.HasTrait(TorannMagicDefOf.Lich) || (customClass != null && customClass.isNecromancer))
            {
                ResolveUndead();
            }
            ResolveEffecter();
            ResolveClassSkills();
            ResolveSpiritOfLight();
            ResolveChronomancerTimeMark();
        }

        public override void CompTick()
        {
            Pawn pawn = Pawn;
            if (pawn == null) return;
            if (!pawn.Spawned)
            {
                CompTickNotSpawned();
                return;
            }
            if (pawn.story == null) return;

            if (IsMagicUser && !pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) && !pawn.IsWildMan())
            {
                if (!firstTick)
                    PostInitializeTick();
                if (doOnce)
                    SingleEvent();
                base.CompTick();

                age++;
                if(chainedAbilitiesList != null && chainedAbilitiesList.Count > 0)
                {
                    for(int i = 0; i < chainedAbilitiesList.Count; i++)
                    {
                        chainedAbilitiesList[i].expirationTicks--;
                        if (chainedAbilitiesList[i].expires && chainedAbilitiesList[i].expirationTicks <= 0)
                        {
                            RemovePawnAbility(chainedAbilitiesList[i].abilityDef);
                            chainedAbilitiesList.Remove(chainedAbilitiesList[i]);
                            break;
                        }
                    }
                }
                if (Mana != null)
                {
                    if (Find.TickManager.TicksGame % 4 == 0 && pawn.CurJobDef == JobDefOf.DoBill && pawn.CurJob?.targetA.Thing != null)
                    {
                        DoArcaneForging();
                    }
                    if (Mana.CurLevel >= .99f * Mana.MaxLevel)
                    {
                        if (age > lastXPGain + magicXPRate)
                        {
                            MagicData.MagicUserXP++;
                            lastXPGain = age;
                        }
                    }

                    int tickModulo60 = Find.TickManager.TicksGame % 60;
                    switch (tickModulo60)  // Switch is fastest here.
                    {
                        case 0:
                            CheckLevelUp();
                            if (pawn.IsColonist)
                                CompTickColonistPawn();
                            else
                                magicPowersInitializedForColonist = false;
                            break;
                        case 30:
                            CheckLevelUp();
                            break;
                    }
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    if (autocastTick < Find.TickManager.TicksGame)  //180 default
                    {
                        if (!pawn.Dead && !pawn.Downed && pawn.Map != null && pawn.story?.traits != null && MagicData != null && AbilityData != null && !pawn.InMentalState)
                        {
                            if (pawn.IsColonist)
                            {
                                autocastTick = Find.TickManager.TicksGame + (int)Rand.Range(.8f * settingsRef.autocastEvaluationFrequency, 1.2f * settingsRef.autocastEvaluationFrequency);
                                ResolveAutoCast();
                            }
                            else if(settingsRef.AICasting && (!pawn.IsPrisoner || pawn.IsFighting()) && (pawn.guest != null && !pawn.IsSlave))
                            {
                                float tickMult = settingsRef.AIAggressiveCasting ? 1f : 2f;
                                autocastTick = Find.TickManager.TicksGame + (int)(Rand.Range(.8f * settingsRef.autocastEvaluationFrequency, 1.2f * settingsRef.autocastEvaluationFrequency) * tickMult);
                                ResolveAIAutoCast();
                            }
                        }
                    }
                    if (!pawn.IsColonist && settingsRef.AICasting && settingsRef.AIAggressiveCasting && Find.TickManager.TicksGame > nextAICastAttemptTick) //Aggressive AI Casting
                    {
                        nextAICastAttemptTick = Find.TickManager.TicksGame + Rand.Range(300, 500);
                        if (pawn.jobs != null && pawn.CurJobDef != TorannMagicDefOf.TMCastAbilitySelf && pawn.CurJobDef != TorannMagicDefOf.TMCastAbilityVerb)
                        {
                            IEnumerable<AbilityUserAIProfileDef> enumerable = pawn.EligibleAIProfiles();
                            foreach (AbilityUserAIProfileDef item in enumerable)
                            {
                                if (item == null) continue;

                                AbilityAIDef useThisAbility = null;
                                if (item.decisionTree != null)
                                    useThisAbility = item.decisionTree.RecursivelyGetAbility(pawn);
                                if (useThisAbility == null) continue;

                                CompAbilityUser compAbilityUser = pawn.AllComps.First(
                                    comp => comp.GetType() == item.compAbilityUserClass) as CompAbilityUser;
                                if (compAbilityUser == null) continue;

                                PawnAbility pawnAbility = compAbilityUser.AbilityData.AllPowers.First(ability => ability.Def == useThisAbility.ability);
                                if (!pawnAbility.CanCastPowerCheck(AbilityContext.AI, out _)) continue;

                                LocalTargetInfo target = useThisAbility.Worker.TargetAbilityFor(useThisAbility, pawn);
                                if (target.IsValid)
                                    pawnAbility.UseAbility(AbilityContext.Player, target);
                            }
                        }
                    }
                }
                if (Find.TickManager.TicksGame % overdriveFrequency == 0)
                {
                    if (pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || CombinedCustomAbilities.Contains(TorannMagicDefOf.TM_Overdrive))
                    {
                        ResolveTechnomancerOverdrive();
                    }
                }

                // Spread out these events once or twice every 600 ticks (but never on the same tick)
                int tickModulo600 = Find.TickManager.TicksGame % 600;
                switch (tickModulo600)
                {
                    case 1:
                        if (pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock))
                            ResolveWarlockEmpathy();
                        break;
                    case 2:
                        ResolveMagicUseEvents();
                        break;
                    case 299:
                    case 599:
                        weaponDamage = TM_Calc.GetSkillDamage(pawn);
                        break;
                }
                if (Find.TickManager.TicksGame % 2001 == 0)
                {
                    if (pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus))
                    {
                        ResolveSuccubusLovin();
                    }
                }
                if (deathRetaliating)
                {
                    DoDeathRetaliation();
                }
                else if (Find.TickManager.TicksGame % 67 == 0 && !pawn.IsColonist && pawn.Downed)
                {
                    DoDeathRetaliation();
                }
            }
            else
            {
                if (
                    Find.TickManager.TicksGame % 2501 != 0
                    || pawn.story == null
                    || !pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted)
                ) return;

                if (!pawn.Inspired && pawn.CurJobDef == JobDefOf.LayDown && Rand.Chance(.025f))
                {
                    pawn.mindState.inspirationHandler.TryStartInspiration(TorannMagicDefOf.ID_ArcanePathways);
                }
            }
        }

        private int deathRetaliationDelayCount;
        public void DoDeathRetaliation()
        {
            if (!Pawn.Downed || Pawn.Map == null || Pawn.IsPrisoner || Pawn.Faction == null || !Pawn.Faction.HostileTo(Faction.OfPlayerSilentFail))
            {
                deathRetaliating = false;
                canDeathRetaliate = false;
                deathRetaliationDelayCount = 0;
            }
            if (canDeathRetaliate && deathRetaliating)
            {
                ticksTillRetaliation--;
                if (deathRing == null || deathRing.Count < 1)
                {
                    deathRing = TM_Calc.GetOuterRing(Pawn.Position, 1f, 2f);
                }
                if (Find.TickManager.TicksGame % 6 == 0)
                {
                    Vector3 moteVec = deathRing.RandomElement().ToVector3Shifted();
                    moteVec.x += Rand.Range(-.4f, .4f);
                    moteVec.z += Rand.Range(-.4f, .4f);
                    float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(moteVec, Pawn.DrawPos)).ToAngleFlat();
                    ThingDef mote = TorannMagicDefOf.Mote_Psi_Grayscale;
                    mote.graphicData.color = Color.white;
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi_Grayscale, moteVec, Pawn.Map, Rand.Range(.25f, .6f), .1f, .05f, .05f, 0, Rand.Range(4f, 6f), angle, angle);
                }
                if (ticksTillRetaliation <= 0)
                {
                    canDeathRetaliate = false;
                    deathRetaliating = false;
                    TM_Action.CreateMagicDeathEffect(Pawn, Pawn.Position);
                }
            }
            else if (canDeathRetaliate)
            {
                if (deathRetaliationDelayCount >= 20 && Rand.Value < .04f)
                {
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    deathRetaliating = true;
                    ticksTillRetaliation = Mathf.RoundToInt(Rand.Range(400, 1200) * settingsRef.deathRetaliationDelayFactor);
                    deathRing = TM_Calc.GetOuterRing(Pawn.Position, 1f, 2f);
                }
                else
                {
                    deathRetaliationDelayCount++;
                }
            }
        }

        public void PostInitializeTick()
        {
            if (Pawn == null) return;
            if (!Pawn.Spawned) return;
            if (Pawn.story == null) return;

            Trait t = Pawn.story.traits.GetTrait(TorannMagicDefOf.TM_Possessed);
            if (t != null && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SpiritPossessionHD))
            {
                Pawn.story.traits.RemoveTrait(t);
            }
            else
            {
                firstTick = true;
                Initialize();
                ResolveMagicTab();
                ResolveMagicPowers();
                ResolveMana();
                DoOncePerLoad();
            }
        }

        private bool cachedIsMagicUser;
        private int isMagicUserCacheTick = -1;
        public bool IsMagicUser 
        {
            get
            {
                if (Pawn?.story == null) return false;
                if (customClass != null) return true;

                int tick = Find.TickManager.TicksGame;
                if (isMagicUserCacheTick == tick) return cachedIsMagicUser;

                isMagicUserCacheTick = tick;
                if (customClass == null && customIndex == -2)
                {
                    customIndex = TM_ClassUtility.CustomClassIndexOfBaseMageClass(Pawn.story.traits.allTraits);
                    if (customIndex >= 0)
                    {
                        TM_CustomClass foundCustomClass = TM_ClassUtility.CustomClasses[customIndex];
                        if (!foundCustomClass.isMage)
                        {
                            customIndex = -1;
                            return cachedIsMagicUser = false;
                        }
                        customClass = foundCustomClass;
                        return cachedIsMagicUser = true;
                    }
                }
                if (Pawn.story.traits.allTraits.Any(t => magicTraitIndexes.Contains(t.def.index) 
                || TM_Calc.IsWanderer(Pawn)
                || (AdvancedClasses != null && AdvancedClasses.Count > 0)))
                {
                    return cachedIsMagicUser = true;
                }

                if (!TM_Calc.HasAdvancedClass(Pawn)) return cachedIsMagicUser = false;

                bool hasMageAdvClass = false;
                foreach(TM_CustomClass cc in TM_ClassUtility.GetAdvancedClassesForPawn(Pawn))
                {
                    if (!cc.isMage) continue;

                    AdvancedClasses.Add(cc);
                    hasMageAdvClass = true;
                }
                if(hasMageAdvClass)
                {
                    return cachedIsMagicUser = true;
                }
                return cachedIsMagicUser = false;
            }
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
                return val;
            }
            return cacheXPFL[lvl];
        }

        public int MagicUserLevel
        {
            get => MagicData.MagicUserLevel;
            set
            {
                if (value > MagicData.MagicUserLevel)
                {
                    MagicData.MagicAbilityPoints++;
                    int XPForLevel = GetXPForLevel(value - 1);
                    if (MagicData.MagicUserXP < XPForLevel)
                    {
                        MagicData.MagicUserXP = XPForLevel;
                    }
                }
                MagicData.MagicUserLevel = value;
            }
        }

        public int MagicUserXP
        {
            get => MagicData.MagicUserXP;
            set => MagicData.MagicUserXP = value;
        }
        
        public float XPLastLevel
        {
            get
            {
                float result = 0f;
                if (MagicUserLevel > 0)
                {
                    
                    result = GetXPForLevel(MagicUserLevel - 1);
                }
                return result;
            }
        }

        public float XPTillNextLevelPercent => (MagicData.MagicUserXP - XPLastLevel) / (MagicUserXPTillNextLevel - XPLastLevel);

        public int MagicUserXPTillNextLevel
        {
            get
            {
                if(MagicUserXP < XPLastLevel)
                {
                    MagicUserXP = (int)XPLastLevel;
                }
                return GetXPForLevel(MagicUserLevel);
            }
        }

        public void CheckLevelUp(bool hideNotification = false, bool forceLevelUp = false)
        {
            if (MagicUserXP <= MagicUserXPTillNextLevel && !forceLevelUp) return;
            if (Pawn.story.traits.allTraits.Any(t =>
                    t.def == TorannMagicDefOf.Faceless  || t.def == TorannMagicDefOf.TM_Wayfarer))
                return;

            if (MagicUserLevel < (customClass?.maxMageLevel ?? 200))
            {
                MagicUserLevel++;
                if (hideNotification) return;

                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (Pawn.IsColonist && settingsRef.showLevelUpMessage)
                {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("TM_MagicLevelUp",
                        parent.Label
                    ), Pawn, MessageTypeDefOf.PositiveEvent);
                }
            }
            else
            {
                MagicUserXP = (int)XPLastLevel;
            }
        }

        public void LevelUpPower(MagicPower power)
        {
            foreach (AbilityUser.AbilityDef current in power.TMabilityDefs)
            {
                RemovePawnAbility(current);
            }
            power.level++;
            AddPawnAbility(power.abilityDef);
            UpdateAbilities();
        }

        public Need_Mana Mana
        {
            get
            {
                if (!Pawn.DestroyedOrNull() && !Pawn.Dead)
                {
                    return Pawn.needs.TryGetNeed<Need_Mana>();
                }
                return null;
            }
        }

        public void ResolveFactionChange()
        {
            if (!colonistPowerCheck)
            {
                RemovePowers();
                spell_BattleHymn = false;
                RemovePawnAbility(TorannMagicDefOf.TM_BattleHymn);
                spell_Blizzard = false;
                RemovePawnAbility(TorannMagicDefOf.TM_Blizzard);
                spell_BloodMoon = false;
                RemovePawnAbility(TorannMagicDefOf.TM_BloodMoon);
                spell_EyeOfTheStorm = false;
                RemovePawnAbility(TorannMagicDefOf.TM_EyeOfTheStorm);
                spell_Firestorm = false;
                RemovePawnAbility(TorannMagicDefOf.TM_Firestorm);
                spell_FoldReality = false;
                RemovePawnAbility(TorannMagicDefOf.TM_FoldReality);
                spell_HolyWrath = false;
                RemovePawnAbility(TorannMagicDefOf.TM_HolyWrath);
                spell_LichForm = false;
                RemovePawnAbility(TorannMagicDefOf.TM_BattleHymn);
                spell_Meteor = false;
                RemovePawnAbility(TorannMagicDefOf.TM_Meteor);
                spell_OrbitalStrike = false;
                RemovePawnAbility(TorannMagicDefOf.TM_OrbitalStrike);
                spell_PsychicShock = false;
                RemovePawnAbility(TorannMagicDefOf.TM_PsychicShock);
                spell_RegrowLimb = false;
                spell_Resurrection = false;
                spell_Scorn = false;
                RemovePawnAbility(TorannMagicDefOf.TM_Scorn);
                spell_Shapeshift = false;
                spell_SummonPoppi = false;
                RemovePawnAbility(TorannMagicDefOf.TM_SummonPoppi);
                RemovePawnAbility(TorannMagicDefOf.TM_Recall);
                spell_Recall = false;
                RemovePawnAbility(TorannMagicDefOf.TM_LightSkipGlobal);
                RemovePawnAbility(TorannMagicDefOf.TM_LightSkipMass);
                RemovePawnAbility(TorannMagicDefOf.TM_SpiritOfLight);
                AssignAbilities();
            }
            magicPowersInitializedForColonist = true;
            colonistPowerCheck = true;
        }

        public override void PostInitialize()
        {
            base.PostInitialize();
            if (MagicAbilities != null) return;

            if (magicPowersInitialized == false && MagicData != null)
            {
                MagicData.MagicUserLevel = 0;
                MagicData.MagicAbilityPoints = 0;
                AssignAbilities();
                if (!Pawn.IsColonist)
                {
                    InitializeSpell();
                    colonistPowerCheck = false;
                }
            }
            magicPowersInitialized = true;
            UpdateAbilities();
        }

        private void AssignAbilities()
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            float hardModeMasterChance = .35f;
            float masterChance = .05f;
            HashSet<TMAbilityDef> usedAbilities = new HashSet<TMAbilityDef>();

            if (Pawn?.story?.traits == null) return;

            if (customClass != null)
            {
                for (int z = 0; z < MagicData.AllMagicPowers.Count; z++)
                {
                    TMAbilityDef ability = (TMAbilityDef)MagicData.AllMagicPowers[z].abilityDef;
                    if (usedAbilities.Contains(ability))
                        continue;
                    usedAbilities.Add(ability);

                    if (customClass.classMageAbilities.Contains(ability))
                    {
                        MagicData.AllMagicPowers[z].learned = true;
                    }
                    //if (MagicData.AllMagicPowers[z] == MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SoulBond) ||
                    //MagicData.AllMagicPowers[z] == MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ShadowBolt) ||
                    //MagicData.AllMagicPowers[z] == MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Dominate))
                    //{
                    //    MagicData.AllMagicPowers[z].learned = false;
                    //}
                    if (MagicData.AllMagicPowers[z].requiresScroll)
                    {
                        MagicData.AllMagicPowers[z].learned = false;
                    }
                    if (!Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty) && !Rand.Chance(ability.learnChance))
                    {
                        MagicData.AllMagicPowers[z].learned = false;
                    }
                    if (MagicData.AllMagicPowers[z].learned)
                    {
                        if (ability.shouldInitialize)
                        {
                            AddPawnAbility(ability);
                        }
                        if (ability.childAbilities != null && ability.childAbilities.Count > 0)
                        {
                            for (int c = 0; c < ability.childAbilities.Count; c++)
                            {
                                if (ability.childAbilities[c].shouldInitialize)
                                {
                                    AddPawnAbility(ability.childAbilities[c]);
                                }
                            }
                        }
                    }
                }
                MagicPower branding = MagicData.AllMagicPowers.FirstOrDefault((MagicPower p) => p.abilityDef == TorannMagicDefOf.TM_Branding);
                if(branding != null && branding.learned && Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Golemancer))
                {
                    int count = 0;
                    while (count < 2)
                    {
                        TMAbilityDef tmpAbility = TM_Data.BrandList().RandomElement();
                        for (int i = 0; i < MagicData.AllMagicPowers.Count; i++)
                        {
                            TMAbilityDef ad = (TMAbilityDef)MagicData.AllMagicPowers[i].abilityDef;
                            if (MagicData.AllMagicPowers[i].learned || ad != tmpAbility) continue;

                            count++;
                            MagicData.AllMagicPowers[i].learned = true;
                            RemovePawnAbility(ad);
                            TryAddPawnAbility(ad);
                        }
                    }
                }
                if (customClass.classHediff != null)
                {
                    HealthUtility.AdjustSeverity(Pawn, customClass.classHediff, customClass.hediffSeverity);
                }
            }
            else
            {
                //for (int z = 0; z < MagicData.AllMagicPowers.Count; z++)
                //{
                //    MagicData.AllMagicPowers[z].learned = false;
                //}
                if (TM_Calc.IsWanderer(Pawn))
                {
                    //Log.Message("Initializing Wanderer Abilities");
                    MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Cantrips).learned = true;
                    magicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_WandererCraft).learned = true;
                    for (int i = 0; i < 3; i++)
                    {
                        MagicPower magicPower = MagicData.MagicPowersStandalone.RandomElement();
                        if (magicPower.abilityDef == TorannMagicDefOf.TM_TransferMana)
                        {
                            magicPower.learned = true;
                            spell_TransferMana = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_SiphonMana)
                        {
                            magicPower.learned = true;
                            spell_SiphonMana = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_SpellMending)
                        {
                            magicPower.learned = true;
                            spell_SpellMending = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_DirtDevil)
                        {
                            magicPower.learned = true;
                            spell_DirtDevil = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_Heater)
                        {
                            magicPower.learned = true;
                            spell_Heater = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_Cooler)
                        {
                            magicPower.learned = true;
                            spell_Cooler = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_PowerNode)
                        {
                            magicPower.learned = true;
                            spell_PowerNode = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_Sunlight)
                        {
                            magicPower.learned = true;
                            spell_Sunlight = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_SmokeCloud)
                        {
                            magicPower.learned = true;
                            spell_SmokeCloud = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_Extinguish)
                        {
                            magicPower.learned = true;
                            spell_Extinguish = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_EMP)
                        {
                            magicPower.learned = true;
                            spell_EMP = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_ManaShield)
                        {
                            magicPower.learned = true;
                            spell_ManaShield = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_Blur)
                        {
                            magicPower.learned = true;
                            spell_Blur = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_ArcaneBolt)
                        {
                            magicPower.learned = true;
                            spell_ArcaneBolt = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_LightningTrap)
                        {
                            magicPower.learned = true;
                            spell_LightningTrap = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_Invisibility)
                        {
                            magicPower.learned = true;
                            spell_Invisibility = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_MageLight)
                        {
                            magicPower.learned = true;
                            spell_MageLight = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_Ignite)
                        {
                            magicPower.learned = true;
                            spell_Ignite = true;
                        }
                        else if (magicPower.abilityDef == TorannMagicDefOf.TM_SnapFreeze)
                        {
                            magicPower.learned = true;
                            spell_SnapFreeze = true;
                        }
                        else
                        {
                            int rnd = Rand.RangeInclusive(0, 4);
                            switch (rnd)
                            {
                                case 0:
                                    MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Heal).learned = true;
                                    spell_Heal = true;
                                    break;
                                case 1:
                                    MagicData.MagicPowersA.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Blink).learned = true;
                                    spell_Blink = true;
                                    break;
                                case 2:
                                    MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Rainmaker).learned = true;
                                    spell_Rain = true;
                                    break;
                                case 3:
                                    MagicData.MagicPowersS.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SummonMinion).learned = true;
                                    spell_SummonMinion = true;
                                    break;
                                case 4:
                                    MagicData.MagicPowersA.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Teleport).learned = true;
                                    spell_Teleport = true;
                                    break;
                            }
                        }
                    }
                    if (!Pawn.IsColonist)
                    {
                        spell_ArcaneBolt = true;
                        AddPawnAbility(TorannMagicDefOf.TM_ArcaneBolt);
                    }
                    InitializeSpell();
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.InnerFire))
                {
                    //Log.Message("Initializing Inner Fire Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_RayofHope);
                            MagicData.MagicPowersIF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_RayofHope).learned = true;
                        }
                        if (Rand.Chance(.6f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Firebolt);
                            MagicData.MagicPowersIF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Firebolt).learned = true;
                        }
                        if (Rand.Chance(.4f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Fireclaw);
                            MagicData.MagicPowersIF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Fireclaw).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Fireball);
                            MagicData.MagicPowersIF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Fireball).learned = true;
                        }
                        MagicData.MagicPowersIF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Firestorm).learned = false;
                    }
                    else
                    {
                        MagicData.MagicPowersIF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_RayofHope).learned = true;
                        AddPawnAbility(TorannMagicDefOf.TM_RayofHope);
                        MagicData.MagicPowersIF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Firebolt).learned = true;
                        AddPawnAbility(TorannMagicDefOf.TM_Firebolt);
                        MagicData.MagicPowersIF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Fireclaw).learned = true;
                        AddPawnAbility(TorannMagicDefOf.TM_Fireclaw);
                        MagicData.MagicPowersIF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Fireball).learned = true;
                        AddPawnAbility(TorannMagicDefOf.TM_Fireball);
                        MagicData.MagicPowersIF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Firestorm).learned = false;

                        if (!Pawn.IsColonist)
                        {
                            if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                            {
                                spell_Firestorm = true;
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost))
                {
                    //Log.Message("Initializing Heart of Frost Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Soothe);
                            MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Soothe).learned = true;
                        }
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Icebolt);
                            MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Icebolt).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Snowball);
                            MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Snowball).learned = true;
                        }
                        if (Rand.Chance(.4f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_FrostRay);
                            MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_FrostRay).learned = true;
                        }
                        if (Rand.Chance(.7f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Rainmaker);
                            MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Rainmaker).learned = true;
                            spell_Rain = true;
                        }
                        MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Blizzard).learned = false;
                    }
                    else
                    {
                        MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Soothe).learned = true;
                        AddPawnAbility(TorannMagicDefOf.TM_Soothe);
                        MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Icebolt).learned = true;
                        AddPawnAbility(TorannMagicDefOf.TM_Icebolt);
                        MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Snowball).learned = true;
                        AddPawnAbility(TorannMagicDefOf.TM_Snowball);
                        MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_FrostRay).learned = true;
                        AddPawnAbility(TorannMagicDefOf.TM_FrostRay);
                        MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Rainmaker).learned = true;
                        AddPawnAbility(TorannMagicDefOf.TM_Rainmaker);
                        spell_Rain = true;
                        MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Blizzard).learned = false;

                        if (!Pawn.IsColonist)
                        {
                            if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                            {
                                spell_Blizzard = true;
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.StormBorn))
                {
                    //Log.Message("Initializing Storm Born Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_AMP);
                            MagicData.MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_AMP).learned = true;
                        }
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_LightningBolt);
                            MagicData.MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_LightningBolt).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_LightningCloud);
                            MagicData.MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_LightningCloud).learned = true;
                        }
                        if (Rand.Chance(.2f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_LightningStorm);
                            MagicData.MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_LightningStorm).learned = true;
                        }
                        MagicData.MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EyeOfTheStorm).learned = false;
                    }
                    else
                    {
                        AddPawnAbility(TorannMagicDefOf.TM_AMP);
                        MagicData.MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_AMP).learned = true;
                        AddPawnAbility(TorannMagicDefOf.TM_LightningBolt);
                        MagicData.MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_LightningBolt).learned = true;
                        AddPawnAbility(TorannMagicDefOf.TM_LightningCloud);
                        MagicData.MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_LightningCloud).learned = true;
                        AddPawnAbility(TorannMagicDefOf.TM_LightningStorm);
                        MagicData.MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_LightningStorm).learned = true;
                        MagicData.MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EyeOfTheStorm).learned = false;

                        if (!Pawn.IsColonist)
                        {
                            if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                            {
                                spell_EyeOfTheStorm = true;
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist))
                {
                    //Log.Message("Initializing Arcane Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Shadow);
                            MagicData.MagicPowersA.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Shadow).learned = true;
                        }
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_MagicMissile);
                            MagicData.MagicPowersA.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_MagicMissile).learned = true;
                        }
                        if (Rand.Chance(.7f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Blink);
                            MagicData.MagicPowersA.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Blink).learned = true;
                            spell_Blink = true;
                        }
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Summon);
                            MagicData.MagicPowersA.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Summon).learned = true;
                        }
                        if (Rand.Chance(.2f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Teleport);
                            MagicData.MagicPowersA.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Teleport).learned = true;
                            spell_Teleport = true;
                        }
                        MagicData.MagicPowersA.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_FoldReality).learned = false;
                    }
                    else
                    {
                        for(int i = 0; i < MagicData.MagicPowersA.Count; i++)
                        {
                            if (MagicData.magicPowerA[i].abilityDef != TorannMagicDefOf.TM_FoldReality)
                            {
                                MagicData.MagicPowersA[i].learned = true;
                            }
                        }
                        AddPawnAbility(TorannMagicDefOf.TM_Shadow);
                        AddPawnAbility(TorannMagicDefOf.TM_MagicMissile);
                        AddPawnAbility(TorannMagicDefOf.TM_Blink);
                        AddPawnAbility(TorannMagicDefOf.TM_Summon);
                        AddPawnAbility(TorannMagicDefOf.TM_Teleport);  //Pending Redesign (graphics?)
                        spell_Blink = true;
                        spell_Teleport = true;

                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin))
                {
                    //Log.Message("Initializing Paladin Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if(Rand.Chance(TorannMagicDefOf.TM_P_RayofHope.learnChance))
                        {
                            MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_P_RayofHope).learned = true;
                            AddPawnAbility(TorannMagicDefOf.TM_P_RayofHope);
                        }
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Heal);
                            MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Heal).learned = true;
                            spell_Heal = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Shield);
                            MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Shield).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_ValiantCharge);
                            MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ValiantCharge).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Overwhelm);
                            MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Overwhelm).learned = true;
                        }
                        MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_HolyWrath).learned = false;
                    }
                    else
                    {
                        for (int i = 0; i < MagicData.MagicPowersP.Count; i++)
                        {
                            if (MagicData.MagicPowersP[i].abilityDef != TorannMagicDefOf.TM_HolyWrath)
                            {
                                MagicData.MagicPowersP[i].learned = true;
                            }
                        }
                        AddPawnAbility(TorannMagicDefOf.TM_Heal);
                        AddPawnAbility(TorannMagicDefOf.TM_Shield);
                        AddPawnAbility(TorannMagicDefOf.TM_ValiantCharge);
                        AddPawnAbility(TorannMagicDefOf.TM_Overwhelm);
                        AddPawnAbility(TorannMagicDefOf.TM_P_RayofHope);
                        spell_Heal = true;

                        if (!Pawn.IsColonist)
                        {
                            if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                            {
                                spell_HolyWrath = true;
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Summoner))
                {
                    //Log.Message("Initializing Summoner Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_SummonMinion);
                            MagicData.MagicPowersS.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SummonMinion).learned = true;
                            spell_SummonMinion = true;
                        }
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_SummonPylon);
                            MagicData.MagicPowersS.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SummonPylon).learned = true;
                        }
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_SummonExplosive);
                            MagicData.MagicPowersS.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SummonExplosive).learned = true;
                        }
                        if (Rand.Chance(.2f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_SummonElemental);
                            MagicData.MagicPowersS.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SummonElemental).learned = true;
                        }
                        MagicData.MagicPowersS.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SummonPoppi).learned = false;
                    }
                    else
                    {
                        for (int i = 0; i < MagicData.MagicPowersS.Count; i++)
                        {
                            if (MagicData.MagicPowersS[i].abilityDef != TorannMagicDefOf.TM_SummonPoppi)
                            {
                                MagicData.MagicPowersS[i].learned = true;
                            }
                        }
                        AddPawnAbility(TorannMagicDefOf.TM_SummonMinion);
                        AddPawnAbility(TorannMagicDefOf.TM_SummonPylon);
                        AddPawnAbility(TorannMagicDefOf.TM_SummonExplosive);
                        AddPawnAbility(TorannMagicDefOf.TM_SummonElemental);
                        spell_SummonMinion = true;

                        if (!Pawn.IsColonist)
                        {
                            if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                            {
                                spell_SummonPoppi = true;
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Druid))
                {
                    // Log.Message("Initializing Druid Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(.6f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Poison);
                            MagicData.MagicPowersD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Poison).learned = true;
                        }
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_SootheAnimal);
                            MagicData.MagicPowersD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SootheAnimal).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Regenerate);
                            MagicData.MagicPowersD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Regenerate).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_CureDisease);
                            MagicData.MagicPowersD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_CureDisease).learned = true;
                        }
                        MagicData.MagicPowersD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_RegrowLimb).learned = false;
                    }
                    else
                    {
                        for (int i = 0; i < MagicData.MagicPowersD.Count; i++)
                        {
                            if (MagicData.MagicPowersD[i].abilityDef != TorannMagicDefOf.TM_RegrowLimb)
                            {
                                MagicData.MagicPowersD[i].learned = true;
                            }
                        }
                        AddPawnAbility(TorannMagicDefOf.TM_Poison);
                        AddPawnAbility(TorannMagicDefOf.TM_SootheAnimal);
                        AddPawnAbility(TorannMagicDefOf.TM_Regenerate);
                        AddPawnAbility(TorannMagicDefOf.TM_CureDisease);
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || Pawn.story.traits.HasTrait(TorannMagicDefOf.Lich))
                {
                    //Log.Message("Initializing Necromancer Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_RaiseUndead);
                            MagicData.MagicPowersN.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_RaiseUndead).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_DeathMark);
                            MagicData.MagicPowersN.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_DeathMark).learned = true;
                        }
                        if (Rand.Chance(.4f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_FogOfTorment);
                            MagicData.MagicPowersN.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_FogOfTorment).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_ConsumeCorpse);
                            MagicData.MagicPowersN.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ConsumeCorpse).learned = true;
                        }
                        if (Rand.Chance(.2f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_CorpseExplosion);
                            MagicData.MagicPowersN.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_CorpseExplosion).learned = true;
                        }
                        MagicData.MagicPowersN.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_LichForm).learned = false;
                    }
                    else
                    {
                        for (int i = 0; i < MagicData.MagicPowersN.Count; i++)
                        {
                            if (MagicData.MagicPowersN[i].abilityDef != TorannMagicDefOf.TM_LichForm)
                            {
                                MagicData.MagicPowersN[i].learned = true;
                            }
                        }
                        AddPawnAbility(TorannMagicDefOf.TM_RaiseUndead);
                        AddPawnAbility(TorannMagicDefOf.TM_DeathMark);
                        AddPawnAbility(TorannMagicDefOf.TM_FogOfTorment);
                        AddPawnAbility(TorannMagicDefOf.TM_ConsumeCorpse);
                        AddPawnAbility(TorannMagicDefOf.TM_CorpseExplosion);

                        if (!Pawn.IsColonist)
                        {
                            if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                            {
                                AddPawnAbility(TorannMagicDefOf.TM_DeathBolt);
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Priest))
                {
                    //Log.Message("Initializing Priest Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_AdvancedHeal);
                            MagicData.MagicPowersPR.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_AdvancedHeal).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Purify);
                            MagicData.MagicPowersPR.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Purify).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_HealingCircle);
                            MagicData.MagicPowersPR.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_HealingCircle).learned = true;
                        }
                        if (Rand.Chance(.4f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_BestowMight);
                            MagicData.MagicPowersPR.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BestowMight).learned = true;
                        }
                        MagicData.MagicPowersPR.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Resurrection).learned = false;
                    }
                    else
                    {
                        for (int i = 0; i < MagicData.MagicPowersPR.Count; i++)
                        {
                            if (MagicData.MagicPowersPR[i].abilityDef != TorannMagicDefOf.TM_Resurrection)
                            {
                                MagicData.MagicPowersPR[i].learned = true;
                            }
                        }
                        AddPawnAbility(TorannMagicDefOf.TM_AdvancedHeal);
                        AddPawnAbility(TorannMagicDefOf.TM_Purify);
                        AddPawnAbility(TorannMagicDefOf.TM_HealingCircle);
                        AddPawnAbility(TorannMagicDefOf.TM_BestowMight);
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard))
                {
                    //Log.Message("Initializing Priest Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (true)
                        {
                            MagicData.MagicPowersB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BardTraining).learned = true;
                            MagicData.MagicPowersB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Inspire).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Entertain);
                            MagicData.MagicPowersB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Entertain).learned = true;
                        }
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Lullaby);
                            MagicData.MagicPowersB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Lullaby).learned = true;
                        }
                        MagicData.MagicPowersB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BattleHymn).learned = false;
                    }
                    else
                    {
                        for (int i = 0; i < MagicData.MagicPowersB.Count; i++)
                        {
                            if (MagicData.MagicPowersB[i].abilityDef != TorannMagicDefOf.TM_BattleHymn)
                            {
                                MagicData.MagicPowersB[i].learned = true;
                            }
                        }
                        //AddPawnAbility(TorannMagicDefOf.TM_BardTraining);
                        AddPawnAbility(TorannMagicDefOf.TM_Entertain);
                        //AddPawnAbility(TorannMagicDefOf.TM_Inspire);
                        AddPawnAbility(TorannMagicDefOf.TM_Lullaby);

                        if (!Pawn.IsColonist)
                        {
                            if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                            {
                                spell_BattleHymn = true;
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus))
                {
                    //Log.Message("Initializing Succubus Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(.7f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_SoulBond);
                            MagicData.MagicPowersSD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SoulBond).learned = true;
                        }
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt);
                            MagicData.MagicPowersSD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ShadowBolt).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Dominate);
                            MagicData.MagicPowersSD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Dominate).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Attraction);
                            MagicData.MagicPowersSD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Attraction).learned = true;
                        }
                        MagicData.MagicPowersSD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Scorn).learned = false;
                    }
                    else
                    {
                        for (int i = 0; i < MagicData.MagicPowersSD.Count; i++)
                        {
                            if (MagicData.MagicPowersSD[i].abilityDef != TorannMagicDefOf.TM_Scorn)
                            {
                                MagicData.MagicPowersSD[i].learned = true;
                            }
                        }
                        AddPawnAbility(TorannMagicDefOf.TM_SoulBond);
                        AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt);
                        AddPawnAbility(TorannMagicDefOf.TM_Dominate);
                        AddPawnAbility(TorannMagicDefOf.TM_Attraction);

                        if (!Pawn.IsColonist)
                        {
                            if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                            {
                                spell_Scorn = true;
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock))
                {
                    //Log.Message("Initializing Succubus Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(.7f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_SoulBond);
                            MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SoulBond).learned = true;
                        }
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt);
                            MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ShadowBolt).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Dominate);
                            MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Dominate).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Repulsion);
                            MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Repulsion).learned = true;
                        }
                        MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_PsychicShock).learned = false;
                    }
                    else
                    {
                        for (int i = 0; i < MagicData.MagicPowersWD.Count; i++)
                        {
                            if (MagicData.MagicPowersWD[i].abilityDef != TorannMagicDefOf.TM_PsychicShock)
                            {
                                MagicData.MagicPowersWD[i].learned = true;
                            }
                        }
                        AddPawnAbility(TorannMagicDefOf.TM_SoulBond);
                        AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt);
                        AddPawnAbility(TorannMagicDefOf.TM_Dominate);
                        AddPawnAbility(TorannMagicDefOf.TM_Repulsion);
                        if (!Pawn.IsColonist)
                        {
                            if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                            {
                                spell_PsychicShock = true;
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Geomancer))
                {
                    //Log.Message("Initializing Heart of Geomancer Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(.4f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Stoneskin);
                            MagicData.MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Stoneskin).learned = true;
                        }
                        if (Rand.Chance(.6f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Encase);
                            MagicData.MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Encase).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_EarthSprites);
                            MagicData.MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EarthSprites).learned = true;
                        }
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_EarthernHammer);
                            MagicData.MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EarthernHammer).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Sentinel);
                            MagicData.MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Sentinel).learned = true;
                        }
                        MagicData.MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Meteor).learned = false;
                    }
                    else
                    {
                        for (int i = 0; i < MagicData.MagicPowersG.Count; i++)
                        {
                            if (!MagicData.MagicPowersG[i].abilityDef.defName.StartsWith("TM_Meteor"))
                            {
                                MagicData.MagicPowersG[i].learned = true;
                            }
                        }
                        AddPawnAbility(TorannMagicDefOf.TM_Stoneskin);
                        AddPawnAbility(TorannMagicDefOf.TM_Encase);
                        AddPawnAbility(TorannMagicDefOf.TM_EarthSprites);
                        AddPawnAbility(TorannMagicDefOf.TM_EarthernHammer);
                        AddPawnAbility(TorannMagicDefOf.TM_Sentinel);

                        if (!Pawn.IsColonist)
                        {
                            if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                            {
                                AddPawnAbility(TorannMagicDefOf.TM_Meteor);
                                spell_Meteor = true;
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer))
                {
                    //Log.Message("Initializing Technomancer Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(.4f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_TechnoShield);
                            MagicData.MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_TechnoShield).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Sabotage);
                            MagicData.MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Sabotage).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Overdrive);
                            MagicData.MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Overdrive).learned = true;
                        }
                        MagicData.MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_OrbitalStrike).learned = false;
                        if (Rand.Chance(.2f))
                        {
                            spell_OrbitalStrike = true;
                            MagicData.MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_OrbitalStrike).learned = true;
                            InitializeSpell();
                        }
                    }
                    else
                    {
                        for (int i = 0; i < MagicData.MagicPowersT.Count; i++)
                        {
                            MagicData.MagicPowersT[i].learned = true;
                        }
                        AddPawnAbility(TorannMagicDefOf.TM_TechnoShield);
                        AddPawnAbility(TorannMagicDefOf.TM_Sabotage);
                        AddPawnAbility(TorannMagicDefOf.TM_Overdrive);
                        if (!Pawn.IsColonist)
                        {
                            if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                            {
                                spell_OrbitalStrike = true;
                            }
                        }
                    }
                    MagicData.MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_TechnoBit).learned = false;
                    MagicData.MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_TechnoTurret).learned = false;
                    MagicData.MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_TechnoWeapon).learned = false;
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.BloodMage))
                {
                    //Log.Message("Initializing Heart of Frost Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(1f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_BloodGift);
                            MagicData.MagicPowersBM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BloodGift).learned = true;
                        }
                        if (Rand.Chance(.4f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_IgniteBlood);
                            MagicData.MagicPowersBM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_IgniteBlood).learned = true;
                        }
                        if (Rand.Chance(.4f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_BloodForBlood);
                            MagicData.MagicPowersBM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BloodForBlood).learned = true;
                        }
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_BloodShield);
                            MagicData.MagicPowersBM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BloodShield).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Rend);
                            MagicData.MagicPowersBM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Rend).learned = true;
                        }
                        MagicData.MagicPowersBM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BloodMoon).learned = false;
                    }
                    else
                    {
                        for (int i = 0; i < MagicData.MagicPowersBM.Count; i++)
                        {
                            if (!MagicData.MagicPowersBM[i].abilityDef.defName.StartsWith("TM_BloodMoon"))
                            {
                                MagicData.MagicPowersBM[i].learned = true;
                            }
                        }
                        AddPawnAbility(TorannMagicDefOf.TM_BloodGift);
                        AddPawnAbility(TorannMagicDefOf.TM_IgniteBlood);
                        AddPawnAbility(TorannMagicDefOf.TM_BloodForBlood);
                        AddPawnAbility(TorannMagicDefOf.TM_BloodShield);
                        AddPawnAbility(TorannMagicDefOf.TM_Rend);
                        if (!Pawn.IsColonist)
                        {
                            if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                            {
                                AddPawnAbility(TorannMagicDefOf.TM_BloodMoon);
                                spell_BloodMoon = true;
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter))
                {
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(.5f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_EnchantedBody);
                            AddPawnAbility(TorannMagicDefOf.TM_EnchantedAura);
                            MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EnchantedAura).learned = true;
                            MagicData.MagicPowersE.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EnchantedBody).learned = true;
                            spell_EnchantedAura = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Transmutate);
                            MagicData.MagicPowersE.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Transmutate).learned = true;
                        }
                        if (Rand.Chance(.4f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_EnchanterStone);
                            MagicData.MagicPowersE.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EnchanterStone).learned = true;
                        }
                        if (Rand.Chance(.4f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_EnchantWeapon);
                            MagicData.MagicPowersE.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EnchantWeapon).learned = true;
                        }
                        if (Rand.Chance(.4f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Polymorph);
                            MagicData.MagicPowersE.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Polymorph).learned = true;
                        }
                        MagicData.MagicPowersE.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Shapeshift).learned = false;
                    }
                    else
                    {
                        for (int i = 0; i < MagicData.MagicPowersE.Count; i++)
                        {
                            if (MagicData.MagicPowersE[i].abilityDef != TorannMagicDefOf.TM_Shapeshift)
                            {
                                MagicData.MagicPowersE[i].learned = true;
                            }
                        }
                        MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EnchantedAura).learned = true;
                        AddPawnAbility(TorannMagicDefOf.TM_EnchantedBody);
                        AddPawnAbility(TorannMagicDefOf.TM_EnchantedAura);
                        spell_EnchantedAura = true;
                        AddPawnAbility(TorannMagicDefOf.TM_Transmutate);
                        AddPawnAbility(TorannMagicDefOf.TM_EnchanterStone);
                        AddPawnAbility(TorannMagicDefOf.TM_EnchantWeapon);
                        AddPawnAbility(TorannMagicDefOf.TM_Polymorph);
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer))
                {
                    //Log.Message("Initializing Chronomancer Abilities");
                    if (Pawn.IsColonist && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty))
                    {
                        if (Rand.Chance(.4f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Prediction);
                            MagicData.MagicPowersC.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Prediction).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_AlterFate);
                            MagicData.MagicPowersC.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_AlterFate).learned = true;
                        }
                        if (Rand.Chance(.4f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_AccelerateTime);
                            MagicData.MagicPowersC.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_AccelerateTime).learned = true;
                        }
                        if (Rand.Chance(.4f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_ReverseTime);
                            MagicData.MagicPowersC.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ReverseTime).learned = true;
                        }
                        if (Rand.Chance(.3f))
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_ChronostaticField);
                            MagicData.MagicPowersC.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ChronostaticField).learned = true;
                        }
                        MagicData.MagicPowersC.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Recall).learned = false;
                    }
                    else
                    {
                        for (int i = 0; i < MagicData.MagicPowersC.Count; i++)
                        {
                            if (MagicData.MagicPowersC[i].abilityDef == TorannMagicDefOf.TM_Recall)
                            {
                                MagicData.MagicPowersC[i].learned = true;
                            }
                        }
                        AddPawnAbility(TorannMagicDefOf.TM_Prediction);
                        AddPawnAbility(TorannMagicDefOf.TM_AlterFate);
                        AddPawnAbility(TorannMagicDefOf.TM_AccelerateTime);
                        AddPawnAbility(TorannMagicDefOf.TM_ReverseTime);
                        AddPawnAbility(TorannMagicDefOf.TM_ChronostaticField);

                        if (!Pawn.IsColonist)
                        {
                            if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                            {
                                AddPawnAbility(TorannMagicDefOf.TM_Recall);
                                spell_Recall = true;
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
                {
                    foreach (MagicPower current in MagicData.AllMagicPowers)
                    {
                        if (current.abilityDef != TorannMagicDefOf.TM_ChaosTradition)
                        {
                            current.learned = false;
                        }
                    }
                    MagicData.MagicPowersCM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ChaosTradition).learned = true;
                    AddPawnAbility(TorannMagicDefOf.TM_ChaosTradition);
                    TM_Calc.AssignChaosMagicPowers(this, !Pawn.IsColonist);
                }
            }
            AssignAdvancedClassAbilities(true);
        }

        public void AssignAdvancedClassAbilities(bool firstAssignment = false)
        {
            if (AdvancedClasses != null && AdvancedClasses.Count > 0)
            {
                for (int z = 0; z < MagicData.AllMagicPowers.Count; z++)
                {
                    TMAbilityDef ability = (TMAbilityDef)MagicData.AllMagicPowers[z].abilityDef;
                    foreach (TM_CustomClass cc in AdvancedClasses)
                    {
                        if (cc.classMageAbilities.Contains(ability))
                        {
                            MagicData.AllMagicPowers[z].learned = true;
                        }
                        if (MagicData.AllMagicPowers[z] == MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SoulBond) ||
                        MagicData.AllMagicPowers[z] == MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ShadowBolt) ||
                        MagicData.AllMagicPowers[z] == MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Dominate))
                        {
                            MagicData.AllMagicPowers[z].learned = false;
                        }
                        if (MagicData.AllMagicPowers[z].requiresScroll)
                        {
                            MagicData.AllMagicPowers[z].learned = false;
                        }
                        if (!Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty) && !Rand.Chance(ability.learnChance))
                        {
                            MagicData.AllMagicPowers[z].learned = false;
                        }
                        if (MagicData.AllMagicPowers[z].learned)
                        {
                            if (ability.shouldInitialize)
                            {
                                AddPawnAbility(ability);
                            }
                            if (ability.childAbilities != null && ability.childAbilities.Count > 0)
                            {
                                for (int c = 0; c < ability.childAbilities.Count; c++)
                                {
                                    if (ability.childAbilities[c].shouldInitialize)
                                    {
                                        AddPawnAbility(ability.childAbilities[c]);
                                    }
                                }
                            }
                        }
                        if (cc.classHediff != null)
                        {
                            HealthUtility.AdjustSeverity(Pawn, customClass.classHediff, customClass.hediffSeverity);
                        }
                    }
                }
                MagicPower branding = MagicData.AllMagicPowers.FirstOrDefault((MagicPower p) => p.abilityDef == TorannMagicDefOf.TM_Branding);
                if (branding != null && branding.learned && firstAssignment)
                {
                    int count = 0;
                    while (count < 2)
                    {
                        TMAbilityDef tmpAbility = TM_Data.BrandList().RandomElement();
                        for (int i = 0; i < MagicData.AllMagicPowers.Count; i++)
                        {
                            TMAbilityDef ad = (TMAbilityDef)MagicData.AllMagicPowers[i].abilityDef;
                            if (!MagicData.AllMagicPowers[i].learned && ad == tmpAbility)
                            {
                                count++;
                                MagicData.AllMagicPowers[i].learned = true;
                                RemovePawnAbility(ad);
                                TryAddPawnAbility(ad);
                            }
                        }
                    }
                }                
            }
        }

        public void InitializeSpell()
        {
            Pawn abilityUser = Pawn;
            if (IsMagicUser)
            {
                if (customClass != null)
                {
                    //for(int j = 0; j < MagicData.AllMagicPowersWithSkills.Count; j++)
                    //{
                    //    if(MagicData.AllMagicPowersWithSkills[j].learned && !customClass.classMageAbilities.Contains(MagicData.AllMagicPowersWithSkills[j].abilityDef))
                    //    {
                    //        MagicData.AllMagicPowersWithSkills[j].learned = false;
                    //        RemovePawnAbility(MagicData.AllMagicPowersWithSkills[j].abilityDef);
                    //    }
                    //}
                    for (int j = 0; j < MagicData.AllMagicPowers.Count; j++)
                    {
                        if (MagicData.AllMagicPowers[j].learned && !customClass.classMageAbilities.Contains(MagicData.AllMagicPowers[j].abilityDef))
                        {
                            RemovePawnAbility(MagicData.AllMagicPowers[j].abilityDef);
                            AddPawnAbility(MagicData.AllMagicPowers[j].abilityDef);
                        }
                    }
                    if(recallSpell)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Recall);
                    }
                }
                else
                {
                    if (spell_Rain && !abilityUser.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost))
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Rainmaker);

                    }
                    if (spell_Blink && !abilityUser.story.traits.HasTrait(TorannMagicDefOf.Arcanist))
                    {
                        if (!abilityUser.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
                        {
                            SafelyAddPawnAbility(TorannMagicDefOf.TM_Blink);
                        }
                        else
                        {
                            bool hasAbility = false;
                            for (int i = 0; i < chaosPowers.Count; i++)
                            {
                                if (chaosPowers[i].Ability == TorannMagicDefOf.TM_Blink || chaosPowers[i].Ability == TorannMagicDefOf.TM_Blink_I || chaosPowers[i].Ability == TorannMagicDefOf.TM_Blink_II || chaosPowers[i].Ability == TorannMagicDefOf.TM_Blink_III)
                                {
                                    hasAbility = true;
                                }
                            }
                            if (!hasAbility)
                            {
                                SafelyAddPawnAbility(TorannMagicDefOf.TM_Blink);
                            }
                        }
                    }
                    if (spell_Teleport && !abilityUser.story.traits.HasTrait(TorannMagicDefOf.Arcanist))
                    {
                        if (!(abilityUser.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) && MagicData.MagicPowersA.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Teleport).learned))
                        {
                            SafelyAddPawnAbility(TorannMagicDefOf.TM_Teleport);
                        }
                    }

                    if (spell_Heal && !abilityUser.story.traits.HasTrait(TorannMagicDefOf.Paladin))
                    {
                        if (!abilityUser.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
                        {
                            SafelyAddPawnAbility(TorannMagicDefOf.TM_Heal);
                        }
                        else
                        {
                            bool hasAbility = false;
                            for (int i = 0; i < chaosPowers.Count; i++)
                            {
                                if (chaosPowers[i].Ability == TorannMagicDefOf.TM_Heal)
                                {
                                    hasAbility = true;
                                }
                            }
                            if (!hasAbility)
                            {
                                SafelyAddPawnAbility(TorannMagicDefOf.TM_Heal);
                            }
                        }
                    }
                    if (spell_Heater)
                    {
                        //if (summonedHeaters == null || (summonedHeaters != null && summonedHeaters.Count <= 0))
                        //{
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Heater);
                        //}
                    }
                    if (spell_Cooler)
                    {
                        //if(summonedCoolers == null || (summonedCoolers != null && summonedCoolers.Count <= 0))
                        //{
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Cooler);
                        //}
                    }
                    if (spell_PowerNode)
                    {
                        //if (summonedPowerNodes == null || (summonedPowerNodes != null && summonedPowerNodes.Count <= 0))
                        //{
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_PowerNode);
                        //}
                    }
                    if (spell_Sunlight)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Sunlight);

                    }
                    if (spell_DryGround)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_DryGround);
                    }
                    if (spell_WetGround)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_WetGround);
                    }
                    if (spell_ChargeBattery)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_ChargeBattery);
                    }
                    if (spell_SmokeCloud)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_SmokeCloud);
                    }
                    if (spell_Extinguish)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Extinguish);
                    }
                    if (spell_EMP)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_EMP);
                    }
                    if (spell_Blizzard)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Blizzard);
                    }
                    if (spell_Firestorm)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Firestorm);
                    }
                    if (spell_SummonMinion && !abilityUser.story.traits.HasTrait(TorannMagicDefOf.Summoner))
                    {
                        if (!abilityUser.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
                        {
                            SafelyAddPawnAbility(TorannMagicDefOf.TM_SummonMinion);
                        }
                        else
                        {
                            bool hasAbility = false;
                            for (int i = 0; i < chaosPowers.Count; i++)
                            {
                                if (chaosPowers[i].Ability == TorannMagicDefOf.TM_SummonMinion)
                                {
                                    hasAbility = true;
                                }
                            }
                            if (!hasAbility)
                            {
                                SafelyAddPawnAbility(TorannMagicDefOf.TM_SummonMinion);
                            }
                        }
                    }
                    if (spell_TransferMana)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_TransferMana);
                    }
                    if (spell_SiphonMana)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_SiphonMana);
                    }
                    if (spell_RegrowLimb)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_RegrowLimb);
                    }
                    if (spell_EyeOfTheStorm)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_EyeOfTheStorm);
                    }
                    if (spell_HeatShield)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_HeatShield);
                    }
                    if (spell_ManaShield)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_ManaShield);
                    }
                    if (spell_Blur)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Blur);
                    }
                    if (spell_FoldReality)
                    {
                        MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_FoldReality).learned = true;
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_FoldReality);
                    }
                    if (spell_Resurrection)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Resurrection);
                    }
                    if (spell_HolyWrath)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_HolyWrath);
                    }
                    if (spell_LichForm)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_LichForm);
                    }
                    if (spell_Flight)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Flight);
                    }
                    if (spell_SummonPoppi)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_SummonPoppi);
                    }
                    if (spell_BattleHymn)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_BattleHymn);
                    }
                    if (spell_CauterizeWound)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_CauterizeWound);
                    }
                    if (spell_SpellMending)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_SpellMending);
                    }
                    if (spell_FertileLands)
                    {
                        //if (fertileLands == null || (fertileLands != null && fertileLands.Count <= 0))
                        //{
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_FertileLands);
                        //}
                    }
                    if (spell_PsychicShock)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_PsychicShock);
                    }
                    if (spell_Scorn)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Scorn);
                    }
                    if (spell_BlankMind)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_BlankMind);
                    }
                    if (spell_ShadowStep)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_ShadowStep);
                    }
                    if (spell_ShadowCall)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_ShadowCall);
                    }
                    if (spell_Teach)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_TeachMagic);
                    }
                    if (spell_Meteor)
                    {
                        MagicPower meteorPower = MagicData.MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Meteor);
                        if (meteorPower == null)
                        {
                            meteorPower = MagicData.MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Meteor_I);
                            if (meteorPower == null)
                            {
                                meteorPower = MagicData.MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Meteor_II);
                                if (meteorPower == null)
                                {
                                    meteorPower = MagicData.MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Meteor_III);
                                }
                            }
                        }
                        if (meteorPower.level == 3)
                        {
                            SafelyAddPawnAbility(TorannMagicDefOf.TM_Meteor_III);
                        }
                        else if (meteorPower.level == 2)
                        {
                            SafelyAddPawnAbility(TorannMagicDefOf.TM_Meteor_II);
                        }
                        else if (meteorPower.level == 1)
                        {
                            SafelyAddPawnAbility(TorannMagicDefOf.TM_Meteor_I);
                        }
                        else
                        {
                            SafelyAddPawnAbility(TorannMagicDefOf.TM_Meteor);
                        }
                    }
                    if (spell_OrbitalStrike)
                    {
                        MagicPower OrbitalStrikePower = MagicData.magicPowerT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_OrbitalStrike);
                        if (OrbitalStrikePower == null)
                        {
                            OrbitalStrikePower = MagicData.magicPowerT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_I);
                            if (OrbitalStrikePower == null)
                            {
                                OrbitalStrikePower = MagicData.magicPowerT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_II);
                                if (OrbitalStrikePower == null)
                                {
                                    OrbitalStrikePower = MagicData.magicPowerT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_III);
                                }
                            }
                        }
                        if (OrbitalStrikePower.level == 3)
                        {
                            SafelyAddPawnAbility(TorannMagicDefOf.TM_OrbitalStrike_III);
                        }
                        else if (OrbitalStrikePower.level == 2)
                        {
                            SafelyAddPawnAbility(TorannMagicDefOf.TM_OrbitalStrike_II);
                        }
                        else if (OrbitalStrikePower.level == 1)
                        {
                            SafelyAddPawnAbility(TorannMagicDefOf.TM_OrbitalStrike_I);
                        }
                        else
                        {
                            SafelyAddPawnAbility(TorannMagicDefOf.TM_OrbitalStrike);
                        }
                    }
                    if (spell_BloodMoon)
                    {
                        MagicPower BloodMoonPower = MagicData.MagicPowersBM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BloodMoon) ??
                                                    MagicData.MagicPowersBM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BloodMoon_I) ??
                                                    MagicData.MagicPowersBM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BloodMoon_II) ??
                                                    MagicData.MagicPowersBM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BloodMoon_III);
                        switch (BloodMoonPower.level)
                        {
                            case 3:
                                SafelyAddPawnAbility(TorannMagicDefOf.TM_BloodMoon_III);
                                break;
                            case 2:
                                SafelyAddPawnAbility(TorannMagicDefOf.TM_BloodMoon_II);
                                break;
                            case 1:
                                SafelyAddPawnAbility(TorannMagicDefOf.TM_BloodMoon_I);
                                break;
                            default:
                                SafelyAddPawnAbility(TorannMagicDefOf.TM_BloodMoon);
                                break;
                        }
                    }
                    if (spell_EnchantedAura)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_EnchantedAura);
                    }
                    if (spell_Shapeshift)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Shapeshift);
                    }
                    if (spell_ShapeshiftDW)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_ShapeshiftDW);
                    }
                    if (spell_DirtDevil)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_DirtDevil);
                    }
                    if (spell_MechaniteReprogramming)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_MechaniteReprogramming);
                    }
                    if (spell_ArcaneBolt)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_ArcaneBolt);
                    }
                    if (spell_LightningTrap)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_LightningTrap);
                    }
                    if (spell_Invisibility)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Invisibility);
                    }
                    if (spell_BriarPatch)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_BriarPatch);
                    }
                    if (spell_Recall)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_TimeMark);
                        if (recallSpell)
                        {
                            SafelyAddPawnAbility(TorannMagicDefOf.TM_Recall);
                        }
                    }
                    if (spell_MageLight)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_MageLight);
                    }
                    if (spell_SnapFreeze)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_SnapFreeze);
                    }
                    if (spell_Ignite)
                    {
                        SafelyAddPawnAbility(TorannMagicDefOf.TM_Ignite);
                    }
                    
                    if (IsMagicUser && MagicData.MagicPowersCustomAll != null && MagicData.MagicPowersCustomAll.Count > 0)
                    {
                        for (int j = 0; j < MagicData.MagicPowersCustomAll.Count; j++)
                        {
                            if (MagicData.MagicPowersCustomAll[j].learned)
                            {
                                RemovePawnAbility(MagicData.MagicPowersCustomAll[j].abilityDef);
                                AddPawnAbility(MagicData.MagicPowersCustomAll[j].abilityDef);
                            }
                        }
                    }
                }
                //UpdateAbilities();
            }
        }

        public void RemovePowers(bool clearStandalone = true)
        {
            Pawn abilityUser = Pawn;
            if (magicPowersInitialized && MagicData != null)
            {
                bool flag2 = true;
                if (customClass != null)
                {
                    for (int i = 0; i < MagicData.AllMagicPowers.Count; i++)
                    {
                        MagicPower mp = MagicData.AllMagicPowers[i];
                        for (int j = 0; j < mp.TMabilityDefs.Count; j++)
                        {
                            TMAbilityDef tmad = mp.TMabilityDefs[j] as TMAbilityDef;
                            if(tmad.childAbilities != null && tmad.childAbilities.Count > 0)
                            {
                                for(int k = 0; k < tmad.childAbilities.Count; k++)
                                {
                                    RemovePawnAbility(tmad.childAbilities[k]);
                                }
                            }                            
                            RemovePawnAbility(tmad);
                        }
                        mp.learned = false;
                    }
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.InnerFire);
                if (TM_Calc.IsWanderer(Pawn))
                {
                    spell_ArcaneBolt = false;
                    RemovePawnAbility(TorannMagicDefOf.TM_ArcaneBolt);
                }
                if (abilityUser.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
                {
                    foreach (MagicPower current in MagicData.AllMagicPowersForChaosMage)
                    {
                        if (current.abilityDef != TorannMagicDefOf.TM_ChaosTradition)
                        {
                            current.learned = false;
                            foreach (TMAbilityDef tmad in current.TMabilityDefs)
                            {
                                if (tmad.childAbilities != null && tmad.childAbilities.Count > 0)
                                {
                                    for (int k = 0; k < tmad.childAbilities.Count; k++)
                                    {
                                        RemovePawnAbility(tmad.childAbilities[k]);
                                    }
                                }
                                RemovePawnAbility(tmad);
                            }
                        }
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_EnchantedAura);
                    RemovePawnAbility(TorannMagicDefOf.TM_NanoStimulant);
                    RemovePawnAbility(TorannMagicDefOf.TM_LightSkipMass);
                    RemovePawnAbility(TorannMagicDefOf.TM_LightSkipGlobal);
                    spell_EnchantedAura = false;
                    spell_ShadowCall = false;
                    spell_ShadowStep = false;
                    RemovePawnAbility(TorannMagicDefOf.TM_ShadowCall);
                    RemovePawnAbility(TorannMagicDefOf.TM_ShadowStep);

                }
                if (flag2)
                {
                    //Log.Message("Fixing Inner Fire Abilities");
                    foreach (MagicPower currentIF in MagicData.MagicPowersIF)
                    {
                        if (currentIF.abilityDef != TorannMagicDefOf.TM_Firestorm)
                        {
                            currentIF.learned = false;
                        }
                        RemovePawnAbility(currentIF.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_RayofHope_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_RayofHope_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_RayofHope_III);

                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost);
                if (flag2)
                {
                    //Log.Message("Fixing Heart of Frost Abilities");
                    foreach (MagicPower currentHoF in MagicData.MagicPowersHoF)
                    {
                        if (currentHoF.abilityDef != TorannMagicDefOf.TM_Blizzard)
                        {
                            currentHoF.learned = false;
                        }
                        RemovePawnAbility(currentHoF.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_Soothe_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_Soothe_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_Soothe_III);
                    RemovePawnAbility(TorannMagicDefOf.TM_FrostRay_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_FrostRay_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_FrostRay_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.StormBorn);
                if (flag2)
                {
                    //Log.Message("Fixing Storm Born Abilities");                   
                    foreach (MagicPower currentSB in MagicData.MagicPowersSB)
                    {
                        if (currentSB.abilityDef != TorannMagicDefOf.TM_EyeOfTheStorm)
                        {
                            currentSB.learned = false;
                        }
                        RemovePawnAbility(currentSB.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_AMP_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_AMP_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_AMP_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Arcanist);
                if (flag2)
                {
                    //Log.Message("Fixing Arcane Abilities");
                    foreach (MagicPower currentA in MagicData.MagicPowersA)
                    {
                        if (currentA.abilityDef != TorannMagicDefOf.TM_FoldReality)
                        {
                            currentA.learned = false;
                        }
                        RemovePawnAbility(currentA.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_Shadow_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_Shadow_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_Shadow_III);
                    RemovePawnAbility(TorannMagicDefOf.TM_MagicMissile_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_MagicMissile_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_MagicMissile_III);
                    RemovePawnAbility(TorannMagicDefOf.TM_Blink_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_Blink_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_Blink_III);
                    RemovePawnAbility(TorannMagicDefOf.TM_Summon_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_Summon_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_Summon_III);

                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Paladin);
                if (flag2)
                {
                    //Log.Message("Fixing Paladin Abilities");
                    foreach (MagicPower currentP in MagicData.MagicPowersP)
                    {
                        if (currentP.abilityDef != TorannMagicDefOf.TM_HolyWrath)
                        {
                            currentP.learned = false;
                        }
                        RemovePawnAbility(currentP.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_Shield_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_Shield_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_Shield_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Summoner);
                if (flag2)
                {
                    foreach (MagicPower currentS in MagicData.MagicPowersS)
                    {
                        if (currentS.abilityDef != TorannMagicDefOf.TM_SummonPoppi)
                        {
                            currentS.learned = false;
                        }
                        RemovePawnAbility(currentS.abilityDef);
                    }
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Druid);
                if (flag2)
                {
                    foreach (MagicPower currentD in MagicData.MagicPowersD)
                    {
                        if (currentD.abilityDef != TorannMagicDefOf.TM_RegrowLimb)
                        {
                            currentD.learned = false;
                        }
                        RemovePawnAbility(currentD.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_SootheAnimal_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_SootheAnimal_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_SootheAnimal_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || abilityUser.story.traits.HasTrait(TorannMagicDefOf.Lich);
                if (flag2)
                {
                    foreach (MagicPower currentN in MagicData.MagicPowersN)
                    {
                        if (currentN.abilityDef != TorannMagicDefOf.TM_LichForm)
                        {
                            currentN.learned = false;
                        }
                        RemovePawnAbility(currentN.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_DeathMark_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_DeathMark_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_DeathMark_III);
                    RemovePawnAbility(TorannMagicDefOf.TM_ConsumeCorpse_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_ConsumeCorpse_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_ConsumeCorpse_III);
                    RemovePawnAbility(TorannMagicDefOf.TM_CorpseExplosion_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_CorpseExplosion_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_CorpseExplosion_III);
                    RemovePawnAbility(TorannMagicDefOf.TM_DeathBolt_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_DeathBolt_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_DeathBolt_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Priest);
                if (flag2)
                {
                    foreach (MagicPower currentPR in MagicData.MagicPowersPR)
                    {
                        if (currentPR.abilityDef != TorannMagicDefOf.TM_Resurrection)
                        {
                            currentPR.learned = false;
                        }
                        RemovePawnAbility(currentPR.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_HealingCircle_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_HealingCircle_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_HealingCircle_III);
                    RemovePawnAbility(TorannMagicDefOf.TM_BestowMight_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_BestowMight_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_BestowMight_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Bard);
                if (flag2)
                {
                    foreach (MagicPower currentB in MagicData.MagicPowersB)
                    {
                        if (currentB.abilityDef != TorannMagicDefOf.TM_BattleHymn)
                        {
                            currentB.learned = false;
                        }
                        RemovePawnAbility(currentB.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_Lullaby_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_Lullaby_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_Lullaby_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Succubus);
                if (flag2)
                {
                    foreach (MagicPower currentSD in MagicData.MagicPowersSD)
                    {
                        if (currentSD.abilityDef != TorannMagicDefOf.TM_Scorn)
                        {
                            currentSD.learned = false;
                        }
                        RemovePawnAbility(currentSD.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_ShadowBolt_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_ShadowBolt_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_ShadowBolt_III);
                    RemovePawnAbility(TorannMagicDefOf.TM_Attraction_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_Attraction_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_Attraction_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Warlock);
                if (flag2)
                {
                    foreach (MagicPower currentWD in MagicData.MagicPowersWD)
                    {
                        if (currentWD.abilityDef != TorannMagicDefOf.TM_PsychicShock)
                        {
                            currentWD.learned = false;
                        }
                        RemovePawnAbility(currentWD.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_Repulsion_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_Repulsion_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_Repulsion_III);
                }
                // flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Geomancer);
                if (flag2)
                {
                    foreach (MagicPower currentG in MagicData.MagicPowersG)
                    {
                        if (currentG.abilityDef == TorannMagicDefOf.TM_Meteor || currentG.abilityDef == TorannMagicDefOf.TM_Meteor_I || currentG.abilityDef == TorannMagicDefOf.TM_Meteor_II || currentG.abilityDef == TorannMagicDefOf.TM_Meteor_III)
                        {
                            currentG.learned = true;
                        }
                        else
                        {
                            currentG.learned = false;
                        }
                        RemovePawnAbility(currentG.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_Encase_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_Encase_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_Encase_III);
                    RemovePawnAbility(TorannMagicDefOf.TM_Meteor_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_Meteor_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_Meteor_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Technomancer);
                if (flag2)
                {
                    foreach (MagicPower currentT in MagicData.MagicPowersT)
                    {
                        if (currentT.abilityDef == TorannMagicDefOf.TM_OrbitalStrike || currentT.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_I || currentT.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_II || currentT.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_III)
                        {
                            currentT.learned = true;
                        }
                        else
                        {
                            currentT.learned = false;
                        }
                        RemovePawnAbility(currentT.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_OrbitalStrike_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_OrbitalStrike_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_OrbitalStrike_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.BloodMage);
                if (flag2)
                {
                    foreach (MagicPower currentBM in MagicData.MagicPowersBM)
                    {
                        if (currentBM.abilityDef == TorannMagicDefOf.TM_BloodMoon || currentBM.abilityDef == TorannMagicDefOf.TM_BloodMoon_I || currentBM.abilityDef == TorannMagicDefOf.TM_BloodMoon_II || currentBM.abilityDef == TorannMagicDefOf.TM_BloodMoon_III)
                        {
                            currentBM.learned = true;
                        }
                        else
                        {
                            currentBM.learned = false;
                        }
                        RemovePawnAbility(currentBM.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_Rend_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_Rend_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_Rend_III);
                    RemovePawnAbility(TorannMagicDefOf.TM_BloodMoon_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_BloodMoon_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_BloodMoon_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Enchanter);
                if (flag2)
                {

                    foreach (MagicPower currentE in MagicData.MagicPowersE)
                    {
                        if (currentE.abilityDef != TorannMagicDefOf.TM_Shapeshift)
                        {
                            currentE.learned = false;
                        }
                        RemovePawnAbility(currentE.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_Polymorph_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_Polymorph_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_Polymorph_III);
                    RemovePawnAbility(TorannMagicDefOf.TM_EnchantedAura);
                }
                // flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Chronomancer);
                if (flag2)
                {
                    foreach (MagicPower currentC in MagicData.MagicPowersC)
                    {
                        if (currentC.abilityDef != TorannMagicDefOf.TM_Recall)
                        {
                            currentC.learned = false;
                        }
                        RemovePawnAbility(currentC.abilityDef);
                    }
                    RemovePawnAbility(TorannMagicDefOf.TM_ChronostaticField_I);
                    RemovePawnAbility(TorannMagicDefOf.TM_ChronostaticField_II);
                    RemovePawnAbility(TorannMagicDefOf.TM_ChronostaticField_III);
                }
                if (flag2)
                {
                    foreach (MagicPower currentShadow in MagicData.MagicPowersShadow)
                    {
                        RemovePawnAbility(currentShadow.abilityDef);
                    }
                }
                if (clearStandalone)
                {
                    foreach (MagicPower currentStandalone in MagicData.MagicPowersStandalone)
                    {
                        RemovePawnAbility(currentStandalone.abilityDef);
                    }
                }
            }
        }

        public override bool TryTransformPawn()
        {
            return IsMagicUser;
        }

        public bool TryAddPawnAbility(TMAbilityDef ability)
        {
            //add check to verify no ability is already added
            AddPawnAbility(ability);
            return true;
        }

        private void ClearPower(MagicPower current)
        {
            Log.Message("Removing ability: " + current.abilityDef.defName);
            RemovePawnAbility(current.abilityDef);
            UpdateAbilities();
        }

        public void ResetSkills()
        {
            MagicData.MagicPowerSkill_global_regen.First(mps => mps.label == "TM_global_regen_pwr").level = 0;
            MagicData.MagicPowerSkill_global_eff.First(mps => mps.label == "TM_global_eff_pwr").level = 0;
            MagicData.MagicPowerSkill_global_spirit.First(mps => mps.label == "TM_global_spirit_pwr").level = 0;
            for (int i = 0; i < MagicData.AllMagicPowersWithSkills.Count; i++)
            {
                MagicData.AllMagicPowersWithSkills[i].level = 0;
                MagicData.AllMagicPowersWithSkills[i].learned = false;
                MagicData.AllMagicPowersWithSkills[i].autocast = false;
                TMAbilityDef ability = (TMAbilityDef)MagicData.AllMagicPowersWithSkills[i].abilityDef;
                MagicPowerSkill mps = MagicData.GetSkill_Efficiency(ability);
                if (mps != null)
                {
                    mps.level = 0;
                }
                mps = MagicData.GetSkill_Power(ability);
                if (mps != null)
                {
                    mps.level = 0;
                }
                mps = MagicData.GetSkill_Versatility(ability);
                if (mps != null)
                {
                    mps.level = 0;
                }
            }
            for(int i = 0; i < MagicData.AllMagicPowers.Count; i++)
            {                
                for(int j = 0; j < MagicData.AllMagicPowers[i].TMabilityDefs.Count; j++)
                {
                    TMAbilityDef ability = (TMAbilityDef)MagicData.AllMagicPowers[i].TMabilityDefs[j];
                    RemovePawnAbility(ability);
                }
                MagicData.AllMagicPowers[i].learned = false;
                MagicData.AllMagicPowers[i].autocast = false;
            }
            MagicUserLevel = 0;
            MagicUserXP = 0;
            MagicData.MagicAbilityPoints = 0;
            //magicPowersInitialized = false;
            //base.IsInitialized = false;
            //CompAbilityUserMagic.MagicAbilities = null;
            //magicData = null;
            AssignAbilities();
          
        }

        private void LoadPowers()
        {
            foreach (MagicPower currentA in MagicData.MagicPowersA)
            {
                //Log.Message("Removing ability: " + currentA.abilityDef.defName.ToString());
                currentA.level = 0;
                RemovePawnAbility(currentA.abilityDef);
            }
            foreach (MagicPower currentHoF in MagicData.MagicPowersHoF)
            {
                //Log.Message("Removing ability: " + currentHoF.abilityDef.defName.ToString());
                currentHoF.level = 0;
                RemovePawnAbility(currentHoF.abilityDef);
            }
            foreach (MagicPower currentSB in MagicData.MagicPowersSB)
            {
                //Log.Message("Removing ability: " + currentSB.abilityDef.defName.ToString());
                currentSB.level = 0;
                RemovePawnAbility(currentSB.abilityDef);
            }
            foreach (MagicPower currentIF in MagicData.MagicPowersIF)
            {
                //Log.Message("Removing ability: " + currentIF.abilityDef.defName.ToString());
                currentIF.level = 0;
                RemovePawnAbility(currentIF.abilityDef);
            }
            foreach (MagicPower currentP in MagicData.MagicPowersP)
            {
                //Log.Message("Removing ability: " + currentP.abilityDef.defName.ToString());
                currentP.level = 0;
                RemovePawnAbility(currentP.abilityDef);
            }
            foreach (MagicPower currentS in MagicData.MagicPowersS)
            {
                //Log.Message("Removing ability: " + currentP.abilityDef.defName.ToString());
                currentS.level = 0;
                RemovePawnAbility(currentS.abilityDef);
            }
            foreach (MagicPower currentD in MagicData.MagicPowersD)
            {
                //Log.Message("Removing ability: " + currentP.abilityDef.defName.ToString());
                currentD.level = 0;
                RemovePawnAbility(currentD.abilityDef);
            }
            foreach (MagicPower currentN in MagicData.MagicPowersN)
            {
                //Log.Message("Removing ability: " + currentP.abilityDef.defName.ToString());
                currentN.level = 0;
                RemovePawnAbility(currentN.abilityDef);
            }
        }

        public void RemoveTraits()
        {
            List<Trait> traits = Pawn.story.traits.allTraits;
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
                if (customClass != null)
                {
                    traits.Remove(Pawn.story.traits.GetTrait(customClass.classTrait));
                    customClass = null;
                    customIndex = -2;
                }
            }
        }

        public void RemoveTMagicHediffs()
        {
            List<Hediff> allHediffs = Pawn.health.hediffSet.GetHediffs<Hediff>().ToList();
            for (int i = 0; i < allHediffs.Count(); i++)
            {
                Hediff hediff = allHediffs[i];
                if (hediff.def.defName.Contains("TM_"))
                {
                    Pawn.health.RemoveHediff(hediff);
                }

            }
        }

        public void RemoveAbilityUser()
        {
            RemovePowers();
            RemoveTMagicHediffs();
            RemoveTraits();
            magicData = null;
            Initialized = false;
        }

        public int MagicAttributeEffeciencyLevel(string attributeName)
        {
            int result = 0;

            if (attributeName == "TM_RayofHope_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_RayofHope.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Firebolt_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Firebolt.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Fireclaw_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Fireclaw.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Fireball_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Fireball.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Firestorm_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Firestorm.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }

            if (attributeName == "TM_Soothe_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Soothe.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Icebolt_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Icebolt.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_FrostRay_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_FrostRay.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Snowball_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Snowball.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Rainmaker_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Rainmaker.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Blizzard_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Blizzard.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }

            if (attributeName == "TM_AMP_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_AMP.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_LightningBolt_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_LightningBolt.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_LightningCloud_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_LightningCloud.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_LightningStorm_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_LightningStorm.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }

            if (attributeName == "TM_Shadow_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Shadow.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_MagicMissile_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_MagicMissile.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Blink_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Blink.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Summon_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Summon.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Teleport_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Teleport.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_FoldReality_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_FoldReality.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Heal_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Heal.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Shield_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Shield.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_ValiantCharge_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_ValiantCharge.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Overwhelm_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Overwhelm.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_HolyWrath_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_HolyWrath.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_SummonMinion_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_SummonMinion.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_SummonPylon_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_SummonPylon.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_SummonExplosive_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_SummonExplosive.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_SummonElemental_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_SummonElemental.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_SummonPoppi_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_SummonPoppi.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Poison_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Poison.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_SootheAnimal_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_SootheAnimal.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Regenerate_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Regenerate.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_CureDisease_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_CureDisease.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_RegrowLimb_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_RegrowLimb.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_EyeOfTheStorm_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_EyeOfTheStorm.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_RaiseUndead_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_RaiseUndead.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_DeathMark_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_DeathMark.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_FogOfTorment_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_FogOfTorment.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_ConsumeCorpse_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_ConsumeCorpse.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_CorpseExplosion_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_CorpseExplosion.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_DeathBolt_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_DeathBolt.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_AdvancedHeal_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_AdvancedHeal.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Purify_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Purify.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_HealingCircle_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_HealingCircle.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_BestowMight_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_BestowMight.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Resurrection_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Resurrection.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Lullaby_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Lullaby.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_BattleHymn_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_BattleHymn.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_SoulBond_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_SoulBond.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_ShadowBolt_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_ShadowBolt.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Dominate_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Dominate.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Attraction_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Attraction.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Repulsion_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Repulsion.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Scorn_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Scorn.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_PsychicShock_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_PsychicShock.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Stoneskin_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Stoneskin.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Encase_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Encase.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_EarthSprites_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_EarthSprites.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_EarthernHammer_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_EarthernHammer.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Sentinel_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Sentinel.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Meteor_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Meteor.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_TechnoTurret_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_TechnoTurret.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_TechnoShield_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_TechnoShield.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Sabotage_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Sabotage.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Overdrive_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Overdrive.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_OrbitalStrike_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_OrbitalStrike.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_BloodGift_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_BloodGift.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_IgniteBlood_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_IgniteBlood.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_BloodForBlood_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_BloodForBlood.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_BloodShield_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_BloodShield.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Rend_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Rend.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_BloodMoon_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_BloodMoon.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_EnchantedBody_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_EnchantedBody.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Transmutate_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Transmutate.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_EnchanterStone_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_EnchanterStone.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_EnchantWeapon_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_EnchantWeapon.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Polymorph_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Polymorph.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Shapeshift_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Shapeshift.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Prediction_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Prediction.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_AlterFate_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_AlterFate.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_AccelerateTime_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_AccelerateTime.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_ReverseTime_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_ReverseTime.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_ChronostaticField_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_ChronostaticField.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Recall_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Recall.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_WandererCraft_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Cantrips_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_Cantrips.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_ChaosTradition_eff")
            {
                MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_ChaosTradition.FirstOrDefault(mps => mps.label == attributeName);
                if (magicPowerSkill != null)
                {
                    result = magicPowerSkill.level;
                }
            }

            return result;
        }

        public float ActualManaCost(TMAbilityDef magicDef)
        {
            float adjustedManaCost = magicDef.manaCost;
            if (magicDef.efficiencyReductionPercent != 0)
            {
                if (magicDef == TorannMagicDefOf.TM_EnchantedAura)
                {
                    adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_EnchantedBody).level);
                }
                else if (magicDef == TorannMagicDefOf.TM_ShapeshiftDW)
                {
                    adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Shapeshift).level);
                }
                else if (magicDef == TorannMagicDefOf.TM_ShadowStep || magicDef == TorannMagicDefOf.TM_ShadowCall)
                {
                    adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_SoulBond).level);
                }
                else if( magicDef == TorannMagicDefOf.TM_LightSkipGlobal || magicDef == TorannMagicDefOf.TM_LightSkipMass)
                {
                    adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_LightSkip).level);
                }      
                else if(magicDef == TorannMagicDefOf.TM_SummonTotemEarth || magicDef == TorannMagicDefOf.TM_SummonTotemHealing || magicDef == TorannMagicDefOf.TM_SummonTotemLightning)
                {
                    adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Totems).level);
                }
                else if (magicDef == TorannMagicDefOf.TM_Hex_CriticalFail || magicDef == TorannMagicDefOf.TM_Hex_Pain || magicDef == TorannMagicDefOf.TM_Hex_MentalAssault)
                {
                    adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Hex).level);
                }
                else if(Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    CompAbilityUserMight compMight = Pawn.GetCompAbilityUserMight();
                    adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * compMight.MightData.GetSkill_Efficiency(TorannMagicDefOf.TM_Mimic).level);
                }
                else
                {
                    MagicPowerSkill mps = MagicData.GetSkill_Efficiency(magicDef);
                    if (mps != null)
                    {
                        adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * mps.level);
                    }
                }
            }

            if (Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_SyrriumSenseHD")))
            {
                adjustedManaCost *= .9f;
            }
            if (mpCost != 1f)
            {
                if (magicDef == TorannMagicDefOf.TM_Explosion)
                {
                    adjustedManaCost *= (1f - (1f - mpCost)/10f);
                }
                else
                {
                    adjustedManaCost *= mpCost;
                }
            }
            if (magicDef.abilityHediff != null && Pawn.health.hediffSet.HasHediff(magicDef.abilityHediff))
            {
                return 0f;
            }
            if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                adjustedManaCost = 0;
            }
            if (Pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) || (customClass != null && customClass.classMageAbilities.Contains(TorannMagicDefOf.TM_ChaosTradition)))
            {
                adjustedManaCost *= 1.2f;
            }

            if(Pawn.Map.gameConditionManager.ConditionIsActive(TorannMagicDefOf.TM_ManaStorm))
            {
                return Mathf.Max(adjustedManaCost *.5f, 0f);
            }
            return Mathf.Max(adjustedManaCost, (.5f * magicDef.manaCost));
        }

        public override List<HediffDef> IgnoredHediffs()
        {
            return new List<HediffDef>
            {
                TorannMagicDefOf.TM_MightUserHD
            };
        }

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            Pawn abilityUser = Pawn;

            List<Hediff> list = new List<Hediff>();
            List<Hediff> arg_32_0 = list;
            IEnumerable<Hediff> arg_32_1;
            if (abilityUser == null)
            {
                arg_32_1 = null;
            }
            else
            {
                arg_32_1 = abilityUser.health?.hediffSet?.hediffs;
            }
            arg_32_0.AddRange(arg_32_1);
            Pawn expr_3E = abilityUser;
            int? arg_84_0;
            if (expr_3E == null)
            {
                arg_84_0 = null;
            }
            else
            {
                Pawn_HealthTracker expr_52 = expr_3E.health;
                if (expr_52 == null)
                {
                    arg_84_0 = null;
                }
                else
                {
                    HediffSet expr_66 = expr_52.hediffSet;
                    arg_84_0 = ((expr_66 != null) ? new int?(expr_66.hediffs.Count<Hediff>()) : null);
                }
            }
            if ((arg_84_0 ?? 0) > 0)
            {
                foreach (Hediff current in list)
                {
                    if (current.def == TorannMagicDefOf.TM_HediffInvulnerable)
                    {
                        absorbed = true;
                        FleckMaker.Static(Pawn.Position, Pawn.Map, FleckDefOf.ExplosionFlash, 10);
                        dinfo.SetAmount(0);
                        return;
                    }                    
                    if (current.def == TorannMagicDefOf.TM_HediffEnchantment_phantomShift && Rand.Chance(.2f))
                    {
                        absorbed = true;
                        FleckMaker.Static(Pawn.Position, Pawn.Map, FleckDefOf.ExplosionFlash, 8);
                        FleckMaker.ThrowSmoke(abilityUser.Position.ToVector3Shifted(), abilityUser.Map, 1.2f);
                        dinfo.SetAmount(0);
                        return;
                    }                    
                    if (current.def == TorannMagicDefOf.TM_HediffShield)
                    {
                        float sev = current.Severity;
                        absorbed = true;
                        int actualDmg = 0;
                        float dmgAmt = dinfo.Amount;
                        float dmgToSev = 0.004f;
                        ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                        if (!abilityUser.IsColonist)
                        {
                            dmgToSev = settingsRef.AIHardMode ? 0.0025f : 0.003f;
                        }
                        sev -= dmgAmt * dmgToSev;
                        if (sev < 0)
                        {
                            actualDmg = Mathf.RoundToInt(Mathf.Abs(sev / dmgToSev));
                            BreakShield(abilityUser);
                        }
                        TM_Action.DisplayShieldHit(abilityUser, dinfo);
                        current.Severity = sev;
                        dinfo.SetAmount(actualDmg);

                        return;
                    }
                    if (current.def == TorannMagicDefOf.TM_DemonScornHD || current.def == TorannMagicDefOf.TM_DemonScornHD_I || current.def == TorannMagicDefOf.TM_DemonScornHD_II || current.def == TorannMagicDefOf.TM_DemonScornHD_III)
                    {
                        float sev = current.Severity;
                        absorbed = true;
                        int actualDmg = 0;
                        float dmgAmt = dinfo.Amount;
                        float dmgToSev = 1f;
                        ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                        if (!abilityUser.IsColonist)
                        {
                            dmgToSev = settingsRef.AIHardMode ? 0.8f : 1f;
                        }
                        sev -= dmgAmt * dmgToSev;
                        if (sev < 0)
                        {
                            actualDmg = Mathf.RoundToInt(Mathf.Abs(sev / dmgToSev));
                            BreakShield(abilityUser);
                        }
                        TM_Action.DisplayShieldHit(abilityUser, dinfo);
                        current.Severity = sev;
                        dinfo.SetAmount(actualDmg);

                        return;
                    }
                    if (current.def == TorannMagicDefOf.TM_ManaShieldHD && damageMitigationDelayMS < age)
                    {
                        float sev = Mana.CurLevel;
                        absorbed = true;
                        int actualDmg = 0;
                        float dmgAmt = dinfo.Amount;
                        float dmgToSev = 0.02f;
                        float maxDmg = 11f;
                        if (MagicData.MagicPowerSkill_Cantrips.First(mps => mps.label == "TM_Cantrips_ver").level >= 3)
                        {
                            dmgToSev = 0.015f;
                            maxDmg = 14f;
                            if (MagicData.MagicPowerSkill_Cantrips.First(mps => mps.label == "TM_Cantrips_ver").level >= 7)
                            {
                                dmgToSev = 0.012f;
                                maxDmg = 17f;
                            }
                        }
                        if (dmgAmt >= maxDmg)
                        {
                            actualDmg = Mathf.RoundToInt(dmgAmt - maxDmg);
                            sev -= maxDmg * dmgToSev;
                        }
                        else
                        {
                            sev -= dmgAmt * dmgToSev;
                        }
                        Mana.CurLevel = sev;
                        if (sev < 0)
                        {
                            actualDmg = Mathf.RoundToInt(Mathf.Abs(sev / dmgToSev));
                            BreakShield(abilityUser);
                            current.Severity = sev;
                            abilityUser.health.RemoveHediff(current);
                        }
                        TM_Action.DisplayShieldHit(abilityUser, dinfo);
                        damageMitigationDelayMS = age + 2;
                        dinfo.SetAmount(actualDmg);
                        abilityUser.TakeDamage(dinfo);
                        return;
                    }
                    if (current.def == TorannMagicDefOf.TM_LichHD && damageMitigationDelay < age)
                    {
                        absorbed = true;
                        int mitigationAmt = 4;
                        int dmgAmt = Mathf.RoundToInt(dinfo.Amount);
                        if (dmgAmt < mitigationAmt)
                        {
                            MoteMaker.ThrowText(Pawn.DrawPos, Pawn.Map, "TM_DamageAbsorbedAll".Translate());
                            return;
                        }
                        MoteMaker.ThrowText(Pawn.DrawPos, Pawn.Map, "TM_DamageAbsorbed".Translate(
                            dmgAmt,
                            mitigationAmt
                        ));
                        damageMitigationDelay = age + 6;
                        dinfo.SetAmount(dmgAmt - mitigationAmt);
                        abilityUser.TakeDamage(dinfo);
                        return;
                    }
                    if (arcaneRes != 0 && resMitigationDelay < age)
                    {
                        if (current.def == TorannMagicDefOf.TM_HediffEnchantment_arcaneRes)
                        {
                            if ((dinfo.Def.armorCategory != null && (dinfo.Def.armorCategory == TorannMagicDefOf.Dark || dinfo.Def.armorCategory == TorannMagicDefOf.Light)) || dinfo.Def.defName.Contains("TM_") || dinfo.Def.defName == "FrostRay" || dinfo.Def.defName == "Snowball" || dinfo.Def.defName == "Iceshard" || dinfo.Def.defName == "Firebolt")
                            {
                                absorbed = true;
                                int actualDmg = Mathf.RoundToInt(dinfo.Amount / arcaneRes);
                                resMitigationDelay = age + 10;
                                dinfo.SetAmount(actualDmg);
                                abilityUser.TakeDamage(dinfo);
                                return;
                            }
                        }
                    }
                }
            }
            base.PostPreApplyDamage(dinfo, out absorbed);
        }

        private void BreakShield(Pawn pawn)
        {
            SoundDefOf.EnergyShield_Broken.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            FleckMaker.Static(pawn.TrueCenter(), pawn.Map, FleckDefOf.ExplosionFlash, 12f);
            for (int i = 0; i < 6; i++)
            {
                Vector3 loc = pawn.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f);
                FleckMaker.ThrowDustPuff(loc, pawn.Map, Rand.Range(0.8f, 1.2f));
            }
        }

        public void DoArcaneForging()
        {
            if (Pawn.CurJob.targetA.Thing.def.defName == "TableArcaneForge")
            {
                ArcaneForging = true;
                Thing forge = Pawn.CurJob.targetA.Thing;
                if (Pawn.Position == forge.InteractionCell && Pawn.jobs.curDriver.CurToilIndex >= 10)
                {
                    if (Find.TickManager.TicksGame % 20 == 0)
                    {
                        if (Mana.CurLevel >= .1f)
                        {
                            Mana.CurLevel -= .025f;
                            MagicUserXP += 4;
                            FleckMaker.ThrowSmoke(forge.DrawPos, forge.Map, Rand.Range(.8f, 1.2f));
                        }
                        else
                        {
                            Pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        }
                    }

                    ThingDef mote = null;
                    int rnd = Rand.RangeInclusive(0, 3);
                    switch (rnd)
                    {
                        case 0:
                            mote = TorannMagicDefOf.Mote_ArcaneFabricationA;
                            break;
                        case 1:
                            mote = TorannMagicDefOf.Mote_ArcaneFabricationB;
                            break;
                        case 2:
                            mote = TorannMagicDefOf.Mote_ArcaneFabricationC;
                            break;
                        case 3:
                            mote = TorannMagicDefOf.Mote_ArcaneFabricationD;
                            break;
                    }
                    Vector3 drawPos = forge.DrawPos;
                    drawPos.x += Rand.Range(-.05f, .05f);
                    drawPos.z += Rand.Range(-.05f, .05f);
                    TM_MoteMaker.ThrowGenericMote(mote, drawPos, forge.Map, Rand.Range(.25f, .4f), .02f, 0f, .01f, Rand.Range(-200, 200), 0, 0, forge.Rotation.AsAngle);
                }
            }
            else
            {
                ArcaneForging = false;
            }
        }

        public void ResolveMagicUseEvents()
        {
            int tick = Find.TickManager.TicksGame - 150000;
            MagicUsed = MagicUsed.Where(er => er.eventTick > tick).ToList();
        }

        public void ResolveAutoCast()
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            bool flagCM = Pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage);
            bool isCustom = customClass != null;
            //bool flagCP = Pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) || Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless);
            //CompAbilityUserMight compMight = null;
            //if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    compMight = Pawn.GetCompAbilityUserMight();
            //}
            if (
                !settingsRef.autocastEnabled
                || Pawn.jobs == null
                || Pawn.CurJob == null
                || Pawn.CurJob.def == TorannMagicDefOf.TMCastAbilityVerb
                || Pawn.CurJob.def == TorannMagicDefOf.TMCastAbilitySelf
                || Pawn.CurJob.def == JobDefOf.Ingest
                || Pawn.CurJob.def == JobDefOf.ManTurret
                || Pawn.GetPosture() != PawnPosture.Standing
                || Pawn.CurJob.playerForced
                || Pawn.Map.GameConditionManager.ConditionIsActive(TorannMagicDefOf.ManaDrain)
                || Pawn.Map.GameConditionManager.ConditionIsActive(TorannMagicDefOf.TM_ManaStorm)
            ) return;

            //Log.Message("pawn " + Pawn.LabelShort + " current job is " + Pawn.CurJob.def.defName);
            //non-combat (undrafted) spells
            bool castSuccess = false;
            if (Pawn.drafter != null && !Pawn.Drafted && Mana != null && Mana.CurLevelPercentage >= settingsRef.autocastMinThreshold)
            {
                foreach (MagicPower mp in MagicData.MagicPowersCustomAll)
                {
                    if (!mp.learned || !mp.autocast || mp.autocasting == null || !mp.autocasting.magicUser ||
                        !mp.autocasting.undrafted) continue;

                    TMAbilityDef tmad = mp.TMabilityDefs[mp.level] as TMAbilityDef; // issues with index?
                    bool canUseWithEquippedWeapon = true;
                    bool canUseIfViolentAbility = !Pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent) || !tmad.MainVerb.isViolent;
                    if(!TM_Calc.HasResourcesForAbility(Pawn, tmad))
                    {
                        continue;
                    }
                    if (canUseWithEquippedWeapon && canUseIfViolentAbility)
                    {
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                        if (mp.autocasting.type == AutocastType.OnTarget && Pawn.CurJob.targetA != null && Pawn.CurJob.targetA.Thing != null)
                        {
                            LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(Pawn, mp.autocasting, Pawn.CurJob.targetA);
                            if (localTarget != null && localTarget.IsValid)
                            {
                                Thing targetThing = localTarget.Thing;
                                if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                {
                                    continue;
                                }
                                if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(Pawn.Position, targetThing, Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                {
                                    continue;
                                }
                                if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (Pawn.Position - targetThing.Position).LengthHorizontal)
                                {
                                    continue;
                                }
                                bool TE = mp.autocasting.targetEnemy && targetThing.Faction != null && targetThing.Faction.HostileTo(Pawn.Faction);
                                if (TE && targetThing is Pawn targetPawn)
                                {
                                    if (targetPawn.Downed || targetPawn.IsPrisonerInPrisonCell())
                                    {
                                        continue;
                                    }
                                }
                                bool TN = mp.autocasting.targetNeutral && targetThing.Faction != null && !targetThing.Faction.HostileTo(Pawn.Faction);
                                bool TNF = mp.autocasting.targetNoFaction && targetThing.Faction == null;
                                bool TF = mp.autocasting.targetFriendly && targetThing.Faction == Pawn.Faction;
                                if (!(TE || TN || TF || TNF))
                                {
                                    continue;
                                }
                                if (!mp.autocasting.ValidConditions(Pawn, targetThing))
                                {
                                    continue;
                                }
                                AutoCast.MagicAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                            }
                        }
                        if (mp.autocasting.type == AutocastType.OnSelf)
                        {
                            LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(Pawn, mp.autocasting, Pawn.CurJob.targetA);
                            if (localTarget != null && localTarget.IsValid)
                            {
                                Pawn targetThing = localTarget.Pawn;
                                if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                {
                                    continue;
                                }
                                if (!mp.autocasting.ValidConditions(Pawn, targetThing))
                                {
                                    continue;
                                }
                                AutoCast.MagicAbility_OnSelf.Evaluate(this, tmad, ability, mp, out castSuccess);
                            }
                        }
                        if (mp.autocasting.type == AutocastType.OnCell && Pawn.CurJob.targetA != null)
                        {
                            LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(Pawn, mp.autocasting, Pawn.CurJob.targetA);
                            if (localTarget != null && localTarget.IsValid)
                            {
                                IntVec3 targetThing = localTarget.Cell;
                                if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                {
                                    continue;
                                }
                                if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(Pawn.Position, targetThing, Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                {
                                    continue;
                                }
                                if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (Pawn.Position - targetThing).LengthHorizontal)
                                {
                                    continue;
                                }
                                if (!mp.autocasting.ValidConditions(Pawn, targetThing))
                                {
                                    continue;
                                }
                                AutoCast.MagicAbility_OnCell.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                            }
                        }
                        if (mp.autocasting.type == AutocastType.OnNearby)
                        {
                            if(mp.autocasting.maxRange == 0f)
                            {
                                mp.autocasting.maxRange = mp.abilityDef.MainVerb.range;
                            }
                            LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(Pawn, mp.autocasting, Pawn.CurJob.targetA);
                            if (localTarget != null && localTarget.IsValid)
                            {
                                Thing targetThing = localTarget.Thing;
                                if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                {
                                    continue;
                                }
                                if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(Pawn.Position, targetThing, Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                {
                                    continue;
                                }
                                if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (Pawn.Position - targetThing.Position).LengthHorizontal)
                                {
                                    continue;
                                }
                                bool TE = mp.autocasting.targetEnemy && targetThing.Faction != null && targetThing.Faction.HostileTo(Pawn.Faction);
                                if (TE && targetThing is Pawn targetPawn)
                                {
                                    if (targetPawn.Downed || targetPawn.IsPrisonerInPrisonCell())
                                    {
                                        continue;
                                    }
                                }
                                bool TN = mp.autocasting.targetNeutral && targetThing.Faction != null && !targetThing.Faction.HostileTo(Pawn.Faction);
                                bool TNF = mp.autocasting.targetNoFaction && targetThing.Faction == null;
                                bool TF = mp.autocasting.targetFriendly && targetThing.Faction == Pawn.Faction;
                                if (!(TE || TN || TF || TNF))
                                {
                                    continue;
                                }
                                if (!mp.autocasting.ValidConditions(Pawn, targetThing))
                                {
                                    continue;
                                }
                                AutoCast.MagicAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                            }
                        }
                        if (castSuccess) return;
                    }
                }

                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Summoner) || flagCM || isCustom)
                {
                    MagicPower magicPower = MagicData.MagicPowersS.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SummonMinion);
                    if (magicPower != null && magicPower.learned && magicPower.autocast && summonedMinions.Count() < 4)
                    {
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_SummonMinion);
                        AutoCast.MagicAbility_OnSelfPosition.Evaluate(this, TorannMagicDefOf.TM_SummonMinion, ability, magicPower, out castSuccess);
                        if (castSuccess) return;
                    }
                }
                if ((Pawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer) || isCustom) && !recallSet)
                {
                    if (AbilityData.Powers.Any(p => p.Def == TorannMagicDefOf.TM_TimeMark))
                    {
                        MagicPower magicPower = MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_TimeMark);
                        if (magicPower != null && (magicPower.learned || spell_Recall) && magicPower.autocast && !Pawn.CurJob.playerForced)
                        {
                            PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_TimeMark);
                            AutoCast.MagicAbility_OnSelfPosition.Evaluate(this, TorannMagicDefOf.TM_TimeMark, ability, magicPower, out castSuccess);
                            if (castSuccess) return;
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist) || flagCM || isCustom)
                {
                    foreach (MagicPower current in MagicData.MagicPowersA)
                    {
                        if (current?.abilityDef == null) continue;

                        foreach (TMAbilityDef tmad in current.TMabilityDefs)
                        {
                            if ((tmad == TorannMagicDefOf.TM_Summon || tmad == TorannMagicDefOf.TM_Summon_I || tmad == TorannMagicDefOf.TM_Summon_II || tmad == TorannMagicDefOf.TM_Summon_III) && !Pawn.CurJob.playerForced)
                            {
                                //Log.Message("evaluating " + tmad.defName);
                                MagicPower magicPower = MagicData.MagicPowersA.FirstOrDefault(mp => mp.abilityDef == tmad);
                                if (magicPower != null && magicPower.learned && magicPower.autocast)
                                {
                                    PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                                    float minDistance = ActualManaCost(tmad) * 150;
                                    AutoCast.Summon.Evaluate(this, tmad, ability, magicPower, minDistance, out castSuccess);
                                    if (castSuccess) return;
                                }
                            }
                            if (tmad == TorannMagicDefOf.TM_Blink || tmad == TorannMagicDefOf.TM_Blink_I || tmad == TorannMagicDefOf.TM_Blink_II || tmad == TorannMagicDefOf.TM_Blink_III)
                            {
                                MagicPower magicPower = MagicData.MagicPowersA.FirstOrDefault(mp => mp.abilityDef == tmad);
                                if (magicPower != null && magicPower.learned && magicPower.autocast)
                                {
                                    PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                                    float minDistance = ActualManaCost(tmad) * 240;
                                    AutoCast.Blink.Evaluate(this, tmad, ability, magicPower, minDistance, out castSuccess);
                                    if (castSuccess)
                                    {
                                        return;
                                    }
                                }
                                if (flagCM && magicPower != null && spell_Blink && !magicPower.learned && magicPower.autocast)
                                {
                                    PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                                    float minDistance = ActualManaCost(tmad) * 200;
                                    AutoCast.Blink.Evaluate(this, tmad, ability, magicPower, minDistance, out castSuccess);
                                    if (castSuccess) return;
                                }
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Druid) || flagCM || isCustom)
                {
                    foreach (MagicPower current in MagicData.MagicPowersD)
                    {
                        if (current?.abilityDef == null || !current.learned || Pawn.CurJob.playerForced) continue;

                        if (current.abilityDef == TorannMagicDefOf.TM_Regenerate)
                        {
                            MagicPower magicPower = MagicData.MagicPowersD.FirstOrDefault(mp => mp.abilityDef == current.abilityDef);
                            if (magicPower != null && magicPower.learned && magicPower.autocast)
                            {
                                PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_Regenerate);
                                MagicPowerSkill pwr = MagicData.MagicPowerSkill_Regenerate.FirstOrDefault(mps => mps.label == "TM_Regenerate_pwr");
                                if (pwr.level == 0)
                                {
                                    AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration"), 10f, out castSuccess);
                                }
                                else if (pwr.level == 1)
                                {
                                    AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_I"), 12f, out castSuccess);
                                }
                                else if (pwr.level == 2)
                                {
                                    AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_II"), 14f, out castSuccess);
                                }
                                else
                                {
                                    AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_III"), 16f, out castSuccess);
                                }
                                if (castSuccess) return;
                            }
                        }
                        if (current.abilityDef == TorannMagicDefOf.TM_CureDisease)
                        {
                            MagicPower magicPower = MagicData.MagicPowersD.FirstOrDefault(mp => mp.abilityDef == current.abilityDef);
                            if (magicPower != null && magicPower.learned && magicPower.autocast)
                            {
                                PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_CureDisease);
                                MagicPowerSkill ver = MagicData.MagicPowerSkill_CureDisease.FirstOrDefault(mps => mps.label == "TM_CureDisease_ver");

                                List<string> afflictionList = HediffCategoryList.Named("TM_Category_Hediffs").diseases
                                    .Where(chd =>
                                        chd.requiredSkillName == "TM_CureDisease_ver"
                                        && chd.requiredSkillLevel <= ver.level
                                    ).Select(chd => chd.hediffDefname).ToList();
                                //afflictionList.Add("Infection");
                                //afflictionList.Add("WoundInfection");
                                //afflictionList.Add("Flu");
                                //if (ver.level >= 1)
                                //{
                                //    afflictionList.Add("GutWorms");
                                //    afflictionList.Add("Malaria");
                                //    afflictionList.Add("FoodPoisoning");
                                //}
                                //if (ver.level >= 2)
                                //{
                                //    afflictionList.Add("SleepingSickness");
                                //    afflictionList.Add("MuscleParasites");
                                //    afflictionList.Add("Scaria");
                                //}
                                //if (ver.level >= 3)
                                //{
                                //    afflictionList.Add("Plague");
                                //    afflictionList.Add("Animal_Plague");
                                //    afflictionList.Add("BloodRot");
                                //}
                                AutoCast.CureSpell.Evaluate(this, TorannMagicDefOf.TM_CureDisease, ability, magicPower, afflictionList, out castSuccess);
                                if (castSuccess) return;
                            }
                        }
                        if (current.abilityDef == TorannMagicDefOf.TM_RegrowLimb && spell_RegrowLimb)
                        {
                            MagicPower magicPower = MagicData.MagicPowersD.FirstOrDefault(mp => mp.abilityDef == current.abilityDef);
                            bool workPriorities = true;
                            if (Pawn.CurJob != null && Pawn.CurJob.workGiverDef != null && Pawn.CurJob.workGiverDef.workType != null)
                            {
                                workPriorities = Pawn.workSettings.GetPriority(Pawn.CurJob.workGiverDef.workType) >= Pawn.workSettings.GetPriority(TorannMagicDefOf.TM_Magic);
                            }
                            if (magicPower != null && magicPower.learned && magicPower.autocast && !Pawn.CurJob.playerForced && workPriorities)
                            {
                                Area tArea = TM_Calc.GetSeedOfRegrowthArea(Pawn.Map, false);
                                if (tArea != null)
                                {
                                    PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_RegrowLimb);
                                    AutoCast.OnTarget_Spell.TryExecute(this, TorannMagicDefOf.TM_RegrowLimb, ability, magicPower, tArea.ActiveCells.RandomElement(), 40, out castSuccess);
                                    if (castSuccess) return;
                                }
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin) || flagCM || isCustom)
                {
                    foreach (MagicPower current in MagicData.MagicPowersP)
                    {
                        if (current?.abilityDef == null || !current.learned || Pawn.CurJob.playerForced) continue;

                        foreach(TMAbilityDef tmad in current.TMabilityDefs)
                        {
                            if (tmad == TorannMagicDefOf.TM_Heal)
                            {
                                MagicPower magicPower = MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == tmad);
                                if (magicPower != null && magicPower.learned && magicPower.autocast)
                                {
                                    PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                                    AutoCast.HealSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                    if (castSuccess) return;
                                }
                            }
                            if (tmad == TorannMagicDefOf.TM_Shield || tmad == TorannMagicDefOf.TM_Shield_I || tmad == TorannMagicDefOf.TM_Shield_II || tmad == TorannMagicDefOf.TM_Shield_III)
                            {
                                MagicPower magicPower = MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == tmad);
                                if (magicPower != null && magicPower.learned && magicPower.autocast)
                                {
                                    PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                                    AutoCast.Shield.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                    if (castSuccess) return;
                                }
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Priest) || flagCM || isCustom)
                {
                    foreach (MagicPower current in MagicData.MagicPowersPR)
                    {
                        if (current?.abilityDef == null || !current.learned || Pawn.CurJob.playerForced) continue;

                        foreach (TMAbilityDef tmad in current.TMabilityDefs)
                        {
                            if (tmad == TorannMagicDefOf.TM_AdvancedHeal)
                            {
                                MagicPower magicPower = MagicData.MagicPowersPR.FirstOrDefault(mp => mp.abilityDef == tmad);
                                if (magicPower != null && magicPower.learned && magicPower.autocast)
                                {
                                    PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                                    AutoCast.HealSpell.EvaluateMinSeverity(this, tmad, ability, magicPower, 1f, out castSuccess);
                                    if (castSuccess) return;
                                }
                            }
                            if (tmad == TorannMagicDefOf.TM_Purify)
                            {
                                MagicPower magicPower = MagicData.MagicPowersPR.FirstOrDefault(mp => mp.abilityDef == tmad);
                                if (magicPower != null && magicPower.learned && magicPower.autocast)
                                {
                                    PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_Purify);
                                    MagicPowerSkill ver = MagicData.MagicPowerSkill_Purify.FirstOrDefault(mps => mps.label == "TM_Purify_ver");
                                    AutoCast.HealPermanentSpell.Evaluate(this, TorannMagicDefOf.TM_Purify, ability, magicPower, out castSuccess);
                                    if (castSuccess) return;
                                    List<string> afflictionList = new List<string>();
                                    afflictionList.Clear();
                                    foreach(TM_CategoryHediff chd in HediffCategoryList.Named("TM_Category_Hediffs").ailments)
                                    {
                                        if(chd.requiredSkillName == "TM_Purify_ver" && chd.requiredSkillLevel <= ver.level)
                                        {
                                            afflictionList.Add(chd.hediffDefname);
                                        }
                                    }
                                    //afflictionList.Add("Cataract");
                                    //afflictionList.Add("HearingLoss");
                                    //afflictionList.Add("ToxicBuildup");
                                    //if (ver.level >= 1)
                                    //{
                                    //    afflictionList.Add("Blindness");
                                    //    afflictionList.Add("Asthma");
                                    //    afflictionList.Add("Cirrhosis");
                                    //    afflictionList.Add("ChemicalDamageModerate");
                                    //}
                                    //if (ver.level >= 2)
                                    //{
                                    //    afflictionList.Add("Frail");
                                    //    afflictionList.Add("BadBack");
                                    //    afflictionList.Add("Carcinoma");
                                    //    afflictionList.Add("ChemicalDamageSevere");
                                    //}
                                    //if (ver.level >= 3)
                                    //{
                                    //    afflictionList.Add("Alzheimers");
                                    //    afflictionList.Add("Dementia");
                                    //    afflictionList.Add("HeartArteryBlockage");
                                    //    afflictionList.Add("PsychicShock");
                                    //    afflictionList.Add("CatatonicBreakdown");
                                    //    afflictionList.Add("Abasia");
                                    //}
                                    AutoCast.CureSpell.Evaluate(this, TorannMagicDefOf.TM_Purify, ability, magicPower, afflictionList, out castSuccess);
                                    if (castSuccess) return;
                                    List<string> addictionList = new List<string>();
                                    addictionList.Clear();
                                    //addictionList.Add("Alcohol");
                                    //addictionList.Add("Smokeleaf");
                                    //if (ver.level >= 1)
                                    //{
                                    //    addictionList.Add("GoJuice");
                                    //    addictionList.Add("WakeUp");
                                    //}
                                    //if (ver.level >= 2)
                                    //{
                                    //    addictionList.Add("Psychite");
                                    //}
                                    foreach (TM_CategoryHediff chd in HediffCategoryList.Named("TM_Category_Hediffs").addictions)
                                    {
                                        if (chd.requiredSkillName == "TM_Purify_ver" && chd.requiredSkillLevel <= ver.level)
                                        {
                                            addictionList.Add(chd.hediffDefname);
                                        }
                                    }
                                    if (ver.level >= 3)
                                    {
                                        IEnumerable<ChemicalDef> enumerable = from def in DefDatabase<ChemicalDef>.AllDefs
                                            where (true)
                                            select def;
                                        foreach (ChemicalDef addiction in enumerable)
                                        {
                                            if (addiction.defName != "ROMV_VitaeAddiction" && addiction != TorannMagicDefOf.Luciferium)
                                            {
                                                addictionList.AddDistinct(addiction.defName);
                                            }
                                        }
                                    }
                                    AutoCast.CureAddictionSpell.Evaluate(this, TorannMagicDefOf.TM_Purify, ability, magicPower, addictionList, out castSuccess);
                                    if (castSuccess) return;
                                }
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter) || flagCM || isCustom)
                {
                    MagicPower magicPower = MagicData.MagicPowersE.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Transmutate);
                    bool workPriorities = true;
                    if (Pawn.CurJob != null && Pawn.CurJob.workGiverDef != null && Pawn.CurJob.workGiverDef.workType != null)
                    {
                        workPriorities = Pawn.workSettings.GetPriority(Pawn.CurJob.workGiverDef.workType) >= Pawn.workSettings.GetPriority(TorannMagicDefOf.TM_Magic);
                    }
                    if (magicPower != null && magicPower.learned && magicPower.autocast && !Pawn.CurJob.playerForced && workPriorities)
                    {
                        Area tArea = TM_Calc.GetTransmutateArea(Pawn.Map, false);
                        if (tArea != null)
                        {
                            Thing transmuteThing = TM_Calc.GetTransmutableThingFromCell(tArea.ActiveCells.RandomElement(), Pawn, out _, out _, out _, out _, out _);
                            if (transmuteThing != null)
                            {
                                PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_Transmutate);
                                AutoCast.OnTarget_Spell.TryExecute(this, TorannMagicDefOf.TM_Transmutate, ability, magicPower, transmuteThing, 50, out castSuccess);
                                if (castSuccess) return;
                            }
                        }
                    }
                }
                if ((spell_MechaniteReprogramming && Pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer)) || flagCM || isCustom)
                {
                    MagicPower magicPower = MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_MechaniteReprogramming);
                    if (magicPower != null && magicPower.learned && magicPower.autocast && !Pawn.CurJob.playerForced)
                    {
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_MechaniteReprogramming);
                        List<string> afflictionList = new List<string>();
                        afflictionList.Clear();
                        foreach (TM_CategoryHediff chd in HediffCategoryList.Named("TM_Category_Hediffs").mechanites)
                        {
                            afflictionList.Add(chd.hediffDefname);
                        }
                        //afflictionList.Add("SensoryMechanites");
                        //afflictionList.Add("FibrousMechanites");
                        AutoCast.CureSpell.Evaluate(this, TorannMagicDefOf.TM_MechaniteReprogramming, ability, magicPower, afflictionList, out castSuccess);
                        if (castSuccess) return;
                    }
                }
                if (spell_Heal && !Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin) && !isCustom)
                {
                    MagicPower magicPower = MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Heal);
                    if (magicPower.learned && magicPower.autocast && !Pawn.CurJob.playerForced)
                    {
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_Heal);
                        AutoCast.HealSpell.Evaluate(this, TorannMagicDefOf.TM_Heal, ability, magicPower, out castSuccess);
                        if (castSuccess) return;
                    }
                }
                if (spell_TransferMana || isCustom)
                {
                    MagicPower magicPower = MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_TransferMana);
                    if (magicPower.learned && magicPower.autocast && !Pawn.CurJob.playerForced)
                    {
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_TransferMana);
                        AutoCast.TransferManaSpell.Evaluate(this, TorannMagicDefOf.TM_TransferMana, ability, magicPower, false, false, out castSuccess);
                        if (castSuccess) return;
                    }
                }
                if (spell_SiphonMana || isCustom)
                {
                    MagicPower magicPower = MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SiphonMana);
                    if (magicPower.learned && magicPower.autocast && !Pawn.CurJob.playerForced)
                    {
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_SiphonMana);
                        AutoCast.TransferManaSpell.Evaluate(this, TorannMagicDefOf.TM_SiphonMana, ability, magicPower, false, true, out castSuccess);
                        if (castSuccess) return;
                    }
                }
                if (spell_CauterizeWound || isCustom)
                {
                    MagicPower magicPower = MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_CauterizeWound);
                    if (magicPower.learned && magicPower.autocast && !Pawn.CurJob.playerForced)
                    {
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_CauterizeWound);
                        AutoCast.HealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_CauterizeWound, ability, magicPower, 40f, out castSuccess);
                        if (castSuccess) return;
                    }
                }
                if (spell_SpellMending || isCustom)
                {
                    MagicPower magicPower = MagicData.MagicPowerStandaloneDictionary.TryGetValue(TorannMagicDefOf.TM_SpellMending.index);
                    if (magicPower != null && magicPower.learned && magicPower.autocast && !Pawn.CurJob.playerForced)
                    {
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_SpellMending);
                        AutoCast.SpellMending.Evaluate(this, TorannMagicDefOf.TM_SpellMending, ability, magicPower, HediffDef.Named("SpellMendingHD"), out castSuccess);
                        if (castSuccess) return;
                    }
                }
                if (spell_Teach || isCustom)
                {
                    MagicPower magicPower = MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_TeachMagic);
                    if (magicPower.learned && magicPower.autocast && !Pawn.CurJob.playerForced)
                    {
                        if (Pawn.CurJobDef.joyKind != null || Pawn.CurJobDef == JobDefOf.Wait_Wander || Pawn.CurJobDef == JobDefOf.GotoWander)
                        {
                            PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_TeachMagic);
                            AutoCast.Teach.Evaluate(this, TorannMagicDefOf.TM_TeachMagic, ability, magicPower, out castSuccess);
                            if (castSuccess) return;
                        }
                    }
                }
                if (spell_SummonMinion && !Pawn.story.traits.HasTrait(TorannMagicDefOf.Summoner) && !isCustom)
                {
                    MagicPower magicPower = MagicData.MagicPowersS.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SummonMinion);
                    if (magicPower.learned && magicPower.autocast && !Pawn.CurJob.playerForced && summonedMinions.Count() < 4)
                    {
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_SummonMinion);
                        AutoCast.MagicAbility_OnSelfPosition.Evaluate(this, TorannMagicDefOf.TM_SummonMinion, ability, magicPower, out castSuccess);
                        if (castSuccess) return;
                    }
                }
                if (spell_DirtDevil || isCustom)
                {
                    MagicPower magicPower = MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_DirtDevil);
                    if (magicPower != null && magicPower.learned && magicPower.autocast && !Pawn.CurJob.playerForced && Pawn.GetRoom() != null)
                    {
                        float roomCleanliness = Pawn.GetRoom().GetStat(RoomStatDefOf.Cleanliness);

                        if (roomCleanliness < -3f)
                        {
                            PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_DirtDevil);
                            AutoCast.MagicAbility_OnSelfPosition.Evaluate(this, TorannMagicDefOf.TM_DirtDevil, ability, magicPower, out castSuccess);
                            if (castSuccess) return;
                        }
                    }
                }
                if (spell_Blink && !Pawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist) && !flagCM && !isCustom)
                {
                    MagicPower magicPower = MagicData.MagicPowersA.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Blink);
                    if (magicPower.autocast)
                    {
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_Blink);
                        float minDistance = ActualManaCost(TorannMagicDefOf.TM_Blink) * 200;
                        AutoCast.Blink.Evaluate(this, TorannMagicDefOf.TM_Blink, ability, magicPower, minDistance, out castSuccess);
                        if (castSuccess) return;
                    }
                }
            }

            //combat (drafted) spells
            if (Pawn.drafter != null && Pawn.Drafted && Pawn.drafter.FireAtWill && Pawn.CurJob.def != JobDefOf.Goto && Mana != null && Mana.CurLevelPercentage >= settingsRef.autocastCombatMinThreshold)
            {
                foreach (MagicPower mp in MagicData.MagicPowersCustom)
                {
                    if (mp.learned && mp.autocast && mp.autocasting != null && mp.autocasting.magicUser && mp.autocasting.drafted)
                    {
                        TMAbilityDef tmad = mp.TMabilityDefs[mp.level] as TMAbilityDef; // issues with index?
                        bool canUseWithEquippedWeapon = true;
                        bool canUseIfViolentAbility = !Pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent) || !tmad.MainVerb.isViolent;
                        if (!TM_Calc.HasResourcesForAbility(Pawn, tmad))
                        {
                            continue;
                        }
                        if (canUseWithEquippedWeapon && canUseIfViolentAbility)
                        {
                            PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                            if (mp.autocasting.type == AutocastType.OnTarget && Pawn.TargetCurrentlyAimingAt != null && Pawn.TargetCurrentlyAimingAt.Thing != null)
                            {
                                LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(Pawn, mp.autocasting, Pawn.TargetCurrentlyAimingAt);
                                if (localTarget != null && localTarget.IsValid)
                                {
                                    Thing targetThing = localTarget.Thing;
                                    if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                    {
                                        continue;
                                    }
                                    if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(Pawn.Position, targetThing, Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                    {
                                        continue;
                                    }
                                    if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (Pawn.Position - targetThing.Position).LengthHorizontal)
                                    {
                                        continue;
                                    }
                                    bool TE = mp.autocasting.targetEnemy && targetThing.Faction != null && targetThing.Faction.HostileTo(Pawn.Faction);
                                    if (TE && targetThing is Pawn)
                                    {
                                        Pawn targetPawn = targetThing as Pawn;
                                        if (targetPawn.Downed || targetPawn.IsPrisonerInPrisonCell())
                                        {
                                            continue;
                                        }
                                    }
                                    bool TN = mp.autocasting.targetNeutral && targetThing.Faction != null && !targetThing.Faction.HostileTo(Pawn.Faction);
                                    bool TNF = mp.autocasting.targetNoFaction && targetThing.Faction == null;
                                    bool TF = mp.autocasting.targetFriendly && targetThing.Faction == Pawn.Faction;
                                    if (!(TE || TN || TF || TNF))
                                    {
                                        continue;
                                    }
                                    if (!mp.autocasting.ValidConditions(Pawn, targetThing))
                                    {
                                        continue;
                                    }
                                    AutoCast.MagicAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                }
                            }
                            if (mp.autocasting.type == AutocastType.OnSelf)
                            {
                                LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(Pawn, mp.autocasting, Pawn);
                                if (localTarget != null && localTarget.IsValid)
                                {
                                    Pawn targetThing = localTarget.Pawn;
                                    if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                    {
                                        continue;
                                    }
                                    if (!mp.autocasting.ValidConditions(Pawn, targetThing))
                                    {
                                        continue;
                                    }
                                    AutoCast.MagicAbility_OnSelf.Evaluate(this, tmad, ability, mp, out castSuccess);
                                }
                            }
                            if (mp.autocasting.type == AutocastType.OnCell && Pawn.TargetCurrentlyAimingAt != null)
                            {
                                LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(Pawn, mp.autocasting, Pawn.TargetCurrentlyAimingAt);
                                if (localTarget != null && localTarget.IsValid)
                                {
                                    IntVec3 targetThing = localTarget.Cell;
                                    if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                    {
                                        continue;
                                    }
                                    if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(Pawn.Position, targetThing, Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                    {
                                        continue;
                                    }
                                    if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (Pawn.Position - targetThing).LengthHorizontal)
                                    {
                                        continue;
                                    }
                                    if (!mp.autocasting.ValidConditions(Pawn, targetThing))
                                    {
                                        continue;
                                    }
                                    AutoCast.MagicAbility_OnCell.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                }
                            }
                            if (mp.autocasting.type == AutocastType.OnNearby)
                            {
                                LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(Pawn, mp.autocasting, Pawn.TargetCurrentlyAimingAt);
                                if (localTarget != null && localTarget.IsValid)
                                {
                                    Thing targetThing = localTarget.Thing;
                                    if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                    {
                                        continue;
                                    }
                                    if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(Pawn.Position, targetThing, Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                    {
                                        continue;
                                    }
                                    if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (Pawn.Position - targetThing.Position).LengthHorizontal)
                                    {
                                        continue;
                                    }
                                    bool TE = mp.autocasting.targetEnemy && targetThing.Faction != null && targetThing.Faction.HostileTo(Pawn.Faction);
                                    if (TE && targetThing is Pawn)
                                    {
                                        Pawn targetPawn = targetThing as Pawn;
                                        if (targetPawn.Downed || targetPawn.IsPrisonerInPrisonCell())
                                        {
                                            continue;
                                        }
                                    }
                                    bool TN = mp.autocasting.targetNeutral && targetThing.Faction != null && !targetThing.Faction.HostileTo(Pawn.Faction);
                                    bool TNF = mp.autocasting.targetNoFaction && targetThing.Faction == null;
                                    bool TF = mp.autocasting.targetFriendly && targetThing.Faction == Pawn.Faction;
                                    if (!(TE || TN || TF || TNF))
                                    {
                                        continue;
                                    }
                                    if (!mp.autocasting.ValidConditions(Pawn, targetThing))
                                    {
                                        continue;
                                    }
                                    AutoCast.MagicAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                }
                            }
                            if (castSuccess) return;
                        }
                    }
                }

                if ((Pawn.story.traits.HasTrait(TorannMagicDefOf.InnerFire) || flagCM || isCustom) && Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                {
                    foreach (MagicPower magicPower in MagicData.MagicPowersIF)
                    {
                        if (magicPower?.abilityDef == null || !magicPower.learned) continue;
                        if (magicPower.abilityDef != TorannMagicDefOf.TM_Firebolt) continue;
                        if (magicPower == null || !magicPower.autocast) continue;
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_Firebolt);
                        AutoCast.DamageSpell.Evaluate(this, TorannMagicDefOf.TM_Firebolt, ability, magicPower, out castSuccess);
                        if (castSuccess) return;
                    }
                }
                if ((Pawn.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost) || flagCM || isCustom) && Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                {
                    foreach (MagicPower current in MagicData.MagicPowersHoF)
                    {
                        if (current?.abilityDef == null || !current.learned) continue;
                        foreach (TMAbilityDef tmad in current.TMabilityDefs)
                        {
                            if (tmad == TorannMagicDefOf.TM_Icebolt)
                            {
                                MagicPower magicPower = MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Icebolt);
                                if (magicPower != null && magicPower.learned && magicPower.autocast)
                                {
                                    PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_Icebolt);
                                    AutoCast.DamageSpell.Evaluate(this, TorannMagicDefOf.TM_Icebolt, ability, magicPower, out castSuccess);
                                    if (castSuccess) return;
                                }
                            }
                            else if ((tmad == TorannMagicDefOf.TM_FrostRay || tmad == TorannMagicDefOf.TM_FrostRay_I || tmad == TorannMagicDefOf.TM_FrostRay_II || tmad == TorannMagicDefOf.TM_FrostRay_III))
                            {
                                MagicPower magicPower = MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == tmad);
                                if (magicPower != null && magicPower.learned && magicPower.autocast)
                                {
                                    PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                                    AutoCast.DamageSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                    if (castSuccess) return;
                                }
                            }
                        }
                    }
                }
                if ((Pawn.story.traits.HasTrait(TorannMagicDefOf.StormBorn) || flagCM || isCustom) && Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                {
                    foreach (MagicPower current in MagicData.MagicPowersSB)
                    {
                        if (current != null && current.abilityDef != null && current.learned)
                        {
                            if (current.abilityDef == TorannMagicDefOf.TM_LightningBolt)
                            {
                                MagicPower magicPower = MagicData.MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_LightningBolt);
                                if (magicPower != null && magicPower.learned && magicPower.autocast)
                                {
                                    PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_LightningBolt);
                                    AutoCast.DamageSpell.Evaluate(this, TorannMagicDefOf.TM_LightningBolt, ability, magicPower, out castSuccess);
                                    if (castSuccess) return;
                                }
                            }
                        }
                    }
                }
                if ((Pawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist) || flagCM || isCustom) && Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                {
                    foreach (MagicPower current in MagicData.MagicPowersA)
                    {
                        if (current != null && current.abilityDef != null && current.learned)
                        {
                            foreach (TMAbilityDef tmad in current.TMabilityDefs)
                            {
                                if ((tmad == TorannMagicDefOf.TM_MagicMissile || tmad == TorannMagicDefOf.TM_MagicMissile_I || tmad == TorannMagicDefOf.TM_MagicMissile_II || tmad == TorannMagicDefOf.TM_MagicMissile_III))
                                {
                                    MagicPower magicPower = MagicData.MagicPowersA.FirstOrDefault(mp => mp.abilityDef == tmad);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                                        AutoCast.DamageSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                        if (castSuccess) return;
                                    }
                                }
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Druid) || flagCM || isCustom)
                {
                    foreach (MagicPower current in MagicData.MagicPowersD)
                    {
                        if (current?.abilityDef != null && current.learned)
                        {
                            if (current.abilityDef == TorannMagicDefOf.TM_Poison && Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                            {
                                MagicPower magicPower = MagicData.MagicPowersD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Poison);
                                if (magicPower != null && magicPower.learned && magicPower.autocast)
                                {
                                    PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_Poison);
                                    AutoCast.HediffSpell.EvaluateMinRange(this, TorannMagicDefOf.TM_Poison, ability, magicPower, HediffDef.Named("TM_Poisoned_HD"), 10, out castSuccess);
                                    if (castSuccess) return;
                                }
                            }
                            if (current.abilityDef == TorannMagicDefOf.TM_Regenerate)
                            {
                                MagicPower magicPower = MagicData.MagicPowersD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Regenerate);
                                if (magicPower != null && magicPower.learned && magicPower.autocast)
                                {
                                    PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_Regenerate);
                                    MagicPowerSkill pwr = MagicData.MagicPowerSkill_Regenerate.FirstOrDefault(mps => mps.label == "TM_Regenerate_pwr");
                                    if (pwr.level == 0)
                                    {
                                        AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration"), 10f, out castSuccess);
                                        if (castSuccess) return;
                                    }
                                    else if (pwr.level == 1)
                                    {
                                        AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_I"), 12f, out castSuccess);
                                        if (castSuccess) return;
                                    }
                                    else if (pwr.level == 2)
                                    {
                                        AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_II"), 14f, out castSuccess);
                                        if (castSuccess) return;
                                    }
                                    else
                                    {
                                        AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_III"), 16f, out castSuccess);
                                        if (castSuccess) return;
                                    }
                                }
                            }
                        }
                    }
                }
                if ((Pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus) || flagCM || isCustom) && Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                {
                    foreach (MagicPower current in MagicData.MagicPowersSD)
                    {
                        if (current?.abilityDef != null && current.learned)
                        {
                            foreach (TMAbilityDef tmad in current.TMabilityDefs)
                            {
                                if ((tmad == TorannMagicDefOf.TM_ShadowBolt || tmad == TorannMagicDefOf.TM_ShadowBolt_I || tmad == TorannMagicDefOf.TM_ShadowBolt_II || tmad == TorannMagicDefOf.TM_ShadowBolt_III))
                                {
                                    MagicPower magicPower = MagicData.MagicPowersSD.FirstOrDefault(mp => mp.abilityDef == tmad);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                                        AutoCast.DamageSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                        if (castSuccess) return;
                                    }
                                }
                            }
                        }
                    }
                }
                if ((Pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock) || flagCM || isCustom) && Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                {
                    foreach (MagicPower current in MagicData.MagicPowersWD)
                    {
                        if (current != null && current.abilityDef != null && current.learned)
                        {
                            foreach (TMAbilityDef tmad in current.TMabilityDefs)
                            {
                                if ((tmad == TorannMagicDefOf.TM_ShadowBolt || tmad == TorannMagicDefOf.TM_ShadowBolt_I || tmad == TorannMagicDefOf.TM_ShadowBolt_II || tmad == TorannMagicDefOf.TM_ShadowBolt_III))
                                {
                                    MagicPower magicPower = MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == tmad);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                                        AutoCast.DamageSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                        if (castSuccess) return;
                                    }
                                }
                            }
                        }
                    }
                }
                if ((Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin) || flagCM || isCustom))
                {
                    foreach (MagicPower current in MagicData.MagicPowersP)
                    {
                        if (current != null && current.abilityDef != null && current.learned)
                        {
                            foreach (TMAbilityDef tmad in current.TMabilityDefs)
                            {
                                if (tmad == TorannMagicDefOf.TM_Heal)
                                {
                                    MagicPower magicPower = MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == tmad);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                                        AutoCast.HealSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                        if (castSuccess) return;
                                    }
                                }
                                if ((tmad == TorannMagicDefOf.TM_Shield || tmad == TorannMagicDefOf.TM_Shield_I || tmad == TorannMagicDefOf.TM_Shield_II || tmad == TorannMagicDefOf.TM_Shield_III))
                                {
                                    MagicPower magicPower = MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == tmad);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                                        AutoCast.Shield.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                        if (castSuccess) return;
                                    }
                                }
                            }
                        }
                    }
                }
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Priest) || flagCM || isCustom)
                {
                    foreach (MagicPower current in MagicData.MagicPowersPR)
                    {
                        if (current != null && current.abilityDef != null && current.learned)
                        {
                            if (current.abilityDef == TorannMagicDefOf.TM_AdvancedHeal)
                            {
                                MagicPower magicPower = MagicData.MagicPowersPR.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_AdvancedHeal);
                                if (magicPower != null && magicPower.learned && magicPower.autocast)
                                {
                                    PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_AdvancedHeal);
                                    AutoCast.HealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_AdvancedHeal, ability, magicPower, 1f, out castSuccess);
                                    if (castSuccess) return;
                                }
                            }
                        }
                    }
                }
                if ((spell_Heal && !Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin)))
                {
                    MagicPower magicPower = MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Heal);
                    if (magicPower.autocast)
                    {
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_Heal);
                        AutoCast.HealSpell.Evaluate(this, TorannMagicDefOf.TM_Heal, ability, magicPower, out castSuccess);
                        if (castSuccess) return;
                    }
                }
                if (spell_SiphonMana || isCustom)
                {
                    MagicPower magicPower = MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SiphonMana);
                    if (magicPower.learned && magicPower.autocast)
                    {
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_SiphonMana);
                        AutoCast.TransferManaSpell.Evaluate(this, TorannMagicDefOf.TM_SiphonMana, ability, magicPower, true, true, out castSuccess);
                        if (castSuccess) return;
                    }
                }
                if (spell_CauterizeWound || isCustom)
                {
                    MagicPower magicPower = MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_CauterizeWound);
                    if (magicPower.learned && magicPower.autocast)
                    {
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_CauterizeWound);
                        AutoCast.HealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_CauterizeWound, ability, magicPower, 40f, out castSuccess);
                        if (castSuccess) return;
                    }
                }
                if ((spell_ArcaneBolt || isCustom) && Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                {
                    MagicPower magicPower = MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ArcaneBolt);
                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                    {
                        PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == TorannMagicDefOf.TM_ArcaneBolt);
                        AutoCast.DamageSpell.Evaluate(this, TorannMagicDefOf.TM_ArcaneBolt, ability, magicPower, out castSuccess);
                        if (castSuccess) return;
                    }
                }
            }
        }

        public void ResolveAIAutoCast()
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (settingsRef.AICasting && Pawn.jobs != null && Pawn.CurJob != null && Pawn.CurJob.def != TorannMagicDefOf.TMCastAbilityVerb && Pawn.CurJob.def != TorannMagicDefOf.TMCastAbilitySelf &&
                Pawn.CurJob.def != JobDefOf.Ingest && Pawn.CurJob.def != JobDefOf.ManTurret && Pawn.GetPosture() == PawnPosture.Standing)
            {
                bool castSuccess = false;
                if (Mana != null && Mana.CurLevelPercentage >= settingsRef.autocastMinThreshold)
                {
                    foreach (MagicPower mp in MagicData.MagicPowersCustom)
                    {
                        if (mp.learned && mp.autocasting != null && mp.autocasting.magicUser && mp.autocasting.AIUsable)
                        {                            
                            //try
                            //{ 
                            TMAbilityDef tmad = mp.TMabilityDefs[mp.level] as TMAbilityDef; // issues with index?                            
                            bool canUseWithEquippedWeapon = true;
                            bool canUseIfViolentAbility = !Pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent) || !tmad.MainVerb.isViolent;
                            if (!TM_Calc.HasResourcesForAbility(Pawn, tmad))
                            {
                                continue;
                            }
                            if (canUseWithEquippedWeapon && canUseIfViolentAbility)
                            {
                                PawnAbility ability = AbilityData.Powers.FirstOrDefault(pa => pa.Def == tmad);
                                LocalTargetInfo currentTarget = Pawn.TargetCurrentlyAimingAt != null ? Pawn.TargetCurrentlyAimingAt : (Pawn.CurJob != null ? Pawn.CurJob.targetA : null);
                                if (mp.autocasting.type == AutocastType.OnTarget && currentTarget != null)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(Pawn, mp.autocasting, currentTarget);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        Thing targetThing = localTarget.Thing;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(Pawn.Position, targetThing, Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (Pawn.Position - targetThing.Position).LengthHorizontal)
                                        {
                                            continue;
                                        }
                                        bool TE = mp.autocasting.targetEnemy && targetThing.Faction != null && targetThing.Faction.HostileTo(Pawn.Faction);
                                        bool TN = mp.autocasting.targetNeutral && targetThing.Faction != null && !targetThing.Faction.HostileTo(Pawn.Faction);
                                        bool TNF = mp.autocasting.targetNoFaction && targetThing.Faction == null;
                                        if (targetThing is Pawn targetPawn)
                                        {
                                            if (TE || TNF)
                                            {
                                                if (targetPawn.Downed || targetPawn.IsPrisoner)
                                                    continue;
                                            }
                                            if (TN)
                                            {
                                                if (targetPawn.Downed || targetPawn.IsPrisoner)
                                                    continue;
                                                if (mp.abilityDef.MainVerb.isViolent && !targetPawn.InMentalState)
                                                    continue;
                                            }
                                        }
                                        bool TF = mp.autocasting.targetFriendly && targetThing.Faction == Pawn.Faction;
                                        if (!(TE || TN || TF || TNF))
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if (mp.autocasting.type == AutocastType.OnSelf)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(Pawn, mp.autocasting, Pawn);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        Pawn targetThing = localTarget.Pawn;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnSelf.Evaluate(this, tmad, ability, mp, out castSuccess);
                                    }
                                }
                                if (mp.autocasting.type == AutocastType.OnCell && currentTarget != null)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(Pawn, mp.autocasting, currentTarget);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        IntVec3 targetThing = localTarget.Cell;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(Pawn.Position, targetThing, Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (Pawn.Position - targetThing).LengthHorizontal)
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnCell.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if (mp.autocasting.type == AutocastType.OnNearby)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(Pawn, mp.autocasting, currentTarget);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        Thing targetThing = localTarget.Thing;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(Pawn.Position, targetThing, Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (Pawn.Position - targetThing.Position).LengthHorizontal)
                                        {
                                            continue;
                                        }
                                        bool TE = mp.autocasting.targetEnemy && targetThing.Faction != null && targetThing.Faction.HostileTo(Pawn.Faction);
                                        if (TE && targetThing is Pawn)
                                        {
                                            Pawn targetPawn = targetThing as Pawn;
                                            if (targetPawn.Downed || targetPawn.IsPrisoner)
                                            {
                                                continue;
                                            }
                                        }
                                        bool TN = mp.autocasting.targetNeutral && targetThing.Faction != null && !targetThing.Faction.HostileTo(Pawn.Faction);
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
                                        bool TF = mp.autocasting.targetFriendly && targetThing.Faction == Pawn.Faction;
                                        if (!(TE || TN || TF || TNF))
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(Pawn, targetThing))
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
            if(SoL != null)
            {
                //if(!spell_CreateLight)
                //{
                //    RemovePawnAbility(TorannMagicDefOf.TM_SoL_CreateLight);
                //    AddPawnAbility(TorannMagicDefOf.TM_SoL_CreateLight);
                //    spell_CreateLight = true;
                //}
                if(!spell_EqualizeLight)
                {
                    SafelyAddPawnAbility(TorannMagicDefOf.TM_SoL_Equalize);
                    spell_EqualizeLight = true;
                }
            }
            if(SoL == null)
            {
                if(spell_CreateLight || spell_EqualizeLight)
                {
                    //RemovePawnAbility(TorannMagicDefOf.TM_SoL_CreateLight);
                    RemovePawnAbility(TorannMagicDefOf.TM_SoL_Equalize);
                    spell_EqualizeLight = false;
                    //spell_CreateLight = false;
                }
            }
        }

        private void ResolveEarthSpriteAction()
        {
            MagicPowerSkill magicPowerSkill = MagicData.MagicPowerSkill_EarthSprites.First(mps => mps.label == "TM_EarthSprites_pwr");
            //Log.Message("resolving sprites");
            if (earthSpriteMap == null)
            {
                earthSpriteMap = Pawn.Map;
            }
            if (earthSpriteType == 1) //mining stone
            {
                //Log.Message("stone");
                Building mineTarget = earthSprites.GetFirstBuilding(earthSpriteMap);
                nextEarthSpriteAction = Find.TickManager.TicksGame + Mathf.RoundToInt(300 * (1 - .1f * magicPowerSkill.level) / arcaneDmg);
                TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.SparkFlash, earthSprites.ToVector3Shifted(), earthSpriteMap, Rand.Range(2f, 5f), .05f, 0f, .1f, 0, 0f, 0f, 0f);
                var mineable = mineTarget as Mineable;
                const int num = 80;
                if (mineable != null && mineTarget.HitPoints > num)
                {
                    var dinfo = new DamageInfo(DamageDefOf.Mining, num, 0, -1f, Pawn);
                    mineTarget.TakeDamage(dinfo);
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    if (Rand.Chance(settingsRef.magicyteChance * 2))
                    {
                        Thing thing = ThingMaker.MakeThing(TorannMagicDefOf.RawMagicyte);
                        thing.stackCount = Rand.Range(8, 16);
                        GenPlace.TryPlaceThing(thing, earthSprites, earthSpriteMap, ThingPlaceMode.Near);
                    }
                }
                else if (mineable != null && mineTarget.HitPoints <= num)
                {
                    mineable.DestroyMined(Pawn);
                }

                if (mineable.DestroyedOrNull())
                {
                    IntVec3 oldEarthSpriteLoc = earthSprites;
                    Building newMineSpot = null;
                    if (earthSpritesInArea)
                    {
                        //Log.Message("moving in area");
                        List<IntVec3> spriteAreaCells = GenRadial.RadialCellsAround(oldEarthSpriteLoc, 6f, false).ToList();
                        spriteAreaCells.Shuffle();
                        for (int i = 0; i < spriteAreaCells.Count; i++)
                        {
                            IntVec3 intVec = spriteAreaCells[i];
                            newMineSpot = intVec.GetFirstBuilding(earthSpriteMap);
                            if (newMineSpot != null && !intVec.Fogged(earthSpriteMap) && TM_Calc.GetSpriteArea() != null && TM_Calc.GetSpriteArea().ActiveCells.Contains(intVec))
                            {
                                mineable = newMineSpot as Mineable;
                                if (mineable != null)
                                {
                                    earthSprites = intVec;
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
                            newMineSpot = intVec.GetFirstBuilding(earthSpriteMap);
                            if (newMineSpot != null)
                            {
                                mineable = newMineSpot as Mineable;
                                if (mineable != null)
                                {
                                    earthSprites = intVec;
                                    i = 20;
                                }
                                newMineSpot = null;
                            }
                        }
                    }

                    if (oldEarthSpriteLoc == earthSprites)
                    {
                        earthSpriteType = 0;
                        earthSprites = IntVec3.Invalid;
                        earthSpritesInArea = false;
                    }
                }
            }
            else if (earthSpriteType == 2) //transforming soil
            {
                //Log.Message("earth");
                nextEarthSpriteAction = Find.TickManager.TicksGame + Mathf.RoundToInt((24000 * (1 - (.1f * magicPowerSkill.level))) / arcaneDmg);
                for (int m = 0; m < 4; m++)
                {
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, earthSprites.ToVector3Shifted(), earthSpriteMap, Rand.Range(.3f, .5f), Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), Rand.Range(-20, 20), Rand.Range(.5f, 1f), Rand.Range(0, 360), Rand.Range(0, 360));
                }
                Map map = earthSpriteMap;
                IntVec3 curCell = earthSprites;
                TerrainDef terrain = curCell.GetTerrain(map);
                if (Rand.Chance(.8f))
                {
                    Thing thing = ThingMaker.MakeThing(TorannMagicDefOf.RawMagicyte);
                    thing.stackCount = Rand.Range(10, 20);
                    GenPlace.TryPlaceThing(thing, earthSprites, earthSpriteMap, ThingPlaceMode.Near);
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
                        earthSprites = IntVec3.Invalid;
                        earthSpriteMap = null;
                        earthSpriteType = 0;
                        earthSpritesInArea = false;
                    }

                    terrain = curCell.GetTerrain(map);
                    if (terrain.defName == "SoilRich")
                    {
                        //look for new spot to transform
                        IntVec3 oldEarthSpriteLoc = earthSprites;
                        if (earthSpritesInArea)
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
                                    terrainHasBuilding = intVec.GetFirstBuilding(earthSpriteMap);
                                    if (TM_Calc.GetSpriteArea() != null && TM_Calc.GetSpriteArea().ActiveCells.Contains(intVec)) //dont transform terrain underneath buildings
                                    {
                                        //Log.Message("assigning");
                                        earthSprites = intVec;
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
                                    Building terrainHasBuilding;
                                    terrainHasBuilding = intVec.GetFirstBuilding(earthSpriteMap);
                                    if (terrainHasBuilding == null) //dont transform terrain underneath buildings
                                    {
                                        earthSprites = intVec;
                                        i = 20;
                                    }
                                }
                            }
                        }

                        if (oldEarthSpriteLoc == earthSprites)
                        {
                            earthSpriteType = 0;
                            earthSpriteMap = null;
                            earthSprites = IntVec3.Invalid;
                            earthSpritesInArea = false;
                            //Log.Message("ending");
                        }
                    }
                }
            }
        }

        public void ResolveEffecter()
        {
            if (!Pawn.Spawned) return;

            if (powerEffecter != null && PowerModifier == 0)
            {
                powerEffecter.Cleanup();
                powerEffecter = null;
            }
            if (powerEffecter == null && PowerModifier > 0)
            {
                EffecterDef progressBar = EffecterDefOf.ProgressBar;
                powerEffecter = progressBar.Spawn();
            }
            if (powerEffecter != null && PowerModifier > 0)
            {
                powerEffecter.EffectTick(Pawn, TargetInfo.Invalid);
                MoteProgressBar mote = ((SubEffecter_ProgressBar)powerEffecter.children[0]).mote;
                if (mote != null)
                {
                    float value = (float)powerModifier / maxPower;
                    mote.progress = Mathf.Clamp01(value);
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
                if (supportedUndead.Count > 0 && dismissUndeadSpell == false)
                {
                    AddPawnAbility(TorannMagicDefOf.TM_DismissUndead);
                    dismissUndeadSpell = true;
                }
                if (supportedUndead.Count <= 0 && dismissUndeadSpell)
                {
                    RemovePawnAbility(TorannMagicDefOf.TM_DismissUndead);
                    dismissUndeadSpell = false;
                }
            }
            else
            {
                supportedUndead = new List<Thing>();
            }
        }

        public void ResolveSuccubusLovin()
        {
            if (Pawn.CurrentBed() == null || Pawn.ageTracker.AgeBiologicalYears <= 17 ||
                Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_VitalityBoostHD"))) return;

            Pawn pawnInMyBed = TM_Calc.FindNearbyOtherPawn(Pawn, 1);
            if (pawnInMyBed?.CurrentBed() == null || !pawnInMyBed.RaceProps.Humanlike ||
                pawnInMyBed.ageTracker.AgeBiologicalYears <= 17) return;

            Job job = new Job(JobDefOf.Lovin, pawnInMyBed, Pawn.CurrentBed());
            Pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            HealthUtility.AdjustSeverity(pawnInMyBed, HediffDef.Named("TM_VitalityDrainHD"), 8);
            HealthUtility.AdjustSeverity(Pawn, HediffDef.Named("TM_VitalityBoostHD"), 6);
        }

        public void ResolveWarlockEmpathy()
        {
            //strange bug observed where other pawns will get the old offset of the previous pawn's offset unless other pawn has no empathy existing
            //in other words, empathy base mood effect seems to carry over from last otherpawn instead of using current otherpawn values
            if (Rand.Chance(Pawn.GetStatValue(StatDefOf.PsychicSensitivity, false) - 1))
            {
                Pawn otherPawn = TM_Calc.FindNearbyOtherPawn(Pawn, 5);
                if (otherPawn != null && otherPawn.RaceProps.Humanlike && otherPawn.IsColonist)
                {
                    if (Rand.Chance(otherPawn.GetStatValue(StatDefOf.PsychicSensitivity, false) - .3f))
                    {
                        ThoughtHandler pawnThoughtHandler = Pawn.needs.mood.thoughts;
                        List<Thought> pawnThoughts = new List<Thought>();
                        pawnThoughtHandler.GetAllMoodThoughts(pawnThoughts);
                        List<Thought> otherThoughts = new List<Thought>();
                        otherPawn.needs.mood.thoughts.GetAllMoodThoughts(otherThoughts);
                        List<Thought_Memory> memoryThoughts = new List<Thought_Memory>();
                        float oldMemoryOffset = 0;
                        if (Rand.Chance(.3f)) //empathy absorbed by warlock
                        {
                            ThoughtDef empathyThought = ThoughtDef.Named("WarlockEmpathy");
                            memoryThoughts = Pawn.needs.mood.thoughts.memories.Memories;
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
                                    Pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(memoryThoughts[i].def);
                                }
                            }
                            Thought transferThought = otherThoughts.RandomElement();
                            float newOffset = Mathf.RoundToInt(transferThought.CurStage.baseMoodEffect / 2);
                            empathyThought.stages.First().baseMoodEffect = newOffset + oldMemoryOffset;

                            Pawn.needs.mood.thoughts.memories.TryGainMemory(empathyThought);
                            Vector3 drawPosOffset = Pawn.DrawPos;
                            drawPosOffset.z += .3f;
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ArcaneCircle, drawPosOffset, Pawn.Map, newOffset / 20, .2f, .1f, .1f, Rand.Range(100, 200), 0, 0, Rand.Range(0, 360));
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
                            empathyThought.stages.First().baseMoodEffect = newOffset + oldMemoryOffset;

                            otherPawn.needs.mood.thoughts.memories.TryGainMemory(empathyThought);
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
            if (overdriveBuilding == null) return;
            List<Pawn> odPawns = ModOptions.Constants.GetOverdrivePawnList();

            if (!odPawns.Contains(Pawn))
            {
                odPawns.Add(Pawn);
                ModOptions.Constants.SetOverdrivePawnList(odPawns);
            }
            Vector3 rndPos = overdriveBuilding.DrawPos;
            rndPos.x += Rand.Range(-.4f, .4f);
            rndPos.z += Rand.Range(-.4f, .4f);
            TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.SparkFlash, rndPos, overdriveBuilding.Map, Rand.Range(.6f, .8f), .1f, .05f, .05f, 0, 0, 0, Rand.Range(0, 360));
            FleckMaker.ThrowSmoke(rndPos, overdriveBuilding.Map, Rand.Range(.8f, 1.2f));
            rndPos = overdriveBuilding.DrawPos;
            rndPos.x += Rand.Range(-.4f, .4f);
            rndPos.z += Rand.Range(-.4f, .4f);
            TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.ElectricalSpark, rndPos, overdriveBuilding.Map, Rand.Range(.4f, .7f), .2f, .05f, .1f, 0, 0, 0, Rand.Range(0, 360));
            SoundInfo info = SoundInfo.InMap(new TargetInfo(overdriveBuilding.Position, overdriveBuilding.Map));
            info.pitchFactor = .4f;
            info.volumeFactor = .3f;
            SoundDefOf.TurretAcquireTarget.PlayOneShot(info);
            MagicPowerSkill damageControl = MagicData.MagicPowerSkill_Overdrive.First(mps => mps.label == "TM_Overdrive_ver");
            if (Rand.Chance(.6f - (.06f * damageControl.level)))
            {
                TM_Action.DamageEntities(overdriveBuilding, null, Rand.Range(3f, (7f - (1f * damageControl.level))), DamageDefOf.Burn, overdriveBuilding);
            }
            overdriveFrequency = 100 + (10 * damageControl.level);
            if (Rand.Chance(.4f))
            {
                overdriveFrequency /= 2;
            }
            overdriveDuration--;
            if (overdriveDuration <= 0)
            {
                if (odPawns.Contains(Pawn))
                {
                    ModOptions.Constants.ClearOverdrivePawns();
                    odPawns.Remove(Pawn);
                    ModOptions.Constants.SetOverdrivePawnList(odPawns);
                }
                overdrivePowerOutput = 0;
                overdriveBuilding = null;
            }
        }

        public void ResolveChronomancerTimeMark()
        {
            //Log.Message("pawn " + Pawn.LabelShort + " recallset: " + recallSet + " expiration: " + recallExpiration + " / " + Find.TickManager.TicksGame + " recallSpell: " + recallSpell + " position: " + recallPosition);
            if(customClass != null && MagicData.MagicPowersC.First(mp => mp.abilityDef == TorannMagicDefOf.TM_Recall).learned && !MagicData.MagicPowersStandalone.First(mp => mp.abilityDef == TorannMagicDefOf.TM_TimeMark).learned)
            {
                MagicData.MagicPowerStandaloneDictionary[TorannMagicDefOf.TM_TimeMark.index].learned = true;
                SafelyAddPawnAbility(TorannMagicDefOf.TM_TimeMark);
            }
            if (recallExpiration <= Find.TickManager.TicksGame)
            {
                recallSet = false;
            }
            if (recallSet && !recallSpell)
            {
                AddPawnAbility(TorannMagicDefOf.TM_Recall);
                recallSpell = true;
            }
            if (recallSpell && (!recallSet || recallPosition == default(IntVec3)))
            {
                recallSpell = false;
                RemovePawnAbility(TorannMagicDefOf.TM_Recall);
            }
        }

        public void ResolveSustainers()
        {
            if(BrandPawns != null && BrandPawns.Count > 0)
            {
                if(!dispelBrandings)
                {
                    AddPawnAbility(TorannMagicDefOf.TM_DispelBranding);
                    dispelBrandings = true;
                }
                for(int i = 0; i < BrandPawns.Count; i++)
                {
                    Pawn p = BrandPawns[i];
                    if (p == null || (!p.Destroyed && !p.Dead)) continue;

                    BrandPawns.Remove(BrandPawns[i]);
                    BrandDefs.Remove(BrandDefs[i]);
                    break;
                }
                if(sigilSurging && Mana.CurLevel <= .01f)
                {
                    sigilSurging = false;
                }
            }
            else if(dispelBrandings)
            {
                dispelBrandings = false;
                RemovePawnAbility(TorannMagicDefOf.TM_DispelBranding);
            }
            if (livingWall != null)
            {
                if (!dispelLivingWall)
                {
                    dispelLivingWall = true;
                    SafelyAddPawnAbility(TorannMagicDefOf.TM_DispelLivingWall);
                }
            }
            else if(dispelLivingWall)
            {
                dispelLivingWall = false;
                RemovePawnAbility(TorannMagicDefOf.TM_DispelLivingWall);
            }

            if (stoneskinPawns.Any())
            {
                if (!dispelStoneskin)
                {
                    dispelStoneskin = true;
                    AddPawnAbility(TorannMagicDefOf.TM_DispelStoneskin);
                }
                for (int i = 0; i < stoneskinPawns.Count(); i++)
                {
                    if (stoneskinPawns[i].DestroyedOrNull() || stoneskinPawns[i].Dead)
                    {
                        stoneskinPawns.Remove(stoneskinPawns[i]);
                    }
                    else
                    {
                        if (!stoneskinPawns[i].health.hediffSet.HasHediff(HediffDef.Named("TM_StoneskinHD")))
                        {
                            stoneskinPawns.Remove(stoneskinPawns[i]);
                        }
                    }
                }
            }
            else if (dispelStoneskin)
            {
                dispelStoneskin = false;
                RemovePawnAbility(TorannMagicDefOf.TM_DispelStoneskin);
            }

            if(bondedSpirit != null && !dismissGuardianSpirit)
            {
                AddPawnAbility(TorannMagicDefOf.TM_DismissGuardianSpirit);
                dismissGuardianSpirit = true;
            }
            if (bondedSpirit == null && dismissGuardianSpirit)
            {
                RemovePawnAbility(TorannMagicDefOf.TM_DismissGuardianSpirit);
                dismissGuardianSpirit = false;
            }

            if (summonedLights.Count > 0 && dismissSunlightSpell == false)
            {
                AddPawnAbility(TorannMagicDefOf.TM_DismissSunlight);
                dismissSunlightSpell = true;
            }

            if (summonedLights.Count <= 0 && dismissSunlightSpell)
            {
                RemovePawnAbility(TorannMagicDefOf.TM_DismissSunlight);
                dismissSunlightSpell = false;
            }

            if (summonedPowerNodes.Count > 0 && dismissPowerNodeSpell == false)
            {
                AddPawnAbility(TorannMagicDefOf.TM_DismissPowerNode);
                dismissPowerNodeSpell = true;
            }

            if (summonedPowerNodes.Count <= 0 && dismissPowerNodeSpell)
            {
                RemovePawnAbility(TorannMagicDefOf.TM_DismissPowerNode);
                dismissPowerNodeSpell = false;
            }

            if (summonedCoolers.Count > 0 && dismissCoolerSpell == false)
            {
                AddPawnAbility(TorannMagicDefOf.TM_DismissCooler);
                dismissCoolerSpell = true;
            }

            if (summonedCoolers.Count <= 0 && dismissCoolerSpell)
            {
                RemovePawnAbility(TorannMagicDefOf.TM_DismissCooler);
                dismissCoolerSpell = false;
            }

            if (summonedHeaters.Count > 0 && dismissHeaterSpell == false)
            {
                AddPawnAbility(TorannMagicDefOf.TM_DismissHeater);
                dismissHeaterSpell = true;
            }

            if (summonedHeaters.Count <= 0 && dismissHeaterSpell)
            {
                RemovePawnAbility(TorannMagicDefOf.TM_DismissHeater);
                dismissHeaterSpell = false;
            }

            if (enchanterStones.Count > 0 && dismissEnchanterStones == false)
            {
                AddPawnAbility(TorannMagicDefOf.TM_DismissEnchanterStones);
                dismissEnchanterStones = true;
            }
            if (enchanterStones.Count <= 0 && dismissEnchanterStones)
            {
                RemovePawnAbility(TorannMagicDefOf.TM_DismissEnchanterStones);
                dismissEnchanterStones = false;
            }

            if (lightningTraps.Count > 0 && dismissLightningTrap == false)
            {
                AddPawnAbility(TorannMagicDefOf.TM_DismissLightningTrap);
                dismissLightningTrap = true;
            }
            if (lightningTraps.Count <= 0 && dismissLightningTrap)
            {
                RemovePawnAbility(TorannMagicDefOf.TM_DismissLightningTrap);
                dismissLightningTrap = false;
            }

            if (summonedSentinels.Count > 0 && shatterSentinel == false)
            {
                AddPawnAbility(TorannMagicDefOf.TM_ShatterSentinel);
                shatterSentinel = true;
            }
            if (summonedSentinels.Count <= 0 && shatterSentinel)
            {
                RemovePawnAbility(TorannMagicDefOf.TM_ShatterSentinel);
                shatterSentinel = false;
            }

            if (soulBondPawn.DestroyedOrNull() && (spell_ShadowStep || spell_ShadowCall))
            {
                soulBondPawn = null;
                spell_ShadowCall = false;
                spell_ShadowStep = false;
                RemovePawnAbility(TorannMagicDefOf.TM_ShadowCall);
                RemovePawnAbility(TorannMagicDefOf.TM_ShadowStep);
            }
            if (soulBondPawn != null)
            {
                if (spell_ShadowStep == false)
                {
                    spell_ShadowStep = true;
                    SafelyAddPawnAbility(TorannMagicDefOf.TM_ShadowStep);
                }
                if (spell_ShadowCall == false)
                {
                    spell_ShadowCall = true;
                    SafelyAddPawnAbility(TorannMagicDefOf.TM_ShadowCall);
                }
            }

            if (weaponEnchants != null && weaponEnchants.Count > 0)
            {
                for (int i = 0; i < weaponEnchants.Count; i++)
                {
                    Pawn ewPawn = weaponEnchants[i];
                    if (ewPawn.DestroyedOrNull() || ewPawn.Dead)
                    {
                        weaponEnchants.Remove(ewPawn);
                    }
                }

                if (dispelEnchantWeapon == false)
                {
                    dispelEnchantWeapon = true;
                    AddPawnAbility(TorannMagicDefOf.TM_DispelEnchantWeapon);
                }
            }
            else if (dispelEnchantWeapon)
            {
                dispelEnchantWeapon = false;
                RemovePawnAbility(TorannMagicDefOf.TM_DispelEnchantWeapon);
            }

            if (mageLightActive)
            {
                if (Pawn.Map == null && mageLightSet)
                {
                    mageLightActive = false;
                    mageLightThing = null;
                    mageLightSet = false;
                }
                Hediff hediff = Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MageLightHD);
                if (hediff == null && !mageLightSet)
                {
                    HealthUtility.AdjustSeverity(Pawn, TorannMagicDefOf.TM_MageLightHD, .5f);
                }
                if (mageLightSet && mageLightThing == null)
                {
                    mageLightActive = false;
                }
            }
            else
            {
                Hediff hediff = Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MageLightHD);
                if (hediff != null)
                {
                    Pawn.health.RemoveHediff(hediff);
                }
                if (!mageLightThing.DestroyedOrNull())
                {
                    mageLightThing.Destroy();
                    mageLightThing = null;
                }
                mageLightSet = false;
            }            
        }

        public void ResolveMinions()
        {
            if (summonedMinions.Count > 0 && !dismissMinionSpell)
            {
                AddPawnAbility(TorannMagicDefOf.TM_DismissMinion);
                dismissMinionSpell = true;
            }

            if (summonedMinions.Count <= 0 && dismissMinionSpell)
            {
                RemovePawnAbility(TorannMagicDefOf.TM_DismissMinion);
                dismissMinionSpell = false;
            }

            summonedMinions = summonedMinions.Where(t => t is Pawn pawn && !pawn.Destroyed && !pawn.Dead).ToList();

            if (earthSpriteType != 0 && !dismissEarthSpriteSpell)
            {
                AddPawnAbility(TorannMagicDefOf.TM_DismissEarthSprites);
                dismissEarthSpriteSpell = true;
            }

            if (earthSpriteType == 0 && dismissEarthSpriteSpell)
            {
                RemovePawnAbility(TorannMagicDefOf.TM_DismissEarthSprites);
                dismissEarthSpriteSpell = false;
            }
        }

        public void ResolveMana()
        {
            if (Mana != null) return;

            Hediff firstHediffOfDef = Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MagicUserHD);
            if (firstHediffOfDef != null)
            {
                firstHediffOfDef.Severity = 1f;
            }
            else
            {
                Hediff hediff = HediffMaker.MakeHediff(TorannMagicDefOf.TM_MagicUserHD, Pawn);
                hediff.Severity = 1f;
                Pawn.health.AddHediff(hediff);
            }
        }
        public void ResolveMagicPowers()
        {
            if (!magicPowersInitialized)
                magicPowersInitialized = true;
        }
        public void ResolveMagicTab()
        {
            if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless)) return;

            InspectTabBase inspectTabsx = Pawn.GetInspectTabs().FirstOrDefault(x => x.labelKey == "TM_TabMagic");
            IEnumerable<InspectTabBase> inspectTabs = Pawn.GetInspectTabs();
            if (inspectTabs == null || !inspectTabs.Any()) return;
            if (inspectTabsx != null) return;

            try
            {
                Pawn.def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_Pawn_Magic)));
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

        public void ResolveClassSkills()
        {
            bool flagCM = Pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage);
            bool isCustom = customClass != null;

            if(isCustom && customClass.classHediff != null && !Pawn.health.hediffSet.HasHediff(customClass.classHediff))
            {
                HealthUtility.AdjustSeverity(Pawn, customClass.classHediff, customClass.hediffSeverity);
            }

            if(Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_CursedTD) && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CursedHD))
            {
                HealthUtility.AdjustSeverity(Pawn, TorannMagicDefOf.TM_CursedHD, .1f);
            }

            if (Pawn.story.traits.HasTrait(TorannMagicDefOf.BloodMage) || (isCustom && (customClass.classMageAbilities.Contains(TorannMagicDefOf.TM_BloodGift) || customClass.classHediff == TorannMagicDefOf.TM_BloodHD)))
            {
                if (!Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_BloodHD")))
                {
                    HealthUtility.AdjustSeverity(Pawn, HediffDef.Named("TM_BloodHD"), .1f);
                    for (int i = 0; i < 4; i++)
                    {
                        TM_MoteMaker.ThrowBloodSquirt(Pawn.DrawPos, Pawn.Map, Rand.Range(.5f, .8f));
                    }
                }
            }

            if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer) || flagCM || (isCustom && customClass.classMageAbilities.Contains(TorannMagicDefOf.TM_Prediction)))
            {
                if (predictionIncidentDef != null && (predictionTick + 30) < Find.TickManager.TicksGame)
                {
                    predictionIncidentDef = null;
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

            if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter) || flagCM || isCustom)
            {
                if (MagicData.MagicPowersE.First(mp => mp.abilityDef == TorannMagicDefOf.TM_EnchantedBody).learned && (spell_EnchantedAura == false || !MagicData.MagicPowerStandaloneDictionary[TorannMagicDefOf.TM_EnchantedAura.index].learned))
                {
                    spell_EnchantedAura = true;
                    MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EnchantedAura).learned = true;
                    InitializeSpell();
                }

                if (MagicData.MagicPowerSkill_Shapeshift.FirstOrDefault(mps => mps.label == "TM_Shapeshift_ver").level >= 3 && (spell_ShapeshiftDW != true || !MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ShapeshiftDW).learned))
                {
                    spell_ShapeshiftDW = true;
                    MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ShapeshiftDW).learned = true;
                    InitializeSpell();
                }
            }

            if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || flagCM || isCustom)
            {
                if (HasTechnoBit)
                {
                    if (!Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TechnoBitHD))
                    {
                        HealthUtility.AdjustSeverity(Pawn, TorannMagicDefOf.TM_TechnoBitHD, .5f);
                        Vector3 bitDrawPos = Pawn.DrawPos;
                        bitDrawPos.x -= .5f;
                        bitDrawPos.z += .45f;
                        for (int i = 0; i < 4; i++)
                        {
                            FleckMaker.ThrowSmoke(bitDrawPos, Pawn.Map, Rand.Range(.6f, .8f));
                        }
                    }
                }
                if (HasTechnoWeapon && Pawn.equipment != null && Pawn.equipment.Primary != null)
                {
                    if (Pawn.equipment.Primary.def.defName.Contains("TM_TechnoWeapon_Base") && Pawn.equipment.Primary.def.Verbs != null && Pawn.equipment.Primary.def.Verbs.FirstOrDefault().range < 2)
                    {
                        TM_Action.DoAction_TechnoWeaponCopy(Pawn, technoWeaponThing, technoWeaponThingDef, technoWeaponQC);
                    }

                    if (!Pawn.equipment.Primary.def.defName.Contains("TM_TechnoWeapon_Base") && (technoWeaponThing != null || technoWeaponThingDef != null))
                    {
                        technoWeaponThing = null;
                        technoWeaponThingDef = null;
                    }
                }
            }

            if (MagicUserLevel >= 20 && (spell_Teach == false || !MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_TeachMagic).learned))
            {
                AddPawnAbility(TorannMagicDefOf.TM_TeachMagic);
                MagicData.MagicPowersStandalone.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_TeachMagic).learned = true;
                spell_Teach = true;
            }

            if ((Pawn.story.traits.HasTrait(TorannMagicDefOf.Geomancer) || flagCM || isCustom) && earthSpriteType != 0 && earthSprites.IsValid)
            {
                if (nextEarthSpriteAction < Find.TickManager.TicksGame)
                {
                    ResolveEarthSpriteAction();
                }

                if (nextEarthSpriteMote < Find.TickManager.TicksGame)
                {
                    nextEarthSpriteMote += Rand.Range(7, 12);
                    Vector3 shiftLoc = earthSprites.ToVector3Shifted();
                    shiftLoc.x += Rand.Range(-.3f, .3f);
                    shiftLoc.z += Rand.Range(-.3f, .3f);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Twinkle, shiftLoc, Pawn.Map, Rand.Range(.6f, 1.4f), .15f, Rand.Range(.2f, .5f), Rand.Range(.2f, .5f), Rand.Range(-100, 100), Rand.Range(0f, .3f), Rand.Range(0, 360), 0);
                    if(Rand.Chance(.3f))
                    {
                        shiftLoc = earthSprites.ToVector3Shifted();
                        shiftLoc.x += Rand.Range(-.3f, .3f);
                        shiftLoc.z += Rand.Range(-.3f, .3f);
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_GreenTwinkle, shiftLoc, Pawn.Map, Rand.Range(.6f, 1f), .15f, Rand.Range(.2f, .9f), Rand.Range(.5f, .9f), Rand.Range(-200, 200), Rand.Range(0f, .3f), Rand.Range(0, 360), 0);
                    }
                }
            }

            if (summonedSentinels.Count > 0)
            {
                for (int i = 0; i < summonedSentinels.Count(); i++)
                {
                    if (summonedSentinels[i].DestroyedOrNull())
                    {
                        summonedSentinels.Remove(summonedSentinels[i]);
                    }
                }
            }

            if (lightningTraps.Count > 0)
            {
                for (int i = 0; i < lightningTraps.Count(); i++)
                {
                    if (lightningTraps[i].DestroyedOrNull())
                    {
                        lightningTraps.Remove(lightningTraps[i]);
                    }
                }
            }

            if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Lich))
            {
                if (!Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_LichHD")))
                {
                    HealthUtility.AdjustSeverity(Pawn, HediffDef.Named("TM_LichHD"), .5f);
                }
                if (spell_Flight != true)
                {
                    SafelyAddPawnAbility(TorannMagicDefOf.TM_DeathBolt);
                    spell_Flight = true;
                    InitializeSpell();
                }
            }

            if (IsMagicUser && !Pawn.Dead && !Pawn.Downed)
            {
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard))
                {
                    MagicPowerSkill bardtraining_pwr = Pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BardTraining.FirstOrDefault(mps => mps.label == "TM_BardTraining_pwr");

                    List<Trait> traits = Pawn.story.traits.allTraits;
                    for (int i = 0; i < traits.Count; i++)
                    {
                        if (traits[i].def.defName == "TM_Bard")
                        {
                            if (traits[i].Degree != bardtraining_pwr.level)
                            {
                                traits.Remove(traits[i]);
                                Pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Bard, bardtraining_pwr.level));
                                FleckMaker.ThrowHeatGlow(Pawn.Position, Pawn.Map, 2);
                            }
                        }
                    }
                }

                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus) || Pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock))
                {
                    if (soulBondPawn != null)
                    {
                        if (!soulBondPawn.Spawned)
                        {
                            RemovePawnAbility(TorannMagicDefOf.TM_SummonDemon);
                            spell_SummonDemon = false;
                        }
                        else if (soulBondPawn.health.hediffSet.HasHediff(HediffDef.Named("TM_DemonicPriceHD")))
                        {
                            if (spell_SummonDemon)
                            {
                                RemovePawnAbility(TorannMagicDefOf.TM_SummonDemon);
                                spell_SummonDemon = false;
                            }
                        }
                        else if (soulBondPawn.health.hediffSet.HasHediff(HediffDef.Named("TM_SoulBondMentalHD")) && soulBondPawn.health.hediffSet.HasHediff(HediffDef.Named("TM_SoulBondPhysicalHD")))
                        {
                            if (spell_SummonDemon == false)
                            {
                                AddPawnAbility(TorannMagicDefOf.TM_SummonDemon);
                                spell_SummonDemon = true;
                            }
                        }
                        else
                        {
                            if (spell_SummonDemon)
                            {
                                RemovePawnAbility(TorannMagicDefOf.TM_SummonDemon);
                                spell_SummonDemon = false;
                            }
                        }
                    }
                    else if (spell_SummonDemon)
                    {
                        RemovePawnAbility(TorannMagicDefOf.TM_SummonDemon);
                        spell_SummonDemon = false;
                    }
                }
            }

            if (IsMagicUser && !Pawn.Dead & !Pawn.Downed && (Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard) || (isCustom && customClass.classMageAbilities.Contains(TorannMagicDefOf.TM_Inspire))))
            {
                if (!Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_InspirationalHD")) && MagicData.MagicPowersB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Inspire).learned)
                {
                    HealthUtility.AdjustSeverity(Pawn, HediffDef.Named("TM_InspirationalHD"), 0.95f);
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
            IEnumerable<DefModExtension_TraitEnchantments> traitEnum = Pawn.story.traits.allTraits
                .Select(t => t.def.GetModExtension<DefModExtension_TraitEnchantments>());
            foreach (DefModExtension_TraitEnchantments e in traitEnum)
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
            foreach(Hediff hd in Pawn.health.hediffSet.hediffs)
            {
                if(hd.def.GetModExtension<DefModExtension_HediffEnchantments>() != null)
                {                    
                    foreach(HediffEnchantment hdStage in hd.def.GetModExtension<DefModExtension_HediffEnchantments>().stages)
                    {
                        if(hd.Severity >= hdStage.minSeverity && hd.Severity < hdStage.maxSeverity)
                        {
                            DefModExtension_TraitEnchantments e = hdStage.enchantments;
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

            List<Apparel> apparel = Pawn.apparel.WornApparel;
            if (apparel != null)
            {
                for (int i = 0; i < Pawn.apparel.WornApparelCount; i++)
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

                            if (item.arcaneSpectre)
                            {
                                _arcaneSpectre = true;
                            }
                            if (item.phantomShift)
                            {
                                _phantomShift = true;
                            }
                        }
                    }
                }
            }
            if (Pawn.equipment != null && Pawn.equipment.Primary != null)
            {
                Enchantment.CompEnchantedItem item = Pawn.equipment.Primary.GetComp<Enchantment.CompEnchantedItem>();
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
                            _arcalleumCooldown += (Pawn.equipment.Primary.def.GetStatValueAbstract(StatDefOf.Mass, Pawn.equipment.Primary.Stuff) * (arcalleumFactor / 100f));

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
                if (Pawn.equipment.Primary.def.defName == "TM_DefenderStaff")
                {
                    if (item_StaffOfDefender == false)
                    {
                        AddPawnAbility(TorannMagicDefOf.TM_ArcaneBarrier);
                        item_StaffOfDefender = true;
                    }
                }
                else
                {
                    if (item_StaffOfDefender)
                    {
                        RemovePawnAbility(TorannMagicDefOf.TM_ArcaneBarrier);
                        item_StaffOfDefender = false;
                    }
                }
            }
            CleanupSummonedStructures();

            //Determine active or sustained hediffs and abilities
            if(SoL != null)
            {
                _maxMPUpkeep += (TorannMagicDefOf.TM_SpiritOfLight.upkeepEnergyCost * (1 - (TorannMagicDefOf.TM_SpiritOfLight.upkeepEfficiencyPercent * MagicData.MagicPowerSkill_SpiritOfLight.First(mps => mps.label == "TM_SpiritOfLight_eff").level)));
                _mpRegenRateUpkeep += (TorannMagicDefOf.TM_SpiritOfLight.upkeepRegenCost * (1 - (TorannMagicDefOf.TM_SpiritOfLight.upkeepEfficiencyPercent * MagicData.MagicPowerSkill_SpiritOfLight.First(mps => mps.label == "TM_SpiritOfLight_eff").level)));
            }
            if (summonedLights.Count > 0)
            {
                _maxMPUpkeep += (summonedLights.Count * TorannMagicDefOf.TM_Sunlight.upkeepEnergyCost);
                _mpRegenRateUpkeep += (summonedLights.Count * TorannMagicDefOf.TM_Sunlight.upkeepRegenCost);
            }
            if (summonedHeaters.Count > 0)
            {
                _maxMPUpkeep += (summonedHeaters.Count * TorannMagicDefOf.TM_Heater.upkeepEnergyCost);
            }
            if (summonedCoolers.Count > 0)
            {
                _maxMPUpkeep += (summonedCoolers.Count * TorannMagicDefOf.TM_Cooler.upkeepEnergyCost);
            }
            if (summonedPowerNodes.Count > 0)
            {
                _maxMPUpkeep += (summonedPowerNodes.Count * TorannMagicDefOf.TM_PowerNode.upkeepEnergyCost);
                _mpRegenRateUpkeep += (summonedPowerNodes.Count * TorannMagicDefOf.TM_PowerNode.upkeepRegenCost);
            }
            if (weaponEnchants.Count > 0)
            {
                _maxMPUpkeep += (weaponEnchants.Count * ActualManaCost(TorannMagicDefOf.TM_EnchantWeapon));
            }
            if (stoneskinPawns != null && stoneskinPawns.Count > 0)
            {
                _maxMPUpkeep += (stoneskinPawns.Count * (TorannMagicDefOf.TM_Stoneskin.upkeepEnergyCost - (.02f * MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Stoneskin).level)));
            }
            if (summonedSentinels != null && summonedSentinels.Count > 0)
            {
                MagicPowerSkill heartofstone = MagicData.MagicPowerSkill_Sentinel.FirstOrDefault(mps => mps.label == "TM_Sentinel_eff");

                if (heartofstone.level == 3)
                {
                    _maxMPUpkeep += (.15f * summonedSentinels.Count);
                }
                else
                {
                    _maxMPUpkeep += ((.2f - (.02f * heartofstone.level)) * summonedSentinels.Count);
                }
            }
            if(BrandPawns != null && BrandPawns.Count > 0)
            {
                float brandCost = BrandPawns.Count * (TorannMagicDefOf.TM_Branding.upkeepRegenCost * (1f - (TorannMagicDefOf.TM_Branding.upkeepEfficiencyPercent * MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Branding).level)));
                if(sigilSurging)
                {
                    brandCost *= (5f * (1f - (.1f * MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_SigilSurge).level)));
                }
                if(sigilDraining)
                {
                    brandCost *= (1.5f * (1f - (.2f * MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_SigilDrain).level)));
                }
                _mpRegenRateUpkeep += brandCost; 
            }
            if(livingWall != null && livingWall.Spawned)
            {
                _maxMPUpkeep += (TorannMagicDefOf.TM_LivingWall.upkeepEnergyCost * (1f - (TorannMagicDefOf.TM_LivingWall.upkeepEfficiencyPercent * MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_LivingWall).level)));
            }
            //Bonded spirit animal
            if (bondedSpirit != null)
            {
                _maxMPUpkeep += (TorannMagicDefOf.TM_GuardianSpirit.upkeepEnergyCost * (1f - (TorannMagicDefOf.TM_GuardianSpirit.upkeepEfficiencyPercent * MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_GuardianSpirit).level)));
                _mpRegenRateUpkeep += (TorannMagicDefOf.TM_GuardianSpirit.upkeepRegenCost * (1f - (TorannMagicDefOf.TM_GuardianSpirit.upkeepEfficiencyPercent * MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_GuardianSpirit).level)));
                if (bondedSpirit.Dead || bondedSpirit.Destroyed)
                {
                    bondedSpirit = null;
                }
                else if (bondedSpirit.Faction != null && bondedSpirit.Faction != Pawn.Faction)
                {
                    bondedSpirit = null;
                }
                else if (!bondedSpirit.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SpiritBondHD))
                {
                    HealthUtility.AdjustSeverity(bondedSpirit, TorannMagicDefOf.TM_SpiritBondHD, .5f);
                }
                if(TorannMagicDefOf.TM_SpiritCrowR == GuardianSpiritType)
                {
                    Hediff hd = Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_CrowInsightHD);
                    if(hd != null && hd.Severity != (.5f + MagicData.GetSkill_Power(TorannMagicDefOf.TM_GuardianSpirit).level))
                    {
                        Pawn.health.RemoveHediff(hd);
                        HealthUtility.AdjustSeverity(Pawn, TorannMagicDefOf.TM_CrowInsightHD, .5f + MagicData.GetSkill_Power(TorannMagicDefOf.TM_GuardianSpirit).level);
                    }
                    else
                    {
                        HealthUtility.AdjustSeverity(Pawn, TorannMagicDefOf.TM_CrowInsightHD, .5f + MagicData.GetSkill_Power(TorannMagicDefOf.TM_GuardianSpirit).level);
                    }
                }
            }
            if (enchanterStones != null && enchanterStones.Count > 0)
            {
                for (int i = 0; i < enchanterStones.Count; i++)
                {
                    if (enchanterStones[i].DestroyedOrNull())
                    {
                        enchanterStones.Remove(enchanterStones[i]);
                    }
                }
                _maxMPUpkeep += (enchanterStones.Count * (TorannMagicDefOf.TM_EnchanterStone.upkeepEnergyCost * (1f - TorannMagicDefOf.TM_EnchanterStone.upkeepEfficiencyPercent * MagicData.MagicPowerSkill_EnchanterStone.FirstOrDefault(mps => mps.label == "TM_EnchanterStone_eff").level)));
            }
            try
            {
                if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Druid) && fertileLands.Count > 0)
                {
                    _mpRegenRateUpkeep += TorannMagicDefOf.TM_FertileLands.upkeepRegenCost;
                }
            }
            catch
            {

            }
            if (Pawn.story.traits.HasTrait(TorannMagicDefOf.Lich))
            {
                if (spell_LichForm || (customClass != null && MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_LichForm).learned))
                {
                    RemovePawnAbility(TorannMagicDefOf.TM_LichForm);
                    MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_LichForm).learned = false;
                    spell_LichForm = false;
                }
                _maxMP += .5f;
                _mpRegenRate += .5f;
            }
            if (Pawn.Inspired && Pawn.Inspiration.def == TorannMagicDefOf.ID_ManaRegen)
            {
                _mpRegenRate += 1f;
            }
            if (recallSet)
            {
                _maxMPUpkeep += TorannMagicDefOf.TM_Recall.upkeepEnergyCost * (1 - (TorannMagicDefOf.TM_Recall.upkeepEfficiencyPercent * MagicData.MagicPowerSkill_Recall.FirstOrDefault(mps => mps.label == "TM_Recall_eff").level));
                _mpRegenRateUpkeep += TorannMagicDefOf.TM_Recall.upkeepRegenCost * (1 - (TorannMagicDefOf.TM_Recall.upkeepEfficiencyPercent * MagicData.MagicPowerSkill_Recall.FirstOrDefault(mps => mps.label == "TM_Recall_eff").level));
            }
            using (IEnumerator<Hediff> enumerator = Pawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Hediff rec = enumerator.Current;
                    TMAbilityDef ability = MagicData.GetHediffAbility(rec);
                    if (ability != null)
                    {
                        MagicPowerSkill skill = MagicData.GetSkill_Efficiency(ability);
                        int level = 0;
                        if (skill != null)
                        {
                            level = skill.level;
                        }
                        if (ability == TorannMagicDefOf.TM_EnchantedAura || ability == TorannMagicDefOf.TM_EnchantedBody)
                        {
                            level = MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_EnchantedBody).level;
                        }

                        _maxMPUpkeep += (ability.upkeepEnergyCost * (1f - (ability.upkeepEfficiencyPercent * level)));

                        if (ability == TorannMagicDefOf.TM_EnchantedAura || ability == TorannMagicDefOf.TM_EnchantedBody)
                        {
                            level = MagicData.GetSkill_Versatility(TorannMagicDefOf.TM_EnchantedBody).level;
                        }
                        _mpRegenRateUpkeep += (ability.upkeepRegenCost * (1f - (ability.upkeepEfficiencyPercent * level)));
                    }
                    //else
                    //{
                    //    if (Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_EntertainingHD"), false))
                    //    {
                    //        _maxMPUpkeep += .3f;
                    //    }
                    //    if (Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PredictionHD, false))
                    //    {
                    //        _mpRegenRateUpkeep += .5f * (1 - (.10f * MagicData.MagicPowerSkill_Prediction.FirstOrDefault(mps => mps.label == "TM_Prediction_eff").level));
                    //    }
                    //    if (Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Shadow_AuraHD, false))
                    //    {
                    //        _maxMPUpkeep += .4f * (1 - (.08f * MagicData.MagicPowerSkill_Shadow.FirstOrDefault(mps => mps.label == "TM_Shadow_eff").level));
                    //        _mpRegenRateUpkeep += .3f * (1 - (.08f * MagicData.MagicPowerSkill_Shadow.FirstOrDefault(mps => mps.label == "TM_Shadow_eff").level));
                    //    }
                    //    if (Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_RayOfHope_AuraHD, false))
                    //    {
                    //        _maxMPUpkeep += .4f * (1 - (.08f * MagicData.MagicPowerSkill_RayofHope.FirstOrDefault(mps => mps.label == "TM_RayofHope_eff").level));
                    //        _mpRegenRateUpkeep += .3f * (1 - (.08f * MagicData.MagicPowerSkill_RayofHope.FirstOrDefault(mps => mps.label == "TM_RayofHope_eff").level));
                    //    }
                    //    if (Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SoothingBreeze_AuraHD, false))
                    //    {
                    //        _maxMPUpkeep += .4f * (1 - (.08f * MagicData.MagicPowerSkill_Soothe.FirstOrDefault(mps => mps.label == "TM_Soothe_eff").level));
                    //        _mpRegenRateUpkeep += .3f * (1 - (.08f * MagicData.MagicPowerSkill_Soothe.FirstOrDefault(mps => mps.label == "TM_Soothe_eff").level));
                    //    }
                    //    if (Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnchantedAuraHD) || Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnchantedBodyHD))
                    //    {
                    //        _maxMPUpkeep += .2f + (1f - (.04f * MagicData.MagicPowerSkill_EnchantedBody.FirstOrDefault(mps => mps.label == "TM_EnchantedBody_eff").level));
                    //        _mpRegenRateUpkeep += .4f + (1f - (.04f * MagicData.MagicPowerSkill_EnchantedBody.FirstOrDefault(mps => mps.label == "TM_EnchantedBody_ver").level));
                    //    }
                    //    if (Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BlurHD))
                    //    {
                    //        _maxMPUpkeep += .2f;
                    //    }
                    //    if (Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MageLightHD))
                    //    {
                    //        _maxMPUpkeep += .1f;
                    //        _mpRegenRateUpkeep += .05f;
                    //    }
                    //}
                }
            }

            if (Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SS_SerumHD))
            {
                Hediff def = Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SS_SerumHD);
                _mpRegenRate -= .15f * def.CurStageIndex;
                _maxMP -= .25f;
                _arcaneRes += .15f * def.CurStageIndex;
                _arcaneDmg -= .1f * def.CurStageIndex;
            }

            //class and global modifiers
            _arcaneDmg += .01f * MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault(mps => mps.label == "TM_WandererCraft_pwr").level;
            _arcaneRes += .02f * MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault(mps => mps.label == "TM_WandererCraft_pwr").level;
            _mpCost -= .01f * MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault(mps => mps.label == "TM_WandererCraft_eff").level;
            _xpGain += .02f * MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault(mps => mps.label == "TM_WandererCraft_eff").level;
            _coolDown -= .01f * MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault(mps => mps.label == "TM_WandererCraft_ver").level;
            _mpRegenRate += .01f * MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault(mps => mps.label == "TM_WandererCraft_ver").level;
            _maxMP += .02f * MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault(mps => mps.label == "TM_WandererCraft_ver").level;

            _maxMP += .04f * MagicData.MagicPowerSkill_global_spirit.FirstOrDefault(mps => mps.label == "TM_global_spirit_pwr").level;
            _mpRegenRate += .05f * MagicData.MagicPowerSkill_global_regen.FirstOrDefault(mps => mps.label == "TM_global_regen_pwr").level;
            _mpCost -= .025f * MagicData.MagicPowerSkill_global_eff.FirstOrDefault(mps => mps.label == "TM_global_eff_pwr").level;
            _arcaneRes += (1f - Pawn.GetStatValue(StatDefOf.PsychicSensitivity, false)) / 2f;
            _arcaneDmg += (Pawn.GetStatValue(StatDefOf.PsychicSensitivity, false) - 1f) / 4f;

            arcalleumCooldown = Mathf.Clamp(0f + _arcalleumCooldown, 0f, Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_BoundlessTD) ? .1f : .5f);

            float val = 1f - .03f * MagicData.MagicPowerSkill_Cantrips.FirstOrDefault(mps => mps.label == "TM_Cantrips_eff").level;
            _maxMPUpkeep *= val;
            _mpRegenRateUpkeep *= val;

            //resolve upkeep costs
            _maxMP -= (_maxMPUpkeep);
            _mpRegenRate -= (_mpRegenRateUpkeep);

            //finalize
            maxMP = Mathf.Clamp(1f + _maxMP, 0f, 5f);
            mpRegenRate = 1f + _mpRegenRate;
            coolDown = Mathf.Clamp(1f + _coolDown, 0.25f, 10f);
            xpGain = Mathf.Clamp(1f + _xpGain, 0.01f, 5f);
            mpCost = Mathf.Clamp(1f + _mpCost, 0.1f, 5f);
            arcaneRes = Mathf.Clamp(1 + _arcaneRes, 0.01f, 5f);
            arcaneDmg = 1 + _arcaneDmg;

            if (IsMagicUser && !TM_Calc.IsCrossClass(Pawn, true))
            {
                if (maxMP != 1f && !Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_maxEnergy")))
                {
                    HealthUtility.AdjustSeverity(Pawn, HediffDef.Named("TM_HediffEnchantment_maxEnergy"), .5f);
                }
                if (mpRegenRate != 1f && !Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_energyRegen")))
                {
                    HealthUtility.AdjustSeverity(Pawn, HediffDef.Named("TM_HediffEnchantment_energyRegen"), .5f);
                }
                if (coolDown != 1f && !Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_coolDown")))
                {
                    HealthUtility.AdjustSeverity(Pawn, HediffDef.Named("TM_HediffEnchantment_coolDown"), .5f);
                }
                if (xpGain != 1f && !Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_xpGain")))
                {
                    HealthUtility.AdjustSeverity(Pawn, HediffDef.Named("TM_HediffEnchantment_xpGain"), .5f);
                }
                if (mpCost != 1f && !Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_energyCost")))
                {
                    HealthUtility.AdjustSeverity(Pawn, HediffDef.Named("TM_HediffEnchantment_energyCost"), .5f);
                }
                if (arcaneRes != 1f && !Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_dmgResistance")))
                {
                    HealthUtility.AdjustSeverity(Pawn, HediffDef.Named("TM_HediffEnchantment_dmgResistance"), .5f);
                }
                if (arcaneDmg != 1f && !Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_dmgBonus")))
                {
                    HealthUtility.AdjustSeverity(Pawn, HediffDef.Named("TM_HediffEnchantment_dmgBonus"), .5f);
                }
                if(_arcalleumCooldown != 0 && !Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_arcalleumCooldown")))
                {
                    HealthUtility.AdjustSeverity(Pawn, HediffDef.Named("TM_HediffEnchantment_arcalleumCooldown"), .5f);
                }
                if (_arcaneSpectre && !Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_arcaneSpectre")))
                {
                    HealthUtility.AdjustSeverity(Pawn, HediffDef.Named("TM_HediffEnchantment_arcaneSpectre"), .5f);
                }
                else if(_arcaneSpectre == false && Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_arcaneSpectre")))
                {
                    Pawn.health.RemoveHediff(Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_HediffEnchantment_arcaneSpectre")));
                }
                if (_phantomShift)
                {
                    HealthUtility.AdjustSeverity(Pawn, HediffDef.Named("TM_HediffEnchantment_phantomShift"), .5f);
                }
                else if (_phantomShift == false && Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_phantomShift")))
                {
                    Pawn.health.RemoveHediff(Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_HediffEnchantment_phantomShift")));
                }               
            }
        }

        private void CleanupSummonedStructures()
        {
            for (int i = 0; i < summonedLights.Count; i++)
            {
                if (summonedLights[i].DestroyedOrNull())
                {
                    summonedLights.Remove(summonedLights[i]);
                    i--;
                }
            }
            for (int i = 0; i < summonedHeaters.Count; i++)
            {
                if (summonedHeaters[i].DestroyedOrNull())
                {
                    summonedHeaters.Remove(summonedHeaters[i]);
                    i--;
                }
            }
            for (int i = 0; i < summonedCoolers.Count; i++)
            {
                if (summonedCoolers[i].DestroyedOrNull())
                {
                    summonedCoolers.Remove(summonedCoolers[i]);
                    i--;
                }
            }
            for (int i = 0; i < summonedPowerNodes.Count; i++)
            {
                if (summonedPowerNodes[i].DestroyedOrNull())
                {
                    summonedPowerNodes.Remove(summonedPowerNodes[i]);
                    i--;
                }
            }
            for (int i = 0; i < lightningTraps.Count; i++)
            {
                if (lightningTraps[i].DestroyedOrNull())
                {
                    lightningTraps.Remove(lightningTraps[i]);
                    i--;
                }
            }
        }

        public override void PostExposeData()
        {
            //base.PostExposeData();            
            Scribe_Values.Look<bool>(ref magicPowersInitialized, "magicPowersInitialized");
            Scribe_Values.Look<bool>(ref magicPowersInitializedForColonist, "magicPowersInitializedForColonist", true);
            Scribe_Values.Look<bool>(ref colonistPowerCheck, "colonistPowerCheck", true);
            Scribe_Values.Look<bool>(ref spell_Rain, "spell_Rain");
            Scribe_Values.Look<bool>(ref spell_Blink, "spell_Blink");
            Scribe_Values.Look<bool>(ref spell_Teleport, "spell_Teleport");
            Scribe_Values.Look<bool>(ref spell_Heal, "spell_Heal");
            Scribe_Values.Look<bool>(ref spell_Heater, "spell_Heater");
            Scribe_Values.Look<bool>(ref spell_Cooler, "spell_Cooler");
            Scribe_Values.Look<bool>(ref spell_PowerNode, "spell_PowerNode");
            Scribe_Values.Look<bool>(ref spell_Sunlight, "spell_Sunlight");
            Scribe_Values.Look<bool>(ref spell_DryGround, "spell_DryGround");
            Scribe_Values.Look<bool>(ref spell_WetGround, "spell_WetGround");
            Scribe_Values.Look<bool>(ref spell_ChargeBattery, "spell_ChargeBattery");
            Scribe_Values.Look<bool>(ref spell_SmokeCloud, "spell_SmokeCloud");
            Scribe_Values.Look<bool>(ref spell_Extinguish, "spell_Extinguish");
            Scribe_Values.Look<bool>(ref spell_EMP, "spell_EMP");
            Scribe_Values.Look<bool>(ref spell_Blizzard, "spell_Blizzard");
            Scribe_Values.Look<bool>(ref spell_Firestorm, "spell_Firestorm");
            Scribe_Values.Look<bool>(ref spell_EyeOfTheStorm, "spell_EyeOfTheStorm");
            Scribe_Values.Look<bool>(ref spell_SummonMinion, "spell_SummonMinion");
            Scribe_Values.Look<bool>(ref spell_TransferMana, "spell_TransferMana");
            Scribe_Values.Look<bool>(ref spell_SiphonMana, "spell_SiphonMana");
            Scribe_Values.Look<bool>(ref spell_RegrowLimb, "spell_RegrowLimb");
            Scribe_Values.Look<bool>(ref spell_ManaShield, "spell_ManaShield");
            Scribe_Values.Look<bool>(ref spell_FoldReality, "spell_FoldReality");
            Scribe_Values.Look<bool>(ref spell_Resurrection, "spell_Resurrection");
            Scribe_Values.Look<bool>(ref spell_HolyWrath, "spell_HolyWrath");
            Scribe_Values.Look<bool>(ref spell_LichForm, "spell_LichForm");
            Scribe_Values.Look<bool>(ref spell_Flight, "spell_Flight");
            Scribe_Values.Look<bool>(ref spell_SummonPoppi, "spell_SummonPoppi");
            Scribe_Values.Look<bool>(ref spell_BattleHymn, "spell_BattleHymn");
            Scribe_Values.Look<bool>(ref spell_FertileLands, "spell_FertileLands");
            Scribe_Values.Look<bool>(ref spell_CauterizeWound, "spell_CauterizeWound");
            Scribe_Values.Look<bool>(ref spell_SpellMending, "spell_SpellMending");
            Scribe_Values.Look<bool>(ref spell_PsychicShock, "spell_PsychicShock");
            Scribe_Values.Look<bool>(ref spell_Scorn, "spell_Scorn");
            Scribe_Values.Look<bool>(ref spell_Meteor, "spell_Meteor");
            Scribe_Values.Look<bool>(ref spell_Teach, "spell_Teach");
            Scribe_Values.Look<bool>(ref spell_OrbitalStrike, "spell_OrbitalStrike");
            Scribe_Values.Look<bool>(ref spell_BloodMoon, "spell_BloodMoon");
            Scribe_Values.Look<bool>(ref spell_Shapeshift, "spell_Shapeshift");
            Scribe_Values.Look<bool>(ref spell_ShapeshiftDW, "spell_ShapeshiftDW");
            Scribe_Values.Look<bool>(ref spell_Blur, "spell_Blur");
            Scribe_Values.Look<bool>(ref spell_BlankMind, "spell_BlankMind");
            Scribe_Values.Look<bool>(ref spell_DirtDevil, "spell_DirtDevil");
            Scribe_Values.Look<bool>(ref spell_ArcaneBolt, "spell_ArcaneBolt");
            Scribe_Values.Look<bool>(ref spell_LightningTrap, "spell_LightningTrap");
            Scribe_Values.Look<bool>(ref spell_Invisibility, "spell_Invisibility");
            Scribe_Values.Look<bool>(ref spell_BriarPatch, "spell_BriarPatch");
            Scribe_Values.Look<bool>(ref spell_MechaniteReprogramming, "spell_MechaniteReprogramming");
            Scribe_Values.Look<bool>(ref spell_Recall, "spell_Recall");
            Scribe_Values.Look<bool>(ref spell_MageLight, "spell_MageLight");
            Scribe_Values.Look<bool>(ref spell_SnapFreeze, "spell_SnapFreeze");
            Scribe_Values.Look<bool>(ref spell_Ignite, "spell_Ignite");
            Scribe_Values.Look<bool>(ref spell_HeatShield, "spell_HeatShield");
            Scribe_Values.Look<bool>(ref useTechnoBitToggle, "useTechnoBitToggle", true);
            Scribe_Values.Look<bool>(ref useTechnoBitRepairToggle, "useTechnoBitRepairToggle", true);
            Scribe_Values.Look<bool>(ref useElementalShotToggle, "useElementalShotToggle", true);
            Scribe_Values.Look<int>(ref powerModifier, "powerModifier");
            Scribe_Values.Look<int>(ref technoWeaponDefNum, "technoWeaponDefNum");
            Scribe_Values.Look<bool>(ref doOnce, "doOnce", true);
            Scribe_Values.Look<int>(ref predictionTick, "predictionTick");
            Scribe_Values.Look<int>(ref predictionHash, "predictionHash");
            Scribe_References.Look<Thing>(ref mageLightThing, "mageLightThing");
            Scribe_Values.Look<bool>(ref mageLightActive, "mageLightActive");
            Scribe_Values.Look<bool>(ref mageLightSet, "mageLightSet");
            Scribe_Values.Look<bool>(ref deathRetaliating, "deathRetaliating");
            Scribe_Values.Look<bool>(ref canDeathRetaliate, "canDeathRetaliate");
            Scribe_Values.Look<int>(ref ticksTillRetaliation, "ticksTillRetaliation", 600);
            Scribe_Defs.Look<IncidentDef>(ref predictionIncidentDef, "predictionIncidentDef");
            Scribe_References.Look<Pawn>(ref soulBondPawn, "soulBondPawn");
            //Scribe_References.Look<Thing>(ref technoWeaponThing, "technoWeaponThing");
            Scribe_Defs.Look<ThingDef>(ref technoWeaponThingDef, "technoWeaponThingDef");
            Scribe_Values.Look<QualityCategory>(ref technoWeaponQC, "technoWeaponQC");
            Scribe_References.Look<Thing>(ref enchanterStone, "enchanterStone");
            Scribe_Collections.Look<Thing>(ref enchanterStones, "enchanterStones", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref summonedMinions, "summonedMinions", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref supportedUndead, "supportedUndead", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref summonedLights, "summonedLights", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref summonedPowerNodes, "summonedPowerNodes", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref summonedCoolers, "summonedCoolers", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref summonedHeaters, "summonedHeaters", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref summonedSentinels, "summonedSentinels", LookMode.Reference);
            Scribe_Collections.Look<Pawn>(ref stoneskinPawns, "stoneskinPawns", LookMode.Reference);
            Scribe_Collections.Look<Pawn>(ref weaponEnchants, "weaponEnchants", LookMode.Reference);
            Scribe_Collections.Look<Thing>(ref lightningTraps, "lightningTraps", LookMode.Reference);
            Scribe_Collections.Look<Pawn>(ref hexedPawns, "hexedPawns", LookMode.Reference);
            Scribe_Values.Look<IntVec3>(ref earthSprites, "earthSprites");
            Scribe_Values.Look<int>(ref earthSpriteType, "earthSpriteType");
            Scribe_References.Look<Map>(ref earthSpriteMap, "earthSpriteMap");
            Scribe_Values.Look<bool>(ref earthSpritesInArea, "earthSpritesInArea");
            Scribe_Values.Look<int>(ref nextEarthSpriteAction, "nextEarthSpriteAction");
            Scribe_Collections.Look<IntVec3>(ref fertileLands, "fertileLands", LookMode.Value);
            Scribe_Values.Look<float>(ref maxMP, "maxMP", 1f);
            Scribe_Values.Look<int>(ref lastChaosTraditionTick, "lastChaosTraditionTick");
            //Scribe_Collections.Look<TM_ChaosPowers>(ref chaosPowers, "chaosPowers", LookMode.Deep, new object[0]);
            //Recall variables 
            Scribe_Values.Look<bool>(ref recallSet, "recallSet");
            Scribe_Values.Look<bool>(ref recallSpell, "recallSpell");
            Scribe_Values.Look<int>(ref recallExpiration, "recallExpiration");
            Scribe_Values.Look<IntVec3>(ref recallPosition, "recallPosition");
            Scribe_References.Look<Map>(ref recallMap, "recallMap");
            Scribe_Collections.Look<string>(ref recallNeedDefnames, "recallNeedDefnames", LookMode.Value);
            Scribe_Collections.Look<float>(ref recallNeedValues, "recallNeedValues", LookMode.Value);
            Scribe_Collections.Look<Hediff>(ref recallHediffList, "recallHediffList", LookMode.Deep);
            Scribe_Collections.Look<float>(ref recallHediffDefSeverityList, "recallHediffSeverityList", LookMode.Value);
            Scribe_Collections.Look<int>(ref recallHediffDefTicksRemainingList, "recallHediffDefTicksRemainingList", LookMode.Value);
            Scribe_Collections.Look<Hediff_Injury>(ref recallInjuriesList, "recallInjuriesList", LookMode.Deep);
            Scribe_References.Look<FlyingObject_SpiritOfLight>(ref SoL, "SoL");
            Scribe_Defs.Look<ThingDef>(ref guardianSpiritType, "guardianSpiritType");
            Scribe_References.Look<Pawn>(ref bondedSpirit, "bondedSpirit");
            Scribe_Collections.Look<Pawn>(ref brands, "brands", LookMode.Reference);
            Scribe_Collections.Look<HediffDef>(ref brandDefs, "brandDefs", LookMode.Def);
            Scribe_Values.Look<bool>(ref sigilSurging, "sigilSurging");
            Scribe_Values.Look<bool>(ref sigilDraining, "sigilDraining");
            Scribe_References.Look<FlyingObject_LivingWall>(ref livingWall, "livingWall");
            Scribe_Deep.Look(ref magicWardrobe, "magicWardrobe", Array.Empty<object>());
            //
            Scribe_Deep.Look<MagicData>(ref magicData, "magicData", new object[]
            {
                this
            });
            bool flag11 = Scribe.mode == LoadSaveMode.PostLoadInit;
            if (flag11)
            {
                Pawn abilityUser = Pawn;
                int index = TM_ClassUtility.CustomClassIndexOfBaseMageClass(abilityUser.story.traits.allTraits);
                if (index >= 0)
                {                   
                    customClass = TM_ClassUtility.CustomClasses[index];
                    customIndex = index;
                    LoadCustomClassAbilities(customClass);
                }                
                else
                {
                    bool flagCM = abilityUser.story.traits.HasTrait(TorannMagicDefOf.ChaosMage);
                    bool flag40 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.InnerFire) || flagCM;
                    if (flag40)
                    {
                        bool flag14 = !MagicData.MagicPowersIF.NullOrEmpty();
                        if (flag14)
                        {
                            //LoadPowers();
                            foreach (MagicPower current3 in MagicData.MagicPowersIF)
                            {
                                if (current3.abilityDef == null) continue;
                                if (!current3.learned || (current3.abilityDef != TorannMagicDefOf.TM_RayofHope &&
                                                          current3.abilityDef != TorannMagicDefOf.TM_RayofHope_I &&
                                                          current3.abilityDef != TorannMagicDefOf.TM_RayofHope_II &&
                                                          current3.abilityDef != TorannMagicDefOf.TM_RayofHope_III))
                                    continue;
                                switch (current3.level)
                                {
                                    case 0:
                                        AddPawnAbility(TorannMagicDefOf.TM_RayofHope);
                                        break;
                                    case 1:
                                        AddPawnAbility(TorannMagicDefOf.TM_RayofHope_I);
                                        break;
                                    case 2:
                                        AddPawnAbility(TorannMagicDefOf.TM_RayofHope_II);
                                        break;
                                    default:
                                        AddPawnAbility(TorannMagicDefOf.TM_RayofHope_III);
                                        break;
                                }
                            }
                        }
                    }
                    bool flag41 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost) || flagCM;
                    if (flag41)
                    {
                        if (!MagicData.MagicPowersHoF.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current4 in MagicData.MagicPowersHoF)
                            {
                                if (current4.abilityDef == null) continue;
                                if (current4.learned && (current4.abilityDef == TorannMagicDefOf.TM_Soothe ||
                                                         current4.abilityDef == TorannMagicDefOf.TM_Soothe_I ||
                                                         current4.abilityDef == TorannMagicDefOf.TM_Soothe_II ||
                                                         current4.abilityDef == TorannMagicDefOf.TM_Soothe_III))
                                {
                                    switch (current4.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_Soothe);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_Soothe_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_Soothe_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_Soothe_III);
                                            break;
                                    }
                                }

                                if (!current4.learned || (current4.abilityDef != TorannMagicDefOf.TM_FrostRay &&
                                                          current4.abilityDef != TorannMagicDefOf.TM_FrostRay_I &&
                                                          current4.abilityDef != TorannMagicDefOf.TM_FrostRay_II &&
                                                          current4.abilityDef != TorannMagicDefOf.TM_FrostRay_III))
                                    continue;
                                switch (current4.level)
                                {
                                    case 0:
                                        AddPawnAbility(TorannMagicDefOf.TM_FrostRay);
                                        break;
                                    case 1:
                                        AddPawnAbility(TorannMagicDefOf.TM_FrostRay_I);
                                        break;
                                    case 2:
                                        AddPawnAbility(TorannMagicDefOf.TM_FrostRay_II);
                                        break;
                                    default:
                                        AddPawnAbility(TorannMagicDefOf.TM_FrostRay_III);
                                        break;
                                }
                            }
                        }
                    }
                    bool flag42 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.StormBorn) || flagCM;
                    if (flag42)
                    {
                        if (!MagicData.MagicPowersSB.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current5 in MagicData.MagicPowersSB)
                            {
                                if (!current5.learned || (current5.abilityDef != TorannMagicDefOf.TM_AMP &&
                                                          current5.abilityDef != TorannMagicDefOf.TM_AMP_I &&
                                                          current5.abilityDef != TorannMagicDefOf.TM_AMP_II &&
                                                          current5.abilityDef != TorannMagicDefOf.TM_AMP_III)) continue;
                                switch (current5.level)
                                {
                                    case 0:
                                        AddPawnAbility(TorannMagicDefOf.TM_AMP);
                                        break;
                                    case 1:
                                        AddPawnAbility(TorannMagicDefOf.TM_AMP_I);
                                        break;
                                    case 2:
                                        AddPawnAbility(TorannMagicDefOf.TM_AMP_II);
                                        break;
                                    default:
                                        AddPawnAbility(TorannMagicDefOf.TM_AMP_III);
                                        break;
                                }
                            }
                        }
                    }
                    bool flag43 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Arcanist) || flagCM;
                    if (flag43)
                    {
                        if (!MagicData.MagicPowersA.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current6 in MagicData.MagicPowersA)
                            {
                                if (current6.abilityDef == null) continue;
                                if (current6.learned && (current6.abilityDef == TorannMagicDefOf.TM_Shadow || current6.abilityDef == TorannMagicDefOf.TM_Shadow_I || current6.abilityDef == TorannMagicDefOf.TM_Shadow_II || current6.abilityDef == TorannMagicDefOf.TM_Shadow_III))
                                {
                                    switch (current6.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_Shadow);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_Shadow_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_Shadow_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_Shadow_III);
                                            break;
                                    }
                                }
                                if (current6.learned && (current6.abilityDef == TorannMagicDefOf.TM_MagicMissile || current6.abilityDef == TorannMagicDefOf.TM_MagicMissile_I || current6.abilityDef == TorannMagicDefOf.TM_MagicMissile_II || current6.abilityDef == TorannMagicDefOf.TM_MagicMissile_III))
                                {
                                    switch (current6.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_MagicMissile);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_MagicMissile_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_MagicMissile_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_MagicMissile_III);
                                            break;
                                    }
                                }
                                if (current6.learned && (current6.abilityDef == TorannMagicDefOf.TM_Blink || current6.abilityDef == TorannMagicDefOf.TM_Blink_I || current6.abilityDef == TorannMagicDefOf.TM_Blink_II || current6.abilityDef == TorannMagicDefOf.TM_Blink_III))
                                {
                                    switch (current6.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_Blink);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_Blink_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_Blink_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_Blink_III);
                                            break;
                                    }
                                }
                                if (current6.learned && (current6.abilityDef == TorannMagicDefOf.TM_Summon || current6.abilityDef == TorannMagicDefOf.TM_Summon_I || current6.abilityDef == TorannMagicDefOf.TM_Summon_II || current6.abilityDef == TorannMagicDefOf.TM_Summon_III))
                                {
                                    switch (current6.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_Summon);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_Summon_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_Summon_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_Summon_III);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    bool flag44 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Paladin) || flagCM;
                    if (flag44)
                    {
                        if (!MagicData.MagicPowersP.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current7 in MagicData.MagicPowersP)
                            {
                                if (current7.abilityDef == null) continue;
                                if (current7.learned && (current7.abilityDef == TorannMagicDefOf.TM_Shield || current7.abilityDef == TorannMagicDefOf.TM_Shield_I || current7.abilityDef == TorannMagicDefOf.TM_Shield_II || current7.abilityDef == TorannMagicDefOf.TM_Shield_III))
                                {
                                    switch (current7.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_Shield);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_Shield_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_Shield_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_Shield_III);
                                            break;
                                    }
                                }
                                if (current7.learned && (current7.abilityDef == TorannMagicDefOf.TM_P_RayofHope || current7.abilityDef == TorannMagicDefOf.TM_P_RayofHope_I || current7.abilityDef == TorannMagicDefOf.TM_P_RayofHope_II || current7.abilityDef == TorannMagicDefOf.TM_P_RayofHope_III))
                                {
                                    switch (current7.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_P_RayofHope);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_P_RayofHope_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_P_RayofHope_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_P_RayofHope_III);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    bool flag45 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Summoner) || flagCM;
                    if (flag45)
                    {
                        if (!MagicData.MagicPowersS.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current8 in MagicData.MagicPowersS)
                            {
                                if (current8.abilityDef != null)
                                {
                                    //if ((current7.abilityDef == TorannMagicDefOf.TM_Shield || current7.abilityDef == TorannMagicDefOf.TM_Shield_I || current7.abilityDef == TorannMagicDefOf.TM_Shield_II || current7.abilityDef == TorannMagicDefOf.TM_Shield_III))
                                    //{
                                    //    if (current7.level == 0)
                                    //    {
                                    //        AddPawnAbility(TorannMagicDefOf.TM_Shield);
                                    //    }
                                    //    else if (current7.level == 1)
                                    //    {
                                    //        AddPawnAbility(TorannMagicDefOf.TM_Shield_I);
                                    //    }
                                    //    else if (current7.level == 2)
                                    //    {
                                    //        AddPawnAbility(TorannMagicDefOf.TM_Shield_II);
                                    //    }
                                    //    else
                                    //    {
                                    //        AddPawnAbility(TorannMagicDefOf.TM_Shield_III);
                                    //    }
                                    //}
                                }
                            }
                        }
                    }
                    bool flag46 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Druid) || flagCM;
                    if (flag46)
                    {
                        if (!MagicData.MagicPowersD.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current9 in MagicData.MagicPowersD)
                            {
                                if (current9.abilityDef == null) continue;
                                if (current9.learned && (current9.abilityDef == TorannMagicDefOf.TM_SootheAnimal || current9.abilityDef == TorannMagicDefOf.TM_SootheAnimal_I || current9.abilityDef == TorannMagicDefOf.TM_SootheAnimal_II || current9.abilityDef == TorannMagicDefOf.TM_SootheAnimal_III))
                                {
                                    switch (current9.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_SootheAnimal);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_SootheAnimal_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_SootheAnimal_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_SootheAnimal_III);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    bool flag47 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || abilityUser.story.traits.HasTrait(TorannMagicDefOf.Lich) || flagCM;
                    if (flag47)
                    {
                        if (!MagicData.MagicPowersN.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current10 in MagicData.MagicPowersN)
                            {
                                if (current10.abilityDef == null) continue;
                                if (current10.learned && (current10.abilityDef == TorannMagicDefOf.TM_DeathMark || current10.abilityDef == TorannMagicDefOf.TM_DeathMark_I || current10.abilityDef == TorannMagicDefOf.TM_DeathMark_II || current10.abilityDef == TorannMagicDefOf.TM_DeathMark_III))
                                {
                                    switch (current10.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_DeathMark);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_DeathMark_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_DeathMark_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_DeathMark_III);
                                            break;
                                    }
                                }
                                if (current10.learned && (current10.abilityDef == TorannMagicDefOf.TM_ConsumeCorpse || current10.abilityDef == TorannMagicDefOf.TM_ConsumeCorpse_I || current10.abilityDef == TorannMagicDefOf.TM_ConsumeCorpse_II || current10.abilityDef == TorannMagicDefOf.TM_ConsumeCorpse_III))
                                {
                                    switch (current10.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_ConsumeCorpse);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_ConsumeCorpse_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_ConsumeCorpse_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_ConsumeCorpse_III);
                                            break;
                                    }
                                }
                                if (current10.learned && (current10.abilityDef == TorannMagicDefOf.TM_CorpseExplosion || current10.abilityDef == TorannMagicDefOf.TM_CorpseExplosion_I || current10.abilityDef == TorannMagicDefOf.TM_CorpseExplosion_II || current10.abilityDef == TorannMagicDefOf.TM_CorpseExplosion_III))
                                {
                                    switch (current10.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_CorpseExplosion);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_CorpseExplosion_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_CorpseExplosion_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_CorpseExplosion_III);
                                            break;
                                    }
                                }
                                if (abilityUser.story.traits.HasTrait(TorannMagicDefOf.Lich) && (current10.learned && (current10.abilityDef == TorannMagicDefOf.TM_DeathBolt || current10.abilityDef == TorannMagicDefOf.TM_DeathBolt_I || current10.abilityDef == TorannMagicDefOf.TM_DeathBolt_II || current10.abilityDef == TorannMagicDefOf.TM_DeathBolt_III)))
                                {
                                    switch (current10.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_DeathBolt);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_DeathBolt_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_DeathBolt_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_DeathBolt_III);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    bool flag48 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Priest) || flagCM;
                    if (flag48)
                    {
                        if (!MagicData.MagicPowersPR.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current11 in MagicData.MagicPowersPR)
                            {
                                if (current11.abilityDef != null)
                                {
                                    if (current11.learned && (current11.abilityDef == TorannMagicDefOf.TM_HealingCircle || current11.abilityDef == TorannMagicDefOf.TM_HealingCircle_I || current11.abilityDef == TorannMagicDefOf.TM_HealingCircle_II || current11.abilityDef == TorannMagicDefOf.TM_HealingCircle_III))
                                    {
                                        switch (current11.level)
                                        {
                                            case 0:
                                                AddPawnAbility(TorannMagicDefOf.TM_HealingCircle);
                                                break;
                                            case 1:
                                                AddPawnAbility(TorannMagicDefOf.TM_HealingCircle_I);
                                                break;
                                            case 2:
                                                AddPawnAbility(TorannMagicDefOf.TM_HealingCircle_II);
                                                break;
                                            default:
                                                AddPawnAbility(TorannMagicDefOf.TM_HealingCircle_III);
                                                break;
                                        }
                                    }
                                    if (current11.learned && (current11.abilityDef == TorannMagicDefOf.TM_BestowMight || current11.abilityDef == TorannMagicDefOf.TM_BestowMight_I || current11.abilityDef == TorannMagicDefOf.TM_BestowMight_II || current11.abilityDef == TorannMagicDefOf.TM_BestowMight_III))
                                    {
                                        switch (current11.level)
                                        {
                                            case 0:
                                                AddPawnAbility(TorannMagicDefOf.TM_BestowMight);
                                                break;
                                            case 1:
                                                AddPawnAbility(TorannMagicDefOf.TM_BestowMight_I);
                                                break;
                                            case 2:
                                                AddPawnAbility(TorannMagicDefOf.TM_BestowMight_II);
                                                break;
                                            default:
                                                AddPawnAbility(TorannMagicDefOf.TM_BestowMight_III);
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag49 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Bard) || flagCM;
                    if (flag49)
                    {
                        bool flag35 = !MagicData.MagicPowersB.NullOrEmpty();
                        if (flag35)
                        {
                            //LoadPowers();
                            foreach (MagicPower current12 in MagicData.MagicPowersB)
                            {
                                if (current12.abilityDef == null) continue;
                                if (current12.learned && (current12.abilityDef == TorannMagicDefOf.TM_Lullaby || current12.abilityDef == TorannMagicDefOf.TM_Lullaby_I || current12.abilityDef == TorannMagicDefOf.TM_Lullaby_II || current12.abilityDef == TorannMagicDefOf.TM_Lullaby_III))
                                {
                                    switch (current12.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_Lullaby);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_Lullaby_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_Lullaby_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_Lullaby_III);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    bool flag50 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Succubus) || flagCM;
                    if (flag50)
                    {
                        if (!MagicData.MagicPowersSD.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current13 in MagicData.MagicPowersSD)
                            {
                                if (current13.abilityDef == null) continue;
                                if (current13.learned && (current13.abilityDef == TorannMagicDefOf.TM_ShadowBolt || current13.abilityDef == TorannMagicDefOf.TM_ShadowBolt_I || current13.abilityDef == TorannMagicDefOf.TM_ShadowBolt_II || current13.abilityDef == TorannMagicDefOf.TM_ShadowBolt_III))
                                {
                                    switch (current13.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt_III);
                                            break;
                                    }
                                }
                                if (current13.learned && (current13.abilityDef == TorannMagicDefOf.TM_Attraction || current13.abilityDef == TorannMagicDefOf.TM_Attraction_I || current13.abilityDef == TorannMagicDefOf.TM_Attraction_II || current13.abilityDef == TorannMagicDefOf.TM_Attraction_III))
                                {
                                    switch (current13.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_Attraction);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_Attraction_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_Attraction_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_Attraction_III);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    bool flag51 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Warlock) || flagCM;
                    if (flag51)
                    {
                        if (!MagicData.MagicPowersWD.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current14 in MagicData.MagicPowersWD)
                            {
                                if (current14.abilityDef == null) continue;
                                if (current14.learned && (current14.abilityDef == TorannMagicDefOf.TM_ShadowBolt || current14.abilityDef == TorannMagicDefOf.TM_ShadowBolt_I || current14.abilityDef == TorannMagicDefOf.TM_ShadowBolt_II || current14.abilityDef == TorannMagicDefOf.TM_ShadowBolt_III))
                                {
                                    switch (current14.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt_III);
                                            break;
                                    }
                                }
                                if (current14.learned && (current14.abilityDef == TorannMagicDefOf.TM_Repulsion || current14.abilityDef == TorannMagicDefOf.TM_Repulsion_I || current14.abilityDef == TorannMagicDefOf.TM_Repulsion_II || current14.abilityDef == TorannMagicDefOf.TM_Repulsion_III))
                                {
                                    switch (current14.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_Repulsion);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_Repulsion_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_Repulsion_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_Repulsion_III);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    bool flag52 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Geomancer) || flagCM;
                    if (flag52)
                    {
                        if (!MagicData.MagicPowersG.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current15 in MagicData.MagicPowersG)
                            {
                                if (current15.abilityDef == null) continue;
                                if (current15.learned && (current15.abilityDef == TorannMagicDefOf.TM_Encase || current15.abilityDef == TorannMagicDefOf.TM_Encase_I || current15.abilityDef == TorannMagicDefOf.TM_Encase_II || current15.abilityDef == TorannMagicDefOf.TM_Encase_III))
                                {
                                    switch (current15.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_Encase);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_Encase_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_Encase_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_Encase_III);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    bool flag53 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || flagCM;
                    if (flag53)
                    {
                        if (!MagicData.MagicPowersT.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current16 in MagicData.MagicPowersT)
                            {
                                if (current16.abilityDef != null)
                                {

                                }
                            }
                        }
                    }
                    bool flag54 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.BloodMage);
                    if (flag54)
                    {
                        if (!MagicData.MagicPowersBM.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current16 in MagicData.MagicPowersBM)
                            {
                                if (current16.abilityDef == null) continue;
                                if (current16.learned && (current16.abilityDef == TorannMagicDefOf.TM_Rend || current16.abilityDef == TorannMagicDefOf.TM_Rend_I || current16.abilityDef == TorannMagicDefOf.TM_Rend_II || current16.abilityDef == TorannMagicDefOf.TM_Rend_III))
                                {
                                    switch (current16.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_Rend);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_Rend_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_Rend_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_Rend_III);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    bool flag55 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Enchanter) || flagCM;
                    if (flag55)
                    {
                        if (!MagicData.MagicPowersE.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current17 in MagicData.MagicPowersE)
                            {
                                if (current17.abilityDef == null) continue;
                                if (current17.learned && (current17.abilityDef == TorannMagicDefOf.TM_Polymorph || current17.abilityDef == TorannMagicDefOf.TM_Polymorph_I || current17.abilityDef == TorannMagicDefOf.TM_Polymorph_II || current17.abilityDef == TorannMagicDefOf.TM_Polymorph_III))
                                {
                                    switch (current17.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_Polymorph);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_Polymorph_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_Polymorph_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_Polymorph_III);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    bool flag56 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Chronomancer) || flagCM;
                    if (flag56)
                    {
                        if (!MagicData.MagicPowersC.NullOrEmpty())
                        {
                            //LoadPowers();
                            foreach (MagicPower current18 in MagicData.MagicPowersC)
                            {
                                if (current18.abilityDef == null) continue;
                                if (current18.learned && (current18.abilityDef == TorannMagicDefOf.TM_ChronostaticField || current18.abilityDef == TorannMagicDefOf.TM_ChronostaticField_I || current18.abilityDef == TorannMagicDefOf.TM_ChronostaticField_II || current18.abilityDef == TorannMagicDefOf.TM_ChronostaticField_III))
                                {
                                    switch (current18.level)
                                    {
                                        case 0:
                                            AddPawnAbility(TorannMagicDefOf.TM_ChronostaticField);
                                            break;
                                        case 1:
                                            AddPawnAbility(TorannMagicDefOf.TM_ChronostaticField_I);
                                            break;
                                        case 2:
                                            AddPawnAbility(TorannMagicDefOf.TM_ChronostaticField_II);
                                            break;
                                        default:
                                            AddPawnAbility(TorannMagicDefOf.TM_ChronostaticField_III);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    if (flag40)
                    {
                        //Log.Message("Loading Inner Fire Abilities");
                        MagicPower mpIF = MagicData.MagicPowersIF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Firebolt);
                        if (mpIF.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Firebolt);
                        }
                        mpIF = MagicData.MagicPowersIF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Fireclaw);
                        if (mpIF.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Fireclaw);
                        }
                        mpIF = MagicData.MagicPowersIF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Fireball);
                        if (mpIF.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Fireball);
                        }
                    }
                    if (flag41)
                    {
                        //Log.Message("Loading Heart of Frost Abilities");
                        MagicPower mpHoF = MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Icebolt);
                        if (mpHoF.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Icebolt);
                        }
                        mpHoF = MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Snowball);
                        if (mpHoF.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Snowball);
                        }
                        mpHoF = MagicData.MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Rainmaker);
                        if (mpHoF.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Rainmaker);
                        }

                    }
                    if (flag42)
                    {
                        //Log.Message("Loading Storm Born Abilities");
                        MagicPower mpSB = MagicData.MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_LightningBolt);
                        if (mpSB.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_LightningBolt);
                        }
                        mpSB = MagicData.MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_LightningCloud);
                        if (mpSB.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_LightningCloud);
                        }
                        mpSB = MagicData.MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_LightningStorm);
                        if (mpSB.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_LightningStorm);
                        }
                    }
                    if (flag43)
                    {
                        //Log.Message("Loading Arcane Abilities");
                        MagicPower mpA = MagicData.MagicPowersA.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Teleport);
                        if (mpA.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Teleport);
                        }
                    }
                    if (flag44)
                    {
                        //Log.Message("Loading Paladin Abilities");
                        MagicPower mpP = MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Heal);
                        if (mpP.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Heal);
                        }
                        mpP = MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ValiantCharge);
                        if (mpP.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_ValiantCharge);
                        }
                        mpP = MagicData.MagicPowersP.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Overwhelm);
                        if (mpP.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Overwhelm);
                        }
                    }
                    if (flag45)
                    {
                        //Log.Message("Loading Summoner Abilities");
                        MagicPower mpS = MagicData.MagicPowersS.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SummonMinion);
                        if (mpS.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_SummonMinion);
                        }
                        mpS = MagicData.MagicPowersS.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SummonPylon);
                        if (mpS.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_SummonPylon);
                        }
                        mpS = MagicData.MagicPowersS.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SummonExplosive);
                        if (mpS.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_SummonExplosive);
                        }
                        mpS = MagicData.MagicPowersS.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SummonElemental);
                        if (mpS.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_SummonElemental);
                        }
                    }
                    if (flag46)
                    {
                        //Log.Message("Loading Druid Abilities");
                        MagicPower mpD = MagicData.MagicPowersD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Poison);
                        if (mpD.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Poison);
                        }
                        mpD = MagicData.MagicPowersD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Regenerate);
                        if (mpD.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Regenerate);
                        }
                        mpD = MagicData.MagicPowersD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_CureDisease);
                        if (mpD.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_CureDisease);
                        }
                    }
                    if (flag47)
                    {
                        //Log.Message("Loading Necromancer Abilities");
                        MagicPower mpN = MagicData.MagicPowersN.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_RaiseUndead);
                        if (mpN.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_RaiseUndead);
                        }
                        mpN = MagicData.MagicPowersN.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_FogOfTorment);
                        if (mpN.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_FogOfTorment);
                        }
                    }
                    if (flag48)
                    {
                        //Log.Message("Loading Priest Abilities");
                        MagicPower mpPR = MagicData.MagicPowersPR.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_AdvancedHeal);
                        if (mpPR.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_AdvancedHeal);
                        }
                        mpPR = MagicData.MagicPowersPR.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Purify);
                        if (mpPR.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Purify);
                        }
                    }
                    if (flag49)
                    {
                        //Log.Message("Loading Bard Abilities");
                        // MagicPower mpB = MagicData.MagicPowersB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BardTraining);
                        //if (mpB.learned)
                        //{
                        //    AddPawnAbility(TorannMagicDefOf.TM_BardTraining);
                        //}
                        MagicPower mpB = MagicData.MagicPowersB.First(mp => mp.abilityDef == TorannMagicDefOf.TM_Entertain);
                        if (mpB.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Entertain);
                        }
                        //mpB = MagicData.MagicPowersB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Inspire);
                        //if (mpB.learned)
                        //{
                        //    AddPawnAbility(TorannMagicDefOf.TM_Inspire);
                        //}
                    }
                    if (flag50)
                    {
                        //Log.Message("Loading Succubus Abilities");
                        MagicPower mpSD = MagicData.MagicPowersSD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Dominate);
                        if (mpSD.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Dominate);
                        }
                        mpSD = MagicData.MagicPowersSD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SoulBond);
                        if (mpSD.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_SoulBond);
                        }
                    }
                    if (flag51)
                    {
                        //Log.Message("Loading Warlock Abilities");
                        MagicPower mpWD = MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Dominate);
                        if (mpWD.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Dominate);
                        }
                        mpWD = MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SoulBond);
                        if (mpWD.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_SoulBond);
                        }
                    }
                    if (flag52)
                    {
                        //Log.Message("Loading Geomancer Abilities");
                        MagicPower mpG = MagicData.MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Stoneskin);
                        if (mpG.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Stoneskin);
                        }
                        mpG = MagicData.MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EarthSprites);
                        if (mpG.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_EarthSprites);
                        }
                        mpG = MagicData.MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EarthernHammer);
                        if (mpG.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_EarthernHammer);
                        }
                        mpG = MagicData.MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Sentinel);
                        if (mpG.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Sentinel);
                        }
                    }
                    if (flag53)
                    {
                        //Log.Message("Loading Geomancer Abilities");
                        MagicPower mpT = MagicData.MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_TechnoTurret);
                        if (mpT.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_TechnoTurret);
                        }
                        mpT = MagicData.MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_TechnoWeapon);
                        if (mpT.learned)
                        {
                            //nano weapon applies only when equipping a new weapon
                            AddPawnAbility(TorannMagicDefOf.TM_TechnoWeapon);
                            AddPawnAbility(TorannMagicDefOf.TM_NanoStimulant);
                        }
                        mpT = MagicData.MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_TechnoShield);
                        if (mpT.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_TechnoShield);
                        }
                        mpT = MagicData.MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Sabotage);
                        if (mpT.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Sabotage);
                        }
                        mpT = MagicData.MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Overdrive);
                        if (mpT.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Overdrive);
                        }
                    }
                    if (flag54)
                    {
                        //Log.Message("Loading BloodMage Abilities");
                        MagicPower mpBM = MagicData.MagicPowersBM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BloodGift);
                        if (mpBM.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_BloodGift);
                        }
                        mpBM = MagicData.MagicPowersBM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_IgniteBlood);
                        if (mpBM.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_IgniteBlood);
                        }
                        mpBM = MagicData.MagicPowersBM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BloodForBlood);
                        if (mpBM.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_BloodForBlood);
                        }
                        mpBM = MagicData.MagicPowersBM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BloodShield);
                        if (mpBM.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_BloodShield);
                        }
                    }
                    if (flag55)
                    {
                        //Log.Message("Loading Enchanter Abilities");
                        MagicPower mpE = MagicData.MagicPowersE.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EnchantedBody);
                        if (mpE.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_EnchantedBody);
                            spell_EnchantedAura = true;
                        }
                        mpE = MagicData.MagicPowersE.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Transmutate);
                        if (mpE.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Transmutate);
                        }
                        mpE = MagicData.MagicPowersE.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EnchanterStone);
                        if (mpE.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_EnchanterStone);
                        }
                        mpE = MagicData.MagicPowersE.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EnchantWeapon);
                        if (mpE.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_EnchantWeapon);
                        }
                    }
                    if (flag56)
                    {
                        //Log.Message("Loading Chronomancer Abilities");
                        MagicPower mpC = MagicData.MagicPowersC.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Prediction);
                        if (mpC.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_Prediction);
                        }
                        mpC = MagicData.MagicPowersC.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_AlterFate);
                        if (mpC.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_AlterFate);
                        }
                        mpC = MagicData.MagicPowersC.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_AccelerateTime);
                        if (mpC.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_AccelerateTime);
                        }
                        mpC = MagicData.MagicPowersC.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ReverseTime);
                        if (mpC.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_ReverseTime);
                        }
                    }
                    if (flagCM)
                    {
                        //Log.Message("Loading Chaos Mage Abilities");
                        MagicPower mpCM = MagicData.MagicPowersCM.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ChaosTradition);
                        if (mpCM.learned)
                        {
                            AddPawnAbility(TorannMagicDefOf.TM_ChaosTradition);
                            chaosPowers = new List<TM_ChaosPowers>();
                            List<MagicPower> learnedList = new List<MagicPower>();
                            for (int i = 0; i < MagicData.AllMagicPowersForChaosMage.Count; i++)
                            {
                                MagicPower mp = MagicData.AllMagicPowersForChaosMage[i];
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
                                    chaosPowers.Add(new TM_ChaosPowers((TMAbilityDef)learnedList[i].GetAbilityDef(0), TM_ClassUtility.GetAssociatedMagicPowerSkill(this, learnedList[i])));
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
                                                    if (TM_Calc.GetSkillPowerLevel(Pawn, TorannMagicDefOf.TM_LightSkip) >= 1)
                                                    {
                                                        AddPawnAbility(TorannMagicDefOf.TM_LightSkipMass);
                                                    }
                                                    if (TM_Calc.GetSkillPowerLevel(Pawn, TorannMagicDefOf.TM_LightSkip) >= 2)
                                                    {
                                                        AddPawnAbility(TorannMagicDefOf.TM_LightSkipGlobal);
                                                    }
                                                }
                                                if (tmad == TorannMagicDefOf.TM_Hex && HexedPawns.Count > 0)
                                                {
                                                    RemovePawnAbility(TorannMagicDefOf.TM_Hex_CriticalFail);
                                                    RemovePawnAbility(TorannMagicDefOf.TM_Hex_MentalAssault);
                                                    RemovePawnAbility(TorannMagicDefOf.TM_Hex_Pain);
                                                    AddPawnAbility(TorannMagicDefOf.TM_Hex_CriticalFail);
                                                    AddPawnAbility(TorannMagicDefOf.TM_Hex_MentalAssault);
                                                    AddPawnAbility(TorannMagicDefOf.TM_Hex_Pain);
                                                }
                                                
                                                SafelyAddPawnAbility(tmad);
                                            }
                                            if(tmad.childAbilities != null && tmad.childAbilities.Count > 0)
                                            {
                                                foreach(TMAbilityDef ad in tmad.childAbilities)
                                                {
                                                    if(ad.shouldInitialize)
                                                    {
                                                        SafelyAddPawnAbility(ad);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    chaosPowers.Add(new TM_ChaosPowers((TMAbilityDef)TM_Calc.GetRandomMagicPower(this).abilityDef, null));
                                }
                            }
                        }
                    }
                }
                if(TM_Calc.HasAdvancedClass(Pawn))
                {
                    List<TM_CustomClass> ccList = TM_ClassUtility.GetAdvancedClassesForPawn(Pawn);
                    foreach(TM_CustomClass cc in ccList)
                    {
                        if(cc.isMage)
                        {
                            AdvancedClasses.Add(cc);
                            LoadCustomClassAbilities(cc);
                        }
                    }                    
                }
                UpdateAutocastDef();
                InitializeSpell();
                //UpdateAbilities();
            }
        }

        public void LoadCustomClassAbilities(TM_CustomClass cc, Pawn fromPawn = null)
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
                            MagicPower magicPower = MagicData.AllMagicPowers.FirstOrDefault(mp => mp.abilityDef == fp.TMabilityDefs[0]);
                            if (magicPower != null)
                            {
                                magicPower.learned = true;
                                magicPower.level = fp.level;
                            }
                        }
                    }
                }

                for (int j = 0; j < MagicData.AllMagicPowers.Count; j++)
                {
                    if (MagicData.AllMagicPowers[j] == MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SoulBond) ||
                            MagicData.AllMagicPowers[j] == MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ShadowBolt) ||
                            MagicData.AllMagicPowers[j] == MagicData.MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Dominate))
                    {
                        MagicData.AllMagicPowers[j].learned = false;
                    }

                    if (!MagicData.AllMagicPowers[j].TMabilityDefs.Contains(cc.classMageAbilities[i]) ||
                        !MagicData.AllMagicPowers[j].learned) continue;

                    if (cc.classMageAbilities[i].shouldInitialize)
                    {
                        int level = MagicData.AllMagicPowers[j].level;
                        AddPawnAbility(MagicData.AllMagicPowers[j].TMabilityDefs[level]);
                        if (magicData.AllMagicPowers[j].TMabilityDefs[level] == TorannMagicDefOf.TM_LightSkip)
                        {
                            if (TM_Calc.GetSkillPowerLevel(Pawn, TorannMagicDefOf.TM_LightSkip) >= 1)
                            {
                                AddPawnAbility(TorannMagicDefOf.TM_LightSkipMass);
                            }
                            if (TM_Calc.GetSkillPowerLevel(Pawn, TorannMagicDefOf.TM_LightSkip) >= 2)
                            {
                                AddPawnAbility(TorannMagicDefOf.TM_LightSkipGlobal);
                            }
                        }
                        if (cc.classMageAbilities[i] == TorannMagicDefOf.TM_Hex && HexedPawns.Count > 0)
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
                                AddPawnAbility(ability.childAbilities[c]);
                            }
                        }
                    }
                }
            }
        }

        public void AddAdvancedClass(TM_CustomClass ac, Pawn fromPawn = null)
        {
            if (ac == null || !ac.isMage || !ac.isAdvancedClass) return;

            Trait t = Pawn.story.traits.GetTrait(TorannMagicDefOf.TM_Possessed);
            if (t != null && !Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SpiritPossessionHD))
            {
                Pawn.story.traits.RemoveTrait(t);
                return;
            }
            if (!AdvancedClasses.Contains(ac))
            {
                AdvancedClasses.Add(ac);
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

            MagicData fromData = fromPawn?.GetCompAbilityUserMagic().MagicData;
            if(fromData != null)
            {
                foreach(TMAbilityDef ability in ac.classMageAbilities)
                {
                    MagicPowerSkill mps_e = MagicData.GetSkill_Efficiency(ability);
                    MagicPowerSkill fps_e = fromData.GetSkill_Efficiency(ability);
                    if (mps_e != null && fps_e != null)
                    {
                        mps_e.level = fps_e.level;
                    }
                    MagicPowerSkill mps_p = MagicData.GetSkill_Power(ability);
                    MagicPowerSkill fps_p = fromData.GetSkill_Power(ability);
                    if (mps_p != null && fps_p != null)
                    {
                        mps_p.level = fps_p.level;
                    }
                    MagicPowerSkill mps_v = MagicData.GetSkill_Versatility(ability);
                    MagicPowerSkill fps_v = fromData.GetSkill_Versatility(ability);
                    if (mps_v != null && fps_v != null)
                    {
                        mps_v.level = fps_v.level;
                    }
                }
            }
            LoadCustomClassAbilities(ac, fromPawn);
        }

        public void RemoveAdvancedClass(TM_CustomClass ac)
        {
            for (int i = 0; i < ac.classMageAbilities.Count; i++)
            {
                TMAbilityDef ability = ac.classMageAbilities[i];

                for (int j = 0; j < MagicData.AllMagicPowers.Count; j++)
                {
                    MagicPower power = MagicData.AllMagicPowers[j];
                    if (power.abilityDef == ability)
                    {
                        if (magicData.AllMagicPowers[j].TMabilityDefs[power.level] == TorannMagicDefOf.TM_LightSkip)
                        {
                            if (TM_Calc.GetSkillPowerLevel(Pawn, TorannMagicDefOf.TM_LightSkip) >= 1)
                            {
                                RemovePawnAbility(TorannMagicDefOf.TM_LightSkipMass);
                            }
                            if (TM_Calc.GetSkillPowerLevel(Pawn, TorannMagicDefOf.TM_LightSkip) >= 2)
                            {
                                RemovePawnAbility(TorannMagicDefOf.TM_LightSkipGlobal);
                            }
                        }
                        if (ac.classMageAbilities[i] == TorannMagicDefOf.TM_Hex && HexedPawns.Count > 0)
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
                                RemovePawnAbility(ability.childAbilities[c]);
                            }
                        }
                    }
                    RemovePawnAbility(ability);
                }
            }
            if (ac.isMage && ac.isAdvancedClass)
            {
                foreach (TMAbilityDef ability in ac.classMageAbilities)
                {
                    MagicPowerSkill mps_e = MagicData.GetSkill_Efficiency(ability);
                    if (mps_e != null)
                    {
                        mps_e.level = 0;
                    }
                    MagicPowerSkill mps_p = MagicData.GetSkill_Power(ability);
                    if (mps_p != null)
                    {
                        mps_p.level = 0;
                    }
                    MagicPowerSkill mps_v = MagicData.GetSkill_Versatility(ability);
                    if (mps_v != null)
                    {
                        mps_v.level = 0;
                    }
                }
            }
            if(AdvancedClasses.Contains(ac))
            {
                AdvancedClasses.Remove(ac);
            }
        }

        public void UpdateAutocastDef()
        {
            IEnumerable<TM_CustomPowerDef> mpDefs = TM_Data.CustomMagePowerDefs();
            if (!IsMagicUser || MagicData?.MagicPowersCustom == null) return;

            foreach (MagicPower mp in MagicData.MagicPowersCustom)
            {
                foreach (TM_CustomPowerDef mpDef in mpDefs)
                {
                    if (mpDef.customPower.abilityDefs[0].ToString() != mp.GetAbilityDef(0).ToString()) continue;

                    if (mpDef.customPower.autocasting != null)
                    {
                        mp.autocasting = mpDef.customPower.autocasting;
                    }
                }
            }
        }

        private Dictionary<string, Command> gizmoCommands = new Dictionary<string, Command>();
        public Command GetGizmoCommands(string key)
        {
            Command commandItem = gizmoCommands.TryGetValue(key);
            if (commandItem != null) return commandItem;
            {
                switch (key)
                {
                    case "wanderer":
                    {
                        Command_Action itemWanderer = new Command_Action
                        {
                            action = () => TM_Action.PromoteWanderer(Pawn),
                            order = 51,
                            defaultLabel = TM_TextPool.TM_PromoteWanderer,
                            defaultDesc = TM_TextPool.TM_PromoteWandererDesc,
                            icon = ContentFinder<Texture2D>.Get("UI/wanderer"),
                        };
                        commandItem = itemWanderer;
                        gizmoCommands.Add(key, itemWanderer);
                        break;
                    }
                    case "technoBit":
                    {
                        string toggle = "bit_c";
                        string label = "TM_TechnoBitEnabled".Translate();
                        string desc = "TM_TechnoBitToggleDesc".Translate();
                        if (!useTechnoBitToggle)
                        {
                            toggle = "bit_off";
                            label = "TM_TechnoBitDisabled".Translate();
                        }
                        var item = new Command_Toggle
                        {
                            isActive = () => useTechnoBitToggle,
                            toggleAction = () =>
                            {
                                useTechnoBitToggle = !useTechnoBitToggle;
                            },
                            defaultLabel = label,
                            defaultDesc = desc,
                            order = -89,
                            icon = ContentFinder<Texture2D>.Get("UI/" + toggle)
                        };
                        commandItem = item;
                        gizmoCommands.Add(key, item);
                        break;
                    }
                    case "technoRepair":
                    {
                        string toggle_repair = "bit_repairon";
                        string label_repair = "TM_TechnoBitRepair".Translate();
                        string desc_repair = "TM_TechnoBitRepairDesc".Translate();
                        if (!useTechnoBitRepairToggle)
                        {
                            toggle_repair = "bit_repairoff";
                        }
                        var item_repair = new Command_Toggle
                        {
                            isActive = () => useTechnoBitRepairToggle,
                            toggleAction = () =>
                            {
                                useTechnoBitRepairToggle = !useTechnoBitRepairToggle;
                            },
                            defaultLabel = label_repair,
                            defaultDesc = desc_repair,
                            order = -88,
                            icon = ContentFinder<Texture2D>.Get("UI/" + toggle_repair)
                        };
                        commandItem = item_repair;
                        gizmoCommands.Add(key, item_repair);
                        break;
                    }
                    case "elementalShot":
                    {
                        string toggle = "elementalshot";
                        string label = "TM_TechnoWeapon_ver".Translate();
                        string desc = "TM_ElementalShotToggleDesc".Translate();
                        if (!useElementalShotToggle)
                        {
                            toggle = "elementalshot_off";
                        }
                        var item = new Command_Toggle
                        {
                            isActive = () => useElementalShotToggle,
                            toggleAction = () =>
                            {
                                useElementalShotToggle = !useElementalShotToggle;
                            },
                            defaultLabel = label,
                            defaultDesc = desc,
                            order = -88,
                            icon = ContentFinder<Texture2D>.Get("UI/" + toggle)
                        };
                        commandItem = item;
                        gizmoCommands.Add(key, item);
                        break;
                    }
                }
            }

            return commandItem;
        }
    }
}
