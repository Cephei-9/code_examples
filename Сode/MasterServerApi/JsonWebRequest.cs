using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DoodleObby._Utils;
using UnityEngine.Networking;

namespace DoodleObby._Multiplayer.NetworkOperations
{
    public abstract class JsonWebRequest<TResultData> : INetworkOperation<OperationResult<TResultData>>
    {
        private CancellationTokenSource _cts;

        public OperationResult<TResultData> FailedResult => new(false, default);

        protected abstract UnityWebRequest NewRequest { get; }
        protected abstract string RequestName { get; }
        protected virtual bool Log { get; set; } = true;

        public async UniTask<OperationResult<TResultData>> DoViaHandler(JsonWebRequest<TResultData> operation,
            float timeout, int countOfAttempt, CancellationToken token = default)
        {
            NetworkOperationHandler handler = new(timeout, countOfAttempt);
            return await handler.DoOperation(operation, token);
        }

        public async UniTask<OperationResult<TResultData>> Do(CancellationToken token)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            UnityWebRequest request = NewRequest;

            try
            {
                await request.SendWebRequest().WithCancellation(_cts.Token);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (Log)
                        UnityEngine.Debug.LogError($"{RequestName} finished with error: {request.error}");

                    return FailedResult;
                }

                string json = request.downloadHandler.text;
                TResultData resultData = ResultDataFromJson(json);

                if (Log)
                    UnityEngine.Debug.Log($"{RequestName} finished successful with json:\n{json}");

                return new OperationResult<TResultData>(true, resultData);
            }
            catch(Exception e)
            {
                if (e is OperationCanceledException)
                    throw;
                
                UnityEngine.Debug.LogError($"Json web request catch exception: {e}");
                return FailedResult;
            }
            finally
            {
                request.Dispose();
            }
        }

        public void Abort()
        {
            _cts?.CancelAndDispose();
        }

        protected abstract TResultData ResultDataFromJson(string json);
    }
}