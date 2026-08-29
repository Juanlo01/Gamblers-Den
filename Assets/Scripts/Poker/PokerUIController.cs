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
    [SerializeField] private GameObject toolPanel;
    [SerializeField] private GameObject toolPanelBackground;
    [SerializeField] private GameObject hoverPanel;
    [SerializeField] private Button foldButton;
    [SerializeField] private ButtonImageHandler foldImageHandler;
    [SerializeField] private Button checkButton;
    [SerializeField] private ButtonImageHandler checkImageHandler;
    [SerializeField] private Button callButton;
    [SerializeField] private ButtonImageHandler callImageHandler;
    [SerializeField] private Button raiseButton;
    [SerializeField] private Slider raiseSlider;
    [SerializeField] private TMP_Text raiseAmountLabel;
    [SerializeField] private RaiseManager raiseManager;

    [Header("Raise Menu")]
    [SerializeField] private Toggle raiseMenuToggle;
    [SerializeField] private CanvasGroup raiseMenuGroup;

    [Header("Cards")]
    [SerializeField] private Image holeCard1Image;
    [SerializeField] private Image holeCard2Image;
    [SerializeField] private List<Image> communityCardImages;

    [Header("Misc")]
    [SerializeField] private TMP_Text potText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text handText;

    private void Awake()
    {
        Instance = this;
        SetToolPanelVisible(false);
        CardSpriteLibrary.Preload();

        raiseMenuToggle.isOn = false;
        raiseMenuToggle.onValueChanged.AddListener(SetRaiseMenuVisible);
        SetRaiseMenuVisible(false);
    }

    private void SetToolPanelVisible(bool visible)
    {
        toolPanel.SetActive(visible);
        toolPanelBackground.SetActive(visible);
        hoverPanel.SetActive(visible);
    }

    private void SetRaiseMenuVisible(bool visible)
    {
        raiseMenuGroup.alpha = visible ? 1f : 0f;

        foreach (var button in raiseMenuGroup.GetComponentsInChildren<Button>(true))
        {
            button.interactable = visible;
        }

        // raiseSlider lives outside raiseMenuGroup's hierarchy, so it needs its
        // own explicit toggle to follow the same open/closed state.
        raiseSlider.gameObject.SetActive(visible);
    }

    public void OnGameStarted()
    {
        statusText.text = "Game starting!";
    }

    public void ShowHoleCards(Card first, Card second)
    {
        holeCard1Image.sprite = CardSpriteLibrary.GetSprite(first);
        holeCard2Image.sprite = CardSpriteLibrary.GetSprite(second);
        handText.text = "";
    }

    public void ShowMoney(int moneyLeft)
    {
        moneyText.text = $"Money: ${moneyLeft}";
        raiseManager.SetMoney(moneyLeft);
    }

    public void ShowHandType(HandRankType rankType)
    {
        handText.text = HandRankMapper.ToDisplayText(rankType);
    }

    public void ShowCommunityCards(IReadOnlyCollection<Card> cards, int currentPot)
    {
        var cardList = cards.ToList();
        for (int i = 0; i < communityCardImages.Count; i++)
        {
            communityCardImages[i].sprite = i < cardList.Count
                ? CardSpriteLibrary.GetSprite(cardList[i])
                : CardSpriteLibrary.GetBackSprite();
        }
        potText.text = $"Pot\n${currentPot}";
    }

    public void OnRoundEnd()
    {
        statusText.text = $"Round ended.";
    }

    public void ShowShowdown(IEndHandContext context)
    {
        statusText.text = string.Join(", ", context.Winnings.Select(kv => $"{kv.Key} wins ${kv.Value}"));
    }

    public void ShowGameOver(string winnerName)
    {
        SetToolPanelVisible(false);
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
        SetToolPanelVisible(true);

        // Raise bar starts collapsed each turn - the player opens it by clicking Raise.
        raiseMenuToggle.gameObject.SetActive(context.CanRaise);
        raiseMenuToggle.isOn = false;
        SetRaiseMenuVisible(false);

        // Fold always available
        foldButton.onClick.RemoveAllListeners();
        foldButton.onClick.AddListener(() => Submit(player, PlayerAction.Fold()));
        foldImageHandler.SetDisabled(false);

        checkButton.onClick.RemoveAllListeners();
        checkButton.onClick.AddListener(() => Submit(player, PlayerAction.CheckOrCall()));
        checkImageHandler.SetDisabled(!context.CanCheck);

        callButton.onClick.RemoveAllListeners();
        callButton.onClick.AddListener(() => Submit(player, PlayerAction.CheckOrCall()));
        callImageHandler.SetDisabled(context.CanCheck);

        raiseButton.gameObject.SetActive(context.CanRaise);

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
                Submit(player, PlayerAction.Raise(raiseManager.RaiseValue)));
        }
    }

    private void Submit(HumanPlayer player, PlayerAction action)
    {
        statusText.text = $"You: {action}";
        player.SubmitAction(action);
        SetToolPanelVisible(false);
        raiseMenuToggle.isOn = false;
        SetRaiseMenuVisible(false);
    }
}
