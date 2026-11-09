using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_RangerBond : HediffComp
    {
        private bool initializing = true;
        public Pawn bonderPawn = null;

        internal static void ApplyPetBond(Pawn owner,Pawn pet,float severity)
        {
            HealthUtility.AdjustSeverity(pet,TorannMagicDefOf.TM_RangerBondHD,severity);
            var marker=pet.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_RangerBondHD)?.TryGetComp<HediffComp_RangerBond>();
            if (marker!=null) marker.bonderPawn=owner;
        }

        public override string CompLabelInBracketsExtra => bonderPawn != null ? bonderPawn.LabelShort + base.CompLabelInBracketsExtra : base.CompLabelInBracketsExtra;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Pawn>(ref this.bonderPawn, "bonderPawn", false);
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
            if (bonderPawn == null)
            {
                if (spawned)
                {
                    FleckMaker.ThrowHeatGlow(base.Pawn.DrawPos.ToIntVec3(), base.Pawn.Map, 2f);
                }
                var owner = Pawn.TryGetComp<CompAnimalController>()?.summonerPawn;
                if (owner?.GetCompAbilityUserMagic()?.bondedSpirit == Pawn)
                {
                    bonderPawn = owner;
                    return;
                }
                if (!spawned) return;
                List<Pawn> mapPawns = this.Pawn.Map.mapPawns.AllPawnsSpawned.ToList();
                for (int i = 0; i < mapPawns.Count(); i++)
                {
                    if (ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(mapPawns[i],out var soul) &&
                        soul.GetCompAbilityUserMight()?.bondedPet==this.Pawn)
                    {
                        this.bonderPawn=soul;
                        return;
                    }
                    if (!mapPawns[i].DestroyedOrNull() && mapPawns[i].Spawned && !mapPawns[i].Downed && mapPawns[i].RaceProps.Humanlike)
                    {
                        CompAbilityUserMight comp = mapPawns[i].GetCompAbilityUserMight();
                        if (comp?.IsMightUser == true && comp.bondedPet != null)
                        {
                            if (comp.bondedPet == this.Pawn)
                            {
                                this.bonderPawn = comp.Pawn;
                                break;
                            }
                        }
                        CompAbilityUserMagic compMagic = mapPawns[i].GetCompAbilityUserMagic();
                        if(compMagic?.IsMagicUser == true && compMagic.bondedSpirit != null)
                        {
                            if(compMagic.bondedSpirit == this.Pawn)
                            {
                                this.bonderPawn = compMagic.Pawn;
                                break;
                            }
                        }
                    }
                }
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

            if (Find.TickManager.TicksGame % 600 == 0)
            {

                Hediff_Injury injuryToHeal = Pawn?.health.hediffSet.hediffs
                    .OfType<Hediff_Injury>()
                    .FirstOrDefault();
                injuryToHeal?.Heal(injuryToHeal.CanHealNaturally() ? 1.0f + parent.Severity / 3f : .2f);

                if (this.bonderPawn != null && !this.bonderPawn.Destroyed && !this.bonderPawn.Dead)
                {
                    RefreshBond();
                    UpdateBond();                    
                }
                else
                {
                    this.Pawn.health.RemoveHediff(this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.parent.def));
                    if(this.Pawn.def.thingClass == typeof(TMPawnSummoned))
                    {
                        if (this.Pawn.Map != null)
                        {
                            FleckMaker.ThrowSmoke(this.Pawn.DrawPos, this.Pawn.Map, 1f);
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ghost, this.Pawn.DrawPos, this.Pawn.Map, 1.3f, .25f, .1f, .45f, 0, Rand.Range(1f, 2f), 0, 0);
                        }
                        this.Pawn.Destroy(DestroyMode.Vanish);                        
                    }
                    //this.Pawn.SetFactionDirect(null);
                }
            }
        }

        private void UpdateBond()
        {
            int verVal = 0;
            CompAbilityUserMight comp = this.bonderPawn.GetCompAbilityUserMight();
            if (comp != null && comp.IsMightUser)
            {
                verVal = comp.MightData.MightPowerSkill_AnimalFriend.FirstOrDefault(x => x.label == "TM_AnimalFriend_ver")?.level ?? 0;
            }
            CompAbilityUserMagic compMagic = this.bonderPawn.GetCompAbilityUserMagic();
            if(comp?.bondedPet != Pawn && compMagic != null && compMagic.IsMagicUser)
            {
                verVal = compMagic.MagicData.MagicPowerSkill_GuardianSpirit.FirstOrDefault(x => x.label == "TM_GuardianSpirit_ver")?.level ?? 0;
            }
            this.parent.Severity = .5f + verVal;
        }

        public void RefreshBond()
        {
            TM_Action.UpdateAnimalTraining(this.Pawn);
        }
    }
}
