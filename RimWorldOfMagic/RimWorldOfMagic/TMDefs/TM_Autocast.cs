using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace TorannMagic.TMDefs
{
    public class TM_Autocast : IExposable 
    {
        public AutocastType type = AutocastType.Null;

        public bool mightUser = false;
        public bool magicUser = false;
        public bool drafted = false;
        public bool undrafted = false;
        public float minRange = 0f;
        public float maxRange = 0f;

        //targetting modifiers
        public bool targetFriendly = false;
        public bool targetNeutral = true;
        public bool targetEnemy = false;
        public bool targetNoFaction = true;
        public bool includeSelf = false;
        public bool requiresLoS = true;
        public bool AIUsable = false;

        public List<string> advancedConditionDefs = null;
        private List<TM_AutocastCondition> accList = null;
        public bool ValidConditions(Pawn caster, LocalTargetInfo target)
        {
            if (advancedConditionDefs != null)
            {
                if (accList == null)
                {
                    accList = new List<TM_AutocastCondition>();
                    accList.Clear();
                    IEnumerable<TM_AutocastConditionDef> acdList = from def in DefDatabase<TM_AutocastConditionDef>.AllDefs
                                                            where (true)
                                                            select def;

                    foreach (string str in advancedConditionDefs)
                    {
                        foreach (TM_AutocastConditionDef acd in acdList)
                        {
                            if (acd != null && acd.defName == str)
                            {
                                accList.Add(acd.autocastCondition);
                            }
                        }
                    }
                }
                if (accList != null && accList.Count > 0)
                {
                    bool meetsConditions = false;
                    foreach (TM_AutocastCondition acc in accList)
                    {
                        //Log.Message("validating " + acc.conditionClass);
                        if (acc.conditionClass == AutocastConditionClass.DamageTaken)
                        {
                            if (acc.onlyAppliesToCaster)
                            {
                                meetsConditions = DamageTaken(acc, caster);
                            }
                            else
                            {
                                meetsConditions = DamageTaken(acc, target.Thing);
                            }
                        }
                        else if(acc.conditionClass == AutocastConditionClass.HasNeed)
                        {
                            if (acc.onlyAppliesToCaster)
                            {
                                meetsConditions = HasNeed(acc, caster);
                            }
                            else
                            {
                                meetsConditions = HasNeed(acc, target.Pawn);
                            }
                        }
                        else if(acc.conditionClass == AutocastConditionClass.HasHediff)
                        {
                            if (acc.onlyAppliesToCaster)
                            {
                                meetsConditions = HasHediff(acc, caster);
                            }
                            else
                            {
                                meetsConditions = HasHediff(acc, target.Pawn);
                            }
                        }
                        else if(acc.conditionClass == AutocastConditionClass.EnemiesInRange)
                        {
                            meetsConditions = EnemiesInRange(acc, caster, target.Cell);
                        }
                        else if(acc.conditionClass == AutocastConditionClass.AlliesInRange)
                        {
                            meetsConditions = AlliesInRange(acc, caster, target.Cell);
                        }
                        else if(acc.conditionClass == AutocastConditionClass.TargetDrafted)
                        {
                            if (acc.onlyAppliesToCaster)
                            {
                                meetsConditions = TargetDrafted(acc, caster);
                            }
                            else
                            {
                                meetsConditions = TargetDrafted(acc, target.Pawn);
                            }
                        }

                        if(!meetsConditions)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;           
        }

        private string targetType = "";
        public Type GetTargetType
        {
            get
            {
                if(targetType == "Pawn")
                {
                    return typeof(Pawn);
                }
                else if(targetType == "ThingWithComps")
                {
                    return typeof(ThingWithComps);
                }
                else if(targetType == "Building")
                {
                    return typeof(Building);
                }
                else if(targetType == "Corpse")
                {
                    return typeof(Corpse);
                }
                else if(targetType == "LocalTargetInfo")
                {
                    return typeof(LocalTargetInfo);
                }
                else
                {
                    return typeof(Thing);
                }
            }
        }

        public bool ValidType(Type targetType, LocalTargetInfo target)
        {
            if (target.Thing != null)
            {
                if (targetType == typeof(Pawn))
                {
                    if (target.Thing is Pawn)
                    {
                        return true;
                    }
                }
                else if (targetType == typeof(ThingWithComps))
                {
                    if (target.Thing is ThingWithComps)
                    {
                        return true;
                    }
                }
                else if (targetType == typeof(Building))
                {
                    if (target.Thing is Building)
                    {
                        return true;
                    }
                }
                else if (targetType == typeof(Corpse))
                {
                    if (target.Thing is Corpse)
                    {
                        return true;
                    }
                }
                else if(target.Thing != null)
                {
                    return true;
                }
                return false;
            }
            if (targetType == typeof(LocalTargetInfo))
            {
                return true;
            }
            return false;
        }

        public void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.mightUser, "mightUser", false);
            Scribe_Values.Look<bool>(ref this.magicUser, "magicUser", false);
            Scribe_Values.Look<bool>(ref this.drafted, "drafted", false);
            Scribe_Values.Look<bool>(ref this.undrafted, "undrafted", false);
            Scribe_Values.Look<float>(ref this.minRange, "minRange", 0f);
            Scribe_Values.Look<float>(ref this.maxRange, "maxRange", 0f);

            Scribe_Values.Look<bool>(ref this.targetFriendly, "targetFriendly", false);
            Scribe_Values.Look<bool>(ref this.targetNeutral, "targetNeutral", false);
            Scribe_Values.Look<bool>(ref this.targetEnemy, "targetEnemy", false);
            Scribe_Values.Look<bool>(ref this.includeSelf, "includeSelf", false);
            Scribe_Values.Look<bool>(ref this.requiresLoS, "requiresLoS", false);
            Scribe_Values.Look<bool>(ref this.AIUsable, "AIUsable", false);

            Scribe_Values.Look<AutocastType>(ref this.type, "type", AutocastType.Null);
            Scribe_Values.Look<string>(ref this.targetType, "targetType", "");
            Scribe_Collections.Look<string>(ref this.advancedConditionDefs, "advancedConditionDefs", LookMode.Value);
        }

        //Advanced condition cases
        private bool DamageTaken(TM_AutocastCondition con, Thing t)
        {
            if (t is Pawn p)
            {
                if (p != null && p.health != null && p.health.hediffSet != null)
                {
                    //Log.Message("pawn injured? " + TM_Calc.IsPawnInjured(p, con.valueA) + " invert is " + con.invert);
                    return (con.invert ? !TM_Calc.IsPawnInjured(p, con.valueA) : TM_Calc.IsPawnInjured(p, con.valueA));
                }
            }
            return false;
        }

        private bool HasHediff(TM_AutocastCondition con, Pawn p)
        {            
            if (p != null && p.health != null && p.health.hediffSet != null)
            {
                bool hasAnyHediff = false;
                foreach(HediffDef hdd in con.hediffDefs)
                {
                    Hediff hd = p.health.hediffSet.GetFirstHediffOfDef(hdd);
                    if(hd != null)
                    {                        
                        if (con.valueA != 0)
                        {
                            if (hd.Severity >= con.valueA)
                            {
                                if (con.valueB != 0)
                                {
                                    if (hd.Severity <= con.valueB)
                                    {
                                        hasAnyHediff = true;
                                    }
                                    else
                                    {
                                        hasAnyHediff = false;
                                    }
                                }
                                else
                                {
                                    hasAnyHediff = true;
                                }
                            }
                            else
                            {
                                hasAnyHediff = false;
                            }
                        }
                        else if (con.valueB != 0)
                        {
                            if (hd.Severity <= con.valueB)
                            {
                                hasAnyHediff = true;
                            }
                            else
                            {
                                hasAnyHediff = false;
                            }
                        }
                        else
                        {
                            hasAnyHediff = true;
                        }
                    }
                }                
                return (con.invert ? !hasAnyHediff : hasAnyHediff);
            }
            return false;
        }

        private bool HasNeed(TM_AutocastCondition con, Pawn p)
        {            
            if (p != null && p.needs != null)
            {
                bool hasAnyNeed = false;
                foreach(Need n in p.needs.AllNeeds)
                {
                    if(n != null && con.needDefs.Contains(n.def))
                    {                        
                        if (con.valueA != 0)
                        {                           
                            if (n.CurLevel >= con.valueA)
                            {
                                if (con.valueB != 0)
                                {
                                    if (n.CurLevel <= con.valueB)
                                    {
                                        hasAnyNeed = true;
                                    }
                                    else
                                    {
                                        hasAnyNeed = false;
                                    }
                                }
                                else
                                {
                                    hasAnyNeed = true;
                                }
                            }
                            else
                            {
                                hasAnyNeed = false;
                            }
                        }
                        else if (con.valueB != 0)
                        {                           
                            if (n.CurLevel <= con.valueB)
                            {
                                hasAnyNeed = true;
                            }
                            else
                            {
                                hasAnyNeed = false;
                            }
                        }
                        else
                        {
                            hasAnyNeed = true;
                        }
                    }
                }                
                return (con.invert ? !hasAnyNeed : hasAnyNeed);
            }
            return false;
        }

        private bool EnemiesInRange(TM_AutocastCondition con, Pawn caster, IntVec3 cell)
        {
            List<Pawn> enemies = TM_Calc.FindPawnsNearTarget(caster, (int)con.valueB, cell, true);
            if (enemies != null)
            {               
                return (con.invert ? enemies.Count < con.valueA : enemies.Count >= con.valueA);
            }
            return false;
        }

        private bool AlliesInRange(TM_AutocastCondition con, Pawn caster, IntVec3 cell)
        {
            List<Pawn> allies = TM_Calc.FindPawnsNearTarget(caster, (int)con.valueB, cell, false);            
            if (allies != null)
            {
                allies.Add(caster);
                return (con.invert ? allies.Count <= con.valueA : allies.Count > con.valueA);
            }
            return false;
        }

        private bool TargetDrafted(TM_AutocastCondition con, Pawn p)
        {
            if(p.drafter != null && p.Drafted)
            {
                return (con.invert ? !p.Drafted : p.Drafted);
            }
            return false;
        }
    }    

    public enum AutocastType
    {
        OnTarget,       //Selects only the target of the job
        OnCell,         //???
        OnNearby,       //Tries to find a target between min and max range meeting critera
        OnSelf,         //always selects self
        Null
    }

    public enum AutocastConditionClass
    {
        DamageTaken,
        HasHediff,
        HasNeed,
        EnemiesInRange,
        AlliesInRange,
        TargetDrafted,
        Null
    }
}
