using UnityEngine;

namespace GamblersDen.Leaderboards
{
    /// <summary>
    /// Opens the leaderboard session once when the game starts, so the first
    /// real submission (at the end of a run) isn't also paying for the
    /// round trip to authenticate.
    ///
    /// This ONLY signs in - it never writes a score. Nothing here touches the
    /// leaderboards' contents; the only write in the project is the genuine
    /// end-of-run submit in PokerGameManager.
    ///
    /// Runs after the first scene loads, so no scene or prefab has to be
    /// touched. Set SignInOnStart = false to skip it, in which case the first
    /// submit or fetch just signs in on demand instead.
    /// </summary>
    public static class LeaderboardBootstrap
    {
        /// <summary>Set false to skip the startup sign-in entirely.</summary>
        public static bool SignInOnStart = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoRun()
        {
            if (!SignInOnStart)
            {
                return;
            }

            Connect();
        }

        /// <summary>
        /// Establishes the guest session. Safe to call more than once - the
        /// service returns immediately if a session already exists, and queues
        /// callers onto a single attempt if one is already in flight.
        /// </summary>
        public static void Connect()
        {
            Debug.Log("[Leaderboards] Connecting to the leaderboard service...");

            Leaderboards.Service.SignIn(signedIn =>
            {
                if (signedIn)
                {
                    Debug.Log("[Leaderboards] Connected - ready to submit and fetch scores.");
                }
                else
                {
                    Debug.LogWarning(
                        "[Leaderboards] Could not connect. Scores will not be recorded this session.\n" +
                        "  - Check Project Settings > LootLocker has the Game API Key.\n" +
                        "  - Check Guest Login is enabled in the LootLocker console " +
                        "(Game Settings > Platforms).\n" +
                        "  - Check the editor has internet access.");
                }
            });
        }
    }
}
