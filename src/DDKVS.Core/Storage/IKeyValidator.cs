namespace DDKVS.Core.Storage
{
    public interface IKeyValidator
    {
        bool IsValid(string key);
        void Validate(string key);
    }
}