using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.Sound;


namespace TorannMagic
{
    public class Verb_Elixir : ClassProgression.Verb_SpiritTargetable
    {
        bool validTarg;
        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt)
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ != null && targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = true;
                }
                else
                {
                    //out of range
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot()
        {
            ApplyEffect();
            bool flag = false;
            PostCastShot(flag, out flag);
            return flag;
        }

        protected override bool ApplyHostedEffect() => ApplyEffect();

        private bool ApplyEffect()
        {
            if (CasterPawn?.Spawned != true || currentTarget.Pawn?.Spawned != true ||
                currentTarget.Pawn.Map != CasterPawn.Map || currentTarget.Pawn.Dead) return false;
            Map map = this.CasterPawn.Map;

            Pawn hitPawn = this.currentTarget.Thing as Pawn;
            Pawn caster = this.CasterPawn;
            Pawn source = Ability?.Pawn ?? caster;

            int verVal = TM_Calc.GetSkillVersatilityLevel(source, TorannMagicDefOf.TM_Elixir, false);
            int pwrVal = TM_Calc.GetSkillPowerLevel(source, TorannMagicDefOf.TM_Elixir, false);

            try
            {
                if (!hitPawn.DestroyedOrNull() && !hitPawn.Dead && hitPawn.Spawned && map != null && hitPawn.health != null && hitPawn.health.hediffSet != null)
                {
                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_HerbalElixirHD, .7f + (.1f * pwrVal));
                    HediffComp_HerbalElixir hdhe = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HerbalElixirHD, false).TryGetComp<HediffComp_HerbalElixir>();
                    if(hdhe != null)
                    {
                        hdhe.pwrVal = pwrVal;
                        hdhe.verVal = verVal;
                    }
                    SoundInfo info = SoundInfo.InMap(new TargetInfo(caster.Position, caster.Map, false), MaintenanceType.None);
                    TorannMagicDefOf.TM_Powerup?.PlayOneShot(info);
                    return true;
                }
                else
                {
                    Messages.Message("TM_InvalidTarget".Translate(caster.LabelShort, this.Ability.Def.label), MessageTypeDefOf.NeutralEvent);
                }
            }
            catch (NullReferenceException ex)
            {
                Compatibility.TM_Log.Error("Herbal Elixir failed: " + ex);
            }
            return false;
        }
    }
}
