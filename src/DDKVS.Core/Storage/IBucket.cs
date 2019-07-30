using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DDKVS.Core.Storage
{
    public interface IBucket
    {
        Task<JToken> GetAsync(IKey key, CancellationToken cancellationToken);
        Task<JToken> AddAsync(IKey key, JToken value, CancellationToken cancellationToken);
        Task RemoveAsync(IKey key, CancellationToken cancellationToken);
        Task<JToken> UpdateAsync(IKey key, JToken value, CancellationToken cancellationToken);
        Task<JToken> AddOrUpdate(IKey key, JToken value, CancellationToken cancellationToken);
        Task<IEnumerable<IKey>> ListKeysAsync(CancellationToken cancellationToken);
    }
}