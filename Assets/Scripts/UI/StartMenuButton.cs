using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Pulses this button's Image with an HDR emissive glow (picked up by URP Bloom -
/// see the "Global Volume" object in StartMenu.unity) while hovered, and flashes
/// faster for a short burst on click. A runtime-only material instance of the
/// "UI/Emissive" shader is swapped in on hover and back out on exit, so the
/// button looks completely normal - and contributes no bloom - whenever it
/// isn't being interacted with.
/// </summary>
[RequireComponent(typeof(Image))]
public class StartMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Tooltip("Must expose an HDR _EmissionColor property. Defaults to Shader.Find(\"UI/Emissive\") if left empty.")]
    [SerializeField] private Shader emissiveShader;

    [Tooltip("Full brightness oscillations per second while hovered.")]
    [SerializeField] private float pulseSpeed = 1.5f;

    [Tooltip("Multiplier applied to pulseSpeed for the click flash burst.")]
    [SerializeField] private float clickSpeedMultiplier = 3f;

    [Tooltip("How long the faster click flash lasts, in seconds.")]
    [SerializeField] private float clickFlashDuration = 0.5f;

    private const float MinEmission = 0.5f;
    private const float MaxEmission = 2.0f;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private Image _image;
    private Material _originalMaterial;
    private Material _glowMaterial;
    private Coroutine _pulseRoutine;
    private float _clickBoostUntil;

    private void Awake()
    {
        _image = GetComponent<Image>();

        if (emissiveShader == null) emissiveShader = Shader.Find("UI/Emissive");
        _glowMaterial = new Material(emissiveShader);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _originalMaterial = _image.material;
        _image.material = _glowMaterial;

        if (_pulseRoutine == null) _pulseRoutine = StartCoroutine(Pulse());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_pulseRoutine != null)
        {
            StopCoroutine(_pulseRoutine);
            _pulseRoutine = null;
        }

        _image.material = _originalMaterial;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _clickBoostUntil = Time.unscaledTime + clickFlashDuration;
    }

    private IEnumerator Pulse()
    {
        while (true)
        {
            // unscaledTime so the hover glow (and click flash) keep animating even if
            // gameplay time is paused on this menu.
            bool boosted = Time.unscaledTime < _clickBoostUntil;
            float speed = boosted ? pulseSpeed * clickSpeedMultiplier : pulseSpeed;

            float wave = (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;
            float emission = Mathf.Lerp(MinEmission, MaxEmission, wave);
            _glowMaterial.SetColor(EmissionColorId, Color.white * emission);

            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (_glowMaterial != null) Destroy(_glowMaterial);
    }
}
