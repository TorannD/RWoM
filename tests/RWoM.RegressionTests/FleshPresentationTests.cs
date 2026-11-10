using System.Reflection;
using System.Runtime.CompilerServices;
using TorannMagic.FleshDevelopment;
using Verse;
using RimWorld;
namespace RWoM.RegressionTests;
internal static class FleshPresentationTests
{
    static object? Call(string method, params object[] args) {
        var type = typeof(FleshProgress).Assembly.GetType("TorannMagic.FleshDevelopment.FleshPresentation");
        Check.NotNull(type, "Shared flesh presentation policy exists");
        return type!.GetMethod(method)!.Invoke(null, args);
    }
    public static IEnumerable<TestCase> Cases() {
        yield return new("FleshPresentation.RealInspectHookIsPerPawnAndSwitchesLive", _ => {
            Check.Equal(typeof(Thing), typeof(Pawn).GetMethod(nameof(Pawn.GetInspectTabs))!.DeclaringType, "installed pawn inherits the patched method through ThingWithComps");
            var pawn = MakePawn();
            var settings = TorannMagic.ModOptions.Settings.Instance ?? (TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings()); bool previous = settings.grimoireOnAbilityBar;
            var rootField = typeof(Current).GetField("rootInt", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;
            var previousRoot = rootField.GetValue(null);
            var root = (Root_Play)RuntimeHelpers.GetUninitializedObject(typeof(Root_Play));
            var ui = (UIRoot_Play)RuntimeHelpers.GetUninitializedObject(typeof(UIRoot_Play));
            var mapUi = (MapInterface)RuntimeHelpers.GetUninitializedObject(typeof(MapInterface));
            var selector = (Selector)RuntimeHelpers.GetUninitializedObject(typeof(Selector));
            root.uiRoot = ui; ui.mapUI = mapUi; mapUi.selector = selector;
            typeof(Selector).GetField("selected", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)!.SetValue(selector,new List<object>{pawn});
            rootField.SetValue(null,root);
            try {
                settings.grimoireOnAbilityBar = false;
                IEnumerable<InspectTabBase> tabs = Array.Empty<InspectTabBase>();
                FleshInspectTabsPatch.Postfix(pawn, ref tabs);
                var tab = tabs.Single();
                Check.Equal(true, tab.IsVisible, "actual selected undead tab visible");
                FleshInspectTabsPatch.Postfix(pawn, ref tabs);
                Check.Equal(1, tabs.Count(), "hook cannot duplicate tab");
                Check.Equal<List<InspectTabBase>?>(null, pawn.def.inspectorTabsResolved, "shared race definition untouched");
                settings.grimoireOnAbilityBar = true;
                Check.Equal(false, tab.IsVisible, "existing tab reacts to setting without reload");
                tabs = Array.Empty<InspectTabBase>(); FleshInspectTabsPatch.Postfix(pawn, ref tabs);
                Check.Equal(0, tabs.Count(), "no tab appended in bar mode");
            } finally { settings.grimoireOnAbilityBar = previous; rootField.SetValue(null,previousRoot); }
        });
        yield return new("FleshPresentation.SettingsSwitchOnlyGrimoire", _ => {
            var pawn = MakePawn();
            var settings = TorannMagic.ModOptions.Settings.Instance ?? (TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings());
            bool previous = settings.grimoireOnAbilityBar;
            try {
                foreach (bool bar in new[] { true, false }) {
                    settings.grimoireOnAbilityBar = bar;
                    Check.Equal(bar, (bool)Call("ShowOnBar", pawn)!, "grimoire bar mode");
                    Check.Equal(!bar, (bool)Call("ShowTab", pawn)!, "grimoire tab mode");
                    Check.Equal(true, (bool)Call("Eligible", pawn)!, "resource/abilities independent of mode");
                    Check.Equal("Devour", string.Join(",", (IEnumerable<FleshSkill>)Call("ActiveSkills", pawn)!), "fresh undead has Devour in either mode");
                }
            } finally { settings.grimoireOnAbilityBar = previous; }
        });
        yield return new("FleshPresentation.LearnedActivesAndLabels", _ => {
            var pawn = MakePawn(); var data = FleshRepository.TryGet(pawn);
            Check.Equal("TM_Flesh_NoPoints", Call("UpgradeLabelKey", data, FleshSkill.Restore), "no points text");
            data.SkillPoints = 3; data.TryUpgrade(FleshSkill.Restore); data.TryUpgrade(FleshSkill.Mobility);
            data.SetReadyTick(FleshSkill.Restore, int.MaxValue);
            Check.Equal("Devour,Restore", string.Join(",", (IEnumerable<FleshSkill>)Call("ActiveSkills", pawn)!), "passives/locked excluded, cooldown remains visible");
            Check.Equal("TM_Flesh_Upgrade", Call("UpgradeLabelKey", data, FleshSkill.Restore), "upgrade with points");
            Check.Equal("TM_Flesh_ActiveKind", Call("SkillKindKey", FleshSkill.Restore), "active suffix");
            Check.Equal("TM_Flesh_PassiveKind", Call("SkillKindKey", FleshSkill.Mobility), "passive suffix");
            data.Thirst = 10;
            Check.Equal(.5f, (float)Call("ThirstFraction", data)!, "bar fill reads current thirst");
            data.TryUpgrade(FleshSkill.Capacity);
            Check.Equal(true, Math.Abs((float)Call("ThirstFraction", data)! - 1f/3f) < .0001f, "capacity reflected without stale cache");
        });
        yield return new("FleshPresentation.NoUiInitializationOrForeignControls", _ => {
            var pawn = MakePawn(false);
            Check.Equal(false, (bool)Call("Eligible", pawn)!, "rendering does not initialize old pawn");
            Check.Equal<FleshProgress?>(null, FleshRepository.TryGet(pawn), "no state mutation");
            FleshRepository.Ensure(pawn);
            typeof(Thing).GetField("factionInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pawn, new Faction { def = new FactionDef() });
            Check.Equal(false, (bool)Call("Eligible", pawn)!, "foreign undead excluded");
        });
    }
    static Pawn MakePawn(bool initialize = true) {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); def.race = new RaceProperties();
        var pawn = new Pawn { def = def }; pawn.health = new Pawn_HealthTracker(pawn);
        pawn.health.hediffSet.hediffs.Add(new Hediff { pawn=pawn, def=new HediffDef { defName="TM_UndeadHD" } });
        typeof(Thing).GetField("factionInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pawn,new Faction { def=new FactionDef { isPlayer=true } });
        if (initialize) FleshRepository.Ensure(pawn);
        return pawn;
    }
}
