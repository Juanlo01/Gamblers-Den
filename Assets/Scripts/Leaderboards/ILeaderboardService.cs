using System;

namespace GamblersDen.Leaderboards
{
    /// <summary>
    /// Backend-agnostic leaderboard contract. Nothing in this file (or in the
    /// types it uses) references LootLocker, so game code can be written and
    /// tested against it without the SDK - see NullLeaderboardService for the
    /// offline stand-in, and LootLockerLeaderboardService for the real one.
    ///
    /// Every method is fire-and-forget with a callback: the backend is a remote
    /// HTTP service, so nothing here returns a value synchronously. Callbacks
    /// are invoked on Unity's main thread by the SDK's own dispatcher.
    /// </summary>
    public interface ILeaderboardService
    {
        /// <summary>True once a session exists and submissions can be made.</summary>
        bool IsSignedIn { get; }

        /// <summary>
        /// Opens a session. Safe to call repeatedly - concurrent callers are
        /// queued onto the one in-flight attempt rather than each starting their
        /// own. The other methods call this for you if needed, so an explicit
        /// call is only useful to warm the connection at startup.
        /// </summary>
        void SignIn(Action<bool> onComplete = null);

        /// <summary>
        /// Posts a finished run to both leaderboards. The name is trimmed to
        /// LeaderboardRun.MaxNameLength, and the sub id is assigned here by
        /// looking at what is already on the board - so the caller supplies only
        /// the name and the two scores.
        ///
        /// The callback receives the run as actually submitted, including the
        /// assigned SubId, so the UI can tell the player they are "deeg #2".
        /// </summary>
        void SubmitRun(string playerName, int bestScore, int endScore, Action<bool, LeaderboardRun> onComplete = null);

        /// <summary>
        /// Fetches the top <paramref name="count"/> entries, best first.
        /// The callback always receives a non-null page.
        /// </summary>
        void GetTopScores(LeaderboardCategory category, int count, Action<LeaderboardPage> onComplete);
    }
}
