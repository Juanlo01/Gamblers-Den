using System;
using UnityEngine;

namespace GamblersDen.Leaderboards
{
    /// <summary>
    /// The game's entry point to leaderboards - a thin static facade over
    /// ILeaderboardService so call sites read as `Leaderboards.SubmitRun(...)`
    /// without every caller having to locate or own the service instance.
    ///
    /// Deliberately not a MonoBehaviour: the LootLocker SDK runs its own HTTP
    /// and dispatches callbacks to the main thread itself, so there is nothing
    /// here that needs a GameObject, an Update, or a scene edit to work.
    ///
    /// Swap the backend with SetService() - useful for tests, or to disable
    /// networking entirely via SetEnabled(false).
    /// </summary>
    public static class Leaderboards
    {
        private static ILeaderboardService _service;

        /// <summary>
        /// The active backend. Defaults to LootLocker on first use; assign a
        /// different one through SetService before that to override.
        /// </summary>
        public static ILeaderboardService Service
        {
            get
            {
                if (_service == null)
                {
                    _service = new LootLockerLeaderboardService();
                }

                return _service;
            }
        }

        /// <summary>Replaces the backend. Pass null to fall back to LootLocker.</summary>
        public static void SetService(ILeaderboardService service)
        {
            _service = service;
        }

        /// <summary>
        /// Turns networked leaderboards on or off. Off swaps in the no-op
        /// service, so existing call sites keep working unchanged.
        /// </summary>
        public static void SetEnabled(bool enabled)
        {
            _service = enabled ? (ILeaderboardService)new LootLockerLeaderboardService() : new NullLeaderboardService();
        }

        public static bool IsSignedIn => Service.IsSignedIn;

        // ---------------- Setters ----------------

        /// <summary>
        /// Opens the session early so the first submission is not also paying
        /// for a round trip to authenticate. Optional - everything else signs in
        /// on demand. A good fit for a title-screen Start().
        /// </summary>
        public static void Warmup() => Service.SignIn(null);

        /// <summary>
        /// The primary setter: records a finished run on both boards.
        /// The name is clamped to 12 characters and the sub id is assigned for
        /// you; the callback hands back the run as actually stored.
        /// </summary>
        public static void SubmitRun(string playerName, int bestScore, int endScore,
            Action<bool, LeaderboardRun> onComplete = null) =>
            Service.SubmitRun(playerName, bestScore, endScore, onComplete);

        // ---------------- Getters ----------------

        /// <summary>
        /// The primary getter: top <paramref name="count"/> entries for a
        /// category, best first. The callback always gets a non-null page.
        /// </summary>
        public static void GetTopScores(LeaderboardCategory category, int count, Action<LeaderboardPage> onComplete) =>
            Service.GetTopScores(category, count, onComplete);

        /// <summary>Top N by peak money held during a run.</summary>
        public static void GetTopBestOverall(int count, Action<LeaderboardPage> onComplete) =>
            Service.GetTopScores(LeaderboardCategory.BestOverall, count, onComplete);

        /// <summary>Top N by money held when the run ended.</summary>
        public static void GetTopBestFinal(int count, Action<LeaderboardPage> onComplete) =>
            Service.GetTopScores(LeaderboardCategory.BestFinal, count, onComplete);

        /// <summary>
        /// Fetches both boards in one call, for a results screen that shows them
        /// side by side. The callback fires once, after both have returned.
        /// </summary>
        public static void GetBothTopScores(int count, Action<LeaderboardPage, LeaderboardPage> onComplete)
        {
            if (onComplete == null) return;

            LeaderboardPage overall = null;
            LeaderboardPage final = null;

            void TryFinish()
            {
                if (overall != null && final != null)
                {
                    onComplete(overall, final);
                }
            }

            GetTopBestOverall(count, page => { overall = page; TryFinish(); });
            GetTopBestFinal(count, page => { final = page; TryFinish(); });
        }

        /// <summary>Formats a page for logging - handy while wiring up UI.</summary>
        public static string Describe(LeaderboardPage page)
        {
            if (page == null) return "<null page>";
            if (!page.Success) return $"<failed: {page.Error}>";
            if (page.Entries.Count == 0) return "<no entries>";

            var lines = new System.Text.StringBuilder();
            foreach (var e in page.Entries)
            {
                lines.Append($"  #{e.Rank,-3} {e.DisplayName,-16} ranked=${e.Value,-8} " +
                             $"best=${e.Run.BestScore,-8} end=${e.Run.EndScore}");
                lines.AppendLine(e.IsLocalPlayer ? "   <- you" : string.Empty);
            }

            return lines.ToString().TrimEnd();
        }

        /// <summary>Logs both boards. Diagnostic helper, safe to delete.</summary>
        public static void LogTopScores(int count = 10)
        {
            GetBothTopScores(count, (overall, final) =>
            {
                Debug.Log($"[Leaderboards] BEST OVERALL (top {count}):\n{Describe(overall)}");
                Debug.Log($"[Leaderboards] BEST FINAL (top {count}):\n{Describe(final)}");
            });
        }
    }
}
