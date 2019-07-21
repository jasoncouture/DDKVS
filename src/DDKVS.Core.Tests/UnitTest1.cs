using System;
using System.Threading.Tasks;
using DDKVS.Core.Storage;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DDKVS.Core.Tests
{
    public class UnitTest1
    {
        private Sha256KeyHasher KeyHasher { get; } = new Sha256KeyHasher(new KeyValidator());
        private FilesystemBucketLocator BucketLocator { get; } = new FilesystemBucketLocator(".", 4);
        [Fact]
        public void Sha256KeyHasher_KeyGeneration_ResultsConsistent()
        {
            var key1 = KeyHasher.ComputeHash("test-key");
            var key2 = KeyHasher.ComputeHash("test-key");
            Assert.NotSame(key1, key2);
            Assert.Equal(key1.Value, key2.Value);
            Assert.Equal(key1.HashCode, key2.HashCode);
        }

        [Fact]
        public async Task Bucket_AddOrUpdate_NeverThrows()
        {
            JObject toStore = JObject.FromObject(new
            {
                Test = "testing"
            });

            var namespaceKey = KeyHasher.ComputeHash("default-namespace");
            var objectKey = KeyHasher.ComputeHash("1");

            var bucket = await BucketLocator.GetBucketAsync(namespaceKey, objectKey);
            await bucket.AddOrUpdate(objectKey, toStore);
            await bucket.AddOrUpdate(objectKey, toStore);
        }
    }
}
