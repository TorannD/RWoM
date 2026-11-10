using System.Reflection;
using TorannMagic;
using UnityEngine;
namespace RWoM.RegressionTests;
internal static class StellarObservatoryEffectsTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("StellarObservatory.Effects.BoundedPulseAndClock", _ => {
            var type = typeof(CompAbilityUserMagic).Assembly.GetType("TorannMagic.Visuals.StellarObservatoryEffects");
            Check.Equal(true, type != null, "observatory effects implemented");
            float Pulse(int now,int start) => (float)type!.GetMethod("DiscoveryPulse")!.Invoke(null,new object[]{now,start})!;
            Check.Equal(0f,Pulse(100,-1),"never discovered");
            Check.Equal(0f,Pulse(99,100),"future timestamp");
            Check.Equal(1f,Pulse(100,100),"discovery flash");
            Check.Equal(true,Pulse(160,100)>0 && Pulse(160,100)<1,"bounded fading flash");
            Check.Equal(0f,Pulse(280,100),"expired flash");
            Check.Equal(0f,Pulse(int.MaxValue,0),"overflow safe elapsed time");
            var anchor=type!.GetMethod("CrystalAnchor")!;
            for(int i=0;i<4;i++) {
                var point=(Vector3)anchor.Invoke(null,new object[]{i,new Vector2(2.4f,2.4f)})!;
                Check.Equal(true,Math.Abs(point.x)<1.2f && Math.Abs(point.z)<1.2f,"anchors inside approved image");
            }
            var left=(Vector3)anchor.Invoke(null,new object[]{0,new Vector2(2.4f,2.4f)})!;
            var right=(Vector3)anchor.Invoke(null,new object[]{1,new Vector2(2.4f,2.4f)})!;
            Check.Equal(left.x,-right.x,"symmetric crystals");
            Check.Equal(left.z,right.z,"aligned crystal row");
        });
    }
}
