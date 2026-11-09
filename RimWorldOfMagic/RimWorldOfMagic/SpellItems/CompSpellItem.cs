using System;
using RimWorld;
using Verse;

namespace TorannMagic.SpellItems
{
    public enum SpellItemKind { Fireball = 0, Heal = 1, Blink = 2, Lightning = 3, Regeneration = 4, CureDisease = 5, Purify = 6, Soothe = 7, Portal = 8, Resurrection = 9, EtherShell = 10, BreachSeal = 11 }

    public sealed class CompProperties_SpellItem : CompProperties
    {
        public SpellItemKind spell;
        public int maxUses = 1;
        public bool requiresEquipped;
        public float range = 20f;
        public int warmupTicks = 60;
        public int cooldownTicks = 120;

        public CompProperties_SpellItem() { compClass = typeof(CompSpellItem); }
    }

    public sealed class CompSpellItem : ThingComp
    {
        private int remainingUses = -1;
        private int readyTick;
        private int nextToken;
        private int activeToken;
        private Pawn activePawn;
        private bool completing;

        public CompProperties_SpellItem Props => (CompProperties_SpellItem)props;
        public int RemainingUses => remainingUses < 0 ? Math.Max(1, Props.maxUses) : remainingUses;
        public int ReadyTick => readyTick;
        public bool IsBusy => activeToken != 0;
        public int ActiveToken => activeToken;
        public Pawn ActivePawn => activePawn;

        public override void PostPostMake()
        {
            base.PostPostMake();
            remainingUses = Math.Max(1, Props.maxUses);
        }

        public bool IsOwnedBy(Pawn pawn)
        {
            if (pawn == null || parent == null || parent.Destroyed || parent.stackCount <= 0) return false;
            return Props.requiresEquipped ? pawn.equipment?.Primary == parent
                : pawn.inventory?.innerContainer?.Contains(parent) == true;
        }

        public string UseRejection(Pawn pawn)
        {
            if (!IsOwnedBy(pawn)) return "TM_SpellItem_NotHeld";
            if (pawn.Spawned != true || pawn.Dead || pawn.Downed || pawn.InMentalState
                || pawn.Faction?.IsPlayer != true || pawn.RaceProps?.Humanlike != true)
                return "TM_SpellItem_CannotUse";
            if (pawn.health?.capacities?.CapableOf(PawnCapacityDefOf.Manipulation) != true)
                return "TM_SpellItem_CannotUse";
            if (RemainingUses <= 0) return "TM_SpellItem_Empty";
            if (readyTick > Find.TickManager.TicksGame) return "TM_SpellItem_Cooldown";
            if (IsBusy) return "TM_SpellItem_Busy";
            return null;
        }

        public int TryBegin(Pawn pawn)
        {
            if (UseRejection(pawn) != null) return 0;
            nextToken = nextToken == int.MaxValue ? 1 : nextToken + 1;
            activeToken = nextToken;
            activePawn = pawn;
            return activeToken;
        }

        public bool Matches(Pawn pawn, int token)
        {
            return token != 0 && token == activeToken && activePawn == pawn && IsOwnedBy(pawn)
                && RemainingUses > 0 && pawn.Spawned && !pawn.Dead && !pawn.Downed && !pawn.InMentalState
                && pawn.Faction?.IsPlayer == true
                && pawn.health?.capacities?.CapableOf(PawnCapacityDefOf.Manipulation) == true;
        }

        public void InvalidateCast()
        {
            if (completing) return;
            activeToken = 0;
            activePawn = null;
        }

        public void Release(Pawn pawn, int token)
        {
            if (completing || activePawn != pawn || activeToken != token) return;
            activeToken = 0;
            activePawn = null;
        }

        public bool TryCommit(Pawn pawn, int token, LocalTargetInfo target)
        {
            if (completing || !Matches(pawn, token) || SpellItemEffects.TargetRejection(pawn, target, Props) != null)
                return false;
            completing = true;
            try
            {
                if (!SpellItemEffects.TryApply(pawn, target, Props)) return false;
                remainingUses = RemainingUses - 1;
                readyTick = Find.TickManager.TicksGame + Math.Max(0, Props.cooldownTicks);
                activeToken = 0;
                activePawn = null;
                if (Props.maxUses == 1)
                {
                    if (parent.stackCount > 1)
                    {
                        parent.stackCount--;
                        remainingUses = 1;
                    }
                    else parent.Destroy(DestroyMode.Vanish);
                }
                else if (remainingUses == 0) parent.Destroy(DestroyMode.Vanish);
                return true;
            }
            finally
            {
                completing = false;
                Release(pawn, token);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref remainingUses, "spellItemUses", -1);
            Scribe_Values.Look(ref readyTick, "spellItemReadyTick", 0);
            Scribe_Values.Look(ref nextToken, "spellItemNextToken", 0);
            Scribe_Values.Look(ref activeToken, "spellItemActiveToken", 0);
            Scribe_References.Look(ref activePawn, "spellItemCaster");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                remainingUses = Math.Min(RemainingUses, Math.Max(1, Props.maxUses));
                if (activePawn == null || !IsOwnedBy(activePawn))
                {
                    activeToken = 0;
                    activePawn = null;
                }
            }
        }

        public override bool AllowStackWith(Thing other)
        {
            if (Props.requiresEquipped || Props.maxUses != 1 || IsBusy) return false;
            CompSpellItem comp = SpellItemCasting.Comp(other);
            return comp != null && !comp.IsBusy && comp.RemainingUses == RemainingUses && comp.readyTick == readyTick;
        }

        public override void PostSplitOff(Thing piece)
        {
            base.PostSplitOff(piece);
            CompSpellItem copy = SpellItemCasting.Comp(piece);
            if (copy != null)
            {
                copy.remainingUses = RemainingUses;
                copy.readyTick = readyTick;
                copy.nextToken = nextToken;
                copy.activeToken = 0;
                copy.activePawn = null;
            }
            activeToken = 0;
            activePawn = null;
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            activeToken = 0;
            activePawn = null;
        }

        public override string CompInspectStringExtra()
        {
            return "TM_SpellItem_Uses".Translate(RemainingUses, Math.Max(1, Props.maxUses));
        }
    }
}
