using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal sealed class ApothecaryProgress : AdditionalMartialData
    {
        internal List<int> Plants = new();
        internal List<int> Wounds = new();
        internal List<int> Poisonings = new();
        internal int ServiceDays;
        internal int NextShift;
        internal int KitReadyAt;
        internal bool GoldenHour;
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref Plants,"harvests",LookMode.Value);
            Scribe_Collections.Look(ref Wounds,"woundEpisodes",LookMode.Value);
            Scribe_Collections.Look(ref Poisonings,"toxinEpisodes",LookMode.Value);
            Scribe_Values.Look(ref ServiceDays,"serviceDays");
            Scribe_Values.Look(ref NextShift,"nextShift");
            Scribe_Values.Look(ref KitReadyAt,"kitReadyAt");
            Scribe_Values.Look(ref GoldenHour,"goldenHour");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Plants ??= new(); Wounds ??= new(); Poisonings ??= new();
            }
        }
    }

    internal sealed class ApothecaryMartialPath : AdditionalMartialPath
    {
        internal const string Career = "rwom.apothecary";
        internal const string Toxin = "TM_ApothecaryAntitoxin";
        internal const string Kit = "TM_ApothecaryGoldenHour";
        internal override string Id => Career;
        internal override string Book => "BookOfApothecary";
        internal override string[] Choices { get; } = { "TM_ReadApothecaryHerbalist", "TM_ReadApothecaryFreshStock" };
        internal static DisciplineProgress Path(Pawn p) => ClassIdentity.TryGetState(p,out var state) ? state.Find(Career) : null;
        internal static Pawn Owner(Pawn actor)
        {
            // A carrier's own class always wins; possession cannot spend or train
            // the other apothecary's stock when the body already has this vocation.
            if (Path(actor)!=null || actor?.story?.traits?.HasTrait(TorannMagicDefOf.TM_Apothecary)==true) return actor;
            if (!SpiritRebirthState.TryGetPossessingSpirit(actor,out var soul) ||
                !SpiritRebirthState.TryGet(soul,out var spirit) || spirit.DepletionPending) return null;
            return Path(soul)!=null || soul.story?.traits?.HasTrait(TorannMagicDefOf.TM_Apothecary)==true ? soul : null;
        }
        internal static ApothecaryProgress Data(DisciplineProgress p) => (ApothecaryProgress)(p.MartialData ??= new ApothecaryProgress());
        internal static Hediff_ApothecaryHerbs Stock(Pawn p) =>
            p?.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_ApothecaryHerbsHD) as Hediff_ApothecaryHerbs;
        internal static TMAbilityDef Ability(string name) => DefDatabase<TMAbilityDef>.GetNamedSilentFail(name);

        private static readonly HashSet<string> NativeAbilities = new() {
            "TM_Herbalist","TM_SoothingBalm","TM_PoisonFlask","TM_Elixir","TM_FireSuppressionFlask",
            "TM_FireSuppressionFlask_I","TM_FireSuppressionFlask_II","TM_FireSuppressionFlask_III" };
        internal override bool SupportsBook(ThingDef book)
        {
            if (book?.defName != Book) return false;
            // Include alternate class claims introduced by patches even when their class selector shares our book.
            return !TM_ClassUtility.CustomClasses.Any(c => c != null && (c.fullScript?.defName == Book || c.tornScript?.defName == Book) &&
                (c.classTrait != TorannMagicDefOf.TM_Apothecary || c.isMage || c.isAdvancedClass || !c.isFighter ||
                 c.classHediff != TorannMagicDefOf.TM_ApothecaryHerbsHD ||
                 c.classFighterAbilities == null || !NativeAbilities.SetEquals(c.classFighterAbilities.Select(a=>a?.defName)) || c.classMageAbilities?.Count > 0));
        }
        internal override bool CanEnroll(CompAbilityUserMight might, string choice) =>
            TorannMagicDefOf.TM_Herbalist != null && TorannMagicDefOf.TM_SoothingBalm != null &&
            TorannMagicDefOf.TM_ApothecaryHerbsHD != null && SupportsBook(DefDatabase<ThingDef>.GetNamedSilentFail(Book));
        private static MightPower Native(MightData data, TMAbilityDef def) =>
            def == null ? null : data.MightPowersApothecary.FirstOrDefault(p=>p.TMabilityDefs.Contains(def));
        private static MightPower Flask(MightData data) =>
            data.MightPowersCustomAll.FirstOrDefault(p=>p.TMabilityDefs?.FirstOrDefault()?.defName == "TM_FireSuppressionFlask");
        private static void Grant(CompAbilityUserMight might, MightPower owner)
        {
            if (owner?.abilityDef == null) return;
            owner.learned=true;
            if (owner.abilityDef != TorannMagicDefOf.TM_Herbalist) TM_ClassState.AddRuntimeAbility(might,owner.abilityDef);
        }
        internal override IEnumerable<MightPower> Powers(CompAbilityUserMight might)
        {
            var data = might.MightData;
            return data.MightPowersApothecary.Concat(new[] { Flask(data) }).Where(p=>p?.learned==true);
        }
        internal override void Restore(CompAbilityUserMight might, DisciplineProgress path)
        {
            var data = might.MightData;
            var pawn = might.Pawn;
            if (Stock(pawn) == null && TorannMagicDefOf.TM_ApothecaryHerbsHD != null && pawn.health?.hediffSet != null)
            {
                var stock = HediffMaker.MakeHediff(TorannMagicDefOf.TM_ApothecaryHerbsHD,pawn);
                stock.Severity = 0; // rebuilding a lost container must never refill its contents
                pawn.health.AddHediff(stock);
            }
            Grant(might,Native(data,TorannMagicDefOf.TM_Herbalist));
            Grant(might,Native(data,TorannMagicDefOf.TM_SoothingBalm));
            if (path.Stage >= InitiationStage.Adept)
            {
                Grant(might,Flask(data));
                TM_ClassState.AddRuntimeAbility(might,Ability(Toxin));
            }
            if (path.Stage >= InitiationStage.Master) Grant(might,Native(data,TorannMagicDefOf.TM_PoisonFlask));
            if (path.Stage != InitiationStage.Ascended) return;
            if (path.MartialData is ApothecaryProgress trained && trained.GoldenHour)
                TM_ClassState.AddRuntimeAbility(might,Ability(Kit));
            else Grant(might,Native(data,TorannMagicDefOf.TM_Elixir));
        }
        internal static bool Ready(DisciplineProgress path)
        {
            if (path?.MartialData is not ApothecaryProgress d) return false;
            return path.Stage switch {
                InitiationStage.Foundation => d.Plants.Count >= 30,
                InitiationStage.Adept => d.Wounds.Count >= 20 || d.Poisonings.Count >= 12,
                InitiationStage.Master => d.ServiceDays >= 12,
                _ => false };
        }
        internal static void Harvest(Pawn p, int plantId)
        {
            var path=Path(p);
            if(path?.Stage != InitiationStage.Foundation || plantId <= 0 || Ready(path)) return;
            var d=Data(path);
            if (!d.Plants.Contains(plantId)) d.Plants.Add(plantId);
        }
        internal static void Treatment(Pawn p, int episode, bool poisoning)
        {
            var path=Path(p);
            if (path == null || episode <= 0 || Ready(path)) return;
            if (path.Stage == InitiationStage.Adept)
            {
                var d=Data(path);
                var episodes=poisoning ? d.Poisonings : d.Wounds;
                if (!episodes.Contains(episode)) episodes.Add(episode);
            }
            else if (path.Stage == InitiationStage.Master)
            {
                var d=Data(path);
                int now=Find.TickManager.TicksGame;
                if (now < d.NextShift) return;
                d.NextShift=now+GenDate.TicksPerDay;
                d.ServiceDays++;
            }
        }
        internal static bool Claim(Pawn p,bool kit)
        {
            if (LegacyMartialConversion.AlternativeConflicts(p,kit)) return false;
            var path=Path(p);
            if (!Ready(path)) return false;
            var might=p.GetCompAbilityUserMight();
            var stage=path.Stage;
            if (stage == InitiationStage.Foundation && (Flask(might.MightData)?.abilityDef == null || Ability(Toxin)==null) ||
                stage == InitiationStage.Adept && Native(might.MightData,TorannMagicDefOf.TM_PoisonFlask)?.abilityDef==null ||
                stage == InitiationStage.Master && (might.MightData.MightUserLevel < 30 || (kit ? Ability(Kit)==null : Native(might.MightData,TorannMagicDefOf.TM_Elixir)?.abilityDef==null)))
                return false;
            string trial=Career+"."+stage;
            path.RecordCompletedTrial(trial);
            if (!ClassIdentity.Component(p).State.TryAdvanceAfterTrial(Career,stage,trial)) return false;
            if (stage==InitiationStage.Master) Data(path).GoldenHour=kit;
            new ApothecaryMartialPath().Restore(might,path);
            return true;
        }
        internal override string Requirements(Pawn p,DisciplineProgress path)
        {
            var d=path.MartialData as ApothecaryProgress;
            var stock=Stock(p);
            string text="TM_ApothecaryStock".Translate(stock?.Severity.ToString("0.0")??"0", stock?.MaxSeverity.ToString("0")??"0");
            text += "\n" + (path.MartialAlternate?"TM_ApothecaryFreshInfo":"TM_ApothecaryGatherInfo").Translate();
            if (path.Stage==InitiationStage.Foundation) return text+"\n"+"TM_ApothecaryTrialGather".Translate(d?.Plants.Count??0);
            if (path.Stage==InitiationStage.Adept) return text+"\n"+"TM_ApothecaryTrialTreat".Translate(d?.Wounds.Count??0,d?.Poisonings.Count??0);
            if (path.Stage==InitiationStage.Master) return text+"\n"+"TM_ApothecaryTrialShift".Translate(d?.ServiceDays??0,p.GetCompAbilityUserMight().MightData.MightUserLevel);
            return text+"\n"+(d?.GoldenHour==true ? Ability(Kit)?.LabelCap.ToString() : TorannMagicDefOf.TM_Elixir.LabelCap.ToString());
        }
        internal override float ControlsHeight(Pawn p,DisciplineProgress path) => Ready(path)?44f:0f;
        internal override void DrawControls(Pawn p,DisciplineProgress path,float width,ref float y)
        {
            if (!Ready(path)) return;
            if ((path.Stage!=InitiationStage.Master || p.GetCompAbilityUserMight().MightData.MightUserLevel>=30) &&
                Widgets.ButtonText(new Rect(0,y,width,40),"TM_ApothecaryClaim".Translate()))
            {
                if (path.Stage!=InitiationStage.Master) Claim(p,false);
                else Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>{
                    new FloatMenuOption(TorannMagicDefOf.TM_Elixir.LabelCap,()=>Claim(p,false)),
                    new FloatMenuOption(Ability(Kit).LabelCap,()=>Claim(p,true))}));
            }
            y+=44;
        }
    }
}
