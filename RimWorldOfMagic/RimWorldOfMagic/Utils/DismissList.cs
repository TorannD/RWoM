using System.Collections.Generic;
using Verse;

namespace TorannMagic.Utils;

// Holds simple abilities that add a dismiss power upon use and removes it when the list is empty
public class DismissList<T>: Dismiss<T> where T : Thing
{
    protected List<T> list;

    public DismissList(CompAbilityUserTMBase _abilityUser, TMAbilityDef _abilityDef): base(_abilityUser, _abilityDef)
    {
        list = new List<T>();
    }

    public T this[int index] => list[index];
    public int Count => list.Count;

    public void Add(T item)
    {
        list.Add(item);
        DismissSpellLearned = true;
    }
    public void RemoveAt(int index)
    {
        list.RemoveAt(index);
        if (list.Count > 0) return;
        DismissSpellLearned = false;
    }
    public void Remove(T item) => RemoveAt(list.IndexOf(item));
    public bool Contains(T item) => list.Contains(item);
    public virtual void Cleanup()
    {
        list.RemoveAll(static item => item.DestroyedOrNull());
        if (list.Count > 0) return;
        DismissSpellLearned = false;
    }
    public void Clear()
    {
        list.Clear();
        DismissSpellLearned = false;
    }
    public void DestroyAll()
    {
        for (int i = 0; i < list.Count; i++)
            list[i].Destroy();
        DismissSpellLearned = false;
    }

    public void Scribe(string listName)
    {
        Scribe_Collections.Look<T>(ref list, listName, LookMode.Reference);
        if (Verse.Scribe.mode == LoadSaveMode.PostLoadInit && list.Count > 0)
            DismissSpellLearned = true;
    }
}

// Provides Extra methods specific to DismissList<Pawn>
public class DismissPawnList<T>: DismissList<T> where T : Pawn
{
    public DismissPawnList(CompAbilityUserTMBase _abilityUser, TMAbilityDef _abilityDef) : base(_abilityUser, _abilityDef) {}

    public override void Cleanup()
    {
        list.RemoveAll(static item => item.DestroyedOrNull() || item.Dead);
        if (list.Count > 0) return;
        DismissSpellLearned = false;
    }

    public void KillAll()
    {
        for (int i = 0; i < list.Count; i++)
            list[i].Kill(null);
        DismissSpellLearned = false;
    }
}
