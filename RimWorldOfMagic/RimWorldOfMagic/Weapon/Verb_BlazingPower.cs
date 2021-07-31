using Verse;
using RimWorld;

namespace TorannMagic.Weapon
{
    public class Verb_BlazingPower : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            if(this.CasterPawn.GetComp<CompAbilityUserMagic>().IsMagicUser)
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
