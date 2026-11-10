using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;
using Verse.AI;


namespace TorannMagic
{
    public class Verb_TechnoWeapon : ClassProgression.Verb_SpiritTargetable
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            return TargetWeapon(target) != null && base.CanHitTargetFrom(root, target);
        }

        private Thing TargetWeapon(LocalTargetInfo target)
        {
            var map = CasterPawn?.Map;
            if (map == null || map.Disposed || map.thingGrid == null || Scribe.mode != LoadSaveMode.Inactive || !target.IsValid || !target.Cell.InBounds(map)) return null;
            return target.Cell.GetThingList(map).FirstOrDefault(t => TM_Action.CanCopyTechnoWeapon(t.def));
        }

        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Thing thing = TargetWeapon(currentTarget);
            if (thing == null) return false;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(caster,Ability);
            if (!TM_Action.TryCopyTechnoWeapon(caster,owner,thing)) return false;
            
            // Equip now owns the pawn. Do not overwrite its stance with this transient casting verb.
            nonInterruptingSelfCast = true;
            this.burstShotsLeft = 0;
            return true;
        }
    }
}
