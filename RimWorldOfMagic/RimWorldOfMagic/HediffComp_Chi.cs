using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_Chi : HediffComp
    {

        private bool initialized = false;
        private bool removeNow = false;
        private int lastProcessedTick = -1;

        private int eventFrequency = 120;
        private int chiFrequency = 4;
        private int lastChiTick = 0;
        private float lastChi = 0;

        public float maxSev = 100f;

        private int pwrVal = 0;
        private int verVal = 0;
        private int effVal = 0;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastProcessedTick, "chiProcessedTick", -1);
        }

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
            CompAbilityUserMight comp = this.Pawn.GetCompAbilityUserMight();

            if (comp != null && (comp.IsMightUser || ClassProgression.MonkMartialPath.IsMonk(Pawn)))
            {
                pwrVal = comp.MightData.MightPowerSkill_Chi.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Chi_pwr")?.level ?? 0;
                verVal = comp.MightData.MightPowerSkill_Chi.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Chi_ver")?.level ?? 0;
                effVal = comp.MightData.MightPowerSkill_Chi.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Chi_eff")?.level ?? 0;
            }
            else
            {
                this.removeNow = true;
            }
        } 

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            var body = PhysicalBody();
            if (body == null) return;
            int now = Find.TickManager.TicksGame;
            if (now <= lastProcessedTick) return;
            lastProcessedTick = now;
            bool flag = body.Map != null;
            if (flag)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }                
            }
            if(initialized && Find.TickManager.TicksGame % this.eventFrequency == 0)
            {
                if (body.IsColonist)
                {
                    severityAdjustment -= (Rand.Range(.03f, .05f) - (.008f * verVal));
                }
                else if(body.IsPrisoner)
                {
                    severityAdjustment -= (Rand.Range(.25f, .5f) - (.00375f * verVal));
                }
                else
                {
                    severityAdjustment += 2f;
                }
            }
            this.parent.Severity = Mathf.Clamp(this.parent.Severity, 0, this.maxSev);
        }

        private Pawn PhysicalBody()
        {
            if (!ClassProgression.SpiritRebirthEnergy.Marked(Pawn)) return Pawn;
            if (!ClassProgression.SpiritRebirthState.TryGet(Pawn, out var state) || state.DepletionPending ||
                state.Host is not Pawn body || !state.HasValidHost(Pawn) ||
                ClassProgression.MonkMartialPath.Owner(body) != Pawn) return null;
            return body;
        }

        internal static void TickHosted(Pawn soul)
        {
            var chi = ClassProgression.MonkMartialPath.Chi(soul);
            var tracker = chi?.TryGetComp<HediffComp_Chi>();
            if (tracker == null) return;
            float adjustment = 0;
            tracker.CompPostTick(ref adjustment);
            if (adjustment != 0) chi.Severity += adjustment;
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.removeNow;
            }
        }        
    }
}
