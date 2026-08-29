using System;
using System.Collections.Generic;
using TexasHoldem.Logic.Cards;
using UnityEngine;

public static class CardSpriteLibrary
{
    private const string ResourceFolder = "Cards";
    private const string BackSpriteName = "back";

    private static Dictionary<string, Sprite> cache;

    public static void Preload()
    {
        EnsureLoaded();
    }

    public static Sprite GetSprite(Card card)
    {
        return GetSprite(CardMapper.ToSpriteName(card));
    }

    public static Sprite GetBackSprite()
    {
        return GetSprite(BackSpriteName);
    }

    private static Sprite GetSprite(string spriteName)
    {
        EnsureLoaded();
        return cache.TryGetValue(spriteName, out var sprite) ? sprite : null;
    }

    private static void EnsureLoaded()
    {
        if (cache != null)
        {
            return;
        }

        cache = new Dictionary<string, Sprite>();

        foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardType type in Enum.GetValues(typeof(CardType)))
            {
                LoadInto(CardMapper.ToSpriteName(new Card(suit, type)));
            }
        }

        LoadInto(BackSpriteName);
    }

    private static void LoadInto(string spriteName)
    {
        var sprite = Resources.Load<Sprite>($"{ResourceFolder}/{spriteName}");
        if (sprite != null)
        {
            cache[spriteName] = sprite;
        }
        else
        {
            Debug.LogWarning($"CardSpriteLibrary: missing sprite '{spriteName}' in Resources/{ResourceFolder}.");
        }
    }
}
