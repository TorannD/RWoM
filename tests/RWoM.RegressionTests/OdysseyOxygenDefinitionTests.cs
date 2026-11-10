using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using TorannMagic;

namespace RWoM.RegressionTests;

internal static class OdysseyOxygenDefinitionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Oxygen.Definitions.NativeComponentsAndInheritance", Native);
        yield return new("Odyssey.Oxygen.Definitions.SevenLanguagesAndFourViews", Content);
    }
    static string FilePath(RuntimeFixture f) => Path.Combine(f.Repository, "v1.7/Odyssey/Defs/Buildings_Oxygen.xml");
    static void Native(RuntimeFixture f)
    {
        var doc=new XmlDocument(); doc.Load(FilePath(f));
        var parent=new XmlDocument(); parent.Load(Path.Combine(f.Repository,"v1.7/Defs/ThingDefs/TM_BuildingBase.xml"));
        var old=DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear();
        try
        {
            XmlInheritance.TryRegister(parent.SelectSingleNode("Defs/ThingDef[@Name='TM_BuildingBase']")!,null);
            var node=doc.SelectSingleNode("Defs/ThingDef[defName='TM_MagicalOxygen']")!;
            XmlInheritance.TryRegister(node,null); XmlInheritance.Resolve();
            var resolved=XmlInheritance.GetResolvedNodeFor(node);
            Check.Equal("Building",resolved.SelectSingleNode("thingClass")!.InnerText,"native building owner");
            Check.Equal("(2,2)",resolved.SelectSingleNode("size")!.InnerText,"two by two footprint");
            Check.Equal("(0,0,-1)",resolved.SelectSingleNode("interactionCellOffset")!.InnerText,"front interaction cell");
            Check.Equal("true",resolved.SelectSingleNode("hasInteractionCell")!.InnerText,"accessible front interaction");
            Check.Equal("true",resolved.SelectSingleNode("rotatable")!.InnerText,"four orientations");
            Check.Equal("MinifiedThing",resolved.SelectSingleNode("minifiedDef")!.InnerText,"minifiable");
            Check.Equal("BuildingsMisc",resolved.SelectSingleNode("thingCategories/li")!.InnerText,"minifiable storage category");
            Check.Equal("Impassable",resolved.SelectSingleNode("passability")!.InnerText,"solid building");
            Check.Equal(1f,float.Parse(resolved.SelectSingleNode("fillPercent")!.InnerText,System.Globalization.CultureInfo.InvariantCulture),"full fill required for impassable player construction");
            Check.Equal("TorannMagic.Odyssey.PlaceWorker_ManaNetwork",resolved.SelectSingleNode("placeWorkers/li")!.InnerText,"shared network overlay");
            Check.Equal(true,resolved.SelectNodes("researchPrerequisites/li")!.Cast<XmlNode>().Select(n=>n.InnerText).ToHashSet().SetEquals(new[]{"TM_MagofuelRefining","OrbitalTech"}),"research gates");
            var all=new[]{"v1.7/Defs","v1.7/Odyssey/Defs"}.SelectMany(p=>Directory.GetFiles(Path.Combine(f.Repository,p),"*.xml",SearchOption.AllDirectories))
                .Concat(new[]{"Core","Odyssey"}.SelectMany(module=>Directory.GetFiles(Path.Combine(Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!,"Data",module,"Defs"),"*.xml",SearchOption.AllDirectories))).SelectMany(p=>XDocument.Load(p).Root!.Elements()).ToList();
            foreach(XmlNode cost in resolved.SelectNodes("costList/*")!)
                Check.Equal(true,all.Any(x=>x.Name=="ThingDef"&&(string?)x.Element("defName")==cost.Name),"real construction material "+cost.Name);
            foreach(XmlNode research in resolved.SelectNodes("researchPrerequisites/li")!)
                Check.Equal(true,all.Any(x=>x.Name=="ResearchProjectDef"&&(string?)x.Element("defName")==research.InnerText),"real research "+research.InnerText);
            Check.Equal(150,int.Parse(resolved.SelectSingleNode("costList/Steel")!.InnerText),"steel construction cost");
            Check.Equal(3,int.Parse(resolved.SelectSingleNode("costList/ComponentIndustrial")!.InnerText),"component construction cost");
            Check.Equal(100,int.Parse(resolved.SelectSingleNode("costList/RawMagicyte")!.InnerText),"magicyte construction cost");
            var comps=resolved.SelectNodes("comps/li")!.Cast<XmlNode>().ToList();
            Check.Equal(7,comps.Count,"oxygen, buffer, endpoint, fuel, switch, breakdown and visual glow");
            var parsed=comps.Select(n=>DirectXmlToObject.ObjectFromXml<CompProperties>(n,false)).ToList();
            var oxygen=parsed.Single(p=>p.GetType().FullName=="TorannMagic.Odyssey.CompProperties_MagicalOxygen");
            var buffer=parsed.Single(p=>p.GetType().FullName=="TorannMagic.Odyssey.CompProperties_OxygenManaBuffer");
            var endpoint=parsed.Single(p=>p.GetType().FullName=="TorannMagic.Odyssey.CompProperties_ManaNetworkEndpoint");
            object Field(CompProperties p,string name)=>p.GetType().GetField(name)!.GetValue(p)!;
            Check.Equal("TorannMagic.Odyssey.CompMagicalOxygen",oxygen.compClass.FullName!,"production oxygen comp");
            Check.Equal("TorannMagic.Odyssey.CompOxygenManaBuffer",buffer.compClass.FullName!,"dedicated non-converting buffer");
            Check.Equal(null,resolved.SelectSingleNode("comps/li/airPerSecondPerHundredCells"),"no room-wide oxygen pump");
            Check.Equal(.1f,(float)Field(oxygen,"manaPerSecond"),"paid mana rate");
            var oxygenComp=(ThingComp)Activator.CreateInstance(oxygen.compClass)!; oxygenComp.props=oxygen;
            Check.Equal(true,Math.Abs(250f-(float)oxygen.compClass.GetProperty("SupportedCells")!.GetValue(oxygenComp)!) < .001f,"rated local atmosphere capacity");
            Check.Equal(10f,(float)Field(oxygen,"manaPerMagicyte"),"magicyte paid credit");
            Check.Equal(60,(int)Field(oxygen,"pulseInterval"),"one second pulse");
            Check.Equal(100f,(float)Field(buffer,"manaCapacity"),"local mana capacity");
            Check.Equal(10f,(float)Field(buffer,"maxTransfer"),"network transfer bound");
            Check.Equal(true,(bool)Field(endpoint,"supportsMana"),"receives network mana");
            Check.Equal(false,(bool)Field(endpoint,"supportsFuel"),"no network magofuel channel");
            Check.Equal("Receive",Field(endpoint,"defaultManaMode").ToString()!,"receive mana by default");
            Check.Equal("Off",Field(endpoint,"defaultFuelMode").ToString()!,"fuel channel disabled");
            Check.Equal(1,parsed.OfType<CompProperties_Flickable>().Count(),"native switch");
            Check.Equal(1,parsed.OfType<CompProperties_Breakdownable>().Count(),"native breakdown");
            Check.Equal(0,parsed.OfType<CompProperties_Power>().Count(),"no electrical requirement");
            var fuel=parsed.OfType<CompProperties_Refuelable>().Single();
            var magic=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); magic.defName="RawMagicyte"; magic.category=ThingCategory.Item; magic.virtualDefs=[];
            DefDatabase<ThingDef>.Clear(); DefDatabase<ThingDef>.Add(magic);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors); fuel.fuelFilter.ResolveReferences();
            Check.Equal(true,fuel.fuelFilter.Allows(magic),"native magicyte filter resolved");
            Check.Equal(1,fuel.fuelFilter.AllowedThingDefs.Count(),"magicyte only");
            Check.Equal("Magicyte",fuel.fuelLabel,"native fuel name");
            Check.Equal("Magicyte",fuel.fuelGizmoLabel,"native fuel gizmo name");
            Check.Equal(50f,fuel.fuelCapacity,"local physical capacity");
            Check.Equal(50f,fuel.initialConfigurableTargetFuelLevel,"native refill target");
            Check.Equal(1f,fuel.FuelMultiplierCurrentDifficulty,"one crystal per fuel unit");
            Check.Equal(0f,fuel.fuelConsumptionPerTickInRain,"no rain consumption");
            Check.Equal(false,fuel.factorByDifficulty||fuel.destroyOnNoFuel,"no difficulty multiplier or destruction");
            Check.Equal(0f,fuel.initialFuelPercent,"no free initial magicyte");
            Check.Equal(0f,fuel.fuelConsumptionRate,"no independent automatic consumption");
            Check.Equal(true,fuel.consumeFuelOnlyWhenUsed&&fuel.functionsInVacuum&&fuel.targetFuelLevelConfigurable,"explicit operation and native refill controls");
            var building=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); building.comps=parsed; building.defName="TM_MagicalOxygen";
            building.size=new IntVec2(2,2); building.hasInteractionCell=true; building.interactionCellOffset=new IntVec3(0,0,-1);
            foreach(var rotation in new[]{Rot4.North,Rot4.East,Rot4.South,Rot4.West})
            {
                var position=new IntVec3(20,0,20); var rect=GenAdj.OccupiedRect(position,rotation,building.size);
                var outlet=ThingUtility.InteractionCellWhenAt(building,position,rotation,null!);
                Check.Equal(false,rect.Contains(outlet),"outlet outside footprint "+rotation);
                Check.Equal(true,GenAdj.CardinalDirections.Any(offset=>rect.Contains(outlet+offset)),"outlet adjacent "+rotation);
            }
            foreach(var props in new[]{oxygen,buffer,endpoint}) Check.Equal("",string.Join(";",props.ConfigErrors(building)),"custom component config errors "+props.GetType().Name);
            RuntimeLog.AssertClean();
        }
        finally { DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear(); DefDatabase<ThingDef>.Clear(); foreach(var def in old) DefDatabase<ThingDef>.Add(def); }
    }
    static void Content(RuntimeFixture f)
    {
        var folder=XDocument.Load(Path.Combine(f.Repository,"LoadFolders.xml")).Root!.Element("v1.7")!.Elements("li").Single(x=>x.Value=="v1.7/Odyssey");
        Check.Equal("ludeon.rimworld.odyssey",(string?)folder.Attribute("IfModActive"),"optional module");
        var defs=XDocument.Load(FilePath(f)).Root!.Elements().ToList();
        var graphic=defs.Single(x=>x.Name=="ThingDef").Element("graphicData")!;
        Check.Equal("Graphic_Multi",graphic.Element("graphicClass")!.Value,"native directional graphics"); Check.Equal("false",graphic.Element("allowFlip")!.Value,"no mirrored fallback");
        string[] langs=["English","Russian","Ukrainian","Spanish","ChineseSimplified","Portuguese","PortugueseBrazilian"];
        Dictionary<string,string> Keys(string language)=>XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Languages",language,"Keyed/TM_MagicalOxygen.xml")).Root!.Elements().ToDictionary(x=>x.Name.LocalName,x=>x.Value);
        var english=Keys("English");
        string[] required=["TM_OxygenMode","TM_OxygenModeMana","TM_OxygenModeMagicyte","TM_OxygenModeDesc","TM_OxygenBuffer","TM_OxygenPaidMagicyte","TM_OxygenStatusRunning","TM_OxygenStatusIdle","TM_OxygenStatusEmpty","TM_OxygenStatusOff","TM_OxygenCapacity","TM_OxygenPressure"];
        required = required.Concat(new[]{"TM_OxygenOutput","TM_OxygenOutputDesc","TM_OxygenOutputAtmosphere","TM_OxygenOutputPipe","TM_OxygenPipeInfo","TM_OxygenPipePending"}).ToArray();
        Check.Equal(true,required.ToHashSet().SetEquals(english.Keys),"all production UI keys");
        string Tokens(string value)=>string.Join("|",Regex.Matches(value,@"\{[^{}]+\}").Select(m=>m.Value).OrderBy(x=>x));
        foreach(var lang in langs)
        {
            var keys=Keys(lang); Check.Equal(true,english.Keys.ToHashSet().SetEquals(keys.Keys),lang+" keyed coverage");
            foreach(var pair in english) { Check.Equal(false,string.IsNullOrWhiteSpace(keys[pair.Key]),lang+" nonempty "+pair.Key); Check.Equal(Tokens(pair.Value),Tokens(keys[pair.Key]),lang+" placeholders "+pair.Key); }
            if(lang!="English") {
                var translated=XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Languages",lang,"DefInjected/ThingDef/TM_MagicalOxygen.xml")).Root!;
                foreach(var suffix in new[]{"comps.0.fuelLabel","comps.0.fuelGizmoLabel"})
                    Check.Equal(false,string.IsNullOrWhiteSpace(translated.Element("TM_MagicalOxygen."+suffix)?.Value),lang+" translated native fuel UI "+suffix);
            }
            foreach(var def in defs)
            foreach(var field in def.Elements().Where(x=>x.Name.LocalName is "label" or "description" or "reportString"))
            {
                if(lang=="English")continue;
                var path=Path.Combine(f.Repository,"v1.7/Odyssey/Languages",lang,"DefInjected",def.Name.LocalName,"TM_MagicalOxygen.xml");
                var text=XDocument.Load(path).Root!.Element(def.Element("defName")!.Value+"."+field.Name.LocalName)?.Value;
                Check.Equal(false,string.IsNullOrWhiteSpace(text),lang+" translated "+field.Name); Check.Equal(Tokens(field.Value),Tokens(text!),lang+" definition placeholders");
            }
        }
        var views=new HashSet<string>();
        foreach(var direction in new[]{"north","east","south","west"})
        {
            var path=Path.Combine(f.Repository,"v1.7/Odyssey/Textures",graphic.Element("texPath")!.Value+"_"+direction+".png");
            Check.Equal(true,File.Exists(path),"dedicated "+direction+" view");
            var bytes=File.ReadAllBytes(path);
            Check.Equal(true,bytes.Length>32&&bytes.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),"PNG signature "+direction);
            views.Add(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
        }
        Check.Equal(4,views.Count,"four distinct view files");
    }
}





