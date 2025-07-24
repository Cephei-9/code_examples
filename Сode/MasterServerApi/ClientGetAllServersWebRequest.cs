using System.Threading;
using Cysharp.Threading.Tasks;
using DoodleObby._Multiplayer.NetworkOperations;
using DoodleObby._Utils.JsonConverters;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace DoodleObby._Multiplayer.GameServers.PlayFlow.MasterServerApi
{
    internal class ClientGetAllServersWebRequest : JsonWebRequest<FullGameServersData>
    {
        private static readonly string Url = $"{MasterServerApi.BaseUrl}/client/servers/";
        
        private readonly string _version;

        private ClientGetAllServersWebRequest(string version)
        {
            _version = version;
        }

        protected override UnityWebRequest NewRequest
        {
            get
            {
                string postData = CreatePostData();
                UnityWebRequest request = UnityWebRequest.Post(Url, postData, "application/json");

                return request;
            }
        }

        protected override string RequestName => "[PlayFlow, MasterServerApi] ClientGetAllServersWebRequest";

        public static async UniTask<OperationResult<FullGameServersData>> DoViaHandler(string version, float timeout,
            int countOfAttempt, CancellationToken token = default)
        {
            ClientGetAllServersWebRequest operation = new(version);
            return await operation.DoViaHandler(operation, timeout, countOfAttempt, token);
        }

        protected override FullGameServersData ResultDataFromJson(string json)
        {
            InternalGameServerData[] internalData = JsonConvert.DeserializeObject<InternalGameServerData[]>(json);

            FullGameServersData result = new()
            {
                Count = internalData.Length,
                AllGameServers = new GameServerData[internalData.Length],
            };
            
            for (int i = 0; i < internalData.Length; i++)
            {
                InternalGameServerData internalServerData = internalData[i];
                GameServerData resultServerData = new()
                {
                    Address = internalServerData.Ip,
                    Id = internalServerData.Id,
                    PlayersCount = internalServerData.PlayerCount,
                };
                
                result.AllGameServers[i] = resultServerData;
            }

            return result;
        }

        private string CreatePostData()
        {
            PostData postData = new()
            {
                Password = MasterServerApi.ClientPassword,
                Version = _version
            };

            string json = JsonConvert.SerializeObject(postData);
            return json;
        }

        [System.Serializable]
        private struct PostData
        {
            [JsonProperty("password")]
            public string Password;
            [JsonProperty("version")]
            public string Version;
        }
        
        public struct InternalGameServerData
        {
            [JsonConverter(typeof(IntToStringConverter))]
            [JsonProperty("id")]
            public int Id;
            [JsonConverter(typeof(IntToStringConverter))]
            [JsonProperty("players_count")]
            public int PlayerCount;
            [JsonProperty("ip")]
            public string Ip;
        }
    }
}