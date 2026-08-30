using UnityEngine;
using UnityEngine.EventSystems;

public class HandRankTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject handRankPanel;
    public void OnPointerEnter(PointerEventData eventData)
    {
        // open hand rank image
        handRankPanel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // close hand rank image
        handRankPanel?.SetActive(false);
    }
}
