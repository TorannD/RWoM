using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public abstract class Verb_SummonTotem : ClassProgression.Verb_SpiritTargetable
    {
        protected abstract ThingDef TotemDef { get; }
        protected virtual ThingDef Material => null;
        protected virtual FleckDef SummonFleck => FleckDefOf.DustPuffThick;

        private IntVec3 LandingCell(IntVec3 target, Map map)
        {
            foreach (var cell in GenRadial.RadialCellsAround(target, 2f, true))
            {
                if (!cell.InBounds(map) || cell.Fogged(map) || !cell.Standable(map) ||
                    cell.GetFirstPawn(map) != null || cell.GetFirstBuilding(map) != null || cell.GetItemCount(map) != 0) continue;
                if (!GenSpawn.WouldWipeAnythingWith(cell, Rot4.North, TotemDef, map, _ => true)) return cell;
            }
            return IntVec3.Invalid;
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            var map = CasterPawn?.Map;
            return map != null && !target.HasThing && target.IsValid && target.Cell.InBounds(map) &&
                !target.Cell.Fogged(map) && target.Cell.Walkable(map) && (root-target.Cell).LengthHorizontal < verbProps.range &&
                TryFindShootLineFromTo(root,target,out _) && LandingCell(target.Cell,map).IsValid;
        }

        protected override bool TryCastShot()
        {
            burstShotsLeft = 0;
            var body = CasterPawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body,Ability);
            if (body?.Spawned != true || owner == null || !CanHitTargetFrom(body.Position,currentTarget)) return false;
            var map = body.Map;
            var cell = LandingCell(currentTarget.Cell,map);
            int power = TM_Calc.GetSkillPowerLevel(owner,TorannMagicDefOf.TM_Totems);
            int versatility = TM_Calc.GetSkillVersatilityLevel(owner,TorannMagicDefOf.TM_Totems);
            var spawned = TM_Action.SingleSpawnLoop(body, new SpawnThings { def=TotemDef, spawnCount=1 }, cell, map,
                2500 + 125 * versatility,true,false,body.Faction,false,Material);
            if (!(spawned is Building totem) || !totem.Spawned) return false;
            Configure(totem,owner,power,versatility);
            for (int i=0; i<3; i++)
            {
                var pos = totem.DrawPos;
                pos.x += Rand.Range(-.5f,.5f);
                pos.z += Rand.Range(-.5f,.5f);
                TM_MoteMaker.ThrowGenericFleck(SummonFleck,pos,map,Rand.Range(.6f,SummonFleck == FleckDefOf.DustPuffThick ? 1f : .8f),.1f,.05f,.05f,0,0,0,Rand.Range(0,360));
                FleckMaker.ThrowSmoke(pos,map,Rand.Range(.8f,1.2f));
            }
            return true;
        }

        protected abstract void Configure(Building totem, Pawn owner, int power, int versatility);
    }
}
