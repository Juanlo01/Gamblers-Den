using UnityEngine;

public class RaiseManager : MonoBehaviour
{
    public int RaiseValue { get; private set; }

    public int Money { get; private set; }

    public void SetMoney(int money)
    {
        Money = money;
    }

    public void RaiseBy(int amount)
    {
        RaiseValue += amount;
    }

    public void Clear()
    {
        RaiseValue = 0;
    }

    public void AllIn()
    {
        RaiseValue = Money;
    }
}
