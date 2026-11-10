using RimWorld;
using System;
using Verse;
using AbilityUser;
using System.Linq;
using UnityEngine;

namespace TorannMagic
{
    class Verb_Disguise : Verb_UseAbility  
    {
        internal static float Strength(Pawn caster, CompAbilityUserMight owner, int versatility)
        {
            float severity = Mathf.RoundToInt(30 + 8f * versatility * owner.mightPwr);
            return ClassProgression.FacelessMartialPath.Path(caster) is { MartialAlternate: false } ? severity * 1.2f : severity;
        }
        internal static HediffDef Effect(int power) => power switch {
            3 => TorannMagicDefOf.TM_DisguiseHD_III, 2 => TorannMagicDefOf.TM_DisguiseHD_II,
            1 => TorannMagicDefOf.TM_DisguiseHD_I, _ => TorannMagicDefOf.TM_DisguiseHD };
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
            MightPowerSkill pwr = comp.MightData.MightPowerSkill_Disguise.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Disguise_pwr");
            MightPowerSkill ver = comp.MightData.MightPowerSkill_Disguise.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Disguise_ver");

            bool flag = caster != null && !caster.Dead;
            if (flag)
            {
                HealthUtility.AdjustSeverity(caster,Effect(pwr.level),Strength(caster,comp,ver.level));
                for(int i =0; i < 4; i++)
                {
                    FleckMaker.ThrowDustPuff(caster.Position, caster.Map, Rand.Range(.6f, 1f));
                }
            }
            return true;
        }
    }
}
