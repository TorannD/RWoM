using System.Reflection;
using TorannMagic;
using Verse;
using AbilityUser;
using RimWorld;
using System.Runtime.CompilerServices;
namespace RWoM.RegressionTests;
internal static class CombatLogTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("CombatLog.KillPrefixRegisteredOnClass", _ =>
        {
            var type = typeof(TorannMagicMod.Undead_Kill_Prefix);
            var patch = type.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "HarmonyPatch");
            Check.NotNull(patch, "Class-only scanner must discover Pawn.Kill prefix");
            Check.Equal(typeof(Pawn), patch!.ConstructorArguments[0].Value, "Kill declaring type");
            Check.Equal("Kill", patch.ConstructorArguments[1].Value as string, "Kill method");
            Check.Equal(false, typeof(TorannMagicMod).GetMethod("TryReleaseFaction", BindingFlags.Static | BindingFlags.NonPublic)!.CustomAttributes.Any(a => a.AttributeType.Name == "HarmonyPatch"), "Helper has no patch annotation");
        });
        yield return new("CombatLog.SuccessBoundaryRegistered", _ =>
        {
            var types = typeof(TorannMagicMod).GetNestedTypes();
            Check.Equal(false, types.Any(t => t.Name == "AbilityUseCombatLog_Patch"), "Request-time hook removed");
            foreach (var name in new[] { "AbilityActivationCombatLog_Patch", "TLineActivationCombatLog_Patch", "AbilitySuccessCombatLog_Patch" })
                Check.NotNull(types.FirstOrDefault(t => t.Name == name)?.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "HarmonyPatch"), name + " registered");
        });
        yield return new("CombatLog.InstalledExecutionHookSignaturesAndIL", _ =>
        {
            foreach (var contract in new[] {
                ("AbilityActivationCombatLog_Patch", typeof(Verb_UseAbility), "WarmupComplete", "Prefix", typeof(Verb_UseAbility)),
                ("TLineActivationCombatLog_Patch", typeof(Verb_ShootTLine), "WarmupComplete", "Prefix", typeof(Verb_ShootTLine)),
                ("AbilitySuccessCombatLog_Patch", typeof(Pawn), "Notify_UsedVerb", "Postfix", typeof(Verb)) })
            {
                var type = typeof(TorannMagicMod).GetNestedType(contract.Item1)!;
                var attribute = type.CustomAttributes.Single(a => a.AttributeType.Name == "HarmonyPatch");
                Check.Equal(contract.Item2, attribute.ConstructorArguments[0].Value, "Hook declaring type");
                Check.Equal(contract.Item3, attribute.ConstructorArguments[1].Value as string, "Hook method");
                Check.NotNull(type.GetMethod(contract.Item4)!.GetParameters().FirstOrDefault(p => p.ParameterType == contract.Item5), "Hook receives correct runtime type");
                Check.NotNull(contract.Item2.GetMethod(contract.Item3, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), "Installed declared method exists");
            }
            var nextShot = typeof(Verb).GetMethod("TryCastNextBurstShot", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var engineIL = Instructions(nextShot).ToArray();
            int available = Array.FindIndex(engineIL, line => line.Contains("callvirt Boolean Available()"));
            int shot = Array.FindIndex(engineIL, line => line.Contains("callvirt Boolean TryCastShot()"));
            int notify = Array.FindIndex(engineIL, line => line.Contains("callvirt Void Notify_UsedVerb(Verse.Pawn, Verse.Verb)"));
            Check.Equal(true, available >= 0 && shot > available && notify > shot, "Installed success calls occur in expected order");
            Check.Equal(true, engineIL[available + 1].Contains("brfalse") && engineIL[shot + 1].Contains("brfalse"), "Both failures branch away before notification");
            string failedTarget = engineIL[shot + 1].Split(' ').Last();
            Check.Equal(true, int.Parse(failedTarget, System.Globalization.NumberStyles.HexNumber) > int.Parse(engineIL[notify].Split(' ')[0], System.Globalization.NumberStyles.HexNumber), "Failed shot skips successful notification");
            Console.WriteLine("ENGINE SUCCESS IL: " + string.Join("; ", engineIL));
            Console.WriteLine("JECS WARMUP IL: " + string.Join("; ", Instructions(typeof(Verb_UseAbility).GetMethod("WarmupComplete")!)));
            Console.WriteLine("TLINE WARMUP IL: " + string.Join("; ", Instructions(typeof(Verb_ShootTLine).GetMethod("WarmupComplete")!)));
        });
        yield return new("CombatLog.RealWarmupPrefixesCaptureAbilityAndTarget", f =>
        {
            foreach (var verbType in new[] { typeof(Verb_UseAbility), typeof(Verb_ShootTLine) })
            {
                var caster = new Pawn(); var target = new Pawn();
                var def = f.AddAbility("TM_RealPrefix" + verbType.Name);
                def.MainVerb.verbClass = verbType;
                var ability = new PawnAbility { AbilityUser = new CompAbilityUserMagic { parent = caster }, Def = def };
                var verb = (Verb_UseAbility)ability.Verb;
                typeof(Verb).GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(verb, new LocalTargetInfo(target));
                if (verb is Verb_ShootTLine line) TorannMagicMod.TLineActivationCombatLog_Patch.Prefix(line);
                else TorannMagicMod.AbilityActivationCombatLog_Patch.Prefix(verb);
                typeof(Verb).GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(verb, LocalTargetInfo.Invalid);
                var entry = Check.NotNull(Consume(verb, caster), "Warmup prefix captured activation");
                Check.Equal(true, EntryReferences(entry, target), "Warmup target retained after target cleared");
                Check.Equal(true, EntryReferences(entry, caster), "Warmup caster retained");
                Check.Same(def, typeof(BattleLogEntry_TMAbilityUsed).GetField("abilityUsed", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(entry), "Actual ability definition captured");
            }
        });
        yield return new("CombatLog.SuccessConsumesActivationOnceAndRearms", f =>
        {
            var verb = new Verb_UseAbility();
            var caster = new Pawn(); var target = new Pawn();
            var ability = f.AddAbility("TM_RegressionCast");
            Capture(verb, caster, target, ability);
            Check.Equal(true, Consume(verb, new Pawn()) == null, "Other pawn notification cannot consume activation");
            Check.Equal(true, Consume(new Verb_UseAbility(), caster) == null, "Other verb cannot consume activation");
            // Capturing a warmup is not emitting an event. Only the engine-success
            // consumer is allowed to turn it into a log entry.
            // Retargeting after effects must not replace the captured participants.
            typeof(Verb).GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(verb, new LocalTargetInfo(new Pawn()));
            var entry = Check.NotNull(Consume(verb, caster), "Successful eligible activation");
            Check.Equal(true, EntryReferences(entry, target), "Original target survives retargeting");
            Check.Equal(true, EntryReferences(entry, caster), "Original caster retained");
            Check.Same(ability, typeof(BattleLogEntry_TMAbilityUsed).GetField("abilityUsed", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(entry), "Captured ability");
            Check.Equal(true, Consume(verb, caster) == null, "Later burst/projectile success cannot duplicate activation");
            Capture(verb, caster, target, ability);
            Check.NotNull(Consume(verb, caster), "Reused verb starts another activation");
        });
        yield return new("CombatLog.RejectedCancelledAndResumedBurstHaveNoEvent", f =>
        {
            var verb = new Verb_UseAbility();
            var caster = new Pawn();
            Check.Equal(true, Consume(verb, caster) == null, "Rejected job or resumed burst has no captured activation");
            Capture(verb, caster, new Pawn(), f.AddAbility("TM_Cancelled"));
            // Cancellation does not call Consume: the next warmup replaces state.
            var next = f.AddAbility("TM_NextCast");
            Capture(verb, caster, new Pawn(), next);
            var entry = Check.NotNull(Consume(verb, caster), "Next successful activation");
            Check.Same(next, typeof(BattleLogEntry_TMAbilityUsed).GetField("abilityUsed", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(entry), "Cancelled target is replaced");
        });
        yield return new("CombatLog.FiltersAndCaptureBeforeDespawn", f =>
        {
            var verb = new Verb_UseAbility();
            var caster = new Pawn(); var target = new Pawn();
            var def = f.AddAbility("TM_Filtered");
            Capture(verb, caster, caster, def);
            Check.Equal(true, Consume(verb, caster) == null, "Self cast excluded");
            Capture(verb, caster, null, def);
            Check.Equal(true, Consume(verb, caster) == null, "Cell-only or nonpawn cast excluded");
            var faction = new Faction();
            typeof(Thing).GetField("factionInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(caster, faction);
            typeof(Thing).GetField("factionInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(target, faction);
            def.MainVerb.isViolent = false;
            Capture(verb, caster, target, def);
            Check.Equal(true, Consume(verb, caster) == null, "Friendly nonviolent support excluded");
            def.MainVerb.isViolent = true;
            Capture(verb, caster, target, def);
            // These fixtures are unspawned. A successful event must not depend on
            // Spawned/Map after effects may have killed or despawned a participant.
            Check.NotNull(Consume(verb, caster), "Offensive same-faction cast and unspawned captured pawns retained");
            typeof(Thing).GetField("factionInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(target, new Faction());
            def.MainVerb.isViolent = false;
            Capture(verb, caster, target, def);
            Check.NotNull(Consume(verb, caster), "Nonviolent hostile cast retained");
            Capture(verb, caster, target, def);
            Capture(verb, caster, null, def);
            Check.Equal(true, Consume(verb, caster) == null, "Ineligible next activation clears previous target");
        });
    }
    private static bool EntryReferences(object entry, Pawn pawn)
    {
        for (Type? type = entry.GetType(); type != null; type = type.BaseType)
            if (type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Any(field => ReferenceEquals(field.GetValue(entry), pawn))) return true;
        return false;
    }
    private static IEnumerable<string> Instructions(MethodInfo method)
    {
        var ops = typeof(System.Reflection.Emit.OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(System.Reflection.Emit.OpCode)).Select(f => (System.Reflection.Emit.OpCode)f.GetValue(null)!).ToDictionary(o => unchecked((ushort)o.Value));
        var bytes = method.GetMethodBody()!.GetILAsByteArray()!;
        for (int i = 0; i < bytes.Length;)
        {
            int offset = i;
            ushort value = bytes[i++];
            if (value == 0xfe) value = (ushort)(0xfe00 | bytes[i++]);
            var op = ops[value];
            int size = op.OperandType switch {
                System.Reflection.Emit.OperandType.InlineNone => 0,
                System.Reflection.Emit.OperandType.ShortInlineBrTarget or System.Reflection.Emit.OperandType.ShortInlineI or System.Reflection.Emit.OperandType.ShortInlineVar => 1,
                System.Reflection.Emit.OperandType.InlineVar => 2,
                System.Reflection.Emit.OperandType.InlineI8 or System.Reflection.Emit.OperandType.InlineR => 8,
                System.Reflection.Emit.OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(bytes, i),
                _ => 4 };
            string operand = size == 0 ? "" : Convert.ToHexString(bytes.AsSpan(i, size));
            if (op.OperandType is System.Reflection.Emit.OperandType.InlineMethod or System.Reflection.Emit.OperandType.InlineField)
                operand = method.Module.ResolveMember(BitConverter.ToInt32(bytes, i))!.ToString()!;
            if (op.OperandType == System.Reflection.Emit.OperandType.ShortInlineBrTarget) operand = (i + 1 + (sbyte)bytes[i]).ToString("X4");
            if (op.OperandType == System.Reflection.Emit.OperandType.InlineBrTarget) operand = (i + 4 + BitConverter.ToInt32(bytes, i)).ToString("X4");
            yield return $"{offset:X4} {op.Name} {operand}";
            i += size;
        }
    }
    private static Type State => typeof(TorannMagicMod).GetNestedType("AbilityCombatLogActivation", BindingFlags.NonPublic)!;
    private static void Capture(Verb verb, Pawn caster, Pawn? target, TMAbilityDef ability) =>
        State.GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Verb), typeof(Pawn), typeof(Pawn), typeof(TMAbilityDef) }, null)!.Invoke(null, new object?[] { verb, caster, target, ability });
    private static BattleLogEntry_TMAbilityUsed? Consume(Verb verb, Pawn caster)
    {
        if (Current.Game == null)
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            typeof(Game).GetField("tickManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(Current.Game, new TickManager());
            typeof(Game).GetField("uniqueIDsManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(Current.Game, new UniqueIDsManager());
        }
        typeof(TickManager).GetField("gameStartAbsTick", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(Find.TickManager, 1);
        try { return (BattleLogEntry_TMAbilityUsed?)State.GetMethod("Consume", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { verb, caster }); }
        catch (TargetInvocationException ex) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException!).Throw(); throw; }
    }
}
