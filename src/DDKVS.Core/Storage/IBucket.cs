using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DDKVS.Core.Storage
{
    public interface IBucket
    {
        Task<JToken> GetAsync(IKey key);
        Task<JToken> AddAsync(IKey key, JToken value);
        Task RemoveAsync(IKey key);
        Task<JToken> UpdateAsync(IKey key, JToken value);
        Task<JToken> AddOrUpdate(IKey key, JToken value);
        Task<IEnumerable<IKey>> ListKeysAsync();
    }
}