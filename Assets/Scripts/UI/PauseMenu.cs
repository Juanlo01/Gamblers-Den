using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause menu / endgame score screen controller.
/// Attach to an object in the gameplay scene that owns references to the
/// pause overlay and the endgame score overlay.
///
/// Wiring:
///   - pauseCanvasGroup: the CanvasGroup on the pause menu panel (backdrop,
///     resume/exit buttons, etc). Its buttons' OnClick() should be wired in
///     the Inspector — e.g. ResumeButton -> PauseMenu.CloseMenu(), and
///     ExitButton -> whatever "exit" should do in your game
///     (Application.Quit(), loading a main-menu scene, etc.) — that behavior
///     wasn't specified, so it's left for you to wire up.
///
///   - scoreCanvasGroup: the CanvasGroup on the score screen panel (backdrop,
///     replay/exit buttons, final-score label, etc).
///   Call OpenEndgame() (e.g. from wherever a round/game ends) to lock out
///   the pause menu, freeze gameplay, and reveal the score screen.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup pauseCanvasGroup;
    [SerializeField] private Image pauseHideImage;

    [Header("Score UI References")]
    [SerializeField] private CanvasGroup scoreCanvasGroup;
    [SerializeField] private Image scoreHideImage;

    private bool isMenuOpen;
    private bool pauseAllowed = true;

    // ModalGroup also switches every child Graphic's raycastTarget, so a hidden
    // panel cannot be clicked through even if a child overrides the CanvasGroup.
    private ModalGroup pauseModal;
    private ModalGroup scoreModal;

    private void Awake()
    {
        // Resolve BEFORE hiding: ModalGroup captures each child's authored
        // raycastTarget on first use, and capturing that after a hide would
        // record the hidden state as the original.
        pauseModal = ModalGroup.For(pauseCanvasGroup);
        scoreModal = ModalGroup.For(scoreCanvasGroup);

        // Start closed and hidden, regardless of how it was left in the editor.
        SetGroupVisible(pauseModal, pauseHideImage, false);
        SetGroupVisible(scoreModal, scoreHideImage, false);
    }

    private void Update()
    {
        // Legacy UnityEngine.Input, consistent with the SceneLoader script.
        if (pauseAllowed && Input.GetKeyDown(KeyCode.Escape) && !isMenuOpen)
        {
            OpenMenu();
        }
    }

    /// <summary>Shows the pause UI and freezes gameplay time.</summary>
    public void OpenMenu()
    {
        if (!pauseAllowed || isMenuOpen) return;

        SetGroupVisible(pauseModal, pauseHideImage, true);
        Time.timeScale = 0f;
        isMenuOpen = true;
    }

    /// <summary>Hides the pause UI and resumes normal game time.</summary>
    public void CloseMenu()
    {
        if (!isMenuOpen) return;

        SetGroupVisible(pauseModal, pauseHideImage, false);
        Time.timeScale = 1f;
        isMenuOpen = false;
    }

    /// <summary>
    /// Ends the round: locks out the pause menu, freezes gameplay time, and
    /// reveals the score screen (Replay / Exit / final score).
    /// </summary>
    public void OpenEndgame()
    {
        pauseAllowed = false;
        Time.timeScale = 0f;
        SetGroupVisible(scoreModal, scoreHideImage, true);
    }

    // hideImage is handled separately from the ModalGroup because it may sit
    // OUTSIDE the CanvasGroup (it is a backdrop). When it is inside, ModalGroup
    // already covers it and this just sets the same value again - harmless.
    private static void SetGroupVisible(ModalGroup modal, Image hideImage, bool visible)
    {
        modal?.SetVisible(visible);

        if (hideImage != null)
        {
            hideImage.raycastTarget = visible;
        }
    }
}
