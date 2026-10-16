using System;
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
            var idx = new List<int>();
            for (int i = 0; i < queuedPlayers.Count; i++) idx.Add(i);
            var b = new List<int>();
            var r = new List<int>();
            bool any = Split(idx, i => queuedPlayers[i], i => isPlayerPicker[i], enabled, b, r);
            board.Clear();
            rest.Clear();
            foreach (var i in b) board.Add(queuedPlayers[i]);
            foreach (var i in r) rest.Add(queuedPlayers[i]);
            return any;
        }

        /// <summary>
        /// The same split over the queue's own entries, so the caller keeps each entry's picker type.
        /// </summary>
        internal static bool Split<T>(IList<T> queue, Func<T, int> player, Func<T, bool> isPlayerPicker,
                                      bool enabled, List<T> board, List<T> rest)
        {
            board.Clear();
            rest.Clear();
            var taken = new HashSet<int>();
            var boardIdx = new HashSet<int>();

            if (enabled)
            {
                for (int i = 0; i < queue.Count && taken.Count < CouchLayout.MaxLanes; i++)
                {
                    if (!isPlayerPicker(queue[i])) continue;
                    if (taken.Add(player(queue[i]))) boardIdx.Add(i);
                }
            }

            if (taken.Count < 2) boardIdx.Clear();

            for (int i = 0; i < queue.Count; i++)
            {
                if (boardIdx.Contains(i)) board.Add(queue[i]);
                else rest.Add(queue[i]);
            }
            return board.Count > 0;
        }
    }
}
