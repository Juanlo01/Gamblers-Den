using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows and hides one modal panel, and makes sure a hidden panel cannot be
/// clicked through.
///
/// Setting CanvasGroup.blocksRaycasts alone is *usually* enough, but it is
/// bypassed by any descendant that carries its own CanvasGroup with
/// ignoreParentGroups enabled - that child keeps receiving clicks while the
/// panel looks closed. This walks the children and switches their Graphics'
/// raycastTarget directly, which nothing can override.
///
/// Reopening restores each Graphic's ORIGINAL raycastTarget rather than turning
/// everything on: most decorative images and text labels are authored with
/// raycastTarget off, and blanket-enabling them would let a label swallow clicks
/// meant for a button behind it. The baseline is captured once, at construction,
/// so build these BEFORE the first Hide() - otherwise "original" is recorded as
/// the already-hidden state and the panel never becomes clickable again.
/// </summary>
public class ModalGroup
{
    // One shared instance per CanvasGroup. Two scripts can legitimately drive
    // the same panel (PauseMenu and LeaderboardUI both hold the ScoreMenu), and
    // if each built its own the second one's baseline would be captured AFTER
    // the first had already hidden the panel - recording "nothing is clickable"
    // as the original state, so it could never be restored. Sharing means the
    // baseline is taken once, by whichever asks first, before any hide.
    private static readonly Dictionary<CanvasGroup, ModalGroup> Shared =
        new Dictionary<CanvasGroup, ModalGroup>();

    private readonly CanvasGroup _group;
    private Graphic[] _graphics;
    private bool[] _baselineRaycastTarget;

    private ModalGroup(CanvasGroup group)
    {
        _group = group;
        CaptureBaseline();
    }

    /// <summary>
    /// The ModalGroup for a panel, creating it on first use. Always prefer this
    /// over holding your own - see the note on Shared above.
    /// </summary>
    public static ModalGroup For(CanvasGroup group)
    {
        if (group == null)
        {
            return new ModalGroup(null);
        }

        // Drop entries whose CanvasGroup was destroyed (scene change), or the
        // dictionary would keep them alive and hand back dead instances.
        PurgeDestroyed();

        if (!Shared.TryGetValue(group, out var modal) || modal == null)
        {
            modal = new ModalGroup(group);
            Shared[group] = modal;
        }

        return modal;
    }

    private static void PurgeDestroyed()
    {
        List<CanvasGroup> dead = null;
        foreach (var pair in Shared)
        {
            // Unity's overloaded == reports destroyed objects as null.
            if (pair.Key == null)
            {
                (dead ?? (dead = new List<CanvasGroup>())).Add(pair.Key);
            }
        }

        if (dead == null) return;
        foreach (var key in dead)
        {
            Shared.Remove(key);
        }
    }

    /// <summary>The panel this drives. Null is tolerated everywhere.</summary>
    public CanvasGroup Group => _group;

    /// <summary>True when the panel is currently shown.</summary>
    public bool IsVisible { get; private set; } = true;

    /// <summary>
    /// Re-reads the children and their raycast settings. Only needed if the
    /// panel's contents are built or replaced at runtime; call it while the
    /// panel is VISIBLE so the captured baseline is the real one.
    /// </summary>
    public void CaptureBaseline()
    {
        if (_group == null)
        {
            _graphics = new Graphic[0];
            _baselineRaycastTarget = new bool[0];
            return;
        }

        // true = include inactive children, so a panel that starts with some
        // rows switched off still has them registered.
        _graphics = _group.GetComponentsInChildren<Graphic>(true);
        _baselineRaycastTarget = new bool[_graphics.Length];

        for (int i = 0; i < _graphics.Length; i++)
        {
            _baselineRaycastTarget[i] = _graphics[i] != null && _graphics[i].raycastTarget;
        }
    }

    /// <summary>
    /// Shows or hides the panel: alpha, interactable, blocksRaycasts, and every
    /// child Graphic's raycastTarget all move together.
    /// </summary>
    public void SetVisible(bool visible)
    {
        IsVisible = visible;

        if (_group != null)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }

        if (_graphics == null)
        {
            return;
        }

        for (int i = 0; i < _graphics.Length; i++)
        {
            var graphic = _graphics[i];
            if (graphic == null)
            {
                continue; // destroyed since the baseline was taken
            }

            // Closed: nothing is clickable. Open: back to how it was authored.
            graphic.raycastTarget = visible && _baselineRaycastTarget[i];
        }
    }

    public void Show() => SetVisible(true);

    public void Hide() => SetVisible(false);
}
