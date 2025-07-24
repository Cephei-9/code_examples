using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DoodleObby._Features.InApp;

namespace _Game._Scripts._Features.InApp.EmotionsPurchasing
{
    public interface IEmotionPurchaser
    {
        event Action OnRestoreEvent;
        bool IsInit { get; }
        IEnumerable<string> RestoredEmotions { get; }
        UniTask<bool> TryBuyAsync(string emotion);
        ShopItemPrice GetPrice(string emotion);
    }
}