namespace DDKVS.Core.Storage
{
    public interface IKey
    {
        string Value { get; }
        uint HashCode { get; }
    }
}