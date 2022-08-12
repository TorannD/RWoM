using RimWorld;
using AbilityUser;
using Verse;
using System.Linq;
using UnityEngine;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class Projectile_Teleport : Projectile_AbilityBase , IExposable
    {

        private int destructTimer = 4800;

        private int age = -1;

        private bool primed = true;

        private int pwrVal = 0;
        private int verVal = 0;


        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < destructTimer;
            if (!flag)
            {
                Messages.Message("PortalCollapseFinal".Translate(), MessageTypeDefOf.SilentInput);                
                base.Destroy(mode);
            }
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            GenClamor.DoClamor(this, 2.1f, ClamorDefOf.Impact);
            //base.Impact(hitThing);
            string msg;
            ThingDef def = this.def;
            Pawn victim = hitThing as Pawn;
            Thing item = hitThing as Thing;
            IntVec3 arg_pos_1;
            IntVec3 arg_pos_2;
            IntVec3 arg_pos_3;

            Pawn pawn = this.launcher as Pawn;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            MagicPowerSkill pwr = comp.MagicData.MagicPowerSkill_Teleport.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Teleport_pwr");
            MagicPowerSkill ver = comp.MagicData.MagicPowerSkill_Teleport.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Teleport_ver");
            pwrVal = pwr.level;
            verVal = ver.level;
            CellRect cellRect = CellRect.CenteredOn(base.Position, 1);
            cellRect.ClipInsideMap(map);
            IntVec3 centerCell = cellRect.CenterCell;
            if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer) || (comp.customClass != null && comp.customClass.classMageAbilities.Contains(TorannMagicDefOf.TM_Cantrips)))
            {
                int tmpPwrVal = (int)((pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr").level) / 5);
                int tmpVerVal = (int)((pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_ver").level) / 5);
                pwrVal = (tmpPwrVal > pwrVal) ? tmpPwrVal : pwrVal;
                verVal = (tmpVerVal > verVal) ? tmpVerVal : verVal;
            }
            if (this.primed != false)
            {
                destructTimer = Mathf.RoundToInt((4800 + (pwrVal * 1200) + (pwrVal * 1200)) * comp.arcaneDmg);
                arg_pos_1 = centerCell;
                centerCell.x++;
                arg_pos_2 = centerCell;
                centerCell.z++;
                arg_pos_3 = centerCell;
                centerCell = cellRect.CenterCell;

                if ((arg_pos_1.IsValid && arg_pos_1.Standable(map)) && (arg_pos_2.IsValid && arg_pos_2.Standable(map)) && (arg_pos_3.IsValid && arg_pos_3.Standable(map)))
                {
                    AbilityUser.SpawnThings tempPod = new SpawnThings();
                    IntVec3 shiftPos = centerCell;
                    centerCell.x++;

                    if (pwrVal == 1)
                    {
                        tempPod.def = ThingDef.Named("TM_Teleporter_I");
                    }
                    else if (pwrVal == 2)
                    {
                        tempPod.def = ThingDef.Named("TM_Teleporter_II");
                    }
                    else if (pwrVal == 3)
                    {
                        tempPod.def = ThingDef.Named("TM_Teleporter_III");
                    }
                    else
                    {
                        tempPod.def = ThingDef.Named("TM_Teleporter");
                    }
                    tempPod.spawnCount = 1;
                    try
                    {
                        this.SingleSpawnLoop(tempPod, shiftPos, map);
                        Building teleporter = shiftPos.GetFirstBuilding(map);                        
                        int num = teleporter.TryGetComp<CompRefuelable>().GetFuelCountToFullyRefuel();
                        teleporter.TryGetComp<CompRefuelable>().Refuel(num);
                        
                    }
                    catch
                    {
                        Log.Message("Attempted to create a portal but threw an unknown exception - recovering and ending attempt");
                        PortalCollapse(shiftPos, map, 3);
                        if (pawn != null)
                        {
                            comp.Mana.CurLevel += comp.ActualManaCost(TorannMagicDefOf.TM_Teleport);                            
                        }
                        this.age = this.destructTimer;
                        return;
                    }
                    

                    if (verVal == 1)
                    {
                        tempPod.def = ThingDef.Named("TM_TeleportPod_I");
                    }
                    else if (verVal == 2)
                    {
                        tempPod.def = ThingDef.Named("TM_TeleportPod_II");
                    }
                    else if (verVal == 3)
                    {
                        tempPod.def = ThingDef.Named("TM_TeleportPod_III");
                    }
                    else
                    {
                        tempPod.def = ThingDef.Named("TM_TeleportPod");
                    }
                    tempPod.spawnCount = 1;
                    shiftPos = centerCell;
                    centerCell.z++;
                    try
                    {
                        this.SingleSpawnLoop(tempPod, shiftPos, map);

                    }
                    catch
                    {
                        Log.Message("Attempted to create a portal but threw an unknown exception - recovering and ending attempt");
                        PortalCollapse(shiftPos, map, 3);
                        if (pawn != null)
                        {
                            comp.Mana.CurLevel += comp.ActualManaCost(TorannMagicDefOf.TM_Teleport);
                        }
                        this.age = this.destructTimer;
                        return;
                    }

                    //tempPod.def = ThingDef.Named("Portfuel");
                    //tempPod.spawnCount = 45 + (pwr.level * 15);
                    //tempPod.factionDef = null;
                    //shiftPos = centerCell;

                    //for (int i = 0; i < tempPod.spawnCount; i++)
                    //{
                    //    try
                    //    {
                    //        this.SingleSpawnLoop(tempPod, shiftPos, map);
                    //    }
                    //    catch
                    //    {
                    //        Log.Message("Attempted to create a portal but threw an unknown exception - recovering and ending attempt");
                    //        PortalCollapse(shiftPos, map, 3);
                    //        if (pawn != null)
                    //        {
                    //            comp.Mana.CurLevel += comp.ActualManaCost(TorannMagicDefOf.TM_Teleport);
                    //        }
                    //        this.age = this.destructTimer;
                    //        i = tempPod.spawnCount;
                    //        return;
                    //    }
                    //}

                    msg = "PortalCollapseIn".Translate( ((destructTimer - this.age) / 60).ToString()
                        );
                    Messages.Message(msg, MessageTypeDefOf.NeutralEvent);
                    this.primed = false;
                }
                else
                {
                    Messages.Message("InvalidPortal".Translate(), MessageTypeDefOf.RejectInput);
                    comp.Mana.GainNeed(comp.ActualManaCost(TorannMagicDefOf.TM_Teleport));
                    this.destructTimer = 0;
                }
            }
            //foreach (SpawnThings current in this.localSpawnThings)
            //{

            //    string msg = "spawned thing is " + current.def;
            //    Messages.Message(msg, MessageSound.Standard);

            //}

            if (this.age < this.destructTimer)
            {
                if (this.age == (destructTimer * 0.5))
                {
                    msg = "PortalCollapseIn".Translate( 
                        ((destructTimer - this.age) / 60).ToString()
                        );
                    Messages.Message(msg, MessageTypeDefOf.NeutralEvent);
                }
                if (this.age == (destructTimer * 0.75))
                {
                    msg = "PortalCollapseIn".Translate(
                        ((destructTimer - this.age) / 60).ToString()
                        );
                    Messages.Message(msg, MessageTypeDefOf.NeutralEvent);
                }
                if (this.age == (destructTimer * 0.95))
                {
                    //msg = "Portal collapses in " + ((destructTimer - this.age) / 60) + " seconds!!";
                    //Messages.Message(msg, MessageTypeDefOf.ThreatBig);
                    msg = "PortalCollapseIn".Translate(
                        ((destructTimer - this.age) / 60).ToString()
                        );
                    Messages.Message(msg, MessageTypeDefOf.NeutralEvent);
                }
            }
            else
            {
                //age expired, destroy teleport
                this.PortalCollapse(centerCell, map, 3);
            }
            Destroy();
        }

        protected void PortalCollapse(IntVec3 pos, Map map, float radius)
        {
            ThingDef def = this.def;
            Explosion(pos, map, radius, DamageDefOf.Bomb, this.launcher, null, def, this.equipmentDef, TorannMagicDefOf.Mote_Base_Smoke, 0.4f, 1, false, null, 0f, 1);

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

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        public override void ExposeData()
        {
            base.ExposeData();
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
