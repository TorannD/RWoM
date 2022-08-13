using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using RimWorld;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    internal class Gizmo_EnergyStatus : Gizmo
    {
        //public HediffComp_Shield shield;

        private static readonly Texture2D FullStaminaTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.0f, 0.5f, 0.0f));
        private static readonly Texture2D FullManaTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.55f, 0.03f, 1f));
        private static readonly Texture2D FullPsionicTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.0f, 0.5f, 1f));
        private static readonly Texture2D FullDeathKnightTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.6f, 0.0f, 0f));
        private static readonly Texture2D FullBloodMageTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.4f, 0.0f, 0f));
        private static readonly Texture2D FullChiTex = SolidColorMaterials.NewSolidColorTexture(new Color(1, .75f, 0));
        private static readonly Texture2D FullCountTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
        private static readonly Texture2D FullNecroticTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.32f, 0.4f, 0.0f));
        private static readonly Texture2D FullBrightmageTex = SolidColorMaterials.NewSolidColorTexture(new Color(1f, .95f, .9f));
        private static readonly Texture2D FullSoLTex = SolidColorMaterials.NewSolidColorTexture(new Color(.9f, .8f, .2f));
        private static readonly Texture2D FullSpiritTex = SolidColorMaterials.NewSolidColorTexture(new Color(0f, 0.5f, .5f));

        private static Texture2D CustomTex;

        private static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public Pawn pawn;
        public Enchantment.CompEnchantedItem iComp = null;
        HediffWithCompsExtra customHediff = null;           //must use custom hediff

        public override float GetWidth(float maxWidth)
        {
            return 100f;            
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            if (!pawn.DestroyedOrNull() && !pawn.Dead)
            {
                CompAbilityUserMagic compMagic = pawn.GetCompAbilityUserMagic();
                CompAbilityUserMight compMight = pawn.GetCompAbilityUserMight();

                bool isMage = compMagic.IsMagicUser && !pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless);
                bool isFighter = compMight.IsMightUser;
                bool isPsionic = pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_PsionicHD"), false);
                bool isBloodMage = pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_BloodHD"), false);
                bool isBrightmage = pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_LightCapacitanceHD);
                bool isMonk = pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ChiHD, false);
                bool isSpirit = TM_Calc.IsPossessedByOrIsSpirit(pawn);
                bool isEnchantedItem = this.iComp != null;
                bool isCustom = false;

                if (customHediff == null || Find.TickManager.TicksGame % 303 == 0)
                {
                    if (isMage && compMagic.customClass != null && compMagic.customClass.classHediff != null && compMagic.customClass.showHediffOnGizmo)
                    {
                        isCustom = true;
                        customHediff = pawn.health.hediffSet.GetFirstHediffOfDef(compMagic.customClass.classHediff) as HediffWithCompsExtra;
                        CustomTex = SolidColorMaterials.NewSolidColorTexture(compMagic.customClass.classIconColor);
                    }
                    else if (isFighter && compMight.customClass != null && compMight.customClass.classHediff != null && compMight.customClass.showHediffOnGizmo)
                    {
                        isCustom = true;
                        customHediff = pawn.health.hediffSet.GetFirstHediffOfDef(compMight.customClass.classHediff) as HediffWithCompsExtra;
                        CustomTex = SolidColorMaterials.NewSolidColorTexture(compMight.customClass.classIconColor);
                    }
                }
                
                Hediff hediff = null;
                for (int h = 0; h < pawn.health.hediffSet.hediffs.Count; h++)
                {
                    if (pawn.health.hediffSet.hediffs[h].def.defName.Contains("TM_HateHD"))
                    {
                        hediff = pawn.health.hediffSet.hediffs[h];
                    }
                }
                bool isDeathKnight = hediff != null;
                //bool isLich = pawn.story.traits.HasTrait(TorannMagicDefOf.Lich);
                float barCount = 0;
                float boostPsiSev = 100;
                float boostHateSev = 100;
                float boostBloodSev = 100;
                float boostChiSev = 100;
                if (isFighter)
                {
                    barCount++;
                }
                if (isMage)
                {
                    barCount++;
                }
                if (isPsionic)
                {
                    barCount++;
                    Hediff hediffBoost = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_Artifact_PsionicBoostHD);
                    if (hediffBoost != null)
                    {
                        boostPsiSev += hediffBoost.Severity;
                    }
                }
                if (isDeathKnight)
                {
                    barCount++;
                    Hediff hediffBoost = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_Artifact_HateBoostHD);
                    if (hediffBoost != null)
                    {
                        boostHateSev += hediffBoost.Severity;
                    }
                }
                if (isBloodMage)
                {
                    barCount++;
                    Hediff hediffBoost = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_Artifact_BloodBoostHD);
                    if (hediffBoost != null)
                    {
                        boostBloodSev += hediffBoost.Severity;
                    }
                }
                if(isSpirit)
                {
                    barCount++;                    
                }
                if(isBrightmage)
                {
                    barCount++;
                }
                if(isMonk)
                {
                    barCount++;
                }
                if(isEnchantedItem)
                {
                    barCount++;
                }
                if(isCustom)
                {
                    barCount++;
                }

                float barHeight;
                float initialShift = 0;
                float barSpacing = 0f;
                float contractionAmount = 6f;
                if (barCount == 1)
                {
                    initialShift = 15f;
                }
                else if (barCount >= 2)
                {
                    initialShift = 5f;
                    barSpacing = 2f;
                    contractionAmount -= barCount;
                }
                if (barCount > 0 && ((isFighter && compMight.Stamina != null) || (isMage && compMagic.Mana != null) || (isEnchantedItem && iComp.NecroticEnergy > 0)))
                {
                    Rect overRect = new Rect(topLeft.x + 2, topLeft.y, this.GetWidth(100), 75); //overall rect size (shell)
                    if (parms.highLight)
                    {
                        QuickSearchWidget.DrawStrongHighlight(overRect.ExpandedBy(12f));
                    }
                    Find.WindowStack.ImmediateWindow(984698, overRect, WindowLayer.GameUI, delegate
                    {
                        barHeight = (((75 - (2* contractionAmount)) - (2*initialShift) - (barSpacing * (barCount - 1))) / (barCount));
                        Rect rect = overRect.AtZero().ContractedBy(contractionAmount); //inner, smaller rect
                        rect.height = barHeight;
                        Rect rect2 = rect; //label rect, starts at top             
                        Text.Font = GameFont.Tiny;
                        float fillPercent = 0;
                        float yShift = initialShift;
                        Text.Anchor = TextAnchor.MiddleCenter;
                        if(isCustom && customHediff != null)
                        {
                            rect2.y = rect.y + yShift;
                            try
                            {
                                fillPercent = customHediff.Severity / customHediff.MaxSeverity;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.CustomTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "" + (customHediff.Severity).ToString("F0") + " / " + customHediff.MaxSeverity.ToString("F0"));
                            }
                            catch
                            {
                                fillPercent = 0f;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.CustomTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "");
                            }
                            yShift += (barHeight) + barSpacing;
                        }
                        if (isPsionic)
                        {
                            rect2.y = rect.y + yShift;
                            try
                            {
                                fillPercent = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_PsionicHD"), false).Severity / (boostPsiSev);
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullPsionicTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "" + (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_PsionicHD"), false).Severity).ToString("F0") + " / " + boostPsiSev.ToString("F0"));
                            }
                            catch
                            {
                                fillPercent = 0f;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullPsionicTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "");
                            }
                            yShift += (barHeight) + barSpacing;
                        }
                        if (isDeathKnight)
                        {
                            rect2.y = rect.y + yShift;
                            try
                            {
                                fillPercent = hediff.Severity / boostHateSev;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullDeathKnightTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "" + hediff.Severity.ToString("F0") + " / " + boostHateSev.ToString("F0"));
                            }
                            catch
                            {
                                fillPercent = 0f;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullDeathKnightTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "");
                            }
                            yShift += (barHeight) + barSpacing;
                        }                        
                        if (isMonk)
                        {
                            rect2.y = rect.y + yShift;
                            try
                            {
                                fillPercent = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD, false).Severity / boostChiSev;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullChiTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "" + (pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD, false).Severity).ToString("F0") + " / " + boostChiSev.ToString("F0"));
                            }
                            catch
                            {
                                fillPercent = 0f;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullChiTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "");
                            }
                            yShift += (barHeight) + barSpacing;
                        }
                        if (isFighter)
                        {
                            rect2.y = rect.y + yShift;
                            try
                            {
                                fillPercent = compMight.Stamina.CurInstantLevel / compMight.maxSP;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullStaminaTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "" + (compMight.Stamina.CurInstantLevel * 100).ToString("F0") + " / " + (compMight.maxSP * 100).ToString("F0"));
                            }
                            catch
                            {
                                fillPercent = 0f;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullStaminaTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "");
                            }

                            yShift += (barHeight) + barSpacing;
                        }
                        if (isBloodMage)
                        {
                            rect2.y = rect.y + yShift;
                            try
                            {
                                fillPercent = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_BloodHD"), false).Severity / boostBloodSev;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullBloodMageTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "" + (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_BloodHD"), false).Severity).ToString("F0") + " / " + boostBloodSev.ToString("F0"));
                            }
                            catch
                            {
                                fillPercent = 0f;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullBloodMageTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "");
                            }
                            yShift += (barHeight) + barSpacing;
                        }
                        if (isBrightmage)
                        {
                            rect2.y = rect.y + yShift;
                            try
                            {
                                if (compMagic.SoL != null)
                                {
                                    fillPercent = compMagic.SoL.LightEnergy / compMagic.SoL.LightEnergyMax;
                                    Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullSoLTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                    //Widgets.Label(rect2, "" + compMagic.SoL.LightEnergy.ToString("F0") + " / " + compMagic.SoL.LightEnergyMax.ToString("F0"));
                                }
                                Hediff hd = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightCapacitanceHD);
                                HediffComp_LightCapacitance hdlc = hd.TryGetComp<HediffComp_LightCapacitance>();
                                fillPercent = hdlc.LightEnergy / hdlc.LightEnergyMax;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullBrightmageTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "" + hdlc.LightEnergy.ToString("F0") + " / " + hdlc.LightEnergyMax.ToString("F0"));                                
                            }
                            catch
                            {
                                fillPercent = 0f;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullBrightmageTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "");
                            }
                            yShift += (barHeight) + barSpacing;
                        }
                        if (isMage)
                        {
                            rect2.y = rect.y + yShift;
                            try
                            {
                                fillPercent = compMagic.Mana.CurInstantLevel / compMagic.maxMP;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullManaTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "" + (compMagic.Mana.CurInstantLevel * 100).ToString("F0") + " / " + (compMagic.maxMP * 100).ToString("F0"));
                            }
                            catch
                            {
                                fillPercent = 0f;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullManaTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "");
                            }
                            yShift += (barHeight) + barSpacing;
                        }
                        if (isSpirit)
                        {
                            rect2.y = rect.y + yShift;
                            Need_Spirit nd = pawn.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND) as Need_Spirit;
                            try
                            {
                                fillPercent = nd.CurLevel / nd.MaxLevel;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullSpiritTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "" + (nd.CurLevel).ToString("F0") + " / " + (nd.MaxLevel).ToString("F0"));
                            }
                            catch
                            {
                                fillPercent = 0f;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullSpiritTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "");
                            }
                            yShift += (barHeight) + barSpacing;
                        }
                        if (isEnchantedItem)
                        {
                            rect2.y = rect.y + yShift;
                            try
                            {
                                fillPercent = iComp.NecroticEnergy / 100f;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullNecroticTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "" + (iComp.NecroticEnergy).ToString("F0") + " / " + (100).ToString("F0"));
                            }
                            catch
                            {
                                fillPercent = 0f;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullNecroticTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "");
                            }
                            yShift += (barHeight) + barSpacing;
                        }
                        Text.Font = GameFont.Small;
                        Text.Anchor = TextAnchor.UpperLeft;
                    }, true, false, 1f);
                }
            }
            else
            {
                Rect overRect = new Rect(topLeft.x + 2, topLeft.y, this.GetWidth(100), 75); //overall rect size (shell)
                float barHeight;
                float initialShift = 0;
                Find.WindowStack.ImmediateWindow(984698, overRect, WindowLayer.GameUI, delegate
                {
                    barHeight = ((75 - 5) / 1);
                    Rect rect = overRect.AtZero().ContractedBy(6f); //inner, smaller rect
                    rect.height = barHeight;
                    Rect rect2 = rect; //label rect, starts at top             
                    Text.Font = GameFont.Tiny;
                    float fillPercent = 0;
                    float yShift = initialShift;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    rect2.y += yShift;
                    fillPercent = 0f;
                    Widgets.FillableBar(rect2, fillPercent, Gizmo_EnergyStatus.FullPsionicTex, Gizmo_EnergyStatus.EmptyShieldBarTex, false);
                    Widgets.Label(rect2, "" );
                    yShift += (barHeight) + 5f;
                }, true, false, 1f);
            }
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
