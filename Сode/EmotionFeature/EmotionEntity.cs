using DoodleObby._Features.InApp;
using UniRx;

namespace _Game._Scripts._Gameplay.Emotions
{
    public class EmotionEntity
    {
        public string EmotionId;
        public ReactiveProperty<bool> IsAvailable;
        public ShopItemPrice Price;

        public EmotionEntity(string emotionId, bool isAvailable, ShopItemPrice price)
        {
            EmotionId = emotionId;
            IsAvailable = new ReactiveProperty<bool>(isAvailable);
            Price = price;
        }
    }
}