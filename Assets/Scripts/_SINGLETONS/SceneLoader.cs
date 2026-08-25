using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent scene-loader singleton.
/// Drop this prefab into every scene as a safety net — only the first instance
/// loaded will survive (DontDestroyOnLoad); any duplicate spawned when a later
/// scene loads destroys itself in Awake().
///
/// Wiring:
///   - loadHideImage: full-screen Image used as a fade curtain (alpha 0<->1).
///   - loadSymbImage: the spinner Image, rotated on its RectTransform's Z axis.
///   Both should live under the same prefab root as this script so they persist
///   together with it.
///
/// Usage:
///   SceneLoader.Instance.ChangeScene("SceneA");
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    private enum LoaderState
    {
        Idle,
        ClosingFade,    // LoadHide fading 0 -> 1
        Changing,       // scene load in flight, LoadSymb spinning indefinitely
        OpeningRotate,  // LoadSymb easing back to Z = 0
        OpeningFade     // LoadHide fading 1 -> 0
    }

    [Header("UI References")]
    [SerializeField] private Image loadHideImage;
    [SerializeField] private Image loadSymbImage;

    [Header("Timings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float spinSpeedDegPerSec = 220f;
    [SerializeField] private float rotateBackDuration = 0.35f;

    /// <summary>True whenever closing, loading, or opening — i.e. not Idle.</summary>
    public bool IsTransitioning => state != LoaderState.Idle;

    private LoaderState state = LoaderState.Idle;
    private float rotateBackTimer;
    private float rotateBackStartAngle;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Start fully hidden / idle regardless of how it was left in the editor.
        if (loadHideImage != null)
        {
            Color c = loadHideImage.color;
            c.a = 0f;
            loadHideImage.color = c;
        }

        if (loadSymbImage != null)
        {
            loadSymbImage.enabled = false;
        }
    }

    private void Update()
    {
        UpdateSymbVisibility();

        switch (state)
        {
            case LoaderState.ClosingFade:
                if (loadHideImage == null)
                {
                    state = LoaderState.Changing;
                    break;
                }
                TickFadeTo(1f);
                if (Mathf.Approximately(loadHideImage.color.a, 1f))
                    state = LoaderState.Changing;
                break;

            case LoaderState.Changing:
                TickIndefiniteSpin();
                break;

            case LoaderState.OpeningRotate:
                TickRotateBack();
                break;

            case LoaderState.OpeningFade:
                if (loadHideImage == null)
                {
                    state = LoaderState.Idle;
                    break;
                }
                TickFadeTo(0f);
                if (Mathf.Approximately(loadHideImage.color.a, 0f))
                    state = LoaderState.Idle;
                break;

            case LoaderState.Idle:
                TickDebugInput();
                break;
        }
    }

    /// <summary>
    /// LoadSymb is only visible while actively closing or opening — hidden while
    /// idle, and hidden while "Changing" (the scene load itself is in flight).
    /// </summary>
    private void UpdateSymbVisibility()
    {
        if (loadSymbImage == null) return;

        bool shouldBeVisible = state == LoaderState.ClosingFade
                             || state == LoaderState.OpeningRotate
                             || state == LoaderState.OpeningFade;

        if (loadSymbImage.enabled != shouldBeVisible)
            loadSymbImage.enabled = shouldBeVisible;
    }

    // ---------------- Public API ----------------

    /// <summary>Entry point: Close -> load "sceneName" -> Open. Ignored if already busy.</summary>
    public void ChangeScene(string sceneName)
    {
        if (state != LoaderState.Idle) return;
        StartCoroutine(SceneChangeRoutine(sceneName));
    }

    /// <summary>
    /// Runs first, before any scene-change logic: fades LoadHide 0 -> 1, then
    /// (once fully faded) begins spinning LoadSymb indefinitely from Z = 0.
    /// The actual per-frame tweening is done in Update(), not here.
    /// </summary>
    public void CloseScene()
    {
        if (loadSymbImage != null)
        {
            Vector3 e = loadSymbImage.rectTransform.localEulerAngles;
            e.z = 0f;
            loadSymbImage.rectTransform.localEulerAngles = e;
        }

        state = LoaderState.ClosingFade;
    }

    /// <summary>
    /// Runs right after the scene-change logic completes: eases LoadSymb back to
    /// Z = 0, then fades LoadHide 1 -> 0. Per-frame tweening is done in Update().
    /// </summary>
    public void OpenScene()
    {
        float currentZ = loadSymbImage != null
            ? NormalizeAngle(loadSymbImage.rectTransform.localEulerAngles.z)
            : 0f;

        rotateBackStartAngle = currentZ;
        rotateBackTimer = 0f;
        state = LoaderState.OpeningRotate;
    }

    // ---------------- Internal flow ----------------

    private IEnumerator SceneChangeRoutine(string sceneName)
    {
        CloseScene();

        // Wait for the close animation (fade-in) to finish — the spin state is
        // set by Update() the instant the fade reaches 1, so this also marks
        // the point where the indefinite spin has begun.
        yield return new WaitUntil(() => state == LoaderState.Changing);

        // Resource + scene loading. The spin keeps animating in Update() the
        // whole time this runs, uninterrupted.
        Resources.UnloadUnusedAssets();

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (asyncLoad != null && !asyncLoad.isDone)
            yield return null;

        OpenScene();

        // Wait for the open animation (rotate-back + fade-out) to finish.
        yield return new WaitUntil(() => state == LoaderState.Idle);
    }

    // ---------------- Animation ticks (Update-driven, never interrupted) ----------------

    private void TickFadeTo(float target)
    {
        Color c = loadHideImage.color;
        c.a = Mathf.MoveTowards(c.a, target, Time.deltaTime / fadeDuration);
        loadHideImage.color = c;
    }

    private void TickIndefiniteSpin()
    {
        if (loadSymbImage == null) return;

        Vector3 e = loadSymbImage.rectTransform.localEulerAngles;
        e.z += spinSpeedDegPerSec * Time.deltaTime; // flip sign for the opposite direction
        loadSymbImage.rectTransform.localEulerAngles = e;
    }

    private void TickRotateBack()
    {
        if (loadSymbImage == null)
        {
            state = LoaderState.OpeningFade;
            return;
        }

        rotateBackTimer += Time.deltaTime;
        float t = Mathf.Clamp01(rotateBackTimer / rotateBackDuration);
        float z = Mathf.LerpAngle(rotateBackStartAngle, 0f, t);

        Vector3 e = loadSymbImage.rectTransform.localEulerAngles;
        e.z = z;
        loadSymbImage.rectTransform.localEulerAngles = e;

        if (t >= 1f)
        {
            e.z = 0f;
            loadSymbImage.rectTransform.localEulerAngles = e;
            state = LoaderState.OpeningFade;
        }
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    // ---------------- Debug scene switching ----------------
    // Legacy UnityEngine.Input class only — no Input System package involved.

    private void TickDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChangeScene("SceneA");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChangeScene("SceneB");
        }
    }
}