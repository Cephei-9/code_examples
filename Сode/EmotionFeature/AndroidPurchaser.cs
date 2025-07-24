using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DoodleObby._Features.InApp;
using Io.AppMetrica;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using Enumerable = System.Linq.Enumerable;

namespace _PlatformCode.Android.Purchasing
{
    public class AndroidPurchaser : IPurchaser, IDetailedStoreListener
    {
        private const string USD_CURRENCY_CODE = "USD";
        
        private IStoreController _storeController;
        
        private UniTaskCompletionSource<bool> _purchaseTcs;
        private bool _isProcessPurchasing;

        public event Action OnInitEvent;
        public event Action OnRestoreEvent;
        public bool IsRestored { get; private set; }
        public IEnumerable<string> RestoredIds { get; private set; }
        public List<PurchasingProduct> ProductsList { get; set; } = new();

        public async UniTaskVoid Init()
        {
            Debug.Log($"[AndroidPurchaser] start initialization");

            await InitUnityServices();
            
            ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            
            foreach (PurchasingProduct purchasingProduct in ProductsList)
            {
                builder.AddProduct(purchasingProduct.Id, purchasingProduct.ProductType);

            }
            
            Debug.Log($"[AndroidPurchaser] add products: {string.Join(", ", ProductsList.Select(product => product.Id))}");
            
            UnityPurchasing.Initialize(this, builder);
        }

        private async UniTask InitUnityServices()
        {
            try
            {
                await UnityServices.InitializeAsync();
            }
            catch (Exception e)
            {
                Debug.LogError("[AndroidPurchaser] Unity Services initialization is failed: " + e.Message);
            }
        }

        public async UniTask<bool> TryBuy(string productId)
        {
            if (!IsRestored)
            {
                Debug.LogError($"[AndroidPurchaser] can't make buy operation, purchaser is not init");
                return false;
            }
            
            if (_isProcessPurchasing)
            {
                return false;
            }

            Debug.Log($"[AndroidPurchaser] start purchase operation on item: {productId}");

            _isProcessPurchasing = true;
            _purchaseTcs = new UniTaskCompletionSource<bool>();
            AsyncLazy<bool> purchaseTask = _purchaseTcs.Task.ToAsyncLazy();
            
            _storeController.InitiatePurchase(productId);

            bool purchaseResult = await purchaseTask;
            _isProcessPurchasing = false;

            Debug.Log($"[AndroidPurchaser] purchase operation on item: {productId} complete with result: {purchaseResult}");

            return purchaseResult;
        }

        public ShopItemPrice GetPrice(string productId)
        {
            if (!IsRestored)
            {
                Debug.LogError($"[AndroidPurchaser] can't return price, purchaser is not init");
                return default;
            }
            
            Product product = _storeController.products.WithID(productId);
            
            if (product == null)
            {
                Debug.LogError($"[AndroidPurchaser] can't return price by this id: {productId}");
                return default;
            }

            decimal priceValue = product.metadata.localizedPrice;
            string currencyCode = product.metadata.isoCurrencyCode;
            string localizedPriceString = product.metadata.localizedPriceString;

            PriceType priceType = currencyCode == USD_CURRENCY_CODE ? PriceType.Usd : PriceType.Unknown;

            return new ShopItemPrice(priceType, priceValue, localizedPriceString);
        }

        void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("[AndroidPurchaser] initialization is success");
            
            _storeController = controller;
            //Restore();
            OnInitEvent?.Invoke();
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[AndroidPurchaser] initialization is failed. Error: {error}, Message: {message}");   
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"[AndroidPurchaser] initialization is failed. Error: {error}");
        }

        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            Debug.Log($"[AndroidPurchaser] process purchase. Product: {purchaseEvent.purchasedProduct.definition.id}");
            TryFinishPurchaseTask(true);

            AppMetricaValidation(purchaseEvent.purchasedProduct);
            
            return PurchaseProcessingResult.Complete;
        }

        void IDetailedStoreListener.OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.Log($"[AndroidPurchaser] purchase is failed. Product: {product.definition.id}, Description: {failureDescription.message}");

            TryFinishPurchaseTask(false);
        }

        void IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.Log($"[AndroidPurchaser] purchase is failed. Product: {product.definition.id}");

            TryFinishPurchaseTask(false);
        }

        private void TryFinishPurchaseTask(bool result)
        {
            _purchaseTcs?.TrySetResult(result);
            _purchaseTcs = null;
        }

        private void Restore()
        {
            List<string> restoredIds = new();

            foreach (Product product in _storeController.products.all)
            {
                if (product.hasReceipt)
                {
                    restoredIds.Add(product.definition.id);
                }
            }

            RestoredIds = restoredIds;
            IsRestored = true;
            OnRestoreEvent?.Invoke();

            Debug.Log($"[AndroidPurchaser] finish restore. All products count: {_storeController.products.all.Length}. " +
                      $"Restored Ids: " + string.Join(", ", restoredIds));
        }

        private void AppMetricaValidation(Product product)
        {
            string currency = product.metadata.isoCurrencyCode;
            decimal price = product.metadata.localizedPrice;

            // Creating the instance of the Revenue class.
            long priceMicros = (long)(price * 1000000m);
            Revenue revenue = new Revenue(priceMicros, currency);
            
            if (product.receipt != null) {
                // Creating the instance of the Revenue.Receipt class.
                Revenue.Receipt yaReceipt = new();
                Receipt receipt = JsonUtility.FromJson<Receipt>(product.receipt);
#if UNITY_ANDROID
                PayloadAndroid payloadAndroid;
                
                try
                {
                    payloadAndroid = JsonUtility.FromJson<PayloadAndroid>(receipt.Payload);
                }
                catch
                {
                    payloadAndroid = new PayloadAndroid
                    {
                        Json = "",
                        Signature = "",
                    };
                }
                
                yaReceipt.Signature = payloadAndroid.Signature;
                yaReceipt.Data = payloadAndroid.Json;
#elif UNITY_IPHONE
                yaReceipt.TransactionID = receipt.TransactionID;
                yaReceipt.Data = receipt.Payload;
#endif
                revenue.ReceiptValue = yaReceipt;
            }
            
            // Sending data to the AppMetrica server.
            AppMetrica.ReportRevenue(revenue);
        }
        
        [System.Serializable]
        public struct Receipt {
            public string Store;
            public string TransactionID;
            public string Payload;
        }

        // Additional information about the IAP for Android.
        [System.Serializable]
        public struct PayloadAndroid {
            public string Json;
            public string Signature;
        }
    }
}