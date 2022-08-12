using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace TorannMagic
{
    class HediffComp_BattleHymn : HediffComp
    {
        private bool initializing = true;
        private float chantRange = 15f;
        private int chantFrequency = 300;
        private int pwrVal = 0;
        private int verVal = 0;
        private int effVal = 0;

        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned)
            {
                CompAbilityUserMagic comp = this.Pawn.GetCompAbilityUserMagic();
                pwrVal = TM_Calc.GetSkillPowerLevel(Pawn, TorannMagicDefOf.TM_BattleHymn);
                verVal = TM_Calc.GetSkillVersatilityLevel(Pawn, TorannMagicDefOf.TM_BattleHymn);
                effVal = TM_Calc.GetSkillEfficiencyLevel(Pawn, TorannMagicDefOf.TM_BattleHymn);
                //MagicPowerSkill pwr = comp.MagicData.MagicPowerSkill_BattleHymn.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BattleHymn_pwr");
                //MagicPowerSkill ver = comp.MagicData.MagicPowerSkill_BattleHymn.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BattleHymn_ver");
                //MagicPowerSkill eff = comp.MagicData.MagicPowerSkill_BattleHymn.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BattleHymn_eff");
                //this.verVal = ver.level;
                //this.pwrVal = pwr.level;
                //this.effVal = eff.level;
                //ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                //if (settingsRef.AIHardMode && !this.Pawn.IsColonist)
                //{
                //    pwrVal = 1;
                //    verVal = 1;
                //    effVal = 1;
                //}
                this.chantRange = this.chantRange + (this.verVal * 3f);
                this.chantFrequency = 300 - (30 * verVal);
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (initializing)
                {
                    initializing = false;
                    this.Initialize();
                }
            }
            Map map = base.Pawn.Map;

            bool flag4 = Find.TickManager.TicksGame % chantFrequency == 0;
            if (flag4 && map != null)
            {
                CompAbilityUserMagic comp = this.Pawn.GetCompAbilityUserMagic();
                if (comp.Mana.CurLevel > (.09f - (.009f * effVal)))
                {
                    List<Pawn> pawns = this.Pawn.Map.mapPawns.AllPawnsSpawned;
                    for (int i = 0; i < pawns.Count; i++)
                    {
                        if (pawns[i].RaceProps.Humanlike && pawns[i].Faction != null && pawns[i].Faction == this.Pawn.Faction)
                        {
                            if ((pawns[i].Position - this.Pawn.Position).LengthHorizontal <= this.chantRange)
                            {
                                HealthUtility.AdjustSeverity(pawns[i], HediffDef.Named("TM_BattleHymnHD"), Rand.Range(.4f, .7f) + (.15f * pwrVal));
                                TM_MoteMaker.ThrowNoteMote(pawns[i].DrawPos, pawns[i].Map, .3f);
                                TM_MoteMaker.ThrowNoteMote(pawns[i].DrawPos, pawns[i].Map, .2f);
                                if(Rand.Chance(.04f + (.01f * pwrVal)))
                                {
                                    List<InspirationDef> id = new List<InspirationDef>();
                                    id.Add(TorannMagicDefOf.ID_Champion); id.Add(TorannMagicDefOf.ID_ManaRegen); id.Add(TorannMagicDefOf.Frenzy_Go); id.Add(TorannMagicDefOf.Frenzy_Shoot);
                                    pawns[i].mindState.inspirationHandler.TryStartInspiration(id.RandomElement());
                                }
                            }
                        }
                    }
                    comp.Mana.CurLevel -= (.09f - (.009f * effVal));
                    TM_MoteMaker.ThrowSiphonMote(this.Pawn.DrawPos, this.Pawn.Map, .5f);
                }
                else
                {
                    Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_SingBattleHymnHD"), false);
                    this.Pawn.health.RemoveHediff(hediff);
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.effVal, "effVal", 0, false);
            Scribe_Values.Look<int>(ref this.chantFrequency, "chantFrequency", 300, false);
            Scribe_Values.Look<float>(ref this.chantRange, "chantRange", 11f, false);
        }
    }
}
