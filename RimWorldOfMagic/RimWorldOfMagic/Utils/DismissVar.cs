using Verse;

namespace TorannMagic.Utils;

public abstract class DismissVar<T> : Dismiss<T>
{
    protected T value;
    protected T offValue;
    public T Value => value;

    public DismissVar(CompAbilityUserTMBase abilityUser, TMAbilityDef abilityDef, T _offValue)
        : base(abilityUser, abilityDef)
    {
        offValue = _offValue;
    }

    public void Set(T newValue)
    {
        value = newValue;
        DismissSpellLearned = !Equals(value, offValue);
    }

    protected void Scribe()
    {
        if (Verse.Scribe.mode == LoadSaveMode.PostLoadInit && !Equals(value, offValue))
            DismissSpellLearned = true;
    }
}

// ========================== Specific versions to specify Scribe
public class DismissThing<T> : DismissVar<T> where T : Thing
{
    public DismissThing(CompAbilityUserTMBase abilityUser, TMAbilityDef abilityDef, T offValue) : base(abilityUser, abilityDef, offValue) {}

    public void Scribe(string thingName)
    {
        Scribe_References.Look<T>(ref value, thingName);
        Scribe();
    }
}

public class DismissValue<T> : DismissVar<T>
{
    public DismissValue(CompAbilityUserTMBase abilityUser, TMAbilityDef abilityDef, T offValue) : base(abilityUser, abilityDef, offValue) {}

    public void Scribe(string valueName)
    {
        Scribe_Values.Look<T>(ref value, valueName);
        Scribe();
    }
}
