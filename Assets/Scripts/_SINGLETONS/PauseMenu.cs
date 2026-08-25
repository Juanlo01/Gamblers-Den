using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause menu controller.
/// Attach to an object in the gameplay scene that owns references to the
/// pause overlay and its two buttons.
///
/// Wiring:
///   - pauseHideImage: the dimming/backdrop Image shown behind the menu.
///   - resumeButton / exitButton: the two menu buttons.
///   In the Inspector, hook ResumeButton's OnClick() -> PauseMenu.CloseMenu(),
///   and ExitButton's OnClick() -> whatever "exit" should do in your game
///   (Application.Quit(), loading a main-menu scene, etc.) — that behavior
///   wasn't specified, so it's left for you to wire up.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image pauseHideImage;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitButton;

    private bool isMenuOpen;

    private void Awake()
    {
        // Start closed and hidden, regardless of how it was left in the editor.
        SetElementsVisible(false);
    }

    private void Update()
    {
        // Legacy UnityEngine.Input, consistent with the SceneLoader script.
        if (Input.GetKeyDown(KeyCode.Escape) && !isMenuOpen)
        {
            OpenMenu();
        }
    }

    /// <summary>Shows the pause UI and freezes gameplay time.</summary>
    public void OpenMenu()
    {
        if (isMenuOpen) return;

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

    private void SetElementsVisible(bool visible)
    {
        if (pauseHideImage != null) pauseHideImage.gameObject.SetActive(visible);
        if (resumeButton != null) resumeButton.gameObject.SetActive(visible);
        if (exitButton != null) exitButton.gameObject.SetActive(visible);
    }
}