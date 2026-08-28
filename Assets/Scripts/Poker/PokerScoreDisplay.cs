using TMPro;
using UnityEngine;

/// <summary>
/// Displays the human player's current and best score, pulled from PokerGameManager.
/// </summary>
public class PokerScoreDisplay : MonoBehaviour
{
    [SerializeField] private PokerGameManager pokerGameManager;
    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text bestScoreText;

    private void Update()
    {
        if (pokerGameManager == null) return;

        currentScoreText.text = $"Score: ${pokerGameManager.GetCurrentScore()}";
        bestScoreText.text = $"Best: ${pokerGameManager.GetBestScore()}";
    }
}
