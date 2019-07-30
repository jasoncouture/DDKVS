using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DDKVS.Core.Storage;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DDKVS.Core.Tests
{
    public class BucketTests
    {
        public BucketTests()
        {
            KeyHasher = new Sha256KeyHasher(new KeyValidator());
            BucketLocator = new FilesystemBucketLocator(KeyHasher, ".", 4);
        }
        private Sha256KeyHasher KeyHasher { get; }
        private FilesystemBucketLocator BucketLocator { get; }
        [Fact]
        public void Sha256KeyHasher_KeyGeneration_ResultsConsistent()
        {
            var key1 = KeyHasher.ComputeHash("test-key1");
            var key2 = KeyHasher.ComputeHash("test-key1");
            Assert.NotSame(key1, key2);
            Assert.Equal(key1.Value, key2.Value);
            Assert.Equal(key1.HashCode, key2.HashCode);
        }

        [Fact]
        public async Task Bucket_AddOrUpdate_NeverThrows()
        {
            JObject toStore = JObject.FromObject(new
            {
                Test = "testing123"
            });

            var namespaceKey = KeyHasher.ComputeHash("default-namespace");
            var objectKey = KeyHasher.ComputeHash("1");

            var bucket = await BucketLocator.GetBucketAsync(namespaceKey, objectKey);
            await bucket.AddOrUpdate(objectKey, toStore, CancellationToken.None);
            await bucket.AddOrUpdate(objectKey, toStore, CancellationToken.None);
        }
    }
}
