using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using AbilityUser;
using AbilityUserAI;
using Verse.AI;
using UnityEngine;
using System.Text;
using CompDeflector;
using TorannMagic.Ideology;

namespace TorannMagic
{
    [CompilerGenerated]
    [Serializable]
    [StaticConstructorOnStartup]
    public class CompAbilityUserMight : CompAbilityUser
    {
        public string LabelKey = "TM_Might";

        public int customIndex = -2;
        public TMDefs.TM_CustomClass customClass = null;

        public bool mightPowersInitialized = false;
        public bool firstMightTick = false;
        private int age = -1;
        private int fortitudeMitigationDelay = 0;
        private int mightXPRate = 900;
        private int lastMightXPGain = 0;
        private int autocastTick = 0;
        private int nextAICastAttemptTick = 0;
        private int nextSSTend = 0;
        public bool canDeathRetaliate = false;
        private bool deathRetaliating = false;
        private int ticksTillRetaliation = 600;
        private List<IntVec3> deathRing = new List<IntVec3>();
        public float weaponDamage = 1f;
        public float weaponCritChance = 0f;
        public bool shouldDrawPsionicShield = false;
        public List<TM_EventRecords> mightUsed = new List<TM_EventRecords>();

        private float G_Sprint_eff = 0.20f;
        private float G_Grapple_eff = 0.10f;
        private float G_Cleave_eff = 0.10f;
        private float G_Whirlwind_eff = 0.10f;
        private float S_Headshot_eff = 0.10f;
        private float S_DisablingShot_eff = 0.10f;
        private float S_AntiArmor_eff = .10f;
        private float B_SeismicSlash_eff = 0.10f;
        private float B_BladeSpin_eff = 0.10f;
        private float B_PhaseStrike_eff = 0.08f;
        private float R_AnimalFriend_eff = 0.15f;
        private float R_ArrowStorm_eff = 0.08f;
        private float F_Disguise_eff = 0.10f;
        private float F_Mimic_eff = 0.08f;
        private float F_Reversal_eff = 0.10f;
        private float F_Transpose_eff = 0.08f;
        private float F_Possess_eff = 0.06f;
        private float P_PsionicBarrier_eff = 0.10f;
        private float P_PsionicBlast_eff = 0.08f;
        private float P_PsionicDash_eff = 0.10f;
        private float P_PsionicStorm_eff = 0.10f;
        private float DK_WaveOfFear_eff = 0.10f;
        private float DK_Spite_eff = 0.10f;
        private float DK_GraveBlade_eff = .08f;
        private float M_TigerStrike_eff = .1f;
        private float M_DragonStrike_eff = .1f;
        private float M_ThunderStrike_eff = .1f;
        private float C_CommanderAura_eff = .1f;
        private float C_TaskMasterAura_eff = .1f;
        private float C_ProvisionerAura_eff = .1f;
        private float C_StayAlert_eff = .1f;
        private float C_MoveOut_eff = .1f;
        private float C_HoldTheLine_eff = .1f;
        private float SS_PistolWhip_eff = .1f;
        private float SS_SuppressingFire_eff = .08f;
        private float SS_Mk203GL_eff = .08f;
        private float SS_Buckshot_eff = .08f;
        private float SS_BreachingCharge_eff = .08f;
        private float SS_CQC_eff = .1f;
        private float SS_FirstAid_eff = .1f;
        private float SS_60mmMortar_eff = .08f;

        private float global_seff = 0.03f;

        public bool skill_Sprint = false;
        public bool skill_GearRepair = false;
        public bool skill_InnerHealing = false;
        public bool skill_HeavyBlow = false;
        public bool skill_StrongBack = false;
        public bool skill_ThickSkin = false;
        public bool skill_FightersFocus = false;
        public bool skill_Teach = false;
        public bool skill_ThrowingKnife = false;
        public bool skill_BurningFury = false;
        public bool skill_PommelStrike = false;
        public bool skill_Legion = false;
        public bool skill_TempestStrike = false;
        public bool skill_PistolWhip = false;
        public bool skill_SuppressingFire = false;
        public bool skill_Mk203GL = false;
        public bool skill_Buckshot = false;
        public bool skill_BreachingCharge = false;

        public float maxSP = 1;
        public float spRegenRate = 1;
        public float coolDown = 1;
        public float spCost = 1;
        public float xpGain = 1;
        public float arcaneRes = 1;
        public float mightPwr = 1;
        private int resMitigationDelay = 0;
        private float totalApparelWeight = 0;
        public float arcalleumCooldown = 0f;

        public bool animalBondingDisabled = false;

        public bool usePsionicAugmentationToggle = true;
        public bool usePsionicMindAttackToggle = true;
        public bool useCleaveToggle = true;
        public bool useCQCToggle = true;
        public List<Thing> combatItems = new List<Thing>();
        public int allowMeditateTick = 0;
        public ThingOwner<ThingWithComps> equipmentContainer = new ThingOwner<ThingWithComps>();
        public int specWpnRegNum = -1;        

        public Verb_Deflected deflectVerb;
        DamageInfo reversal_dinfo;
        Thing reversalTarget = null;
        public Pawn bondedPet = null;

        public Verb_UseAbility lastVerbUsed = null;
        public int lastTickVerbUsed = 0;

        public TMAbilityDef mimicAbility = null;

        private MightData mightData = null;
        public MightData MightData
        {
            get
            {
                bool flag = this.mightData == null && this.IsMightUser;
                if (flag)
                {
                    this.mightData = new MightData(this);
                }
                return this.mightData;
            }
        }

        public List<TM_EventRecords> MightUsed
        {
            get
            {
                if (mightUsed == null)
                {
                    mightUsed = new List<TM_EventRecords>();
                    mightUsed.Clear();
                }
                return mightUsed;
            }
            set
            {
                if (mightUsed == null)
                {
                    mightUsed = new List<TM_EventRecords>();
                    mightUsed.Clear();
                }
                mightUsed = value;
            }
        }

        public bool shouldDraw = true;
        public override void PostDraw()
        {
            if (shouldDraw)
            {
                base.PostDraw();
                if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD, false) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_I, false) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_II, false) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_III, false) ||
                    this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CoOpPossessionHD, false) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CoOpPossessionHD_I, false) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CoOpPossessionHD_II, false) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CoOpPossessionHD_III, false))
                {
                    DrawDeceptionTicker(true);
                }
                else if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD, false) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_I, false) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_II, false) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_III, false))
                {
                    DrawDeceptionTicker(false);
                }

                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (settingsRef.AIFriendlyMarking && this.Pawn != null && this.Pawn.IsColonist && this.IsMightUser)
                {
                    DrawFighterMark();
                }
                if (settingsRef.AIMarking && base.Pawn != null && !base.Pawn.IsColonist && this.IsMightUser)
                {
                    DrawFighterMark();
                }

                if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BurningFuryHD, false))
                {
                    float num = Mathf.Lerp(1.2f, 1.55f, 1f);
                    Vector3 vector = this.Pawn.Drawer.DrawPos;
                    vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);

                    float angle = (float)Rand.Range(0, 360);
                    Vector3 s = new Vector3(1.7f, 1f, 1.7f);
                    Matrix4x4 matrix = default(Matrix4x4);
                    matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                    if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BurningFuryHD))
                    {
                        Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.burningFuryMat, 0);
                    }
                }

                if (shouldDrawPsionicShield)
                {
                    float radius = 2.5f;
                    radius = 2.5f + (.75f * TM_Calc.GetSkillVersatilityLevel(Pawn, TorannMagicDefOf.TM_PsionicBarrier, false));//TM_Calc.GetMightSkillLevel(this.Pawn, this.MightData.MightPowerSkill_PsionicBarrier, "TM_PsionicBarrier", "_ver", true));
                    float drawRadius = radius * .23f;
                    float num = Mathf.Lerp(drawRadius, 9.5f, drawRadius);
                    Vector3 vector = this.Pawn.CurJob.targetA.CenterVector3;
                    vector.y = Altitudes.AltitudeFor(AltitudeLayer.VisEffects);
                    Vector3 s = new Vector3(num, 9.5f, num);
                    Matrix4x4 matrix = default(Matrix4x4);
                    matrix.SetTRS(vector, Quaternion.AngleAxis(Rand.Range(0, 360), Vector3.up), s);
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_MatPool.PsionicBarrier, 0);
                }
            }
        }

        public void DrawFighterMark()
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
                CompAbilityUserMagic mComp = this.Pawn.TryGetComp<CompAbilityUserMagic>();
                bool shouldDraw = true;
                if(mComp != null)
                {
                    if(mComp.customClass != null)
                    {
                        shouldDraw = false;
                    }
                }
                if (shouldDraw)
                {
                    Material mat = TM_RenderQueue.fighterMarkMat;
                    if (this.customClass.classIconPath != "")
                    {
                        mat = MaterialPool.MatFrom("Other/" + this.customClass.classIconPath.ToString());
                    }
                    else if (this.customClass.classTexturePath != "")
                    {
                        mat = MaterialPool.MatFrom("Other/ClassTextures/" + this.customClass.classTexturePath, true);
                    }
                    if (this.customClass.classIconColor != null)
                    {
                        mat.color = this.customClass.classIconColor;
                    }
                    Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
                }
            }            
            else
            {
                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.gladiatorMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.sniperMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.bladedancerMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Ranger))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.rangerMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.facelessMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.psionicMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.deathknightMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.monkMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Commander))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.commanderMarkMat, 0);
                }
                else if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.supersoldierMarkMat, 0);
                }
                else if (TM_Calc.IsWayfarer(this.Pawn))
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.wayfarerMarkMat, 0);
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.fighterMarkMat, 0);
                }
            }
        }

        public void DrawDeceptionTicker(bool possessed)
        {
            if (possessed)
            {
                float num = Mathf.Lerp(1.2f, 1.55f, 1f);
                Vector3 vector = this.Pawn.Drawer.DrawPos;
                vector.x = vector.x - .25f;
                vector.z = vector.z + .8f;
                vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                float angle = 0f;
                Vector3 s = new Vector3(.45f, 1f, .4f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.possessionMask, 0);
                if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD, false) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_I, false) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_II, false) || this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_III, false))
                {
                    vector.z = vector.z + .35f;
                    matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                    Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.deceptionEye, 0);
                }
            }
            else
            {
                float num = Mathf.Lerp(1.2f, 1.55f, 1f);
                Vector3 vector = this.Pawn.Drawer.DrawPos;
                vector.x = vector.x - .25f;
                vector.z = vector.z + .8f;
                vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                float angle = 0f;
                Vector3 s = new Vector3(.45f, 1f, .4f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.deceptionEye, 0);
            }
        }

        public static List<TMAbilityDef> MightAbilities = null;    
        
        public int LevelUpSkill_global_refresh(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_global_refresh.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }        
        public int LevelUpSkill_global_seff(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_global_seff.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }        
        public int LevelUpSkill_global_strength(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_global_endurance(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_global_endurance.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_Sprint(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Sprint.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Fortitude(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Fortitude.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Grapple(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Grapple.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Cleave(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Cleave.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Whirlwind(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Whirlwind.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_SniperFocus(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_SniperFocus.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Headshot(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Headshot.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_DisablingShot(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_DisablingShot.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_AntiArmor(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_AntiArmor.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_BladeFocus(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_BladeFocus.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_BladeArt(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_BladeArt.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_SeismicSlash(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_SeismicSlash.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_BladeSpin(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_BladeSpin.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_PhaseStrike(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_PhaseStrike.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_RangerTraining(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_RangerTraining.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_BowTraining(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_BowTraining.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_PoisonTrap(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_PoisonTrap.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_AnimalFriend(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_AnimalFriend.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_ArrowStorm(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_ArrowStorm.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_Disguise(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Disguise.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Mimic(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Reversal(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Reversal.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Transpose(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Transpose.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Possess(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Possess.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_PsionicAugmentation(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_PsionicAugmentation.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_PsionicBarrier(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_PsionicBarrier.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_PsionicBlast(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_PsionicBlast.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_PsionicDash(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_PsionicDash.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_PsionicStorm(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_PsionicStorm.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }

        public int LevelUpSkill_Shroud(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Shroud.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_WaveOfFear(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_WaveOfFear.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Spite(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Spite.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_LifeSteal(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_LifeSteal.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_GraveBlade(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_GraveBlade.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Chi(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Chi.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_MindOverBody(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_MindOverBody.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Meditate(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Meditate.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_TigerStrike(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_TigerStrike.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_DragonStrike(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_DragonStrike.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_ThunderStrike(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_ThunderStrike.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_WayfarerCraft(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_WayfarerCraft.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_FieldTraining(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Provisioner(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_ProvisionerAura.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_TaskMaster(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_TaskMasterAura.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_Commander(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_CommanderAura.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_StayAlert(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_StayAlert.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_MoveOut(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_MoveOut.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_HoldTheLine(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_HoldTheLine.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_PistolSpec(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_PistolSpec.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_RifleSpec(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_RifleSpec.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_ShotgunSpec(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_ShotgunSpec.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_CQC(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_CQC.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_FirstAid(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_FirstAid.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }
        public int LevelUpSkill_60mmMortar(string skillName)
        {
            int result = 0;
            MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_60mmMortar.FirstOrDefault((MightPowerSkill x) => x.label == skillName);
            bool flag = mightPowerSkill != null;
            if (flag)
            {
                result = mightPowerSkill.level;
            }
            return result;
        }

        public override void CompTick()
        {
            bool flag = base.Pawn != null;
            if (flag)
            {
                bool spawned = base.Pawn.Spawned;
                if (spawned)
                {
                    bool isMightUser = this.IsMightUser && !this.Pawn.NonHumanlikeOrWildMan();
                    if (isMightUser)
                    {
                        bool flag3 = !this.MightData.Initialized;
                        if (flag3)
                        {
                            this.PostInitializeTick();
                        }
                        base.CompTick();
                        this.age++;
                        if (Find.TickManager.TicksGame % 20 == 0)
                        {
                            ResolveSustainedSkills();
                            if (reversalTarget != null)
                            {
                                ResolveReversalDamage();
                            }
                        }
                        if (Find.TickManager.TicksGame % 60 == 0)
                        {                            
                            ResolveClassSkills();
                            //ResolveClassPassions(); currently disabled
                        }
                        ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                        if (this.autocastTick < Find.TickManager.TicksGame)  //180 default
                        {
                            if ( !this.Pawn.Dead && !this.Pawn.Downed && this.Pawn.Map != null && this.Pawn.story != null && this.Pawn.story.traits != null && this.MightData != null && this.AbilityData != null && !this.Pawn.InMentalState)
                            {
                                if (this.Pawn.IsColonist)
                                {
                                    this.autocastTick = Find.TickManager.TicksGame + (int)Rand.Range(.8f * settingsRef.autocastEvaluationFrequency, 1.2f * settingsRef.autocastEvaluationFrequency);
                                    ResolveAutoCast();
                                }
                                else if(settingsRef.AICasting && (!this.Pawn.IsPrisoner || this.Pawn.IsFighting()))
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
                        if (this.Pawn.needs.AllNeeds.Contains(this.Stamina) && this.Stamina.CurLevel > (.99f * this.Stamina.MaxLevel))
                        {
                            if (this.age > (lastMightXPGain + mightXPRate))
                            {
                                MightData.MightUserXP++;
                                lastMightXPGain = this.age;
                            }
                        }
                        bool flag4 = Find.TickManager.TicksGame % 30 == 0;
                        if (flag4)
                        {
                            bool flag5 = this.MightUserXP > this.MightUserXPTillNextLevel;
                            if (flag5)
                            {
                                this.LevelUp(false);
                            }
                        }
                        if (Find.TickManager.TicksGame % 30 == 0)
                        {
                            bool flag6 = this.Pawn.TargetCurrentlyAimingAt != null;
                            if (flag6)
                            {
                                if (this.Pawn.TargetCurrentlyAimingAt.Thing is Pawn)
                                {
                                    Pawn targetPawn = this.Pawn.TargetCurrentlyAimingAt.Thing as Pawn;
                                    if (targetPawn.RaceProps.Humanlike)
                                    {
                                        bool flag7 = (this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_DisguiseHD")) || this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_DisguiseHD_I")) || this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_DisguiseHD_II")) || this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_DisguiseHD_III")));
                                        if (targetPawn.Faction != this.Pawn.Faction && flag7)
                                        {
                                            using (IEnumerator<Hediff> enumerator = this.Pawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
                                            {
                                                while (enumerator.MoveNext())
                                                {
                                                    Hediff rec = enumerator.Current;
                                                    if (rec.def == TorannMagicDefOf.TM_DisguiseHD || rec.def == TorannMagicDefOf.TM_DisguiseHD_I || rec.def == TorannMagicDefOf.TM_DisguiseHD_II || rec.def == TorannMagicDefOf.TM_DisguiseHD_III)
                                                    {
                                                        this.Pawn.health.RemoveHediff(rec);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
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
                        if(Find.TickManager.TicksGame % 301 == 0) //cache weapon damage for tooltip and damage calculations
                        {
                            this.weaponDamage = TM_Calc.GetSkillDamage(this.Pawn);
                        }
                        if (Find.TickManager.TicksGame % 602 == 0)
                        {
                            ResolveMightUseEvents();
                        }
                    }
                }
                else
                {
                    if (Find.TickManager.TicksGame % 600 == 0)
                    {
                        if (this.Pawn.Map == null)
                        {
                            if (this.IsMightUser)
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
                if (Find.TickManager.TicksGame % 7 == 0)
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
                    TM_Action.CreateMightDeathEffect(this.Pawn, this.Pawn.Position);
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
                        //this.MightData.Initialized = true;
                        this.Initialize();
                        this.ResolveMightTab();
                        this.ResolveMightPowers();
                        this.ResolveStamina();
                    }
                }
            }
        }

        public bool IsMightUser
        {
            get
            {                
                bool flag = base.Pawn != null;
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
                                if (!TM_ClassUtility.CustomClasses()[this.customIndex].isFighter)
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
                        bool flag4 = base.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic) ||
                            base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer) ||
                            base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Ranger) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) || TM_Calc.IsWayfarer(base.Pawn) || base.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Commander) ||
                            base.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier);
                        if (flag4)
                        {
                            return true;
                        }                        
                    }
                }
                return false;
            }
        }

        public int MightUserLevel
        {
            get
            {
                if (this.MightData != null)
                {
                    return this.MightData.MightUserLevel;
                }
                return 0;
            }
            set
            {
                bool flag = value > this.MightData.MightUserLevel;
                if (flag)
                {
                    this.MightData.MightAbilityPoints++;

                    bool flag2 = this.MightData.MightUserXP < GetXPForLevel(value-1);
                    if (flag2)
                    {
                        this.MightData.MightUserXP = GetXPForLevel(value-1);
                    }
                }
                this.MightData.MightUserLevel = value;
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

        public int MightUserXP
        {
            get
            {
                return this.MightData.MightUserXP;
            }
            set
            {
                this.MightData.MightUserXP = value;
            }
        }

        public float XPLastLevel
        {
            get
            {
                bool flag = this.MightUserLevel > 0;
                if (flag)
                {
                    return GetXPForLevel(this.MightUserLevel - 1);
                }
                return 0f;
            }
        }

        public float XPTillNextLevelPercent
        {
            get
            {
                return ((float)this.MightData.MightUserXP - this.XPLastLevel) / ((float)this.MightUserXPTillNextLevel - this.XPLastLevel);
            }
        }

        
        public int MightUserXPTillNextLevel
        {
            get
            {
                if (MightUserXP < XPLastLevel)
                {
                    MightUserXP = (int)XPLastLevel;
                }
                return GetXPForLevel(this.MightUserLevel);
            }
        }

        public void LevelUp(bool hideNotification = false)
        {
            if (!this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer))
            {
                if (this.MightUserLevel < (this.customClass?.maxFighterLevel ?? 200))
                {
                    this.MightUserLevel++;
                    bool flag = !hideNotification;
                    if (flag)
                    {
                        ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                        if (Pawn.IsColonist && settingsRef.showLevelUpMessage)
                        {
                            Messages.Message("TM_MightLevelUp".Translate(
                                this.parent.Label
                            ), Pawn, MessageTypeDefOf.PositiveEvent);
                        }
                    }                    
                }
            }
            else
            {
                this.MightUserXP = (int)this.XPLastLevel;
            }
        }

        public void LevelUpPower(MightPower power)
        {
            foreach (AbilityUser.AbilityDef current in power.TMabilityDefs)
            {
                base.RemovePawnAbility(current);
            }
            power.level++;
            base.AddPawnAbility(power.TMabilityDefs[power.level], true, -1f);
            this.UpdateAbilities();
        }

        public Need_Stamina Stamina
        {
            get
            {
                if (!base.Pawn.DestroyedOrNull() && base.Pawn.needs != null)
                {
                    return base.Pawn.needs.TryGetNeed<Need_Stamina>();
                }
                return null;
            }
        }

        public override void PostInitialize()
        {
            base.PostInitialize();
            bool flag = CompAbilityUserMight.MightAbilities == null;
            if (flag)
            {
                if (this.mightPowersInitialized == false && MightData != null)
                {
                    AssignAbilities();
                }
                //this.UpdateAbilities();
                //base.UpdateAbilities();
            }
        }

        public void AssignAbilities()
        {
            Pawn abilityUser = base.Pawn;
            bool flag2;
            MightData.MightUserLevel = 0;
            MightData.MightAbilityPoints = 0;
            if (this.customClass != null)
            {
                for (int z = 0; z < this.MightData.AllMightPowers.Count; z++)
                {
                    if (this.customClass.classFighterAbilities.Contains(this.MightData.AllMightPowers[z].abilityDef))
                    {
                        this.MightData.AllMightPowers[z].learned = true;
                    }
                    TMAbilityDef ability = (TMAbilityDef)this.MightData.AllMightPowers[z].abilityDef;
                    if (this.MightData.AllMightPowers[z].learned)
                    {
                        if (ability.shouldInitialize)
                        {
                            this.AddPawnAbility(ability);
                        }
                        if(ability.childAbilities != null && ability.childAbilities.Count > 0)
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
                //for (int j = 0; j < this.customClass.classFighterAbilities.Count; j++)
                //{
                    
                //}
                if (this.customClass.classHediff != null)
                {
                    HealthUtility.AdjustSeverity(abilityUser, this.customClass.classHediff, this.customClass.hediffSeverity);
                }
            }
            else
            {
                flag2 = TM_Calc.IsWayfarer(abilityUser);
                if (flag2)
                {
                    //Log.Message("Initializing Wayfarer Abilities");
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_WayfarerCraft).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_FieldTraining).learned = true;
                    if (!abilityUser.IsColonist)
                    {
                        this.skill_ThrowingKnife = true;
                        this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_ThrowingKnife).learned = true;
                        this.AddPawnAbility(TorannMagicDefOf.TM_ThrowingKnife);
                    }
                    for (int i = 0; i < 2; i++)
                    {
                        MightPower mp = this.MightData.MightPowersStandalone.RandomElement();
                        if (mp.abilityDef == TorannMagicDefOf.TM_GearRepair)
                        {
                            mp.learned = true;
                            skill_GearRepair = true;
                        }
                        else if (mp.abilityDef == TorannMagicDefOf.TM_InnerHealing)
                        {
                            mp.learned = true;
                            skill_InnerHealing = true;
                        }
                        else if (mp.abilityDef == TorannMagicDefOf.TM_HeavyBlow)
                        {
                            mp.learned = true;
                            skill_HeavyBlow = true;
                        }
                        else if (mp.abilityDef == TorannMagicDefOf.TM_ThickSkin)
                        {
                            mp.learned = true;
                            skill_ThickSkin = true;
                        }
                        else if (mp.abilityDef == TorannMagicDefOf.TM_FightersFocus)
                        {
                            mp.learned = true;
                            skill_FightersFocus = true;
                        }
                        else if (mp.abilityDef == TorannMagicDefOf.TM_StrongBack)
                        {
                            mp.learned = true;
                            skill_StrongBack = true;
                        }
                        else if (mp.abilityDef == TorannMagicDefOf.TM_ThrowingKnife)
                        {
                            mp.learned = true;
                            skill_ThrowingKnife = true;
                        }
                        else if (mp.abilityDef == TorannMagicDefOf.TM_PommelStrike)
                        {
                            mp.learned = true;
                            skill_PommelStrike = true;
                        }
                    }
                    InitializeSkill();
                }
                flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Gladiator);
                if (flag2)
                {
                    //Log.Message("Initializing Gladiator Abilities");
                    this.AddPawnAbility(TorannMagicDefOf.TM_Sprint);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Sprint).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Sprint_I).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Sprint_II).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Sprint_III).learned = true;
                    //this.AddPawnAbility(TorannMagicDefOf.TM_Fortitude);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Fortitude).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_Grapple);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Grapple).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Grapple_I).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Grapple_II).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Grapple_III).learned = true;
                    //this.AddPawnAbility(TorannMagicDefOf.TM_Cleave);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Cleave).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_Whirlwind);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Whirlwind).learned = true;
                }
                flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper);
                if (flag2)
                {
                    //Log.Message("Initializing Sniper Abilities");
                    //this.AddPawnAbility(TorannMagicDefOf.TM_SniperFocus);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_SniperFocus).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_Headshot);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Headshot).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_DisablingShot);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_DisablingShot).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_DisablingShot_I).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_DisablingShot_II).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_DisablingShot_III).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_AntiArmor);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_AntiArmor).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_ShadowSlayer);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_ShadowSlayer).learned = true;
                }
                flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Bladedancer);
                if (flag2)
                {
                    // Log.Message("Initializing Bladedancer Abilities");
                    // this.AddPawnAbility(TorannMagicDefOf.TM_BladeFocus);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_BladeFocus).learned = true;
                    //this.AddPawnAbility(TorannMagicDefOf.TM_BladeArt);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_BladeArt).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_SeismicSlash);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_SeismicSlash).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_BladeSpin);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_BladeSpin).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_PhaseStrike);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_PhaseStrike).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_PhaseStrike_I).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_PhaseStrike_II).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_PhaseStrike_III).learned = true;
                }
                flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Ranger);
                if (flag2)
                {
                    //Log.Message("Initializing Ranger Abilities");
                    //this.AddPawnAbility(TorannMagicDefOf.TM_RangerTraining);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_RangerTraining).learned = true;
                    // this.AddPawnAbility(TorannMagicDefOf.TM_BowTraining);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_BowTraining).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_PoisonTrap);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_PoisonTrap).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_AnimalFriend);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_AnimalFriend).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_ArrowStorm);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_ArrowStorm).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_ArrowStorm_I).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_ArrowStorm_II).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_ArrowStorm_III).learned = true;
                }
                flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Faceless);
                if (flag2)
                {
                    //Log.Message("Initializing Faceless Abilities");
                    this.AddPawnAbility(TorannMagicDefOf.TM_Disguise);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Disguise).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_Mimic);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Mimic).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_Reversal);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Reversal).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_Transpose);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Transpose).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Transpose_I).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Transpose_II).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Transpose_III).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_Possess);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Possess).learned = true;
                }
                flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic);
                if (flag2)
                {
                    //Log.Message("Initializing Psionic Abilities");
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_PsionicAugmentation).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_PsionicBarrier);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_PsionicBarrier).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_PsionicBarrier_Projected).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_PsionicBlast);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_PsionicBlast).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_PsionicBlast_I).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_PsionicBlast_II).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_PsionicBlast_III).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_PsionicDash);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_PsionicDash).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_PsionicStorm);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_PsionicStorm).learned = true;
                }
                flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.DeathKnight);
                if (flag2)
                {
                    //Log.Message("Initializing Death Knight Abilities");
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Shroud).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_WaveOfFear);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_WaveOfFear).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_Spite);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Spite).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Spite_I).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Spite_II).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Spite_III).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_LifeSteal).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_GraveBlade);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_GraveBlade).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_GraveBlade_I).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_GraveBlade_II).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_GraveBlade_III).learned = true;
                }
                flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Monk);
                if (flag2)
                {
                    //Log.Message("Initializing Monk Abilities");
                    //this.AddPawnAbility(TorannMagicDefOf.TM_Chi);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Chi).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_ChiBurst);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_MindOverBody).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_Meditate);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_Meditate).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_TigerStrike);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_TigerStrike).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_DragonStrike);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_DragonStrike).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_ThunderStrike);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_ThunderStrike).learned = true;
                }
                flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Commander);
                if (flag2)
                {
                    //Log.Message("Initializing Commander Abilities");
                    this.AddPawnAbility(TorannMagicDefOf.TM_ProvisionerAura);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_ProvisionerAura).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_TaskMasterAura);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_TaskMasterAura).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_CommanderAura);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_CommanderAura).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_StayAlert);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_StayAlert).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_StayAlert_I).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_StayAlert_II).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_StayAlert_III).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_MoveOut);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_MoveOut).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_MoveOut_I).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_MoveOut_II).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_MoveOut_III).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_HoldTheLine);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_HoldTheLine).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_HoldTheLine_I).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_HoldTheLine_II).learned = true;
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_HoldTheLine_III).learned = true;
                }
                flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier);
                if (flag2)
                {
                    //Log.Message("Initializing Super Soldier Abilities");
                    this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_PistolSpec).learned = false;
                    this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_RifleSpec).learned = false;
                    this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_ShotgunSpec).learned = false;
                    //this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_PistolWhip).learned = false;
                    //this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_SuppressingFire).learned = false;
                    //this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_Mk203GL).learned = false;
                    //this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_Buckshot).learned = false;
                    //this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_BreachingCharge).learned = false;

                    //this.AddPawnAbility(TorannMagicDefOf.TM_CQC);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_CQC).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_FirstAid);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_FirstAid).learned = true;
                    this.AddPawnAbility(TorannMagicDefOf.TM_60mmMortar);
                    this.MightData.ReturnMatchingMightPower(TorannMagicDefOf.TM_60mmMortar).learned = true;
                }
            }
            this.mightPowersInitialized = true;
            //base.UpdateAbilities();
        }

        public void InitializeSkill()  //used for class independant skills
        {
            Pawn abilityUser = base.Pawn;
            if (this.mimicAbility != null)
            {
                this.RemovePawnAbility(mimicAbility);
                this.AddPawnAbility(mimicAbility);
            }
            if (this.customClass != null)
            {
                //for (int j = 0; j < this.MightData.AllMightPowersWithSkills.Count; j++)
                //{
                //    if (this.MightData.AllMightPowersWithSkills[j].learned && !this.customClass.classFighterAbilities.Contains(this.MightData.AllMightPowersWithSkills[j].abilityDef))
                //    {
                //        this.MightData.AllMightPowersWithSkills[j].learned = false;
                //        this.RemovePawnAbility(this.MightData.AllMightPowersWithSkills[j].abilityDef);
                //    }
                //}
                for (int j = 0; j< this.MightData.AllMightPowers.Count; j++)
                {                    
                    if (this.MightData.AllMightPowers[j].learned && !this.customClass.classFighterAbilities.Contains(this.MightData.AllMightPowers[j].abilityDef))
                    {
                        this.RemovePawnAbility(this.MightData.AllMightPowers[j].abilityDef);
                        this.AddPawnAbility(this.MightData.AllMightPowers[j].abilityDef);
                    }
                }
            }
            else
            {
                if (this.skill_Sprint == true && !abilityUser.story.traits.HasTrait(TorannMagicDefOf.Gladiator))
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint);
                    this.AddPawnAbility(TorannMagicDefOf.TM_Sprint);
                }
                if (this.skill_GearRepair == true)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_GearRepair);
                    this.AddPawnAbility(TorannMagicDefOf.TM_GearRepair);
                }
                if (this.skill_InnerHealing == true)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_InnerHealing);
                    this.AddPawnAbility(TorannMagicDefOf.TM_InnerHealing);
                }
                if (this.skill_StrongBack == true)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_StrongBack);
                    this.AddPawnAbility(TorannMagicDefOf.TM_StrongBack);
                }
                if (this.skill_HeavyBlow == true)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_HeavyBlow);
                    this.AddPawnAbility(TorannMagicDefOf.TM_HeavyBlow);
                }
                if (this.skill_ThickSkin == true)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ThickSkin);
                    this.AddPawnAbility(TorannMagicDefOf.TM_ThickSkin);
                }
                if (this.skill_FightersFocus == true)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_FightersFocus);
                    this.AddPawnAbility(TorannMagicDefOf.TM_FightersFocus);
                }
                if (this.skill_Teach == true)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_TeachMight);
                    this.AddPawnAbility(TorannMagicDefOf.TM_TeachMight);
                }
                if (this.skill_ThrowingKnife == true)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ThrowingKnife);
                    this.AddPawnAbility(TorannMagicDefOf.TM_ThrowingKnife);
                }
                if (this.skill_BurningFury == true)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_BurningFury);
                    this.AddPawnAbility(TorannMagicDefOf.TM_BurningFury);
                }
                if (this.skill_PommelStrike == true)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_PommelStrike);
                    this.AddPawnAbility(TorannMagicDefOf.TM_PommelStrike);
                }
                if (this.skill_TempestStrike == true)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_TempestStrike);
                    this.AddPawnAbility(TorannMagicDefOf.TM_TempestStrike);
                }
                if (this.skill_Legion == true)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Legion);
                    this.AddPawnAbility(TorannMagicDefOf.TM_Legion);
                }
                if (this.skill_PistolWhip)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_PistolWhip);
                    this.AddPawnAbility(TorannMagicDefOf.TM_PistolWhip);
                }
                if (this.skill_SuppressingFire)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_SuppressingFire);
                    this.AddPawnAbility(TorannMagicDefOf.TM_SuppressingFire);
                }
                if (this.skill_Mk203GL)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Mk203GL);
                    this.AddPawnAbility(TorannMagicDefOf.TM_Mk203GL);
                }
                if (this.skill_Buckshot)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Buckshot);
                    this.AddPawnAbility(TorannMagicDefOf.TM_Buckshot);
                }
                if (this.skill_BreachingCharge)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_BreachingCharge);
                    this.AddPawnAbility(TorannMagicDefOf.TM_BreachingCharge);
                }
                if (this.IsMightUser && this.MightData.MightPowersCustomAll != null && this.MightData.MightPowersCustomAll.Count > 0)
                {
                    for (int j = 0; j < this.MightData.MightPowersCustomAll.Count; j++)
                    {
                        if (this.MightData.MightPowersCustomAll[j].learned)
                        {
                            this.RemovePawnAbility(this.MightData.MightPowersCustomAll[j].abilityDef);
                            this.AddPawnAbility(this.MightData.MightPowersCustomAll[j].abilityDef);
                        }
                    }
                }
            }
        }

        public void FixPowers()
        {
            Pawn abilityUser = base.Pawn;
            if (this.mightPowersInitialized == true)
            {
                bool flag2;
                flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Gladiator);
                if (flag2)
                {
                    Log.Message("Fixing Gladiator Abilities");
                    foreach (MightPower currentG in this.MightData.MightPowersG)
                    {
                        if (currentG.abilityDef.defName == "TM_Sprint" || currentG.abilityDef.defName == "TM_Sprint_I" || currentG.abilityDef.defName == "TM_Sprint_II" || currentG.abilityDef.defName == "TM_Sprint_III")
                        {
                            if (currentG.level == 0)
                            {
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint_I);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint_II);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint_III);
                            }
                            else if (currentG.level == 1)
                            {
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint_II);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint_III);
                            }
                            else if (currentG.level == 2)
                            {
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint_I);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint_III);
                            }
                            else if (currentG.level == 3)
                            {
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint_II);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint_I);
                            }
                            else
                            {
                                Log.Message("Ability level not found to fix");
                            }
                        }
                        if (currentG.abilityDef.defName == "TM_Grapple" || currentG.abilityDef.defName == "TM_Grapple_I" || currentG.abilityDef.defName == "TM_Grapple_II" || currentG.abilityDef.defName == "TM_Grapple_III")
                        {
                            if (currentG.level == 0)
                            {
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple_I);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple_II);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple_III);
                            }
                            else if (currentG.level == 1)
                            {
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple_II);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple_III);
                            }
                            else if (currentG.level == 2)
                            {
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple_I);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple_III);
                            }
                            else if (currentG.level == 3)
                            {
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple_II);
                                this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple_I);
                            }
                            else
                            {
                                Log.Message("Ability level not found to fix");
                            }
                        }
                    }            
                }
            }
            //this.UpdateAbilities();
            //base.UpdateAbilities();
        }

        public override bool TryTransformPawn()
        {
            return this.IsMightUser;
        }

        public bool TryAddPawnAbility(TMAbilityDef ability)
        {
            //add check to verify no ability is already added
            bool result = false;
            base.AddPawnAbility(ability, true, -1f);
            result = true;
            return result;
        }

        public void RemovePowers(bool clearStandalone = false)
        {
            Pawn abilityUser = base.Pawn;
            if (this.mightPowersInitialized == true && MightData != null)
            {
                bool flag2 = true;
                if (this.customClass != null)
                {
                    for (int i = 0; i < this.MightData.AllMightPowers.Count; i++)
                    {
                        MightPower mp = this.MightData.AllMightPowers[i];
                        for (int j = 0; j < mp.TMabilityDefs.Count; j++)
                        {
                            TMAbilityDef tmad = mp.TMabilityDefs[j] as TMAbilityDef;
                            if (tmad.childAbilities != null && tmad.childAbilities.Count > 0)
                            {
                                for (int k = 0; k < tmad.childAbilities.Count; k++)
                                {
                                    this.RemovePawnAbility(tmad.childAbilities[k]);
                                }
                            }
                            this.RemovePawnAbility(mp.TMabilityDefs[j]);
                        }
                        mp.learned = false;
                    }
                }
                if (clearStandalone)
                {
                    this.skill_BurningFury = false;
                    this.skill_FightersFocus = false;
                    this.skill_GearRepair = false;
                    this.skill_HeavyBlow = false;
                    this.skill_InnerHealing = false;
                    this.skill_Legion = false;
                    this.skill_PommelStrike = false;
                    this.skill_Sprint = false;
                    this.skill_StrongBack = false;
                    this.skill_Teach = false;
                    this.skill_TempestStrike = false;
                    this.skill_ThickSkin = false;
                    this.skill_ThrowingKnife = false;
                }
                
                foreach (MightPower current in this.MightData.MightPowersStandalone)
                {
                    this.RemovePawnAbility(current.abilityDef);
                }
                foreach(MightPower current in this.MightData.AllMightPowers)
                {
                    this.RemovePawnAbility(current.abilityDef);
                }
                if (TM_Calc.IsWayfarer(this.Pawn))
                {
                    this.skill_ThrowingKnife = false;
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ThrowingKnife);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Gladiator);
                if (flag2)
                {
                    foreach (MightPower current in this.MightData.MightPowersG)
                    {
                        current.learned = false;
                        this.RemovePawnAbility(current.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Sprint_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Grapple_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper);
                if (flag2)
                {
                    foreach (MightPower current in this.MightData.MightPowersS)
                    {
                        current.learned = false;
                        this.RemovePawnAbility(current.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_DisablingShot_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_DisablingShot_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_DisablingShot_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Bladedancer);
                if (flag2)
                {
                    foreach (MightPower current in this.MightData.MightPowersB)
                    {
                        current.learned = false;
                        this.RemovePawnAbility(current.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_PhaseStrike_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_PhaseStrike_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_PhaseStrike_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Ranger);
                if (flag2)
                {
                    foreach (MightPower current in this.MightData.MightPowersR)
                    {
                        current.learned = false;
                        this.RemovePawnAbility(current.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ArrowStorm_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ArrowStorm_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ArrowStorm_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Faceless);
                if (flag2)
                {
                    foreach (MightPower current in this.MightData.MightPowersF)
                    {
                        current.learned = false;
                        this.RemovePawnAbility(current.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Transpose_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Transpose_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Transpose_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic);
                if (flag2)
                {
                    foreach (MightPower current in this.MightData.MightPowersP)
                    {
                        current.learned = false;
                        this.RemovePawnAbility(current.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_PsionicBarrier_Projected);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_PsionicBlast_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_PsionicBlast_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_PsionicBlast_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.DeathKnight);
                if (flag2)
                {
                    foreach (MightPower current in this.MightData.MightPowersDK)
                    {
                        current.learned = false;
                        this.RemovePawnAbility(current.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Spite_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Spite_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Spite_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_GraveBlade_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_GraveBlade_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_GraveBlade_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Monk);
                if (flag2)
                {
                    foreach (MightPower current in this.MightData.MightPowersM)
                    {
                        current.learned = false;
                        this.RemovePawnAbility(current.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_ChiBurst);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Commander);
                if (flag2)
                {
                    foreach (MightPower current in this.MightData.MightPowersC)
                    {
                        current.learned = false;
                        this.RemovePawnAbility(current.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_StayAlert_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_StayAlert_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_StayAlert_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_MoveOut_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_MoveOut_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_MoveOut_III);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_HoldTheLine_I);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_HoldTheLine_II);
                    this.RemovePawnAbility(TorannMagicDefOf.TM_HoldTheLine_III);
                }
                //flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier);
                if (flag2)
                {
                    foreach (MightPower current in this.MightData.MightPowersSS)
                    {
                        current.learned = false;
                        this.RemovePawnAbility(current.abilityDef);
                    }
                    this.RemovePawnAbility(TorannMagicDefOf.TM_PistolWhip);
                    this.skill_PistolWhip = false;
                    this.RemovePawnAbility(TorannMagicDefOf.TM_SuppressingFire);
                    this.skill_SuppressingFire = false;
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Mk203GL);
                    this.skill_Mk203GL = false;
                    this.RemovePawnAbility(TorannMagicDefOf.TM_Buckshot);
                    this.skill_Buckshot = false;
                    this.RemovePawnAbility(TorannMagicDefOf.TM_BreachingCharge);
                    this.skill_BreachingCharge = false;
                }                
            }
        }

        public void ResetSkills()
        {
            this.MightData.MightPowerSkill_global_endurance.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_endurance_pwr").level = 0;
            this.MightData.MightPowerSkill_global_refresh.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_refresh_pwr").level = 0;
            this.MightData.MightPowerSkill_global_seff.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_seff_pwr").level = 0;
            this.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_strength_pwr").level = 0;
            for (int i = 0; i < this.MightData.AllMightPowersWithSkills.Count; i++)
            {
                this.MightData.AllMightPowersWithSkills[i].level = 0;
                this.MightData.AllMightPowersWithSkills[i].learned = false;
                this.MightData.AllMightPowersWithSkills[i].autocast = false;
                TMAbilityDef ability = (TMAbilityDef)this.MightData.AllMightPowersWithSkills[i].abilityDef;
                MightPowerSkill mps = this.MightData.GetSkill_Efficiency(ability);
                if (mps != null)
                {
                    mps.level = 0;
                }
                mps = this.MightData.GetSkill_Power(ability);
                if (mps != null)
                {
                    mps.level = 0;
                }
                mps = this.MightData.GetSkill_Versatility(ability);
                if (mps != null)
                {
                    mps.level = 0;
                }
            }
            for (int i = 0; i < this.MightData.AllMightPowers.Count; i++)
            {
                for (int j = 0; j < this.MightData.AllMightPowers[i].TMabilityDefs.Count; j++)
                {
                    TMAbilityDef ability = (TMAbilityDef)this.MightData.AllMightPowers[i].TMabilityDefs[j];
                    this.RemovePawnAbility(ability);
                }
                this.MightData.AllMightPowers[i].learned = false;
                this.MightData.AllMightPowers[i].autocast = false;
            }
            this.MightUserLevel = 0;
            this.MightUserXP = 0;
            this.MightData.MightAbilityPoints = 0;
            //this.MightPowersInitialized = false;
            //base.IsInitialized = false;
            //CompAbilityUserMight.MightAbilities = null;
            //this.MightData = null;
            this.AssignAbilities();
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
            this.mightData = null;
            base.Initialized = false;
        }

        private void ClearPower(MightPower current)
        {
            Log.Message("Removing ability: " + current.abilityDef.defName.ToString());
            base.RemovePawnAbility(current.abilityDef);
            base.UpdateAbilities();
        }

        public void RemoveTraits()
        {
            List<Trait> traits = this.Pawn.story.traits.allTraits;
            for (int i = 0; i < traits.Count; i++)
            {
                if (traits[i].def == TorannMagicDefOf.Gladiator || traits[i].def == TorannMagicDefOf.Bladedancer || traits[i].def == TorannMagicDefOf.Ranger || traits[i].def == TorannMagicDefOf.Faceless ||
                    traits[i].def == TorannMagicDefOf.DeathKnight || traits[i].def == TorannMagicDefOf.TM_Psionic || traits[i].def == TorannMagicDefOf.TM_Sniper || traits[i].def == TorannMagicDefOf.TM_Monk ||
                    traits[i].def == TorannMagicDefOf.TM_Wayfarer || traits[i].def == TorannMagicDefOf.TM_Commander || traits[i].def == TorannMagicDefOf.TM_SuperSoldier)
                {
                    Log.Message("Removing trait " + traits[i].Label);
                    traits.Remove(traits[i]);
                    i--;
                }
            }
        }

        private void LoadPowers(Pawn pawn)
        {
            if (pawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator))
            {
                foreach (MightPower currentG in this.MightData.MightPowersG)
                {
                    Log.Message("Removing ability: " + currentG.abilityDef.defName.ToString());
                    currentG.level = 0;
                    base.RemovePawnAbility(currentG.abilityDef);
                }
                
            }
        }

        public int MightAttributeEffeciencyLevel(string attributeName)
        {
            int result = 0;

            if (this.mightData != null && attributeName != null)
            {
                if (attributeName == "TM_Sprint_eff")
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Sprint.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = mightPowerSkill != null;
                    if (flag)
                    {
                        result = mightPowerSkill.level;
                    }
                }
                if (attributeName == "TM_Fortitude_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_Fortitude.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_Grapple_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_Grapple.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_Cleave_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_Cleave.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_Whirlwind_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_Whirlwind.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_Headshot_eff")
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Headshot.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = mightPowerSkill != null;
                    if (flag)
                    {
                        result = mightPowerSkill.level;
                    }
                }
                if (attributeName == "TM_DisablingShot_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_DisablingShot.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_AntiArmor_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_AntiArmor.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_SeismicSlash_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_SeismicSlash.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_BladeSpin_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_BladeSpin.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_PhaseStrike_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_PhaseStrike.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_AnimalFriend_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_AnimalFriend.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_ArrowStorm_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_ArrowStorm.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_Disguise_eff")
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Disguise.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = mightPowerSkill != null;
                    if (flag)
                    {
                        result = mightPowerSkill.level;
                    }
                }
                if (attributeName == "TM_Mimic_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_Reversal_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_Reversal.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_Transpose_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_Transpose.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_Possess_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_Possess.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_PsionicBarrier_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_PsionicBarrier.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_PsionicBlast_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_PsionicBlast.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_PsionicDash_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_PsionicDash.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_PsionicStorm_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_PsionicStorm.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_WaveOfFear_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_WaveOfFear.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_Spite_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_Spite.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_GraveBlade_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_GraveBlade.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_TigerStrike_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_TigerStrike.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_DragonStrike_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_DragonStrike.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_ThunderStrike_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_ThunderStrike.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_FieldTraining_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_WayfarerCraft_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_WayfarerCraft.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_StayAlert_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_StayAlert.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_MoveOut_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_MoveOut.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
                if (attributeName == "TM_HoldTheLine_eff")
                {
                    MightPowerSkill magicPowerSkill = this.MightData.MightPowerSkill_HoldTheLine.FirstOrDefault((MightPowerSkill x) => x.label == attributeName);
                    bool flag = magicPowerSkill != null;
                    if (flag)
                    {
                        result = magicPowerSkill.level;
                    }
                }
            }

            return result;
        }

        public float ActualChiCost(TMAbilityDef mightDef)
        {
            float num = mightDef.chiCost;
            num *= (1 - .06f * this.MightData.MightPowerSkill_Chi.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Chi_eff").level);          
            
            MightPowerSkill mps = this.MightData.GetSkill_Efficiency(mightDef);
            if (mps != null)
            {
                num *= (1 - (mightDef.efficiencyReductionPercent * mps.level));
            }
            return num;            
        }

        public float ActualStaminaCost(TMAbilityDef mightDef)
        {
            float adjustedStaminaCost = mightDef.staminaCost;
            if (mightDef.efficiencyReductionPercent != 0)
            {                
                if(mightDef == TorannMagicDefOf.TM_PistolWhip)
                {
                    adjustedStaminaCost *= 1f - (mightDef.efficiencyReductionPercent * this.MightData.GetSkill_Versatility(TorannMagicDefOf.TM_PistolSpec).level);
                }
                else if(mightDef == TorannMagicDefOf.TM_SuppressingFire)
                {
                    adjustedStaminaCost *= 1f - (mightDef.efficiencyReductionPercent * this.MightData.GetSkill_Efficiency(TorannMagicDefOf.TM_RifleSpec).level);
                }
                else if(mightDef == TorannMagicDefOf.TM_Mk203GL)
                {
                    adjustedStaminaCost *= 1f - (mightDef.efficiencyReductionPercent * this.MightData.GetSkill_Versatility(TorannMagicDefOf.TM_RifleSpec).level);
                }
                else if(mightDef == TorannMagicDefOf.TM_Buckshot)
                {
                    adjustedStaminaCost *= 1f - (mightDef.efficiencyReductionPercent * this.MightData.GetSkill_Efficiency(TorannMagicDefOf.TM_ShotgunSpec).level);
                }
                else if(mightDef == TorannMagicDefOf.TM_BreachingCharge)
                {
                    adjustedStaminaCost *= 1f - (mightDef.efficiencyReductionPercent * this.MightData.GetSkill_Versatility(TorannMagicDefOf.TM_ShotgunSpec).level);
                }
                else if(this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless) && (mightDef != TorannMagicDefOf.TM_Possess && mightDef != TorannMagicDefOf.TM_Disguise && mightDef != TorannMagicDefOf.TM_Transpose &&
                    mightDef != TorannMagicDefOf.TM_Transpose_I && mightDef != TorannMagicDefOf.TM_Transpose_II && mightDef != TorannMagicDefOf.TM_Transpose_III && mightDef != TorannMagicDefOf.TM_Mimic && mightDef != TorannMagicDefOf.TM_Reversal))
                {
                    adjustedStaminaCost *= 1f - (mightDef.efficiencyReductionPercent * this.mightData.GetSkill_Efficiency(TorannMagicDefOf.TM_Mimic).level);
                }
                else if(mightDef == TorannMagicDefOf.TM_AnimalFriend)
                {
                    return .5f * mightDef.staminaCost;
                }
                else if(mightDef == TorannMagicDefOf.TM_ProvisionerAura && this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ProvisionerAuraHD))
                {
                    return 0f;
                }
                else if (mightDef == TorannMagicDefOf.TM_TaskMasterAura && this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TaskMasterAuraHD))
                {
                    return 0f;
                }
                else if (mightDef == TorannMagicDefOf.TM_CommanderAura && this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CommanderAuraHD))
                {
                    return 0f;
                }
                else
                {
                    MightPowerSkill mps = this.MightData.GetSkill_Efficiency(mightDef);
                    if (mps != null)
                    {
                        adjustedStaminaCost *= 1f - (mightDef.efficiencyReductionPercent * mps.level);
                    }
                }
            }
            else
            {
                if (mightDef == TorannMagicDefOf.TM_Sprint || mightDef == TorannMagicDefOf.TM_Sprint_I || mightDef == TorannMagicDefOf.TM_Sprint_II || mightDef == TorannMagicDefOf.TM_Sprint_III)
                {
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.G_Sprint_eff * (float)this.MightAttributeEffeciencyLevel("TM_Sprint_eff"));
                }
                if (mightDef == TorannMagicDefOf.TM_Grapple || mightDef == TorannMagicDefOf.TM_Grapple_I || mightDef == TorannMagicDefOf.TM_Grapple_II || mightDef == TorannMagicDefOf.TM_Grapple_III)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Grapple.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Grapple_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.G_Grapple_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_Cleave)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Cleave.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Cleave_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.G_Cleave_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_Whirlwind)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Whirlwind.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Whirlwind_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.G_Whirlwind_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_Headshot)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Headshot.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Headshot_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.S_Headshot_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_DisablingShot || mightDef == TorannMagicDefOf.TM_DisablingShot_I || mightDef == TorannMagicDefOf.TM_DisablingShot_II || mightDef == TorannMagicDefOf.TM_DisablingShot_III)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_DisablingShot.FirstOrDefault((MightPowerSkill x) => x.label == "TM_DisablingShot_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.S_DisablingShot_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_AntiArmor)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_AntiArmor.FirstOrDefault((MightPowerSkill x) => x.label == "TM_AntiArmor_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.S_AntiArmor_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_SeismicSlash)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_SeismicSlash.FirstOrDefault((MightPowerSkill x) => x.label == "TM_SeismicSlash_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.B_SeismicSlash_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_BladeSpin)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_BladeSpin.FirstOrDefault((MightPowerSkill x) => x.label == "TM_BladeSpin_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.B_BladeSpin_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_PhaseStrike || mightDef == TorannMagicDefOf.TM_PhaseStrike_I || mightDef == TorannMagicDefOf.TM_PhaseStrike_II || mightDef == TorannMagicDefOf.TM_PhaseStrike_III)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_PhaseStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PhaseStrike_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.B_PhaseStrike_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_AnimalFriend)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_AnimalFriend.FirstOrDefault((MightPowerSkill x) => x.label == "TM_AnimalFriend_eff");
                    if (this.bondedPet != null)
                    {
                        adjustedStaminaCost = (mightDef.staminaCost - (mightDef.staminaCost * (this.R_AnimalFriend_eff * (float)mightPowerSkill.level)) / 2);
                    }
                    else
                    {
                        adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.R_AnimalFriend_eff * (float)mightPowerSkill.level);
                    }
                }
                if (mightDef == TorannMagicDefOf.TM_ArrowStorm || mightDef == TorannMagicDefOf.TM_ArrowStorm_I || mightDef == TorannMagicDefOf.TM_ArrowStorm_II || mightDef == TorannMagicDefOf.TM_ArrowStorm_III)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_ArrowStorm.FirstOrDefault((MightPowerSkill x) => x.label == "TM_ArrowStorm_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.R_ArrowStorm_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_Disguise)
                {
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.F_Disguise_eff * (float)this.MightAttributeEffeciencyLevel("TM_Disguise_eff"));
                }
                if (mightDef == TorannMagicDefOf.TM_Transpose || mightDef == TorannMagicDefOf.TM_Transpose_I || mightDef == TorannMagicDefOf.TM_Transpose_II || mightDef == TorannMagicDefOf.TM_Transpose_III)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Transpose.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Transpose_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.F_Transpose_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_Mimic)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.F_Mimic_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_Reversal)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Reversal.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Reversal_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.F_Reversal_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_Possess)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Possess.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Possess_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.F_Possess_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_PsionicBarrier || mightDef == TorannMagicDefOf.TM_PsionicBarrier_Projected)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_PsionicBarrier.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicBarrier_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.P_PsionicBarrier_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_PsionicBlast || mightDef == TorannMagicDefOf.TM_PsionicBlast_I || mightDef == TorannMagicDefOf.TM_PsionicBlast_II || mightDef == TorannMagicDefOf.TM_PsionicBlast_III)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_PsionicBlast.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicBlast_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.P_PsionicBlast_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_PsionicDash)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_PsionicDash.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicDash_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.P_PsionicDash_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_PsionicStorm)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_PsionicStorm.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicStorm_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.P_PsionicStorm_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_Spite || mightDef == TorannMagicDefOf.TM_Spite_I || mightDef == TorannMagicDefOf.TM_Spite_II || mightDef == TorannMagicDefOf.TM_Spite_III)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_Spite.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Spite_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.DK_Spite_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_WaveOfFear)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_WaveOfFear.FirstOrDefault((MightPowerSkill x) => x.label == "TM_WaveOfFear_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.DK_WaveOfFear_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_GraveBlade || mightDef == TorannMagicDefOf.TM_GraveBlade_I || mightDef == TorannMagicDefOf.TM_GraveBlade_II || mightDef == TorannMagicDefOf.TM_GraveBlade_III)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_GraveBlade.FirstOrDefault((MightPowerSkill x) => x.label == "TM_GraveBlade_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.DK_GraveBlade_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_TigerStrike)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_TigerStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_TigerStrike_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.M_TigerStrike_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_DragonStrike)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_DragonStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_DragonStrike_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.M_DragonStrike_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_ThunderStrike)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_ThunderStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_ThunderStrike_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.M_ThunderStrike_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_ProvisionerAura)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_ProvisionerAura.FirstOrDefault((MightPowerSkill x) => x.label == "TM_ProvisionerAura_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.C_ProvisionerAura_eff * (float)mightPowerSkill.level);
                    if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ProvisionerAuraHD))
                    {
                        return 0f;
                    }
                }
                if (mightDef == TorannMagicDefOf.TM_TaskMasterAura)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_TaskMasterAura.FirstOrDefault((MightPowerSkill x) => x.label == "TM_TaskMasterAura_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.C_TaskMasterAura_eff * (float)mightPowerSkill.level);
                    if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_TaskMasterAuraHD))
                    {
                        return 0f;
                    }
                }
                if (mightDef == TorannMagicDefOf.TM_CommanderAura)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_CommanderAura.FirstOrDefault((MightPowerSkill x) => x.label == "TM_CommanderAura_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.C_CommanderAura_eff * (float)mightPowerSkill.level);
                    if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_CommanderAuraHD))
                    {
                        return 0f;
                    }
                }
                if (mightDef == TorannMagicDefOf.TM_StayAlert || mightDef == TorannMagicDefOf.TM_StayAlert_I || mightDef == TorannMagicDefOf.TM_StayAlert_II || mightDef == TorannMagicDefOf.TM_StayAlert_III)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_StayAlert.FirstOrDefault((MightPowerSkill x) => x.label == "TM_StayAlert_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.C_StayAlert_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_MoveOut || mightDef == TorannMagicDefOf.TM_MoveOut_I || mightDef == TorannMagicDefOf.TM_MoveOut_II || mightDef == TorannMagicDefOf.TM_MoveOut_III)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_MoveOut.FirstOrDefault((MightPowerSkill x) => x.label == "TM_MoveOut_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.C_MoveOut_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_HoldTheLine || mightDef == TorannMagicDefOf.TM_HoldTheLine_I || mightDef == TorannMagicDefOf.TM_HoldTheLine_II || mightDef == TorannMagicDefOf.TM_HoldTheLine_III)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_HoldTheLine.FirstOrDefault((MightPowerSkill x) => x.label == "TM_HoldTheLine_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.C_HoldTheLine_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_PistolWhip)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_PistolSpec.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PistolSpec_ver");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.SS_PistolWhip_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_SuppressingFire)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_RifleSpec.FirstOrDefault((MightPowerSkill x) => x.label == "TM_RifleSpec_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.SS_SuppressingFire_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_Mk203GL)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_RifleSpec.FirstOrDefault((MightPowerSkill x) => x.label == "TM_RifleSpec_ver");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.SS_Mk203GL_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_Buckshot)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_ShotgunSpec.FirstOrDefault((MightPowerSkill x) => x.label == "TM_ShotgunSpec_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.SS_Buckshot_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_BreachingCharge)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_ShotgunSpec.FirstOrDefault((MightPowerSkill x) => x.label == "TM_ShotgunSpec_ver");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.SS_BreachingCharge_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_CQC)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_CQC.FirstOrDefault((MightPowerSkill x) => x.label == "TM_CQC_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.SS_CQC_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_FirstAid)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_FirstAid.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FirstAid_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.SS_FirstAid_eff * (float)mightPowerSkill.level);
                }
                if (mightDef == TorannMagicDefOf.TM_60mmMortar)
                {
                    MightPowerSkill mightPowerSkill = this.MightData.MightPowerSkill_60mmMortar.FirstOrDefault((MightPowerSkill x) => x.label == "TM_60mmMortar_eff");
                    adjustedStaminaCost = mightDef.staminaCost - mightDef.staminaCost * (this.SS_60mmMortar_eff * (float)mightPowerSkill.level);
                }
            }
            if (this.spCost != 1f && (mightDef != TorannMagicDefOf.TM_ProvisionerAura && mightDef != TorannMagicDefOf.TM_CommanderAura && mightDef != TorannMagicDefOf.TM_TaskMasterAura))
            {
                adjustedStaminaCost = adjustedStaminaCost * this.spCost;
            }            

            MightPowerSkill globalSkill = this.MightData.MightPowerSkill_global_seff.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_seff_pwr");
            if (globalSkill != null && (mightDef != TorannMagicDefOf.TM_ProvisionerAura && mightDef != TorannMagicDefOf.TM_CommanderAura && mightDef != TorannMagicDefOf.TM_TaskMasterAura))
            {
                adjustedStaminaCost -= (adjustedStaminaCost * (global_seff * globalSkill.level));
            }

            return Mathf.Max(adjustedStaminaCost, (.5f * mightDef.staminaCost));
            

        }

        public override List<HediffDef> IgnoredHediffs()
        {
            return new List<HediffDef>
            {
                TorannMagicDefOf.TM_MagicUserHD
            };
        }

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            Pawn abilityUser = base.Pawn;
            absorbed = false;
            //bool flag = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Gladiator) || abilityUser.story.traits.HasTrait;
            //if (isGladiator)
            //{
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
                    if(current.def ==  TorannMagicDefOf.TM_PsionicHD)
                    {
                        if(dinfo.Def == TMDamageDefOf.DamageDefOf.TM_PsionicInjury)
                        {
                            absorbed = true;
                            dinfo.SetAmount(0);
                            return;
                        }
                    }
                    if (current.def == TorannMagicDefOf.TM_ReversalHD)
                    {
                        Pawn instigator = dinfo.Instigator as Pawn;
                        if (instigator != null)
                        {
                            if (instigator.equipment != null && instigator.equipment.PrimaryEq != null)
                            {
                                if (instigator.equipment.PrimaryEq.PrimaryVerb != null)
                                {
                                    absorbed = true;
                                    Vector3 drawPos = Pawn.DrawPos;
                                    drawPos.x += ((instigator.DrawPos.x - drawPos.x) / 20f) + Rand.Range(-.2f, .2f);
                                    drawPos.z += ((instigator.DrawPos.z - drawPos.z) / 20f) + Rand.Range(-.2f, .2f);
                                    TM_MoteMaker.ThrowSparkFlashMote(drawPos, this.Pawn.Map, 2f);                                    
                                    DoReversal(dinfo);
                                    dinfo.SetAmount(0);
                                    MightPowerSkill ver = this.MightData.MightPowerSkill_Reversal.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Reversal_ver");
                                    if(ver.level > 0)
                                    {
                                        SiphonReversal(ver.level);
                                    }
                                    return;
                                }
                            }
                            else if(instigator.RaceProps.Animal && dinfo.Amount != 0 && (instigator.Position - this.Pawn.Position).LengthHorizontal <= 2)
                            {
                                absorbed = true;
                                Vector3 drawPos = Pawn.DrawPos;
                                drawPos.x += ((instigator.DrawPos.x - drawPos.x) / 20f) + Rand.Range(-.2f, .2f);
                                drawPos.z += ((instigator.DrawPos.z - drawPos.z) / 20f) + Rand.Range(-.2f, .2f);
                                TM_MoteMaker.ThrowSparkFlashMote(drawPos, this.Pawn.Map, 2f);
                                DoMeleeReversal(dinfo);
                                dinfo.SetAmount(0);
                                MightPowerSkill ver = this.MightData.MightPowerSkill_Reversal.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Reversal_ver");
                                if (ver.level > 0)
                                {
                                    SiphonReversal(ver.level);
                                }
                                return;
                            }
                        }
                        Building instigatorBldg = dinfo.Instigator as Building;
                        if(instigatorBldg != null)
                        {
                            if(instigatorBldg.def.Verbs != null)
                            {
                                absorbed = true;
                                Vector3 drawPos = Pawn.DrawPos;
                                drawPos.x += ((instigatorBldg.DrawPos.x - drawPos.x) / 20f) + Rand.Range(-.2f, .2f);
                                drawPos.z += ((instigatorBldg.DrawPos.z - drawPos.z) / 20f) + Rand.Range(-.2f, .2f);
                                TM_MoteMaker.ThrowSparkFlashMote(drawPos, this.Pawn.Map, 2f);
                                DoReversal(dinfo);
                                dinfo.SetAmount(0);
                                MightPowerSkill ver = this.MightData.MightPowerSkill_Reversal.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Reversal_ver");
                                if (ver.level > 0)
                                {
                                    SiphonReversal(ver.level);
                                }
                                return;
                            }
                        }
                    }
                    if (current.def == TorannMagicDefOf.TM_HediffEnchantment_phantomShift && Rand.Chance(.2f))
                    {
                        absorbed = true;
                        FleckMaker.Static(Pawn.Position, Pawn.Map, FleckDefOf.ExplosionFlash, 8);
                        FleckMaker.ThrowSmoke(abilityUser.Position.ToVector3Shifted(), abilityUser.Map, 1.2f);
                        dinfo.SetAmount(0);
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
                    if (fortitudeMitigationDelay < this.age )
                    {
                        if (current.def == TorannMagicDefOf.TM_HediffFortitude)
                        {
                            MightPowerSkill pwr = this.MightData.MightPowerSkill_Fortitude.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Fortitude_pwr");
                            MightPowerSkill ver = this.MightData.MightPowerSkill_Fortitude.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Fortitude_ver");
                            absorbed = true;
                            int mitigationAmt = 5 + pwr.level;
                            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                            if (settingsRef.AIHardMode && !abilityUser.IsColonist)
                            {
                                mitigationAmt = 8;
                            }
                            float actualDmg;
                            float dmgAmt = dinfo.Amount;
                            this.Stamina.GainNeed((.01f * dmgAmt) + (.005f * (float)ver.level));
                            if (dmgAmt < mitigationAmt)
                            {
                                actualDmg = 0;
                                return;
                            }
                            else
                            {
                                actualDmg = dmgAmt - mitigationAmt;
                            }
                            fortitudeMitigationDelay = this.age + 5;
                            dinfo.SetAmount(actualDmg);
                            abilityUser.TakeDamage(dinfo);
                            return;
                        }
                        if (current.def == TorannMagicDefOf.TM_MindOverBodyHD)
                        {
                            MightPowerSkill ver = this.MightData.MightPowerSkill_MindOverBody.FirstOrDefault((MightPowerSkill x) => x.label == "TM_MindOverBody_ver");
                            absorbed = true;
                            int mitigationAmt = Mathf.Clamp((7 + (2 * ver.level) - Mathf.RoundToInt(totalApparelWeight/2)), 0, 13);
                            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                            if (settingsRef.AIHardMode && !abilityUser.IsColonist)
                            {
                                mitigationAmt = 10;
                            }
                            float actualDmg;
                            float dmgAmt = dinfo.Amount;
                            if (dmgAmt < mitigationAmt)
                            {
                                Vector3 drawPos = this.Pawn.DrawPos;
                                Thing instigator = dinfo.Instigator;
                                if (instigator != null && instigator.DrawPos != null)
                                {
                                    float drawAngle = (instigator.DrawPos - drawPos).AngleFlat();
                                    drawPos.x += Mathf.Clamp(((instigator.DrawPos.x - drawPos.x) / 5f) + Rand.Range(-.1f, .1f), -.45f, .45f);
                                    drawPos.z += Mathf.Clamp(((instigator.DrawPos.z - drawPos.z) / 5f) + Rand.Range(-.1f, .1f), -.45f, .45f);
                                    TM_MoteMaker.ThrowSparkFlashMote(drawPos, this.Pawn.Map, 1f);
                                }
                                actualDmg = 0;
                                return;
                            }
                            else
                            {
                                actualDmg = dmgAmt - mitigationAmt;
                            }
                            fortitudeMitigationDelay = this.age + 6;
                            dinfo.SetAmount(actualDmg);
                            abilityUser.TakeDamage(dinfo);
                            return;
                        }
                    }
                }
            }
            list.Clear();
            list = null;
            base.PostPreApplyDamage(dinfo, out absorbed);
        }

        public void DoMeleeReversal(DamageInfo dinfo)
        {
            TM_Action.DoMeleeReversal(dinfo, this.Pawn);          
        }

        public void DoReversal(DamageInfo dinfo)
        {
            TM_Action.DoReversal(dinfo, this.Pawn);       
        }

        public void SiphonReversal(int verVal)
        {
            Pawn pawn = this.Pawn;
            CompAbilityUserMight comp = pawn.GetComp<CompAbilityUserMight>();
            comp.Stamina.CurLevel += (.015f * verVal);         
            int num = 1 + verVal;
            using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    BodyPartRecord rec = enumerator.Current;
                    bool flag2 = num > 0;
                    if (flag2)
                    {
                        int num2 = 1 + verVal;
                        ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                        if (!pawn.IsColonist && settingsRef.AIHardMode)
                        {
                            num2 = 5;
                        }
                        IEnumerable<Hediff_Injury> arg_BB_0 = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                        Func<Hediff_Injury, bool> arg_BB_1;
                        arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                        foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                        {
                            bool flag4 = num2 > 0;
                            if (flag4)
                            {
                                bool flag5 = current.CanHealNaturally() && !current.IsPermanent();
                                if (flag5)
                                {
                                    if (!pawn.IsColonist)
                                    {
                                        current.Heal(20.0f + (float)verVal * 3f); // power affects how much to heal
                                    }
                                    else
                                    {
                                        current.Heal((2.0f + (float)verVal * 1f)); // power affects how much to heal
                                    }
                                    TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .6f);
                                    TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .4f);
                                    num--;
                                    num2--;
                                }
                            }
                        }
                    }
                }
            }
            
        }

        public void GiveReversalJob(DamageInfo dinfo)  // buggy AF due to complications with CompDeflector
        {
            try
            {
                Pawn pawn;
                bool flag = (pawn = (dinfo.Instigator as Pawn)) != null && dinfo.Weapon != null;
                if (flag)
                {
                    if (dinfo.Weapon.IsMeleeWeapon || dinfo.WeaponBodyPartGroup != null)
                    {                        
                        reversal_dinfo = new DamageInfo(dinfo.Def, dinfo.Amount, dinfo.ArmorPenetrationInt, dinfo.Angle - 180, this.Pawn, dinfo.HitPart, dinfo.Weapon, DamageInfo.SourceCategory.ThingOrUnknown);
                        reversalTarget = dinfo.Instigator;
                    }
                    else
                    {
                        Job job = new Job(CompDeflectorDefOf.CastDeflectVerb)
                        {
                            playerForced = true,
                            locomotionUrgency = LocomotionUrgency.Sprint
                        };
                        bool flag2 = pawn.equipment != null;
                        if (flag2)
                        {
                            CompEquippable primaryEq = pawn.equipment.PrimaryEq;
                            bool flag3 = primaryEq != null;
                            if (flag3)
                            {
                                bool flag4 = primaryEq.PrimaryVerb != null;
                                if (flag4)
                                {
                                    Verb_Deflected verb_Deflected = (Verb_Deflected)this.CopyAndReturnNewVerb(primaryEq.PrimaryVerb);
                                    //verb_Deflected = this.ReflectionHandler(verb_Deflected);
                                    //Log.Message("verb deflected with properties is " + verb_Deflected.ToString()); //throwing an error, so nothing is happening in jobdriver_castdeflectverb
                                    pawn = dinfo.Instigator as Pawn;
                                    job.targetA = pawn;
                                    job.verbToUse = verb_Deflected;
                                    job.killIncappedTarget = pawn.Downed;
                                    this.Pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                }
                            }
                        }
                    }
                }
            }
            catch (NullReferenceException)
            {
            }
        }

        public Verb CopyAndReturnNewVerb(Verb newVerb = null)
        {
            if (newVerb != null)
            {
                deflectVerb = newVerb as Verb_Deflected;
                deflectVerb = (Verb_Deflected)Activator.CreateInstance(typeof(Verb_Deflected));
                deflectVerb.caster = this.Pawn;
                

                //Initialize VerbProperties
                var newVerbProps = new VerbProperties
                {
                    //Copy values over to a new verb props
                    
                    hasStandardCommand = newVerb.verbProps.hasStandardCommand,
                    defaultProjectile = newVerb.verbProps.defaultProjectile,
                    range = newVerb.verbProps.range,
                    muzzleFlashScale = newVerb.verbProps.muzzleFlashScale,                    
                    warmupTime = 0,
                    defaultCooldownTime = 0,
                    soundCast = SoundDefOf.MetalHitImportant,
                    impactMote = newVerb.verbProps.impactMote,
                    label = newVerb.verbProps.label,
                    ticksBetweenBurstShots = 0,
                    rangedFireRulepack = RulePackDef.Named("TM_Combat_Reflection"),
                    accuracyLong = 70f * Rand.Range(1f, 2f),
                    accuracyMedium = 80f * Rand.Range(1f, 2f),
                    accuracyShort = 90f * Rand.Range(1f, 2f)
                };

                //Apply values
                deflectVerb.verbProps = newVerbProps;
            }
            else
            {
                if (deflectVerb != null) return deflectVerb;
                deflectVerb = (Verb_Deflected)Activator.CreateInstance(typeof(Verb_Deflected));
                deflectVerb.caster = this.Pawn;
                deflectVerb.verbProps = newVerb.verbProps;
            }
            return deflectVerb;
        }

        public Verb_Deflected ReflectionHandler(Verb_Deflected newVerb)
        {
            VerbProperties verbProperties = new VerbProperties
            {
                hasStandardCommand = newVerb.verbProps.hasStandardCommand,
                defaultProjectile = newVerb.verbProps.defaultProjectile,
                range = newVerb.verbProps.range,
                muzzleFlashScale = newVerb.verbProps.muzzleFlashScale,
                warmupTime = 0f,
                defaultCooldownTime = 0f,
                soundCast = SoundDefOf.MetalHitImportant,
                accuracyLong = 70f * Rand.Range(1f, 2f),
                accuracyMedium = 80f * Rand.Range(1f, 2f),
                accuracyShort = 90f * Rand.Range(1f, 2f)
            };

            newVerb.verbProps = verbProperties;
            return newVerb;
        }

        public void ResolveReversalDamage()
        {
            reversalTarget.TakeDamage(reversal_dinfo);
            reversalTarget = null;
        }

        public void ResolveMightUseEvents()
        {
            List<TM_EventRecords> tmpList = new List<TM_EventRecords>();
            tmpList.Clear();
            foreach (TM_EventRecords ev in MightUsed)
            {
                if (Find.TickManager.TicksGame - 60000 > ev.eventTick)
                {
                    tmpList.Add(ev);
                }
            }
            foreach (TM_EventRecords rem_ev in tmpList)
            {
                MightUsed.Remove(rem_ev);
            }
        }

        public void ResolveAutoCast()
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (settingsRef.autocastEnabled && this.Pawn.jobs != null && this.Pawn.CurJob != null && this.Pawn.CurJob.def != TorannMagicDefOf.TMCastAbilityVerb && this.Pawn.CurJob.def != TorannMagicDefOf.TMCastAbilitySelf && 
                this.Pawn.CurJob.def != JobDefOf.Ingest && this.Pawn.CurJob.def != JobDefOf.ManTurret && this.Pawn.GetPosture() == PawnPosture.Standing && !this.Pawn.CurJob.playerForced)
            {
                //Log.Message("pawn " + this.Pawn.LabelShort + " current job is " + this.Pawn.CurJob.def.defName);
                //non-combat (undrafted) spells    
                bool castSuccess = false;
                bool isFaceless = (this.mimicAbility != null);
                bool isCustom = this.customIndex >= 0;
                if (this.Pawn.drafter != null && !this.Pawn.Drafted && this.Stamina != null && this.Stamina.CurLevelPercentage >= settingsRef.autocastMinThreshold)
                {
                    foreach (MightPower mp in this.MightData.MightPowersCustomAll)
                    {
                        //Log.Message("checking custom power " + mp.abilityDef.defName);
                        if (mp.learned && mp.autocast && mp.autocasting != null && mp.autocasting.mightUser && mp.autocasting.undrafted)
                        {
                            TMAbilityDef tmad = mp.TMabilityDefs[mp.level] as TMAbilityDef; // issues with index?
                            //Log.Message("checking autocast for ability " + tmad.defName);
                            bool canUseWithEquippedWeapon = true;
                            bool canUseIfViolentAbility = this.Pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent) ? !tmad.MainVerb.isViolent : true;
                            if (!TM_Calc.HasResourcesForAbility(this.Pawn, tmad))
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
                                            if (targetPawn.Downed)
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
                                        AutoCast.CombatAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if(mp.autocasting.type == TMDefs.AutocastType.OnSelf)
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
                                        AutoCast.CombatAbility_OnSelf.Evaluate(this, tmad, ability, mp, out castSuccess);
                                    }
                                }
                                if(mp.autocasting.type == TMDefs.AutocastType.OnCell && this.Pawn.CurJob.targetA != null)
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
                                        AutoCast.CombatAbility_OnCell.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if(mp.autocasting.type == TMDefs.AutocastType.OnNearby)
                                {
                                    //Log.Message("nearby autocast for " + tmad.defName);
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.CurJob.targetA);
                                    if(localTarget != null && localTarget.IsValid)
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
                                            if (targetPawn.Downed)
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
                                        AutoCast.CombatAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if (castSuccess) goto AutoCastExit;
                            }
                        }
                    }
                    //Hunting only
                    if (this.Pawn.CurJob.def == JobDefOf.Hunt && this.Pawn.CurJob.targetA != null && this.Pawn.CurJob.targetA.Thing != null && this.Pawn.CurJob.targetA.Thing is Pawn)
                    {                        
                        if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Ranger) || isFaceless || isCustom) && !this.Pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent))
                        {
                            PawnAbility ability = null;
                            foreach (MightPower current in this.MightData.MightPowersR)
                            {
                                if (current.abilityDef != null && ((this.mimicAbility != null && this.mimicAbility.defName.Contains(current.abilityDef.defName)) || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Ranger)))
                                {
                                    foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                    {
                                        if ((tmad == TorannMagicDefOf.TM_ArrowStorm || tmad == TorannMagicDefOf.TM_ArrowStorm_I || tmad == TorannMagicDefOf.TM_ArrowStorm_II || tmad == TorannMagicDefOf.TM_ArrowStorm_III))
                                        {
                                            if (this.Pawn.equipment.Primary != null && this.Pawn.equipment.Primary.def.IsRangedWeapon)
                                            {
                                                Thing wpn = this.Pawn.equipment.Primary;

                                                if (TM_Calc.IsUsingBow(this.Pawn))
                                                {
                                                    MightPower mightPower = this.MightData.MightPowersR.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == tmad);
                                                    if (mightPower != null && mightPower.learned && mightPower.autocast)
                                                    {
                                                        ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                                        AutoCast.CombatAbility_OnTarget_LoS.TryExecute(this, tmad, ability, mightPower, this.Pawn.CurJob.targetA, 4, out castSuccess);
                                                        if (castSuccess) goto AutoCastExit;
                                                    }                                                    
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) || isFaceless || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                        {
                            PawnAbility ability = null;

                            foreach (MightPower current in this.MightData.MightPowersSS)
                            {
                                if (current.abilityDef != null)
                                {
                                    if (this.Pawn.equipment.Primary != null && this.Pawn.equipment.Primary.def.IsRangedWeapon)
                                    {
                                        if (this.specWpnRegNum != -1)
                                        {
                                            if (current.abilityDef == TorannMagicDefOf.TM_PistolWhip)
                                            {
                                                MightPower mightPower = this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_PistolWhip);
                                                if (mightPower != null && mightPower.autocast)
                                                {
                                                    ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_PistolWhip);
                                                    Pawn targetPawn = this.Pawn.TargetCurrentlyAimingAt.Thing as Pawn;
                                                    if (targetPawn != null && this.Pawn.TargetCurrentlyAimingAt != this.Pawn)
                                                    {
                                                        AutoCast.MeleeCombat_OnTarget.TryExecute(this, TorannMagicDefOf.TM_PistolWhip, ability, mightPower, targetPawn, out castSuccess);
                                                        if (castSuccess) goto AutoCastExit;
                                                    }
                                                }
                                            }
                                            if (current.abilityDef == TorannMagicDefOf.TM_SuppressingFire)
                                            {
                                                MightPower mightPower = this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_SuppressingFire);
                                                if (mightPower != null && mightPower.autocast)
                                                {
                                                    ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_SuppressingFire);
                                                    Pawn targetPawn = this.Pawn.TargetCurrentlyAimingAt.Thing as Pawn;
                                                    if (targetPawn != null)
                                                    {
                                                        AutoCast.CombatAbility_OnTarget_LoS.TryExecute(this, TorannMagicDefOf.TM_SuppressingFire, ability, mightPower, targetPawn, 1, out castSuccess);
                                                        if (castSuccess) goto AutoCastExit;
                                                    }
                                                }
                                            }
                                            if (current.abilityDef == TorannMagicDefOf.TM_Buckshot)
                                            {
                                                MightPower mightPower = this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_Buckshot);
                                                if (mightPower != null && mightPower.autocast)
                                                {
                                                    ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_Buckshot);
                                                    Pawn targetPawn = this.Pawn.TargetCurrentlyAimingAt.Thing as Pawn;
                                                    if (targetPawn != null)
                                                    {
                                                        AutoCast.CombatAbility_OnTarget_LoS.TryExecute(this, TorannMagicDefOf.TM_Buckshot, ability, mightPower, targetPawn, 1, out castSuccess);
                                                        if (castSuccess) goto AutoCastExit;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }                            
                        }
                        if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper) || isFaceless || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                        {
                            PawnAbility ability = null;
                            foreach (MightPower current in this.MightData.MightPowersS)
                            {
                                if (current.abilityDef != null && (current.abilityDef == this.mimicAbility || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper)))
                                {
                                    if (this.Pawn.equipment.Primary != null && this.Pawn.equipment.Primary.def.IsRangedWeapon)
                                    {
                                        if (current.abilityDef == TorannMagicDefOf.TM_Headshot)
                                        {
                                            MightPower mightPower = this.MightData.MightPowersS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_Headshot);
                                            if (mightPower != null && mightPower.autocast)
                                            {
                                                ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_Headshot);
                                                AutoCast.CombatAbility_OnTarget_LoS.TryExecute(this, TorannMagicDefOf.TM_Headshot, ability, mightPower, this.Pawn.CurJob.targetA, 4, out castSuccess);
                                                if (castSuccess) goto AutoCastExit;
                                            }
                                        }                                        
                                    }
                                }
                            }
                        }
                        if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight) || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                        {
                            PawnAbility ability = null;
                            foreach (MightPower current in this.MightData.MightPowersDK)
                            {
                                if (current.abilityDef != null && ((this.mimicAbility != null && this.mimicAbility.defName.Contains(current.abilityDef.defName)) || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight)))
                                {
                                    foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                    {
                                        if ((tmad == TorannMagicDefOf.TM_Spite || tmad == TorannMagicDefOf.TM_Spite_I || tmad == TorannMagicDefOf.TM_Spite_II || tmad == TorannMagicDefOf.TM_Spite_III))
                                        {
                                            if (this.Pawn.equipment.Primary != null && this.Pawn.equipment.Primary.def.IsRangedWeapon)
                                            {
                                                MightPower mightPower = this.MightData.MightPowersDK.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == tmad);
                                                if (mightPower != null && mightPower.learned && mightPower.autocast)
                                                {
                                                    ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                                    AutoCast.CombatAbility_OnTarget_LoS.TryExecute(this, tmad, ability, mightPower, this.Pawn.CurJob.targetA, 4, out castSuccess);
                                                    if (castSuccess) goto AutoCastExit;
                                                }                                                
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (this.skill_ThrowingKnife && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                        {
                            MightPower mightPower = this.MightData.MightPowersStandalone.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_ThrowingKnife);
                            if (mightPower != null && mightPower.autocast)
                            {
                                PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_ThrowingKnife);
                                AutoCast.CombatAbility_OnTarget_LoS.TryExecute(this, TorannMagicDefOf.TM_ThrowingKnife, ability, mightPower, this.Pawn.CurJob.targetA, 4, out castSuccess);
                                if (castSuccess) goto AutoCastExit;                                
                            }
                        }
                        if (this.skill_TempestStrike && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                        {
                            MightPower mightPower = this.MightData.MightPowersStandalone.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_TempestStrike);
                            if (mightPower != null && mightPower.autocast)
                            {
                                PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_TempestStrike);
                                AutoCast.CombatAbility_OnTarget_LoS.TryExecute(this, TorannMagicDefOf.TM_TempestStrike, ability, mightPower, this.Pawn.CurJob.targetA, 4, out castSuccess);
                                if (castSuccess) goto AutoCastExit;
                            }
                        }
                    }                    

                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer) || isFaceless || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MightPower current in this.MightData.MightPowersB)
                        {                      
                            if (current.abilityDef != null && ((this.mimicAbility != null && this.mimicAbility.defName.Contains(current.abilityDef.defName)) || (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer) || isCustom))) //current.abilityDef == this.mimicAbility ||
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if (tmad == TorannMagicDefOf.TM_PhaseStrike || tmad == TorannMagicDefOf.TM_PhaseStrike_I || tmad == TorannMagicDefOf.TM_PhaseStrike_II || tmad == TorannMagicDefOf.TM_PhaseStrike_III)
                                    {
                                        MightPower mightPower = this.MightData.MightPowersB.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == tmad);
                                        if (mightPower != null && mightPower.autocast && this.Pawn.equipment.Primary != null && !this.Pawn.equipment.Primary.def.IsRangedWeapon)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            float minDistance = ActualStaminaCost(tmad) * 100;
                                            AutoCast.Phase.Evaluate(this, tmad, ability, mightPower, minDistance, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }                                        
                                    }
                                }
                            }
                        }
                    }

                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) || isFaceless || isCustom)
                    {
                        PawnAbility ability = null;
                        foreach (MightPower current in this.MightData.MightPowersSS)
                        {
                            if (current.abilityDef != null && (current.abilityDef == this.mimicAbility || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier))) 
                            {
                                if (current.abilityDef == TorannMagicDefOf.TM_FirstAid && TM_Calc.IsPawnInjured(this.Pawn, .2f))
                                {
                                    MightPower mightPower = this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_FirstAid);
                                    if (mightPower != null && mightPower.autocast && !this.Pawn.CurJob.playerForced)
                                    {
                                        ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_FirstAid);
                                        AutoCast.HealSelf.Evaluate(this, TorannMagicDefOf.TM_FirstAid, ability, mightPower, out castSuccess);
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                            }
                        }
                    }

                    if (this.MightUserLevel >= 20)
                    {
                        MightPower mightPower = this.MightData.MightPowersStandalone.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_TeachMight);
                        if (mightPower.autocast && !this.Pawn.CurJob.playerForced && mightPower.learned)
                        {
                            if (this.Pawn.CurJobDef.joyKind != null || this.Pawn.CurJobDef == JobDefOf.Wait_Wander || Pawn.CurJobDef == JobDefOf.GotoWander)
                            {
                                PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_TeachMight);
                                AutoCast.TeachMight.Evaluate(this, TorannMagicDefOf.TM_TeachMight, ability, mightPower, out castSuccess);
                                if (castSuccess) goto AutoCastExit;
                            }
                        }
                    }
                }

                //combat (drafted) spells
                if (this.Pawn.drafter != null && this.Pawn.Drafted && this.Pawn.drafter.FireAtWill && this.Stamina != null && (this.Stamina.CurLevelPercentage >= settingsRef.autocastCombatMinThreshold || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk)) && this.Pawn.CurJob.def != JobDefOf.Goto && this.Pawn.CurJob.def != JobDefOf.AttackMelee)
                {
                    foreach (MightPower mp in this.MightData.MightPowersCustom)
                    {
                        if (mp.learned && mp.autocast && mp.autocasting != null && mp.autocasting.mightUser && mp.autocasting.drafted)
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
                                            if (targetPawn.Downed)
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
                                        AutoCast.CombatAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
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
                                        AutoCast.CombatAbility_OnSelf.Evaluate(this, tmad, ability, mp, out castSuccess);
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
                                        AutoCast.CombatAbility_OnCell.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
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
                                            if (targetPawn.Downed)
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
                                        AutoCast.CombatAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if (castSuccess) goto AutoCastExit;
                            }
                            //}
                            //catch
                            //{
                            //    Log.Message("no index found at " + mp.level + " for " + mp.abilityDef.defName);
                            //}
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer) || isFaceless || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MightPower current in this.MightData.MightPowersB)
                        {
                            if (current.abilityDef != null && (current.abilityDef == this.mimicAbility || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer)))
                            {
                                if (current.abilityDef == TorannMagicDefOf.TM_BladeSpin)
                                {
                                    MightPower mightPower = this.MightData.MightPowersB.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_BladeSpin);
                                    if (mightPower != null && mightPower.autocast && this.Pawn.equipment.Primary != null && !this.Pawn.equipment.Primary.def.IsRangedWeapon)
                                    {
                                        ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_BladeSpin);
                                        MightPowerSkill ver = this.MightData.MightPowerSkill_BladeSpin.FirstOrDefault((MightPowerSkill x) => x.label == "TM_BladeSpin_ver");
                                        AutoCast.AoECombat.Evaluate(this, TorannMagicDefOf.TM_BladeSpin, ability, mightPower, 2, Mathf.RoundToInt(2+(.5f*ver.level)), this.Pawn.Position, true, out castSuccess);
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk) || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MightPower current in this.MightData.MightPowersM)
                        {
                            if (current.abilityDef != null && (current.abilityDef == this.mimicAbility || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk)))
                            {
                                if (current.abilityDef == TorannMagicDefOf.TM_TigerStrike)
                                {
                                    MightPower mightPower = this.MightData.MightPowersM.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_TigerStrike);
                                    if (mightPower != null && mightPower.autocast && this.Pawn.equipment.Primary == null)
                                    {
                                        ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_TigerStrike);
                                        AutoCast.MonkCombatAbility.EvaluateMinRange(this, TorannMagicDefOf.TM_TigerStrike, ability, mightPower, this.Pawn.TargetCurrentlyAimingAt, 1.48f, out castSuccess);
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                                if (current.abilityDef == TorannMagicDefOf.TM_ThunderStrike)
                                {
                                    MightPower mightPower = this.MightData.MightPowersM.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_ThunderStrike);
                                    if (mightPower != null && mightPower.autocast)
                                    {
                                        ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_ThunderStrike);
                                        AutoCast.MonkCombatAbility.EvaluateMinRange(this, TorannMagicDefOf.TM_ThunderStrike, ability, mightPower, this.Pawn.TargetCurrentlyAimingAt, 6f, out castSuccess);
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator) || isFaceless || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MightPower current in this.MightData.MightPowersG)
                        {
                            if (current.abilityDef != null && ((this.mimicAbility != null && this.mimicAbility.defName.Contains(current.abilityDef.defName)) || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator)))
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if ((tmad == TorannMagicDefOf.TM_Grapple || tmad == TorannMagicDefOf.TM_Grapple_I || tmad == TorannMagicDefOf.TM_Grapple_II || tmad == TorannMagicDefOf.TM_Grapple_III))
                                    {
                                        MightPower mightPower = this.MightData.MightPowersG.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == tmad);
                                        if (mightPower != null && mightPower.learned && mightPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            AutoCast.CombatAbility.Evaluate(this, tmad, ability, mightPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }                                        
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Ranger) || isFaceless || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MightPower current in this.MightData.MightPowersR)
                        {
                            if (current.abilityDef != null && ((this.mimicAbility != null && this.mimicAbility.defName.Contains(current.abilityDef.defName)) || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Ranger)))
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if ((tmad == TorannMagicDefOf.TM_ArrowStorm || tmad == TorannMagicDefOf.TM_ArrowStorm_I || tmad == TorannMagicDefOf.TM_ArrowStorm_II || tmad == TorannMagicDefOf.TM_ArrowStorm_III))
                                    {
                                        if (this.Pawn.equipment.Primary != null && this.Pawn.equipment.Primary.def.IsRangedWeapon)
                                        {
                                            Thing wpn = this.Pawn.equipment.Primary;

                                            if (TM_Calc.IsUsingBow(this.Pawn))
                                            {
                                                MightPower mightPower = this.MightData.MightPowersR.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == tmad);
                                                if (mightPower != null && mightPower.learned && mightPower.autocast)
                                                {
                                                    ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                                    AutoCast.CombatAbility.Evaluate(this, tmad, ability, mightPower, out castSuccess);
                                                    if (castSuccess) goto AutoCastExit;
                                                }                                                
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight) || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MightPower current in this.MightData.MightPowersDK)
                        {
                            if (current.abilityDef != null)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if ((tmad == TorannMagicDefOf.TM_Spite || tmad == TorannMagicDefOf.TM_Spite_I || tmad == TorannMagicDefOf.TM_Spite_II || tmad == TorannMagicDefOf.TM_Spite_III))
                                    {
                                        MightPower mightPower = this.MightData.MightPowersDK.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == tmad);
                                        if (mightPower != null && mightPower.learned && mightPower.autocast)
                                        {
                                            ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                            AutoCast.CombatAbility.EvaluateMinRange(this, tmad, ability, mightPower, 4, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }                                        
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper) || isFaceless || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MightPower current in this.MightData.MightPowersS)
                        {
                            if (current.abilityDef != null && ((this.mimicAbility != null && this.mimicAbility.defName.Contains(current.abilityDef.defName)) || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper))) 
                            {
                                if(TM_Calc.IsUsingRanged(this.Pawn))
                                {
                                    foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                    {
                                        if (tmad == TorannMagicDefOf.TM_AntiArmor)
                                        {
                                            MightPower mightPower = this.MightData.MightPowersS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_AntiArmor);
                                            if (mightPower != null && mightPower.autocast)
                                            {
                                                ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_AntiArmor);
                                                AutoCast.CombatAbility.Evaluate(this, TorannMagicDefOf.TM_AntiArmor, ability, mightPower, out castSuccess);
                                                if (castSuccess) goto AutoCastExit;
                                            }
                                        }
                                        if (tmad == TorannMagicDefOf.TM_Headshot)
                                        {
                                            MightPower mightPower = this.MightData.MightPowersS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_Headshot);
                                            if (mightPower != null && mightPower.autocast)
                                            {
                                                ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_Headshot);
                                                AutoCast.CombatAbility.Evaluate(this, TorannMagicDefOf.TM_Headshot, ability, mightPower, out castSuccess);
                                                if (castSuccess) goto AutoCastExit;
                                            }
                                        }
                                        if ((tmad == TorannMagicDefOf.TM_DisablingShot || tmad == TorannMagicDefOf.TM_DisablingShot_I || tmad == TorannMagicDefOf.TM_DisablingShot_II || tmad == TorannMagicDefOf.TM_DisablingShot_III))
                                        {
                                            MightPower mightPower = this.MightData.MightPowersS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == tmad);
                                            if (mightPower != null && mightPower.learned && mightPower.autocast)
                                            {
                                                ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
                                                AutoCast.CombatAbility.Evaluate(this, tmad, ability, mightPower, out castSuccess);
                                                if (castSuccess) goto AutoCastExit;
                                            }                                            
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) || isFaceless || isCustom)
                    {
                        PawnAbility ability = null;

                        foreach (MightPower current in this.MightData.MightPowersSS)
                        {
                            if (current.abilityDef != null && (current.abilityDef == this.mimicAbility || this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier)))
                            {
                                if (current.abilityDef == TorannMagicDefOf.TM_FirstAid && TM_Calc.IsPawnInjured(this.Pawn, .2f))
                                {
                                    MightPower mightPower = this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_FirstAid);
                                    if (mightPower != null && mightPower.autocast)
                                    {
                                        ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_FirstAid);
                                        AutoCast.HealSelf.Evaluate(this, TorannMagicDefOf.TM_FirstAid, ability, mightPower, out castSuccess);
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }                                
                            }
                        }
                        if (this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                        {
                            foreach (MightPower current in this.MightData.MightPowersSS)
                            {
                                if (current.abilityDef != null && this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier))
                                {
                                    if (this.Pawn.equipment.Primary != null && this.Pawn.equipment.Primary.def.IsRangedWeapon)
                                    {
                                        if (this.specWpnRegNum != -1)
                                        {
                                            if (current.abilityDef == TorannMagicDefOf.TM_PistolWhip)
                                            {
                                                MightPower mightPower = this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_PistolWhip);
                                                if (mightPower != null && mightPower.autocast)
                                                {
                                                    ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_PistolWhip);
                                                    Pawn targetPawn = this.Pawn.TargetCurrentlyAimingAt.Thing as Pawn;
                                                    if (targetPawn != null)
                                                    {
                                                        AutoCast.MeleeCombat_OnTarget.TryExecute(this, TorannMagicDefOf.TM_PistolWhip, ability, mightPower, targetPawn, out castSuccess);
                                                        if (castSuccess) goto AutoCastExit;
                                                    }
                                                }
                                            }
                                            if (current.abilityDef == TorannMagicDefOf.TM_SuppressingFire)
                                            {
                                                MightPower mightPower = this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_SuppressingFire);
                                                if (mightPower != null && mightPower.autocast)
                                                {
                                                    ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_SuppressingFire);
                                                    Pawn targetPawn = this.Pawn.TargetCurrentlyAimingAt.Thing as Pawn;
                                                    if (targetPawn != null)
                                                    {
                                                        AutoCast.CombatAbility_OnTarget_LoS.TryExecute(this, TorannMagicDefOf.TM_SuppressingFire, ability, mightPower, targetPawn, 6, out castSuccess);
                                                        if (castSuccess) goto AutoCastExit;
                                                    }
                                                }
                                            }
                                            if (current.abilityDef == TorannMagicDefOf.TM_Buckshot)
                                            {
                                                MightPower mightPower = this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_Buckshot);
                                                if (mightPower != null && mightPower.autocast)
                                                {
                                                    ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_Buckshot);
                                                    Pawn targetPawn = this.Pawn.TargetCurrentlyAimingAt.Thing as Pawn;
                                                    if (targetPawn != null)
                                                    {
                                                        AutoCast.CombatAbility_OnTarget_LoS.TryExecute(this, TorannMagicDefOf.TM_Buckshot, ability, mightPower, targetPawn, 2, out castSuccess);
                                                        if (castSuccess) goto AutoCastExit;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        MightPower mightPower = this.MightData.MightPowersStandalone.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_ThrowingKnife);
                        if (mightPower != null && mightPower.learned && mightPower.autocast)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_ThrowingKnife);
                            Pawn targetPawn = TM_Calc.FindNearbyEnemy(this.Pawn, Mathf.RoundToInt(ability.Def.MainVerb.range * .9f));
                            AutoCast.CombatAbility_OnTarget_LoS.TryExecute(this, TorannMagicDefOf.TM_ThrowingKnife, ability, mightPower, targetPawn, 1, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        MightPower mightPower = this.MightData.MightPowersStandalone.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_TempestStrike);
                        if (mightPower != null && mightPower.learned && mightPower.autocast)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_TempestStrike);
                            Pawn targetPawn = TM_Calc.FindNearbyEnemy(this.Pawn, Mathf.RoundToInt(ability.Def.MainVerb.range * .9f));
                            AutoCast.CombatAbility_OnTarget_LoS.TryExecute(this, TorannMagicDefOf.TM_TempestStrike, ability, mightPower, targetPawn, 4, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        MightPower mightPower = this.MightData.MightPowersStandalone.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_PommelStrike);                        
                        if (mightPower != null && mightPower.learned && mightPower.autocast && this.Pawn.TargetCurrentlyAimingAt != null && this.Pawn.TargetCurrentlyAimingAt != this.Pawn)
                        {
                            PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == TorannMagicDefOf.TM_PommelStrike);
                            Pawn targetPawn = this.Pawn.TargetCurrentlyAimingAt.Thing as Pawn;
                            float minPain = .3f;
                            if(this.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_pwr").level >= 2)
                            {
                                minPain = .2f;
                            }
                            if (targetPawn != null && targetPawn.health?.hediffSet?.PainTotal >= minPain)
                            {
                                AutoCast.MeleeCombat_OnTarget.TryExecute(this, TorannMagicDefOf.TM_PommelStrike, ability, mightPower, targetPawn, out castSuccess);
                                if (castSuccess) goto AutoCastExit;
                            }
                        }
                    }
                }
                AutoCastExit:;
            }
        }

        public void ResolveAIAutoCast()
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (settingsRef.autocastEnabled && this.Pawn.jobs != null && this.Pawn.CurJob != null && this.Pawn.CurJob.def != TorannMagicDefOf.TMCastAbilityVerb && this.Pawn.CurJob.def != TorannMagicDefOf.TMCastAbilitySelf && this.Pawn.CurJob.def != JobDefOf.Ingest && this.Pawn.CurJob.def != JobDefOf.ManTurret && this.Pawn.GetPosture() == PawnPosture.Standing)
            {
                //Log.Message("pawn " + this.Pawn.LabelShort + " current job is " + this.Pawn.CurJob.def.defName);
                bool castSuccess = false;
                if (this.Stamina != null && this.Stamina.CurLevelPercentage >= settingsRef.autocastMinThreshold)
                {
                    foreach (MightPower mp in this.MightData.MightPowersCustom)
                    {
                        if (mp.learned && mp.autocasting != null && mp.autocasting.mightUser && mp.autocasting.AIUsable)
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
                                if (mp.autocasting.type == TMDefs.AutocastType.OnTarget && this.Pawn.TargetCurrentlyAimingAt != null)
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
                                            if (targetPawn.Downed)
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
                                        //if(targetThing is Pawn)
                                        //{
                                        //    Pawn targetPawn = targetThing as Pawn;
                                        //    if(targetPawn.IsPrisoner)
                                        //    {
                                        //        continue;
                                        //    }
                                        //}
                                        if (!mp.autocasting.ValidConditions(this.Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.CombatAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if (mp.autocasting.type == TMDefs.AutocastType.OnSelf)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.TargetCurrentlyAimingAt);
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
                                        AutoCast.CombatAbility_OnSelf.Evaluate(this, tmad, ability, mp, out castSuccess);
                                    }
                                }
                                if (mp.autocasting.type == TMDefs.AutocastType.OnCell && this.Pawn.CurJob.targetA != null)
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
                                        AutoCast.CombatAbility_OnCell.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
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
                                            if (targetPawn.Downed)
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
                                        AutoCast.CombatAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
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

        public void ResolveClassSkills()               
        {
            if (this.MightUserLevel >= 20 && (this.skill_Teach == false || !this.MightData.MightPowersStandalone.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_TeachMight).learned))
            {
                this.MightData.MightPowersStandalone.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_TeachMight).learned = true;
                this.AddPawnAbility(TorannMagicDefOf.TM_TeachMight);
                this.skill_Teach = true;
            }

            if (this.customClass != null && this.customClass.classHediff != null && !this.Pawn.health.hediffSet.HasHediff(this.customClass.classHediff))
            {
                HealthUtility.AdjustSeverity(this.Pawn, this.customClass.classHediff, this.customClass.hediffSeverity);
            }

            if (this.IsMightUser && !this.Pawn.Dead && !this.Pawn.Downed)
            {                
                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer))
                {
                    MightPowerSkill bladefocus_pwr = this.Pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_BladeFocus.FirstOrDefault((MightPowerSkill x) => x.label == "TM_BladeFocus_pwr");

                    List<Trait> traits = this.Pawn.story.traits.allTraits;
                    for (int i = 0; i < traits.Count; i++)
                    {
                        if (traits[i].def.defName == "Bladedancer")
                        {
                            if (traits[i].Degree != bladefocus_pwr.level)
                            {
                                traits.Remove(traits[i]);
                                this.Pawn.story.traits.GainTrait(new Trait(TraitDef.Named("Bladedancer"), bladefocus_pwr.level, false));
                                FleckMaker.ThrowHeatGlow(this.Pawn.Position, this.Pawn.Map, 2);
                            }
                        }
                    }
                }

                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator) || (this.customClass != null && this.customClass.classFighterAbilities.Contains(TorannMagicDefOf.TM_Fortitude)))
                {
                    if (!this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffFortitude))
                    {
                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_HediffFortitude, -5f);
                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_HediffFortitude, 1f);
                    }                    
                }
                if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffSprint))
                {
                    Hediff rec = this.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HediffSprint, false);
                    if (rec != null && rec.Severity != (.5f + this.MightData.MightPowerSkill_Sprint.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Sprint_pwr").level))
                    {
                        this.Pawn.health.RemoveHediff(rec);
                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_HediffSprint, (.5f + this.MightData.MightPowerSkill_Sprint.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Sprint_pwr").level));
                    }
                }

                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Ranger))
                {
                    MightPowerSkill rangertraining_pwr = this.MightData.MightPowerSkill_RangerTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_RangerTraining_pwr");

                    List<Trait> traits = this.Pawn.story.traits.allTraits;
                    for (int i = 0; i < traits.Count; i++)
                    {
                        if (traits[i].def == TorannMagicDefOf.Ranger)
                        {
                            if (traits[i].Degree != rangertraining_pwr.level)
                            {
                                traits.Remove(traits[i]);
                                this.Pawn.story.traits.GainTrait(new Trait(TraitDef.Named("Ranger"), rangertraining_pwr.level, false));
                                FleckMaker.ThrowHeatGlow(this.Pawn.Position, this.Pawn.Map, 2);
                            }
                        }
                    }
                }

                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper))
                {
                    MightPowerSkill sniperfocus_pwr = this.Pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_SniperFocus.FirstOrDefault((MightPowerSkill x) => x.label == "TM_SniperFocus_pwr");

                    List<Trait> traits = this.Pawn.story.traits.allTraits;
                    for (int i = 0; i < traits.Count; i++)
                    {
                        if (traits[i].def.defName == "TM_Sniper")
                        {
                            if (traits[i].Degree != sniperfocus_pwr.level)
                            {
                                traits.Remove(traits[i]);
                                this.Pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Sniper, sniperfocus_pwr.level, false));
                                FleckMaker.ThrowHeatGlow(base.Pawn.Position, this.Pawn.Map, 2);
                            }
                        }
                    }
                }

                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic) || TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_PsionicAugmentation))
                {
                    if (!this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_PsionicHD"), false))
                    {
                        HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_PsionicHD"), 1f);
                    }
                }

                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer) || (this.customClass != null && this.customClass.classFighterAbilities.Contains(TorannMagicDefOf.TM_BladeArt))) && !this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BladeArtHD))
                {
                    MightPowerSkill bladeart_pwr = this.Pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_BladeArt.FirstOrDefault((MightPowerSkill x) => x.label == "TM_BladeArt_pwr");

                    //HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_BladeArtHD, -5f);
                    HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_BladeArtHD, (.5f) + bladeart_pwr.level);
                    if (!this.Pawn.IsColonist && settingsRef.AIHardMode)
                    {
                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_BladeArtHD, 4);
                    }
                }
                if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Ranger) || (this.customClass != null && this.customClass.classFighterAbilities.Contains(TorannMagicDefOf.TM_BowTraining))))
                {                    
                    MightPowerSkill bowtraining_pwr = this.Pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_BowTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_BowTraining_pwr");
                    if (!this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BowTrainingHD))
                    {
                        //HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_BowTrainingHD, -5f);
                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_BowTrainingHD, (.5f) + bowtraining_pwr.level);
                        if (!this.Pawn.IsColonist && settingsRef.AIHardMode)
                        {
                            HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_BowTrainingHD, 4);
                        }
                    }
                }

                using (IEnumerator<Hediff> enumerator = this.Pawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Hediff rec = enumerator.Current;

                        if (rec.def == TorannMagicDefOf.TM_BladeArtHD && this.Pawn.IsColonist)
                        {
                            MightPowerSkill bladeart_pwr = this.Pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_BladeArt.FirstOrDefault((MightPowerSkill x) => x.label == "TM_BladeArt_pwr");
                            if (rec.Severity < (float)(.5f + bladeart_pwr.level) || rec.Severity > (float)(.6f + bladeart_pwr.level))
                            {
                                HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_BladeArtHD, -5f);
                                HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_BladeArtHD, (.5f) + bladeart_pwr.level);
                                FleckMaker.ThrowDustPuff(this.Pawn.Position.ToVector3Shifted(), this.Pawn.Map, .6f);
                                FleckMaker.ThrowHeatGlow(this.Pawn.Position, this.Pawn.Map, 1.6f);
                            }
                        }

                        if (rec.def == TorannMagicDefOf.TM_BowTrainingHD && this.Pawn.IsColonist)
                        {
                            MightPowerSkill bowtraining_pwr = this.Pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_BowTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_BowTraining_pwr");
                            if (rec.Severity < (float)(.5f + bowtraining_pwr.level) || rec.Severity > (float)(.6f + bowtraining_pwr.level))
                            {
                                HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_BowTrainingHD, -5f);
                                HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_BowTrainingHD, (.5f) + bowtraining_pwr.level);
                                FleckMaker.ThrowDustPuff(this.Pawn.Position.ToVector3Shifted(), this.Pawn.Map, .6f);
                                FleckMaker.ThrowHeatGlow(this.Pawn.Position, this.Pawn.Map, 1.6f);
                            }
                        }
                    }
                }

                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight) || (this.customClass != null && (this.customClass.classFighterAbilities.Contains(TorannMagicDefOf.TM_Shroud) || this.customClass.classHediff == TorannMagicDefOf.TM_HateHD)))
                {
                    int hatePwr = this.MightData.MightPowerSkill_Shroud.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Shroud_pwr").level;
                    float sevSvr = 0;
                    Hediff hediff = null;
                    for (int h = 0; h < this.Pawn.health.hediffSet.hediffs.Count; h++)
                    {
                        if (this.Pawn.health.hediffSet.hediffs[h].def.defName.Contains("TM_HateH"))
                        {
                            hediff = this.Pawn.health.hediffSet.hediffs[h];
                        }
                    }

                    if (hediff != null)
                    {
                        sevSvr = hediff.Severity;
                        if (hatePwr == 5 && hediff.def.defName != "TM_HateHD_V")
                        {
                            this.Pawn.health.RemoveHediff(hediff);
                            HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HateHD_V"), sevSvr);
                        }
                        else if (hatePwr == 4 && hediff.def.defName != "TM_HateHD_IV")
                        {
                            this.Pawn.health.RemoveHediff(hediff);
                            HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HateHD_IV"), sevSvr);
                        }
                        else if (hatePwr == 3 && hediff.def.defName != "TM_HateHD_III")
                        {
                            this.Pawn.health.RemoveHediff(hediff);
                            HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HateHD_III"), sevSvr);
                        }
                        else if (hatePwr == 2 && hediff.def.defName != "TM_HateHD_II")
                        {
                            this.Pawn.health.RemoveHediff(hediff);
                            HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HateHD_II"), sevSvr);
                        }
                        else if (hatePwr == 1 && hediff.def.defName != "TM_HateHD_I")
                        {
                            this.Pawn.health.RemoveHediff(hediff);
                            HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HateHD_I"), sevSvr);
                        }
                        else if (hatePwr == 0 && hediff.def.defName != "TM_HateHD")
                        {
                            this.Pawn.health.RemoveHediff(hediff);
                            HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HateHD"), sevSvr);
                        }
                    }

                    if (!TM_Calc.HasHateHediff(this.Pawn))
                    {
                        HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HateHD"), 1);
                    }
                }

                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk) || (this.customClass != null && (this.customClass.classHediff == TorannMagicDefOf.TM_ChiHD || this.customClass.classFighterAbilities.Contains(TorannMagicDefOf.TM_Chi))))
                {
                    if (!this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ChiHD, false))
                    {
                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_ChiHD, 1f);
                    }
                }

                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk) || (this.customClass != null && this.customClass.classFighterAbilities.Contains(TorannMagicDefOf.TM_MindOverBody)))
                {
                    if (!this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MindOverBodyHD, false))
                    {
                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_MindOverBodyHD, .5f);
                    }
                    else
                    {
                        Hediff mob = this.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MindOverBodyHD, false);
                        int mobPwr = this.MightData.MightPowerSkill_MindOverBody.FirstOrDefault((MightPowerSkill x) => x.label == "TM_MindOverBody_pwr").level;
                        if (mobPwr == 3 && mob.Severity < 3)
                        {
                            this.Pawn.health.RemoveHediff(mob);
                            HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_MindOverBodyHD, 3.5f);
                        }
                        else if (mobPwr == 2 && mob.Severity < 2)
                        {
                            this.Pawn.health.RemoveHediff(mob);
                            HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_MindOverBodyHD, 2.5f);
                        }
                        else if (mobPwr == 1 && mob.Severity < 1)
                        {
                            this.Pawn.health.RemoveHediff(mob);
                            HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_MindOverBodyHD, 1.5f);
                        }
                        else if (mobPwr == 0 && mob.Severity >= 1)
                        {
                            this.Pawn.health.RemoveHediff(mob);
                            HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_MindOverBodyHD, .5f);
                        }
                    }
                }

                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer) || (this.customClass != null && this.customClass.classFighterAbilities.Contains(TorannMagicDefOf.TM_FieldTraining)))
                {
                    int pwrVal = this.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_pwr").level;
                    int verVal = this.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_ver").level;
                    using (IEnumerator<Hediff> enumerator = this.Pawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Hediff rec = enumerator.Current;
                            if (rec.def == TorannMagicDefOf.TM_HediffHeavyBlow && rec.Severity != (.95f + (.19f * pwrVal)))
                            {
                                this.Pawn.health.RemoveHediff(rec);
                                HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_HediffHeavyBlow, .95f + (.19f * pwrVal));
                            }
                            if (rec.def == TorannMagicDefOf.TM_HediffStrongBack)
                            {
                                if (verVal >= 8)
                                {
                                    if (rec.Severity != 2.5f)
                                    {
                                        this.Pawn.health.RemoveHediff(rec);
                                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_HediffStrongBack, 2.5f);
                                    }
                                }
                                else if (verVal >= 3)
                                {
                                    if (rec.Severity != 1.5f)
                                    {
                                        this.Pawn.health.RemoveHediff(rec);
                                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_HediffStrongBack, 1.5f);
                                    }
                                }
                            }
                            if (rec.def == TorannMagicDefOf.TM_HediffThickSkin)
                            {
                                if (verVal >= 12)
                                {
                                    if (rec.Severity != 3.5f)
                                    {
                                        this.Pawn.health.RemoveHediff(rec);
                                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_HediffThickSkin, 3.5f);
                                    }
                                }
                                else if (verVal >= 7)
                                {
                                    if (rec.Severity != 2.5f)
                                    {
                                        this.Pawn.health.RemoveHediff(rec);
                                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_HediffThickSkin, 2.5f);
                                    }
                                }
                                else if (verVal >= 2)
                                {
                                    if (rec.Severity != 1.5f)
                                    {
                                        this.Pawn.health.RemoveHediff(rec);
                                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_HediffThickSkin, 1.5f);
                                    }
                                }
                            }
                            if (rec.def == TorannMagicDefOf.TM_HediffFightersFocus)
                            {
                                if (verVal >= 1)
                                {
                                    if (rec.Severity != 1.5f)
                                    {
                                        this.Pawn.health.RemoveHediff(rec);
                                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_HediffFightersFocus, 1.5f);
                                    }
                                }
                            }
                            if (rec.def == TorannMagicDefOf.TM_HediffSprint)
                            {
                                if (rec.Severity != (.5f + (int)(pwrVal / 3)))
                                {
                                    this.Pawn.health.RemoveHediff(rec);
                                    HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_HediffSprint, (.5f + (int)(pwrVal / 3)));
                                }
                            }
                        }
                    }
                    if (verVal >= 6 && !this.skill_Legion)
                    {
                        this.skill_Legion = true;
                        this.AddPawnAbility(TorannMagicDefOf.TM_Legion);
                    }
                }

                if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) || (this.customClass != null && this.customClass.classHediff == TorannMagicDefOf.TM_SS_SerumHD))
                {
                    if (!this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SS_SerumHD, false))
                    {
                        if (!this.Pawn.IsColonist)
                        {
                            float range = Rand.Range(5f, 25f);
                            HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_SS_SerumHD, range);
                        }
                        else
                        {
                            HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_SS_SerumHD, 2.2f);
                        }
                    }                    
                }

                if (this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SS_SerumHD) && this.Pawn.Downed && nextSSTend < Find.TickManager.TicksGame && 
                    (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier) || (this.customClass != null && this.customClass.classFighterAbilities.Contains(TorannMagicDefOf.TM_FirstAid))))
                {
                    Hediff_Injury wound = this.Pawn.health.hediffSet.GetInjuriesTendable().RandomElement();
                    if (wound != null && wound.CanHealNaturally())
                    {
                        wound.Tended(Rand.Range(0, .3f), .3f);
                    }
                    nextSSTend = Find.TickManager.TicksGame + Rand.Range(6000, 8000);
                }

            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            return base.CompGetGizmosExtra();
        }

        private void ResolveSustainedSkills()
        {
            float _maxSP = 0;
            float _maxSPUpkeep = 0;
            float _spRegenRate = 0;
            float _spRegenRateUpkeep = 0;
            float _coolDown = 0;
            float _spCost = 0;
            float _xpGain = 0;
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
                    _maxSP += e.maxSP;
                    _spCost += e.spCost;
                    _spRegenRate += e.spRegenRate;
                    _coolDown += e.mightCooldown;
                    _xpGain += e.xpGain;
                    _arcaneRes += e.arcaneRes;
                    _arcaneDmg += e.combatDmg;
                }
            }

            //Determine hediff adjustments
            foreach (Hediff hd in this.Pawn.health.hediffSet.hediffs)
            {
                if (hd.def.GetModExtension<TMDefs.DefModExtension_HediffEnchantments>() != null)
                {
                    foreach (TMDefs.HediffEnchantment hdStage in hd.def.GetModExtension<TMDefs.DefModExtension_HediffEnchantments>().stages)
                    {
                        if (hd.Severity >= hdStage.minSeverity && hd.Severity < hdStage.maxSeverity)
                        {
                            TMDefs.DefModExtension_TraitEnchantments e = hdStage.enchantments;
                            if (e != null)
                            {
                                _maxSP += e.maxSP;
                                _spCost += e.spCost;
                                _spRegenRate += e.spRegenRate;
                                _coolDown += e.mightCooldown;
                                _xpGain += e.xpGain;
                                _arcaneRes += e.arcaneRes;
                                _arcaneDmg += e.arcaneDmg;
                            }
                            break;
                        }
                    }
                }
            }

            //Determine apparel and equipment enchantments
            List<Apparel> apparel = this.Pawn.apparel.WornApparel;
            if (apparel != null)
            {
                totalApparelWeight = 0;
                for (int i = 0; i < this.Pawn.apparel.WornApparelCount; i++)
                {
                    Enchantment.CompEnchantedItem item = apparel[i].GetComp<Enchantment.CompEnchantedItem>();
                    if (item != null)
                    {
                        if (item.HasEnchantment)
                        {
                            float enchantmentFactor = 1f;
                            totalApparelWeight += apparel[i].def.GetStatValueAbstract(StatDefOf.Mass, apparel[i].Stuff);
                            if (item.MadeFromEnchantedStuff)
                            {                                
                                Enchantment.CompProperties_EnchantedStuff compES = apparel[i].Stuff.GetCompProperties<Enchantment.CompProperties_EnchantedStuff>();
                                enchantmentFactor = compES.enchantmentBonusMultiplier;                                    

                                float arcalleumFactor = compES.arcalleumCooldownPerMass;
                                if (apparel[i].Stuff.defName == "TM_Arcalleum")
                                {
                                    _arcaneRes += .05f;
                                }
                                _arcalleumCooldown += (totalApparelWeight * (arcalleumFactor / 100));                                
                            }

                            _maxSP += item.maxMP * enchantmentFactor;
                            _spRegenRate += item.mpRegenRate * enchantmentFactor;
                            _coolDown += item.coolDown * enchantmentFactor;
                            _xpGain += item.xpGain * enchantmentFactor;
                            _spCost += item.mpCost * enchantmentFactor;
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

                        _maxSP += item.maxMP * enchantmentFactor;
                        _spRegenRate += item.mpRegenRate * enchantmentFactor;
                        _coolDown += item.coolDown * enchantmentFactor;
                        _xpGain += item.xpGain * enchantmentFactor;
                        _spCost += item.mpCost * enchantmentFactor;
                        _arcaneRes += item.arcaneRes * enchantmentFactor;
                        _arcaneDmg += item.arcaneDmg * enchantmentFactor;

                    }
                    if (this.Pawn.story != null && this.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk) && this.Pawn.Faction != null && this.Pawn.Faction.HostileTo(Faction.OfPlayer))
                    {
                        ThingWithComps outItem;
                        this.Pawn.equipment.TryDropEquipment(this.Pawn.equipment.Primary, out outItem, this.Pawn.Position, true);
                    }
                }
            }

            //Determine active or sustained abilities            
            using (IEnumerator<Hediff> enumerator = this.Pawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Hediff rec = enumerator.Current;
                    TMAbilityDef ability = this.MightData.GetHediffAbility(rec);
                    if (ability != null)
                    {
                        MightPowerSkill skill = this.MightData.GetSkill_Efficiency(ability);
                        int level = 0;
                        if (skill != null)
                        {
                            level = skill.level;
                        }

                        _maxSPUpkeep += (ability.upkeepEnergyCost * (1f - (ability.upkeepEfficiencyPercent * level)));
                        _spRegenRateUpkeep += (ability.upkeepRegenCost * (1f - (ability.upkeepEfficiencyPercent * level)));
                        
                    }                    
                    if(rec.def == TorannMagicDefOf.TM_SS_SerumHD)
                    {
                        _spRegenRate += (float)(.1f * rec.CurStageIndex);
                        _arcaneRes += (float)(.15f * rec.CurStageIndex);
                        _arcaneDmg += (float)(.05f * rec.CurStageIndex);
                    }
                }
            }
            //Bonded animal upkeep
            if (this.bondedPet != null)
            {
                _maxSPUpkeep += (TorannMagicDefOf.TM_AnimalFriend.upkeepEnergyCost * (1f - (TorannMagicDefOf.TM_AnimalFriend.upkeepEfficiencyPercent * this.MightData.GetSkill_Efficiency(TorannMagicDefOf.TM_AnimalFriend).level)));
                if (this.bondedPet.Dead || this.bondedPet.Destroyed)
                {
                    if(this.bondedPet.Dead)
                    {
                        this.bondedPet.health.RemoveHediff(this.bondedPet.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_RangerBondHD, false));
                    }
                    this.Pawn.needs.mood.thoughts.memories.TryGainMemory(TorannMagicDefOf.RangerPetDied, null);
                    this.bondedPet = null;
                }
                else if (this.bondedPet.Faction != null && this.bondedPet.Faction != this.Pawn.Faction)
                {
                    //sold? punish evil
                    this.Pawn.needs.mood.thoughts.memories.TryGainMemory(TorannMagicDefOf.RangerSoldBondedPet, null);
                    this.bondedPet = null;
                }
                else if(!this.bondedPet.health.hediffSet.HasHediff(TorannMagicDefOf.TM_RangerBondHD))
                {
                    HealthUtility.AdjustSeverity(this.bondedPet, TorannMagicDefOf.TM_RangerBondHD, .5f);
                }
            }
            if(this.Pawn.needs.mood.thoughts.memories.NumMemoriesOfDef(ThoughtDef.Named("RangerSoldBondedPet")) > 0)
            {
                if(this.animalBondingDisabled == false)
                {
                    this.RemovePawnAbility(TorannMagicDefOf.TM_AnimalFriend);
                    this.animalBondingDisabled = true;
                }
            }
            else
            {
                if(this.animalBondingDisabled == true)
                {
                    this.AddPawnAbility(TorannMagicDefOf.TM_AnimalFriend);
                    this.animalBondingDisabled = false;
                }
            }

            if(MightData.MightAbilityPoints < 0)
            {
                MightData.MightAbilityPoints = 0;
            }      
            //Class and global bonuses

            _arcaneDmg += (.01f * this.MightData.MightPowerSkill_WayfarerCraft.FirstOrDefault((MightPowerSkill x) => x.label == "TM_WayfarerCraft_pwr").level);
            _arcaneRes += (.02f * this.MightData.MightPowerSkill_WayfarerCraft.FirstOrDefault((MightPowerSkill x) => x.label == "TM_WayfarerCraft_pwr").level);
            _spCost -= (.01f * this.MightData.MightPowerSkill_WayfarerCraft.FirstOrDefault((MightPowerSkill x) => x.label == "TM_WayfarerCraft_eff").level);
            _xpGain += (.02f * this.MightData.MightPowerSkill_WayfarerCraft.FirstOrDefault((MightPowerSkill x) => x.label == "TM_WayfarerCraft_eff").level);
            _coolDown -= (.01f * this.MightData.MightPowerSkill_WayfarerCraft.FirstOrDefault((MightPowerSkill x) => x.label == "TM_WayfarerCraft_ver").level);
            _spRegenRate += (.01f * this.MightData.MightPowerSkill_WayfarerCraft.FirstOrDefault((MightPowerSkill x) => x.label == "TM_WayfarerCraft_ver").level);
            _maxSP += (.02f * this.MightData.MightPowerSkill_WayfarerCraft.FirstOrDefault((MightPowerSkill x) => x.label == "TM_WayfarerCraft_ver").level);
            _maxSPUpkeep *= (1f - (.03f * this.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_eff").level));
            _spRegenRateUpkeep *= (1f - (.03f * this.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_eff").level));

            _maxSP += (.04f * this.MightData.MightPowerSkill_global_endurance.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_endurance_pwr").level);
            _spRegenRate += (.05f * this.MightData.MightPowerSkill_global_refresh.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_refresh_pwr").level);
            _spCost += (-.025f * this.MightData.MightPowerSkill_global_seff.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_seff_pwr").level);
            _arcaneDmg += (.05f * this.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_strength_pwr").level);
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

            //resolve upkeep costs            
            _maxSP -= (_maxSPUpkeep);
            _spRegenRate -= (_spRegenRateUpkeep);

            //finalize
            this.maxSP = Mathf.Clamp(1 + _maxSP, 0f, 5f);
            this.spRegenRate = 1f + _spRegenRate;
            this.coolDown = Mathf.Clamp(1f + _coolDown, .25f, 10f);
            this.xpGain = Mathf.Clamp(1f + _xpGain, 0.01f, 5f);
            this.spCost = Mathf.Clamp(1f + _spCost, 0.1f, 5f);
            this.arcaneRes = 1 + _arcaneRes;
            this.mightPwr = 1 + _arcaneDmg;

            if (this.IsMightUser && !TM_Calc.IsCrossClass(this.Pawn, false))
            {
                if (_maxSP != 0 && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_maxEnergy")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_maxEnergy"), .5f);
                }
                if (_spRegenRate != 0 && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_energyRegen")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_energyRegen"), .5f);
                }
                if (_coolDown != 0 && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_coolDown")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_coolDown"), .5f);
                }
                if (_xpGain != 0 && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_xpGain")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_xpGain"), .5f);
                }
                if (_spCost != 0 && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_energyCost")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_energyCost"), .5f);
                }
                if (_arcaneRes != 0 && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_dmgResistance")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_dmgResistance"), .5f);
                }
                if (_arcaneDmg != 0 && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_dmgBonus")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_dmgBonus"), .5f);
                }
                if (_arcalleumCooldown != 0 && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_arcalleumCooldown")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_arcalleumCooldown"), .5f);
                }
                if (_arcaneSpectre == true && !this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_arcaneSpectre")))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_HediffEnchantment_arcaneSpectre"), .5f);
                }
                else if (_arcaneSpectre == false && this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffEnchantment_arcaneSpectre")))
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

        public void ResolveStamina()
        {
            bool flag = this.Stamina == null;
            if (flag)
            {
                Hediff firstHediffOfDef = base.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MightUserHD, false);
                bool flag2 = firstHediffOfDef != null;
                if (flag2)
                {
                    firstHediffOfDef.Severity = 1f;
                }
                else
                {
                    Hediff hediff = HediffMaker.MakeHediff(TorannMagicDefOf.TM_MightUserHD, base.Pawn, null);
                    hediff.Severity = 1f;
                    base.Pawn.health.AddHediff(hediff, null, null);
                }
            }
        }
        public void ResolveMightPowers()
        {
            bool flag = this.mightPowersInitialized;
            if (!flag)
            {
                this.mightPowersInitialized = true;
            }
        }
        public void ResolveMightTab()
        {
            InspectTabBase inspectTabsx = base.Pawn.GetInspectTabs().FirstOrDefault((InspectTabBase x) => x.labelKey == "TM_TabMight");
            IEnumerable<InspectTabBase> inspectTabs = base.Pawn.GetInspectTabs();
            bool flag = inspectTabs != null && inspectTabs.Count<InspectTabBase>() > 0;
            if (flag)
            {         
                if (inspectTabsx == null)
                {
                    try
                    {
                        base.Pawn.def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_Pawn_Might)));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Concat(new object[]
                        {
                            "Could not instantiate inspector tab of type ",
                            typeof(ITab_Pawn_Might),
                            ": ",
                            ex
                        }));
                    }
                }
            }
        }

        public override void PostExposeData()
        {
            //base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.mightPowersInitialized, "mightPowersInitialized", false, false);
            Scribe_Collections.Look<Thing>(ref this.combatItems, "combatItems", LookMode.Reference);
            Scribe_Deep.Look(ref this.equipmentContainer, "equipmentContainer", new object[0]);
            Scribe_References.Look<Pawn>(ref this.bondedPet, "bondedPet", false);
            Scribe_Values.Look<bool>(ref this.skill_GearRepair, "skill_GearRepair", false, false);
            Scribe_Values.Look<bool>(ref this.skill_InnerHealing, "skill_InnerHealing", false, false);
            Scribe_Values.Look<bool>(ref this.skill_HeavyBlow, "skill_HeavyBlow", false, false);
            Scribe_Values.Look<bool>(ref this.skill_Sprint, "skill_Sprint", false, false);
            Scribe_Values.Look<bool>(ref this.skill_StrongBack, "skill_StrongBack", false, false);
            Scribe_Values.Look<bool>(ref this.skill_ThickSkin, "skill_ThickSkin", false, false);
            Scribe_Values.Look<bool>(ref this.skill_FightersFocus, "skill_FightersFocus", false, false);
            Scribe_Values.Look<bool>(ref this.skill_BurningFury, "skill_BurningFury", false, false);
            Scribe_Values.Look<bool>(ref this.skill_ThrowingKnife, "skill_ThrowingKnife", false, false);
            Scribe_Values.Look<bool>(ref this.skill_PommelStrike, "skill_PommelStrike", false, false);
            Scribe_Values.Look<bool>(ref this.skill_Legion, "skill_Legion", false, false);
            Scribe_Values.Look<bool>(ref this.skill_TempestStrike, "skill_TempestStrike", false, false);
            Scribe_Values.Look<bool>(ref this.skill_PistolWhip, "skill_PistolWhip", false, false);
            Scribe_Values.Look<bool>(ref this.skill_SuppressingFire, "skill_SuppressingFire", false, false);
            Scribe_Values.Look<bool>(ref this.skill_Mk203GL, "skill_Mk203GL", false, false);
            Scribe_Values.Look<bool>(ref this.skill_Buckshot, "skill_Buckshot", false, false);
            Scribe_Values.Look<bool>(ref this.skill_BreachingCharge, "skill_BreachingCharge", false, false);
            Scribe_Values.Look<bool>(ref this.skill_Teach, "skill_Teach", false, false);
            Scribe_Values.Look<int>(ref this.allowMeditateTick, "allowMeditateTick", 0, false);
            Scribe_Values.Look<bool>(ref this.deathRetaliating, "deathRetaliating", false, false);
            Scribe_Values.Look<bool>(ref this.canDeathRetaliate, "canDeathRetaliate", false, false);
            Scribe_Values.Look<int>(ref this.ticksTillRetaliation, "ticksTillRetaliation", 600, false);
            Scribe_Values.Look<bool>(ref this.useCleaveToggle, "useCleaveToggle", true, false);
            Scribe_Values.Look<bool>(ref this.useCQCToggle, "useCQCToggle", true, false);
            Scribe_Defs.Look<TMAbilityDef>(ref this.mimicAbility, "mimicAbility");
            Scribe_Values.Look<float>(ref this.maxSP, "maxSP", 1f, false);
            Scribe_Deep.Look<MightData>(ref this.mightData, "mightData", new object[]
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
                    if (TM_ClassUtility.CustomClasses()[index].isFighter)
                    {
                        this.customClass = TM_ClassUtility.CustomClasses()[index];
                        this.customIndex = index;

                        for (int i = 0; i < customClass.classFighterAbilities.Count; i++)
                        {
                            TMAbilityDef ability = customClass.classFighterAbilities[i];
                            
                            for (int j = 0; j < this.MightData.AllMightPowers.Count; j++)
                            {
                                if (this.MightData.AllMightPowers[j].TMabilityDefs.Contains(ability) && this.MightData.AllMightPowers[j].learned)
                                {
                                    if (ability.shouldInitialize)
                                    {
                                        int level = this.MightData.AllMightPowers[j].level;
                                        base.AddPawnAbility(this.MightData.AllMightPowers[j].TMabilityDefs[level]);
                                    }
                                    if(ability.childAbilities != null && ability.childAbilities.Count > 0)
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
                    bool flag40 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Gladiator);
                    if (flag40)
                    {
                        bool flag14 = !this.MightData.MightPowersG.NullOrEmpty<MightPower>();
                        if (flag14)
                        {
                            foreach (MightPower current3 in this.MightData.MightPowersG)
                            {
                                bool flag15 = current3.abilityDef != null;
                                if (flag15)
                                {
                                    if ((current3.abilityDef == TorannMagicDefOf.TM_Sprint || current3.abilityDef == TorannMagicDefOf.TM_Sprint_I || current3.abilityDef == TorannMagicDefOf.TM_Sprint_II || current3.abilityDef == TorannMagicDefOf.TM_Sprint_III))
                                    {
                                        if (current3.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Sprint);
                                        }
                                        else if (current3.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Sprint_I);
                                        }
                                        else if (current3.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Sprint_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Sprint_III);
                                        }
                                    }
                                    if ((current3.abilityDef == TorannMagicDefOf.TM_Grapple || current3.abilityDef == TorannMagicDefOf.TM_Grapple_I || current3.abilityDef == TorannMagicDefOf.TM_Grapple_II || current3.abilityDef == TorannMagicDefOf.TM_Grapple_III))
                                    {
                                        if (current3.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Grapple);
                                        }
                                        else if (current3.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Grapple_I);
                                        }
                                        else if (current3.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Grapple_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Grapple_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (flag40)
                    {
                        // Log.Message("Loading Gladiator Abilities");
                        //this.AddPawnAbility(TorannMagicDefOf.TM_Fortitude);
                        //this.AddPawnAbility(TorannMagicDefOf.TM_Cleave);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Whirlwind);
                    }
                    bool flag41 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper);
                    if (flag41)
                    {
                        bool flag17 = !this.MightData.MightPowersS.NullOrEmpty<MightPower>();
                        if (flag17)
                        {
                            foreach (MightPower current4 in this.MightData.MightPowersS)
                            {
                                bool flag18 = current4.abilityDef != null;
                                if (flag18)
                                {
                                    if ((current4.abilityDef == TorannMagicDefOf.TM_DisablingShot || current4.abilityDef == TorannMagicDefOf.TM_DisablingShot_I || current4.abilityDef == TorannMagicDefOf.TM_DisablingShot_II || current4.abilityDef == TorannMagicDefOf.TM_DisablingShot_III))
                                    {
                                        if (current4.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_DisablingShot);
                                        }
                                        else if (current4.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_DisablingShot_I);
                                        }
                                        else if (current4.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_DisablingShot_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_DisablingShot_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (flag41)
                    {
                        //Log.Message("Loading Sniper Abilities");
                        //this.AddPawnAbility(TorannMagicDefOf.TM_SniperFocus);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Headshot);
                        this.AddPawnAbility(TorannMagicDefOf.TM_AntiArmor);
                        this.AddPawnAbility(TorannMagicDefOf.TM_ShadowSlayer);
                    }
                    bool flag42 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Bladedancer);
                    if (flag42)
                    {
                        bool flag19 = !this.MightData.MightPowersB.NullOrEmpty<MightPower>();
                        if (flag19)
                        {
                            foreach (MightPower current5 in this.MightData.MightPowersB)
                            {
                                bool flag20 = current5.abilityDef != null;
                                if (flag20)
                                {
                                    if ((current5.abilityDef == TorannMagicDefOf.TM_PhaseStrike || current5.abilityDef == TorannMagicDefOf.TM_PhaseStrike_I || current5.abilityDef == TorannMagicDefOf.TM_PhaseStrike_II || current5.abilityDef == TorannMagicDefOf.TM_PhaseStrike_III))
                                    {
                                        if (current5.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_PhaseStrike);
                                        }
                                        else if (current5.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_PhaseStrike_I);
                                        }
                                        else if (current5.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_PhaseStrike_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_PhaseStrike_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (flag42)
                    {
                        //Log.Message("Loading Bladedancer Abilities");
                        //this.AddPawnAbility(TorannMagicDefOf.TM_BladeFocus);
                        //this.AddPawnAbility(TorannMagicDefOf.TM_BladeArt);
                        this.AddPawnAbility(TorannMagicDefOf.TM_SeismicSlash);
                        this.AddPawnAbility(TorannMagicDefOf.TM_BladeSpin);
                    }
                    bool flag43 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Ranger);
                    if (flag43)
                    {
                        bool flag21 = !this.MightData.MightPowersR.NullOrEmpty<MightPower>();
                        if (flag21)
                        {
                            foreach (MightPower current6 in this.MightData.MightPowersR)
                            {
                                bool flag22 = current6.abilityDef != null;
                                if (flag22)
                                {
                                    if ((current6.abilityDef == TorannMagicDefOf.TM_ArrowStorm || current6.abilityDef == TorannMagicDefOf.TM_ArrowStorm_I || current6.abilityDef == TorannMagicDefOf.TM_ArrowStorm_II || current6.abilityDef == TorannMagicDefOf.TM_ArrowStorm_III))
                                    {
                                        if (current6.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ArrowStorm);
                                        }
                                        else if (current6.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ArrowStorm_I);
                                        }
                                        else if (current6.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ArrowStorm_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_ArrowStorm_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (flag43)
                    {
                        //Log.Message("Loading Ranger Abilities");
                        //this.AddPawnAbility(TorannMagicDefOf.TM_RangerTraining);
                        //this.AddPawnAbility(TorannMagicDefOf.TM_BowTraining);
                        this.AddPawnAbility(TorannMagicDefOf.TM_PoisonTrap);
                        this.AddPawnAbility(TorannMagicDefOf.TM_AnimalFriend);
                    }

                    bool flag44 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Faceless);
                    if (flag44)
                    {
                        bool flag21 = !this.MightData.MightPowersF.NullOrEmpty<MightPower>();
                        if (flag21)
                        {
                            foreach (MightPower current7 in this.MightData.MightPowersF)
                            {
                                bool flag22 = current7.abilityDef != null;
                                if (flag22)
                                {
                                    if ((current7.abilityDef == TorannMagicDefOf.TM_Transpose || current7.abilityDef == TorannMagicDefOf.TM_Transpose_I || current7.abilityDef == TorannMagicDefOf.TM_Transpose_II || current7.abilityDef == TorannMagicDefOf.TM_Transpose_III))
                                    {
                                        if (current7.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Transpose);
                                        }
                                        else if (current7.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Transpose_I);
                                        }
                                        else if (current7.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Transpose_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Transpose_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (flag44)
                    {
                        //Log.Message("Loading Faceless Abilities");
                        this.AddPawnAbility(TorannMagicDefOf.TM_Disguise);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Mimic);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Reversal);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Possess);
                    }

                    bool flag45 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic);
                    if (flag45)
                    {
                        bool flag21 = !this.MightData.MightPowersP.NullOrEmpty<MightPower>();
                        if (flag21)
                        {
                            foreach (MightPower current8 in this.MightData.MightPowersP)
                            {
                                bool flag22 = current8.abilityDef != null;
                                if (flag22)
                                {
                                    if ((current8.abilityDef == TorannMagicDefOf.TM_PsionicBlast || current8.abilityDef == TorannMagicDefOf.TM_PsionicBlast_I || current8.abilityDef == TorannMagicDefOf.TM_PsionicBlast_II || current8.abilityDef == TorannMagicDefOf.TM_PsionicBlast_III))
                                    {
                                        if (current8.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_PsionicBlast);
                                        }
                                        else if (current8.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_PsionicBlast_I);
                                        }
                                        else if (current8.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_PsionicBlast_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_PsionicBlast_III);
                                        }
                                    }
                                    if ((current8.abilityDef == TorannMagicDefOf.TM_PsionicBarrier || current8.abilityDef == TorannMagicDefOf.TM_PsionicBarrier_Projected))
                                    {
                                        if (current8.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_PsionicBarrier);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_PsionicBarrier_Projected);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (flag45)
                    {
                        //Log.Message("Loading Psionic Abilities");
                        //this.AddPawnAbility(TorannMagicDefOf.TM_PsionicBarrier);
                        this.AddPawnAbility(TorannMagicDefOf.TM_PsionicDash);
                        this.AddPawnAbility(TorannMagicDefOf.TM_PsionicStorm);
                    }

                    bool flag46 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.DeathKnight);
                    if (flag46)
                    {
                        bool flag21 = !this.MightData.MightPowersDK.NullOrEmpty<MightPower>();
                        if (flag21)
                        {
                            foreach (MightPower current9 in this.MightData.MightPowersDK)
                            {
                                bool flag22 = current9.abilityDef != null;
                                if (flag22)
                                {
                                    if ((current9.abilityDef == TorannMagicDefOf.TM_Spite || current9.abilityDef == TorannMagicDefOf.TM_Spite_I || current9.abilityDef == TorannMagicDefOf.TM_Spite_II || current9.abilityDef == TorannMagicDefOf.TM_Spite_III))
                                    {
                                        if (current9.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Spite);
                                        }
                                        else if (current9.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Spite_I);
                                        }
                                        else if (current9.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Spite_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_Spite_III);
                                        }
                                    }
                                    if ((current9.abilityDef == TorannMagicDefOf.TM_GraveBlade || current9.abilityDef == TorannMagicDefOf.TM_GraveBlade_I || current9.abilityDef == TorannMagicDefOf.TM_GraveBlade_II || current9.abilityDef == TorannMagicDefOf.TM_GraveBlade_III))
                                    {
                                        if (current9.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_GraveBlade);
                                        }
                                        else if (current9.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_GraveBlade_I);
                                        }
                                        else if (current9.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_GraveBlade_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_GraveBlade_III);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (flag46)
                    {
                        //Log.Message("Loading Death Knight Abilities");
                        this.AddPawnAbility(TorannMagicDefOf.TM_WaveOfFear);
                    }

                    bool flag47 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Monk);
                    if (flag47)
                    {
                        //Log.Message("Loading Monk Abilities");
                        //this.AddPawnAbility(TorannMagicDefOf.TM_Chi);
                        this.AddPawnAbility(TorannMagicDefOf.TM_ChiBurst);
                        //this.AddPawnAbility(TorannMagicDefOf.TM_MindOverBody);
                        this.AddPawnAbility(TorannMagicDefOf.TM_Meditate);
                        this.AddPawnAbility(TorannMagicDefOf.TM_TigerStrike);
                        this.AddPawnAbility(TorannMagicDefOf.TM_DragonStrike);
                        this.AddPawnAbility(TorannMagicDefOf.TM_ThunderStrike);
                    }

                    bool flag48 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_Commander);
                    if (flag48)
                    {
                        // Log.Message("Loading Commander Abilities");
                        this.AddPawnAbility(TorannMagicDefOf.TM_ProvisionerAura);
                        this.AddPawnAbility(TorannMagicDefOf.TM_TaskMasterAura);
                        this.AddPawnAbility(TorannMagicDefOf.TM_CommanderAura);
                    }
                    if (flag48)
                    {
                        bool flag14 = !this.MightData.MightPowersC.NullOrEmpty<MightPower>();
                        if (flag14)
                        {
                            foreach (MightPower current10 in this.MightData.MightPowersC)
                            {
                                bool flag15 = current10.abilityDef != null;
                                if (flag15)
                                {
                                    if ((current10.abilityDef == TorannMagicDefOf.TM_StayAlert || current10.abilityDef == TorannMagicDefOf.TM_StayAlert_I || current10.abilityDef == TorannMagicDefOf.TM_StayAlert_II || current10.abilityDef == TorannMagicDefOf.TM_StayAlert_III))
                                    {
                                        if (current10.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_StayAlert);
                                        }
                                        else if (current10.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_StayAlert_I);
                                        }
                                        else if (current10.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_StayAlert_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_StayAlert_III);
                                        }
                                    }
                                    if ((current10.abilityDef == TorannMagicDefOf.TM_MoveOut || current10.abilityDef == TorannMagicDefOf.TM_MoveOut_I || current10.abilityDef == TorannMagicDefOf.TM_MoveOut_II || current10.abilityDef == TorannMagicDefOf.TM_MoveOut_III))
                                    {
                                        if (current10.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_MoveOut);
                                        }
                                        else if (current10.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_MoveOut_I);
                                        }
                                        else if (current10.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_MoveOut_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_MoveOut_III);
                                        }
                                    }
                                    if ((current10.abilityDef == TorannMagicDefOf.TM_HoldTheLine || current10.abilityDef == TorannMagicDefOf.TM_HoldTheLine_I || current10.abilityDef == TorannMagicDefOf.TM_HoldTheLine_II || current10.abilityDef == TorannMagicDefOf.TM_HoldTheLine_III))
                                    {
                                        if (current10.level == 0)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_HoldTheLine);
                                        }
                                        else if (current10.level == 1)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_HoldTheLine_I);
                                        }
                                        else if (current10.level == 2)
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_HoldTheLine_II);
                                        }
                                        else
                                        {
                                            base.AddPawnAbility(TorannMagicDefOf.TM_HoldTheLine_III);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    bool flag49 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier);
                    if (flag49)
                    {
                        //Log.Message("Loading Super Soldier Abilities");
                        //this.AddPawnAbility(TorannMagicDefOf.TM_CQC);
                        this.AddPawnAbility(TorannMagicDefOf.TM_FirstAid);
                        this.AddPawnAbility(TorannMagicDefOf.TM_60mmMortar);                        
                    }
                }
                if (this.equipmentContainer != null && this.equipmentContainer.Count > 0)
                {
                    //Thing outThing = new Thing();
                    try
                    {
                        //Log.Message("primary is " + this.Pawn.equipment.Primary);
                        //Log.Message("equipment container is " + this.equipmentContainer[0]);                        
                        for (int i = 0; i < this.Pawn.equipment.AllEquipmentListForReading.Count; i++)
                        {
                            ThingWithComps t = this.Pawn.equipment.AllEquipmentListForReading[i];
                            if (t.def.defName.Contains("Spec_Base"))
                            {
                                t.Destroy(DestroyMode.Vanish);
                            }
                        }
                        if(ModCheck.Validate.SimpleSidearms.IsInitialized())
                        {
                            ModCheck.SS.ClearWeaponMemory(this.Pawn);
                        }
                        if (this.specWpnRegNum == -1)
                        {
                            if (this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_PistolSpec).learned)
                            {
                                TM_Action.DoAction_PistolSpecCopy(this.Pawn, this.equipmentContainer[0]);
                            }
                            else if (this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_RifleSpec).learned)
                            {
                                TM_Action.DoAction_RifleSpecCopy(this.Pawn, this.equipmentContainer[0]);
                            }
                            else if (this.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_ShotgunSpec).learned)
                            {
                                TM_Action.DoAction_ShotgunSpecCopy(this.Pawn, this.equipmentContainer[0]);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Message("exception on load: " + ex);
                        //do nothing
                    }
                }
                this.UpdateAutocastDef();
                this.InitializeSkill();
                //base.UpdateAbilities();
            }            
        }

        public void UpdateAutocastDef()
        {
            IEnumerable <TM_CustomPowerDef> mpDefs = TM_Data.CustomFighterPowerDefs();
            if (this.IsMightUser && this.MightData != null && this.MightData.MightPowersCustom != null)
            {
                foreach (MightPower mp in this.MightData.MightPowersCustom)
                {
                    foreach (TM_CustomPowerDef mpDef in mpDefs)
                    {
                        if (mpDef.customPower.abilityDefs.FirstOrDefault().ToString() == mp.GetAbilityDef(0).ToString())
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
                if (key == "wayfarer")
                {
                    Command_Action itemWayfarer = new Command_Action
                    {

                        action = new Action(delegate
                        {
                            TM_Action.PromoteWayfarer(p);
                        }),
                        order = 52,
                        defaultLabel = TM_TextPool.TM_PromoteWayfarer,
                        defaultDesc = TM_TextPool.TM_PromoteWayfarerDesc,
                        icon = ContentFinder<Texture2D>.Get("UI/wayfarer", true),
                    };
                    gizmoCommands.Add(key, itemWayfarer);
                }
                if(key == "cleave")
                {
                    String toggle = "cleave";
                    String label = "TM_CleaveEnabled".Translate();
                    String desc = "TM_CleaveToggleDesc".Translate();
                    if (!this.useCleaveToggle)
                    {
                        toggle = "cleavetoggle_off";
                        label = "TM_CleaveDisabled".Translate();
                    }
                    Command_Toggle itemCleave = new Command_Toggle
                    {
                        defaultLabel = label,
                        defaultDesc = desc,
                        order = -90,
                        icon = ContentFinder<Texture2D>.Get("UI/" + toggle, true),
                        isActive = (() => this.useCleaveToggle),
                        toggleAction = delegate
                        {
                            this.useCleaveToggle = !this.useCleaveToggle;
                        }
                    };
                    gizmoCommands.Add(key, itemCleave);
                }
                if(key == "cqc")
                {
                    String toggle = "cqc";
                    String label = "TM_CQCEnabled".Translate();
                    String desc = "TM_CQCToggleDesc".Translate();
                    if (!this.useCQCToggle)
                    {
                        //toggle = "cqc_off";
                        label = "TM_CQCDisabled".Translate();
                    }
                    Command_Toggle itemCQC = new Command_Toggle
                    {
                        defaultLabel = label,
                        defaultDesc = desc,
                        order = -90,
                        icon = ContentFinder<Texture2D>.Get("UI/" + toggle, true),
                        isActive = (() => this.useCQCToggle),
                        toggleAction = delegate
                        {
                            this.useCQCToggle = !this.useCQCToggle;
                        }
                    };
                    gizmoCommands.Add(key, itemCQC);
                }
                if(key == "psiAugmentation")
                {
                    String toggle = "psionicaugmentation";
                    String label = "TM_AugmentationsEnabled".Translate();
                    String desc = "TM_AugmentationsToggleDesc".Translate();
                    if (!this.usePsionicAugmentationToggle)
                    {
                        toggle = "psionicaugmentation_off";
                        label = "TM_AugmentationsDisabled".Translate();
                    }
                    Command_Toggle item = new Command_Toggle
                    {
                        defaultLabel = label,
                        defaultDesc = desc,
                        order = -90,
                        icon = ContentFinder<Texture2D>.Get("UI/" + toggle, true),
                        isActive = (() => this.usePsionicAugmentationToggle),
                        toggleAction = delegate
                        {
                            this.usePsionicAugmentationToggle = !this.usePsionicAugmentationToggle;
                        }
                    };
                    gizmoCommands.Add(key, item);
                }
                if(key == "psiMindAttack")
                {

                    String toggle2 = "psionicmindattack";
                    String label2 = "TM_MindAttackEnabled".Translate();
                    String desc2 = "TM_MindAttackToggleDesc".Translate();
                    if (!this.usePsionicMindAttackToggle)
                    {
                        toggle2 = "psionicmindattack_off";
                        label2 = "TM_MindAttackDisabled".Translate();
                    }
                    Command_Toggle item2 = new Command_Toggle
                    {
                        defaultLabel = label2,
                        defaultDesc = desc2,
                        order = -89,
                        icon = ContentFinder<Texture2D>.Get("UI/" + toggle2, true),
                        isActive = (() => this.usePsionicMindAttackToggle),
                        toggleAction = delegate
                        {
                            this.usePsionicMindAttackToggle = !this.usePsionicMindAttackToggle;
                        }
                    };
                    gizmoCommands.Add(key, item2);
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
