using System.Threading;
using Cysharp.Threading.Tasks;

namespace DoodleObby._Multiplayer.NetworkOperations
{
    public interface INetworkOperation<T> where T : OperationResult
    {
        T FailedResult { get; }

        UniTask<T> Do(CancellationToken token = default);
        void Abort();
    }

    public abstract class NetworkOperation : INetworkOperation<OperationResult>
    {
        public OperationResult FailedResult => new(false);

        public abstract UniTask<OperationResult> Do(CancellationToken token = default);
        public abstract void Abort();
    }

    public class OperationResult
    {
        public static OperationResult Failed => new(false);
        public static OperationResult Successful => new(true);
        
        public bool IsSuccessful { get; }
        public bool IsFailed { get; }
        
        public OperationResult(bool isSuccessful)
        {
            IsSuccessful = isSuccessful;
            IsFailed = !isSuccessful;
        }
    }

    public class OperationResult<T> : OperationResult
    {
        public T Result { get; set; }

        public OperationResult(bool isSuccessful, T result) : base(isSuccessful)
        {
            Result = result;
        }
    }
}