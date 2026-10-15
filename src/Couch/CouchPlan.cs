using System.Collections.Generic;

namespace EveryonePicks.Couch
{
    /// <summary>
    /// Decides which queued local picks the couch board takes and which stay with the sequential
    /// native runner. Pure, so the queue rules are unit-tested without the game.
    /// </summary>
    internal static class CouchPlan
    {
        /// <summary>
        /// Splits a snapshot of the local queue. The board takes the first queued entry of each
        /// distinct player whose picker is a real player (<paramref name="isPlayerPicker"/>), in
        /// queue order, up to <see cref="CouchLayout.MaxLanes"/>. Everything else — a player's second
        /// entry, non-player pickers, overflow — stays in <paramref name="rest"/> in its original order.
        /// Returns false (board empty, rest = whole queue) when couch mode is off or fewer than two
        /// distinct players qualify: one local picker keeps the native flow untouched.
        /// </summary>
        internal static bool Split(IList<int> queuedPlayers, IList<bool> isPlayerPicker, bool enabled,
                                   List<int> board, List<int> rest)
        {
            board.Clear();
            rest.Clear();
            var taken = new HashSet<int>();
            var boardIdx = new HashSet<int>();

            if (enabled)
            {
                for (int i = 0; i < queuedPlayers.Count && taken.Count < CouchLayout.MaxLanes; i++)
                {
                    if (!isPlayerPicker[i]) continue;
                    if (taken.Add(queuedPlayers[i])) boardIdx.Add(i);
                }
            }

            if (taken.Count < 2) boardIdx.Clear();

            for (int i = 0; i < queuedPlayers.Count; i++)
            {
                if (boardIdx.Contains(i)) board.Add(queuedPlayers[i]);
                else rest.Add(queuedPlayers[i]);
            }
            return board.Count > 0;
        }
    }
}
