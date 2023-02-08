using Verse;

namespace TorannMagic.Utils;

public abstract class DismissVar<T> : Dismiss<T>
{
    public T Value;

    public DismissVar(CompAbilityUserTMBase abilityUser, TMAbilityDef abilityDef) : base(abilityUser, abilityDef) {}

    public void Set(T newValue)
    {
        Value = newValue;
        DismissSpellLearned = !Equals(Value, default);
    }

    protected void Scribe()
    {
        if (Verse.Scribe.mode == LoadSaveMode.PostLoadInit && !Equals(Value, default))
            DismissSpellLearned = true;
    }
}

// ========================== Specific versions to specify Scribe
public class DismissThing<T> : DismissVar<T> where T : Thing
{
    public DismissThing(CompAbilityUserTMBase abilityUser, TMAbilityDef abilityDef) : base(abilityUser, abilityDef) {}

    public void Scribe(string thingName)
    {
        Scribe_References.Look<T>(ref Value, thingName);
        Scribe();
    }
}

public class DismissValue<T> : DismissVar<T>
{
    public DismissValue(CompAbilityUserTMBase abilityUser, TMAbilityDef abilityDef) : base(abilityUser, abilityDef) {}

    public void Scribe(string valueName)
    {
        Scribe_Values.Look<T>(ref Value, valueName);
        Scribe();
    }
}
