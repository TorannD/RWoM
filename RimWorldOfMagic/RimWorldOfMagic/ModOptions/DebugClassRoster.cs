using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using TorannMagic.ClassProgression;
using TorannMagic.Compatibility;
using TorannMagic.TMDefs;
using Verse;

namespace TorannMagic.ModOptions
{
    internal enum RosterMode { Preferred, Foundations, ClassicNative, Migration, Addons }

    internal sealed class RosterEntry
    {
        internal TraitDef Trait;
        internal int Degree, CustomIndex=-1;
        internal TM_CustomClass Custom;
        internal string Book, Choice, Foundation;
        internal bool Mage, Addon, Wanderer, FoundationOnly;
        internal bool HasFoundation=>Foundation!=null;
        internal string Key=>Trait.defName+"#"+Degree;
        internal string Title=>Trait.degreeDatas?.FirstOrDefault(x=>x.degree==Degree)?.label??Trait.label??Trait.defName;
        internal int ProfileSize=>(Custom?.classMageAbilities?.Count??0)+(Custom?.classFighterAbilities?.Count??0)+(Custom?.classHediff!=null?1:0)+
            (Custom?.learnableSpells?.Count??0)+(Custom?.learnableSkills?.Count??0);
    }

    internal static class DebugClassRoster
    {
        // PawnGenerator.GenerateTraits is synchronous. Only suppress the RWoM random roll for these new pawns.
        [ThreadStatic] internal static bool Generating;

        [DebugAction("RWoM","T: Spawn one colonist per class...",actionType=DebugActionType.ToolMap,allowedGameStates=AllowedGameStates.PlayingOnMap)]
        private static void Choose()
        {
            var map=Find.CurrentMap;var center=UI.MouseCell();
            if(map==null || !center.InBounds(map))return;
            var options=new List<FloatMenuOption>();
            foreach(RosterMode mode in Enum.GetValues(typeof(RosterMode)))
            {
                var captured=mode;
                int total=Catalog(mode).Count;
                string label=("TM_Roster"+mode).Translate()+" ("+total+")";
                options.Add(new FloatMenuOption(label,total==0?null:()=>Run(map,center,captured)));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        internal static List<RosterEntry> Catalog(RosterMode mode)
        {
            var list=new List<RosterEntry>();
            void Add(TraitDef trait,bool mage)
            {
                if(trait!=null && !list.Any(e=>e.Trait==trait))
                    list.Add(new RosterEntry{Trait=trait,Degree=trait.degreeDatas?.FirstOrDefault(d=>d.degree==0)?.degree??
                        trait.degreeDatas?.FirstOrDefault()?.degree??0,Mage=mage});
            }
            foreach(var t in TM_ClassGroups.MagicEntriesField)Add(DefDatabase<TraitDef>.GetNamedSilentFail(t.trait),true);
            foreach(var t in TM_ClassGroups.MightEntriesField)Add(DefDatabase<TraitDef>.GetNamedSilentFail(t.trait),false);
            var customClasses=TM_ClassUtility.CustomClasses;
            for(int i=0;i<(customClasses?.Count??0);i++)
            {
                var cc=customClasses[i];
                if(cc?.classTrait==null || (!cc.isMage && !cc.isFighter))continue;
                int degree=ClassDefinitionCompatibility.CanonicalTraitDegree(cc.classTrait,cc.traitDegree);
                var origin=TorannMagicDefOf.TM_Gifted?.modContentPack;
                bool addon=cc.classTrait.modContentPack!=origin || cc.classMageAbilities?.Any(a=>a?.modContentPack!=null && a.modContentPack!=origin)==true ||
                    cc.classFighterAbilities?.Any(a=>a?.modContentPack!=null && a.modContentPack!=origin)==true ||
                    (cc.fullScript?.modContentPack!=null && cc.fullScript.modContentPack!=TorannMagicDefOf.TM_Gifted?.modContentPack);
                var item=new RosterEntry{Trait=cc.classTrait,Mage=cc.isMage,Degree=degree,Custom=cc,CustomIndex=i,Addon=addon};
                var prior=list.Find(e=>e.Key==item.Key);
                if(prior!=null && prior.Custom!=null &&
                    (prior.ProfileSize>item.ProfileSize || prior.ProfileSize==item.ProfileSize && customClasses[prior.CustomIndex].traitDegree==degree))continue;
                if(prior!=null)list.Remove(prior);
                list.Add(item);
            }
            foreach(var p in ClassInitiation.FoundationProfiles)
            {
                if(p.legacyTrait==null)continue;
                Add(p.legacyTrait,true);
                var row=list.FirstOrDefault(e=>e.Trait==p.legacyTrait && !e.Addon);
                if(row!=null)row.Foundation=p.profileId;
            }
            void Martial(string trait,string id,string book,string choice)
            {
                var e=list.FirstOrDefault(x=>x.Trait.defName==trait && !x.Addon);
                if(e==null)return;
                var form=DefDatabase<ThingDef>.GetNamedSilentFail(book);
                if(form==null || AdditionalMartialRegistry.ForId(id)?.SupportsBook(form)==false)return;
                if(e.HasFoundation && e.Foundation!=id)
                {
                    e=new RosterEntry{Trait=e.Trait,Degree=e.Degree,FoundationOnly=true};list.Add(e);
                }
                e.Foundation=id;e.Book=book;e.Choice=choice;
            }
            Martial("Gladiator","rwom.gladiator","BookOfGladiator","TM_ReadGladiatorSprint");
            Martial("TM_Sniper","rwom.sniper","BookOfSniper","TM_ReadSniperFocus");
            Martial("Bladedancer","rwom.bladedancer","BookOfBladedancer","TM_ReadBladeFocus");
            Martial("Ranger","rwom.ranger","BookOfRanger","TM_ReadRangerTraining");
            foreach(var p in AdditionalMartialRegistry.Paths)
            {
                string[] names=p.Id=="rwom.shadowwalker" ? new[]{"TM_TheShadow"} : new[]{p.Id.Substring(5),"TM_"+p.Id.Substring(5)};
                var row=list.FirstOrDefault(e=>names.Contains(e.Trait.defName,StringComparer.OrdinalIgnoreCase) && !e.Addon);
                if(row!=null)Martial(row.Trait.defName,p.Id,p.Book,p.Choices[0]);
            }
            foreach(var e in list.Where(e=>e.Trait==TorannMagicDefOf.TM_Wanderer || e.Trait==TorannMagicDefOf.TM_Wayfarer))
            {e.Wanderer=true;e.Foundation="rwom.wanderer";}
            return Select(list,mode,ClassProgressionFeature.Enabled);
        }

        internal static List<RosterEntry> Select(IEnumerable<RosterEntry> entries,RosterMode mode,bool enabled)
        {
            var rows=entries.Where(e=>mode switch{
                RosterMode.Addons=>e.Addon,
                RosterMode.Foundations=>enabled && e.HasFoundation && !e.Addon,
                RosterMode.ClassicNative or RosterMode.Migration=>!e.Addon && !e.FoundationOnly,
                _=>enabled || !e.FoundationOnly});
            bool modern=enabled && (mode==RosterMode.Preferred || mode==RosterMode.Foundations);
            return rows.OrderBy(e=>e.Trait.defName,StringComparer.Ordinal).ThenBy(e=>e.Degree)
                .GroupBy(e=>modern && e.HasFoundation ? e.Foundation:e.Key,StringComparer.Ordinal)
                .Select(g=>g.First()).ToList();
        }

        internal static bool ClearCell(IntVec3 cell,Map map,ThingDef race) =>
            map!=null && race!=null && cell.InBounds(map) && !cell.Fogged(map) && cell.Standable(map) && cell.GetEdifice(map)==null &&
            cell.GetFirstPawn(map)==null && (!ModsConfig.OdysseyActive || map.Biome?.inVacuum!=true || cell.GetVacuum(map)<.5f) && !cell.ContainsStaticFire(map) && !GenSpawn.WouldWipeAnythingWith(cell,Rot4.North,race,map,_=>true);
        private static void Run(Map map,IntVec3 center,RosterMode mode)
        {
            if(map==null || map!=Find.CurrentMap || !center.InBounds(map))return;
            var rows=Catalog(mode);var report=new List<string>();int placed=0;
            var used=new HashSet<IntVec3>();
            foreach(var row in rows)
            {
                Pawn pawn=null;
                try
                {
                    if(row.Custom!=null && row.ProfileSize==0 && !row.HasFoundation)
                        throw new InvalidOperationException("registered class has no granted or learnable abilities or class hediff");
                    bool spirit=row.Trait==TorannMagicDefOf.TM_Possessor;
                    var kind=PawnKindDefOf.Colonist;
                    var race=spirit?TorannMagicDefOf.TM_SpiritTD:kind.race;
                    var cell=GenRadial.RadialCellsAround(center,32,true).FirstOrDefault(c=>!used.Contains(c) && ClearCell(c,map,race),IntVec3.Invalid);
                    if(!cell.IsValid){report.Add(row.Key+" — no free cell");continue;}
                    var blocked=TM_Data.MagicTraits.Concat(TM_Data.MightTraits).Where(t=>t!=null).Distinct();
                    bool previous=Generating;Generating=true;
                    try {pawn=spirit ? TM_Action.GenerateSpiritPawn(cell,Faction.OfPlayer) :
                        PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind,Faction.OfPlayer,PawnGenerationContext.NonPlayer,map.Tile,
                        forceGenerateNewPawn:true,canGeneratePawnRelations:false,mustBeCapableOfViolence:true,prohibitedTraits:blocked));}
                    finally {Generating=previous;}
                    if(pawn!=null && (pawn.Spawned || Find.WorldPawns.Contains(pawn)))
                    {pawn=null;throw new InvalidOperationException("generator returned an existing pawn");}
                    if(pawn==null || pawn.Destroyed || pawn.Dead || !ClearCell(cell,map,pawn.def))throw new InvalidOperationException("could not place generated pawn");
                    GenSpawn.Spawn(pawn,cell,map);used.Add(cell);
                    bool modern=ClassProgressionFeature.Enabled && row.HasFoundation && (mode==RosterMode.Preferred || mode==RosterMode.Foundations);
                    if(!Grant(pawn,row,modern,mode!=RosterMode.Migration))throw new InvalidOperationException("class grant refused");
                    report.Add(row.Key+" — "+(modern?row.Foundation:"classic")+" — "+pawn.LabelShort+" "+cell);
                    placed++;pawn=null;
                }
                catch(Exception e){report.Add(row.Key+" — "+e.Message);}
                finally {if(pawn!=null && !pawn.Destroyed){if(pawn.Spawned)pawn.DeSpawn();pawn.Discard(true);}}
            }
            var summary="TM_RosterReport".Translate(placed,rows.Count);
            Messages.Message(summary,placed==rows.Count?MessageTypeDefOf.TaskCompletion:MessageTypeDefOf.CautionInput);
            TM_Log.Message("Classes",summary+"\n"+string.Join("\n",report));
        }
        internal static bool Grant(Pawn pawn,RosterEntry row,bool modern,bool keepClassic)
        {
            if(pawn?.story?.traits==null || !pawn.Spawned || pawn.Dead || pawn.Faction?.IsPlayer!=true)return false;
            Utils.TM_PawnTracker.EnsureClassComps(pawn);
            if((modern || pawn.def!=TorannMagicDefOf.TM_SpiritTD) && (ClassEnrollment.HasChosenClass(pawn) || !ClassEnrollment.HasNoPriorOwnership(pawn)))return false;
            if(!modern)
            {
                var owner=pawn.GetComp<CompClassProgression>();
                if(owner!=null)owner.DebugKeepClassic=keepClassic;
                return TM_DebugTools.ApplyClassTrait(pawn,row.Trait,row.Degree,row.CustomIndex,row.Custom,row.Mage,false) &&
                    pawn.story.traits.allTraits.Any(t=>t.def==row.Trait && t.Degree==row.Degree) &&
                    (!(row.Mage || row.Custom?.isMage==true) || pawn.GetCompAbilityUserMagic()?.ExistingMagicData!=null) &&
                    (!(!row.Mage || row.Custom?.isFighter==true) || pawn.GetCompAbilityUserMight()?.ExistingMightData!=null);
            }
            if(row.Wanderer || row.Book!=null)
            {
                if(!pawn.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy))pawn.story.traits.GainTrait(new Trait(TorannMagicDefOf.PhysicalProdigy));
                if(row.Wanderer)return ClassEnrollment.ChooseWanderer(pawn,Vocation.WarriorMage);
                if(row.Trait==TorannMagicDefOf.TM_Psionic && !PsionicMartialPath.CanAwaken(pawn))
                {
                    var psi=DefDatabase<TraitDef>.GetNamedSilentFail("PsychicSensitivity");
                    if(psi==null)return false;
                    foreach(var t in pawn.story.traits.allTraits.Where(t=>t.def==psi).ToList())pawn.story.traits.RemoveTrait(t);
                    pawn.story.traits.GainTrait(new Trait(psi,2));
                }
                var form=DefDatabase<ThingDef>.GetNamedSilentFail(row.Book);
                if(form==null || pawn.inventory==null)return false;
                var book=ThingMaker.MakeThing(form);
                try{return pawn.inventory.innerContainer.TryAdd(book,false) && JobDriver_GladiatorInitiation.Complete(pawn,book,row.Choice);}
                finally{if(!book.Destroyed)book.Destroy(DestroyMode.Vanish);}
            }
            return ClassInitiation.TryGrantStartingFoundation(pawn,row.Trait);
        }
    }
}
