using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.ModOptions
{

    public class FactionDictionary
    {
        //public FactionStruct GetFactionSettings(FactionDef f)
        //{
        //    if(!fdic.ContainsKey(f))
        //    {
        //        fdic.Add(f, new FactionStruct(1f, 1f));
        //    }
        //    return fdic[f];
        //}

        public void SetFactionSettings(string f, float fighter, float mage)
        {
            if(Settings.Instance.FactionFighterSettings.ContainsKey(f))
            {
                Settings.Instance.FactionFighterSettings.Remove(f);
            }
            if(Settings.Instance.FactionMageSettings.ContainsKey(f))
            {
                Settings.Instance.FactionMageSettings.Remove(f);
            }
            Settings.Instance.FactionFighterSettings.Add(f, fighter);
            Settings.Instance.FactionMageSettings.Add(f, mage);
        }

        public static void InitializeFactionSettings()
        {
            IEnumerable<FactionDef> factionDefs = from def in DefDatabase<FactionDef>.AllDefs
                                                  where (!def.isPlayer && !def.hidden)
                                                  select def;

            if (Settings.Instance.FactionFighterSettings == null || (Settings.Instance.FactionFighterSettings.Count != Settings.Instance.FactionMageSettings.Count))
            {
                Settings.Instance.FactionFighterSettings = new Dictionary<string, float>();
                Settings.Instance.FactionMageSettings = new Dictionary<string, float>();
                Settings.Instance.FactionFighterSettings.Clear();
                Settings.Instance.FactionMageSettings.Clear();
                foreach (FactionDef current in factionDefs)
                {
                    if (current.defName == "Seers")
                    {
                        Settings.Instance.FactionFighterSettings.Add(current.defName, 1.5f);
                        Settings.Instance.FactionMageSettings.Add(current.defName, 2.2f);
                    }
                    else
                    {
                        Settings.Instance.FactionFighterSettings.Add(current.defName, 1f);
                        Settings.Instance.FactionMageSettings.Add(current.defName, 1f);
                    }
                }
            }
            else
            {
                Dictionary<string, float> fList = new Dictionary<string, float>();
                fList.Clear();
                Dictionary<string, float> mList = new Dictionary<string, float>();
                mList.Clear();
                foreach (FactionDef current in factionDefs)
                {
                    if (Settings.Instance.FactionFighterSettings.ContainsKey(current.defName))
                    {
                        fList.Add(current.defName, Settings.Instance.FactionFighterSettings[current.defName]);
                    }       
                    else
                    {
                        fList.Add(current.defName, 1f);
                    }
                    if (Settings.Instance.FactionMageSettings.ContainsKey(current.defName))
                    {
                        mList.Add(current.defName, Settings.Instance.FactionMageSettings[current.defName]);
                    }
                    else
                    {
                        mList.Add(current.defName, 1f);
                    }
                }
                Settings.Instance.FactionFighterSettings.Clear();
                Settings.Instance.FactionFighterSettings = fList;
                Settings.Instance.FactionMageSettings.Clear();
                Settings.Instance.FactionMageSettings = mList;
            }
        }
    }
}
