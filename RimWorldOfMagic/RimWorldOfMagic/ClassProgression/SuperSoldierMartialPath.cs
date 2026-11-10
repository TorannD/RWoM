using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse.AI;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal sealed class SuperSoldierTraining : AdditionalMartialData
    {
        internal string School;
        internal bool Support;
        internal List<int> Weapons = new(), Wounds = new(), Hostiles = new(), Missions = new();
        public override void ExposeData()
        {
            Scribe_Values.Look(ref School, "weaponSchool");
            Scribe_Values.Look(ref Support, "fieldSurgeon");
            Scribe_Collections.Look(ref Weapons, "serviced", LookMode.Value);
            Scribe_Collections.Look(ref Wounds, "wounds", LookMode.Value);
            Scribe_Collections.Look(ref Hostiles, "hostiles", LookMode.Value);
            Scribe_Collections.Look(ref Missions, "missions", LookMode.Value);
            if (Scribe.mode==LoadSaveMode.PostLoadInit)
            {
                Weapons ??= new(); Wounds ??= new(); Hostiles ??= new(); Missions ??= new();
            }
        }
    }

    internal sealed class SuperSoldierMartialPath : AdditionalMartialPath
    {
        internal const string PathId = "rwom.supersoldier", Dressing = "TM_SuperSoldierFieldDressing";
        private static readonly string[] tracks = {"TM_ReadSuperSoldierCloseQuarters","TM_ReadSuperSoldierFieldAid"};
        internal static readonly string[] Schools = {"TM_PistolSpec","TM_RifleSpec","TM_ShotgunSpec"};
        private static readonly string[] techniques = {"TM_PistolWhip","TM_SuppressingFire","TM_Buckshot"};
        internal override string Id => PathId;
        internal override string Book => "TM_SuperSoldierFieldManual";
        internal override string[] Choices => tracks;
        internal static DisciplineProgress Path(Pawn p) => ClassIdentity.TryGetState(p,out var state)?state.Find(PathId):null;
        internal static Pawn Owner(Pawn actor)
        {
            if(Path(actor)!=null || actor?.story?.traits?.HasTrait(TorannMagicDefOf.TM_SuperSoldier)==true) return actor;
            if(!SpiritRebirthState.TryGetPossessingSpirit(actor,out var soul) ||
                !SpiritRebirthState.TryGet(soul,out var state) || state.DepletionPending) return null;
            return Path(soul)!=null || soul.story?.traits?.HasTrait(TorannMagicDefOf.TM_SuperSoldier)==true ? soul : null;
        }
        internal static SuperSoldierTraining Existing(DisciplineProgress path) => path?.MartialData as SuperSoldierTraining;
        private static SuperSoldierTraining Training(DisciplineProgress path) => (SuperSoldierTraining)(path.MartialData??=new SuperSoldierTraining());
        private static TMAbilityDef Def(string name) => DefDatabase<TMAbilityDef>.GetNamedSilentFail(name);
        internal override bool CanEnroll(CompAbilityUserMight might,string choice) =>
            might!=null && tracks.Contains(choice) && Def(Dressing)!=null && Def("TM_CQC")!=null;
        internal override IEnumerable<MightPower> Powers(CompAbilityUserMight might) =>
            might.MightData.MightPowersSS.Where(power=>power.learned);
        private static void Grant(CompAbilityUserMight might,string name,bool active)
        {
            var def=Def(name);
            var owner=def==null?null:might.MightData.MightPowersSS.FirstOrDefault(p=>p.TMabilityDefs.Contains(def));
            if(owner==null) return;
            owner.learned=true;
            if(active) TM_ClassState.AddRuntimeAbility(might,owner.abilityDef);
        }
        internal override void Restore(CompAbilityUserMight might,DisciplineProgress path)
        {
            if(!path.MartialAlternate || path.Stage>=InitiationStage.Adept) Grant(might,"TM_CQC",false);
            if(path.MartialAlternate || path.Stage>=InitiationStage.Adept) TM_ClassState.AddRuntimeAbility(might,Def(Dressing));
            int school=System.Array.IndexOf(Schools,Existing(path)?.School);
            if(school<0) return;
            Grant(might,Schools[school],false);
            if(path.Stage>=InitiationStage.Master) Grant(might,techniques[school],true);
        }
        internal static bool SelectSchool(Pawn p,string school)
        {
            var path=Path(p);var might=p?.GetCompAbilityUserMight();
            if(path==null || might==null || !Schools.Contains(school) || Def(school)==null || Existing(path)?.School!=null) return false;
            var known=might.MightData.MightPowersSS.Where(x=>x.learned && Schools.Contains(x.abilityDef?.defName)).ToList();
            if(known.Any(x=>x.abilityDef.defName!=school)) return false;
            Training(path).School=school;
            Grant(might,school,false);
            return true;
        }
        internal static bool CanInject(Pawn p) => Path(Owner(p))!=null && p.Spawned && !p.Downed && !p.Dead && p.health?.hediffSet!=null &&
            TorannMagicDefOf.TM_SS_SerumHD!=null && !p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SS_SerumHD);
        internal static bool Inject(Pawn p,Thing serum)
        {
            if(!CanInject(p) || serum?.def!=TorannMagicDefOf.BookOfSuperSoldier || serum.Destroyed || serum.stackCount<1 ||
                (serum.Spawned ? !p.Spawned || serum.Map!=p.Map || !p.CanReachImmediate(serum,Verse.AI.PathEndMode.Touch)
                    : serum.holdingOwner==null || (serum.holdingOwner!=p.inventory?.innerContainer && serum.holdingOwner!=p.carryTracker?.innerContainer))) return false;
            HealthUtility.AdjustSeverity(p,TorannMagicDefOf.TM_SS_SerumHD,.1f);
            serum.SplitOff(1).Destroy();
            return true;
        }
        internal static bool Cqc(Pawn pawn)
        {
            var path=Path(pawn);
            return path!=null && (!path.MartialAlternate || path.Stage>=InitiationStage.Adept);
        }
        private static bool HasCqc(Pawn pawn,CompAbilityUserMight comp) => comp!=null &&
            (pawn.story?.traits?.HasTrait(TorannMagicDefOf.TM_SuperSoldier)==true || Cqc(pawn) ||
             TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_CQC,null,comp));
        internal static CompAbilityUserMight CqcSource(Pawn body)
        {
            var local=body?.GetCompAbilityUserMight();
            if(HasCqc(body,local)) return local;
            var soul=Owner(body);
            var borrowed=soul!=body ? soul?.GetCompAbilityUserMight() : null;
            return HasCqc(soul,borrowed) ? borrowed : null;
        }
        internal static bool HasPistolTraining(CompAbilityUserMight comp) => comp!=null &&
            (comp.Pawn.story?.traits?.HasTrait(TorannMagicDefOf.TM_SuperSoldier)==true ||
             Existing(Path(comp.Pawn))?.School=="TM_PistolSpec" ||
             TM_ClassUtility.ClassHasAbility(TorannMagicDefOf.TM_PistolSpec,null,comp));
        internal static CompAbilityUserMight PistolSource(Pawn body)
        {
            var local=body?.GetCompAbilityUserMight();
            if(HasPistolTraining(local)) return local;
            var soul=Owner(body);
            var borrowed=soul!=body ? soul?.GetCompAbilityUserMight() : null;
            return HasPistolTraining(borrowed) ? borrowed : null;
        }
        internal static float ImpactDamage(Pawn body,CompAbilityUserMight training)
        {
            if(training.Pawn==body) return training.weaponDamage;
            // The legacy cache is keyed only by body, not by the soul's strength.
            // A borrowed attack must not reuse that cache or the soul's own weapon.
            var weapon=body.equipment?.Primary;
            if(weapon==null) return body.GetStatValue(StatDefOf.MeleeDPS,false)*training.mightPwr;
            if(weapon.def.IsMeleeWeapon)
                return Mathf.Max((body.GetStatValue(StatDefOf.MeleeDPS,false)+weapon.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS,false))*training.mightPwr,5f);
            return TM_Calc.RangedWeaponDamage(weapon,weapon.def.Verbs?.FirstOrDefault(),training.mightPwr);
        }
        internal static bool Matches(Pawn p,string school)
        {
            if(p?.equipment?.Primary==null) return false;
            return school switch {
                "TM_PistolSpec"=>TM_Calc.IsUsingPistol(p),
                "TM_RifleSpec"=>TM_Calc.IsUsingRifle(p),
                "TM_ShotgunSpec"=>TM_Calc.IsUsingShotgun(p),
                _=>false
            };
        }
        internal static bool CanService(Pawn p,Thing w)
        {
            var path=Path(p);
            return path?.Stage==InitiationStage.Foundation && w?.thingIDNumber>0 &&
                (Existing(path)?.Weapons.Count??0)<20 && Existing(path)?.Weapons.Contains(w.thingIDNumber)!=true;
        }
        internal static void Serviced(Pawn p,Thing w)
        {
            if(CanService(p,w)) Training(Path(p)).Weapons.Add(w.thingIDNumber);
        }
        internal static void Event(Pawn p,int episode,bool hostile,int day)
        {
            var path=Path(p);
            if(path==null || episode<=0) return;
            if(path.Stage==InitiationStage.Adept || (path.Stage==InitiationStage.Foundation && !hostile))
            {
                var d=Training(path);var list=hostile?d.Hostiles:d.Wounds;
                if(list.Count<(hostile?10:20) && !list.Contains(episode)) list.Add(episode);
            }
            if(path.Stage==InitiationStage.Master && day>=0)
            {
                var shifts=Training(path).Missions;
                if(shifts.Count<8 && !shifts.Contains(day)) shifts.Add(day);
            }
        }
        private static bool Ready(Pawn pawn,DisciplineProgress path)
        {
            var d=Existing(path);
            if(d?.School==null) return false;
            return path.Stage switch {
                InitiationStage.Foundation => d.Weapons.Count>=20 || d.Wounds.Count>=10,
                InitiationStage.Adept => d.Hostiles.Count>=10 || d.Wounds.Count>=20,
                InitiationStage.Master => d.Missions.Count>=8 && pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightUserLevel>=30,
                _=>false
            };
        }
        internal static bool Advance(Pawn pawn,bool support)
        {
            var path=Path(pawn);
            if(!Ready(pawn,path)) return false;
            var stage=path.Stage;
            string trial=PathId+"."+stage;
            path.RecordCompletedTrial(trial);
            if(!ClassIdentity.Component(pawn).State.TryAdvanceAfterTrial(PathId,stage,trial)) return false;
            if(stage==InitiationStage.Foundation) Training(path).Wounds.Clear();
            if(stage==InitiationStage.Master) Training(path).Support=support;
            new SuperSoldierMartialPath().Restore(pawn.GetCompAbilityUserMight(),path);
            return true;
        }
        internal override string Requirements(Pawn p,DisciplineProgress path)
        {
            var d=Existing(path);
            return path.Stage switch {
                InitiationStage.Foundation=>"TM_SoldierMaintenance".Translate(d?.Weapons.Count??0,d?.Wounds.Count??0),
                InitiationStage.Adept=>"TM_SoldierTraining".Translate(d?.Hostiles.Count??0,d?.Wounds.Count??0),
                InitiationStage.Master=>"TM_SoldierShifts".Translate(d?.Missions.Count??0,p.GetCompAbilityUserMight()?.ExistingMightData?.MightUserLevel??0),
                _=>(d?.Support==true?"TM_SoldierSurgeon":"TM_SoldierMarksman").Translate()
            };
        }
        internal override float ControlsHeight(Pawn pawn,DisciplineProgress path) => 80;
        internal override void DrawControls(Pawn p,DisciplineProgress path,float width,ref float y)
        {
            if(Existing(path)?.School==null && Widgets.ButtonText(new Rect(0,y,width,32),"TM_SoldierChooseSchool".Translate()))
                Find.WindowStack.Add(new FloatMenu(Schools.Where(s=>Def(s)!=null).Select(s=>new FloatMenuOption(Def(s).LabelCap,()=>SelectSchool(p,s))).ToList()));
            else if(Existing(path)?.School!=null) Widgets.Label(new Rect(0,y,width,32),Def(Existing(path).School)?.LabelCap.ToString());
            y+=40;
            if(Ready(p,path))
            {
                if(Widgets.ButtonText(new Rect(0,y,width,32),"TM_SoldierAdvance".Translate()))
                {
                    if(path.Stage!=InitiationStage.Master) Advance(p,false);
                    else Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>{
                        new FloatMenuOption("TM_SoldierMarksman".Translate(),()=>Advance(p,false)),
                        new FloatMenuOption("TM_SoldierSurgeon".Translate(),()=>Advance(p,true))}));
                }
            }
            else if(path.Stage==InitiationStage.Foundation && Widgets.ButtonText(new Rect(0,y,width,32),"TM_SoldierService".Translate())) JobDriver_BladeWeaponService.Target(p);
            y+=40;
        }
    }
}
