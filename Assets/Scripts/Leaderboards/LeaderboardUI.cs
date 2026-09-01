using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamblersDen.Leaderboards
{
    /// <summary>
    /// Drives the leaderboard screen: two ranked columns (best overall value and
    /// best end value), a name box, and a one-shot submit.
    ///
    /// Wiring (all Inspector, no code changes needed):
    ///   - The four label arrays are fixed at 10 entries, index 0 = rank 1.
    ///     Assign the name column and the money column for each board.
    ///   - nameInput: a TMP_InputField. Its character limit and content type are
    ///     forced in Awake, so it does not matter how it is configured in the
    ///     Inspector.
    ///   - submitButton: enabled only once the typed name is long enough.
    ///   - scoreMenuGroup / leaderboardGroup: the two CanvasGroups swapped by
    ///     OpenLeaderboard() and BackToScoreMenu().
    ///
    /// Button hookups:
    ///   ScoreMenu's "Leaderboard" button -> LeaderboardUI.OpenLeaderboard()
    ///   Leaderboard's "Back" button      -> LeaderboardUI.BackToScoreMenu()
    ///   Leaderboard's "Submit" button    -> LeaderboardUI.SubmitUser()
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        /// <summary>How many ranks this screen shows. The arrays are sized to match.</summary>
        public const int RankCount = 10;

        /// <summary>Shortest name that may be submitted.</summary>
        public const int MinNameLength = 3;

        [Header("Best Overall Value - rank 1 at index 0")]
        [SerializeField] private TMP_Text[] overallNames = new TMP_Text[RankCount];
        [SerializeField] private TMP_Text[] overallMoney = new TMP_Text[RankCount];

        [Header("Best End Value - rank 1 at index 0")]
        [SerializeField] private TMP_Text[] endNames = new TMP_Text[RankCount];
        [SerializeField] private TMP_Text[] endMoney = new TMP_Text[RankCount];

        [Header("Name Entry")]
        [Tooltip("Where the player types their name, if using a TMP Input Field. " +
                 "Limited to 12 characters. Assign this OR Legacy Name Input, not both.")]
        [SerializeField] private TMP_InputField nameInput;

        [Tooltip("Use instead of Name Input if the field is a legacy UI Input Field. " +
                 "Note a legacy Input Field can only render into a legacy UI Text - " +
                 "it cannot drive a TextMeshPro label.")]
        [SerializeField] private InputField legacyNameInput;

        [Tooltip("Only interactable once the typed name is at least 3 characters.")]
        [SerializeField] private Button submitButton;

        [Tooltip("Optional: shows why submit is unavailable, and the result afterwards.")]
        [SerializeField] private TMP_Text statusLabel;

        [Header("Modal Switching")]
        [Tooltip("CanvasGroup on the ScoreMenu panel.")]
        [SerializeField] private CanvasGroup scoreMenuGroup;

        [Tooltip("CanvasGroup on this leaderboard panel.")]
        [SerializeField] private CanvasGroup leaderboardGroup;

        [Header("Display")]
        [Tooltip("Shown in a rank slot that has no entry yet.")]
        [SerializeField] private string emptyNameText = "---";

        [SerializeField] private string emptyMoneyText = "";

        [Tooltip("Refresh both boards automatically when the screen opens.")]
        [SerializeField] private bool refreshOnOpen = true;

        /// <summary>
        /// The leaderboard backend this screen talks to. Resolved from the
        /// Leaderboards facade rather than dragged in, because the LootLocker
        /// handler is a plain C# object (not a MonoBehaviour) and so cannot be
        /// an Inspector reference. Assign SetService() before Awake to override,
        /// which is what tests and the offline stub do.
        /// </summary>
        public ILeaderboardService Service
        {
            get => _service ?? (_service = Leaderboards.Service);
            private set => _service = value;
        }

        private ILeaderboardService _service;

        /// <summary>Replaces the backend, e.g. with NullLeaderboardService.</summary>
        public void SetService(ILeaderboardService service) => Service = service;

        /// <summary>True once SubmitUser has posted - it will not run twice.</summary>
        public bool HasSubmitted { get; private set; }

        // Score at rank 10 on each board as of the last feed, and how many rows
        // that board actually held. Together these answer "would this score have
        // made the table?" without a second round trip - see MadeTheTable.
        private int _overallCutoff;
        private int _overallCount;
        private int _endCutoff;
        private int _endCount;

        // Drive both panels through ModalGroup so a hidden one cannot be clicked
        // through - see ModalGroup for why blocksRaycasts alone is not enough.
        private ModalGroup _scoreMenu;
        private ModalGroup _leaderboard;

        private void Awake()
        {
            // Built before anything is hidden: ModalGroup records each child's
            // authored raycastTarget at construction, so constructing it after a
            // hide would bake in the hidden state and the panel would never
            // become clickable again.
            _scoreMenu = ModalGroup.For(scoreMenuGroup);
            _leaderboard = ModalGroup.For(leaderboardGroup);

            // Force the input rules here rather than trusting the Inspector, so
            // the 12-character cap cannot be lost by someone editing the field.
            // Either input flavour is accepted; whichever is assigned wins.
            if (nameInput != null)
            {
                nameInput.characterLimit = LeaderboardRun.MaxNameLength;
                nameInput.contentType = TMP_InputField.ContentType.Standard;
                nameInput.lineType = TMP_InputField.LineType.SingleLine;
                nameInput.onValueChanged.AddListener(OnNameChanged);
            }

            if (legacyNameInput != null)
            {
                legacyNameInput.characterLimit = LeaderboardRun.MaxNameLength;
                legacyNameInput.contentType = InputField.ContentType.Standard;
                legacyNameInput.lineType = InputField.LineType.SingleLine;
                legacyNameInput.onValueChanged.AddListener(OnNameChanged);

                // A legacy Input Field renders through a legacy UI Text. Without
                // one it silently accepts nothing - no caret, no characters - so
                // say so rather than leaving a dead box.
                if (legacyNameInput.textComponent == null)
                {
                    Debug.LogWarning(
                        $"[Leaderboards] '{legacyNameInput.name}' is a legacy Input Field with no Text " +
                        "Component assigned, so typing into it will do nothing. A legacy Input Field " +
                        "cannot use a TextMeshPro label - either give it a legacy UI Text, or replace " +
                        "the component with a TMP Input Field and assign it to Name Input.",
                        legacyNameInput);
                }
            }

            if (nameInput == null && legacyNameInput == null)
            {
                Debug.LogWarning("[Leaderboards] No name input assigned - submitting will be blocked.", this);
            }
            else if (nameInput != null && legacyNameInput != null)
            {
                Debug.LogWarning(
                    "[Leaderboards] Both Name Input and Legacy Name Input are assigned; " +
                    "the TMP one is used and the legacy one ignored.", this);
            }

            // This screen starts closed - the ScoreMenu opens it.
            _leaderboard.Hide();

            ClearAllRows();
            RefreshSubmitInteractable();
        }

        private void OnDestroy()
        {
            if (nameInput != null)
            {
                nameInput.onValueChanged.RemoveListener(OnNameChanged);
            }

            if (legacyNameInput != null)
            {
                legacyNameInput.onValueChanged.RemoveListener(OnNameChanged);
            }
        }

        // ---------------- Modal switching ----------------

        /// <summary>
        /// ScoreMenu -> Leaderboard. Hides the score menu and shows this screen,
        /// refreshing both boards if refreshOnOpen is set.
        /// </summary>
        public void OpenLeaderboard()
        {
            _scoreMenu.Hide();
            _leaderboard.Show();

            if (refreshOnOpen)
            {
                FeedTopOveralls();
                FeedTopBests();
            }

            RefreshSubmitInteractable();
        }

        /// <summary>Leaderboard -> ScoreMenu. The Back button.</summary>
        public void BackToScoreMenu()
        {
            _leaderboard.Hide();
            _scoreMenu.Show();
        }

        // ---------------- Feeding the tables ----------------

        /// <summary>
        /// Fills the "best overall value" column with the top 10, ranked by the
        /// most money the player ever held during a run.
        /// </summary>
        public void FeedTopOveralls()
        {
            Feed(LeaderboardCategory.BestOverall, overallNames, overallMoney,
                (cutoff, count) => { _overallCutoff = cutoff; _overallCount = count; });
        }

        /// <summary>
        /// Fills the "best end value" column with the top 10, ranked by the money
        /// the player finished a run with.
        /// </summary>
        public void FeedTopBests()
        {
            Feed(LeaderboardCategory.BestFinal, endNames, endMoney,
                (cutoff, count) => { _endCutoff = cutoff; _endCount = count; });
        }

        private void Feed(LeaderboardCategory category, TMP_Text[] nameLabels, TMP_Text[] moneyLabels,
            Action<int, int> recordCutoff)
        {
            Service.GetTopScores(category, RankCount, page =>
            {
                if (page == null || !page.Success)
                {
                    var reason = page?.Error ?? "unknown error";
                    Debug.LogWarning($"[Leaderboards] Could not load {category}: {reason}");
                    SetStatus($"Could not load scores: {reason}");
                    ClearRows(nameLabels, moneyLabels);
                    recordCutoff(0, 0);
                    return;
                }

                var entries = page.Entries;
                for (int i = 0; i < RankCount; i++)
                {
                    if (i < entries.Count)
                    {
                        SetLabel(nameLabels, i, entries[i].DisplayName);
                        SetLabel(moneyLabels, i, FormatMoney(entries[i].Value));
                    }
                    else
                    {
                        SetLabel(nameLabels, i, emptyNameText);
                        SetLabel(moneyLabels, i, emptyMoneyText);
                    }
                }

                // Score to beat = the last row shown. With a short board there is
                // nothing to beat, which MadeTheTable treats as an automatic yes.
                var cutoff = entries.Count > 0 ? entries[entries.Count - 1].Value : 0;
                recordCutoff(cutoff, entries.Count);
            });
        }

        // ---------------- Submitting ----------------

        /// <summary>
        /// Posts the finished run's two scores under the typed name, then
        /// refreshes whichever board the run actually placed on. One-shot: after
        /// a successful post it disables itself so a score cannot be filed twice.
        /// </summary>
        public void SubmitUser()
        {
            if (HasSubmitted)
            {
                Debug.Log("[Leaderboards] Submit ignored - this run has already been posted.");
                return;
            }

            var name = CurrentName();
            if (!IsNameValid(name))
            {
                SetStatus($"Enter at least {MinNameLength} characters.");
                return;
            }

            var manager = PokerGameManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[Leaderboards] No PokerGameManager - nothing to submit.");
                SetStatus("No run to submit.");
                return;
            }

            var best = manager.GetBestScore();
            var end = manager.GetFinalScore();

            // Claim the one-shot up front: without this, double-clicking submit
            // fires two posts before the first response lands.
            HasSubmitted = true;
            SetInteractable(false);
            SetStatus("Submitting...");

            manager.SetSubmittedPlayerName(name);

            Debug.Log($"[Leaderboards] Submitting '{name}' - best ${best}, end ${end}.");
            Service.SubmitRun(name, best, end, (ok, run) =>
            {
                if (!ok)
                {
                    // Let them try again - the failure was the network, not the input.
                    HasSubmitted = false;
                    SetInteractable(true);
                    SetStatus("Could not submit. Check your connection and try again.");
                    Debug.LogWarning("[Leaderboards] Submit failed.");
                    return;
                }

                SetStatus($"Submitted as {run.DisplayName}.");
                Debug.Log($"[Leaderboards] Submitted as {run}.");

                // Only re-read a board the run could actually have landed on.
                if (MadeTheTable(best, _overallCutoff, _overallCount))
                {
                    Debug.Log("[Leaderboards] Run placed on BEST OVERALL - refreshing.");
                    FeedTopOveralls();
                }

                if (MadeTheTable(end, _endCutoff, _endCount))
                {
                    Debug.Log("[Leaderboards] Run placed on BEST END - refreshing.");
                    FeedTopBests();
                }
            });
        }

        /// <summary>
        /// Whether a score would appear in the top 10. True when the board is not
        /// yet full (there is a free slot regardless of value) or the score beats
        /// the lowest one shown. Ties count as making it: equal scores can
        /// reorder the table, so it is worth re-reading.
        /// </summary>
        private static bool MadeTheTable(int score, int cutoff, int shownCount)
        {
            return shownCount < RankCount || score >= cutoff;
        }

        // ---------------- Name entry ----------------

        private void OnNameChanged(string _) => RefreshSubmitInteractable();

        /// <summary>Typed name from whichever input flavour is wired up.</summary>
        private string CurrentName()
        {
            if (nameInput != null) return (nameInput.text ?? string.Empty).Trim();
            if (legacyNameInput != null) return (legacyNameInput.text ?? string.Empty).Trim();
            return string.Empty;
        }

        /// <summary>At least MinNameLength characters, once padding is ignored.</summary>
        public static bool IsNameValid(string name) =>
            !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= MinNameLength;

        private void RefreshSubmitInteractable()
        {
            if (HasSubmitted)
            {
                SetInteractable(false);
                return;
            }

            SetInteractable(IsNameValid(CurrentName()));
        }

        private void SetInteractable(bool value)
        {
            if (submitButton != null) submitButton.interactable = value;

            // The box stays usable until the run is actually filed - it is only
            // the submit button that gates on name length.
            if (nameInput != null) nameInput.interactable = !HasSubmitted;
            if (legacyNameInput != null) legacyNameInput.interactable = !HasSubmitted;
        }

        // ---------------- Helpers ----------------

        private void SetStatus(string message)
        {
            if (statusLabel != null) statusLabel.text = message;
        }

        private static string FormatMoney(int amount) => "$" + amount.ToString("N0");

        private static void SetLabel(TMP_Text[] labels, int index, string value)
        {
            if (labels != null && index < labels.Length && labels[index] != null)
            {
                labels[index].text = value;
            }
        }

        private void ClearAllRows()
        {
            ClearRows(overallNames, overallMoney);
            ClearRows(endNames, endMoney);
        }

        private void ClearRows(TMP_Text[] nameLabels, TMP_Text[] moneyLabels)
        {
            for (int i = 0; i < RankCount; i++)
            {
                SetLabel(nameLabels, i, emptyNameText);
                SetLabel(moneyLabels, i, emptyMoneyText);
            }
        }
    }
}
