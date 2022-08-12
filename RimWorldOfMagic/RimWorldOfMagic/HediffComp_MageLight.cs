using RimWorld;
using Verse;
using System.Linq;

namespace TorannMagic
{
    public class HediffComp_MageLight : HediffComp
    {
        private bool initializing = true;
        CompProperties_Glower gProps = new CompProperties_Glower();
        CompGlower glower = new CompGlower();
        IntVec3 oldPos = default(IntVec3);
        CompAbilityUserMagic comp;
        private bool canCastLightning = false;
        private int nextLightningTick = 0;

        public ColorInt glowColor = new ColorInt(255, 255, 204, 1);

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
            if (spawned && base.Pawn.Map != null)
            {
                FleckMaker.ThrowLightningGlow(base.Pawn.TrueCenter(), base.Pawn.Map, 3f);
                this.glower = new CompGlower();
                gProps.glowColor = glowColor;
                gProps.glowRadius = 7f;
                glower.parent = this.Pawn;
                glower.Initialize(gProps);
                comp = base.Pawn.GetCompAbilityUserMagic();
                this.nextLightningTick = Find.TickManager.TicksGame + Rand.Range(400, 800);
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

            if (Find.TickManager.TicksGame >= this.nextLightningTick && comp != null)
            {
                if (canCastLightning || comp?.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_ver").level >= 13)
                {
                    canCastLightning = true;
                    this.nextLightningTick = Find.TickManager.TicksGame + Rand.Range(400, 800);
                    if(this.Pawn.Drafted && !this.Pawn.Downed && this.Pawn.Map != null && this.Pawn.Spawned)
                    {
                        Pawn e = TM_Calc.FindNearbyEnemy(this.Pawn.Position, this.Pawn.Map, this.Pawn.Faction, 24, 0);
                        if (e != null && TM_Calc.HasLoSFromTo(this.Pawn.Position, e, this.Pawn, 0, 25))
                        {
                            Projectile lightning = ThingMaker.MakeThing(ThingDef.Named("Laser_LightningBolt"), null) as Projectile;
                            TM_CopyAndLaunchProjectile.CopyAndLaunchProjectile(lightning, this.Pawn, e, e, ProjectileHitFlags.All, null);
                        }
                    }
                }
            }

            if (this.glower != null && glower.parent != null && comp != null)
            {
                if (oldPos != this.Pawn.Position)
                {
                    if (this.Pawn != null && this.Pawn.Map != null)
                    {
                        if (oldPos != default(IntVec3))
                        {
                            this.Pawn.Map.mapDrawer.MapMeshDirty(oldPos, MapMeshFlag.Things);
                            this.Pawn.Map.glowGrid.DeRegisterGlower(glower);
                        }
                        if ((this.Pawn.Map.skyManager.CurSkyGlow < 0.7f || this.Pawn.Position.Roofed(Pawn.Map)) && !comp.mageLightSet)
                        {
                            this.Pawn.Map.mapDrawer.MapMeshDirty(this.Pawn.Position, MapMeshFlag.Things);
                            oldPos = this.Pawn.Position;
                            this.Pawn.Map.glowGrid.RegisterGlower(glower);
                        }
                    }
                }
            }
            else
            {
                initializing = false;
                this.Initialize();
            }
        }

        public override void CompPostPostRemoved()
        {
            if (oldPos != default(IntVec3))
            {
                this.Pawn.Map.mapDrawer.MapMeshDirty(oldPos, MapMeshFlag.Things);
                this.Pawn.Map.glowGrid.DeRegisterGlower(glower);
            }
            base.CompPostPostRemoved();
        }
    }
}