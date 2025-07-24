using System.Threading;
using Cysharp.Threading.Tasks;
using DoodleObby._Multiplayer.NetworkOperations;

namespace DoodleObby._Multiplayer.GameServers.PlayFlow.MasterServerApi
{
    public static class MasterServerApi
    {
        private const float RequestTimeOut = 2; 
        private const int RequestCountOfAttempt = 3;

        public const int TimeoutForGameServers = 10;

        public static string Address;
        
        public static string ClientPassword
        {
            get
            {
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_STANDALONE_WIN
                return "obbby.saByivMB";
#endif
                return "";
            }
        }

        public static string ServerPassword
        {
            get
            {
#if UNITY_EDITOR || UNITY_STANDALONE_LINUX
                return "obby.Gp@RCF*F";
#endif
                return "";
            }
        }

        public static string DevPassword
        {
            get
            {
#if UNITY_EDITOR
                return "pass";
#endif
                return "";
            }
        }
        
        internal static string BaseUrl => $"https://{Address}";

        public static async UniTask<OperationResult> UpdateGameServerStateAsync(GameServerState state,
            CancellationToken token = default)
        {
            OperationResult operationResult =
                await UpdateGameServerStateWebRequest.DoViaHandler(state, RequestTimeOut, RequestCountOfAttempt, token);
            
            return operationResult;
            
        }

        public static async UniTask<OperationResult<FullGameServersData>> ClientGetAllServersAsync(string version,
            CancellationToken token = default)
        {
            OperationResult<FullGameServersData> operationResult =
                await ClientGetAllServersWebRequest.DoViaHandler(version, RequestTimeOut, RequestCountOfAttempt, token);
            
            operationResult.Result = FullGameServersData.SortById(operationResult.Result);
            return operationResult;
        }

        public static async UniTask<OperationResult> DevGetAllServersAsync(CancellationToken token = default)
        {
            OperationResult operationResult =
                await GetAllServersWebRequest.DoViaHandler(RequestTimeOut, RequestCountOfAttempt, token);
            
            return operationResult;
        }

        public static async UniTask<OperationResult> UpdateMasterServerConfigAsync(MasterServerConfig newConfig,
            CancellationToken token = default)
        {
            OperationResult operationResult =
                await UpdateConfigWebRequest.DoViaHandler(newConfig, RequestTimeOut, RequestCountOfAttempt, token);
            
            return operationResult;
        }
    }
}