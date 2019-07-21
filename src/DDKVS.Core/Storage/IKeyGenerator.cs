using System.Threading.Tasks;

namespace DDKVS.Core.Storage
{
    public interface IKeyGenerator
    {
        Task<IKey> GenerateKeyAsync(); // This is async because it may need to contact the cluster to generate a key. (TBD)
    }
}