using UnityEngine;

namespace GamblersDen.Leaderboards
{
    /// <summary>
    /// Debug harness: posts one throwaway run to the API and reads it back,
    /// logging every step to the console. Its whole job is to answer "are the
    /// keys right and is data actually landing?" without needing any UI.
    ///
    /// Runs once per play session, after the first scene loads, so no scene or
    /// prefab has to be touched. Turn it off by setting RunOnPlay to false (or
    /// delete this file once leaderboards are wired into real UI).
    ///
    /// NOTE: it writes a REAL entry to the live board, under the name below.
    /// Point the keys at the Development environment while testing, or delete
    /// the test rows from the LootLocker dashboard afterwards.
    /// </summary>
    public static class LeaderboardConnectionTest
    {
        /// <summary>Master switch. Set false to stop the test firing on play.</summary>
        public static bool RunOnPlay = true;

        /// <summary>Name the test submits under - deliberately obvious in a list.</summary>
        public const string TestPlayerName = "ConnTest";

        private const int TestBestScore = 4321;
        private const int TestEndScore = 765;
        private const int FetchCount = 10;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoRun()
        {
            if (!RunOnPlay)
            {
                return;
            }

            Run();
        }

        /// <summary>
        /// Full round trip: sign in -> post -> read back. Safe to call manually
        /// (e.g. from a debug key) as well as on play.
        /// </summary>
        public static void Run()
        {
            Debug.Log("===== [Leaderboards] CONNECTION TEST START =====");
            Debug.Log("[Leaderboards] Step 1/3: signing in (guest session)...");

            Leaderboards.Service.SignIn(signedIn =>
            {
                if (!signedIn)
                {
                    Debug.LogError(
                        "[Leaderboards] TEST FAILED at sign-in. Nothing was posted.\n" +
                        "  - Check Project Settings > LootLocker has the Game API Key.\n" +
                        "  - Check the key's environment (dev keys start with 'dev_').\n" +
                        "  - Check the editor has internet access.");
                    Debug.Log("===== [Leaderboards] CONNECTION TEST END (failed) =====");
                    return;
                }

                Debug.Log($"[Leaderboards] Step 2/3: posting a test run - " +
                          $"name='{TestPlayerName}', best={TestBestScore}, end={TestEndScore}...");

                Leaderboards.SubmitRun(TestPlayerName, TestBestScore, TestEndScore, (posted, run) =>
                {
                    if (!posted)
                    {
                        Debug.LogError(
                            "[Leaderboards] TEST FAILED at submit. The session opened, so the API key is " +
                            "valid - the leaderboards themselves are the likely problem.\n" +
                            $"  - Confirm boards exist with keys '{LootLockerLeaderboardService.DefaultBestOverallKey}' " +
                            $"and '{LootLockerLeaderboardService.DefaultBestFinalKey}'.\n" +
                            "  - Confirm both are type 'Player' (not Generic) and Descending.");
                        Debug.Log("===== [Leaderboards] CONNECTION TEST END (failed) =====");
                        return;
                    }

                    Debug.Log($"[Leaderboards] Posted OK. Stored as: {run} " +
                              $"(name='{run.PlayerName}', subId={run.SubId}, " +
                              $"best={run.BestScore}, end={run.EndScore})");

                    Debug.Log($"[Leaderboards] Step 3/3: reading back the top {FetchCount} of both boards...");

                    Leaderboards.GetBothTopScores(FetchCount, (overall, final) =>
                    {
                        Debug.Log($"[Leaderboards] BEST OVERALL:\n{Leaderboards.Describe(overall)}");
                        Debug.Log($"[Leaderboards] BEST FINAL:\n{Leaderboards.Describe(final)}");

                        var ok = overall.Success && final.Success;
                        if (ok)
                        {
                            Debug.Log("===== [Leaderboards] CONNECTION TEST PASSED " +
                                      "(post + read-back both worked) =====");
                        }
                        else
                        {
                            Debug.LogError("[Leaderboards] TEST FAILED at read-back - the post succeeded but " +
                                           "fetching did not. Check both board keys exist.");
                            Debug.Log("===== [Leaderboards] CONNECTION TEST END (failed) =====");
                        }
                    });
                });
            });
        }
    }
}
