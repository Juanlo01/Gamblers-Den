using System;
using System.Collections.Generic;

namespace GamblersDen.Leaderboards
{
    /// <summary>
    /// The two things this game ranks players on. Both are "higher is better".
    /// A run is posted to both, carrying the same payload; only the ranked
    /// value differs.
    /// </summary>
    public enum LeaderboardCategory
    {
        /// <summary>
        /// The most money the player ever held at once during a run - their peak,
        /// regardless of how the run ended. Backed by PokerGameManager.GetBestScore().
        /// </summary>
        BestOverall = 0,

        /// <summary>
        /// The money the player walked away with when the run ended - busting out
        /// scores near zero, winning the table scores the whole stack.
        /// </summary>
        BestFinal = 1,
    }

    /// <summary>
    /// Everything recorded about one finished run. This is the payload posted to
    /// the API: the same four fields go to both leaderboards, so either board can
    /// display a full row without a second lookup.
    /// </summary>
    [Serializable]
    public class LeaderboardRun
    {
        /// <summary>Hard cap on a display name, enforced on submit.</summary>
        public const int MaxNameLength = 12;

        public LeaderboardRun() { }

        public LeaderboardRun(string playerName, int subId, int bestScore, int endScore)
        {
            PlayerName = ClampName(playerName);
            SubId = subId;
            BestScore = bestScore;
            EndScore = endScore;
        }

        /// <summary>Display name, at most <see cref="MaxNameLength"/> characters.</summary>
        public string PlayerName;

        /// <summary>
        /// Disambiguates repeated use of the same name: the first "deeg" is 1,
        /// the next is 2, and so on. Assigned by the service at submit time by
        /// looking at what is already on the board - callers pass 0 / leave it
        /// unset and read it back from the completion callback.
        /// </summary>
        public int SubId;

        /// <summary>Most money held at once during the run.</summary>
        public int BestScore;

        /// <summary>Money left when the run ended.</summary>
        public int EndScore;

        /// <summary>Name as shown to players, e.g. "deeg #2".</summary>
        public string DisplayName => SubId > 1 ? $"{PlayerName} #{SubId}" : PlayerName;

        /// <summary>
        /// Trims and truncates to the name limit. Null/blank becomes "Anonymous"
        /// so a row is never nameless.
        /// </summary>
        public static string ClampName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Anonymous";
            }

            name = name.Trim();
            return name.Length <= MaxNameLength ? name : name.Substring(0, MaxNameLength);
        }

        public override string ToString() =>
            $"{DisplayName} (best ${BestScore}, end ${EndScore})";
    }

    /// <summary>One row of a leaderboard.</summary>
    public readonly struct LeaderboardEntry
    {
        public LeaderboardEntry(int rank, LeaderboardRun run, int value, string memberId, bool isLocalPlayer)
        {
            Rank = rank;
            Run = run;
            Value = value;
            MemberId = memberId;
            IsLocalPlayer = isLocalPlayer;
        }

        /// <summary>1-based position on the board.</summary>
        public int Rank { get; }

        /// <summary>The four fields recorded for this run. Never null.</summary>
        public LeaderboardRun Run { get; }

        /// <summary>
        /// The value this board ranked on - BestScore on the overall board,
        /// EndScore on the final board.
        /// </summary>
        public int Value { get; }

        /// <summary>Backend id for this entry - useful to de-duplicate or highlight.</summary>
        public string MemberId { get; }

        /// <summary>True when this row belongs to the player on this device.</summary>
        public bool IsLocalPlayer { get; }

        public string PlayerName => Run?.PlayerName ?? "Anonymous";

        public int SubId => Run?.SubId ?? 0;

        public string DisplayName => Run?.DisplayName ?? "Anonymous";

        public override string ToString() => $"#{Rank} {DisplayName} ${Value}";
    }

    /// <summary>
    /// Result of a fetch. Always non-null: on failure, Success is false, Error
    /// explains why, and Entries is an empty list rather than null - so callers
    /// can bind to it without null-checking.
    /// </summary>
    public class LeaderboardPage
    {
        private static readonly LeaderboardEntry[] Empty = new LeaderboardEntry[0];

        public static LeaderboardPage Ok(IReadOnlyList<LeaderboardEntry> entries) =>
            new LeaderboardPage { Success = true, Entries = entries ?? Empty, Error = null };

        public static LeaderboardPage Fail(string error) =>
            new LeaderboardPage { Success = false, Entries = Empty, Error = error };

        public bool Success { get; private set; }

        public string Error { get; private set; }

        public IReadOnlyList<LeaderboardEntry> Entries { get; private set; } = Empty;
    }
}
