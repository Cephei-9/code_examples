using System.Threading;
using Cysharp.Threading.Tasks;
using DoodleObby._Multiplayer.NetworkOperations;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace DoodleObby._Multiplayer.GameServers.PlayFlow.MasterServerApi
{
    internal sealed class UpdateGameServerStateWebRequest : JsonWebRequest<string>
    {
        private static readonly string Url = $"{MasterServerApi.BaseUrl}/server/";
        
        private readonly GameServerState _state;

        private UpdateGameServerStateWebRequest(GameServerState state)
        {
            _state = state;
            Log = false;
        }

        protected override UnityWebRequest NewRequest
        {
            get
            {
                string postData = JsonConvert.SerializeObject(_state);
                UnityWebRequest request = UnityWebRequest.Post(Url, postData, "application/json");

                //Debug.Log($"{RequestName} create new request with this post data: \n{postData}");
                
                return request;
            }
        }

        protected override string RequestName => "[PlayFlow, MasterServerApi] UpdateGameServerStateWebRequest";

        public static async UniTask<OperationResult> DoViaHandler(GameServerState state, float timeout,
            int countOfAttempt, CancellationToken token = default)
        {
            UpdateGameServerStateWebRequest operation = new(state);
            return await operation.DoViaHandler(operation, timeout, countOfAttempt, token);
        }

        protected override string ResultDataFromJson(string json)
        {
            return json;
        }
    }
}