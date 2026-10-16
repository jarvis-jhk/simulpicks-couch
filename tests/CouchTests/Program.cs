using System;
using System.Collections.Generic;
using System.Linq;
using EveryonePicks.Couch;

// Tiny assertion runner: no test framework to download. Exit code = number of failures.
internal static class Program
{
    private static int failures, checks;

    private static void Check(bool ok, string what)
    {
        checks++;
        if (!ok) { failures++; Console.WriteLine("FAIL " + what); }
    }

    private static bool Near(float a, float b, float eps = 1e-4f) => Math.Abs(a - b) <= eps;

    private static int Main()
    {
        Grid();
        Regions();
        CardRows();
        Plan();
        Cursor();
        Console.WriteLine(checks + " checks, " + failures + " failed");
        return failures;
    }

    private static void Grid()
    {
        var want = new Dictionary<int, (int, int)>
        {
            [1] = (1, 1), [2] = (1, 2), [3] = (2, 2), [4] = (2, 2),
            [5] = (3, 3), [8] = (3, 3), [9] = (3, 3),
        };
        foreach (var kv in want)
        {
            CouchLayout.Grid(kv.Key, out int c, out int r);
            Check((c, r) == kv.Value, "grid " + kv.Key + " = " + c + "x" + r);
        }
        foreach (int bad in new[] { 0, 10 })
        {
            bool threw = false;
            try { CouchLayout.Grid(bad, out _, out _); } catch (ArgumentOutOfRangeException) { threw = true; }
            Check(threw, "grid " + bad + " rejected");
        }
    }

    private static void Regions()
    {
        for (int n = 1; n <= CouchLayout.MaxLanes; n++)
        {
            var rs = Enumerable.Range(0, n).Select(i => CouchLayout.Region(i, n)).ToList();
            foreach (var r in rs)
                Check(r.x >= 0 && r.y >= 0 && r.x + r.w <= 1.0001f && r.y + r.h <= 1.0001f && r.w > 0.2f && r.h > 0.2f,
                      "region inside screen n=" + n + " " + r);
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    var a = rs[i]; var b = rs[j];
                    bool overlap = a.x < b.x + b.w - 1e-5f && b.x < a.x + a.w - 1e-5f &&
                                   a.y < b.y + b.h - 1e-5f && b.y < a.y + a.h - 1e-5f;
                    Check(!overlap, "regions " + i + "," + j + " disjoint n=" + n);
                }
            // Row-major player order: later lanes are never above-and-left of earlier ones.
            for (int i = 1; i < n; i++)
                Check(rs[i].y > rs[i - 1].y + 1e-5f || (Near(rs[i].y, rs[i - 1].y) && rs[i].x > rs[i - 1].x),
                      "row-major order n=" + n + " lane " + i);
        }
        // Two players: full-width rows, one above the other.
        var top = CouchLayout.Region(0, 2); var bottom = CouchLayout.Region(1, 2);
        Check(top.w > 0.97f && bottom.w > 0.97f && top.y < bottom.y, "2 lanes are full-width rows");
        // Three players in a 2x2 leave the bottom-right cell empty.
        Check(CouchLayout.Region(2, 3).x < 0.1f, "3rd of 3 starts the second row");
    }

    private static void CardRows()
    {
        var xs = new float[8];
        foreach (int n in new[] { 2, 4, 9 })
            for (int i = 0; i < n; i++)
            {
                var reg = CouchLayout.Region(i, n);
                foreach (int cards in new[] { 1, 3, 5, 7 })
                {
                    float scale = CouchLayout.CardRow(reg, cards, 16f / 9f, 0.7f, xs, out float cy);
                    float cardH = 0.9f * scale; // upper bound of a full-screen card's height
                    float cardW = Math.Abs(cards > 1 ? xs[1] - xs[0] : 0f);
                    Check(scale > 0f && scale <= 1.0001f, "scale in (0,1] n=" + n + " cards=" + cards + " s=" + scale);
                    Check(cy > reg.y && cy < reg.y + reg.h, "row centre inside region");
                    Check(xs.Take(cards).All(x => x > reg.x && x < reg.x + reg.w), "card centres inside region");
                    Check(Near(xs.Take(cards).Average(), reg.CenterX), "row centred n=" + n + " cards=" + cards);
                    for (int k = 1; k < cards; k++) Check(xs[k] > xs[k - 1], "cards left to right");
                }
            }
        // Full screen, one lane, same card count: scale should be ~1 minus the label strip's cost.
        float full = CouchLayout.CardRow(CouchLayout.Region(0, 1, 0f), 5, 16f / 9f, 0.7f, xs, out _);
        Check(full > 0.8f && full <= 1.0001f, "one lane near full size: " + full);
        // 2x2 should land roughly in the design's 50-60% band for a vanilla hand of five.
        float quad = CouchLayout.CardRow(CouchLayout.Region(0, 4), 5, 16f / 9f, 0.7f, xs, out _);
        Check(quad > 0.4f && quad < 0.65f, "2x2 hand of five about half size: " + quad);
    }

    private static void Plan()
    {
        var board = new List<int>(); var rest = new List<int>();
        bool Run(int[] ids, bool[] pl, bool on = true) => CouchPlan.Split(ids, pl, on, board, rest);
        bool[] All(int n) => Enumerable.Repeat(true, n).ToArray();

        Check(!Run(new[] { 3 }, All(1)) && rest.SequenceEqual(new[] { 3 }), "one picker stays native");
        Check(!Run(new[] { 3, 3 }, All(2)) && rest.SequenceEqual(new[] { 3, 3 }), "one player twice stays native");
        Check(Run(new[] { 0, 1, 2, 3 }, All(4)) && board.SequenceEqual(new[] { 0, 1, 2, 3 }) && rest.Count == 0, "four players to the board");
        Check(!Run(new[] { 0, 1 }, All(2), on: false) && rest.SequenceEqual(new[] { 0, 1 }), "disabled keeps native");
        Check(Run(new[] { 0, 1, 0, 2 }, All(4)) && board.SequenceEqual(new[] { 0, 1, 2 }) && rest.SequenceEqual(new[] { 0 }),
              "a second entry for a player waits");
        Check(Run(new[] { 5, 0, 1 }, new[] { false, true, true }) && board.SequenceEqual(new[] { 0, 1 }) && rest.SequenceEqual(new[] { 5 }),
              "non-player picker stays sequential");
        Check(!Run(new[] { 5, 0 }, new[] { false, true }) && rest.SequenceEqual(new[] { 5, 0 }), "one real player + other picker stays native");
        var ten = Enumerable.Range(0, 10).ToArray();
        Check(Run(ten, All(10)) && board.Count == CouchLayout.MaxLanes && rest.SequenceEqual(new[] { 9 }), "overflow beyond nine waits");
        Check(!Run(new int[0], new bool[0]) && rest.Count == 0, "empty queue");

        // The runner splits its queue entries directly, keeping each entry's picker type and order.
        var q = new List<KeyValuePair<int, string>> { new(0, "P"), new(7, "T"), new(1, "P"), new(0, "P") };
        var eb = new List<KeyValuePair<int, string>>(); var er = new List<KeyValuePair<int, string>>();
        Check(CouchPlan.Split(q, e => e.Key, e => e.Value == "P", true, eb, er)
              && eb.SequenceEqual(new[] { q[0], q[2] }) && er.SequenceEqual(new[] { q[1], q[3] }),
              "entries split with their picker type, rest in queue order");
    }

    private static void Cursor()
    {
        var c = new LaneCursor(5);
        Check(c.Selected == 2, "starts in the middle");
        Check(!c.Update(0.3f, 0.016f) && c.Selected == 2, "inside deadzone does nothing");
        Check(c.Update(1f, 0.016f) && c.Selected == 3, "push moves once at once");
        Check(!c.Update(1f, 0.2f) && c.Selected == 3, "no repeat before the delay");
        Check(c.Update(1f, 0.2f) && c.Selected == 4, "repeat after the delay");
        Check(c.Update(1f, 0.12f) && c.Selected == 0, "repeat wraps to the left end");
        c.Update(0f, 0.016f);
        Check(c.Update(-1f, 0.016f) && c.Selected == 4, "left wraps to the right end");
        c.Update(0f, 0.016f);
        c.Update(1f, 0.016f);           // 0
        int before = c.Selected;
        c.Update(1f, 0.35f + 0.12f * 2); // one hitch owes first repeat + two more
        Check(c.Selected == (before + 3) % 5, "a hitch delivers every owed repeat: " + c.Selected);
        Check(c.Update(-1f, 0.016f), "reversing moves at once");
        var one = new LaneCursor(1);
        Check(!one.Update(1f, 0.016f) && one.Selected == 0, "single card never moves");
        one.Reset(4);
        Check(one.Selected == 2, "reset recentres");
        bool threw = false;
        try { new LaneCursor(0); } catch (ArgumentOutOfRangeException) { threw = true; }
        Check(threw, "empty hand rejected");
    }
}
