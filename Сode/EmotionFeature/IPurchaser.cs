using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DoodleObby._Features.InApp;

namespace _PlatformCode.Android.Purchasing
{
    public interface IPurchaser
    {
        event Action OnInitEvent;
        event Action OnRestoreEvent;
        bool IsRestored { get; }
        IEnumerable<string> RestoredIds { get; }
        List<PurchasingProduct> ProductsList { get; set; }

        UniTask<bool> TryBuy(string productId);
        ShopItemPrice GetPrice(string productId);
    }
}