using RimWorld;
using AbilityUser;
using Verse;
using System.Linq;


namespace TorannMagic
{
    public class Projectile_SummonPylon : Projectile_AbilityBase
    {
        private int age = -1;
        private int duration = 14400;
        private bool primed = false;
        Thing placedThing;
        private int verVal;
        private int pwrVal;

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age <= duration;
            if (!flag)
            {
                //try
                //{
                //    if (!placedThing.Destroyed && placedThing != null)
                //    {
                //        FleckMaker.ThrowSmoke(placedThing.Position.ToVector3(), base.Map, 1);
                //        FleckMaker.ThrowHeatGlow(placedThing.Position, base.Map, 1);
                //        placedThing.Destroy();
                //        Messages.Message("PylonDeSpawn".Translate(), MessageTypeDefOf.SilentInput);
                //    }
                //}
                //catch
                //{
                //    Log.Message("TM_ExceptionClose".Translate(new object[]
                //    {
                //        this.def.defName
                //    }));
                //    base.Destroy(mode);
                //}
                base.Destroy(mode);
            }
        }


        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            GenClamor.DoClamor(this, 2.1f, ClamorDefOf.Impact);
            //base.Impact(hitThing);
            ThingDef def = this.def;
            Pawn victim = hitThing as Pawn;
            Thing item = hitThing as Thing;
            IntVec3 arg_pos_1;

            Pawn pawn = this.launcher as Pawn;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonPylon.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SummonPylon_pwr");
            MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonPylon.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SummonPylon_ver");
            verVal = ver.level;
            pwrVal = pwr.level;
            if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                MightPowerSkill mpwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                MightPowerSkill mver = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                pwrVal = mpwr.level;
                verVal = mver.level;
            }
            CellRect cellRect = CellRect.CenteredOn(base.Position, 1);
            cellRect.ClipInsideMap(map);
            IntVec3 centerCell = cellRect.CenterCell;

            if (!this.primed)
            {
                duration += (verVal * 7200);
                duration = (int)(duration * comp.arcaneDmg);
                arg_pos_1 = centerCell;

                if ((arg_pos_1.IsValid && arg_pos_1.Standable(map)))
                {
                    AbilityUser.SpawnThings tempPod = new SpawnThings();
                    IntVec3 shiftPos = centerCell;
                    centerCell.x++;

                    if (pwrVal == 1)
                    {
                        tempPod.def = ThingDef.Named("DefensePylon_I");
                    }
                    else if (pwrVal == 2)
                    {
                        tempPod.def = ThingDef.Named("DefensePylon_II");
                    }
                    else if (pwrVal == 3)
                    {
                        tempPod.def = ThingDef.Named("DefensePylon_III");
                    }
                    else
                    {
                        tempPod.def = ThingDef.Named("DefensePylon");
                        
                    }
                    tempPod.spawnCount = 1;
                    try
                    {
                        this.SingleSpawnLoop(tempPod, shiftPos, map);
                    }
                    catch
                    {
                        comp.Mana.CurLevel += comp.ActualManaCost(TorannMagicDefOf.TM_SummonPylon);
                        this.age = this.duration;
                        Log.Message("TM_Exception".Translate(
                                pawn.LabelShort,
                                this.def.defName
                            ));
                    }                 

                    this.primed = true;
                }
                else
                {
                    Messages.Message("InvalidSummon".Translate(), MessageTypeDefOf.RejectInput);
                    comp.Mana.GainNeed(comp.ActualManaCost(TorannMagicDefOf.TM_SummonExplosive));
                    this.duration = 0;
                }
            }
            this.age = this.duration;
            Destroy();
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
                        stuff = ThingDefOf.Steel;
                    }
                    Thing thing = ThingMaker.MakeThing(def, stuff);
                    if (thing.def.defName != "Portfuel")
                    {
                        thing.SetFaction(faction, null);
                    }
                    placedThing = thing;
                    CompSummoned bldgComp = thing.TryGetComp<CompSummoned>();
                    bldgComp.TicksToDestroy = this.duration;
                    bldgComp.Temporary = true;
                    GenSpawn.Spawn(thing, position, map, Rot4.North, WipeMode.Vanish, false);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Thing>(ref this.placedThing, "placedThing");
            Scribe_Values.Look<bool>(ref this.primed, "primed", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 7200, false);
        }
    }
}
