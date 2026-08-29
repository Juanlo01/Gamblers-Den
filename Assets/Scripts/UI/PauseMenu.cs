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

    private void Awake()
    {
        // Start closed and hidden, regardless of how it was left in the editor.
        SetGroupVisible(pauseCanvasGroup, pauseHideImage, false);
        SetGroupVisible(scoreCanvasGroup, scoreHideImage, false);
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

        SetGroupVisible(pauseCanvasGroup, pauseHideImage, true);
        Time.timeScale = 0f;
        isMenuOpen = true;
    }

    /// <summary>Hides the pause UI and resumes normal game time.</summary>
    public void CloseMenu()
    {
        if (!isMenuOpen) return;

        SetGroupVisible(pauseCanvasGroup, pauseHideImage, false);
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
        SetGroupVisible(scoreCanvasGroup, scoreHideImage, true);
    }

    private static void SetGroupVisible(CanvasGroup group, Image hideImage, bool visible)
    {
        if (group != null)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        if (hideImage != null)
        {
            hideImage.raycastTarget = visible;
        }
    }
}
