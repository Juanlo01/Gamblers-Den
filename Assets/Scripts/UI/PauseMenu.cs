using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause menu / endgame score screen controller.
/// Attach to an object in the gameplay scene that owns references to the
/// pause overlay and the endgame score overlay.
///
/// Wiring:
///   - pauseHideImage: the dimming/backdrop Image shown behind the pause menu.
///   - resumeButton / exitButton: the pause menu's two buttons.
///   In the Inspector, hook ResumeButton's OnClick() -> PauseMenu.CloseMenu(),
///   and ExitButton's OnClick() -> whatever "exit" should do in your game
///   (Application.Quit(), loading a main-menu scene, etc.) — that behavior
///   wasn't specified, so it's left for you to wire up.
///
///   - scoreHideImage: the dimming/backdrop Image shown behind the score screen.
///   - replayButton / scoreExitButton: the score screen's two buttons.
///   - scoreCountLabel: rich-text label showing the final score.
///   Call OpenEndgame() (e.g. from wherever a round/game ends) to lock out
///   the pause menu, freeze gameplay, and reveal the score screen.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image pauseHideImage;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitButton;

    [Header("Score UI References")]
    [SerializeField] private Image scoreHideImage;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button scoreExitButton;
    [SerializeField] private TMP_Text scoreCountLabel;

    private bool isMenuOpen;
    private bool pauseAllowed = true;

    private void Awake()
    {
        // Start closed and hidden, regardless of how it was left in the editor.
        SetElementsVisible(false);
        SetScoreElementsVisible(false);
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

        SetElementsVisible(true);
        Time.timeScale = 0f;
        isMenuOpen = true;
    }

    /// <summary>Hides the pause UI and resumes normal game time.</summary>
    public void CloseMenu()
    {
        if (!isMenuOpen) return;

        SetElementsVisible(false);
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
        OpenScore();
    }

    private void OpenScore()
    {
        SetScoreElementsVisible(true);
    }

    private void SetElementsVisible(bool visible)
    {
        if (pauseHideImage != null) pauseHideImage.gameObject.SetActive(visible);
        if (resumeButton != null) resumeButton.gameObject.SetActive(visible);
        if (exitButton != null) exitButton.gameObject.SetActive(visible);
    }

    private void SetScoreElementsVisible(bool visible)
    {
        if (scoreHideImage != null) scoreHideImage.gameObject.SetActive(visible);
        if (replayButton != null) replayButton.gameObject.SetActive(visible);
        if (scoreExitButton != null) scoreExitButton.gameObject.SetActive(visible);
        if (scoreCountLabel != null) scoreCountLabel.gameObject.SetActive(visible);
    }
}