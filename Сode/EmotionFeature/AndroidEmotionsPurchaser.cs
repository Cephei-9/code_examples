using System;
using System.Collections.Generic;
using System.Linq;
using _Game._Scripts._Features.InApp.EmotionsPurchasing;
using _Game._Scripts._Gameplay.Emotions;
using Cysharp.Threading.Tasks;
using DoodleObby._Features.InApp;
using UnityEngine.Purchasing;

namespace _PlatformCode.Android.Purchasing
{
    public class AndroidEmotionPurchaser : IEmotionPurchaser
    {
        private const string HIPHOP_DANCING_ID = "non_consumable.hip_hop_dancing";
        private const string BLOW_KISS_ID = "non_consumable.blow_kiss";
        private const string WAVING_ID = "non_consumable.waving";
        private const string CHICKEN_DANCE_ID = "non_consumable.chicken_dance";
        private const string SWING_DANCING_ID = "non_consumable.swing_dancing";
        private const string WARMING_UP_ID = "non_consumable.warming_up";
        private const string DANCING_MARASCHINO_STEP_ID = "non_consumable.dancing_maraschino_step";
        private const string SILLY_DANCING_ID = "non_consumable.silly_dancing";
        private const string SAMBA_DANCING_ID = "non_consumable.samba_dancing";
        private const string TWERK_ID = "non_consumable.twerk";

        // Stickers
        private const string BU_ID = "non_consumable.bu";
        private const string CAT_ID = "non_consumable.cat";
        private const string DOGE_ID = "non_consumable.doge";
        private const string GIRL_ID = "non_consumable.girl";
        private const string GRAVE_ID = "non_consumable.grave";
        private const string LIVSI_ID = "non_consumable.livsi";
        private const string LUCKY_ID = "non_consumable.lucky";
        private const string MONKEY_ID = "non_consumable.monkey";
        private const string PEPE_ID = "non_consumable.pepe";
        private const string SANTA_ID = "non_consumable.santa";

        // Transformations
        private const string BANANA_ID = "non_consumable.banana";
        private const string SKULL_ID = "non_consumable.skull";
        private const string CUCUMBER_ID = "non_consumable.cucumber";

        private readonly Dictionary<string, string> _idByEmotionMap = new()
        {
            // Emotions
            { EmotionsIds.HIP_HOP_DANCING, HIPHOP_DANCING_ID },
            { EmotionsIds.BLOW_KISS, BLOW_KISS_ID },
            { EmotionsIds.WAVING, WAVING_ID },
            { EmotionsIds.CHICKEN_DANCE, CHICKEN_DANCE_ID },
            { EmotionsIds.SWING_DANCING, SWING_DANCING_ID },
            { EmotionsIds.WARMING_UP, WARMING_UP_ID },
            { EmotionsIds.DANCING_MARASCHINO_STEP, DANCING_MARASCHINO_STEP_ID },
            { EmotionsIds.SILLY_DANCING, SILLY_DANCING_ID },
            { EmotionsIds.SAMBA_DANCING, SAMBA_DANCING_ID },
            { EmotionsIds.TWERK, TWERK_ID },

            // Stickers
            { EmotionsIds.BU, BU_ID },
            { EmotionsIds.CAT, CAT_ID },
            { EmotionsIds.DOGE, DOGE_ID },
            { EmotionsIds.GIRL, GIRL_ID },
            { EmotionsIds.GRAVE, GRAVE_ID },
            { EmotionsIds.LIVSI, LIVSI_ID },
            { EmotionsIds.LUCKY, LUCKY_ID },
            { EmotionsIds.MONKEY, MONKEY_ID },
            { EmotionsIds.PEPE, PEPE_ID },
            { EmotionsIds.SANTA, SANTA_ID },

            // Transformations
            { EmotionsIds.BANANA, BANANA_ID },
            { EmotionsIds.SKULL, SKULL_ID },
            { EmotionsIds.CUCUMBER, CUCUMBER_ID },
        };

        private readonly IPurchaser _purchaser;

        public event Action OnRestoreEvent;
        public bool IsInit { get; private set; }
        public IEnumerable<string> RestoredEmotions { get; private set; }

        public AndroidEmotionPurchaser(IPurchaser purchaser)
        {
            _purchaser = purchaser;

            InitPurchaser();

            _purchaser.OnInitEvent += () => IsInit = true;
            _purchaser.OnRestoreEvent += Restore;
        }

        public UniTask<bool> TryBuyAsync(string emotion)
        {
            string id = _idByEmotionMap[emotion];
            return _purchaser.TryBuy(id);
        }

        public ShopItemPrice GetPrice(string emotion)
        {
            string id = _idByEmotionMap[emotion];
            return _purchaser.GetPrice(id);
        }

        private void InitPurchaser()
        {
            foreach (string id in _idByEmotionMap.Values)
            {
                PurchasingProduct product = new(id, ProductType.NonConsumable);
                _purchaser.ProductsList.Add(product);
            }
        }

        private void Restore()
        {
            _purchaser.OnRestoreEvent -= Restore;

            Dictionary<string, string> emotionByIdMap = _idByEmotionMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            List<string> restoredList = new();

            foreach (string restoredId in _purchaser.RestoredIds)
            {
                if (emotionByIdMap.TryGetValue(restoredId, out string emotion))
                    restoredList.Add(emotion);
            }

            RestoredEmotions = restoredList;
            OnRestoreEvent?.Invoke();
        }
    }
}
