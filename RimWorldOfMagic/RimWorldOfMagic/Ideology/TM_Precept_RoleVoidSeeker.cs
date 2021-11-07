using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace TorannMagic.Ideology
{
    public class TM_Precept_RoleVoidSeeker : Precept_RoleMulti
    {
        public override void Assign(Pawn p, bool addThoughts)
        {
            List<Pawn> unassignPawns = new List<Pawn>();
            unassignPawns.Clear();
            foreach(IdeoRoleInstance iri in chosenPawns)
            {
                if(iri.pawn != p && iri.sourceRole.def == TorannMagicDefOf.TM_IdeoRole_VoidSeeker)
                {
                    unassignPawns.Add(iri.pawn);
                }
            }
            if(unassignPawns != null && unassignPawns.Count > 0)
            {
                for(int i = 0; i < unassignPawns.Count; i++)
                {
                    Unassign(unassignPawns[i], true);
                }
            }
            base.Assign(p, addThoughts);
        }
    }
}
