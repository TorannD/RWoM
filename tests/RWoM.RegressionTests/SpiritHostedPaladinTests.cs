using System.Reflection;
using RimWorld;
using TorannMagic;
using static RWoM.RegressionTests.GrimoireProgressionTests;
using TorannMagic.ClassProgression;
using Verse;

namespace RWoM.RegressionTests;

internal static class SpiritHostedPaladinTests
{
    public static IEnumerable<TestCase> Cases() => SpiritMapCollapseTests.HostedPaladinCases();
}

internal static partial class SpiritMapCollapseTests
{
    internal static IEnumerable<TestCase> HostedPaladinCases()
    {
        foreach (var spell in new[] { "HolyWrath", "P_RayofHope", "P_RayofHope_III", "RayofHope", "RayofHope_III", "Shadow", "Shadow_I", "Shadow_II", "Shadow_III", "Soothe", "Soothe_I", "Soothe_II", "Soothe_III" })
            yield return new((spell.StartsWith("Shadow") || spell.StartsWith("Soothe") ? "SpiritHostedAura" : "SpiritHostedPaladin") + ".OwnerAndLocalDuplicate." + spell, r => SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var fixture = new Fixture(r, c);
                var def = (TMAbilityDef)typeof(TorannMagicDefOf).GetField("TM_" + spell)!.GetValue(null)!;
                var soul = c.Original;
                var body = c.Creator;
                var owner = soul.GetCompAbilityUserMagic();
                var power = new MagicAbility(owner){ Def=def };
                owner.AbilityData.Powers.Add(power);
                owner.AbilityData.AllPowers=null;
                var api = Production("SpiritHostedBolt");
                var find = api.GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic)!;
                var job = new JobDef { defName="TM_Spirit"+spell };
                Check.Same(def,api.GetMethod("Spell",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[job]),"job selects registered native def");
                Check.Same(power,find.Invoke(null,[body,soul,def]),"soul owns exact native power");
                var local = body.GetCompAbilityUserMagic();
                local.AbilityData.Powers.Add(new MagicAbility(local){Def=def});
                local.AbilityData.AllPowers=null;
                Check.Equal(null,find.Invoke(null,[body,soul,def]),"host native copy wins");
            }));
        foreach (var shadow in new[]{true,false})
        yield return new("SpiritHostedAura.ActualAllyTrial."+(shadow?"Shadow":"Soothe"), r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var fixture = new Fixture(r,c);
            var soul=c.Original;
            var body=c.Creator;
            var id=shadow?"rwom.arcanist":"rwom.ice";
            var trial=shadow?ArcanistTrials.MasteryTrialId:IceTrials.FoundationTrialId;
            var pathState=new ClassProgressionState(Vocation.Mage);
            Set(ClassIdentity.Component(soul),"state",pathState);
            Check.Equal(true,pathState.TryAddFoundation(id,DisciplineDomain.Magic),"prepared native mage");
            if(shadow)
            {
                pathState.Find(id).RecordCompletedTrial(ArcanistTrials.FoundationTrialId);
                Check.Equal(true,pathState.TryAdvanceAfterTrial(id,InitiationStage.Foundation,ArcanistTrials.FoundationTrialId),"adept Arcanist");
            }
            var root=shadow?TorannMagicDefOf.TM_Shadow:TorannMagicDefOf.TM_Soothe;
            var rank=shadow?TorannMagicDefOf.TM_Shadow_II:TorannMagicDefOf.TM_Soothe_II;
            var oldAura=shadow?TorannMagicDefOf.TM_Shadow_AuraHD:TorannMagicDefOf.TM_SoothingBreeze_AuraHD;
            var oldEffect=shadow?TorannMagicDefOf.Shadow_II:TorannMagicDefOf.SoothingBreeze_II;
            var oldRankDef=rank.abilityHediff;
            try
            {
                var sourceDef=new HediffDef {defName="NativeHostedAura",hediffClass=typeof(HediffWithComps)};
                var effectDef=new HediffDef {defName="NativeHostedRecipient",hediffClass=typeof(Hediff)};
                if(shadow){TorannMagicDefOf.TM_Shadow_AuraHD=sourceDef;TorannMagicDefOf.Shadow_II=effectDef;}
                else{TorannMagicDefOf.TM_SoothingBreeze_AuraHD=sourceDef;TorannMagicDefOf.SoothingBreeze_II=effectDef;}
                rank.abilityHediff=sourceDef;
                var magic=soul.GetCompAbilityUserMagic();
                var family=new MagicPower{TMabilityDefs=[root,rank],learned=true,level=1};
                if(shadow)magic.MagicData.magicPowerA=[family];else magic.MagicData.magicPowerHoF=[family];
                var ability=new MagicAbility(magic){Def=rank};magic.AbilityData.Powers.Add(ability);magic.AbilityData.AllPowers=null;
                var source=new HediffWithComps {pawn=soul,def=sourceDef};
                var anchor=new HediffComp_Aura {parent=source};
                source.comps=[anchor];soul.health.hediffSet.hediffs.Add(source);soul.health.hediffSet.DirtyCache();
                typeof(HediffComp_Aura).GetMethod("BindHost",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(anchor,[body,rank]);
                if(shadow)ArcanistTrials.MarkAura(ability);
                var ally=PawnWithComps(false,false).pawn;
                ally.def.race.body=body.def.race.body;
                Set(ally,"factionInt",body.Faction);Set(ally,"mapIndexOrState",(sbyte)c.Map.Index);
                ally.guest=new Pawn_GuestTracker(ally);
                var recipient=new Hediff{pawn=ally,def=effectDef};
                ally.health.hediffSet.hediffs.Add(recipient);ally.health.hediffSet.DirtyCache();
                Check.Equal(false,pathState.Find(id).CompletedTrials.Contains(trial),"activation alone never earns a trial");
                Set(recipient,"severityInt",1f);
                PaladinTrials.ApplyAura(source,ally,effectDef);
                Check.Equal(false,pathState.Find(id).CompletedTrials.Contains(trial),"preexisting recipient buff cannot earn the trial");
                Set(recipient,"severityInt",0f);
                PaladinTrials.ApplyAura(source,ally,effectDef);
                Check.Equal(1f,recipient.Severity,"single native recipient mutation");
                Check.Equal(true,pathState.Find(id).CompletedTrials.Contains(trial),"unspawned soul earns actual effect through its physical host");
            }
            finally
            {
                rank.abilityHediff=oldRankDef;
                if(shadow){TorannMagicDefOf.TM_Shadow_AuraHD=oldAura;TorannMagicDefOf.Shadow_II=oldEffect;}
                else{TorannMagicDefOf.TM_SoothingBreeze_AuraHD=oldAura;TorannMagicDefOf.SoothingBreeze_II=oldEffect;}
            }
        }));
        yield return new("SpiritHostedPaladin.AlternateLocalRankSuppressesSecondCommand", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var fixture = new Fixture(r,c);
            var native=TorannMagicDefOf.TM_P_RayofHope;
            var ranked=TorannMagicDefOf.TM_P_RayofHope_II;
            var oldNative=native.abilityHediff;
            var oldRanked=ranked.abilityHediff;
            var oldAura=TorannMagicDefOf.TM_RayOfHope_AuraHD;
            try
            {
                TorannMagicDefOf.TM_RayOfHope_AuraHD=native.abilityHediff=ranked.abilityHediff=new HediffDef {defName="HostedSharedAura"};
                var owner=c.Original.GetCompAbilityUserMagic();
                var ability=new MagicAbility(owner){Def=ranked};
                owner.AbilityData.Powers.Add(ability);owner.AbilityData.AllPowers=null;
                var host=c.Creator.GetCompAbilityUserMagic();
                host.AbilityData.Powers.Add(new MagicAbility(host){Def=native});host.AbilityData.AllPowers=null;
                var equivalent=Production("SpiritHostedAura").GetMethod("HasLocalEquivalent",BindingFlags.Static|BindingFlags.NonPublic)!;
                Check.Equal(true,(bool)equivalent.Invoke(null,[c.Creator,ranked])!,"other local rank supplies the same aura");
                Check.Equal(false,(bool)equivalent.Invoke(null,[c.Creator,TorannMagicDefOf.TM_HolyWrath])!,"unrelated spell still available");
            }
            finally
            {
                native.abilityHediff=oldNative;ranked.abilityHediff=oldRanked;TorannMagicDefOf.TM_RayOfHope_AuraHD=oldAura;
            }
        }));
        yield return new("SpiritHostedPaladin.DetachOnlyProjectedAuras", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var fixture = new Fixture(r, c);
            var soul = c.Original;
            var spell = TorannMagicDefOf.TM_P_RayofHope;
            var oldHediff = spell.abilityHediff;
            var oldAura = TorannMagicDefOf.TM_RayOfHope_AuraHD;
            try
            {
                var auraDef = new HediffDef { defName = "HostedRay", hediffClass = typeof(HediffWithComps) };
                TorannMagicDefOf.TM_RayOfHope_AuraHD = spell.abilityHediff = auraDef;
                var family = new MagicPower { TMabilityDefs = [spell], learned = true };
                soul.GetCompAbilityUserMagic().MagicData.magicPowerP = [family];
                var source = new HediffWithComps { pawn = soul, def = auraDef };
                var comp = new HediffComp_Aura { parent = source };
                source.comps = [comp];
                soul.health.hediffSet.hediffs.Add(source);
                soul.health.hediffSet.DirtyCache();
                Check.Same(source, soul.health.hediffSet.GetFirstHediffOfDef(auraDef), "exact saved source present");
                var end = Production("SpiritHostedAura").GetMethod("EndAuras", BindingFlags.NonPublic | BindingFlags.Static)!;
                end.Invoke(null, [soul]);
                Check.Equal(true, soul.health.hediffSet.hediffs.Contains(source), "unprojected aura left alone");
                typeof(HediffComp_Aura).GetMethod("BindHost", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(comp, [c.Creator, spell]);
                family.autocast = true;
                end.Invoke(null, [soul]);
                end.Invoke(null, [soul]);
                Check.Equal(false, family.autocast, "forced release restores prior autocast flag");
                Check.Equal(false, soul.health.hediffSet.hediffs.Contains(source), "hosted upkeep removed once");
            }
            finally
            {
                spell.abilityHediff = oldHediff;
                TorannMagicDefOf.TM_RayOfHope_AuraHD = oldAura;
            }
        }));
    }
}

