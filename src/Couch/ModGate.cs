using System;
using System.Collections.Generic;

namespace EveryonePicks.Couch
{
    /// <summary>
    /// Which loaded mods switch couch mode off. The user lists plugin GUIDs or Harmony ids in
    /// <c>Couch.SequentialWith</c>; a mod that turns out to fight the board can be sidelined from
    /// the config without a new release. Pure, so the matching is unit-tested.
    /// </summary>
    internal static class ModGate
    {
        /// <summary>The entries of a comma/semicolon separated list, trimmed, empties dropped.</summary>
        internal static List<string> Parse(string list)
        {
            var r = new List<string>();
            if (string.IsNullOrEmpty(list)) return r;
            foreach (var part in list.Split(',', ';'))
            {
                var p = part.Trim();
                if (p.Length > 0 && !r.Exists(x => string.Equals(x, p, StringComparison.OrdinalIgnoreCase))) r.Add(p);
            }
            return r;
        }

        /// <summary>
        /// The listed ids that are loaded, case-insensitive and exact (no prefix guessing: a GUID
        /// like "com.x.cards" must not catch "com.x.cards.extra"). Order follows the list.
        /// </summary>
        internal static List<string> Blocking(string list, IEnumerable<string> loaded)
        {
            var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in loaded) if (!string.IsNullOrEmpty(id)) have.Add(id.Trim());
            var r = new List<string>();
            foreach (var want in Parse(list)) if (have.Contains(want)) r.Add(want);
            return r;
        }
    }
}
