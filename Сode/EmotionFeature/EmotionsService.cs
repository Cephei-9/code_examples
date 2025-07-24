using System;
using System.Collections.Generic;
using _Game._Scripts._Features.InApp.EmotionsPurchasing;
using Cysharp.Threading.Tasks;
using DoodleObby._Features.Analytics;
using DoodleObby._Features.InApp;
using DoodleObby._Infrastructure.RunSystem;
using DoodleObby._Infrastructure.SaveSystem;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;

namespace _Game._Scripts._Gameplay.Emotions
{
    public class EmotionsService : MonoBehaviour
    {
        [ShowInInspector]
        [DictionaryDrawerSettings(KeyLabel = "EmotionId", ValueLabel = "Entity")]
        public readonly Dictionary<string, EmotionEntity> EmotionEntitiesMap = new();

        public ReactiveProperty<bool> IsBuyProcess;

        private IEmotionPurchaser _purchaser;
        private IRunService _runService;
        private GameDataManager _gameDataManager;
        private IAnalytics _analytics;

        public event Action OnInitEvent;
        public event Action OnUpdateDataEvent;

        [ShowInInspector]
        public bool IsInit { get; private set; }

        public async UniTaskVoid Init(IEmotionPurchaser purchaser,
            IRunService runService,
            GameDataManager gameDataManager,
            IAnalytics analytics)
        {
            _analytics = analytics;
            _gameDataManager = gameDataManager;
            _runService = runService;
            _purchaser = purchaser;

            await UniTask.WaitUntil(() => purchaser.IsInit, cancellationToken: destroyCancellationToken);

            CreateEntities();
            _purchaser.OnRestoreEvent += Restore;

            IsInit = true;
            OnInitEvent?.Invoke();
        }

        [Button]
        public async UniTask<bool> TryBuyEmotionAsync(string emotionId)
        {
            EmotionEntity entity = EmotionEntitiesMap[emotionId];

            if (entity.IsAvailable.Value || IsBuyProcess.Value)
                return false;

            IsBuyProcess.Value = true;

            bool purchasingResult;

            if (_runService.IsFreePurchasing)
                purchasingResult = true;
            else
                purchasingResult = await _purchaser.TryBuyAsync(emotionId);

            Debug.Log($"[EmotionService] buy emotion: {emotionId} with result: {purchasingResult}");

            if (purchasingResult)
            {
                entity.IsAvailable.Value = true;
                OnUpdateDataEvent?.Invoke();
                Save();
            }

            if (purchasingResult && !_runService.IsFreePurchasing)
            {
                _analytics.TrackPurchaseEmotion(emotionId);
            }

            IsBuyProcess.Value = false;
            return purchasingResult;
        }
        
        [Button]
        public bool TryUnlockEmotion(string emotionId)
        {
            EmotionEntity entity = EmotionEntitiesMap[emotionId];

            if (entity.IsAvailable.Value || IsBuyProcess.Value)
                return false;
            
            Debug.Log($"[EmotionService] unlock emotion: {emotionId}");
            
            entity.IsAvailable.Value = true;
            OnUpdateDataEvent?.Invoke();
            Save();

            return true;
        }

        private void CreateEntities()
        {
            EmotionSaveData[] saveData = _gameDataManager.GameData.EmotionsSaveData;

            foreach (EmotionSaveData data in saveData)
            {
                ShopItemPrice price = _purchaser.GetPrice(data.EmotionId);
                EmotionEntity entity = new(data.EmotionId, data.IsAvailable, price);
                EmotionEntitiesMap.Add(data.EmotionId, entity);
            }
        }

        private void Save()
        {
            EmotionSaveData[] saveData = _gameDataManager.GameData.EmotionsSaveData;

            foreach (EmotionSaveData data in saveData)
            {
                EmotionEntity entity = EmotionEntitiesMap[data.EmotionId];
                data.IsAvailable = entity.IsAvailable.Value;
            }

            _gameDataManager.UpdateEmotionsData(saveData);
        }

        private void Restore()
        {
            _purchaser.OnRestoreEvent -= Restore;
            
            foreach (string restoredEmotionId in _purchaser.RestoredEmotions)
            {
                if (EmotionEntitiesMap.TryGetValue(restoredEmotionId, out var entity))
                {
                    entity.IsAvailable.Value = true;
                    Debug.Log($"[EmotionService] restore emotion: {restoredEmotionId}");
                }
            }

            Save();
        }
    }
}