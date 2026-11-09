using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;
namespace TorannMagic.ClassProgression
{
    public static class GolemancerTrials
    {
        public const string FoundationTrialId = "rwom.golemancer.foundation", MasteryTrialId = "rwom.golemancer.mastery";
        static CompAbilityUserMagic Magic(Pawn p) => p?.AllComps.OfType<CompAbilityUserMagic>().FirstOrDefault();
        static MagicPower Owner(Pawn p, bool brand) => Magic(p)?.MagicData.MagicPowersGolemancer.FirstOrDefault(x => x.learned && x.TMabilityDefs.Contains(brand ? TorannMagicDefOf.TM_Branding : TorannMagicDefOf.TM_RuneCarving));
        static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        static DisciplineProgress Path(Pawn p, bool brand, Pawn surgeon = null)
        {
            if (p == null || p.Dead || !(surgeon ?? p).Spawned || p.Faction == null || p.story?.traits?.HasTrait(TorannMagicDefOf.Faceless) == true
                || !ClassIdentity.TryGetState(p, out var state) || (state.Vocation & Vocation.Mage) == 0) return null;
            var path = state.Find("rwom.golemancer");
            return path?.Domain == DisciplineDomain.Magic && path.Stage == (brand ? InitiationStage.Adept : InitiationStage.Foundation)
                && !path.CompletedTrials.Contains(brand ? MasteryTrialId : FoundationTrialId) && Owner(p, brand) != null ? path : null;
        }
        static bool Ally(Pawn caster, Pawn target) => target != null && !target.Dead && target.Spawned && target.Map == caster.Map
            && target.Faction == caster.Faction && !target.IsPrisoner && !target.IsSlave && !TM_Calc.IsUndead(target) && target.health?.hediffSet != null;
        static bool HasRune(Pawn target, BodyPartRecord part) => target.health.hediffSet.hediffs.Any(h => h.Part == part && (h.def == TorannMagicDefOf.TM_RuneCarvedPartHD || h.def == TorannMagicDefOf.TM_ArcaneTatooPartHD));
        public sealed class RuneSnapshot
        {
            internal Pawn caster, surgeon, target;
            internal Recipe_RuneCarving worker;
            internal Bill bill;
            internal BodyPartRecord part;
            internal DisciplineProgress path;
            internal MagicPower owner;
            internal bool paid, used;
        }
        public static RuneSnapshot CaptureRune(Recipe_RuneCarving worker, Pawn caster, Pawn target, BodyPartRecord part, Bill bill)
        {
            var surgeon = caster;
            caster = GolemancerLifecycle.RuneOwner(surgeon)?.Pawn;
            var path = Path(caster, false, surgeon);
            return path != null && worker?.GetType() == typeof(Recipe_RuneCarving) && bill?.recipe != null && bill.recipe == worker.recipe && bill.recipe == DefDatabase<RecipeDef>.GetNamedSilentFail("TM_RuneCarveBodyPart")
                && bill.recipe.surgerySuccessChanceFactor < 1f && part != null && Ally(surgeon, target) && !HasRune(target, part)
                ? new RuneSnapshot { caster = caster, surgeon = surgeon, target = target, worker = worker, bill = bill, part = part, path = path, owner = Owner(caster, false) } : null;
        }
        public static void RecordRunePayment(RuneSnapshot s, float before, float after, float needed)
        {
            if (s == null || s.used || s.paid) return;
            s.paid = Finite(before) && Finite(after) && Finite(needed) && needed > 0 && before >= needed && after < before
                && System.Math.Abs((before - after) - needed) < .00001f;
        }
        public static bool CompleteRune(RuneSnapshot s)
        {
            if (s == null || s.used) return false;
            s.used = true;
            if (!s.paid || GolemancerLifecycle.RuneOwner(s.surgeon)?.Pawn != s.caster || Path(s.caster, false, s.surgeon) != s.path || Owner(s.caster, false) != s.owner || !Ally(s.surgeon, s.target)) return false;
            var def = s.part.IsCorePart ? TorannMagicDefOf.TM_ArcaneTatooPartHD : TorannMagicDefOf.TM_RuneCarvedPartHD;
            return s.target.health.hediffSet.hediffs.Any(h => h.pawn == s.target && h.Part == s.part && h.def == def && Finite(h.Severity) && h.Severity > 0)
                && s.path.RecordCompletedTrial(FoundationTrialId);
        }
        static HediffDef BrandDef(PawnAbility a)
        {
            if (a?.Def == TorannMagicDefOf.TM_AwarenessBrand) return TorannMagicDefOf.TM_AwarenessBrandHD;
            if (a?.Def == TorannMagicDefOf.TM_EmotionBrand) return TorannMagicDefOf.TM_EmotionBrandHD;
            if (a?.Def == TorannMagicDefOf.TM_FitnessBrand) return TorannMagicDefOf.TM_FitnessBrandHD;
            if (a?.Def == TorannMagicDefOf.TM_ProtectionBrand) return TorannMagicDefOf.TM_ProtectionBrandHD;
            if (a?.Def == TorannMagicDefOf.TM_VitalityBrand) return TorannMagicDefOf.TM_VitalityBrandHD;
            return null;
        }
        static MagicPower BrandOwner(PawnAbility a) => a == null ? null : GolemancerBrands.Owners(Magic(a.Pawn)?.MagicData)?.SingleOrDefault(p => p.learned && p.abilityDef == a.Def);
        static bool Runtime(PawnAbility a) => a != null && a.AbilityUser == Magic(a.Pawn) && Magic(a.Pawn)?.AbilityData.Powers.Any(p => ReferenceEquals(p, a)) == true;
        static int Pairs(CompAbilityUserMagic c, Pawn target, HediffDef def)
        {
            if (c.BrandPawns.Count != c.BrandDefs.Count) return -1;
            int count = 0; for (int i = 0; i < c.BrandPawns.Count; i++) if (c.BrandPawns[i] == target && c.BrandDefs[i] == def) count++;
            return count;
        }
        public sealed class BrandSnapshot
        {
            internal PawnAbility ability;
            internal Pawn caster, body, target;
            internal MagicPower owner, brandOwner;
            internal HediffDef def;
            internal DisciplineProgress path;
            internal bool used;
        }
        public static BrandSnapshot CaptureBrand(PawnAbility ability, Pawn target)
        {
            var caster = ability?.Pawn; var body = GolemancerLifecycle.MasterBody(caster); var path = body == null ? null : Path(caster, true, body); var def = BrandDef(ability); var owner = BrandOwner(ability);
            return path != null && def != null && owner != null && Runtime(ability) && target != body && Ally(body, target)
                && !target.health.hediffSet.HasHediff(def) && Pairs(Magic(caster), target, def) == 0
                ? new BrandSnapshot { ability = ability, caster = caster, body = body, target = target, owner = Owner(caster, true), brandOwner = owner, def = def, path = path } : null;
        }
        public static bool CompleteBrand(BrandSnapshot s)
        {
            if (s == null || s.used) return false;
            s.used = true;
            if (GolemancerLifecycle.MasterBody(s.caster) != s.body || Path(s.caster, true, s.body) != s.path || Owner(s.caster, true) != s.owner || BrandOwner(s.ability) != s.brandOwner || !Runtime(s.ability)
                || !Ally(s.body, s.target) || Pairs(Magic(s.caster), s.target, s.def) != 1) return false;
            var hd = s.target.health.hediffSet.GetFirstHediffOfDef(s.def);
            return hd?.pawn == s.target && Finite(hd.Severity) && hd.Severity > 0 && hd.TryGetComp<HediffComp_BrandingBase>()?.BranderPawn == s.caster
                && s.path.RecordCompletedTrial(MasteryTrialId);
        }
    }
}
