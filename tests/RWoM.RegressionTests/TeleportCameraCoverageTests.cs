using System.Reflection;
using System.Reflection.Emit;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

internal static class TeleportCameraCoverageTests
{
    public static IEnumerable<TestCase> Cases()
    {
        foreach (string typeName in new[] {
            "AutoCast.TM_WorkFlight",
            "FlyingObject_DemonFlight", "FlyingObject_Leap", "Projectile_LightSkip", "Projectile_LightSkipMass",
            "FlyingObject_DragonStrike", "FlyingObject_Flight", "FlyingObject_PsionicDash",
            "FlyingObject_PsionicLeap", "FlyingObject_PsionicStorm", "FlyingObject_ValiantCharge",
            "FlyingObject_Whirlwind", "Projectile_ShadowStrike", "Verb_ShadowStep",
            "Verb_ShadowWalk", "Verb_Transpose" })
        {
            yield return new("TeleportCamera.NoPawnCameraCalls." + typeName, _ =>
            {
                var methods = ProductionType(typeName).GetMethods(DeclaredMethods);
                Check.NotEqual(0, methods.Length, typeName + " declares gameplay methods");
                foreach (var method in methods)
                    AssertNoPawnCameraCalls(method);
            });
        }

        foreach (string methodName in new[] { "RecallPosition", "DoWildSurge" })
        {
            yield return new("TeleportCamera.NoPawnCameraCalls.TM_Action." + methodName, _ =>
                AssertNoPawnCameraCalls(ProductionMethod("TM_Action", methodName)));
        }

        // Explicit world/destination selection is not a gameplay pawn-camera jump.
        foreach (string typeName in new[] { "JobDriver_PortalDestination", "Verb_FoldReality", "Verb_LightSkipGlobal" })
        {
            yield return new("TeleportCamera.WorldDestinationSelectionRetained." + typeName, _ =>
            {
                var calls = CalledMethods(ProductionMethod(typeName, "StartChoosingDestination")).ToArray();
                foreach (string name in new[] { "TryJump", "GetWorldTarget" })
                    Check.Equal(true, calls.Any(call => call.DeclaringType == typeof(CameraJumper) && call.Name == name),
                        typeName + " retains CameraJumper." + name);
            });
        }
    }

    private const BindingFlags DeclaredMethods = BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static Type ProductionType(string name) => typeof(TM_Action).Assembly.GetType("TorannMagic." + name, true)!;

    private static MethodInfo ProductionMethod(string typeName, string methodName) =>
        Check.NotNull(ProductionType(typeName).GetMethod(methodName, DeclaredMethods), typeName + "." + methodName + " exists");

    private static void AssertNoPawnCameraCalls(MethodInfo method)
    {
        foreach (var call in CalledMethods(method))
        {
            bool forbidden = (call.DeclaringType == typeof(CameraJumper) && call.Name == "TryJumpAndSelect") ||
                call.Name == "SelectNextColonist";
            Check.Equal(false, forbidden, method.DeclaringType!.Name + "." + method.Name +
                " must not call " + call.DeclaringType?.Name + "." + call.Name);
        }
    }

    private static readonly Dictionary<ushort, OpCode> Codes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(code => unchecked((ushort)code.Value));

    private static IEnumerable<MethodBase> CalledMethods(MethodInfo method)
    {
        byte[] il = Check.NotNull(method.GetMethodBody()?.GetILAsByteArray(), method.Name + " has IL");
        for (int index = 0; index < il.Length;)
        {
            ushort value = il[index++];
            if (value == 0xfe) value = (ushort)(0xfe00 | il[index++]);
            var operand = Codes[value].OperandType;
            if (operand == OperandType.InlineMethod)
                yield return Check.NotNull(method.Module.ResolveMethod(BitConverter.ToInt32(il, index),
                    method.DeclaringType?.GetGenericArguments(), method.GetGenericArguments()), "Resolved method reference");
            index += operand switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, index),
                _ => 4
            };
        }
    }
}
