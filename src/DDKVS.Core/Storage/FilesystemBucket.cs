using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DDKVS.Core.Storage
{
    public class FilesystemBucket : IBucket
    {
        public string BasePath { get; }
        public uint BucketId { get; }
        public FilesystemBucket(string basePath, uint bucketId)
        {
            BasePath = basePath;
            BucketId = bucketId;
        }
        public async Task<JToken> GetAsync(IKey key)
        {
            var filePath = GetFilePathForKey(key);
            if (File.Exists(filePath))
            {
                return JToken.Parse(await File.ReadAllTextAsync(filePath));
            }
            return null;
        }

        private string GetFilePathForKey(IKey key)
        {
            return Path.Combine(BasePath, $"{key.Value}.json");
        }

        public async Task<JToken> AddAsync(IKey key, JToken value)
        {
            var filePath = GetFilePathForKey(key);
            if (File.Exists(filePath))
                throw new IOException("File exists");
            await AddOrUpdate(key, value);
            return value;
        }

        public Task RemoveAsync(IKey key)
        {
            var filePath = GetFilePathForKey(key);
            if (File.Exists(filePath))
                File.Delete(filePath);
            return Task.CompletedTask;
        }

        public async Task<JToken> UpdateAsync(IKey key, JToken value)
        {
            var filePath = GetFilePathForKey(key);
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            return await AddOrUpdate(key, value);
        }

        private JObject CreateMetadata(IKey key)
        {
            return JObject.FromObject(new
            {
                id = key.Value,
                hashCode = key.HashCode,
                modified = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                bucket = BucketId
            });
        }

        public async Task<JToken> AddOrUpdate(IKey key, JToken value)
        {
            var filePath = GetFilePathForKey(key);

            if (value is JObject obj)
            {
                obj["$_metadata"] = CreateMetadata(key);
            }

            await File.WriteAllTextAsync(filePath, value.ToString(Formatting.None));
            return value;
        }

        public Task<IEnumerable<IKey>> ListKeysAsync()
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}