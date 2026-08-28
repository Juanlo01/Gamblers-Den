using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TexasHoldem.Logic;
using TexasHoldem.Logic.Cards;
using TexasHoldem.Logic.Players;
using TMPro;
using UnityEngine.UI;

public class PokerUIController : MonoBehaviour
{
    public static PokerUIController Instance { get; private set; }

    [Header("Action Panel")]
    [SerializeField] private GameObject actionPanel;
    [SerializeField] private Button foldButton;
    [SerializeField] private Button checkCallButton;
    [SerializeField] private Button raiseButton;
    [SerializeField] private Slider raiseSlider;
    [SerializeField] private TMP_Text raiseAmountLabel;

    private TMP_Text _checkCallLabel;

    [Header("Cards")]
    [SerializeField] private TMP_Text holeCard1Text;
    [SerializeField] private TMP_Text holeCard2Text;
    [SerializeField] private List<TMP_Text> communityCardTexts;

    [Header("Misc")]
    [SerializeField] private TMP_Text potText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text handText;

    private void Awake()
    {
        Instance = this;
        actionPanel.SetActive(false);
        _checkCallLabel = checkCallButton.GetComponentInChildren<TMP_Text>();
    }

    public void OnGameStarted()
    {
        statusText.text = "Game starting!";
    }

    public void ShowHoleCards(Card first, Card second)
    {
        holeCard1Text.text = CardMapper.ToDisplayText(first);
        holeCard2Text.text = CardMapper.ToDisplayText(second);
        handText.text = "";
    }

    public void ShowMoney(int moneyLeft)
    {
        moneyText.text = $"Money: ${moneyLeft}";
    }

    public void ShowHandType(HandRankType rankType)
    {
        handText.text = HandRankMapper.ToDisplayText(rankType);
    }

    public void ShowCommunityCards(IReadOnlyCollection<Card> cards, int currentPot)
    {
        var cardList = cards.ToList();
        for (int i = 0; i < communityCardTexts.Count; i++)
        {
            communityCardTexts[i].text = i < cardList.Count
                ? CardMapper.ToDisplayText(cardList[i])
                : "";
        }
        potText.text = $"Pot\n${currentPot}";
    }

    public void OnRoundEnd()
    {
        statusText.text = $"Round ended.";
    }

    public void ShowShowdown(Dictionary<string, ICollection<Card>> showdownCards)
    {
        statusText.text = "Showdown: " + string.Join(" | ",
            showdownCards.Select(kv => $"{kv.Key}: {string.Join(", ", kv.Value.Select(CardMapper.ToDisplayText))}"));
    }

    public void ShowGameOver(string winnerName)
    {
        actionPanel.SetActive(false);
        statusText.text = $"Game over. Winner: {winnerName}";
    }

    public void ShowBlindPosted(string playerName, PlayerAction blindAction)
    {
        statusText.text = $"{playerName} posts {blindAction.Money}";
    }

    public void ShowNpcAction(string playerName, PlayerAction action)
    {
        statusText.text = $"{playerName}: {action}";
    }

    public void ShowActionPrompt(IGetTurnContext context, HumanPlayer player)
    {
        actionPanel.SetActive(true);

        // Fold always available
        foldButton.onClick.RemoveAllListeners();
        foldButton.onClick.AddListener(() => Submit(player, PlayerAction.Fold()));

        _checkCallLabel.text = context.CanCheck ? "Check" : "Call";

        checkCallButton.onClick.RemoveAllListeners();
        checkCallButton.onClick.AddListener(() => Submit(player, PlayerAction.CheckOrCall()));

        raiseButton.gameObject.SetActive(context.CanRaise);
        raiseSlider.gameObject.SetActive(context.CanRaise);

        if (context.CanRaise)
        {
            int maxRaise = context.MoneyLeft - context.MoneyToCall;
            raiseSlider.minValue = context.MinRaise;
            raiseSlider.maxValue = maxRaise;
            raiseSlider.value = context.MinRaise;
            raiseAmountLabel.text = context.MinRaise.ToString();

            raiseSlider.onValueChanged.RemoveAllListeners();
            raiseSlider.onValueChanged.AddListener(v =>
                raiseAmountLabel.text = ((int)v).ToString());

            raiseButton.onClick.RemoveAllListeners();
            raiseButton.onClick.AddListener(() =>
                Submit(player, PlayerAction.Raise((int)raiseSlider.value)));
        }
    }

    private void Submit(HumanPlayer player, PlayerAction action)
    {
        statusText.text = $"You: {action}";
        player.SubmitAction(action);
        actionPanel.SetActive(false);
    }
}
