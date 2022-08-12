using Verse;
using RimWorld;
using TorannMagic.Golems;

namespace TorannMagic.Weapon
{
    public class Verb_BlazingPower : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
            if (comp != null && comp.IsMagicUser)
            {
                return base.TryCastShot();
            }
            else if (this.CasterPawn is TMHollowGolem)
            {
                return base.TryCastShot();
            }
            else
            {                
                MoteMaker.ThrowText(this.CasterPawn.DrawPos, this.CasterPawn.Map, "Failed", -1);                
                return false;
            }          
            
        }
    }
}
