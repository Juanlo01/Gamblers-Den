using TMPro;
using UnityEngine;

// Displays the running count of cheaters caught, pulled from CPU_Controller.
public class CaughtPanelDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text caughtText;

    private void Update()
    {
        caughtText.text = $"Cheaters caught: {CPU_Controller.CheatersCaught}";
    }
}
