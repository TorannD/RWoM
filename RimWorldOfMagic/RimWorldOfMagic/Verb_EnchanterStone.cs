using System.Collections.Generic;
using System.Linq;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;

namespace TorannMagic
{
    public class Verb_EnchanterStone : Verb_SpiritTargetable
    {
        internal static (ThingDef artifact, int count, int rank) Recipe(ThingDef material)
        {
            switch (material?.defName)
            {
                case "Silver": return (TorannMagicDefOf.TM_Artifact_Silver, 500, 1);
                case "Gold": return (TorannMagicDefOf.TM_Artifact_Gold, 50, 3);
                case "Jade": return (TorannMagicDefOf.TM_Artifact_Jade, 50, 2);
                case "RawMagicyte": return (TorannMagicDefOf.TM_Artifact_Magicyte, 100, 3);
                case "Steel": return (TorannMagicDefOf.TM_Artifact_Steel, 75, 1);
                case "MedicineHerbal": return (TorannMagicDefOf.TM_Artifact_Medicine, 10, 2);
                case "WoodLog": return (TorannMagicDefOf.TM_Artifact_Wood, 75, 2);
                case "LotR_Iron": return (TorannMagicDefOf.TM_Artifact_Iron, 75, 1);
                case "ChunkSlate": return (TorannMagicDefOf.TM_Artifact_Slate, 1, 0);
                case "ChunkMarble": return (TorannMagicDefOf.TM_Artifact_Marble, 1, 0);
                case "ChunkSandstone": return (TorannMagicDefOf.TM_Artifact_Sandstone, 1, 0);
                case "ChunkGranite": return (TorannMagicDefOf.TM_Artifact_Granite, 1, 0);
                case "ChunkLimestone": return (TorannMagicDefOf.TM_Artifact_Limestone, 1, 0);
                default: return default;
            }
        }

        internal static Thing Source(Pawn body, Pawn owner, LocalTargetInfo target)
        {
            var magic = owner?.GetCompAbilityUserMagic();
            if (magic == null || body?.Map == null || !target.IsValid || !target.Cell.InBounds(body.Map)) return null;
            int rank = magic.MagicData.MagicPowerSkill_EnchanterStone.First(s => s.label == "TM_EnchanterStone_ver").level;
            bool Eligible(Thing item)
            {
                var recipe = Recipe(item.def);
                return item.Spawned && item.Map == body.Map && recipe.artifact != null &&
                    rank >= recipe.rank && item.stackCount >= recipe.count;
            }
            if (target.HasThing) return Eligible(target.Thing) ? target.Thing : null;
            return target.Cell.GetThingList(body.Map).FirstOrDefault(Eligible);
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            return target.IsValid && target.Cell.InBounds(CasterPawn.Map) && !target.Cell.Fogged(CasterPawn.Map) &&
                (root - target.Cell).LengthHorizontal < verbProps.range;
        }

        protected override bool TryCastShot()
        {
            ApplyHostedEffect();
            burstShotsLeft = 0;
            return false;
        }

        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            var owner = SpiritRebirthCasting.SkillOwner(body, Ability);
            var source = Source(body, owner, currentTarget);
            if (source == null)
            {
                if (body?.Map == null || !currentTarget.Cell.InBounds(body.Map)) return false;
                var material = currentTarget.Thing ?? currentTarget.Cell.GetThingList(body.Map)
                    .FirstOrDefault(t => Recipe(t.def).artifact != null);
                var missing = Recipe(material?.def);
                var reason = missing.artifact == null ? "TM_InvalidTarget".Translate(body.LabelShort, verbProps.label) :
                    material.stackCount < missing.count ? "TM_InsufficientMaterialForArtifact".Translate(material.LabelShort) :
                    "TM_CannotCreateStone".Translate(body.LabelShort, material.LabelShort);
                Messages.Message(reason, MessageTypeDefOf.RejectInput);
                return false;
            }
            var recipe = Recipe(source.def);
            var stone = ThingMaker.MakeThing(recipe.artifact);
            var position = source.Position;
            if (!GenPlace.TryPlaceThing(stone, position, body.Map, ThingPlaceMode.Near)) return false;
            source.SplitOff(recipe.count).Destroy();
            var magic = owner.GetCompAbilityUserMagic();
            (magic.enchanterStones ??= new List<Thing>()).Add(stone);
            magic.SyncEnchanterStoneControl();
            TM_Action.TransmutateEffects(position, body);
            return true;
        }
    }
}
