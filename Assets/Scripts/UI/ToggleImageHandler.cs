using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Per-toggle state texture swapper. Same default/hover/clicked/disabled
/// behavior as ButtonImageHandler, plus a Toggled On sprite used as the base
/// (resting) sprite whenever the toggle is on; toggled off falls back to
/// Default Sprite, same as a plain button.
///
/// If a Toggle component is present on the same GameObject, its
/// onValueChanged is used to drive the toggled state automatically;
/// otherwise (or in addition), call SetToggled(bool) directly.
/// </summary>
public class ToggleImageHandler : ButtonImageHandler
{
    [SerializeField] private Sprite toggledOnSprite;

    private Toggle _toggle;
    private bool _isToggled;

    protected override void Awake()
    {
        // base.Awake() assigns _image/_button and calls Refresh() once with
        // _isToggled at its default (false) — re-run it below so a toggle
        // starting in the "on" state renders correctly from the start.
        base.Awake();

        _toggle = GetComponent<Toggle>();
        if (_toggle != null)
        {
            _isToggled = _toggle.isOn;
            _toggle.onValueChanged.AddListener(SetToggled);
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (_toggle != null)
        {
            _toggle.onValueChanged.RemoveListener(SetToggled);
        }
    }

    public void SetToggled(bool toggled)
    {
        _isToggled = toggled;
        Refresh();
    }

    protected override Sprite GetBaseSprite()
    {
        return _isToggled ? toggledOnSprite : base.GetBaseSprite();
    }
}
