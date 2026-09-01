using System;
using System.Collections.Generic;
using LootLocker;
using LootLocker.Requests;
using UnityEngine;

namespace GamblersDen.Leaderboards
{
    /// <summary>
    /// LootLocker-backed implementation of ILeaderboardService.
    ///
    /// This is deliberately the ONLY file in the project that references the
    /// LootLocker SDK. Everything else talks to ILeaderboardService, so if the
    /// SDK's API changes (or the backend is swapped out entirely) the blast
    /// radius is this one class.
    ///
    /// Auth is a guest session: itch.io/WebGL has no first-party platform login,
    /// and a guest session needs no account from the player. The identifier
    /// LootLocker hands back on first run is persisted to PlayerPrefs and
    /// replayed on later runs, so the same browser keeps the same leaderboard
    /// identity instead of creating a new player on every page load.
    ///
    /// A leaderboard row only ranks on a single int, so the other three fields
    /// (name, sub id, and whichever score is not being ranked) ride along in the
    /// entry's metadata as JSON - see RunPayload.
    /// </summary>
    public class LootLockerLeaderboardService : ILeaderboardService
    {
        /// <summary>Leaderboard keys, as created in the LootLocker dashboard.</summary>
        public const string DefaultBestOverallKey = "best_overall_value";
        public const string DefaultBestFinalKey = "best_final_value";

        /// <summary>
        /// How many existing rows to scan when working out the next sub id for a
        /// name. Anything past this is treated as "not found", so a very long
        /// board could in principle reuse a sub id - acceptable for a name
        /// disambiguator, and it keeps this to one request.
        /// </summary>
        public const int SubIdScanDepth = 200;

        private const string GuestIdentifierPrefsKey = "GamblersDen.LootLocker.GuestIdentifier";

        /// <summary>
        /// Wire format for the metadata blob. Short field names keep the string
        /// small; JsonUtility needs public fields on a [Serializable] type.
        /// </summary>
        [Serializable]
        private class RunPayload
        {
            public string n;    // player name
            public int s;       // sub id
            public int b;       // best score
            public int e;       // end score
        }

        private readonly string _bestOverallKey;
        private readonly string _bestFinalKey;

        // Guards against a second SignIn while one is already running: extra
        // callers are queued here and all fire when the single attempt lands.
        private readonly List<Action<bool>> _pendingSignIn = new List<Action<bool>>();
        private bool _signInInFlight;

        public LootLockerLeaderboardService(
            string bestOverallKey = DefaultBestOverallKey,
            string bestFinalKey = DefaultBestFinalKey)
        {
            _bestOverallKey = bestOverallKey;
            _bestFinalKey = bestFinalKey;
        }

        public bool IsSignedIn { get; private set; }

        /// <summary>The player's own member id, used to flag their row in results.</summary>
        public string LocalMemberId { get; private set; } = string.Empty;

        private string KeyFor(LeaderboardCategory category)
        {
            return category == LeaderboardCategory.BestOverall ? _bestOverallKey : _bestFinalKey;
        }

        private static string Describe(LootLockerResponse response)
        {
            if (response == null) return "no response";
            if (response.errorData != null && !string.IsNullOrEmpty(response.errorData.message))
            {
                return $"{response.errorData.message} (HTTP {response.statusCode})";
            }

            return $"HTTP {response.statusCode}";
        }

        // ---------------- Metadata ----------------

        private static string Encode(LeaderboardRun run)
        {
            return JsonUtility.ToJson(new RunPayload
            {
                n = run.PlayerName,
                s = run.SubId,
                b = run.BestScore,
                e = run.EndScore,
            });
        }

        /// <summary>
        /// Rebuilds a run from an entry's metadata. Rows written before this
        /// payload existed (or by another client) decode to a best-effort run
        /// using the ranked score, rather than throwing.
        /// </summary>
        private static LeaderboardRun Decode(string metadata, string fallbackName, int rankedValue)
        {
            if (!string.IsNullOrEmpty(metadata))
            {
                try
                {
                    var payload = JsonUtility.FromJson<RunPayload>(metadata);
                    if (payload != null && !string.IsNullOrEmpty(payload.n))
                    {
                        return new LeaderboardRun(payload.n, payload.s, payload.b, payload.e);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Leaderboards] Could not decode metadata '{metadata}': {e.Message}");
                }
            }

            return new LeaderboardRun(fallbackName, 0, rankedValue, rankedValue);
        }

        // ---------------- Session ----------------

        public void SignIn(Action<bool> onComplete = null)
        {
            if (IsSignedIn)
            {
                onComplete?.Invoke(true);
                return;
            }

            if (onComplete != null)
            {
                _pendingSignIn.Add(onComplete);
            }

            if (_signInInFlight)
            {
                return; // already running; this caller was queued above
            }

            _signInInFlight = true;

            var storedIdentifier = PlayerPrefs.GetString(GuestIdentifierPrefsKey, string.Empty);

            // Two different overloads on purpose: passing an empty identifier is
            // not the same as passing none, so only use the string overload when
            // there really is a stored identity to resume.
            if (string.IsNullOrEmpty(storedIdentifier))
            {
                Debug.Log("[Leaderboards] Starting a NEW guest session (no stored identifier).");
                LootLockerSDKManager.StartGuestSession(OnSessionComplete);
            }
            else
            {
                Debug.Log($"[Leaderboards] Resuming guest session '{storedIdentifier}'.");
                LootLockerSDKManager.StartGuestSession(storedIdentifier, OnSessionComplete);
            }
        }

        private void OnSessionComplete(LootLockerGuestSessionResponse response)
        {
            _signInInFlight = false;
            IsSignedIn = response != null && response.success;

            if (IsSignedIn)
            {
                if (!string.IsNullOrEmpty(response.player_identifier))
                {
                    PlayerPrefs.SetString(GuestIdentifierPrefsKey, response.player_identifier);
                    PlayerPrefs.Save();
                }

                LocalMemberId = response.player_id.ToString();

                Debug.Log($"[Leaderboards] Signed in OK - player_id={response.player_id}, " +
                          $"returning={response.seen_before}, identifier={response.player_identifier}");
            }
            else
            {
                Debug.LogWarning($"[Leaderboards] Guest sign-in FAILED: {Describe(response)}. " +
                                 "Check the API key in Project Settings > LootLocker.");
            }

            // Copy before invoking: a callback may itself call SignIn again.
            var waiting = _pendingSignIn.ToArray();
            _pendingSignIn.Clear();
            foreach (var callback in waiting)
            {
                callback(IsSignedIn);
            }
        }

        /// <summary>Runs an action once signed in, failing it if sign-in fails.</summary>
        private void WhenSignedIn(Action work, Action onFailure)
        {
            if (IsSignedIn)
            {
                work();
                return;
            }

            SignIn(ok =>
            {
                if (ok) work();
                else onFailure?.Invoke();
            });
        }

        // ---------------- Submitting ----------------

        public void SubmitRun(string playerName, int bestScore, int endScore, Action<bool, LeaderboardRun> onComplete = null)
        {
            var run = new LeaderboardRun(playerName, 0, bestScore, endScore);

            WhenSignedIn(
                () => ResolveSubId(run.PlayerName, subId =>
                {
                    run.SubId = subId;
                    Debug.Log($"[Leaderboards] Posting run: {run} (name clamped to {LeaderboardRun.MaxNameLength} chars).");
                    PostToBothBoards(run, onComplete);
                }),
                () => onComplete?.Invoke(false, run));
        }

        /// <summary>
        /// Works out the next sub id for a name by scanning the overall board for
        /// rows already using it. Sub ids are per-name, so one board is enough -
        /// every run is posted to both, so both carry the same set of names.
        /// </summary>
        private void ResolveSubId(string playerName, Action<int> onResolved)
        {
            LootLockerSDKManager.GetScoreList(_bestOverallKey, SubIdScanDepth, 0, response =>
            {
                if (response == null || !response.success || response.items == null)
                {
                    // Not fatal: fall back to 1 rather than blocking the submit.
                    Debug.LogWarning($"[Leaderboards] Sub id scan failed ({Describe(response)}); using 1.");
                    onResolved(1);
                    return;
                }

                var highest = 0;
                foreach (var item in response.items)
                {
                    if (item == null) continue;

                    var existing = Decode(item.metadata, item.player?.name, item.score);
                    if (string.Equals(existing.PlayerName, playerName, StringComparison.OrdinalIgnoreCase))
                    {
                        highest = Mathf.Max(highest, existing.SubId);
                    }
                }

                var next = highest + 1;
                Debug.Log($"[Leaderboards] Sub id for '{playerName}': {next} " +
                          $"(highest existing was {highest}, scanned {response.items.Length} rows).");
                onResolved(next);
            });
        }

        private void PostToBothBoards(LeaderboardRun run, Action<bool, LeaderboardRun> onComplete)
        {
            var metadata = Encode(run);
            var results = new bool?[2];

            void Report(int slot, bool ok)
            {
                results[slot] = ok;
                if (results[0].HasValue && results[1].HasValue)
                {
                    onComplete?.Invoke(results[0].Value && results[1].Value, run);
                }
            }

            Post(_bestOverallKey, run.BestScore, metadata, ok => Report(0, ok));
            Post(_bestFinalKey, run.EndScore, metadata, ok => Report(1, ok));
        }

        // Empty memberId: on a *player* leaderboard LootLocker attributes the
        // score to the session's player, which is what we want. A non-empty id
        // here would instead target a generic leaderboard member.
        private void Post(string leaderboardKey, int score, string metadata, Action<bool> onComplete)
        {
            LootLockerSDKManager.SubmitScore(string.Empty, score, leaderboardKey, metadata, response =>
            {
                var ok = response != null && response.success;
                if (ok)
                {
                    Debug.Log($"[Leaderboards] POST '{leaderboardKey}' score={score} OK -> rank {response.rank}. " +
                              $"metadata={metadata}");
                }
                else
                {
                    Debug.LogWarning($"[Leaderboards] POST '{leaderboardKey}' score={score} FAILED: {Describe(response)}");
                }

                onComplete(ok);
            });
        }

        // ---------------- Fetching ----------------

        public void GetTopScores(LeaderboardCategory category, int count, Action<LeaderboardPage> onComplete)
        {
            if (onComplete == null)
            {
                return; // a fetch with nowhere to deliver is a no-op
            }

            if (count <= 0)
            {
                onComplete(LeaderboardPage.Ok(new LeaderboardEntry[0]));
                return;
            }

            var key = KeyFor(category);

            WhenSignedIn(
                // "after" = 0 starts at rank 1, so this is the top N.
                () => LootLockerSDKManager.GetScoreList(key, count, 0, response =>
                {
                    if (response == null || !response.success)
                    {
                        var error = Describe(response);
                        Debug.LogWarning($"[Leaderboards] GET '{key}' FAILED: {error}");
                        onComplete(LeaderboardPage.Fail(error));
                        return;
                    }

                    var entries = Convert(response.items);
                    Debug.Log($"[Leaderboards] GET '{key}' OK - {entries.Count} entries.");
                    onComplete(LeaderboardPage.Ok(entries));
                }),
                () => onComplete(LeaderboardPage.Fail("Not signed in to the leaderboard service.")));
        }

        private IReadOnlyList<LeaderboardEntry> Convert(LootLockerLeaderboardMember[] items)
        {
            if (items == null || items.Length == 0)
            {
                return new LeaderboardEntry[0];
            }

            var entries = new List<LeaderboardEntry>(items.Length);
            foreach (var item in items)
            {
                if (item == null) continue;

                var run = Decode(item.metadata, item.player?.name, item.score);

                var isLocal = !string.IsNullOrEmpty(LocalMemberId)
                              && (item.member_id == LocalMemberId
                                  || (item.player != null && item.player.id.ToString() == LocalMemberId));

                entries.Add(new LeaderboardEntry(item.rank, run, item.score, item.member_id, isLocal));
            }

            return entries;
        }
    }
}
