using System;

namespace GamblersDen.Leaderboards
{
    /// <summary>
    /// Does nothing, successfully. Used when leaderboards are switched off (see
    /// Leaderboards.SetEnabled) so call sites never have to null-check the
    /// service, and so the game runs identically offline or in tests without
    /// hitting the network or needing LootLocker credentials.
    ///
    /// Submissions report success - the game did its part, there is simply no
    /// backend listening. Fetches report an empty (successful) page, which a UI
    /// renders as "no scores yet" rather than as an error.
    /// </summary>
    public class NullLeaderboardService : ILeaderboardService
    {
        public bool IsSignedIn => false;

        public void SignIn(Action<bool> onComplete = null) => onComplete?.Invoke(false);

        public void SubmitRun(string playerName, int bestScore, int endScore, Action<bool, LeaderboardRun> onComplete = null) =>
            onComplete?.Invoke(true, new LeaderboardRun(playerName, 1, bestScore, endScore));

        public void GetTopScores(LeaderboardCategory category, int count, Action<LeaderboardPage> onComplete) =>
            onComplete?.Invoke(LeaderboardPage.Ok(new LeaderboardEntry[0]));
    }
}
