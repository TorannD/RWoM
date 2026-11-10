using Verse;
using System.Linq;
using UnityEngine;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritHostedMonk
    {
        internal static void AfterBurst(Verb verb, LocalTargetInfo target, int shotsLeft)
        {
            var body = verb.CasterPawn;
            var comp = BodyTraining(body)?.pawn.GetCompAbilityUserMight();
            if (comp?.Stamina == null || body.equipment?.Primary != null || shotsLeft > 0) return;
            var data = comp.MightData;
            int power = data.MightPowerSkill_TigerStrike.FirstOrDefault(x => x.label == "TM_TigerStrike_pwr")?.level ?? 0;
            int efficiency = data.MightPowerSkill_TigerStrike.FirstOrDefault(x => x.label == "TM_TigerStrike_eff")?.level ?? 0;
            int global = data.MightPowerSkill_global_seff.FirstOrDefault(x => x.label == "TM_global_seff_pwr")?.level ?? 0;
            float cost = .06f * (1 - (.1f * efficiency) * (1 - (.03f * global)));
            if (comp.Stamina.CurLevel < cost || !Rand.Chance(.3f + .05f * power)) return;
            Vector3 end = target.CenterVector3;
            end.x += Rand.Range(-.2f, .2f); end.z += Rand.Range(-.2f, .2f);
            Vector3 start = body.DrawPos;
            start.z += Rand.Range(-.2f, .2f); start.x += Rand.Range(-.2f, .2f);
            Vector3 angle = TM_Calc.GetVector(start, end);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Strike, start, body.Map, .2f, .08f, .03f, .05f,
                0, 8f, (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(), (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat());
            body.stances.SetStance(new Stance_Cooldown(5, target, verb));
            comp.Stamina.CurLevel -= cost;
            comp.MightUserXP += (int)(.06f * 180);
        }

        internal static Hediff BodyTraining(Pawn body)
        {
            var local = body?.health?.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MindOverBodyHD);
            if (local != null) return local;
            if (!SpiritRebirthState.TryGetPossessingSpirit(body, out var soul) ||
                !SpiritRebirthState.TryGet(soul, out var state) || state.DepletionPending) return null;
            return soul.health?.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_MindOverBodyHD);
        }

        internal static Hediff ProjectedTraining(Pawn body, Hediff receipt)
        {
            if (receipt == null || receipt != body.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD)) return null;
            var training = BodyTraining(body);
            return training?.pawn != body ? training : null;
        }
    }
}
