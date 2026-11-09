using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
namespace TorannMagic.Odyssey
{
    public sealed class GameComponent_EtherServants : GameComponent
    {
        private List<Pawn_EtherServant> servants = new List<Pawn_EtherServant>();
        public GameComponent_EtherServants(Game game) { }
        public void Register(Pawn_EtherServant pawn) { if (pawn != null && !servants.Contains(pawn)) servants.Add(pawn); }
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref servants, "TM_etherServants", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) servants = servants?.Where(p => p != null && !p.Destroyed).Distinct().ToList() ?? new List<Pawn_EtherServant>();
        }
        public override void GameComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (now % 250 != 0) return;
            foreach (var pawn in servants.ToArray())
            {
                if (pawn == null || pawn.Destroyed) { servants.Remove(pawn); continue; }
                // Map pawns have exact-tick expiry; this also visits suspended world owners.
                if (!pawn.Spawned) pawn.TickLifetime(now);
            }
        }
    }
}
