using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static partial class SpiritMapCollapseTests
{
    internal static Type Production(string name) => Check.NotNull(typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression." + name), "map collapse " + name + " exists");
    public static IEnumerable<TestCase> Cases()
    {
        foreach(var spell in new[]{"Firebolt","Icebolt","Fireball","Snowball","Heal","AdvancedHeal","Purify","Regenerate","Fireclaw","Poison","CureDisease","SootheAnimal","SootheAnimal_I","SootheAnimal_II","SootheAnimal_III","Rainmaker","Resurrection","HealingCircle","HealingCircle_I","HealingCircle_II","HealingCircle_III","RaiseUndead","Blink","Blink_I","Blink_II","Blink_III","Summon","Summon_I","Summon_II","Summon_III","Stoneskin","DispelStoneskin","EarthSprites","DismissEarthSprites","Encase","Encase_I","Encase_II","Encase_III","EarthernHammer","Sentinel","ShatterSentinel","Meteor","Meteor_I","Meteor_II","Meteor_III","Prediction","AlterFate","AccelerateTime","ReverseTime","TimeMark","Recall","ChainLightning","Enrage","Transmutate","EnchantedBody","EnchantedAura","Shapeshift","ShapeshiftDW","Polymorph","Polymorph_I","Polymorph_II","Polymorph_III","ShadowBolt","ShadowBolt_I","ShadowBolt_II","ShadowBolt_III","SoulBond","Dominate","SummonDemon","Scorn","PsychicShock","Repulsion","Repulsion_I","Repulsion_II","Repulsion_III","Attraction","Attraction_I","Attraction_II","Attraction_III","ShadowStep","ShadowCall","Symbiosis","FadeEmotions","HarvestPassion","IncitePassion","SuppressiveAura","MindKiller","LivingWall","DispelLivingWall","SigilSurge","SigilDrain","DispelBranding","AwarenessBrand","EmotionBrand","FitnessBrand","ProtectionBrand","VitalityBrand","SiphonBrand","EnchantWeapon","DispelEnchantWeapon","EnchanterStone","DismissEnchanterStones","GuardianSpirit","DismissGuardianSpirit","Hex","Hex_I","Hex_II","Hex_III","Hex_Pain","Hex_CriticalFail","Hex_MentalAssault","SummonTotemEarth","SummonTotemLightning","SummonTotemHealing","ChronostaticField","ChronostaticField_I","ChronostaticField_II","ChronostaticField_III","BloodMoon","BloodMoon_I","BloodMoon_II","BloodMoon_III","Rend","Rend_I","Rend_II","Rend_III","BloodForBlood","IgniteBlood","BloodShield","Entertain","BattleHymn","Lullaby","Lullaby_I","Lullaby_II","Lullaby_III","MagicMissile","MagicMissile_I","MagicMissile_II","MagicMissile_III","LightningBolt","LightningCloud","LightningStorm","Firestorm","SpiritOfLight","SoL_Equalize","SoL_CreateLight","LightSkip","LightSkipMass","LightSkipGlobal","Refraction","LightLance","LightBurst","Sunfire","Sunfire_I","Sunfire_II","Sunfire_III","NanoStimulant","TechnoWeapon","TechnoTurret","TechnoShield","Overdrive","OrbitalStrike","OrbitalStrike_I","OrbitalStrike_II","OrbitalStrike_III","Sabotage","FoldReality","Teleport","ValiantCharge","Overwhelm","BestowMight","BestowMight_I","BestowMight_II","BestowMight_III","DeathBolt","DeathBolt_I","DeathBolt_II","DeathBolt_III","FogOfTorment","CorpseExplosion","CorpseExplosion_I","CorpseExplosion_II","CorpseExplosion_III","ConsumeCorpse","ConsumeCorpse_I","ConsumeCorpse_II","ConsumeCorpse_III","ConsumeCorpse_Mass","DeathMark","DeathMark_I","DeathMark_II","DeathMark_III","FrostRay","FrostRay_I","FrostRay_II","FrostRay_III","AMP","AMP_I","AMP_II","AMP_III"})
        yield return new("SpiritProjectedCast."+spell+"RemainsPhysicalWithOriginalOwner", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c=>{
                using var fixture=new Fixture(r,c);
                var body=c.Creator;var soul=c.Original;var magic=soul.GetCompAbilityUserMagic();
                var def = (TMAbilityDef)typeof(TorannMagicDefOf).GetField("TM_" + spell)!.GetValue(null)!;
                var own=new MagicAbility(magic){Def=def};
                magic.AbilityData.Powers.Add(own);magic.AbilityData.AllPowers=null;
                var api=typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression.SpiritHostedBolt",true)!;
                var find=api.GetMethod("Find",BindingFlags.NonPublic|BindingFlags.Static)!;
                Check.Same(own,find.Invoke(null,[body,soul,def]),"native original owns the projected shot");
                var local=body.GetCompAbilityUserMagic();
                var duplicate=new MagicAbility(local){Def=def};
                local.AbilityData.Powers.Add(duplicate);local.AbilityData.AllPowers=null;
                Check.Equal(true,find.Invoke(null,[body,soul,def])==null,"local spell wins without a second shot");
                local.AbilityData.Powers.Remove(duplicate);local.AbilityData.AllPowers=null;
                if (spell == "Blink_II")
                {
                    duplicate.Def = TorannMagicDefOf.TM_Blink;
                    local.AbilityData.Powers.Add(duplicate);local.AbilityData.AllPowers=null;
                    Check.Equal(true, find.Invoke(null,[body,soul,def])==null,"host's own lower rank wins without an extra button");
                }
                Check.Equal(true,find.Invoke(null,[body,body,def])==null,"physical body cannot impersonate the soul");
            }));
        yield return new("GolemancerLifecycle.MasterChoicesKeepBothOwnersAndFilterFaction", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c => {
                using var fixture = new Fixture(r,c);
                var body=c.Creator;
                var soul=c.Original;
                var candidates=typeof(GolemancerLifecycle).GetMethod("Masters",BindingFlags.Static|BindingFlags.NonPublic)!;
                var physical=typeof(GolemancerLifecycle).GetMethod("MasterBody",BindingFlags.Static|BindingFlags.NonPublic)!;
                foreach(var p in new[]{body,soul})
                    p.GetCompAbilityUserMagic().MagicData.MagicPowersGolemancer.Single(x=>x.TMabilityDefs.Contains(TorannMagicDefOf.TM_Golemancy)).learned=true;
                var masters=((IEnumerable<Pawn>)candidates.Invoke(null,[new[]{body},body.Faction])!).ToArray();
                Check.Equal(2,masters.Length,"host and held master may own separate golems");
                Check.Same(body,masters[0],"body's own mastery");
                Check.Same(soul,masters[1],"soul is the other permanent master");
                Check.Same(body,physical.Invoke(null,[soul]),"assigned soul follows through its host");
                Check.Equal(false,((IEnumerable<Pawn>)candidates.Invoke(null,[new[]{body},new Faction()])!).Any(),"foreign golem cannot claim either pawn");
                soul.GetCompAbilityUserMagic().MagicData.MagicPowersGolemancer.Single(x=>x.TMabilityDefs.Contains(TorannMagicDefOf.TM_Golemancy)).learned=false;
                Check.Equal(1,((IEnumerable<Pawn>)candidates.Invoke(null,[new[]{body},body.Faction])!).Count(),"unlearned soul not offered");
            }));
        yield return new("GolemancerTrial.PossessedSurgeonUsesOneLearnedRuneOwner", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c => {
                using var fixture = new Fixture(r,c);
                var local = c.Creator.GetCompAbilityUserMagic();
                var soul = c.Original.GetCompAbilityUserMagic();
                var rune = TorannMagicDefOf.TM_RuneCarving;
                var localPower = local.MagicData.MagicPowersGolemancer.Single(p=>p.TMabilityDefs.Contains(rune));
                var spiritPower = soul.MagicData.MagicPowersGolemancer.Single(p=>p.TMabilityDefs.Contains(rune));
                var resolve = typeof(GolemancerLifecycle).GetMethod("RuneOwner",BindingFlags.Static|BindingFlags.NonPublic)!;
                localPower.learned=false; spiritPower.learned=true;
                Check.Same(soul,resolve.Invoke(null,[c.Creator]),"surgery on the map uses the learned original");
                localPower.learned=true;
                Check.Same(local,resolve.Invoke(null,[c.Creator]),"surgeon's own learned rune takes precedence");
                localPower.learned=false; spiritPower.learned=false;
                Check.Equal(true,resolve.Invoke(null,[c.Creator])==null,"no owner invented");
            }));
        yield return new("EnchanterAspect.OnlyTheOriginalMaySustainAProjectedAspect", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c => {
                using var fixture = new Fixture(r,c);
                var host = c.Creator;
                var soul = c.Original;
                var effect = new HediffWithComps { pawn = host };
                var marker = new HediffComp_EnchanterAspect { parent = effect };
                effect.comps = [marker];
                var owner = typeof(HediffComp_EnchanterAspect).GetMethod("Owner", BindingFlags.NonPublic | BindingFlags.Static)!;
                var field = typeof(HediffComp_EnchanterAspect).GetField("projectedOwner", BindingFlags.NonPublic | BindingFlags.Instance)!;
                Check.Same(host, owner.Invoke(null,[effect]), "unmarked legacy aspect belongs to its body");
                field.SetValue(marker, soul);
                Check.Same(soul, owner.Invoke(null,[effect]), "exact possessed body resolves the soul");
                Check.Equal(false, marker.CompShouldRemove, "linked aspect persists");
                effect.pawn = soul;
                Check.Equal(true, owner.Invoke(null,[effect]) == null && marker.CompShouldRemove, "foreign body cannot keep the enchantment");
            }));
        yield return new("BloodMageTrial.HostBloodUsesSoulRankAndBodyBleeding", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c => {
                using var fixture = new Fixture(r,c);
                var body=c.Creator;var soul=c.Original;
                var magic=soul.GetCompAbilityUserMagic();var local=body.GetCompAbilityUserMagic();
                magic.arcaneDmg=2.5f;local.arcaneDmg=.75f;
                var gift=new MagicAbility(magic) { Def=TorannMagicDefOf.TM_BloodGift };
                magic.AbilityData.Powers.Add(gift);magic.AbilityData.AllPowers=null;
                var meter=new HediffWithComps { pawn=body, def=new HediffDef { defName="TM_BloodHD" } };
                var blood=new HediffComp_Blood { parent=meter };meter.comps=[blood];
                var initialize=typeof(HediffComp_Blood).GetMethod("Initialize",BindingFlags.Instance|BindingFlags.NonPublic)!;
                initialize.Invoke(blood,null);
                var dmg=typeof(HediffComp_Blood).GetField("arcaneDmg",BindingFlags.Instance|BindingFlags.NonPublic)!;
                Check.Equal(2.5f,(float)dmg.GetValue(blood)!,"physical blood accrues with the original's potency");
                Check.Equal(false,blood.CompShouldRemove,"non-blood-mage carrier holds physical blood");
                Check.Same(body,meter.pawn,"blood remains on host");
            }));
        yield return new("ChronomancerRecall.HostMarkRejectsAnotherBodyAndDetachedOwner", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c=>{
                using var fixture = new Fixture(r,c);
                var body=c.Creator; var soul=c.Original; var magic=soul.GetCompAbilityUserMagic();
                var matches=typeof(TM_Action).GetMethod("RecallMatches",BindingFlags.NonPublic|BindingFlags.Static)!;
                bool Match(Pawn pawn)=>(bool)matches.Invoke(null,[pawn,magic])!;
                magic.recallSet=true;
                Check.Equal(true,Match(soul),"old marks without a body ID belong to their original pawn");
                Check.Equal(false,Match(body),"old marks cannot restore bodily health onto a host");
                Set(magic,"recallBodyId",body.thingIDNumber);
                Check.Equal(true,Match(body),"current host owns physical snapshot");
                Check.Equal(false,Match(soul),"cannot apply a host's snapshot to the soul");
                var holder=body.health.hediffSet.hediffs.Select(h=>h.TryGetComp<HediffComp_SpiritPossession>()).First(h=>h!=null);
                holder.GetDirectlyHeldThings().Remove(soul);
                Check.Equal(false,Match(body),"losing custody invalidates old body immediately");
            }));
        yield return new("ChronomancerTrial.PredictionFollowsOnlyItsAttachedOwner", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c => {
                using var fixture=new Fixture(r,c);
                var host=c.Creator; var soul=c.Original;
                var api=typeof(ChronomancerPrediction).GetMethod("Predictor", BindingFlags.Static|BindingFlags.NonPublic)!;
                var hd=new HediffWithComps { pawn=host, def=TorannMagicDefOf.TM_PredictionHD };
                var source=new HediffComp_Prediction { parent=hd }; hd.comps=[source]; host.health.hediffSet.hediffs.Add(hd);
                Check.Same(host,api.Invoke(null,[host])!,"local prediction belongs to the physical mage");
                Set(source,"projectedOwner",soul);
                Check.Same(soul,api.Invoke(null,[host])!,"hosted prediction uses the soul's incident queue and rank");
                fixture.Possession.GetDirectlyHeldThings().Remove(soul);
                Check.Same(null!,api.Invoke(null,[host])!,"released prediction cannot claim a new incident for either pawn");
                Check.Equal(true,source.CompShouldRemove,"unhosted effect expires");
            }));
        yield return new("BardTrials.HostedSongsRequireOriginalOwnerAndAttachedBody", r =>
            SpiritRebirthRitualTransactionTests.Run(r, c => {
                using var fixture = new Fixture(r,c);
                var host = c.Creator; var soul = c.Original;
                var magic = soul.GetCompAbilityUserMagic();
                var performance = typeof(BardTrials).GetMethod("EntertainmentOwner", BindingFlags.Static | BindingFlags.NonPublic)!;
                var inspiration = typeof(BardTrials).GetMethod("InspirationBody", BindingFlags.Static | BindingFlags.NonPublic)!;
                var song = new Hediff { pawn = soul, def = new HediffDef { defName = "TM_EntertainingHD" } };
                soul.health.hediffSet.hediffs.Add(song);
                Check.Same(null!,performance.Invoke(null,[host])!,"song without a retained command grants no host work");
                var own = new MagicAbility(magic) { Def = TorannMagicDefOf.TM_Entertain };
                magic.AbilityData.Powers.Add(own);magic.AbilityData.AllPowers=null;
                Check.Same(soul,performance.Invoke(null,[host])!,"entertainment job belongs to original");
                var local = host.GetCompAbilityUserMagic();
                local.AbilityData.Powers.Add(new MagicAbility(local) { Def = own.Def });local.AbilityData.AllPowers=null;
                Check.Same(null!,performance.Invoke(null,[host])!,"host's local dormant command has precedence");
                var inspire = magic.MagicData.MagicPowersB.Single(row => row.TMabilityDefs.Contains(TorannMagicDefOf.TM_Inspire));
                inspire.learned = false;
                Check.Same(null!,inspiration.Invoke(null,[soul])!,"unlearned aura cannot use borrowed body");
                inspire.learned = true;
                Check.Same(host,inspiration.Invoke(null,[soul])!,"learned aura uses the retained physical host");
                var aura = new Hediff { pawn = host, def = new HediffDef { defName = "TM_InspirationalHD" } };
                host.health.hediffSet.hediffs.Add(aura);
                Check.Same(null!,inspiration.Invoke(null,[soul])!,"local inspiration prevents duplicate roll");
                host.health.hediffSet.hediffs.Remove(aura);
                fixture.Possession.GetDirectlyHeldThings().Remove(soul);
                Check.Same(null!,inspiration.Invoke(null,[soul])!,"detached original has no emitter");
                Set(soul,"mapIndexOrState",(sbyte)0);
                try { Check.Same(null!,performance.Invoke(null,[soul])!,"unhosted soul cannot resume physical performance"); }
                finally { Set(soul,"mapIndexOrState",(sbyte)-1); }
            }));
        yield return new("SpiritProjectedCast.TechnoBitRequiresOriginalLearnedOwner", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c =>
            {
                using var fixture = new Fixture(r,c);
                var soul=c.Original; var host=c.Creator;
                var magic=soul.GetCompAbilityUserMagic();
                var bit=magic.MagicData.MagicPowersT.Single(row=>row.TMabilityDefs.Contains(TorannMagicDefOf.TM_TechnoBit));
                var api=Production("SpiritHostedTechnoBit").GetMethod("Available",BindingFlags.Static|BindingFlags.NonPublic)!;
                bit.learned=false;
                Check.Equal(false,(bool)api.Invoke(null,[host,magic])!,"unlearned bit excluded");
                bit.learned=true;
                Check.Equal(true,(bool)api.Invoke(null,[host,magic])!,"soul's learned bit follows its current host");
                var local=host.GetCompAbilityUserMagic();
                var ownBit=local.MagicData.MagicPowersT.Single(row=>row.TMabilityDefs.Contains(TorannMagicDefOf.TM_TechnoBit));
                ownBit.learned=true;
                Check.Equal(false,(bool)api.Invoke(null,[host,magic])!,"host's own bit has precedence");
                ownBit.learned=false;
                fixture.Possession.GetDirectlyHeldThings().Remove(soul);
                Check.Equal(false,(bool)api.Invoke(null,[host,magic])!,"release disables stale commands");
            }));
        yield return new("BrightmageLifecycle.MovingSunfireBeamsPreserveTheirCurves", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c =>
            {
                using var fixture = new Fixture(r,c);
                var mat = typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader),typeof(int)])!;
                using var skipMaterial = new Patch(mat,nameof(CancelNative));
                using var baseMaterial = new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
                var definition = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                definition.defName="RegressionSunfireField"; definition.thingClass=typeof(Projectile_Sunfire); definition.comps=[]; definition.category=ThingCategory.Projectile;
                DefDatabase<ThingDef>.Add(definition);
                var field = new Projectile_Sunfire {def=definition, thingIDNumber=19866, usedTarget=IntVec3.Zero, intendedTarget=IntVec3.Zero};
                var origin = new UnityEngine.Vector3(12,0,13);
                var curve = new UnityEngine.Vector3(.0001f,0,-.0002f);
                Set(field,"launcher",c.Creator);Set(field,"initialized",true);
                Set(field,"sfBeams",new List<UnityEngine.Vector3>{origin});
                Set(field,"sfBeamsROM",new List<UnityEngine.Vector3>{new(.03f,0,.1f)});
                Set(field,"sfBeamsCurve",new List<UnityEngine.Vector3>{curve});
                Set(field,"sfBeamsStep",new List<int>{9});Set(field,"sfBeamsStartTick",new List<int>{11});
                field=RoundTripSummon(r,"sunfire-live-beams",field,c.Creator);
                var flags=BindingFlags.NonPublic|BindingFlags.Instance;
                Check.Equal(origin,((List<UnityEngine.Vector3>)typeof(Projectile_Sunfire).GetField("sfBeams",flags)!.GetValue(field)!).Single(),"beam resumes from actual cell");
                var restored=(List<UnityEngine.Vector3>)typeof(Projectile_Sunfire).GetField("sfBeamsCurve",flags)!.GetValue(field)!;
                Check.Equal(1,restored.Count,"one saved curved trajectory");
                Check.Equal(curve,restored[0],"curve is distinct from current beam position");
                Check.Equal(9,((List<int>)typeof(Projectile_Sunfire).GetField("sfBeamsStep",flags)!.GetValue(field)!).Single(),"remaining steps survive");
                var path=Path.Combine(r.WorkingDirectory,"sunfire-live-beams.xml");
                var xml=XDocument.Load(path);
                var saved=xml.Descendants("summon").Single();
                saved.Element("TM_sunfireCurvature")!.Remove();
                saved.Add(new XElement("sfBeamsCurve",saved.Element("sfBeams")!.Nodes().Select(n=>new XElement((XElement)n))));
                xml.Save(path);
                var culture=System.Globalization.CultureInfo.CurrentCulture;
                Projectile_Sunfire? legacy=null;
                try
                {
                    System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
                    Scribe.loader.InitLoading(path);
                    Scribe_Deep.Look(ref legacy,"summon");
                    var directory=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",flags)!.GetValue(Scribe.loader.crossRefs)!;
                    directory.RegisterLoaded(c.Creator);
                    Scribe.loader.FinalizeLoading();
                }
                finally { Scribe.ForceStop(); System.Globalization.CultureInfo.CurrentCulture=culture; }
                Check.Equal(UnityEngine.Vector3.zero,((List<UnityEngine.Vector3>)typeof(Projectile_Sunfire).GetField("sfBeamsCurve",flags)!.GetValue(legacy)!).Single(),"legacy positions are never interpreted as curvature");
                Check.Equal(origin,((List<UnityEngine.Vector3>)typeof(Projectile_Sunfire).GetField("sfBeams",flags)!.GetValue(legacy)!).Single(),"old beam keeps its position without restarting");
            }));
        yield return new("BrightmageLifecycle.ActiveLightLanceKeepsDurationAndOrigin", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c =>
            {
                using var fixture = new Fixture(r,c);
                using var material = new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader),typeof(int)])!,nameof(CancelNative));
                using var baseMaterial = new Patch(typeof(MaterialPool).GetMethod("MatFrom",[typeof(string),typeof(UnityEngine.Shader)])!,nameof(CancelNative));
                var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.defName = "RegressionLanceBeam"; def.thingClass = typeof(Projectile_LightLance); def.comps = []; def.category = ThingCategory.Projectile;
                DefDatabase<ThingDef>.Add(def);
                var start = new IntVec3(7,0,12);
                var beam = new Projectile_LightLance {def=def, thingIDNumber=19867, burnTime=244, usedTarget=IntVec3.Zero, intendedTarget=IntVec3.Zero};
                Set(beam,"launcher",c.Creator); Set(beam,"launchPosition",start); Set(beam,"age",78);
                Set(beam,"origin",start.ToVector3Shifted()); Set(beam,"ticksToImpact",-2);
                Set(beam,"initialized",true); Set(beam,"pwrVal",2);
                beam=RoundTripSummon(r,"lance-active-beam",beam,c.Creator);
                var flags=BindingFlags.NonPublic|BindingFlags.Instance;
                Check.Equal(true,(bool)typeof(Projectile_LightLance).GetField("initialized",flags)!.GetValue(beam)!, "already impacted beam stays initialized");
                Check.Equal(start,(IntVec3)typeof(Projectile_LightLance).GetField("launchPosition",flags)!.GetValue(beam)!, "beam anchored to original cell");
                typeof(Projectile_LightLance).GetMethod("Impact",flags)!.Invoke(beam,[null,false]);
                Check.Equal(244,beam.burnTime,"reload does not extend beam lifetime");
                Check.Equal(166,(int)typeof(Projectile_LightLance).GetProperty("TicksLeft",flags)!.GetValue(beam)!, "exact remaining lifetime");
                var file=Path.Combine(r.WorkingDirectory,"lance-active-beam.xml");
                var xml=XDocument.Load(file);
                var record=xml.Descendants("summon").Single();
                record.Element("TM_lanceImpacted")!.Remove();
                record.Element("TM_lanceOrigin")!.Remove();
                xml.Save(file);
                var culture=System.Globalization.CultureInfo.CurrentCulture;
                Projectile_LightLance? old=null;
                try
                {
                    System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
                    Scribe.loader.InitLoading(file);
                    Scribe_Deep.Look(ref old,"summon");
                    var directory=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",flags)!.GetValue(Scribe.loader.crossRefs)!;
                    directory.RegisterLoaded(c.Creator);
                    Scribe.loader.FinalizeLoading();
                }
                finally { Scribe.ForceStop(); System.Globalization.CultureInfo.CurrentCulture=culture; }
                Check.Equal(start,(IntVec3)typeof(Projectile_LightLance).GetField("launchPosition",flags)!.GetValue(old)!, "old impacted beam uses projectile origin");
                typeof(Projectile_LightLance).GetMethod("Impact",flags)!.Invoke(old,[null,false]);
                Check.Equal(244,old!.burnTime,"legacy impacted beam does not add its rank twice");
            }));
        yield return new("BrightmageLifecycle.RefractionKeepsChosenEndpoint", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c =>
            {
                using var fixture = new Fixture(r,c);
                var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.defName = "RegressionRefraction"; def.thingClass = typeof(Projectile_Refraction); def.comps = []; def.category = ThingCategory.Projectile;
                DefDatabase<ThingDef>.Add(def);
                var field = new Projectile_Refraction {def=def, thingIDNumber=19868, usedTarget=IntVec3.Zero, intendedTarget=IntVec3.Zero};
                var end = new LocalTargetInfo(new IntVec3(7,0,12));
                Set(field,"secondTarget",end); Set(field,"initialized",true); Set(field,"caster",c.Creator);
                field=RoundTripSummon(r,"refraction-chosen-end",field,c.Creator);
                var flags=BindingFlags.NonPublic|BindingFlags.Instance;
                Check.Equal(end,(LocalTargetInfo)typeof(Projectile_Refraction).GetField("secondTarget",flags)!.GetValue(field)!, "selected endpoint survives before next impact");
                Check.Same(c.Creator,typeof(Projectile_Refraction).GetField("caster",flags)!.GetValue(field)!, "payer survives reload");
                var cells=new List<IntVec3>{new(5,0,12),new(6,0,12)};
                Set(field,"secondTarget",LocalTargetInfo.Invalid); Set(field,"wallActive",true);
                Set(field,"wallPositions",cells); Set(field,"mirrored",new HashSet<int>{312});
                Set(field,"wallEnergy",625f); Set(field,"age",125);
                field=RoundTripSummon(r,"refraction-active-field",field,c.Creator);
                Check.Equal(true,((HashSet<int>)typeof(Projectile_Refraction).GetField("mirrored",flags)!.GetValue(field)!).SetEquals([312]), "already mirrored crossing survives reload");
                Check.Equal(true,((List<IntVec3>)typeof(Projectile_Refraction).GetField("wallPositions",flags)!.GetValue(field)!).SequenceEqual(cells), "same wall cells survive");
                Check.Equal(625f,(float)typeof(Projectile_Refraction).GetField("wallEnergy",flags)!.GetValue(field)!, "remaining wall energy survives");
            }));
        yield return new("BrightmageLifecycle.MassLightSkipResumesPendingCapsules", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c =>
            {
                using var fixture = new Fixture(r,c);
                var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.defName="RegressionMassLightSkip"; def.thingClass=typeof(Projectile_LightSkipMass); def.comps=[]; def.category=ThingCategory.Projectile;
                DefDatabase<ThingDef>.Add(def);
                var shot=new Projectile_LightSkipMass {def=def,thingIDNumber=19869,usedTarget=IntVec3.Zero,intendedTarget=IntVec3.Zero};
                var start=new IntVec3(31,0,18);
                Set(shot,"launcherPosition",start); Set(shot,"launchedFlag",true); Set(shot,"initialized",true); Set(shot,"draftFlag",true);
                var podDef=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); podDef.defName="TM_LightPod";
                var pod=new ThingWithComps {def=podDef,thingIDNumber=19870};
                Set(shot,"pods",new List<Thing>{pod});
                shot=RoundTripSummon(r,"mass-skip-in-progress",shot,pod);
                var flags=BindingFlags.NonPublic|BindingFlags.Instance;
                object Read(string name)=>typeof(Projectile_LightSkipMass).GetField(name,flags)!.GetValue(shot)!;
                Check.Equal(start,(IntVec3)Read("launcherPosition"),"formation keeps its launch cell");
                Check.Equal(true,(bool)Read("draftFlag"),"original draft status retained");
                Check.Same(pod,((List<Thing>)Read("pods")).Single(),"pending transporter reference retained exactly once");
            }));
        yield return new("BrightmageLifecycle.LightSkipFlightUsesNativePassengerManifest", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c =>
            {
                var trip = new TorannMagic.WorldTransport.TM_TravelingTransportPods();
                var pod = new ActiveTransporterInfo();
                trip.AddPod(pod,true);
                var holders = new List<IThingHolder>();
                trip.GetChildHolders(holders);
                Check.Equal(1,holders.Count,"native save and arrival must see the light transporter");
                Check.Same(pod,holders.Single(),"exact transporter retained");
                Check.Same(trip,pod.parent,"single world owner");
            }));
        yield return new("BrightmageLifecycle.LightSkipArrivalRestoresOldDestination", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c =>
            {
                var parent = new RimWorld.Planet.MapParent {ID=19335};
                var cell = new IntVec3(9,0,13);
                var action = new TorannMagic.WorldTransport.TM_TransportPodsArrivalAction_LandAtExactCell(parent,cell,true);
                var path=Path.Combine(r.WorkingDirectory,"light-arrival.xml");
                try { Scribe.saver.InitSaving(path,"trial"); Scribe_Deep.Look(ref action,"arrival"); Scribe.saver.FinalizeSaving(); }
                finally { Scribe.ForceStop(); }
                var xml=XDocument.Load(path);
                var row=xml.Descendants("arrival").Single();
                Check.Equal(1,row.Elements("mapParent").Count(),"new action has one authoritative destination");
                Check.Equal(1,row.Elements("cell").Count(),"new action has one authoritative landing cell");
                row.AddFirst(new XElement("mapParent","null"),new XElement("cell","(0, 0, 0)"));
                xml.Save(path);
                action=null;
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                try
                {
                    Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref action,"arrival");
                    ((LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",flags)!.GetValue(Scribe.loader.crossRefs)!).RegisterLoaded(parent);
                    Scribe.loader.FinalizeLoading();
                }
                finally { Scribe.ForceStop(); }
                var native=typeof(RimWorld.Planet.TransportersArrivalAction_LandInSpecificCell);
                Check.Same(parent,native.GetField("mapParent",flags)!.GetValue(action)!,"original destination survives legacy duplicate");
                Check.Equal(cell,(IntVec3)native.GetField("cell",flags)!.GetValue(action)!,"original landing cell survives legacy duplicate");
                Check.Equal(true,action!.draftFlag,"draft instruction preserved");
            }));
        foreach (bool impacted in new[] { false, true })
            yield return new("SpiritProjectedCast.SabotageScribeAfterImpact" + impacted, r =>
                SpiritRebirthRitualTransactionTests.Run(r, c =>
                {
                    using var fixture = new Fixture(r,c);
                    using var material = new Patch(typeof(MaterialPool).GetMethod("MatFrom", [typeof(string),typeof(UnityEngine.Shader)])!, nameof(CancelNative));
                    var type = typeof(Projectile_SpiritBolt).Assembly.GetType("TorannMagic.Projectile_Sabotage",true)!;
                    var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                    def.defName = "RegressionSabotage" + impacted; def.thingClass = type; def.category = ThingCategory.Projectile; def.comps = [];
                    DefDatabase<ThingDef>.Add(def);
                    var shot = (Projectile_SpiritBolt)Activator.CreateInstance(type)!;
                    shot.def = def; shot.thingIDNumber = 19884; shot.usedTarget = shot.intendedTarget = IntVec3.Zero;
                    Set(shot,"launcher",c.Creator); Set(shot,"initialized",impacted);
                    var magic = c.Original.GetCompAbilityUserMagic();
                    magic.MagicData.MagicPowerSkill_Sabotage.Single(s=>s.label=="TM_Sabotage_pwr").level=2;
                    var power = new MagicAbility(magic){ Def=TorannMagicDefOf.TM_Sabotage };
                    typeof(Projectile_SpiritBolt).GetMethod("PrepareProjection",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(shot,[power]);
                    shot = RoundTripSummon(r, "sabotage-impact-"+impacted,shot,c.Creator,c.Original);
                    fixture.Possession.GetDirectlyHeldThings().Remove(c.Original);
                    Check.Equal(impacted,(bool)type.GetField("initialized",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot)!,"impact receipt survives load");
                    Check.Same(c.Creator,shot.Launcher,"physical origin retained");
                    Check.Same(c.Original,typeof(Projectile_SpiritBolt).GetProperty("SourcePawn",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot),"owner remains with projectile after exit");
                    Check.Equal(2,(int)typeof(Projectile_SpiritBolt).GetField("pwrVal",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shot)!,"captured rank survives load");
                }));
        yield return new("SpiritProjectedCast.ValiantChargeCarrierOwnsPassenger", r =>
        {
            Check.Equal(true,typeof(IThingHolder).IsAssignableFrom(typeof(FlyingObject_ValiantCharge)),"flight exposes native passenger ownership");
        });
        yield return new("SpiritProjectedCast.PoisonRetainsMovedVictimBodyAndOriginalRanksAfterLoad", r =>
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                using var shadow = new Patch(typeof(MaterialPool).GetMethod("MatFrom", [typeof(string), typeof(UnityEngine.Shader)])!, nameof(CancelNative));
                RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(BodyPartTagDefOf));
                var victim = c.Creator;
                var previousBody = victim.def.race.body;
                var previousHeartTag = BodyPartTagDefOf.BloodPumpingSource;
                try
                {
                    BodyPartTagDefOf.BloodPumpingSource = new BodyPartTagDef { defName = "TestHeartTag" };
                    var body = new BodyDef();
                    var heart = new BodyPartRecord { body = body, def = new BodyPartDef { defName = "TestPoisonHeart", tags = [BodyPartTagDefOf.BloodPumpingSource] } };
                    body.corePart = heart; body.AllParts.Add(heart); victim.def.race.body = body;
                    var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                    def.defName = "RegressionPoison"; def.thingClass = typeof(Projectile_Poison);
                    def.comps = []; def.category = ThingCategory.Projectile; DefDatabase<ThingDef>.Add(def);
                    var shot = new Projectile_Poison { def = def, thingIDNumber = 19551, intendedTarget = IntVec3.Zero, usedTarget = IntVec3.Zero };
                    var owner = c.Original.GetCompAbilityUserMagic();
                    var rank = owner.MagicData.MagicPowerSkill_Poison.Single(x => x.label == "TM_Poison_pwr"); rank.level = 2;
                    var power = new MagicAbility(owner) { Def = TorannMagicDefOf.TM_Poison };
                    typeof(Projectile_SpiritBolt).GetMethod("PrepareProjection", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(shot, [power]);
                    var origin = new IntVec3(9, 0, 11);
                    Set(shot, "initialized", true); Set(shot, "age", 400); Set(shot, "lastPoison", 271);
                    Set(shot, "oldPosition", origin); Set(shot, "hitPawn", victim); Set(shot, "launcher", victim);
                    rank.level = 0;
                    shot = RoundTripSummon(r, "poison-after-first-dose", shot, c.Original, victim);
                    object Field(string name) => typeof(Projectile_Poison).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(shot)!;
                    Check.Same(heart, ((BodyPartRecord[])Field("vulnerableParts"))[0], "body lookup rebuilt from saved victim");
                    Check.Same(victim, Field("hitPawn"), "persistent poison stays on the same pawn");
                    Check.Same(c.Original, typeof(Projectile_SpiritBolt).GetProperty("SourcePawn", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(shot)!, "persistent poison credits the soul");
                    Check.Equal(2, (int)Field("pwrVal"), "damage uses the paid rank, even after the soul's rank changes");
                    Check.Equal(origin, (IntVec3)Field("oldPosition"), "resuming preserves distance moved since the last dose");
                    Check.Equal(271, (int)Field("lastPoison"), "the previous dose is not replayed");
                    RuntimeLog.AssertClean();
                }
                finally { victim.def.race.body = previousBody; BodyPartTagDefOf.BloodPumpingSource = previousHeartTag; }
            }));
        yield return new("SpiritProjectedCast.DeathMarkTrialFollowsPaidSoulAfterExit", r =>
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                var state = new ClassProgressionState(Vocation.Mage);
                Set(ClassIdentity.Component(c.Original), "state", state);
                state.TryAddFoundation("rwom.necromancer", DisciplineDomain.Magic);
                var path = state.Find("rwom.necromancer");
                path.RecordCompletedTrial(NecromancerControlTrial.TrialId);
                state.TryAdvanceAfterTrial(path.ClassId, InitiationStage.Foundation, NecromancerControlTrial.TrialId);
                var friend = new Faction { def = new FactionDef { isPlayer = true } };
                var enemy = new Faction { def = new FactionDef() };
                Set(friend, "relations", new List<FactionRelation>{new(){other=enemy,kind=FactionRelationKind.Hostile}});
                Set(enemy, "relations", new List<FactionRelation>{new(){other=friend,kind=FactionRelationKind.Hostile}});
                Set(c.Original,"factionInt",friend); Set(c.Creator,"factionInt",friend);
                var target = PawnWithComps(false,false).pawn;
                Set(target,"factionInt",enemy); Set(target,"mapIndexOrState",(sbyte)0);
                target.guest = new Pawn_GuestTracker(target); target.mindState = new Verse.AI.Pawn_MindState(target);
                target.kindDef = new PawnKindDef { defName = "TestMarkedPawn" };
                var curse = new HediffWithComps { def = new HediffDef { defName = "TM_DeathMarkCurse" }, pawn = target };
                var tracker = new HediffComp_DeathMarkTrial { parent = curse, props = new HediffCompProperties_DeathMarkTrial() };
                curse.comps = [tracker]; Set(curse,"severityInt",8f); target.health.hediffSet.hediffs.Add(curse);
                var def = TorannMagicDefOf.TM_DeathMark_III;
                var owner = c.Original.GetCompAbilityUserMagic();
                var mark = new MagicAbility(owner) { Def = def };
                owner.AbilityData.Powers.Add(mark); owner.AbilityData.AllPowers = null;
                NecromancerDeathMarkTrial.Applied(c.Creator,target,mark,8);
                var sources = (List<Pawn>)typeof(HediffComp_DeathMarkTrial).GetField("sources", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(tracker)!;
                Check.Same(c.Original,sources.Single(),"rank-three curse records the soul while the body is on the target map");
                fixture.Possession.GetDirectlyHeldThings().Remove(c.Original);
                NecromancerDeathMarkTrial.Applied(c.Creator,target,mark,8);
                tracker.Notify_PawnKilled(); Set(target.health,"healthState",PawnHealthState.Dead); Set(target,"mapIndexOrState",(sbyte)-2); tracker.Notify_PawnDied(null);
                Check.Equal(true,path.CompletedTrials.Contains(NecromancerDeathMarkTrial.TrialId),"death after exit credits the earlier real mark");
            }));
        yield return new("SpiritProjectedCast.RegrowthCreatesSeedOnHostMap", r =>
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.defName = "SeedofRegrowth"; def.thingClass = typeof(Thing); def.category = ThingCategory.Item;
                def.stackLimit = 10; def.size = new IntVec2(1, 1); def.drawerType = DrawerType.RealtimeOnly; def.comps = [];
                DefDatabase<ThingDef>.Add(def);
                var soul = c.Original.GetCompAbilityUserMagic();
                var power = new MagicAbility(soul) { Def = TorannMagicDefOf.TM_RegrowLimb };
                soul.AbilityData.Powers.Add(power); soul.AbilityData.AllPowers = null;
                var api = Production("SpiritHostedBolt");
                Check.Same(power, api.GetMethod("Find", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [c.Creator,c.Original,power.magicDef]), "held soul supplies regrowth");
                var cell = new IntVec3(16, 0, 16);
                var verb = new Verb_RegrowLimb { caster = c.Creator, Ability = power, verbProps = power.magicDef.MainVerb };
                var cast = typeof(Verb_SpiritTargetable).GetMethod("CastFromHost", BindingFlags.NonPublic | BindingFlags.Instance)!;
                Check.Equal(true, (bool)cast.Invoke(verb, [new LocalTargetInfo(cell)])!, "successful native placement commits the hosted effect");
                var seed = cell.GetThingList(c.Map).Single(t => t.def == def);
                Check.Equal(1, seed.stackCount, "exactly one seed at the selected cell");
            }));
        yield return new("SpiritProjectedCast.RegrowthSurgeryChargesOnlyQualifiedOwner", r =>
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                var owner = c.Original.GetCompAbilityUserMagic();
                var local = c.Creator.GetCompAbilityUserMagic();
                var def = TorannMagicDefOf.TM_RegrowLimb;
                var spell = new MagicAbility(owner) { Def = def };
                owner.AbilityData.Powers.Add(spell); owner.AbilityData.AllPowers = null;
                var policy = Production("RegrowthSurgery");
                var resolve = policy.GetMethod("QualifiedOwner", BindingFlags.Static | BindingFlags.NonPublic)!;
                owner.spell_RegrowLimb = true;
                Check.Equal(null, resolve.Invoke(null, [c.Original]), "soul cannot perform former class surgery independently");
                Check.Same(owner, resolve.Invoke(null, [c.Creator]), "host's personal magic does not displace the trained soul");
                var rank = owner.MagicData.MagicPowerSkill_RegrowLimb.Single(s => s.label == "TM_RegrowLimb_eff");
                rank.level = 2; owner.arcaneDmg = 1;
                if (owner.Mana == null) c.Original.needs.AllNeeds.Add(new Need_Mana(c.Original) { def = TorannMagicDefOf.TM_Mana });
                float debit = .9f * (1f - 2 * .08f);
                owner.Mana.CurLevel = debit;
                var pay = policy.GetMethod("TryPay", BindingFlags.Static | BindingFlags.NonPublic)!;
                var localBefore = local.Mana?.CurLevel;
                Check.Equal(true, (bool)pay.Invoke(null,[owner,.08f])!, "exact threshold pays");
                Check.Equal(true, Math.Abs(owner.Mana.CurLevel) < .0001f, "debit uses original efficiency");
                Check.Equal(localBefore, local.Mana?.CurLevel, "host mana untouched");
                Check.Equal(false, (bool)pay.Invoke(null,[owner,.08f])!, "depleted original cannot pay again");
                owner.AbilityData.Powers.Remove(spell); owner.AbilityData.AllPowers = null;
                Check.Equal(null, resolve.Invoke(null,[c.Creator]), "missing original spell blocks surgery");
            }));
        yield return new("SpiritProjectedCast.LightningCloudPreviewUsesSoulRank", r =>
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                var magic = c.Original.GetCompAbilityUserMagic();
                var power = new MagicAbility(magic) { Def = TorannMagicDefOf.TM_LightningCloud };
                var projectile = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                projectile.projectile = new ProjectileProperties { explosionRadius = 6 };
                var props = new AbilityUser.VerbProperties_Ability { abilityDef = power.magicDef, defaultProjectile = projectile };
                var verb = new Verb_LightningCloud { caster = c.Creator, Ability = power, verbProps = props };
                magic.MagicData.MagicPowerSkill_LightningCloud.Single(x => x.label == "TM_LightningCloud_ver").level = 3;
                Check.Equal(7f, verb.HighlightFieldRadiusAroundTarget(out var needsSight), "preview includes three versatility ranks from the held soul");
                Check.Equal(true, needsSight, "native targeting checks sight to the center");
            }));
        yield return new("SpiritProjectedCast.BlizzardKeepsSoulRanksAfterLaunch", r =>
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                var magic = c.Original.GetCompAbilityUserMagic();
                var spell = TorannMagicDefOf.TM_Blizzard;
                var power = new MagicAbility(magic) { Def = spell };
                magic.AbilityData.Powers.Add(power); magic.AbilityData.AllPowers = null;
                var skills = magic.MagicData.MagicPowerSkill_Blizzard;
                skills.Single(x => x.label == "TM_Blizzard_pwr").level = 2;
                var vers = skills.Single(x => x.label == "TM_Blizzard_ver");
                vers.level = 3;
                magic.arcaneDmg = 2;
                var job = new JobDef { defName = "TM_SpiritBlizzard" };
                var api = Production("SpiritHostedBolt");
                Check.Same(spell, api.GetMethod("Spell", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [job]), "job resolves to original Blizzard");
                Check.Same(power, api.GetMethod("Find", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [c.Creator, c.Original, spell]), "currently held original owns the cast");
                var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.projectile = new ProjectileProperties { explosionRadius = 6 };
                var props = new AbilityUser.VerbProperties_Ability { abilityDef = spell, defaultProjectile = def };
                var verb = new Verb_Blizzard { caster = c.Creator, Ability = power, verbProps = props };
                Check.Equal(8.5f, verb.HighlightFieldRadiusAroundTarget(out _), "both original ranks contribute to radius");
                var shot = new Projectile_Blizzard { def = def };
                typeof(Projectile_SpiritBolt).GetMethod("PrepareProjection", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(shot, [power]);
                vers.level = 0;
                shot.Initialize(c.Map);
                var duration = typeof(Projectile_Blizzard).GetField("duration", BindingFlags.Instance | BindingFlags.NonPublic)!;
                Check.Equal(1260, (int)duration.GetValue(shot)!, "duration uses the ranks and scale at launch without reading the host");
                shot.Initialize(c.Map);
                Check.Equal(1260, (int)duration.GetValue(shot)!, "rebuilding the rectangle does not extend the storm");
            }));
        yield return new("SpiritPossession.LegacySummonReleasesClassicSoul", r =>
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                var host = c.Creator; var soul = c.Original;
                host.def.race.lifeStageAges = soul.def.race.lifeStageAges;
                host.ageTracker ??= new Pawn_AgeTracker(host);
                host.ageTracker.AgeBiologicalTicks = 25L * GenDate.TicksPerYear;
                Set(ClassIdentity.Component(soul), "spiritRebirth", null);
                var timed = new CompSummoned { parent = host, Temporary = true };
                host.AllComps.Add(timed);
                var native = host.GetCompAbilityUserMagic();
                host.AllComps.Remove(native); TM_Calc.InvalidateCompCache(host);
                var prior = TorannMagicDefOf.TM_SpiritPossessorHD;
                try
                {
                    TorannMagicDefOf.TM_SpiritPossessorHD = new HediffDef { defName = "ClassicSpiritOwnerTest" };
                    var spiritHediff = new Hediff_Possessor { def = TorannMagicDefOf.TM_SpiritPossessorHD, pawn = soul, previousFaction = host.Faction.def };
                    soul.health.hediffSet.hediffs.Add(spiritHediff);
                    using var visuals = new Patch(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowGenericMote))!, nameof(CancelNative));
                    var dismiss = Production("SpiritSummonedHost").GetMethod("CanDismiss", BindingFlags.NonPublic | BindingFlags.Static)!;
                    Check.Equal(true, (bool)dismiss.Invoke(null, [host])!, "old occupied summon can finish dismissal");
                    Check.Same(c.Map, soul.Map, "same classic pawn returned to the map");
                    Check.Equal(false, fixture.Possession.GetDirectlyHeldThings().Contains(soul), "old container no longer owns the pawn");
                    Check.Equal(true, (bool)dismiss.Invoke(null, [host])!, "retry after successful release is safe");
                }
                finally { TorannMagicDefOf.TM_SpiritPossessorHD = prior; host.AllComps.Remove(timed); host.AllComps.Add(native); TM_Calc.InvalidateCompCache(host); }
            }));
        yield return new("SpiritMapCollapse.DisabledBetaSecuresCommittedHostDeath", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c => {
                using var f=new Fixture(r,c);
                var state=ClassIdentity.Component(c.Original).SpiritRebirth;
                bool beta=ClassProgressionFeature.Enabled;
                try
                {
                    ClassProgressionFeature.Enabled=false;
                    c.Creator.health.SetDead();
                    Production("SpiritHostSetDeadPatch").GetMethod("Postfix",BindingFlags.Static|BindingFlags.NonPublic)!
                        .Invoke(null,[c.Creator,true]);
                    var stone=Check.NotNull(state.Stone,"death while beta is off still secures the pawn");
                    Check.Same(c.Original,stone.ContainedSpirit,"same soul in stone");
                    Check.Equal(false,SpiritRebirthState.TryGet(c.Original,out _),"disabled gameplay facade remains unavailable");
                }
                finally { ClassProgressionFeature.Enabled=beta; }
                Check.Equal(true,state.HasValidStone(c.Original),"reenabling recognizes the secured stone");
            }));
        yield return new("SpiritPossession.DisabledBetaKeepsRebornPawnHeld", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c => {
                using var fixture=new Fixture(r,c);
                var state=ClassIdentity.Component(c.Original).SpiritRebirth;
                int recovery=state.RecoveryTicks;
                bool active=ClassProgressionFeature.Enabled;
                try
                {
                    ClassProgressionFeature.Enabled=false;
                    float severity=0f;
                    fixture.Possession.CompPostTick(ref severity);
                    TM_Action.RemovePossession(c.Creator,c.Creator.Position,false);
                    Check.Same(c.Original,fixture.Possession.SpiritPawn,"disabled beta retains the original instead of releasing a classic spirit");
                    Check.Equal(recovery,state.RecoveryTicks,"paused possession cannot progress its clock");
                    Check.Equal(false,SpiritRebirthState.TryGetPossessingSpirit(c.Creator,out _),"paused powers unavailable");
                }
                finally { ClassProgressionFeature.Enabled=active; }
                Check.Equal(true,state.HasValidHost(c.Original),"re-enabled beta sees the same reciprocal holder");
            }));
        yield return new("SpiritPossession.SummonedHostsRejectedEvenByDirectEntry", r =>
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                var api = Production("SpiritSummonedHost");
                var check = api.GetMethod("IsSummoned", BindingFlags.NonPublic | BindingFlags.Static)!;
                bool Excluded(Pawn p) => (bool)check.Invoke(null, [p])!;
                Check.Equal(false, Excluded(c.Creator), "ordinary host allowed");
                foreach (var summoned in new Pawn[] { new TMPawnSummoned(), new TorannMagic.Odyssey.Pawn_EtherServant(), new TorannMagic.Golems.TMPawnGolem() })
                {
                    Check.Equal(true, Excluded(summoned), "summoned race excluded before touching its faction or health");
                    Check.Equal(false, new Projectile_SpiritPossession().Possess(c.Creator, summoned), "classic Spirit rejects a summoned host");
                    Check.Equal(false, new Projectile_SpiritPossession().Possess(c.Original, summoned), "ritual Spirit rejects the same host");
                }
                var temp = new CompSummoned { parent = c.Creator, Temporary = true };
                c.Creator.AllComps.Add(temp);
                try
                {
                    Check.Equal(true, Excluded(c.Creator), "ordinary pawn with a timed summon component excluded");
                    Check.Equal(false, new Projectile_SpiritPossession().Possess(c.Original, c.Creator), "direct impact cannot bypass prohibition");
                    var blocks = api.GetMethod("BlocksTarget", BindingFlags.NonPublic | BindingFlags.Static)!;
                    bool Blocked(LocalTargetInfo target) => (bool)blocks.Invoke(null, [target, c.Map])!;
                    Check.Equal(true, Blocked(c.Creator), "reject explicit target before warmup");
                    var cell = new IntVec3(29, 0, 29);
                    var things = cell.GetThingList(c.Map);
                    things.Add(c.Creator);
                    try { Check.Equal(true, Blocked(cell), "targeting the occupied cell cannot bypass the block"); }
                    finally { things.Remove(c.Creator); }
                    var corpse = new Corpse();
                    corpse.InnerPawn = new TorannMagic.Odyssey.Pawn_EtherServant { def = c.Creator.def, thingIDNumber = 59898 };
                    Check.Equal(true, Blocked(corpse), "corpse is classified by its inner pawn");
                    var dismiss = api.GetMethod("CanDismiss", BindingFlags.NonPublic | BindingFlags.Static)!;
                    var state = ClassIdentity.Component(c.Original).SpiritRebirth;
                    Check.Equal(false, state.CanVoluntarilyLeaveHost(c.Original), "the old bond is still recovering");
                    var mapIndex = typeof(Thing).GetField("mapIndexOrState", BindingFlags.NonPublic | BindingFlags.Instance)!;
                    var prior = mapIndex.GetValue(c.Creator);
                    try
                    {
                        mapIndex.SetValue(c.Creator, (sbyte)-1);
                        Check.Equal(false, (bool)dismiss.Invoke(null, [c.Creator])!, "a host without a destination cannot disappear with its soul");
                        Check.Same(c.Original, fixture.Possession.SpiritPawn, "soul still has the same holder");
                    }
                    finally { mapIndex.SetValue(c.Creator,prior); }
                    Check.Equal(true, (bool)dismiss.Invoke(null, [c.Creator])!, "old host can disappear after the soul has been secured");
                    var stone = Check.NotNull(state.Stone, "ritual soul becomes a real stone");
                    Check.Same(c.Original, stone.ContainedSpirit, "the stone holds exactly the original pawn");
                    Check.Equal(0, state.RecoveryTicks, "forced collapse restarts recovery");
                    Check.Equal(false, (bool)Production("SoulStoneImplant").GetMethod("Valid", BindingFlags.NonPublic | BindingFlags.Static)!
                        .Invoke(null, [c.Creator, stone, c.Original, c.Creator])!, "cannot put the soul back in that timed host");
                    Check.Same(c.Original, stone.ContainedSpirit, "failed entry leaves stone occupied");
                }
                finally { c.Creator.AllComps.Remove(temp); }
            }));
        yield return new("SpiritProjectedSummon.RealMineScribeBeforeAndAfterTrigger", r =>
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(FactionDefOf));
                var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.defName = "RegressionManaMine"; def.comps = []; def.category = ThingCategory.Building;
                def.building = new BuildingProperties(); def.thingClass = typeof(Building_ExplosiveProximityTrap);
                DefDatabase<ThingDef>.Add(def);
                var mine = new Building_ExplosiveProximityTrap { def = def, thingIDNumber = 19887 };
                var target = mine.GetType().GetField("trapPawn", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var armed = RoundTripSummon(r, "armed-mana-mine", mine);
                RuntimeLog.AssertClean();
                Check.Equal(true, armed.Armed && target.GetValue(armed) == null, "newly armed mine has no pawn reference");
                Set(mine, "trapPawn", c.Creator); Set(mine, "trapSprung", true); Set(mine, "trapSpringDelay", 11);
                var triggered = RoundTripSummon(r, "triggered-mana-mine", mine, c.Creator);
                Check.Same(c.Creator, target.GetValue(triggered), "real target persisted");
                Check.Equal(false, triggered.Armed, "pending wick survives load");
                Check.Equal(11, (int)mine.GetType().GetField("trapSpringDelay", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(triggered)!, "remaining delay survived");
                RuntimeLog.AssertClean();
            }));
        yield return new("SpiritProjectedCast.StormSkyfallerNativeImpactAfterLoad", r =>
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                var type = Production("SpiritStormSkyfaller");
                var xml = XDocument.Load(Path.Combine(r.Repository, "v1.7/Defs/AbilityDefs/Skyfallers.xml"));
                var spells = xml.Root!.Elements().Where(x => x.Element("defName")?.Value.StartsWith("TM_Firestorm_") == true ||
                    x.Element("defName")?.Value.StartsWith("TM_Blizzard_") == true).ToList();
                Check.Equal(6, spells.Count, "six native falling projectiles");
                foreach (var spell in spells) Check.Equal(type.FullName, spell.Element("thingClass")?.Value, "storm supports original caster");
                var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.defName = "RegressionStormSkyfaller"; def.thingClass = type; def.comps = [];
                def.category = ThingCategory.Ethereal; def.size = new IntVec2(1, 1); def.stackLimit = 1;
                def.drawerType = DrawerType.RealtimeOnly; def.altitudeLayer = AltitudeLayer.Skyfaller;
                def.skyfaller = new SkyfallerProperties { explosionRadius = 2, explosionDamage = new DamageDef { defaultDamage = 10 } };
                DefDatabase<ThingDef>.Add(def);
                var sky = (Skyfaller)Activator.CreateInstance(type)!;
                sky.def = def; sky.thingIDNumber = 19885;
                var impact = typeof(Skyfaller).GetMethod("Impact", BindingFlags.Instance | BindingFlags.NonPublic)!;
                using var production = new FleshStatTests.ProductionPatches();
                production.Add(Production("SpiritStormSkyfallerImpactPatch"));
                using var blasts = new Patch(typeof(GenExplosion).GetMethods().Single(m => m.Name == nameof(GenExplosion.DoExplosion)), nameof(StopAtSkyExplosion));
                void Verify(Skyfaller obj, Thing? expected)
                {
                    Set(obj, "positionInt", new IntVec3(15, 0, 15)); Set(obj, "mapIndexOrState", (sbyte)0);
                    try { impact.Invoke(obj, null); throw new InvalidOperationException("skyfaller skipped native explosion"); }
                    catch (TargetInvocationException e) when (e.InnerException is SkyExplosionObserved found)
                    { Check.Same(expected!, found.Owner!, "native GenExplosion received the right instigator"); }
                }
                Verify(sky, null);
                var field = new Projectile_Blizzard();
                var power = new MagicAbility(c.Original.GetCompAbilityUserMagic()) { Def = TorannMagicDefOf.TM_Blizzard };
                typeof(Projectile_SpiritBolt).GetMethod("PrepareProjection", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(field, [power]);
                var cell = new IntVec3(15, 0, 15);
                typeof(Projectile_SpiritStormField).GetMethod("SpawnStrike", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(field, [def, cell, c.Map]);
                sky = (Skyfaller)cell.GetThingList(c.Map).Single(item => item.def == def);
                sky = RoundTripSummon(r, "storm-falling-body", sky, c.Original);
                Verify(sky, c.Original);
            }));
        yield return new("SpiritProjectedCast.FirestormDelayedShrapnelCellsAndOwnerSurviveLoad", r =>
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                using var shadow = new Patch(typeof(MaterialPool).GetMethod("MatFrom", [typeof(string), typeof(UnityEngine.Shader)])!, nameof(CancelNative));
                var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.defName = "RegressionFirestorm"; def.comps = []; def.category = ThingCategory.Projectile;
                def.thingClass = typeof(Projectile_Firestorm);
                def.projectile = new ProjectileProperties { explosionRadius = 5 };
                Set(def.projectile, "damageAmountBase", 20);
                DefDatabase<ThingDef>.Add(def);
                var storm = new Projectile_Firestorm { def = def, thingIDNumber = 19882, intendedTarget = IntVec3.Zero, usedTarget = IntVec3.Zero };
                var owner = c.Original.GetCompAbilityUserMagic();
                owner.MagicData.MagicPowerSkill_Firestorm.Single(x => x.label == "TM_Firestorm_ver").level = 2;
                owner.arcaneDmg = 2f;
                var power = new MagicAbility(owner) { Def = TorannMagicDefOf.TM_Firestorm };
                var field = typeof(Projectile_SpiritStormField);
                typeof(Projectile_SpiritBolt).GetMethod("PrepareProjection", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(storm, [power]);
                Set(storm, "launcher", c.Creator);
                storm.Initialize(c.Map);
                var schedule = field.GetMethod("ScheduleCell", BindingFlags.NonPublic | BindingFlags.Instance)!;
                var due = field.GetMethod("ApplyDueEffects", BindingFlags.NonPublic | BindingFlags.Instance)!;
                var first = new IntVec3(13, 0, 12); var second = new IntVec3(15, 0, 14);
                Set(storm, "age", 103);
                schedule.Invoke(storm, [first, 2]); schedule.Invoke(storm, [IntVec3.Invalid, 2]); schedule.Invoke(storm, [second, 5]);
                storm = RoundTripSummon(r, "firestorm-before-shrapnel", storm, c.Creator, c.Original);
                storm.Initialize(c.Map);
                Check.Equal(1080, (int)field.GetField("duration", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(storm)!, "scaled duration remains fixed after reload");
                using var blasts = new Patch(typeof(GenExplosion).GetMethods().Single(m => m.Name == nameof(GenExplosion.DoExplosion)), nameof(ObserveStormExplosion));
                try
                {
                    foreach (int tick in new[] { 104, 105, 105, 107, 108, 109 })
                    {
                        Set(storm, "age", tick);
                        due.Invoke(storm, [c.Map]);
                        Check.Equal(tick < 105 ? 0 : tick < 108 ? 1 : 2, stormExplosions.Count, "each valid fragment fires once at its own deadline");
                    }
                    Check.Equal((first, (Thing)c.Original), stormExplosions[0], "first explosion");
                    Check.Equal((second, (Thing)c.Original), stormExplosions[1], "second explosion");
                }
                finally { stormExplosions.Clear(); }
            }));
        foreach (var (suffix, shots) in new[] { ("", 1), ("_I", 1), ("_II", 2), ("_III", 3) })
        yield return new("SpiritProjectedCast.FrostRayRankCadence" + suffix, r =>
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                var xml = XDocument.Load(Path.Combine(r.Repository, "v1.7/Defs/AbilityDefs/Abilities_TMagic_IceMage.xml")).Root!;
                var row = xml.Elements().Single(x => x.Element("defName")?.Value == "TM_FrostRay" + suffix).Element("MainVerb")!;
                var shotXml = xml.Elements().Single(x => x.Element("defName")?.Value == row.Element("defaultProjectile")!.Value);
                var def = (TMAbilityDef)typeof(TorannMagicDefOf).GetField("TM_FrostRay" + suffix)!.GetValue(null)!;
                var saved = def.MainVerb;
                try
                {
                    var assembly = typeof(AbilityUser.Verb_UseAbility).Assembly;
                    var bullet = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                    bullet.thingClass = typeof(TorannMagicDefOf).Assembly.GetType(shotXml.Element("thingClass")!.Value, true);
                    def.MainVerb = new AbilityUser.VerbProperties_Ability { verbClass = assembly.GetType(row.Element("verbClass")!.Value, true),
                        burstShotCount = (int)row.Element("burstShotCount")!, ticksBetweenBurstShots = (int)row.Element("ticksBetweenBurstShots")!, defaultProjectile = bullet };
                    var job = JobMaker.MakeJob(new JobDef { defName = "TM_SpiritFrostRay" + suffix }, c.Creator.Position, c.Original);
                    var driver = new JobDriver_SpiritBolt { pawn = c.Creator, job = job };
                    Set(c.Creator.jobs, "curJob", job); Set(c.Creator.jobs, "curDriver", driver);
                    using var launch = new Patch(Production("SpiritHostedBolt").GetMethod("Launch", BindingFlags.NonPublic | BindingFlags.Static)!, nameof(RecordRayLaunch));
                    var toils = ((IEnumerable<Verse.AI.Toil>)typeof(JobDriver_SpiritBolt).GetMethod("MakeNewToils", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(driver, null)!).ToArray();
                    Check.Equal(shots > 1 ? 3 : 2, toils.Length, "only multi-shot ranks schedule a continuation");
                    Set(Find.TickManager, "ticksGameInt", 100);
                    toils[1].initAction();
                    if (shots > 1)
                        for (int tick = 101; tick <= 100 + 5 * (shots - 1); tick++)
                        {
                            Set(Find.TickManager, "ticksGameInt", tick);
                            toils[2].tickAction();
                        }
                    Check.Equal(shots, rayLaunches.Count, "exact native shot count");
                    for (int i = 0; i < shots; i++)
                        Check.Equal((100 + 5 * i, i > 0), rayLaunches[i], "only first shot requests payment");
                }
                finally { def.MainVerb = saved; rayLaunches.Clear(); }
            }));
        yield return new("SpiritProjectedCast.DeathBoltNativeProjectileKeepsOwnerAcrossLoadAndExit", r =>
            SpiritRebirthRitualTransactionTests.Run(r,c =>
            {
                using var fixture = new Fixture(r,c);
                using var material = new Patch(typeof(MaterialPool).GetMethod("MatFrom", [typeof(string), typeof(UnityEngine.Shader)])!, nameof(CancelNative));
                var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.defName = "TestHostedDeathBolt"; def.thingClass = typeof(Projectile_HostedDeathBolt);
                def.comps = []; def.category = ThingCategory.Projectile; DefDatabase<ThingDef>.Add(def);
                var shot = new Projectile_HostedDeathBolt { def = def, thingIDNumber = 19852, intendedTarget = IntVec3.Zero, usedTarget = IntVec3.Zero };
                Set(shot,"launcher",c.Creator);
                var impact = typeof(Projectile_HostedDeathBolt).GetMethod("Impact", BindingFlags.Instance | BindingFlags.NonPublic)!;
                using var capture = new Patch(typeof(AbilityUser.Projectile_AbilityBase).GetMethod("ApplyImpact", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!,nameof(ObserveDeathBoltImpact));
                try
                {
                    impact.Invoke(shot,[null,true]);
                    Check.Same(c.Creator,observedRayOwner,"ordinary projectile keeps physical caster");
                    typeof(Projectile_HostedDeathBolt).GetMethod("SetSource",BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(shot,[c.Original]);
                    shot = RoundTripSummon(r,"deathbolt-primary-owner",shot,c.Creator,c.Original);
                    fixture.Possession.GetDirectlyHeldThings().Remove(c.Original);
                    impact.Invoke(shot,[null,true]);
                    Check.Same(c.Original,observedRayOwner,"loaded native projectile attributes impact to soul");
                    Check.Same(c.Creator,shot.Launcher,"physical launcher restored after impact");
                }
                finally { observedRayOwner = null; }
            }));
        yield return new("SpiritProjectedCast.FrostRayNativeImpactUsesSoulAndRestoresHost", r =>
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                using var shadow = new Patch(typeof(MaterialPool).GetMethod("MatFrom", [typeof(string), typeof(UnityEngine.Shader)])!, nameof(CancelNative));
                var beamDef = (AbilityUser.ProjectileDef_AbilityLaser)RuntimeHelpers.GetUninitializedObject(typeof(AbilityUser.ProjectileDef_AbilityLaser));
                beamDef.defName = "RegressionFrostRay"; beamDef.comps = []; beamDef.category = ThingCategory.Projectile;
                beamDef.thingClass = typeof(Projectile_SpiritRay); DefDatabase<ThingDef>.Add(beamDef);
                var ray = new Projectile_SpiritRay { def = beamDef, thingIDNumber = 19881, intendedTarget = IntVec3.Zero, usedTarget = IntVec3.Zero };
                Set(ray, "launcher", c.Creator);
                var type = typeof(Projectile_SpiritRay);
                var impact = type.GetMethod("Impact", BindingFlags.Instance | BindingFlags.NonPublic)!;
                using var capture = new Patch(typeof(AbilityUser.Projectile_AbilityLaser).GetMethod("FinishImpact", BindingFlags.Instance | BindingFlags.NonPublic)!, nameof(ObserveFrostRayOwner));
                try
                {
                    impact.Invoke(ray, [c.Creator, true]);
                    Check.Same(c.Creator, observedRayOwner, "normal laser passes physical launcher to native shield impact");
                    var power = new MagicAbility(c.Original.GetCompAbilityUserMagic()) { Def = TorannMagicDefOf.TM_FrostRay_III };
                    type.GetMethod("PrepareProjection", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(ray, [power]);
                    ray = RoundTripSummon(r, "frost-ray-before-impact", ray, c.Creator, c.Original);
                    var state = ClassIdentity.Component(c.Original).SpiritRebirth;
                    Set(state, "recoveryTicks", SpiritRebirthState.RecoveryDurationTicks);
                    fixture.Possession.GetDirectlyHeldThings().Remove(c.Original);
                    GenSpawn.Spawn(c.Original, new IntVec3(15, 0, 15), c.Map);
                    Check.Equal(true, SpiritRebirthStateTests.Invoke(state, "TryLeaveHost", c.Original, c.Creator), "soul leaves the living host before impact");
                    impact.Invoke(ray, [c.Creator, true]);
                    Check.Same(c.Original, observedRayOwner, "projected native impact uses original soul");
                    Check.Same(c.Creator, ray.Launcher, "native beam still originates at host after impact");
                }
                finally { observedRayOwner = null; }
            }));
        yield return new("SpiritProjectedCast.ObstructedProjectileUsesNativeLaunchRules", r =>
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                var def = TorannMagicDefOf.TM_AMP;
                var previous = def.MainVerb;
                try
                {
                    var props = def.MainVerb = new AbilityUser.VerbProperties_Ability();
                    var power = new MagicAbility(c.Original.GetCompAbilityUserMagic()) { Def = def };
                    var verb = new Verb_BLOS { caster = c.Creator, Ability = power, verbProps = props };
                    var shoot = Production("SpiritHostedBolt").GetMethod("Shoot", BindingFlags.Static | BindingFlags.NonPublic)!;
                    using var blocked = new Patch(typeof(Verb).GetMethods().Single(m => m.Name == "TryFindShootLineFromTo" && m.GetParameters()[0].ParameterType == typeof(IntVec3)), nameof(CancelNative));
                    using var observe = new Patch(typeof(ThingMaker).GetMethod(nameof(ThingMaker.MakeThing))!, nameof(ObserveProjectileCreation));
                    foreach (bool needsSight in new[] { true, false }) foreach (bool stopBurst in new[] { true, false })
                    {
                        props.requireLineOfSight = needsSight;
                        props.stopBurstWithoutLos = stopBurst;
                        bool created = false;
                        try { shoot.Invoke(null, [c.Creator, power, verb, new LocalTargetInfo(c.Creator.Position)]); }
                        catch (TargetInvocationException error) when (error.InnerException is ProjectileCreationObserved) { created = true; }
                        Check.Equal(!(needsSight && stopBurst), created, $"obstructed shot requireLineOfSight={needsSight} stopBurstWithoutLos={stopBurst}");
                    }
                }
                finally { def.MainVerb = previous; }
            }));
        foreach(var test in HostedWardCases()) yield return test;
        foreach(var test in HostedRaiseCases()) yield return test;
        foreach(var test in HostedSummonCases()) yield return test;
        foreach (bool ordered in new[] { false, true })
        yield return new("SpiritProjectedCast.BloodGiftNativeFilthAndHostPool." + (ordered ? "Job" : "Effect"), r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var fixture = new Fixture(r, c);
            var oldBlood = ThingDefOf.Filth_Blood;
            var oldSettings = TorannMagic.ModOptions.Settings.Instance;
            var defs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
            try
            {
                TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings();
                var body = c.Creator; var soul = c.Original;
                var caster = soul.GetCompAbilityUserMagic();
                var ability = new MagicAbility(caster) { Def = TorannMagicDefOf.TM_BloodGift };
                var skill = caster.MagicData.MagicPowerSkill_BloodGift.Single(x => x.label == "TM_BloodGift_ver");
                skill.level = 1; caster.arcaneDmg = 1f;
                body.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "SpiritHostCore", bleedRate = 0 } } };
                var blood = DefDatabase<HediffDef>.GetNamedSilentFail("TM_BloodHD");
                if (blood == null) { blood = new HediffDef { defName = "TM_BloodHD", maxSeverity = 1000f }; DefDatabase<HediffDef>.Add(blood); }
                var pool = new Hediff { pawn = body, def = blood }; body.health.hediffSet.hediffs.Add(pool);
                var soulPool = new Hediff { pawn = soul, def = blood }; soul.health.hediffSet.hediffs.Add(soulPool);
                float before = pool.Severity, soulBefore = soulPool.Severity;
                ThingDefOf.Filth_Blood = c.Magicyte.def;
                var filth = ThingMaker.MakeThing(c.Magicyte.def); filth.stackCount = 3;
                GenSpawn.Spawn(filth, body.Position, c.Map);
                int charges = 0;
                var apply = typeof(Verb_BloodGift).GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic)!;
                if (ordered)
                {
                    var gift = ability.magicDef;
                    var oldCost = gift.manaCost;
                    float oldRecharge = gift.MainVerb.SecondsToRecharge;
                    var events = DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();
                    var oldEvent = TorannMagicDefOf.TM_UsedMagic;
                    try
                    {
                        TorannMagicDefOf.TM_UsedMagic = new HistoryEventDef { defName = "SpiritJobHistoryEvent" };
                        DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedMagic);
                        var history = (History)RuntimeHelpers.GetUninitializedObject(typeof(History));
                        history.historyEventsManager = new HistoryEventsManager();
                        Set(Current.Game, "history", history);
                        using var noIdeoNotifications = new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!, nameof(CancelNative));
                        if (DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null)
                            DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_SyrriumSenseHD" });
                        gift.manaCost = .20f; gift.MainVerb.SecondsToRecharge = 3;
                        caster.AbilityData.Powers.Add(ability); caster.AbilityData.AllPowers = null;
                        if (caster.Mana == null) soul.needs.AllNeeds.Add(new Need_Mana(soul) { def = TorannMagicDefOf.TM_Mana });
                        caster.Mana.CurLevel = .70f;
                        float beforeMana = caster.Mana.CurLevel;
                        float cost = caster.ActualManaCost(gift);
                        var nativeHostMana = body.GetCompAbilityUserMagic()?.Mana;
                        float hostMana = nativeHostMana?.CurLevel ?? 0;
                        var job = new Verse.AI.Job(new JobDef { defName = "TM_SpiritBloodSacrifice" }, soul);
                        var driver = new JobDriver_SpiritBloodSacrifice { pawn = body, job = job };
                        Set(body.jobs, "curJob", job); Set(body.jobs, "curDriver", driver);
                        var toils = (IEnumerable<Verse.AI.Toil>)driver.GetType().GetMethod("MakeNewToils", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(driver, null)!;
                        toils.Single().initAction();
                        Check.Equal(true, Math.Abs(caster.Mana.CurLevel - (beforeMana - cost)) < .0001f && cost > 0, "actual mana paid once by original");
                        Check.Equal(hostMana, nativeHostMana?.CurLevel ?? 0, "host mana untouched");
                        Check.Equal(true, ability.CooldownTicksLeft > 0, "original ability recharging");
                    }
                    finally { gift.manaCost = oldCost; gift.MainVerb.SecondsToRecharge = oldRecharge;
                        TorannMagicDefOf.TM_UsedMagic = oldEvent; DefDatabase<HistoryEventDef>.Clear(); foreach (var e in events) DefDatabase<HistoryEventDef>.Add(e); }
                }
                else Check.Equal(true, (bool)apply.Invoke(null, [body, ability, (Action)(() => charges++)])!, "rank one on soul absorbs filth for host");
                Check.Equal(true, filth.Destroyed, "native physical resource consumed");
                if (!ordered) Check.Equal(1, charges, "one commitment before effects");
                Check.Equal(true, Math.Abs(pool.Severity - before - 3.3f) < .001f, "blood pool credited on host with soul rank");
                Check.Equal(soulBefore, soulPool.Severity, "soul blood unaffected");
                RuntimeLog.AssertClean();
            }
            finally { ThingDefOf.Filth_Blood = oldBlood; TorannMagic.ModOptions.Settings.Instance = oldSettings;
                DefDatabase<HediffDef>.Clear(); foreach (var def in defs) DefDatabase<HediffDef>.Add(def); }
        }));
        yield return new("SpiritMapLoss.NativeWarningHooksAndManifest", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var f = new Fixture(r, c);
            using var patches = new FleshStatTests.ProductionPatches();
            foreach (var type in new[] { "SpiritReformWarningPatch", "SpiritMapAbandonWarningPatch", "SpiritMapDeinitPatch" })
                patches.Add(Production(type));
            var native = typeof(RimWorld.Planet.SettlementAbandonUtility).GetMethod("TryAbandonWithColonyCheck", BindingFlags.Static | BindingFlags.NonPublic)!;
            var processor = Assembly.Load("0Harmony").GetType("HarmonyLib.PatchProcessor")!;
            var read = processor.GetMethods().First(m => m.Name == "GetOriginalInstructions" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.Name == "ILGenerator");
            var args = read.GetParameters().Select((p,i) => i == 0 ? (object)native : p.HasDefaultValue ? p.DefaultValue : null).ToArray();
            var transpiler = Production("SpiritMapAbandonWarningPatch").GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static)!;
            var il = ((System.Collections.IEnumerable)transpiler.Invoke(null, [read.Invoke(null,args)])!).Cast<object>().ToList();
            Check.Equal(1, il.Count(op => (op.GetType().GetField("operand")?.GetValue(op) as MethodInfo)?.Name == "AppendWarning"), "one warning in the final native confirmation");
            c.Creator.health.SetDead();
            Check.Equal(true, Begin(f.Keeper, c.Original, c.Creator), "real occupied shell");
            var stone = ClassIdentity.Component(c.Original).SpiritRebirth.Stone;
            var cargo = new List<TransferableOneWay>();
            var leaves = Production("SpiritReformWarningPatch").GetMethod("LeavesWithParty", BindingFlags.NonPublic | BindingFlags.Static)!;
            bool Selected() => (bool)leaves.Invoke(null, [c.Original, cargo])!;
            Check.Equal(false, Selected(), "omitted stone warned");
            var row = new TransferableOneWay();
            row.things.Add(stone); cargo.Add(row);
            row.AdjustTo(1);
            Check.Equal(true, Selected(), "selected specific stone excluded from warning");
            row.AdjustTo(0);
            Check.Equal(false, Selected(), "deselected stone warned again");
            Check.Same(c.Original, stone.ContainedSpirit, "preview does not alter ownership");
        }));
        foreach (string route in new[] { "Ground", "Evacuated", "Pending" })
            yield return new("SpiritMapLoss." + route, r => SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var f = new Fixture(r, c);
                c.Creator.health.SetDead();
                using (var refuse = route == "Pending" ? new Patch(PlaceMethod(), nameof(RefusePlacement)) : null)
                    Check.Equal(true, Begin(f.Keeper, c.Original, c.Creator), "committed loss of original host");
                var loss = Production("SpiritMapLoss");
                var remaining = loss.GetMethod("Remaining", BindingFlags.NonPublic | BindingFlags.Static)!;
                var discard = loss.GetMethod("DiscardRemaining", BindingFlags.NonPublic | BindingFlags.Static)!;
                List<Pawn> Souls() => (List<Pawn>)remaining.Invoke(null, [c.Map])!;
                Check.Same(c.Original, Souls().Single(), "warning selects exact soul once");
                if (route == "Evacuated")
                {
                    var stone = ClassIdentity.Component(c.Original).SpiritRebirth.Stone;
                    stone.DeSpawn();
                    var owner = new Pawn_InventoryTracker(new Pawn()).innerContainer;
                    Check.Equal(true, owner.TryAdd(stone, false), "evacuated to off-map inventory");
                    discard.Invoke(null, [c.Map]);
                    Check.Same(c.Original, stone.ContainedSpirit, "evacuated soul survives map loss");
                }
                else
                {
                    var worldObjects = Find.World.worldObjects;
                    try
                    {
                        Find.World.worldObjects = new RimWorld.Planet.WorldObjectsHolder();
                        c.Map.attackTargetReservationManager = new Verse.AI.AttackTargetReservationManager(c.Map);
                        c.Map.pawnDestinationReservationManager = new PawnDestinationReservationManager();
                        c.Original.def.destroyable = true;
                        typeof(SpellItemEffectTests).GetMethod("HeadlessDefOf", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [typeof(PawnRelationDefOf)]);
                        discard.Invoke(null, [c.Map]);
                        Check.Equal(true, c.Original.Discarded && c.Original.Destroyed, "soul is permanently retired");
                        Check.Equal(false, Find.WorldPawns.Contains(c.Original), "no independent world pawn");
                        discard.Invoke(null, [c.Map]);
                    }
                    finally { Find.World.worldObjects = worldObjects; }
                }
                Check.Equal(0, Souls().Count, "no stale warning or second disposal");
                RuntimeLog.AssertClean();
            }));
        foreach (string route in new[] { "Survivor", "LastOwner", "Veto", "OccupiedCargo" })
            yield return new("SpiritCaravanLoss.Callback." + route, r => SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var f = new Fixture(r, c);
                bool survives = route == "Survivor";
                var previous = Find.World.worldObjects;
                try
                {
                    Find.World.worldObjects = new RimWorld.Planet.WorldObjectsHolder();
                    var caravan = new RimWorld.Planet.Caravan { ID = 69992, def = new RimWorld.WorldObjectDef { defName = "SpiritTestCaravan" } };
                    Set(caravan, "factionInt", survives ? c.Creator.Faction : new Faction { def = new FactionDef() });
                    if (!survives) Set(c.Creator, "factionInt", caravan.Faction);
                    Find.World.worldObjects.AllWorldObjects.Add(caravan);
                    typeof(RimWorld.Planet.WorldObjectsHolder).GetMethod("AddToCache", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Find.World.worldObjects, [caravan]);
                    Set(((UIRoot_Play)Current.Root.uiRoot).mapUI, "colonistBar", RuntimeHelpers.GetUninitializedObject(typeof(ColonistBar)));
                    Set(caravan, "babies", null);
                    c.Map.spawnedThings.Remove(c.Creator);
                    Set(c.Creator, "mapIndexOrState", (sbyte)-1);
                    Check.Equal(true, caravan.pawns.TryAdd(c.Creator, false), "caravan holds host");
                    var carrier = new Pawn { def = c.Creator.def, kindDef = c.Creator.kindDef, thingIDNumber = 69993 };
                    Set(carrier, "factionInt", c.Creator.Faction);
                    carrier.inventory = new Pawn_InventoryTracker(carrier);
                    if (survives) Check.Equal(true, caravan.pawns.TryAdd(carrier, false), "caravan holds survivor");
                    c.Creator.health.SetDead();
                    c.Original.def.destroyable = true;
                    c.Map.attackTargetReservationManager = new Verse.AI.AttackTargetReservationManager(c.Map);
                    c.Map.pawnDestinationReservationManager = new PawnDestinationReservationManager();
                    typeof(SpellItemEffectTests).GetMethod("HeadlessDefOf", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [typeof(PawnRelationDefOf)]);
                    var handler = Production("SpiritCaravanHostLoss").GetMethod("BeforeRemoval", BindingFlags.NonPublic | BindingFlags.Static)!;
                    using var hooks = new FleshStatTests.ProductionPatches();
                    hooks.Add(Production("SpiritCaravanHostLoss"));
                    using var noWorldGraphics = new Patch(typeof(RimWorld.Planet.WorldObject).GetMethod(nameof(RimWorld.Planet.WorldObject.PostRemove))!, nameof(CancelNative));
                    if (route == "Veto")
                    {
                        using var veto = new Patch(typeof(RimWorld.Planet.Caravan).GetMethod(nameof(RimWorld.Planet.Caravan.Notify_MemberDied))!, nameof(CancelNative));
                        caravan.Notify_MemberDied(c.Creator);
                        Check.Same(c.Original, f.Possession.SpiritPawn, "cancelled native removal retains original");
                        Check.Equal(true, caravan.Spawned && caravan.pawns.Contains(c.Creator), "no terminal loss on a veto");
                        return;
                    }
                    if (route == "OccupiedCargo")
                    {
                        var secure = Production("SpiritCaravanHostLoss").GetMethod("TrySecure", BindingFlags.Static | BindingFlags.NonPublic)!;
                        Check.Equal(true,(bool)secure.Invoke(null,[c.Original,c.Creator,c.Creator.inventory.innerContainer])!,"actual occupied cargo aboard");
                        Check.Equal(true,f.Possession.SpiritPawn == null,"dead member no longer contains a Spirit");
                    }
                    caravan.Notify_MemberDied(c.Creator);
                    Check.Equal(survives, caravan.Spawned, "native callback keeps only a surviving caravan");
                    handler.Invoke(null, [caravan, c.Creator]);
                    if (survives)
                    {
                        var stone = (Thing_SoulStone)carrier.inventory.innerContainer.Single();
                        Check.Same(c.Original, stone.ContainedSpirit, "surviving owner holds original");
                    }
                    else Check.Equal(true, c.Original.Destroyed && c.Original.Discarded, "last native owner lost");
                    Check.Equal(false, Find.WorldPawns.Contains(c.Original), "no independent world pawn");
                    RuntimeLog.AssertClean();
                }
                finally { Find.World.worldObjects = previous; }
            }));
        yield return new("SpiritCaravanLoss.SurvivorInventory", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var fixture = new Fixture(r, c);
            var carrier = new Pawn();
            var inventory = new Pawn_InventoryTracker(carrier).innerContainer;
            var secure = Production("SpiritCaravanHostLoss").GetMethod("TrySecure", BindingFlags.Static | BindingFlags.NonPublic)!;
            bool Transfer() => (bool)secure.Invoke(null, [c.Original, c.Creator, inventory])!;
            Check.Equal(false, Transfer(), "living host cannot be collapsed");
            c.Creator.health.SetDead();
            Check.Equal(true, Transfer(), "survivor inventory secures original");
            Check.Equal(false, Transfer(), "duplicate death does not mint a stone");
            var state = ClassIdentity.Component(c.Original).SpiritRebirth;
            Check.Equal(true, state.HasValidStone(c.Original), "reciprocal stone custody");
            Check.Same(state.Stone, inventory.Single(), "only stone belongs to survivor");
            Check.Equal(0, state.RecoveryTicks, "lost host resets recovery");
            RuntimeLog.AssertClean();
        }));
        foreach (var route in new[] { "SetDead", "CancelKill", "RemoveHediff", "RemovePossession", "Destroy", "Kill", "RefusedRemoval" })
            yield return new("SpiritHostLoss.Native." + route, r => SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                using var hooks = new FleshStatTests.ProductionPatches();
                foreach (var patch in new[] { Production("SpiritHostSetDeadPatch"), Production("SpiritHostRemoveHediffPatch"),
                    Production("SpiritHostDestroyPatch"), typeof(TorannMagicMod.KillPossessed_Patch) })
                    hooks.Add(patch);
                var host = c.Creator;
                var state = ClassIdentity.Component(c.Original).SpiritRebirth;
                if (route == "CancelKill")
                {
                    using var veto = new Patch(typeof(Pawn).GetMethod(nameof(Pawn.Kill))!, nameof(CancelNative));
                    TM_Action.RemovePossession(host, host.Position, false, true);
                    host.Kill(null);
                    Check.Equal(false, host.Dead, "cancelled native kill");
                    Check.Equal(true, state.HasValidHost(c.Original), "cancelled kill keeps exact original in host");
                    Check.Equal(87, state.RecoveryTicks, "cancelled kill keeps recovery");
                    return;
                }
                host.def.destroyable = true;
                if (route == "RefusedRemoval")
                {
                    c.Map.components.Remove(fixture.Keeper);
                    host.health.RemoveHediff(fixture.Possession.parent);
                    host.Destroy();
                    Check.Equal(true, host.Spawned && state.HasValidHost(c.Original), "source survives both refused destructive operations");
                    return;
                }
                host.def.comps ??= [];
                host.def.category = ThingCategory.Pawn;
                host.def.size = new IntVec2(1,1);
                host.def.thingClass = typeof(Pawn); host.def.drawerType = DrawerType.RealtimeOnly;
                c.Map.events = new MapEvents(c.Map);
                c.Map.thingGrid.Register(host); c.Map.listerThings.Add(host);
                c.Map.attackTargetsCache.Notify_ThingSpawned(host);
                Find.World.ideoManager = new IdeoManager();
                Current.Game.components ??= [];
                Current.Game.components.Add(new GameComponent_Bossgroup(Current.Game));
                Set(Find.TickManager, "gameStartAbsTick", 1);
                host.meleeVerbs = new Pawn_MeleeVerbs(host);
                // Tales, battle-log rendering and colony thoughts need a running game.
                using var stories = new Patch(typeof(Pawn).GetMethod("DoKillSideEffects", BindingFlags.Instance | BindingFlags.NonPublic)!, nameof(CancelNative));
                using var thoughts = new Patch(typeof(Pawn).GetMethod("DropBeforeDying", BindingFlags.Instance | BindingFlags.NonPublic)!, nameof(CancelNative));
                using var unrelatedHealth = new Patch(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!, nameof(CancelNative));
                if (route == "SetDead") host.health.SetDead();
                if (route == "Kill") { host.Kill(null); RuntimeLog.AssertClean(); }
                if (route == "RemoveHediff") host.health.RemoveHediff(fixture.Possession.parent);
                if (route == "RemovePossession") TM_Action.RemovePossession(host, host.Position, true);
                if (route == "Destroy") host.Destroy();
                Check.Equal(true, state.HasValidStone(c.Original), "native " + route + " secures exact original");
                var stone = state.Stone;
                if (route == "Kill" || route == "SetDead") Check.Equal(true, host.Dead, "native death committed");
                if (route == "Destroy") Check.Equal(true, host.Destroyed, "native destruction completed");
                if (route == "RemovePossession") TM_Action.RemovePossession(host, host.Position, true);
                Check.Equal(0, state.RecoveryTicks, "full week resets on commit");
                Check.Equal(true, fixture.Possession.SpiritPawn == null, "former holder released original");
                MapComponentUtility.MapComponentTick(c.Map);
                Check.Same(stone, state.Stone, "no second stone on retry");
            }));
        yield return new("SpiritMapCollapse.NativePlacementFalseAfterSuccess", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var f = new Fixture(r, c); c.Creator.health.SetDead();
            using (new Patch(PlaceMethod(), nameof(ReturnFalse), postfix: true))
                Check.Equal(true, Begin(f.Keeper, c.Original, c.Creator), "native successful placement remains committed despite callback false");
            var state = ClassIdentity.Component(c.Original).SpiritRebirth;
            var stone = Check.NotNull(state.Stone, "actual candidate committed");
            Check.Same(c.Map.spawnedThings, stone.holdingOwner, "observed native map owner wins over false callback result");
            var registered = c.Map.components;
            try
            {
                c.Map.components = null!;
                Check.Equal(false, (bool)Production("MapComponent_SpiritCollapse").GetMethod("IsPendingAccountingOwner", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [c.Original])!, "committed stone without pending token never needs keeper lookup");
            }
            finally { c.Map.components = registered; }
            Check.Same(c.Original, stone.ContainedSpirit, "same original retained after native placement callback");
            Check.Equal(0, ((IThingHolder)f.Keeper).GetDirectlyHeldThings().Count, "completed graph leaves no duplicate keeper ownership");
            Set(Find.TickManager, "ticksGameInt", 500); MapComponentUtility.MapComponentTick(c.Map);
            Check.Same(stone, state.Stone, "completed placement retry does not recreate or reset");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritMapCollapse.RejectedAuthorizationRemovesOnlyEmptyCandidate", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var f = new Fixture(r, c); c.Creator.health.SetDead();
            using (new Patch(typeof(SpiritRebirthState).GetMethod("BeginPendingMapDeath", BindingFlags.Instance | BindingFlags.NonPublic)!, nameof(RefuseOperation)))
                Check.Equal(false, Begin(f.Keeper, c.Original, c.Creator), "refused state authorization aborts before source mutation");
            Check.Same(f.Possession.GetDirectlyHeldThings(), c.Original.holdingOwner, "exact original remains native source-owned");
            Check.Equal(0, ((IThingHolder)f.Keeper).GetDirectlyHeldThings().Count, "new empty rejected shell not leaked in keeper");
            Check.Equal(SpiritRebirthForm.Possessed, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "source state unchanged by rejected begin");
            RuntimeLog.AssertClean();
        }));
        foreach (bool occupied in new[] { false, true })
        yield return new("SpiritMapCollapse.NativePendingSave." + (occupied ? "OccupiedCandidate" : "EmergencyOriginalAndMovedEmpty"), r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            Production("MapComponent_SpiritCollapse");
            using var f = new Fixture(r, c);
            c.Creator.health.SetDead();
            using var placement = new Patch(PlaceMethod(), nameof(RefusePlacement));
            using (new Patch(typeof(SpiritRebirthState).GetMethod(occupied ? "TryCompletePendingMapDeath" : "SecurePendingMapDeath", BindingFlags.Instance | BindingFlags.NonPublic)!, occupied ? nameof(RefuseOperation) : nameof(ReturnFalse), postfix: !occupied))
            {
                bool began = Begin(f.Keeper, c.Original, c.Creator);
                RuntimeLog.AssertClean();
                Check.Equal(true, began, "pending failure still secures exact original in keeper graph");
            }
            var state = ClassIdentity.Component(c.Original).SpiritRebirth;
            Check.Equal(SpiritRebirthForm.Dormant, state.Form, "only secured original becomes inactive pending");
            Check.Equal(null, state.Host, "pending retry no longer depends on source host reference");
            Check.Equal(87, state.RecoveryTicks, "uncommitted capture does not prematurely reset recovery");
            c.Creator.health.RemoveHediff(f.Possession.parent);
            Check.Equal(false, c.Creator.health.hediffSet.hediffs.Contains(f.Possession.parent), "native source possession removed before independent retry");
            c.Original.needs.TryGetNeed<Need_Spirit>().CurLevel = 0f;
            c.Original.needs.TryGetNeed<Need_Mana>().CurLevel = .27f;
            ObservePendingAccounting(c);
            string identity = c.Original.GetUniqueLoadID();
            var keeper = SaveComponents(r, c, f.Keeper, occupied);
            var owner = ((IThingHolder)keeper).GetDirectlyHeldThings();
            var oldStone = owner.OfType<Thing_SoulStone>().Single();
            var original = occupied ? oldStone.ContainedSpirit! : owner.OfType<Pawn>().Single();
            Check.Equal(identity, original.GetUniqueLoadID(), "native deep graph preserves original saved identity");
            Check.Equal(false, ReferenceEquals(original, c.Original), "actual original deep-loaded, no reference-only surrogate");
            Check.Equal(true, (bool)Production("MapComponent_SpiritCollapse").GetMethod("IsPendingAccountingOwner", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [original])!, "loaded native custody and saved authorization agree");
            Check.NotNull(original.needs.TryGetNeed<Need_Spirit>(), "loaded Spirit exists before explicit refresh");
            Check.NotNull(original.needs.TryGetNeed<Need_Mana>(), "loaded Mana exists before explicit refresh");
            original.needs.AddOrRemoveNeedsAsAppropriate();
            Check.Equal(0f, original.needs.TryGetNeed<Need_Spirit>().CurLevel, "existing zero Spirit survives pending save and native need refresh");
            Check.Equal(.27f, original.needs.TryGetNeed<Need_Mana>().CurLevel, "existing pending mana survives native save");
            if (!occupied)
            {
                owner.Remove(oldStone); Check.Equal(true, c.Creator.inventory.innerContainer.TryAdd(oldStone, false), "empty candidate moved to real foreign owner");
            }
            Set(Find.TickManager, "ticksGameInt", 500); MapComponentUtility.MapComponentTick(c.Map);
            var restored = ClassIdentity.Component(original).SpiritRebirth;
            Check.Equal(true, restored.HasValidStone(original), "independent native map tick completes after source removal and load");
            Check.Equal(0, restored.RecoveryTicks, "committed host death resets full week exactly at commit");
            if (occupied) Check.Same(oldStone, restored.Stone, "occupied pending candidate never recreated");
            else
            {
                Check.Equal(false, ReferenceEquals(oldStone, restored.Stone), "only stale empty candidate replaced");
                Check.Same(c.Creator.inventory.innerContainer, oldStone.holdingOwner, "foreign empty shell neither stolen nor destroyed");
            }
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritMapCollapse.NativeKeeperAndExactDeathCapture", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            Production("MapComponent_SpiritCollapse");
            using var f = new Fixture(r, c);
            Check.Equal(false, Begin(f.Keeper, c.Original, c.Creator), "live host cannot start death collapse");
            c.Creator.health.SetDead();
            Thing_SoulStone stone;
            using (new Patch(PlaceMethod(), nameof(RefusePlacement)))
            {
                Check.Equal(true, Begin(f.Keeper, c.Original, c.Creator), "actual SetDead host evacuates exact original");
                var state = ClassIdentity.Component(c.Original).SpiritRebirth;
                stone = Check.NotNull(state.Stone, "same-original stone commit completed");
                Check.Equal(true, state.HasValidStone(c.Original), "native stone custody reciprocal");
                Check.Equal(true, state.RecoveryRequired, "death requires full recovery debt");
                Check.Equal(0, state.RecoveryTicks, "death resets prior progress");
                Check.Equal(0, f.Possession.GetDirectlyHeldThings().Count, "source native holder empty");
                Check.Same(f.Keeper, stone.holdingOwner.Owner, "refused placement retains actual map component owner");
                var holders = new List<IThingHolder>(); c.Map.GetChildHolders(holders);
                Check.Equal(1, holders.Count(h => ReferenceEquals(h, f.Keeper)), "native Map.GetChildHolders discovers keeper once");
                var all = new List<Thing>(); ThingOwnerUtility.GetAllThingsRecursively(c.Map, all, false);
                Check.Equal(1, all.Count(x => ReferenceEquals(x, stone)), "native recursive walk finds actual stone once");
                Check.Equal(1, all.Count(x => ReferenceEquals(x, c.Original)), "native recursive walk finds exact original once");
                Check.Same(c.Map, ThingOwnerUtility.GetRootMap((IThingHolder)stone), "native root map follows keeper parent");
                Check.Equal(true, Begin(f.Keeper, c.Original, c.Creator), "repeated death entry reuses pending actual candidate");
                Check.Same(stone, state.Stone, "repeat cannot manufacture candidate");
                Set(Find.TickManager, "ticksGameInt", 249); MapComponentUtility.MapComponentTick(c.Map);
                Check.Same(f.Keeper, stone.holdingOwner.Owner, "placement backoff independent of dead host");
            }
            Set(Find.TickManager, "ticksGameInt", 250); MapComponentUtility.MapComponentTick(c.Map);
            RuntimeLog.AssertClean();
            Check.Same(c.Map.spawnedThings, stone.holdingOwner, "native local retry places existing stone");
            Check.Equal(true, stone.Spawned && c.Map.thingGrid.ThingsListAt(stone.Position).Contains(stone), "native map registration agrees with holder");
            Check.Same(c.Original, stone.ContainedSpirit, "placement retains exact original");
            RuntimeLog.AssertClean();
        }));
    }
    static void ObservePendingAccounting(SpiritRebirthRitualTransactionTests.Context c)
    {
        var pawn = c.Original; var magic = pawn.GetComp<CompAbilityUserMagic>();
        var mana = pawn.needs.TryGetNeed<Need_Mana>(); var spirit = pawn.needs.TryGetNeed<Need_Spirit>();
        var oldSettings = TorannMagic.ModOptions.Settings.Instance;
        var defs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        try
        {
            TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings();
            foreach (string name in new[] { "TM_SyrriumSenseHD", "TM_PomanaSenseHD" })
                if (DefDatabase<HediffDef>.GetNamedSilentFail(name) == null) DefDatabase<HediffDef>.Add(new HediffDef { defName = name });
            c.Map.gameConditionManager = new GameConditionManager(c.Map);
            Set(mana, "lastParacyteCheck", int.MaxValue);
            magic.summonedMinions = Enumerable.Range(0, 8).Select(_ => (Thing)PawnWithComps(false, false).pawn).ToList();
            magic.mpRegenRate = 1f; magic.maxMP = 1f;
            int tick = Enumerable.Range(1, 150).First(t => { Set(Find.TickManager, "ticksGameInt", t); return pawn.IsHashIntervalTick(150); });
            MapComponentUtility.MapComponentTick(c.Map);
            Check.Equal(true, mana.CurLevel < .27f, "pending native keeper clock pays existing negative minion upkeep");
            float once = mana.CurLevel; MapComponentUtility.MapComponentTick(c.Map);
            Check.Equal(once, mana.CurLevel, "duplicate observed keeper tick cannot charge twice");
            Check.Same(mana, pawn.needs.TryGetNeed<Need_Mana>(), "pending clock preserves actual Mana owner");
            Check.Equal(0f, spirit.CurLevel, "pending clock does not drain or refill Spirit");
            Check.Equal(87, ClassIdentity.Component(pawn).SpiritRebirth.RecoveryTicks, "pending accounting never advances host recovery");
            mana.CurLevel = .27f;
            RuntimeLog.AssertClean();
        }
        finally { TorannMagic.ModOptions.Settings.Instance = oldSettings; DefDatabase<HediffDef>.Clear(); foreach (var def in defs) DefDatabase<HediffDef>.Add(def); }
    }
    static MapComponent SaveComponents(RuntimeFixture r, SpiritRebirthRitualTransactionTests.Context c, MapComponent keeper, bool occupied)
    {
        var pawn = c.Original; var comp = ClassIdentity.Component(pawn);
        // Native ownership/state/needs graph. Unrelated optional Pawn trackers
        // retain their separate established identity/save coverage.
        foreach (var field in typeof(Pawn).GetFields(BindingFlags.Instance | BindingFlags.Public))
            if (!field.IsInitOnly && field.FieldType.Name.StartsWith("Pawn_") && field.Name != "inventory" && field.Name != "health" && field.Name != "needs" && field.Name != "ageTracker") field.SetValue(pawn, null);
        pawn.ageTracker = new Pawn_AgeTracker(pawn);
        Set(pawn.ageTracker, "ageBiologicalTicksInt", 25L * 3600000L);
        pawn.def.race.lifeStageAges = [new LifeStageAge { minAge = 13, def = new LifeStageDef { developmentalStage = DevelopmentalStage.Adult } }];
        Set(pawn.ageTracker, "cachedLifeStageIndex", 0);
        pawn.Name = null; pawn.kindDef = null; Set(pawn, "factionInt", null); pawn.health.hediffSet.hediffs.Clear();
        pawn.def.comps = [new CompProperties { compClass = typeof(CompClassProgression) }]; Set(pawn, "comps", new List<ThingComp> { comp });
        foreach (var def in new[] { pawn.def, c.Magicyte.def, c.Silver.def })
            if (DefDatabase<ThingDef>.GetNamedSilentFail(def.defName) == null) DefDatabase<ThingDef>.Add(def);
        c.Map.uniqueID = 654;
        var path = Path.Combine(r.WorkingDirectory, "native-map-components-" + occupied + ".xml");
        var components = c.Map.components;
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Collections.Look(ref components, "components", LookMode.Deep, c.Map); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            var xml = XDocument.Load(path);
            Check.Equal(1, xml.Descendants("def").Count(x => x.Value == pawn.def.defName), "one deep native original node");
            Check.Equal(1, xml.Descendants("def").Count(x => x.Value == "TM_SoulStone"), "one deep actual candidate node");
            components = null!;
            try
            {
                Scribe.loader.InitLoading(path); Scribe_Collections.Look(ref components, "components", LookMode.Deep, c.Map); c.Map.components = components;
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                foreach (var anchor in new ILoadReferenceable[] { c.Map, c.Creator, c.Circle }) directory.RegisterLoaded(anchor);
                Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            return components.Single();
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = culture; }
    }
    static bool RefuseOperation(ref bool __result) { __result = false; return false; }
    static void ReturnFalse(ref bool __result) { __result = false; }
    static bool Begin(MapComponent keeper, Pawn original, Pawn host) => SpiritRebirthStateTests.Invoke(keeper, "TryBeginHostDeath", original, host);
    static MethodInfo PlaceMethod() => typeof(GenPlace).GetMethods().Single(m => m.Name == "TryPlaceThing" && m.GetParameters().Any(p => p.IsOut));
    static bool RefusePlacement(Thing thing, ref bool __result) { if (thing is not Thing_SoulStone) return true; __result = false; return false; }
    sealed class Fixture : IDisposable
    {
        readonly SpiritRebirthReleaseTests.ReleaseFixture release;
        readonly IDisposable ui;
        readonly ThingDef[] defs = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        readonly HediffDef oldPossession = TorannMagicDefOf.TM_SpiritPossessionHD;
        public MapComponent Keeper;
        public HediffComp_SpiritPossession Possession;
        public Fixture(RuntimeFixture r, SpiritRebirthRitualTransactionTests.Context c)
        {
            Check.Equal("ConvertedHeld", c.Execute(), "actual paid original prepared");
            release = new(r, c);
            ui = (IDisposable)Activator.CreateInstance(typeof(SpiritRebirthRuntimeTests).GetNestedType("UiPauseScope", BindingFlags.NonPublic)!)!;
            var receipt = ClassIdentity.Component(c.Original).GetType().GetProperty("SpiritRitual", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(ClassIdentity.Component(c.Original))!;
            Check.Equal("Released", receipt.GetType().GetMethod("TryRelease", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(receipt, [c.Creator, c.Map, new IntVec3(15, 0, 15)])!.ToString(), "actual original native release");
            c.Original.DeSpawn();
            TorannMagicDefOf.TM_SpiritPossessionHD = new HediffDef { defName = "MapCollapsePossession" };
            var hediff = new HediffWithComps { def = TorannMagicDefOf.TM_SpiritPossessionHD, pawn = c.Creator };
            Possession = new HediffComp_SpiritPossession { parent = hediff }; hediff.comps = [Possession]; c.Creator.health.hediffSet.hediffs.Add(hediff);
            var owner = new ThingOwner<Thing>(Possession); Set(Possession, "innerContainer", owner);
            Check.Equal(true, owner.TryAdd(c.Original, false), "native possession holds same original");
            var state = ClassIdentity.Component(c.Original).SpiritRebirth;
            Check.Equal(true, SpiritRebirthStateTests.Invoke(state, "TryEnterHost", c.Original, c.Creator), "exact native host transition");
            Set(state, "recoveryRequired", true); Set(state, "recoveryTicks", 87);
            c.Creator.def.race.body = c.Original.def.race.body;
            var stoneDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            stoneDef.defName = "TM_SoulStone"; stoneDef.thingClass = typeof(Thing_SoulStone); stoneDef.category = ThingCategory.Item; stoneDef.stackLimit = 1;
            stoneDef.size = new IntVec2(1, 1); stoneDef.comps = []; stoneDef.drawerType = DrawerType.RealtimeOnly; stoneDef.altitudeLayer = AltitudeLayer.Item; stoneDef.destroyable = true;
            DefDatabase<ThingDef>.Add(stoneDef);
            c.Map.components ??= [];
            c.Map.passingShipManager = new PassingShipManager(c.Map);
            Keeper = (MapComponent)Activator.CreateInstance(Production("MapComponent_SpiritCollapse"), [c.Map])!; c.Map.components.Add(Keeper);
        }
        public void Dispose() { ui.Dispose(); TorannMagicDefOf.TM_SpiritPossessionHD = oldPossession; DefDatabase<ThingDef>.Clear(); foreach (var def in defs) DefDatabase<ThingDef>.Add(def); release.Dispose(); }
    }
    static bool CancelNative() => false;
    sealed class ProjectileCreationObserved : Exception { }
    static bool ObserveProjectileCreation() => throw new ProjectileCreationObserved();
    static Thing? observedRayOwner;
    static readonly List<(IntVec3, Thing)> stormExplosions = [];
    sealed class SkyExplosionObserved(Thing? owner) : Exception { public Thing? Owner => owner; }
    static bool StopAtSkyExplosion(Thing instigator) => throw new SkyExplosionObserved(instigator);
    static bool ObserveStormExplosion(IntVec3 center, Thing instigator)
    {
        stormExplosions.Add((center, instigator));
        return false;
    }
    static readonly List<(int tick, bool paid)> rayLaunches = [];
    static bool RecordRayLaunch(bool paidBurst, ref bool __result)
    {
        rayLaunches.Add((Find.TickManager.TicksGame, paidBurst));
        __result = true;
        return false;
    }
    static bool ObserveDeathBoltImpact(AbilityUser.Projectile_AbilityBase __instance)
    {
        observedRayOwner = __instance.Launcher;
        return false;
    }
    static bool ObserveFrostRayOwner(AbilityUser.Projectile_AbilityLaser __instance)
    {
        observedRayOwner = __instance.Launcher;
        return false;
    }
    sealed class Patch : IDisposable
    {
        readonly object harmony; readonly string id = "rwom.map-collapse-fault." + Guid.NewGuid();
        public Patch(MethodBase target, string callback, bool postfix = false)
        {
            var assembly = Assembly.Load("0Harmony"); harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            var method = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(SpiritMapCollapseTests).GetMethod(callback, BindingFlags.NonPublic | BindingFlags.Static)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [target, postfix ? null : method, postfix ? method : null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
}
