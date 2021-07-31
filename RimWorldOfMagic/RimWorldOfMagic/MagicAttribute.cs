using AbilityUser;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    public class MagicAttribute : IExposable
    {
        public  int level = 0;

        public List<AbilityDef> TMattributeDefs;

        public AbilityDef attributeDef
        {
            get
            {
                AbilityDef result = null;
                bool flag = this.TMattributeDefs != null && this.TMattributeDefs.Count > 0;
                if (flag)
                {
                    result = this.TMattributeDefs[0];
                    int num = this.level;
                    bool flag2 = num > -1 && num < this.TMattributeDefs.Count;
                    if (flag2)
                    {
                        result = this.TMattributeDefs[num];
                    }
                    else
                    {
                        bool flag3 = num >= this.TMattributeDefs.Count;
                        if (flag3)
                        {
                            result = this.TMattributeDefs[this.TMattributeDefs.Count - 1];
                        }
                    }
                }
                return result;
            }
        }

        public AbilityDef nextLevelAttributeDef
        {
            get
            {
                AbilityDef result = null;
                bool flag = this.attributeDef != null && this.TMattributeDefs.Count > 0;
                if (flag)
                {
                    result = this.TMattributeDefs[0];
                    int num = this.level;
                    bool flag2 = num > -1 && num <= this.TMattributeDefs.Count;
                    if (flag2)
                    {
                        result = this.TMattributeDefs[num];
                    }
                    else
                    {
                        bool flag3 = num >= this.TMattributeDefs.Count;
                        if (flag3)
                        {
                            result = this.TMattributeDefs[this.TMattributeDefs.Count - 1];
                        }
                    }
                }
                return result;
            }
        }

        public AbilityDef GetAttributeDef(int index)
        {
            AbilityDef result = null;
            bool flag = this.TMattributeDefs != null && this.TMattributeDefs.Count > 0;
            if (flag)
            {
                result = this.TMattributeDefs[0];
                bool flag2 = index > -1 && index < this.TMattributeDefs.Count;
                if (flag2)
                {
                    result = this.TMattributeDefs[index];
                }
                else
                {
                    bool flag3 = index >= this.TMattributeDefs.Count;
                    if (flag3)
                    {
                        result = this.TMattributeDefs[this.TMattributeDefs.Count - 1];
                    }
                }
            }
            return result;
        }

        public MagicAttribute()
        {
        }

        public MagicAttribute(List<AbilityDef> newAttributeDefs)
        {
            this.TMattributeDefs = newAttributeDefs;
        }

        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.level, "level", 0, false);
            Scribe_Collections.Look<AbilityDef>(ref this.TMattributeDefs, "TMattributeDefs", LookMode.Def, null);
        }
    }
}
