using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Opens and closes the three CPU dialogue displays by animating their
/// horizontal ANCHORS (never offsets or pivots), so each box grows to exactly
/// the width its text needs.
///
/// Each box opens from a different edge:
///   CPU 1 - grows rightward:      min = 0,          max = w
///   CPU 2 - grows from centre:    min = 0.5 - w/2,  max = 0.5 + w/2
///   CPU 3 - grows leftward:       min = 1 - w,      max = 1
/// and closes to zero width at that same edge (0/0, 0.5/0.5, 1/1).
///
/// "w" is the required width expressed as a fraction of the box's PARENT rect,
/// because anchors are always parent-relative. Note the Speaker parents span
/// only part of the canvas, so this fraction is not a screen fraction.
///
/// Each CPU's CanvasGroup is cross-faded over the same duration, and its
/// blocksRaycasts is tied to visibility so a faded-out speaker cannot sit over
/// the table swallowing clicks.
/// </summary>
public class DialogueRunner : MonoBehaviour
{
    [Header("CPU 1")]
    [SerializeField] private RectTransform CPU1_Textbox;
    [SerializeField] private RectTransform CPU1_Namebox;

    [Tooltip("Faded in/out alongside the anchors. Left empty, the CanvasGroup above the boxes is found automatically.")]
    [SerializeField] private CanvasGroup CPU1_Group;

    [Header("CPU 2")]
    [SerializeField] private RectTransform CPU2_Textbox;
    [SerializeField] private RectTransform CPU2_Namebox;

    [SerializeField] private CanvasGroup CPU2_Group;

    [Header("CPU 3")]
    [SerializeField] private RectTransform CPU3_Textbox;
    [SerializeField] private RectTransform CPU3_Namebox;

    [SerializeField] private CanvasGroup CPU3_Group;

    [Header("Animation")]
    [SerializeField] private float openDuration = 0.25f;
    [SerializeField] private float closeDuration = 0.18f;

    [Tooltip("Eased 0->1 progress. Defaults to EaseInOut when left empty.")]
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Sizing")]
    [Tooltip("Extra pixels added around the measured text. The textbox's TMP child is inset 64px, so its padding should cover that.")]
    [SerializeField] private float textboxPadding = 72f;

    [SerializeField] private float nameboxPadding = 48f;

    [Tooltip("Upper bound on the opened width, as a fraction of the parent rect.")]
    [SerializeField] private float maxWidthFraction = 1f;

    // Cached TMP found under each box, used to measure the required width with
    // that box's own font settings.
    private readonly System.Collections.Generic.Dictionary<RectTransform, TMP_Text> _labels =
        new System.Collections.Generic.Dictionary<RectTransform, TMP_Text>();

    private readonly System.Collections.Generic.Dictionary<RectTransform, Coroutine> _running =
        new System.Collections.Generic.Dictionary<RectTransform, Coroutine>();

    private readonly System.Collections.Generic.Dictionary<CanvasGroup, Coroutine> _fading =
        new System.Collections.Generic.Dictionary<CanvasGroup, Coroutine>();

    private void Awake()
    {
        // Start every box fully closed and transparent so nothing shows, and
        // nothing catches clicks, before a CPU speaks.
        for (int cpu = 1; cpu <= 3; cpu++)
        {
            GetClosedAnchors(cpu, out float min, out float max);
            ApplyAnchorX(GetTextbox(cpu), min, max);
            ApplyAnchorX(GetNamebox(cpu), min, max);

            var group = ResolveGroup(cpu);
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
        }
    }

    /// <summary>
    /// Returns the CanvasGroup for a CPU, falling back to the nearest one above
    /// its boxes so the field can be left empty in the Inspector.
    /// </summary>
    private CanvasGroup ResolveGroup(int cpuIndex)
    {
        CanvasGroup assigned;
        switch (cpuIndex)
        {
            case 1: assigned = CPU1_Group; break;
            case 2: assigned = CPU2_Group; break;
            case 3: assigned = CPU3_Group; break;
            default: return null;
        }

        if (assigned != null)
        {
            return assigned;
        }

        var box = GetTextbox(cpuIndex) ?? GetNamebox(cpuIndex);
        if (box == null)
        {
            return null;
        }

        var found = box.GetComponentInParent<CanvasGroup>();
        switch (cpuIndex)
        {
            case 1: CPU1_Group = found; break;
            case 2: CPU2_Group = found; break;
            case 3: CPU3_Group = found; break;
        }

        return found;
    }

    /// <summary>
    /// Opens CPU <paramref name="cpuIndex"/>'s (1-3) name and text boxes, each
    /// sized to fit the string it is given.
    /// </summary>
    public void OpenDialogue(int cpuIndex, string speakerName, string text)
    {
        Debug.Log($"[DialogueRunner] OpenDialogue(cpu={cpuIndex}, name=\"{speakerName}\", text=\"{text}\")", this);

        var textbox = GetTextbox(cpuIndex);
        var namebox = GetNamebox(cpuIndex);

        if (textbox == null && namebox == null)
        {
            Debug.LogWarning($"[DialogueRunner] ABORT: no boxes assigned for CPU {cpuIndex}.", this);
            return;
        }

        if (textbox == null) Debug.LogWarning($"[DialogueRunner] CPU {cpuIndex} has no Textbox assigned.", this);
        if (namebox == null) Debug.LogWarning($"[DialogueRunner] CPU {cpuIndex} has no Namebox assigned.", this);

        WarnIfInvisible(cpuIndex, textbox ?? namebox);

        AnimateTo(textbox, cpuIndex, WidthFractionFor(textbox, text, textboxPadding, "textbox"), openDuration);
        AnimateTo(namebox, cpuIndex, WidthFractionFor(namebox, speakerName, nameboxPadding, "namebox"), openDuration);
        FadeTo(cpuIndex, 1f, openDuration);
    }

    /// <summary>Retracts CPU <paramref name="cpuIndex"/>'s (1-3) boxes to zero width.</summary>
    public void CloseDialogue(int cpuIndex)
    {
        Debug.Log($"[DialogueRunner] CloseDialogue(cpu={cpuIndex})", this);

        AnimateTo(GetTextbox(cpuIndex), cpuIndex, 0f, closeDuration);
        AnimateTo(GetNamebox(cpuIndex), cpuIndex, 0f, closeDuration);
        FadeTo(cpuIndex, 0f, closeDuration);
    }

    // ---------------- Fading ----------------

    private void FadeTo(int cpuIndex, float targetAlpha, float duration)
    {
        var group = ResolveGroup(cpuIndex);
        if (group == null)
        {
            Debug.LogWarning(
                $"[DialogueRunner] CPU {cpuIndex} has no CanvasGroup to fade; "
                + "assign one or add it above the boxes.", this);
            return;
        }

        Debug.Log(
            $"[DialogueRunner]   fading '{group.name}': alpha {group.alpha:F2}->{targetAlpha:F2} over {duration}s",
            group);

        if (_fading.TryGetValue(group, out var existing) && existing != null)
        {
            StopCoroutine(existing);
        }

        // Opening takes clicks immediately; closing keeps them until it is gone.
        group.blocksRaycasts = targetAlpha > 0f;

        if (!isActiveAndEnabled || duration <= 0f)
        {
            group.alpha = targetAlpha;
            _fading[group] = null;
            return;
        }

        _fading[group] = StartCoroutine(FadeRoutine(group, targetAlpha, duration));
    }

    private IEnumerator FadeRoutine(CanvasGroup group, float targetAlpha, float duration)
    {
        float startAlpha = group.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = easing.Evaluate(Mathf.Clamp01(elapsed / duration));
            group.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, t);
            yield return null;
        }

        group.alpha = targetAlpha;
        group.blocksRaycasts = targetAlpha > 0f;
        _fading[group] = null;
    }

    /// <summary>
    /// The anchors can animate perfectly and still show nothing if a parent
    /// CanvasGroup is transparent or an object in the chain is inactive, so
    /// report that rather than leaving it looking like the calls never ran.
    /// </summary>
    private void WarnIfInvisible(int cpuIndex, RectTransform box)
    {
        if (box == null)
        {
            return;
        }

        // The group this CPU owns is faded up by FadeTo, so its alpha starting
        // at 0 is expected; only report groups nothing here is driving.
        var driven = ResolveGroup(cpuIndex);

        for (Transform t = box; t != null; t = t.parent)
        {
            if (!t.gameObject.activeSelf)
            {
                Debug.LogWarning(
                    $"[DialogueRunner] CPU {cpuIndex} will not be visible: '{t.name}' is inactive.", t);
            }

            var group = t.GetComponent<CanvasGroup>();
            if (group != null && group != driven && group.alpha <= 0f)
            {
                Debug.LogWarning(
                    $"[DialogueRunner] CPU {cpuIndex} will not be visible: CanvasGroup on '{t.name}' "
                    + $"has alpha {group.alpha} and is not the group being faded.", t);
            }
        }
    }

    // ---------------- Sizing ----------------

    /// <summary>
    /// Width needed to fit <paramref name="content"/>, as a fraction of the
    /// box's parent rect. Returns 0 for empty content so the box stays shut.
    /// </summary>
    private float WidthFractionFor(RectTransform box, string content, float padding, string what)
    {
        if (box == null)
        {
            return 0f;
        }

        if (string.IsNullOrEmpty(content))
        {
            Debug.LogWarning($"[DialogueRunner] {what} '{box.name}' got empty content; staying closed.", box);
            return 0f;
        }

        var parent = box.parent as RectTransform;
        float parentWidth = parent != null ? parent.rect.width : 0f;
        if (parentWidth <= 0f)
        {
            Debug.LogWarning(
                $"[DialogueRunner] {what} '{box.name}' parent width is {parentWidth} "
                + "(parent missing or not laid out yet); staying closed.", box);
            return 0f;
        }

        var label = GetLabel(box);
        if (label == null)
        {
            Debug.LogWarning($"[DialogueRunner] {what} '{box.name}' has no TMP_Text child to measure with.", box);
            return 0f;
        }

        float textWidth = label.GetPreferredValues(content).x;
        float fraction = Mathf.Clamp((textWidth + padding) / parentWidth, 0f, Mathf.Max(0f, maxWidthFraction));

        Debug.Log(
            $"[DialogueRunner]   {what} '{box.name}': text={textWidth:F1}px + pad={padding} "
            + $"/ parent={parentWidth:F1}px -> fraction={fraction:F3}", box);

        return fraction;
    }

    private TMP_Text GetLabel(RectTransform box)
    {
        if (!_labels.TryGetValue(box, out var label))
        {
            label = box.GetComponentInChildren<TMP_Text>(true);
            _labels[box] = label;
        }

        return label;
    }

    // ---------------- Anchor maths ----------------

    private static void GetOpenAnchors(int cpuIndex, float width, out float min, out float max)
    {
        switch (cpuIndex)
        {
            case 1: // grows rightward off the left edge
                min = 0f;
                max = width;
                break;

            case 2: // grows outward from the centre in both directions
                min = 0.5f - (width / 2f);
                max = 0.5f + (width / 2f);
                break;

            default: // CPU 3 - grows leftward off the right edge
                min = 1f - width;
                max = 1f;
                break;
        }
    }

    private static void GetClosedAnchors(int cpuIndex, out float min, out float max)
    {
        GetOpenAnchors(cpuIndex, 0f, out min, out max);
    }

    private void AnimateTo(RectTransform box, int cpuIndex, float width, float duration)
    {
        if (box == null)
        {
            return;
        }

        GetOpenAnchors(cpuIndex, width, out float targetMin, out float targetMax);

        Debug.Log(
            $"[DialogueRunner]   animating '{box.name}': anchorX {box.anchorMin.x:F3}->{targetMin:F3} .. "
            + $"{box.anchorMax.x:F3}->{targetMax:F3} over {duration}s", box);

        if (_running.TryGetValue(box, out var existing) && existing != null)
        {
            StopCoroutine(existing);
        }

        if (!isActiveAndEnabled || duration <= 0f)
        {
            // Coroutines cannot run on a disabled component, so snap instead.
            Debug.Log(
                $"[DialogueRunner]   snapping '{box.name}' (activeAndEnabled={isActiveAndEnabled}, duration={duration})",
                box);
            ApplyAnchorX(box, targetMin, targetMax);
            _running[box] = null;
            return;
        }

        _running[box] = StartCoroutine(AnchorRoutine(box, targetMin, targetMax, duration));
    }

    private IEnumerator AnchorRoutine(RectTransform box, float targetMin, float targetMax, float duration)
    {
        float startMin = box.anchorMin.x;
        float startMax = box.anchorMax.x;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Unscaled: dialogue should still animate if the game is paused.
            elapsed += Time.unscaledDeltaTime;
            float t = easing.Evaluate(Mathf.Clamp01(elapsed / duration));

            ApplyAnchorX(box, Mathf.LerpUnclamped(startMin, targetMin, t), Mathf.LerpUnclamped(startMax, targetMax, t));
            yield return null;
        }

        ApplyAnchorX(box, targetMin, targetMax);
        _running[box] = null;
    }

    /// <summary>
    /// Writes the horizontal anchors, leaving the vertical ones alone. sizeDelta.x
    /// and anchoredPosition.x are zeroed because any non-zero value there is added
    /// on top of the anchor span — which would stop the anchors from determining
    /// the width, and several of these boxes ship with non-zero values.
    /// </summary>
    private static void ApplyAnchorX(RectTransform box, float min, float max)
    {
        if (box == null)
        {
            return;
        }

        var anchorMin = box.anchorMin;
        anchorMin.x = min;
        box.anchorMin = anchorMin;

        var anchorMax = box.anchorMax;
        anchorMax.x = max;
        box.anchorMax = anchorMax;

        var sizeDelta = box.sizeDelta;
        sizeDelta.x = 0f;
        box.sizeDelta = sizeDelta;

        var anchoredPosition = box.anchoredPosition;
        anchoredPosition.x = 0f;
        box.anchoredPosition = anchoredPosition;
    }

    private RectTransform GetTextbox(int cpuIndex)
    {
        switch (cpuIndex)
        {
            case 1: return CPU1_Textbox;
            case 2: return CPU2_Textbox;
            case 3: return CPU3_Textbox;
            default: return null;
        }
    }

    private RectTransform GetNamebox(int cpuIndex)
    {
        switch (cpuIndex)
        {
            case 1: return CPU1_Namebox;
            case 2: return CPU2_Namebox;
            case 3: return CPU3_Namebox;
            default: return null;
        }
    }
}
