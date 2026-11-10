using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_SoulBond : HediffComp
    {
        private bool initializing = true;
        private bool soulPawnRemove = false;
        Pawn bondedPawn = null;

        internal int originalId;
        internal Pawn Owner => originalId == 0 ? Pawn :
            ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(Pawn,out var soul) && soul.thingIDNumber == originalId ? soul : null;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref originalId,"soulBondOriginalId",0);
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo,culprit);
            if (originalId == 0 && Pawn.Dead)
                ClassProgression.DemonSoulBond.EndOnDeath(Pawn,Pawn.GetCompAbilityUserMagic()?.soulBondPawn,parent.def.defName == "TM_WDSoulBondMentalHD",false);
        }

        public override string CompLabelInBracketsExtra => bondedPawn != null ? bondedPawn.LabelShort + "[+]" + base.CompLabelInBracketsExtra : base.CompLabelInBracketsExtra;

        public string labelCap
        {
            get
            {
                if(bondedPawn != null)
                {
                    return base.Def.LabelCap + "(" + bondedPawn.LabelShort + ")";
                }
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                if (bondedPawn != null)
                {
                    return base.Def.label + "(" + bondedPawn.LabelShort + ")";
                }
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned)
            {
                FleckMaker.ThrowHeatGlow(base.Pawn.DrawPos.ToIntVec3(), base.Pawn.Map, 2f);
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
            bool flag4 = Find.TickManager.TicksGame % 600 == 0;
            if (flag4)
            {
                if(Find.TickManager.TicksGame % 3600 == 0)
                {
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(TorannMagicDefOf.TM_UsedMagic, this.Pawn.Named(HistoryEventArgsNames.Doer), this.Pawn.Named(HistoryEventArgsNames.Subject), this.Pawn.Named(HistoryEventArgsNames.AffectedFaction), this.Pawn.Named(HistoryEventArgsNames.Victim)), true);
                }
                RefreshPair();
            }
        }

        private void RefreshPair()
        {
            var owner = Owner;
            var magic = owner?.GetCompAbilityUserMagic();
            bondedPawn = magic?.soulBondPawn;
            if (bondedPawn.DestroyedOrNull() || bondedPawn.Dead)
            {
                soulPawnRemove = true;
                return;
            }
            bool mental = parent.def.defName == "TM_WDSoulBondMentalHD";
            if ((!mental && parent.def.defName != "TM_SDSoulBondPhysicalHD") ||
                (ClassProgression.ClassIdentity.TryGetDemonBranch(owner,out bool warlock) && warlock != mental))
            {
                soulPawnRemove = true;
                return;
            }

            string name = mental ? "TM_SoulBondMentalHD" : "TM_SoulBondPhysicalHD";
            var effects = bondedPawn.health?.hediffSet;
            if (effects == null) { soulPawnRemove = true; return; }
            var target = effects.hediffs.FirstOrDefault(h => h.def.defName == name);
            var partner = target?.TryGetComp<HediffComp_SoulBondHost>();
            if (target != null && (partner == null || partner.BonderPawn != null && partner.BonderPawn != owner))
            {
                soulPawnRemove = true;
                return;
            }

            var ranks = magic.MagicData.MagicPowerSkill_SoulBond;
            float receiverRank = .5f + (ranks.FirstOrDefault(s => s.label == "TM_SoulBond_ver")?.level ?? 0);
            if (target == null)
            {
                target = HediffMaker.MakeHediff(HediffDef.Named(name),bondedPawn);
                partner = target.TryGetComp<HediffComp_SoulBondHost>();
                if (partner == null) { soulPawnRemove = true; return; }
                partner.BonderPawn = owner;
                target.Severity = receiverRank;
                bondedPawn.health.AddHediff(target);
            }
            else
            {
                partner.BonderPawn = owner;
                if (target.Severity != receiverRank) target.Severity = receiverRank;
            }
            float senderRank = .5f + (ranks.FirstOrDefault(s => s.label == "TM_SoulBond_pwr")?.level ?? 0);
            if (parent.Severity != senderRank) parent.Severity = senderRank;
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.soulPawnRemove;
            }
        }
    }
}
