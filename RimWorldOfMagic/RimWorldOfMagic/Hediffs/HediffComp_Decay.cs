using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_Decay : HediffComp
    {

        int tickAction = 21;
        bool shouldRemove = false;
        Pawn hediffInstigator = null;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref this.hediffInstigator, "hediffInstigator");
        }

        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }


        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned)
            {
                
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            bool flag = base.Pawn.DestroyedOrNull();
            if (!flag)
            {
                if (base.Pawn.Spawned)
                {
                    if (Find.TickManager.TicksGame % tickAction == 0 && !base.Pawn.Dead && this.parent.Part != null && Pawn.health.hediffSet.GetPartHealth(this.parent.Part) > 0)
                    {
                        severityAdjustment -= Rand.Range(.05f, .1f);
                        if(Pawn.RaceProps != null)
                        {
                            bool damageArmor = false;
                            if(Pawn.RaceProps.Humanlike && Pawn.apparel != null && Pawn.apparel.WornApparel != null && Pawn.apparel.WornApparel.Count > 0)
                            {
                                for(int i = 0; i < Pawn.apparel.WornApparel.Count; i++)
                                {
                                    Apparel p = Pawn.apparel.WornApparel[i] as Apparel;
                                    if(p != null)
                                    {                                        
                                        if(p.def.apparel.CoversBodyPart(this.parent.Part) && p.HitPoints > 0)
                                        {
                                            p.HitPoints = Mathf.Clamp(p.HitPoints - Rand.Range(30, 40), 0, p.MaxHitPoints);
                                            damageArmor = true;
                                            FleckMaker.ThrowSmoke(Pawn.DrawPos, Pawn.Map, .6f);
                                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BlackSmoke, Pawn.DrawPos, Pawn.Map, Rand.Range(.4f, .6f), 1f, .1f, 2f, Rand.Range(-20, 20), Rand.Range(.25f, .5f), 0, Rand.Range(0, 360));
                                            break;
                                        }
                                    }
                                }
                            }
                            if(!damageArmor)
                            {
                                TM_Action.DamageEntities(Pawn, this.parent.Part, 10f, TMDamageDefOf.DamageDefOf.TM_DecayDD, hediffInstigator);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BlackSmoke, Pawn.DrawPos, Pawn.Map, Rand.Range(.2f, .5f), 1f, .1f, 2f, Rand.Range(-20, 20), Rand.Range(.25f, .5f), 0, Rand.Range(0, 360));
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Disease, Pawn.DrawPos, Pawn.Map, Rand.Range(.2f, .5f), 1f, .1f, 2f, Rand.Range(-20, 20), Rand.Range(.25f, .5f), Rand.Range(0,360), Rand.Range(0, 360));
                            }                           
                        }
                    }

                    if (Pawn.health.hediffSet.GetPartHealth(this.parent.Part) <= 0)
                    {
                        this.shouldRemove = true;
                    }
                } 
            }
            base.CompPostTick(ref severityAdjustment);
        }

        public override bool CompShouldRemove => base.CompShouldRemove || shouldRemove;
    }
}
