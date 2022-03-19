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

namespace TorannMagic
{
    [CompilerGenerated]
    [Serializable]
    [StaticConstructorOnStartup]
    public class CompAbilityUserMagic : CompAbilityUser
    {
        public string LabelKey = "TM_Magic";

        public int customIndex = -2;
        public TMDefs.TM_CustomClass customClass = null;

        public bool firstTick = false;
        public bool magicPowersInitialized = false;
        public bool magicPowersInitializedForColonist = true;
        private bool colonistPowerCheck = true;
        private int resMitigationDelay = 0;
        private int damageMitigationDelay = 0;
        private int damageMitigationDelayMS = 0;
        public int magicXPRate = 1000;
        public int lastXPGain = 0;
        private int age = -1;
        private bool doOnce = true;
        private int autocastTick = 0;
        private int nextAICastAttemptTick = 0;
        public bool canDeathRetaliate = false;
        private bool deathRetaliating = false;
        private int ticksTillRetaliation = 600;
        private List<IntVec3> deathRing = new List<IntVec3>();
        public float weaponDamage = 1;
        public float weaponCritChance = 0f;
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
        public float coolDown = 1;
        public float mpCost = 1;
        public float xpGain = 1;
        public float arcaneDmg = 1;
        public float arcaneRes = 1;
        public float arcalleumCooldown = 0f;

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
        //public List<TMDefs.Branding> brandings = new List<TMDefs.Branding>();
        public List<Pawn> brandedPawns = new List<Pawn>();
        public bool sigilSurging = false;
        public bool sigilDraining = false;
        public FlyingObject_LivingWall livingWall = null;
        public int lastChaosTraditionTick = 0;
        public ThingOwner<ThingWithComps> magicWardrobe;

        private Effecter powerEffecter = null;
        private int powerModifier = 0;
        private int maxPower = 10;
        private int previousHexedPawns = 0;
        public int nextEntertainTick = -1;
        public int nextSuccubusLovinTick = -1;

        //public List<TMDefs.Branding> Brandings
        //{
        //    get
        //    {
        //        if (brandings == null)
        //        {
        //            brandings = new List<TMDefs.Branding>();
        //            brandings.Clear();
        //        }
        //        List<TMDefs.Branding> tmpList = new List<TMDefs.Branding>();
        //        tmpList.Clear();
        //        foreach (TMDefs.Branding br in brandings)
        //        {
        //            Pawn p = br.pawn;
        //            if (p.DestroyedOrNull() || p.Dead)
        //            {
        //                tmpList.Add(br);
        //                continue;
        //            }
        //            Hediff hd = p.health?.hediffSet?.GetFirstHediffOfDef(br.hediffDef);
        //            if(hd == null)
        //            {
        //                tmpList.Add(br);
        //            }                    
        //        }
        //        for (int i = 0; i < tmpList.Count; i++)
        //        {
        //            brandings.Remove(tmpList[i]);
        //        }
        //        return brandings;
        //    }
        //}

        public List<Pawn> BrandedPawns
        {
            get
            {
                if (brandedPawns == null)
                {
                    brandedPawns = new List<Pawn>();
                    brandedPawns.Clear();
                }
                List<Pawn> tmpList = new List<Pawn>();
                tmpList.Clear();
                foreach (Pawn br in brandedPawns)
                {
                    Pawn p = br;
                    if (p.DestroyedOrNull() || p.Dead)
                    {
                        tmpList.Add(br);
                    }
                }
                for (int i = 0; i < tmpList.Count; i++)
                {
                    brandedPawns.Remove(tmpList[i]);
                }
                return brandedPawns;
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
            get
            {
                return powerModifier;
            }
            set
            {
                TM_MoteMaker.ThrowSiphonMote(this.Pawn.DrawPos, this.Pawn.Map, 1f);
                powerModifier = Mathf.Clamp(value, 0, maxPower);
            }
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

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
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
            if (shouldDraw && IsMagicUser)
            {
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (settingsRef.AIFriendlyMarking && base.Pawn.IsColonist && this.IsMagicUser)
                {
                    if (!this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                    {
                        DrawMageMark();
                    }
                }
                if (settingsRef.AIMarking && !base.Pawn.IsColonist && this.IsMagicUser)
                {
                    if (!this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                    {
                        DrawMageMark();
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
                try
                {
                    if (this.IsMagicUser && compEnchant != null && compEnchant.enchantingContainer.Count > 0)
                    {
                        DrawEnchantMark();
                    }
                }
                catch
                {
                    Enchantment.CompProperties_Enchant newEnchantComp = new Enchantment.CompProperties_Enchant();
                    this.Pawn.def.comps.Add(newEnchantComp);
                }
            }
            base.PostDraw();
        }


        private void DrawTechnoBit()
        {
            float num = Mathf.Lerp(1.2f, 1.55f, 1f);
            if (this.bitFloatingDown)
            {
                if (this.bitOffset < .38f)
                {
                    this.bitFloatingDown = false;
                }
                this.bitOffset -= .001f;
            }
            else
            {
                if (this.bitOffset > .57f)
                {
                    this.bitFloatingDown = true;
                }
                this.bitOffset += .001f;
            }

            this.bitPosition = this.Pawn.Drawer.DrawPos;
            this.bitPosition.x -= .5f + Rand.Range(-.01f, .01f);
            this.bitPosition.z += this.bitOffset;
            this.bitPosition.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            float angle = 0f;
            Vector3 s = new Vector3(.35f, 1f, .35f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(this.bitPosition, Quaternion.AngleAxis(angle, Vector3.up), s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.bitMat, 0);
        }

        private void DrawMageLight()
        {
            if (!mageLightSet)
            {
                float num = Mathf.Lerp(1.2f, 1.55f, 1f);
                Vector3 lightPos = Vector3.zero;

                lightPos = this.Pawn.Drawer.DrawPos;
                lightPos.x -= .5f;
                lightPos.z += .6f;

                lightPos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                float angle = Rand.Range(0, 360);
                Vector3 s = new Vector3(.27f, .5f, .27f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(lightPos, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.mageLightMat, 0);
            }

        }

        public void DrawMageMark()
        {
            float num = Mathf.Lerp(1.2f, 1.55f, 1f);
            Vector3 vector = this.Pawn.Drawer.DrawPos;
            vector.x = vector.x + .45f;
            vector.z = vector.z + .45f;
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            float angle = 0f;
            Vector3 s = new Vector3(.28f, 1f, .28f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
            if (this.customClass != null)
            {
                Material mat = TM_RenderQueue.mageMarkMat;
                if (this.customClass.classIconPath != "")
                {
                    mat = MaterialPool.MatFrom("Other/" + this.customClass.classIconPath.ToString());
                }
                else if(this.customClass.classTexturePath != "")
                {
                    mat = MaterialPool.MatFrom("Other/ClassTextures/" + this.customClass.classTexturePath, true);
                }
                if (this.customClass.classIconColor != null)
                {
                    mat.color = this.customClass.classIconColor;
                }
                Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
            }
            else
            {
                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.InnerFire))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.fireMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.iceMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.StormBorn))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.lightningMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.arcanistMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.paladinMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Summoner))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.summonerMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Druid))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.druidMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Lich))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.necroMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Priest))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.priestMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.bardMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus) || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.demonkinMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Geomancer))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.earthMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.technoMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.BloodMage))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.bloodmageMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.enchanterMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.chronomancerMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.chaosMarkMat, 0);
                }
                else if (TM_Calc.IsWanderer(this.Pawn))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.wandererMarkMat, 0);
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.mageMarkMat, 0);
                }
            }

        }

        public void DrawEnchantMark()
        {
            float num = Mathf.Lerp(1.2f, 1.55f, 1f);
            Vector3 vector = this.Pawn.Drawer.DrawPos;
            vector.x = vector.x + .45f;
            vector.z = vector.z + .45f;
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            float angle = 0f;
            Vector3 s = new Vector3(.5f, 1f, .5f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.enchantMark, 0);

        }

        public void DrawScornWings()
        {
            bool flag = !this.Pawn.Dead && !this.Pawn.Downed;
            if (flag)
            {
                float num = Mathf.Lerp(1.2f, 1.55f, 1f);
                Vector3 vector = this.Pawn.Drawer.DrawPos;
                vector.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn);
                if (this.Pawn.Rotation == Rot4.North)
                {
                    vector.y = Altitudes.AltitudeFor(AltitudeLayer.PawnState);
                }
                float angle = (float)Rand.Range(0, 360);
                Vector3 s = new Vector3(3f, 3f, 3f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(vector, Quaternion.AngleAxis(0f, Vector3.up), s);
                if (this.Pawn.Rotation == Rot4.South || this.Pawn.Rotation == Rot4.North)
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.scornWingsNS, 0);
                }
                if (this.Pawn.Rotation == Rot4.East)
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.scornWingsE, 0);
                }
                if (this.Pawn.Rotation == Rot4.West)
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.scornWingsW, 0);
                }
            }
        }

        public static List<TMAbilityDef> MagicAbilities = null;

        public int LevelUpSkill_global_regen(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_global_regen.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_global_eff(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_global_eff.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_global_spirit(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_global_spirit.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_RayofHope(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_RayofHope.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Firebolt(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Firebolt.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Fireball(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Fireball.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Fireclaw(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Fireclaw.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Firestorm(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Firestorm.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_Soothe(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Soothe.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Icebolt(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Icebolt.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_FrostRay(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_FrostRay.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Snowball(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Snowball.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Rainmaker(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Rainmaker.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Blizzard(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Blizzard.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_AMP(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_AMP.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_LightningBolt(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_LightningBolt.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_LightningCloud(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_LightningCloud.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_LightningStorm(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_LightningStorm.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_EyeOfTheStorm(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_EyeOfTheStorm.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_Shadow(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Shadow.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_MagicMissile(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_MagicMissile.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Blink(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Blink.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Summon(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Summon.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Teleport(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Teleport.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_FoldReality(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_FoldReality.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_Heal(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Heal.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Shield(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Shield.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_ValiantCharge(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_ValiantCharge.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Overwhelm(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Overwhelm.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_HolyWrath(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_HolyWrath.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_SummonMinion(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_SummonMinion.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_SummonPylon(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_SummonPylon.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_SummonExplosive(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_SummonExplosive.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_SummonElemental(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_SummonElemental.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_SummonPoppi(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_SummonPoppi.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_Poison(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Poison.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_SootheAnimal(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_SootheAnimal.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Regenerate(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Regenerate.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_CureDisease(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_CureDisease.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_RegrowLimb(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_RegrowLimb.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_RaiseUndead(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_RaiseUndead.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_DeathMark(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_DeathMark.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_FogOfTorment(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_FogOfTorment.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_ConsumeCorpse(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_ConsumeCorpse.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_CorpseExplosion(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_CorpseExplosion.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_DeathBolt(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_DeathBolt.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_AdvancedHeal(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_AdvancedHeal.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Purify(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Purify.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_HealingCircle(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_HealingCircle.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_BestowMight(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_BestowMight.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Resurrection(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Resurrection.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_BardTraining(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_BardTraining.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Entertain(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Entertain.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Inspire(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Inspire.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Lullaby(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Lullaby.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_BattleHymn(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_BattleHymn.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_SoulBond(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_SoulBond.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_ShadowBolt(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_ShadowBolt.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Dominate(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Dominate.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Attraction(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Attraction.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Repulsion(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Repulsion.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Scorn(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Scorn.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_PsychicShock(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_PsychicShock.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_Stoneskin(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Stoneskin.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Encase(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Encase.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_EarthSprites(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_EarthSprites.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_EarthernHammer(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_EarthernHammer.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Meteor(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Meteor.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Sentinel(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Sentinel.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_TechnoBit(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_TechnoBit.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_TechnoTurret(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_TechnoTurret.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_TechnoWeapon(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_TechnoWeapon.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_TechnoShield(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_TechnoShield.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Sabotage(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Sabotage.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Overdrive(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Overdrive.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_OrbitalStrike(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_OrbitalStrike.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_BloodGift(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_BloodGift.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_IgniteBlood(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_IgniteBlood.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_BloodForBlood(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_BloodForBlood.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_BloodShield(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_BloodShield.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Rend(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Rend.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_BloodMoon(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_BloodMoon.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_EnchantedBody(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_EnchantedBody.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Transmutate(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Transmutate.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_EnchanterStone(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_EnchanterStone.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_EnchantWeapon(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_EnchantWeapon.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Polymorph(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Polymorph.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Shapeshift(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Shapeshift.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Prediction(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Prediction.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_AlterFate(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_AlterFate.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_AccelerateTime(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_AccelerateTime.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_ReverseTime(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_ReverseTime.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_ChronostaticField(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_ChronostaticField.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Recall(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Recall.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_ChaosTradition(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_ChaosTradition.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_WandererCraft(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Cantrips(string skillName)
        {
            int result = 0;
            MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == skillName);
            bool flag = magicPowerSkill != null;
            if (flag)
            {
                result = magicPowerSkill.level;
            }
            return result;
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
            for (int z = 0; z < this.MagicData.AllMagicPowers.Count; z++)
            {
                MagicPower mp = this.MagicData.AllMagicPowers[z];
                if(mp.TMabilityDefs.Contains(TorannMagicDefOf.TM_Branding) && !this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Golemancer))
                {
                    foreach(TMAbilityDef tm in TM_Data.BrandList())
                    {
                        RemovePawnAbility(tm);
                    }
                }
            }
        }

        public override void CompTick()
        {
            bool flag = base.Pawn != null;
            if (flag)
            {
                bool spawned = base.Pawn.Spawned;
                if (spawned)
                {
                    bool isMagicUser = this.IsMagicUser && !this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) && !this.Pawn.IsWildMan();
                    if (isMagicUser)
                    {
                        bool flag3 = !this.firstTick;
                        if (flag3)
                        {
                            this.PostInitializeTick();
                        }
                        if (this.doOnce)
                        {
                            SingleEvent();
                        }
                        base.CompTick();
                        this.age++;
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
                            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                            if (this.autocastTick < Find.TickManager.TicksGame)  //180 default
                            {
                                if (!this.Pawn.Dead && !this.Pawn.Downed && this.Pawn.Map != null && this.Pawn.story != null && this.Pawn.story.traits != null && this.MagicData != null && this.AbilityData != null && !this.Pawn.InMentalState)
                                {
                                    if (this.Pawn.IsColonist)
                                    {
                                        this.autocastTick = Find.TickManager.TicksGame + (int)Rand.Range(.8f * settingsRef.autocastEvaluationFrequency, 1.2f * settingsRef.autocastEvaluationFrequency);
                                        ResolveAutoCast();
                                    }
                                    else if(settingsRef.AICasting && (!this.Pawn.IsPrisoner || this.Pawn.IsFighting()) && (this.Pawn.guest != null && !this.Pawn.IsSlave))
                                    {
                                        float tickMult = settingsRef.AIAggressiveCasting ? 1f : 2f;
                                        this.autocastTick = Find.TickManager.TicksGame + (int)(Rand.Range(.8f * settingsRef.autocastEvaluationFrequency, 1.2f * settingsRef.autocastEvaluationFrequency) * tickMult);
                                        ResolveAIAutoCast();
                                    }
                                }                                
                            }
                            if (!this.Pawn.IsColonist && settingsRef.AICasting && settingsRef.AIAggressiveCasting && Find.TickManager.TicksGame > this.nextAICastAttemptTick) //Aggressive AI Casting
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
                            if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || (this.customClass != null && this.customClass.classMageAbilities != null && this.customClass.classMageAbilities.Contains(TorannMagicDefOf.TM_Overdrive)))
                            {
                                ResolveTechnomancerOverdrive();
                            }
                        }
                        if (Find.TickManager.TicksGame % 299 == 0) //cache weapon damage for tooltip and damage calculations
                        {
                            this.weaponDamage = TM_Calc.GetSkillDamage(this.Pawn);
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
                    else
                    {                        
                        if(Find.TickManager.TicksGame % 2501 == 0 && base.Pawn.story != null && this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted))
                        {                            
                            if (!this.Pawn.Inspired && this.Pawn.CurJobDef == JobDefOf.LayDown && Rand.Chance(.025f))
                            {
                                this.Pawn.mindState.inspirationHandler.TryStartInspiration(TorannMagicDefOf.ID_ArcanePathways);
                            }
                        }
                    }
                }
                else
                {
                    if (Find.TickManager.TicksGame % 600 == 0)
                    {
                        if (this.Pawn.Map == null)
                        {
                            if (this.IsMagicUser)
                            {
                                int num;
                                if (AbilityData?.AllPowers != null)
                                {
                                    AbilityData obj = AbilityData;
                                    num = ((obj != null && obj.AllPowers.Count > 0) ? 1 : 0);
                                }
                                else
                                {
                                    num = 0;
                                }
                                if (num != 0)
                                {
                                    foreach (PawnAbility allPower in AbilityData.AllPowers)
                                    {
                                        allPower.CooldownTicksLeft -= 600;
                                        if (allPower.CooldownTicksLeft <= 0)
                                        {
                                            allPower.CooldownTicksLeft = 0;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (Initialized)
            {
                //custom code
            }
        }

        private int deathRetaliationDelayCount = 0;
        public void DoDeathRetaliation()
        {
            if (!this.Pawn.Downed || this.Pawn.Map == null || this.Pawn.IsPrisoner || this.Pawn.Faction == null || !this.Pawn.Faction.HostileTo(Faction.OfPlayerSilentFail))
            {
                this.deathRetaliating = false;
                this.canDeathRetaliate = false;
                deathRetaliationDelayCount = 0;
            }
            if (this.canDeathRetaliate && this.deathRetaliating)
            {
                this.ticksTillRetaliation--;
                if (this.deathRing == null || this.deathRing.Count < 1)
                {
                    this.deathRing = TM_Calc.GetOuterRing(this.Pawn.Position, 1f, 2f);
                }
                if (Find.TickManager.TicksGame % 6 == 0)
                {
                    Vector3 moteVec = this.deathRing.RandomElement().ToVector3Shifted();
                    moteVec.x += Rand.Range(-.4f, .4f);
                    moteVec.z += Rand.Range(-.4f, .4f);
                    float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(moteVec, this.Pawn.DrawPos)).ToAngleFlat();
                    ThingDef mote = TorannMagicDefOf.Mote_Psi_Grayscale;
                    mote.graphicData.color = Color.white;
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi_Grayscale, moteVec, this.Pawn.Map, Rand.Range(.25f, .6f), .1f, .05f, .05f, 0, Rand.Range(4f, 6f), angle, angle);
                }
                if (this.ticksTillRetaliation <= 0)
                {
                    this.canDeathRetaliate = false;
                    this.deathRetaliating = false;
                    TM_Action.CreateMagicDeathEffect(this.Pawn, this.Pawn.Position);
                }
            }
            else if (this.canDeathRetaliate)
            {
                if (deathRetaliationDelayCount >= 20 && Rand.Value < .04f)
                {
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    this.deathRetaliating = true;
                    this.ticksTillRetaliation = Mathf.RoundToInt(Rand.Range(400, 1200) * settingsRef.deathRetaliationDelayFactor);
                    this.deathRing = TM_Calc.GetOuterRing(this.Pawn.Position, 1f, 2f);
                }
                else
                {
                    deathRetaliationDelayCount++;
                }
            }
        }

        public void PostInitializeTick()
        {
            bool flag = base.Pawn != null;
            if (flag)
            {
                bool spawned = base.Pawn.Spawned;
                if (spawned)
                {
                    bool flag2 = base.Pawn.story != null;
                    if (flag2)
                    {
                        this.firstTick = true;
                        this.Initialize();
                        this.ResolveMagicTab();
                        this.ResolveMagicPowers();
                        this.ResolveMana();
                        this.DoOncePerLoad();
                    }
                }
            }
        }

        public bool IsMagicUser
        {
            get
            {
                bool flag = base.Pawn != null;
                bool result;
                if (flag)
                {
                    bool flag3 = base.Pawn.story != null;
                    if (flag3)
                    {
                        if (this.customClass != null)
                        {
                            return true;
                        }
                        if (this.customClass == null && this.customIndex == -2)
                        {
                            this.customIndex = TM_ClassUtility.IsCustomClassIndex(this.Pawn.story.traits.allTraits);
                            if (this.customIndex >= 0)
                            {
                                if (!TM_ClassUtility.CustomClasses()[this.customIndex].isMage)
                                {
                                    this.customIndex = -1;
                                    return false;
                                }
                                else
                                {
                                    this.customClass = TM_ClassUtility.CustomClasses()[this.customIndex];
                                    return true;
                                }
                            }
                        }
                        bool flag4 = base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.BloodMage) ||
                            base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Geomancer) ||
                            base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus) ||
                            base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.InnerFire) ||
                            base.Pawn.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.StormBorn) ||
                            base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin) ||
                            base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Summoner) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Druid) ||
                            (base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Lich)) ||
                            base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Priest) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard) ||
                            base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer) || TM_Calc.IsWanderer(base.Pawn) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage);
                        if (flag4)
                        {
                            return true;
                        }
                    }
                }
                return false;
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
                        ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                        if (Pawn.IsColonist && settingsRef.showLevelUpMessage)
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

        private void AssignAbilities()
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            float hardModeMasterChance = .35f;
            float masterChance = .05f;
            Pawn abilityUser = base.Pawn;
            bool flag2;
            if (abilityUser != null && abilityUser.story != null && abilityUser.story.traits != null)
            {
                if (this.customClass != null)
                {
                    for (int z = 0; z < this.MagicData.AllMagicPowers.Count; z++)
                    {
                        TMAbilityDef ability = (TMAbilityDef)this.MagicData.AllMagicPowers[z].abilityDef;
                        if (this.customClass.classMageAbilities.Contains(ability))
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
                        if (!abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false) && !Rand.Chance(ability.learnChance))
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
                    }
                    MagicPower branding = this.MagicData.AllMagicPowers.FirstOrDefault((MagicPower p) => p.abilityDef == TorannMagicDefOf.TM_Branding);
                    if(branding != null && branding.learned && abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Golemancer))
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
                    if (this.customClass.classHediff != null)
                    {
                        HealthUtility.AdjustSeverity(abilityUser, this.customClass.classHediff, this.customClass.hediffSeverity);
                    }
                }
                else
                {
                    //for (int z = 0; z < this.MagicData.AllMagicPowers.Count; z++)
                    //{
                    //    this.MagicData.AllMagicPowers[z].learned = false;                        
                    //}
                    flag2 = TM_Calc.IsWanderer(abilityUser);
                    if (flag2)
                    {
                        //Log.Message("Initializing Wanderer Abilities");
                        this.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Cantrips).learned = true;
                        this.magicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_WandererCraft).learned = true;
                        for (int i = 0; i < 3; i++)
                        {
                            MagicPower mp = this.MagicData.MagicPowersStandalone.RandomElement();
                            if (mp.abilityDef == TorannMagicDefOf.TM_TransferMana)
                            {
                                mp.learned = true;
                                spell_TransferMana = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_SiphonMana)
                            {
                                mp.learned = true;
                                spell_SiphonMana = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_SpellMending)
                            {
                                mp.learned = true;
                                spell_SpellMending = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_DirtDevil)
                            {
                                mp.learned = true;
                                spell_DirtDevil = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_Heater)
                            {
                                mp.learned = true;
                                spell_Heater = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_Cooler)
                            {
                                mp.learned = true;
                                spell_Cooler = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_PowerNode)
                            {
                                mp.learned = true;
                                spell_PowerNode = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_Sunlight)
                            {
                                mp.learned = true;
                                spell_Sunlight = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_SmokeCloud)
                            {
                                mp.learned = true;
                                spell_SmokeCloud = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_Extinguish)
                            {
                                mp.learned = true;
                                spell_Extinguish = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_EMP)
                            {
                                mp.learned = true;
                                spell_EMP = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_ManaShield)
                            {
                                mp.learned = true;
                                spell_ManaShield = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_Blur)
                            {
                                mp.learned = true;
                                spell_Blur = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_ArcaneBolt)
                            {
                                mp.learned = true;
                                spell_ArcaneBolt = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_LightningTrap)
                            {
                                mp.learned = true;
                                spell_LightningTrap = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_Invisibility)
                            {
                                mp.learned = true;
                                spell_Invisibility = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_MageLight)
                            {
                                mp.learned = true;
                                spell_MageLight = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_Ignite)
                            {
                                mp.learned = true;
                                spell_Ignite = true;
                            }
                            else if (mp.abilityDef == TorannMagicDefOf.TM_SnapFreeze)
                            {
                                mp.learned = true;
                                spell_SnapFreeze = true;
                            }
                            else
                            {
                                int rnd = Rand.RangeInclusive(0, 4);
                                switch (rnd)
                                {
                                    case 0:
                                        this.MagicData.MagicPowersP.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Heal).learned = true;
                                        spell_Heal = true;
                                        break;
                                    case 1:
                                        this.MagicData.MagicPowersA.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Blink).learned = true;
                                        spell_Blink = true;
                                        break;
                                    case 2:
                                        this.MagicData.MagicPowersHoF.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Rainmaker).learned = true;
                                        spell_Rain = true;
                                        break;
                                    case 3:
                                        this.MagicData.MagicPowersS.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonMinion).learned = true;
                                        spell_SummonMinion = true;
                                        break;
                                    case 4:
                                        this.MagicData.MagicPowersA.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Teleport).learned = true;
                                        spell_Teleport = true;
                                        break;
                                }
                            }
                        }
                        if (!abilityUser.IsColonist)
                        {
                            this.spell_ArcaneBolt = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_ArcaneBolt);
                        }
                        InitializeSpell();
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.InnerFire);
                    if (flag2)
                    {
                        //Log.Message("Initializing Inner Fire Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_RayofHope);
                                this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RayofHope).learned = true;
                            }
                            if (Rand.Chance(.6f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Firebolt);
                                this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Firebolt).learned = true;
                            }
                            if (Rand.Chance(.4f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Fireclaw);
                                this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Fireclaw).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Fireball);
                                this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Fireball).learned = true;
                            }
                            this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Firestorm).learned = true;
                        }
                        else
                        {
                            this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RayofHope).learned = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_RayofHope);
                            this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Firebolt).learned = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_Firebolt);
                            this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Fireclaw).learned = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_Fireclaw);
                            this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Fireball).learned = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_Fireball);
                            this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Firestorm).learned = true;

                            if (!abilityUser.IsColonist)
                            {
                                if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                                {
                                    this.spell_Firestorm = true;
                                }
                            }
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost);
                    if (flag2)
                    {
                        //Log.Message("Initializing Heart of Frost Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Soothe);
                                this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Soothe).learned = true;
                            }
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Icebolt);
                                this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Icebolt).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Snowball);
                                this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Snowball).learned = true;
                            }
                            if (Rand.Chance(.4f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_FrostRay);
                                this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_FrostRay).learned = true;
                            }
                            if (Rand.Chance(.7f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Rainmaker);
                                this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Rainmaker).learned = true;
                                this.spell_Rain = true;
                            }
                            this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Blizzard).learned = true;
                        }
                        else
                        {
                            this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Soothe).learned = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_Soothe);
                            this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Icebolt).learned = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_Icebolt);
                            this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Snowball).learned = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_Snowball);
                            this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_FrostRay).learned = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_FrostRay);
                            this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Rainmaker).learned = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_Rainmaker);
                            this.spell_Rain = true;
                            this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Blizzard).learned = true;

                            if (!abilityUser.IsColonist)
                            {
                                if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                                {
                                    this.spell_Blizzard = true;
                                }
                            }
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.StormBorn);
                    if (flag2)
                    {
                        //Log.Message("Initializing Storm Born Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_AMP);
                                this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_AMP).learned = true;
                            }
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_LightningBolt);
                                this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LightningBolt).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_LightningCloud);
                                this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LightningCloud).learned = true;
                            }
                            if (Rand.Chance(.2f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_LightningStorm);
                                this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LightningStorm).learned = true;
                            }
                            this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EyeOfTheStorm).learned = true;
                        }
                        else
                        {
                            this.AddPawnAbility(TorannMagicDefOf.TM_AMP);
                            this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_AMP).learned = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_LightningBolt);
                            this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LightningBolt).learned = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_LightningCloud);
                            this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LightningCloud).learned = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_LightningStorm);
                            this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LightningStorm).learned = true;
                            this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EyeOfTheStorm).learned = true;

                            if (!abilityUser.IsColonist)
                            {
                                if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                                {
                                    this.spell_EyeOfTheStorm = true;
                                }
                            }
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Arcanist);
                    if (flag2)
                    {
                        //Log.Message("Initializing Arcane Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Shadow);
                                this.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shadow).learned = true;
                            }
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_MagicMissile);
                                this.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_MagicMissile).learned = true;
                            }
                            if (Rand.Chance(.7f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Blink);
                                this.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Blink).learned = true;
                                this.spell_Blink = true;
                            }
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Summon);
                                this.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Summon).learned = true;
                            }
                            if (Rand.Chance(.2f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Teleport);
                                this.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Teleport).learned = true;
                                this.spell_Teleport = true;
                            }
                            this.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_FoldReality).learned = true;
                        }
                        else
                        {
                            for(int i = 0; i < this.MagicData.MagicPowersA.Count; i++)
                            {
                                this.MagicData.MagicPowersA[i].learned = true;
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_Shadow);
                            this.AddPawnAbility(TorannMagicDefOf.TM_MagicMissile);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Blink);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Summon);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Teleport);  //Pending Redesign (graphics?)
                            this.spell_Blink = true;
                            this.spell_Teleport = true;

                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Paladin);
                    if (flag2)
                    {
                        //Log.Message("Initializing Paladin Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if(Rand.Chance(TorannMagicDefOf.TM_P_RayofHope.learnChance))
                            {
                                this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_P_RayofHope).learned = true;
                                this.AddPawnAbility(TorannMagicDefOf.TM_P_RayofHope);
                            }
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Heal);
                                this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Heal).learned = true;
                                this.spell_Heal = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Shield);
                                this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shield).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_ValiantCharge);
                                this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ValiantCharge).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Overwhelm);
                                this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Overwhelm).learned = true;
                            }
                            this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_HolyWrath).learned = true;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersP.Count; i++)
                            {
                                this.MagicData.MagicPowersP[i].learned = true;
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_Heal);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Shield);
                            this.AddPawnAbility(TorannMagicDefOf.TM_ValiantCharge);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Overwhelm);
                            this.AddPawnAbility(TorannMagicDefOf.TM_P_RayofHope);
                            this.spell_Heal = true;

                            if (!abilityUser.IsColonist)
                            {
                                if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                                {
                                    this.spell_HolyWrath = true;
                                }
                            }
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Summoner);
                    if (flag2)
                    {
                        //Log.Message("Initializing Summoner Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_SummonMinion);
                                this.MagicData.MagicPowersS.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonMinion).learned = true;
                                this.spell_SummonMinion = true;
                            }
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_SummonPylon);
                                this.MagicData.MagicPowersS.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonPylon).learned = true;
                            }                            
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_SummonExplosive);
                                this.MagicData.MagicPowersS.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonExplosive).learned = true;
                            }                            
                            if (Rand.Chance(.2f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_SummonElemental);
                                this.MagicData.MagicPowersS.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonElemental).learned = true;
                            }
                            this.MagicData.MagicPowersS.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonPoppi).learned = true;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersS.Count; i++)
                            {
                                this.MagicData.MagicPowersS[i].learned = true;
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_SummonMinion);
                            this.AddPawnAbility(TorannMagicDefOf.TM_SummonPylon);
                            this.AddPawnAbility(TorannMagicDefOf.TM_SummonExplosive);
                            this.AddPawnAbility(TorannMagicDefOf.TM_SummonElemental);
                            this.spell_SummonMinion = true;

                            if (!abilityUser.IsColonist)
                            {
                                if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                                {
                                    this.spell_SummonPoppi = true;
                                }
                            }
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Druid);
                    if (flag2)
                    {
                        // Log.Message("Initializing Druid Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(.6f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Poison);
                                this.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Poison).learned = true;
                            }                            
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_SootheAnimal);
                                this.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SootheAnimal).learned = true;
                            }                            
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Regenerate);
                                this.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Regenerate).learned = true;
                            }                            
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_CureDisease);
                                this.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_CureDisease).learned = true;
                            }
                            this.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RegrowLimb).learned = true;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersD.Count; i++)
                            {
                                this.MagicData.MagicPowersD[i].learned = true;
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_Poison);
                            this.AddPawnAbility(TorannMagicDefOf.TM_SootheAnimal);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Regenerate);
                            this.AddPawnAbility(TorannMagicDefOf.TM_CureDisease);
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || abilityUser.story.traits.HasTrait(TorannMagicDefOf.Lich);
                    if (flag2)
                    {
                        //Log.Message("Initializing Necromancer Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_RaiseUndead);
                                this.MagicData.MagicPowersN.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RaiseUndead).learned = true;
                            }                            
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_DeathMark);
                                this.MagicData.MagicPowersN.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_DeathMark).learned = true;
                            }                            
                            if (Rand.Chance(.4f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_FogOfTorment);
                                this.MagicData.MagicPowersN.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_FogOfTorment).learned = true;
                            }                            
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_ConsumeCorpse);
                                this.MagicData.MagicPowersN.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ConsumeCorpse).learned = true;
                            }                           
                            if (Rand.Chance(.2f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_CorpseExplosion);
                                this.MagicData.MagicPowersN.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_CorpseExplosion).learned = true;
                            }
                            this.MagicData.MagicPowersN.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LichForm).learned = true;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersN.Count; i++)
                            {
                                this.MagicData.MagicPowersN[i].learned = true;
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_RaiseUndead);
                            this.AddPawnAbility(TorannMagicDefOf.TM_DeathMark);
                            this.AddPawnAbility(TorannMagicDefOf.TM_FogOfTorment);
                            this.AddPawnAbility(TorannMagicDefOf.TM_ConsumeCorpse);
                            this.AddPawnAbility(TorannMagicDefOf.TM_CorpseExplosion);

                            if (!abilityUser.IsColonist)
                            {
                                if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                                {
                                    this.AddPawnAbility(TorannMagicDefOf.TM_DeathBolt);
                                }
                            }
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Priest);
                    if (flag2)
                    {
                        //Log.Message("Initializing Priest Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_AdvancedHeal);
                                this.MagicData.MagicPowersPR.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_AdvancedHeal).learned = true;
                            }                            
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Purify);
                                this.MagicData.MagicPowersPR.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Purify).learned = true;
                            }                            
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_HealingCircle);
                                this.MagicData.MagicPowersPR.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_HealingCircle).learned = true;
                            }                            
                            if (Rand.Chance(.4f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_BestowMight);
                                this.MagicData.MagicPowersPR.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BestowMight).learned = true;
                            }
                            this.MagicData.MagicPowersPR.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Resurrection).learned = true;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersPR.Count; i++)
                            {
                                this.MagicData.MagicPowersPR[i].learned = true;
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_AdvancedHeal);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Purify);
                            this.AddPawnAbility(TorannMagicDefOf.TM_HealingCircle);
                            this.AddPawnAbility(TorannMagicDefOf.TM_BestowMight);
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Bard);
                    if (flag2)
                    {
                        //Log.Message("Initializing Priest Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (true)
                            {
                                this.MagicData.MagicPowersB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BardTraining).learned = true;
                                this.MagicData.MagicPowersB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Inspire).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Entertain);
                                this.MagicData.MagicPowersB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Entertain).learned = true;
                            }
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Lullaby);
                                this.MagicData.MagicPowersB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Lullaby).learned = true;
                            }
                            this.MagicData.MagicPowersB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BattleHymn).learned = true;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersB.Count; i++)
                            {
                                this.MagicData.MagicPowersB[i].learned = true;
                            }
                            //this.AddPawnAbility(TorannMagicDefOf.TM_BardTraining);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Entertain);
                            //this.AddPawnAbility(TorannMagicDefOf.TM_Inspire);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Lullaby);

                            if (!abilityUser.IsColonist)
                            {
                                if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                                {
                                    this.spell_BattleHymn = true;
                                }
                            }
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Succubus);
                    if (flag2)
                    {
                        //Log.Message("Initializing Succubus Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(.7f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_SoulBond);
                                this.MagicData.MagicPowersSD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SoulBond).learned = true;
                            }
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt);
                                this.MagicData.MagicPowersSD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ShadowBolt).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Dominate);
                                this.MagicData.MagicPowersSD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Dominate).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Attraction);
                                this.MagicData.MagicPowersSD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Attraction).learned = true;
                            }
                            this.MagicData.MagicPowersSD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Scorn).learned = true;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersSD.Count; i++)
                            {
                                this.MagicData.MagicPowersSD[i].learned = true;
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_SoulBond);
                            this.AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Dominate);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Attraction);

                            if (!abilityUser.IsColonist)
                            {
                                if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                                {
                                    this.spell_Scorn = true;
                                }
                            }
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Warlock);
                    if (flag2)
                    {
                        //Log.Message("Initializing Succubus Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(.7f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_SoulBond);
                                this.MagicData.MagicPowersWD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SoulBond).learned = true;
                            }
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt);
                                this.MagicData.MagicPowersWD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ShadowBolt).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Dominate);
                                this.MagicData.MagicPowersWD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Dominate).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Repulsion);
                                this.MagicData.MagicPowersWD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Repulsion).learned = true;
                            }
                            this.MagicData.MagicPowersWD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_PsychicShock).learned = true;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersWD.Count; i++)
                            {
                                this.MagicData.MagicPowersWD[i].learned = true;
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_SoulBond);
                            this.AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Dominate);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Repulsion);
                            if (!abilityUser.IsColonist)
                            {
                                if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                                {
                                    this.spell_PsychicShock = true;
                                }
                            }
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Geomancer);
                    if (flag2)
                    {
                        //Log.Message("Initializing Heart of Frost Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(.4f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Stoneskin);
                                this.MagicData.MagicPowersG.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Stoneskin).learned = true;
                            }
                            if (Rand.Chance(.6f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Encase);
                                this.MagicData.MagicPowersG.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Encase).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_EarthSprites);
                                this.MagicData.MagicPowersG.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EarthSprites).learned = true;
                            }
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_EarthernHammer);
                                this.MagicData.MagicPowersG.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EarthernHammer).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Sentinel);
                                this.MagicData.MagicPowersG.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Sentinel).learned = true;
                            }
                            this.MagicData.MagicPowersG.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Meteor).learned = true;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersG.Count; i++)
                            {
                                this.MagicData.MagicPowersG[i].learned = true;
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_Stoneskin);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Encase);
                            this.AddPawnAbility(TorannMagicDefOf.TM_EarthSprites);
                            this.AddPawnAbility(TorannMagicDefOf.TM_EarthernHammer);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Sentinel);

                            if (!abilityUser.IsColonist)
                            {
                                if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                                {
                                    this.AddPawnAbility(TorannMagicDefOf.TM_Meteor);
                                    this.spell_Meteor = true;
                                }
                            }
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Technomancer);
                    if (flag2)
                    {
                        //Log.Message("Initializing Technomancer Abilities");                        
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(.4f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_TechnoShield);
                                this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TechnoShield).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Sabotage);
                                this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Sabotage).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Overdrive);
                                this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Overdrive).learned = true;
                            }
                            this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_OrbitalStrike).learned = true;
                            if (Rand.Chance(.2f))
                            {
                                this.spell_OrbitalStrike = true;
                                this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_OrbitalStrike).learned = true;
                                this.InitializeSpell();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersT.Count; i++)
                            {
                                this.MagicData.MagicPowersT[i].learned = true;
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_TechnoShield);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Sabotage);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Overdrive);
                            if (!abilityUser.IsColonist)
                            {
                                if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                                {
                                    this.spell_OrbitalStrike = true;
                                }
                            }
                        }
                        this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TechnoBit).learned = false;
                        this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TechnoTurret).learned = false;
                        this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TechnoWeapon).learned = false;
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.BloodMage);
                    if (flag2)
                    {
                        //Log.Message("Initializing Heart of Frost Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(1f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_BloodGift);
                                this.MagicData.MagicPowersBM.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BloodGift).learned = true;
                            }
                            if (Rand.Chance(.4f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_IgniteBlood);
                                this.MagicData.MagicPowersBM.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_IgniteBlood).learned = true;
                            }
                            if (Rand.Chance(.4f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_BloodForBlood);
                                this.MagicData.MagicPowersBM.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BloodForBlood).learned = true;
                            }
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_BloodShield);
                                this.MagicData.MagicPowersBM.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BloodShield).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Rend);
                                this.MagicData.MagicPowersBM.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Rend).learned = true;
                            }
                            this.MagicData.MagicPowersBM.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BloodMoon).learned = true;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersBM.Count; i++)
                            {
                                this.MagicData.MagicPowersBM[i].learned = true;
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_BloodGift);
                            this.AddPawnAbility(TorannMagicDefOf.TM_IgniteBlood);
                            this.AddPawnAbility(TorannMagicDefOf.TM_BloodForBlood);
                            this.AddPawnAbility(TorannMagicDefOf.TM_BloodShield);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Rend);
                            if (!abilityUser.IsColonist)
                            {
                                if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                                {
                                    this.AddPawnAbility(TorannMagicDefOf.TM_BloodMoon);
                                    this.spell_BloodMoon = true;
                                }
                            }
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Enchanter);
                    if (flag2)
                    {
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(.5f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_EnchantedBody);
                                this.AddPawnAbility(TorannMagicDefOf.TM_EnchantedAura);
                                this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchantedAura).learned = true;
                                this.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchantedBody).learned = true;
                                this.spell_EnchantedAura = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Transmutate);
                                this.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Transmutate).learned = true;
                            }
                            if (Rand.Chance(.4f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_EnchanterStone);
                                this.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchanterStone).learned = true;
                            }
                            if (Rand.Chance(.4f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_EnchantWeapon);
                                this.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchantWeapon).learned = true;
                            }
                            if (Rand.Chance(.4f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Polymorph);
                                this.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Polymorph).learned = true;
                            }
                            this.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shapeshift).learned = true;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersE.Count; i++)
                            {
                                this.MagicData.MagicPowersE[i].learned = true;
                            }
                            this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchantedAura).learned = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_EnchantedBody);
                            this.AddPawnAbility(TorannMagicDefOf.TM_EnchantedAura);
                            this.spell_EnchantedAura = true;
                            this.AddPawnAbility(TorannMagicDefOf.TM_Transmutate);
                            this.AddPawnAbility(TorannMagicDefOf.TM_EnchanterStone);
                            this.AddPawnAbility(TorannMagicDefOf.TM_EnchantWeapon);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Polymorph);
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Chronomancer);
                    if (flag2)
                    {
                        //Log.Message("Initializing Chronomancer Abilities");
                        if (abilityUser.IsColonist && !abilityUser.health.hediffSet.HasHediff(TorannMagicDefOf.TM_Uncertainty, false))
                        {
                            if (Rand.Chance(.4f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_Prediction);
                                this.MagicData.MagicPowersC.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Prediction).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_AlterFate);
                                this.MagicData.MagicPowersC.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_AlterFate).learned = true;
                            }
                            if (Rand.Chance(.4f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_AccelerateTime);
                                this.MagicData.MagicPowersC.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_AccelerateTime).learned = true;
                            }
                            if (Rand.Chance(.4f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_ReverseTime);
                                this.MagicData.MagicPowersC.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ReverseTime).learned = true;
                            }
                            if (Rand.Chance(.3f))
                            {
                                this.AddPawnAbility(TorannMagicDefOf.TM_ChronostaticField);
                                this.MagicData.MagicPowersC.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ChronostaticField).learned = true;
                            }
                            this.MagicData.MagicPowersC.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Recall).learned = true;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersC.Count; i++)
                            {
                                this.MagicData.MagicPowersC[i].learned = true;
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_Prediction);
                            this.AddPawnAbility(TorannMagicDefOf.TM_AlterFate);
                            this.AddPawnAbility(TorannMagicDefOf.TM_AccelerateTime);
                            this.AddPawnAbility(TorannMagicDefOf.TM_ReverseTime);
                            this.AddPawnAbility(TorannMagicDefOf.TM_ChronostaticField);

                            if (!abilityUser.IsColonist)
                            {
                                if ((settingsRef.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                                {
                                    this.AddPawnAbility(TorannMagicDefOf.TM_Recall);
                                    this.spell_Recall = true;
                                }
                            }
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.ChaosMage);
                    if (flag2)
                    {
                        foreach (MagicPower current in this.MagicData.AllMagicPowers)
                        {
                            if (current.abilityDef != TorannMagicDefOf.TM_ChaosTradition)
                            {
                                current.learned = false;
                            }
                        }
                        this.MagicData.MagicPowersCM.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ChaosTradition).learned = true;
                        this.AddPawnAbility(TorannMagicDefOf.TM_ChaosTradition);
                        TM_Calc.AssignChaosMagicPowers(this, !abilityUser.IsColonist);
                    }
                }
            }
        }

        public void InitializeSpell()
        {
            Pawn abilityUser = base.Pawn;
            if (this.IsMagicUser)
            {
                if (this.customClass != null)
                {
                    //for(int j = 0; j < this.MagicData.AllMagicPowersWithSkills.Count; j++)
                    //{
                    //    if(this.MagicData.AllMagicPowersWithSkills[j].learned && !this.customClass.classMageAbilities.Contains(this.MagicData.AllMagicPowersWithSkills[j].abilityDef))
                    //    {
                    //        this.MagicData.AllMagicPowersWithSkills[j].learned = false;
                    //        this.RemovePawnAbility(this.MagicData.AllMagicPowersWithSkills[j].abilityDef);
                    //    }
                    //}                   
                    for (int j = 0; j < this.MagicData.AllMagicPowers.Count; j++)
                    {                       
                        if (this.MagicData.AllMagicPowers[j].learned && !this.customClass.classMageAbilities.Contains(this.MagicData.AllMagicPowers[j].abilityDef))
                        {
                            this.RemovePawnAbility(this.MagicData.AllMagicPowers[j].abilityDef);
                            this.AddPawnAbility(this.MagicData.AllMagicPowers[j].abilityDef);                            
                        }
                    }
                    if(this.recallSpell)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Recall);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Recall);
                    }
                }
                else
                {
                    if (this.spell_Rain == true && !abilityUser.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost))
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Rainmaker);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Rainmaker);

                    }
                    if (this.spell_Blink == true && !abilityUser.story.traits.HasTrait(TorannMagicDefOf.Arcanist))
                    {
                        if (!abilityUser.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_Blink);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Blink);
                        }
                        else
                        {
                            bool hasAbility = false;
                            for (int i = 0; i < this.chaosPowers.Count; i++)
                            {
                                if (this.chaosPowers[i].Ability == TorannMagicDefOf.TM_Blink || this.chaosPowers[i].Ability == TorannMagicDefOf.TM_Blink_I || this.chaosPowers[i].Ability == TorannMagicDefOf.TM_Blink_II || this.chaosPowers[i].Ability == TorannMagicDefOf.TM_Blink_III)
                                {
                                    hasAbility = true;
                                }
                            }
                            if (!hasAbility)
                            {
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Blink);
                                this.AddPawnAbility(TorannMagicDefOf.TM_Blink);
                            }
                        }
                    }
                    if (this.spell_Teleport == true && !abilityUser.story.traits.HasTrait(TorannMagicDefOf.Arcanist))
                    {
                        if (!(abilityUser.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) && this.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Teleport).learned))
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_Teleport);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Teleport);
                        }
                    }
                    
                    if (this.spell_Heal == true && !abilityUser.story.traits.HasTrait(TorannMagicDefOf.Paladin))
                    {
                        if (!abilityUser.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_Heal);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Heal);
                        }
                        else
                        {
                            bool hasAbility = false;
                            for (int i = 0; i < this.chaosPowers.Count; i++)
                            {
                                if (this.chaosPowers[i].Ability == TorannMagicDefOf.TM_Heal)
                                {
                                    hasAbility = true;
                                }
                            }
                            if (!hasAbility)
                            {
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Heal);
                                this.AddPawnAbility(TorannMagicDefOf.TM_Heal);
                            }
                        }
                    }
                    if (this.spell_Heater == true)
                    {
                        //if (this.summonedHeaters == null || (this.summonedHeaters != null && this.summonedHeaters.Count <= 0))
                        //{
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Heater);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Heater);
                        //}
                    }
                    if (this.spell_Cooler == true)
                    {
                        //if(this.summonedCoolers == null || (this.summonedCoolers != null && this.summonedCoolers.Count <= 0))
                        //{
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Cooler);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Cooler);
                        //}
                    }
                    if (this.spell_PowerNode == true)
                    {
                        //if (this.summonedPowerNodes == null || (this.summonedPowerNodes != null && this.summonedPowerNodes.Count <= 0))
                        //{
                        this.RemovePawnAbility(TorannMagicDefOf.TM_PowerNode);
                        this.AddPawnAbility(TorannMagicDefOf.TM_PowerNode);
                        //}
                    }
                    if (this.spell_Sunlight == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Sunlight);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Sunlight);

                    }
                    if (this.spell_DryGround == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_DryGround);
                        this.AddPawnAbility(TorannMagicDefOf.TM_DryGround);
                    }
                    if (this.spell_WetGround == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_WetGround);
                        this.AddPawnAbility(TorannMagicDefOf.TM_WetGround);
                    }
                    if (this.spell_ChargeBattery == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_ChargeBattery);
                        this.AddPawnAbility(TorannMagicDefOf.TM_ChargeBattery);
                    }
                    if (this.spell_SmokeCloud == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_SmokeCloud);
                        this.AddPawnAbility(TorannMagicDefOf.TM_SmokeCloud);
                    }
                    if (this.spell_Extinguish == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Extinguish);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Extinguish);
                    }
                    if (this.spell_EMP == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_EMP);
                        this.AddPawnAbility(TorannMagicDefOf.TM_EMP);
                    }
                    if (this.spell_Blizzard == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Blizzard);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Blizzard);
                    }
                    if (this.spell_Firestorm == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Firestorm);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Firestorm);
                    }
                    if (this.spell_SummonMinion == true && !abilityUser.story.traits.HasTrait(TorannMagicDefOf.Summoner))
                    {
                        if (!abilityUser.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_SummonMinion);
                            this.AddPawnAbility(TorannMagicDefOf.TM_SummonMinion);
                        }
                        else
                        {
                            bool hasAbility = false;
                            for (int i = 0; i < this.chaosPowers.Count; i++)
                            {
                                if (this.chaosPowers[i].Ability == TorannMagicDefOf.TM_SummonMinion)
                                {
                                    hasAbility = true;
                                }
                            }
                            if (!hasAbility)
                            {
                                this.RemovePawnAbility(TorannMagicDefOf.TM_SummonMinion);
                                this.AddPawnAbility(TorannMagicDefOf.TM_SummonMinion);
                            }
                        }
                    }
                    if (this.spell_TransferMana == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_TransferMana);
                        this.AddPawnAbility(TorannMagicDefOf.TM_TransferMana);
                    }
                    if (this.spell_SiphonMana == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_SiphonMana);
                        this.AddPawnAbility(TorannMagicDefOf.TM_SiphonMana);
                    }
                    if (this.spell_RegrowLimb == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_RegrowLimb);
                        this.AddPawnAbility(TorannMagicDefOf.TM_RegrowLimb);
                    }
                    if (this.spell_EyeOfTheStorm == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_EyeOfTheStorm);
                        this.AddPawnAbility(TorannMagicDefOf.TM_EyeOfTheStorm);
                    }
                    if (this.spell_HeatShield == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_HeatShield);
                        this.AddPawnAbility(TorannMagicDefOf.TM_HeatShield);
                    }
                    if (this.spell_ManaShield == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_ManaShield);
                        this.AddPawnAbility(TorannMagicDefOf.TM_ManaShield);
                    }
                    if (this.spell_Blur == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Blur);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Blur);
                    }
                    if (this.spell_FoldReality == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_FoldReality);
                        this.AddPawnAbility(TorannMagicDefOf.TM_FoldReality);
                    }
                    if (this.spell_Resurrection == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Resurrection);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Resurrection);
                    }
                    if (this.spell_HolyWrath == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_HolyWrath);
                        this.AddPawnAbility(TorannMagicDefOf.TM_HolyWrath);
                    }
                    if (this.spell_LichForm == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_LichForm);
                        this.AddPawnAbility(TorannMagicDefOf.TM_LichForm);
                    }
                    if (this.spell_Flight == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Flight);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Flight);
                    }
                    if (this.spell_SummonPoppi == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_SummonPoppi);
                        this.AddPawnAbility(TorannMagicDefOf.TM_SummonPoppi);
                    }
                    if (this.spell_BattleHymn == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_BattleHymn);
                        this.AddPawnAbility(TorannMagicDefOf.TM_BattleHymn);
                    }
                    if (this.spell_CauterizeWound == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_CauterizeWound);
                        this.AddPawnAbility(TorannMagicDefOf.TM_CauterizeWound);
                    }
                    if (this.spell_SpellMending == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_SpellMending);
                        this.AddPawnAbility(TorannMagicDefOf.TM_SpellMending);
                    }
                    if (this.spell_FertileLands == true)
                    {
                        //if (this.fertileLands == null || (this.fertileLands != null && this.fertileLands.Count <= 0))
                        //{
                        this.RemovePawnAbility(TorannMagicDefOf.TM_FertileLands);
                        this.AddPawnAbility(TorannMagicDefOf.TM_FertileLands);
                        //}
                    }
                    if (this.spell_PsychicShock == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_PsychicShock);
                        this.AddPawnAbility(TorannMagicDefOf.TM_PsychicShock);
                    }
                    if (this.spell_Scorn == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Scorn);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Scorn);
                    }
                    if (this.spell_BlankMind == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_BlankMind);
                        this.AddPawnAbility(TorannMagicDefOf.TM_BlankMind);
                    }
                    if (this.spell_ShadowStep == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_ShadowStep);
                        this.AddPawnAbility(TorannMagicDefOf.TM_ShadowStep);
                    }
                    if (this.spell_ShadowCall == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_ShadowCall);
                        this.AddPawnAbility(TorannMagicDefOf.TM_ShadowCall);
                    }
                    if (this.spell_Teach == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_TeachMagic);
                        this.AddPawnAbility(TorannMagicDefOf.TM_TeachMagic);
                    }
                    if (this.spell_Meteor == true)
                    {
                        MagicPower meteorPower = this.MagicData.MagicPowersG.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Meteor);
                        if (meteorPower == null)
                        {
                            meteorPower = this.MagicData.MagicPowersG.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Meteor_I);
                            if (meteorPower == null)
                            {
                                meteorPower = this.MagicData.MagicPowersG.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Meteor_II);
                                if (meteorPower == null)
                                {
                                    meteorPower = this.MagicData.MagicPowersG.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Meteor_III);
                                }
                            }
                        }
                        if (meteorPower.level == 3)
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_Meteor_III);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Meteor_III);
                        }
                        else if (meteorPower.level == 2)
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_Meteor_II);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Meteor_II);
                        }
                        else if (meteorPower.level == 1)
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_Meteor_I);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Meteor_I);
                        }
                        else
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_Meteor);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Meteor);
                        }
                    }
                    if (this.spell_OrbitalStrike == true)
                    {
                        MagicPower OrbitalStrikePower = this.MagicData.magicPowerT.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_OrbitalStrike);
                        if (OrbitalStrikePower == null)
                        {
                            OrbitalStrikePower = this.MagicData.magicPowerT.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_I);
                            if (OrbitalStrikePower == null)
                            {
                                OrbitalStrikePower = this.MagicData.magicPowerT.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_II);
                                if (OrbitalStrikePower == null)
                                {
                                    OrbitalStrikePower = this.MagicData.magicPowerT.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_III);
                                }
                            }
                        }
                        if (OrbitalStrikePower.level == 3)
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_OrbitalStrike_III);
                            this.AddPawnAbility(TorannMagicDefOf.TM_OrbitalStrike_III);
                        }
                        else if (OrbitalStrikePower.level == 2)
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_OrbitalStrike_II);
                            this.AddPawnAbility(TorannMagicDefOf.TM_OrbitalStrike_II);
                        }
                        else if (OrbitalStrikePower.level == 1)
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_OrbitalStrike_I);
                            this.AddPawnAbility(TorannMagicDefOf.TM_OrbitalStrike_I);
                        }
                        else
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_OrbitalStrike);
                            this.AddPawnAbility(TorannMagicDefOf.TM_OrbitalStrike);
                        }
                    }
                    if (this.spell_BloodMoon == true)
                    {
                        MagicPower BloodMoonPower = this.MagicData.MagicPowersBM.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BloodMoon);
                        if (BloodMoonPower == null)
                        {
                            BloodMoonPower = this.MagicData.MagicPowersBM.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BloodMoon_I);
                            if (BloodMoonPower == null)
                            {
                                BloodMoonPower = this.MagicData.MagicPowersBM.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BloodMoon_II);
                                if (BloodMoonPower == null)
                                {
                                    BloodMoonPower = this.MagicData.MagicPowersBM.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BloodMoon_III);
                                }
                            }
                        }
                        if (BloodMoonPower.level == 3)
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_BloodMoon_III);
                            this.AddPawnAbility(TorannMagicDefOf.TM_BloodMoon_III);
                        }
                        else if (BloodMoonPower.level == 2)
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_BloodMoon_II);
                            this.AddPawnAbility(TorannMagicDefOf.TM_BloodMoon_II);
                        }
                        else if (BloodMoonPower.level == 1)
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_BloodMoon_I);
                            this.AddPawnAbility(TorannMagicDefOf.TM_BloodMoon_I);
                        }
                        else
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_BloodMoon);
                            this.AddPawnAbility(TorannMagicDefOf.TM_BloodMoon);
                        }
                    }
                    if (this.spell_EnchantedAura == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_EnchantedAura);
                        this.AddPawnAbility(TorannMagicDefOf.TM_EnchantedAura);
                    }
                    if (this.spell_Shapeshift == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Shapeshift);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Shapeshift);
                    }
                    if (this.spell_ShapeshiftDW == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_ShapeshiftDW);
                        this.AddPawnAbility(TorannMagicDefOf.TM_ShapeshiftDW);
                    }
                    if (this.spell_DirtDevil == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_DirtDevil);
                        this.AddPawnAbility(TorannMagicDefOf.TM_DirtDevil);
                    }
                    if (this.spell_MechaniteReprogramming == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_MechaniteReprogramming);
                        this.AddPawnAbility(TorannMagicDefOf.TM_MechaniteReprogramming);
                    }
                    if (this.spell_ArcaneBolt == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_ArcaneBolt);
                        this.AddPawnAbility(TorannMagicDefOf.TM_ArcaneBolt);
                    }
                    if (this.spell_LightningTrap == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_LightningTrap);
                        this.AddPawnAbility(TorannMagicDefOf.TM_LightningTrap);
                    }
                    if (this.spell_Invisibility == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Invisibility);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Invisibility);
                    }
                    if (this.spell_BriarPatch == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_BriarPatch);
                        this.AddPawnAbility(TorannMagicDefOf.TM_BriarPatch);
                    }
                    if (this.spell_Recall == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_TimeMark);
                        this.AddPawnAbility(TorannMagicDefOf.TM_TimeMark);
                        if (this.recallSpell)
                        {
                            this.RemovePawnAbility(TorannMagicDefOf.TM_Recall);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Recall);
                        }
                    }
                    if (this.spell_MageLight == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_MageLight);
                        this.AddPawnAbility(TorannMagicDefOf.TM_MageLight);
                    }
                    if (this.spell_SnapFreeze == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_SnapFreeze);
                        this.AddPawnAbility(TorannMagicDefOf.TM_SnapFreeze);
                    }
                    if (this.spell_Ignite == true)
                    {
                        this.RemovePawnAbility(TorannMagicDefOf.TM_Ignite);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Ignite);
                    }
                    
                    if (this.IsMagicUser && this.MagicData.MagicPowersCustomAll != null && this.MagicData.MagicPowersCustomAll.Count > 0)
                    {
                        for (int j = 0; j < this.MagicData.MagicPowersCustomAll.Count; j++)
                        {
                            if (this.MagicData.MagicPowersCustomAll[j].learned)
                            {
                                this.RemovePawnAbility(this.MagicData.MagicPowersCustomAll[j].abilityDef);
                                this.AddPawnAbility(this.MagicData.MagicPowersCustomAll[j].abilityDef);
                            }
                        }
                    }
                }
                //this.UpdateAbilities();
            }
        }

        public void RemovePowers(bool clearStandalone = true)
        {
            Pawn abilityUser = base.Pawn;
            if (this.magicPowersInitialized == true && MagicData != null)
            {
                bool flag2 = true;
                if (this.customClass != null)
                {
                    for (int i = 0; i < this.MagicData.AllMagicPowers.Count; i++)
                    {
                        MagicPower mp = this.MagicData.AllMagicPowers[i];
                        for (int j = 0; j < mp.TMabilityDefs.Count; j++)
                        {
                            TMAbilityDef tmad = mp.TMabilityDefs[j] as TMAbilityDef;
                            if(tmad.childAbilities != null && tmad.childAbilities.Count > 0)
                            {
                                for(int k = 0; k < tmad.childAbilities.Count; k++)
                                {
                                    this.RemovePawnAbility(tmad.childAbilities[k]);
                                }
                            }                            
                            this.RemovePawnAbility(tmad);
                        }
                        mp.learned = false;
                    }
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.InnerFire);
                if (TM_Calc.IsWanderer(this.Pawn))
                {
                    this.spell_ArcaneBolt = false;
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ArcaneBolt);
                }
                if (abilityUser.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
                {
                    foreach (MagicPower current in this.MagicData.AllMagicPowersForChaosMage)
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
                                        this.RemovePawnAbility(tmad.childAbilities[k]);
                                    }
                                }
                                this.RemovePawnAbility(tmad);
                            }
                        }
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_EnchantedAura);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_NanoStimulant);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_LightSkipMass);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_LightSkipGlobal);
                    this.spell_EnchantedAura = false;
                    this.spell_ShadowCall = false;
                    this.spell_ShadowStep = false;
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ShadowCall);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ShadowStep);

                }
                if (flag2)
                {
                    //Log.Message("Fixing Inner Fire Abilities");
                    foreach (MagicPower currentIF in this.MagicData.MagicPowersIF)
                    {
                        if (currentIF.abilityDef != TorannMagicDefOf.TM_Firestorm)
                        {
                            currentIF.learned = false;
                        }
                        this.RemovePawnAbility(currentIF.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_RayofHope_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_RayofHope_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_RayofHope_III);

                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost);
                if (flag2)
                {
                    //Log.Message("Fixing Heart of Frost Abilities");
                    foreach (MagicPower currentHoF in this.MagicData.MagicPowersHoF)
                    {
                        if (currentHoF.abilityDef != TorannMagicDefOf.TM_Blizzard)
                        {
                            currentHoF.learned = false;
                        }
                        this.RemovePawnAbility(currentHoF.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Soothe_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Soothe_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Soothe_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_FrostRay_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_FrostRay_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_FrostRay_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.StormBorn);
                if (flag2)
                {
                    //Log.Message("Fixing Storm Born Abilities");                   
                    foreach (MagicPower currentSB in this.MagicData.MagicPowersSB)
                    {
                        if (currentSB.abilityDef != TorannMagicDefOf.TM_EyeOfTheStorm)
                        {
                            currentSB.learned = false;
                        }
                        this.RemovePawnAbility(currentSB.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_AMP_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_AMP_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_AMP_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Arcanist);
                if (flag2)
                {
                    //Log.Message("Fixing Arcane Abilities");
                    foreach (MagicPower currentA in this.MagicData.MagicPowersA)
                    {
                        if (currentA.abilityDef != TorannMagicDefOf.TM_FoldReality)
                        {
                            currentA.learned = false;
                        }
                        this.RemovePawnAbility(currentA.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Shadow_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Shadow_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Shadow_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_MagicMissile_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_MagicMissile_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_MagicMissile_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Blink_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Blink_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Blink_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Summon_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Summon_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Summon_III);

                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Paladin);
                if (flag2)
                {
                    //Log.Message("Fixing Paladin Abilities");
                    foreach (MagicPower currentP in this.MagicData.MagicPowersP)
                    {
                        if (currentP.abilityDef != TorannMagicDefOf.TM_HolyWrath)
                        {
                            currentP.learned = false;
                        }
                        this.RemovePawnAbility(currentP.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Shield_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Shield_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Shield_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Summoner);
                if (flag2)
                {
                    foreach (MagicPower currentS in this.MagicData.MagicPowersS)
                    {
                        if (currentS.abilityDef != TorannMagicDefOf.TM_SummonPoppi)
                        {
                            currentS.learned = false;
                        }
                        this.RemovePawnAbility(currentS.abilityDef);
                    }
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Druid);
                if (flag2)
                {
                    foreach (MagicPower currentD in this.MagicData.MagicPowersD)
                    {
                        if (currentD.abilityDef != TorannMagicDefOf.TM_RegrowLimb)
                        {
                            currentD.learned = false;
                        }
                        this.RemovePawnAbility(currentD.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_SootheAnimal_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_SootheAnimal_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_SootheAnimal_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Necromancer) || abilityUser.story.traits.HasTrait(TorannMagicDefOf.Lich);
                if (flag2)
                {
                    foreach (MagicPower currentN in this.MagicData.MagicPowersN)
                    {
                        if (currentN.abilityDef != TorannMagicDefOf.TM_LichForm)
                        {
                            currentN.learned = false;
                        }
                        this.RemovePawnAbility(currentN.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_DeathMark_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_DeathMark_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_DeathMark_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ConsumeCorpse_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ConsumeCorpse_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ConsumeCorpse_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_CorpseExplosion_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_CorpseExplosion_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_CorpseExplosion_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_DeathBolt_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_DeathBolt_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_DeathBolt_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Priest);
                if (flag2)
                {
                    foreach (MagicPower currentPR in this.MagicData.MagicPowersPR)
                    {
                        if (currentPR.abilityDef != TorannMagicDefOf.TM_Resurrection)
                        {
                            currentPR.learned = false;
                        }
                        this.RemovePawnAbility(currentPR.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_HealingCircle_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_HealingCircle_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_HealingCircle_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_BestowMight_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_BestowMight_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_BestowMight_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Bard);
                if (flag2)
                {
                    foreach (MagicPower currentB in this.MagicData.MagicPowersB)
                    {
                        if (currentB.abilityDef != TorannMagicDefOf.TM_BattleHymn)
                        {
                            currentB.learned = false;
                        }
                        this.RemovePawnAbility(currentB.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Lullaby_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Lullaby_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Lullaby_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Succubus);
                if (flag2)
                {
                    foreach (MagicPower currentSD in this.MagicData.MagicPowersSD)
                    {
                        if (currentSD.abilityDef != TorannMagicDefOf.TM_Scorn)
                        {
                            currentSD.learned = false;
                        }
                        this.RemovePawnAbility(currentSD.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ShadowBolt_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ShadowBolt_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ShadowBolt_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Attraction_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Attraction_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Attraction_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Warlock);
                if (flag2)
                {
                    foreach (MagicPower currentWD in this.MagicData.MagicPowersWD)
                    {
                        if (currentWD.abilityDef != TorannMagicDefOf.TM_PsychicShock)
                        {
                            currentWD.learned = false;
                        }
                        this.RemovePawnAbility(currentWD.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Repulsion_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Repulsion_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Repulsion_III);
                }
                // flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Geomancer);
                if (flag2)
                {
                    foreach (MagicPower currentG in this.MagicData.MagicPowersG)
                    {
                        if (currentG.abilityDef == TorannMagicDefOf.TM_Meteor || currentG.abilityDef == TorannMagicDefOf.TM_Meteor_I || currentG.abilityDef == TorannMagicDefOf.TM_Meteor_II || currentG.abilityDef == TorannMagicDefOf.TM_Meteor_III)
                        {
                            currentG.learned = true;
                        }
                        else
                        {
                            currentG.learned = false;
                        }
                        this.RemovePawnAbility(currentG.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Encase_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Encase_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Encase_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Meteor_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Meteor_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Meteor_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Technomancer);
                if (flag2)
                {
                    foreach (MagicPower currentT in this.MagicData.MagicPowersT)
                    {
                        if (currentT.abilityDef == TorannMagicDefOf.TM_OrbitalStrike || currentT.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_I || currentT.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_II || currentT.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_III)
                        {
                            currentT.learned = true;
                        }
                        else
                        {
                            currentT.learned = false;
                        }
                        this.RemovePawnAbility(currentT.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_OrbitalStrike_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_OrbitalStrike_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_OrbitalStrike_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.BloodMage);
                if (flag2)
                {
                    foreach (MagicPower currentBM in this.MagicData.MagicPowersBM)
                    {
                        if (currentBM.abilityDef == TorannMagicDefOf.TM_BloodMoon || currentBM.abilityDef == TorannMagicDefOf.TM_BloodMoon_I || currentBM.abilityDef == TorannMagicDefOf.TM_BloodMoon_II || currentBM.abilityDef == TorannMagicDefOf.TM_BloodMoon_III)
                        {
                            currentBM.learned = true;
                        }
                        else
                        {
                            currentBM.learned = false;
                        }
                        this.RemovePawnAbility(currentBM.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Rend_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Rend_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Rend_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_BloodMoon_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_BloodMoon_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_BloodMoon_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Enchanter);
                if (flag2)
                {

                    foreach (MagicPower currentE in this.MagicData.MagicPowersE)
                    {
                        if (currentE.abilityDef != TorannMagicDefOf.TM_Shapeshift)
                        {
                            currentE.learned = false;
                        }
                        this.RemovePawnAbility(currentE.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Polymorph_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Polymorph_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Polymorph_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_EnchantedAura);
                }
                // flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Chronomancer);
                if (flag2)
                {
                    foreach (MagicPower currentC in this.MagicData.MagicPowersC)
                    {
                        if (currentC.abilityDef != TorannMagicDefOf.TM_Recall)
                        {
                            currentC.learned = false;
                        }
                        this.RemovePawnAbility(currentC.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ChronostaticField_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ChronostaticField_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ChronostaticField_III);
                }
                if (flag2)
                {
                    foreach (MagicPower currentShadow in this.MagicData.MagicPowersShadow)
                    {
                        this.RemovePawnAbility(currentShadow.abilityDef);
                    }
                }
                if (clearStandalone)
                {
                    foreach (MagicPower currentStandalone in this.MagicData.MagicPowersStandalone)
                    {
                        this.RemovePawnAbility(currentStandalone.abilityDef);
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
            //this.magicPowersInitialized = false;
            //base.IsInitialized = false;
            //CompAbilityUserMagic.MagicAbilities = null;
            //this.magicData = null;
            this.AssignAbilities();
          
        }

        private void LoadPowers()
        {
            foreach (MagicPower currentA in this.MagicData.MagicPowersA)
            {
                //Log.Message("Removing ability: " + currentA.abilityDef.defName.ToString());
                currentA.level = 0;
                base.RemovePawnAbility(currentA.abilityDef);
            }
            foreach (MagicPower currentHoF in this.MagicData.MagicPowersHoF)
            {
                //Log.Message("Removing ability: " + currentHoF.abilityDef.defName.ToString());
                currentHoF.level = 0;
                base.RemovePawnAbility(currentHoF.abilityDef);
            }
            foreach (MagicPower currentSB in this.MagicData.MagicPowersSB)
            {
                //Log.Message("Removing ability: " + currentSB.abilityDef.defName.ToString());
                currentSB.level = 0;
                base.RemovePawnAbility(currentSB.abilityDef);
            }
            foreach (MagicPower currentIF in this.MagicData.MagicPowersIF)
            {
                //Log.Message("Removing ability: " + currentIF.abilityDef.defName.ToString());
                currentIF.level = 0;
                base.RemovePawnAbility(currentIF.abilityDef);
            }
            foreach (MagicPower currentP in this.MagicData.MagicPowersP)
            {
                //Log.Message("Removing ability: " + currentP.abilityDef.defName.ToString());
                currentP.level = 0;
                base.RemovePawnAbility(currentP.abilityDef);
            }
            foreach (MagicPower currentS in this.MagicData.MagicPowersS)
            {
                //Log.Message("Removing ability: " + currentP.abilityDef.defName.ToString());
                currentS.level = 0;
                base.RemovePawnAbility(currentS.abilityDef);
            }
            foreach (MagicPower currentD in this.MagicData.MagicPowersD)
            {
                //Log.Message("Removing ability: " + currentP.abilityDef.defName.ToString());
                currentD.level = 0;
                base.RemovePawnAbility(currentD.abilityDef);
            }
            foreach (MagicPower currentN in this.MagicData.MagicPowersN)
            {
                //Log.Message("Removing ability: " + currentP.abilityDef.defName.ToString());
                currentN.level = 0;
                base.RemovePawnAbility(currentN.abilityDef);
            }
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
            List<Hediff> allHediffs = this.Pawn.health.hediffSet.GetHediffs<Hediff>().ToList();
            for (int i = 0; i < allHediffs.Count(); i++)
            {
                Hediff hediff = allHediffs[i];
                if (hediff.def.defName.Contains("TM_"))
                {
                    this.Pawn.health.RemoveHediff(hediff);
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
        }

        public int MagicAttributeEffeciencyLevel(string attributeName)
        {
            int result = 0;

            if (attributeName == "TM_RayofHope_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_RayofHope.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Firebolt_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Firebolt.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Fireclaw_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Fireclaw.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Fireball_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Fireball.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Firestorm_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Firestorm.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }

            if (attributeName == "TM_Soothe_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Soothe.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Icebolt_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Icebolt.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_FrostRay_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_FrostRay.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Snowball_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Snowball.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Rainmaker_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Rainmaker.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Blizzard_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Blizzard.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }

            if (attributeName == "TM_AMP_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_AMP.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_LightningBolt_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_LightningBolt.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_LightningCloud_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_LightningCloud.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_LightningStorm_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_LightningStorm.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }

            if (attributeName == "TM_Shadow_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Shadow.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_MagicMissile_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_MagicMissile.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Blink_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Blink.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Summon_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Summon.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Teleport_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Teleport.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_FoldReality_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_FoldReality.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Heal_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Heal.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Shield_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Shield.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_ValiantCharge_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_ValiantCharge.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Overwhelm_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Overwhelm.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_HolyWrath_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_HolyWrath.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_SummonMinion_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_SummonMinion.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_SummonPylon_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_SummonPylon.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_SummonExplosive_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_SummonExplosive.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_SummonElemental_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_SummonElemental.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_SummonPoppi_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_SummonPoppi.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Poison_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Poison.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_SootheAnimal_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_SootheAnimal.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Regenerate_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Regenerate.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_CureDisease_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_CureDisease.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_RegrowLimb_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_RegrowLimb.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_EyeOfTheStorm_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_EyeOfTheStorm.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_RaiseUndead_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_RaiseUndead.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_DeathMark_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_DeathMark.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_FogOfTorment_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_FogOfTorment.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_ConsumeCorpse_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_ConsumeCorpse.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_CorpseExplosion_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_CorpseExplosion.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_DeathBolt_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_DeathBolt.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_AdvancedHeal_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_AdvancedHeal.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Purify_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Purify.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_HealingCircle_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_HealingCircle.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_BestowMight_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_BestowMight.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Resurrection_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Resurrection.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Lullaby_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Lullaby.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_BattleHymn_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_BattleHymn.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_SoulBond_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_SoulBond.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_ShadowBolt_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_ShadowBolt.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Dominate_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Dominate.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Attraction_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Attraction.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Repulsion_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Repulsion.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Scorn_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Scorn.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_PsychicShock_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_PsychicShock.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Stoneskin_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Stoneskin.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Encase_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Encase.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_EarthSprites_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_EarthSprites.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_EarthernHammer_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_EarthernHammer.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Sentinel_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Sentinel.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Meteor_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Meteor.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_TechnoTurret_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_TechnoTurret.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_TechnoShield_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_TechnoShield.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Sabotage_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Sabotage.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Overdrive_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Overdrive.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_OrbitalStrike_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_OrbitalStrike.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_BloodGift_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_BloodGift.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_IgniteBlood_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_IgniteBlood.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_BloodForBlood_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_BloodForBlood.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_BloodShield_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_BloodShield.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Rend_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Rend.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_BloodMoon_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_BloodMoon.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_EnchantedBody_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_EnchantedBody.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Transmutate_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Transmutate.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_EnchanterStone_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_EnchanterStone.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_EnchantWeapon_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_EnchantWeapon.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Polymorph_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Polymorph.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Shapeshift_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Shapeshift.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Prediction_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Prediction.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_AlterFate_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_AlterFate.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_AccelerateTime_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_AccelerateTime.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_ReverseTime_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_ReverseTime.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_ChronostaticField_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_ChronostaticField.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Recall_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Recall.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_WandererCraft_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_WandererCraft.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_Cantrips_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
                {
                    result = magicPowerSkill.level;
                }
            }
            if (attributeName == "TM_ChaosTradition_eff")
            {
                MagicPowerSkill magicPowerSkill = this.MagicData.MagicPowerSkill_ChaosTradition.FirstOrDefault((MagicPowerSkill x) => x.label == attributeName);
                bool flag = magicPowerSkill != null;
                if (flag)
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
                    adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_EnchantedBody).level);
                }
                else if (magicDef == TorannMagicDefOf.TM_ShapeshiftDW)
                {
                    adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Shapeshift).level);
                }
                else if (magicDef == TorannMagicDefOf.TM_ShadowStep || magicDef == TorannMagicDefOf.TM_ShadowCall)
                {
                    adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_SoulBond).level);
                }
                else if( magicDef == TorannMagicDefOf.TM_LightSkipGlobal || magicDef == TorannMagicDefOf.TM_LightSkipMass)
                {
                    adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_LightSkip).level);
                }      
                else if(magicDef == TorannMagicDefOf.TM_SummonTotemEarth || magicDef == TorannMagicDefOf.TM_SummonTotemHealing || magicDef == TorannMagicDefOf.TM_SummonTotemLightning)
                {
                    adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Totems).level);
                }
                else if (magicDef == TorannMagicDefOf.TM_Hex_CriticalFail || magicDef == TorannMagicDefOf.TM_Hex_Pain || magicDef == TorannMagicDefOf.TM_Hex_MentalAssault)
                {
                    adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Hex).level);
                }
                else if(this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    CompAbilityUserMight compMight = this.Pawn.TryGetComp<CompAbilityUserMight>();
                    adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * compMight.MightData.GetSkill_Efficiency(TorannMagicDefOf.TM_Mimic).level);
                }
                else
                {
                    MagicPowerSkill mps = this.MagicData.GetSkill_Efficiency(magicDef);
                    if (mps != null)
                    {
                        adjustedManaCost *= 1f - (magicDef.efficiencyReductionPercent * mps.level);
                    }
                }
            }

            if (this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_SyrriumSenseHD"), false))
            {
                adjustedManaCost = adjustedManaCost * .9f;
            }
            if (this.mpCost != 1f)
            {
                if (magicDef == TorannMagicDefOf.TM_Explosion)
                {
                    adjustedManaCost = adjustedManaCost * (1f - ((1f - this.mpCost)/10f));
                }
                else
                {
                    adjustedManaCost = adjustedManaCost * this.mpCost;
                }
            }
            if (magicDef.abilityHediff != null && this.Pawn.health.hediffSet.HasHediff(magicDef.abilityHediff))
            {
                return 0f;
            }
            if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                adjustedManaCost = 0;
            }
            if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) || (this.customClass != null && this.customClass.classMageAbilities.Contains(TorannMagicDefOf.TM_ChaosTradition)))
            {
                adjustedManaCost = adjustedManaCost * 1.2f;
            }

            if(this.Pawn.Map.gameConditionManager.ConditionIsActive(TorannMagicDefOf.TM_ManaStorm))
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
            Pawn abilityUser = base.Pawn;

            List<Hediff> list = new List<Hediff>();
            List<Hediff> arg_32_0 = list;
            IEnumerable<Hediff> arg_32_1;
            if (abilityUser == null)
            {
                arg_32_1 = null;
            }
            else
            {
                Pawn_HealthTracker expr_1A = abilityUser.health;
                if (expr_1A == null)
                {
                    arg_32_1 = null;
                }
                else
                {
                    HediffSet expr_26 = expr_1A.hediffSet;
                    arg_32_1 = ((expr_26 != null) ? expr_26.hediffs : null);
                }
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
            bool flag = (arg_84_0 ?? 0) > 0;
            if (flag)
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
                        float dmgAmt = (float)dinfo.Amount;
                        float dmgToSev = 0.004f;
                        ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                        if (!abilityUser.IsColonist)
                        {
                            if (settingsRef.AIHardMode)
                            {
                                dmgToSev = 0.0025f;
                            }
                            else
                            {
                                dmgToSev = 0.003f;
                            }
                        }
                        sev = sev - (dmgAmt * dmgToSev);
                        if (sev < 0)
                        {
                            actualDmg = (int)Mathf.RoundToInt(Mathf.Abs(sev / dmgToSev));
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
                        float dmgAmt = (float)dinfo.Amount;
                        float dmgToSev = 1f;
                        ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                        if (!abilityUser.IsColonist)
                        {
                            if (settingsRef.AIHardMode)
                            {
                                dmgToSev = 0.8f;
                            }
                            else
                            {
                                dmgToSev = 1f;
                            }
                        }
                        sev = sev - (dmgAmt * dmgToSev);
                        if (sev < 0)
                        {
                            actualDmg = (int)Mathf.RoundToInt(Mathf.Abs(sev / dmgToSev));
                            BreakShield(abilityUser);
                        }
                        TM_Action.DisplayShieldHit(abilityUser, dinfo);
                        current.Severity = sev;
                        dinfo.SetAmount(actualDmg);

                        return;
                    }
                    if (current.def == TorannMagicDefOf.TM_ManaShieldHD && this.damageMitigationDelayMS < this.age)
                    {
                        float sev = this.Mana.CurLevel;
                        absorbed = true;
                        int actualDmg = 0;
                        float dmgAmt = (float)dinfo.Amount;
                        float dmgToSev = 0.02f;
                        float maxDmg = 11f;
                        if (this.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_ver").level >= 3)
                        {
                            dmgToSev = 0.015f;
                            maxDmg = 14f;
                            if (this.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_ver").level >= 7)
                            {
                                dmgToSev = 0.012f;
                                maxDmg = 17f;
                            }
                        }
                        if (dmgAmt >= maxDmg)
                        {
                            actualDmg = Mathf.RoundToInt(dmgAmt - maxDmg);
                            sev = sev - (maxDmg * dmgToSev);
                        }
                        else
                        {
                            sev = sev - (dmgAmt * dmgToSev);
                        }
                        this.Mana.CurLevel = sev;
                        if (sev < 0)
                        {
                            actualDmg = (int)Mathf.RoundToInt(Mathf.Abs(sev / dmgToSev));
                            BreakShield(abilityUser);
                            current.Severity = sev;
                            abilityUser.health.RemoveHediff(current);
                        }
                        TM_Action.DisplayShieldHit(abilityUser, dinfo);
                        this.damageMitigationDelayMS = this.age + 2;
                        dinfo.SetAmount(actualDmg);
                        abilityUser.TakeDamage(dinfo);
                        return;
                    }
                    if (current.def == TorannMagicDefOf.TM_LichHD && this.damageMitigationDelay < this.age)
                    {
                        absorbed = true;
                        int mitigationAmt = 4;
                        int actualDmg;
                        int dmgAmt = Mathf.RoundToInt(dinfo.Amount);
                        if (dmgAmt < mitigationAmt)
                        {
                            MoteMaker.ThrowText(this.Pawn.DrawPos, this.Pawn.Map, "TM_DamageAbsorbedAll".Translate(), -1);
                            actualDmg = 0;
                            return;
                        }
                        else
                        {
                            MoteMaker.ThrowText(this.Pawn.DrawPos, this.Pawn.Map, "TM_DamageAbsorbed".Translate(
                                dmgAmt,
                                mitigationAmt
                            ), -1);
                            actualDmg = dmgAmt - mitigationAmt;
                        }
                        this.damageMitigationDelay = this.age + 6;
                        dinfo.SetAmount(actualDmg);
                        abilityUser.TakeDamage(dinfo);
                        return;
                    }
                    if (arcaneRes != 0 && resMitigationDelay < this.age)
                    {
                        if (current.def == TorannMagicDefOf.TM_HediffEnchantment_arcaneRes)
                        {
                            if ((dinfo.Def.armorCategory != null && (dinfo.Def.armorCategory == TorannMagicDefOf.Dark || dinfo.Def.armorCategory == TorannMagicDefOf.Light)) || dinfo.Def.defName.Contains("TM_") || dinfo.Def.defName == "FrostRay" || dinfo.Def.defName == "Snowball" || dinfo.Def.defName == "Iceshard" || dinfo.Def.defName == "Firebolt")
                            {
                                absorbed = true;
                                int actualDmg = Mathf.RoundToInt(dinfo.Amount / arcaneRes);
                                resMitigationDelay = this.age + 10;
                                dinfo.SetAmount(actualDmg);
                                abilityUser.TakeDamage(dinfo);
                                return;
                            }
                        }
                    }
                }

                list.Clear();
                list = null;
            }
            base.PostPreApplyDamage(dinfo, out absorbed);
        }

        private void BreakShield(Pawn pawn)
        {
            SoundDefOf.EnergyShield_Broken.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
            FleckMaker.Static(pawn.TrueCenter(), pawn.Map, FleckDefOf.ExplosionFlash, 12f);
            for (int i = 0; i < 6; i++)
            {
                Vector3 loc = pawn.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle((float)Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f);
                FleckMaker.ThrowDustPuff(loc, pawn.Map, Rand.Range(0.8f, 1.2f));
            }
        }

        public void DoArcaneForging()
        {
            if (this.Pawn.CurJob.targetA.Thing.def.defName == "TableArcaneForge")
            {
                this.ArcaneForging = true;
                Job job = this.Pawn.CurJob;
                Thing forge = this.Pawn.CurJob.targetA.Thing;
                if (this.Pawn.Position == forge.InteractionCell && this.Pawn.jobs.curDriver.CurToilIndex >= 10)
                {
                    if (Find.TickManager.TicksGame % 20 == 0)
                    {
                        if (this.Mana.CurLevel >= .1f)
                        {
                            this.Mana.CurLevel -= .025f;
                            this.MagicUserXP += 4;
                            FleckMaker.ThrowSmoke(forge.DrawPos, forge.Map, Rand.Range(.8f, 1.2f));
                        }
                        else
                        {
                            this.Pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
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
                this.ArcaneForging = false;
            }
        }

        public void ResolveMagicUseEvents()
        {
            List<TM_EventRecords> tmpList = new List<TM_EventRecords>();
            tmpList.Clear();
            foreach(TM_EventRecords ev in MagicUsed)
            {
                if(Find.TickManager.TicksGame - 60000 > ev.eventTick)
                {
                    tmpList.Add(ev);
                }
            }
            foreach(TM_EventRecords rem_ev in tmpList)
            {
                MagicUsed.Remove(rem_ev);
            }
        }

        public void ResolveAutoCast()
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            bool flagCM = this.Pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage);
            bool isCustom = this.customClass != null;
            //bool flagCP = this.Pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless);
            //CompAbilityUserMight compMight = null;
            //if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    compMight = this.Pawn.TryGetComp<CompAbilityUserMight>();
            //}
            if (settingsRef.autocastEnabled && this.Pawn.jobs != null && this.Pawn.CurJob != null && this.Pawn.CurJob.def != TorannMagicDefOf.TMCastAbilityVerb && this.Pawn.CurJob.def != TorannMagicDefOf.TMCastAbilitySelf && 
                this.Pawn.CurJob.def != JobDefOf.Ingest && this.Pawn.CurJob.def != JobDefOf.ManTurret && this.Pawn.GetPosture() == PawnPosture.Standing && !this.Pawn.CurJob.playerForced && !this.Pawn.Map.GameConditionManager.ConditionIsActive(TorannMagicDefOf.ManaDrain) && !this.Pawn.Map.GameConditionManager.ConditionIsActive(TorannMagicDefOf.TM_ManaStorm))
            {
                //Log.Message("pawn " + this.Pawn.LabelShort + " current job is " + this.Pawn.CurJob.def.defName);
                //non-combat (undrafted) spells
                bool castSuccess = false;
                if (this.Pawn.drafter != null && !this.Pawn.Drafted && this.Mana != null && this.Mana.CurLevelPercentage >= settingsRef.autocastMinThreshold)
                {
                    foreach (MagicPower mp in this.MagicData.MagicPowersCustomAll)
                    {
                        if (mp.learned && mp.autocast && mp.autocasting != null && mp.autocasting.magicUser && mp.autocasting.undrafted)
                        {
                            TMAbilityDef tmad = mp.TMabilityDefs[mp.level] as TMAbilityDef; // issues with index?
                            bool canUseWithEquippedWeapon = true;
                            bool canUseIfViolentAbility = this.Pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent) ? !tmad.MainVerb.isViolent : true;
                            if(!TM_Calc.HasResourcesForAbility(this.Pawn, tmad))
                            {
                                continue;
                            }
                            if (canUseWithEquippedWeapon && canUseIfViolentAbility)
                            {
                                PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                if (mp.autocasting.type == TMDefs.AutocastType.OnTarget && this.Pawn.CurJob.targetA != null && this.Pawn.CurJob.targetA.Thing != null)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.CurJob.targetA);
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
                                            if (targetPawn.Downed || targetPawn.IsPrisonerInPrisonCell())
                                            {
                                                continue;
                                            }
                                        }
                                        bool TN = mp.autocasting.targetNeutral && (targetThing.Faction == null || !targetThing.Faction.HostileTo(this.Pawn.Faction));
                                        bool TF = mp.autocasting.targetFriendly && targetThing.Faction == this.Pawn.Faction;
                                        if (!(TE || TN || TF))
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
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.CurJob.targetA);
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
                                if (mp.autocasting.type == TMDefs.AutocastType.OnCell && this.Pawn.CurJob.targetA != null)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.CurJob.targetA);
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
                                    if(mp.autocasting.maxRange == 0f)
                                    {
                                        mp.autocasting.maxRange = mp.abilityDef.MainVerb.range;
                                    }
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.CurJob.targetA);
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
                                            if (targetPawn.Downed || targetPawn.IsPrisonerInPrisonCell())
                                            {
                                                continue;
                                            }
                                        }
                                        bool TN = mp.autocasting.targetNeutral && (targetThing.Faction == null || !targetThing.Faction.HostileTo(this.Pawn.Faction));
                                        bool TF = mp.autocasting.targetFriendly && targetThing.Faction == this.Pawn.Faction;
                                        if (!(TE || TN || TF))
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
                                if (castSuccess) goto AutoCastExit;
                            }
                        }
                    }

                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Summoner) || flagCM || isCustom)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersS.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonMinion);
                        if (magicPower != null && magicPower.learned && magicPower.autocast && this.summonedMinions.Count() < 4)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_SummonMinion);
                            AutoCast.MagicAbility_OnSelfPosition.Evaluate(this, TorannMagicDefOf.TM_SummonMinion, ability, magicPower, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer) || isCustom) && !this.recallSet)
                    {
                        if (this.AbilityData.Powers.Any(p => p.Def == TorannMagicDefOf.TM_TimeMark))
                        {
                            MagicPower magicPower = this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TimeMark);
                            if (magicPower != null && (magicPower.learned || spell_Recall) && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                            {
                                PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_TimeMark);
                                AutoCast.MagicAbility_OnSelfPosition.Evaluate(this, TorannMagicDefOf.TM_TimeMark, ability, magicPower, out castSuccess);
                                if (castSuccess) goto AutoCastExit;
                            }
                        }
                    }
                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist) || flagCM || isCustom)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersA)
                        {
                            if (current != null && current.abilityDef != null)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if ((tmad == TorannMagicDefOf.TM_Summon || tmad == TorannMagicDefOf.TM_Summon_I || tmad == TorannMagicDefOf.TM_Summon_II || tmad == TorannMagicDefOf.TM_Summon_III) && !this.Pawn.CurJob.playerForced)
                                    {
                                        //Log.Message("evaluating " + tmad.defName);
                                        MagicPower magicPower = this.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            float minDistance = ActualManaCost(tmad) * 150;
                                            AutoCast.Summon.Evaluate(this, tmad, ability, magicPower, minDistance, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                    if ((tmad == TorannMagicDefOf.TM_Blink || tmad == TorannMagicDefOf.TM_Blink_I || tmad == TorannMagicDefOf.TM_Blink_II || tmad == TorannMagicDefOf.TM_Blink_III))
                                    {
                                        MagicPower magicPower = this.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            float minDistance = ActualManaCost(tmad) * 240;
                                            AutoCast.Blink.Evaluate(this, tmad, ability, magicPower, minDistance, out castSuccess);
                                            if (castSuccess)
                                            {
                                                goto AutoCastExit;
                                            }
                                        }
                                        if (flagCM && magicPower != null && this.spell_Blink && !magicPower.learned && magicPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            float minDistance = ActualManaCost(tmad) * 200;
                                            AutoCast.Blink.Evaluate(this, tmad, ability, magicPower, minDistance, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Druid) || flagCM || isCustom)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersD)
                        {
                            if (current != null && current.abilityDef != null && current.learned && !this.Pawn.CurJob.playerForced)
                            {
                                if (current.abilityDef == TorannMagicDefOf.TM_Regenerate)
                                {
                                    MagicPower magicPower = this.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == current.abilityDef);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_Regenerate);
                                        MagicPowerSkill pwr = this.MagicData.MagicPowerSkill_Regenerate.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Regenerate_pwr");
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
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                                if (current.abilityDef == TorannMagicDefOf.TM_CureDisease)
                                {
                                    MagicPower magicPower = this.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == current.abilityDef);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_CureDisease);
                                        MagicPowerSkill ver = this.MagicData.MagicPowerSkill_CureDisease.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_CureDisease_ver");

                                        List<string> afflictionList = new List<string>();
                                        afflictionList.Clear();
                                        foreach (TMDefs.TM_CategoryHediff chd in HediffCategoryList.Named("TM_Category_Hediffs").diseases)
                                        {
                                            if (chd.requiredSkillName == "TM_CureDisease_ver" && chd.requiredSkillLevel <= ver.level)
                                            {
                                                afflictionList.Add(chd.hediffDefname);
                                            }
                                        }
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
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                                if (current.abilityDef == TorannMagicDefOf.TM_RegrowLimb && spell_RegrowLimb)
                                {
                                    MagicPower magicPower = this.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == current.abilityDef);
                                    bool workPriorities = true;
                                    if (this.Pawn.CurJob != null && this.Pawn.CurJob.workGiverDef != null && this.Pawn.CurJob.workGiverDef.workType != null)
                                    {
                                        workPriorities = this.Pawn.workSettings.GetPriority(this.Pawn.CurJob.workGiverDef.workType) >= this.Pawn.workSettings.GetPriority(TorannMagicDefOf.TM_Magic);
                                    }
                                    if (magicPower != null && magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced && workPriorities)
                                    {
                                        Area tArea = TM_Calc.GetSeedOfRegrowthArea(this.Pawn.Map, false);
                                        if (tArea != null)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_RegrowLimb);
                                            AutoCast.OnTarget_Spell.TryExecute(this, TorannMagicDefOf.TM_RegrowLimb, ability, magicPower, tArea.ActiveCells.RandomElement(), 40, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin) || flagCM || isCustom)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersP)
                        {
                            if (current != null && current.abilityDef != null && current.learned && !this.Pawn.CurJob.playerForced)
                            {
                                foreach(TMAbilityDef tmad in current.TMabilityDefs)
                                { 
                                    if (tmad == TorannMagicDefOf.TM_Heal)
                                    {
                                        MagicPower magicPower = this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            AutoCast.HealSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                    if ((tmad == TorannMagicDefOf.TM_Shield || tmad == TorannMagicDefOf.TM_Shield_I || tmad == TorannMagicDefOf.TM_Shield_II || tmad == TorannMagicDefOf.TM_Shield_III))
                                    {
                                        MagicPower magicPower = this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            AutoCast.Shield.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }                                        
                                }
                            }
                        }
                    }
                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Priest) || flagCM || isCustom)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersPR)
                        {
                            if (current != null && current.abilityDef != null && current.learned && !this.Pawn.CurJob.playerForced)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if (tmad == TorannMagicDefOf.TM_AdvancedHeal)
                                    {
                                        MagicPower magicPower = this.MagicData.MagicPowersPR.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            AutoCast.HealSpell.EvaluateMinSeverity(this, tmad, ability, magicPower, 1f, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                    if (tmad == TorannMagicDefOf.TM_Purify)
                                    {
                                        MagicPower magicPower = this.MagicData.MagicPowersPR.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_Purify);
                                            MagicPowerSkill ver = this.MagicData.MagicPowerSkill_Purify.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Purify_ver");
                                            AutoCast.HealPermanentSpell.Evaluate(this, TorannMagicDefOf.TM_Purify, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                            List<string> afflictionList = new List<string>();
                                            afflictionList.Clear();
                                            foreach(TMDefs.TM_CategoryHediff chd in HediffCategoryList.Named("TM_Category_Hediffs").ailments)
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
                                            if (castSuccess) goto AutoCastExit;
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
                                            foreach (TMDefs.TM_CategoryHediff chd in HediffCategoryList.Named("TM_Category_Hediffs").addictions)
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
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter) || flagCM || isCustom)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Transmutate);
                        bool workPriorities = true;
                        if (this.Pawn.CurJob != null && this.Pawn.CurJob.workGiverDef != null && this.Pawn.CurJob.workGiverDef.workType != null)
                        {
                            workPriorities = this.Pawn.workSettings.GetPriority(this.Pawn.CurJob.workGiverDef.workType) >= this.Pawn.workSettings.GetPriority(TorannMagicDefOf.TM_Magic);
                        }
                        if (magicPower != null && magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced && workPriorities)
                        {
                            Area tArea = TM_Calc.GetTransmutateArea(this.Pawn.Map, false);
                            if (tArea != null)
                            {
                                bool _out;
                                Thing transmuteThing = TM_Calc.GetTransmutableThingFromCell(tArea.ActiveCells.RandomElement(), this.Pawn, out _out, out _out, out _out, out _out, out _out);
                                if (transmuteThing != null)
                                {
                                    PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_Transmutate);
                                    AutoCast.OnTarget_Spell.TryExecute(this, TorannMagicDefOf.TM_Transmutate, ability, magicPower, transmuteThing, 50, out castSuccess);
                                    if (castSuccess) goto AutoCastExit;
                                }
                            }
                        }
                    }
                    if ((this.spell_MechaniteReprogramming && this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer)) || flagCM || isCustom)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_MechaniteReprogramming);
                        if (magicPower != null && magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_MechaniteReprogramming);
                            List<string> afflictionList = new List<string>();
                            afflictionList.Clear();
                            foreach (TMDefs.TM_CategoryHediff chd in HediffCategoryList.Named("TM_Category_Hediffs").mechanites)
                            {
                                afflictionList.Add(chd.hediffDefname);                                
                            }
                            //afflictionList.Add("SensoryMechanites");
                            //afflictionList.Add("FibrousMechanites");
                            AutoCast.CureSpell.Evaluate(this, TorannMagicDefOf.TM_MechaniteReprogramming, ability, magicPower, afflictionList, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_Heal && !this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin) && !isCustom)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Heal);
                        if (magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_Heal);
                            AutoCast.HealSpell.Evaluate(this, TorannMagicDefOf.TM_Heal, ability, magicPower, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_TransferMana || isCustom)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TransferMana);
                        if (magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_TransferMana);
                            AutoCast.TransferManaSpell.Evaluate(this, TorannMagicDefOf.TM_TransferMana, ability, magicPower, false, false, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_SiphonMana || isCustom)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SiphonMana);
                        if (magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_SiphonMana);
                            AutoCast.TransferManaSpell.Evaluate(this, TorannMagicDefOf.TM_SiphonMana, ability, magicPower, false, true, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_CauterizeWound || isCustom)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_CauterizeWound);
                        if (magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_CauterizeWound);
                            AutoCast.HealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_CauterizeWound, ability, magicPower, 40f, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_SpellMending || isCustom)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SpellMending);
                        if (magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_SpellMending);
                            AutoCast.SpellMending.Evaluate(this, TorannMagicDefOf.TM_SpellMending, ability, magicPower, HediffDef.Named("SpellMendingHD"), out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_Teach || isCustom)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TeachMagic);
                        if (magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                        {
                            if (this.Pawn.CurJobDef.joyKind != null || this.Pawn.CurJobDef == JobDefOf.Wait_Wander || Pawn.CurJobDef == JobDefOf.GotoWander)
                            {
                                PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_TeachMagic);
                                AutoCast.Teach.Evaluate(this, TorannMagicDefOf.TM_TeachMagic, ability, magicPower, out castSuccess);
                                if (castSuccess) goto AutoCastExit;
                            }
                        }
                    }
                    if (this.spell_SummonMinion && !this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Summoner) && !isCustom)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersS.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonMinion);
                        if (magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced && this.summonedMinions.Count() < 4)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_SummonMinion);
                            AutoCast.MagicAbility_OnSelfPosition.Evaluate(this, TorannMagicDefOf.TM_SummonMinion, ability, magicPower, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_DirtDevil || isCustom)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_DirtDevil);
                        if (magicPower != null && magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced && this.Pawn.GetRoom() != null)
                        {
                            float roomCleanliness = this.Pawn.GetRoom().GetStat(RoomStatDefOf.Cleanliness);

                            if (roomCleanliness < -3f)
                            {
                                PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_DirtDevil);
                                AutoCast.MagicAbility_OnSelfPosition.Evaluate(this, TorannMagicDefOf.TM_DirtDevil, ability, magicPower, out castSuccess);
                                if (castSuccess) goto AutoCastExit;
                            }
                        }
                    }
                    if (this.spell_Blink && !this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist) && !flagCM && !isCustom)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Blink);
                        if (magicPower.autocast)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_Blink);
                            float minDistance = ActualManaCost(TorannMagicDefOf.TM_Blink) * 200;
                            AutoCast.Blink.Evaluate(this, TorannMagicDefOf.TM_Blink, ability, magicPower, minDistance, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                }

                //combat (drafted) spells
                if (this.Pawn.drafter != null && this.Pawn.Drafted && this.Pawn.drafter.FireAtWill && this.Pawn.CurJob.def != JobDefOf.Goto && this.Mana != null && this.Mana.CurLevelPercentage >= settingsRef.autocastCombatMinThreshold)
                {
                    foreach (MagicPower mp in this.MagicData.MagicPowersCustom)
                    {
                        if (mp.learned && mp.autocast && mp.autocasting != null && mp.autocasting.magicUser && mp.autocasting.drafted)
                        {
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
                                if (mp.autocasting.type == TMDefs.AutocastType.OnTarget && this.Pawn.TargetCurrentlyAimingAt != null && this.Pawn.TargetCurrentlyAimingAt.Thing != null)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.TargetCurrentlyAimingAt);
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
                                            if (targetPawn.Downed || targetPawn.IsPrisonerInPrisonCell())
                                            {
                                                continue;
                                            }
                                        }
                                        bool TN = mp.autocasting.targetNeutral && (targetThing.Faction == null || !targetThing.Faction.HostileTo(this.Pawn.Faction));
                                        bool TF = mp.autocasting.targetFriendly && targetThing.Faction == this.Pawn.Faction;
                                        if (!(TE || TN || TF))
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
                                if (mp.autocasting.type == TMDefs.AutocastType.OnCell && this.Pawn.TargetCurrentlyAimingAt != null)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.TargetCurrentlyAimingAt);
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
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.TargetCurrentlyAimingAt);
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
                                            if (targetPawn.Downed || targetPawn.IsPrisonerInPrisonCell())
                                            {
                                                continue;
                                            }
                                        }
                                        bool TN = mp.autocasting.targetNeutral && (targetThing.Faction == null || !targetThing.Faction.HostileTo(this.Pawn.Faction));
                                        bool TF = mp.autocasting.targetFriendly && targetThing.Faction == this.Pawn.Faction;
                                        if (!(TE || TN || TF))
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
                                if (castSuccess) goto AutoCastExit;
                            }
                        }
                    }

                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.InnerFire) || flagCM || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersIF)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                if (current.abilityDef == TorannMagicDefOf.TM_Firebolt)
                                {
                                    MagicPower magicPower = this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == current.abilityDef);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_Firebolt);
                                        AutoCast.DamageSpell.Evaluate(this, TorannMagicDefOf.TM_Firebolt, ability, magicPower, out castSuccess);
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost) || flagCM || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersHoF)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if (tmad == TorannMagicDefOf.TM_Icebolt)
                                    {
                                        MagicPower magicPower = this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Icebolt);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_Icebolt);
                                            AutoCast.DamageSpell.Evaluate(this, TorannMagicDefOf.TM_Icebolt, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                    else if ((tmad == TorannMagicDefOf.TM_FrostRay || tmad == TorannMagicDefOf.TM_FrostRay_I || tmad == TorannMagicDefOf.TM_FrostRay_II || tmad == TorannMagicDefOf.TM_FrostRay_III))
                                    {
                                        MagicPower magicPower = this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            AutoCast.DamageSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }                                       
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.StormBorn) || flagCM || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersSB)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                if (current.abilityDef == TorannMagicDefOf.TM_LightningBolt)
                                {
                                    MagicPower magicPower = this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LightningBolt);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_LightningBolt);
                                        AutoCast.DamageSpell.Evaluate(this, TorannMagicDefOf.TM_LightningBolt, ability, magicPower, out castSuccess);
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist) || flagCM || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersA)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if ((tmad == TorannMagicDefOf.TM_MagicMissile || tmad == TorannMagicDefOf.TM_MagicMissile_I || tmad == TorannMagicDefOf.TM_MagicMissile_II || tmad == TorannMagicDefOf.TM_MagicMissile_III))
                                    {
                                        MagicPower magicPower = this.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            AutoCast.DamageSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }                                        
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Druid) || flagCM || isCustom))
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersD)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                if (current.abilityDef == TorannMagicDefOf.TM_Poison && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                                {
                                    MagicPower magicPower = this.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Poison);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_Poison);
                                        AutoCast.HediffSpell.EvaluateMinRange(this, TorannMagicDefOf.TM_Poison, ability, magicPower, HediffDef.Named("TM_Poisoned_HD"), 10, out castSuccess);
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                                if (current.abilityDef == TorannMagicDefOf.TM_Regenerate)
                                {
                                    MagicPower magicPower = this.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Regenerate);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_Regenerate);
                                        MagicPowerSkill pwr = this.MagicData.MagicPowerSkill_Regenerate.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Regenerate_pwr");
                                        if (pwr.level == 0)
                                        {
                                            AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration"), 10f, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                        else if (pwr.level == 1)
                                        {
                                            AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_I"), 12f, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                        else if (pwr.level == 2)
                                        {
                                            AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_II"), 14f, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                        else
                                        {
                                            AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_III"), 16f, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus) || flagCM || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersSD)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if ((tmad == TorannMagicDefOf.TM_ShadowBolt || tmad == TorannMagicDefOf.TM_ShadowBolt_I || tmad == TorannMagicDefOf.TM_ShadowBolt_II || tmad == TorannMagicDefOf.TM_ShadowBolt_III))
                                    {
                                        MagicPower magicPower = this.MagicData.MagicPowersSD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            AutoCast.DamageSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }                                        
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock) || flagCM || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersWD)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if ((tmad == TorannMagicDefOf.TM_ShadowBolt || tmad == TorannMagicDefOf.TM_ShadowBolt_I || tmad == TorannMagicDefOf.TM_ShadowBolt_II || tmad == TorannMagicDefOf.TM_ShadowBolt_III))
                                    {
                                        MagicPower magicPower = this.MagicData.MagicPowersWD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            AutoCast.DamageSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }                                        
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin) || flagCM || isCustom))
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersP)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if (tmad == TorannMagicDefOf.TM_Heal)
                                    {
                                        MagicPower magicPower = this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            AutoCast.HealSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                    if ((tmad == TorannMagicDefOf.TM_Shield || tmad == TorannMagicDefOf.TM_Shield_I || tmad == TorannMagicDefOf.TM_Shield_II || tmad == TorannMagicDefOf.TM_Shield_III))
                                    {
                                        MagicPower magicPower = this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            AutoCast.Shield.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Priest) || flagCM || isCustom)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersPR)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                if (current.abilityDef == TorannMagicDefOf.TM_AdvancedHeal)
                                {
                                    MagicPower magicPower = this.MagicData.MagicPowersPR.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_AdvancedHeal);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_AdvancedHeal);
                                        AutoCast.HealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_AdvancedHeal, ability, magicPower, 1f, out castSuccess);
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                            }
                        }
                    }
                    if ((this.spell_Heal && !this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin)))
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Heal);
                        if (magicPower.autocast)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_Heal);
                            AutoCast.HealSpell.Evaluate(this, TorannMagicDefOf.TM_Heal, ability, magicPower, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_SiphonMana || isCustom)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SiphonMana);
                        if (magicPower.learned && magicPower.autocast)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_SiphonMana);
                            AutoCast.TransferManaSpell.Evaluate(this, TorannMagicDefOf.TM_SiphonMana, ability, magicPower, true, true, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_CauterizeWound || isCustom)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_CauterizeWound);
                        if (magicPower.learned && magicPower.autocast)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_CauterizeWound);
                            AutoCast.HealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_CauterizeWound, ability, magicPower, 40f, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if ((this.spell_ArcaneBolt || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        MagicPower magicPower = this.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ArcaneBolt);
                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_ArcaneBolt);
                            AutoCast.DamageSpell.Evaluate(this, TorannMagicDefOf.TM_ArcaneBolt, ability, magicPower, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                }
                AutoCastExit:;
            }
        }

        public void ResolveAIAutoCast()
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (settingsRef.autocastEnabled && this.Pawn.jobs != null && this.Pawn.CurJob != null && this.Pawn.CurJob.def != TorannMagicDefOf.TMCastAbilityVerb && this.Pawn.CurJob.def != TorannMagicDefOf.TMCastAbilitySelf && 
                this.Pawn.CurJob.def != JobDefOf.Ingest && this.Pawn.CurJob.def != JobDefOf.ManTurret && this.Pawn.GetPosture() == PawnPosture.Standing)
            {
                bool castSuccess = false;
                if (this.Mana != null && this.Mana.CurLevelPercentage >= settingsRef.autocastMinThreshold)
                {
                    foreach (MagicPower mp in this.MagicData.MagicPowersCustom)
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
                                        bool TN = mp.autocasting.targetNeutral && (targetThing.Faction == null || !targetThing.Faction.HostileTo(this.Pawn.Faction));
                                        if (TN && targetThing is Pawn)
                                        {
                                            Pawn targetPawn = targetThing as Pawn;
                                            if (targetPawn.Downed || targetPawn.IsPrisoner)
                                            {
                                                continue;
                                            }
                                            if (mp.abilityDef.MainVerb.isViolent && targetThing.Faction != null && !targetPawn.InMentalState)
                                            {
                                                continue;
                                            }
                                        }                                        
                                        bool TF = mp.autocasting.targetFriendly && targetThing.Faction == this.Pawn.Faction;
                                        if (!(TE || TN || TF))
                                        {
                                            continue;
                                        }
                                        //if (targetThing is Pawn)
                                        //{
                                        //    Pawn targetPawn = targetThing as Pawn;
                                        //    if (targetPawn.IsPrisoner)
                                        //    {
                                        //        continue;
                                        //    }
                                        //}
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
                                        bool TN = mp.autocasting.targetNeutral && (targetThing.Faction == null || !targetThing.Faction.HostileTo(this.Pawn.Faction));
                                        if (TN && targetThing is Pawn)
                                        {
                                            Pawn targetPawn = targetThing as Pawn;
                                            if (targetPawn.Downed || targetPawn.IsPrisoner)
                                            {
                                                continue;
                                            }
                                            if (mp.abilityDef.MainVerb.isViolent && targetThing.Faction != null && !targetPawn.InMentalState)
                                            {
                                                continue;
                                            }
                                        }
                                        bool TF = mp.autocasting.targetFriendly && targetThing.Faction == this.Pawn.Faction;
                                        if (!(TE || TN || TF))
                                        {
                                            continue;
                                        }
                                        //if (targetThing is Pawn)
                                        //{
                                        //    Pawn targetPawn = targetThing as Pawn;
                                        //    if (targetPawn.IsPrisoner)
                                        //    {
                                        //        continue;
                                        //    }
                                        //}
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
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    if (Rand.Chance(settingsRef.magicyteChance * 2))
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
                if (curCell.InBounds(map) && curCell.IsValid && terrain != null)
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
            bool spawned = this.Pawn.Spawned;
            if (spawned)
            {
                if (this.powerEffecter != null && this.PowerModifier == 0)
                {
                    this.powerEffecter.Cleanup();
                    this.powerEffecter = null;
                }
                bool flag4 = this.powerEffecter == null && this.PowerModifier > 0;
                if (flag4)
                {
                    EffecterDef progressBar = EffecterDefOf.ProgressBar;
                    this.powerEffecter = progressBar.Spawn();
                }
                if (this.powerEffecter != null && this.PowerModifier > 0)
                {
                    this.powerEffecter.EffectTick(this.Pawn, TargetInfo.Invalid);
                    MoteProgressBar mote = ((SubEffecter_ProgressBar)this.powerEffecter.children[0]).mote;
                    bool flag5 = mote != null;
                    if (flag5)
                    {
                        float value = (float)(this.powerModifier) / (float)(this.maxPower);
                        mote.progress = Mathf.Clamp01(value);
                        mote.offsetZ = +0.85f;
                    }
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
            if (this.Pawn.CurrentBed() != null && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_VitalityBoostHD"), false))
            {
                Pawn pawnInMyBed = TM_Calc.FindNearbyOtherPawn(this.Pawn, 1);
                if (pawnInMyBed != null)
                {
                    if (pawnInMyBed.CurrentBed() != null && pawnInMyBed.RaceProps.Humanlike)
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
            if(this.BrandedPawns.Count > 0)
            {
                if(!this.dispelBrandings)
                {
                    this.AddPawnAbility(TorannMagicDefOf.TM_DispelBranding);
                    this.dispelBrandings = true;
                }
                List<Pawn> tmpBrands = new List<Pawn>();
                tmpBrands.Clear();
                foreach(Pawn brand in this.BrandedPawns)
                {
                    if(brand.DestroyedOrNull() || brand.Dead)
                    {
                        tmpBrands.Add(brand);
                    }
                }
                foreach(Pawn removeBrand in tmpBrands)
                {
                    BrandedPawns.Remove(removeBrand);
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
                    MagicPowerSkill bardtraining_pwr = this.Pawn.GetComp<CompAbilityUserMagic>().MagicData.MagicPowerSkill_BardTraining.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BardTraining_pwr");

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
            if(this.BrandedPawns.Count > 0)
            {
                float brandCost = this.BrandedPawns.Count * (TorannMagicDefOf.TM_Branding.upkeepRegenCost * (1f - (TorannMagicDefOf.TM_Branding.upkeepEfficiencyPercent * this.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Branding).level)));
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
            using (IEnumerator<Hediff> enumerator = this.Pawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
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
            Scribe_Collections.Look<Pawn>(ref this.brandedPawns, "brandedPawns", LookMode.Reference);
            Scribe_Values.Look<bool>(ref this.sigilSurging, "sigilSurging", false, false);
            Scribe_Values.Look<bool>(ref this.sigilDraining, "sigilDraining", false, false);
            Scribe_References.Look<FlyingObject_LivingWall>(ref this.livingWall, "livingWall");
            Scribe_Deep.Look(ref this.magicWardrobe, "magicWardrobe", new object[0]);
            //
            Scribe_Deep.Look<MagicData>(ref this.magicData, "magicData", new object[]
            {
                this
            });
            bool flag11 = Scribe.mode == LoadSaveMode.PostLoadInit;
            if (flag11)
            {
                Pawn abilityUser = base.Pawn;
                int index = TM_ClassUtility.IsCustomClassIndex(abilityUser.story.traits.allTraits);
                if (index >= 0)
                {                    
                    if (TM_ClassUtility.CustomClasses()[index].isMage)
                    {
                        this.customClass = TM_ClassUtility.CustomClasses()[index];
                        this.customIndex = index;
                        for (int i = 0; i < this.customClass.classMageAbilities.Count; i++)
                        {                            
                            TMAbilityDef ability = customClass.classMageAbilities[i];

                            for (int j = 0; j < this.MagicData.AllMagicPowers.Count; j++)
                            {
                                if (this.MagicData.AllMagicPowers[j] == this.MagicData.MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SoulBond) ||
                                        this.MagicData.AllMagicPowers[j] == this.MagicData.MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ShadowBolt) ||
                                        this.MagicData.AllMagicPowers[j] == this.MagicData.MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Dominate))
                                {
                                    this.MagicData.AllMagicPowers[j].learned = false;
                                }
                                if (this.MagicData.AllMagicPowers[j].TMabilityDefs.Contains(this.customClass.classMageAbilities[i]) && this.MagicData.AllMagicPowers[j].learned)
                                {
                                    if (this.customClass.classMageAbilities[i].shouldInitialize)
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
                                        if(this.customClass.classMageAbilities[i] == TorannMagicDefOf.TM_Hex && this.HexedPawns.Count > 0)
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
                this.UpdateAutocastDef();
                this.InitializeSpell();
                //base.UpdateAbilities();
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
                if (key == "wanderer")
                {
                    Command_Action itemWanderer = new Command_Action
                    {
                        action = new Action(delegate
                        {
                            TM_Action.PromoteWanderer(p);
                        }),
                        order = 51,
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
                        order = -89,
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
                        order = -88,
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
                        order = -88,
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
