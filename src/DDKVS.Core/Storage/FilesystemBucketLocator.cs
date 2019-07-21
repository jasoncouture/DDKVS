using System.IO;
using System.Threading.Tasks;

namespace DDKVS.Core.Storage
{
    public class FilesystemBucketLocator : IBucketLocator
    {
        public uint Buckets { get; }
        public string BasePath { get; }
        public FilesystemBucketLocator(string rootPath = ".", uint buckets = 65536u)
        {
            Buckets = buckets;
            BasePath = Directory.CreateDirectory(Path.GetFullPath(rootPath)).FullName;
        }
        public Task<IBucket> GetBucketAsync(IKey namespaceKey, IKey key)
        {
            return GetBucketAsync(namespaceKey, key.HashCode);
        }

        private string GetBucketPath(IKey namespaceKey, uint keyHashCode)
        {
            return Path.Combine(BasePath, namespaceKey.HashCode.ToString(), namespaceKey.Value, keyHashCode.ToString());
        }

        public async Task<IBucket> GetBucketAsync(IKey namespaceKey, uint hashCode)
        {
            var bucketId = hashCode % Buckets;
            var bucketBasePath = Directory.CreateDirectory(GetBucketPath(namespaceKey, bucketId)).FullName;
            return new FilesystemBucket(bucketBasePath, bucketId);
        }
    }
}