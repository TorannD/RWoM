using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic
{
    public partial class CompAbilityUserMagic
    {
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
            DrawMark(TM_RenderQueue.enchantMark, new Vector3(.5f, 1f, .5f), 0, -.2f);
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
                    
                    this.deathRetaliating = true;
                    this.ticksTillRetaliation = Mathf.RoundToInt(Rand.Range(400, 1200) * ModOptions.Settings.Instance.deathRetaliationDelayFactor);
                    this.deathRing = TM_Calc.GetOuterRing(this.Pawn.Position, 1f, 2f);
                }
                else
                {
                    deathRetaliationDelayCount++;
                }
            }
        }
        
        private void AssignAbilities()
        {
            
            float hardModeMasterChance = .35f;
            float masterChance = .05f;
            Pawn abilityUser = base.Pawn;
            bool flag2;
            List<TMAbilityDef> usedAbilities = new List<TMAbilityDef>();
            usedAbilities.Clear();
            if (abilityUser != null && abilityUser.story != null && abilityUser.story.traits != null)
            {
                if (this.customClass != null)
                {
                    for (int z = 0; z < this.MagicData.AllMagicPowers.Count; z++)
                    {
                        TMAbilityDef ability = (TMAbilityDef)this.MagicData.AllMagicPowers[z].abilityDef;
                        if (usedAbilities.Contains(ability))
                        {
                            continue;
                        }
                        else
                        {
                            usedAbilities.Add(ability);
                        }
                        if (this.customClass.classMageAbilities.Contains(ability))
                        {
                            this.MagicData.AllMagicPowers[z].learned = true;
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
                            this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Firestorm).learned = false;
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
                            this.MagicData.MagicPowersIF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Firestorm).learned = false;

                            if (!abilityUser.IsColonist)
                            {
                                if ((ModOptions.Settings.Instance.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
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
                            this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Blizzard).learned = false;
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
                            this.MagicData.MagicPowersHoF.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Blizzard).learned = false;

                            if (!abilityUser.IsColonist)
                            {
                                if ((ModOptions.Settings.Instance.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
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
                            this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EyeOfTheStorm).learned = false;
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
                            this.MagicData.MagicPowersSB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EyeOfTheStorm).learned = false;

                            if (!abilityUser.IsColonist)
                            {
                                if ((ModOptions.Settings.Instance.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
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
                            this.MagicData.MagicPowersA.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_FoldReality).learned = false;
                        }
                        else
                        {
                            for(int i = 0; i < this.MagicData.MagicPowersA.Count; i++)
                            {
                                if (this.MagicData.magicPowerA[i].abilityDef != TorannMagicDefOf.TM_FoldReality)
                                {
                                    this.MagicData.MagicPowersA[i].learned = true;
                                }
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
                            this.MagicData.MagicPowersP.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_HolyWrath).learned = false;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersP.Count; i++)
                            {
                                if (this.MagicData.MagicPowersP[i].abilityDef != TorannMagicDefOf.TM_HolyWrath)
                                {
                                    this.MagicData.MagicPowersP[i].learned = true;
                                }
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_Heal);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Shield);
                            this.AddPawnAbility(TorannMagicDefOf.TM_ValiantCharge);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Overwhelm);
                            this.AddPawnAbility(TorannMagicDefOf.TM_P_RayofHope);
                            this.spell_Heal = true;

                            if (!abilityUser.IsColonist)
                            {
                                if ((ModOptions.Settings.Instance.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
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
                            this.MagicData.MagicPowersS.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonPoppi).learned = false;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersS.Count; i++)
                            {
                                if (this.MagicData.MagicPowersS[i].abilityDef != TorannMagicDefOf.TM_SummonPoppi)
                                {
                                    this.MagicData.MagicPowersS[i].learned = true;
                                }
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_SummonMinion);
                            this.AddPawnAbility(TorannMagicDefOf.TM_SummonPylon);
                            this.AddPawnAbility(TorannMagicDefOf.TM_SummonExplosive);
                            this.AddPawnAbility(TorannMagicDefOf.TM_SummonElemental);
                            this.spell_SummonMinion = true;

                            if (!abilityUser.IsColonist)
                            {
                                if ((ModOptions.Settings.Instance.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
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
                            this.MagicData.MagicPowersD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RegrowLimb).learned = false;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersD.Count; i++)
                            {
                                if (this.MagicData.MagicPowersD[i].abilityDef != TorannMagicDefOf.TM_RegrowLimb)
                                {
                                    this.MagicData.MagicPowersD[i].learned = true;
                                }
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
                            this.MagicData.MagicPowersN.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LichForm).learned = false;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersN.Count; i++)
                            {
                                if (this.MagicData.MagicPowersN[i].abilityDef != TorannMagicDefOf.TM_LichForm)
                                {
                                    this.MagicData.MagicPowersN[i].learned = true;
                                }
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_RaiseUndead);
                            this.AddPawnAbility(TorannMagicDefOf.TM_DeathMark);
                            this.AddPawnAbility(TorannMagicDefOf.TM_FogOfTorment);
                            this.AddPawnAbility(TorannMagicDefOf.TM_ConsumeCorpse);
                            this.AddPawnAbility(TorannMagicDefOf.TM_CorpseExplosion);

                            if (!abilityUser.IsColonist)
                            {
                                if ((ModOptions.Settings.Instance.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
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
                            this.MagicData.MagicPowersPR.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Resurrection).learned = false;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersPR.Count; i++)
                            {
                                if (this.MagicData.MagicPowersPR[i].abilityDef != TorannMagicDefOf.TM_Resurrection)
                                {
                                    this.MagicData.MagicPowersPR[i].learned = true;
                                }
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
                            this.MagicData.MagicPowersB.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BattleHymn).learned = false;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersB.Count; i++)
                            {
                                if (this.MagicData.MagicPowersB[i].abilityDef != TorannMagicDefOf.TM_BattleHymn)
                                {
                                    this.MagicData.MagicPowersB[i].learned = true;
                                }
                            }
                            //this.AddPawnAbility(TorannMagicDefOf.TM_BardTraining);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Entertain);
                            //this.AddPawnAbility(TorannMagicDefOf.TM_Inspire);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Lullaby);

                            if (!abilityUser.IsColonist)
                            {
                                if ((ModOptions.Settings.Instance.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
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
                            this.MagicData.MagicPowersSD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Scorn).learned = false;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersSD.Count; i++)
                            {
                                if (this.MagicData.MagicPowersSD[i].abilityDef != TorannMagicDefOf.TM_Scorn)
                                {
                                    this.MagicData.MagicPowersSD[i].learned = true;
                                }
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_SoulBond);
                            this.AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Dominate);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Attraction);

                            if (!abilityUser.IsColonist)
                            {
                                if ((ModOptions.Settings.Instance.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
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
                            this.MagicData.MagicPowersWD.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_PsychicShock).learned = false;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersWD.Count; i++)
                            {
                                if (this.MagicData.MagicPowersWD[i].abilityDef != TorannMagicDefOf.TM_PsychicShock)
                                {
                                    this.MagicData.MagicPowersWD[i].learned = true;
                                }
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_SoulBond);
                            this.AddPawnAbility(TorannMagicDefOf.TM_ShadowBolt);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Dominate);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Repulsion);
                            if (!abilityUser.IsColonist)
                            {
                                if ((ModOptions.Settings.Instance.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
                                {
                                    this.spell_PsychicShock = true;
                                }
                            }
                        }
                    }
                    flag2 = abilityUser.story.traits.HasTrait(TorannMagicDefOf.Geomancer);
                    if (flag2)
                    {
                        //Log.Message("Initializing Heart of Geomancer Abilities");
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
                            this.MagicData.MagicPowersG.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Meteor).learned = false;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersG.Count; i++)
                            {
                                if (!this.MagicData.MagicPowersG[i].abilityDef.defName.StartsWith("TM_Meteor"))
                                {
                                    this.MagicData.MagicPowersG[i].learned = true;
                                }
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_Stoneskin);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Encase);
                            this.AddPawnAbility(TorannMagicDefOf.TM_EarthSprites);
                            this.AddPawnAbility(TorannMagicDefOf.TM_EarthernHammer);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Sentinel);

                            if (!abilityUser.IsColonist)
                            {
                                if ((ModOptions.Settings.Instance.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
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
                            this.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_OrbitalStrike).learned = false;
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
                                if ((ModOptions.Settings.Instance.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
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
                            this.MagicData.MagicPowersBM.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BloodMoon).learned = false;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersBM.Count; i++)
                            {
                                if (!this.MagicData.MagicPowersBM[i].abilityDef.defName.StartsWith("TM_BloodMoon"))
                                {
                                    this.MagicData.MagicPowersBM[i].learned = true;
                                }
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_BloodGift);
                            this.AddPawnAbility(TorannMagicDefOf.TM_IgniteBlood);
                            this.AddPawnAbility(TorannMagicDefOf.TM_BloodForBlood);
                            this.AddPawnAbility(TorannMagicDefOf.TM_BloodShield);
                            this.AddPawnAbility(TorannMagicDefOf.TM_Rend);
                            if (!abilityUser.IsColonist)
                            {
                                if ((ModOptions.Settings.Instance.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
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
                            this.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shapeshift).learned = false;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersE.Count; i++)
                            {
                                if (this.MagicData.MagicPowersE[i].abilityDef != TorannMagicDefOf.TM_Shapeshift)
                                {
                                    this.MagicData.MagicPowersE[i].learned = true;
                                }
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
                            this.MagicData.MagicPowersC.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Recall).learned = false;
                        }
                        else
                        {
                            for (int i = 0; i < this.MagicData.MagicPowersC.Count; i++)
                            {
                                if (this.MagicData.MagicPowersC[i].abilityDef == TorannMagicDefOf.TM_Recall)
                                {
                                    this.MagicData.MagicPowersC[i].learned = true;
                                }
                            }
                            this.AddPawnAbility(TorannMagicDefOf.TM_Prediction);
                            this.AddPawnAbility(TorannMagicDefOf.TM_AlterFate);
                            this.AddPawnAbility(TorannMagicDefOf.TM_AccelerateTime);
                            this.AddPawnAbility(TorannMagicDefOf.TM_ReverseTime);
                            this.AddPawnAbility(TorannMagicDefOf.TM_ChronostaticField);

                            if (!abilityUser.IsColonist)
                            {
                                if ((ModOptions.Settings.Instance.AIHardMode && Rand.Chance(hardModeMasterChance)) || Rand.Chance(masterChance))
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
                AssignAdvancedClassAbilities(true);
            }
        }
        
        public void InitializeSpell()
        {
            Pawn abilityUser = base.Pawn;
            if (this.IsMagicUser)
            {
                if (this.customClass != null)
                {
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
                        this.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_FoldReality).learned = true;
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
                    CompAbilityUserMight compMight = this.Pawn.GetCompAbilityUserMight();
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
            if (this.Pawn.Map != null && this.Pawn.Map.gameConditionManager.ConditionIsActive(TorannMagicDefOf.TM_ManaStorm))
            {
                return Mathf.Max(adjustedManaCost *.5f, 0f);
            }
            return Mathf.Max(adjustedManaCost, (.5f * magicDef.manaCost));
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
    }
}