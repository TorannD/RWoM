using RimWorld;
using Verse;
using Verse.Sound;

namespace TorannMagic
{
    public class Building_LightningTrap : Building_ExplosiveProximityTrap
    {
        public bool extendedTrap;
        public bool iceTrap;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.extendedTrap, "extendedTrap",false, false);
            Scribe_Values.Look<bool>(ref this.iceTrap, "iceTrap", false, false);
        }

        public new void Spring(Pawn p)
        {
            base.Spring(p);
            IntVec3 targetPos = Position;
            targetPos.z += 2;
            LocalTargetInfo t = targetPos;
            bool flag = t.Cell != default;
            float speed = .8f;
            if(extendedTrap)
            {
                speed = .6f;
            }
            if (flag)
            {
                Thing eyeThing = new Thing
                {
                    def = TorannMagicDefOf.FlyingObject_LightningTrap
                };
                FlyingObject_LightningTrap flyingObject = (FlyingObject_LightningTrap)GenSpawn.Spawn(TorannMagicDefOf.FlyingObject_LightningTrap, Position, Map);
                flyingObject.Launch(p, Position.ToVector3Shifted(), t.Cell, eyeThing, Faction, null, speed);
            }
            if(iceTrap)
            {
                AddSnowRadial(Position, Map, 6, 1.1f);
            }
        }

        public static void AddSnowRadial(IntVec3 center, Map map, float radius, float depth)
        {
            int num = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = center + GenRadial.RadialPattern[i];
                if (!intVec.InBoundsWithNullCheck(map)) continue;

                float lengthHorizontal = (center - intVec).LengthHorizontal;
                float num2 = 1f - lengthHorizontal / radius;
                map.snowGrid.AddDepth(intVec, num2 * depth);
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            base.Destroy(mode);
            InstallBlueprintUtility.CancelBlueprintsFor(this);
            if (mode == DestroyMode.Deconstruct)
            {
                SoundDef.Named("Building_Deconstructed").PlayOneShot(new TargetInfo(Position, map));
            }
        }
    }
}
