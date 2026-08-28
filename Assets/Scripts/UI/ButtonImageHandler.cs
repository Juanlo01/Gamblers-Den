using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Standalone per-button state texture swapper. Attach alongside a Button;
/// swaps its Image's sprite between default/hover/clicked/disabled textures.
/// Call SetDisabled(bool) (e.g. from a controller script) to drive the
/// disabled state; hover/clicked are driven by pointer events.
/// </summary>
[RequireComponent(typeof(Image))]
public class ButtonImageHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite clickedSprite;
    [SerializeField] private Sprite disabledSprite;

    private Image _image;
    private Button _button;
    private bool _isDisabled;
    private bool _isHovered;
    private bool _isPressed;

    private void Awake()
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

    private void Refresh()
    {
        _image.sprite = _isDisabled ? disabledSprite
            : _isPressed ? clickedSprite
            : _isHovered ? hoverSprite
            : defaultSprite;
    }
}
