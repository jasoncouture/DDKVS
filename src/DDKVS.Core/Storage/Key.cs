using Newtonsoft.Json;

namespace DDKVS.Core.Storage
{
    public class Key : IKey
    {
        [JsonConstructor]
        public Key(string value, uint hashCode)
        {
            Value = value;
            HashCode = hashCode;
        }
        public string Value { get; }
        public uint HashCode { get; }
    }
}