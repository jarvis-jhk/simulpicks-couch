using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EveryonePicks.Couch;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace EveryonePicks
{
    /// <summary>
    /// Couch mode: every local player who owes a pick gets their own region of the screen and
    /// picks with their own controller, all at once. Runs inside the local runner in place of the
    /// native one-at-a-time session, and reports through the same <see cref="State.ReportSession"/>,
    /// so results, barrier, reveal and the wire protocol do not know the difference.
    /// Layout, queue split and input repeat are the pure parts in src/Couch (unit-tested).
    /// </summary>
    internal static class CouchBoard
    {
        private sealed class Lane
        {
            public int playerID;
            public Player player;
            public NRect region;
            public string name;
            public Color color;

            public readonly List<GameObject> cards = new List<GameObject>();
            public readonly List<GameObject> spawned = new List<GameObject>();   // this lane's CardChoice.spawnedCards
            public float[] xs = new float[0];
            public float cy;
            public float cardW;
            public LaneCursor cursor;

            public readonly List<ResultEntry> picks = new List<ResultEntry>();
            public int baseIdx;
            public bool inSession;    // between a hand appearing and its ReportSession
            public bool picked;       // confirmed; waiting out the settle before reporting
            public float deadline;
            public float inputFrom;
            public float settleUntil;
        }

        private static readonly List<Lane> lanes = new List<Lane>();
        private static int token;
        private static bool warnedUnready;

        // Card aspect (width / height) the layout assumes; only the row spacing depends on it.
        private const float CardAspect = 0.72f;
        // Jump held from the last frame of play must not pick the card under the cursor.
        private const float InputGrace = 0.4f;
        // Time between confirming and reporting, for the pick's own RPCs and card effects.
        private const float Settle = 0.35f;

        private static readonly MethodInfo M_spawnUnique = AccessTools.Method(typeof(CardChoice), "SpawnUniqueCard",
            new[] { typeof(Vector3), typeof(Quaternion) });
        private static readonly MethodInfo M_applyPick = AccessTools.Method(typeof(ApplyCardStats), "Pick",
            new[] { typeof(int), typeof(bool), typeof(PickerType) });
        private static readonly FieldInfo F_children = AccessTools.Field(typeof(CardChoice), "children");

        internal static bool Active => lanes.Count > 0;

        /// <summary>Whether the game members the board needs are all there; else the runner stays native.</summary>
        internal static bool Ready()
        {
            bool ok = M_spawnUnique != null && M_applyPick != null && State.F_pickrID != null
                      && State.F_pickerType != null && State.F_spawnedCards != null
                      && CardChoice.instance != null && MainCam.instance != null && MainCam.instance.cam != null;
            if (!ok && !warnedUnready)
            {
                warnedUnready = true;
                EveryonePicksPlugin.Warn("Couch board unavailable (missing game member or camera); local players take turns.");
            }
            return ok;
        }

        /// <summary>A lane is running a session for this player (counts as still owing a pick).</summary>
        internal static bool InSession(int playerID)
        {
            foreach (var l in lanes) if (l.playerID == playerID && l.inSession) return true;
            return false;
        }

        /// <summary>Where <see cref="Patch_Pick_Capture"/> records a card this player got, or null.</summary>
        internal static List<ResultEntry> CaptureFor(int playerID)
        {
            foreach (var l in lanes) if (l.playerID == playerID && l.inSession) return l.picks;
            return null;
        }

        internal static IEnumerator Run(List<int> players)
        {
            token = State.phaseToken;
            lanes.Clear();
            State.localCards.Clear();
            State.currentSessionPlayer = -1;
            State.localPickActive = true;
            EveryonePicksPlugin.Log("Couch board: " + players.Count + " local players pick at once.");

            try
            {
                for (int i = 0; i < players.Count; i++)
                {
                    int pid = players[i];
                    lanes.Add(new Lane
                    {
                        playerID = pid,
                        player = State.FindPlayer(pid),
                        region = CouchLayout.Region(i, players.Count),
                        name = State.DisplayName(pid),
                        color = State.PlayerColor(pid),
                    });
                }
                foreach (var l in lanes) StartHand(l);

                while (token == State.phaseToken)
                {
                    bool anyOpen = false;
                    foreach (var l in lanes)
                    {
                        if (!l.inSession && TakeQueued(l.playerID)) StartHand(l);
                        if (!l.inSession) continue;
                        anyOpen = true;
                        Tick(l);
                    }
                    if (!anyOpen) break;
                    yield return null;
                }
            }
            finally
            {
                // A barrier cut stops this coroutine without running finally (Unity does not dispose
                // stopped coroutines); FlushRunner calls Flush for that. This covers exceptions and
                // the normal end.
                foreach (var l in lanes) DestroyCards(l);
                lanes.Clear();
                State.localPickActive = false;
            }
        }

        /// <summary>Remove this player's next queued Player-type entry, if any: an extra pick.</summary>
        private static bool TakeQueued(int playerID)
        {
            var q = State.localQueue;
            if (q.Count == 0) return false;
            bool found = false;
            var keep = new Queue<KeyValuePair<int, PickerType>>();
            while (q.Count > 0)
            {
                var kv = q.Dequeue();
                if (!found && kv.Key == playerID && kv.Value == PickerType.Player) { found = true; continue; }
                keep.Enqueue(kv);
            }
            while (keep.Count > 0) q.Enqueue(keep.Dequeue());
            return found;
        }

        private static void StartHand(Lane l)
        {
            State.producedCount.TryGetValue(l.playerID, out l.baseIdx);
            l.picks.Clear();
            l.inSession = true;
            l.picked = false;
            float now = Time.realtimeSinceStartup;
            l.deadline = now + State.PickSeconds;
            l.inputFrom = now + InputGrace;
            State.BroadcastStatus(token, l.playerID, 1, false);

            try { SpawnHand(l); }
            catch (Exception e) { EveryonePicksPlugin.Warn("Couch hand for player " + l.playerID + " failed: " + e.Message); }

            if (l.cards.Count == 0)
            {
                EveryonePicksPlugin.Warn("Couch hand for player " + l.playerID + " is empty; skipping the pick.");
                Finish(l);
                return;
            }
            l.cursor = new LaneCursor(l.cards.Count);
            ShowSelection(l);
        }

        private static int HandSize(CardChoice choice)
        {
            try
            {
                if (F_children?.GetValue(choice) is Transform[] slots && slots.Length > 0) return slots.Length;
            }
            catch { }
            return 5;
        }

        private static void SpawnHand(Lane l)
        {
            var choice = CardChoice.instance;
            int n = HandSize(choice);
            l.xs = new float[n];
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            float scale = CouchLayout.CardRow(l.region, n, aspect, CardAspect, l.xs, out l.cy)
                          * Mathf.Clamp(EveryonePicksPlugin.CouchCardScale.Value, 0.3f, 2f);
            l.cardW = l.xs.Length > 1 ? (l.xs[1] - l.xs[0]) / 1.12f : l.region.w * 0.3f;
            l.spawned.Clear();

            // The spawn-time picker is global CardChoice state: rarity, class blacklists and
            // duplicate checks (CardChoiceSpawnUniqueCardPatch, ModdingUtils) read pickrID,
            // pickerType and spawnedCards. Swap in this lane's for each card and put them back.
            var oldID = State.F_pickrID.GetValue(choice);
            var oldType = State.F_pickerType.GetValue(choice);
            var oldList = State.F_spawnedCards.GetValue(choice);
            try
            {
                State.F_pickrID.SetValue(choice, l.playerID);
                State.F_pickerType.SetValue(choice, PickerType.Player);
                State.F_spawnedCards.SetValue(choice, l.spawned);
                for (int i = 0; i < n; i++)
                {
                    var go = M_spawnUnique.Invoke(choice, new object[] { ToWorld(l.xs[i], l.cy), Quaternion.identity }) as GameObject;
                    if (go == null) continue;
                    if (!l.spawned.Contains(go)) l.spawned.Add(go);
                    go.transform.localScale = go.transform.localScale * scale;
                    l.cards.Add(go);
                }
            }
            finally
            {
                State.F_pickrID.SetValue(choice, oldID);
                State.F_pickerType.SetValue(choice, oldType);
                State.F_spawnedCards.SetValue(choice, oldList);
            }
        }

        /// <summary>Normalised screen (0,0 top left) to world, the way PickNCards places its slots.</summary>
        private static Vector3 ToWorld(float nx, float ny)
        {
            var v = MainCam.instance.cam.ScreenToWorldPoint(new Vector3(nx * Screen.width, (1f - ny) * Screen.height, 0f));
            v.z = -5f;
            return v;
        }

        private static void Tick(Lane l)
        {
            float now = Time.realtimeSinceStartup;
            if (l.picked)
            {
                if (now >= l.settleUntil) Finish(l);
                return;
            }

            var actions = l.player != null && l.player.data != null ? l.player.data.playerActions : null;
            if (actions != null && now >= l.inputFrom)
            {
                float x = 0f;
                try { x = actions.Move != null ? actions.Move.X : 0f; } catch { }
                if (l.cursor.Update(x, Time.unscaledDeltaTime)) ShowSelection(l);

                bool confirm = false;
                try { confirm = actions.Jump != null && actions.Jump.WasPressed; } catch { }
                if (confirm) { Confirm(l); return; }
            }

            if (now > l.deadline)
            {
                EveryonePicksPlugin.Warn("Pick time expired for player " + l.playerID + " - auto-picking the selected card.");
                Confirm(l);
            }
        }

        private static void ShowSelection(Lane l)
        {
            for (int i = 0; i < l.cards.Count; i++)
            {
                var go = l.cards[i];
                if (go == null) continue;
                try { go.GetComponentInChildren<CardVisuals>()?.ChangeSelected(i == l.cursor.Selected); }
                catch { }
            }
        }

        private static void Confirm(Lane l)
        {
            l.picked = true;
            l.settleUntil = Time.realtimeSinceStartup + Settle;
            var card = l.cursor != null && l.cursor.Selected < l.cards.Count ? l.cards[l.cursor.Selected] : null;
            if (card == null) return;

            // The vanilla pick does exactly this for the picker; Patch_Pick_Capture records it into
            // this lane, and the redirected RPCA_Pick applies it on this machine.
            try
            {
                var stats = card.GetComponentInChildren<ApplyCardStats>();
                if (stats != null) M_applyPick.Invoke(stats, new object[] { l.playerID, false, PickerType.Player });
            }
            catch (Exception e)
            {
                EveryonePicksPlugin.Warn("Couch pick for player " + l.playerID + " failed: " + (e.InnerException?.Message ?? e.Message));
            }

            // Everything but the picked card goes at once, so the lane visibly answers the press.
            for (int i = 0; i < l.cards.Count; i++)
                if (l.cards[i] != null && l.cards[i] != card) DestroyCard(l.cards[i]);
        }

        private static void Finish(Lane l)
        {
            DestroyCards(l);
            l.inSession = false;
            l.picked = false;
            State.ReportSession(token, l.playerID, l.baseIdx, new List<ResultEntry>(l.picks));
        }

        private static void DestroyCards(Lane l)
        {
            foreach (var go in l.cards) DestroyCard(go);
            l.cards.Clear();
            l.spawned.Clear();
        }

        private static void DestroyCard(GameObject go)
        {
            if (go == null) return;
            State.localCards.Remove(go);
            foreach (var view in go.GetComponentsInChildren<PhotonView>(true))
            {
                try { PhotonNetwork.LocalCleanPhotonView(view); } catch { }
            }
            UnityEngine.Object.Destroy(go);
        }

        /// <summary>The barrier stopped the runner: report what each open lane produced, as the native path does.</summary>
        internal static void Flush(int barrierToken)
        {
            foreach (var l in lanes)
            {
                DestroyCards(l);
                if (!l.inSession) continue;
                l.inSession = false;
                EveryonePicksPlugin.Warn("Barrier cut player " + l.playerID + "'s couch pick short; reporting what it had.");
                State.ReportSession(barrierToken, l.playerID, l.baseIdx, new List<ResultEntry>(l.picks));
            }
            lanes.Clear();
        }

        /// <summary>Hard reset: drop everything, report nothing.</summary>
        internal static void Abort()
        {
            foreach (var l in lanes) DestroyCards(l);
            lanes.Clear();
        }

        // ---------- drawing ----------

        private static Texture2D white;
        private static GUIStyle nameStyle, infoStyle;

        internal static void Draw()
        {
            if (lanes.Count == 0) return;
            if (white == null)
            {
                white = new Texture2D(1, 1);
                white.SetPixel(0, 0, Color.white);
                white.Apply();
                white.hideFlags = HideFlags.HideAndDontSave;
                nameStyle = new GUIStyle { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
                infoStyle = new GUIStyle { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = Color.white } };
            }

            float sw = Screen.width, sh = Screen.height;
            float now = Time.realtimeSinceStartup;
            var prev = GUI.color;
            foreach (var l in lanes)
            {
                var r = new Rect(l.region.x * sw, l.region.y * sh, l.region.w * sw, l.region.h * sh);
                float t = Mathf.Max(2f, sh * 0.004f);
                var c = l.color;

                // Frame in the player's colour; no fill, so the cards keep their own colours.
                GUI.color = new Color(c.r, c.g, c.b, 0.9f);
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), white);
                GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), white);
                GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), white);
                GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), white);

                // Name strip, the part of the region the layout keeps free of cards.
                float strip = r.height * 0.18f;
                GUI.color = new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 0.75f);
                GUI.DrawTexture(new Rect(r.x + t, r.y + t, r.width - 2 * t, strip - t), white);
                GUI.color = Color.white;
                int font = Mathf.Clamp((int)(strip * 0.5f), 12, 42);
                nameStyle.fontSize = infoStyle.fontSize = font;
                nameStyle.normal.textColor = Color.Lerp(c, Color.white, 0.5f);
                var labelRect = new Rect(r.x + strip * 0.4f, r.y + t, r.width - strip * 0.8f, strip - t);
                GUI.Label(labelRect, l.name, nameStyle);

                // Plain text: the default IMGUI font has no check mark glyph.
                string info = !l.inSession || l.picked ? "picked"
                            : Mathf.CeilToInt(Mathf.Max(0f, l.deadline - now)) + "s";
                infoStyle.normal.textColor = l.inSession && !l.picked && l.deadline - now < 10f
                    ? new Color(1f, 0.45f, 0.4f) : Color.white;
                GUI.Label(labelRect, info, infoStyle);

                // Marker under the selected card, so each player can find their cursor at a glance.
                if (l.inSession && !l.picked && l.cursor != null && l.cursor.Selected < l.xs.Length)
                {
                    float mx = l.xs[l.cursor.Selected] * sw, mw = l.cardW * sw * 0.8f;
                    GUI.color = new Color(c.r, c.g, c.b, 1f);
                    GUI.DrawTexture(new Rect(mx - mw / 2f, r.yMax - r.height * 0.05f, mw, Mathf.Max(4f, r.height * 0.02f)), white);
                }
            }
            GUI.color = prev;
        }
    }
}
