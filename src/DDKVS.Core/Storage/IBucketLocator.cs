using System.Threading.Tasks;

namespace DDKVS.Core.Storage
{
    public interface IBucketLocator
    {
        Task<IBucket> GetBucketAsync(IKey namespaceKey, IKey key);
        Task<IBucket> GetBucketAsync(IKey namespaceKey, uint hashCode);
    }
}