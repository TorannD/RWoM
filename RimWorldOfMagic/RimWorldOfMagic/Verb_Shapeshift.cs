using System.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic.ClassProgression;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public class Verb_Shapeshift : Verb_SpiritTargetable
    {
        public int verVal, pwrVal, effVal;
        protected virtual bool DireWolf => false;

        protected override bool TryCastShot()
        {
            var body = CasterPawn;
            var owner = SpiritRebirthCasting.SkillOwner(body, Ability);
            if (body?.Spawned != true || body.Dead || owner == null) return false;
            var current = body.GetComp<CompPolymorph>();
            if (current?.Original != null)
            {
                current.CancelPolymorph();
                return true;
            }
            var magic = owner.GetCompAbilityUserMagic();
            var skills = magic.MagicData.MagicPowerSkill_Shapeshift;
            verVal = skills.First(x => x.label == "TM_Shapeshift_ver").level;
            pwrVal = skills.First(x => x.label == "TM_Shapeshift_pwr").level;
            effVal = skills.First(x => x.label == "TM_Shapeshift_eff").level;
            var form = new SpawnThings { factionDef = body.Faction?.def ?? TorannMagicDefOf.TM_SummonedFaction, spawnCount = 1 };
            if (DireWolf)
            {
                form.kindDef = PawnKindDef.Named("TM_Dire_Wolf");
                form.def = form.kindDef.race;
            }
            else
            {
                var bounds = verVal >= 3 ? (200,500) : verVal == 2 ? (150,420) : verVal == 1 ? (120,320) : (80,250);
                TM_Action.AssignRandomCreatureDef(form, bounds.Item1, bounds.Item2);
            }
            int duration = Mathf.Max(1, Mathf.RoundToInt((1800 + 360 * effVal) * magic.arcaneDmg));
            var animal = TM_Action.PolymorphPawn(owner, body, body, form, body.Position, effVal < 3, duration, body.Faction);
            if (animal == null) return false;
            FleckMaker.ThrowSmoke(animal.DrawPos, animal.Map, 2);
            FleckMaker.ThrowMicroSparks(animal.DrawPos, animal.Map);
            FleckMaker.ThrowHeatGlow(animal.Position, animal.Map, 2);
            HealthUtility.AdjustSeverity(animal, HediffDef.Named("TM_ShapeshiftHD"), .5f + pwrVal);
            animal.GetComp<CompPolymorph>().EnterSelfForm();
            return true;
        }
    }
}
