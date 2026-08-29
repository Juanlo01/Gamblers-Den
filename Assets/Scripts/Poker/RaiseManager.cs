using TMPro;
using UnityEngine;

public class RaiseManager : MonoBehaviour
{
    [SerializeField] private TMP_Text raiseValueText;

    public int RaiseValue { get; private set; }

    public int Money { get; private set; }

    private void Awake()
    {
        UpdateDisplay();
    }

    public void SetMoney(int money)
    {
        Money = money;
        Debug.Log($"[RaiseManager] SetMoney({money}) -> Money={Money}");
    }

    public void RaiseBy(int amount)
    {
        RaiseValue += amount;
        Debug.Log($"[RaiseManager] RaiseBy({amount}) -> RaiseValue={RaiseValue}");
        UpdateDisplay();
    }

    public void Clear()
    {
        RaiseValue = 0;
        Debug.Log($"[RaiseManager] Clear() -> RaiseValue={RaiseValue}");
        UpdateDisplay();
    }

    public void AllIn()
    {
        RaiseValue = Money;
        Debug.Log($"[RaiseManager] AllIn() -> RaiseValue={RaiseValue}");
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (raiseValueText != null)
        {
            raiseValueText.text = $"{RaiseValue}";
        }
    }
}
