using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Standalone per-button state texture swapper. Attach alongside a Button;
/// swaps its Image's sprite between default/clicked/disabled textures, fades
/// a separate hover overlay Image in/out on top of it, and swaps the OS
/// cursor between hover/click textures (disabled buttons never change it).
/// Call SetDisabled(bool) (e.g. from a controller script) to drive the
/// disabled state; hover/clicked are driven by pointer events.
/// </summary>
[RequireComponent(typeof(Image))]
public class ButtonImageHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite clickedSprite;
    [SerializeField] private Sprite disabledSprite;

    [Tooltip("Overlay Image faded in (white, opaque) on hover and out (transparent) otherwise.")]
    [SerializeField] private Image hoverImage;

    [Header("Cursor")]
    [Tooltip("Shown while the pointer is over the button. Leave empty to keep the default OS cursor on hover.")]
    [SerializeField] private Texture2D hoverCursor;

    [Tooltip("Shown while the button is pressed. Leave empty to keep showing the hover cursor (or the default) while pressed.")]
    [SerializeField] private Texture2D clickCursor;

    [Tooltip("Pixel offset from the cursor texture's top-left corner to its \"hot\" point.")]
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;

    private Image _image;
    private Button _button;
    private bool _isDisabled;
    private bool _isHovered;
    private bool _isPressed;

    protected virtual void Awake()
    {
        _image = GetComponent<Image>();
        _button = GetComponent<Button>();
        Refresh();
    }

    public void SetDisabled(bool disabled)
    {
        _isDisabled = disabled;
        if (_button != null) _button.interactable = !disabled;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        _isPressed = false;
        Refresh();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPressed = true;
        Refresh();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;
        Refresh();
    }

    /// <summary>The sprite shown when neither disabled nor pressed. Overridden by ToggleImageHandler.</summary>
    protected virtual Sprite GetBaseSprite()
    {
        return defaultSprite;
    }

    protected virtual void Refresh()
    {
        _image.sprite = _isDisabled ? disabledSprite
            : _isPressed ? clickedSprite
            : GetBaseSprite();

        if (hoverImage != null)
        {
            hoverImage.color = _isHovered ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        UpdateCursor();
    }

    private void UpdateCursor()
    {
        Texture2D texture = null;

        if (!_isDisabled)
        {
            if (_isPressed)
            {
                // Fall back to the hover cursor if no dedicated click cursor is set —
                // pressed almost always means still hovered too.
                texture = clickCursor != null ? clickCursor : hoverCursor;
            }
            else if (_isHovered)
            {
                texture = hoverCursor;
            }
        }

        Cursor.SetCursor(texture, cursorHotspot, CursorMode.Auto);
    }
}
