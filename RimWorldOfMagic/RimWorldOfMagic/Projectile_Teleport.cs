using RimWorld;
using AbilityUser;
using Verse;
using System.Linq;
using UnityEngine;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class Projectile_Teleport : ClassProgression.Projectile_SpiritBolt
    {

        private int destructTimer = 4800;

        private int age = -1;

        private bool primed = true;
        private float refundableMana = -1f; // Older in-flight projectiles did not store a quoted cost.

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget,
            ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
            var owner = SourcePawn;
            var magic = owner?.GetCompAbilityUserMagic();
            refundableMana = localAbilityDef == TorannMagicDefOf.TM_Teleport && magic?.Mana != null
                ? magic.ActualManaCost(TorannMagicDefOf.TM_Teleport) : 0f;
            if (owner?.IsColonist == false && ModOptions.Settings.Instance.AIAggressiveCasting) refundableMana *= .5f;
        }

        protected override string SkillStem => "TM_Teleport";
        protected override System.Collections.Generic.List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_Teleport;

        internal override void PrepareProjection(MagicAbility owner)
        {
            base.PrepareProjection(owner);
            ReadPortalRanks(owner.Pawn, owner.MagicUser);
        }

        private void ReadPortalRanks(Pawn owner, CompAbilityUserMagic magic)
        {
            var skills = Skills(magic.MagicData);
            pwrVal = skills.FirstOrDefault(s => s.label == "TM_Teleport_pwr")?.level ?? 0;
            verVal = skills.FirstOrDefault(s => s.label == "TM_Teleport_ver")?.level ?? 0;
            if (owner.story?.traits?.HasTrait(TorannMagicDefOf.TM_Wanderer) == true ||
                magic.customClass?.classMageAbilities?.Contains(TorannMagicDefOf.TM_Cantrips) == true)
            {
                var cantrips = magic.MagicData.MagicPowerSkill_Cantrips;
                pwrVal = Mathf.Max(pwrVal, (cantrips.FirstOrDefault(s => s.label == "TM_Cantrips_pwr")?.level ?? 0) / 5);
                verVal = Mathf.Max(verVal, (cantrips.FirstOrDefault(s => s.label == "TM_Cantrips_ver")?.level ?? 0) / 5);
            }
            arcaneDmg = magic.arcaneDmg;
        }

        private static ThingDef PortalDef(string stem, int rank)
            => ThingDef.Named(stem + (rank == 1 ? "_I" : rank == 2 ? "_II" : rank == 3 ? "_III" : ""));
        private static bool Clear(Map map, IntVec3 position, ThingDef building)
        {
            foreach (var cell in GenAdj.OccupiedRect(position, Rot4.North, building.size))
                if (!cell.InBounds(map) || !cell.Standable(map) ||
                    cell.GetThingList(map).Any(t => GenSpawn.SpawningWipes(building, t.def))) return false;
            return true;
        }
        internal static bool HasSpace(Map map, IntVec3 cell) => map != null && cell.IsValid &&
            Clear(map, cell, PortalDef("TM_Teleporter", 0)) && Clear(map, cell + IntVec3.East, PortalDef("TM_TeleportPod", 0));

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (age >= destructTimer) base.Destroy(mode);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            if (primed)
            {
                primed = false;
                var owner = SourcePawn;
                var magic = owner?.GetCompAbilityUserMagic();
                if (magic?.ExistingMagicData == null || !HasSpace(map, Position))
                {
                    CancelPortal(magic);
                    return;
                }
                if (!projected) ReadPortalRanks(owner, magic);
                destructTimer = Mathf.RoundToInt((4800 + 2400 * pwrVal) * arcaneDmg);
                Thing platform = null, pod = null;
                try
                {
                    GenClamor.DoClamor(this, 2.1f, ClamorDefOf.Impact);
                    var faction = (launcher as Pawn)?.Faction ?? owner.Faction;
                    platform = ThingMaker.MakeThing(PortalDef("TM_Teleporter", pwrVal));
                    pod = ThingMaker.MakeThing(PortalDef("TM_TeleportPod", verVal));
                    platform.SetFaction(faction); pod.SetFaction(faction);
                    GenSpawn.Spawn(platform, Position, map, Rot4.North);
                    var fuel = platform.TryGetComp<CompRefuelable>();
                    fuel.Refuel(fuel.GetFuelCountToFullyRefuel());
                    GenSpawn.Spawn(pod, Position + IntVec3.East, map, Rot4.North);
                }
                catch (System.Exception error)
                {
                    if (platform?.Spawned == true) platform.Destroy();
                    if (pod?.Spawned == true) pod.Destroy();
                    Compatibility.TM_Log.Warning("Teleport", "Portal creation failed: " + error);
                    CancelPortal(magic);
                    return;
                }
                Messages.Message("PortalCollapseIn".Translate(((destructTimer - age) / 60).ToString()), MessageTypeDefOf.NeutralEvent);
            }
            if (age >= destructTimer)
            {
                try { PortalCollapse(Position, map, 3); }
                finally
                {
                    Messages.Message("PortalCollapseFinal".Translate(), MessageTypeDefOf.SilentInput);
                    base.Destroy();
                }
            }
            else if (age == destructTimer * .5 || age == destructTimer * .75 || age == destructTimer * .95)
                Messages.Message("PortalCollapseIn".Translate(((destructTimer - age) / 60).ToString()), MessageTypeDefOf.NeutralEvent);
        }

        private void CancelPortal(CompAbilityUserMagic magic)
        {
            if (magic?.Mana != null)
            {
                float amount = refundableMana < 0f ? magic.ActualManaCost(TorannMagicDefOf.TM_Teleport) : refundableMana;
                refundableMana = 0f;
                magic.Mana.CurLevel += amount;
            }
            Messages.Message("InvalidPortal".Translate(), MessageTypeDefOf.RejectInput);
            base.Destroy();
        }

        protected void PortalCollapse(IntVec3 pos, Map map, float radius)
        {
            ThingDef def = this.def;
            Explosion(pos, map, radius, DamageDefOf.Bomb, SpellInstigator, null, def, this.equipmentDef, TorannMagicDefOf.Mote_Base_Smoke, 0.4f, 1, false, null, 0f, 1);

        }

        public static void Explosion(IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, SoundDef explosionSound = null, ThingDef projectile = null, ThingDef source = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1)
        {

            if (map == null)
            {
                Log.Warning("Tried to do explosion in a null map.");
                return;
            }
            Explosion explosion = (Explosion)GenSpawn.Spawn(ThingDefOf.Explosion, center, map);
            explosion.damageFalloff = true;
            explosion.chanceToStartFire = 0.0f;
            explosion.Position = center;
            explosion.radius = radius;
            explosion.damType = damType;
            explosion.instigator = instigator;
            explosion.damAmount = 30;
            explosion.weapon = source;
            explosion.chanceToStartFire = 0;
            explosion.preExplosionSpawnThingDef = preExplosionSpawnThingDef;
            explosion.preExplosionSpawnChance = preExplosionSpawnChance;
            explosion.preExplosionSpawnThingCount = preExplosionSpawnThingCount;
            explosion.postExplosionSpawnThingDef = postExplosionSpawnThingDef;
            explosion.postExplosionSpawnChance = postExplosionSpawnChance;
            explosion.postExplosionSpawnThingCount = postExplosionSpawnThingCount;
            explosion.applyDamageToExplosionCellsNeighbors = applyDamageToExplosionCellsNeighbors;
            explosion.StartExplosion(explosionSound, null);
        }

        public void SingleSpawnLoop(SpawnThings spawnables, IntVec3 position, Map map)
        {
            bool flag = spawnables.def != null;
            if (flag)
            {
                Faction faction = TM_Action.ResolveFaction(this.launcher as Pawn, spawnables, this.launcher.Faction);
                bool flag2 = spawnables.def.race != null;
                if (flag2)
                {
                    bool flag3 = spawnables.kindDef == null;
                    if (flag3)
                    {
                        Log.Error("Missing kinddef");
                    }
                    else
                    {
                        TM_Action.SpawnPawn(this.launcher as Pawn, spawnables, faction, position, 0, map);
                    }
                }
                else
                {
                    ThingDef def = spawnables.def;
                    ThingDef stuff = null;
                    bool madeFromStuff = def.MadeFromStuff;
                    if (madeFromStuff)
                    {
                        stuff = ThingDefOf.WoodLog;
                    }
                    Thing thing = ThingMaker.MakeThing(def, stuff);
                    if (thing.def.defName != "Portfuel")
                    { 
                        thing.SetFaction(faction, null);
                    }
                    GenSpawn.Spawn(thing, position, map, Rot4.North, WipeMode.Vanish, false);
                }
            }
        }        

        protected override void Tick()
        {
            base.Tick();
            this.age++;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref refundableMana, "TM_portalRefund", -1f);
            Scribe_Values.Look<bool>(ref this.primed, "primed", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.destructTimer, "destructTimer", 3600, false);
        }


        //public GlobalTargetInfo globeTarget = new GlobalTargetInfo();

        //public ThingWithComps parent = new ThingWithComps();

        //public CompProperties props = new CompProperties();

        //private List<Pawn> portablePawn = new List<Pawn>();

        //private List<Thing> portableItem = new List<Thing>();

        //private static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("UI/Overlays/LaunchableMouseAttachment", true);

        //private static readonly Texture2D LaunchCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", true);

        //public IThingHolder ParentHolder
        //{
        //    get
        //    {
        //        return parent.ParentHolder;
        //    }
        //}

        //[DebuggerHidden]
        //public IEnumerable<Gizmo> CompGetGizmosExtra(Pawn victim)
        //{

        //    if (true)
        //    {
        //        Command_Action launch = new Command_Action();
        //        launch.defaultLabel = "CommandLaunchGroup".Translate();
        //        launch.defaultDesc = "CommandLaunchGroupDesc".Translate();
        //        launch.icon = LaunchCommandTex;
        //        launch.action = delegate
        //        {
        //            if (false)
        //            {
        //                //Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmSendNotCompletelyLoadedPods".Translate(new object[]
        //                //{
        //                //    //this.<>f__this.FirstThingLeftToLoadInGroup.LabelCapNoCount
        //                //}), new Action(this.StartChoosingDestination), false, null));
        //            }
        //            else
        //            {
        //                this.StartChoosingDestination(victim);
        //            }
        //        };
        //        yield return launch;
        //    }

        //}

        //private void StartChoosingDestination(Pawn victim)
        //{
        //    CameraJumper.TryJump(CameraJumper.GetWorldTarget(this.parent));
        //    Find.WorldSelector.ClearSelection();
        //    int tile = victim.Map.Tile;
        //    Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChoseWorldTarget), true, TargeterMouseAttachment, true, delegate
        //    {
        //        GenDraw.DrawWorldRadiusRing(tile, this.MaxLaunchDistance);
        //    }, delegate (GlobalTargetInfo target)
        //    {
        //        if (!target.IsValid)
        //        {
        //            return null;
        //        }
        //        int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
        //        if (num <= this.MaxLaunchDistance)
        //        {
        //            return null;
        //        }
        //        if (num > 30)
        //        {
        //            return "TransportPodDestinationBeyondMaximumRange".Translate();
        //        }
        //        return "TransportPodNotEnoughFuel".Translate();
        //    });
        //}

        //private bool ChoseWorldTarget(GlobalTargetInfo target)
        //{
        //    if (!target.IsValid)
        //    {
        //        Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput);
        //        return false;
        //    }
        //    int num = Find.WorldGrid.TraversalDistanceBetween(this.parent.Map.Tile, target.Tile);
        //    if (num > this.MaxLaunchDistance)
        //    {
        //        Messages.Message("MessageTransportPodsDestinationIsTooFar".Translate(new object[]
        //        {
        //            CompLaunchable.FuelNeededToLaunchAtDist((float)num).ToString("0.#")
        //        }), MessageTypeDefOf.RejectInput);
        //        return false;
        //    }
        //    MapParent mapParent = target.WorldObject as MapParent;
        //    if (mapParent != null && mapParent.HasMap)
        //    {
        //        Map myMap = this.parent.Map;
        //        Map map = mapParent.Map;
        //        Current.Game.VisibleMap = map;
        //        Targeter arg_13B_0 = Find.Targeter;
        //        Action actionWhenFinished = delegate
        //        {
        //            if (Find.Maps.Contains(myMap))
        //            {
        //                Current.Game.VisibleMap = myMap;
        //            }
        //        };
        //        arg_13B_0.BeginTargeting(TargetingParameters.ForDropPodsDestination(), delegate (LocalTargetInfo x)
        //        {
        //            this.TryLaunch(x.ToGlobalTargetInfo(map), PawnsArriveMode.Undecided, false);
        //        }, null, actionWhenFinished, TargeterMouseAttachment);
        //        return true;
        //    }
        //    bool flag;
        //    if (mapParent != null)
        //    {
        //        Settlement settlement = mapParent as Settlement;
        //        List<FloatMenuOption> list = new List<FloatMenuOption>();
        //        if (settlement != null && settlement.Visitable)
        //        {
        //            list.Add(new FloatMenuOption("VisitSettlement".Translate(new object[]
        //            {
        //                target.WorldObject.Label
        //            }), delegate
        //            {
        //                this.TryLaunch(target, PawnsArriveMode.Undecided, false);
        //                CameraJumper.TryHideWorld();
        //            }, MenuOptionPriority.Default, null, null, 0f, null, null));
        //        }
        //        if (mapParent.TransportPodsCanLandAndGenerateMap)
        //        {
        //            list.Add(new FloatMenuOption("DropAtEdge".Translate(), delegate
        //            {
        //                this.TryLaunch(target, PawnsArriveMode.EdgeDrop, true);
        //                CameraJumper.TryHideWorld();
        //            }, MenuOptionPriority.Default, null, null, 0f, null, null));
        //            list.Add(new FloatMenuOption("DropInCenter".Translate(), delegate
        //            {
        //                this.TryLaunch(target, PawnsArriveMode.CenterDrop, true);
        //                CameraJumper.TryHideWorld();
        //            }, MenuOptionPriority.Default, null, null, 0f, null, null));
        //        }
        //        if (list.Any<FloatMenuOption>())
        //        {
        //            Find.WorldTargeter.closeWorldTabWhenFinished = false;
        //            Find.WindowStack.Add(new FloatMenu(list));
        //            return true;
        //        }
        //        flag = true;
        //    }
        //    else
        //    {
        //        flag = true;
        //    }
        //    if (!flag)
        //    {
        //        return false;
        //    }
        //    if (Find.World.Impassable(target.Tile))
        //    {
        //        Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput);
        //        return false;
        //    }
        //    this.TryLaunch(target, PawnsArriveMode.Undecided, false);
        //    return true;
        //}

        //private void TryLaunch(GlobalTargetInfo target, PawnsArriveMode arriveMode, bool attackOnArrival)
        //{
        //    if (!this.parent.Spawned)
        //    {
        //        Log.Error("Tried to launch " + this.parent + ", but it's unspawned.");
        //        return;
        //    }
        //    //List<CompTransporter> transportersInGroup = this.TransportersInGroup;
        //    //if (transportersInGroup == null)
        //    //{
        //    //    Log.Error("Tried to launch " + this.parent + ", but it's not in any group.");
        //    //    return;
        //    //}
        //    Map map = this.parent.Map;
        //    int num = Find.WorldGrid.TraversalDistanceBetween(map.Tile, target.Tile);
        //    if (num > this.MaxLaunchDistance)
        //    {
        //        return;
        //    }
        //    //this.Transporter.TryRemoveLord(map);
        //    //int groupID = this.Transporter.groupID;
        //    float amount = Mathf.Max(CompLaunchable.FuelNeededToLaunchAtDist((float)num), 1f);
        //    for (int i = 0; i < 1; i++)
        //    {
        //        CompTransporter compTransporter = new CompTransporter();
        //        Building fuelingPortSource = compTransporter.Launchable.FuelingPortSource;
        //        if (fuelingPortSource != null)
        //        {
        //            fuelingPortSource.TryGetComp<CompRefuelable>().ConsumeFuel(amount);
        //        }
        //        DropPodLeaving dropPodLeaving = (DropPodLeaving)ThingMaker.MakeThing(ThingDefOf.DropPodLeaving, null);
        //        dropPodLeaving.groupID = 1;
        //        dropPodLeaving.destinationTile = target.Tile;
        //        dropPodLeaving.destinationCell = target.Cell;
        //        dropPodLeaving.arriveMode = arriveMode;
        //        dropPodLeaving.attackOnArrival = attackOnArrival;
        //        ThingOwner directlyHeldThings = compTransporter.GetDirectlyHeldThings();
        //        dropPodLeaving.Contents = new ActiveDropPodInfo();
        //        dropPodLeaving.Contents.innerContainer.TryAddRangeOrTransfer(directlyHeldThings, true);
        //        directlyHeldThings.Clear();
        //        compTransporter.CleanUpLoadingVars(map);
        //        compTransporter.parent.Destroy(DestroyMode.Vanish);
        //        GenSpawn.Spawn(dropPodLeaving, compTransporter.parent.Position, map);
        //    }
        //}

        //public override string ToString()
        //{
        //    return string.Concat(new object[]
        //    {
        //        base.GetType().Name,
        //        "(parent=",
        //        this.parent,
        //        " at=",
        //        (this.parent == null) ? IntVec3.Invalid : this.parent.Position,
        //        ")"
        //    });
        //}

        //private int MaxLaunchDistance
        //{
        //    get
        //    {
        //        return CompLaunchable.MaxLaunchDistanceAtFuelLevel(30);
        //    }
        //}


        //protected void TeleportSelect(IntVec3 pos, Map map, float radius)
        //{
        //    ThingDef def = this.def;
        //    //GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.Flame, this.launcher, null, def, this.equipmentDef, ThingDefOf.FilthFuel, 0.4f, 1, false, null, 0f, 1);
        //    TeleportAdd(pos, map, radius, DamageDefOf.Flame, this.launcher, null, def, this.equipmentDef, null, 0.4f, 1, false, null, 0f, 1);

        //}

        //public static void TeleportAdd(IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, SoundDef explosionSound = null, ThingDef projectile = null, ThingDef source = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1)
        //{
        //    //System.Random rnd = new System.Random();
        //    //int modDamAmountRand = GenMath.RoundRandom(rnd.Next(1, projectile.projectile.GetDamageAmount(1,null) / 2));
        //    if (map == null)
        //    {
        //        Log.Warning("Tried to do explosion in a null map.");
        //        return;
        //    }
        //    Explosion teleportAdd = new Explosion();
        //    teleportAdd.position = center;
        //    teleportAdd.radius = radius;
        //    teleportAdd.damType = damType;
        //    teleportAdd.instigator = instigator;
        //    teleportAdd.damAmount = 0; // ((projectile == null) ? GenMath.RoundRandom((float)damType.defaultDamage) : modDamAmountRand);
        //    teleportAdd.weaponGear = source;

        //    bool flagPawn;
        //    bool flagThing;

        //    map.GetComponent<ExplosionManager>().StartExplosion(teleportAdd, explosionSound);

        //}

    }
}
