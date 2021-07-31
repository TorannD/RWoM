using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using System.Diagnostics;
using UnityEngine;
using RimWorld;
using AbilityUser;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Building_TMSmallMagicCircle : Building_TMMagicCircleBase
    {

        private float matMagnitude = 5.2f;

        public override IntVec3 GetCircleCenter
        {
            get
            {
                IntVec3 center = this.InteractionCell;
                if (this.Rotation == Rot4.North)
                {
                    center.z -= 2;
                }
                else if (this.Rotation == Rot4.South)
                {
                    center.z += 2;
                }
                else if (this.Rotation == Rot4.West)
                {
                    center.x += 2;
                }
                else
                {
                    center.x -= 2;
                }
                return center;
            }
        }

        public override float SpellSuccessModifier
        {
            get
            {
                if(Stuff != null)
                {
                    if (this.Stuff == ThingDef.Named("Jade"))
                    {
                        return .15f;
                    }
                    else if (this.Stuff == ThingDef.Named("Uranium"))
                    {
                        return .1f;
                    }      
                    else if(this.Stuff == TorannMagicDefOf.TM_Arcalleum)
                    {
                        return .1f;
                    }
                }
                return 0f;
            }
        }

        public override float MaterialCostModifier
        {
            get
            {
                if (Stuff != null)
                {
                    if (this.Stuff == ThingDef.Named("Silver"))
                    {
                        return .15f;
                    }
                    else if (this.Stuff == ThingDef.Named("Gold"))
                    {
                        return .1f;
                    }
                }
                return 0f;
            }
        }

        public override float DurationModifier
        {
            get
            {
                if (Stuff != null)
                {
                    if (this.Stuff == ThingDef.Named("Gold"))
                    {
                        return .15f;
                    }
                    else if(this.Stuff == ThingDef.Named("Uranium"))
                    {
                        return .1f;
                    }
                }
                return 0f;
            }
        }

        public override float ManaCostModifer
        {
            get
            {
                if (Stuff != null)
                {
                    if (this.Stuff == TorannMagicDefOf.TM_Arcalleum)
                    {
                        return .1f;
                    }
                }
                return 0f;
            }
        }

        public override float PointModifer
        {
            get
            {
                if (Stuff != null)
                {
                    if (this.Stuff == ThingDef.Named("Gold"))
                    {
                        return .1f;
                    }
                    else if(this.Stuff == ThingDefOf.Plasteel)
                    {
                        return .15f;
                    }
                }
                return 0f;
            }
        }

        public override void DoActiveEffecter()
        {
            Effecter CircleED = TorannMagicDefOf.TM_SmallMagicCircleED.Spawn();
            CircleED.Trigger(new TargetInfo(GetCircleCenter, this.Map, false), new TargetInfo(GetCircleCenter, this.Map, false));
            CircleED.Cleanup();
        }

        public override void Draw()
        {            
            if (this.IsActive)
            {
                Vector3 vector = base.DrawPos;
                vector.y = Altitudes.AltitudeFor(AltitudeLayer.Blueprint);
                Vector3 s = new Vector3(matMagnitude, 2*matMagnitude, matMagnitude);
                Quaternion quaternion = Quaternion.AngleAxis(Rotation.AsAngle, Vector3.up);
                Matrix4x4 matrix = default(Matrix4x4);
                float angle = this.ExactRotation;
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.smc, 0);                               
            }
            base.Draw();
            
        }

        public override IntVec3 GetMageIndexPosition(List<Pawn> allMages, Pawn mage)
        {
            IntVec3 magePosition = default(IntVec3);
            for (int i = 0; i < allMages.Count; i++)
            {
                if (allMages[i] == mage)
                {
                    IntVec3 ic = this.InteractionCell;
                    if (i == 0)
                    {
                        //always interaction cell
                    }
                    else if (i == 1)
                    {
                        if (this.Rotation == Rot4.North)
                        {
                            ic.x -= 1;
                            ic.z -= 3;
                        }
                        else if (this.Rotation == Rot4.South)
                        {
                            ic.x += 1;
                            ic.z += 3;
                        }
                        else if (this.Rotation == Rot4.West)
                        {
                            ic.x += 3;
                            ic.z -= 1;
                        }
                        else
                        {
                            ic.x -= 3;
                            ic.z += 1;
                        }
                    }                    
                    else if (i == 2)
                    {
                        if (this.Rotation == Rot4.North)
                        {
                            ic.x += 1;
                            ic.z -= 3;
                        }
                        else if (this.Rotation == Rot4.South)
                        {
                            ic.x -= 1;
                            ic.z += 3;
                        }
                        else if (this.Rotation == Rot4.West)
                        {
                            ic.x += 3;
                            ic.z += 1;
                        }
                        else
                        {
                            ic.x -= 3;
                            ic.z -= 1;
                        }
                    }
                    else
                    {
                        ic = GetRandomStaticIndexPositionFor(mage);
                    }
                    magePosition = ic;
                }
            }
            return magePosition;
        }
    }
}
