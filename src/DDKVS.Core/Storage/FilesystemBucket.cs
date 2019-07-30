using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DDKVS.Core.Storage
{
    public class ApiBucket : IBucket
    {
        public HttpClient HttpClient { get; }
        public uint BucketId { get; }
        public IKey NamespaceKey { get; }
        public ApiBucket(HttpClient httpClient, uint bucketId, IKey namespaceKey)
        {
            NamespaceKey = namespaceKey;
            BucketId = bucketId;
            HttpClient = httpClient;
        }

        public async Task<JToken> AddAsync(IKey key, JToken value, CancellationToken cancellationToken)
        {
            return await WriteOperationAsync(key, value, OperationType.Add, cancellationToken);
        }

        private enum OperationType
        {
            Add,
            Update,
            AddOrUpdate
        }
        private string EscapeUri(string uri)
        {
            return uri;
        }
        private string EscapeUri(FormattableString formattableString)
        {
            string[] args = new string[formattableString.ArgumentCount];
            for (var x = 0; x < formattableString.ArgumentCount; x++)
            {
                args[x] = Uri.EscapeDataString($"{formattableString.GetArgument(x)}");
            }

            return string.Format(formattableString.Format, args);
        }
        private async Task<JToken> WriteOperationAsync(IKey key, JToken value, OperationType operationType, CancellationToken cancellationToken)
        {
            var requestObject = JObject.FromObject(new
            {
                key,
                value
            });
            var content = new StringContent(requestObject.ToString(Formatting.None), Encoding.UTF8, "application/json");
            var url = EscapeUri($"api/v1/bucket/{NamespaceKey.Value}/key/{key.Value}/");
            switch (operationType)
            {
                case OperationType.Add:
                    url = url + "add";
                    break;
                case OperationType.Update:
                    url = url + "update";
                    break;
                case OperationType.AddOrUpdate:
                    url = url + "overwrite";
                    break;
                default:
                    throw new InvalidOperationException($"Unknown operation type: {operationType:G}");
            }
            var response = await HttpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return JToken.Parse(await response.Content.ReadAsStringAsync());
        }

        public Task<JToken> AddOrUpdate(IKey key, JToken value, CancellationToken cancellationToken)
        {
            return WriteOperationAsync(key, value, OperationType.AddOrUpdate, cancellationToken);
        }

        public async Task<JToken> GetAsync(IKey key, CancellationToken cancellationToken)
        {
            var response = await HttpClient.GetAsync(EscapeUri($"api/v1/bucket/{NamespaceKey.Value}/key/{key.Value}"));
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return JToken.Parse(await response.Content.ReadAsStringAsync());
        }

        public async Task<IEnumerable<IKey>> ListKeysAsync(CancellationToken cancellationToken)
        {
            //api/v1/bucket/list/{BucketId}
            var response = await HttpClient.GetAsync(EscapeUri($"api/v1/bucket/{NamespaceKey.Value}/list/{BucketId}"));
            response.EnsureSuccessStatusCode();
            var keyListContainer = JObject.Parse(await response.Content.ReadAsStringAsync()).ToObject<BucketKeyListContainer>();
            return keyListContainer.Keys;
        }

        public class BucketKeyListContainer
        {
            public List<IKey> Keys { get; }
        }

        public async Task RemoveAsync(IKey key, CancellationToken cancellationToken)
        {
            var response = await HttpClient.DeleteAsync(EscapeUri($"api/v1/bucket/{NamespaceKey.Value}/key/{key.Value}"));
            response.EnsureSuccessStatusCode();
        }

        public Task<JToken> UpdateAsync(IKey key, JToken value, CancellationToken cancellationToken)
        {
            return WriteOperationAsync(key, value, OperationType.Update, cancellationToken);
        }
    }
    public class FilesystemBucket : IBucket
    {
        public string BasePath { get; }
        public uint BucketId { get; }
        public IKeyHasher KeyHasher { get; }
        public FilesystemBucket(string basePath, uint bucketId, IKeyHasher keyHasher)
        {
            KeyHasher = keyHasher;
            BasePath = basePath;
            BucketId = bucketId;
        }
        public async Task<JToken> GetAsync(IKey key, CancellationToken cancellationToken)
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

        public async Task<JToken> AddAsync(IKey key, JToken value, CancellationToken cancellationToken)
        {
            var filePath = GetFilePathForKey(key);
            if (File.Exists(filePath))
                throw new IOException("File exists");
            await AddOrUpdate(key, value, cancellationToken);
            return value;
        }

        public Task RemoveAsync(IKey key, CancellationToken cancellationToken)
        {
            var filePath = GetFilePathForKey(key);
            if (File.Exists(filePath))
                File.Delete(filePath);
            return Task.CompletedTask;
        }

        public async Task<JToken> UpdateAsync(IKey key, JToken value, CancellationToken cancellationToken)
        {
            var filePath = GetFilePathForKey(key);
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            return await AddOrUpdate(key, value, cancellationToken);
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

        public async Task<JToken> AddOrUpdate(IKey key, JToken value, CancellationToken cancellationToken)
        {
            var filePath = GetFilePathForKey(key);

            if (value is JObject obj)
            {
                obj["$_metadata"] = CreateMetadata(key);
            }

            await File.WriteAllTextAsync(filePath, value.ToString(Formatting.None));
            return value;
        }

        public Task<IEnumerable<IKey>> ListKeysAsync(CancellationToken cancellationToken)
        {
            // TODO
            var directory = new DirectoryInfo(BasePath);
            return Task.FromResult(EnumerateKeysFromFiles(directory.EnumerateFiles("*.json")));
        }

        private IEnumerable<IKey> EnumerateKeysFromFiles(IEnumerable<FileInfo> files)
        {
            foreach (var name in files.Select(i => Path.ChangeExtension(i.Name, null)))
            {
                IKey next;
                try
                {
                    next = KeyHasher.ComputeHash(name);
                }
                catch
                {
                    continue;
                }
                yield return next;
            }
        }
    }
}