# Leaderboards — setup

Code is done and compiles. The remaining steps need your LootLocker account, so
they can't be scripted — they're all dashboard/Inspector work.

## 1. Packages (already done)

`Packages/manifest.json` now has:

```json
"scopedRegistries": [
  { "name": "package.openupm.com",
    "url": "https://package.openupm.com",
    "scopes": [ "com.lootlocker.lootlockersdk" ] }
],
"dependencies": {
  "com.lootlocker.lootlockersdk": "8.1.1",
  "com.unity.nuget.newtonsoft-json": "3.2.2",
  ...
}
```

Unity resolves both the next time the project is opened/focused.

> **Newtonsoft is not optional.** LootLocker's runtime does
> `using Newtonsoft.Json;` but does **not** declare it in its own
> `package.json`, and this project didn't already have it. Without that second
> entry the SDK fails to compile, which takes the whole project down with it.

## 2. LootLocker account + game

1. Sign up at <https://console.lootlocker.com>.
2. Create a game (any name). Pick the **Development** environment while testing.
3. Copy the **Game API Key** from *Settings → API Keys*.

## 3. SDK settings asset

In Unity: **Window → LootLocker → Settings** (or *Edit → Project Settings →
LootLocker*), then paste the API key. This writes
`Assets/LootLockerSDK/Resources/Config/LootLockerConfig.asset`.

- Set **Game Version** to something like `0.1.0.0` — LootLocker rejects
  malformed versions.
- Leave **Development Mode** on until you ship.

## 4. Create the two leaderboards

*Dashboard → Leaderboards → Create.* The keys must match exactly, or
`LootLockerLeaderboardService` will submit into nothing:

| Key                  | Type   | Direction  | What it holds                          |
| -------------------- | ------ | ---------- | -------------------------------------- |
| `best_overall_value` | Player | Descending | Most money held at once during a run   |
| `best_final_value`   | Player | Descending | Money left when the run ended          |

- **Type must be `Player`**, not `Generic`. The code submits with an empty
  `memberId` so LootLocker attributes the score to the session's player; on a
  generic board that would not resolve to a player and names would come back
  empty.
- **Direction must be Descending** — both are "higher is better".

To use different keys, pass them to the constructor:

```csharp
Leaderboards.SetService(new LootLockerLeaderboardService("my_peak", "my_final"));
```

## 5. itch.io / WebGL notes

- **Auth is guest sessions** — no login UI, which is the only practical option
  on itch.io. The identifier LootLocker returns is saved to `PlayerPrefs` and
  replayed on later visits, so a returning browser keeps its leaderboard
  identity instead of creating a new player per page load.
- **Clearing site data resets identity.** That's inherent to guest auth on the
  web, not a bug — the "same player" is really "same browser profile".
- **Player names**: guests have no name until you call
  `Leaderboards.SetPlayerName(...)`. Rows with no name render as `Anonymous`.
  If you want players to enter a name, that's the UI hook (deliberately not
  built here — UI was out of scope).
- **Compression**: itch.io serves WebGL fine with Brotli/gzip; LootLocker's
  requests are plain HTTPS `UnityWebRequest`, so nothing extra is needed.
- No domain allowlisting is required for the Game API Key.

## 6. Where it's wired

Submission happens automatically at the end of a run, from
`PokerGameManager.SubmitRunToLeaderboards()`:

- **Bust** → `CheckBust()` (player out of money)
- **Table decided** → `HumanPlayer.EndGame()` → `PokerGameManager.OnGameEnded()`

Both funnel through one `runSubmitted` guard so a run is only ever recorded
once, whichever way it ends.

## 7. Reading scores (for whoever builds the UI)

```csharp
using GamblersDen.Leaderboards;

// One board
Leaderboards.GetTopBestOverall(10, page => {
    if (!page.Success) { Debug.LogWarning(page.Error); return; }
    foreach (var e in page.Entries)
        Debug.Log($"#{e.Rank} {e.PlayerName} ${e.Value}" + (e.IsLocalPlayer ? " <- you" : ""));
});

// Both boards, one callback, fires after both return
Leaderboards.GetBothTopScores(10, (overall, final) => { /* bind UI */ });

// Set the name this player submits under
Leaderboards.SetPlayerName("Diego", ok => Debug.Log($"name set: {ok}"));

// Optional: open the session at the title screen so the first
// submission isn't also paying for the auth round trip
Leaderboards.Warmup();
```

`page` is never null and `page.Entries` is never null — on failure you get an
empty list plus `page.Error`, so UI code doesn't need null guards.

## 8. Turning it off

```csharp
Leaderboards.SetEnabled(false);   // swaps in NullLeaderboardService
```

No network, no credentials, submissions report success, fetches return empty
pages. Useful for offline testing or if the backend is down.
