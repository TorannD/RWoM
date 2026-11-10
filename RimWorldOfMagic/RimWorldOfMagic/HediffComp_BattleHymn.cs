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
        private int lastVerse = -1;

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
            pwrVal = TM_Calc.GetSkillPowerLevel(Pawn, TorannMagicDefOf.TM_BattleHymn);
            verVal = TM_Calc.GetSkillVersatilityLevel(Pawn, TorannMagicDefOf.TM_BattleHymn);
            effVal = TM_Calc.GetSkillEfficiencyLevel(Pawn, TorannMagicDefOf.TM_BattleHymn);
            chantRange = 15f + verVal * 3f;
            chantFrequency = Math.Max(30, 300 - 30 * verVal);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            int now = Find.TickManager.TicksGame;
            if (now % 30 != 0 || now == lastVerse) return;
            var body = ClassProgression.BardTrials.HymnBody(Pawn);
            if (body == null) return;
            if (initializing)
            {
                Initialize();
                initializing = false;
            }
            if (now % chantFrequency == 0)
            {
                lastVerse = now;
                CompAbilityUserMagic comp = this.Pawn.GetCompAbilityUserMagic();
                if (comp?.Mana != null && comp.Mana.CurLevel > Math.Max(0f, .09f - .009f * effVal))
                {
                    List<Pawn> pawns = body.Map.mapPawns.AllPawnsSpawned.ToList();
                    for (int i = 0; i < pawns.Count; i++)
                    {
                        if (pawns[i].RaceProps.Humanlike && pawns[i].Faction != null && pawns[i].Faction == body.Faction)
                        {
                            if ((pawns[i].Position - body.Position).LengthHorizontal <= this.chantRange)
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
                    comp.Mana.CurLevel -= Math.Max(0f, .09f - .009f * effVal);
                    TM_MoteMaker.ThrowSiphonMote(body.DrawPos, body.Map, .5f);
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
            Scribe_Values.Look(ref lastVerse, "TM_lastHymnVerse", -1);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.effVal, "effVal", 0, false);
            Scribe_Values.Look<int>(ref this.chantFrequency, "chantFrequency", 300, false);
            Scribe_Values.Look<float>(ref this.chantRange, "chantRange", 11f, false);
        }
    }
}
